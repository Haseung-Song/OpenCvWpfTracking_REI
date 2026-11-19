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

        string IWebAgentEquipmentFeatureContext.ConnectionSessionId
        {
            get { return _features.DevicePowerFeature._connectionSessionId; }
            set { _features.DevicePowerFeature._connectionSessionId = value; }
        }

        Services.Communication.IControlCommandServicePort IWebAgentEquipmentFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        byte IWebAgentEquipmentFeatureContext.CurrentScbPowerStatus
        {
            get { return _features.DevicePowerFeature._currentScbPowerStatus; }
            set { _features.DevicePowerFeature._currentScbPowerStatus = value; }
        }

        bool IWebAgentEquipmentFeatureContext.HasDevicePowerStatus
        {
            get { return _features.DevicePowerFeature._hasDevicePowerStatus; }
            set { _features.DevicePowerFeature._hasDevicePowerStatus = value; }
        }

        bool IWebAgentEquipmentFeatureContext.IsDevicePowerStatusStale
        {
            get { return _features.DevicePowerFeature._isDevicePowerStatusStale; }
            set { _features.DevicePowerFeature._isDevicePowerStatusStale = value; }
        }

        IWebAgentThermalPaletteServicePort IWebAgentEquipmentFeatureContext.WebAgentThermalPaletteService
        {
            get { return _features.Runtime._webAgentThermalPaletteService; }
        }

        bool IWebAgentEquipmentFeatureContext.CameraFeatureTransportReady
        {
            get { return _features.CameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        /// <summary>
        /// UI 큐에 넣기 전에 세션을 캡처하고 연결 변경 후 도착한 callback을 버린다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentEquipmentFeatureContext.DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply)
        {
            _features.SpecialFeaturesFeature.DispatchSpecialFeaturePacket(p, apply);
        }

        /// <summary>
        /// 2026-10-14 규격 대상별 행을 한 번 생성한다. 기존 LENS 제어는 이동하거나 제거하지 않는다. 실제 처리는 연결된 기능 객체에
        /// 위임한다.
        /// </summary>
        void IWebAgentEquipmentFeatureContext.EnsureSpecialFeatures()
        {
            _features.SpecialFeaturesFeature.EnsureSpecialFeatures();
        }

        bool IWebAgentEquipmentFeatureContext.IsControlInputLocked
        {
            get { return _features.PropertiesFeature.IsControlInputLocked; }
        }

        bool IWebAgentEquipmentFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        bool IWebAgentEquipmentFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features.RtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        bool IWebAgentEquipmentFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentEquipmentFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// IR 팔레트 버튼의 선택 표시를 초기화한다.
        /// </summary>
        void IWebAgentEquipmentFeatureContext.ResetThermalPaletteButtonVisuals()
        {
            _features.ThermalFeature.ResetThermalPaletteButtonVisuals();
        }

        System.Collections.Generic.IEnumerable<Models.Main.CameraSpecialFeatureRow> IWebAgentEquipmentFeatureContext.SpecialFeatureRows
        {
            get { return _features.SpecialFeaturesFeature.SpecialFeatureRows; }
        }

        /// <summary>
        /// F1 장비 시간과 단조 시계를 사용하여 로컬 기준 샘플 시각으로 보정한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IWebAgentEquipmentFeatureContext.TryReadTelemetryTime(ulong milliseconds, out DateTimeOffset value, bool requireFresh)
        {
            return _features.WebAgentProtocolFeature.TryReadTelemetryTime(milliseconds, out value, requireFresh);
        }

        Action IWebAgentEquipmentFeatureContext.QueryEquipmentTemperatureCallback
        {
            get { return _features.Callbacks.QueryEquipmentTemperatureCallback; }
        }
    }
}
