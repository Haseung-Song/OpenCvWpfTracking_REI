using System;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class PanoramaFeatureContext : IPanoramaFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PanoramaFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Services.Communication.IControlCommandServicePort IPanoramaFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        double IPanoramaFeatureContext.CurrentPan
        {
            get { return _features.ScreenStateFeature._currentPan; }
            set { _features.ScreenStateFeature._currentPan = value; }
        }

        double IPanoramaFeatureContext.CurrentTilt
        {
            get { return _features.ScreenStateFeature._currentTilt; }
            set { _features.ScreenStateFeature._currentTilt = value; }
        }

        bool IPanoramaFeatureContext.IsEoFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isEoFrameDisplayed; }
            set { _features.ScreenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IPanoramaFeatureContext.IsHomePositionMoving
        {
            get { return _features.ScreenStateFeature._isHomePositionMoving; }
            set { _features.ScreenStateFeature._isHomePositionMoving = value; }
        }

        bool IPanoramaFeatureContext.IsLaPresetScanRunning
        {
            get { return _features.ScreenStateFeature._isLaPresetScanRunning; }
            set { _features.ScreenStateFeature._isLaPresetScanRunning = value; }
        }

        bool IPanoramaFeatureContext.IsPresetScanRunning
        {
            get { return _features.ScreenStateFeature._isPresetScanRunning; }
            set { _features.ScreenStateFeature._isPresetScanRunning = value; }
        }

        ref double? IPanoramaFeatureContext.LastPanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort IPanoramaFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref long IPanoramaFeatureContext.PanTiltStatusVersion
        {
            get { return ref _features.ScreenStateFeature._panTiltStatusVersion; }
        }

        string IPanoramaFeatureContext.ControlLockMessage
        {
            get { return _features.PropertiesFeature.ControlLockMessage; }
        }

        string IPanoramaFeatureContext.ControlLockTitle
        {
            get { return _features.PropertiesFeature.ControlLockTitle; }
        }

        /// <summary>
        /// 현재 광학 줌과 채널 비율에 따른 파노라마 화각 정보를 반환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Tuple<double, double> IPanoramaFeatureContext.ControlTuningForPanorama(bool ir)
        {
            return _features.OpticalCenterFeature.ControlTuningForPanorama(ir);
        }

        BitmapSource IPanoramaFeatureContext.EOCameraImage
        {
            get { return _features.PropertiesFeature.EOCameraImage; }
            set { _features.PropertiesFeature.EOCameraImage = value; }
        }

        string IPanoramaFeatureContext.EoStatusText
        {
            get { return _features.PropertiesFeature.EoStatusText; }
            set { _features.PropertiesFeature.EoStatusText = value; }
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IPanoramaFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// 현재 장비와 줌 위치에 대응하는 십자선 중심을 반환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        System.Windows.Point IPanoramaFeatureContext.GetOpticalCenter(bool ir)
        {
            return _features.OpticalCenterFeature.GetOpticalCenter(ir);
        }

        /// <summary>
        /// Web Agent의 0/360도 경계를 고려한 Tilt 차이를 반환한다. ROOFTOP LA는 기존 선형 -90 ~ 90도 차이를 유지한다.
        /// 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        double IPanoramaFeatureContext.GetTiltDifference(double currentTilt, double targetTilt)
        {
            return _features.MoveControlFeature.GetTiltDifference(currentTilt, targetTilt);
        }

        bool IPanoramaFeatureContext.IsControlInputLocked
        {
            get { return _features.PropertiesFeature.IsControlInputLocked; }
        }

        bool IPanoramaFeatureContext.IsEventAlertControlEnabled
        {
            get { return _features.PropertiesFeature.IsEventAlertControlEnabled; }
        }

        bool IPanoramaFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        bool IPanoramaFeatureContext.IsOperationLockOverlayVisible
        {
            get { return _features.PropertiesFeature.IsOperationLockOverlayVisible; }
        }

        bool IPanoramaFeatureContext.IsOperationTabNavigationEnabled
        {
            get { return _features.PropertiesFeature.IsOperationTabNavigationEnabled; }
        }

        bool IPanoramaFeatureContext.IsPanTiltSpeedControlEnabled
        {
            get { return _features.PropertiesFeature.IsPanTiltSpeedControlEnabled; }
        }

        bool IPanoramaFeatureContext.IsRooftopStatusSelected
        {
            get { return _features.PropertiesFeature.IsRooftopStatusSelected; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IPanoramaFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IPanoramaFeatureContext.PanoramaPresetContextHash
        {
            get { return _features.PanoramaPresetsFeature.PanoramaPresetContextHash; }
        }

        Services.Video.PanoramaReferenceState IPanoramaFeatureContext.PanoramaReference
        {
            get { return _features.PanoramaReferenceFeature.PanoramaReference; }
        }

        byte IPanoramaFeatureContext.PanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.PanTiltSpeedLevel; }
            set { _features.PropertiesFeature.PanTiltSpeedLevel = value; }
        }
    }
}
