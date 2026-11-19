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

        /// <summary>
        /// 화면의 IR Focus 표준 목표값을 현재 장비가 보고하는 상태 좌표로 변환한다. LA 상태 좌표만 반대 방향이며, Web Agent 상태
        /// 좌표는 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int ISyncFeatureContext.ConvertIrFocusStandardToStatusPosition(int standardPosition)
        {
            return _features._moveControlFeature.ConvertIrFocusStandardToStatusPosition(standardPosition);
        }

        /// <summary>
        /// IR 피드백을 확보하지 못하면 절대위치 성공/시간추정으로 프리셋 완료를 표시하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool ISyncFeatureContext.HasFreshPresetLens(bool ir, bool zoom)
        {
            return _features._presetLensFeature.HasFreshPresetLens(ir, zoom);
        }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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

        /// <summary>
        /// 채널과 축별 마지막 렌즈 응답 시간을 조회한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        long ISyncFeatureContext.PresetLensTicks(bool ir, bool zoom)
        {
            return _features._presetLensFeature.PresetLensTicks(ir, zoom);
        }

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다. HTTP 200
        /// OK만으로 조회 완료 처리하지 않는다. Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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
