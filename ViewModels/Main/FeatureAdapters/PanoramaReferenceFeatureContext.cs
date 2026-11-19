using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Video;
using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class PanoramaReferenceFeatureContext : IPanoramaReferenceFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PanoramaReferenceFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Services.Communication.IControlCommandServicePort IPanoramaReferenceFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ref global::System.Tuple<double, double, long> IPanoramaReferenceFeatureContext.VideoCenterPose
        {
            get { return ref _features.CameraFeatureControlFeature._videoCenterPose; }
        }

        /// <summary>
        /// 변환과 적용 번호를 같은 XML에 확정해 재시작 시 중복 보정을 방지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPanoramaReferenceFeatureContext.ApplyZoneReferenceRebase(PanoramaZoneRebase operation)
        {
            _features.ZonesFeature.ApplyZoneReferenceRebase(operation);
        }

        /// <summary>
        /// ZERO 확인 전에는 구역을 지우지 않고 과거 프레임·정합 결과의 적용만 중지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPanoramaReferenceFeatureContext.BeginZoneReferenceChange()
        {
            _features.ZonesFeature.BeginZoneReferenceChange();
        }

        /// <summary>
        /// 장면 기준 변경 후 구역 참조와 표시 상태를 마무리한다.
        /// </summary>
        void IPanoramaReferenceFeatureContext.FinishZoneReferenceChange(bool notifyEditor)
        {
            _features.ZonesFeature.FinishZoneReferenceChange(notifyEditor);
        }

        /// <summary>
        /// 현재 연결 장비에 해당하는 기존 프리셋만 번호 순서로 복사한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        PresetPointOption[] IPanoramaReferenceFeatureContext.GetPanoramaPresets()
        {
            return _features.PanoramaPresetsFeature.GetPanoramaPresets();
        }

        string IPanoramaReferenceFeatureContext.HomeZeroStatusText
        {
            get { return _features.PropertiesFeature.HomeZeroStatusText; }
            set { _features.PropertiesFeature.HomeZeroStatusText = value; }
        }

        bool IPanoramaReferenceFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaReferenceFeatureContext.LaPresetPoints
        {
            get { return _features.PropertiesFeature.LaPresetPoints; }
        }

        bool IPanoramaReferenceFeatureContext.PanoramaPoseFresh
        {
            get { return _features.OpticalCenterFeature.PanoramaPoseFresh; }
        }

        string IPanoramaReferenceFeatureContext.PanoramaPresetContext
        {
            get { return _features.PanoramaPresetsFeature.PanoramaPresetContext; }
        }

        string IPanoramaReferenceFeatureContext.PanoramaPresetContextHash
        {
            get { return _features.PanoramaPresetsFeature.PanoramaPresetContextHash; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaReferenceFeatureContext.PresetPoints
        {
            get { return _features.PropertiesFeature.PresetPoints; }
        }

        /// <summary>
        /// 현재 프리셋과 순서 정보를 저장하며 기존 파일 보존 규칙을 적용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IPanoramaReferenceFeatureContext.SavePresetStorage()
        {
            return _features.PresetStorageFeature.SavePresetStorage();
        }

        PresetPointOption IPanoramaReferenceFeatureContext.SelectedLaPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedLaPresetPoint; }
            set { _features.PropertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IPanoramaReferenceFeatureContext.SelectedPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedPresetPoint; }
            set { _features.PropertiesFeature.SelectedPresetPoint = value; }
        }

        string IPanoramaReferenceFeatureContext.ZoneEquipmentKey
        {
            get { return _features.ZonesFeature.ZoneEquipmentKey; }
        }
    }
}
