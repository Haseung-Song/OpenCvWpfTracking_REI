using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Control;
using System;
using System.Threading;
using System.Threading.Tasks;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class SyncFeatureContext : ISyncFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal SyncFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        RtspSourceOption ISyncFeatureContext._connectedEoCtecSource
        {
            get { return _features._screenStateFeature._connectedEoCtecSource; }
            set { _features._screenStateFeature._connectedEoCtecSource = value; }
        }

        string ISyncFeatureContext._connectionSessionId
        {
            get { return _features._devicePowerFeature._connectionSessionId; }
            set { _features._devicePowerFeature._connectionSessionId = value; }
        }

        Services.Communication.IControlCommandServicePort ISyncFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        Services.Communication.ICtecCameraCommandServicePort ISyncFeatureContext._ctecCameraCommandService
        {
            get { return _features.Runtime._ctecCameraCommandService; }
        }

        short ISyncFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short ISyncFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort ISyncFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort ISyncFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        int ISyncFeatureContext._environmentIrZoomPhysicalTeleRaw
        {
            get { return _features._screenStateFeature._environmentIrZoomPhysicalTeleRaw; }
            set { _features._screenStateFeature._environmentIrZoomPhysicalTeleRaw = value; }
        }

        int ISyncFeatureContext._environmentIrZoomPhysicalWideRaw
        {
            get { return _features._screenStateFeature._environmentIrZoomPhysicalWideRaw; }
            set { _features._screenStateFeature._environmentIrZoomPhysicalWideRaw = value; }
        }

        Services.Video.IFFmpegDecoderServicePort ISyncFeatureContext._eoDecoder
        {
            get { return _features.Runtime._eoDecoder; }
        }

        ref long ISyncFeatureContext._eoLensStatusVersion
        {
            get { return ref _features._screenStateFeature._eoLensStatusVersion; }
        }

        IFieldOfViewSyncServicePort ISyncFeatureContext._fieldOfViewSyncService
        {
            get { return _features.Runtime._fieldOfViewSyncService; }
        }

        Services.Video.IFFmpegDecoderServicePort ISyncFeatureContext._irDecoder
        {
            get { return _features.Runtime._irDecoder; }
        }

        ref long ISyncFeatureContext._irLensStatusVersion
        {
            get { return ref _features._screenStateFeature._irLensStatusVersion; }
        }

        double ISyncFeatureContext._irZoomObservedStopLag
        {
            get { return _features._screenStateFeature._irZoomObservedStopLag; }
            set { _features._screenStateFeature._irZoomObservedStopLag = value; }
        }

        bool ISyncFeatureContext._isEoFrameDisplayed
        {
            get { return _features._screenStateFeature._isEoFrameDisplayed; }
            set { _features._screenStateFeature._isEoFrameDisplayed = value; }
        }

        bool ISyncFeatureContext._isIrFrameDisplayed
        {
            get { return _features._screenStateFeature._isIrFrameDisplayed; }
            set { _features._screenStateFeature._isIrFrameDisplayed = value; }
        }

        SemaphoreSlim ISyncFeatureContext._lensSyncOperationLock
        {
            get { return _features.Runtime._lensSyncOperationLock; }
        }

        string ISyncFeatureContext._ptzPerformanceScenario
        {
            get { return _features._screenStateFeature._ptzPerformanceScenario; }
            set { _features._screenStateFeature._ptzPerformanceScenario = value; }
        }

        CancellationTokenSource ISyncFeatureContext._rooftopFocusSyncCts
        {
            get { return _features._screenStateFeature._rooftopFocusSyncCts; }
            set { _features._screenStateFeature._rooftopFocusSyncCts = value; }
        }

        CancellationTokenSource ISyncFeatureContext._rooftopZoomSyncCts
        {
            get { return _features._screenStateFeature._rooftopZoomSyncCts; }
            set { _features._screenStateFeature._rooftopZoomSyncCts = value; }
        }

        Services.Communication.WebAgent.IWebAgentZoomControlServicePort ISyncFeatureContext._webAgentZoomControlService
        {
            get { return _features.Runtime._webAgentZoomControlService; }
        }

        int ISyncFeatureContext.ConvertIrFocusStandardToStatusPosition(int standardPosition)
        {
            return _features._moveControlFeature.ConvertIrFocusStandardToStatusPosition(standardPosition);
        }

        Task<bool> ISyncFeatureContext.EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token)
        {
            return _features._presetLensFeature.EnsurePresetIrLensFeedbackAsync(zoom, token);
        }

        System.Collections.ObjectModel.ObservableCollection<ZoomSyncLevelOption> ISyncFeatureContext.FocusSyncLevelOptions
        {
            get { return _features._propertiesFeature.FocusSyncLevelOptions; }
        }

        string ISyncFeatureContext.FocusSyncStatusText
        {
            get { return _features._propertiesFeature.FocusSyncStatusText; }
            set { _features._propertiesFeature.FocusSyncStatusText = value; }
        }

        bool ISyncFeatureContext.HasFreshPresetLens(bool ir, bool zoom)
        {
            return _features._presetLensFeature.HasFreshPresetLens(ir, zoom);
        }

        bool ISyncFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features._webAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool ISyncFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool ISyncFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        long ISyncFeatureContext.PresetLensTicks(bool ir, bool zoom)
        {
            return _features._presetLensFeature.PresetLensTicks(ir, zoom);
        }

        Task<int?> ISyncFeatureContext.RequestAndWaitCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, CancellationToken cancellationToken)
        {
            return _features._continuousControlFeature.RequestAndWaitCtecEoPositionAsync(moveType, sourceOption, cancellationToken);
        }

        EquipmentStatusMode ISyncFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features._propertiesFeature.SelectedEquipmentStatusMode; }
            set { _features._propertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        ZoomSyncLevelOption ISyncFeatureContext.SelectedFocusSyncLevel
        {
            get { return _features._propertiesFeature.SelectedFocusSyncLevel; }
            set { _features._propertiesFeature.SelectedFocusSyncLevel = value; }
        }

        ZoomSyncLevelOption ISyncFeatureContext.SelectedZoomSyncLevel
        {
            get { return _features._propertiesFeature.SelectedZoomSyncLevel; }
            set { _features._propertiesFeature.SelectedZoomSyncLevel = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<ZoomSyncLevelOption> ISyncFeatureContext.ZoomSyncLevelOptions
        {
            get { return _features._propertiesFeature.ZoomSyncLevelOptions; }
        }

        string ISyncFeatureContext.ZoomSyncStatusText
        {
            get { return _features._propertiesFeature.ZoomSyncStatusText; }
            set { _features._propertiesFeature.ZoomSyncStatusText = value; }
        }
    }
}
