using OpenCvWpfTracking.Services.Communication;
using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class GpsSettingsFeatureContext : IGpsSettingsFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal GpsSettingsFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        IControlCommandServicePort IGpsSettingsFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ITcpClientServicePort IGpsSettingsFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref double? IGpsSettingsFeatureContext._storedRpyYawOffset
        {
            get { return ref _features._rpySettingsFeature._storedRpyYawOffset; }
        }

        bool IGpsSettingsFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IGpsSettingsFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        void IGpsSettingsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        void IGpsSettingsFeatureContext.UpdatePositionGps(double latitude, double longitude, double altitude, double speed, double course, double hdop, int satelliteCount, int fixStatus, bool valid)
        {
            _features._positionFeature.UpdatePositionGps(latitude, longitude, altitude, speed, course, hdop, satelliteCount, fixStatus, valid);
        }

        Action IGpsSettingsFeatureContext.RequestGpsTelemetryCallback
        {
            get { return _features.Callbacks.RequestGpsTelemetryCallback; }
        }

        Action IGpsSettingsFeatureContext.ApplyGpsAutoTxRateCallback
        {
            get { return _features.Callbacks.ApplyGpsAutoTxRateCallback; }
        }

        Action IGpsSettingsFeatureContext.RequestGpsAutoTxRateCallback
        {
            get { return _features.Callbacks.RequestGpsAutoTxRateCallback; }
        }

        Action IGpsSettingsFeatureContext.UpdateGpsHeadingReferenceDisplayCallback
        {
            get { return _features.Callbacks.UpdateGpsHeadingReferenceDisplayCallback; }
        }
    }
}
