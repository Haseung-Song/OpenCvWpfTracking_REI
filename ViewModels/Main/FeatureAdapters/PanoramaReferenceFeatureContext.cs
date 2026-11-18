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

        Services.Communication.IControlCommandServicePort IPanoramaReferenceFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ref global::System.Tuple<double, double, long> IPanoramaReferenceFeatureContext._videoCenterPose
        {
            get { return ref _features._cameraFeatureControlFeature._videoCenterPose; }
        }

        void IPanoramaReferenceFeatureContext.ApplyZoneReferenceRebase(PanoramaZoneRebase operation)
        {
            _features._zonesFeature.ApplyZoneReferenceRebase(operation);
        }

        void IPanoramaReferenceFeatureContext.BeginZoneReferenceChange()
        {
            _features._zonesFeature.BeginZoneReferenceChange();
        }

        void IPanoramaReferenceFeatureContext.FinishZoneReferenceChange(bool notifyEditor)
        {
            _features._zonesFeature.FinishZoneReferenceChange(notifyEditor);
        }

        PresetPointOption[] IPanoramaReferenceFeatureContext.GetPanoramaPresets()
        {
            return _features._panoramaPresetsFeature.GetPanoramaPresets();
        }

        string IPanoramaReferenceFeatureContext.HomeZeroStatusText
        {
            get { return _features._propertiesFeature.HomeZeroStatusText; }
            set { _features._propertiesFeature.HomeZeroStatusText = value; }
        }

        bool IPanoramaReferenceFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaReferenceFeatureContext.LaPresetPoints
        {
            get { return _features._propertiesFeature.LaPresetPoints; }
        }

        bool IPanoramaReferenceFeatureContext.PanoramaPoseFresh
        {
            get { return _features._opticalCenterFeature.PanoramaPoseFresh; }
        }

        string IPanoramaReferenceFeatureContext.PanoramaPresetContext
        {
            get { return _features._panoramaPresetsFeature.PanoramaPresetContext; }
        }

        string IPanoramaReferenceFeatureContext.PanoramaPresetContextHash
        {
            get { return _features._panoramaPresetsFeature.PanoramaPresetContextHash; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPanoramaReferenceFeatureContext.PresetPoints
        {
            get { return _features._propertiesFeature.PresetPoints; }
        }

        bool IPanoramaReferenceFeatureContext.SavePresetStorage()
        {
            return _features._presetStorageFeature.SavePresetStorage();
        }

        PresetPointOption IPanoramaReferenceFeatureContext.SelectedLaPresetPoint
        {
            get { return _features._propertiesFeature.SelectedLaPresetPoint; }
            set { _features._propertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IPanoramaReferenceFeatureContext.SelectedPresetPoint
        {
            get { return _features._propertiesFeature.SelectedPresetPoint; }
            set { _features._propertiesFeature.SelectedPresetPoint = value; }
        }

        string IPanoramaReferenceFeatureContext.ZoneEquipmentKey
        {
            get { return _features._zonesFeature.ZoneEquipmentKey; }
        }
    }
}
