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
        internal WebAgentMotionFocusFeature WebAgentMotionFocusFeature { get; }
        internal WebAgentEquipmentFeature WebAgentEquipmentFeature { get; }
        internal PanoramaReferenceFeature PanoramaReferenceFeature { get; }
        internal PanoramaPresetsFeature PanoramaPresetsFeature { get; }
        internal SpecialFeaturesFeature SpecialFeaturesFeature { get; }
        internal PresetLensFeature PresetLensFeature { get; }
        internal InputCoordinatorFeature InputCoordinatorFeature { get; }
        internal CameraFeatureControlFeature CameraFeatureControlFeature { get; }
        internal OpticalCenterFeature OpticalCenterFeature { get; }
        internal ScanTraversalFeature ScanTraversalFeature { get; }
        internal RoutesFeature RoutesFeature { get; }
        internal ZonesFeature ZonesFeature { get; }
        internal AiFeature AiFeature { get; }
        internal ContinuousControlFeature ContinuousControlFeature { get; }
        internal ControlAgentFeature ControlAgentFeature { get; }
        internal WebAgentProtocolFeature WebAgentProtocolFeature { get; }
        internal FireEventsFeature FireEventsFeature { get; }
        internal EventAlertsFeature EventAlertsFeature { get; }
        internal MoveControlFeature MoveControlFeature { get; }
        internal PanoramaFeature PanoramaFeature { get; }
        internal PositionFeature PositionFeature { get; }
        internal PresetStorageFeature PresetStorageFeature { get; }
        internal PropertiesFeature PropertiesFeature { get; }
        internal RtspSettingsFeature RtspSettingsFeature { get; }
        internal RpySettingsFeature RpySettingsFeature { get; }
        internal GpsSettingsFeature GpsSettingsFeature { get; }
        internal DevicePowerFeature DevicePowerFeature { get; }
        internal SystemDateTimeFeature SystemDateTimeFeature { get; }
        internal SmokeFeature SmokeFeature { get; }
        internal SyncFeature SyncFeature { get; }
        internal ThermalFeature ThermalFeature { get; }
        internal VideoFeature VideoFeature { get; }
        internal ScreenStateFeature ScreenStateFeature { get; }

        // 기능 객체를 만드는 순서는 이 구성 지점에서 관리한다.
        internal FeatureComposition(FeatureCallbacks callbacks)
        {
            Callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            // Context는 생성 시 연결 대상만 보관하고 아직 다른 기능을 호출하지 않는다.
            // 전체 기능을 모두 만든 뒤 MainViewModel이 기존 순서로 상태·명령·이벤트를 초기화한다.
            WebAgentMotionFocusFeature = new WebAgentMotionFocusFeature(new WebAgentMotionFocusFeatureContext(this));
            WebAgentEquipmentFeature = new WebAgentEquipmentFeature(new WebAgentEquipmentFeatureContext(this));
            PanoramaReferenceFeature = new PanoramaReferenceFeature(new PanoramaReferenceFeatureContext(this));
            PanoramaPresetsFeature = new PanoramaPresetsFeature(new PanoramaPresetsFeatureContext(this));
            SpecialFeaturesFeature = new SpecialFeaturesFeature(new SpecialFeaturesFeatureContext(this));
            PresetLensFeature = new PresetLensFeature(new PresetLensFeatureContext(this));
            InputCoordinatorFeature = new InputCoordinatorFeature(new InputCoordinatorFeatureContext(this));
            CameraFeatureControlFeature = new CameraFeatureControlFeature(new CameraFeatureControlFeatureContext(this));
            OpticalCenterFeature = new OpticalCenterFeature(new OpticalCenterFeatureContext(this));
            ScanTraversalFeature = new ScanTraversalFeature(new ScanTraversalFeatureContext(this));
            RoutesFeature = new RoutesFeature(new RoutesFeatureContext(this));
            ZonesFeature = new ZonesFeature(new ZonesFeatureContext(this));
            AiFeature = new AiFeature(new AiFeatureContext(this));
            ContinuousControlFeature = new ContinuousControlFeature(new ContinuousControlFeatureContext(this));
            ControlAgentFeature = new ControlAgentFeature(new ControlAgentFeatureContext(this));
            WebAgentProtocolFeature = new WebAgentProtocolFeature(new WebAgentProtocolFeatureContext(this));
            FireEventsFeature = new FireEventsFeature(new FireEventsFeatureContext(this));
            EventAlertsFeature = new EventAlertsFeature(new EventAlertsFeatureContext(this));
            MoveControlFeature = new MoveControlFeature(new MoveControlFeatureContext(this));
            PanoramaFeature = new PanoramaFeature(new PanoramaFeatureContext(this));
            PositionFeature = new PositionFeature(new PositionFeatureContext(this));
            PresetStorageFeature = new PresetStorageFeature(new PresetStorageFeatureContext(this));
            PropertiesFeature = new PropertiesFeature(new PropertiesFeatureContext(this));
            RtspSettingsFeature = new RtspSettingsFeature(new RtspSettingsFeatureContext(this));
            RpySettingsFeature = new RpySettingsFeature(new RpySettingsFeatureContext(this));
            GpsSettingsFeature = new GpsSettingsFeature(new GpsSettingsFeatureContext(this));
            DevicePowerFeature = new DevicePowerFeature(new DevicePowerFeatureContext(this));
            SystemDateTimeFeature = new SystemDateTimeFeature(new SystemDateTimeFeatureContext(this));
            SmokeFeature = new SmokeFeature(new SmokeFeatureContext(this));
            SyncFeature = new SyncFeature(new SyncFeatureContext(this));
            ThermalFeature = new ThermalFeature(new ThermalFeatureContext(this));
            VideoFeature = new VideoFeature(new VideoFeatureContext(this));
            ScreenStateFeature = new ScreenStateFeature(new ScreenStateFeatureContext(this));
        }
    }
}
