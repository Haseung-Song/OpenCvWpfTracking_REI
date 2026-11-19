using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Control;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>절대 이동과 정지에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IMoveControlFeatureContext
    {
        ref double? _activePanAbsoluteTarget { get; }

        KeyboardPanTiltDirection _activePanTiltMoveDirection { get; set; }

        ref double? _activeTiltAbsoluteTarget { get; }

        RtspSourceOption _connectedEoCtecSource { get; set; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        ushort _currentCtecEoFocusPosition { get; set; }

        ushort _currentCtecEoZoomPosition { get; set; }

        short _currentEoFocus { get; set; }

        short _currentEoZoom { get; set; }

        ushort _currentIrFocus { get; set; }

        ushort _currentIrZoom { get; set; }

        KeyboardPanTiltDirection _currentKeyboardPanTiltDirection { get; set; }

        ContinuousMoveType _currentMoveType { get; set; }

        double _currentPan { get; set; }

        double _currentTilt { get; set; }

        ref long _eoLensStatusVersion { get; }

        SemaphoreSlim _homeZeroOperationLock { get; }

        ref global::System.Threading.CancellationTokenSource _laPresetDirectScanCts { get; }

        ref global::System.Threading.CancellationTokenSource _laPresetSingleMoveCts { get; }

        ref double? _lastPanAbsoluteTarget { get; }

        Services.Communication.ITcpClientServicePort _laTcpService { get; }

        Services.Communication.IMcbMaintenanceCommandServicePort _mcbMaintenanceCommandService { get; }

        ref global::System.Threading.CancellationTokenSource _moveControlPanCts { get; }

        bool _panoramaPanTiltConfirmed { get; set; }

        CancellationTokenSource _panoramaPresetCts { get; set; }

        ref long _panTiltStatusVersion { get; }

        PanTurnMode _panTurnMode { get; set; }

        bool _precisionStopUncertain { get; set; }

        ref global::System.Threading.CancellationTokenSource _presetDirectScanCts { get; }

        ref global::System.Threading.CancellationTokenSource _presetSingleMoveCts { get; }

        int _presetSkippedFocusMask { get; set; }

        CancellationTokenSource _rooftopFocusSyncCts { get; set; }

        CancellationTokenSource _rooftopZoomSyncCts { get; set; }

        ControlAgentProfileOption _selectedControlAgentProfile { get; set; }

        Services.Communication.WebAgent.IWebAgentZoomControlServicePort _webAgentZoomControlService { get; }

        /// <summary>
        /// 연동 버튼과 공통 정지는 기존 단일 프리셋 이동의 취소 경로를 그대로 사용한다.
        /// </summary>
        void CancelPanoramaPresets();

        /// <summary>
        /// 현재 위치·센서·렌즈 상태를 일관된 스냅샷으로 취득한다.
        /// </summary>
        Models.Position.PositionSnapshot CapturePositionSnapshot(string source, int? presetId = null);

        /// <summary>
        /// Keyboard 방향키 입력 상태 초기화
        /// </summary>
        void ClearKeyboardPanTiltPressedState();

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다.
        /// </summary>
        Services.Configuration.LensControlTuning ControlTuningFor(bool ir);

        /// <summary>
        /// 일반 WEB 줌은 수신·송신 위치를 그대로 사용한다. LA의 기존 방향 반전만 유지한다.
        /// </summary>
        int ConvertIrZoomStandardToStatusPosition(int standardPosition);

        /// <summary>
        /// 현재 프리셋과 설정된 순회 순서로 자동 감시 대기열을 만든다.
        /// </summary>
        PresetPointOption[] CreatePresetScanQueue(System.Collections.Generic.IEnumerable<PresetPointOption> source);

        string CurrentEoFocusText { get; }

        string CurrentEoZoomText { get; }

        double CurrentMoveControlPanMaximum { get; }

        double CurrentMoveControlPanMinimum { get; }

        double CurrentMoveControlTiltMaximum { get; }

        double CurrentMoveControlTiltMinimum { get; }

        /// <summary>
        /// IR 피드백을 확보하지 못하면 절대위치 성공/시간추정으로 프리셋 완료를 표시하지 않는다.
        /// </summary>
        Task<bool> EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token);

        /// <summary>
        /// LA 프리셋·직접 입력은 기존 화각 위치 이동 0x29를 한 번 송신하고 실제 도착을 확인한다.
        /// </summary>
        Task<bool> MoveLaIrZoomAbsoluteAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token, string requestType = "PRESET");

        /// <summary>
        /// 새 수신값으로 ZERO 적용을 확인한 경우에만 좌표 변환과 기존 프리셋 스냅샷을 함께 갱신한다.
        /// </summary>
        Task FinishPanoramaZeroReferenceAsync(PanoramaZeroTicket ticket, bool sent);

        int? FocusPositionValue { get; set; }

        /// <summary>
        /// 완료한 이동과 적용하지 않은 저장 초점을 화면에서 구분한다.
        /// </summary>
        string FormatPresetCompletion(string completed);

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다.
        /// </summary>
        bool HasFreshPresetLens(bool ir, bool zoom);

        string HomeZeroLockMessage { get; set; }

        string HomeZeroLockTitle { get; set; }

        string HomeZeroStatusText { get; set; }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다.
        /// </summary>
        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsEnvironmentStatusSelected { get; }

        bool IsHomePositionMoving { get; set; }

        bool IsLaPresetScanRunning { get; set; }

        bool IsPanoramaPresetRunning { get; }

        /// <summary>
        /// 프리셋에서는 자동 초점 채널의 초점만 제외하며 팬·틸트·줌 이동은 차단하지 않는다.
        /// </summary>
        bool IsPresetFocusProtected(bool ir);

        bool IsPresetScanRunning { get; set; }

        bool IsSelectedControlAgentWebAgent { get; }

        string LaPresetCommandStatusText { get; set; }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> LaPresetPoints { get; }

        int LaPresetScanDelay { get; set; }

        int LaPresetScanSpeed { get; set; }

        int LaPresetSlotNumber { get; set; }

        string McbMaintenanceIpAddress { get; set; }

        double MoveControlEoMaximumZoomRatio { get; }

        double MoveControlMinimumZoomRatio { get; }

        /// <summary>
        /// IR Focus를 목표 Position으로 이동한다. 사용 명령: - Direct : Command2 0x2B / Position Big
        /// Endian - Near Start : Command2 0x31 / Data1 0x03 - Far Start : Command2 0x31 /
        /// Data1 0x04 - Focus Stop : Command2 0x31 / Data1 0x05 상태 확인: - Function 0x07의
        /// IR Focus Position 0 ~ 1000 0x2B 직접 절대이동을 우선 시도한다. WEB은 명시적인 미지원 응답에만 기존 제어로
        /// 전환하며 단순 지연·미도달에는 명령을 겹쳐 보내지 않는다.
        /// </summary>
        Task<bool> MoveIrFocusToPositionAsync(int targetPosition, CancellationToken cancellationToken, int tolerance = IrFocusSyncTolerance, bool explicitAbsolute = false);

        /// <summary>
        /// 2026-10-16: 환경부 장비는 신규 CMD2=0xB7 절대위치를 우선 사용하고 실제 IR 렌즈 피드백으로 도달 여부를 확인한다. 새
        /// 피드백이 계속 수신되는데도 위치가 전혀 변하지 않을 때만 0x31 폐루프 경로로 전환한다.
        /// </summary>
        Task<bool> MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget = -1, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false, bool explicitAbsolute = false);

        /// <summary>
        /// 옥상장비 CTEC EO Focus를 목표 Raw Position으로 직접 이동한다. VISCA Focus Direct Position 명령을
        /// 한 번만 송신하고, TCP Port 9000 Focus Position Inquiry 응답은 목표 도착 확인 용도로만 사용한다.
        /// </summary>
        Task<bool> MoveRooftopEoFocusToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken);

        /// <summary>
        /// 옥상장비 CTEC EO Zoom을 목표 Raw Position으로 직접 이동한다. 기존 TELE / WIDE 연속 이동 기반 Sync는 다음
        /// 문제가 있었다. 1. TCP Position 응답이 계단식으로 늦게 반영됨 2. 프로그램이 목표 통과를 늦게 확인함 3. 다음 단계에서 반대
        /// 방향 보정이 발생함 4. 화면이 확대 → 축소 → 확대 형태로 왕복함 현재 방식은 VISCA Zoom Direct 명령으로 목표 Raw
        /// Position을 한 번만 송신하고, 이후 Inquiry는 도착 확인 용도로만 사용한다. 따라서 Sync 이동 중 TELE / WIDE 방향
        /// 전환 및 재보정은 수행하지 않는다.
        /// </summary>
        Task<bool> MoveRooftopEoZoomToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken);

        /// <summary>
        /// 상태 표시·새 프리셋 저장은 SYNC 끝점 보정과 독립적인 장비 위치를 사용한다.
        /// </summary>
        int NormalizeIrZoomStatusPosition(int rawPosition);

        double? PanAbsoluteValue { get; set; }

        Services.Video.PanoramaReferenceState PanoramaReference { get; }

        byte PanTiltSpeedLevel { get; set; }

        /// <summary>
        /// 정지 후 새 위치 세 개가 안정된 경우에만 ZERO 직전 기준을 확보한다.
        /// </summary>
        Task<PanoramaZeroTicket> PreparePanoramaZeroReferenceAsync(bool panAxis);

        /// <summary>
        /// 프리셋 추가·갱신 전에 저장에 필요한 항목을 준비한다.
        /// </summary>
        void PreparePresetForUpsert(PresetPointOption newPreset, System.Collections.Generic.IEnumerable<PresetPointOption> presets, PresetPointOption existingPreset);

        string PresetCommandStatusText { get; set; }

        string PresetFocusSkipText { get; }

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다.
        /// </summary>
        int PresetIrZoomPosition(PresetPointOption preset);

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> PresetPoints { get; }

        int PresetScanDelay { get; set; }

        int PresetScanSpeed { get; set; }

        int PresetSlotNumber { get; set; }

        /// <summary>
        /// Config의 변경 기록을 먼저 확정하므로 CSV 저장 중 종료되어도 같은 보정을 한 번만 적용한다.
        /// </summary>
        void RecoverPanoramaPresetReference();

        /// <summary>
        /// 저장된 순서에 맞춰 화면 프리셋 목록을 정렬한다.
        /// </summary>
        void ReorderPresetCollections();

        bool RequiresDirectPresetRestore { get; }

        /// <summary>
        /// 장비 제어용 키보드 상태 전체 초기화 HOME POSITION 시작/완료/실패/Timeout 시 공통으로 호출한다. 현재 구현: - 방향키
        /// Pan/Tilt 상태 초기화 - WASD Pan/Tilt 상태 초기화 방향키와 WASD는 동일한 Pan/Tilt 상태 필드를 사용하므로
        /// ResetKeyboardPanTiltState 호출 한 번으로 함께 초기화된다. Zoom/Focus 단축키는 현재 별도 눌림 상태를 보관하지
        /// 않고 Window PreviewKeyDown/PreviewKeyUp 입구에서 HOME Lock 중 전체 차단한다. 추후 Zoom/Focus
        /// 키 상태 필드가 추가되면 반드시 이 함수에서 같이 초기화한다.
        /// </summary>
        void ResetAllKeyboardControlState();

        /// <summary>
        /// 프리셋에 저장한 IR 광학 줌 위치를 복원한다.
        /// </summary>
        Task<bool> RestorePresetIrZoomAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token);

        /// <summary>
        /// 선택 채널의 줌 도착을 확인한 뒤 정지 시간을 시작하고 그동안 저장 초점을 복원한다.
        /// </summary>
        Task<bool> RestorePresetLensAsync(int ez, int ef, int iz, int iff, int delaySeconds, bool scanTiming, CancellationToken token, bool? onlyIr = null);

        /// <summary>
        /// 장비 저장 PRESET W도 로컬에 저장한 네 렌즈 값을 복원·검증한 뒤 완료 처리한다.
        /// </summary>
        Task<bool> RestoreStoredPresetLensAsync(PresetPointOption preset, CancellationToken token, bool scanTiming = false);

        /// <summary>
        /// 현재 프리셋과 순서 정보를 저장하며 기존 파일 보존 규칙을 적용한다.
        /// </summary>
        bool SavePresetStorage();

        EquipmentStatusMode SelectedEquipmentStatusMode { get; set; }

        PresetPointOption SelectedLaPresetPoint { get; set; }

        PresetPointOption SelectedPresetPoint { get; set; }

        /// <summary>
        /// 연속 이동 정지 버튼 [MouseUp] 또는 [MouseLeave] 시 호출된다. 옥상 MR300 EO Zoom / Focus는 CTEC
        /// CGI 전용 Stop 명령을 송신하고, 그 외 제어는 기존 Control Agent Stop 명령을 유지한다.
        /// </summary>
        void StopContinuousMove();

        /// <summary>
        /// 진행 중인 Focus Sync 확인 작업과 장비 이동을 정지한다.
        /// </summary>
        Task StopFocusSyncAsync(bool stopEo = true, bool stopIr = true);

        /// <summary>
        /// 진행 중 정밀 동작 상태를 정리하고 필요할 때 양 축 정지를 요청한다.
        /// </summary>
        void StopPrecisionMotion(string reason, bool force = false);

        /// <summary>
        /// 진행 중인 줌 동기화를 취소하고 관련 렌즈 이동을 중지한다.
        /// </summary>
        Task StopZoomSyncAsync();

        double? TiltAbsoluteValue { get; set; }

        /// <summary>
        /// 새 EO 렌즈 응답이 목표 허용 범위에 연속 도달할 때까지 기다린다.
        /// </summary>
        Task<bool> WaitForEnvironmentEoLensTargetAsync(int target, bool zoom, long afterSequence, CancellationToken cancellationToken);

        /// <summary>
        /// EO 단발 Direct 이후 해당 축의 새 상태가 두 번 목표에 도달했는지 확인한다.
        /// </summary>
        Task<bool> WaitPresetEoLensTargetAsync(int target, bool zoom, long afterTicks, CancellationToken token, int tolerance = LensSyncTargetTolerance);

        int? ZoomPositionValue { get; set; }

        double? ZoomRatioValue { get; set; }
    }

    /// <summary>절대 이동과 정지 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class MoveControlFeature
    {

        #region [협력 객체와 초기화]

        private readonly IMoveControlFeatureContext _context;

        /// <summary>
        /// PTZF 이동·프리셋 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal MoveControlFeature(IMoveControlFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 이동 제어 입력값을 VertiportNexus 기본값으로 초기화한다. Pan Absolute : 0 (-180 ~ 180) Tilt
        /// Absolute : 0 (-90 ~ 90) Zoom Position : 0 Zoom Ratio : 1.0 Focus Position: 0
        /// </summary>
        internal void ResetMoveControlInput()
        {
            _context.PanAbsoluteValue = 0.0;
            _context.TiltAbsoluteValue = 0.0;
            _context.ZoomPositionValue = 0;
            _context.ZoomRatioValue = 1.0;
            _context.FocusPositionValue = 0;

            Console.WriteLine();

            Console.WriteLine("[MOVE CONTROL] INPUT RESET");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 현재 설정된 Pan / Tilt 좌표계의 0°, 0° 위치로 절대 이동한다. 기존 LA 0xB1 명령은 기계식
        /// Homing(StartHoming) 명령이므로 카메라가 홈 센서 방향으로 이동한 뒤 해당 위치를 원점으로 잡는다. UI의 HOME
        /// POSITION은 사용자가 설정한 좌표 원점으로 복귀하는 기능이므로 Pan 0°(0x45), Tilt 0°(0x47) 절대 이동 명령을
        /// 사용한다.
        /// </summary>
        internal async Task MoveHomePositionAsync()
        {
            // HOME은 같은 좌표계의 위치 이동이므로 파노라마 연동을 유지한다.
            ConsoleLogHelper.Command("HOME / ZERO", "HOME POSITION requested");

            // 2026-09-16: ROOFTOP/ENVIRONMENT 모두 기존 절대 위치 HOME 경로를 공유한다.
            // 연결되지 않은 상태에서는 어떤 장비에도 명령을 송신하지 않는다.
            if (!_context._laTcpService.IsConnected)
            {
                _context.HomeZeroStatusText = "CONTROL NOT CONNECTED";

                ConsoleLogHelper.Warning("HOME / ZERO", "HOME POSITION skipped / control not connected");

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] HOME POSITION SKIPPED / CONTROL NOT CONNECTED");

                ConsoleLogHelper.PrintLine();

                return;
            }

            if (!await _context._homeZeroOperationLock.WaitAsync(0))
            {
                ConsoleLogHelper.Warning("HOME / ZERO", "HOME POSITION ignored / HOME or ZERO operation already running");

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] HOME POSITION IGNORED / OPERATION LOCKED");

                ConsoleLogHelper.PrintLine();

                return;
            }

            try
            {
                // 기존에 진행 중이던 모든 키보드 제어 상태를 먼저 초기화한다.
                //
                // 현재 키보드 연속 제어 상태는 Pan/Tilt 방향키와 WASD가
                // 동일한 KeyboardPanTiltDirection 상태를 공유한다.
                // 향후 Zoom/Focus 단축키 상태가 추가되더라도 이 공통 함수에서
                // 함께 초기화하도록 진입점을 하나로 유지한다.
                //
                // 이 시점의 Stop은 HOME 명령보다 먼저 1회만 송신한다.
                _context.ResetAllKeyboardControlState();

                if (_context._currentMoveType != ContinuousMoveType.None)
                {
                    _context.StopContinuousMove();

                    await Task.Delay(150);
                }

                _context.HomeZeroLockTitle = "HOME POSITION";
                _context.HomeZeroLockMessage = "MOVING TO HOME POSITION...";

                SetHomePositionMovingState(true);

                _context.HomeZeroStatusText = "HOME POSITION MOVING...";

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] HOME POSITION START / UI+KEYBOARD LOCKED");

                ConsoleLogHelper.PrintLine();

                bool stopResult = _context._controlCommandService.StopMove();

                await Task.Delay(150);

                bool modeResult = ApplySelectedPanTurnMode();

                // 2026-09-17: Web Agent가 연속 도착한 설정/이동 패킷을 누락하지
                // 않도록 회전 모드 적용 뒤 절대 위치 명령 사이에 처리 시간을 둔다.
                await Task.Delay(100);

                bool panResult = modeResult && _context._controlCommandService.PanGoPosition(0.0);

                await Task.Delay(100);

                bool tiltResult = _context._controlCommandService.TiltGoPosition(0.0);

                bool sendResult = panResult && tiltResult;

                if (panResult)
                {
                    _context._lastPanAbsoluteTarget = 0.0;
                }

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] HOME POSITION ABSOLUTE " + $"/ STOP={stopResult} " + $"/ PAN_MODE={modeResult} " + $"/ PAN=0.00:{panResult} " + $"/ TILT=0.00:{tiltResult} " + $"/ RESULT={sendResult}");

                ConsoleLogHelper.PrintLine();

                if (!sendResult)
                {
                    _context.HomeZeroStatusText = "HOME POSITION SEND FAILED";

                    ConsoleLogHelper.Error("HOME / ZERO", $"HOME POSITION command send failed / STOP={stopResult} / PAN_MODE={modeResult} / PAN={panResult} / TILT={tiltResult}");

                    return;
                }

                bool isCompleted = await WaitHomePositionCompletedAsync();

                _context.HomeZeroStatusText = isCompleted ? "HOME POSITION COMPLETE" : "HOME POSITION WAIT TIMEOUT";

                ConsoleLogHelper.StateSection("HOME / ZERO", isCompleted ? "HOME POSITION complete" : "HOME POSITION timeout", string.Empty, $"PAN  : {_context._currentPan:F2}", $"TILT : {_context._currentTilt:F2}");
            }
            catch (Exception ex)
            {
                _context.HomeZeroStatusText = "HOME POSITION FAILED";

                ConsoleLogHelper.Error("HOME / ZERO", "HOME POSITION failed", ex);

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] HOME POSITION ERROR : " + ex.Message);

                ConsoleLogHelper.PrintLine();
            }
            finally
            {
                // 정상 완료, 송신 실패, 예외, 30초 Timeout 등
                // 어떤 종료 경로에서도 눌림 상태를 모두 제거한 뒤 Lock을 해제한다.
                //
                // HOME 이동 중에는 Reset 함수가 별도 Stop 패킷을 송신하지 않으므로
                // HOME 완료 직전에 불필요한 STOP이 끼어들지 않는다.
                _context.ResetAllKeyboardControlState();

                SetHomePositionMovingState(false);

                ConsoleLogHelper.StateSection("HOME / ZERO", "UI and keyboard unlocked");

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] UI+KEYBOARD UNLOCKED " + "/ ARROW+WASD+ZOOM+FOCUS INPUT ENABLED");

                ConsoleLogHelper.PrintLine();

                _context._homeZeroOperationLock.Release();
            }
        }

        /// <summary>
        /// HOME POSITION 완료 여부를 Pan/Tilt 상태값으로 판단한다. 단순 고정 Delay로 완료 처리하지 않고, Pan/Tilt가
        /// 0도 허용 오차 안에 들어온 상태에서 연속 5회 안정적으로 유지될 때 완료로 판정한다. 최대 대기시간을 초과하면 Timeout으로 종료한다.
        /// </summary>
        internal async Task<bool> WaitHomePositionCompletedAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var stabilityTracker = new HomePositionStabilityTracker(_context._currentPan, _context._currentTilt, HomePositionTargetTolerance, HomePositionStableTolerance, HomePositionStableSampleCount);

            // 명령 직후 이전 상태 Packet을 완료로 오판하지 않도록
            // 최소 이동 시작 시간을 확보한다.
            await Task.Delay(300);

            while (stopwatch.ElapsedMilliseconds < HomePositionTimeoutMs)
            {
                double currentPan = _context._currentPan;

                double currentTilt = _context._currentTilt;

                bool isCompleted = stabilityTracker.Update(currentPan, currentTilt);

                Console.WriteLine("[HOME / ZERO] WAIT " + $"/ PAN={currentPan:F2} " + $"/ TILT={currentTilt:F2} " + $"/ NEAR={stabilityTracker.IsNearTarget} " + $"/ STABLE={stabilityTracker.StableCount}/{HomePositionStableSampleCount} " + $"/ ELAPSED={stopwatch.ElapsedMilliseconds}ms");

                if (isCompleted)
                {
                    return true;
                }

                await Task.Delay(HomePositionPollingIntervalMs);
            }

            ConsoleLogHelper.Warning("HOME / ZERO", $"HOME POSITION timeout / PAN={_context._currentPan:F2} / TILT={_context._currentTilt:F2}");

            return false;
        }

        /// <summary>
        /// HOME POSITION 이동 상태를 반영하고 관련 UI Binding을 갱신한다.
        /// </summary>
        internal void SetHomePositionMovingState(bool isMoving)
        {
            _context.IsHomePositionMoving = isMoving;

            // HOME Lock 진입/해제 양쪽에서 남아 있는 방향키/WASD 상태를 제거한다.
            //
            // Zoom/Focus를 포함한 모든 키 입력은 MainWindow의
            // PreviewKeyDown/PreviewKeyUp 최상단에서 IsHomePositionMoving을
            // 확인하여 HOME 진행 중 전체 차단한다.
            _context.ClearKeyboardPanTiltPressedState();

            _context._currentKeyboardPanTiltDirection = KeyboardPanTiltDirection.None;
        }

        /// <summary>
        /// 현재 Pan Encoder 위치를 즉시 0으로 설정한다. MCB Set0 명령: MO=0;PX=0;MO=1;
        /// </summary>
        internal async Task SetPanZeroAsync()
        {
            ConsoleLogHelper.Command("HOME / ZERO", "PAN ZERO requested");

            if (!await _context._homeZeroOperationLock.WaitAsync(0))
            {
                _context.HomeZeroStatusText = "HOME / ZERO OPERATION BUSY";

                ConsoleLogHelper.Warning("HOME / ZERO", "PAN ZERO ignored / HOME or ZERO operation already running");

                return;
            }

            _context.HomeZeroLockTitle = "PAN ZERO";
            _context.HomeZeroLockMessage = "PAN MOTOR STABILIZING...";

            SetHomePositionMovingState(true);

            try
            {
                var panoramaZero = await _context.PreparePanoramaZeroReferenceAsync(true);

                bool useWebAgent = _context._selectedControlAgentProfile?.AgentType == ControlAgentType.WebAgent;

                if (useWebAgent)
                {
                    _context.HomeZeroLockMessage = "SENDING PAN ZERO REQUEST TO WEB AGENT...";

                    bool sent = _context._controlCommandService.RequestWebAgentPositionZero(0x01);

                    ConsoleLogHelper.Command("HOME / ZERO", "PAN ZERO WebAgent request " + "/ PACKET=FF 01 00 B3 01 00 B5 " + $"/ SENT={sent}");

                    _context.HomeZeroStatusText = sent ? "PAN ZERO REQUEST SENT / WEB AGENT" : "PAN ZERO REQUEST SEND FAILED";

                    _context.HomeZeroLockMessage = sent ? "PAN ZERO REQUEST SENT / APPLYING..." : "PAN ZERO REQUEST SEND FAILED";

                    await Task.Delay(WebAgentZeroOverlayMinimumMilliseconds);

                    await _context.FinishPanoramaZeroReferenceAsync(panoramaZero, sent);

                    return;
                }

                _context.HomeZeroStatusText = "PAN ZERO / MOTOR STABILIZING...";

                string mcbIpAddress = GetMcbMaintenanceIpAddress();

                _context.HomeZeroLockMessage = "PAN MOTOR STABILIZING...";

                bool result = await _context._mcbMaintenanceCommandService.SetPanZeroAsync(mcbIpAddress, McbMaintenancePort);

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] PAN SET ORIGIN " + $"/ MCB={mcbIpAddress}:{McbMaintenancePort} " + $"/ SEQUENCE=],),|2,( " + $"/ MOTOR_STABILIZATION=1000ms " + $"/ RESULT={result}");

                ConsoleLogHelper.PrintLine();

                _context.HomeZeroStatusText = result ? "PAN ZERO COMPLETE / MOTOR READY" : "PAN ZERO SEND FAILED";

                await _context.FinishPanoramaZeroReferenceAsync(panoramaZero, result);
            }
            catch (Exception ex)
            {
                _context.HomeZeroStatusText = "PAN ZERO FAILED";

                ConsoleLogHelper.Error("HOME / ZERO", "PAN ZERO failed", ex);
            }
            finally
            {
                _context.ResetAllKeyboardControlState();

                SetHomePositionMovingState(false);

                _context._homeZeroOperationLock.Release();
            }
        }

        /// <summary>
        /// 현재 Tilt Encoder 위치를 즉시 0으로 설정한다. MCB Set0 명령: MO=0;PX=0;MO=1;
        /// </summary>
        internal async Task SetTiltZeroAsync()
        {
            ConsoleLogHelper.Command("HOME / ZERO", "TILT ZERO requested");

            if (!await _context._homeZeroOperationLock.WaitAsync(0))
            {
                _context.HomeZeroStatusText = "HOME / ZERO OPERATION BUSY";

                ConsoleLogHelper.Warning("HOME / ZERO", "TILT ZERO ignored / HOME or ZERO operation already running");

                return;
            }

            // TILT ZERO의 Stop / Motor Off / Position Zero / Motor On과
            // 마지막 모터 안정화 대기가 모두 끝날 때까지 기존 HOME Lock을 유지한다.
            // 이 구간에는 HOME, 방향키, WASD, Zoom/Focus 입력이 차단된다.
            _context.HomeZeroLockTitle = "TILT ZERO";
            _context.HomeZeroLockMessage = "TILT MOTOR STABILIZING...";

            SetHomePositionMovingState(true);

            try
            {
                var panoramaZero = await _context.PreparePanoramaZeroReferenceAsync(false);

                bool useWebAgent = _context._selectedControlAgentProfile?.AgentType == ControlAgentType.WebAgent;

                if (useWebAgent)
                {
                    _context.HomeZeroLockMessage = "SENDING TILT ZERO REQUEST TO WEB AGENT...";

                    bool sent = _context._controlCommandService.RequestWebAgentPositionZero(0x02);

                    ConsoleLogHelper.Command("HOME / ZERO", "TILT ZERO WebAgent request " + "/ PACKET=FF 01 00 B3 02 00 B6 " + $"/ SENT={sent}");

                    _context.HomeZeroStatusText = sent ? "TILT ZERO REQUEST SENT / WEB AGENT" : "TILT ZERO REQUEST SEND FAILED";

                    _context.HomeZeroLockMessage = sent ? "TILT ZERO REQUEST SENT / APPLYING..." : "TILT ZERO REQUEST SEND FAILED";

                    await Task.Delay(WebAgentZeroOverlayMinimumMilliseconds);

                    // 2026-10-01: WebAgent가 TILT ZERO 성공 직후 Function 0x02를
                    // 자동 송신하지만, 누락 여부를 즉시 확인할 수 있도록 제한값도
                    // 한 번 더 명시적으로 조회한다.
                    bool limitQuerySent = _context._controlCommandService.RequestWebAgentTiltLimits();

                    ConsoleLogHelper.Command("WEB AGENT TILT LIMIT", "Post TILT ZERO query / " + "PACKET=FF 01 00 B5 02 00 B8 / " + "SENT=" + limitQuerySent);

                    await _context.FinishPanoramaZeroReferenceAsync(panoramaZero, sent);

                    return;
                }

                _context.HomeZeroStatusText = "TILT ZERO / MOTOR STABILIZING...";

                string mcbIpAddress = GetMcbMaintenanceIpAddress();

                bool result = await _context._mcbMaintenanceCommandService.SetTiltZeroAsync(mcbIpAddress, McbMaintenancePort);

                Console.WriteLine();

                Console.WriteLine("[HOME / ZERO] TILT SET ORIGIN " + $"/ MCB={mcbIpAddress}:{McbMaintenancePort} " + $"/ SEQUENCE=],),|2,( " + $"/ MOTOR_STABILIZATION=1000ms " + $"/ RESULT={result}");

                ConsoleLogHelper.PrintLine();

                _context.HomeZeroStatusText = result ? "TILT ZERO COMPLETE / MOTOR READY" : "TILT ZERO SEND FAILED";

                await _context.FinishPanoramaZeroReferenceAsync(panoramaZero, result);
            }
            catch (Exception ex)
            {
                _context.HomeZeroStatusText = "TILT ZERO FAILED";

                ConsoleLogHelper.Error("HOME / ZERO", "TILT ZERO failed", ex);
            }
            finally
            {
                _context.ResetAllKeyboardControlState();

                SetHomePositionMovingState(false);

                _context._homeZeroOperationLock.Release();
            }
        }

        /// <summary>
        /// 옥상 MCB 유지보수 직접 연결 IP를 반환한다. Control Agent는 Local LA(기본 127.0.0.1:5001),
        /// 2026-08-26: MCB는 실장비(기본 192.168.0.112:4001), SCB는 4002를 사용하므로 두 주소를 서로 공유하지
        /// 않는다.
        /// </summary>
        internal string GetMcbMaintenanceIpAddress()
        {
            string ipAddress = _context.McbMaintenanceIpAddress?.Trim();

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return "192.168.0.112";
            }
            else
            {
                return ipAddress;
            }
        }

        /// <summary>
        /// 현재 PAN / TILT SPEED UI 값으로 위치 이동 속도를 설정한다. Pan Position Speed : Command2 0x49
        /// Tilt Position Speed: Command2 0x4B UI 1~60 값을 위치 이동 속도 1~60 deg/s로 적용하며, UI 0은
        /// 위치 이동을 시작하지 않고 STOP으로 처리한다.
        /// </summary>
        internal bool ApplyPanTiltPositionSpeedFromUi(out double positionSpeed)
        {
            positionSpeed = _context.PanTiltSpeedLevel;

            if (positionSpeed <= 0.0)
            {
                bool stopResult = _context._controlCommandService.StopMove();

                ClearActivePanTiltAbsoluteMove();

                _context._currentMoveType = ContinuousMoveType.None;
                _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.None;

                ConsoleLogHelper.Warning("PAN / TILT SPEED", "Position move blocked / UI_SPEED=0 / " + $"STOP_RESULT={stopResult}");

                return false;
            }

            bool panSpeedResult = _context._controlCommandService.SetPanPositionSpeed(positionSpeed);

            bool tiltSpeedResult = _context._controlCommandService.SetTiltPositionSpeed(positionSpeed);

            return panSpeedResult && tiltSpeedResult;
        }

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다.
        /// </summary>
        internal void ClearActivePanTiltAbsoluteMove()
        {
            _context._activePanAbsoluteTarget = null;
            _context._activeTiltAbsoluteTarget = null;
        }

        /// <summary>
        /// [PAN] / [TILT] Absolute 위치 이동 정지 방향 버튼 중앙의 STOP은 연속 속도 이동(0x00)을 정지한다.
        /// Absolute 이동은 위치 이동 전용 정지 명령인 0x4F를 사용해야 하므로, Pan 정지(Data1=0x01)와 Tilt
        /// 정지(Data1=0x02)를 각각 송신한다.
        /// </summary>
        internal void StopAbsoluteMove()
        {
            _context.StopPrecisionMotion("절대 이동 STOP", _context._precisionStopUncertain);

            bool stopResult = _context._controlCommandService.StopPanTiltPositionMove();

            ClearActivePanTiltAbsoluteMove();

            ConsoleLogHelper.Command("ABSOLUTE MOVE", $"Position stop requested / CMD2=0x4F / RESULT={stopResult}");
        }

        /// <summary>
        /// 위치 이동 중 속도 변경 시 현재 Absolute 목표를 그대로 재송신한다. 일부 장비는 0x49 / 0x4B 속도 설정을 다음 위치 이동
        /// 명령부터 적용하므로, 진행 중인 0x45 / 0x47 목표를 다시 보내야 변경된 속도가 현재 이동에도 즉시 반영된다.
        /// </summary>
        internal void ReapplyActivePanTiltAbsoluteTarget(double positionSpeed)
        {
            double? panTarget = _context._activePanAbsoluteTarget;

            double? tiltTarget = _context._activeTiltAbsoluteTarget;

            bool hasPanTarget = panTarget.HasValue;

            bool hasTiltTarget = tiltTarget.HasValue;

            if (!hasPanTarget && !hasTiltTarget)
            {
                return;
            }

            bool panResult = !hasPanTarget || _context._controlCommandService.PanGoPosition(panTarget.Value);

            bool tiltResult = !hasTiltTarget || _context._controlCommandService.TiltGoPosition(tiltTarget.Value);

            ConsoleLogHelper.Command("PAN / TILT SPEED", "Absolute target reapplied / " + $"UI_SPEED={_context.PanTiltSpeedLevel} / " + $"POSITION_SPEED={positionSpeed:F2}deg/s / " + $"PAN_TARGET={(panTarget.HasValue ? panTarget.Value.ToString("F2") : "-")} / " + $"TILT_TARGET={(tiltTarget.HasValue ? tiltTarget.Value.ToString("F2") : "-")} / " + $"RESULT={panResult && tiltResult}");
        }

        /// <summary>
        /// 입력한 Pan 각도를 검사하고 선택한 회전 방식·속도에 맞춰 절대 이동을 요청한다.
        /// </summary>
        internal async Task MovePanAbsoluteFromInputAsync()
        {
            if (!_context.PanAbsoluteValue.HasValue)
            {
                Console.WriteLine("[MOVE CONTROL] PAN ABSOLUTE FAILED : EMPTY VALUE");

                return;
            }

            double targetPan = Clamp(RoundAngleToProtocolScale(_context.PanAbsoluteValue.Value), _context.CurrentMoveControlPanMinimum, _context.CurrentMoveControlPanMaximum);

            // 2026-09-18: GUI/VM은 -180~180 signed 값을 보존한다.
            // WEB AGENT용 반전 0~360 좌표는 PanGoPosition 송신 경계에서만 변환한다.
            _context.PanAbsoluteValue = targetPan;

            CancelMoveControlPanOperation();

            bool speedResult = ApplyPanTiltPositionSpeedFromUi(out double positionSpeed);

            if (!speedResult)
            {
                return;
            }

            // 2026-09-21: Web Agent는 연속 패킷 처리 중 앞선 설정 명령을
            // 누락하는 실장비 사례가 있어 MOE 검증값 100ms를 적용한다.
            // LA Agent는 기존 80ms 동작을 유지한다.
            await Task.Delay(_context.IsEnvironmentStatusSelected ? 100 : 80);

            bool modeResult = ApplySelectedPanTurnMode();

            if (!modeResult)
            {
                Console.WriteLine();

                Console.WriteLine("[MOVE CONTROL] PAN ABSOLUTE FAILED : " + "PAN TURN MODE COMMAND FAILED");

                ConsoleLogHelper.PrintLine();

                return;
            }

            await Task.Delay(_context.IsEnvironmentStatusSelected ? 100 : 80);

            bool moveResult = _context._controlCommandService.PanGoPosition(targetPan);

            if (moveResult)
            {
                _context._lastPanAbsoluteTarget = targetPan;
                _context._activePanAbsoluteTarget = targetPan;
            }

            Console.WriteLine();

            Console.WriteLine("[MOVE CONTROL] PAN ABSOLUTE / " + $"MODE={_context._panTurnMode}");

            Console.WriteLine($"[MOVE CONTROL] CURRENT={_context._currentPan:F2} / " + $"TARGET={targetPan:F2} / " + $"UI SPEED={_context.PanTiltSpeedLevel} / " + $"POSITION SPEED={positionSpeed:F2}deg/s / " + $"SPEED COMMAND={speedResult} / " + $"MODE COMMAND={modeResult} / " + $"MOVE COMMAND={moveResult}");

            ConsoleLogHelper.PrintLine();

            return;
        }

        /// <summary>
        /// 현재 RadioButton에서 선택한 Pan 회전 모드를 Pelco-D 확장 명령 0x4D로 장비에 적용한다.
        /// </summary>
        internal bool ApplySelectedPanTurnMode()
        {
            bool result;

            if (_context._panTurnMode == PanTurnMode.ViaZero)
            {
                result = _context._controlCommandService.SetPanViaZeroMode();

                Console.WriteLine("[MOVE CONTROL] PAN TURN MODE / " + "VIA ZERO / CMD=0x4D / DATA1=0x01 / " + $"RESULT={result}");
            }
            else
            {
                result = _context._controlCommandService.SetPanShortestPathMode();

                Console.WriteLine("[MOVE CONTROL] PAN TURN MODE / " + "SHORTEST PATH / CMD=0x4D / DATA1=0x02 / " + $"RESULT={result}");
            }

            return result;
        }

        /// <summary>
        /// Tilt Absolute 이동 요청
        /// </summary>
        internal async void MoveTiltAbsoluteFromInput()
        {
            if (!_context.TiltAbsoluteValue.HasValue)
            {
                Console.WriteLine("[MOVE CONTROL] TILT ABSOLUTE FAILED : EMPTY VALUE");

                return;
            }

            double targetTilt = Clamp(RoundAngleToProtocolScale(_context.TiltAbsoluteValue.Value), _context.CurrentMoveControlTiltMinimum, _context.CurrentMoveControlTiltMaximum);

            _context.TiltAbsoluteValue = targetTilt;

            bool speedResult = ApplyPanTiltPositionSpeedFromUi(out double positionSpeed);

            if (!speedResult)
            {
                return;
            }

            // 2026-09-21: Web Agent가 Tilt 속도 설정을 처리한 뒤 절대 위치
            // 명령을 받도록 MOE 실장비 검증 간격을 동일하게 적용한다.
            if (_context.IsEnvironmentStatusSelected)
            {
                await Task.Delay(60);
            }

            Console.WriteLine();

            Console.WriteLine($"[MOVE CONTROL] TILT ABSOLUTE / TARGET={targetTilt:F2}");

            ConsoleLogHelper.PrintLine();

            bool moveResult = _context._controlCommandService.TiltGoPosition(targetTilt);

            if (moveResult)
            {
                _context._activeTiltAbsoluteTarget = targetTilt;
            }

            Console.WriteLine($"[MOVE CONTROL] UI SPEED={_context.PanTiltSpeedLevel} / " + $"POSITION SPEED={positionSpeed:F2}deg/s / " + $"SPEED COMMAND={speedResult} / " + $"MOVE COMMAND={moveResult}");
        }

        /// <summary>
        /// LA와 WEB 슬롯은 공유하지 않는다. 잘못된 명령 호출도 패킷 전송 전에 차단한다.
        /// </summary>
        internal bool EnsurePresetAgent(bool web)
        {
            if (web == _context.IsSelectedControlAgentWebAgent)
            {
                return true;
            }

            const string message = "PRESET BLOCKED : AGENT CHANGED";

            _context.LaPresetCommandStatusText = message;
            _context.PresetCommandStatusText = message;

            Console.WriteLine($"[PRESET] 다른 Agent 명령 차단 / REQUEST={(web ? "WEB" : "LA")} / ACTIVE={(_context.IsSelectedControlAgentWebAgent ? "WEB" : "LA")}");

            return false;
        }

        /// <summary>
        /// PRESET 1 (LA TEST) 등록. LA 실제 구현 순서: 0x19 ID ->; 0x91 Pan ->; 0x93 Tilt ->; 0x95
        /// Zoom ->; 0x97 Focus + AddPreset
        /// </summary>
        internal void AddOrUpdateLaPresetPoint()
        {
            if (!EnsurePresetAgent(false))
            {
                return;
            }

            ConsoleLogHelper.Command("PRESET L", "Register/update requested");

            int presetNumber = Math.Max(1, Math.Min(100, _context.LaPresetSlotNumber));

            ushort zoom = GetCurrentPresetStandardZoom();

            ushort focus = GetCurrentPresetStandardFocus();

            int laInternalId = presetNumber - 1;

            bool idResult = _context._controlCommandService.SetLaPresetId((ushort)laInternalId);

            bool panResult = idResult && _context._controlCommandService.SetLaPresetPan(_context._currentPan);

            bool tiltResult = panResult && _context._controlCommandService.SetLaPresetTilt(_context._currentTilt);

            bool zoomResult = tiltResult && _context._controlCommandService.SetLaPresetZoom(zoom);

            bool focusResult = zoomResult && _context._controlCommandService.SetLaPresetFocusAndCommit(focus);

            bool result = idResult && panResult && tiltResult && zoomResult && focusResult;

            Console.WriteLine();

            Console.WriteLine("[PRESET 1 / LA] ADD / UPDATE");

            Console.WriteLine($"[PRESET 1 / LA] DISPLAY=P{presetNumber:00} / " + $"INTERNAL_ID={laInternalId} / " + $"PAN={_context._currentPan:F2} / " + $"TILT={_context._currentTilt:F2} / " + $"ZOOM={zoom} / FOCUS={focus} / " + $"RESULT={result}");

            ConsoleLogHelper.PrintLine();

            if (!result)
            {
                _context.LaPresetCommandStatusText = $"P{presetNumber:00} REGISTER SEND FAILED";

                return;
            }

            PresetPointOption newPreset = new PresetPointOption(presetNumber, _context._currentPan, _context._currentTilt, zoom.ToString(), focus.ToString(), GetCurrentIrZoomStandardPosition().ToString(), GetCurrentIrFocusStandardPosition().ToString(), positionSnapshot: _context.CapturePositionSnapshot("PRESET_L_SAVE", presetNumber));

            UpsertLaPresetPoint(newPreset);

            _context.LaPresetCommandStatusText = $"P{presetNumber:00} LA SCAN POINT REGISTERED";
        }

        /// <summary>
        /// PRESET 1 선택 지점으로 WPF 저장값 기준 PTZF 전체를 직접 복원한다. LA 0x05 한 번에 의존하지 않고 Pan, Tilt,
        /// EO/IR Zoom, EO/IR Focus를 현재 프로젝트의 실제 제어 경로로 각각 실행한다.
        /// </summary>
        internal async Task MoveToSelectedLaPresetPointAsync()
        {
            if (!EnsurePresetAgent(false))
            {
                return;
            }

            _context.RecoverPanoramaPresetReference();

            if (_context.PanoramaReference.Pending)
            {
                _context.LaPresetCommandStatusText = "PRESET REFERENCE CHECK REQUIRED";

                return;
            }

            PresetPointOption preset = _context.SelectedLaPresetPoint;

            if (preset == null)
            {
                _context.LaPresetCommandStatusText = "MOVE FAILED : SELECT LA PRESET";

                return;
            }

            CancellationTokenSource moveCts = new CancellationTokenSource();

            CancellationTokenSource oldMoveCts = Interlocked.Exchange(ref _context._laPresetSingleMoveCts, moveCts);

            if (oldMoveCts != null)
            {
                oldMoveCts.Cancel();
            }

            _context.LaPresetCommandStatusText = $"P{preset.Number:00} DIRECT PTZF MOVING";

            try
            {
                bool result = await MoveLaPresetPointDirectAsync(preset, moveCts.Token);

                Console.WriteLine();

                Console.WriteLine("[PRESET 1 / WPF DIRECT] MOVE");

                Console.WriteLine($"[PRESET 1 / WPF DIRECT] DISPLAY=P{preset.Number:00} / " + $"PAN={preset.Pan:F2} / TILT={preset.Tilt:F2} / " + $"EO_ZOOM={preset.EoZoomText} / " + $"EO_FOCUS={preset.EoFocusText} / " + $"IR_ZOOM={preset.IrZoomText} / " + $"IR_FOCUS={preset.IrFocusText} / RESULT={result}");

                ConsoleLogHelper.PrintLine();

                _context.LaPresetCommandStatusText = result ? _context.FormatPresetCompletion($"P{preset.Number:00} DIRECT PTZF MOVE COMPLETED") : $"P{preset.Number:00} DIRECT PTZF MOVE INCOMPLETE";
            }
            catch (OperationCanceledException)
            {
                // Stop/Disconnect로 취소된 정상 종료 경로이다.
                _context.LaPresetCommandStatusText = $"P{preset.Number:00} DIRECT PTZF MOVE STOPPED";
            }
            finally
            {
                if (ReferenceEquals(Interlocked.CompareExchange(ref _context._laPresetSingleMoveCts, null, moveCts), moveCts))
                {
                    moveCts.Dispose();
                }
            }
        }

        /// <summary>
        /// 저장된 PRESET 1 한 지점의 PTZF 값을 직접 복원한다.
        /// </summary>
        internal async Task<bool> MoveLaPresetPointDirectAsync(PresetPointOption preset, CancellationToken cancellationToken, bool scanTiming = false, int? routeSpeed = null, int? routeDelay = null, bool? onlyIr = null)
        {
            _context._presetSkippedFocusMask = 0;

            if (preset == null)
            {
                return false;
            }

            int storedValue;

            if (!TryPresetLensValue(preset.EoZoomText, out storedValue) || !TryPresetLensValue(preset.EoFocusText, out storedValue) || !TryPresetLensValue(preset.IrZoomText, out storedValue) || !TryPresetLensValue(preset.IrFocusText, out storedValue))
            {
                ConsoleLogHelper.Warning("PRESET LENS", "Missing stored lens value; re-register preset");

                return false;
            }

            if (preset == null)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            int moveSpeed = Math.Max(1, Math.Min(60, routeSpeed ?? _context.LaPresetScanSpeed));

            int settleTimeoutMs = CalculatePresetPanTiltSettleTimeoutMs(preset, moveSpeed);

            bool panSpeedResult = _context._controlCommandService.SetPanPositionSpeed(moveSpeed);

            bool tiltSpeedResult = _context._controlCommandService.SetTiltPositionSpeed(moveSpeed);

            bool speedResult = panSpeedResult && tiltSpeedResult;

            // 2026-09-21: Web Agent 프리셋은 속도/회전 모드/Pan/Tilt 패킷을
            // 60ms 간격으로 직렬화한다. LA Agent 경로에는 지연을 추가하지 않는다.
            if (_context.IsEnvironmentStatusSelected)
            {
                await Task.Delay(60, cancellationToken);
            }

            bool modeResult = ApplySelectedPanTurnMode();

            if (_context.IsEnvironmentStatusSelected)
            {
                await Task.Delay(60, cancellationToken);
            }

            bool panResult = modeResult && speedResult && _context._controlCommandService.PanGoPosition(preset.Pan);

            if (_context.IsEnvironmentStatusSelected)
            {
                await Task.Delay(60, cancellationToken);
            }

            bool tiltResult = speedResult && _context._controlCommandService.TiltGoPosition(preset.Tilt);

            if (panResult)
            {
                _context._lastPanAbsoluteTarget = preset.Pan;
            }

            bool panTiltSettled = panResult && tiltResult && await EnsurePresetPanTiltTargetAsync("PRESET L", preset, () =>
            {
                bool retryPanSpeedResult = _context._controlCommandService.SetPanPositionSpeed(moveSpeed);

                bool retryTiltSpeedResult = _context._controlCommandService.SetTiltPositionSpeed(moveSpeed);

                bool retryModeResult = ApplySelectedPanTurnMode();

                bool retryPanResult = retryPanSpeedResult && retryTiltSpeedResult && retryModeResult && _context._controlCommandService.PanGoPosition(preset.Pan);

                bool retryTiltResult = retryPanSpeedResult && retryTiltSpeedResult && _context._controlCommandService.TiltGoPosition(preset.Tilt);

                return retryModeResult && retryPanSpeedResult && retryTiltSpeedResult && retryPanResult && retryTiltResult;
            }, settleTimeoutMs, cancellationToken);

            // 파노라마 이동에서는 실제 정착 확인 결과를 별도로 보존해 렌즈 미완료와 구분한다.
            if (_context.IsPanoramaPresetRunning)
            {
                _context._panoramaPanTiltConfirmed = panTiltSettled;
            }

            int eoZoom = ParsePresetPosition(preset.EoZoomText, GetCurrentPresetStandardZoom());

            int eoFocus = ParsePresetPosition(preset.EoFocusText, GetCurrentPresetStandardFocus());

            int irZoom = _context.PresetIrZoomPosition(preset);

            int irFocus = ParsePresetPosition(preset.IrFocusText, GetCurrentIrFocusStandardPosition());

            bool lensResult = panTiltSettled && await _context.RestorePresetLensAsync(eoZoom, eoFocus, irZoom, irFocus, routeDelay ?? _context.LaPresetScanDelay, scanTiming, cancellationToken, onlyIr);

            bool result = speedResult && modeResult && panResult && tiltResult && panTiltSettled && lensResult;

            Console.WriteLine();

            Console.WriteLine("[PRESET 1 / WPF DIRECT] PTZF RESULT");

            Console.WriteLine($"[PRESET 1 / WPF DIRECT] " + $"SPEED={moveSpeed}deg/s:{speedResult} / " + $"P={panResult} / T={tiltResult} / " + $"PT_SETTLED={panTiltSettled} / " + $"LENS={lensResult} / " + $"RESULT={result}");

            ConsoleLogHelper.PrintLine();

            return result;
        }

        /// <summary>
        /// 프리셋 Pan / Tilt 목표각 도달을 최신 상태 Packet으로 확인하고, 0.01° Protocol 위치가 일치하지 않으면 제한 횟수만
        /// 재보정한다.
        /// </summary>
        internal async Task<bool> EnsurePresetPanTiltTargetAsync(string category, PresetPointOption preset, Func<bool> resendCommand, int settleTimeoutMs, CancellationToken cancellationToken)
        {
            for (int correctionCount = 0; correctionCount <= PresetPanTiltCorrectionRetryCount; correctionCount++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (correctionCount > 0)
                {
                    double correctionPanDelta = GetShortestPanDifference(_context._currentPan, preset.Pan);

                    double correctionTiltDelta = GetTiltDifference(_context._currentTilt, preset.Tilt);

                    bool resendResult = resendCommand != null && resendCommand();

                    ConsoleLogHelper.Command(category, $"Pan/Tilt correction / " + $"COUNT={correctionCount}/" + $"{PresetPanTiltCorrectionRetryCount} / " + $"TARGET={preset.Pan:F2},{preset.Tilt:F2} / " + $"ACTUAL={_context._currentPan:F2},{_context._currentTilt:F2} / " + $"DELTA={correctionPanDelta:+0.00;-0.00;0.00}," + $"{correctionTiltDelta:+0.00;-0.00;0.00} / " + $"RESULT={resendResult}");

                    if (!resendResult)
                    {
                        return false;
                    }
                }

                bool settled = await WaitForPresetPanTiltTargetAsync(category, preset, correctionCount, settleTimeoutMs, cancellationToken);

                if (settled)
                {
                    return true;
                }
            }

            double finalPanDelta = GetShortestPanDifference(_context._currentPan, preset.Pan);

            double finalTiltDelta = GetTiltDifference(_context._currentTilt, preset.Tilt);

            ConsoleLogHelper.Warning(category, $"Pan/Tilt settle incomplete / " + $"TARGET_PAN={preset.Pan:F2} / " + $"ACTUAL_PAN={_context._currentPan:F2} / " + $"DELTA_PAN={finalPanDelta:+0.00;-0.00;0.00} / " + $"TARGET_TILT={preset.Tilt:F2} / " + $"ACTUAL_TILT={_context._currentTilt:F2} / " + $"DELTA_TILT={finalTiltDelta:+0.00;-0.00;0.00}");

            return false;
        }

        /// <summary>
        /// 명령 이후 새로 수신된 상태 Packet만 사용하여 목표각 안정화를 판정한다.
        /// </summary>
        internal async Task<bool> WaitForPresetPanTiltTargetAsync(string category, PresetPointOption preset, int correctionCount, int settleTimeoutMs, CancellationToken cancellationToken)
        {
            long observedVersion = Interlocked.Read(ref _context._panTiltStatusVersion);

            int stableCount = 0;

            int stationaryCount = 0;

            bool hasPreviousPosition = false;

            bool movementObserved = false;

            double previousPan = 0.0;

            double previousTilt = 0.0;

            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settleTimeoutMs)
            {
                await Task.Delay(100, cancellationToken);

                long currentVersion = Interlocked.Read(ref _context._panTiltStatusVersion);

                if (currentVersion == observedVersion)
                {
                    continue;
                }

                observedVersion = currentVersion;

                double currentPan = _context._currentPan;

                double currentTilt = _context._currentTilt;

                double panDelta = GetShortestPanDifference(currentPan, preset.Pan);

                double tiltDelta = GetTiltDifference(currentTilt, preset.Tilt);

                bool isTargetSample = Math.Abs(panDelta) <= PresetPanTiltTargetTolerance && Math.Abs(tiltDelta) <= PresetPanTiltTargetTolerance;

                stableCount = isTargetSample ? stableCount + 1 : 0;

                if (hasPreviousPosition)
                {
                    double panMovement = Math.Abs(GetShortestPanDifference(currentPan, previousPan));

                    double tiltMovement = Math.Abs(GetTiltDifference(currentTilt, previousTilt));

                    bool isMoving = panMovement > PresetPanTiltTargetTolerance || tiltMovement > PresetPanTiltTargetTolerance;

                    if (isMoving)
                    {
                        movementObserved = true;
                        stationaryCount = 0;
                    }
                    else if (movementObserved)
                    {
                        stationaryCount++;
                    }
                }

                previousPan = currentPan;
                previousTilt = currentTilt;
                hasPreviousPosition = true;

                if (stableCount >= PresetPanTiltStableSampleCount)
                {
                    ConsoleLogHelper.State(category, $"Pan/Tilt settled / " + $"TARGET={preset.Pan:F2},{preset.Tilt:F2} / " + $"ACTUAL={currentPan:F2},{currentTilt:F2} / " + $"CORRECTION={correctionCount}");

                    return true;
                }

                if (movementObserved && stationaryCount >= PresetPanTiltStationarySampleCount)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 위치와 목표 위치의 각도 차이 및 설정 속도로 통신 장애 방지용 안전 제한시간을 계산한다. 정상 도착 시 즉시 반환되므로 이 값은
        /// 사용자 Delay에 추가되지 않는다.
        /// </summary>
        internal int CalculatePresetPanTiltSettleTimeoutMs(PresetPointOption preset, double speedDegreesPerSecond)
        {
            double panDistance = Math.Abs(GetShortestPanDifference(_context._currentPan, preset.Pan));

            double tiltDistance = Math.Abs(GetTiltDifference(_context._currentTilt, preset.Tilt));

            double maximumDistance = Math.Max(panDistance, tiltDistance);

            double safeSpeed = Math.Max(1.0, speedDegreesPerSecond);

            int expectedTravelMs = (int)Math.Ceiling(maximumDistance / safeSpeed * 1000.0);

            return Math.Max(PresetPanTiltSettleTimeoutMinimumMs, Math.Min(PresetPanTiltSettleTimeoutMaximumMs, expectedTravelMs + PresetPanTiltSafetyTimeoutMarginMs));
        }

        /// <summary>
        /// ±180° 경계를 고려한 Pan 최단 각도 차이를 반환한다.
        /// </summary>
        internal static double GetShortestPanDifference(double currentPan, double targetPan)
        {
            double difference = currentPan - targetPan;

            while (difference > 180.0)
            {
                difference -= 360.0;
            }

            while (difference < -180.0)
            {
                difference += 360.0;
            }

            return difference;
        }

        /// <summary>
        /// Web Agent의 0/360도 경계를 고려한 Tilt 차이를 반환한다. ROOFTOP LA는 기존 선형 -90 ~ 90도 차이를 유지한다.
        /// </summary>
        internal double GetTiltDifference(double currentTilt, double targetTilt)
        {
            if (_context.IsEnvironmentStatusSelected)
            {
                return GetShortestPanDifference(currentTilt, targetTilt);
            }
            else
            {
                return currentTilt - targetTilt;
            }
        }

        /// <summary>
        /// 프리셋 위치 문자열을 정수로 해석하고 허용 범위로 제한하며 실패하면 기본값을 사용한다.
        /// </summary>
        internal static int ParsePresetPosition(string text, int fallback)
        {
            int parsed;

            if (!int.TryParse(text, out parsed))
            {
                parsed = fallback;
            }

            return Math.Max(MoveControlPositionMinimum, Math.Min(MoveControlPositionMaximum, parsed));
        }

        /// <summary>
        /// PRESET 복원 전에 EO/IR Zoom이 이미 목표 위치인지 확인한다. 이미 도착한 축에는 불필요한 Direct 명령과 완료 조회를
        /// 보내지 않는다.
        /// </summary>
        internal bool IsPresetEoZoomAtTarget(int standardTarget)
        {
            if (!_context.HasFreshPresetLens(false, true))
            {
                return false;
            }

            if (_context.SelectedEquipmentStatusMode != EquipmentStatusMode.Environment && _context._connectedEoCtecSource != null)
            {
                int rawTarget = ConvertStandardZoomToCtecRaw(standardTarget);

                return Math.Abs(_context._currentCtecEoZoomPosition - rawTarget) <= RooftopZoomSyncTolerance;
            }

            return Math.Abs(_context._currentEoZoom - standardTarget) <= 1;
        }

        /// <summary>
        /// 새 EO 초점 상태 응답이 장비별 변환을 적용한 프리셋 목표에 도달했는지 확인한다.
        /// </summary>
        internal bool IsPresetEoFocusAtTarget(int standardTarget)
        {
            if (!_context.HasFreshPresetLens(false, false))
            {
                return false;
            }

            if (_context.SelectedEquipmentStatusMode != EquipmentStatusMode.Environment && _context._connectedEoCtecSource != null)
            {
                int rawTarget = ConvertStandardFocusToCtecRaw(standardTarget);

                return Math.Abs(_context._currentCtecEoFocusPosition - rawTarget) <= RooftopFocusSyncTolerance;
            }

            return Math.Abs(_context._currentEoFocus - standardTarget) <= 1;
        }

        /// <summary>
        /// AUTO SCAN 시작 시 첫 프리셋의 전체 PTZF가 이미 현재 상태와 같은지 확인한다. 같은 지점이면 불필요한 복원과 Delay 없이
        /// 다음 프리셋을 즉시 시작한다.
        /// </summary>
        internal bool IsLaPresetPointAlreadyAtTarget(PresetPointOption preset)
        {
            if (!_context.HasFreshPresetLens(true, true) || !_context.HasFreshPresetLens(true, false))
            {
                return false;
            }

            int eoZoom = ParsePresetPosition(preset.EoZoomText, GetCurrentPresetStandardZoom());

            int eoFocus = ParsePresetPosition(preset.EoFocusText, GetCurrentPresetStandardFocus());

            int irZoom = _context.PresetIrZoomPosition(preset);

            int irFocus = ParsePresetPosition(preset.IrFocusText, GetCurrentIrFocusStandardPosition());

            return Math.Abs(GetShortestPanDifference(_context._currentPan, preset.Pan)) <= PresetPanTiltTargetTolerance && Math.Abs(GetTiltDifference(_context._currentTilt, preset.Tilt)) <= PresetPanTiltTargetTolerance && IsPresetEoZoomAtTarget(eoZoom) && IsPresetEoFocusAtTarget(eoFocus) && Math.Abs(GetCurrentIrZoomStandardPosition() - irZoom) <= 1 && Math.Abs(GetCurrentIrFocusStandardPosition() - irFocus) <= IrFocusSyncTolerance;
        }

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Zoom을 각각 복원한다.
        /// </summary>
        internal async Task<bool> MoveLaPresetZoomDirectAsync(int eoStandardPosition, int irPosition, CancellationToken cancellationToken, int eoTolerance = LensSyncTargetTolerance, int irTolerance = 1, bool? onlyIr = null)
        {
            int safeEoPosition = Clamp(eoStandardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            int safeIrPosition = Clamp(irPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            bool eoAlreadyAtTarget = _context.HasFreshPresetLens(false, true) && Math.Abs(GetCurrentPresetStandardZoom() - safeEoPosition) <= eoTolerance;

            bool irAlreadyAtTarget = _context.HasFreshPresetLens(true, true) && Math.Abs(GetCurrentIrZoomStandardPosition() - safeIrPosition) <= irTolerance;

            if (onlyIr == true)
            {
                eoAlreadyAtTarget = true;
            }

            if (onlyIr == false)
            {
                irAlreadyAtTarget = true;
            }

            if (eoAlreadyAtTarget && irAlreadyAtTarget)
            {
                Console.WriteLine("[PRESET 1 / WPF DIRECT] ZOOM " + $"/ EO={safeEoPosition}:SKIPPED " + $"/ IR={safeIrPosition}:SKIPPED");

                return true;
            }

            await _context.StopZoomSyncAsync();

            cancellationToken.ThrowIfCancellationRequested();

            // 2026-10-16: 환경부 IR은 신규 CMD2=0xB7 절대위치를 송신하고 실제 피드백으로 검증한다.
            // 확인된 무이동일 때만 0x31 경로를 사용하며 목표값으로 UI 현재값을 덮어쓰지 않는다.
            int irRawTarget = _context.ConvertIrZoomStandardToStatusPosition(safeIrPosition);

            Task<bool> irMoveTask = irAlreadyAtTarget ? Task.FromResult(true) : _context.RestorePresetIrZoomAsync(irRawTarget, safeIrPosition, Math.Max(IrZoomTargetTolerance, irTolerance), cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            long eoCommandTicks = DateTime.UtcNow.Ticks;

            bool eoResult;

            if (eoAlreadyAtTarget)
            {
                eoResult = true;
            }
            else if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
            {
                eoResult = _context._controlCommandService.EoZoomGoPosition((short)safeEoPosition);
            }
            else
            {
                RtspSourceOption ctecSource = _context._connectedEoCtecSource;

                if (ctecSource == null)
                {
                    eoResult = _context._controlCommandService.EoZoomGoPosition((short)safeEoPosition);
                }
                else
                {
                    int eoRawTarget = ConvertStandardZoomToCtecRaw(safeEoPosition);

                    eoResult = await _context.MoveRooftopEoZoomToRawPositionAsync(ctecSource, eoRawTarget, cancellationToken);
                }
            }

            bool irResult = await irMoveTask;

            cancellationToken.ThrowIfCancellationRequested();

            if (eoResult && !eoAlreadyAtTarget && (_context.IsEnvironmentStatusSelected || _context._connectedEoCtecSource == null))
            {
                eoResult = await _context.WaitPresetEoLensTargetAsync(safeEoPosition, true, eoCommandTicks, cancellationToken, eoTolerance);
            }

            ConsoleLogHelper.State("PRESET LENS", "ZOOM / EO_TARGET=" + safeEoPosition + " / IR_STANDARD_TARGET=" + safeIrPosition + " / IR_RAW_TARGET=" + irRawTarget + " / IR_ACTUAL=" + _context._currentIrZoom + " / EO_RESULT=" + eoResult + " / IR_RESULT=" + irResult);

            Console.WriteLine("[PRESET 1 / WPF DIRECT] ZOOM " + $"/ EO={safeEoPosition}:{eoResult} " + $"/ IR={safeIrPosition}:{irResult}");

            return eoResult && irResult;
        }

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Focus를 각각 복원한다.
        /// </summary>
        internal async Task<bool> MoveLaPresetFocusDirectAsync(int eoStandardPosition, int irStandardPosition, CancellationToken cancellationToken, int eoTolerance = LensSyncTargetTolerance, int irTolerance = IrFocusSyncTolerance, bool? onlyIr = null)
        {
            bool skipEoFocus = onlyIr != true && _context.IsPresetFocusProtected(false);

            bool skipIrFocus = onlyIr != false && _context.IsPresetFocusProtected(true);

            if (skipEoFocus || skipIrFocus)
            {
                _context._presetSkippedFocusMask |= (skipEoFocus ? 1 : 0) | (skipIrFocus ? 2 : 0);

                ConsoleLogHelper.State("PRESET FOCUS", _context.PresetFocusSkipText + " / 자동 초점·상태 미확인 채널의 저장 초점만 제외 / PTZ 이동 유지");
            }

            int safeEoPosition = Clamp(eoStandardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            int safeIrStandardPosition = Clamp(irStandardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            int irRawTargetPosition = ConvertIrFocusStandardToStatusPosition(safeIrStandardPosition);

            bool eoAlreadyAtTarget = skipEoFocus || _context.HasFreshPresetLens(false, false) && Math.Abs(GetCurrentPresetStandardFocus() - safeEoPosition) <= eoTolerance;

            bool irAlreadyAtTarget = skipIrFocus || _context.HasFreshPresetLens(true, false) && Math.Abs(GetCurrentIrFocusStandardPosition() - safeIrStandardPosition) <= irTolerance;

            if (onlyIr == true)
            {
                eoAlreadyAtTarget = true;
            }

            if (onlyIr == false)
            {
                irAlreadyAtTarget = true;
            }

            if (eoAlreadyAtTarget && irAlreadyAtTarget)
            {
                Console.WriteLine("[PRESET 1 / WPF DIRECT] FOCUS " + $"/ EO={safeEoPosition}:SKIPPED " + $"/ IR_STANDARD={safeIrStandardPosition}:SKIPPED");

                return true;
            }

            await _context.StopFocusSyncAsync(stopEo: !skipEoFocus && onlyIr != true, stopIr: !skipIrFocus && onlyIr != false);

            cancellationToken.ThrowIfCancellationRequested();

            Task<bool> irMoveTask;

            if (irAlreadyAtTarget)
            {
                irMoveTask = Task.FromResult(true);
            }
            else if (!await _context.EnsurePresetIrLensFeedbackAsync(false, cancellationToken))
            {
                irMoveTask = Task.FromResult(false);
            }
            else
            {
                irMoveTask = _context.MoveIrFocusToPositionAsync(irRawTargetPosition, cancellationToken, Math.Max(1, Math.Max(Math.Abs(ConvertIrFocusStandardToStatusPosition(Clamp(safeIrStandardPosition + irTolerance, 0, 1000)) - irRawTargetPosition), Math.Abs(ConvertIrFocusStandardToStatusPosition(Clamp(safeIrStandardPosition - irTolerance, 0, 1000)) - irRawTargetPosition))));
            }

            bool eoResult;

            long eoCommandTicks = DateTime.UtcNow.Ticks;

            if (eoAlreadyAtTarget)
            {
                eoResult = true;
            }
            else if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
            {
                eoResult = _context._controlCommandService.EoFocusGoPosition((short)safeEoPosition);
            }
            else
            {
                RtspSourceOption ctecSource = _context._connectedEoCtecSource;

                if (ctecSource == null)
                {
                    eoResult = _context._controlCommandService.EoFocusGoPosition((short)safeEoPosition);
                }
                else
                {
                    int eoRawTarget = ConvertStandardFocusToCtecRaw(safeEoPosition);

                    eoResult = await _context.MoveRooftopEoFocusToRawPositionAsync(ctecSource, eoRawTarget, cancellationToken);
                }
            }

            bool irResult = await irMoveTask;

            irResult = onlyIr == false || skipIrFocus || irResult && _context.HasFreshPresetLens(true, false) && Math.Abs(GetCurrentIrFocusStandardPosition() - safeIrStandardPosition) <= irTolerance;

            cancellationToken.ThrowIfCancellationRequested();

            if (eoResult && !eoAlreadyAtTarget && (_context.IsEnvironmentStatusSelected || _context._connectedEoCtecSource == null))
            {
                eoResult = await _context.WaitPresetEoLensTargetAsync(safeEoPosition, false, eoCommandTicks, cancellationToken, eoTolerance);
            }

            ConsoleLogHelper.State("PRESET LENS", "FOCUS / EO_TARGET=" + safeEoPosition + " / IR_STANDARD_TARGET=" + safeIrStandardPosition + " / IR_ACTUAL=" + GetCurrentIrFocusStandardPosition() + " / EO_RESULT=" + eoResult + " / IR_RESULT=" + irResult);

            Console.WriteLine("[PRESET 1 / WPF DIRECT] FOCUS " + $"/ EO={safeEoPosition}:{eoResult} " + $"/ IR_STANDARD={safeIrStandardPosition} " + $"/ IR_RAW_TARGET={irRawTargetPosition}:{irResult}");

            return eoResult && irResult;
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        internal ushort GetCurrentPresetStandardZoom()
        {
            if (_context._connectedEoCtecSource != null)
            {
                return (ushort)Math.Max(0, Math.Min(1000, (int)Math.Round(_context._currentCtecEoZoomPosition * 1000.0 / CtecEoZoomPositionMax)));
            }

            return (ushort)Math.Max(0, Math.Min(1000, (int)_context._currentEoZoom));
        }

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        internal ushort GetCurrentPresetStandardFocus()
        {
            if (_context._connectedEoCtecSource != null)
            {
                return (ushort)Math.Max(0, Math.Min(1000, (int)Math.Round(_context._currentCtecEoFocusPosition * 1000.0 / CtecEoFocusPositionMax)));
            }

            return (ushort)Math.Max(0, Math.Min(1000, (int)_context._currentEoFocus));
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        internal int GetCurrentIrZoomStandardPosition()
        {
            int safeRaw = Clamp((int)_context._currentIrZoom, MoveControlPositionMinimum, MoveControlPositionMaximum);

            return _context.NormalizeIrZoomStatusPosition(safeRaw);
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// ->; Standard 0(Far) Raw 0(Near) ->; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        internal int GetCurrentIrFocusStandardPosition()
        {
            int safeRaw = Clamp((int)_context._currentIrFocus, MoveControlPositionMinimum, MoveControlPositionMaximum);

            if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop)
            {
                return MoveControlPositionMaximum - safeRaw;
            }
            else
            {
                return safeRaw;
            }
        }

        /// <summary>
        /// 화면의 IR Focus 표준 목표값을 현재 장비가 보고하는 상태 좌표로 변환한다. LA 상태 좌표만 반대 방향이며, Web Agent 상태
        /// 좌표는 그대로 사용한다.
        /// </summary>
        internal int ConvertIrFocusStandardToStatusPosition(int standardPosition)
        {
            int safePosition = Clamp(standardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop)
            {
                return MoveControlPositionMaximum - safePosition;
            }
            else
            {
                return safePosition;
            }
        }

        /// <summary>
        /// PRESET 1 화면 목록에 같은 번호가 있으면 갱신하고, 없으면 번호순으로 삽입한다.
        /// </summary>
        internal void UpsertLaPresetPoint(PresetPointOption newPreset)
        {
            PresetPointOption existingPreset = _context.LaPresetPoints.FirstOrDefault(preset => preset.Number == newPreset.Number);

            _context.PreparePresetForUpsert(newPreset, _context.LaPresetPoints, existingPreset);

            if (existingPreset != null)
            {
                _context.LaPresetPoints.Remove(existingPreset);
            }

            _context.LaPresetPoints.Add(newPreset);

            _context.ReorderPresetCollections();

            _context.SelectedLaPresetPoint = newPreset;

            _context.SavePresetStorage();
        }

        /// <summary>
        /// LA 장비의 저장 프리셋 전체 삭제를 요청한다.
        /// </summary>
        internal void ClearAllLaPresetPoints()
        {
            if (!EnsurePresetAgent(false))
            {
                return;
            }

            StopLaPresetScan();

            bool result = _context._controlCommandService.ClearAllLaPresets();

            if (result)
            {
                _context.LaPresetPoints.Clear();

                _context.SelectedLaPresetPoint = null;
                _context.IsLaPresetScanRunning = false;

                _context.SavePresetStorage();
            }

            Console.WriteLine();

            Console.WriteLine("[PRESET 1 / LA] CLEAR ALL");

            Console.WriteLine($"[PRESET 1 / LA] RESULT={result}");

            ConsoleLogHelper.PrintLine();

            _context.LaPresetCommandStatusText = result ? "LA PRESET DATA CLEAR COMMAND SENT" : "LA PRESET DATA CLEAR SEND FAILED";
        }

        /// <summary>
        /// 2026-08-28: PRESET L의 선택 항목만 자동 순회 목록에서 제거한다. LA Agent는 개별 삭제 명령을 제공하지 않으므로 장비
        /// 전체 프리셋은 유지하고, 현재 Viewer의 선택 슬롯만 제거한다.
        /// </summary>
        internal void DeleteSelectedLaPresetPoint()
        {
            if (!EnsurePresetAgent(false))
            {
                return;
            }

            PresetPointOption selected = _context.SelectedLaPresetPoint;

            if (selected == null)
            {
                _context.LaPresetCommandStatusText = "DELETE FAILED : SELECT PRESET";

                return;
            }

            _context.LaPresetPoints.Remove(selected);

            _context.SelectedLaPresetPoint = _context.LaPresetPoints.FirstOrDefault();

            _context.SavePresetStorage();

            ConsoleLogHelper.State("PRESET L", "Selected scan point removed / NUMBER=" + selected.Number + " / REMAINING=" + _context.LaPresetPoints.Count);

            _context.LaPresetCommandStatusText = $"P{selected.Number:00} REMOVED FROM SCAN LIST";
        }

        /// <summary>
        /// PRESET 1 저장 목록을 WPF가 직접 P01 ->; P02 ->; ... 순회한다. 각 지점마다 PTZF 전체 직접 복원 루틴을 사용한다.
        /// </summary>
        internal async Task StartLaPresetScanAsync()
        {
            if (!EnsurePresetAgent(false))
            {
                return;
            }

            _context.RecoverPanoramaPresetReference();

            if (_context.PanoramaReference.Pending)
            {
                _context.LaPresetCommandStatusText = "PRESET REFERENCE CHECK REQUIRED";

                return;
            }

            ConsoleLogHelper.Command("PRESET L", "Scan start requested");

            if (_context.LaPresetPoints.Count < 2)
            {
                _context.LaPresetCommandStatusText = "SCAN START FAILED : REGISTER 2 OR MORE POINTS";

                return;
            }

            CancellationTokenSource oldCts = Interlocked.Exchange(ref _context._laPresetDirectScanCts, null);

            if (oldCts != null)
            {
                oldCts.Cancel();

                oldCts.Dispose();
            }

            CancellationTokenSource scanCts = new CancellationTokenSource();

            _context._laPresetDirectScanCts = scanCts;

            int delaySeconds = Math.Max(1, Math.Min(60, _context.LaPresetScanDelay));

            int speedDegreesPerSecond = Math.Max(1, Math.Min(60, _context.LaPresetScanSpeed));

            _context.IsLaPresetScanRunning = true;

            _context.LaPresetCommandStatusText = $"WPF DIRECT SCAN START / {_context.LaPresetPoints.Count} POINTS";

            Console.WriteLine();

            Console.WriteLine("[PRESET 1 / WPF DIRECT SCAN] START");

            Console.WriteLine($"[PRESET 1 / WPF DIRECT SCAN] " + $"POINTS={_context.LaPresetPoints.Count} / " + $"SPEED={speedDegreesPerSecond}deg/s / " + $"DELAY={delaySeconds}s");

            ConsoleLogHelper.PrintLine();

            try
            {
                bool isFirstPointOfScan = true;

                PresetPointOption[] points = _context.CreatePresetScanQueue(_context.LaPresetPoints);

                while (!scanCts.IsCancellationRequested)
                {
                    foreach (PresetPointOption point in points)
                    {
                        scanCts.Token.ThrowIfCancellationRequested();

                        if (isFirstPointOfScan && IsLaPresetPointAlreadyAtTarget(point))
                        {
                            isFirstPointOfScan = false;

                            _context.SelectedLaPresetPoint = point;

                            ConsoleLogHelper.Command("PRESET L", $"Initial current point skipped / P{point.Number:00} / " + "NEXT POINT STARTS IMMEDIATELY");

                            continue;
                        }

                        isFirstPointOfScan = false;

                        _context.SelectedLaPresetPoint = point;

                        _context.LaPresetCommandStatusText = $"SCAN MOVING P{point.Number:00}";

                        bool moveResult = await MoveLaPresetPointDirectAsync(point, scanCts.Token, true);

                        Console.WriteLine("[PRESET 1 / WPF DIRECT SCAN] " + $"P{point.Number:00} MOVE RESULT={moveResult}");

                        if (!moveResult)
                        {
                            ConsoleLogHelper.Warning("PRESET L", "이동 미완료 / 다음 지점 이동 없이 자동 순회를 중단합니다.");

                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[PRESET 1 / WPF DIRECT SCAN] CANCELED");
            }
            finally
            {
                if (ReferenceEquals(_context._laPresetDirectScanCts, scanCts))
                {
                    _context._laPresetDirectScanCts = null;

                    scanCts.Dispose();
                }

                _context.IsLaPresetScanRunning = false;
                _context.LaPresetCommandStatusText = "WPF DIRECT SCAN STOPPED";

                ConsoleLogHelper.PrintLine();
            }
        }

        /// <summary>
        /// 선택한 LA 프리셋 순회 속도와 대기 시간을 허용 범위로 제한해 장비에 반영한다.
        /// </summary>
        internal void UpdateLaPresetScan()
        {
            if (!EnsurePresetAgent(false))
            {
                return;
            }

            int speed = Math.Max(1, Math.Min(60, _context.LaPresetScanSpeed));

            int delay = Math.Max(1, Math.Min(60, _context.LaPresetScanDelay));

            Console.WriteLine();

            Console.WriteLine("[PRESET 1 / WPF DIRECT SCAN] OPTION");

            Console.WriteLine($"[PRESET 1 / WPF DIRECT SCAN] " + $"SPEED={speed}deg/s / DELAY={delay}s");

            ConsoleLogHelper.PrintLine();

            _context.LaPresetCommandStatusText = $"SPEED={speed}deg/s NEXT POINT / " + $"DELAY={delay}s NEXT START";
        }

        /// <summary>
        /// LA 장비의 프리셋 순회를 중지한다.
        /// </summary>
        internal void StopLaPresetScan()
        {
            ConsoleLogHelper.Command("PRESET L", "Auto scan stop requested / CMD2=0x9B");

            CancellationTokenSource scanCts = Interlocked.Exchange(ref _context._laPresetDirectScanCts, null);

            if (scanCts != null)
            {
                scanCts.Cancel();
            }

            bool stopResult = _context._controlCommandService.StopPresetScan();

            _context.IsLaPresetScanRunning = false;

            Console.WriteLine();

            Console.WriteLine("[PRESET 1 / WPF DIRECT SCAN] STOP REQUEST");

            ConsoleLogHelper.PrintLine();

            _context.LaPresetCommandStatusText = "AUTO SCAN STOP REQUESTED";

            ConsoleLogHelper.Command("PRESET L", $"Auto scan stop sent / CMD2=0x9B / RESULT={stopResult}");
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다. AUTO SCAN 반복 상태에는 영향을 주지 않으며, Pan/Tilt 위치 이동 정지
        /// 명령(0x4F)을 각각 송신한다.
        /// </summary>
        internal void StopLaPresetMove()
        {
            _context._panoramaPresetCts?.Cancel();

            CancellationTokenSource moveCts = Interlocked.Exchange(ref _context._laPresetSingleMoveCts, null);

            if (moveCts != null)
            {
                moveCts.Cancel();
            }

            bool stopResult = _context._controlCommandService.StopPanTiltPositionMove();

            _context.LaPresetCommandStatusText = "PRESET L MOVE STOPPED";

            ConsoleLogHelper.Command("PRESET L", $"Single move stop / CMD2=0x4F / RESULT={stopResult}");
        }

        /// <summary>
        /// PRESET W 이동에 사용할 Pan / Tilt 위치 속도를 장비에 적용한다. AUTO SCAN의 Speed(1 ~ 60)와 단일 MOVE
        /// TO PRESET이 같은 값을 사용한다.
        /// </summary>
        internal bool ApplyPresetPositionSpeed(int speed)
        {
            int safeSpeed = Math.Max(1, Math.Min(60, speed));

            return _context._controlCommandService.SetPanPositionSpeed(safeSpeed) && _context._controlCommandService.SetTiltPositionSpeed(safeSpeed);
        }

        /// <summary>
        /// 선택한 WEB 프리셋 번호에 현재 위치 저장을 요청하고 결과를 목록에 반영한다.
        /// </summary>
        internal void AddOrUpdatePresetPoint()
        {
            if (!EnsurePresetAgent(true))
            {
                return;
            }

            ConsoleLogHelper.Command("PRESET W", "Set preset requested");

            int presetNumber = Math.Max(1, Math.Min(100, _context.PresetSlotNumber));

            /*
             * PRESET 2는 WEB AGENT Pelco-D 프리셋 검증 모드다.
             * LA 전용 Scan Point(0x19/0x91/0x93/0x95/0x97)는 절대 등록하지 않는다.
             *
             * 문서 2.10:
             * Command2 = 0x03 : SET PRESET
             * Data2    = Preset Number
             */
            bool result = _context._controlCommandService.AddPresetPoint((byte)presetNumber);

            Console.WriteLine();

            Console.WriteLine("[PRESET 2 / WEB AGENT] SET PRESET");

            Console.WriteLine($"[PRESET 2 / WEB AGENT] NUMBER={presetNumber} / " + $"PELCO_D=0x03 / RESULT={result}");

            ConsoleLogHelper.PrintLine();

            if (!result)
            {
                _context.PresetCommandStatusText = $"P{presetNumber:00} SET PRESET FAILED";

                return;
            }

            PresetPointOption newPreset = new PresetPointOption(presetNumber, _context._currentPan, _context._currentTilt, _context.CurrentEoZoomText, _context.CurrentEoFocusText, GetCurrentIrZoomStandardPosition().ToString(), GetCurrentIrFocusStandardPosition().ToString(), positionSnapshot: _context.CapturePositionSnapshot("PRESET_W_SAVE", presetNumber));

            UpsertPresetPoint(newPreset);

            _context.PresetCommandStatusText = $"P{presetNumber:00} SET PRESET SENT";
        }

        /// <summary>
        /// 선택 슬롯의 프리셋을 제거한다. TORUSS 명령: Command2 = 0x05 Data1 = 0x00 Data2 = Preset
        /// Number (1 ~ 100)
        /// </summary>
        internal void DeletePresetPoint()
        {
            if (!EnsurePresetAgent(true))
            {
                return;
            }

            if (_context.SelectedPresetPoint == null)
            {
                _context.PresetCommandStatusText = "DELETE FAILED : SELECT PRESET";

                return;
            }

            int presetNumber = _context.SelectedPresetPoint.Number;

            /*
             * WEB AGENT 프리셋 삭제 명령
             * Command2 = 0x05: 저장된 프리셋 삭제
             * Data2: 프리셋 번호
             */
            bool result = _context._controlCommandService.RemovePresetPoint((byte)presetNumber);

            Console.WriteLine();

            Console.WriteLine("[PRESET 2 / WEB AGENT] CLEAR PRESET");

            Console.WriteLine($"[PRESET 2 / WEB AGENT] NUMBER={presetNumber} / " + $"PELCO_D=0x05 / RESULT={result}");

            ConsoleLogHelper.PrintLine();

            if (!result)
            {
                _context.PresetCommandStatusText = $"P{presetNumber:00} CLEAR PRESET FAILED";

                return;
            }

            PresetPointOption removed = _context.SelectedPresetPoint;

            _context.PresetPoints.Remove(removed);

            _context.SelectedPresetPoint = _context.PresetPoints.FirstOrDefault();

            _context.SavePresetStorage();

            _context.PresetCommandStatusText = $"P{presetNumber:00} CLEAR PRESET SENT";
        }

        /// <summary>
        /// 2026-08-28: PRESET W 전체 삭제를 각 슬롯의 표준 CLEAR PRESET 명령으로 수행한다. 일부 송신 실패 시 성공한
        /// 항목만 화면 목록에서 제거하고 실패 개수를 알린다.
        /// </summary>
        internal void ClearAllPresetPoints()
        {
            if (!EnsurePresetAgent(true))
            {
                return;
            }

            StopPresetScan();

            PresetPointOption[] registered = _context.PresetPoints.ToArray();

            int removedCount = 0;

            int failedCount = 0;

            foreach (PresetPointOption preset in registered)
            {
                try
                {
                    if (_context._controlCommandService.RemovePresetPoint((byte)preset.Number))
                    {
                        _context.PresetPoints.Remove(preset);

                        removedCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;

                    ConsoleLogHelper.Error("PRESET W", "Clear preset exception / NUMBER=" + preset.Number, ex);
                }
            }

            _context.SelectedPresetPoint = _context.PresetPoints.FirstOrDefault();

            _context.SavePresetStorage();

            _context.PresetCommandStatusText = failedCount == 0 ? "ALL REGISTERED PRESETS CLEARED" : $"CLEAR COMPLETE : {removedCount} OK / {failedCount} FAILED";

            ConsoleLogHelper.State("PRESET W", "Clear all completed / REMOVED=" + removedCount + " / FAILED=" + failedCount);
        }

        /// <summary>
        /// ComboBox에서 선택한 프리셋으로 이동한다. 현재 UI의 PAN TURN MODE를 먼저 송신한 뒤 프리셋 이동 명령을 송신한다.
        /// TORUSS 명령: Command2 = 0x07 Data1 = 0x00 Data2 = Preset Number (1 ~ 100)
        /// </summary>
        internal async void MoveToSelectedPresetPoint()
        {
            await MoveToSelectedPresetPointAsync();
        }

        /// <summary>
        /// 기존 웹 프리셋 이동을 번호 클릭과 순회 연동에서 완료까지 기다릴 수 있도록 공유한다.
        /// </summary>
        internal async Task MoveToSelectedPresetPointAsync()
        {
            if (!EnsurePresetAgent(true))
            {
                return;
            }

            _context.RecoverPanoramaPresetReference();

            if (_context.PanoramaReference.Pending)
            {
                _context.PresetCommandStatusText = "PRESET REFERENCE CHECK REQUIRED";

                return;
            }

            if (_context.SelectedPresetPoint == null)
            {
                _context.PresetCommandStatusText = _context.PresetPoints.Count == 0 ? "NO WEB PRESET / REGISTER FIRST" : "MOVE FAILED : SELECT WEB PRESET";

                Console.WriteLine("[PRESET 2 / WEB AGENT] GOTO FAILED : " + "프리셋 미선택 상태");

                return;
            }

            PresetPointOption preset = _context.SelectedPresetPoint;

            int presetNumber = preset.Number;

            int moveSpeed = Math.Max(1, Math.Min(60, _context.PresetScanSpeed));

            CancellationTokenSource moveCts = new CancellationTokenSource();

            CancellationTokenSource oldMoveCts = Interlocked.Exchange(ref _context._presetSingleMoveCts, moveCts);

            if (oldMoveCts != null)
            {
                oldMoveCts.Cancel();
            }

            /*
             * PRESET 2 WEB AGENT Pelco-D 검증:
             * Command2 = 0x07 : GOTO PRESET
             * Data2    = Preset Number
             *
             * 0x4D Pan Turn Mode는 Pan 절대 위치 제어용이며
             * 프리셋 이동 명령에 포함되지 않으므로 전송하지 않는다.
             */
            // ZERO 이후에는 장비 슬롯의 내부 기준을 추측하지 않고 기존 직접 PTZF 복원 경로를 공유한다.
            _context._presetSkippedFocusMask = 0;

            bool referenceAdjusted = _context.RequiresDirectPresetRestore;

            // 직접 이동 경로가 속도를 설정하므로 같은 속도 패킷을 중복 전송하지 않는다.
            bool speedResult = referenceAdjusted || ApplyPresetPositionSpeed(moveSpeed);

            bool moveResult = referenceAdjusted ? speedResult : speedResult && _context._controlCommandService.MoveToPresetPoint((byte)presetNumber);

            if (moveResult)
            {
                _context._lastPanAbsoluteTarget = _context.SelectedPresetPoint.Pan;
            }

            bool panTiltSettled = false;

            bool lensRestored = false;

            try
            {
                if (referenceAdjusted)
                {
                    panTiltSettled = moveResult && await MoveLaPresetPointDirectAsync(preset, moveCts.Token, routeSpeed: moveSpeed, routeDelay: _context.PresetScanDelay);

                    lensRestored = panTiltSettled;
                }
                else
                {
                    panTiltSettled = moveResult && await EnsurePresetPanTiltTargetAsync("PRESET W", preset, () => ApplyPresetPositionSpeed(moveSpeed) && _context._controlCommandService.MoveToPresetPoint((byte)presetNumber), CalculatePresetPanTiltSettleTimeoutMs(preset, moveSpeed), moveCts.Token);

                    if (_context.IsPanoramaPresetRunning)
                    {
                        _context._panoramaPanTiltConfirmed = panTiltSettled;
                    }

                    if (panTiltSettled)
                    {
                        lensRestored = await _context.RestoreStoredPresetLensAsync(preset, moveCts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _context.PresetCommandStatusText = $"P{presetNumber:00} GOTO PRESET STOPPED";

                return;
            }
            finally
            {
                if (ReferenceEquals(Interlocked.CompareExchange(ref _context._presetSingleMoveCts, null, moveCts), moveCts))
                {
                    moveCts.Dispose();
                }
            }

            Console.WriteLine();

            Console.WriteLine("[PRESET 2 / WEB AGENT] GOTO PRESET");

            Console.WriteLine($"[PRESET 2 / WEB AGENT] NUMBER={presetNumber} / " + $"SPEED={moveSpeed} / MODE={(referenceAdjusted ? "STORED_DIRECT_PTZF" : "DEVICE_PRESET")} / RESULT={moveResult} / " + $"PT_SETTLED={panTiltSettled}");

            ConsoleLogHelper.PrintLine();

            _context.PresetCommandStatusText = moveResult && panTiltSettled && lensRestored ? _context.FormatPresetCompletion($"P{presetNumber:00} GOTO PRESET COMPLETED") : $"P{presetNumber:00} GOTO PRESET INCOMPLETE";
        }

        /// <summary>
        /// 프리셋 오토 스캔을 시작한다. TORUSS 명령: Command2 = 0x99 Data1 = Speed (1 ~ 60) Data2 =
        /// Delay (1 ~ 60)
        /// </summary>
        internal void StartPresetScan()
        {
            if (!EnsurePresetAgent(true))
            {
                return;
            }

            _context.RecoverPanoramaPresetReference();

            if (_context.PanoramaReference.Pending)
            {
                _context.PresetCommandStatusText = "PRESET REFERENCE CHECK REQUIRED";

                return;
            }

            ConsoleLogHelper.Command("PRESET W", "Loop start requested");

            if (_context.PresetPoints.Count <= 0)
            {
                _context.PresetCommandStatusText = "START FAILED : NO PRESET";

                return;
            }

            // 기존 PRESET 2 루프만 취소한다.
            // START 시 장비 Stop 명령을 먼저 보내지 않는다.
            CancelWebAgentPresetScan();

            CancellationTokenSource scanCts = new CancellationTokenSource();

            _context._presetDirectScanCts = scanCts;
            _context.IsPresetScanRunning = true;

            int delaySeconds = Math.Max(1, Math.Min(60, _context.PresetScanDelay));

            Console.WriteLine();

            Console.WriteLine("[PRESET 2 / WEB AGENT] LOOP START");

            Console.WriteLine($"[PRESET 2 / WEB AGENT] COUNT={_context.PresetPoints.Count} / " + $"DELAY={delaySeconds}s / " + "COMMAND=POSITION_SPEED_THEN_GOTO_PRESET_0x07");

            ConsoleLogHelper.PrintLine();

            _context.PresetCommandStatusText = "WEB AGENT PRESET LOOP STARTED";

            PresetPointOption[] scanQueue = _context.CreatePresetScanQueue(_context.PresetPoints);

            _ = RunWebAgentPresetScanAsync(scanCts, scanQueue);
        }

        /// <summary>
        /// 실행 중인 WEB 프리셋 순회 작업에 취소를 요청한다.
        /// </summary>
        internal bool CancelWebAgentPresetScan()
        {
            CancellationTokenSource scanCts = Interlocked.Exchange(ref _context._presetDirectScanCts, null);

            if (scanCts == null)
            {
                return false;
            }

            scanCts.Cancel();

            scanCts.Dispose();

            return true;
        }

        /// <summary>
        /// WEB 프리셋의 순서·지점별 이동·대기 설정에 따라 순회를 실행한다.
        /// </summary>
        internal async Task RunWebAgentPresetScanAsync(CancellationTokenSource scanCts, PresetPointOption[] snapshot)
        {
            CancellationToken cancellationToken = scanCts.Token;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (PresetPointOption preset in snapshot)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // 줌 복원을 기다리는 동안에도 현재 순회 대상 번호와 저장 상세를 표시한다.
                        _context.SelectedPresetPoint = preset;

                        _context.PresetCommandStatusText = $"SCAN MOVING P{preset.Number:00}";

                        int moveSpeed = Math.Max(1, Math.Min(60, _context.PresetScanSpeed));

                        _context._presetSkippedFocusMask = 0;

                        if (_context.RequiresDirectPresetRestore)
                        {
                            // AF 보호 또는 ZERO 보정이 필요한 순회도 단일 이동과 같은 직접 복원을 사용한다.
                            bool restored = await MoveLaPresetPointDirectAsync(preset, cancellationToken, scanTiming: true, routeSpeed: moveSpeed, routeDelay: _context.PresetScanDelay);

                            _context.PresetCommandStatusText = restored ? _context.FormatPresetCompletion($"P{preset.Number:00} GOTO COMPLETED") : $"P{preset.Number:00} GOTO INCOMPLETE";

                            if (!restored)
                            {
                                ConsoleLogHelper.Warning("PRESET W", "이동 미완료 / 다음 지점 이동 없이 자동 순회를 중단합니다.");

                                return;
                            }

                            continue;
                        }

                        bool speedResult = ApplyPresetPositionSpeed(moveSpeed);

                        bool result = speedResult && _context._controlCommandService.MoveToPresetPoint((byte)preset.Number);

                        Console.WriteLine();

                        Console.WriteLine("[PRESET 2 / WEB AGENT] LOOP GOTO PRESET");

                        Console.WriteLine($"[PRESET 2 / WEB AGENT] NUMBER={preset.Number} / " + $"SPEED={moveSpeed} / PELCO_D=0x07 / RESULT={result}");

                        ConsoleLogHelper.PrintLine();

                        if (!result)
                        {
                            _context.PresetCommandStatusText = $"P{preset.Number:00} GOTO FAILED";

                            return;
                        }

                        bool panTiltSettled = await EnsurePresetPanTiltTargetAsync("PRESET W", preset, () => ApplyPresetPositionSpeed(moveSpeed) && _context._controlCommandService.MoveToPresetPoint((byte)preset.Number), CalculatePresetPanTiltSettleTimeoutMs(preset, moveSpeed), cancellationToken);

                        if (!panTiltSettled)
                        {
                            _context.PresetCommandStatusText = $"P{preset.Number:00} PAN/TILT SETTLE INCOMPLETE";

                            ConsoleLogHelper.Warning("PRESET W", $"팬·틸트 미도달 / 순회 중단 / P{preset.Number:00}");

                            return;
                        }

                        bool lensRestored = panTiltSettled && await _context.RestoreStoredPresetLensAsync(preset, cancellationToken, true);

                        _context.PresetCommandStatusText = panTiltSettled && lensRestored ? _context.FormatPresetCompletion($"P{preset.Number:00} GOTO COMPLETED") : $"P{preset.Number:00} GOTO INCOMPLETE";

                        if (!lensRestored)
                        {
                            ConsoleLogHelper.Warning("PRESET W", "줌 도착 미확인 / 다음 지점 이동 없이 자동 순회를 중단합니다.");

                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 사용자가 PRESET 순회 또는 연결을 중지한 정상 종료 경로이다.
            }
            finally
            {
                if (ReferenceEquals(Interlocked.CompareExchange(ref _context._presetDirectScanCts, null, scanCts), scanCts))
                {
                    scanCts.Dispose();

                    _context.IsPresetScanRunning = false;
                }
            }
        }

        /// <summary>
        /// 실행 중인 스캔의 속도 / 정지시간을 변경한다. TORUSS 명령: Command2 = 0x9D Data1 = Speed (1 ~ 60)
        /// Data2 = Delay (1 ~ 60)
        /// </summary>
        internal void UpdatePresetScan()
        {
            if (!EnsurePresetAgent(true))
            {
                return;
            }

            int speed = Math.Max(1, Math.Min(60, _context.PresetScanSpeed));

            int delay = Math.Max(1, Math.Min(60, _context.PresetScanDelay));

            /*
             * PRESET 2는 0x99/0x9D LA Auto Scan을 사용하지 않는다.
             * APPLY 이후 다음 단일/반복 이동부터 Speed와 Delay를 사용한다.
             * Speed는 GOTO PRESET(0x07) 직전에 Pan/Tilt 위치 속도 명령으로 적용한다.
             */
            Console.WriteLine();

            Console.WriteLine("[PRESET 2 / WEB AGENT] LOOP OPTION APPLY");

            Console.WriteLine($"[PRESET 2 / WEB AGENT] DELAY={delay}s / " + $"MOVE_SPEED={speed} / " + "APPLIES_TO=NEXT_SINGLE_OR_SCAN_MOVE");

            ConsoleLogHelper.PrintLine();

            _context.PresetCommandStatusText = $"MOVE SPEED={speed} / LOOP DELAY={delay}s APPLIED";
        }

        /// <summary>
        /// 프리셋 오토 스캔을 정지한다. TORUSS 명령: Command2 = 0x9B Data1 = 0x00 Data2 = 0x00 Data2 =
        /// 0x01인 프리셋 데이터 초기화는 오조작 위험이 있으므로 UI에 제공하지 않는다.
        /// </summary>
        internal void StopPresetScan()
        {
            ConsoleLogHelper.Command("PRESET W", "Auto scan stop requested / CMD2=0x9B");

            bool hadRunningLoop = CancelWebAgentPresetScan();

            bool stopResult = _context._controlCommandService.StopPresetScan();

            /*
             * 상단 STOP AUTO SCAN은 반복 순회만 종료하고
             * 전용 Auto Scan Stop(0x9B)을 송신한다.
             * 진행 중인 단일 위치 이동은 STOP MOVE(0x4F)에서만 정지한다.
             */
            _context.IsPresetScanRunning = false;

            Console.WriteLine();

            Console.WriteLine("[PRESET 2 / WEB AGENT] LOOP STOP");

            Console.WriteLine($"[PRESET 2 / WEB AGENT] SCAN_ACTIVE={hadRunningLoop} / " + "AUTO_SCAN_STOP_0x9B_SENT=True / " + $"STOP_RESULT={stopResult} / " + "POSITION_STOP_0x4F_SENT=False");

            ConsoleLogHelper.PrintLine();

            _context.PresetCommandStatusText = "AUTO SCAN STOP REQUESTED";
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다. AUTO SCAN 반복 상태에는 영향을 주지 않으며, Pan/Tilt 위치 이동 정지
        /// 명령(0x4F)을 각각 송신한다.
        /// </summary>
        internal void StopPresetMove()
        {
            _context._panoramaPresetCts?.Cancel();

            CancellationTokenSource moveCts = Interlocked.Exchange(ref _context._presetSingleMoveCts, null);

            moveCts?.Cancel();

            bool stopResult = _context._controlCommandService.StopPanTiltPositionMove();

            _context.PresetCommandStatusText = "PRESET W MOVE STOPPED";

            ConsoleLogHelper.Command("PRESET W", $"Single move stop / CMD2=0x4F / RESULT={stopResult}");
        }

        /// <summary>
        /// 잠금 Overlay의 공통 STOP 버튼에서 현재 실행 중인 PRESET L 또는 PRESET W AUTO SCAN을 정지한다.
        /// </summary>
        internal void StopActivePresetScan()
        {
            _context.CancelPanoramaPresets();

            if (_context.IsLaPresetScanRunning)
            {
                StopLaPresetScan();
            }

            if (_context.IsPresetScanRunning)
            {
                StopPresetScan();
            }
        }

        /// <summary>
        /// 화면 확인용 프리셋 목록에 슬롯을 추가하거나 갱신한다. 슬롯 번호 오름차순으로 유지하여 ComboBox에서 P01, P02, P03 순으로
        /// 표시한다.
        /// </summary>
        internal void UpsertPresetPoint(PresetPointOption newPreset)
        {
            PresetPointOption existingPreset = _context.PresetPoints.FirstOrDefault(preset => preset.Number == newPreset.Number);

            _context.PreparePresetForUpsert(newPreset, _context.PresetPoints, existingPreset);

            if (existingPreset != null)
            {
                _context.PresetPoints.Remove(existingPreset);
            }

            _context.PresetPoints.Add(newPreset);

            _context.ReorderPresetCollections();

            _context.SelectedPresetPoint = newPreset;

            _context.SavePresetStorage();
        }

        /// <summary>
        /// Zoom Position 입력값 적용
        /// </summary>
        internal async Task SetZoomPositionFromInputAsync()
        {
            if (!_context.ZoomPositionValue.HasValue)
            {
                Console.WriteLine("[MOVE CONTROL] ZOOM POSITION FAILED : EMPTY VALUE");

                return;
            }

            int standardPosition = Clamp(_context.ZoomPositionValue.Value, MoveControlPositionMinimum, MoveControlPositionMaximum);

            await ApplyMoveControlZoomPositionAsync(standardPosition, "POSITION");
        }

        /// <summary>
        /// Zoom Ratio 입력값 적용 입력값은 EO 광학 배율 1.0 ~ 90.0배 기준이다. EO 배율을 공통 Position 0 ~
        /// 1000으로 변환한 뒤 EO와 IR에 동일한 진행률을 적용한다. EO 1.0배 ->; Position 0 ->; IR 1.0배 EO 90.0배
        /// ->; Position 1000 ->; IR 9.0배
        /// </summary>
        internal async Task SetZoomRatioFromInputAsync()
        {
            if (!_context.ZoomRatioValue.HasValue)
            {
                Console.WriteLine("[MOVE CONTROL] ZOOM RATIO FAILED : EMPTY VALUE");

                return;
            }

            double zoomRatio = Clamp(_context.ZoomRatioValue.Value, _context.MoveControlMinimumZoomRatio, _context.MoveControlEoMaximumZoomRatio);

            int standardPosition = ConvertEoZoomRatioToStandardPosition(zoomRatio);

            await ApplyMoveControlZoomPositionAsync(standardPosition, $"RATIO {zoomRatio:F1}x");
        }

        /// <summary>
        /// 이동 제어 Zoom Position을 현재 장비 구성에 적용한다. 환경장비: EO / IR Control Agent Position 명령
        /// 옥상장비: EO CTEC Direct Position + IR Control Agent Position 명령
        /// </summary>
        internal async Task ApplyMoveControlZoomPositionAsync(int standardPosition, string requestType)
        {
            int safePosition = Clamp(standardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);
            await _context.StopZoomSyncAsync();
            Console.WriteLine();
            Console.WriteLine($"[MOVE CONTROL] ZOOM {requestType} " + $"/ STANDARD POSITION={safePosition} " + $"/ EO RATIO={ConvertStandardPositionToEoZoomRatio(safePosition):F1}x " + $"/ IR RATIO={ConvertStandardPositionToIrZoomRatio(safePosition):F1}x");
            ConsoleLogHelper.PrintLine();
            if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
            {
                await ApplyEnvironmentProportionalZoomAsync(safePosition, requestType);
                return;
            }

            CancellationTokenSource zoomCts = new CancellationTokenSource();
            _context._rooftopZoomSyncCts = zoomCts;
            bool eoResult = false;
            bool irResult = false;
            try
            {
                // LA 직접 입력도 프리셋과 같은 0x29를 사용한다. WEB 0xB7 경로로 보내지 않는다.
                Task<bool> irMoveTask = MoveLaIrZoomFromInputAsync(safePosition, requestType, zoomCts.Token);
                RtspSourceOption ctecSource = _context._connectedEoCtecSource;
                Task<bool> eoMoveTask = ctecSource == null ? Task.FromResult(_context._controlCommandService.EoZoomGoPosition((short)safePosition)) : _context.MoveRooftopEoZoomToRawPositionAsync(ctecSource, ConvertStandardZoomToCtecRaw(safePosition), zoomCts.Token);
                bool[] results = await Task.WhenAll(eoMoveTask, irMoveTask);
                eoResult = results[0];
                irResult = results[1];
            }
            finally
            {
                if (ReferenceEquals(_context._rooftopZoomSyncCts, zoomCts))
                {
                    _context._rooftopZoomSyncCts = null;
                    zoomCts.Dispose();
                }
            }

            Console.WriteLine("[MOVE CONTROL] ROOFTOP ZOOM RESULT " + $"/ EO={eoResult} / IR={irResult}");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 2026-10-16: 환경부 비례 Zoom은 EO 0x37과 IR 0xB7 절대위치를 우선 송신한다. IR은 실제 렌즈 피드백으로 확인하며,
        /// 무이동이 확인된 때만 0x31로 대체한다.
        /// </summary>
        internal async Task ApplyEnvironmentProportionalZoomAsync(int standardPosition, string requestType)
        {
            int eoTarget = Clamp(standardPosition, 0, 1000);

            int irTarget = _context.ConvertIrZoomStandardToStatusPosition(eoTarget);

            long eoStartSequence = Interlocked.Read(ref _context._eoLensStatusVersion);

            long commandStartedTicks = Stopwatch.GetTimestamp();

            CancellationTokenSource zoomCts = new CancellationTokenSource();

            _context._rooftopZoomSyncCts = zoomCts;

            bool eoSendResult = false;

            bool eoResult = false;

            bool irResult = false;

            long commandIntervalMs = 0;

            ConsoleLogHelper.Command("EO/IR PROPORTIONAL ZOOM", $"COMMAND=EO_IR_PROPORTIONAL_ZOOM / REQUEST={requestType} / " + $"EO_CURRENT={_context._currentEoZoom} / EO_TARGET={eoTarget} / " + $"IR_CURRENT={_context._currentIrZoom} / IR_TARGET={irTarget}");

            try
            {
                Task<bool> irMoveTask = _context.MoveIrZoomToPositionAsync(irTarget, zoomCts.Token, eoTarget, 1, explicitAbsolute: true);

                eoSendResult = _context._webAgentZoomControlService.SetEoZoomPosition((short)eoTarget);

                commandIntervalMs = (long)((Stopwatch.GetTimestamp() - commandStartedTicks) * 1000.0 / Stopwatch.Frequency);

                Task<bool> eoMoveTask = eoSendResult ? _context.WaitForEnvironmentEoLensTargetAsync(eoTarget, true, eoStartSequence, zoomCts.Token) : Task.FromResult(false);

                bool[] results = await Task.WhenAll(eoMoveTask, irMoveTask);

                eoResult = results[0];

                irResult = results[1];
            }
            catch (OperationCanceledException)
            {
                ConsoleLogHelper.State("EO/IR PROPORTIONAL ZOOM", $"COMMAND=EO_IR_PROPORTIONAL_ZOOM / FINAL_RESULT=CANCELED / " + $"EO_FINAL={_context._currentEoZoom} / IR_FINAL={_context._currentIrZoom}");

                return;
            }
            finally
            {
                if (ReferenceEquals(_context._rooftopZoomSyncCts, zoomCts))
                {
                    _context._rooftopZoomSyncCts = null;

                    zoomCts.Dispose();
                }
            }

            ConsoleLogHelper.State("EO/IR PROPORTIONAL ZOOM", $"COMMAND=EO_IR_PROPORTIONAL_ZOOM / EO_TARGET={eoTarget} / " + $"IR_TARGET={irTarget} / EO_SEND_RESULT={eoSendResult} / " + $"IR_SEND_RESULT={irResult} / COMMAND_INTERVAL_MS={commandIntervalMs} / " + $"EO_FINAL={_context._currentEoZoom} / IR_FINAL={_context._currentIrZoom} / " + $"EO_RESULT={eoResult} / IR_RESULT={irResult} / RETRY_COUNT=SEE_IR_ZOOM_SYNC / " + $"FINAL_RESULT={(eoResult && irResult ? "COMPLETED" : "INCOMPLETE")}");
        }

        /// <summary>
        /// Focus Position 입력값 적용 표준 방향: 0 = Far 1000 = Near IR Raw 방향: 1000 = Far 0 =
        /// Near
        /// </summary>
        internal async Task SetFocusPositionFromInputAsync()
        {
            if (_context.IsAutoFocusBlockingManualFocus(false) || _context.IsAutoFocusBlockingManualFocus(true))
            {
                return;
            }

            if (!_context.FocusPositionValue.HasValue)
            {
                Console.WriteLine("[MOVE CONTROL] FOCUS POSITION FAILED : EMPTY VALUE");

                return;
            }

            int standardPosition = Clamp(_context.FocusPositionValue.Value, MoveControlPositionMinimum, MoveControlPositionMaximum);

            await ApplyMoveControlFocusPositionAsync(standardPosition);
        }

        /// <summary>
        /// 이동 제어 Focus Position을 EO / IR에 동시에 적용한다. IR은 기존 0x2B 절대 위치 명령과 Function 0x07
        /// 수신값으로 도착을 확인한다. LA의 반전은 표준 목표를 수신 목표로 바꿀 때 한 번 적용한다. 무이동 시 연속 명령 전환은 정지·정착이
        /// 확인된 경우에만 허용한다.
        /// </summary>
        internal async Task ApplyMoveControlFocusPositionAsync(int standardPosition)
        {
            int safePosition = Clamp(standardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            await _context.StopFocusSyncAsync();

            CancellationTokenSource focusCts = new CancellationTokenSource();

            _context._rooftopFocusSyncCts = focusCts;

            bool eoResult = false;

            bool irResult = false;

            try
            {
                int irRawTargetPosition = ConvertIrFocusStandardToStatusPosition(safePosition);

                if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
                {
                    eoResult = _context._controlCommandService.EoFocusGoPosition((short)safePosition);

                    irResult = await _context.MoveIrFocusToPositionAsync(irRawTargetPosition, focusCts.Token, 1, explicitAbsolute: true);
                }
                else
                {
                    Task<bool> irMoveTask = _context.MoveIrFocusToPositionAsync(irRawTargetPosition, focusCts.Token);

                    RtspSourceOption ctecSource = _context._connectedEoCtecSource;

                    if (ctecSource == null)
                    {
                        eoResult = _context._controlCommandService.EoFocusGoPosition((short)safePosition);

                        irResult = await irMoveTask;
                    }
                    else
                    {
                        int eoRawTarget = ConvertStandardFocusToCtecRaw(safePosition);

                        Task<bool> eoMoveTask = _context.MoveRooftopEoFocusToRawPositionAsync(ctecSource, eoRawTarget, focusCts.Token);

                        bool[] moveResults = await Task.WhenAll(eoMoveTask, irMoveTask);

                        eoResult = moveResults[0];

                        irResult = moveResults[1];
                    }
                }
            }
            finally
            {
                if (ReferenceEquals(_context._rooftopFocusSyncCts, focusCts))
                {
                    _context._rooftopFocusSyncCts = null;

                    focusCts.Dispose();
                }
            }

            Console.WriteLine();

            Console.WriteLine("[MOVE CONTROL] FOCUS POSITION RESULT " + $"/ STANDARD={safePosition} " + $"/ EO={eoResult} / IR={irResult}");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 기존 VIA 0 Pan 이동 작업 취소
        /// </summary>
        internal void CancelMoveControlPanOperation()
        {
            CancellationTokenSource cts = Interlocked.Exchange(ref _context._moveControlPanCts, null);

            if (cts == null)
            {
                return;
            }

            cts.Cancel();
        }

        /// <summary>
        /// EO 광학 배율 1.0 ~ 90.0을 공통 표준 Position 0 ~ 1000으로 변환한다. 이 Position을 EO와 IR에 동일하게
        /// 적용하여 두 렌즈의 기계적 Zoom 진행률을 맞춘다.
        /// </summary>
        internal int ConvertEoZoomRatioToStandardPosition(double eoZoomRatio)
        {
            double safeRatio = Clamp(eoZoomRatio, _context.MoveControlMinimumZoomRatio, _context.MoveControlEoMaximumZoomRatio);

            return _context.ControlTuningFor(false).Position(safeRatio);
        }

        /// <summary>
        /// 표준 Position 0 ~ 1000을 EO 광학 배율 1.0 ~ 90.0으로 변환한다.
        /// </summary>
        internal double ConvertStandardPositionToEoZoomRatio(int standardPosition)
        {
            int safePosition = Clamp(standardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            return _context.ControlTuningFor(false).Ratio(safePosition);
        }

        /// <summary>
        /// 표준 Position 0 ~ 1000을 IR 광학 배율 1.0 ~ 9.0으로 변환한다. 실제 장비 명령은 기존과 동일하게 Position
        /// 기준으로 보내며, 이 값은 UI 및 로그에 표시하는 예상 광학 배율이다.
        /// </summary>
        internal double ConvertStandardPositionToIrZoomRatio(int standardPosition)
        {
            int safePosition = Clamp(standardPosition, MoveControlPositionMinimum, MoveControlPositionMaximum);

            return _context.ControlTuningFor(true).Ratio(safePosition);
        }

        /// <summary>
        /// 각도 입력값을 프로토콜 소수점 둘째 자리 기준으로 반올림한다.
        /// </summary>
        internal static double RoundAngleToProtocolScale(double angle)
        {
            return Math.Round(angle, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Nullable 각도 입력값 반올림
        /// </summary>
        internal static double? RoundNullableAngle(double? angle)
        {
            return angle.HasValue ? RoundAngleToProtocolScale(angle.Value) : (double?)null;
        }

        /// <summary>
        /// double 범위 제한
        /// </summary>
        internal static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// int 범위 제한
        /// </summary>
        internal static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        // LA 직접 입력도 프리셋 담당 기능의 동일한 절대 줌·도달 확인 경로를 재사용한다.
        internal async Task<bool> MoveLaIrZoomFromInputAsync(int standardPosition, string requestType, CancellationToken token)
        {
            if (!await _context.EnsurePresetIrLensFeedbackAsync(true, token))
                return false;
            int rawTarget = _context.ConvertIrZoomStandardToStatusPosition(standardPosition);
            return await _context.MoveLaIrZoomAbsoluteAsync(rawTarget, standardPosition, 8, token, requestType);
        }

        #endregion

    }
}
