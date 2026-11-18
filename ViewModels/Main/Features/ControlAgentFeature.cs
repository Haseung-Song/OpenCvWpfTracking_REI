using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>제어 연결과 재연결에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IControlAgentFeatureContext
    {
        ref double? _activePanAbsoluteTarget { get; }

        ref double? _activeTiltAbsoluteTarget { get; }

        string _connectionSessionId { get; set; }

        ref global::System.Threading.CancellationTokenSource _controlAgentReconnectCts { get; }

        IControlCommandServicePort _controlCommandService { get; }

        short _currentEoFocus { get; set; }

        short _currentEoZoom { get; set; }

        ushort _currentIrFocus { get; set; }

        ushort _currentIrZoom { get; set; }

        double _currentPan { get; set; }

        byte _currentPowerStatus { get; set; }

        double _currentTilt { get; set; }

        ref int _eoFocusReceiveSequence { get; }

        ref long _eoLensStatusVersion { get; }

        Stopwatch _focusLogStopwatch { get; }

        ref long _function01ReceiveCount { get; }

        bool _hasWebAgentTiltLimits { get; set; }

        ref long _irLensStatusVersion { get; }

        bool _isDeviceConnectionRequested { get; set; }

        ILAPacketParserPort _laPacketParser { get; }

        long _lastEoFocusCommandElapsedMs { get; set; }

        string _lastEoFocusCommandName { get; set; }

        ref global::System.DateTime _lastLaExtendedStatusLogTime { get; }

        ref global::System.DateTime _lastLaStatusLogTime { get; }

        ref double? _lastPanAbsoluteTarget { get; }

        ITcpClientServicePort _laTcpService { get; }

        ref long _panTiltStatusVersion { get; }

        ref long _ptzfStatusUpdateCount { get; }

        ref global::System.Tuple<double, double, long> _videoCenterPose { get; }

        ref int _webAgentTiltLimitQueryRequested { get; }

        double _webAgentTiltMaximum { get; set; }

        double _webAgentTiltMinimum { get; set; }

        void ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime = null, string sourceFunction = "0x31");

        void BeginConnectedControlAgentSession();

        string ControlAgentConnectionStatusColor { get; set; }

        string ControlAgentConnectionStatusText { get; set; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        string CurrentControlPowerText { get; }

        string CurrentEoFocusText { get; }

        string CurrentEoZoomText { get; }

        string CurrentIrFocusText { get; }

        string CurrentIrZoomText { get; }

        string CurrentLaPresetSnapshotText { get; }

        double CurrentMoveControlTiltMaximum { get; }

        double CurrentMoveControlTiltMinimum { get; }

        string CurrentPanText { get; }

        string CurrentPowerText { get; }

        string CurrentPresetSnapshotText { get; }

        string CurrentTiltText { get; }

        byte EffectivePanTiltSpeedLevel { get; }

        void EndConnectedControlAgentSession();

        string EnvironmentEoFocusStatusText { get; }

        string EnvironmentEoZoomStatusText { get; }

        string EnvironmentIrFocusStatusText { get; }

        string EnvironmentIrZoomStatusText { get; }

        string EoSourceAddress { get; set; }

        string EoStatusText { get; set; }

        int GetCurrentIrFocusStandardPosition();

        int GetCurrentIrZoomStandardPosition();

        double GetTiltDifference(double currentTilt, double targetTilt);

        void InitializeWebAgentSession();

        string IrSourceAddress { get; set; }

        string IrStatusText { get; set; }

        string LrfDistanceText { get; set; }

        void MarkLegacyWebAgentStatusReceived(ushort panRaw, ushort tiltRaw);

        void MarkLensEvidence(bool ir);

        void MarkPresetLensEvidence(bool ir, bool zoom, bool focus);

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanTiltCoordinateRangeText { get; }

        string PanTiltSpeedDisplayText { get; }

        void ParseWebAgentTelemetryPacket(LaResponsePacket packet);

        void ReleaseAllControlInputs(string reason);

        void ResetDigitalZoomSession();

        string RooftopEoFocusStatusText { get; }

        string RooftopEoZoomStatusText { get; }

        string RooftopIrFocusStatusText { get; }

        string RooftopIrZoomStatusText { get; }

        string SessionControlIp { get; }

        string SessionControlPort { get; }

        void SetPositionConnectionState(bool connected);

        void StartSystemTimePollingAfterConnection();

        void StopSystemTimePolling(string reason);

        string TiltAbsoluteRangeLabel { get; }

        double? TiltAbsoluteValue { get; set; }

        void UpdatePositionIrLens(int irZoom, int irFocus);

        void UpdatePositionPtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus);
    }

    /// <summary>제어 연결과 재연결 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class ControlAgentFeature
    {

        #region [협력 객체와 초기화]

        private readonly IControlAgentFeatureContext _context;

        internal ControlAgentFeature(IControlAgentFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal int _eoStatusNotificationPending;

        internal int _eoPanNotificationDirty;

        internal int _eoTiltNotificationDirty;

        internal int _eoZoomNotificationDirty;

        internal int _eoFocusNotificationDirty;

        internal int _eoPowerNotificationDirty;

        internal int _irStatusNotificationPending;

        internal bool _isEoRtspAddressValid;

        internal bool _isIrRtspAddressValid;

        internal string _lastIrUiStatusSignature;

        #endregion

        #region [동작 처리]

        internal void SetControlAgentConnectionStatus(string statusText, string statusColor, CancellationToken token = default(CancellationToken))
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var dispatcher = App.Current?.Dispatcher;

            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                if (!dispatcher.HasShutdownStarted)
                {
                    dispatcher.Invoke(() => SetControlAgentConnectionStatus(statusText, statusColor, token));
                }

                return;
            }

            bool isConnected = string.Equals(statusText, "Connected", StringComparison.OrdinalIgnoreCase);

            _context.SetPositionConnectionState(isConnected);

            if (isConnected)
            {
                _context.BeginConnectedControlAgentSession();

                RequestWebAgentTiltLimitsAfterConnection();

                _context.StartSystemTimePollingAfterConnection();
            }
            else
            {
                _context.EndConnectedControlAgentSession();

                _context.StopSystemTimePolling("Control Agent disconnected");

                ResetWebAgentTiltLimits();
            }

            /*
             * 자동 재연결 Loop는 백그라운드 Task에서 실행되므로
             * UI Dispatcher를 통해 바인딩 값을 변경한다.
             */
            if (App.Current?.Dispatcher == null)
            {
                _context.ControlAgentConnectionStatusText = statusText;
                _context.ControlAgentConnectionStatusColor = statusColor;

                return;
            }

            if (App.Current.Dispatcher.CheckAccess())
            {
                _context.ControlAgentConnectionStatusText = statusText;
                _context.ControlAgentConnectionStatusColor = statusColor;

                return;
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                _context.ControlAgentConnectionStatusText = statusText;
                _context.ControlAgentConnectionStatusColor = statusColor;
            });
        }

        internal async Task<bool> ConnectLaAsync()
        {
            ConsoleLogHelper.InfoSection("CONTROL AGENT", "TCP connect workflow started", string.Empty, $"TARGET : {_context.ControlAgentIp}:{_context.ControlAgentPortText}");

            Console.WriteLine();

            /*
            * Connecting 상태가 시작된 시각을 기록한다.
            *
            * 실제 TCP 연결이 너무 빨리 완료되더라도
            * 최소 표시시간을 계산하기 위해 사용한다.
            */
            Stopwatch connectingStopwatch = Stopwatch.StartNew();

            /*
             * 연결 버튼 클릭 즉시
             * UI 상태를 Connecting으로 변경한다.
             */
            SetControlAgentConnectionStatus("Connecting", "#FFD166");

            ConsoleLogHelper.PrintSection("[CONTROL AGENT]", "Connect Start");

            /*
             * UI 입력값 검증
             *
             * IP 빈값, Port 문자 입력,
             * Port 범위 오류 등을 검사한다.
             */
            if (!TryGetControlAgentEndpoint(out string targetIp, out int targetPort))
            {
                SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

                return false;
            }

            /*
             * 이전 입력값으로 실행 중인 자동 재연결 Loop가 있다면
             * 새 연결 시도 전에 정리한다.
             */
            _context._controlAgentReconnectCts?.Cancel();

            _context._controlAgentReconnectCts?.Dispose();

            _context._controlAgentReconnectCts = null;

            try
            {
                bool result = await _context._laTcpService.ConnectAsync(targetIp, targetPort);

                /*
                * 실제 TCP 연결에 걸린 시간을 제외하고
                * Connecting 상태 최소 표시시간이 남아 있으면 기다린다.
                *
                * Task.Delay를 await하므로 UI Thread를 막지 않는다.
                */
                int remainingDisplayMs = ControlAgentConnectingMinimumDisplayMs - (int)connectingStopwatch.ElapsedMilliseconds;

                if (remainingDisplayMs > 0)
                {
                    await Task.Delay(remainingDisplayMs);
                }

                /*
                 * 연결 결과에 따라 UI 상태 갱신
                 */
                if (result)
                {
                    SetControlAgentConnectionStatus("Connected", "#55D187");
                }
                else
                {
                    if (_context._isDeviceConnectionRequested)
                    {
                        SetControlAgentConnectionStatus("Reconnecting", "#FFD166");
                    }
                    else
                    {
                        SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");
                    }
                }

                ConsoleLogHelper.StateSection("CONTROL AGENT", "Connect completed", string.Empty, $"RESULT : {result}", $"TARGET : {targetIp}:{targetPort}");

                /*
                 * 연결 실패 상태이지만
                 * 사용자가 장비 연결 유지를 요청한 경우
                 * 자동 재연결을 시작한다.
                 */
                if (!result && _context._isDeviceConnectionRequested)
                {
                    StartControlAgentReconnect(targetIp, targetPort);
                }

                return result;
            }
            catch (Exception ex)
            {
                /*
                 * 연결 예외 발생 시에도
                 * 앱이 종료되지 않도록 상태만 갱신한다.
                 */
                if (_context._isDeviceConnectionRequested)
                {
                    SetControlAgentConnectionStatus("Reconnecting", "#FFD166");
                }
                else
                {
                    SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");
                }

                Console.WriteLine();

                Console.WriteLine("[CONTROL AGENT] Connect Exception");

                Console.WriteLine($"[CONTROL AGENT] {ex.Message}");

                ConsoleLogHelper.PrintLine();

                if (_context._isDeviceConnectionRequested)
                {
                    StartControlAgentReconnect(targetIp, targetPort);
                }

                return false;
            }
        }

        internal bool TryGetRtspEndpoints(out string eoRtspAddress, out string irRtspAddress)
        {
            eoRtspAddress = _context.EoSourceAddress?.Trim();

            irRtspAddress = _context.IrSourceAddress?.Trim();

            _isEoRtspAddressValid = IsValidRtspAddress(eoRtspAddress);

            _isIrRtspAddressValid = IsValidRtspAddress(irRtspAddress);

            if (!_isEoRtspAddressValid)
            {
                _context.EoStatusText = string.IsNullOrWhiteSpace(eoRtspAddress) ? "[EO] Not Configured" : "[EO] Invalid RTSP Address";

                Console.WriteLine();

                Console.WriteLine("[EO RTSP] Connect Failed : " + "Invalid RTSP address.");

                Console.WriteLine($"[EO RTSP] INPUT : " + $"{ConsoleLogHelper.MaskRtspPassword(_context.EoSourceAddress)}");

                ConsoleLogHelper.PrintLine();
            }

            if (!_isIrRtspAddressValid)
            {
                _context.IrStatusText = string.IsNullOrWhiteSpace(irRtspAddress) ? "[IR] Not Configured" : "[IR] Invalid RTSP Address";

                Console.WriteLine();

                Console.WriteLine("[IR RTSP] Connect Failed : " + "Invalid RTSP address.");

                Console.WriteLine($"[IR RTSP] INPUT : " + $"{ConsoleLogHelper.MaskRtspPassword(_context.IrSourceAddress)}");

                ConsoleLogHelper.PrintLine();
            }

            if (_isEoRtspAddressValid && _isIrRtspAddressValid && string.Equals(eoRtspAddress, irRtspAddress, StringComparison.OrdinalIgnoreCase))
            {
                _isEoRtspAddressValid = false;
                _isIrRtspAddressValid = false;

                _context.EoStatusText = "[EO] RTSP Config Error";
                _context.IrStatusText = "[IR] RTSP Config Error";

                ConsoleLogHelper.Warning("RTSP CONFIG ERROR", "EO and IR RTSP URLs must be different");

                return false;
            }

            return _isEoRtspAddressValid || _isIrRtspAddressValid;
        }

        internal static bool IsValidRtspAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            bool supportedScheme = string.Equals(uri.Scheme, "rtsp", StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Scheme, "rtsps", StringComparison.OrdinalIgnoreCase);

            // RTSP는 표준 포트 554를 생략할 수 있다. .NET Uri는 RTSP의
            // 기본 포트를 내장하지 않아 생략 시 Port=-1을 반환하므로 허용한다.
            bool validPort = uri.Port == -1 || (uri.Port > 0 && uri.Port <= 65535);

            return supportedScheme && !string.IsNullOrWhiteSpace(uri.Host) && validPort;
        }

        internal bool TryGetControlAgentEndpoint(out string ipAddress, out int port)
        {
            ipAddress = _context.SessionControlIp?.Trim();

            port = 0;

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

                Console.WriteLine("[CONTROL AGENT] Connect Failed : IP is empty.");

                return false;
            }

            if (!int.TryParse(_context.SessionControlPort?.Trim(), out port))
            {
                SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

                Console.WriteLine("[CONTROL AGENT] Connect Failed : " + "Port must be a number.");

                return false;
            }

            if (port < 1 || port > 65535)
            {
                SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

                Console.WriteLine("[CONTROL AGENT] Connect Failed : " + "Port range must be 1 ~ 65535.");

                return false;
            }

            return true;
        }

        internal void OnControlAgentConnectionClosed()
        {
            // 재연결된 세션에 이전에 누른 키 입력을 재전송하지 않는다.
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                _context.ReleaseAllControlInputs("Control TCP lost");

                _context.ResetDigitalZoomSession();
            }));

            if (!_context._isDeviceConnectionRequested)
            {
                SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

                return;
            }

            SetControlAgentConnectionStatus("Reconnecting", "#FFD166");

            string targetIp = _context.SessionControlIp?.Trim();

            if (string.IsNullOrWhiteSpace(targetIp))
            {
                SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

                return;
            }

            if (!int.TryParse(_context.SessionControlPort?.Trim(), out int targetPort) || targetPort < 1 || targetPort > 65535)
            {
                SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

                return;
            }

            StartControlAgentReconnect(targetIp, targetPort);
        }

        internal void StartControlAgentReconnect(string ipAddress, int port)
        {
            if (_context._controlAgentReconnectCts != null && !_context._controlAgentReconnectCts.IsCancellationRequested)
            {
                return;
            }

            _context._controlAgentReconnectCts?.Dispose();

            _context._controlAgentReconnectCts = new CancellationTokenSource();

            CancellationTokenSource reconnectCts = _context._controlAgentReconnectCts;

            CancellationToken token = reconnectCts.Token;

            _ = Task.Run(async () =>
            {
                const int reconnectDelayMs = 1500;

                int retryCount = 0;

                try
                {
                    while (_context._isDeviceConnectionRequested && !token.IsCancellationRequested && !_context._laTcpService.IsConnected)
                    {
                        retryCount++;

                        SetControlAgentConnectionStatus("Reconnecting", "#FFD166", token);

                        Console.WriteLine($"[CONTROL AGENT] Reconnect Try " + $"({retryCount}) : " + $"{ipAddress}:{port}");

                        bool connected = await _context._laTcpService.ConnectAsync(ipAddress, port);

                        // 취소된 재연결이 연결 상태를 복원하거나 새 세션 상태를 지우지 못하게 한다.
                        if (token.IsCancellationRequested || !_context._isDeviceConnectionRequested)
                        {
                            return;
                        }

                        if (connected)
                        {
                            SetControlAgentConnectionStatus("Connected", "#55D187", token);

                            // 재접속한 TCP 연결은 호환 모드에서 시작하므로 지원 조회부터 다시 수행한다.
                            string reconnectedSession = _context._connectionSessionId;

                            var reconnectDispatcher = App.Current?.Dispatcher;

                            if (reconnectDispatcher != null && !reconnectDispatcher.HasShutdownStarted)
                            {
                                _ = reconnectDispatcher.BeginInvoke(new Action(() =>
                                {
                                    if (!token.IsCancellationRequested && reconnectedSession == _context._connectionSessionId && _context._laTcpService.IsConnected)
                                    {
                                        _context.InitializeWebAgentSession();
                                    }
                                }));
                            }

                            Console.WriteLine("[CONTROL AGENT] Reconnect Success");

                            return;
                        }

                        await Task.Delay(reconnectDelayMs, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 명시적 연결 해제 또는 새 세션이 현재 화면 상태의 소유권을 가진다.
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    SetControlAgentConnectionStatus("Disconnected", "#FF6B6B", token);

                    Console.WriteLine("[CONTROL AGENT] Reconnect Exception : " + ex.Message);
                }
                finally
                {
                    if (ReferenceEquals(Interlocked.CompareExchange(ref _context._controlAgentReconnectCts, null, reconnectCts), reconnectCts))
                    {
                        reconnectCts.Dispose();
                    }
                }
            });
        }

        internal void OnLaMessageReceived(byte[] data, DateTime receiveTime)
        {
            // 수신된 [byte[] 데이터]를 [CONTROL AGENT] 응답 [Packet] 목록으로 변환.
            //
            List<LaResponsePacket> packets = _context._laPacketParser.Parse(data);

            // 분리된 [Packet]을 하나씩 처리
            // <summary></summary>
            //
            foreach (LaResponsePacket packet in packets)
            {
                HandleLaPacket(packet);
            }
        }

        internal void HandleLaPacket(LaResponsePacket packet)
        {
            // [Header] / [Checksum] 검증 실패 시 처리하지 않음
            //
            if (!packet.IsValid)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine("[LA PACKET] Invalid Checksum");

                ConsoleLogHelper.PrintLine();

                return;
            }

            bool canPrintLog = CanPrintLaLog();

            bool canPrintExtendedStatusLog = CanPrintLaExtendedStatusLog();

            switch (packet.Function)
            {
                case 0x02:
                    ParseWebAgentTiltLimitPacket(packet.RawData);
                    break;
                case 0x01:

                    // [Pan] / [Tilt] / [Zoom] / [Focus] 상태 정보
                    //
                    if (!canPrintLog)
                    {
                        ParseLaStatusPacket(packet.RawData, false);

                        return;
                    }

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine("[LA PACKET] [Pan] / [Tilt] / [Zoom] / [Focus] Status");

                    Console.WriteLine();

                    ParseLaStatusPacket(packet.RawData, true);

                    ConsoleLogHelper.PrintLine();
                    break;
                case 0x31:

                    // 2026-10-06: 기존 0x01 PTZF와 동일한 위치값에
                    // MCB/SCB 원본 상태 바이트를 추가한 고정 13Byte 응답.
                    ParseLaStatusPacket(packet.RawData, canPrintLog, false);

                    _context.ApplyDevicePowerStatus(packet.RawData[10], packet.RawData[11]);
                    break;
                case 0x07:

                    // [Function] [0x07]
                    //
                    // 열영상 카메라 [Zoom] / [Focus] 위치 상태 Packet
                    //
                    // Packet 구조:
                    //
                    // [0] 패킷 시작
                    // [1]  Function = 0x07
                    // [2] IR 줌 하위 바이트
                    // [3] IR 줌 상위 바이트
                    // [4] IR 초점 하위 바이트
                    // [5] IR 초점 상위 바이트
                    // [6] ~ [10] 상태 / 예약 영역
                    // [11] 검사 합계
                    //
                    // 장비에서 실제 수신되는 위치값은
                    // Little Endian 방식으로 확인된다.
                    //
                    // 예:
                    // D6 03 → 982
                    // E8 03 → 1000
                    //
                    ParseLaIrCameraStatusPacket(packet, canPrintExtendedStatusLog);
                    break;
                case 0xA1:

                    // 상태값은 모든 Packet마다 파싱하고,
                    // Console 로그만 설정된 주기로 제한한다.
                    //
                    ParseLaExtendedStatusPacket(packet.RawData, canPrintExtendedStatusLog);
                    break;
                case 0xA3:

                    // [Function] [0xA3]
                    //
                    // 현재 장비에서 주기적으로 수신되는
                    // 확장 상태 Packet
                    //
                    // 세부 의미 미확인
                    // Console 출력 생략
                    //
                    break;
                case 0x04:

                    // [LRF] 거리측정 응답 Packet
                    //
                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine("[LA PACKET] [LRF] Distance Packet");

                    Console.WriteLine();

                    ParseLrfDistancePacket(packet.RawData);

                    ConsoleLogHelper.PrintLine();
                    break;
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                case 0x2F:
                case 0x30:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                    _context.ParseWebAgentTelemetryPacket(packet);
                    break;
                default:

                    // 정의되지 않은 [Function] 번호
                    //
                    // [LRF] / [GPS] / 기타 확장 [Packet] 확인용으로
                    // 로그 제한 없이 출력한다.
                    //
                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine($"[LA PACKET] Unknown Function: 0x{packet.Function:X2}");

                    Console.WriteLine();
                    foreach (byte b in packet.RawData)
                    {
                        Console.Write($"{b:X2} ");
                    }

                    Console.WriteLine();

                    ConsoleLogHelper.PrintLine();
                    break;
            }
        }

        internal void RequestWebAgentTiltLimitsAfterConnection()
        {
            if (_context._controlCommandService == null || !_context._controlCommandService.UseUnsignedWebAgentTiltCoordinates || Interlocked.Exchange(ref _context._webAgentTiltLimitQueryRequested, 1) != 0)
            {
                return;
            }

            bool sent = _context._controlCommandService.RequestWebAgentTiltLimits();

            ConsoleLogHelper.Command("WEB AGENT TILT LIMIT", "Query requested / " + "PACKET=FF 01 00 B5 02 00 B8 / " + "RESULT=" + sent);

            if (!sent)
            {
                Interlocked.Exchange(ref _context._webAgentTiltLimitQueryRequested, 0);
            }
        }

        internal void ParseWebAgentTiltLimitPacket(byte[] packet)
        {
            if (_context._controlCommandService == null || !_context._controlCommandService.UseUnsignedWebAgentTiltCoordinates)
            {
                ConsoleLogHelper.Warning("WEB AGENT TILT LIMIT", "Function 0x02 ignored outside WebAgent mode");

                return;
            }

            if (packet == null || packet.Length != 12)
            {
                ConsoleLogHelper.Warning("WEB AGENT TILT LIMIT", "Invalid response length");

                return;
            }

            ushort positiveRaw = (ushort)(packet[2] | packet[3] << 8);

            ushort negativeRaw = (ushort)(packet[4] | packet[5] << 8);

            double positiveLimit = positiveRaw / 100.0;

            double negativeLimit = -(negativeRaw / 100.0);

            // 손상되거나 다른 의미의 Function 0x02 패킷이 UI 이동 범위를
            // 비정상적으로 확장하지 않도록 TILT 물리 범위 내에서 검증한다.
            if (positiveLimit < 0.0 || positiveLimit > 180.0 || negativeLimit < -180.0 || negativeLimit > 0.0 || positiveRaw + negativeRaw == 0)
            {
                ConsoleLogHelper.Warning("WEB AGENT TILT LIMIT", "Invalid limit values / " + $"POS_RAW={positiveRaw} / NEG_RAW={negativeRaw}");

                return;
            }

            Action applyLimits = () =>
            {
                _context._webAgentTiltMinimum = negativeLimit;
                _context._webAgentTiltMaximum = positiveLimit;
                _context._hasWebAgentTiltLimits = true;

                if (_context.TiltAbsoluteValue.HasValue)
                {
                    _context.TiltAbsoluteValue = Clamp(_context.TiltAbsoluteValue.Value, _context.CurrentMoveControlTiltMinimum, _context.CurrentMoveControlTiltMaximum);
                }

                _context.OnPropertyChanged(nameof(_context.PanTiltCoordinateRangeText));

                _context.OnPropertyChanged(nameof(_context.TiltAbsoluteRangeLabel));
            };

            if (App.Current?.Dispatcher == null || App.Current.Dispatcher.CheckAccess())
            {
                applyLimits();
            }
            else
            {
                App.Current.Dispatcher.BeginInvoke(applyLimits, DispatcherPriority.DataBind);
            }

            ConsoleLogHelper.StateSection("WEB AGENT TILT LIMIT", "Limits updated", string.Empty, $"NEGATIVE={negativeLimit:F2}deg", $"POSITIVE={positiveLimit:F2}deg", "SOURCE=FUNCTION_0x02");
        }

        internal void ResetWebAgentTiltLimits()
        {
            Interlocked.Exchange(ref _context._webAgentTiltLimitQueryRequested, 0);

            if (!_context._hasWebAgentTiltLimits)
            {
                return;
            }

            Action resetLimits = () =>
            {
                _context._webAgentTiltMinimum = MoveControlTiltMinimum;
                _context._webAgentTiltMaximum = MoveControlTiltMaximum;
                _context._hasWebAgentTiltLimits = false;

                _context.OnPropertyChanged(nameof(_context.PanTiltCoordinateRangeText));

                _context.OnPropertyChanged(nameof(_context.TiltAbsoluteRangeLabel));
            };

            if (App.Current?.Dispatcher == null || App.Current.Dispatcher.CheckAccess())
            {
                resetLimits();
            }
            else
            {
                App.Current.Dispatcher.BeginInvoke(resetLimits, DispatcherPriority.DataBind);
            }
        }

        internal bool CanPrintLaLog()
        {
            if ((DateTime.Now - _context._lastLaStatusLogTime).TotalSeconds < LaLogIntervalSeconds)
            {
                return false;
            }

            _context._lastLaStatusLogTime = DateTime.Now;

            return true;
        }

        internal bool CanPrintLaExtendedStatusLog()
        {
            if ((DateTime.Now - _context._lastLaExtendedStatusLogTime).TotalSeconds < LaLogIntervalSeconds)
            {
                return false;
            }

            _context._lastLaExtendedStatusLogTime = DateTime.Now;

            return true;
        }

        internal void ParseLaStatusPacket(byte[] packet, bool printLog, bool countFunction01 = true)
        {
            const int requiredLength = 12;

            if (packet == null || packet.Length < requiredLength)
            {
                if (printLog)
                {
                }

                return;
            }

            if (countFunction01)
            {
                Interlocked.Increment(ref _context._function01ReceiveCount);
            }

            Interlocked.Increment(ref _context._ptzfStatusUpdateCount);

            ushort panUnsignedRaw = BitConverter.ToUInt16(packet, 2);

            short panSignedRaw = unchecked((short)panUnsignedRaw);

            ushort tiltUnsignedRaw = BitConverter.ToUInt16(packet, 4);

            short tiltSignedRaw = unchecked((short)tiltUnsignedRaw);

            short zoomRaw = BitConverter.ToInt16(packet, 6);

            short focusRaw = BitConverter.ToInt16(packet, 8);

            byte powerStatus = packet[10];

            // 2026-09-15: Web Agent 환경에서는 Function 0x01의 PT/EO Lens 값을
            // 실제 장비 상태의 기준으로 사용하고, 신규 0x28과의 충돌을 차단한다.
            _context.MarkLegacyWebAgentStatusReceived(panUnsignedRaw, tiltUnsignedRaw);

            /*
            * Focus 변화 비교용 이전값 저장
            *
            * _currentEoFocus를 갱신하기 전에
            * 반드시 기존 값을 먼저 보관한다.
            */
            short previousFocus = _context._currentEoFocus;

            double previousPan = _context._currentPan;

            double previousTilt = _context._currentTilt;

            short previousZoom = _context._currentEoZoom;

            byte previousPower = _context._currentPowerStatus;

            double panDegree = _context._controlCommandService.UseUnsignedWebAgentPanCoordinates ? ConvertWebAgentPanToGuiAngle(panUnsignedRaw / 100.0) : panSignedRaw / 100.0;

            double tiltDegree = _context._controlCommandService.UseUnsignedWebAgentTiltCoordinates ? NormalizeUnsignedWebAgentTilt(tiltUnsignedRaw / 100.0, _context._currentTilt) : tiltSignedRaw / 100.0;

            // -180도와 +180도는 동일한 물리 위치다.
            //
            // LA가 -180 명령 후에도 상태값을 +180으로 반환할 수 있으므로,
            // 상태값이 경계 ±180도이고 마지막 목표도 경계값인 경우에는
            // 사용자가 마지막으로 입력한 목표 부호를 표시값에 유지한다.
            //
            // 일반 위치에서는 수신 상태값을 그대로 사용한다.
            //
            if (Math.Abs(Math.Abs(panDegree) - 180.0) <= 0.05 && _context._lastPanAbsoluteTarget.HasValue && Math.Abs(Math.Abs(_context._lastPanAbsoluteTarget.Value) - 180.0) <= 0.05)
            {
                panDegree = _context._lastPanAbsoluteTarget.Value < 0.0 ? -180.0 : 180.0;
            }

            /*
             * 모든 0x01 패킷에서 상태값 갱신
             */
            _context._currentPan = panDegree;
            _context._currentTilt = tiltDegree;

            // 클릭 이동 보호에 사용할 수신 위치를 불변 스냅샷으로 보관한다.
            Interlocked.Exchange(ref _context._videoCenterPose, Tuple.Create(panDegree, tiltDegree, DateTime.UtcNow.Ticks));

            // 이동이 완료된 축은 더 이상 Slider 변경 시 목표를 재송신하지 않는다.
            double? activePanTarget = _context._activePanAbsoluteTarget;

            double? activeTiltTarget = _context._activeTiltAbsoluteTarget;

            if (activePanTarget.HasValue && Math.Abs(_context._currentPan - activePanTarget.Value) <= 0.03)
            {
                _context._activePanAbsoluteTarget = null;
            }

            if (activeTiltTarget.HasValue && Math.Abs(_context.GetTiltDifference(_context._currentTilt, activeTiltTarget.Value)) <= 0.03)
            {
                _context._activeTiltAbsoluteTarget = null;
            }

            Interlocked.Increment(ref _context._panTiltStatusVersion);

            _context._currentEoZoom = zoomRaw;
            _context._currentEoFocus = focusRaw;

            _context.UpdatePositionPtzAndEoLens(_context._currentPan, _context._currentTilt, _context._currentEoZoom, _context._currentEoFocus);

            _context.MarkLensEvidence(false);

            _context.MarkPresetLensEvidence(false, zoomRaw >= 0 && zoomRaw <= 1000, focusRaw >= 0 && focusRaw <= 1000);

            Interlocked.Increment(ref _context._eoLensStatusVersion);

            _context._currentPowerStatus = powerStatus;

            /*
             * Focus 상태값 변화 상세 로그
             *
             * 기존 전체 상태 로그는 1초마다 제한되지만,
             * Focus 변화 로그는 값이 실제로 바뀔 때마다 출력한다.
             */
            if (focusRaw != previousFocus)
            {
                int receiveSequence = Interlocked.Increment(ref _context._eoFocusReceiveSequence);

                long receiveElapsedMs = _context._focusLogStopwatch.ElapsedMilliseconds;

                long afterCommandMs = _context._lastEoFocusCommandElapsedMs > 0 ? receiveElapsedMs - _context._lastEoFocusCommandElapsedMs : -1;

                int difference = focusRaw - previousFocus;

                Console.WriteLine();

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + $"[FOCUS RECEIVE #{receiveSequence}] " + $"RAW={packet[8]:X2} {packet[9]:X2} / " + $"PREV={previousFocus} / " + $"CURRENT={focusRaw} / " + $"DELTA={difference:+#;-#;0} / " + $"LAST_CMD={_context._lastEoFocusCommandName} / " + $"AFTER_CMD=" + $"{(afterCommandMs >= 0 ? afterCommandMs + "ms" : "N/A")}");

                ConsoleLogHelper.PrintLine();
            }

            /*
             * UI Binding 갱신
             * 반드시 printLog 검사 전에 호출
             */
            NotifyEoCurrentStatusChanged(Math.Abs(previousPan - _context._currentPan) > 0.0001, Math.Abs(previousTilt - _context._currentTilt) > 0.0001, previousZoom != _context._currentEoZoom, previousFocus != _context._currentEoFocus, previousPower != _context._currentPowerStatus);

            /*
             * 아래부터 Console 로그만 1초 간격으로 제한
             */
            if (!printLog)
            {
                return;
            }

            ConsoleLogHelper.State("WEB AGENT PAN/TILT POSITION", $"PAN_RAW={panUnsignedRaw} / PAN_SIGNED_RAW={panSignedRaw} / " + $"PAN_WEB_DEG={NormalizeUnsignedWebAgentAngle(panUnsignedRaw / 100.0):F2} / " + $"PAN_DEG={panDegree:+0.00;-0.00;0.00} / " + $"TILT_RAW={tiltUnsignedRaw} / TILT_SIGNED_RAW={tiltSignedRaw} / " + $"TILT_DEG={tiltDegree:+0.00;-0.00;0.00} / " + $"MODE={(_context._controlCommandService.UseUnsignedWebAgentPanCoordinates ? "WEB_AGENT_UNSIGNED" : "LA_SIGNED")}");
        }

        internal static double NormalizeUnsignedWebAgentAngle(double angle)
        {
            double normalizedAngle = angle % 360.0;

            if (normalizedAngle < 0.0)
            {
                normalizedAngle += 360.0;
            }

            return normalizedAngle;
        }

        internal static double ConvertWebAgentPanToGuiAngle(double webAgentAngle)
        {
            double guiAngle = NormalizeUnsignedWebAgentAngle(360.0 - webAgentAngle);

            // 2026-09-21: Web Agent 수신값은 0~360이지만 모든 GUI/프리셋/판정은
            // 공통 signed 좌표(-180~180)만 사용한다. 330도는 -30도로 표시한다.
            if (guiAngle > 180.0)
            {
                return guiAngle - 360.0;
            }
            else
            {
                return guiAngle;
            }
        }

        internal static double NormalizeUnsignedWebAgentTilt(double angle, double fallbackTilt)
        {
            double normalizedAngle = NormalizeUnsignedWebAgentAngle(angle);

            double signedTilt = normalizedAngle > 180.0 ? normalizedAngle - 360.0 : normalizedAngle;

            if (signedTilt < -90.0 || signedTilt > 90.0)
            {
                ConsoleLogHelper.Warning("WEB AGENT TILT", $"Out-of-range status ignored / RAW={normalizedAngle:F2} / " + $"SIGNED={signedTilt:F2} / KEEP={fallbackTilt:F2}");

                return fallbackTilt;
            }

            return signedTilt;
        }

        internal void NotifyEoCurrentStatusChanged(bool panChanged = true, bool tiltChanged = true, bool zoomChanged = true, bool focusChanged = true, bool powerChanged = true)
        {
            // 예상 목표값이 아니라 실제 수신한 값을 근거로 사용한다.
            if (panChanged)
            {
                Interlocked.Exchange(ref _eoPanNotificationDirty, 1);
            }

            if (tiltChanged)
            {
                Interlocked.Exchange(ref _eoTiltNotificationDirty, 1);
            }

            if (zoomChanged)
            {
                Interlocked.Exchange(ref _eoZoomNotificationDirty, 1);
            }

            if (focusChanged)
            {
                Interlocked.Exchange(ref _eoFocusNotificationDirty, 1);
            }

            if (powerChanged)
            {
                Interlocked.Exchange(ref _eoPowerNotificationDirty, 1);
            }

            if (!panChanged && !tiltChanged && !zoomChanged && !focusChanged && !powerChanged)
            {
                return;
            }

            Dispatcher dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null)
            {
                return;
            }

            void Notify()
            {
                bool notifyPan = Interlocked.Exchange(ref _eoPanNotificationDirty, 0) != 0;

                bool notifyTilt = Interlocked.Exchange(ref _eoTiltNotificationDirty, 0) != 0;

                bool notifyZoom = Interlocked.Exchange(ref _eoZoomNotificationDirty, 0) != 0;

                bool notifyFocus = Interlocked.Exchange(ref _eoFocusNotificationDirty, 0) != 0;

                bool notifyPower = Interlocked.Exchange(ref _eoPowerNotificationDirty, 0) != 0;

                if (notifyPan)
                {
                    _context.OnPropertyChanged(nameof(_context.CurrentPanText));
                }

                if (notifyTilt)
                {
                    _context.OnPropertyChanged(nameof(_context.CurrentTiltText));
                }

                if (notifyZoom)
                {
                    _context.OnPropertyChanged(nameof(_context.CurrentEoZoomText));

                    _context.OnPropertyChanged(nameof(_context.RooftopEoZoomStatusText));

                    _context.OnPropertyChanged(nameof(_context.EnvironmentEoZoomStatusText));

                    _context.OnPropertyChanged(nameof(_context.EffectivePanTiltSpeedLevel));

                    _context.OnPropertyChanged(nameof(_context.PanTiltSpeedDisplayText));
                }

                if (notifyFocus)
                {
                    _context.OnPropertyChanged(nameof(_context.CurrentEoFocusText));

                    _context.OnPropertyChanged(nameof(_context.RooftopEoFocusStatusText));

                    _context.OnPropertyChanged(nameof(_context.EnvironmentEoFocusStatusText));
                }

                if (notifyPan || notifyTilt || notifyZoom || notifyFocus)
                {
                    _context.OnPropertyChanged(nameof(_context.CurrentPresetSnapshotText));

                    _context.OnPropertyChanged(nameof(_context.CurrentLaPresetSnapshotText));
                }

                if (notifyPower)
                {
                    _context.OnPropertyChanged(nameof(_context.CurrentPowerText));
                }

                /*
                * XAML에서 개별 Run으로 바인딩 중이므로
                * CONTROL 상태 프로퍼티도 별도로 갱신해야 한다.
                */
                if (notifyPower)
                {
                    _context.OnPropertyChanged(nameof(_context.CurrentControlPowerText));
                }
            }

            // 2026-09-23 TEST A: 장비 상태 수신값은 즉시 내부 필드에 반영하되,
            // Text Binding 알림만 100ms 단위 최신값으로 합쳐 영상 Render와 분리한다.
            if (Interlocked.CompareExchange(ref _eoStatusNotificationPending, 1, 0) != 0)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                bool queued = false;

                try
                {
                    await Task.Delay(StatusUiCoalesceMilliseconds).ConfigureAwait(false);

                    if (dispatcher.HasShutdownStarted)
                    {
                        return;
                    }

                    _ = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        Interlocked.Exchange(ref _eoStatusNotificationPending, 0);

                        Notify();
                    }));

                    queued = true;
                }
                finally
                {
                    if (!queued)
                    {
                        Interlocked.Exchange(ref _eoStatusNotificationPending, 0);
                    }
                }
            });
        }

        internal void NotifyIrCurrentStatusChanged()
        {
            Dispatcher dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null)
            {
                return;
            }

            void Notify()
            {
                // 동일한 주기 상태는 IR·상태 바인딩 전체를 다시 그리지 않는다.
                string signature = _context.CurrentIrZoomText + "|" + _context.CurrentIrFocusText + "|" + _context.RooftopIrZoomStatusText + "|" + _context.EnvironmentIrZoomStatusText + "|" + _context.RooftopIrFocusStatusText + "|" + _context.EnvironmentIrFocusStatusText + "|" + _context.PanTiltSpeedDisplayText;

                if (_lastIrUiStatusSignature == signature)
                {
                    return;
                }

                _lastIrUiStatusSignature = signature;

                _context.OnPropertyChanged(nameof(_context.CurrentIrZoomText));

                _context.OnPropertyChanged(nameof(_context.RooftopIrZoomStatusText));

                _context.OnPropertyChanged(nameof(_context.EnvironmentIrZoomStatusText));

                _context.OnPropertyChanged(nameof(_context.EffectivePanTiltSpeedLevel));

                _context.OnPropertyChanged(nameof(_context.PanTiltSpeedDisplayText));

                _context.OnPropertyChanged(nameof(_context.CurrentIrFocusText));

                _context.OnPropertyChanged(nameof(_context.RooftopIrFocusStatusText));

                _context.OnPropertyChanged(nameof(_context.EnvironmentIrFocusStatusText));

                _context.OnPropertyChanged(nameof(_context.CurrentPresetSnapshotText));

                _context.OnPropertyChanged(nameof(_context.CurrentLaPresetSnapshotText));
            }

            if (Interlocked.CompareExchange(ref _irStatusNotificationPending, 1, 0) != 0)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                bool queued = false;

                try
                {
                    await Task.Delay(StatusUiCoalesceMilliseconds).ConfigureAwait(false);

                    if (dispatcher.HasShutdownStarted)
                    {
                        return;
                    }

                    _ = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        Interlocked.Exchange(ref _irStatusNotificationPending, 0);

                        Notify();
                    }));

                    queued = true;
                }
                finally
                {
                    if (!queued)
                    {
                        Interlocked.Exchange(ref _irStatusNotificationPending, 0);
                    }
                }
            });
        }

        internal void ApplyEnvironmentIrCommandedPosition(int? zoomPosition, int? focusPosition, string reason)
        {
            // 2026-11-03 정비: 실제 상태가 이미 수신되는 장비의 현재값을 명령값으로 덮어쓰지 않는다.
            if (Interlocked.Read(ref _context._irLensStatusVersion) > 0)
            {
                return;
            }

            if (zoomPosition.HasValue)
            {
                _context._currentIrZoom = (ushort)Math.Max(0, Math.Min(1000, zoomPosition.Value));
            }

            if (focusPosition.HasValue)
            {
                _context._currentIrFocus = (ushort)Math.Max(0, Math.Min(1000, focusPosition.Value));
            }

            NotifyIrCurrentStatusChanged();

            ConsoleLogHelper.State("IR LENS STATUS", "Commanded-position fallback applied" + $" / ZOOM={_context._currentIrZoom}" + $" / FOCUS={_context._currentIrFocus}" + $" / REASON={reason}");
        }

        internal void ParseLaIrCameraStatusPacket(LaResponsePacket packet, bool printLog)
        {
            if (packet == null || packet.RawData == null || packet.RawData.Length < 12)
            {
                if (printLog)
                {
                    Console.WriteLine("[LA IR STATUS] Invalid Packet Length : " + (packet?.RawData?.Length ?? 0));
                }

                return;
            }

            ushort irZoomPosition = packet.IrZoomPosition;

            ushort irFocusPosition = packet.IrFocusPosition;

            bool isZoomInRange = irZoomPosition <= 1000;

            bool isFocusInRange = irFocusPosition <= 1000;

            if (!isZoomInRange || !isFocusInRange)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine("[LA IR STATUS] Invalid Position Range");

                Console.WriteLine($"[LA IR STATUS] Zoom  : {irZoomPosition} / 1000");

                Console.WriteLine($"[LA IR STATUS] Focus : {irFocusPosition} / 1000");

                Console.WriteLine("[LA IR STATUS RAW] " + BitConverter.ToString(packet.RawData).Replace("-", " "));

                ConsoleLogHelper.PrintLine();

                return;
            }

            ushort previousIrZoom = _context._currentIrZoom;

            ushort previousIrFocus = _context._currentIrFocus;

            _context._currentIrZoom = irZoomPosition;
            _context._currentIrFocus = irFocusPosition;

            _context.UpdatePositionIrLens(_context._currentIrZoom, _context._currentIrFocus);

            _context.MarkLensEvidence(true);

            _context.MarkPresetLensEvidence(true, true, true);

            Interlocked.Increment(ref _context._irLensStatusVersion);

            // IR 상태값은 CONTROL AGENT TCP 수신 Thread에서 갱신된다.
            //
            // WPF UI Binding은 Dispatcher를 통해 갱신한다.
            //
            NotifyIrCurrentStatusChanged();

            // 주기 상태 Packet 전체 로그는 제한하지만,
            // 실제 Zoom / Focus 값이 바뀐 경우에는 변화 로그를 출력한다.
            //
            // 장비 시험 시 어떤 값이 Zoom이고 Focus인지
            // 조작 방향과 함께 바로 확인할 수 있다.
            //
            if (previousIrZoom != _context._currentIrZoom || previousIrFocus != _context._currentIrFocus)
            {
                Console.WriteLine();

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + "[IR STATUS CHANGED] " + $"ZOOM_RAW={previousIrZoom}→{_context._currentIrZoom} / " + $"ZOOM_STD={_context.GetCurrentIrZoomStandardPosition()} / " + $"FOCUS_RAW={previousIrFocus}→{_context._currentIrFocus} / " + $"FOCUS_STD={_context.GetCurrentIrFocusStandardPosition()}");

                ConsoleLogHelper.PrintLine();
            }

            if (!printLog)
            {
                return;
            }

            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[LA PACKET] [IR] Zoom / Focus Status");

            Console.WriteLine();

            Console.WriteLine($"[LA IR STATUS] [Zoom]  : {_context._currentIrZoom} / 1000");

            Console.WriteLine($"[LA IR STATUS] [Focus] : {_context._currentIrFocus} / 1000");

            Console.WriteLine();

            Console.WriteLine("[LA IR STATUS RAW] " + BitConverter.ToString(packet.RawData).Replace("-", " "));

            ConsoleLogHelper.PrintLine();
        }

        internal void ParseLaExtendedStatusPacket(byte[] packet, bool printLog)
        {
            if (!printLog)
            {
                return;
            }

            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[LA PACKET] Unconfirmed Extended Status : 0xA1");

            Console.WriteLine();

            Console.WriteLine("[LA EXT STATUS RAW] " + BitConverter.ToString(packet).Replace("-", " "));

            Console.WriteLine();

            Console.WriteLine("[LA EXT STATUS] CURRENT STATUS not updated.");

            ConsoleLogHelper.PrintLine();
        }

        internal void ParseLrfDistancePacket(byte[] packet)
        {
            if (packet == null || packet.Length < 10)
            {
                Console.WriteLine("[LRF] Invalid Distance Packet");

                return;
            }

            double distance = BitConverter.ToDouble(packet, 2);

            _context.LrfDistanceText = $"DISTANCE : {distance:F1} m";

            Console.WriteLine($"[LRF] Distance : {distance:F1} m");
        }

        #endregion

    }
}
