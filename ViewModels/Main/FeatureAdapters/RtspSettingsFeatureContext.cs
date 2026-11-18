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

        bool IRtspSettingsFeatureContext._applyingPresetProfile
        {
            get { return _features._presetStorageFeature._applyingPresetProfile; }
            set { _features._presetStorageFeature._applyingPresetProfile = value; }
        }

        System.Threading.CancellationTokenSource IRtspSettingsFeatureContext._controlAgentReconnectCts
        {
            get { return _features._screenStateFeature._controlAgentReconnectCts; }
            set { _features._screenStateFeature._controlAgentReconnectCts = value; }
        }

        Services.Communication.IControlCommandServicePort IRtspSettingsFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Threading.CancellationTokenSource IRtspSettingsFeatureContext._cts
        {
            get { return _features._screenStateFeature._cts; }
            set { _features._screenStateFeature._cts = value; }
        }

        Services.Video.IFFmpegDecoderServicePort IRtspSettingsFeatureContext._eoDecoder
        {
            get { return _features.Runtime._eoDecoder; }
        }

        Services.Video.IFFmpegDecoderServicePort IRtspSettingsFeatureContext._irDecoder
        {
            get { return _features.Runtime._irDecoder; }
        }

        bool IRtspSettingsFeatureContext._isEoFrameDisplayed
        {
            get { return _features._screenStateFeature._isEoFrameDisplayed; }
            set { _features._screenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IRtspSettingsFeatureContext._isIrFrameDisplayed
        {
            get { return _features._screenStateFeature._isIrFrameDisplayed; }
            set { _features._screenStateFeature._isIrFrameDisplayed = value; }
        }

        bool IRtspSettingsFeatureContext._isVideoConnecting
        {
            get { return _features._screenStateFeature._isVideoConnecting; }
            set { _features._screenStateFeature._isVideoConnecting = value; }
        }

        Services.Communication.ITcpClientServicePort IRtspSettingsFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        RtspSourceOption IRtspSettingsFeatureContext._selectedAiEoRtspSource
        {
            get { return _features._propertiesFeature._selectedAiEoRtspSource; }
            set { _features._propertiesFeature._selectedAiEoRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext._selectedAiIrRtspSource
        {
            get { return _features._propertiesFeature._selectedAiIrRtspSource; }
            set { _features._propertiesFeature._selectedAiIrRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext._selectedEoRtspSource
        {
            get { return _features._propertiesFeature._selectedEoRtspSource; }
            set { _features._propertiesFeature._selectedEoRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext._selectedIrRtspSource
        {
            get { return _features._propertiesFeature._selectedIrRtspSource; }
            set { _features._propertiesFeature._selectedIrRtspSource = value; }
        }

        string IRtspSettingsFeatureContext.AiRtsp0Address
        {
            get { return _features._propertiesFeature.AiRtsp0Address; }
            set { _features._propertiesFeature.AiRtsp0Address = value; }
        }

        string IRtspSettingsFeatureContext.AiRtsp1Address
        {
            get { return _features._propertiesFeature.AiRtsp1Address; }
            set { _features._propertiesFeature.AiRtsp1Address = value; }
        }

        string IRtspSettingsFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features._propertiesFeature.ControlAgentConnectionStatusText; }
            set { _features._propertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IRtspSettingsFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IRtspSettingsFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        string IRtspSettingsFeatureContext.CurrentControlPowerText
        {
            get { return _features._propertiesFeature.CurrentControlPowerText; }
        }

        string IRtspSettingsFeatureContext.CurrentEoPowerText
        {
            get { return _features._propertiesFeature.CurrentEoPowerText; }
        }

        string IRtspSettingsFeatureContext.CurrentIrPowerText
        {
            get { return _features._propertiesFeature.CurrentIrPowerText; }
        }

        string IRtspSettingsFeatureContext.DevicePowerFreshnessText
        {
            get { return _features._devicePowerFeature.DevicePowerFreshnessText; }
        }

        int IRtspSettingsFeatureContext.EoMinimumSpeed
        {
            get { return _features._inputCoordinatorFeature.EoMinimumSpeed; }
            set { _features._inputCoordinatorFeature.EoMinimumSpeed = value; }
        }

        ObservableCollection<RtspSourceOption> IRtspSettingsFeatureContext.EoRtspSourceOptions
        {
            get { return _features._propertiesFeature.EoRtspSourceOptions; }
        }

        string IRtspSettingsFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        double IRtspSettingsFeatureContext.EoSpeedExponent
        {
            get { return _features._inputCoordinatorFeature.EoSpeedExponent; }
            set { _features._inputCoordinatorFeature.EoSpeedExponent = value; }
        }

        string IRtspSettingsFeatureContext.EoStatusText
        {
            get { return _features._propertiesFeature.EoStatusText; }
            set { _features._propertiesFeature.EoStatusText = value; }
        }

        string IRtspSettingsFeatureContext.EoZoomRatioOverlayText
        {
            get { return _features._inputCoordinatorFeature.EoZoomRatioOverlayText; }
        }

        int IRtspSettingsFeatureContext.IrMinimumSpeed
        {
            get { return _features._inputCoordinatorFeature.IrMinimumSpeed; }
            set { _features._inputCoordinatorFeature.IrMinimumSpeed = value; }
        }

        ObservableCollection<RtspSourceOption> IRtspSettingsFeatureContext.IrRtspSourceOptions
        {
            get { return _features._propertiesFeature.IrRtspSourceOptions; }
        }

        string IRtspSettingsFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        double IRtspSettingsFeatureContext.IrSpeedExponent
        {
            get { return _features._inputCoordinatorFeature.IrSpeedExponent; }
            set { _features._inputCoordinatorFeature.IrSpeedExponent = value; }
        }

        string IRtspSettingsFeatureContext.IrStatusText
        {
            get { return _features._propertiesFeature.IrStatusText; }
            set { _features._propertiesFeature.IrStatusText = value; }
        }

        string IRtspSettingsFeatureContext.IrZoomRatioOverlayText
        {
            get { return _features._inputCoordinatorFeature.IrZoomRatioOverlayText; }
        }

        bool IRtspSettingsFeatureContext.IsAiEoRtspDirectInput
        {
            get { return _features._propertiesFeature.IsAiEoRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsAiIrRtspDirectInput
        {
            get { return _features._propertiesFeature.IsAiIrRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsAutoFocusPanelVisible
        {
            get { return _features._webAgentMotionFocusFeature.IsAutoFocusPanelVisible; }
        }

        bool IRtspSettingsFeatureContext.IsContextPanelsVisible
        {
            get { return _features._propertiesFeature.IsContextPanelsVisible; }
        }

        bool IRtspSettingsFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IRtspSettingsFeatureContext.IsEoRtspDirectInput
        {
            get { return _features._propertiesFeature.IsEoRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsIrRtspDirectInput
        {
            get { return _features._propertiesFeature.IsIrRtspDirectInput; }
        }

        bool IRtspSettingsFeatureContext.IsWebAgentFocusControlEnabled
        {
            get { return _features._webAgentMotionFocusFeature.IsWebAgentFocusControlEnabled; }
            set { _features._webAgentMotionFocusFeature.IsWebAgentFocusControlEnabled = value; }
        }

        void IRtspSettingsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        byte IRtspSettingsFeatureContext.PanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.PanTiltSpeedLevel; }
            set { _features._propertiesFeature.PanTiltSpeedLevel = value; }
        }

        string IRtspSettingsFeatureContext.PanTiltZeroRouteText
        {
            get { return _features._propertiesFeature.PanTiltZeroRouteText; }
        }

        void IRtspSettingsFeatureContext.ResetThermalPaletteButtonVisuals()
        {
            _features._thermalFeature.ResetThermalPaletteButtonVisuals();
        }

        void IRtspSettingsFeatureContext.ResetWebAgentControlSession()
        {
            _features._webAgentMotionFocusFeature.ResetWebAgentControlSession();
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedAiEoRtspSource
        {
            get { return _features._propertiesFeature.SelectedAiEoRtspSource; }
            set { _features._propertiesFeature.SelectedAiEoRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedAiIrRtspSource
        {
            get { return _features._propertiesFeature.SelectedAiIrRtspSource; }
            set { _features._propertiesFeature.SelectedAiIrRtspSource = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedEoRtspSource
        {
            get { return _features._propertiesFeature.SelectedEoRtspSource; }
            set { _features._propertiesFeature.SelectedEoRtspSource = value; }
        }

        EquipmentStatusMode IRtspSettingsFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features._propertiesFeature.SelectedEquipmentStatusMode; }
            set { _features._propertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        RtspSourceOption IRtspSettingsFeatureContext.SelectedIrRtspSource
        {
            get { return _features._propertiesFeature.SelectedIrRtspSource; }
            set { _features._propertiesFeature.SelectedIrRtspSource = value; }
        }

        void IRtspSettingsFeatureContext.SwitchEquipmentPresets()
        {
            _features._presetStorageFeature.SwitchEquipmentPresets();
        }

        string IRtspSettingsFeatureContext.ThermalColorPaletteButtonText
        {
            get { return _features._webAgentEquipmentFeature.ThermalColorPaletteButtonText; }
        }

        void IRtspSettingsFeatureContext.UpdateSystemTimePollingState()
        {
            _features._systemDateTimeFeature.UpdateSystemTimePollingState();
        }

        string IRtspSettingsFeatureContext.ZoomOpticalRangeText
        {
            get { return _features._inputCoordinatorFeature.ZoomOpticalRangeText; }
        }

        string IRtspSettingsFeatureContext.ZoomOpticalStandardText
        {
            get { return _features._inputCoordinatorFeature.ZoomOpticalStandardText; }
        }
    }
}
