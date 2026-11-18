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

        IControlCommandServicePort IDevicePowerFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        string IDevicePowerFeatureContext._eoStatusText
        {
            get { return _features._screenStateFeature._eoStatusText; }
            set { _features._screenStateFeature._eoStatusText = value; }
        }

        string IDevicePowerFeatureContext._irStatusText
        {
            get { return _features._screenStateFeature._irStatusText; }
            set { _features._screenStateFeature._irStatusText = value; }
        }

        ITcpClientServicePort IDevicePowerFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ControlAgentProfileOption IDevicePowerFeatureContext._requestedControlProfile
        {
            get { return _features._rtspSettingsFeature._requestedControlProfile; }
            set { _features._rtspSettingsFeature._requestedControlProfile = value; }
        }

        string IDevicePowerFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features._propertiesFeature.ControlAgentConnectionStatusText; }
            set { _features._propertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IDevicePowerFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IDevicePowerFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        string IDevicePowerFeatureContext.CurrentControlPowerText
        {
            get { return _features._propertiesFeature.CurrentControlPowerText; }
        }

        string IDevicePowerFeatureContext.CurrentEoPowerText
        {
            get { return _features._propertiesFeature.CurrentEoPowerText; }
        }

        string IDevicePowerFeatureContext.CurrentIrPowerText
        {
            get { return _features._propertiesFeature.CurrentIrPowerText; }
        }

        string IDevicePowerFeatureContext.CurrentPowerText
        {
            get { return _features._propertiesFeature.CurrentPowerText; }
        }

        string IDevicePowerFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        string IDevicePowerFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        bool IDevicePowerFeatureContext.IsGpsTabVisible
        {
            get { return _features._rtspSettingsFeature.IsGpsTabVisible; }
        }

        bool IDevicePowerFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IDevicePowerFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features._rtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        void IDevicePowerFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        void IDevicePowerFeatureContext.RefreshWebAgentEquipmentState()
        {
            _features._webAgentEquipmentFeature.RefreshWebAgentEquipmentState();
        }

        void IDevicePowerFeatureContext.ResetWebAgentTelemetrySession()
        {
            _features._webAgentProtocolFeature.ResetWebAgentTelemetrySession();
        }

        ControlAgentProfileOption IDevicePowerFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        string IDevicePowerFeatureContext.SessionControlIp
        {
            get { return _features._rtspSettingsFeature.SessionControlIp; }
        }

        string IDevicePowerFeatureContext.SessionControlPort
        {
            get { return _features._rtspSettingsFeature.SessionControlPort; }
        }

        EventHandler IDevicePowerFeatureContext.HandleDevicePowerFreshnessTickCallback
        {
            get { return _features.Callbacks.HandleDevicePowerFreshnessTickCallback; }
        }
    }
}
