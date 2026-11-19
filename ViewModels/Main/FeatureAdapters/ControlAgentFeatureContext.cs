using OpenCvWpfTracking.Services.Communication;
using System;
using System.Diagnostics;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class ControlAgentFeatureContext : IControlAgentFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal ControlAgentFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? IControlAgentFeatureContext._activePanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activePanAbsoluteTarget; }
        }

        ref double? IControlAgentFeatureContext._activeTiltAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activeTiltAbsoluteTarget; }
        }

        string IControlAgentFeatureContext._connectionSessionId
        {
            get { return _features._devicePowerFeature._connectionSessionId; }
            set { _features._devicePowerFeature._connectionSessionId = value; }
        }

        ref global::System.Threading.CancellationTokenSource IControlAgentFeatureContext._controlAgentReconnectCts
        {
            get { return ref _features._screenStateFeature._controlAgentReconnectCts; }
        }

        IControlCommandServicePort IControlAgentFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        short IControlAgentFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IControlAgentFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IControlAgentFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort IControlAgentFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        double IControlAgentFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        byte IControlAgentFeatureContext._currentPowerStatus
        {
            get { return _features._screenStateFeature._currentPowerStatus; }
            set { _features._screenStateFeature._currentPowerStatus = value; }
        }

        double IControlAgentFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        ref int IControlAgentFeatureContext._eoFocusReceiveSequence
        {
            get { return ref _features._screenStateFeature._eoFocusReceiveSequence; }
        }

        ref long IControlAgentFeatureContext._eoLensStatusVersion
        {
            get { return ref _features._screenStateFeature._eoLensStatusVersion; }
        }

        Stopwatch IControlAgentFeatureContext._focusLogStopwatch
        {
            get { return _features.Runtime._focusLogStopwatch; }
        }

        ref long IControlAgentFeatureContext._function01ReceiveCount
        {
            get { return ref _features._screenStateFeature._function01ReceiveCount; }
        }

        bool IControlAgentFeatureContext._hasWebAgentTiltLimits
        {
            get { return _features._screenStateFeature._hasWebAgentTiltLimits; }
            set { _features._screenStateFeature._hasWebAgentTiltLimits = value; }
        }

        ref long IControlAgentFeatureContext._irLensStatusVersion
        {
            get { return ref _features._screenStateFeature._irLensStatusVersion; }
        }

        bool IControlAgentFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        ILAPacketParserPort IControlAgentFeatureContext._laPacketParser
        {
            get { return _features.Runtime._laPacketParser; }
        }

        long IControlAgentFeatureContext._lastEoFocusCommandElapsedMs
        {
            get { return _features._screenStateFeature._lastEoFocusCommandElapsedMs; }
            set { _features._screenStateFeature._lastEoFocusCommandElapsedMs = value; }
        }

        string IControlAgentFeatureContext._lastEoFocusCommandName
        {
            get { return _features._screenStateFeature._lastEoFocusCommandName; }
            set { _features._screenStateFeature._lastEoFocusCommandName = value; }
        }

        ref global::System.DateTime IControlAgentFeatureContext._lastLaExtendedStatusLogTime
        {
            get { return ref _features._screenStateFeature._lastLaExtendedStatusLogTime; }
        }

        ref global::System.DateTime IControlAgentFeatureContext._lastLaStatusLogTime
        {
            get { return ref _features._screenStateFeature._lastLaStatusLogTime; }
        }

        ref double? IControlAgentFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        ITcpClientServicePort IControlAgentFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref long IControlAgentFeatureContext._panTiltStatusVersion
        {
            get { return ref _features._screenStateFeature._panTiltStatusVersion; }
        }

        ref long IControlAgentFeatureContext._ptzfStatusUpdateCount
        {
            get { return ref _features._screenStateFeature._ptzfStatusUpdateCount; }
        }

        ref global::System.Tuple<double, double, long> IControlAgentFeatureContext._videoCenterPose
        {
            get { return ref _features._cameraFeatureControlFeature._videoCenterPose; }
        }

        ref int IControlAgentFeatureContext._webAgentTiltLimitQueryRequested
        {
            get { return ref _features._screenStateFeature._webAgentTiltLimitQueryRequested; }
        }

        double IControlAgentFeatureContext._webAgentTiltMaximum
        {
            get { return _features._screenStateFeature._webAgentTiltMaximum; }
            set { _features._screenStateFeature._webAgentTiltMaximum = value; }
        }

        double IControlAgentFeatureContext._webAgentTiltMinimum
        {
            get { return _features._screenStateFeature._webAgentTiltMinimum; }
            set { _features._screenStateFeature._webAgentTiltMinimum = value; }
        }

        /// <summary>
        /// 2026-10-06: 0x31의 MCB/SCB 원본 상태를 보존한다. 요청 송신 여부로 상태를 추정하지 않고 이 수신값만 표시 기준으로
        /// 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime, string sourceFunction)
        {
            _features._devicePowerFeature.ApplyDevicePowerStatus(mcbStatus, scbStatus, sourceTime, sourceFunction);
        }

        /// <summary>
        /// 2026-10-06: 선택 대상과 실제 연결 세션을 분리해 늦은 0x31이 새 ComboBox 프로필 소유로 기록되는 문제를 방지한다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.BeginConnectedControlAgentSession()
        {
            _features._devicePowerFeature.BeginConnectedControlAgentSession();
        }

        string IControlAgentFeatureContext.ControlAgentConnectionStatusColor
        {
            get { return _features._propertiesFeature.ControlAgentConnectionStatusColor; }
            set { _features._propertiesFeature.ControlAgentConnectionStatusColor = value; }
        }

        string IControlAgentFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features._propertiesFeature.ControlAgentConnectionStatusText; }
            set { _features._propertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IControlAgentFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IControlAgentFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        string IControlAgentFeatureContext.CurrentControlPowerText
        {
            get { return _features._propertiesFeature.CurrentControlPowerText; }
        }

        string IControlAgentFeatureContext.CurrentEoFocusText
        {
            get { return _features._propertiesFeature.CurrentEoFocusText; }
        }

        string IControlAgentFeatureContext.CurrentEoZoomText
        {
            get { return _features._propertiesFeature.CurrentEoZoomText; }
        }

        string IControlAgentFeatureContext.CurrentIrFocusText
        {
            get { return _features._propertiesFeature.CurrentIrFocusText; }
        }

        string IControlAgentFeatureContext.CurrentIrZoomText
        {
            get { return _features._propertiesFeature.CurrentIrZoomText; }
        }

        string IControlAgentFeatureContext.CurrentLaPresetSnapshotText
        {
            get { return _features._propertiesFeature.CurrentLaPresetSnapshotText; }
        }

        double IControlAgentFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double IControlAgentFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        string IControlAgentFeatureContext.CurrentPanText
        {
            get { return _features._propertiesFeature.CurrentPanText; }
        }

        string IControlAgentFeatureContext.CurrentPowerText
        {
            get { return _features._propertiesFeature.CurrentPowerText; }
        }

        string IControlAgentFeatureContext.CurrentPresetSnapshotText
        {
            get { return _features._propertiesFeature.CurrentPresetSnapshotText; }
        }

        string IControlAgentFeatureContext.CurrentTiltText
        {
            get { return _features._propertiesFeature.CurrentTiltText; }
        }

        byte IControlAgentFeatureContext.EffectivePanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.EffectivePanTiltSpeedLevel; }
        }

        /// <summary>
        /// 연결 장비 세션 정보를 종료해 이전 장비 응답을 새 연결에 적용하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.EndConnectedControlAgentSession()
        {
            _features._devicePowerFeature.EndConnectedControlAgentSession();
        }

        string IControlAgentFeatureContext.EnvironmentEoFocusStatusText
        {
            get { return _features._propertiesFeature.EnvironmentEoFocusStatusText; }
        }

        string IControlAgentFeatureContext.EnvironmentEoZoomStatusText
        {
            get { return _features._propertiesFeature.EnvironmentEoZoomStatusText; }
        }

        string IControlAgentFeatureContext.EnvironmentIrFocusStatusText
        {
            get { return _features._propertiesFeature.EnvironmentIrFocusStatusText; }
        }

        string IControlAgentFeatureContext.EnvironmentIrZoomStatusText
        {
            get { return _features._propertiesFeature.EnvironmentIrZoomStatusText; }
        }

        string IControlAgentFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        string IControlAgentFeatureContext.EoStatusText
        {
            get { return _features._propertiesFeature.EoStatusText; }
            set { _features._propertiesFeature.EoStatusText = value; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// -&gt; Standard 0(Far) Raw 0(Near) -&gt; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IControlAgentFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// -&gt; Standard 0(Wide) Raw 0(Tele) -&gt; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IControlAgentFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// Web Agent의 0/360도 경계를 고려한 Tilt 차이를 반환한다. ROOFTOP LA는 기존 선형 -90 ~ 90도 차이를 유지한다.
        /// 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        double IControlAgentFeatureContext.GetTiltDifference(double currentTilt, double targetTilt)
        {
            return _features._moveControlFeature.GetTiltDifference(currentTilt, targetTilt);
        }

        /// <summary>
        /// 새 WEB AGENT 연결의 조회·상태 관리 세션을 초기화한다.
        /// </summary>
        void IControlAgentFeatureContext.InitializeWebAgentSession()
        {
            _features._webAgentProtocolFeature.InitializeWebAgentSession();
        }

        string IControlAgentFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        string IControlAgentFeatureContext.IrStatusText
        {
            get { return _features._propertiesFeature.IrStatusText; }
            set { _features._propertiesFeature.IrStatusText = value; }
        }

        string IControlAgentFeatureContext.LrfDistanceText
        {
            get { return _features._propertiesFeature.LrfDistanceText; }
            set { _features._propertiesFeature.LrfDistanceText = value; }
        }

        /// <summary>
        /// 기존 형식의 상태 수신 시각을 기록해 오래된 상태와 구분한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.MarkLegacyWebAgentStatusReceived(ushort panRaw, ushort tiltRaw)
        {
            _features._webAgentProtocolFeature.MarkLegacyWebAgentStatusReceived(panRaw, tiltRaw);
        }

        /// <summary>
        /// 채널별 렌즈 응답 시각을 원자적으로 기록한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.MarkLensEvidence(bool ir)
        {
            _features._inputCoordinatorFeature.MarkLensEvidence(ir);
        }

        /// <summary>
        /// 2026-10-14 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            _features._presetLensFeature.MarkPresetLensEvidence(ir, zoom, focus);
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IControlAgentFeatureContext.PanTiltCoordinateRangeText
        {
            get { return _features._propertiesFeature.PanTiltCoordinateRangeText; }
        }

        string IControlAgentFeatureContext.PanTiltSpeedDisplayText
        {
            get { return _features._propertiesFeature.PanTiltSpeedDisplayText; }
        }

        /// <summary>
        /// WEB AGENT 텔레메트리의 명령·채널을 판별해 해당 상태 해석기로 전달한다.
        /// </summary>
        void IControlAgentFeatureContext.ParseWebAgentTelemetryPacket(LaResponsePacket packet)
        {
            _features._webAgentProtocolFeature.ParseWebAgentTelemetryPacket(packet);
        }

        /// <summary>
        /// 입력 세대를 갱신해 대기 명령을 무효화하고 필요할 때 전체 PTZF를 정지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.ReleaseAllControlInputs(string reason)
        {
            _features._inputCoordinatorFeature.ReleaseAllControlInputs(reason);
        }

        /// <summary>
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.ResetDigitalZoomSession()
        {
            _features._cameraFeatureControlFeature.ResetDigitalZoomSession();
        }

        string IControlAgentFeatureContext.RooftopEoFocusStatusText
        {
            get { return _features._propertiesFeature.RooftopEoFocusStatusText; }
        }

        string IControlAgentFeatureContext.RooftopEoZoomStatusText
        {
            get { return _features._propertiesFeature.RooftopEoZoomStatusText; }
        }

        string IControlAgentFeatureContext.RooftopIrFocusStatusText
        {
            get { return _features._propertiesFeature.RooftopIrFocusStatusText; }
        }

        string IControlAgentFeatureContext.RooftopIrZoomStatusText
        {
            get { return _features._propertiesFeature.RooftopIrZoomStatusText; }
        }

        string IControlAgentFeatureContext.SessionControlIp
        {
            get { return _features._rtspSettingsFeature.SessionControlIp; }
        }

        string IControlAgentFeatureContext.SessionControlPort
        {
            get { return _features._rtspSettingsFeature.SessionControlPort; }
        }

        /// <summary>
        /// 위치 상태 저장소에 현재 장비의 연결 여부를 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.SetPositionConnectionState(bool connected)
        {
            _features._positionFeature.SetPositionConnectionState(connected);
        }

        /// <summary>
        /// 연결 직후와 다른 탭에서도 5초마다 읽기 전용 F1 조회. SYSTEM TIME 탭은 기존 1Hz이며 응답 대기 중에는 중복 송신하지
        /// 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.StartSystemTimePollingAfterConnection()
        {
            _features._systemDateTimeFeature.StartSystemTimePollingAfterConnection();
        }

        /// <summary>
        /// 시각 자동 조회를 멈추고 진행 중 조회 표시를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.StopSystemTimePolling(string reason)
        {
            _features._systemDateTimeFeature.StopSystemTimePolling(reason);
        }

        string IControlAgentFeatureContext.TiltAbsoluteRangeLabel
        {
            get { return _features._propertiesFeature.TiltAbsoluteRangeLabel; }
        }

        double? IControlAgentFeatureContext.TiltAbsoluteValue
        {
            get { return _features._propertiesFeature.TiltAbsoluteValue; }
            set { _features._propertiesFeature.TiltAbsoluteValue = value; }
        }

        /// <summary>
        /// IR 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        void IControlAgentFeatureContext.UpdatePositionIrLens(int irZoom, int irFocus)
        {
            _features._positionFeature.UpdatePositionIrLens(irZoom, irFocus);
        }

        /// <summary>
        /// 수신한 PTZ와 EO 렌즈 값을 위치 상태에 반영한다.
        /// </summary>
        void IControlAgentFeatureContext.UpdatePositionPtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus)
        {
            _features._positionFeature.UpdatePositionPtzAndEoLens(pan, tilt, eoZoom, eoFocus);
        }
    }
}
