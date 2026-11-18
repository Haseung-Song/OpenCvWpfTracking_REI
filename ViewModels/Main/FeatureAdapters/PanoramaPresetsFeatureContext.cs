using OpenCvWpfTracking.Models.Main;
using System;
using System.Threading;
using System.Threading.Tasks;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class PanoramaPresetsFeatureContext : IPanoramaPresetsFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PanoramaPresetsFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        System.Collections.Generic.Dictionary<string, RtspSourceOption> IPanoramaPresetsFeatureContext._axisCtecSources
        {
            get { return _features._inputCoordinatorFeature._axisCtecSources; }
        }

        Services.Communication.IControlCommandServicePort IPanoramaPresetsFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> IPanoramaPresetsFeatureContext._controlInputs
        {
            get { return _features._inputCoordinatorFeature._controlInputs; }
        }

        ContinuousMoveType IPanoramaPresetsFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        double IPanoramaPresetsFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        double IPanoramaPresetsFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        bool IPanoramaPresetsFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<int, int>> IPanoramaPresetsFeatureContext._issuedControlAxes
        {
            get { return _features._inputCoordinatorFeature._issuedControlAxes; }
        }

        CancellationTokenSource IPanoramaPresetsFeatureContext._laPresetSingleMoveCts
        {
            get { return _features._screenStateFeature._laPresetSingleMoveCts; }
            set { _features._screenStateFeature._laPresetSingleMoveCts = value; }
        }

        Services.Communication.ITcpClientServicePort IPanoramaPresetsFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        CancellationTokenSource IPanoramaPresetsFeatureContext._presetSingleMoveCts
        {
            get { return _features._screenStateFeature._presetSingleMoveCts; }
            set { _features._screenStateFeature._presetSingleMoveCts = value; }
        }

        bool IPanoramaPresetsFeatureContext._videoCenterBusy
        {
            get { return _features._cameraFeatureControlFeature._videoCenterBusy; }
            set { _features._cameraFeatureControlFeature._videoCenterBusy = value; }
        }

        string IPanoramaPresetsFeatureContext.ControlLockMessage
        {
            get { return _features._propertiesFeature.ControlLockMessage; }
        }

        string IPanoramaPresetsFeatureContext.ControlLockTitle
        {
            get { return _features._propertiesFeature.ControlLockTitle; }
        }

        string IPanoramaPresetsFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        bool IPanoramaPresetsFeatureContext.HasRegisteredEquipmentIdentity
        {
            get { return _features._presetStorageFeature.HasRegisteredEquipmentIdentity; }
        }

        bool IPanoramaPresetsFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IPanoramaPresetsFeatureContext.IsEventAlertControlEnabled
        {
            get { return _features._propertiesFeature.IsEventAlertControlEnabled; }
        }

        bool IPanoramaPresetsFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IPanoramaPresetsFeatureContext.IsOperationLockOverlayVisible
        {
            get { return _features._propertiesFeature.IsOperationLockOverlayVisible; }
        }

        bool IPanoramaPresetsFeatureContext.IsPanTiltSpeedControlEnabled
        {
            get { return _features._propertiesFeature.IsPanTiltSpeedControlEnabled; }
        }

        bool IPanoramaPresetsFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        string IPanoramaPresetsFeatureContext.LaPresetCommandStatusText
        {
            get { return _features._propertiesFeature.LaPresetCommandStatusText; }
            set { _features._propertiesFeature.LaPresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaPresetsFeatureContext.LaPresetPoints
        {
            get { return _features._propertiesFeature.LaPresetPoints; }
        }

        Task IPanoramaPresetsFeatureContext.MoveToSelectedLaPresetPointAsync()
        {
            return _features._moveControlFeature.MoveToSelectedLaPresetPointAsync();
        }

        Task IPanoramaPresetsFeatureContext.MoveToSelectedPresetPointAsync()
        {
            return _features._moveControlFeature.MoveToSelectedPresetPointAsync();
        }

        void IPanoramaPresetsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IPanoramaPresetsFeatureContext.PanoramaDeviceKey
        {
            get { return _features._opticalCenterFeature.PanoramaDeviceKey; }
        }

        bool IPanoramaPresetsFeatureContext.PanoramaPoseFresh
        {
            get { return _features._opticalCenterFeature.PanoramaPoseFresh; }
        }

        Services.Video.PanoramaReferenceState IPanoramaPresetsFeatureContext.PanoramaReference
        {
            get { return _features._panoramaReferenceFeature.PanoramaReference; }
        }

        string IPanoramaPresetsFeatureContext.PresetCommandStatusText
        {
            get { return _features._propertiesFeature.PresetCommandStatusText; }
            set { _features._propertiesFeature.PresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaPresetsFeatureContext.PresetPoints
        {
            get { return _features._propertiesFeature.PresetPoints; }
        }

        string IPanoramaPresetsFeatureContext.RegisteredEquipmentIdentity
        {
            get { return _features._presetStorageFeature.RegisteredEquipmentIdentity; }
        }

        ControlAgentProfileOption IPanoramaPresetsFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        PresetPointOption IPanoramaPresetsFeatureContext.SelectedLaPresetPoint
        {
            get { return _features._propertiesFeature.SelectedLaPresetPoint; }
            set { _features._propertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IPanoramaPresetsFeatureContext.SelectedPresetPoint
        {
            get { return _features._propertiesFeature.SelectedPresetPoint; }
            set { _features._propertiesFeature.SelectedPresetPoint = value; }
        }

        void IPanoramaPresetsFeatureContext.StopLaPresetMove()
        {
            _features._moveControlFeature.StopLaPresetMove();
        }

        void IPanoramaPresetsFeatureContext.StopPresetMove()
        {
            _features._moveControlFeature.StopPresetMove();
        }
    }
}
