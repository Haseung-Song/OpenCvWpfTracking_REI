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

        bool IPresetStorageFeatureContext.IsLoadingPresetStorage
        {
            get { return _features.ScreenStateFeature._isLoadingPresetStorage; }
            set { _features.ScreenStateFeature._isLoadingPresetStorage = value; }
        }

        bool IPresetStorageFeatureContext.IsLoadingRtspCommunicationSettings
        {
            get { return _features.RtspSettingsFeature._isLoadingRtspCommunicationSettings; }
            set { _features.RtspSettingsFeature._isLoadingRtspCommunicationSettings = value; }
        }

        string IPresetStorageFeatureContext.LaPresetCommandStatusTextState
        {
            get { return _features.ScreenStateFeature._laPresetCommandStatusText; }
            set { _features.ScreenStateFeature._laPresetCommandStatusText = value; }
        }

        int IPresetStorageFeatureContext.LaPresetScanDelayState
        {
            get { return _features.ScreenStateFeature._laPresetScanDelay; }
            set { _features.ScreenStateFeature._laPresetScanDelay = value; }
        }

        int IPresetStorageFeatureContext.LaPresetScanSpeedState
        {
            get { return _features.ScreenStateFeature._laPresetScanSpeed; }
            set { _features.ScreenStateFeature._laPresetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext.LaPresetSlotNumberState
        {
            get { return _features.ScreenStateFeature._laPresetSlotNumber; }
            set { _features.ScreenStateFeature._laPresetSlotNumber = value; }
        }

        string IPresetStorageFeatureContext.PresetCommandStatusTextState
        {
            get { return _features.ScreenStateFeature._presetCommandStatusText; }
            set { _features.ScreenStateFeature._presetCommandStatusText = value; }
        }

        int IPresetStorageFeatureContext.PresetScanDelayState
        {
            get { return _features.ScreenStateFeature._presetScanDelay; }
            set { _features.ScreenStateFeature._presetScanDelay = value; }
        }

        PresetScanOrderMode IPresetStorageFeatureContext.PresetScanOrderMode
        {
            get { return _features.ScreenStateFeature._presetScanOrderMode; }
            set { _features.ScreenStateFeature._presetScanOrderMode = value; }
        }

        int IPresetStorageFeatureContext.PresetScanSpeedState
        {
            get { return _features.ScreenStateFeature._presetScanSpeed; }
            set { _features.ScreenStateFeature._presetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext.PresetSlotNumberState
        {
            get { return _features.ScreenStateFeature._presetSlotNumber; }
            set { _features.ScreenStateFeature._presetSlotNumber = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<ControlAgentProfileOption> IPresetStorageFeatureContext.ControlAgentProfiles
        {
            get { return _features.RtspSettingsFeature.ControlAgentProfiles; }
        }

        string IPresetStorageFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        /// <summary>
        /// 위치 기준 변경을 등록된 화면에 알린다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPresetStorageFeatureContext.InvalidatePanoramaReference()
        {
            _features.OpticalCenterFeature.InvalidatePanoramaReference();
        }

        string IPresetStorageFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetNearestOrderMode
        {
            get { return _features.PropertiesFeature.IsPresetNearestOrderMode; }
            set { _features.PropertiesFeature.IsPresetNearestOrderMode = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetPanOrderMode
        {
            get { return _features.ScanTraversalFeature.IsPresetPanOrderMode; }
            set { _features.ScanTraversalFeature.IsPresetPanOrderMode = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetPingPong
        {
            get { return _features.ScanTraversalFeature.IsPresetPingPong; }
            set { _features.ScanTraversalFeature.IsPresetPingPong = value; }
        }

        bool IPresetStorageFeatureContext.IsPresetSavedOrderMode
        {
            get { return _features.PropertiesFeature.IsPresetSavedOrderMode; }
            set { _features.PropertiesFeature.IsPresetSavedOrderMode = value; }
        }

        bool IPresetStorageFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        string IPresetStorageFeatureContext.LaPresetCommandStatusText
        {
            get { return _features.PropertiesFeature.LaPresetCommandStatusText; }
            set { _features.PropertiesFeature.LaPresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPresetStorageFeatureContext.LaPresetPoints
        {
            get { return _features.PropertiesFeature.LaPresetPoints; }
        }

        int IPresetStorageFeatureContext.LaPresetScanDelay
        {
            get { return _features.PropertiesFeature.LaPresetScanDelay; }
            set { _features.PropertiesFeature.LaPresetScanDelay = value; }
        }

        int IPresetStorageFeatureContext.LaPresetScanSpeed
        {
            get { return _features.PropertiesFeature.LaPresetScanSpeed; }
            set { _features.PropertiesFeature.LaPresetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext.LaPresetSlotNumber
        {
            get { return _features.PropertiesFeature.LaPresetSlotNumber; }
            set { _features.PropertiesFeature.LaPresetSlotNumber = value; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPresetStorageFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IPresetStorageFeatureContext.PanoramaDeviceKey
        {
            get { return _features.OpticalCenterFeature.PanoramaDeviceKey; }
        }

        string IPresetStorageFeatureContext.PresetCommandStatusText
        {
            get { return _features.PropertiesFeature.PresetCommandStatusText; }
            set { _features.PropertiesFeature.PresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IPresetStorageFeatureContext.PresetPoints
        {
            get { return _features.PropertiesFeature.PresetPoints; }
        }

        int IPresetStorageFeatureContext.PresetScanDelay
        {
            get { return _features.PropertiesFeature.PresetScanDelay; }
            set { _features.PropertiesFeature.PresetScanDelay = value; }
        }

        int IPresetStorageFeatureContext.PresetScanSpeed
        {
            get { return _features.PropertiesFeature.PresetScanSpeed; }
            set { _features.PropertiesFeature.PresetScanSpeed = value; }
        }

        int IPresetStorageFeatureContext.PresetSlotNumber
        {
            get { return _features.PropertiesFeature.PresetSlotNumber; }
            set { _features.PropertiesFeature.PresetSlotNumber = value; }
        }

        /// <summary>
        /// Config의 변경 기록을 먼저 확정하므로 CSV 저장 중 종료되어도 같은 보정을 한 번만 적용한다. 실제 처리는 연결된 기능 객체에
        /// 위임한다.
        /// </summary>
        void IPresetStorageFeatureContext.RecoverPanoramaPresetReference()
        {
            _features.PanoramaReferenceFeature.RecoverPanoramaPresetReference();
        }

        ControlAgentProfileOption IPresetStorageFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        PresetPointOption IPresetStorageFeatureContext.SelectedLaPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedLaPresetPoint; }
            set { _features.PropertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IPresetStorageFeatureContext.SelectedPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedPresetPoint; }
            set { _features.PropertiesFeature.SelectedPresetPoint = value; }
        }
    }
}
