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

        /// <summary>현재 제어 작업의 진입 사유를 전달하고 자동 추적을 종료한다.</summary>
        void IControlAgentFeatureContext.StopAutoTracking(string reason)
        {
            _features.AutoTrackingFeature.Stop(reason);
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal ControlAgentFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? IControlAgentFeatureContext.ActivePanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activePanAbsoluteTarget; }
        }

        ref double? IControlAgentFeatureContext.ActiveTiltAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activeTiltAbsoluteTarget; }
        }

        string IControlAgentFeatureContext.ConnectionSessionId
        {
            get { return _features.DevicePowerFeature._connectionSessionId; }
            set { _features.DevicePowerFeature._connectionSessionId = value; }
        }

        ref global::System.Threading.CancellationTokenSource IControlAgentFeatureContext.ControlAgentReconnectCts
        {
            get { return ref _features.ScreenStateFeature._controlAgentReconnectCts; }
        }

        IControlCommandServicePort IControlAgentFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        short IControlAgentFeatureContext.CurrentEoFocus
        {
            get { return _features.ScreenStateFeature._currentEoFocus; }
            set { _features.ScreenStateFeature._currentEoFocus = value; }
        }

        short IControlAgentFeatureContext.CurrentEoZoom
        {
            get { return _features.ScreenStateFeature._currentEoZoom; }
            set { _features.ScreenStateFeature._currentEoZoom = value; }
        }

        ushort IControlAgentFeatureContext.CurrentIrFocus
        {
            get { return _features.ScreenStateFeature._currentIrFocus; }
            set { _features.ScreenStateFeature._currentIrFocus = value; }
        }

        ushort IControlAgentFeatureContext.CurrentIrZoom
        {
            get { return _features.ScreenStateFeature._currentIrZoom; }
            set { _features.ScreenStateFeature._currentIrZoom = value; }
        }

        double IControlAgentFeatureContext.CurrentPan
        {
            get { return _features.ScreenStateFeature._currentPan; }
            set { _features.ScreenStateFeature._currentPan = value; }
        }

        byte IControlAgentFeatureContext.CurrentPowerStatus
        {
            get { return _features.ScreenStateFeature._currentPowerStatus; }
            set { _features.ScreenStateFeature._currentPowerStatus = value; }
        }

        double IControlAgentFeatureContext.CurrentTilt
        {
            get { return _features.ScreenStateFeature._currentTilt; }
            set { _features.ScreenStateFeature._currentTilt = value; }
        }

        ref int IControlAgentFeatureContext.EoFocusReceiveSequence
        {
            get { return ref _features.ScreenStateFeature._eoFocusReceiveSequence; }
        }

        ref long IControlAgentFeatureContext.EoLensStatusVersion
        {
            get { return ref _features.ScreenStateFeature._eoLensStatusVersion; }
        }

        Stopwatch IControlAgentFeatureContext.FocusLogStopwatch
        {
            get { return _features.Runtime._focusLogStopwatch; }
        }

        ref long IControlAgentFeatureContext.Function01ReceiveCount
        {
            get { return ref _features.ScreenStateFeature._function01ReceiveCount; }
        }

        bool IControlAgentFeatureContext.HasWebAgentTiltLimits
        {
            get { return _features.ScreenStateFeature._hasWebAgentTiltLimits; }
            set { _features.ScreenStateFeature._hasWebAgentTiltLimits = value; }
        }

        ref long IControlAgentFeatureContext.IrLensStatusVersion
        {
            get { return ref _features.ScreenStateFeature._irLensStatusVersion; }
        }

        bool IControlAgentFeatureContext.IsDeviceConnectionRequested
        {
            get { return _features.ScreenStateFeature._isDeviceConnectionRequested; }
            set { _features.ScreenStateFeature._isDeviceConnectionRequested = value; }
        }

        ILAPacketParserPort IControlAgentFeatureContext.LaPacketParser
        {
            get { return _features.Runtime._laPacketParser; }
        }

        long IControlAgentFeatureContext.LastEoFocusCommandElapsedMs
        {
            get { return _features.ScreenStateFeature._lastEoFocusCommandElapsedMs; }
            set { _features.ScreenStateFeature._lastEoFocusCommandElapsedMs = value; }
        }

        string IControlAgentFeatureContext.LastEoFocusCommandName
        {
            get { return _features.ScreenStateFeature._lastEoFocusCommandName; }
            set { _features.ScreenStateFeature._lastEoFocusCommandName = value; }
        }

        ref global::System.DateTime IControlAgentFeatureContext.LastLaExtendedStatusLogTime
        {
            get { return ref _features.ScreenStateFeature._lastLaExtendedStatusLogTime; }
        }

        ref global::System.DateTime IControlAgentFeatureContext.LastLaStatusLogTime
        {
            get { return ref _features.ScreenStateFeature._lastLaStatusLogTime; }
        }

        ref double? IControlAgentFeatureContext.LastPanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._lastPanAbsoluteTarget; }
        }

        ITcpClientServicePort IControlAgentFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref long IControlAgentFeatureContext.PanTiltStatusVersion
        {
            get { return ref _features.ScreenStateFeature._panTiltStatusVersion; }
        }

        ref long IControlAgentFeatureContext.PtzfStatusUpdateCount
        {
            get { return ref _features.ScreenStateFeature._ptzfStatusUpdateCount; }
        }

        ref global::System.Tuple<double, double, long> IControlAgentFeatureContext.VideoCenterPose
        {
            get { return ref _features.CameraFeatureControlFeature._videoCenterPose; }
        }

        ref int IControlAgentFeatureContext.WebAgentTiltLimitQueryRequested
        {
            get { return ref _features.ScreenStateFeature._webAgentTiltLimitQueryRequested; }
        }

        double IControlAgentFeatureContext.WebAgentTiltMaximum
        {
            get { return _features.ScreenStateFeature._webAgentTiltMaximum; }
            set { _features.ScreenStateFeature._webAgentTiltMaximum = value; }
        }

        double IControlAgentFeatureContext.WebAgentTiltMinimum
        {
            get { return _features.ScreenStateFeature._webAgentTiltMinimum; }
            set { _features.ScreenStateFeature._webAgentTiltMinimum = value; }
        }

        /// <summary>
        /// 2026-10-06: 0x31의 MCB/SCB 원본 상태를 보존한다. 요청 송신 여부로 상태를 추정하지 않고 이 수신값만 표시 기준으로
        /// 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime, string sourceFunction)
        {
            _features.DevicePowerFeature.ApplyDevicePowerStatus(mcbStatus, scbStatus, sourceTime, sourceFunction);
        }

        /// <summary>
        /// 2026-10-06: 선택 대상과 실제 연결 세션을 분리해 늦은 0x31이 새 ComboBox 프로필 소유로 기록되는 문제를 방지한다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.BeginConnectedControlAgentSession()
        {
            _features.DevicePowerFeature.BeginConnectedControlAgentSession();
        }

        string IControlAgentFeatureContext.ControlAgentConnectionStatusColor
        {
            get { return _features.PropertiesFeature.ControlAgentConnectionStatusColor; }
            set { _features.PropertiesFeature.ControlAgentConnectionStatusColor = value; }
        }

        string IControlAgentFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features.PropertiesFeature.ControlAgentConnectionStatusText; }
            set { _features.PropertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IControlAgentFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string IControlAgentFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        string IControlAgentFeatureContext.CurrentControlPowerText
        {
            get { return _features.PropertiesFeature.CurrentControlPowerText; }
        }

        string IControlAgentFeatureContext.CurrentEoFocusText
        {
            get { return _features.PropertiesFeature.CurrentEoFocusText; }
        }

        string IControlAgentFeatureContext.CurrentEoZoomText
        {
            get { return _features.PropertiesFeature.CurrentEoZoomText; }
        }

        string IControlAgentFeatureContext.CurrentIrFocusText
        {
            get { return _features.PropertiesFeature.CurrentIrFocusText; }
        }

        string IControlAgentFeatureContext.CurrentIrZoomText
        {
            get { return _features.PropertiesFeature.CurrentIrZoomText; }
        }

        string IControlAgentFeatureContext.CurrentLaPresetSnapshotText
        {
            get { return _features.PropertiesFeature.CurrentLaPresetSnapshotText; }
        }

        double IControlAgentFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double IControlAgentFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        string IControlAgentFeatureContext.CurrentPanText
        {
            get { return _features.PropertiesFeature.CurrentPanText; }
        }

        string IControlAgentFeatureContext.CurrentPowerText
        {
            get { return _features.PropertiesFeature.CurrentPowerText; }
        }

        string IControlAgentFeatureContext.CurrentPresetSnapshotText
        {
            get { return _features.PropertiesFeature.CurrentPresetSnapshotText; }
        }

        string IControlAgentFeatureContext.CurrentTiltText
        {
            get { return _features.PropertiesFeature.CurrentTiltText; }
        }

        byte IControlAgentFeatureContext.EffectivePanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.EffectivePanTiltSpeedLevel; }
        }

        /// <summary>
        /// 연결 장비 세션 정보를 종료해 이전 장비 응답을 새 연결에 적용하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.EndConnectedControlAgentSession()
        {
            _features.DevicePowerFeature.EndConnectedControlAgentSession();
        }

        string IControlAgentFeatureContext.EnvironmentEoFocusStatusText
        {
            get { return _features.PropertiesFeature.EnvironmentEoFocusStatusText; }
        }

        string IControlAgentFeatureContext.EnvironmentEoZoomStatusText
        {
            get { return _features.PropertiesFeature.EnvironmentEoZoomStatusText; }
        }

        string IControlAgentFeatureContext.EnvironmentIrFocusStatusText
        {
            get { return _features.PropertiesFeature.EnvironmentIrFocusStatusText; }
        }

        string IControlAgentFeatureContext.EnvironmentIrZoomStatusText
        {
            get { return _features.PropertiesFeature.EnvironmentIrZoomStatusText; }
        }

        string IControlAgentFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        string IControlAgentFeatureContext.EoStatusText
        {
            get { return _features.PropertiesFeature.EoStatusText; }
            set { _features.PropertiesFeature.EoStatusText = value; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// ->; Standard 0(Far) Raw 0(Near) ->; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IControlAgentFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IControlAgentFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// Web Agent의 0/360도 경계를 고려한 Tilt 차이를 반환한다. ROOFTOP LA는 기존 선형 -90 ~ 90도 차이를 유지한다.
        /// 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        double IControlAgentFeatureContext.GetTiltDifference(double currentTilt, double targetTilt)
        {
            return _features.MoveControlFeature.GetTiltDifference(currentTilt, targetTilt);
        }

        /// <summary>
        /// 새 WEB AGENT 연결의 조회·상태 관리 세션을 초기화한다.
        /// </summary>
        void IControlAgentFeatureContext.InitializeWebAgentSession()
        {
            _features.WebAgentProtocolFeature.InitializeWebAgentSession();
        }

        string IControlAgentFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        string IControlAgentFeatureContext.IrStatusText
        {
            get { return _features.PropertiesFeature.IrStatusText; }
            set { _features.PropertiesFeature.IrStatusText = value; }
        }

        string IControlAgentFeatureContext.LrfDistanceText
        {
            get { return _features.PropertiesFeature.LrfDistanceText; }
            set { _features.PropertiesFeature.LrfDistanceText = value; }
        }

        /// <summary>
        /// 기존 형식의 상태 수신 시각을 기록해 오래된 상태와 구분한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.MarkLegacyWebAgentStatusReceived(ushort panRaw, ushort tiltRaw)
        {
            _features.WebAgentProtocolFeature.MarkLegacyWebAgentStatusReceived(panRaw, tiltRaw);
        }

        /// <summary>
        /// 채널별 렌즈 응답 시각을 원자적으로 기록한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.MarkLensEvidence(bool ir)
        {
            _features.InputCoordinatorFeature.MarkLensEvidence(ir);
        }

        /// <summary>
        /// 2026-10-14 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            _features.PresetLensFeature.MarkPresetLensEvidence(ir, zoom, focus);
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
            get { return _features.PropertiesFeature.PanTiltCoordinateRangeText; }
        }

        string IControlAgentFeatureContext.PanTiltSpeedDisplayText
        {
            get { return _features.PropertiesFeature.PanTiltSpeedDisplayText; }
        }

        /// <summary>
        /// WEB AGENT 텔레메트리의 명령·채널을 판별해 해당 상태 해석기로 전달한다.
        /// </summary>
        void IControlAgentFeatureContext.ParseWebAgentTelemetryPacket(LaResponsePacket packet)
        {
            _features.WebAgentProtocolFeature.ParseWebAgentTelemetryPacket(packet);
        }

        /// <summary>
        /// 입력 세대를 갱신해 대기 명령을 무효화하고 필요할 때 전체 PTZF를 정지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.ReleaseAllControlInputs(string reason)
        {
            _features.InputCoordinatorFeature.ReleaseAllControlInputs(reason);
        }

        /// <summary>
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.ResetDigitalZoomSession()
        {
            _features.CameraFeatureControlFeature.ResetDigitalZoomSession();
        }

        string IControlAgentFeatureContext.RooftopEoFocusStatusText
        {
            get { return _features.PropertiesFeature.RooftopEoFocusStatusText; }
        }

        string IControlAgentFeatureContext.RooftopEoZoomStatusText
        {
            get { return _features.PropertiesFeature.RooftopEoZoomStatusText; }
        }

        string IControlAgentFeatureContext.RooftopIrFocusStatusText
        {
            get { return _features.PropertiesFeature.RooftopIrFocusStatusText; }
        }

        string IControlAgentFeatureContext.RooftopIrZoomStatusText
        {
            get { return _features.PropertiesFeature.RooftopIrZoomStatusText; }
        }

        string IControlAgentFeatureContext.SessionControlIp
        {
            get { return _features.RtspSettingsFeature.SessionControlIp; }
        }

        string IControlAgentFeatureContext.SessionControlPort
        {
            get { return _features.RtspSettingsFeature.SessionControlPort; }
        }

        /// <summary>
        /// 위치 상태 저장소에 현재 장비의 연결 여부를 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.SetPositionConnectionState(bool connected)
        {
            _features.PositionFeature.SetPositionConnectionState(connected);
        }

        /// <summary>
        /// 연결 직후와 다른 탭에서도 5초마다 읽기 전용 F1 조회. SYSTEM TIME 탭은 기존 1Hz이며 응답 대기 중에는 중복 송신하지
        /// 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.StartSystemTimePollingAfterConnection()
        {
            _features.SystemDateTimeFeature.StartSystemTimePollingAfterConnection();
        }

        /// <summary>
        /// 시각 자동 조회를 멈추고 진행 중 조회 표시를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IControlAgentFeatureContext.StopSystemTimePolling(string reason)
        {
            _features.SystemDateTimeFeature.StopSystemTimePolling(reason);
        }

        string IControlAgentFeatureContext.TiltAbsoluteRangeLabel
        {
            get { return _features.PropertiesFeature.TiltAbsoluteRangeLabel; }
        }

        double? IControlAgentFeatureContext.TiltAbsoluteValue
        {
            get { return _features.PropertiesFeature.TiltAbsoluteValue; }
            set { _features.PropertiesFeature.TiltAbsoluteValue = value; }
        }

        /// <summary>
        /// IR 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        void IControlAgentFeatureContext.UpdatePositionIrLens(int irZoom, int irFocus)
        {
            _features.PositionFeature.UpdatePositionIrLens(irZoom, irFocus);
        }

        /// <summary>
        /// 수신한 PTZ와 EO 렌즈 값을 위치 상태에 반영한다.
        /// </summary>
        void IControlAgentFeatureContext.UpdatePositionPtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus)
        {
            _features.PositionFeature.UpdatePositionPtzAndEoLens(pan, tilt, eoZoom, eoFocus);
        }
    }
}
