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

        void IControlAgentFeatureContext.ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime, string sourceFunction)
        {
            _features._devicePowerFeature.ApplyDevicePowerStatus(mcbStatus, scbStatus, sourceTime, sourceFunction);
        }

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

        int IControlAgentFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        int IControlAgentFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        double IControlAgentFeatureContext.GetTiltDifference(double currentTilt, double targetTilt)
        {
            return _features._moveControlFeature.GetTiltDifference(currentTilt, targetTilt);
        }

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

        void IControlAgentFeatureContext.MarkLegacyWebAgentStatusReceived(ushort panRaw, ushort tiltRaw)
        {
            _features._webAgentProtocolFeature.MarkLegacyWebAgentStatusReceived(panRaw, tiltRaw);
        }

        void IControlAgentFeatureContext.MarkLensEvidence(bool ir)
        {
            _features._inputCoordinatorFeature.MarkLensEvidence(ir);
        }

        void IControlAgentFeatureContext.MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            _features._presetLensFeature.MarkPresetLensEvidence(ir, zoom, focus);
        }

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

        void IControlAgentFeatureContext.ParseWebAgentTelemetryPacket(LaResponsePacket packet)
        {
            _features._webAgentProtocolFeature.ParseWebAgentTelemetryPacket(packet);
        }

        void IControlAgentFeatureContext.ReleaseAllControlInputs(string reason)
        {
            _features._inputCoordinatorFeature.ReleaseAllControlInputs(reason);
        }

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

        void IControlAgentFeatureContext.SetPositionConnectionState(bool connected)
        {
            _features._positionFeature.SetPositionConnectionState(connected);
        }

        void IControlAgentFeatureContext.StartSystemTimePollingAfterConnection()
        {
            _features._systemDateTimeFeature.StartSystemTimePollingAfterConnection();
        }

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

        void IControlAgentFeatureContext.UpdatePositionIrLens(int irZoom, int irFocus)
        {
            _features._positionFeature.UpdatePositionIrLens(irZoom, irFocus);
        }

        void IControlAgentFeatureContext.UpdatePositionPtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus)
        {
            _features._positionFeature.UpdatePositionPtzAndEoLens(pan, tilt, eoZoom, eoFocus);
        }
    }
}
