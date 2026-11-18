using OpenCvWpfTracking.Models.Main;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class PropertiesFeatureContext : IPropertiesFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PropertiesFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        int IPropertiesFeatureContext._activePanTiltZoomSource
        {
            get { return _features._continuousControlFeature._activePanTiltZoomSource; }
            set { _features._continuousControlFeature._activePanTiltZoomSource = value; }
        }

        int IPropertiesFeatureContext._aiAgentPort
        {
            get { return _features._screenStateFeature._aiAgentPort; }
            set { _features._screenStateFeature._aiAgentPort = value; }
        }

        string IPropertiesFeatureContext._aiControlAgentIp
        {
            get { return _features._screenStateFeature._aiControlAgentIp; }
            set { _features._screenStateFeature._aiControlAgentIp = value; }
        }

        double IPropertiesFeatureContext._aiDisplayConfidenceThreshold
        {
            get { return _features._screenStateFeature._aiDisplayConfidenceThreshold; }
            set { _features._screenStateFeature._aiDisplayConfidenceThreshold = value; }
        }

        double IPropertiesFeatureContext._aiMappingConfidence
        {
            get { return _features._screenStateFeature._aiMappingConfidence; }
            set { _features._screenStateFeature._aiMappingConfidence = value; }
        }

        double IPropertiesFeatureContext._aiMappingIou
        {
            get { return _features._screenStateFeature._aiMappingIou; }
            set { _features._screenStateFeature._aiMappingIou = value; }
        }

        string IPropertiesFeatureContext._aiPowerStatusText
        {
            get { return _features._screenStateFeature._aiPowerStatusText; }
            set { _features._screenStateFeature._aiPowerStatusText = value; }
        }

        string IPropertiesFeatureContext._aiRtsp0Address
        {
            get { return _features._screenStateFeature._aiRtsp0Address; }
            set { _features._screenStateFeature._aiRtsp0Address = value; }
        }

        int IPropertiesFeatureContext._aiRtsp0OnnxIndex
        {
            get { return _features._screenStateFeature._aiRtsp0OnnxIndex; }
            set { _features._screenStateFeature._aiRtsp0OnnxIndex = value; }
        }

        string IPropertiesFeatureContext._aiRtsp1Address
        {
            get { return _features._screenStateFeature._aiRtsp1Address; }
            set { _features._screenStateFeature._aiRtsp1Address = value; }
        }

        int IPropertiesFeatureContext._aiRtsp1OnnxIndex
        {
            get { return _features._screenStateFeature._aiRtsp1OnnxIndex; }
            set { _features._screenStateFeature._aiRtsp1OnnxIndex = value; }
        }

        string IPropertiesFeatureContext._aiSettingStatusText
        {
            get { return _features._screenStateFeature._aiSettingStatusText; }
            set { _features._screenStateFeature._aiSettingStatusText = value; }
        }

        string IPropertiesFeatureContext._controlAgentConnectionStatusColor
        {
            get { return _features._screenStateFeature._controlAgentConnectionStatusColor; }
            set { _features._screenStateFeature._controlAgentConnectionStatusColor = value; }
        }

        string IPropertiesFeatureContext._controlAgentConnectionStatusText
        {
            get { return _features._screenStateFeature._controlAgentConnectionStatusText; }
            set { _features._screenStateFeature._controlAgentConnectionStatusText = value; }
        }

        Services.Communication.IControlCommandServicePort IPropertiesFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        string IPropertiesFeatureContext._controlControlAgentIp
        {
            get { return _features._screenStateFeature._controlControlAgentIp; }
            set { _features._screenStateFeature._controlControlAgentIp = value; }
        }

        string IPropertiesFeatureContext._controlControlAgentPortText
        {
            get { return _features._screenStateFeature._controlControlAgentPortText; }
            set { _features._screenStateFeature._controlControlAgentPortText = value; }
        }

        Brush IPropertiesFeatureContext._crosshairBrush
        {
            get { return _features._screenStateFeature._crosshairBrush; }
            set { _features._screenStateFeature._crosshairBrush = value; }
        }

        byte IPropertiesFeatureContext._currentMcbPowerStatus
        {
            get { return _features._devicePowerFeature._currentMcbPowerStatus; }
            set { _features._devicePowerFeature._currentMcbPowerStatus = value; }
        }

        double IPropertiesFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        byte IPropertiesFeatureContext._currentPowerStatus
        {
            get { return _features._screenStateFeature._currentPowerStatus; }
            set { _features._screenStateFeature._currentPowerStatus = value; }
        }

        byte IPropertiesFeatureContext._currentScbPowerStatus
        {
            get { return _features._devicePowerFeature._currentScbPowerStatus; }
            set { _features._devicePowerFeature._currentScbPowerStatus = value; }
        }

        double IPropertiesFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        bool IPropertiesFeatureContext._eoAlreadyConnectedNotice
        {
            get { return _features._rtspSettingsFeature._eoAlreadyConnectedNotice; }
            set { _features._rtspSettingsFeature._eoAlreadyConnectedNotice = value; }
        }

        BitmapSource IPropertiesFeatureContext._eoCameraImage
        {
            get { return _features._screenStateFeature._eoCameraImage; }
            set { _features._screenStateFeature._eoCameraImage = value; }
        }

        string IPropertiesFeatureContext._eoSourceAddress
        {
            get { return _features._screenStateFeature._eoSourceAddress; }
            set { _features._screenStateFeature._eoSourceAddress = value; }
        }

        string IPropertiesFeatureContext._eoStatusText
        {
            get { return _features._screenStateFeature._eoStatusText; }
            set { _features._screenStateFeature._eoStatusText = value; }
        }

        int IPropertiesFeatureContext._eoVideoHeight
        {
            get { return _features._screenStateFeature._eoVideoHeight; }
            set { _features._screenStateFeature._eoVideoHeight = value; }
        }

        int IPropertiesFeatureContext._eoVideoWidth
        {
            get { return _features._screenStateFeature._eoVideoWidth; }
            set { _features._screenStateFeature._eoVideoWidth = value; }
        }

        ref int? IPropertiesFeatureContext._focusPositionValue
        {
            get { return ref _features._screenStateFeature._focusPositionValue; }
        }

        string IPropertiesFeatureContext._focusSyncStatusText
        {
            get { return _features._screenStateFeature._focusSyncStatusText; }
            set { _features._screenStateFeature._focusSyncStatusText = value; }
        }

        bool IPropertiesFeatureContext._hasDevicePowerStatus
        {
            get { return _features._devicePowerFeature._hasDevicePowerStatus; }
            set { _features._devicePowerFeature._hasDevicePowerStatus = value; }
        }

        string IPropertiesFeatureContext._homeZeroLockMessage
        {
            get { return _features._screenStateFeature._homeZeroLockMessage; }
            set { _features._screenStateFeature._homeZeroLockMessage = value; }
        }

        string IPropertiesFeatureContext._homeZeroLockTitle
        {
            get { return _features._screenStateFeature._homeZeroLockTitle; }
            set { _features._screenStateFeature._homeZeroLockTitle = value; }
        }

        string IPropertiesFeatureContext._homeZeroStatusText
        {
            get { return _features._screenStateFeature._homeZeroStatusText; }
            set { _features._screenStateFeature._homeZeroStatusText = value; }
        }

        bool IPropertiesFeatureContext._irAlreadyConnectedNotice
        {
            get { return _features._rtspSettingsFeature._irAlreadyConnectedNotice; }
            set { _features._rtspSettingsFeature._irAlreadyConnectedNotice = value; }
        }

        BitmapSource IPropertiesFeatureContext._irCameraImage
        {
            get { return _features._screenStateFeature._irCameraImage; }
            set { _features._screenStateFeature._irCameraImage = value; }
        }

        string IPropertiesFeatureContext._irSourceAddress
        {
            get { return _features._screenStateFeature._irSourceAddress; }
            set { _features._screenStateFeature._irSourceAddress = value; }
        }

        string IPropertiesFeatureContext._irStatusText
        {
            get { return _features._screenStateFeature._irStatusText; }
            set { _features._screenStateFeature._irStatusText = value; }
        }

        int IPropertiesFeatureContext._irVideoHeight
        {
            get { return _features._screenStateFeature._irVideoHeight; }
            set { _features._screenStateFeature._irVideoHeight = value; }
        }

        int IPropertiesFeatureContext._irVideoWidth
        {
            get { return _features._screenStateFeature._irVideoWidth; }
            set { _features._screenStateFeature._irVideoWidth = value; }
        }

        bool IPropertiesFeatureContext._isAutoTrackingEnabled
        {
            get { return _features._screenStateFeature._isAutoTrackingEnabled; }
            set { _features._screenStateFeature._isAutoTrackingEnabled = value; }
        }

        bool IPropertiesFeatureContext._isCrosshairVisible
        {
            get { return _features._screenStateFeature._isCrosshairVisible; }
            set { _features._screenStateFeature._isCrosshairVisible = value; }
        }

        bool IPropertiesFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        bool IPropertiesFeatureContext._isDevicePowerStatusStale
        {
            get { return _features._devicePowerFeature._isDevicePowerStatusStale; }
            set { _features._devicePowerFeature._isDevicePowerStatusStale = value; }
        }

        bool IPropertiesFeatureContext._isHomePositionMoving
        {
            get { return _features._screenStateFeature._isHomePositionMoving; }
            set { _features._screenStateFeature._isHomePositionMoving = value; }
        }

        bool IPropertiesFeatureContext._isLaPresetScanRunning
        {
            get { return _features._screenStateFeature._isLaPresetScanRunning; }
            set { _features._screenStateFeature._isLaPresetScanRunning = value; }
        }

        bool IPropertiesFeatureContext._isPresetScanRunning
        {
            get { return _features._screenStateFeature._isPresetScanRunning; }
            set { _features._screenStateFeature._isPresetScanRunning = value; }
        }

        string IPropertiesFeatureContext._laPresetCommandStatusText
        {
            get { return _features._screenStateFeature._laPresetCommandStatusText; }
            set { _features._screenStateFeature._laPresetCommandStatusText = value; }
        }

        int IPropertiesFeatureContext._laPresetScanDelay
        {
            get { return _features._screenStateFeature._laPresetScanDelay; }
            set { _features._screenStateFeature._laPresetScanDelay = value; }
        }

        int IPropertiesFeatureContext._laPresetScanSpeed
        {
            get { return _features._screenStateFeature._laPresetScanSpeed; }
            set { _features._screenStateFeature._laPresetScanSpeed = value; }
        }

        int IPropertiesFeatureContext._laPresetSlotNumber
        {
            get { return _features._screenStateFeature._laPresetSlotNumber; }
            set { _features._screenStateFeature._laPresetSlotNumber = value; }
        }

        string IPropertiesFeatureContext._lrfDistanceText
        {
            get { return _features._screenStateFeature._lrfDistanceText; }
            set { _features._screenStateFeature._lrfDistanceText = value; }
        }

        string IPropertiesFeatureContext._mcbMaintenanceIpAddress
        {
            get { return _features._screenStateFeature._mcbMaintenanceIpAddress; }
            set { _features._screenStateFeature._mcbMaintenanceIpAddress = value; }
        }

        ref double? IPropertiesFeatureContext._panAbsoluteValue
        {
            get { return ref _features._screenStateFeature._panAbsoluteValue; }
        }

        byte IPropertiesFeatureContext._panTiltSpeedLevel
        {
            get { return _features._screenStateFeature._panTiltSpeedLevel; }
            set { _features._screenStateFeature._panTiltSpeedLevel = value; }
        }

        PanTurnMode IPropertiesFeatureContext._panTurnMode
        {
            get { return _features._screenStateFeature._panTurnMode; }
            set { _features._screenStateFeature._panTurnMode = value; }
        }

        bool IPropertiesFeatureContext._precisionActive
        {
            get { return _features._webAgentMotionFocusFeature._precisionActive; }
            set { _features._webAgentMotionFocusFeature._precisionActive = value; }
        }

        bool IPropertiesFeatureContext._precisionStopUncertain
        {
            get { return _features._webAgentMotionFocusFeature._precisionStopUncertain; }
            set { _features._webAgentMotionFocusFeature._precisionStopUncertain = value; }
        }

        string IPropertiesFeatureContext._presetCommandStatusText
        {
            get { return _features._screenStateFeature._presetCommandStatusText; }
            set { _features._screenStateFeature._presetCommandStatusText = value; }
        }

        int IPropertiesFeatureContext._presetScanDelay
        {
            get { return _features._screenStateFeature._presetScanDelay; }
            set { _features._screenStateFeature._presetScanDelay = value; }
        }

        PresetScanOrderMode IPropertiesFeatureContext._presetScanOrderMode
        {
            get { return _features._screenStateFeature._presetScanOrderMode; }
            set { _features._screenStateFeature._presetScanOrderMode = value; }
        }

        int IPropertiesFeatureContext._presetScanSpeed
        {
            get { return _features._screenStateFeature._presetScanSpeed; }
            set { _features._screenStateFeature._presetScanSpeed = value; }
        }

        int IPropertiesFeatureContext._presetSlotNumber
        {
            get { return _features._screenStateFeature._presetSlotNumber; }
            set { _features._screenStateFeature._presetSlotNumber = value; }
        }

        ControlAgentProfileOption IPropertiesFeatureContext._selectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature._selectedControlAgentProfile; }
            set { _features._rtspSettingsFeature._selectedControlAgentProfile = value; }
        }

        EquipmentStatusMode IPropertiesFeatureContext._selectedEquipmentStatusMode
        {
            get { return _features._screenStateFeature._selectedEquipmentStatusMode; }
            set { _features._screenStateFeature._selectedEquipmentStatusMode = value; }
        }

        ZoomSyncLevelOption IPropertiesFeatureContext._selectedFocusSyncLevel
        {
            get { return _features._screenStateFeature._selectedFocusSyncLevel; }
            set { _features._screenStateFeature._selectedFocusSyncLevel = value; }
        }

        PresetPointOption IPropertiesFeatureContext._selectedLaPresetPoint
        {
            get { return _features._screenStateFeature._selectedLaPresetPoint; }
            set { _features._screenStateFeature._selectedLaPresetPoint = value; }
        }

        int IPropertiesFeatureContext._selectedOperationControlTabIndex
        {
            get { return _features._screenStateFeature._selectedOperationControlTabIndex; }
            set { _features._screenStateFeature._selectedOperationControlTabIndex = value; }
        }

        PresetPointOption IPropertiesFeatureContext._selectedPresetPoint
        {
            get { return _features._screenStateFeature._selectedPresetPoint; }
            set { _features._screenStateFeature._selectedPresetPoint = value; }
        }

        int IPropertiesFeatureContext._selectedRightPanelTabIndex
        {
            get { return _features._screenStateFeature._selectedRightPanelTabIndex; }
            set { _features._screenStateFeature._selectedRightPanelTabIndex = value; }
        }

        ZoomSyncLevelOption IPropertiesFeatureContext._selectedZoomSyncLevel
        {
            get { return _features._screenStateFeature._selectedZoomSyncLevel; }
            set { _features._screenStateFeature._selectedZoomSyncLevel = value; }
        }

        ref double? IPropertiesFeatureContext._tiltAbsoluteValue
        {
            get { return ref _features._screenStateFeature._tiltAbsoluteValue; }
        }

        ref int? IPropertiesFeatureContext._zoomPositionValue
        {
            get { return ref _features._screenStateFeature._zoomPositionValue; }
        }

        ref double? IPropertiesFeatureContext._zoomRatioValue
        {
            get { return ref _features._screenStateFeature._zoomRatioValue; }
        }

        string IPropertiesFeatureContext._zoomSyncStatusText
        {
            get { return _features._screenStateFeature._zoomSyncStatusText; }
            set { _features._screenStateFeature._zoomSyncStatusText = value; }
        }

        string IPropertiesFeatureContext.AiAlertStatusText
        {
            get { return _features._eventAlertsFeature.AiAlertStatusText; }
        }

        void IPropertiesFeatureContext.ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
            _features._rtspSettingsFeature.ApplyControlAgentPanCoordinateMode(profile);
        }

        void IPropertiesFeatureContext.ApplyPanTiltSpeedWhileMoving()
        {
            _features._continuousControlFeature.ApplyPanTiltSpeedWhileMoving();
        }

        bool IPropertiesFeatureContext.ApplySelectedPanTurnMode()
        {
            return _features._moveControlFeature.ApplySelectedPanTurnMode();
        }

        byte IPropertiesFeatureContext.ApplyZoomAdaptivePanTiltSpeed(byte baseProtocolSpeed, out int standardZoom, out string activeCamera, out double opticalZoomRatio, out double speedScale)
        {
            return _features._continuousControlFeature.ApplyZoomAdaptivePanTiltSpeed(baseProtocolSpeed, out standardZoom, out activeCamera, out opticalZoomRatio, out speedScale);
        }

        byte IPropertiesFeatureContext.ConvertPanTiltSpeedLevel(int uiSpeed)
        {
            return _features._continuousControlFeature.ConvertPanTiltSpeedLevel(uiSpeed);
        }

        string IPropertiesFeatureContext.CurrentEoPowerStatusColor
        {
            get { return _features._devicePowerFeature.CurrentEoPowerStatusColor; }
        }

        string IPropertiesFeatureContext.CurrentIrPowerStatusColor
        {
            get { return _features._devicePowerFeature.CurrentIrPowerStatusColor; }
        }

        double IPropertiesFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double IPropertiesFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        string IPropertiesFeatureContext.DeviceControlConnectionStatusText
        {
            get { return _features._devicePowerFeature.DeviceControlConnectionStatusText; }
        }

        void IPropertiesFeatureContext.EnsureVisibleCommunicationSettingsTab()
        {
            _features._rtspSettingsFeature.EnsureVisibleCommunicationSettingsTab();
        }

        string IPropertiesFeatureContext.EoPowerControlStatusText
        {
            get { return _features._devicePowerFeature.EoPowerControlStatusText; }
        }

        int IPropertiesFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        int IPropertiesFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort IPropertiesFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardFocus();
        }

        ushort IPropertiesFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        string IPropertiesFeatureContext.IrPowerControlStatusText
        {
            get { return _features._devicePowerFeature.IrPowerControlStatusText; }
        }

        bool IPropertiesFeatureContext.IsGpsTabVisible
        {
            get { return _features._rtspSettingsFeature.IsGpsTabVisible; }
        }

        bool IPropertiesFeatureContext.IsImuRpyTabVisible
        {
            get { return _features._rtspSettingsFeature.IsImuRpyTabVisible; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCancellationCompleted
        {
            get { return _features._panoramaFeature.IsPanoramaCancellationCompleted; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCancellationRequested
        {
            get { return _features._panoramaFeature.IsPanoramaCancellationRequested; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCaptureRunning
        {
            get { return _features._panoramaFeature.IsPanoramaCaptureRunning; }
            set { _features._panoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCompleted
        {
            get { return _features._panoramaFeature.IsPanoramaCompleted; }
        }

        bool IPropertiesFeatureContext.IsPanoramaPresetRunning
        {
            get { return _features._panoramaPresetsFeature.IsPanoramaPresetRunning; }
        }

        bool IPropertiesFeatureContext.IsPanoramaProcessingRunning
        {
            get { return _features._panoramaFeature.IsPanoramaProcessingRunning; }
        }

        bool IPropertiesFeatureContext.IsRouteRunning
        {
            get { return _features._routesFeature.IsRouteRunning; }
        }

        bool IPropertiesFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features._rtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        bool IPropertiesFeatureContext.IsSystemDateTimeTabVisible
        {
            get { return _features._rtspSettingsFeature.IsSystemDateTimeTabVisible; }
        }

        void IPropertiesFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        int IPropertiesFeatureContext.PresetIrZoomPosition(PresetPointOption preset)
        {
            return _features._presetLensFeature.PresetIrZoomPosition(preset);
        }

        void IPropertiesFeatureContext.QuerySpecialFeatureSupport()
        {
            _features._specialFeaturesFeature.QuerySpecialFeatureSupport();
        }

        void IPropertiesFeatureContext.RememberCameraPower(string camera, bool on)
        {
            _features._devicePowerFeature.RememberCameraPower(camera, on);
        }

        bool IPropertiesFeatureContext.SavePresetStorage()
        {
            return _features._presetStorageFeature.SavePresetStorage();
        }

        void IPropertiesFeatureContext.SaveRtspCommunicationSettings()
        {
            _features._rtspSettingsFeature.SaveRtspCommunicationSettings();
        }

        ControlAgentProfileOption IPropertiesFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        int IPropertiesFeatureContext.SelectedEventAlertTabIndex
        {
            get { return _features._eventAlertsFeature.SelectedEventAlertTabIndex; }
            set { _features._eventAlertsFeature.SelectedEventAlertTabIndex = value; }
        }

        void IPropertiesFeatureContext.ZoneChannelChanged(string camera)
        {
            _features._zonesFeature.ZoneChannelChanged(camera);
        }

        string IPropertiesFeatureContext.ZoomRatioStatus(bool ir)
        {
            return _features._inputCoordinatorFeature.ZoomRatioStatus(ir);
        }
    }
}
