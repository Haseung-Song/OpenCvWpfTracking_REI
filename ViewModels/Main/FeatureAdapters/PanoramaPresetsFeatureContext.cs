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

        bool IPanoramaPresetsFeatureContext.IsAutoTrackingActive
        {
            get { return _features.PropertiesFeature.IsAutoTrackingEnabled; }
        }

        /// <summary>제어 소유권을 넘기기 전에 추적을 중단한다.</summary>
        void IPanoramaPresetsFeatureContext.StopAutoTracking(string reason)
        {
            _features.AutoTrackingFeature.Stop(reason);
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PanoramaPresetsFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        System.Collections.Generic.Dictionary<string, RtspSourceOption> IPanoramaPresetsFeatureContext.AxisCtecSources
        {
            get { return _features.InputCoordinatorFeature._axisCtecSources; }
        }

        Services.Communication.IControlCommandServicePort IPanoramaPresetsFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> IPanoramaPresetsFeatureContext.ControlInputs
        {
            get { return _features.InputCoordinatorFeature._controlInputs; }
        }

        ContinuousMoveType IPanoramaPresetsFeatureContext.CurrentMoveType
        {
            get { return _features.ScreenStateFeature._currentMoveType; }
            set { _features.ScreenStateFeature._currentMoveType = value; }
        }

        double IPanoramaPresetsFeatureContext.CurrentPan
        {
            get { return _features.ScreenStateFeature._currentPan; }
            set { _features.ScreenStateFeature._currentPan = value; }
        }

        double IPanoramaPresetsFeatureContext.CurrentTilt
        {
            get { return _features.ScreenStateFeature._currentTilt; }
            set { _features.ScreenStateFeature._currentTilt = value; }
        }

        bool IPanoramaPresetsFeatureContext.IsDeviceConnectionRequested
        {
            get { return _features.ScreenStateFeature._isDeviceConnectionRequested; }
            set { _features.ScreenStateFeature._isDeviceConnectionRequested = value; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<int, int>> IPanoramaPresetsFeatureContext.IssuedControlAxes
        {
            get { return _features.InputCoordinatorFeature._issuedControlAxes; }
        }

        CancellationTokenSource IPanoramaPresetsFeatureContext.LaPresetSingleMoveCts
        {
            get { return _features.ScreenStateFeature._laPresetSingleMoveCts; }
            set { _features.ScreenStateFeature._laPresetSingleMoveCts = value; }
        }

        Services.Communication.ITcpClientServicePort IPanoramaPresetsFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        CancellationTokenSource IPanoramaPresetsFeatureContext.PresetSingleMoveCts
        {
            get { return _features.ScreenStateFeature._presetSingleMoveCts; }
            set { _features.ScreenStateFeature._presetSingleMoveCts = value; }
        }

        bool IPanoramaPresetsFeatureContext.VideoCenterBusy
        {
            get { return _features.CameraFeatureControlFeature._videoCenterBusy; }
            set { _features.CameraFeatureControlFeature._videoCenterBusy = value; }
        }

        string IPanoramaPresetsFeatureContext.ControlLockMessage
        {
            get { return _features.PropertiesFeature.ControlLockMessage; }
        }

        string IPanoramaPresetsFeatureContext.ControlLockTitle
        {
            get { return _features.PropertiesFeature.ControlLockTitle; }
        }

        string IPanoramaPresetsFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        bool IPanoramaPresetsFeatureContext.HasRegisteredEquipmentIdentity
        {
            get { return _features.PresetStorageFeature.HasRegisteredEquipmentIdentity; }
        }

        bool IPanoramaPresetsFeatureContext.IsControlInputLocked
        {
            get { return _features.PropertiesFeature.IsControlInputLocked; }
        }

        bool IPanoramaPresetsFeatureContext.IsEventAlertControlEnabled
        {
            get { return _features.PropertiesFeature.IsEventAlertControlEnabled; }
        }

        bool IPanoramaPresetsFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        bool IPanoramaPresetsFeatureContext.IsOperationLockOverlayVisible
        {
            get { return _features.PropertiesFeature.IsOperationLockOverlayVisible; }
        }

        bool IPanoramaPresetsFeatureContext.IsPanTiltSpeedControlEnabled
        {
            get { return _features.PropertiesFeature.IsPanTiltSpeedControlEnabled; }
        }

        bool IPanoramaPresetsFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        string IPanoramaPresetsFeatureContext.LaPresetCommandStatusText
        {
            get { return _features.PropertiesFeature.LaPresetCommandStatusText; }
            set { _features.PropertiesFeature.LaPresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaPresetsFeatureContext.LaPresetPoints
        {
            get { return _features.PropertiesFeature.LaPresetPoints; }
        }

        /// <summary>
        /// PRESET 1 선택 지점으로 WPF 저장값 기준 PTZF 전체를 직접 복원한다. LA 0x05 한 번에 의존하지 않고 Pan, Tilt,
        /// EO/IR Zoom, EO/IR Focus를 현재 프로젝트의 실제 제어 경로로 각각 실행한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IPanoramaPresetsFeatureContext.MoveToSelectedLaPresetPointAsync()
        {
            return _features.MoveControlFeature.MoveToSelectedLaPresetPointAsync();
        }

        /// <summary>
        /// 기존 웹 프리셋 이동을 번호 클릭과 순회 연동에서 완료까지 기다릴 수 있도록 공유한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IPanoramaPresetsFeatureContext.MoveToSelectedPresetPointAsync()
        {
            return _features.MoveControlFeature.MoveToSelectedPresetPointAsync();
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPanoramaPresetsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IPanoramaPresetsFeatureContext.PanoramaDeviceKey
        {
            get { return _features.OpticalCenterFeature.PanoramaDeviceKey; }
        }

        bool IPanoramaPresetsFeatureContext.PanoramaPoseFresh
        {
            get { return _features.OpticalCenterFeature.PanoramaPoseFresh; }
        }

        Services.Video.PanoramaReferenceState IPanoramaPresetsFeatureContext.PanoramaReference
        {
            get { return _features.PanoramaReferenceFeature.PanoramaReference; }
        }

        string IPanoramaPresetsFeatureContext.PresetCommandStatusText
        {
            get { return _features.PropertiesFeature.PresetCommandStatusText; }
            set { _features.PropertiesFeature.PresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaPresetsFeatureContext.PresetPoints
        {
            get { return _features.PropertiesFeature.PresetPoints; }
        }

        string IPanoramaPresetsFeatureContext.RegisteredEquipmentIdentity
        {
            get { return _features.PresetStorageFeature.RegisteredEquipmentIdentity; }
        }

        ControlAgentProfileOption IPanoramaPresetsFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        PresetPointOption IPanoramaPresetsFeatureContext.SelectedLaPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedLaPresetPoint; }
            set { _features.PropertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IPanoramaPresetsFeatureContext.SelectedPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedPresetPoint; }
            set { _features.PropertiesFeature.SelectedPresetPoint = value; }
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다. AUTO SCAN 반복 상태에는 영향을 주지 않으며, Pan/Tilt 위치 이동 정지
        /// 명령(0x4F)을 각각 송신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPanoramaPresetsFeatureContext.StopLaPresetMove()
        {
            _features.MoveControlFeature.StopLaPresetMove();
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다. AUTO SCAN 반복 상태에는 영향을 주지 않으며, Pan/Tilt 위치 이동 정지
        /// 명령(0x4F)을 각각 송신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPanoramaPresetsFeatureContext.StopPresetMove()
        {
            _features.MoveControlFeature.StopPresetMove();
        }
    }
}
