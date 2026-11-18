using System;
using System.Threading;
using System.Threading.Tasks;
using OpenCvWpfTracking.Models.Main;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class RoutesFeatureContext : IRoutesFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal RoutesFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Services.Communication.IControlCommandServicePort IRoutesFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        bool IRoutesFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        CancellationTokenSource IRoutesFeatureContext._laPresetSingleMoveCts
        {
            get { return _features._screenStateFeature._laPresetSingleMoveCts; }
            set { _features._screenStateFeature._laPresetSingleMoveCts = value; }
        }

        Services.Communication.ITcpClientServicePort IRoutesFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        CancellationTokenSource IRoutesFeatureContext._presetSingleMoveCts
        {
            get { return _features._screenStateFeature._presetSingleMoveCts; }
            set { _features._screenStateFeature._presetSingleMoveCts = value; }
        }

        string IRoutesFeatureContext.ControlLockTitle
        {
            get { return _features._propertiesFeature.ControlLockTitle; }
        }

        double IRoutesFeatureContext.CurrentPanoramaPan
        {
            get { return _features._opticalCenterFeature.CurrentPanoramaPan; }
        }

        double IRoutesFeatureContext.CurrentPanoramaTilt
        {
            get { return _features._opticalCenterFeature.CurrentPanoramaTilt; }
        }

        int IRoutesFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        int IRoutesFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort IRoutesFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardFocus();
        }

        ushort IRoutesFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        bool IRoutesFeatureContext.HasFreshPresetLens(bool ir, bool zoom)
        {
            return _features._presetLensFeature.HasFreshPresetLens(ir, zoom);
        }

        bool IRoutesFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IRoutesFeatureContext.IsEventAlertControlEnabled
        {
            get { return _features._propertiesFeature.IsEventAlertControlEnabled; }
        }

        bool IRoutesFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IRoutesFeatureContext.IsOperationLockOverlayVisible
        {
            get { return _features._propertiesFeature.IsOperationLockOverlayVisible; }
        }

        bool IRoutesFeatureContext.IsPresetScanControlLocked
        {
            get { return _features._propertiesFeature.IsPresetScanControlLocked; }
        }

        Task<bool> IRoutesFeatureContext.MoveLaPresetPointDirectAsync(PresetPointOption preset, CancellationToken cancellationToken, bool scanTiming, int? routeSpeed, int? routeDelay, bool? onlyIr)
        {
            return _features._moveControlFeature.MoveLaPresetPointDirectAsync(preset, cancellationToken, scanTiming, routeSpeed, routeDelay, onlyIr);
        }

        void IRoutesFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        bool IRoutesFeatureContext.OpticalFeedbackFresh(bool ir)
        {
            return _features._opticalCenterFeature.OpticalFeedbackFresh(ir);
        }

        string IRoutesFeatureContext.PanoramaDeviceKey
        {
            get { return _features._opticalCenterFeature.PanoramaDeviceKey; }
        }

        bool IRoutesFeatureContext.PanoramaPoseFresh
        {
            get { return _features._opticalCenterFeature.PanoramaPoseFresh; }
        }

        void IRoutesFeatureContext.ReleaseControlOwner(string owner)
        {
            _features._inputCoordinatorFeature.ReleaseControlOwner(owner);
        }
    }
}
