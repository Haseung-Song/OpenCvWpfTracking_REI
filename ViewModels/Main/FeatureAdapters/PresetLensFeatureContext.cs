using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class PresetLensFeatureContext : IPresetLensFeatureContext
    {
        private readonly FeatureComposition _features;

        string IPresetLensFeatureContext._connectionSessionId
        {
            get { return _features._devicePowerFeature._connectionSessionId; }
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PresetLensFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Services.Communication.AutoFocusSnapshot[] IPresetLensFeatureContext._autoFocus
        {
            get { return _features._webAgentMotionFocusFeature._autoFocus; }
        }

        bool IPresetLensFeatureContext._autoFocusActivatedForSession
        {
            get { return _features._webAgentMotionFocusFeature._autoFocusActivatedForSession; }
            set { _features._webAgentMotionFocusFeature._autoFocusActivatedForSession = value; }
        }

        Services.Communication.IControlCommandServicePort IPresetLensFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        short IPresetLensFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IPresetLensFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IPresetLensFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext._laPresetDirectScanCts
        {
            get { return _features._screenStateFeature._laPresetDirectScanCts; }
            set { _features._screenStateFeature._laPresetDirectScanCts = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext._laPresetSingleMoveCts
        {
            get { return _features._screenStateFeature._laPresetSingleMoveCts; }
            set { _features._screenStateFeature._laPresetSingleMoveCts = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext._presetDirectScanCts
        {
            get { return _features._screenStateFeature._presetDirectScanCts; }
            set { _features._screenStateFeature._presetDirectScanCts = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext._presetSingleMoveCts
        {
            get { return _features._screenStateFeature._presetSingleMoveCts; }
            set { _features._screenStateFeature._presetSingleMoveCts = value; }
        }

        bool IPresetLensFeatureContext.AutoFocusTargetUsesAgent(int target)
        {
            return _features._webAgentMotionFocusFeature.AutoFocusTargetUsesAgent(target);
        }

        bool IPresetLensFeatureContext.CameraFeatureTransportReady
        {
            get { return _features._cameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        int IPresetLensFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        int IPresetLensFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort IPresetLensFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardFocus();
        }

        ushort IPresetLensFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        bool IPresetLensFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features._webAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IPresetLensFeatureContext.IsAutoFocusPanelVisible
        {
            get { return _features._webAgentMotionFocusFeature.IsAutoFocusPanelVisible; }
        }

        bool IPresetLensFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IPresetLensFeatureContext.IsIrZoomTargetReached(int target, int actual, int standardTarget, int tolerance, bool syncCalibration)
        {
            return _features._syncFeature.IsIrZoomTargetReached(target, actual, standardTarget, tolerance, syncCalibration);
        }

        bool IPresetLensFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        Task<bool> IPresetLensFeatureContext.MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget, int tolerance, bool syncCalibration, bool explicitAbsolute)
        {
            return _features._syncFeature.MoveIrZoomToPositionAsync(targetPosition, cancellationToken, standardTarget, tolerance, syncCalibration, explicitAbsolute);
        }

        Task<bool> IPresetLensFeatureContext.MoveLaPresetFocusDirectAsync(int eoStandardPosition, int irStandardPosition, CancellationToken cancellationToken, int eoTolerance, int irTolerance, bool? onlyIr)
        {
            return _features._moveControlFeature.MoveLaPresetFocusDirectAsync(eoStandardPosition, irStandardPosition, cancellationToken, eoTolerance, irTolerance, onlyIr);
        }

        Task<bool> IPresetLensFeatureContext.MoveLaPresetZoomDirectAsync(int eoStandardPosition, int irPosition, CancellationToken cancellationToken, int eoTolerance, int irTolerance, bool? onlyIr)
        {
            return _features._moveControlFeature.MoveLaPresetZoomDirectAsync(eoStandardPosition, irPosition, cancellationToken, eoTolerance, irTolerance, onlyIr);
        }

        Services.Video.PanoramaReferenceState IPresetLensFeatureContext.PanoramaReference
        {
            get { return _features._panoramaReferenceFeature.PanoramaReference; }
        }

        int IPresetLensFeatureContext.PresetScanDelay
        {
            get { return _features._propertiesFeature.PresetScanDelay; }
            set { _features._propertiesFeature.PresetScanDelay = value; }
        }

        Task<int> IPresetLensFeatureContext.WaitForIrLensSettledPositionAsync(bool zoom, CancellationToken cancellationToken)
        {
            return _features._syncFeature.WaitForIrLensSettledPositionAsync(zoom, cancellationToken);
        }
    }
}
