using OpenCvWpfTracking.Services.Communication;
using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class RpySettingsFeatureContext : IRpySettingsFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal RpySettingsFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        IControlCommandServicePort IRpySettingsFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ITcpClientServicePort IRpySettingsFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        bool IRpySettingsFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features.PropertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IRpySettingsFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IRpySettingsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// GPS 방위 기준과 IMU 원시값에 연결된 표시를 갱신한다.
        /// </summary>
        void IRpySettingsFeatureContext.UpdateGpsHeadingReferenceDisplay()
        {
            _features.GpsSettingsFeature.UpdateGpsHeadingReferenceDisplay();
        }

        Action IRpySettingsFeatureContext.ApplyRpyOffsetCallback
        {
            get { return _features.Callbacks.ApplyRpyOffsetCallback; }
        }

        Action IRpySettingsFeatureContext.ApplyRpyAutoTxRateCallback
        {
            get { return _features.Callbacks.ApplyRpyAutoTxRateCallback; }
        }

        Action IRpySettingsFeatureContext.RequestRpyTelemetryCallback
        {
            get { return _features.Callbacks.RequestRpyTelemetryCallback; }
        }

        Action IRpySettingsFeatureContext.RequestStoredRpyOffsetCallback
        {
            get { return _features.Callbacks.RequestStoredRpyOffsetCallback; }
        }

        Action IRpySettingsFeatureContext.RequestStoredRpyRateCallback
        {
            get { return _features.Callbacks.RequestStoredRpyRateCallback; }
        }
    }
}
