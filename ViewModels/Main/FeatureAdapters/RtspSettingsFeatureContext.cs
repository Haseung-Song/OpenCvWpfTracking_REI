using OpenCvWpfTracking.Models.Main;
using System;
using System.Collections.ObjectModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class RtspSettingsFeatureContext : IRtspSettingsFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal RtspSettingsFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        bool IRtspSettingsFeatureContext.ApplyingPresetProfile
        {
            get { return _features.PresetStorageFeature._applyingPresetProfile; }
            set { _features.PresetStorageFeature._applyingPresetProfile = value; }
        }

        System.Threading.CancellationTokenSource IRtspSettingsFeatureContext.ControlAgentReconnectCts
        {
            get { return _features.ScreenStateFeature._controlAgentReconnectCts; }
            set { _features.ScreenStateFeature._controlAgentReconnectCts = value; }
        }

        Services.Communication.IControlCommandServicePort IRtspSettingsFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Threading.CancellationTokenSource IRtspSettingsFeatureContext.Cts
        {
            get { return _features.ScreenStateFeature._cts; }
            set { _features.ScreenStateFeature._cts = value; }
        }

        Services.Video.IFFmpegDecoderServicePort IRtspSettingsFeatureContext.EoDecoder
        {
            get { return _features.Runtime._eoDecoder; }
        }

        Services.Video.IFFmpegDecoderServicePort IRtspSettingsFeatureContext.IrDecoder
        {
            get { return _features.Runtime._irDecoder; }
        }

        bool IRtspSettingsFeatureContext.IsEoFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isEoFrameDisplayed; }
            set { _features.ScreenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IRtspSettingsFeatureContext.IsIrFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isIrFrameDisplayed; }
            set { _features.ScreenStateFeature._isIrFrameDisplayed = value; }
        }

        bool IRtspSettingsFeatureContext.IsVideoConnecting
        {
            get { return _features.ScreenStateFeature._isVideoConnecting; }
            set { _features.ScreenStateFeature._isVideoConnecting = value; }
        }

        Services.Communication.ITcpClientServicePort IRtspSettingsFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedAiEoRtspSourceState
        {
            get { return _features.PropertiesFeature._selectedAiEoRtspSource; }
            set { _features.PropertiesFeature._selectedAiEoRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedAiIrRtspSourceState
        {
            get { return _features.PropertiesFeature._selectedAiIrRtspSource; }
            set { _features.PropertiesFeature._selectedAiIrRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedEoRtspSourceState
        {
            get { return _features.PropertiesFeature._selectedEoRtspSource; }
            set { _features.PropertiesFeature._selectedEoRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedIrRtspSourceState
        {
            get { return _features.PropertiesFeature._selectedIrRtspSource; }
            set { _features.PropertiesFeature._selectedIrRtspSource = value; }
        }

        string IRtspSettingsFeatureContext.AiRtsp0Address
        {
            get { return _features.PropertiesFeature.AiRtsp0Address; }
            set { _features.PropertiesFeature.AiRtsp0Address = value; }
        }

        string IRtspSettingsFeatureContext.AiRtsp1Address
        {
            get { return _features.PropertiesFeature.AiRtsp1Address; }
            set { _features.PropertiesFeature.AiRtsp1Address = value; }
        }

        string IRtspSettingsFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features.PropertiesFeature.ControlAgentConnectionStatusText; }
            set { _features.PropertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IRtspSettingsFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string IRtspSettingsFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        string IRtspSettingsFeatureContext.CurrentControlPowerText
        {
            get { return _features.PropertiesFeature.CurrentControlPowerText; }
        }

        string IRtspSettingsFeatureContext.CurrentEoPowerText
        {
            get { return _features.PropertiesFeature.CurrentEoPowerText; }
        }

        string IRtspSettingsFeatureContext.CurrentIrPowerText
        {
            get { return _features.PropertiesFeature.CurrentIrPowerText; }
        }

        string IRtspSettingsFeatureContext.DevicePowerFreshnessText
        {
            get { return _features.DevicePowerFeature.DevicePowerFreshnessText; }
        }

        int IRtspSettingsFeatureContext.EoMinimumSpeed
        {
            get { return _features.InputCoordinatorFeature.EoMinimumSpeed; }
            set { _features.InputCoordinatorFeature.EoMinimumSpeed = value; }
        }

        ObservableCollection<RtspSourceOption> IRtspSettingsFeatureContext.EoRtspSourceOptions
        {
            get { return _features.PropertiesFeature.EoRtspSourceOptions; }
        }

        string IRtspSettingsFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        double IRtspSettingsFeatureContext.EoSpeedExponent
        {
            get { return _features.InputCoordinatorFeature.EoSpeedExponent; }
            set { _features.InputCoordinatorFeature.EoSpeedExponent = value; }
        }

        string IRtspSettingsFeatureContext.EoStatusText
        {
            get { return _features.PropertiesFeature.EoStatusText; }
            set { _features.PropertiesFeature.EoStatusText = value; }
        }

        string IRtspSettingsFeatureContext.EoZoomRatioOverlayText
        {
            get { return _features.InputCoordinatorFeature.EoZoomRatioOverlayText; }
        }

        int IRtspSettingsFeatureContext.IrMinimumSpeed
        {
            get { return _features.InputCoordinatorFeature.IrMinimumSpeed; }
            set { _features.InputCoordinatorFeature.IrMinimumSpeed = value; }
        }

        ObservableCollection<RtspSourceOption> IRtspSettingsFeatureContext.IrRtspSourceOptions
        {
            get { return _features.PropertiesFeature.IrRtspSourceOptions; }
        }

        string IRtspSettingsFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        double IRtspSettingsFeatureContext.IrSpeedExponent
        {
            get { return _features.InputCoordinatorFeature.IrSpeedExponent; }
            set { _features.InputCoordinatorFeature.IrSpeedExponent = value; }
        }

        string IRtspSettingsFeatureContext.IrStatusText
        {
            get { return _features.PropertiesFeature.IrStatusText; }
            set { _features.PropertiesFeature.IrStatusText = value; }
        }

        string IRtspSettingsFeatureContext.IrZoomRatioOverlayText
        {
            get { return _features.InputCoordinatorFeature.IrZoomRatioOverlayText; }
        }

        bool IRtspSettingsFeatureContext.IsAiEoRtspDirectInput
        {
            get { return _features.PropertiesFeature.IsAiEoRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsAiIrRtspDirectInput
        {
            get { return _features.PropertiesFeature.IsAiIrRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsAutoFocusPanelVisible
        {
            get { return _features.WebAgentMotionFocusFeature.IsAutoFocusPanelVisible; }
        }

        bool IRtspSettingsFeatureContext.IsContextPanelsVisible
        {
            get { return _features.PropertiesFeature.IsContextPanelsVisible; }
        }

        bool IRtspSettingsFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features.PropertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IRtspSettingsFeatureContext.IsEoRtspDirectInput
        {
            get { return _features.PropertiesFeature.IsEoRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsIrRtspDirectInput
        {
            get { return _features.PropertiesFeature.IsIrRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsWebAgentFocusControlEnabled
        {
            get { return _features.WebAgentMotionFocusFeature.IsWebAgentFocusControlEnabled; }
            set { _features.WebAgentMotionFocusFeature.IsWebAgentFocusControlEnabled = value; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IRtspSettingsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        byte IRtspSettingsFeatureContext.PanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.PanTiltSpeedLevel; }
            set { _features.PropertiesFeature.PanTiltSpeedLevel = value; }
        }

        string IRtspSettingsFeatureContext.PanTiltZeroRouteText
        {
            get { return _features.PropertiesFeature.PanTiltZeroRouteText; }
        }

        /// <summary>
        /// IR 팔레트 버튼의 선택 표시를 초기화한다.
        /// </summary>
        void IRtspSettingsFeatureContext.ResetThermalPaletteButtonVisuals()
        {
            _features.ThermalFeature.ResetThermalPaletteButtonVisuals();
        }

        /// <summary>
        /// 연결 세대를 변경하고 이전 장비의 AF 상태·대기 요청·실행 확인을 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IRtspSettingsFeatureContext.ResetWebAgentControlSession()
        {
            _features.WebAgentMotionFocusFeature.ResetWebAgentControlSession();
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedAiEoRtspSource
        {
            get { return _features.PropertiesFeature.SelectedAiEoRtspSource; }
            set { _features.PropertiesFeature.SelectedAiEoRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedAiIrRtspSource
        {
            get { return _features.PropertiesFeature.SelectedAiIrRtspSource; }
            set { _features.PropertiesFeature.SelectedAiIrRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedEoRtspSource
        {
            get { return _features.PropertiesFeature.SelectedEoRtspSource; }
            set { _features.PropertiesFeature.SelectedEoRtspSource = value; }
        }

        EquipmentStatusMode IRtspSettingsFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features.PropertiesFeature.SelectedEquipmentStatusMode; }
            set { _features.PropertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedIrRtspSource
        {
            get { return _features.PropertiesFeature.SelectedIrRtspSource; }
            set { _features.PropertiesFeature.SelectedIrRtspSource = value; }
        }

        /// <summary>
        /// 장비를 바꾸면 이전 목록을 비우고 해당 장비의 저장값만 복원한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IRtspSettingsFeatureContext.SwitchEquipmentPresets()
        {
            _features.PresetStorageFeature.SwitchEquipmentPresets();
        }

        string IRtspSettingsFeatureContext.ThermalColorPaletteButtonText
        {
            get { return _features.WebAgentEquipmentFeature.ThermalColorPaletteButtonText; }
        }

        /// <summary>
        /// WEB AGENT 연결 상태에 맞춰 읽기 전용 시각 자동 조회를 시작하거나 중지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IRtspSettingsFeatureContext.UpdateSystemTimePollingState()
        {
            _features.SystemDateTimeFeature.UpdateSystemTimePollingState();
        }

        string IRtspSettingsFeatureContext.ZoomOpticalRangeText
        {
            get { return _features.InputCoordinatorFeature.ZoomOpticalRangeText; }
        }

        string IRtspSettingsFeatureContext.ZoomOpticalStandardText
        {
            get { return _features.InputCoordinatorFeature.ZoomOpticalStandardText; }
        }
    }
}
