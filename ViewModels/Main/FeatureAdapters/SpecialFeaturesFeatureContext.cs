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

        string ISpecialFeaturesFeatureContext.ConnectionSessionId
        {
            get { return _features.DevicePowerFeature._connectionSessionId; }
            set { _features.DevicePowerFeature._connectionSessionId = value; }
        }

        Services.Communication.IControlCommandServicePort ISpecialFeaturesFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        IWebAgentThermalPaletteServicePort ISpecialFeaturesFeatureContext.WebAgentThermalPaletteService
        {
            get { return _features.Runtime._webAgentThermalPaletteService; }
        }

        bool ISpecialFeaturesFeatureContext.CameraFeatureTransportReady
        {
            get { return _features.CameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        string ISpecialFeaturesFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string ISpecialFeaturesFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        bool ISpecialFeaturesFeatureContext.IsControlInputLocked
        {
            get { return _features.PropertiesFeature.IsControlInputLocked; }
        }

        bool ISpecialFeaturesFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        bool ISpecialFeaturesFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features.RtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        bool ISpecialFeaturesFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ISpecialFeaturesFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// 2026-08-27: 카메라 이동 또는 영상 조건 변경 뒤 EO/IR 기준 프레임과 시간축 후보를 함께 초기화하여 화면 전체 변화가 이벤트가
        /// 되지 않게 한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ISpecialFeaturesFeatureContext.ResetFireSmokeFrameAnalysis(string reason)
        {
            _features.SmokeFeature.ResetFireSmokeFrameAnalysis(reason);
        }

        ControlAgentProfileOption ISpecialFeaturesFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        /// <summary>
        /// 운용 잠금 검증 후 BOOLEAN 명령을 송신한다. 송신 성공을 장비 적용 확인으로 취급하지 않는다. 실제 처리는 연결된 기능 객체에
        /// 위임한다.
        /// </summary>
        bool ISpecialFeaturesFeatureContext.SetDigitalZoom(bool ir, bool on)
        {
            return _features.CameraFeatureControlFeature.SetDigitalZoom(ir, on);
        }

        /// <summary>
        /// 현재 열상 팔레트에 맞춰 선택 버튼의 강조 상태를 갱신한다.
        /// </summary>
        void ISpecialFeaturesFeatureContext.UpdateThermalPaletteButtonVisual(int paletteType)
        {
            _features.ThermalFeature.UpdateThermalPaletteButtonVisual(paletteType);
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
