using System;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Communication.WebAgent;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class SpecialFeaturesFeatureContext : ISpecialFeaturesFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal SpecialFeaturesFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        string ISpecialFeaturesFeatureContext._connectionSessionId
        {
            get { return _features._devicePowerFeature._connectionSessionId; }
            set { _features._devicePowerFeature._connectionSessionId = value; }
        }

        Services.Communication.IControlCommandServicePort ISpecialFeaturesFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        IWebAgentThermalPaletteServicePort ISpecialFeaturesFeatureContext._webAgentThermalPaletteService
        {
            get { return _features.Runtime._webAgentThermalPaletteService; }
        }

        bool ISpecialFeaturesFeatureContext.CameraFeatureTransportReady
        {
            get { return _features._cameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        string ISpecialFeaturesFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string ISpecialFeaturesFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        bool ISpecialFeaturesFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool ISpecialFeaturesFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool ISpecialFeaturesFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features._rtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        bool ISpecialFeaturesFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        void ISpecialFeaturesFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        void ISpecialFeaturesFeatureContext.ResetFireSmokeFrameAnalysis(string reason)
        {
            _features._smokeFeature.ResetFireSmokeFrameAnalysis(reason);
        }

        ControlAgentProfileOption ISpecialFeaturesFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        bool ISpecialFeaturesFeatureContext.SetDigitalZoom(bool ir, bool on)
        {
            return _features._cameraFeatureControlFeature.SetDigitalZoom(ir, on);
        }

        void ISpecialFeaturesFeatureContext.UpdateThermalPaletteButtonVisual(int paletteType)
        {
            _features._thermalFeature.UpdateThermalPaletteButtonVisual(paletteType);
        }

        Action ISpecialFeaturesFeatureContext.QuerySpecialFeatureSupportCallback
        {
            get { return _features.Callbacks.QuerySpecialFeatureSupportCallback; }
        }

        Action<CameraSpecialFeatureRow, int> ISpecialFeaturesFeatureContext.SendSpecialFeatureCallback
        {
            get { return _features.Callbacks.SendSpecialFeatureCallback; }
        }

        Action<CameraSpecialFeatureRow> ISpecialFeaturesFeatureContext.QuerySpecialFeatureStateCallback
        {
            get { return _features.Callbacks.QuerySpecialFeatureStateCallback; }
        }
    }
}
