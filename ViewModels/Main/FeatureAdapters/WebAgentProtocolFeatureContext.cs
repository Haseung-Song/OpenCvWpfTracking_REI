using OpenCvWpfTracking.Services.Communication;
using System;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class WebAgentProtocolFeatureContext : IWebAgentProtocolFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal WebAgentProtocolFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? IWebAgentProtocolFeatureContext._activePanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activePanAbsoluteTarget; }
        }

        KeyboardPanTiltDirection IWebAgentProtocolFeatureContext._activePanTiltMoveDirection
        {
            get { return _features._screenStateFeature._activePanTiltMoveDirection; }
            set { _features._screenStateFeature._activePanTiltMoveDirection = value; }
        }

        ref double? IWebAgentProtocolFeatureContext._activeTiltAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activeTiltAbsoluteTarget; }
        }

        string IWebAgentProtocolFeatureContext._connectionSessionId
        {
            get { return _features._devicePowerFeature._connectionSessionId; }
            set { _features._devicePowerFeature._connectionSessionId = value; }
        }

        IControlCommandServicePort IWebAgentProtocolFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        short IWebAgentProtocolFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IWebAgentProtocolFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IWebAgentProtocolFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort IWebAgentProtocolFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        double IWebAgentProtocolFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        double IWebAgentProtocolFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        ref long IWebAgentProtocolFeatureContext._eoLensStatusVersion
        {
            get { return ref _features._screenStateFeature._eoLensStatusVersion; }
        }

        string IWebAgentProtocolFeatureContext._irFocusDirectUnavailableSession
        {
            get { return _features._syncFeature._irFocusDirectUnavailableSession; }
            set { _features._syncFeature._irFocusDirectUnavailableSession = value; }
        }

        ref long IWebAgentProtocolFeatureContext._irLensStatusVersion
        {
            get { return ref _features._screenStateFeature._irLensStatusVersion; }
        }

        ref double? IWebAgentProtocolFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        ref long IWebAgentProtocolFeatureContext._panStartTxCount
        {
            get { return ref _features._screenStateFeature._panStartTxCount; }
        }

        ref long IWebAgentProtocolFeatureContext._panTiltStatusVersion
        {
            get { return ref _features._screenStateFeature._panTiltStatusVersion; }
        }

        Services.Position.PositionSnapshotService IWebAgentProtocolFeatureContext._positionSnapshotService
        {
            get { return _features._positionFeature._positionSnapshotService; }
        }

        string IWebAgentProtocolFeatureContext._ptzPerformanceScenario
        {
            get { return _features._screenStateFeature._ptzPerformanceScenario; }
            set { _features._screenStateFeature._ptzPerformanceScenario = value; }
        }

        ref long IWebAgentProtocolFeatureContext._tiltStartTxCount
        {
            get { return ref _features._screenStateFeature._tiltStartTxCount; }
        }

        ref global::System.Tuple<double, double, long> IWebAgentProtocolFeatureContext._videoCenterPose
        {
            get { return ref _features._cameraFeatureControlFeature._videoCenterPose; }
        }

        void IWebAgentProtocolFeatureContext.ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime, string sourceFunction)
        {
            _features._devicePowerFeature.ApplyDevicePowerStatus(mcbStatus, scbStatus, sourceTime, sourceFunction);
        }

        void IWebAgentProtocolFeatureContext.ApplyDigitalZoomCapability(byte[] p)
        {
            _features._cameraFeatureControlFeature.ApplyDigitalZoomCapability(p);
        }

        void IWebAgentProtocolFeatureContext.ApplyDigitalZoomState(byte[] p)
        {
            _features._cameraFeatureControlFeature.ApplyDigitalZoomState(p);
        }

        void IWebAgentProtocolFeatureContext.ApplyDigitalZoomUnavailable(int target)
        {
            _features._cameraFeatureControlFeature.ApplyDigitalZoomUnavailable(target);
        }

        void IWebAgentProtocolFeatureContext.ApplySpecialFeatureCapability(byte[] p)
        {
            _features._specialFeaturesFeature.ApplySpecialFeatureCapability(p);
        }

        void IWebAgentProtocolFeatureContext.ApplySpecialFeatureState(byte[] p)
        {
            _features._specialFeaturesFeature.ApplySpecialFeatureState(p);
        }

        void IWebAgentProtocolFeatureContext.ApplySpecialFeatureUnavailable(byte[] p)
        {
            _features._specialFeaturesFeature.ApplySpecialFeatureUnavailable(p);
        }

        void IWebAgentProtocolFeatureContext.BeginWebAgentBuildCheck()
        {
            _features._rpySettingsFeature.BeginWebAgentBuildCheck();
        }

        bool IWebAgentProtocolFeatureContext.CameraFeatureTransportReady
        {
            get { return _features._cameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        void IWebAgentProtocolFeatureContext.DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply)
        {
            _features._specialFeaturesFeature.DispatchSpecialFeaturePacket(p, apply);
        }

        double IWebAgentProtocolFeatureContext.GetTiltDifference(double currentTilt, double targetTilt)
        {
            return _features._moveControlFeature.GetTiltDifference(currentTilt, targetTilt);
        }

        void IWebAgentProtocolFeatureContext.HandleGpsUnavailable(byte[] payload)
        {
            _features._gpsSettingsFeature.HandleGpsUnavailable(payload);
        }

        void IWebAgentProtocolFeatureContext.HandlePrecisionUnavailable(byte[] payload)
        {
            _features._webAgentMotionFocusFeature.HandlePrecisionUnavailable(payload);
        }

        void IWebAgentProtocolFeatureContext.HandleWebAgentEquipmentUnavailable(byte[] payload)
        {
            _features._webAgentEquipmentFeature.HandleWebAgentEquipmentUnavailable(payload);
        }

        bool IWebAgentProtocolFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        void IWebAgentProtocolFeatureContext.MarkLensEvidence(bool ir)
        {
            _features._inputCoordinatorFeature.MarkLensEvidence(ir);
        }

        void IWebAgentProtocolFeatureContext.MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            _features._presetLensFeature.MarkPresetLensEvidence(ir, zoom, focus);
        }

        void IWebAgentProtocolFeatureContext.NotifyEoCurrentStatusChanged(bool panChanged, bool tiltChanged, bool zoomChanged, bool focusChanged, bool powerChanged)
        {
            _features._controlAgentFeature.NotifyEoCurrentStatusChanged(panChanged, tiltChanged, zoomChanged, focusChanged, powerChanged);
        }

        void IWebAgentProtocolFeatureContext.NotifyIrCurrentStatusChanged()
        {
            _features._controlAgentFeature.NotifyIrCurrentStatusChanged();
        }

        void IWebAgentProtocolFeatureContext.ParseAutoFocusResponse(byte[] payload)
        {
            _features._webAgentMotionFocusFeature.ParseAutoFocusResponse(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseEquipmentTemperature(byte[] payload)
        {
            _features._webAgentEquipmentFeature.ParseEquipmentTemperature(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseGpsAutoTxRateResponse(byte[] payload)
        {
            _features._gpsSettingsFeature.ParseGpsAutoTxRateResponse(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseStoredRpyOffsetResponse(byte[] payload)
        {
            _features._rpySettingsFeature.ParseStoredRpyOffsetResponse(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseStoredRpyRateResponse(byte[] payload)
        {
            _features._rpySettingsFeature.ParseStoredRpyRateResponse(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseWebAgentGps(byte[] payload)
        {
            _features._gpsSettingsFeature.ParseWebAgentGps(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseWebAgentRpyOffsetAck(byte[] payload)
        {
            _features._rpySettingsFeature.ParseWebAgentRpyOffsetAck(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseWebAgentRpyRateAck(byte[] payload)
        {
            _features._rpySettingsFeature.ParseWebAgentRpyRateAck(payload);
        }

        void IWebAgentProtocolFeatureContext.ParseWebAgentSystemDateTime(byte[] payload)
        {
            _features._systemDateTimeFeature.ParseWebAgentSystemDateTime(payload);
        }

        void IWebAgentProtocolFeatureContext.QueryEquipmentTemperature()
        {
            _features._webAgentEquipmentFeature.QueryEquipmentTemperature();
        }

        void IWebAgentProtocolFeatureContext.QueuePositionStatusNotification(bool includeLastSnapshot)
        {
            _features._positionFeature.QueuePositionStatusNotification(includeLastSnapshot);
        }

        void IWebAgentProtocolFeatureContext.RecordRpyTelemetry(double roll, double pitch, double yaw)
        {
            _features._rpySettingsFeature.RecordRpyTelemetry(roll, pitch, yaw);
        }

        void IWebAgentProtocolFeatureContext.RequestGpsAutoTxRateAfterConnection()
        {
            _features._gpsSettingsFeature.RequestGpsAutoTxRateAfterConnection();
        }

        void IWebAgentProtocolFeatureContext.ResetDigitalZoomSession()
        {
            _features._cameraFeatureControlFeature.ResetDigitalZoomSession();
        }

        void IWebAgentProtocolFeatureContext.ResetWebAgentControlSession()
        {
            _features._webAgentMotionFocusFeature.ResetWebAgentControlSession();
        }

        void IWebAgentProtocolFeatureContext.ResetWebAgentEquipmentState()
        {
            _features._webAgentEquipmentFeature.ResetWebAgentEquipmentState();
        }

        void IWebAgentProtocolFeatureContext.UpdateGpsImuRawReference(double yaw)
        {
            _features._gpsSettingsFeature.UpdateGpsImuRawReference(yaw);
        }

        void IWebAgentProtocolFeatureContext.UpdatePositionEoLens(int eoZoom, int eoFocus)
        {
            _features._positionFeature.UpdatePositionEoLens(eoZoom, eoFocus);
        }

        void IWebAgentProtocolFeatureContext.UpdatePositionImu(double roll, double pitch, double yaw, bool valid)
        {
            _features._positionFeature.UpdatePositionImu(roll, pitch, yaw, valid);
        }

        void IWebAgentProtocolFeatureContext.UpdatePositionIrLens(int irZoom, int irFocus)
        {
            _features._positionFeature.UpdatePositionIrLens(irZoom, irFocus);
        }

        void IWebAgentProtocolFeatureContext.UpdateWebAgentBuild(string buildText)
        {
            _features._rpySettingsFeature.UpdateWebAgentBuild(buildText);
        }

        Action<byte[]> IWebAgentProtocolFeatureContext.ApplySpecialFeatureCapabilityCallback
        {
            get { return _features.Callbacks.ApplySpecialFeatureCapabilityCallback; }
        }

        Action<byte[]> IWebAgentProtocolFeatureContext.ApplySpecialFeatureStateCallback
        {
            get { return _features.Callbacks.ApplySpecialFeatureStateCallback; }
        }

        Action<byte[]> IWebAgentProtocolFeatureContext.ApplySpecialFeatureUnavailableCallback
        {
            get { return _features.Callbacks.ApplySpecialFeatureUnavailableCallback; }
        }
    }
}
