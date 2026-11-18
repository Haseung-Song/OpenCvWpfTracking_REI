using OpenCvWpfTracking.Models.Main;
using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class PresetStorageFeatureContext : IPresetStorageFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PresetStorageFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        bool IPresetStorageFeatureContext._isLoadingPresetStorage
        {
            get { return _features._screenStateFeature._isLoadingPresetStorage; }
            set { _features._screenStateFeature._isLoadingPresetStorage = value; }
        }

        bool IPresetStorageFeatureContext._isLoadingRtspCommunicationSettings
        {
            get { return _features._rtspSettingsFeature._isLoadingRtspCommunicationSettings; }
            set { _features._rtspSettingsFeature._isLoadingRtspCommunicationSettings = value; }
        }

        string IPresetStorageFeatureContext._laPresetCommandStatusText
        {
            get { return _features._screenStateFeature._laPresetCommandStatusText; }
            set { _features._screenStateFeature._laPresetCommandStatusText = value; }
        }

        int IPresetStorageFeatureContext._laPresetScanDelay
        {
            get { return _features._screenStateFeature._laPresetScanDelay; }
            set { _features._screenStateFeature._laPresetScanDelay = value; }
        }

        int IPresetStorageFeatureContext._laPresetScanSpeed
        {
            get { return _features._screenStateFeature._laPresetScanSpeed; }
            set { _features._screenStateFeature._laPresetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext._laPresetSlotNumber
        {
            get { return _features._screenStateFeature._laPresetSlotNumber; }
            set { _features._screenStateFeature._laPresetSlotNumber = value; }
        }

        string IPresetStorageFeatureContext._presetCommandStatusText
        {
            get { return _features._screenStateFeature._presetCommandStatusText; }
            set { _features._screenStateFeature._presetCommandStatusText = value; }
        }

        int IPresetStorageFeatureContext._presetScanDelay
        {
            get { return _features._screenStateFeature._presetScanDelay; }
            set { _features._screenStateFeature._presetScanDelay = value; }
        }

        PresetScanOrderMode IPresetStorageFeatureContext._presetScanOrderMode
        {
            get { return _features._screenStateFeature._presetScanOrderMode; }
            set { _features._screenStateFeature._presetScanOrderMode = value; }
        }

        int IPresetStorageFeatureContext._presetScanSpeed
        {
            get { return _features._screenStateFeature._presetScanSpeed; }
            set { _features._screenStateFeature._presetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext._presetSlotNumber
        {
            get { return _features._screenStateFeature._presetSlotNumber; }
            set { _features._screenStateFeature._presetSlotNumber = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<ControlAgentProfileOption> IPresetStorageFeatureContext.ControlAgentProfiles
        {
            get { return _features._rtspSettingsFeature.ControlAgentProfiles; }
        }

        string IPresetStorageFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        void IPresetStorageFeatureContext.InvalidatePanoramaReference()
        {
            _features._opticalCenterFeature.InvalidatePanoramaReference();
        }

        string IPresetStorageFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetNearestOrderMode
        {
            get { return _features._propertiesFeature.IsPresetNearestOrderMode; }
            set { _features._propertiesFeature.IsPresetNearestOrderMode = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetPanOrderMode
        {
            get { return _features._scanTraversalFeature.IsPresetPanOrderMode; }
            set { _features._scanTraversalFeature.IsPresetPanOrderMode = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetPingPong
        {
            get { return _features._scanTraversalFeature.IsPresetPingPong; }
            set { _features._scanTraversalFeature.IsPresetPingPong = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetSavedOrderMode
        {
            get { return _features._propertiesFeature.IsPresetSavedOrderMode; }
            set { _features._propertiesFeature.IsPresetSavedOrderMode = value; }
        }

        bool IPresetStorageFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        string IPresetStorageFeatureContext.LaPresetCommandStatusText
        {
            get { return _features._propertiesFeature.LaPresetCommandStatusText; }
            set { _features._propertiesFeature.LaPresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPresetStorageFeatureContext.LaPresetPoints
        {
            get { return _features._propertiesFeature.LaPresetPoints; }
        }

        int IPresetStorageFeatureContext.LaPresetScanDelay
        {
            get { return _features._propertiesFeature.LaPresetScanDelay; }
            set { _features._propertiesFeature.LaPresetScanDelay = value; }
        }

        int IPresetStorageFeatureContext.LaPresetScanSpeed
        {
            get { return _features._propertiesFeature.LaPresetScanSpeed; }
            set { _features._propertiesFeature.LaPresetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext.LaPresetSlotNumber
        {
            get { return _features._propertiesFeature.LaPresetSlotNumber; }
            set { _features._propertiesFeature.LaPresetSlotNumber = value; }
        }

        void IPresetStorageFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IPresetStorageFeatureContext.PanoramaDeviceKey
        {
            get { return _features._opticalCenterFeature.PanoramaDeviceKey; }
        }

        string IPresetStorageFeatureContext.PresetCommandStatusText
        {
            get { return _features._propertiesFeature.PresetCommandStatusText; }
            set { _features._propertiesFeature.PresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPresetStorageFeatureContext.PresetPoints
        {
            get { return _features._propertiesFeature.PresetPoints; }
        }

        int IPresetStorageFeatureContext.PresetScanDelay
        {
            get { return _features._propertiesFeature.PresetScanDelay; }
            set { _features._propertiesFeature.PresetScanDelay = value; }
        }

        int IPresetStorageFeatureContext.PresetScanSpeed
        {
            get { return _features._propertiesFeature.PresetScanSpeed; }
            set { _features._propertiesFeature.PresetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext.PresetSlotNumber
        {
            get { return _features._propertiesFeature.PresetSlotNumber; }
            set { _features._propertiesFeature.PresetSlotNumber = value; }
        }

        void IPresetStorageFeatureContext.RecoverPanoramaPresetReference()
        {
            _features._panoramaReferenceFeature.RecoverPanoramaPresetReference();
        }

        ControlAgentProfileOption IPresetStorageFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        PresetPointOption IPresetStorageFeatureContext.SelectedLaPresetPoint
        {
            get { return _features._propertiesFeature.SelectedLaPresetPoint; }
            set { _features._propertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IPresetStorageFeatureContext.SelectedPresetPoint
        {
            get { return _features._propertiesFeature.SelectedPresetPoint; }
            set { _features._propertiesFeature.SelectedPresetPoint = value; }
        }
    }
}
