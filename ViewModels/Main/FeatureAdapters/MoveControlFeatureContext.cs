using OpenCvWpfTracking.Models.Main;
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
    internal sealed class MoveControlFeatureContext : IMoveControlFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal MoveControlFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? IMoveControlFeatureContext._activePanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activePanAbsoluteTarget; }
        }

        KeyboardPanTiltDirection IMoveControlFeatureContext._activePanTiltMoveDirection
        {
            get { return _features._screenStateFeature._activePanTiltMoveDirection; }
            set { _features._screenStateFeature._activePanTiltMoveDirection = value; }
        }

        ref double? IMoveControlFeatureContext._activeTiltAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activeTiltAbsoluteTarget; }
        }

        RtspSourceOption IMoveControlFeatureContext._connectedEoCtecSource
        {
            get { return _features._screenStateFeature._connectedEoCtecSource; }
            set { _features._screenStateFeature._connectedEoCtecSource = value; }
        }

        Services.Communication.IControlCommandServicePort IMoveControlFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ushort IMoveControlFeatureContext._currentCtecEoFocusPosition
        {
            get { return _features._screenStateFeature._currentCtecEoFocusPosition; }
            set { _features._screenStateFeature._currentCtecEoFocusPosition = value; }
        }

        ushort IMoveControlFeatureContext._currentCtecEoZoomPosition
        {
            get { return _features._screenStateFeature._currentCtecEoZoomPosition; }
            set { _features._screenStateFeature._currentCtecEoZoomPosition = value; }
        }

        short IMoveControlFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IMoveControlFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IMoveControlFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort IMoveControlFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        KeyboardPanTiltDirection IMoveControlFeatureContext._currentKeyboardPanTiltDirection
        {
            get { return _features._screenStateFeature._currentKeyboardPanTiltDirection; }
            set { _features._screenStateFeature._currentKeyboardPanTiltDirection = value; }
        }

        ContinuousMoveType IMoveControlFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        double IMoveControlFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        double IMoveControlFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        ref long IMoveControlFeatureContext._eoLensStatusVersion
        {
            get { return ref _features._screenStateFeature._eoLensStatusVersion; }
        }

        SemaphoreSlim IMoveControlFeatureContext._homeZeroOperationLock
        {
            get { return _features.Runtime._homeZeroOperationLock; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext._laPresetDirectScanCts
        {
            get { return ref _features._screenStateFeature._laPresetDirectScanCts; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext._laPresetSingleMoveCts
        {
            get { return ref _features._screenStateFeature._laPresetSingleMoveCts; }
        }

        ref double? IMoveControlFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort IMoveControlFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        Services.Communication.IMcbMaintenanceCommandServicePort IMoveControlFeatureContext._mcbMaintenanceCommandService
        {
            get { return _features.Runtime._mcbMaintenanceCommandService; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext._moveControlPanCts
        {
            get { return ref _features._screenStateFeature._moveControlPanCts; }
        }

        bool IMoveControlFeatureContext._panoramaPanTiltConfirmed
        {
            get { return _features._panoramaPresetsFeature._panoramaPanTiltConfirmed; }
            set { _features._panoramaPresetsFeature._panoramaPanTiltConfirmed = value; }
        }

        CancellationTokenSource IMoveControlFeatureContext._panoramaPresetCts
        {
            get { return _features._panoramaPresetsFeature._panoramaPresetCts; }
            set { _features._panoramaPresetsFeature._panoramaPresetCts = value; }
        }

        ref long IMoveControlFeatureContext._panTiltStatusVersion
        {
            get { return ref _features._screenStateFeature._panTiltStatusVersion; }
        }

        PanTurnMode IMoveControlFeatureContext._panTurnMode
        {
            get { return _features._screenStateFeature._panTurnMode; }
            set { _features._screenStateFeature._panTurnMode = value; }
        }

        bool IMoveControlFeatureContext._precisionStopUncertain
        {
            get { return _features._webAgentMotionFocusFeature._precisionStopUncertain; }
            set { _features._webAgentMotionFocusFeature._precisionStopUncertain = value; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext._presetDirectScanCts
        {
            get { return ref _features._screenStateFeature._presetDirectScanCts; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext._presetSingleMoveCts
        {
            get { return ref _features._screenStateFeature._presetSingleMoveCts; }
        }

        int IMoveControlFeatureContext._presetSkippedFocusMask
        {
            get { return _features._presetLensFeature._presetSkippedFocusMask; }
            set { _features._presetLensFeature._presetSkippedFocusMask = value; }
        }

        CancellationTokenSource IMoveControlFeatureContext._rooftopFocusSyncCts
        {
            get { return _features._screenStateFeature._rooftopFocusSyncCts; }
            set { _features._screenStateFeature._rooftopFocusSyncCts = value; }
        }

        CancellationTokenSource IMoveControlFeatureContext._rooftopZoomSyncCts
        {
            get { return _features._screenStateFeature._rooftopZoomSyncCts; }
            set { _features._screenStateFeature._rooftopZoomSyncCts = value; }
        }

        ControlAgentProfileOption IMoveControlFeatureContext._selectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature._selectedControlAgentProfile; }
            set { _features._rtspSettingsFeature._selectedControlAgentProfile = value; }
        }

        Services.Communication.WebAgent.IWebAgentZoomControlServicePort IMoveControlFeatureContext._webAgentZoomControlService
        {
            get { return _features.Runtime._webAgentZoomControlService; }
        }

        void IMoveControlFeatureContext.CancelPanoramaPresets()
        {
            _features._panoramaPresetsFeature.CancelPanoramaPresets();
        }

        Models.Position.PositionSnapshot IMoveControlFeatureContext.CapturePositionSnapshot(string source, int? presetId)
        {
            return _features._positionFeature.CapturePositionSnapshot(source, presetId);
        }

        void IMoveControlFeatureContext.ClearKeyboardPanTiltPressedState()
        {
            _features._continuousControlFeature.ClearKeyboardPanTiltPressedState();
        }

        Services.Configuration.LensControlTuning IMoveControlFeatureContext.ControlTuningFor(bool ir)
        {
            return _features._inputCoordinatorFeature.ControlTuningFor(ir);
        }

        int IMoveControlFeatureContext.ConvertIrZoomStandardToStatusPosition(int standardPosition)
        {
            return _features._syncFeature.ConvertIrZoomStandardToStatusPosition(standardPosition);
        }

        PresetPointOption[] IMoveControlFeatureContext.CreatePresetScanQueue(System.Collections.Generic.IEnumerable<PresetPointOption> source)
        {
            return _features._presetStorageFeature.CreatePresetScanQueue(source);
        }

        string IMoveControlFeatureContext.CurrentEoFocusText
        {
            get { return _features._propertiesFeature.CurrentEoFocusText; }
        }

        string IMoveControlFeatureContext.CurrentEoZoomText
        {
            get { return _features._propertiesFeature.CurrentEoZoomText; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlPanMaximum
        {
            get { return _features._screenStateFeature.CurrentMoveControlPanMaximum; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlPanMinimum
        {
            get { return _features._screenStateFeature.CurrentMoveControlPanMinimum; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        Task<bool> IMoveControlFeatureContext.EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token)
        {
            return _features._presetLensFeature.EnsurePresetIrLensFeedbackAsync(zoom, token);
        }

        // 직접 입력과 프리셋이 같은 LA 절대 줌 구현·상태 판정을 공유한다.
        Task<bool> IMoveControlFeatureContext.MoveLaIrZoomAbsoluteAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token, string requestType)
        {
            return _features._presetLensFeature.MoveLaIrZoomAbsoluteAsync(rawTarget, standardTarget, tolerance, token, requestType);
        }

        Task IMoveControlFeatureContext.FinishPanoramaZeroReferenceAsync(PanoramaZeroTicket ticket, bool sent)
        {
            return _features._panoramaReferenceFeature.FinishPanoramaZeroReferenceAsync(ticket, sent);
        }

        int? IMoveControlFeatureContext.FocusPositionValue
        {
            get { return _features._propertiesFeature.FocusPositionValue; }
            set { _features._propertiesFeature.FocusPositionValue = value; }
        }

        string IMoveControlFeatureContext.FormatPresetCompletion(string completed)
        {
            return _features._presetLensFeature.FormatPresetCompletion(completed);
        }

        bool IMoveControlFeatureContext.HasFreshPresetLens(bool ir, bool zoom)
        {
            return _features._presetLensFeature.HasFreshPresetLens(ir, zoom);
        }

        string IMoveControlFeatureContext.HomeZeroLockMessage
        {
            get { return _features._propertiesFeature.HomeZeroLockMessage; }
            set { _features._propertiesFeature.HomeZeroLockMessage = value; }
        }

        string IMoveControlFeatureContext.HomeZeroLockTitle
        {
            get { return _features._propertiesFeature.HomeZeroLockTitle; }
            set { _features._propertiesFeature.HomeZeroLockTitle = value; }
        }

        string IMoveControlFeatureContext.HomeZeroStatusText
        {
            get { return _features._propertiesFeature.HomeZeroStatusText; }
            set { _features._propertiesFeature.HomeZeroStatusText = value; }
        }

        bool IMoveControlFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features._webAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IMoveControlFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IMoveControlFeatureContext.IsHomePositionMoving
        {
            get { return _features._propertiesFeature.IsHomePositionMoving; }
            set { _features._propertiesFeature.IsHomePositionMoving = value; }
        }

        bool IMoveControlFeatureContext.IsLaPresetScanRunning
        {
            get { return _features._propertiesFeature.IsLaPresetScanRunning; }
            set { _features._propertiesFeature.IsLaPresetScanRunning = value; }
        }

        bool IMoveControlFeatureContext.IsPanoramaPresetRunning
        {
            get { return _features._panoramaPresetsFeature.IsPanoramaPresetRunning; }
        }

        bool IMoveControlFeatureContext.IsPresetFocusProtected(bool ir)
        {
            return _features._presetLensFeature.IsPresetFocusProtected(ir);
        }

        bool IMoveControlFeatureContext.IsPresetScanRunning
        {
            get { return _features._propertiesFeature.IsPresetScanRunning; }
            set { _features._propertiesFeature.IsPresetScanRunning = value; }
        }

        bool IMoveControlFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        string IMoveControlFeatureContext.LaPresetCommandStatusText
        {
            get { return _features._propertiesFeature.LaPresetCommandStatusText; }
            set { _features._propertiesFeature.LaPresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IMoveControlFeatureContext.LaPresetPoints
        {
            get { return _features._propertiesFeature.LaPresetPoints; }
        }

        int IMoveControlFeatureContext.LaPresetScanDelay
        {
            get { return _features._propertiesFeature.LaPresetScanDelay; }
            set { _features._propertiesFeature.LaPresetScanDelay = value; }
        }

        int IMoveControlFeatureContext.LaPresetScanSpeed
        {
            get { return _features._propertiesFeature.LaPresetScanSpeed; }
            set { _features._propertiesFeature.LaPresetScanSpeed = value; }
        }

        int IMoveControlFeatureContext.LaPresetSlotNumber
        {
            get { return _features._propertiesFeature.LaPresetSlotNumber; }
            set { _features._propertiesFeature.LaPresetSlotNumber = value; }
        }

        string IMoveControlFeatureContext.McbMaintenanceIpAddress
        {
            get { return _features._propertiesFeature.McbMaintenanceIpAddress; }
            set { _features._propertiesFeature.McbMaintenanceIpAddress = value; }
        }

        double IMoveControlFeatureContext.MoveControlEoMaximumZoomRatio
        {
            get { return _features._screenStateFeature.MoveControlEoMaximumZoomRatio; }
        }

        double IMoveControlFeatureContext.MoveControlMinimumZoomRatio
        {
            get { return _features._screenStateFeature.MoveControlMinimumZoomRatio; }
        }

        Task<bool> IMoveControlFeatureContext.MoveIrFocusToPositionAsync(int targetPosition, CancellationToken cancellationToken, int tolerance, bool explicitAbsolute)
        {
            return _features._syncFeature.MoveIrFocusToPositionAsync(targetPosition, cancellationToken, tolerance, explicitAbsolute);
        }

        Task<bool> IMoveControlFeatureContext.MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget, int tolerance, bool syncCalibration, bool explicitAbsolute)
        {
            return _features._syncFeature.MoveIrZoomToPositionAsync(targetPosition, cancellationToken, standardTarget, tolerance, syncCalibration, explicitAbsolute);
        }

        Task<bool> IMoveControlFeatureContext.MoveRooftopEoFocusToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken)
        {
            return _features._syncFeature.MoveRooftopEoFocusToRawPositionAsync(ctecSource, targetRawPosition, cancellationToken);
        }

        Task<bool> IMoveControlFeatureContext.MoveRooftopEoZoomToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken)
        {
            return _features._syncFeature.MoveRooftopEoZoomToRawPositionAsync(ctecSource, targetRawPosition, cancellationToken);
        }

        int IMoveControlFeatureContext.NormalizeIrZoomStatusPosition(int rawPosition)
        {
            return _features._syncFeature.NormalizeIrZoomStatusPosition(rawPosition);
        }

        double? IMoveControlFeatureContext.PanAbsoluteValue
        {
            get { return _features._propertiesFeature.PanAbsoluteValue; }
            set { _features._propertiesFeature.PanAbsoluteValue = value; }
        }

        Services.Video.PanoramaReferenceState IMoveControlFeatureContext.PanoramaReference
        {
            get { return _features._panoramaReferenceFeature.PanoramaReference; }
        }

        byte IMoveControlFeatureContext.PanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.PanTiltSpeedLevel; }
            set { _features._propertiesFeature.PanTiltSpeedLevel = value; }
        }

        Task<PanoramaZeroTicket> IMoveControlFeatureContext.PreparePanoramaZeroReferenceAsync(bool panAxis)
        {
            return _features._panoramaReferenceFeature.PreparePanoramaZeroReferenceAsync(panAxis);
        }

        void IMoveControlFeatureContext.PreparePresetForUpsert(PresetPointOption newPreset, System.Collections.Generic.IEnumerable<PresetPointOption> presets, PresetPointOption existingPreset)
        {
            _features._presetStorageFeature.PreparePresetForUpsert(newPreset, presets, existingPreset);
        }

        string IMoveControlFeatureContext.PresetCommandStatusText
        {
            get { return _features._propertiesFeature.PresetCommandStatusText; }
            set { _features._propertiesFeature.PresetCommandStatusText = value; }
        }

        string IMoveControlFeatureContext.PresetFocusSkipText
        {
            get { return _features._presetLensFeature.PresetFocusSkipText; }
        }

        int IMoveControlFeatureContext.PresetIrZoomPosition(PresetPointOption preset)
        {
            return _features._presetLensFeature.PresetIrZoomPosition(preset);
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IMoveControlFeatureContext.PresetPoints
        {
            get { return _features._propertiesFeature.PresetPoints; }
        }

        int IMoveControlFeatureContext.PresetScanDelay
        {
            get { return _features._propertiesFeature.PresetScanDelay; }
            set { _features._propertiesFeature.PresetScanDelay = value; }
        }

        int IMoveControlFeatureContext.PresetScanSpeed
        {
            get { return _features._propertiesFeature.PresetScanSpeed; }
            set { _features._propertiesFeature.PresetScanSpeed = value; }
        }

        int IMoveControlFeatureContext.PresetSlotNumber
        {
            get { return _features._propertiesFeature.PresetSlotNumber; }
            set { _features._propertiesFeature.PresetSlotNumber = value; }
        }

        void IMoveControlFeatureContext.RecoverPanoramaPresetReference()
        {
            _features._panoramaReferenceFeature.RecoverPanoramaPresetReference();
        }

        void IMoveControlFeatureContext.ReorderPresetCollections()
        {
            _features._presetStorageFeature.ReorderPresetCollections();
        }

        bool IMoveControlFeatureContext.RequiresDirectPresetRestore
        {
            get { return _features._presetLensFeature.RequiresDirectPresetRestore; }
        }

        void IMoveControlFeatureContext.ResetAllKeyboardControlState()
        {
            _features._continuousControlFeature.ResetAllKeyboardControlState();
        }

        Task<bool> IMoveControlFeatureContext.RestorePresetIrZoomAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token)
        {
            return _features._presetLensFeature.RestorePresetIrZoomAsync(rawTarget, standardTarget, tolerance, token);
        }

        Task<bool> IMoveControlFeatureContext.RestorePresetLensAsync(int ez, int ef, int iz, int iff, int delaySeconds, bool scanTiming, CancellationToken token, bool? onlyIr)
        {
            return _features._presetLensFeature.RestorePresetLensAsync(ez, ef, iz, iff, delaySeconds, scanTiming, token, onlyIr);
        }

        Task<bool> IMoveControlFeatureContext.RestoreStoredPresetLensAsync(PresetPointOption preset, CancellationToken token, bool scanTiming)
        {
            return _features._presetLensFeature.RestoreStoredPresetLensAsync(preset, token, scanTiming);
        }

        bool IMoveControlFeatureContext.SavePresetStorage()
        {
            return _features._presetStorageFeature.SavePresetStorage();
        }

        EquipmentStatusMode IMoveControlFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features._propertiesFeature.SelectedEquipmentStatusMode; }
            set { _features._propertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        PresetPointOption IMoveControlFeatureContext.SelectedLaPresetPoint
        {
            get { return _features._propertiesFeature.SelectedLaPresetPoint; }
            set { _features._propertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IMoveControlFeatureContext.SelectedPresetPoint
        {
            get { return _features._propertiesFeature.SelectedPresetPoint; }
            set { _features._propertiesFeature.SelectedPresetPoint = value; }
        }

        void IMoveControlFeatureContext.StopContinuousMove()
        {
            _features._continuousControlFeature.StopContinuousMove();
        }

        Task IMoveControlFeatureContext.StopFocusSyncAsync(bool stopEo, bool stopIr)
        {
            return _features._syncFeature.StopFocusSyncAsync(stopEo, stopIr);
        }

        void IMoveControlFeatureContext.StopPrecisionMotion(string reason, bool force)
        {
            _features._webAgentMotionFocusFeature.StopPrecisionMotion(reason, force);
        }

        Task IMoveControlFeatureContext.StopZoomSyncAsync()
        {
            return _features._syncFeature.StopZoomSyncAsync();
        }

        double? IMoveControlFeatureContext.TiltAbsoluteValue
        {
            get { return _features._propertiesFeature.TiltAbsoluteValue; }
            set { _features._propertiesFeature.TiltAbsoluteValue = value; }
        }

        Task<bool> IMoveControlFeatureContext.WaitForEnvironmentEoLensTargetAsync(int target, bool zoom, long afterSequence, CancellationToken cancellationToken)
        {
            return _features._syncFeature.WaitForEnvironmentEoLensTargetAsync(target, zoom, afterSequence, cancellationToken);
        }

        Task<bool> IMoveControlFeatureContext.WaitPresetEoLensTargetAsync(int target, bool zoom, long afterTicks, CancellationToken token, int tolerance)
        {
            return _features._presetLensFeature.WaitPresetEoLensTargetAsync(target, zoom, afterTicks, token, tolerance);
        }

        int? IMoveControlFeatureContext.ZoomPositionValue
        {
            get { return _features._propertiesFeature.ZoomPositionValue; }
            set { _features._propertiesFeature.ZoomPositionValue = value; }
        }

        double? IMoveControlFeatureContext.ZoomRatioValue
        {
            get { return _features._propertiesFeature.ZoomRatioValue; }
            set { _features._propertiesFeature.ZoomRatioValue = value; }
        }
    }
}
