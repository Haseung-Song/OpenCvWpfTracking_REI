using OpenCvWpfTracking.Services.Communication;
using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class DevicePowerFeatureContext : IDevicePowerFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal DevicePowerFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        IControlCommandServicePort IDevicePowerFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        string IDevicePowerFeatureContext.EoStatusText
        {
            get { return _features.ScreenStateFeature._eoStatusText; }
            set { _features.ScreenStateFeature._eoStatusText = value; }
        }

        string IDevicePowerFeatureContext.IrStatusText
        {
            get { return _features.ScreenStateFeature._irStatusText; }
            set { _features.ScreenStateFeature._irStatusText = value; }
        }

        ITcpClientServicePort IDevicePowerFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ControlAgentProfileOption IDevicePowerFeatureContext.RequestedControlProfile
        {
            get { return _features.RtspSettingsFeature._requestedControlProfile; }
            set { _features.RtspSettingsFeature._requestedControlProfile = value; }
        }

        string IDevicePowerFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features.PropertiesFeature.ControlAgentConnectionStatusText; }
            set { _features.PropertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IDevicePowerFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string IDevicePowerFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        string IDevicePowerFeatureContext.CurrentControlPowerText
        {
            get { return _features.PropertiesFeature.CurrentControlPowerText; }
        }

        string IDevicePowerFeatureContext.CurrentEoPowerText
        {
            get { return _features.PropertiesFeature.CurrentEoPowerText; }
        }

        string IDevicePowerFeatureContext.CurrentIrPowerText
        {
            get { return _features.PropertiesFeature.CurrentIrPowerText; }
        }

        string IDevicePowerFeatureContext.CurrentPowerText
        {
            get { return _features.PropertiesFeature.CurrentPowerText; }
        }

        string IDevicePowerFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        string IDevicePowerFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        bool IDevicePowerFeatureContext.IsGpsTabVisible
        {
            get { return _features.RtspSettingsFeature.IsGpsTabVisible; }
        }

        bool IDevicePowerFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        bool IDevicePowerFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features.RtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IDevicePowerFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// 원래 표본 시각으로 경과 시간을 계산하여 캐시를 최신값으로 위장하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IDevicePowerFeatureContext.RefreshWebAgentEquipmentState()
        {
            _features.WebAgentEquipmentFeature.RefreshWebAgentEquipmentState();
        }

        /// <summary>
        /// 연결마다 이전 캐시 시각과 기능 협상 결과를 비운다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IDevicePowerFeatureContext.ResetWebAgentTelemetrySession()
        {
            _features.WebAgentProtocolFeature.ResetWebAgentTelemetrySession();
        }

        ControlAgentProfileOption IDevicePowerFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        string IDevicePowerFeatureContext.SessionControlIp
        {
            get { return _features.RtspSettingsFeature.SessionControlIp; }
        }

        string IDevicePowerFeatureContext.SessionControlPort
        {
            get { return _features.RtspSettingsFeature.SessionControlPort; }
        }

        EventHandler IDevicePowerFeatureContext.HandleDevicePowerFreshnessTickCallback
        {
            get { return _features.Callbacks.HandleDevicePowerFreshnessTickCallback; }
        }
    }
}
