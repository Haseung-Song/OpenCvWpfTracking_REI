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

        bool IMoveControlFeatureContext.IsAutoTrackingActive
        {
            get { return _features.PropertiesFeature.IsAutoTrackingEnabled; }
        }

        /// <summary>현재 제어 작업의 진입 사유를 전달하고 자동 추적을 종료한다.</summary>
        void IMoveControlFeatureContext.StopAutoTracking(string reason)
        {
            _features.AutoTrackingFeature.Stop(reason);
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal MoveControlFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? IMoveControlFeatureContext.ActivePanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activePanAbsoluteTarget; }
        }

        KeyboardPanTiltDirection IMoveControlFeatureContext.ActivePanTiltMoveDirection
        {
            get { return _features.ScreenStateFeature._activePanTiltMoveDirection; }
            set { _features.ScreenStateFeature._activePanTiltMoveDirection = value; }
        }

        ref double? IMoveControlFeatureContext.ActiveTiltAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activeTiltAbsoluteTarget; }
        }

        RtspSourceOption IMoveControlFeatureContext.ConnectedEoCtecSource
        {
            get { return _features.ScreenStateFeature._connectedEoCtecSource; }
            set { _features.ScreenStateFeature._connectedEoCtecSource = value; }
        }

        Services.Communication.IControlCommandServicePort IMoveControlFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ushort IMoveControlFeatureContext.CurrentCtecEoFocusPosition
        {
            get { return _features.ScreenStateFeature._currentCtecEoFocusPosition; }
            set { _features.ScreenStateFeature._currentCtecEoFocusPosition = value; }
        }

        ushort IMoveControlFeatureContext.CurrentCtecEoZoomPosition
        {
            get { return _features.ScreenStateFeature._currentCtecEoZoomPosition; }
            set { _features.ScreenStateFeature._currentCtecEoZoomPosition = value; }
        }

        short IMoveControlFeatureContext.CurrentEoFocus
        {
            get { return _features.ScreenStateFeature._currentEoFocus; }
            set { _features.ScreenStateFeature._currentEoFocus = value; }
        }

        short IMoveControlFeatureContext.CurrentEoZoom
        {
            get { return _features.ScreenStateFeature._currentEoZoom; }
            set { _features.ScreenStateFeature._currentEoZoom = value; }
        }

        ushort IMoveControlFeatureContext.CurrentIrFocus
        {
            get { return _features.ScreenStateFeature._currentIrFocus; }
            set { _features.ScreenStateFeature._currentIrFocus = value; }
        }

        ushort IMoveControlFeatureContext.CurrentIrZoom
        {
            get { return _features.ScreenStateFeature._currentIrZoom; }
            set { _features.ScreenStateFeature._currentIrZoom = value; }
        }

        KeyboardPanTiltDirection IMoveControlFeatureContext.CurrentKeyboardPanTiltDirection
        {
            get { return _features.ScreenStateFeature._currentKeyboardPanTiltDirection; }
            set { _features.ScreenStateFeature._currentKeyboardPanTiltDirection = value; }
        }

        ContinuousMoveType IMoveControlFeatureContext.CurrentMoveType
        {
            get { return _features.ScreenStateFeature._currentMoveType; }
            set { _features.ScreenStateFeature._currentMoveType = value; }
        }

        double IMoveControlFeatureContext.CurrentPan
        {
            get { return _features.ScreenStateFeature._currentPan; }
            set { _features.ScreenStateFeature._currentPan = value; }
        }

        double IMoveControlFeatureContext.CurrentTilt
        {
            get { return _features.ScreenStateFeature._currentTilt; }
            set { _features.ScreenStateFeature._currentTilt = value; }
        }

        ref long IMoveControlFeatureContext.EoLensStatusVersion
        {
            get { return ref _features.ScreenStateFeature._eoLensStatusVersion; }
        }

        SemaphoreSlim IMoveControlFeatureContext.HomeZeroOperationLock
        {
            get { return _features.Runtime._homeZeroOperationLock; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext.LaPresetDirectScanCts
        {
            get { return ref _features.ScreenStateFeature._laPresetDirectScanCts; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext.LaPresetSingleMoveCts
        {
            get { return ref _features.ScreenStateFeature._laPresetSingleMoveCts; }
        }

        ref double? IMoveControlFeatureContext.LastPanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort IMoveControlFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        Services.Communication.IMcbMaintenanceCommandServicePort IMoveControlFeatureContext.McbMaintenanceCommandService
        {
            get { return _features.Runtime._mcbMaintenanceCommandService; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext.MoveControlPanCts
        {
            get { return ref _features.ScreenStateFeature._moveControlPanCts; }
        }

        bool IMoveControlFeatureContext.PanoramaPanTiltConfirmed
        {
            get { return _features.PanoramaPresetsFeature._panoramaPanTiltConfirmed; }
            set { _features.PanoramaPresetsFeature._panoramaPanTiltConfirmed = value; }
        }

        CancellationTokenSource IMoveControlFeatureContext.PanoramaPresetCts
        {
            get { return _features.PanoramaPresetsFeature._panoramaPresetCts; }
            set { _features.PanoramaPresetsFeature._panoramaPresetCts = value; }
        }

        ref long IMoveControlFeatureContext.PanTiltStatusVersion
        {
            get { return ref _features.ScreenStateFeature._panTiltStatusVersion; }
        }

        PanTurnMode IMoveControlFeatureContext.PanTurnMode
        {
            get { return _features.ScreenStateFeature._panTurnMode; }
            set { _features.ScreenStateFeature._panTurnMode = value; }
        }

        bool IMoveControlFeatureContext.PrecisionStopUncertain
        {
            get { return _features.WebAgentMotionFocusFeature._precisionStopUncertain; }
            set { _features.WebAgentMotionFocusFeature._precisionStopUncertain = value; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext.PresetDirectScanCts
        {
            get { return ref _features.ScreenStateFeature._presetDirectScanCts; }
        }

        ref global::System.Threading.CancellationTokenSource IMoveControlFeatureContext.PresetSingleMoveCts
        {
            get { return ref _features.ScreenStateFeature._presetSingleMoveCts; }
        }

        int IMoveControlFeatureContext.PresetSkippedFocusMask
        {
            get { return _features.PresetLensFeature._presetSkippedFocusMask; }
            set { _features.PresetLensFeature._presetSkippedFocusMask = value; }
        }

        CancellationTokenSource IMoveControlFeatureContext.RooftopFocusSyncCts
        {
            get { return _features.ScreenStateFeature._rooftopFocusSyncCts; }
            set { _features.ScreenStateFeature._rooftopFocusSyncCts = value; }
        }

        CancellationTokenSource IMoveControlFeatureContext.RooftopZoomSyncCts
        {
            get { return _features.ScreenStateFeature._rooftopZoomSyncCts; }
            set { _features.ScreenStateFeature._rooftopZoomSyncCts = value; }
        }

        ControlAgentProfileOption IMoveControlFeatureContext.SelectedControlAgentProfileState
        {
            get { return _features.RtspSettingsFeature._selectedControlAgentProfile; }
            set { _features.RtspSettingsFeature._selectedControlAgentProfile = value; }
        }

        Services.Communication.WebAgent.IWebAgentZoomControlServicePort IMoveControlFeatureContext.WebAgentZoomControlService
        {
            get { return _features.Runtime._webAgentZoomControlService; }
        }

        /// <summary>
        /// 연동 버튼과 공통 정지는 기존 단일 프리셋 이동의 취소 경로를 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.CancelPanoramaPresets()
        {
            _features.PanoramaPresetsFeature.CancelPanoramaPresets();
        }

        /// <summary>
        /// 현재 위치·센서·렌즈 상태를 일관된 스냅샷으로 취득한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Models.Position.PositionSnapshot IMoveControlFeatureContext.CapturePositionSnapshot(string source, int? presetId)
        {
            return _features.PositionFeature.CapturePositionSnapshot(source, presetId);
        }

        /// <summary>
        /// Keyboard 방향키 입력 상태 초기화 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.ClearKeyboardPanTiltPressedState()
        {
            _features.ContinuousControlFeature.ClearKeyboardPanTiltPressedState();
        }

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Services.Configuration.LensControlTuning IMoveControlFeatureContext.ControlTuningFor(bool ir)
        {
            return _features.InputCoordinatorFeature.ControlTuningFor(ir);
        }

        /// <summary>
        /// 일반 WEB 줌은 수신·송신 위치를 그대로 사용한다. LA의 기존 방향 반전만 유지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IMoveControlFeatureContext.ConvertIrZoomStandardToStatusPosition(int standardPosition)
        {
            return _features.SyncFeature.ConvertIrZoomStandardToStatusPosition(standardPosition);
        }

        /// <summary>
        /// 현재 프리셋과 설정된 순회 순서로 자동 감시 대기열을 만든다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        PresetPointOption[] IMoveControlFeatureContext.CreatePresetScanQueue(System.Collections.Generic.IEnumerable<PresetPointOption> source)
        {
            return _features.PresetStorageFeature.CreatePresetScanQueue(source);
        }

        string IMoveControlFeatureContext.CurrentEoFocusText
        {
            get { return _features.PropertiesFeature.CurrentEoFocusText; }
        }

        string IMoveControlFeatureContext.CurrentEoZoomText
        {
            get { return _features.PropertiesFeature.CurrentEoZoomText; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlPanMaximum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlPanMaximum; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlPanMinimum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlPanMinimum; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double IMoveControlFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features.ScreenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        /// <summary>
        /// IR 피드백을 확보하지 못하면 절대위치 성공/시간추정으로 프리셋 완료를 표시하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token)
        {
            return _features.PresetLensFeature.EnsurePresetIrLensFeedbackAsync(zoom, token);
        }

        // 직접 입력과 프리셋이 같은 LA 절대 줌 구현·상태 판정을 공유한다.
        Task<bool> IMoveControlFeatureContext.MoveLaIrZoomAbsoluteAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token, string requestType)
        {
            return _features.PresetLensFeature.MoveLaIrZoomAbsoluteAsync(rawTarget, standardTarget, tolerance, token, requestType);
        }

        /// <summary>
        /// 새 수신값으로 ZERO 적용을 확인한 경우에만 좌표 변환과 기존 프리셋 스냅샷을 함께 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IMoveControlFeatureContext.FinishPanoramaZeroReferenceAsync(PanoramaZeroTicket ticket, bool sent)
        {
            return _features.PanoramaReferenceFeature.FinishPanoramaZeroReferenceAsync(ticket, sent);
        }

        int? IMoveControlFeatureContext.FocusPositionValue
        {
            get { return _features.PropertiesFeature.FocusPositionValue; }
            set { _features.PropertiesFeature.FocusPositionValue = value; }
        }

        /// <summary>
        /// 완료한 이동과 적용하지 않은 저장 초점을 화면에서 구분한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        string IMoveControlFeatureContext.FormatPresetCompletion(string completed)
        {
            return _features.PresetLensFeature.FormatPresetCompletion(completed);
        }

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IMoveControlFeatureContext.HasFreshPresetLens(bool ir, bool zoom)
        {
            return _features.PresetLensFeature.HasFreshPresetLens(ir, zoom);
        }

        string IMoveControlFeatureContext.HomeZeroLockMessage
        {
            get { return _features.PropertiesFeature.HomeZeroLockMessage; }
            set { _features.PropertiesFeature.HomeZeroLockMessage = value; }
        }

        string IMoveControlFeatureContext.HomeZeroLockTitle
        {
            get { return _features.PropertiesFeature.HomeZeroLockTitle; }
            set { _features.PropertiesFeature.HomeZeroLockTitle = value; }
        }

        string IMoveControlFeatureContext.HomeZeroStatusText
        {
            get { return _features.PropertiesFeature.HomeZeroStatusText; }
            set { _features.PropertiesFeature.HomeZeroStatusText = value; }
        }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IMoveControlFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features.WebAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IMoveControlFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features.PropertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IMoveControlFeatureContext.IsHomePositionMoving
        {
            get { return _features.PropertiesFeature.IsHomePositionMoving; }
            set { _features.PropertiesFeature.IsHomePositionMoving = value; }
        }

        bool IMoveControlFeatureContext.IsLaPresetScanRunning
        {
            get { return _features.PropertiesFeature.IsLaPresetScanRunning; }
            set { _features.PropertiesFeature.IsLaPresetScanRunning = value; }
        }

        bool IMoveControlFeatureContext.IsPanoramaPresetRunning
        {
            get { return _features.PanoramaPresetsFeature.IsPanoramaPresetRunning; }
        }

        /// <summary>
        /// 프리셋에서는 자동 초점 채널의 초점만 제외하며 팬·틸트·줌 이동은 차단하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IMoveControlFeatureContext.IsPresetFocusProtected(bool ir)
        {
            return _features.PresetLensFeature.IsPresetFocusProtected(ir);
        }

        bool IMoveControlFeatureContext.IsPresetScanRunning
        {
            get { return _features.PropertiesFeature.IsPresetScanRunning; }
            set { _features.PropertiesFeature.IsPresetScanRunning = value; }
        }

        bool IMoveControlFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        string IMoveControlFeatureContext.LaPresetCommandStatusText
        {
            get { return _features.PropertiesFeature.LaPresetCommandStatusText; }
            set { _features.PropertiesFeature.LaPresetCommandStatusText = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IMoveControlFeatureContext.LaPresetPoints
        {
            get { return _features.PropertiesFeature.LaPresetPoints; }
        }

        int IMoveControlFeatureContext.LaPresetScanDelay
        {
            get { return _features.PropertiesFeature.LaPresetScanDelay; }
            set { _features.PropertiesFeature.LaPresetScanDelay = value; }
        }

        int IMoveControlFeatureContext.LaPresetScanSpeed
        {
            get { return _features.PropertiesFeature.LaPresetScanSpeed; }
            set { _features.PropertiesFeature.LaPresetScanSpeed = value; }
        }

        int IMoveControlFeatureContext.LaPresetSlotNumber
        {
            get { return _features.PropertiesFeature.LaPresetSlotNumber; }
            set { _features.PropertiesFeature.LaPresetSlotNumber = value; }
        }

        string IMoveControlFeatureContext.McbMaintenanceIpAddress
        {
            get { return _features.PropertiesFeature.McbMaintenanceIpAddress; }
            set { _features.PropertiesFeature.McbMaintenanceIpAddress = value; }
        }

        double IMoveControlFeatureContext.MoveControlEoMaximumZoomRatio
        {
            get { return _features.ScreenStateFeature.MoveControlEoMaximumZoomRatio; }
        }

        double IMoveControlFeatureContext.MoveControlMinimumZoomRatio
        {
            get { return _features.ScreenStateFeature.MoveControlMinimumZoomRatio; }
        }

        /// <summary>
        /// IR Focus를 목표 Position으로 이동한다. 사용 명령: - Direct : Command2 0x2B / Position Big
        /// Endian - Near Start : Command2 0x31 / Data1 0x03 - Far Start : Command2 0x31 /
        /// Data1 0x04 - Focus Stop : Command2 0x31 / Data1 0x05 상태 확인: - Function 0x07의
        /// IR Focus Position 0 ~ 1000 0x2B 직접 절대이동을 우선 시도한다. WEB은 명시적인 미지원 응답에만 기존 제어로
        /// 전환하며 단순 지연·미도달에는 명령을 겹쳐 보내지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.MoveIrFocusToPositionAsync(int targetPosition, CancellationToken cancellationToken, int tolerance, bool explicitAbsolute)
        {
            return _features.SyncFeature.MoveIrFocusToPositionAsync(targetPosition, cancellationToken, tolerance, explicitAbsolute);
        }

        /// <summary>
        /// 2026-10-16: 환경부 장비는 신규 CMD2=0xB7 절대위치를 우선 사용하고 실제 IR 렌즈 피드백으로 도달 여부를 확인한다. 새
        /// 피드백이 계속 수신되는데도 위치가 전혀 변하지 않을 때만 0x31 폐루프 경로로 전환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget, int tolerance, bool syncCalibration, bool explicitAbsolute)
        {
            return _features.SyncFeature.MoveIrZoomToPositionAsync(targetPosition, cancellationToken, standardTarget, tolerance, syncCalibration, explicitAbsolute);
        }

        /// <summary>
        /// 옥상장비 CTEC EO Focus를 목표 Raw Position으로 직접 이동한다. VISCA Focus Direct Position 명령을
        /// 한 번만 송신하고, TCP Port 9000 Focus Position Inquiry 응답은 목표 도착 확인 용도로만 사용한다. 실제 처리는
        /// 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.MoveRooftopEoFocusToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken)
        {
            return _features.SyncFeature.MoveRooftopEoFocusToRawPositionAsync(ctecSource, targetRawPosition, cancellationToken);
        }

        /// <summary>
        /// 옥상장비 CTEC EO Zoom을 목표 Raw Position으로 직접 이동한다. 기존 TELE / WIDE 연속 이동 기반 Sync는 다음
        /// 문제가 있었다. 1. TCP Position 응답이 계단식으로 늦게 반영됨 2. 프로그램이 목표 통과를 늦게 확인함 3. 다음 단계에서 반대
        /// 방향 보정이 발생함 4. 화면이 확대 → 축소 → 확대 형태로 왕복함 현재 방식은 VISCA Zoom Direct 명령으로 목표 Raw
        /// Position을 한 번만 송신하고, 이후 Inquiry는 도착 확인 용도로만 사용한다. 따라서 Sync 이동 중 TELE / WIDE 방향
        /// 전환 및 재보정은 수행하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.MoveRooftopEoZoomToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken)
        {
            return _features.SyncFeature.MoveRooftopEoZoomToRawPositionAsync(ctecSource, targetRawPosition, cancellationToken);
        }

        /// <summary>
        /// 상태 표시·새 프리셋 저장은 SYNC 끝점 보정과 독립적인 장비 위치를 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IMoveControlFeatureContext.NormalizeIrZoomStatusPosition(int rawPosition)
        {
            return _features.SyncFeature.NormalizeIrZoomStatusPosition(rawPosition);
        }

        double? IMoveControlFeatureContext.PanAbsoluteValue
        {
            get { return _features.PropertiesFeature.PanAbsoluteValue; }
            set { _features.PropertiesFeature.PanAbsoluteValue = value; }
        }

        Services.Video.PanoramaReferenceState IMoveControlFeatureContext.PanoramaReference
        {
            get { return _features.PanoramaReferenceFeature.PanoramaReference; }
        }

        byte IMoveControlFeatureContext.PanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.PanTiltSpeedLevel; }
            set { _features.PropertiesFeature.PanTiltSpeedLevel = value; }
        }

        /// <summary>
        /// 정지 후 새 위치 세 개가 안정된 경우에만 ZERO 직전 기준을 확보한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<PanoramaZeroTicket> IMoveControlFeatureContext.PreparePanoramaZeroReferenceAsync(bool panAxis)
        {
            return _features.PanoramaReferenceFeature.PreparePanoramaZeroReferenceAsync(panAxis);
        }

        /// <summary>
        /// 프리셋 추가·갱신 전에 저장에 필요한 항목을 준비한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.PreparePresetForUpsert(PresetPointOption newPreset, System.Collections.Generic.IEnumerable<PresetPointOption> presets, PresetPointOption existingPreset)
        {
            _features.PresetStorageFeature.PreparePresetForUpsert(newPreset, presets, existingPreset);
        }

        string IMoveControlFeatureContext.PresetCommandStatusText
        {
            get { return _features.PropertiesFeature.PresetCommandStatusText; }
            set { _features.PropertiesFeature.PresetCommandStatusText = value; }
        }

        string IMoveControlFeatureContext.PresetFocusSkipText
        {
            get { return _features.PresetLensFeature.PresetFocusSkipText; }
        }

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IMoveControlFeatureContext.PresetIrZoomPosition(PresetPointOption preset)
        {
            return _features.PresetLensFeature.PresetIrZoomPosition(preset);
        }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> IMoveControlFeatureContext.PresetPoints
        {
            get { return _features.PropertiesFeature.PresetPoints; }
        }

        int IMoveControlFeatureContext.PresetScanDelay
        {
            get { return _features.PropertiesFeature.PresetScanDelay; }
            set { _features.PropertiesFeature.PresetScanDelay = value; }
        }

        int IMoveControlFeatureContext.PresetScanSpeed
        {
            get { return _features.PropertiesFeature.PresetScanSpeed; }
            set { _features.PropertiesFeature.PresetScanSpeed = value; }
        }

        int IMoveControlFeatureContext.PresetSlotNumber
        {
            get { return _features.PropertiesFeature.PresetSlotNumber; }
            set { _features.PropertiesFeature.PresetSlotNumber = value; }
        }

        /// <summary>
        /// Config의 변경 기록을 먼저 확정하므로 CSV 저장 중 종료되어도 같은 보정을 한 번만 적용한다. 실제 처리는 연결된 기능 객체에
        /// 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.RecoverPanoramaPresetReference()
        {
            _features.PanoramaReferenceFeature.RecoverPanoramaPresetReference();
        }

        /// <summary>
        /// 저장된 순서에 맞춰 화면 프리셋 목록을 정렬한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.ReorderPresetCollections()
        {
            _features.PresetStorageFeature.ReorderPresetCollections();
        }

        bool IMoveControlFeatureContext.RequiresDirectPresetRestore
        {
            get { return _features.PresetLensFeature.RequiresDirectPresetRestore; }
        }

        /// <summary>
        /// 장비 제어용 키보드 상태 전체 초기화 HOME POSITION 시작/완료/실패/Timeout 시 공통으로 호출한다. 현재 구현: - 방향키
        /// Pan/Tilt 상태 초기화 - WASD Pan/Tilt 상태 초기화 방향키와 WASD는 동일한 Pan/Tilt 상태 필드를 사용하므로
        /// ResetKeyboardPanTiltState 호출 한 번으로 함께 초기화된다. Zoom/Focus 단축키는 현재 별도 눌림 상태를 보관하지
        /// 않고 Window PreviewKeyDown/PreviewKeyUp 입구에서 HOME Lock 중 전체 차단한다. 추후 Zoom/Focus
        /// 키 상태 필드가 추가되면 반드시 이 함수에서 같이 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.ResetAllKeyboardControlState()
        {
            _features.ContinuousControlFeature.ResetAllKeyboardControlState();
        }

        /// <summary>
        /// 프리셋에 저장한 IR 광학 줌 위치를 복원한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.RestorePresetIrZoomAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token)
        {
            return _features.PresetLensFeature.RestorePresetIrZoomAsync(rawTarget, standardTarget, tolerance, token);
        }

        /// <summary>
        /// 선택 채널의 줌 도착을 확인한 뒤 정지 시간을 시작하고 그동안 저장 초점을 복원한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.RestorePresetLensAsync(int ez, int ef, int iz, int iff, int delaySeconds, bool scanTiming, CancellationToken token, bool? onlyIr)
        {
            return _features.PresetLensFeature.RestorePresetLensAsync(ez, ef, iz, iff, delaySeconds, scanTiming, token, onlyIr);
        }

        /// <summary>
        /// 장비 저장 PRESET W도 로컬에 저장한 네 렌즈 값을 복원·검증한 뒤 완료 처리한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.RestoreStoredPresetLensAsync(PresetPointOption preset, CancellationToken token, bool scanTiming)
        {
            return _features.PresetLensFeature.RestoreStoredPresetLensAsync(preset, token, scanTiming);
        }

        /// <summary>
        /// 현재 프리셋과 순서 정보를 저장하며 기존 파일 보존 규칙을 적용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IMoveControlFeatureContext.SavePresetStorage()
        {
            return _features.PresetStorageFeature.SavePresetStorage();
        }

        EquipmentStatusMode IMoveControlFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features.PropertiesFeature.SelectedEquipmentStatusMode; }
            set { _features.PropertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        PresetPointOption IMoveControlFeatureContext.SelectedLaPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedLaPresetPoint; }
            set { _features.PropertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IMoveControlFeatureContext.SelectedPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedPresetPoint; }
            set { _features.PropertiesFeature.SelectedPresetPoint = value; }
        }

        /// <summary>
        /// 연속 이동 정지 버튼 [MouseUp] 또는 [MouseLeave] 시 호출된다. 옥상 MR300 EO Zoom / Focus는 CTEC
        /// CGI 전용 Stop 명령을 송신하고, 그 외 제어는 기존 Control Agent Stop 명령을 유지한다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.StopContinuousMove()
        {
            _features.ContinuousControlFeature.StopContinuousMove();
        }

        /// <summary>
        /// 진행 중인 Focus Sync 확인 작업과 장비 이동을 정지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IMoveControlFeatureContext.StopFocusSyncAsync(bool stopEo, bool stopIr)
        {
            return _features.SyncFeature.StopFocusSyncAsync(stopEo, stopIr);
        }

        /// <summary>
        /// 진행 중 정밀 동작 상태를 정리하고 필요할 때 양 축 정지를 요청한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IMoveControlFeatureContext.StopPrecisionMotion(string reason, bool force)
        {
            _features.WebAgentMotionFocusFeature.StopPrecisionMotion(reason, force);
        }

        /// <summary>
        /// 진행 중인 줌 동기화를 취소하고 관련 렌즈 이동을 중지한다.
        /// </summary>
        Task IMoveControlFeatureContext.StopZoomSyncAsync()
        {
            return _features.SyncFeature.StopZoomSyncAsync();
        }

        double? IMoveControlFeatureContext.TiltAbsoluteValue
        {
            get { return _features.PropertiesFeature.TiltAbsoluteValue; }
            set { _features.PropertiesFeature.TiltAbsoluteValue = value; }
        }

        /// <summary>
        /// 새 EO 렌즈 응답이 목표 허용 범위에 연속 도달할 때까지 기다린다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.WaitForEnvironmentEoLensTargetAsync(int target, bool zoom, long afterSequence, CancellationToken cancellationToken)
        {
            return _features.SyncFeature.WaitForEnvironmentEoLensTargetAsync(target, zoom, afterSequence, cancellationToken);
        }

        /// <summary>
        /// EO 단발 Direct 이후 해당 축의 새 상태가 두 번 목표에 도달했는지 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IMoveControlFeatureContext.WaitPresetEoLensTargetAsync(int target, bool zoom, long afterTicks, CancellationToken token, int tolerance)
        {
            return _features.PresetLensFeature.WaitPresetEoLensTargetAsync(target, zoom, afterTicks, token, tolerance);
        }

        int? IMoveControlFeatureContext.ZoomPositionValue
        {
            get { return _features.PropertiesFeature.ZoomPositionValue; }
            set { _features.PropertiesFeature.ZoomPositionValue = value; }
        }

        double? IMoveControlFeatureContext.ZoomRatioValue
        {
            get { return _features.PropertiesFeature.ZoomRatioValue; }
            set { _features.PropertiesFeature.ZoomRatioValue = value; }
        }
    }
}
