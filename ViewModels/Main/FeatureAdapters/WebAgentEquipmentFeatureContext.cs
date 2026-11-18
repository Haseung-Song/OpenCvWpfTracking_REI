using System;
using OpenCvWpfTracking.Services.Communication.WebAgent;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class WebAgentEquipmentFeatureContext : IWebAgentEquipmentFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal WebAgentEquipmentFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        string IWebAgentEquipmentFeatureContext._connectionSessionId
        {
            get { return _features._devicePowerFeature._connectionSessionId; }
            set { _features._devicePowerFeature._connectionSessionId = value; }
        }

        Services.Communication.IControlCommandServicePort IWebAgentEquipmentFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        byte IWebAgentEquipmentFeatureContext._currentScbPowerStatus
        {
            get { return _features._devicePowerFeature._currentScbPowerStatus; }
            set { _features._devicePowerFeature._currentScbPowerStatus = value; }
        }

        bool IWebAgentEquipmentFeatureContext._hasDevicePowerStatus
        {
            get { return _features._devicePowerFeature._hasDevicePowerStatus; }
            set { _features._devicePowerFeature._hasDevicePowerStatus = value; }
        }

        bool IWebAgentEquipmentFeatureContext._isDevicePowerStatusStale
        {
            get { return _features._devicePowerFeature._isDevicePowerStatusStale; }
            set { _features._devicePowerFeature._isDevicePowerStatusStale = value; }
        }

        IWebAgentThermalPaletteServicePort IWebAgentEquipmentFeatureContext._webAgentThermalPaletteService
        {
            get { return _features.Runtime._webAgentThermalPaletteService; }
        }

        bool IWebAgentEquipmentFeatureContext.CameraFeatureTransportReady
        {
            get { return _features._cameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        void IWebAgentEquipmentFeatureContext.DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply)
        {
            _features._specialFeaturesFeature.DispatchSpecialFeaturePacket(p, apply);
        }

        void IWebAgentEquipmentFeatureContext.EnsureSpecialFeatures()
        {
            _features._specialFeaturesFeature.EnsureSpecialFeatures();
        }

        bool IWebAgentEquipmentFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IWebAgentEquipmentFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IWebAgentEquipmentFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features._rtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        bool IWebAgentEquipmentFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        void IWebAgentEquipmentFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        void IWebAgentEquipmentFeatureContext.ResetThermalPaletteButtonVisuals()
        {
            _features._thermalFeature.ResetThermalPaletteButtonVisuals();
        }

        System.Collections.Generic.IEnumerable<Models.Main.CameraSpecialFeatureRow> IWebAgentEquipmentFeatureContext.SpecialFeatureRows
        {
            get { return _features._specialFeaturesFeature.SpecialFeatureRows; }
        }

        bool IWebAgentEquipmentFeatureContext.TryReadTelemetryTime(ulong milliseconds, out DateTimeOffset value, bool requireFresh)
        {
            return _features._webAgentProtocolFeature.TryReadTelemetryTime(milliseconds, out value, requireFresh);
        }

        Action IWebAgentEquipmentFeatureContext.QueryEquipmentTemperatureCallback
        {
            get { return _features.Callbacks.QueryEquipmentTemperatureCallback; }
        }
    }
}
