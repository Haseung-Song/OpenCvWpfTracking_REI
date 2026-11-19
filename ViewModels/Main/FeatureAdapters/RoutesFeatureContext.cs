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

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// -&gt; Standard 0(Far) Raw 0(Near) -&gt; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IRoutesFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// -&gt; Standard 0(Wide) Raw 0(Tele) -&gt; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IRoutesFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IRoutesFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardFocus();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IRoutesFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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

        /// <summary>
        /// 저장된 PRESET 1 한 지점의 PTZF 값을 직접 복원한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IRoutesFeatureContext.MoveLaPresetPointDirectAsync(PresetPointOption preset, CancellationToken cancellationToken, bool scanTiming, int? routeSpeed, int? routeDelay, bool? onlyIr)
        {
            return _features._moveControlFeature.MoveLaPresetPointDirectAsync(preset, cancellationToken, scanTiming, routeSpeed, routeDelay, onlyIr);
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IRoutesFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// 보정에 필요한 렌즈와 위치 응답의 신선도를 검사한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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

        /// <summary>
        /// 지정 창/입력 소유자의 토큰만 일괄 해제한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IRoutesFeatureContext.ReleaseControlOwner(string owner)
        {
            _features._inputCoordinatorFeature.ReleaseControlOwner(owner);
        }
    }
}
