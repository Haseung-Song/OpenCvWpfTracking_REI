using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Text;
using System.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>수신 패킷 분배에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IWebAgentProtocolFeatureContext
    {
        ref double? _activePanAbsoluteTarget { get; }

        KeyboardPanTiltDirection _activePanTiltMoveDirection { get; set; }

        ref double? _activeTiltAbsoluteTarget { get; }

        string _connectionSessionId { get; set; }

        IControlCommandServicePort _controlCommandService { get; }

        short _currentEoFocus { get; set; }

        short _currentEoZoom { get; set; }

        ushort _currentIrFocus { get; set; }

        ushort _currentIrZoom { get; set; }

        double _currentPan { get; set; }

        double _currentTilt { get; set; }

        ref long _eoLensStatusVersion { get; }

        string _irFocusDirectUnavailableSession { get; set; }

        ref long _irLensStatusVersion { get; }

        ref double? _lastPanAbsoluteTarget { get; }

        ref long _panStartTxCount { get; }

        ref long _panTiltStatusVersion { get; }

        Services.Position.PositionSnapshotService _positionSnapshotService { get; }

        string _ptzPerformanceScenario { get; set; }

        ref long _tiltStartTxCount { get; }

        ref global::System.Tuple<double, double, long> _videoCenterPose { get; }

        /// <summary>
        /// 2026-10-06: 0x31의 MCB/SCB 원본 상태를 보존한다. 요청 송신 여부로 상태를 추정하지 않고 이 수신값만 표시 기준으로
        /// 사용한다.
        /// </summary>
        void ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime = null, string sourceFunction = "0x31");

        /// <summary>
        /// 길이와 BOOLEAN 범위를 확인한 capability 응답으로 조회/설정 지원을 갱신한다.
        /// </summary>
        void ApplyDigitalZoomCapability(byte[] p);

        /// <summary>
        /// 유효한 장비 ON/OFF 응답을 적용하며 요청 확인과 추정 억제 상태를 갱신한다.
        /// </summary>
        void ApplyDigitalZoomState(byte[] p);

        /// <summary>
        /// DIGITAL_ZOOM 거절 응답 시 요청·추정을 해제한다. 수동 재요청 버튼은 유지한다.
        /// </summary>
        void ApplyDigitalZoomUnavailable(int target);

        /// <summary>
        /// 현재 연결 세션의 지원 응답을 반영하고 필요한 상태 조회를 예약한다.
        /// </summary>
        void ApplySpecialFeatureCapability(byte[] p);

        /// <summary>
        /// 검증한 기능 상태 응답을 해당 채널의 행에 반영한다.
        /// </summary>
        void ApplySpecialFeatureState(byte[] p);

        /// <summary>
        /// 기능 미지원 또는 실행 거절 응답을 해당 행에 반영한다.
        /// </summary>
        void ApplySpecialFeatureUnavailable(byte[] p);

        /// <summary>
        /// 연결 장비의 빌드 정보를 조회해 설정 지원 여부를 판단할 근거를 확보한다.
        /// </summary>
        void BeginWebAgentBuildCheck();

        bool CameraFeatureTransportReady { get; }

        /// <summary>
        /// UI 큐에 넣기 전에 세션을 캡처하고 연결 변경 후 도착한 callback을 버린다.
        /// </summary>
        void DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply);

        /// <summary>
        /// Web Agent의 0/360도 경계를 고려한 Tilt 차이를 반환한다. ROOFTOP LA는 기존 선형 -90 ~ 90도 차이를 유지한다.
        /// </summary>
        double GetTiltDifference(double currentTilt, double targetTilt);

        /// <summary>
        /// GPS 미지원·사용 불가 응답을 설정 상태와 안내에 반영한다.
        /// </summary>
        void HandleGpsUnavailable(byte[] payload);

        /// <summary>
        /// 정밀 명령의 실행 불가 응답을 확인해 보관된 동작 상태와 정지 처리를 정리한다.
        /// </summary>
        void HandlePrecisionUnavailable(byte[] payload);

        /// <summary>
        /// 실패 응답의 원 명령을 확인해 해당 기능의 요청 상태만 갱신한다.
        /// </summary>
        void HandleWebAgentEquipmentUnavailable(byte[] payload);

        bool IsEnvironmentStatusSelected { get; }

        /// <summary>
        /// 채널별 렌즈 응답 시각을 원자적으로 기록한다.
        /// </summary>
        void MarkLensEvidence(bool ir);

        /// <summary>
        /// 2026-10-14 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다.
        /// </summary>
        void MarkPresetLensEvidence(bool ir, bool zoom, bool focus);

        /// <summary>
        /// EO 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void NotifyEoCurrentStatusChanged(bool panChanged = true, bool tiltChanged = true, bool zoomChanged = true, bool focusChanged = true, bool powerChanged = true);

        /// <summary>
        /// IR 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void NotifyIrCurrentStatusChanged();

        /// <summary>
        /// AF 응답의 형식·세션·대상·작업을 확인하고 지원 여부와 실제 상태를 반영한다.
        /// </summary>
        void ParseAutoFocusResponse(byte[] payload);

        /// <summary>
        /// 파서가 검증한 표본을 현재 연결의 UI에만 반영한다.
        /// </summary>
        void ParseEquipmentTemperature(byte[] payload);

        /// <summary>
        /// GPS 자동 전송 주기 응답을 해석해 현재 설정 표시를 갱신한다.
        /// </summary>
        void ParseGpsAutoTxRateResponse(byte[] payload);

        /// <summary>
        /// 저장된 자세 보정값 응답을 해석해 입력·표시 상태에 반영한다.
        /// </summary>
        void ParseStoredRpyOffsetResponse(byte[] payload);

        /// <summary>
        /// 저장된 자세 전송 주기 응답을 해석해 표시 상태를 갱신한다.
        /// </summary>
        void ParseStoredRpyRateResponse(byte[] payload);

        /// <summary>
        /// GPS 응답을 해석하고 유효한 위치·방향 데이터를 반영한다.
        /// </summary>
        void ParseWebAgentGps(byte[] payload);

        /// <summary>
        /// WEB AGENT의 RPY 보정 설정 응답을 읽어 성공·실패 상태에 반영한다.
        /// </summary>
        void ParseWebAgentRpyOffsetAck(byte[] payload);

        /// <summary>
        /// WEB AGENT의 RPY 전송 주파수 설정 응답을 읽어 성공·실패 상태에 반영한다.
        /// </summary>
        void ParseWebAgentRpyRateAck(byte[] payload);

        /// <summary>
        /// 장비 UTC 응답을 읽어 화면과 상태 유효성 판정용 시각 기준을 갱신한다.
        /// </summary>
        void ParseWebAgentSystemDateTime(byte[] payload);

        /// <summary>
        /// 최신 온도 캐시 조회는 새 센서 측정이나 장비 시간 설정이 아니다.
        /// </summary>
        void QueryEquipmentTemperature();

        /// <summary>
        /// 위치 상태 알림을 화면 스레드에 예약해 수신 스레드의 직접 화면 접근을 피한다.
        /// </summary>
        void QueuePositionStatusNotification(bool includeLastSnapshot = false);

        /// <summary>
        /// 0x24는 최대 30Hz이므로 내부 위치 상태는 즉시 갱신하되, 추가 RPY UI 문자열은 최대 10Hz로 제한하고 진단 로그는 약 1Hz로
        /// 제한한다.
        /// </summary>
        void RecordRpyTelemetry(double roll, double pitch, double yaw);

        /// <summary>
        /// 새 연결의 GPS 전송 주기를 조회해 이전 세션 표시가 남지 않게 한다.
        /// </summary>
        void RequestGpsAutoTxRateAfterConnection();

        /// <summary>
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다.
        /// </summary>
        void ResetDigitalZoomSession();

        /// <summary>
        /// 연결 세대를 변경하고 이전 장비의 AF 상태·대기 요청·실행 확인을 초기화한다.
        /// </summary>
        void ResetWebAgentControlSession();

        /// <summary>
        /// 장비 변경·재연결 시 이전 온도와 요청 상태를 남기지 않는다.
        /// </summary>
        void ResetWebAgentEquipmentState();

        /// <summary>
        /// GPS·IMU 수신 원시값을 방위 기준 계산에 보관한다.
        /// </summary>
        void UpdateGpsImuRawReference(double yaw);

        /// <summary>
        /// EO 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        void UpdatePositionEoLens(int eoZoom, int eoFocus);

        /// <summary>
        /// IMU Roll·Pitch·Yaw 응답을 위치 상태에 반영한다.
        /// </summary>
        void UpdatePositionImu(double roll, double pitch, double yaw, bool valid);

        /// <summary>
        /// IR 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        void UpdatePositionIrLens(int irZoom, int irFocus);

        /// <summary>
        /// 수신한 WEB AGENT 빌드 번호를 지원 기능 판단에 반영한다.
        /// </summary>
        void UpdateWebAgentBuild(string buildText);

        Action<byte[]> ApplySpecialFeatureCapabilityCallback { get; }

        Action<byte[]> ApplySpecialFeatureStateCallback { get; }

        Action<byte[]> ApplySpecialFeatureUnavailableCallback { get; }
    }

    /// <summary>수신 패킷 분배 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class WebAgentProtocolFeature
    {

        #region [협력 객체와 초기화]

        private readonly IWebAgentProtocolFeatureContext _context;

        /// <summary>
        /// WEB 상태 패킷 해석 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal WebAgentProtocolFeature(IWebAgentProtocolFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 기능 객체가 사용하는 초기 상태·목록·타이머를 준비한다.
        /// </summary>
        internal void InitializeState()
        {
            _lastWebAgentPanRaw = -1;

            _lastWebAgentTiltRaw = -1;

            _webAgentTelemetryClockGeneration = -1;
        }

        #endregion

        #region [상태 필드]

        internal long _lastLegacyEoLensStatusUtcTicks;

        internal long _lastEoLensConflictWarningUtcTicks;

        internal long _lastPanTiltMoveCommandUtcTicks;

        internal long _lastPanTiltValueChangeUtcTicks;

        internal long _lastPanTiltStallWarningUtcTicks;

        internal int _lastWebAgentPanRaw;

        internal int _lastWebAgentTiltRaw;

        internal int _webAgentTelemetryNegotiationStarted;

        internal int _webAgentSeparatedTelemetryAckMask;

        internal readonly long[] _webAgentLensSampleMilliseconds = new long[2];

        internal long _webAgentPanSampleMilliseconds;

        internal long _webAgentTiltSampleMilliseconds;

        internal long _webAgentPowerSampleMilliseconds;

        internal long _webAgentSplitLogTicks;

        internal readonly TelemetryClock _webAgentTelemetryClock = new TelemetryClock();

        internal int _webAgentTelemetryClockGeneration;

        internal double _webAgentTelemetryRejectedLogAt;

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 지연 수신된 WEB 텔레메트리의 종류와 시각을 진단 로그에 기록한다.
        /// </summary>
        internal void LogDeferredWebAgentTelemetry(string channel, ulong sample)
        {
            double now = TelemetryClock.MonotonicMilliseconds;

            if (now - _webAgentTelemetryRejectedLogAt < 5000)
            {
                return;
            }

            _webAgentTelemetryRejectedLogAt = now;

            ConsoleLogHelper.State("WEB AGENT TELEMETRY DEFERRED", "CHANNEL=" + channel + " / SOURCE_TIMESTAMP=" + sample + " / Clock reference pending, invalid time, or stale sample; packet is not used for control");
        }

        /// <summary>
        /// 연결마다 이전 캐시 시각과 기능 협상 결과를 비운다.
        /// </summary>
        internal void ResetWebAgentTelemetrySession()
        {
            _context.ResetWebAgentControlSession();

            _context.ResetWebAgentEquipmentState();

            _webAgentTelemetryClock.Reset();

            _webAgentTelemetryClockGeneration = -1;

            Interlocked.Exchange(ref _webAgentTelemetryNegotiationStarted, 0);

            Interlocked.Exchange(ref _webAgentSeparatedTelemetryAckMask, 0);

            Interlocked.Exchange(ref _lastLegacyEoLensStatusUtcTicks, 0);

            Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, 0);

            Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, 0);

            Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, 0);

            for (int index = 0; index < _webAgentLensSampleMilliseconds.Length; index++)
            {
                Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[index], 0);
            }

            _context._irFocusDirectUnavailableSession = null;
        }

        /// <summary>
        /// F1 장비 시간과 단조 시계를 사용하여 로컬 기준 샘플 시각으로 보정한다.
        /// </summary>
        internal bool TryReadTelemetryTime(ulong milliseconds, out DateTimeOffset value, bool requireFresh = true)
        {
            int generation;

            bool valid = _webAgentTelemetryClock.TryGetLocalSampleTime(milliseconds, TelemetryClock.MonotonicMilliseconds, DateTimeOffset.UtcNow, out value, out generation, requireFresh);

            if (generation != _webAgentTelemetryClockGeneration)
            {
                _webAgentTelemetryClockGeneration = generation;

                Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, 0);

                Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, 0);

                Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, 0);

                for (int index = 0; index < _webAgentLensSampleMilliseconds.Length; index++)
                {
                    Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[index], 0);
                }
            }

            return valid;
        }

        /// <summary>
        /// 새 WEB AGENT 연결의 조회·상태 관리 세션을 초기화한다.
        /// </summary>
        internal void InitializeWebAgentSession()
        {
            if (!_context.IsEnvironmentStatusSelected)
            {
                return;
            }

            _context.ResetDigitalZoomSession();

            _context.BeginWebAgentBuildCheck();

            _context._controlCommandService.RequestWebAgentIdentity();

            _context.QueryEquipmentTemperature();

            _context._controlCommandService.RequestLensCapabilityAndTelemetry(0x00);

            _context._controlCommandService.RequestLensCapabilityAndTelemetry(0x01);

            _context._controlCommandService.RequestCameraFeatureCapability(0x01);

            _context._controlCommandService.RequestCameraFeatureCapability(0x00);

            _context._controlCommandService.RequestImuTelemetry();

            _context.RequestGpsAutoTxRateAfterConnection();

            for (byte category = 0; category <= 3; category++)
            {
                _context._controlCommandService.RequestWebAgentTelemetrySetting(category);
            }

            ConsoleLogHelper.State("WEB AGENT V2.9", "Identity, EO/IR lens, feature, IMU, GPS rate and separated telemetry capability queries sent");
        }

        /// <summary>
        /// WEB AGENT 텔레메트리의 명령·채널을 판별해 해당 상태 해석기로 전달한다.
        /// </summary>
        internal void ParseWebAgentTelemetryPacket(LaResponsePacket packet)
        {
            byte[] payload = packet.Payload;

            try
            {
                switch (packet.Function)
                {
                    case 0x23:
                        _context.ParseWebAgentGps(payload);
                        break;
                    case 0x24:
                        ParseWebAgentImu(payload);
                        break;
                    case 0x25:
                        ParseWebAgentCapability(payload);
                        break;
                    case 0x26:
                        ParseWebAgentIdentity(payload);
                        break;
                    case 0x27:
                        ParseWebAgentFeatureState(payload);
                        break;
                    case 0x28:
                        ParseWebAgentLensTelemetry(payload);
                        break;
                    case 0x29:
                        ParseWebAgentHoming(payload);
                        break;
                    case 0x2A:
                        ParseWebAgentUnavailable(payload);
                        break;
                    case 0x2B:
                        _context.ParseWebAgentRpyRateAck(payload);
                        break;
                    case 0x2C:
                        _context.ParseWebAgentRpyOffsetAck(payload);
                        break;
                    case 0x2D:
                        _context.ParseWebAgentSystemDateTime(payload);
                        break;
                    case 0x2E:
                        _context.ParseStoredRpyOffsetResponse(payload);
                        break;
                    case 0x2F:
                        _context.ParseStoredRpyRateResponse(payload);
                        break;
                    case 0x30:
                        _context.ParseGpsAutoTxRateResponse(payload);
                        break;
                    case 0x32:
                        ParseWebAgentPanTiltTelemetry(payload);
                        break;
                    case 0x33:
                        ParseWebAgentPowerTelemetry(payload);
                        break;
                    case 0x34:
                        ParseWebAgentTelemetrySetting(payload);
                        break;
                    case 0x35:
                        _context.ParseAutoFocusResponse(payload);
                        break;
                    case 0x36:
                        _context.ParseEquipmentTemperature(payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("WEB AGENT V1.8", "Response parse failed / " + ex.Message);
            }
        }

        /// <summary>
        /// v2.9 Function 0x32 PAN/TILT 분리 텔레메트리를 기존 상태에 반영한다. Payload의 각도와 시각은 Little
        /// Endian이다.
        /// </summary>
        internal void ParseWebAgentPanTiltTelemetry(byte[] p)
        {
            if (p == null || p.Length != 22 || p[0] != 0x01)
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x32 / INVALID_PAYLOAD");

                return;
            }

            byte validFlags = p[1];

            bool panValid = (validFlags & 0x01) != 0;

            bool tiltValid = (validFlags & 0x02) != 0;

            ushort panRaw = BitConverter.ToUInt16(p, 2);

            ushort tiltRaw = BitConverter.ToUInt16(p, 4);

            ulong panUpdatedAt = BitConverter.ToUInt64(p, 6);

            ulong tiltUpdatedAt = BitConverter.ToUInt64(p, 14);

            DateTimeOffset panTime;

            DateTimeOffset tiltTime;

            panValid &= TryReadTelemetryTime(panUpdatedAt, out panTime);

            tiltValid &= TryReadTelemetryTime(tiltUpdatedAt, out tiltTime);

            panValid &= DateTimeOffset.UtcNow - panTime <= TimeSpan.FromSeconds(3);

            tiltValid &= DateTimeOffset.UtcNow - tiltTime <= TimeSpan.FromSeconds(3);

            if (!panValid && !tiltValid)
            {
                LogDeferredWebAgentTelemetry("PT", panUpdatedAt);
            }

            if ((panValid && panRaw > 35999) || (tiltValid && tiltRaw > 35999))
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", $"FUNCTION=0x32 / OUT_OF_RANGE / PAN={panRaw} / TILT={tiltRaw}");

                return;
            }

            double previousPan = _context._currentPan;

            double previousTilt = _context._currentTilt;

            bool panNew = panValid && (long)panUpdatedAt > Interlocked.Read(ref _webAgentPanSampleMilliseconds);

            bool tiltNew = tiltValid && (long)tiltUpdatedAt > Interlocked.Read(ref _webAgentTiltSampleMilliseconds);

            if (panNew)
            {
                Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, (long)panUpdatedAt);

                _context._currentPan = ConvertWebAgentPanToGuiAngle(panRaw / 100.0);

                if (Math.Abs(Math.Abs(_context._currentPan) - 180.0) <= 0.05 && _context._lastPanAbsoluteTarget.HasValue && Math.Abs(Math.Abs(_context._lastPanAbsoluteTarget.Value) - 180.0) <= 0.05)
                {
                    _context._currentPan = _context._lastPanAbsoluteTarget.Value < 0 ? -180.0 : 180.0;
                }
            }

            if (tiltNew)
            {
                Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, (long)tiltUpdatedAt);

                _context._currentTilt = NormalizeUnsignedWebAgentTilt(tiltRaw / 100.0, _context._currentTilt);
            }

            if (!panNew && !tiltNew)
            {
                return;
            }

            if (panNew && _context._activePanAbsoluteTarget.HasValue && Math.Abs(_context._currentPan - _context._activePanAbsoluteTarget.Value) <= 0.03)
            {
                _context._activePanAbsoluteTarget = null;
            }

            if (tiltNew && _context._activeTiltAbsoluteTarget.HasValue && Math.Abs(_context.GetTiltDifference(_context._currentTilt, _context._activeTiltAbsoluteTarget.Value)) <= 0.03)
            {
                _context._activeTiltAbsoluteTarget = null;
            }

            if (panValid && tiltValid)
            {
                DateTimeOffset poseTime = panTime < tiltTime ? panTime : tiltTime;

                Interlocked.Exchange(ref _context._videoCenterPose, Tuple.Create(_context._currentPan, _context._currentTilt, poseTime.UtcDateTime.Ticks));

                Interlocked.Increment(ref _context._panTiltStatusVersion);

                _context._positionSnapshotService.UpdatePtz(_context._currentPan, _context._currentTilt, poseTime);

                _context.QueuePositionStatusNotification();
            }

            _context.NotifyEoCurrentStatusChanged(panValid && Math.Abs(previousPan - _context._currentPan) > 0.0001, tiltValid && Math.Abs(previousTilt - _context._currentTilt) > 0.0001, false, false, false);

            long nowTicks = DateTime.UtcNow.Ticks;

            if (nowTicks - Interlocked.Read(ref _webAgentSplitLogTicks) >= TimeSpan.TicksPerSecond)
            {
                Interlocked.Exchange(ref _webAgentSplitLogTicks, nowTicks);

                ConsoleLogHelper.State("WEB AGENT TELEMETRY", $"FUNCTION=0x32 / VALID=0x{validFlags:X2} / PAN={_context._currentPan:F2} / TILT={_context._currentTilt:F2} / PAN_AT={panUpdatedAt} / TILT_AT={tiltUpdatedAt}");
            }
        }

        /// <summary>
        /// v2.9 Function 0x33 MCB/SCB 전원 상태 이벤트를 반영한다. 미수신 Valid=0은 실제 OFF로 해석하지 않는다.
        /// </summary>
        internal void ParseWebAgentPowerTelemetry(byte[] p)
        {
            if (p == null || p.Length != 12 || p[0] != 0x01)
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x33 / INVALID_PAYLOAD");

                return;
            }

            byte validFlags = p[1];

            byte mcbStatus = p[2];

            byte scbStatus = p[3];

            ulong updatedAt = BitConverter.ToUInt64(p, 4);

            DateTimeOffset powerTime;

            if ((validFlags & 0x01) != 0 && TryReadTelemetryTime(updatedAt, out powerTime, false) && (long)updatedAt > Interlocked.Read(ref _webAgentPowerSampleMilliseconds))
            {
                Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, (long)updatedAt);

                _context.ApplyDevicePowerStatus(mcbStatus, scbStatus, powerTime.LocalDateTime, "0x33");
            }

            ConsoleLogHelper.State("WEB AGENT TELEMETRY", $"FUNCTION=0x33 / VALID=0x{validFlags:X2} / MCB=0x{mcbStatus:X2} / SCB=0x{scbStatus:X2} / UPDATED_AT={updatedAt}");
        }

        /// <summary>
        /// v2.9 Function 0x34 설정/지원 응답을 처리한다. 구형 서버는 BB에 응답하지 않으므로 기존 호환 모드를 그대로 유지한다.
        /// </summary>
        internal void ParseWebAgentTelemetrySetting(byte[] p)
        {
            if (p == null || p.Length != 6 || p[0] != 0x01 || p[1] > 3)
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x34 / INVALID_PAYLOAD");

                return;
            }

            byte category = p[1];

            byte mode = p[2];

            byte frequencyHz = p[3];

            byte maximumHz = p[4];

            byte flags = p[5];

            if (mode > 1 || frequencyHz > 30 || maximumHz > 30 || (category == 3 && (frequencyHz != 0 || maximumHz != 0)))
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x34 / INVALID_SETTING");

                return;
            }

            ConsoleLogHelper.State("WEB AGENT TELEMETRY", $"FUNCTION=0x34 / CATEGORY={category} / MODE={mode} / HZ={frequencyHz} / MAX={maximumHz} / FLAGS=0x{flags:X2}");

            if (_context.IsEnvironmentStatusSelected && category == 0 && maximumHz >= 10 && (flags & 0x01) != 0 && Interlocked.CompareExchange(ref _webAgentTelemetryNegotiationStarted, 1, 0) == 0)
            {
                bool ptSent = _context._controlCommandService.SetWebAgentTelemetryRate(0, 10);

                bool eoSent = _context._controlCommandService.SetWebAgentTelemetryRate(1, 2);

                bool irSent = _context._controlCommandService.SetWebAgentTelemetryRate(2, 2);

                ConsoleLogHelper.Command("WEB AGENT TELEMETRY", $"SEPARATED_MODE_REQUEST / PT_10HZ={ptSent} / EO_2HZ={eoSent} / IR_2HZ={irSent}");
            }

            if (mode == 0x01 && category <= 2)
            {
                int mask = 1 << category;

                int currentMask;

                int acknowledged;

                do
                {
                    currentMask = Volatile.Read(ref _webAgentSeparatedTelemetryAckMask);

                    acknowledged = currentMask | mask;
                }
                while (Interlocked.CompareExchange(ref _webAgentSeparatedTelemetryAckMask, acknowledged, currentMask) != currentMask);

                if ((acknowledged & 0x07) == 0x07 && (currentMask & 0x07) != 0x07)
                {
                    _context._controlCommandService.RequestWebAgentTelemetrySnapshot(3);
                }
            }
        }

        /// <summary>
        /// WEB AGENT의 관성 센서 응답을 해석해 현재 센서 상태를 갱신한다.
        /// </summary>
        internal void ParseWebAgentImu(byte[] p)
        {
            if (p.Length < 12)
            {
                return;
            }

            double roll = BitConverter.ToInt32(p, 0) / 10000.0;

            double pitch = BitConverter.ToInt32(p, 4) / 10000.0;

            double yaw = BitConverter.ToInt32(p, 8) / 10000.0;

            bool valid = Math.Abs(roll) <= 360.0 && Math.Abs(pitch) <= 360.0 && Math.Abs(yaw) <= 360.0;

            _context.UpdatePositionImu(roll, pitch, yaw, valid);

            _context.RecordRpyTelemetry(roll, pitch, yaw);

            _context.UpdateGpsImuRawReference(yaw);
        }

        /// <summary>
        /// 장비가 응답한 지원 기능을 반영해 사용 가능한 제어 기능을 구분한다.
        /// </summary>
        internal void ParseWebAgentCapability(byte[] p)
        {
            if (p.Length < 13)
            {
                return;
            }

            _context.DispatchSpecialFeaturePacket(p, _context.ApplySpecialFeatureCapabilityCallback);

            var digitalPayload = (byte[])p.Clone();

            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_context.CameraFeatureTransportReady)
                {
                    _context.ApplyDigitalZoomCapability(digitalPayload);
                }
            }));

            ConsoleLogHelper.State("WEB AGENT CAPABILITY", string.Format("SCHEMA={0} / TARGET=0x{1:X2} / LENS_FLAGS=0x{2:X4} / FEATURES={3}", p[0], p[1], BitConverter.ToUInt16(p, 2), p[12]));
        }

        /// <summary>
        /// 장비 식별 응답을 읽어 연결 장비 정보를 갱신한다.
        /// </summary>
        internal void ParseWebAgentIdentity(byte[] p)
        {
            if (p.Length < 2)
            {
                return;
            }

            int versionLength = p[0];

            if (versionLength > 31 || p.Length < versionLength + 2)
            {
                return;
            }

            int buildLength = p[versionLength + 1];

            if (p.Length < versionLength + buildLength + 2)
            {
                return;
            }

            string version = Encoding.ASCII.GetString(p, 1, versionLength);

            string build = Encoding.ASCII.GetString(p, versionLength + 2, buildLength);

            _context.UpdateWebAgentBuild(build);

            ConsoleLogHelper.State("WEB AGENT IDENTITY", "VERSION=" + version + " / BUILD=" + build);
        }

        /// <summary>
        /// 채널별 렌즈 위치와 유효 시각을 확인해 수신한 줌·초점 상태를 반영한다.
        /// </summary>
        internal void ParseWebAgentLensTelemetry(byte[] p)
        {
            if (p.Length != 16 || p[0] != 1 || p[1] > 1)
            {
                return;
            }

            byte target = p[1];

            byte flags = p[2];

            ushort zoom = BitConverter.ToUInt16(p, 4);

            ushort focus = BitConverter.ToUInt16(p, 6);

            bool zoomValid = (flags & 0x01) != 0 && zoom <= 1000;

            bool focusValid = (flags & 0x02) != 0 && focus <= 1000;

            DateTimeOffset sampleTime;

            ulong sampleMilliseconds = BitConverter.ToUInt64(p, 8);

            if (!TryReadTelemetryTime(sampleMilliseconds, out sampleTime) || DateTimeOffset.UtcNow - sampleTime > TimeSpan.FromSeconds(3))
            {
                LogDeferredWebAgentTelemetry(target == 0 ? "EO" : "IR", sampleMilliseconds);

                return;
            }

            if ((long)sampleMilliseconds <= Interlocked.Read(ref _webAgentLensSampleMilliseconds[target]))
            {
                return;
            }

            Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[target], (long)sampleMilliseconds);

            if (target == 0x00)
            {
                Interlocked.Increment(ref _context._eoLensStatusVersion);

                // 2026-09-15: Web Agent가 기존 Function 0x01에서는 EO Zoom/Focus를
                // 정상값으로 보내면서 Function 0x28에서는 valid=1인 0을 반복하는
                // 실장비 현상을 확인했다. 최근 0x01 상태가 있으면 해당 값을
                // 권위값으로 유지하여 CURRENT STATUS가 0과 정상값 사이에서
                // 깜빡이지 않도록 한다. 0x01이 끊긴 경우에만 0x28을 fallback한다.
                if (!HasRecentLegacyEoLensStatus())
                {
                    if (zoomValid)
                    {
                        _context._currentEoZoom = (short)zoom;

                        _context.MarkLensEvidence(false);
                    }

                    if (focusValid)
                    {
                        _context._currentEoFocus = (short)focus;
                    }

                    _context.MarkPresetLensEvidence(false, zoomValid, focusValid);

                    if (zoomValid && focusValid)
                    {
                        _context.UpdatePositionEoLens(_context._currentEoZoom, _context._currentEoFocus);
                    }

                    _context.NotifyEoCurrentStatusChanged();
                }
                else if ((zoomValid && zoom != (ushort)_context._currentEoZoom) || (focusValid && focus != (ushort)_context._currentEoFocus))
                {
                    LogWebAgentEoLensConflict(zoom, focus, flags);
                }
            }
            else if (target == 0x01)
            {
                if (zoomValid)
                {
                    _context._currentIrZoom = zoom;

                    _context.MarkLensEvidence(true);
                }

                if (focusValid)
                {
                    _context._currentIrFocus = focus;
                }

                _context.MarkPresetLensEvidence(true, zoomValid, focusValid);

                if (zoomValid && focusValid)
                {
                    _context.UpdatePositionIrLens(_context._currentIrZoom, _context._currentIrFocus);
                }

                Interlocked.Increment(ref _context._irLensStatusVersion);

                _context.NotifyIrCurrentStatusChanged();
            }

            ConsoleLogHelper.State("WEB AGENT LENS", string.Format("TARGET=0x{0:X2} / VALID=0x{1:X2} / ZOOM={2} / FOCUS={3}", target, flags, zoom, focus));
        }

        /// <summary>
        /// 기존 형식의 상태 수신 시각을 기록해 오래된 상태와 구분한다.
        /// </summary>
        internal void MarkLegacyWebAgentStatusReceived(ushort panRaw, ushort tiltRaw)
        {
            if (!_context.IsEnvironmentStatusSelected)
            {
                return;
            }

            long nowTicks = DateTime.UtcNow.Ticks;

            Interlocked.Exchange(ref _lastLegacyEoLensStatusUtcTicks, nowTicks);

            bool valueChanged = _lastWebAgentPanRaw != panRaw || _lastWebAgentTiltRaw != tiltRaw;

            _lastWebAgentPanRaw = panRaw;
            _lastWebAgentTiltRaw = tiltRaw;

            if (valueChanged || Interlocked.Read(ref _lastPanTiltValueChangeUtcTicks) == 0)
            {
                Interlocked.Exchange(ref _lastPanTiltValueChangeUtcTicks, nowTicks);

                return;
            }

            long lastCommandTicks = Interlocked.Read(ref _lastPanTiltMoveCommandUtcTicks);

            long lastChangeTicks = Interlocked.Read(ref _lastPanTiltValueChangeUtcTicks);

            if (lastCommandTicks == 0 || _context._activePanTiltMoveDirection == KeyboardPanTiltDirection.None || nowTicks - lastCommandTicks < TimeSpan.TicksPerSecond || nowTicks - lastCommandTicks > TimeSpan.TicksPerSecond * 3 || nowTicks - lastChangeTicks < TimeSpan.TicksPerSecond)
            {
                return;
            }

            long lastWarningTicks = Interlocked.Read(ref _lastPanTiltStallWarningUtcTicks);

            if (nowTicks - lastWarningTicks < WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(ref _lastPanTiltStallWarningUtcTicks, nowTicks);

            ConsoleLogHelper.Warning("WEB AGENT PAN/TILT", "Movement command was sent, but Function 0x01 position is unchanged" + " / PAN_RAW=" + panRaw + " / TILT_RAW=" + tiltRaw + " / CHECK=WebAgent PT status publisher");
        }

        /// <summary>
        /// 팬·틸트 이동 명령 시점을 기록해 이전 수신 위치를 새 이동의 완료로 오인하지 않는다.
        /// </summary>
        internal void MarkPanTiltMoveCommandIssued()
        {
            switch (_context._activePanTiltMoveDirection)
            {
                case KeyboardPanTiltDirection.PanLeft:
                    _context._ptzPerformanceScenario = "PAN_LEFT";
                    Interlocked.Increment(ref _context._panStartTxCount);
                    break;
                case KeyboardPanTiltDirection.PanRight:
                    _context._ptzPerformanceScenario = "PAN_RIGHT";
                    Interlocked.Increment(ref _context._panStartTxCount);
                    break;
                case KeyboardPanTiltDirection.TiltUp:
                    _context._ptzPerformanceScenario = "TILT_UP";
                    Interlocked.Increment(ref _context._tiltStartTxCount);
                    break;
                case KeyboardPanTiltDirection.TiltDown:
                    _context._ptzPerformanceScenario = "TILT_DOWN";
                    Interlocked.Increment(ref _context._tiltStartTxCount);
                    break;
                default:
                    _context._ptzPerformanceScenario = "PAN_TILT_DIAGONAL";
                    Interlocked.Increment(ref _context._panStartTxCount);

                    Interlocked.Increment(ref _context._tiltStartTxCount);
                    break;
            }

            if (!_context.IsEnvironmentStatusSelected)
            {
                return;
            }

            Interlocked.Exchange(ref _lastPanTiltMoveCommandUtcTicks, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// 기존 형식 EO 렌즈 상태가 최근 수신되어 비교에 사용할 수 있는지 확인한다.
        /// </summary>
        internal bool HasRecentLegacyEoLensStatus()
        {
            long receivedTicks = Interlocked.Read(ref _lastLegacyEoLensStatusUtcTicks);

            return receivedTicks > 0 && DateTime.UtcNow.Ticks - receivedTicks <= LegacyEoLensAuthorityTicks;
        }

        /// <summary>
        /// 신규·기존 EO 렌즈 상태가 충돌할 때 진단 로그를 남긴다.
        /// </summary>
        internal void LogWebAgentEoLensConflict(ushort zoom, ushort focus, byte flags)
        {
            long nowTicks = DateTime.UtcNow.Ticks;

            long lastWarningTicks = Interlocked.Read(ref _lastEoLensConflictWarningUtcTicks);

            if (nowTicks - lastWarningTicks < WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(ref _lastEoLensConflictWarningUtcTicks, nowTicks);

            ConsoleLogHelper.Warning("WEB AGENT EO LENS", "Conflicting Function 0x28 value ignored; recent Function 0x01 retained" + " / FLAGS=0x" + flags.ToString("X2") + " / 0x28_ZOOM=" + zoom + " / 0x28_FOCUS=" + focus + " / ACTIVE_ZOOM=" + _context._currentEoZoom + " / ACTIVE_FOCUS=" + _context._currentEoFocus);
        }

        /// <summary>
        /// 장비 기능 상태 응답을 해석해 현재 기능 상태를 갱신한다.
        /// </summary>
        internal void ParseWebAgentFeatureState(byte[] p)
        {
            if (p.Length < 18)
            {
                return;
            }

            _context.DispatchSpecialFeaturePacket(p, _context.ApplySpecialFeatureStateCallback);

            byte target = p[1];

            byte feature = p[2];

            byte flags = p[3];

            short value = BitConverter.ToInt16(p, 4);

            if (feature == 0x1D)
            {
                var digitalPayload = (byte[])p.Clone();

                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_context.CameraFeatureTransportReady)
                    {
                        _context.ApplyDigitalZoomState(digitalPayload);
                    }
                }));
            }

            ConsoleLogHelper.State("WEB AGENT FEATURE", string.Format("TARGET=0x{0:X2} / FEATURE=0x{1:X2} / FLAGS=0x{2:X2} / VALUE={3}", target, feature, flags, value));

            // 카메라별 원시 번호는 색 이름이나 직접 버튼 선택 상태로 해석하지 않는다.
        }

        /// <summary>
        /// 홈 동작 응답을 해석해 진행 상태를 반영한다.
        /// </summary>
        internal static void ParseWebAgentHoming(byte[] p)
        {
            if (p.Length < 7)
            {
                return;
            }

            ConsoleLogHelper.State("WEB AGENT HOMING", string.Format("TYPE={0} / AXIS={1} / STATE=0x{2:X2} / FLAGS=0x{3:X2} / STATUS=0x{4:X4}", p[1], p[2], p[3], p[4], BitConverter.ToUInt16(p, 5)));
        }

        /// <summary>
        /// 장비의 명령 미지원·실행 불가 응답을 처리한다.
        /// </summary>
        internal void ParseWebAgentUnavailable(byte[] p)
        {
            if (p.Length < 6)
            {
                return;
            }

            _context.HandlePrecisionUnavailable(p);

            _context.HandleWebAgentEquipmentUnavailable(p);

            if (p[0] == 1 && p[1] == 0 && p[2] == 0 && p[3] == 0x2B)
            {
                _context._irFocusDirectUnavailableSession = _context._connectionSessionId;
            }

            _context.DispatchSpecialFeaturePacket(p, _context.ApplySpecialFeatureUnavailableCallback);

            _context.HandleGpsUnavailable(p);

            if (p[2] == 0x1D && p[3] == 0xE9 && p[4] < 2)
            {
                byte target = p[4];

                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_context.CameraFeatureTransportReady)
                    {
                        _context.ApplyDigitalZoomUnavailable(target);
                    }
                }));
            }

            ConsoleLogHelper.Warning("WEB AGENT V1.8", string.Format("Request unavailable / STATUS={0} / REQUEST={1:X2}{2:X2}{3:X2}{4:X2}", p[1], p[2], p[3], p[4], p[5]));
        }

        #endregion

    }
}
