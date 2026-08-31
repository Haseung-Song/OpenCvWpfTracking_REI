using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// Control Agent TCP 연결, LA Packet 수신/파싱과 장비 상태 반영을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [LA Communication]

        #region [LA Connect]

        /// <summary>
        /// Control Agent TCP 연결 상태 UI 갱신
        /// </summary>
        private void SetControlAgentConnectionStatus(
            string statusText,
            string statusColor)
        {
            /*
             * 자동 재연결 Loop는 백그라운드 Task에서 실행되므로
             * UI Dispatcher를 통해 바인딩 값을 변경한다.
             */
            if (App.Current?.Dispatcher ==
                null)
            {
                ControlAgentConnectionStatusText =
                    statusText;

                ControlAgentConnectionStatusColor =
                    statusColor;

                return;
            }

            if (App.Current.Dispatcher
                .CheckAccess())
            {
                ControlAgentConnectionStatusText =
                    statusText;

                ControlAgentConnectionStatusColor =
                    statusColor;

                return;
            }

            App.Current.Dispatcher.Invoke(
                () =>
                {
                    ControlAgentConnectionStatusText =
                        statusText;

                    ControlAgentConnectionStatusColor =
                        statusColor;
                });
        }

        /// <summary>
        /// [Control Agent] 제어 TCP 연결
        ///
        /// 기존 고흥 제어 구조는 유지하며,
        /// 운용 환경에 따라 연결 대상 IP / Port만 변경하여 사용한다.
        /// </summary>
        private async Task<bool> ConnectLaAsync()
        {
            ConsoleLogHelper.InfoSection(
                "CONTROL AGENT",
                "TCP connect workflow started",
                string.Empty,
                $"TARGET : {ControlAgentIp}:{ControlAgentPortText}");

            Console.WriteLine();

            /*
            * Connecting 상태가 시작된 시각을 기록한다.
            *
            * 실제 TCP 연결이 너무 빨리 완료되더라도
            * 최소 표시시간을 계산하기 위해 사용한다.
            */
            Stopwatch connectingStopwatch =
                Stopwatch.StartNew();

            /*
             * 연결 버튼 클릭 즉시
             * UI 상태를 Connecting으로 변경한다.
             */
            SetControlAgentConnectionStatus(
                "Connecting",
                "#FFD166");

            ConsoleLogHelper.PrintSection(
                "[CONTROL AGENT]",
                "Connect Start");

            /*
             * UI 입력값 검증
             *
             * IP 빈값, Port 문자 입력,
             * Port 범위 오류 등을 검사한다.
             */
            if (!TryGetControlAgentEndpoint(
                    out string targetIp,
                    out int targetPort))
            {
                SetControlAgentConnectionStatus(
                    "Disconnected",
                    "#FF6B6B");

                return false;
            }

            /*
             * 이전 입력값으로 실행 중인 자동 재연결 Loop가 있다면
             * 새 연결 시도 전에 정리한다.
             */
            _controlAgentReconnectCts?.Cancel();
            _controlAgentReconnectCts?.Dispose();

            _controlAgentReconnectCts =
                null;

            try
            {
                bool result =
                    await _laTcpService.ConnectAsync(
                        targetIp,
                        targetPort);

                /*
                * 실제 TCP 연결에 걸린 시간을 제외하고
                * Connecting 상태 최소 표시시간이 남아 있으면 기다린다.
                *
                * Task.Delay를 await하므로 UI Thread를 막지 않는다.
                */
                int remainingDisplayMs =
                    ControlAgentConnectingMinimumDisplayMs -
                    (int)connectingStopwatch.ElapsedMilliseconds;

                if (remainingDisplayMs > 0)
                {
                    await Task.Delay(
                        remainingDisplayMs);
                }

                /*
                 * 연결 결과에 따라 UI 상태 갱신
                 */
                if (result)
                {
                    SetControlAgentConnectionStatus(
                        "Connected",
                        "#55D187");
                }
                else
                {
                    if (_isDeviceConnectionRequested)
                    {
                        SetControlAgentConnectionStatus(
                            "Reconnecting",
                            "#FFD166");
                    }
                    else
                    {
                        SetControlAgentConnectionStatus(
                            "Disconnected",
                            "#FF6B6B");
                    }

                }

                ConsoleLogHelper.StateSection(
                    "CONTROL AGENT",
                    "Connect completed",
                    string.Empty,
                    $"RESULT : {result}",
                    $"TARGET : {targetIp}:{targetPort}");

                /*
                 * 연결 실패 상태이지만
                 * 사용자가 장비 연결 유지를 요청한 경우
                 * 자동 재연결을 시작한다.
                 */
                if (!result &&
                    _isDeviceConnectionRequested)
                {
                    StartControlAgentReconnect(
                        targetIp,
                        targetPort);
                }
                return result;
            }
            catch (Exception ex)
            {
                /*
                 * 연결 예외 발생 시에도
                 * 앱이 종료되지 않도록 상태만 갱신한다.
                 */
                if (_isDeviceConnectionRequested)
                {
                    SetControlAgentConnectionStatus(
                        "Reconnecting",
                        "#FFD166");
                }
                else
                {
                    SetControlAgentConnectionStatus(
                        "Disconnected",
                        "#FF6B6B");
                }

                Console.WriteLine();
                Console.WriteLine(
                    "[CONTROL AGENT] Connect Exception");

                Console.WriteLine(
                    $"[CONTROL AGENT] {ex.Message}");

                ConsoleLogHelper.PrintLine();

                if (_isDeviceConnectionRequested)
                {
                    StartControlAgentReconnect(
                        targetIp,
                        targetPort);
                }
                return false;
            }

        }

        /// <summary>
        /// 통신 설정 탭에서 선택된 EO / IR RTSP 주소 검증
        ///
        /// 선택값이 없거나 rtsp / rtsps 형식이 아닌 주소는
        /// 실제 FFmpeg 연결 전에 차단한다.
        /// </summary>
        private bool TryGetRtspEndpoints(
            out string eoRtspAddress,
            out string irRtspAddress)
        {
            eoRtspAddress =
                EoSourceAddress?.Trim();

            irRtspAddress =
                IrSourceAddress?.Trim();

            if (!IsValidRtspAddress(
                    eoRtspAddress))
            {
                EoStatusText =
                    "[EO] Invalid RTSP Address";

                Console.WriteLine();
                Console.WriteLine(
                    "[EO RTSP] Connect Failed : " +
                    "Invalid RTSP address.");

                Console.WriteLine(
                    $"[EO RTSP] INPUT : " +
                    $"{ConsoleLogHelper.MaskRtspPassword(EoSourceAddress)}");

                ConsoleLogHelper.PrintLine();

                return false;
            }

            if (!IsValidRtspAddress(
                    irRtspAddress))
            {
                IrStatusText =
                    "[IR] Invalid RTSP Address";

                Console.WriteLine();
                Console.WriteLine(
                    "[IR RTSP] Connect Failed : " +
                    "Invalid RTSP address.");

                Console.WriteLine(
                    $"[IR RTSP] INPUT : " +
                    $"{ConsoleLogHelper.MaskRtspPassword(IrSourceAddress)}");

                ConsoleLogHelper.PrintLine();

                return false;
            }

            return true;
        }

        /// <summary>
        /// RTSP 주소 형식 확인
        ///
        /// 절대 URI이며 Scheme이 rtsp 또는 rtsps인 경우만
        /// 유효한 영상 주소로 처리한다.
        /// </summary>
        private static bool IsValidRtspAddress(
            string address)
        {
            if (string.IsNullOrWhiteSpace(
                    address))
            {
                return false;
            }

            if (!Uri.TryCreate(
                    address,
                    UriKind.Absolute,
                    out Uri uri))
            {
                return false;
            }

            return string.Equals(
                       uri.Scheme,
                       "rtsp",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       uri.Scheme,
                       "rtsps",
                       StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 통신 설정 탭의 Control Agent IP / Port 입력값 검증
        ///
        /// Port는 문자열로 관리한 뒤 연결 시점에 TryParse하여
        /// 빈값이나 문자 입력으로 인한 바인딩 예외를 방지한다.
        /// </summary>
        private bool TryGetControlAgentEndpoint(
            out string ipAddress,
            out int port)
        {
            ipAddress =
                ControlAgentIp?.Trim();

            port =
                0;

            if (string.IsNullOrWhiteSpace(
                    ipAddress))
            {
                SetControlAgentConnectionStatus(
                    "Disconnected",
                    "#FF6B6B");

                Console.WriteLine(
                    "[CONTROL AGENT] Connect Failed : IP is empty.");

                return false;
            }

            if (!int.TryParse(
                    ControlAgentPortText?.Trim(),
                    out port))
            {
                SetControlAgentConnectionStatus(
                    "Disconnected",
                    "#FF6B6B");

                Console.WriteLine(
                    "[CONTROL AGENT] Connect Failed : " +
                    "Port must be a number.");

                return false;
            }

            if (port < 1 ||
                port > 65535)
            {
                SetControlAgentConnectionStatus(
                    "Disconnected",
                    "#FF6B6B");

                Console.WriteLine(
                    "[CONTROL AGENT] Connect Failed : " +
                    "Port range must be 1 ~ 65535.");

                return false;
            }
            return true;
        }

        /// <summary>
        /// [Control Agent] 비정상 연결 종료 처리
        ///
        /// 현재 통신 설정 탭에 입력된 IP / Port를 사용하여
        /// 자동 재연결을 시작한다.
        /// </summary>
        private void OnControlAgentConnectionClosed()
        {
            if (!_isDeviceConnectionRequested)
            {
                SetControlAgentConnectionStatus(
                    "Disconnected",
                    "#FF6B6B");

                return;
            }

            SetControlAgentConnectionStatus(
                "Reconnecting",
                "#FFD166");

            string targetIp =
                ControlAgentIp?.Trim();

            if (string.IsNullOrWhiteSpace(
                    targetIp))
            {
                SetControlAgentConnectionStatus(
                    "Disconnected",
                    "#FF6B6B");

                return;
            }

            if (!int.TryParse(
                    ControlAgentPortText?.Trim(),
                    out int targetPort) ||
                targetPort < 1 ||
                targetPort > 65535)
            {
                SetControlAgentConnectionStatus(
                    "Disconnected",
                    "#FF6B6B");

                return;
            }

            StartControlAgentReconnect(
                targetIp,
                targetPort);
        }

        /// <summary>
        /// [Control Agent] 자동 재연결 Loop 시작
        ///
        /// 최초 연결 실패 또는 운용 중 연결 종료 시
        /// 연결 해제 요청 전까지 일정 간격으로 재연결한다.
        /// </summary>
        private void StartControlAgentReconnect(
            string ipAddress,
            int port)
        {
            if (_controlAgentReconnectCts != null &&
                !_controlAgentReconnectCts.IsCancellationRequested)
            {
                return;
            }

            _controlAgentReconnectCts?.Dispose();

            _controlAgentReconnectCts =
                new CancellationTokenSource();

            CancellationToken token =
                _controlAgentReconnectCts.Token;

            _ = Task.Run(async () =>
            {
                const int reconnectDelayMs =
                    1500;

                int retryCount =
                    0;

                try
                {
                    while (_isDeviceConnectionRequested &&
                           !token.IsCancellationRequested &&
                           !_laTcpService.IsConnected)
                    {
                        retryCount++;

                        SetControlAgentConnectionStatus(
                            "Reconnecting",
                            "#FFD166");

                        Console.WriteLine(
                            $"[CONTROL AGENT] Reconnect Try " +
                            $"({retryCount}) : " +
                            $"{ipAddress}:{port}");

                        bool connected =
                            await _laTcpService.ConnectAsync(
                                ipAddress,
                                port);

                        if (connected)
                        {
                            SetControlAgentConnectionStatus(
                                "Connected",
                                "#55D187");

                            Console.WriteLine(
                                "[CONTROL AGENT] Reconnect Success");

                            return;
                        }

                        await Task.Delay(
                            reconnectDelayMs,
                            token);
                    }

                }
                catch (OperationCanceledException)
                {
                    SetControlAgentConnectionStatus(
                        "Disconnected",
                        "#FF6B6B");
                }
                catch (Exception ex)
                {
                    SetControlAgentConnectionStatus(
                        "Disconnected",
                        "#FF6B6B");

                    Console.WriteLine(
                        "[CONTROL AGENT] Reconnect Exception : " +
                        ex.Message);
                }
                finally
                {
                    if (_controlAgentReconnectCts != null &&
                        _controlAgentReconnectCts.Token == token)
                    {
                        _controlAgentReconnectCts.Dispose();
                        _controlAgentReconnectCts = null;
                    }

                }

            });

        }

        #endregion

        #region [LA Receive]

        /// <summary>
        /// [CONTROL AGENT] [TCP] 수신 데이터 처리 함수
        ///
        /// [TcpClientService]에서 byte[] 원본 데이터를 받으면,
        /// [LaPacketParser]를 통해 12byte [Packet] 단위로 분리한다.
        /// </summary>
        private void OnLaMessageReceived(
            byte[] data,
            DateTime receiveTime)
        {
            /// <summary>
            /// 수신된 [byte[] 데이터]를 [CONTROL AGENT] 응답 [Packet] 목록으로 변환.
            /// </summary>
            List<LaResponsePacket> packets = _laPacketParser.Parse(data);

            /// <summary>
            /// 분리된 [Packet]을 하나씩 처리
            /// <summary></summary>
            foreach (LaResponsePacket packet in packets)
            {
                HandleLaPacket(packet);
            }

        }

        #endregion

        #region [LA Packet Handling]

        /// <summary>
        /// [CONTROL AGENT] 응답 [Packet] 처리 함수
        ///
        /// [Function] 번호를 기준으로
        /// [Status] / [Alive] / [Extended Status Packet]을 구분한다.
        /// </summary>
        private void HandleLaPacket(LaResponsePacket packet)
        {
            /// <summary>
            /// [Header] / [Checksum] 검증 실패 시 처리하지 않음
            /// </summary>
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
                case 0x01:
                    /// <summary>
                    /// [Pan] / [Tilt] / [Zoom] / [Focus] 상태 정보
                    /// </summary>
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

                case 0x07:
                    /// <summary>
                    /// [Function] [0x07]
                    ///
                    /// 열영상 카메라 [Zoom] / [Focus] 위치 상태 Packet
                    ///
                    /// Packet 구조:
                    ///
                    /// [0]  Header
                    /// [1]  Function = 0x07
                    /// [2]  IR Zoom Low Byte
                    /// [3]  IR Zoom High Byte
                    /// [4]  IR Focus Low Byte
                    /// [5]  IR Focus High Byte
                    /// [6] ~ [10] 상태 / 예약 영역
                    /// [11] Checksum
                    ///
                    /// 장비에서 실제 수신되는 위치값은
                    /// Little Endian 방식으로 확인된다.
                    ///
                    /// 예:
                    /// D6 03 → 982
                    /// E8 03 → 1000
                    /// </summary>
                    ParseLaIrCameraStatusPacket(
                        packet,
                        canPrintExtendedStatusLog);

                    break;

                case 0xA1:

                    /// <summary>
                    /// 상태값은 모든 Packet마다 파싱하고,
                    /// Console 로그만 설정된 주기로 제한한다.
                    /// </summary>
                    ParseLaExtendedStatusPacket(
                        packet.RawData,
                        canPrintExtendedStatusLog);

                    break;

                case 0xA3:
                    /// <summary>
                    /// [Function] [0xA3]
                    ///
                    /// 현재 장비에서 주기적으로 수신되는
                    /// 확장 상태 Packet
                    ///
                    /// 세부 의미 미확인
                    /// Console 출력 생략
                    /// </summary>
                    break;

                case 0x04:
                    /// <summary>
                    /// [LRF] 거리측정 응답 Packet
                    /// </summary>

                    ConsoleLogHelper.PrintLine();
                    Console.WriteLine("[LA PACKET] [LRF] Distance Packet");
                    Console.WriteLine();
                    ParseLrfDistancePacket(packet.RawData);

                    ConsoleLogHelper.PrintLine();
                    break;

                default:
                    /// <summary>
                    /// 정의되지 않은 [Function] 번호
                    ///
                    /// [LRF] / [GPS] / 기타 확장 [Packet] 확인용으로
                    /// 로그 제한 없이 출력한다.
                    /// </summary>

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

        #endregion

        #region [LA Log Helpers]

        /// <summary>
        /// [CONTROL AGENT] 상태 로그 출력 여부 확인
        ///
        /// 현재 시간과 마지막 출력 시간을 비교하여
        /// 설정된 출력 간격 이내인 경우
        /// [Console] 출력을 생략한다.
        ///
        /// [0x01] 상태 [Packet] 로그 출력 제어용
        /// </summary>
        private bool CanPrintLaLog()
        {
            if ((DateTime.Now -
                 _lastLaStatusLogTime)
                .TotalSeconds
                < LaLogIntervalSeconds)
            {
                return false;
            }
            _lastLaStatusLogTime = DateTime.Now;

            return true;
        }

        /// <summary>
        /// [CONTROL AGENT] [Extended Status] 로그 출력 여부 확인
        ///
        /// 현재 시간과 마지막 출력 시간을 비교하여
        /// 설정된 출력 간격 이내인 경우
        /// [Console] 출력을 생략한다.
        ///
        /// [0xA1] 확장 상태 Packet 로그 출력 제어용.
        /// </summary>
        private bool CanPrintLaExtendedStatusLog()
        {
            if ((DateTime.Now -
                 _lastLaExtendedStatusLogTime)
                .TotalSeconds
                < LaLogIntervalSeconds)
            {
                return false;
            }
            _lastLaExtendedStatusLogTime = DateTime.Now;

            return true;
        }

        #endregion

        #region [LA Packet Parsing]

        /// <summary>
        /// [CONTROL AGENT] [Status Packet] 파싱
        ///
        /// [Function] [0x01]:
        /// [Pan] / [Tilt] / [EO Zoom] / [EO Focus] / [Power] 상태 정보
        ///
        /// 응답 Packet의 2Byte 이상 값은
        /// Little Endian 방식으로 처리한다.
        /// </summary>
        private void ParseLaStatusPacket(
            byte[] packet,
            bool printLog)
        {
            const int requiredLength =
                12;

            if (packet == null ||
                packet.Length < requiredLength)
            {
                if (printLog)
                {
                    //Console.WriteLine(
                    //    "[LA STATUS] Invalid Packet Length : " +
                    //    (packet?.Length ?? 0));
                }

                return;
            }

            short panRaw =
                BitConverter.ToInt16(
                    packet,
                    2);

            short tiltRaw =
                BitConverter.ToInt16(
                    packet,
                    4);

            short zoomRaw =
                BitConverter.ToInt16(
                    packet,
                    6);

            short focusRaw =
                BitConverter.ToInt16(
                    packet,
                    8);

            byte powerStatus =
                packet[10];

            /*
            * Focus 변화 비교용 이전값 저장
            *
            * _currentEoFocus를 갱신하기 전에
            * 반드시 기존 값을 먼저 보관한다.
            */
            short previousFocus =
                _currentEoFocus;

            double panDegree =
                panRaw / 100.0;

            double tiltDegree =
                tiltRaw / 100.0;

            /// <summary>
            /// -180도와 +180도는 동일한 물리 위치다.
            ///
            /// LA가 -180 명령 후에도 상태값을 +180으로 반환할 수 있으므로,
            /// 상태값이 경계 ±180도이고 마지막 목표도 경계값인 경우에는
            /// 사용자가 마지막으로 입력한 목표 부호를 표시값에 유지한다.
            ///
            /// 일반 위치에서는 수신 상태값을 그대로 사용한다.
            /// </summary>
            if (Math.Abs(
                    Math.Abs(
                        panDegree) -
                    180.0) <= 0.05 &&
                _lastPanAbsoluteTarget.HasValue &&
                Math.Abs(
                    Math.Abs(
                        _lastPanAbsoluteTarget.Value) -
                    180.0) <= 0.05)
            {
                panDegree =
                    _lastPanAbsoluteTarget.Value < 0.0
                        ? -180.0
                        : 180.0;
            }

            /*
             * 모든 0x01 패킷에서 상태값 갱신
             */
            _currentPan =
                panDegree;

            _currentTilt =
                tiltDegree;

            // 이동이 완료된 축은 더 이상 Slider 변경 시 목표를 재송신하지 않는다.
            double? activePanTarget =
                _activePanAbsoluteTarget;

            double? activeTiltTarget =
                _activeTiltAbsoluteTarget;

            if (activePanTarget.HasValue &&
                Math.Abs(
                    _currentPan -
                    activePanTarget.Value) <= 0.03)
            {
                _activePanAbsoluteTarget =
                    null;
            }

            if (activeTiltTarget.HasValue &&
                Math.Abs(
                    _currentTilt -
                    activeTiltTarget.Value) <= 0.03)
            {
                _activeTiltAbsoluteTarget =
                    null;
            }

            Interlocked.Increment(
                ref _panTiltStatusVersion);

            _currentEoZoom =
                zoomRaw;

            _currentEoFocus =
                focusRaw;

            _currentPowerStatus =
                powerStatus;

            /*
             * Focus 상태값 변화 상세 로그
             *
             * 기존 전체 상태 로그는 1초마다 제한되지만,
             * Focus 변화 로그는 값이 실제로 바뀔 때마다 출력한다.
             */
            if (focusRaw !=
                previousFocus)
            {
                int receiveSequence =
                    Interlocked.Increment(
                        ref _eoFocusReceiveSequence);

                long receiveElapsedMs =
                    _focusLogStopwatch
                        .ElapsedMilliseconds;

                long afterCommandMs =
                    _lastEoFocusCommandElapsedMs > 0
                        ? receiveElapsedMs -
                          _lastEoFocusCommandElapsedMs
                        : -1;

                int difference =
                    focusRaw -
                    previousFocus;

                Console.WriteLine();
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.fff}] " +
                    $"[FOCUS RECEIVE #{receiveSequence}] " +
                    $"RAW={packet[8]:X2} {packet[9]:X2} / " +
                    $"PREV={previousFocus} / " +
                    $"CURRENT={focusRaw} / " +
                    $"DELTA={difference:+#;-#;0} / " +
                    $"LAST_CMD={_lastEoFocusCommandName} / " +
                    $"AFTER_CMD=" +
                    $"{(afterCommandMs >= 0 ? afterCommandMs + "ms" : "N/A")}");

                ConsoleLogHelper.PrintLine();
            }

            /*
             * UI Binding 갱신
             * 반드시 printLog 검사 전에 호출
             */
            NotifyEoCurrentStatusChanged();

            /*
             * 아래부터 Console 로그만 1초 간격으로 제한
             */
            if (!printLog)
            {
                return;
            }

            //Console.WriteLine(
            //    $"[LA PT RAW] " +
            //    $"PAN BYTE={packet[2]:X2} {packet[3]:X2}, " +
            //    $"TILT BYTE={packet[4]:X2} {packet[5]:X2}");

            //Console.WriteLine(
            //    $"[LA PT PARSED] " +
            //    $"PAN RAW={panRaw}, PAN={panDegree:F2}°, " +
            //    $"TILT RAW={tiltRaw}, TILT={tiltDegree:F2}°");

            //Console.WriteLine(
            //    $"[LA STATUS] [EO Zoom]  : {_currentEoZoom}");

            //Console.WriteLine(
            //    $"[LA STATUS] [EO Focus] : {_currentEoFocus}");

            //Console.WriteLine(
            //    $"[LA STATUS] [Power]    : 0x{_currentPowerStatus:X2}");
        }

        /// <summary>
        /// Pan / Tilt / EO Zoom / EO Focus / Power
        /// CURRENT STATUS UI 갱신
        ///
        /// LA TCP 수신 이벤트는 Receive Thread에서 호출되므로
        /// WPF Dispatcher를 통해 UI Binding 갱신을 수행한다.
        /// </summary>
        private void NotifyEoCurrentStatusChanged()
        {
            Dispatcher dispatcher =
                System.Windows.Application
                    .Current?
                    .Dispatcher;

            if (dispatcher == null)
            {
                return;
            }

            void Notify()
            {
                OnPropertyChanged(
                    nameof(CurrentPanText));

                OnPropertyChanged(
                    nameof(CurrentTiltText));

                OnPropertyChanged(
                    nameof(CurrentEoZoomText));

                OnPropertyChanged(
                    nameof(CurrentEoFocusText));

                OnPropertyChanged(
                    nameof(RooftopEoZoomStatusText));

                OnPropertyChanged(
                    nameof(RooftopEoFocusStatusText));

                OnPropertyChanged(
                    nameof(EnvironmentEoZoomStatusText));

                OnPropertyChanged(
                    nameof(EnvironmentEoFocusStatusText));

                OnPropertyChanged(
                    nameof(CurrentPresetSnapshotText));

                OnPropertyChanged(
                    nameof(CurrentLaPresetSnapshotText));

                OnPropertyChanged(
                    nameof(CurrentPowerText));

                /*
                * XAML에서 개별 Run으로 바인딩 중이므로
                * CONTROL 상태 프로퍼티도 별도로 갱신해야 한다.
                */
                OnPropertyChanged(
                    nameof(CurrentControlPowerText));
            }

            if (dispatcher.CheckAccess())
            {
                Notify();
                return;
            }

            // 2026-08-27: 연속 Zoom / Focus 상태 패킷은 일반 UI 입력보다 먼저
            // Binding 큐에서 처리하여 다음 버튼 입력까지 표시가 지연되지 않게 한다.
            dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.DataBind,
                new Action(Notify));
        }

        /// <summary>
        /// IR Zoom / IR Focus
        /// CURRENT STATUS UI 갱신
        /// </summary>
        private void NotifyIrCurrentStatusChanged()
        {
            Dispatcher dispatcher =
                System.Windows.Application
                    .Current?
                    .Dispatcher;

            if (dispatcher == null)
            {
                return;
            }

            void Notify()
            {
                OnPropertyChanged(
                    nameof(CurrentIrZoomText));

                OnPropertyChanged(
                    nameof(RooftopIrZoomStatusText));

                OnPropertyChanged(
                    nameof(EnvironmentIrZoomStatusText));

                OnPropertyChanged(
                    nameof(CurrentIrFocusText));

                OnPropertyChanged(
                    nameof(RooftopIrFocusStatusText));

                OnPropertyChanged(
                    nameof(EnvironmentIrFocusStatusText));

                OnPropertyChanged(
                    nameof(CurrentPresetSnapshotText));

                OnPropertyChanged(
                    nameof(CurrentLaPresetSnapshotText));
            }

            if (dispatcher.CheckAccess())
            {
                Notify();
                return;
            }

            dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.DataBind,
                new Action(Notify));
        }

        /// <summary>
        /// [CONTROL AGENT] [IR Camera Status Packet] 파싱
        ///
        /// Function 0x07
        ///
        /// 열영상 카메라의 Zoom / Focus 현재 위치값을 파싱한다.
        ///
        /// 수신 Packet 구조:
        ///
        /// [2] [3] : IR Zoom Position
        /// [4] [5] : IR Focus Position
        ///
        /// 장비 제어 명령 문서의 Position 입력은 Big Endian이지만,
        /// 현재 CONTROL AGENT에서 수신되는 0x07 상태 Packet은
        /// 실제 로그 기준 Little Endian으로 확인된다.
        ///
        /// 예:
        /// D6 03 → 982
        /// E8 03 → 1000
        ///
        /// 정상 범위:
        /// 0 ~ 1000
        ///
        /// 범위를 벗어난 값은 상태값에 반영하지 않고
        /// 원본 Packet과 함께 Console에 출력한다.
        /// </summary>
        private void ParseLaIrCameraStatusPacket(
            LaResponsePacket packet,
            bool printLog)
        {
            if (packet == null ||
                packet.RawData == null ||
                packet.RawData.Length < 12)
            {
                if (printLog)
                {
                    Console.WriteLine(
                        "[LA IR STATUS] Invalid Packet Length : " +
                        (packet?.RawData?.Length ?? 0));
                }

                return;
            }

            ushort irZoomPosition =
                packet.IrZoomPosition;

            ushort irFocusPosition =
                packet.IrFocusPosition;

            bool isZoomInRange =
                irZoomPosition <=
                1000;

            bool isFocusInRange =
                irFocusPosition <=
                1000;

            if (!isZoomInRange ||
                !isFocusInRange)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[LA IR STATUS] Invalid Position Range");

                Console.WriteLine(
                    $"[LA IR STATUS] Zoom  : {irZoomPosition} / 1000");

                Console.WriteLine(
                    $"[LA IR STATUS] Focus : {irFocusPosition} / 1000");

                Console.WriteLine(
                    "[LA IR STATUS RAW] " +
                    BitConverter
                        .ToString(
                            packet.RawData)
                        .Replace(
                            "-",
                            " "));

                ConsoleLogHelper.PrintLine();

                return;
            }

            ushort previousIrZoom =
                _currentIrZoom;

            ushort previousIrFocus =
                _currentIrFocus;

            _currentIrZoom =
                irZoomPosition;

            _currentIrFocus =
                irFocusPosition;

            Interlocked.Increment(
                ref _irLensStatusVersion);

            /// <summary>
            /// IR 상태값은 CONTROL AGENT TCP 수신 Thread에서 갱신된다.
            ///
            /// WPF UI Binding은 Dispatcher를 통해 갱신한다.
            /// </summary>
            NotifyIrCurrentStatusChanged();

            /// <summary>
            /// 주기 상태 Packet 전체 로그는 제한하지만,
            /// 실제 Zoom / Focus 값이 바뀐 경우에는 변화 로그를 출력한다.
            ///
            /// 장비 시험 시 어떤 값이 Zoom이고 Focus인지
            /// 조작 방향과 함께 바로 확인할 수 있다.
            /// </summary>
            if (previousIrZoom !=
                    _currentIrZoom ||
                previousIrFocus !=
                    _currentIrFocus)
            {
                Console.WriteLine();

                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.fff}] " +
                    "[IR STATUS CHANGED] " +
                    $"ZOOM_RAW={previousIrZoom}→{_currentIrZoom} / " +
                    $"ZOOM_STD={GetCurrentIrZoomStandardPosition()} / " +
                    $"FOCUS_RAW={previousIrFocus}→{_currentIrFocus} / " +
                    $"FOCUS_STD={GetCurrentIrFocusStandardPosition()}");

                ConsoleLogHelper.PrintLine();
            }

            if (!printLog)
            {
                return;
            }

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[LA PACKET] [IR] Zoom / Focus Status");

            Console.WriteLine();

            Console.WriteLine(
                $"[LA IR STATUS] [Zoom]  : {_currentIrZoom} / 1000");

            Console.WriteLine(
                $"[LA IR STATUS] [Focus] : {_currentIrFocus} / 1000");

            Console.WriteLine();

            Console.WriteLine(
                "[LA IR STATUS RAW] " +
                BitConverter
                    .ToString(
                        packet.RawData)
                    .Replace(
                        "-",
                        " "));

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CONTROL AGENT] [Extended Status] Packet 확인
        ///
        /// Function 0xA1
        ///
        /// 현재 문서에서 IR Zoom / Focus 상태 Packet으로
        /// 확인되지 않았으므로 CURRENT STATUS에는 반영하지 않는다.
        ///
        /// 의미가 확정되지 않은 Packet 값을 IR 상태에 넣으면
        /// 정상적으로 파싱된 Function 0x07 값이 잘못 덮어써질 수 있다.
        /// </summary>
        private void ParseLaExtendedStatusPacket(
            byte[] packet,
            bool printLog)
        {
            if (!printLog)
            {
                return;
            }

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[LA PACKET] Unconfirmed Extended Status : 0xA1");

            Console.WriteLine();

            Console.WriteLine(
                "[LA EXT STATUS RAW] " +
                BitConverter
                    .ToString(
                        packet)
                    .Replace(
                        "-",
                        " "));

            Console.WriteLine();

            Console.WriteLine(
                "[LA EXT STATUS] CURRENT STATUS not updated.");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [LRF] 거리측정 응답 [Packet] 파싱
        ///
        /// 거리값은 [8byte double] 형식이며,
        /// [Little Endian] 방식으로 저장된다.
        ///
        /// 현재는 장비 응답 [Function] 번호 확인 전 단계이며,
        /// 실제 거리 응답 수신 시 [HandleLaPacket]의
        /// [Function] 분기와 함께 최종 검증 예정이다.
        /// </summary>
        private void ParseLrfDistancePacket(byte[] packet)
        {
            if (packet == null ||
                packet.Length < 10)
            {
                Console.WriteLine("[LRF] Invalid Distance Packet");
                return;
            }
            double distance = BitConverter.ToDouble(packet, 2);
            LrfDistanceText = $"DISTANCE : {distance:F1} m";

            Console.WriteLine($"[LRF] Distance : {distance:F1} m");
        }

        #endregion

        #endregion
    }

}
