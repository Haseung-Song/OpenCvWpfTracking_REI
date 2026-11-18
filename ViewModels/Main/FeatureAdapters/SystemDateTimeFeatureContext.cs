using OpenCvWpfTracking.Services.Communication;
using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class SystemDateTimeFeatureContext : ISystemDateTimeFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal SystemDateTimeFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        IControlCommandServicePort ISystemDateTimeFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ITcpClientServicePort ISystemDateTimeFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        TelemetryClock ISystemDateTimeFeatureContext._webAgentTelemetryClock
        {
            get { return _features._webAgentProtocolFeature._webAgentTelemetryClock; }
        }

        bool ISystemDateTimeFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool ISystemDateTimeFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool ISystemDateTimeFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        void ISystemDateTimeFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        int ISystemDateTimeFeatureContext.SelectedCommunicationSettingsTabIndex
        {
            get { return _features._rtspSettingsFeature.SelectedCommunicationSettingsTabIndex; }
            set { _features._rtspSettingsFeature.SelectedCommunicationSettingsTabIndex = value; }
        }

        Action ISystemDateTimeFeatureContext.RequestSystemDateTimeCommandCallback
        {
            get { return _features.Callbacks.RequestSystemDateTimeCommandCallback; }
        }

        Action ISystemDateTimeFeatureContext.SetSystemDateTimeFromPcCallback
        {
            get { return _features.Callbacks.SetSystemDateTimeFromPcCallback; }
        }

        EventHandler ISystemDateTimeFeatureContext.HandleSystemTimePollTickCallback
        {
            get { return _features.Callbacks.HandleSystemTimePollTickCallback; }
        }

        Action ISystemDateTimeFeatureContext.RefreshSystemDateTimeAfterSetCallback
        {
            get { return _features.Callbacks.RefreshSystemDateTimeAfterSetCallback; }
        }
    }
}
