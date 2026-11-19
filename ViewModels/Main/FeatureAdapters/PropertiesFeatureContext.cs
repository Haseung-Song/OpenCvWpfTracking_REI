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

        int IPropertiesFeatureContext.ActivePanTiltZoomSource
        {
            get { return _features.ContinuousControlFeature._activePanTiltZoomSource; }
            set { _features.ContinuousControlFeature._activePanTiltZoomSource = value; }
        }

        int IPropertiesFeatureContext.AiAgentPort
        {
            get { return _features.ScreenStateFeature._aiAgentPort; }
            set { _features.ScreenStateFeature._aiAgentPort = value; }
        }

        string IPropertiesFeatureContext.AiControlAgentIpState
        {
            get { return _features.ScreenStateFeature._aiControlAgentIp; }
            set { _features.ScreenStateFeature._aiControlAgentIp = value; }
        }

        double IPropertiesFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features.ScreenStateFeature._aiDisplayConfidenceThreshold; }
            set { _features.ScreenStateFeature._aiDisplayConfidenceThreshold = value; }
        }

        double IPropertiesFeatureContext.AiMappingConfidence
        {
            get { return _features.ScreenStateFeature._aiMappingConfidence; }
            set { _features.ScreenStateFeature._aiMappingConfidence = value; }
        }

        double IPropertiesFeatureContext.AiMappingIou
        {
            get { return _features.ScreenStateFeature._aiMappingIou; }
            set { _features.ScreenStateFeature._aiMappingIou = value; }
        }

        string IPropertiesFeatureContext.AiPowerStatusText
        {
            get { return _features.ScreenStateFeature._aiPowerStatusText; }
            set { _features.ScreenStateFeature._aiPowerStatusText = value; }
        }

        string IPropertiesFeatureContext.AiRtsp0Address
        {
            get { return _features.ScreenStateFeature._aiRtsp0Address; }
            set { _features.ScreenStateFeature._aiRtsp0Address = value; }
        }

        int IPropertiesFeatureContext.AiRtsp0OnnxIndex
        {
            get { return _features.ScreenStateFeature._aiRtsp0OnnxIndex; }
            set { _features.ScreenStateFeature._aiRtsp0OnnxIndex = value; }
        }

        string IPropertiesFeatureContext.AiRtsp1Address
        {
            get { return _features.ScreenStateFeature._aiRtsp1Address; }
            set { _features.ScreenStateFeature._aiRtsp1Address = value; }
        }

        int IPropertiesFeatureContext.AiRtsp1OnnxIndex
        {
            get { return _features.ScreenStateFeature._aiRtsp1OnnxIndex; }
            set { _features.ScreenStateFeature._aiRtsp1OnnxIndex = value; }
        }

        string IPropertiesFeatureContext.AiSettingStatusText
        {
            get { return _features.ScreenStateFeature._aiSettingStatusText; }
            set { _features.ScreenStateFeature._aiSettingStatusText = value; }
        }

        string IPropertiesFeatureContext.ControlAgentConnectionStatusColor
        {
            get { return _features.ScreenStateFeature._controlAgentConnectionStatusColor; }
            set { _features.ScreenStateFeature._controlAgentConnectionStatusColor = value; }
        }

        string IPropertiesFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features.ScreenStateFeature._controlAgentConnectionStatusText; }
            set { _features.ScreenStateFeature._controlAgentConnectionStatusText = value; }
        }

        Services.Communication.IControlCommandServicePort IPropertiesFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        string IPropertiesFeatureContext.ControlControlAgentIp
        {
            get { return _features.ScreenStateFeature._controlControlAgentIp; }
            set { _features.ScreenStateFeature._controlControlAgentIp = value; }
        }

        string IPropertiesFeatureContext.ControlControlAgentPortText
        {
            get { return _features.ScreenStateFeature._controlControlAgentPortText; }
            set { _features.ScreenStateFeature._controlControlAgentPortText = value; }
        }

        Brush IPropertiesFeatureContext.CrosshairBrush
        {
            get { return _features.ScreenStateFeature._crosshairBrush; }
            set { _features.ScreenStateFeature._crosshairBrush = value; }
        }

        byte IPropertiesFeatureContext.CurrentMcbPowerStatus
        {
            get { return _features.DevicePowerFeature._currentMcbPowerStatus; }
            set { _features.DevicePowerFeature._currentMcbPowerStatus = value; }
        }

        double IPropertiesFeatureContext.CurrentPan
        {
            get { return _features.ScreenStateFeature._currentPan; }
            set { _features.ScreenStateFeature._currentPan = value; }
        }

        byte IPropertiesFeatureContext.CurrentPowerStatus
        {
            get { return _features.ScreenStateFeature._currentPowerStatus; }
            set { _features.ScreenStateFeature._currentPowerStatus = value; }
        }

        byte IPropertiesFeatureContext.CurrentScbPowerStatus
        {
            get { return _features.DevicePowerFeature._currentScbPowerStatus; }
            set { _features.DevicePowerFeature._currentScbPowerStatus = value; }
        }

        double IPropertiesFeatureContext.CurrentTilt
        {
            get { return _features.ScreenStateFeature._currentTilt; }
            set { _features.ScreenStateFeature._currentTilt = value; }
        }

        bool IPropertiesFeatureContext.EoAlreadyConnectedNotice
        {
            get { return _features.RtspSettingsFeature._eoAlreadyConnectedNotice; }
            set { _features.RtspSettingsFeature._eoAlreadyConnectedNotice = value; }
        }

        BitmapSource IPropertiesFeatureContext.EoCameraImage
        {
            get { return _features.ScreenStateFeature._eoCameraImage; }
            set { _features.ScreenStateFeature._eoCameraImage = value; }
        }

        string IPropertiesFeatureContext.EoSourceAddress
        {
            get { return _features.ScreenStateFeature._eoSourceAddress; }
            set { _features.ScreenStateFeature._eoSourceAddress = value; }
        }

        string IPropertiesFeatureContext.EoStatusText
        {
            get { return _features.ScreenStateFeature._eoStatusText; }
            set { _features.ScreenStateFeature._eoStatusText = value; }
        }

        int IPropertiesFeatureContext.EoVideoHeight
        {
            get { return _features.ScreenStateFeature._eoVideoHeight; }
            set { _features.ScreenStateFeature._eoVideoHeight = value; }
        }

        int IPropertiesFeatureContext.EoVideoWidth
        {
            get { return _features.ScreenStateFeature._eoVideoWidth; }
            set { _features.ScreenStateFeature._eoVideoWidth = value; }
        }

        ref int? IPropertiesFeatureContext.FocusPositionValue
        {
            get { return ref _features.ScreenStateFeature._focusPositionValue; }
        }

        string IPropertiesFeatureContext.FocusSyncStatusText
        {
            get { return _features.ScreenStateFeature._focusSyncStatusText; }
            set { _features.ScreenStateFeature._focusSyncStatusText = value; }
        }

        bool IPropertiesFeatureContext.HasDevicePowerStatus
        {
            get { return _features.DevicePowerFeature._hasDevicePowerStatus; }
            set { _features.DevicePowerFeature._hasDevicePowerStatus = value; }
        }

        string IPropertiesFeatureContext.HomeZeroLockMessage
        {
            get { return _features.ScreenStateFeature._homeZeroLockMessage; }
            set { _features.ScreenStateFeature._homeZeroLockMessage = value; }
        }

        string IPropertiesFeatureContext.HomeZeroLockTitle
        {
            get { return _features.ScreenStateFeature._homeZeroLockTitle; }
            set { _features.ScreenStateFeature._homeZeroLockTitle = value; }
        }

        string IPropertiesFeatureContext.HomeZeroStatusText
        {
            get { return _features.ScreenStateFeature._homeZeroStatusText; }
            set { _features.ScreenStateFeature._homeZeroStatusText = value; }
        }

        bool IPropertiesFeatureContext.IrAlreadyConnectedNotice
        {
            get { return _features.RtspSettingsFeature._irAlreadyConnectedNotice; }
            set { _features.RtspSettingsFeature._irAlreadyConnectedNotice = value; }
        }

        BitmapSource IPropertiesFeatureContext.IrCameraImage
        {
            get { return _features.ScreenStateFeature._irCameraImage; }
            set { _features.ScreenStateFeature._irCameraImage = value; }
        }

        string IPropertiesFeatureContext.IrSourceAddress
        {
            get { return _features.ScreenStateFeature._irSourceAddress; }
            set { _features.ScreenStateFeature._irSourceAddress = value; }
        }

        string IPropertiesFeatureContext.IrStatusText
        {
            get { return _features.ScreenStateFeature._irStatusText; }
            set { _features.ScreenStateFeature._irStatusText = value; }
        }

        int IPropertiesFeatureContext.IrVideoHeight
        {
            get { return _features.ScreenStateFeature._irVideoHeight; }
            set { _features.ScreenStateFeature._irVideoHeight = value; }
        }

        int IPropertiesFeatureContext.IrVideoWidth
        {
            get { return _features.ScreenStateFeature._irVideoWidth; }
            set { _features.ScreenStateFeature._irVideoWidth = value; }
        }

        bool IPropertiesFeatureContext.IsAutoTrackingEnabled
        {
            get { return _features.ScreenStateFeature._isAutoTrackingEnabled; }
            set { _features.ScreenStateFeature._isAutoTrackingEnabled = value; }
        }

        bool IPropertiesFeatureContext.IsCrosshairVisible
        {
            get { return _features.ScreenStateFeature._isCrosshairVisible; }
            set { _features.ScreenStateFeature._isCrosshairVisible = value; }
        }

        bool IPropertiesFeatureContext.IsDeviceConnectionRequested
        {
            get { return _features.ScreenStateFeature._isDeviceConnectionRequested; }
            set { _features.ScreenStateFeature._isDeviceConnectionRequested = value; }
        }

        bool IPropertiesFeatureContext.IsDevicePowerStatusStale
        {
            get { return _features.DevicePowerFeature._isDevicePowerStatusStale; }
            set { _features.DevicePowerFeature._isDevicePowerStatusStale = value; }
        }

        bool IPropertiesFeatureContext.IsHomePositionMoving
        {
            get { return _features.ScreenStateFeature._isHomePositionMoving; }
            set { _features.ScreenStateFeature._isHomePositionMoving = value; }
        }

        bool IPropertiesFeatureContext.IsLaPresetScanRunning
        {
            get { return _features.ScreenStateFeature._isLaPresetScanRunning; }
            set { _features.ScreenStateFeature._isLaPresetScanRunning = value; }
        }

        bool IPropertiesFeatureContext.IsPresetScanRunning
        {
            get { return _features.ScreenStateFeature._isPresetScanRunning; }
            set { _features.ScreenStateFeature._isPresetScanRunning = value; }
        }

        string IPropertiesFeatureContext.LaPresetCommandStatusTextState
        {
            get { return _features.ScreenStateFeature._laPresetCommandStatusText; }
            set { _features.ScreenStateFeature._laPresetCommandStatusText = value; }
        }

        int IPropertiesFeatureContext.LaPresetScanDelayState
        {
            get { return _features.ScreenStateFeature._laPresetScanDelay; }
            set { _features.ScreenStateFeature._laPresetScanDelay = value; }
        }

        int IPropertiesFeatureContext.LaPresetScanSpeedState
        {
            get { return _features.ScreenStateFeature._laPresetScanSpeed; }
            set { _features.ScreenStateFeature._laPresetScanSpeed = value; }
        }

        int IPropertiesFeatureContext.LaPresetSlotNumberState
        {
            get { return _features.ScreenStateFeature._laPresetSlotNumber; }
            set { _features.ScreenStateFeature._laPresetSlotNumber = value; }
        }

        string IPropertiesFeatureContext.LrfDistanceText
        {
            get { return _features.ScreenStateFeature._lrfDistanceText; }
            set { _features.ScreenStateFeature._lrfDistanceText = value; }
        }

        string IPropertiesFeatureContext.McbMaintenanceIpAddress
        {
            get { return _features.ScreenStateFeature._mcbMaintenanceIpAddress; }
            set { _features.ScreenStateFeature._mcbMaintenanceIpAddress = value; }
        }

        ref double? IPropertiesFeatureContext.PanAbsoluteValue
        {
            get { return ref _features.ScreenStateFeature._panAbsoluteValue; }
        }

        byte IPropertiesFeatureContext.PanTiltSpeedLevel
        {
            get { return _features.ScreenStateFeature._panTiltSpeedLevel; }
            set { _features.ScreenStateFeature._panTiltSpeedLevel = value; }
        }

        PanTurnMode IPropertiesFeatureContext.PanTurnMode
        {
            get { return _features.ScreenStateFeature._panTurnMode; }
            set { _features.ScreenStateFeature._panTurnMode = value; }
        }

        bool IPropertiesFeatureContext.PrecisionActive
        {
            get { return _features.WebAgentMotionFocusFeature._precisionActive; }
            set { _features.WebAgentMotionFocusFeature._precisionActive = value; }
        }

        bool IPropertiesFeatureContext.PrecisionStopUncertain
        {
            get { return _features.WebAgentMotionFocusFeature._precisionStopUncertain; }
            set { _features.WebAgentMotionFocusFeature._precisionStopUncertain = value; }
        }

        string IPropertiesFeatureContext.PresetCommandStatusTextState
        {
            get { return _features.ScreenStateFeature._presetCommandStatusText; }
            set { _features.ScreenStateFeature._presetCommandStatusText = value; }
        }

        int IPropertiesFeatureContext.PresetScanDelayState
        {
            get { return _features.ScreenStateFeature._presetScanDelay; }
            set { _features.ScreenStateFeature._presetScanDelay = value; }
        }

        PresetScanOrderMode IPropertiesFeatureContext.PresetScanOrderMode
        {
            get { return _features.ScreenStateFeature._presetScanOrderMode; }
            set { _features.ScreenStateFeature._presetScanOrderMode = value; }
        }

        int IPropertiesFeatureContext.PresetScanSpeedState
        {
            get { return _features.ScreenStateFeature._presetScanSpeed; }
            set { _features.ScreenStateFeature._presetScanSpeed = value; }
        }

        int IPropertiesFeatureContext.PresetSlotNumberState
        {
            get { return _features.ScreenStateFeature._presetSlotNumber; }
            set { _features.ScreenStateFeature._presetSlotNumber = value; }
        }

        ControlAgentProfileOption IPropertiesFeatureContext.SelectedControlAgentProfileState
        {
            get { return _features.RtspSettingsFeature._selectedControlAgentProfile; }
            set { _features.RtspSettingsFeature._selectedControlAgentProfile = value; }
        }

        EquipmentStatusMode IPropertiesFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features.ScreenStateFeature._selectedEquipmentStatusMode; }
            set { _features.ScreenStateFeature._selectedEquipmentStatusMode = value; }
        }

        ZoomSyncLevelOption IPropertiesFeatureContext.SelectedFocusSyncLevel
        {
            get { return _features.ScreenStateFeature._selectedFocusSyncLevel; }
            set { _features.ScreenStateFeature._selectedFocusSyncLevel = value; }
        }

        PresetPointOption IPropertiesFeatureContext.SelectedLaPresetPoint
        {
            get { return _features.ScreenStateFeature._selectedLaPresetPoint; }
            set { _features.ScreenStateFeature._selectedLaPresetPoint = value; }
        }

        int IPropertiesFeatureContext.SelectedOperationControlTabIndex
        {
            get { return _features.ScreenStateFeature._selectedOperationControlTabIndex; }
            set { _features.ScreenStateFeature._selectedOperationControlTabIndex = value; }
        }

        PresetPointOption IPropertiesFeatureContext.SelectedPresetPoint
        {
            get { return _features.ScreenStateFeature._selectedPresetPoint; }
            set { _features.ScreenStateFeature._selectedPresetPoint = value; }
        }

        int IPropertiesFeatureContext.SelectedRightPanelTabIndex
        {
            get { return _features.ScreenStateFeature._selectedRightPanelTabIndex; }
            set { _features.ScreenStateFeature._selectedRightPanelTabIndex = value; }
        }

        ZoomSyncLevelOption IPropertiesFeatureContext.SelectedZoomSyncLevel
        {
            get { return _features.ScreenStateFeature._selectedZoomSyncLevel; }
            set { _features.ScreenStateFeature._selectedZoomSyncLevel = value; }
        }

        ref double? IPropertiesFeatureContext.TiltAbsoluteValue
        {
            get { return ref _features.ScreenStateFeature._tiltAbsoluteValue; }
        }

        ref int? IPropertiesFeatureContext.ZoomPositionValue
        {
            get { return ref _features.ScreenStateFeature._zoomPositionValue; }
        }

        ref double? IPropertiesFeatureContext.ZoomRatioValue
        {
            get { return ref _features.ScreenStateFeature._zoomRatioValue; }
        }

        string IPropertiesFeatureContext.ZoomSyncStatusText
        {
            get { return _features.ScreenStateFeature._zoomSyncStatusText; }
            set { _features.ScreenStateFeature._zoomSyncStatusText = value; }
        }

        string IPropertiesFeatureContext.AiAlertStatusText
        {
            get { return _features.EventAlertsFeature.AiAlertStatusText; }
        }

        /// <summary>
        /// 2026-09-15: REI 통합본에서는 Web Agent 프로필에만 unsigned Pan 좌표를 적용한다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IPropertiesFeatureContext.ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
            _features.RtspSettingsFeature.ApplyControlAgentPanCoordinateMode(profile);
        }

        /// <summary>
        /// Pan / Tilt 연속 이동 중 Slider 값이 바뀌면 현재 방향을 유지한 채 Pelco-D Data1 / Data2 속도값만 갱신하여
        /// 즉시 재송신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPropertiesFeatureContext.ApplyPanTiltSpeedWhileMoving()
        {
            _features.ContinuousControlFeature.ApplyPanTiltSpeedWhileMoving();
        }

        /// <summary>
        /// 현재 RadioButton에서 선택한 Pan 회전 모드를 Pelco-D 확장 명령 0x4D로 장비에 적용한다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        bool IPropertiesFeatureContext.ApplySelectedPanTurnMode()
        {
            return _features.MoveControlFeature.ApplySelectedPanTurnMode();
        }

        /// <summary>
        /// 2026-10-01: 현재 활성 EO/IR 카메라의 광학 줌이 커질수록 같은 PT 속도도 화면에서는 더 크게 이동해 보이므로 실제 전송
        /// 속도를 낮춘다. UI Slider 값은 사용자가 선택한 기준 속도로 유지하며 광각(1x)에서는 기존 속도를 그대로 사용한다.
        /// 2026-10-01 실장비 로그에서 EO 최대 줌 시 20% 하한(30→6)이 빠르게 보이는 것을 확인하여 5% 안전 하한으로 완화한다.
        /// 실제 EO 90x 계산값은 약 10.5%이므로 기준 속도 30에서 약 3이 적용된다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        byte IPropertiesFeatureContext.ApplyZoomAdaptivePanTiltSpeed(byte baseProtocolSpeed, out int standardZoom, out string activeCamera, out double opticalZoomRatio, out double speedScale)
        {
            return _features.ContinuousControlFeature.ApplyZoomAdaptivePanTiltSpeed(baseProtocolSpeed, out standardZoom, out activeCamera, out opticalZoomRatio, out speedScale);
        }

        /// <summary>
        /// 2026-09-17: Web Agent 합의 규격인 UI 속도 [0 ~ 60]을 그대로 사용한다. UI 0은 이동 속도로 사용하지 않으며
        /// 정지 요청으로 처리한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        byte IPropertiesFeatureContext.ConvertPanTiltSpeedLevel(int uiSpeed)
        {
            return _features.ContinuousControlFeature.ConvertPanTiltSpeedLevel(uiSpeed);
        }

        string IPropertiesFeatureContext.CurrentEoPowerStatusColor
        {
            get { return _features.DevicePowerFeature.CurrentEoPowerStatusColor; }
        }

        string IPropertiesFeatureContext.CurrentIrPowerStatusColor
        {
            get { return _features.DevicePowerFeature.CurrentIrPowerStatusColor; }
        }

        double IPropertiesFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double IPropertiesFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        string IPropertiesFeatureContext.DeviceControlConnectionStatusText
        {
            get { return _features.DevicePowerFeature.DeviceControlConnectionStatusText; }
        }

        /// <summary>
        /// 장비 모드 변경으로 숨겨진 통신 탭이 선택된 경우 표시 가능한 탭으로 복귀한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPropertiesFeatureContext.EnsureVisibleCommunicationSettingsTab()
        {
            _features.RtspSettingsFeature.EnsureVisibleCommunicationSettingsTab();
        }

        string IPropertiesFeatureContext.EoPowerControlStatusText
        {
            get { return _features.DevicePowerFeature.EoPowerControlStatusText; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// ->; Standard 0(Far) Raw 0(Near) ->; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IPropertiesFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IPropertiesFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IPropertiesFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardFocus();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IPropertiesFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        string IPropertiesFeatureContext.IrPowerControlStatusText
        {
            get { return _features.DevicePowerFeature.IrPowerControlStatusText; }
        }

        bool IPropertiesFeatureContext.IsGpsTabVisible
        {
            get { return _features.RtspSettingsFeature.IsGpsTabVisible; }
        }

        bool IPropertiesFeatureContext.IsImuRpyTabVisible
        {
            get { return _features.RtspSettingsFeature.IsImuRpyTabVisible; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCancellationCompleted
        {
            get { return _features.PanoramaFeature.IsPanoramaCancellationCompleted; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCancellationRequested
        {
            get { return _features.PanoramaFeature.IsPanoramaCancellationRequested; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCaptureRunning
        {
            get { return _features.PanoramaFeature.IsPanoramaCaptureRunning; }
            set { _features.PanoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        bool IPropertiesFeatureContext.IsPanoramaCompleted
        {
            get { return _features.PanoramaFeature.IsPanoramaCompleted; }
        }

        bool IPropertiesFeatureContext.IsPanoramaPresetRunning
        {
            get { return _features.PanoramaPresetsFeature.IsPanoramaPresetRunning; }
        }

        bool IPropertiesFeatureContext.IsPanoramaProcessingRunning
        {
            get { return _features.PanoramaFeature.IsPanoramaProcessingRunning; }
        }

        bool IPropertiesFeatureContext.IsRouteRunning
        {
            get { return _features.RoutesFeature.IsRouteRunning; }
        }

        bool IPropertiesFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features.RtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        bool IPropertiesFeatureContext.IsSystemDateTimeTabVisible
        {
            get { return _features.RtspSettingsFeature.IsSystemDateTimeTabVisible; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPropertiesFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IPropertiesFeatureContext.PresetIrZoomPosition(PresetPointOption preset)
        {
            return _features.PresetLensFeature.PresetIrZoomPosition(preset);
        }

        /// <summary>
        /// EO와 IR 기능 지원 조회를 요청하고 응답 기한을 기록한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPropertiesFeatureContext.QuerySpecialFeatureSupport()
        {
            _features.SpecialFeaturesFeature.QuerySpecialFeatureSupport();
        }

        /// <summary>
        /// 카메라별로 마지막 확인된 전원 상태와 확인 시점을 보관한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPropertiesFeatureContext.RememberCameraPower(string camera, bool on)
        {
            _features.DevicePowerFeature.RememberCameraPower(camera, on);
        }

        /// <summary>
        /// 현재 프리셋과 순서 정보를 저장하며 기존 파일 보존 규칙을 적용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IPropertiesFeatureContext.SavePresetStorage()
        {
            return _features.PresetStorageFeature.SavePresetStorage();
        }

        /// <summary>
        /// 현재 선택값을 다음 연결용 설정으로 저장하며 로그의 인증정보는 가린다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPropertiesFeatureContext.SaveRtspCommunicationSettings()
        {
            _features.RtspSettingsFeature.SaveRtspCommunicationSettings();
        }

        ControlAgentProfileOption IPropertiesFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        int IPropertiesFeatureContext.SelectedEventAlertTabIndex
        {
            get { return _features.EventAlertsFeature.SelectedEventAlertTabIndex; }
            set { _features.EventAlertsFeature.SelectedEventAlertTabIndex = value; }
        }

        /// <summary>
        /// 선택한 영상 채널에 맞춰 구역 참조와 표시 상태를 갱신한다.
        /// </summary>
        void IPropertiesFeatureContext.ZoneChannelChanged(string camera)
        {
            _features.ZonesFeature.ZoneChannelChanged(camera);
        }

        /// <summary>
        /// 표준 줌 위치와 확인된 배율을 상태 문자열로 구성한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        string IPropertiesFeatureContext.ZoomRatioStatus(bool ir)
        {
            return _features.InputCoordinatorFeature.ZoomRatioStatus(ir);
        }
    }
}
