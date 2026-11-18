using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 기능 객체와 기능별 협력 계약을 한 번 조립한다. 화면 모델을 경유하지 않고 실제 담당 객체에 연결한다.
    /// 생성 중에는 기능을 실행하지 않으며 모든 객체를 만든 뒤 기존 순서로 초기화한다.
    /// </summary>
    internal sealed class FeatureComposition
    {
        // 화면 경계의 콜백과 공유 자원은 아래 기능들이 같은 인스턴스를 사용한다.
        internal FeatureCallbacks Callbacks { get; }
        internal FeatureRuntimeServices Runtime { get; } = new FeatureRuntimeServices();
        internal WebAgentMotionFocusFeature _webAgentMotionFocusFeature { get; }
        internal WebAgentEquipmentFeature _webAgentEquipmentFeature { get; }
        internal PanoramaReferenceFeature _panoramaReferenceFeature { get; }
        internal PanoramaPresetsFeature _panoramaPresetsFeature { get; }
        internal SpecialFeaturesFeature _specialFeaturesFeature { get; }
        internal PresetLensFeature _presetLensFeature { get; }
        internal InputCoordinatorFeature _inputCoordinatorFeature { get; }
        internal CameraFeatureControlFeature _cameraFeatureControlFeature { get; }
        internal OpticalCenterFeature _opticalCenterFeature { get; }
        internal ScanTraversalFeature _scanTraversalFeature { get; }
        internal RoutesFeature _routesFeature { get; }
        internal ZonesFeature _zonesFeature { get; }
        internal AiFeature _aiFeature { get; }
        internal ContinuousControlFeature _continuousControlFeature { get; }
        internal ControlAgentFeature _controlAgentFeature { get; }
        internal WebAgentProtocolFeature _webAgentProtocolFeature { get; }
        internal FireEventsFeature _fireEventsFeature { get; }
        internal EventAlertsFeature _eventAlertsFeature { get; }
        internal MoveControlFeature _moveControlFeature { get; }
        internal PanoramaFeature _panoramaFeature { get; }
        internal PositionFeature _positionFeature { get; }
        internal PresetStorageFeature _presetStorageFeature { get; }
        internal PropertiesFeature _propertiesFeature { get; }
        internal RtspSettingsFeature _rtspSettingsFeature { get; }
        internal RpySettingsFeature _rpySettingsFeature { get; }
        internal GpsSettingsFeature _gpsSettingsFeature { get; }
        internal DevicePowerFeature _devicePowerFeature { get; }
        internal SystemDateTimeFeature _systemDateTimeFeature { get; }
        internal SmokeFeature _smokeFeature { get; }
        internal SyncFeature _syncFeature { get; }
        internal ThermalFeature _thermalFeature { get; }
        internal VideoFeature _videoFeature { get; }
        internal ScreenStateFeature _screenStateFeature { get; }

        // 기능 객체를 만드는 순서는 이 구성 지점에서 관리한다.
        internal FeatureComposition(FeatureCallbacks callbacks)
        {
            Callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            // Context는 생성 시 연결 대상만 보관하고 아직 다른 기능을 호출하지 않는다.
            // 전체 기능을 모두 만든 뒤 MainViewModel이 기존 순서로 상태·명령·이벤트를 초기화한다.
            _webAgentMotionFocusFeature = new WebAgentMotionFocusFeature(new WebAgentMotionFocusFeatureContext(this));
            _webAgentEquipmentFeature = new WebAgentEquipmentFeature(new WebAgentEquipmentFeatureContext(this));
            _panoramaReferenceFeature = new PanoramaReferenceFeature(new PanoramaReferenceFeatureContext(this));
            _panoramaPresetsFeature = new PanoramaPresetsFeature(new PanoramaPresetsFeatureContext(this));
            _specialFeaturesFeature = new SpecialFeaturesFeature(new SpecialFeaturesFeatureContext(this));
            _presetLensFeature = new PresetLensFeature(new PresetLensFeatureContext(this));
            _inputCoordinatorFeature = new InputCoordinatorFeature(new InputCoordinatorFeatureContext(this));
            _cameraFeatureControlFeature = new CameraFeatureControlFeature(new CameraFeatureControlFeatureContext(this));
            _opticalCenterFeature = new OpticalCenterFeature(new OpticalCenterFeatureContext(this));
            _scanTraversalFeature = new ScanTraversalFeature(new ScanTraversalFeatureContext(this));
            _routesFeature = new RoutesFeature(new RoutesFeatureContext(this));
            _zonesFeature = new ZonesFeature(new ZonesFeatureContext(this));
            _aiFeature = new AiFeature(new AiFeatureContext(this));
            _continuousControlFeature = new ContinuousControlFeature(new ContinuousControlFeatureContext(this));
            _controlAgentFeature = new ControlAgentFeature(new ControlAgentFeatureContext(this));
            _webAgentProtocolFeature = new WebAgentProtocolFeature(new WebAgentProtocolFeatureContext(this));
            _fireEventsFeature = new FireEventsFeature(new FireEventsFeatureContext(this));
            _eventAlertsFeature = new EventAlertsFeature(new EventAlertsFeatureContext(this));
            _moveControlFeature = new MoveControlFeature(new MoveControlFeatureContext(this));
            _panoramaFeature = new PanoramaFeature(new PanoramaFeatureContext(this));
            _positionFeature = new PositionFeature(new PositionFeatureContext(this));
            _presetStorageFeature = new PresetStorageFeature(new PresetStorageFeatureContext(this));
            _propertiesFeature = new PropertiesFeature(new PropertiesFeatureContext(this));
            _rtspSettingsFeature = new RtspSettingsFeature(new RtspSettingsFeatureContext(this));
            _rpySettingsFeature = new RpySettingsFeature(new RpySettingsFeatureContext(this));
            _gpsSettingsFeature = new GpsSettingsFeature(new GpsSettingsFeatureContext(this));
            _devicePowerFeature = new DevicePowerFeature(new DevicePowerFeatureContext(this));
            _systemDateTimeFeature = new SystemDateTimeFeature(new SystemDateTimeFeatureContext(this));
            _smokeFeature = new SmokeFeature(new SmokeFeatureContext(this));
            _syncFeature = new SyncFeature(new SyncFeatureContext(this));
            _thermalFeature = new ThermalFeature(new ThermalFeatureContext(this));
            _videoFeature = new VideoFeature(new VideoFeatureContext(this));
            _screenStateFeature = new ScreenStateFeature(new ScreenStateFeatureContext(this));
        }
    }
}
