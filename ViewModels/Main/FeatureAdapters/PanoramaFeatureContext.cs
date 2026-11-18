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

        Services.Communication.IControlCommandServicePort IPanoramaFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        double IPanoramaFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        double IPanoramaFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        bool IPanoramaFeatureContext._isEoFrameDisplayed
        {
            get { return _features._screenStateFeature._isEoFrameDisplayed; }
            set { _features._screenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IPanoramaFeatureContext._isHomePositionMoving
        {
            get { return _features._screenStateFeature._isHomePositionMoving; }
            set { _features._screenStateFeature._isHomePositionMoving = value; }
        }

        bool IPanoramaFeatureContext._isLaPresetScanRunning
        {
            get { return _features._screenStateFeature._isLaPresetScanRunning; }
            set { _features._screenStateFeature._isLaPresetScanRunning = value; }
        }

        bool IPanoramaFeatureContext._isPresetScanRunning
        {
            get { return _features._screenStateFeature._isPresetScanRunning; }
            set { _features._screenStateFeature._isPresetScanRunning = value; }
        }

        ref double? IPanoramaFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort IPanoramaFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref long IPanoramaFeatureContext._panTiltStatusVersion
        {
            get { return ref _features._screenStateFeature._panTiltStatusVersion; }
        }

        string IPanoramaFeatureContext.ControlLockMessage
        {
            get { return _features._propertiesFeature.ControlLockMessage; }
        }

        string IPanoramaFeatureContext.ControlLockTitle
        {
            get { return _features._propertiesFeature.ControlLockTitle; }
        }

        Tuple<double, double> IPanoramaFeatureContext.ControlTuningForPanorama(bool ir)
        {
            return _features._opticalCenterFeature.ControlTuningForPanorama(ir);
        }

        BitmapSource IPanoramaFeatureContext.EOCameraImage
        {
            get { return _features._propertiesFeature.EOCameraImage; }
            set { _features._propertiesFeature.EOCameraImage = value; }
        }

        string IPanoramaFeatureContext.EoStatusText
        {
            get { return _features._propertiesFeature.EoStatusText; }
            set { _features._propertiesFeature.EoStatusText = value; }
        }

        ushort IPanoramaFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        System.Windows.Point IPanoramaFeatureContext.GetOpticalCenter(bool ir)
        {
            return _features._opticalCenterFeature.GetOpticalCenter(ir);
        }

        double IPanoramaFeatureContext.GetTiltDifference(double currentTilt, double targetTilt)
        {
            return _features._moveControlFeature.GetTiltDifference(currentTilt, targetTilt);
        }

        bool IPanoramaFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IPanoramaFeatureContext.IsEventAlertControlEnabled
        {
            get { return _features._propertiesFeature.IsEventAlertControlEnabled; }
        }

        bool IPanoramaFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IPanoramaFeatureContext.IsOperationLockOverlayVisible
        {
            get { return _features._propertiesFeature.IsOperationLockOverlayVisible; }
        }

        bool IPanoramaFeatureContext.IsOperationTabNavigationEnabled
        {
            get { return _features._propertiesFeature.IsOperationTabNavigationEnabled; }
        }

        bool IPanoramaFeatureContext.IsPanTiltSpeedControlEnabled
        {
            get { return _features._propertiesFeature.IsPanTiltSpeedControlEnabled; }
        }

        bool IPanoramaFeatureContext.IsRooftopStatusSelected
        {
            get { return _features._propertiesFeature.IsRooftopStatusSelected; }
        }

        void IPanoramaFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IPanoramaFeatureContext.PanoramaPresetContextHash
        {
            get { return _features._panoramaPresetsFeature.PanoramaPresetContextHash; }
        }

        Services.Video.PanoramaReferenceState IPanoramaFeatureContext.PanoramaReference
        {
            get { return _features._panoramaReferenceFeature.PanoramaReference; }
        }

        byte IPanoramaFeatureContext.PanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.PanTiltSpeedLevel; }
            set { _features._propertiesFeature.PanTiltSpeedLevel = value; }
        }
    }
}
