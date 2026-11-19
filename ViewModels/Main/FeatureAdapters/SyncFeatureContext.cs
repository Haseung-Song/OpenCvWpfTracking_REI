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

        RtspSourceOption ISyncFeatureContext.ConnectedEoCtecSource
        {
            get { return _features.ScreenStateFeature._connectedEoCtecSource; }
            set { _features.ScreenStateFeature._connectedEoCtecSource = value; }
        }

        string ISyncFeatureContext.ConnectionSessionId
        {
            get { return _features.DevicePowerFeature._connectionSessionId; }
            set { _features.DevicePowerFeature._connectionSessionId = value; }
        }

        Services.Communication.IControlCommandServicePort ISyncFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        Services.Communication.ICtecCameraCommandServicePort ISyncFeatureContext.CtecCameraCommandService
        {
            get { return _features.Runtime._ctecCameraCommandService; }
        }

        short ISyncFeatureContext.CurrentEoFocus
        {
            get { return _features.ScreenStateFeature._currentEoFocus; }
            set { _features.ScreenStateFeature._currentEoFocus = value; }
        }

        short ISyncFeatureContext.CurrentEoZoom
        {
            get { return _features.ScreenStateFeature._currentEoZoom; }
            set { _features.ScreenStateFeature._currentEoZoom = value; }
        }

        ushort ISyncFeatureContext.CurrentIrFocus
        {
            get { return _features.ScreenStateFeature._currentIrFocus; }
            set { _features.ScreenStateFeature._currentIrFocus = value; }
        }

        ushort ISyncFeatureContext.CurrentIrZoom
        {
            get { return _features.ScreenStateFeature._currentIrZoom; }
            set { _features.ScreenStateFeature._currentIrZoom = value; }
        }

        int ISyncFeatureContext.EnvironmentIrZoomPhysicalTeleRaw
        {
            get { return _features.ScreenStateFeature._environmentIrZoomPhysicalTeleRaw; }
            set { _features.ScreenStateFeature._environmentIrZoomPhysicalTeleRaw = value; }
        }

        int ISyncFeatureContext.EnvironmentIrZoomPhysicalWideRaw
        {
            get { return _features.ScreenStateFeature._environmentIrZoomPhysicalWideRaw; }
            set { _features.ScreenStateFeature._environmentIrZoomPhysicalWideRaw = value; }
        }

        Services.Video.IFFmpegDecoderServicePort ISyncFeatureContext.EoDecoder
        {
            get { return _features.Runtime._eoDecoder; }
        }

        ref long ISyncFeatureContext.EoLensStatusVersion
        {
            get { return ref _features.ScreenStateFeature._eoLensStatusVersion; }
        }

        IFieldOfViewSyncServicePort ISyncFeatureContext.FieldOfViewSyncService
        {
            get { return _features.Runtime._fieldOfViewSyncService; }
        }

        Services.Video.IFFmpegDecoderServicePort ISyncFeatureContext.IrDecoder
        {
            get { return _features.Runtime._irDecoder; }
        }

        ref long ISyncFeatureContext.IrLensStatusVersion
        {
            get { return ref _features.ScreenStateFeature._irLensStatusVersion; }
        }

        double ISyncFeatureContext.IrZoomObservedStopLag
        {
            get { return _features.ScreenStateFeature._irZoomObservedStopLag; }
            set { _features.ScreenStateFeature._irZoomObservedStopLag = value; }
        }

        bool ISyncFeatureContext.IsEoFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isEoFrameDisplayed; }
            set { _features.ScreenStateFeature._isEoFrameDisplayed = value; }
        }

        bool ISyncFeatureContext.IsIrFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isIrFrameDisplayed; }
            set { _features.ScreenStateFeature._isIrFrameDisplayed = value; }
        }

        SemaphoreSlim ISyncFeatureContext.LensSyncOperationLock
        {
            get { return _features.Runtime._lensSyncOperationLock; }
        }

        string ISyncFeatureContext.PtzPerformanceScenario
        {
            get { return _features.ScreenStateFeature._ptzPerformanceScenario; }
            set { _features.ScreenStateFeature._ptzPerformanceScenario = value; }
        }

        CancellationTokenSource ISyncFeatureContext.RooftopFocusSyncCts
        {
            get { return _features.ScreenStateFeature._rooftopFocusSyncCts; }
            set { _features.ScreenStateFeature._rooftopFocusSyncCts = value; }
        }

        CancellationTokenSource ISyncFeatureContext.RooftopZoomSyncCts
        {
            get { return _features.ScreenStateFeature._rooftopZoomSyncCts; }
            set { _features.ScreenStateFeature._rooftopZoomSyncCts = value; }
        }

        Services.Communication.WebAgent.IWebAgentZoomControlServicePort ISyncFeatureContext.WebAgentZoomControlService
        {
            get { return _features.Runtime._webAgentZoomControlService; }
        }

        /// <summary>
        /// 화면의 IR Focus 표준 목표값을 현재 장비가 보고하는 상태 좌표로 변환한다. LA 상태 좌표만 반대 방향이며, Web Agent 상태
        /// 좌표는 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int ISyncFeatureContext.ConvertIrFocusStandardToStatusPosition(int standardPosition)
        {
            return _features.MoveControlFeature.ConvertIrFocusStandardToStatusPosition(standardPosition);
        }

        /// <summary>
        /// IR 피드백을 확보하지 못하면 절대위치 성공/시간추정으로 프리셋 완료를 표시하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> ISyncFeatureContext.EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token)
        {
            return _features.PresetLensFeature.EnsurePresetIrLensFeedbackAsync(zoom, token);
        }

        System.Collections.ObjectModel.ObservableCollection<ZoomSyncLevelOption> ISyncFeatureContext.FocusSyncLevelOptions
        {
            get { return _features.PropertiesFeature.FocusSyncLevelOptions; }
        }

        string ISyncFeatureContext.FocusSyncStatusText
        {
            get { return _features.PropertiesFeature.FocusSyncStatusText; }
            set { _features.PropertiesFeature.FocusSyncStatusText = value; }
        }

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool ISyncFeatureContext.HasFreshPresetLens(bool ir, bool zoom)
        {
            return _features.PresetLensFeature.HasFreshPresetLens(ir, zoom);
        }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool ISyncFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features.WebAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool ISyncFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features.PropertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool ISyncFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        /// <summary>
        /// 채널과 축별 마지막 렌즈 응답 시간을 조회한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        long ISyncFeatureContext.PresetLensTicks(bool ir, bool zoom)
        {
            return _features.PresetLensFeature.PresetLensTicks(ir, zoom);
        }

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다. HTTP 200
        /// OK만으로 조회 완료 처리하지 않는다. Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<int?> ISyncFeatureContext.RequestAndWaitCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, CancellationToken cancellationToken)
        {
            return _features.ContinuousControlFeature.RequestAndWaitCtecEoPositionAsync(moveType, sourceOption, cancellationToken);
        }

        EquipmentStatusMode ISyncFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features.PropertiesFeature.SelectedEquipmentStatusMode; }
            set { _features.PropertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        ZoomSyncLevelOption ISyncFeatureContext.SelectedFocusSyncLevel
        {
            get { return _features.PropertiesFeature.SelectedFocusSyncLevel; }
            set { _features.PropertiesFeature.SelectedFocusSyncLevel = value; }
        }

        ZoomSyncLevelOption ISyncFeatureContext.SelectedZoomSyncLevel
        {
            get { return _features.PropertiesFeature.SelectedZoomSyncLevel; }
            set { _features.PropertiesFeature.SelectedZoomSyncLevel = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<ZoomSyncLevelOption> ISyncFeatureContext.ZoomSyncLevelOptions
        {
            get { return _features.PropertiesFeature.ZoomSyncLevelOptions; }
        }

        string ISyncFeatureContext.ZoomSyncStatusText
        {
            get { return _features.PropertiesFeature.ZoomSyncStatusText; }
            set { _features.PropertiesFeature.ZoomSyncStatusText = value; }
        }
    }
}
