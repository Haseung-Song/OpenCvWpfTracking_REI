using OpenCvWpfTracking.Models.Main;
using System;
using System.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class ContinuousControlFeatureContext : IContinuousControlFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal ContinuousControlFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        RtspSourceOption IContinuousControlFeatureContext._activeEoCtecSource
        {
            get { return _features._screenStateFeature._activeEoCtecSource; }
            set { _features._screenStateFeature._activeEoCtecSource = value; }
        }

        KeyboardPanTiltDirection IContinuousControlFeatureContext._activePanTiltMoveDirection
        {
            get { return _features._screenStateFeature._activePanTiltMoveDirection; }
            set { _features._screenStateFeature._activePanTiltMoveDirection = value; }
        }

        System.Collections.Generic.Dictionary<string, RtspSourceOption> IContinuousControlFeatureContext._axisCtecSources
        {
            get { return _features._inputCoordinatorFeature._axisCtecSources; }
        }

        long IContinuousControlFeatureContext._centerDeadlineTicks
        {
            get { return _features._cameraFeatureControlFeature._centerDeadlineTicks; }
            set { _features._cameraFeatureControlFeature._centerDeadlineTicks = value; }
        }

        RtspSourceOption IContinuousControlFeatureContext._connectedEoCtecSource
        {
            get { return _features._screenStateFeature._connectedEoCtecSource; }
            set { _features._screenStateFeature._connectedEoCtecSource = value; }
        }

        Services.Communication.IControlCommandServicePort IContinuousControlFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> IContinuousControlFeatureContext._controlInputs
        {
            get { return _features._inputCoordinatorFeature._controlInputs; }
        }

        Services.Communication.ICtecCameraCommandServicePort IContinuousControlFeatureContext._ctecCameraCommandService
        {
            get { return _features.Runtime._ctecCameraCommandService; }
        }

        Services.Communication.ICtecCameraResponseServicePort IContinuousControlFeatureContext._ctecCameraResponseService
        {
            get { return _features.Runtime._ctecCameraResponseService; }
        }

        ref long IContinuousControlFeatureContext._ctecEoPositionOperationGeneration
        {
            get { return ref _features._screenStateFeature._ctecEoPositionOperationGeneration; }
        }

        ref global::System.Threading.CancellationTokenSource IContinuousControlFeatureContext._ctecEoPositionPollingCts
        {
            get { return ref _features._screenStateFeature._ctecEoPositionPollingCts; }
        }

        SemaphoreSlim IContinuousControlFeatureContext._ctecEoPositionQueryLock
        {
            get { return _features.Runtime._ctecEoPositionQueryLock; }
        }

        byte IContinuousControlFeatureContext._currentCtecEoFocusMode
        {
            get { return _features._screenStateFeature._currentCtecEoFocusMode; }
            set { _features._screenStateFeature._currentCtecEoFocusMode = value; }
        }

        ushort IContinuousControlFeatureContext._currentCtecEoFocusPosition
        {
            get { return _features._screenStateFeature._currentCtecEoFocusPosition; }
            set { _features._screenStateFeature._currentCtecEoFocusPosition = value; }
        }

        ushort IContinuousControlFeatureContext._currentCtecEoZoomPosition
        {
            get { return _features._screenStateFeature._currentCtecEoZoomPosition; }
            set { _features._screenStateFeature._currentCtecEoZoomPosition = value; }
        }

        short IContinuousControlFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IContinuousControlFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IContinuousControlFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort IContinuousControlFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        KeyboardPanTiltDirection IContinuousControlFeatureContext._currentKeyboardPanTiltDirection
        {
            get { return _features._screenStateFeature._currentKeyboardPanTiltDirection; }
            set { _features._screenStateFeature._currentKeyboardPanTiltDirection = value; }
        }

        ContinuousMoveType IContinuousControlFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        int IContinuousControlFeatureContext._environmentIrManualMoveDirection
        {
            get { return _features._screenStateFeature._environmentIrManualMoveDirection; }
            set { _features._screenStateFeature._environmentIrManualMoveDirection = value; }
        }

        ref global::System.DateTime IContinuousControlFeatureContext._environmentIrManualMoveStartedUtc
        {
            get { return ref _features._screenStateFeature._environmentIrManualMoveStartedUtc; }
        }

        ContinuousMoveType IContinuousControlFeatureContext._environmentIrManualMoveType
        {
            get { return _features._screenStateFeature._environmentIrManualMoveType; }
            set { _features._screenStateFeature._environmentIrManualMoveType = value; }
        }

        ref int IContinuousControlFeatureContext._eoFocusCommandSequence
        {
            get { return ref _features._screenStateFeature._eoFocusCommandSequence; }
        }

        System.Diagnostics.Stopwatch IContinuousControlFeatureContext._focusLogStopwatch
        {
            get { return _features.Runtime._focusLogStopwatch; }
        }

        int IContinuousControlFeatureContext._inputEpoch
        {
            get { return _features._inputCoordinatorFeature._inputEpoch; }
            set { _features._inputCoordinatorFeature._inputEpoch = value; }
        }

        ref long IContinuousControlFeatureContext._irLensStatusVersion
        {
            get { return ref _features._screenStateFeature._irLensStatusVersion; }
        }

        bool IContinuousControlFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        bool IContinuousControlFeatureContext._isKeyboardPanLeftPressed
        {
            get { return _features._screenStateFeature._isKeyboardPanLeftPressed; }
            set { _features._screenStateFeature._isKeyboardPanLeftPressed = value; }
        }

        bool IContinuousControlFeatureContext._isKeyboardPanRightPressed
        {
            get { return _features._screenStateFeature._isKeyboardPanRightPressed; }
            set { _features._screenStateFeature._isKeyboardPanRightPressed = value; }
        }

        bool IContinuousControlFeatureContext._isKeyboardTiltDownPressed
        {
            get { return _features._screenStateFeature._isKeyboardTiltDownPressed; }
            set { _features._screenStateFeature._isKeyboardTiltDownPressed = value; }
        }

        bool IContinuousControlFeatureContext._isKeyboardTiltUpPressed
        {
            get { return _features._screenStateFeature._isKeyboardTiltUpPressed; }
            set { _features._screenStateFeature._isKeyboardTiltUpPressed = value; }
        }

        bool IContinuousControlFeatureContext._isPanTiltMoveActive
        {
            get { return _features._screenStateFeature._isPanTiltMoveActive; }
            set { _features._screenStateFeature._isPanTiltMoveActive = value; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<int, int>> IContinuousControlFeatureContext._issuedControlAxes
        {
            get { return _features._inputCoordinatorFeature._issuedControlAxes; }
        }

        long IContinuousControlFeatureContext._lastEoFocusCommandElapsedMs
        {
            get { return _features._screenStateFeature._lastEoFocusCommandElapsedMs; }
            set { _features._screenStateFeature._lastEoFocusCommandElapsedMs = value; }
        }

        string IContinuousControlFeatureContext._lastEoFocusCommandName
        {
            get { return _features._screenStateFeature._lastEoFocusCommandName; }
            set { _features._screenStateFeature._lastEoFocusCommandName = value; }
        }

        ref global::System.DateTime IContinuousControlFeatureContext._lastEoFocusCommandTime
        {
            get { return ref _features._screenStateFeature._lastEoFocusCommandTime; }
        }

        ref long IContinuousControlFeatureContext._panStopTxCount
        {
            get { return ref _features._screenStateFeature._panStopTxCount; }
        }

        ref long IContinuousControlFeatureContext._panTiltStatusVersion
        {
            get { return ref _features._screenStateFeature._panTiltStatusVersion; }
        }

        bool IContinuousControlFeatureContext._precisionStopUncertain
        {
            get { return _features._webAgentMotionFocusFeature._precisionStopUncertain; }
            set { _features._webAgentMotionFocusFeature._precisionStopUncertain = value; }
        }

        string IContinuousControlFeatureContext._ptzPerformanceScenario
        {
            get { return _features._screenStateFeature._ptzPerformanceScenario; }
            set { _features._screenStateFeature._ptzPerformanceScenario = value; }
        }

        ref long IContinuousControlFeatureContext._tiltStopTxCount
        {
            get { return ref _features._screenStateFeature._tiltStopTxCount; }
        }

        bool IContinuousControlFeatureContext._videoCenterBusy
        {
            get { return _features._cameraFeatureControlFeature._videoCenterBusy; }
            set { _features._cameraFeatureControlFeature._videoCenterBusy = value; }
        }

        ref global::System.DateTimeOffset IContinuousControlFeatureContext._zoneCtecZoomAt
        {
            get { return ref _features._zonesFeature._zoneCtecZoomAt; }
        }

        int IContinuousControlFeatureContext._zoneCtecZoomEpoch
        {
            get { return _features._zonesFeature._zoneCtecZoomEpoch; }
            set { _features._zonesFeature._zoneCtecZoomEpoch = value; }
        }

        string IContinuousControlFeatureContext._zoneCtecZoomKey
        {
            get { return _features._zonesFeature._zoneCtecZoomKey; }
            set { _features._zonesFeature._zoneCtecZoomKey = value; }
        }

        int IContinuousControlFeatureContext._zoneEpoch
        {
            get { return _features._zonesFeature._zoneEpoch; }
            set { _features._zonesFeature._zoneEpoch = value; }
        }

        ref long IContinuousControlFeatureContext._zoomStartTxCount
        {
            get { return ref _features._screenStateFeature._zoomStartTxCount; }
        }

        ref long IContinuousControlFeatureContext._zoomStopTxCount
        {
            get { return ref _features._screenStateFeature._zoomStopTxCount; }
        }

        void IContinuousControlFeatureContext.ApplyEnvironmentIrCommandedPosition(int? zoomPosition, int? focusPosition, string reason)
        {
            _features._controlAgentFeature.ApplyEnvironmentIrCommandedPosition(zoomPosition, focusPosition, reason);
        }

        bool IContinuousControlFeatureContext.ApplyPanTiltPositionSpeedFromUi(out double positionSpeed)
        {
            return _features._moveControlFeature.ApplyPanTiltPositionSpeedFromUi(out positionSpeed);
        }

        bool IContinuousControlFeatureContext.AutoFocusTargetUsesAgent(int target)
        {
            return _features._webAgentMotionFocusFeature.AutoFocusTargetUsesAgent(target);
        }

        void IContinuousControlFeatureContext.CancelMoveControlPanOperation()
        {
            _features._moveControlFeature.CancelMoveControlPanOperation();
        }

        void IContinuousControlFeatureContext.CancelPanoramaPresets()
        {
            _features._panoramaPresetsFeature.CancelPanoramaPresets();
        }

        void IContinuousControlFeatureContext.ClearActivePanTiltAbsoluteMove()
        {
            _features._moveControlFeature.ClearActivePanTiltAbsoluteMove();
        }

        Services.Configuration.LensControlTuning IContinuousControlFeatureContext.ControlTuningFor(bool ir)
        {
            return _features._inputCoordinatorFeature.ControlTuningFor(ir);
        }

        string IContinuousControlFeatureContext.CurrentEoFocusText
        {
            get { return _features._propertiesFeature.CurrentEoFocusText; }
        }

        string IContinuousControlFeatureContext.CurrentEoZoomText
        {
            get { return _features._propertiesFeature.CurrentEoZoomText; }
        }

        string IContinuousControlFeatureContext.CurrentLaPresetSnapshotText
        {
            get { return _features._propertiesFeature.CurrentLaPresetSnapshotText; }
        }

        string IContinuousControlFeatureContext.CurrentPresetSnapshotText
        {
            get { return _features._propertiesFeature.CurrentPresetSnapshotText; }
        }

        byte IContinuousControlFeatureContext.EffectivePanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.EffectivePanTiltSpeedLevel; }
        }

        string IContinuousControlFeatureContext.EnvironmentEoFocusStatusText
        {
            get { return _features._propertiesFeature.EnvironmentEoFocusStatusText; }
        }

        string IContinuousControlFeatureContext.EnvironmentEoZoomStatusText
        {
            get { return _features._propertiesFeature.EnvironmentEoZoomStatusText; }
        }

        void IContinuousControlFeatureContext.ExecuteAutoFocus(object parameter)
        {
            _features._webAgentMotionFocusFeature.ExecuteAutoFocus(parameter);
        }

        int IContinuousControlFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        int IContinuousControlFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort IContinuousControlFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardFocus();
        }

        ushort IContinuousControlFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        bool IContinuousControlFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features._webAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IContinuousControlFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IContinuousControlFeatureContext.IsFovSpeedEnabled
        {
            get { return _features._inputCoordinatorFeature.IsFovSpeedEnabled; }
            set { _features._inputCoordinatorFeature.IsFovSpeedEnabled = value; }
        }

        bool IContinuousControlFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        bool IContinuousControlFeatureContext.IsWebAgentFocusControlReady
        {
            get { return _features._webAgentMotionFocusFeature.IsWebAgentFocusControlReady; }
        }

        void IContinuousControlFeatureContext.MarkLensEvidence(bool ir)
        {
            _features._inputCoordinatorFeature.MarkLensEvidence(ir);
        }

        void IContinuousControlFeatureContext.MarkPanTiltMoveCommandIssued()
        {
            _features._webAgentProtocolFeature.MarkPanTiltMoveCommandIssued();
        }

        void IContinuousControlFeatureContext.MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            _features._presetLensFeature.MarkPresetLensEvidence(ir, zoom, focus);
        }

        void IContinuousControlFeatureContext.NotifyEoCurrentStatusChanged(bool panChanged, bool tiltChanged, bool zoomChanged, bool focusChanged, bool powerChanged)
        {
            _features._controlAgentFeature.NotifyEoCurrentStatusChanged(panChanged, tiltChanged, zoomChanged, focusChanged, powerChanged);
        }

        void IContinuousControlFeatureContext.NotifyIrCurrentStatusChanged()
        {
            _features._controlAgentFeature.NotifyIrCurrentStatusChanged();
        }

        void IContinuousControlFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IContinuousControlFeatureContext.PanTiltSpeedDisplayText
        {
            get { return _features._propertiesFeature.PanTiltSpeedDisplayText; }
        }

        byte IContinuousControlFeatureContext.PanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.PanTiltSpeedLevel; }
            set { _features._propertiesFeature.PanTiltSpeedLevel = value; }
        }

        void IContinuousControlFeatureContext.ReapplyActivePanTiltAbsoluteTarget(double positionSpeed)
        {
            _features._moveControlFeature.ReapplyActivePanTiltAbsoluteTarget(positionSpeed);
        }

        string IContinuousControlFeatureContext.RooftopEoFocusStatusText
        {
            get { return _features._propertiesFeature.RooftopEoFocusStatusText; }
        }

        string IContinuousControlFeatureContext.RooftopEoZoomStatusText
        {
            get { return _features._propertiesFeature.RooftopEoZoomStatusText; }
        }

        RtspSourceOption IContinuousControlFeatureContext.SelectedEoRtspSource
        {
            get { return _features._propertiesFeature.SelectedEoRtspSource; }
            set { _features._propertiesFeature.SelectedEoRtspSource = value; }
        }

        EquipmentStatusMode IContinuousControlFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features._propertiesFeature.SelectedEquipmentStatusMode; }
            set { _features._propertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        void IContinuousControlFeatureContext.SetDigitalEstimateDirection(int direction, bool ir)
        {
            _features._cameraFeatureControlFeature.SetDigitalEstimateDirection(direction, ir);
        }

        void IContinuousControlFeatureContext.StopPrecisionMotion(string reason, bool force)
        {
            _features._webAgentMotionFocusFeature.StopPrecisionMotion(reason, force);
        }

        string IContinuousControlFeatureContext.ZoneKey(string camera)
        {
            return _features._zonesFeature.ZoneKey(camera);
        }
    }
}
