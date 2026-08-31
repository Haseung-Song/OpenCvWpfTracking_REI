using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Control;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// Home/Zero, Absolute 이동, Lens Position과 PRESET L/W 제어를 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Move Control Methods]

        /// <summary>
        /// 이동 제어 입력값을 VertiportNexus 기본값으로 초기화한다.
        ///
        /// Pan Absolute  : 0 (-180 ~ 180)
        /// Tilt Absolute : 0 (-90 ~ 90)
        /// Zoom Position : 0
        /// Zoom Ratio    : 1.0
        /// Focus Position: 0
        /// </summary>
        private void ResetMoveControlInput()
        {
            PanAbsoluteValue =
                0.0;

            TiltAbsoluteValue =
                0.0;

            ZoomPositionValue =
                0;

            ZoomRatioValue =
                1.0;

            FocusPositionValue =
                0;

            Console.WriteLine();
            Console.WriteLine(
                "[MOVE CONTROL] INPUT RESET");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// Pan Absolute 이동 요청
        ///
        /// 입력 범위:
        /// -180 ~ 180
        ///
        /// 기존처럼 현재각과 목표각을 이용해 앱에서
        /// 최단거리 또는 원점 통과 경로를 직접 계산하지 않는다.
        ///
        /// UI RadioButton에서 선택한 PAN TURN MODE를
        /// Pelco-D 확장 명령 0x4D로 먼저 설정한 뒤,
        /// 목표 Pan 각도를 위치 이동 명령 0x45로 전송한다.
        ///
        /// SHORT:
        /// 0x4D / Data1 0x02
        ///
        /// VIA 0:
        /// 0x4D / Data1 0x01
        /// </summary>
        /// <summary>
        /// 현재 설정된 Pan / Tilt 좌표계의 0°, 0° 위치로 절대 이동한다.
        ///
        /// 기존 LA 0xB1 명령은 기계식 Homing(StartHoming) 명령이므로
        /// 카메라가 홈 센서 방향으로 이동한 뒤 해당 위치를 원점으로 잡는다.
        /// 
        /// UI의 HOME POSITION은 사용자가 설정한 좌표 원점으로 복귀하는 기능이므로
        /// Pan 0°(0x45), Tilt 0°(0x47) 절대 이동 명령을 사용한다.
        /// </summary>
        private async Task MoveHomePositionAsync()
        {
            ConsoleLogHelper.Command(
                "HOME / ZERO",
                "HOME POSITION requested");

            // HOME / ZERO는 LA AGENT 전용 기능이다.
            // WEB AGENT 상태에서는 UI를 숨기지만,
            // Command가 코드에서 직접 호출되는 경우도 방어한다.
            if (!IsRooftopStatusSelected)
            {
                HomeZeroStatusText =
                    "LA AGENT ONLY";

                ConsoleLogHelper.Warning(
                    "HOME / ZERO",
                    "HOME POSITION skipped / WEB AGENT mode");

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] HOME POSITION SKIPPED / WEB AGENT MODE");
                ConsoleLogHelper.PrintLine();

                return;
            }

            if (!await _homeZeroOperationLock.WaitAsync(0))
            {
                ConsoleLogHelper.Warning(
                    "HOME / ZERO",
                    "HOME POSITION ignored / HOME or ZERO operation already running");

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] HOME POSITION IGNORED / OPERATION LOCKED");
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
                ResetAllKeyboardControlState();

                if (_currentMoveType !=
                    ContinuousMoveType.None)
                {
                    StopContinuousMove();

                    await Task.Delay(
                        150);
                }

                HomeZeroLockTitle =
                    "HOME POSITION";

                HomeZeroLockMessage =
                    "MOVING TO HOME POSITION...";

                SetHomePositionMovingState(
                    true);

                HomeZeroStatusText =
                    "HOME POSITION MOVING...";

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] HOME POSITION START / UI+KEYBOARD LOCKED");
                ConsoleLogHelper.PrintLine();

                bool stopResult =
                    _controlCommandService
                        .StopMove();

                await Task.Delay(
                    150);

                bool modeResult =
                    ApplySelectedPanTurnMode();

                bool panResult =
                    modeResult &&
                    _controlCommandService
                        .PanGoPosition(
                            0.0);

                await Task.Delay(
                    100);

                bool tiltResult =
                    _controlCommandService
                        .TiltGoPosition(
                            0.0);

                bool sendResult =
                    panResult &&
                    tiltResult;

                if (panResult)
                {
                    _lastPanAbsoluteTarget =
                        0.0;
                }

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] HOME POSITION ABSOLUTE " +
                    $"/ STOP={stopResult} " +
                    $"/ PAN_MODE={modeResult} " +
                    $"/ PAN=0.00:{panResult} " +
                    $"/ TILT=0.00:{tiltResult} " +
                    $"/ RESULT={sendResult}");
                ConsoleLogHelper.PrintLine();

                if (!sendResult)
                {
                    HomeZeroStatusText =
                        "HOME POSITION SEND FAILED";

                    ConsoleLogHelper.Error(
                        "HOME / ZERO",
                        $"HOME POSITION command send failed / STOP={stopResult} / PAN_MODE={modeResult} / PAN={panResult} / TILT={tiltResult}");

                    return;
                }

                bool isCompleted =
                    await WaitHomePositionCompletedAsync();

                HomeZeroStatusText =
                    isCompleted
                        ? "HOME POSITION COMPLETE"
                        : "HOME POSITION WAIT TIMEOUT";

                ConsoleLogHelper.StateSection(
                    "HOME / ZERO",
                    isCompleted
                        ? "HOME POSITION complete"
                        : "HOME POSITION timeout",
                    string.Empty,
                    $"PAN  : {_currentPan:F2}",
                    $"TILT : {_currentTilt:F2}");
            }
            catch (Exception ex)
            {
                HomeZeroStatusText =
                    "HOME POSITION FAILED";

                ConsoleLogHelper.Error(
                    "HOME / ZERO",
                    "HOME POSITION failed",
                    ex);

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] HOME POSITION ERROR : " +
                    ex.Message);
                ConsoleLogHelper.PrintLine();
            }
            finally
            {
                // 정상 완료, 송신 실패, 예외, 30초 Timeout 등
                // 어떤 종료 경로에서도 눌림 상태를 모두 제거한 뒤 Lock을 해제한다.
                //
                // HOME 이동 중에는 Reset 함수가 별도 Stop 패킷을 송신하지 않으므로
                // HOME 완료 직전에 불필요한 STOP이 끼어들지 않는다.
                ResetAllKeyboardControlState();

                SetHomePositionMovingState(
                    false);

                ConsoleLogHelper.StateSection(
                    "HOME / ZERO",
                    "UI and keyboard unlocked");

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] UI+KEYBOARD UNLOCKED " +
                    "/ ARROW+WASD+ZOOM+FOCUS INPUT ENABLED");
                ConsoleLogHelper.PrintLine();

                _homeZeroOperationLock.Release();
            }

        }

        /// <summary>
        /// HOME POSITION 완료 여부를 Pan/Tilt 상태값으로 판단한다.
        ///
        /// 단순 고정 Delay로 완료 처리하지 않고,
        /// Pan/Tilt가 0도 허용 오차 안에 들어온 상태에서
        /// 연속 5회 안정적으로 유지될 때 완료로 판정한다.
        /// 최대 대기시간을 초과하면 Timeout으로 종료한다.
        /// </summary>
        private async Task<bool> WaitHomePositionCompletedAsync()
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            var stabilityTracker =
                new HomePositionStabilityTracker(
                    _currentPan,
                    _currentTilt,
                    HomePositionTargetTolerance,
                    HomePositionStableTolerance,
                    HomePositionStableSampleCount);

            // 명령 직후 이전 상태 Packet을 완료로 오판하지 않도록
            // 최소 이동 시작 시간을 확보한다.
            await Task.Delay(
                300);

            while (stopwatch.ElapsedMilliseconds <
                   HomePositionTimeoutMs)
            {
                double currentPan =
                    _currentPan;

                double currentTilt =
                    _currentTilt;

                bool isCompleted =
                    stabilityTracker.Update(
                        currentPan,
                        currentTilt);

                Console.WriteLine(
                    "[HOME / ZERO] WAIT " +
                    $"/ PAN={currentPan:F2} " +
                    $"/ TILT={currentTilt:F2} " +
                    $"/ NEAR={stabilityTracker.IsNearTarget} " +
                    $"/ STABLE={stabilityTracker.StableCount}/{HomePositionStableSampleCount} " +
                    $"/ ELAPSED={stopwatch.ElapsedMilliseconds}ms");

                if (isCompleted)
                {
                    return true;
                }

                await Task.Delay(
                    HomePositionPollingIntervalMs);
            }

            ConsoleLogHelper.Warning(
                "HOME / ZERO",
                $"HOME POSITION timeout / PAN={_currentPan:F2} / TILT={_currentTilt:F2}");

            return false;
        }

        /// <summary>
        /// HOME POSITION 이동 상태를 반영하고 관련 UI Binding을 갱신한다.
        /// </summary>
        private void SetHomePositionMovingState(
            bool isMoving)
        {
            IsHomePositionMoving =
                isMoving;

            // HOME Lock 진입/해제 양쪽에서 남아 있는 방향키/WASD 상태를 제거한다.
            //
            // Zoom/Focus를 포함한 모든 키 입력은 MainWindow의
            // PreviewKeyDown/PreviewKeyUp 최상단에서 IsHomePositionMoving을
            // 확인하여 HOME 진행 중 전체 차단한다.
            ClearKeyboardPanTiltPressedState();

            _currentKeyboardPanTiltDirection =
                KeyboardPanTiltDirection.None;
        }

        /// <summary>
        /// 현재 Pan Encoder 위치를 즉시 0으로 설정한다.
        /// MCB Set0 명령: MO=0;PX=0;MO=1;
        /// </summary>
        private async Task SetPanZeroAsync()
        {
            ConsoleLogHelper.Command(
                "HOME / ZERO",
                "PAN ZERO requested");

            if (!await _homeZeroOperationLock.WaitAsync(0))
            {
                HomeZeroStatusText =
                    "HOME / ZERO OPERATION BUSY";

                ConsoleLogHelper.Warning(
                    "HOME / ZERO",
                    "PAN ZERO ignored / HOME or ZERO operation already running");

                return;
            }

            HomeZeroLockTitle =
                "PAN ZERO";

            HomeZeroLockMessage =
                "PAN MOTOR STABILIZING...";

            SetHomePositionMovingState(
                true);

            try
            {
                HomeZeroStatusText =
                    "PAN ZERO / MOTOR STABILIZING...";

                string mcbIpAddress =
                    GetMcbMaintenanceIpAddress();

                HomeZeroLockMessage =
                    "PAN MOTOR STABILIZING...";

                bool result =
                    await _mcbMaintenanceCommandService
                        .SetPanZeroAsync(
                            mcbIpAddress,
                            McbMaintenancePort);

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] PAN SET ORIGIN " +
                    $"/ MCB={mcbIpAddress}:{McbMaintenancePort} " +
                    $"/ SEQUENCE=],),|2,( " +
                    $"/ MOTOR_STABILIZATION=1000ms " +
                    $"/ RESULT={result}");

                ConsoleLogHelper.PrintLine();

                HomeZeroStatusText =
                    result
                        ? "PAN ZERO COMPLETE / MOTOR READY"
                        : "PAN ZERO SEND FAILED";
            }
            catch (Exception ex)
            {
                HomeZeroStatusText =
                    "PAN ZERO FAILED";

                ConsoleLogHelper.Error(
                    "HOME / ZERO",
                    "PAN ZERO failed",
                    ex);
            }
            finally
            {
                ResetAllKeyboardControlState();

                SetHomePositionMovingState(
                    false);

                _homeZeroOperationLock.Release();
            }

        }

        /// <summary>
        /// 현재 Tilt Encoder 위치를 즉시 0으로 설정한다.
        /// MCB Set0 명령: MO=0;PX=0;MO=1;
        /// </summary>
        private async Task SetTiltZeroAsync()
        {
            ConsoleLogHelper.Command(
                "HOME / ZERO",
                "TILT ZERO requested");

            if (!await _homeZeroOperationLock.WaitAsync(0))
            {
                HomeZeroStatusText =
                    "HOME / ZERO OPERATION BUSY";

                ConsoleLogHelper.Warning(
                    "HOME / ZERO",
                    "TILT ZERO ignored / HOME or ZERO operation already running");

                return;
            }

            // TILT ZERO의 Stop / Motor Off / Position Zero / Motor On과
            // 마지막 모터 안정화 대기가 모두 끝날 때까지 기존 HOME Lock을 유지한다.
            // 이 구간에는 HOME, 방향키, WASD, Zoom/Focus 입력이 차단된다.
            HomeZeroLockTitle =
                "TILT ZERO";

            HomeZeroLockMessage =
                "TILT MOTOR STABILIZING...";

            SetHomePositionMovingState(
                true);

            try
            {
                HomeZeroStatusText =
                    "TILT ZERO / MOTOR STABILIZING...";

                string mcbIpAddress =
                    GetMcbMaintenanceIpAddress();

                bool result =
                    await _mcbMaintenanceCommandService
                        .SetTiltZeroAsync(
                            mcbIpAddress,
                            McbMaintenancePort);

                Console.WriteLine();
                Console.WriteLine(
                    "[HOME / ZERO] TILT SET ORIGIN " +
                    $"/ MCB={mcbIpAddress}:{McbMaintenancePort} " +
                    $"/ SEQUENCE=],),|2,( " +
                    $"/ MOTOR_STABILIZATION=1000ms " +
                    $"/ RESULT={result}");

                ConsoleLogHelper.PrintLine();

                HomeZeroStatusText =
                    result
                        ? "TILT ZERO COMPLETE / MOTOR READY"
                        : "TILT ZERO SEND FAILED";
            }
            catch (Exception ex)
            {
                HomeZeroStatusText =
                    "TILT ZERO FAILED";

                ConsoleLogHelper.Error(
                    "HOME / ZERO",
                    "TILT ZERO failed",
                    ex);
            }
            finally
            {
                ResetAllKeyboardControlState();

                SetHomePositionMovingState(
                    false);

                _homeZeroOperationLock.Release();
            }

        }

        /// <summary>
        /// 옥상 MCB 유지보수 직접 연결 IP를 반환한다.
        ///
        /// Control Agent는 Local LA(기본 127.0.0.1:5001),
        /// 2026-08-26: MCB는 실장비(기본 192.168.0.112:4001),
        /// SCB는 4002를 사용하므로
        /// 두 주소를 서로 공유하지 않는다.
        /// </summary>
        private string GetMcbMaintenanceIpAddress()
        {
            string ipAddress =
                McbMaintenanceIpAddress?
                    .Trim();

            return string.IsNullOrWhiteSpace(
                    ipAddress)
                ? "192.168.0.112"
                : ipAddress;
        }

        /// <summary>
        /// 현재 PAN / TILT SPEED UI 값으로 위치 이동 속도를 설정한다.
        ///
        /// Pan Position Speed : Command2 0x49
        /// Tilt Position Speed: Command2 0x4B
        ///
        /// UI 1~50 값을 위치 이동 속도 1~50 deg/s로 적용하며,
        /// UI 0은 위치 이동을 시작하지 않고 STOP으로 처리한다.
        /// </summary>
        private bool ApplyPanTiltPositionSpeedFromUi(
            out double positionSpeed)
        {
            positionSpeed =
                PanTiltSpeedLevel;

            if (positionSpeed <= 0.0)
            {
                bool stopResult =
                    _controlCommandService
                        .StopMove();

                ClearActivePanTiltAbsoluteMove();

                _currentMoveType =
                    ContinuousMoveType.None;

                _activePanTiltMoveDirection =
                    KeyboardPanTiltDirection.None;

                ConsoleLogHelper.Warning(
                    "PAN / TILT SPEED",
                    "Position move blocked / UI_SPEED=0 / " +
                    $"STOP_RESULT={stopResult}");

                return false;
            }

            bool panSpeedResult =
                _controlCommandService
                    .SetPanPositionSpeed(
                        positionSpeed);

            bool tiltSpeedResult =
                _controlCommandService
                    .SetTiltPositionSpeed(
                        positionSpeed);

            return panSpeedResult &&
                   tiltSpeedResult;
        }

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다.
        /// </summary>
        private void ClearActivePanTiltAbsoluteMove()
        {
            _activePanAbsoluteTarget =
                null;

            _activeTiltAbsoluteTarget =
                null;
        }

        /// <summary>
        /// [PAN] / [TILT] Absolute 위치 이동 정지
        ///
        /// 방향 버튼 중앙의 STOP은 연속 속도 이동(0x00)을 정지한다.
        /// Absolute 이동은 위치 이동 전용 정지 명령인 0x4F를 사용해야 하므로,
        /// Pan 정지(Data1=0x01)와 Tilt 정지(Data1=0x02)를 각각 송신한다.
        /// </summary>
        private void StopAbsoluteMove()
        {
            bool stopResult =
                _controlCommandService
                    .StopPanTiltPositionMove();

            ClearActivePanTiltAbsoluteMove();

            ConsoleLogHelper.Command(
                "ABSOLUTE MOVE",
                $"Position stop requested / CMD2=0x4F / RESULT={stopResult}");
        }

        /// <summary>
        /// 위치 이동 중 속도 변경 시 현재 Absolute 목표를 그대로 재송신한다.
        ///
        /// 일부 장비는 0x49 / 0x4B 속도 설정을 다음 위치 이동 명령부터
        /// 적용하므로, 진행 중인 0x45 / 0x47 목표를 다시 보내야 변경된
        /// 속도가 현재 이동에도 즉시 반영된다.
        /// </summary>
        private void ReapplyActivePanTiltAbsoluteTarget(
            double positionSpeed)
        {
            double? panTarget =
                _activePanAbsoluteTarget;

            double? tiltTarget =
                _activeTiltAbsoluteTarget;

            bool hasPanTarget =
                panTarget.HasValue;

            bool hasTiltTarget =
                tiltTarget.HasValue;

            if (!hasPanTarget &&
                !hasTiltTarget)
            {
                return;
            }

            bool panResult =
                !hasPanTarget ||
                _controlCommandService
                    .PanGoPosition(
                        panTarget.Value);

            bool tiltResult =
                !hasTiltTarget ||
                _controlCommandService
                    .TiltGoPosition(
                        tiltTarget.Value);

            ConsoleLogHelper.Command(
                "PAN / TILT SPEED",
                "Absolute target reapplied / " +
                $"UI_SPEED={PanTiltSpeedLevel} / " +
                $"POSITION_SPEED={positionSpeed:F2}deg/s / " +
                $"PAN_TARGET={(panTarget.HasValue ? panTarget.Value.ToString("F2") : "-")} / " +
                $"TILT_TARGET={(tiltTarget.HasValue ? tiltTarget.Value.ToString("F2") : "-")} / " +
                $"RESULT={panResult && tiltResult}");
        }

        /// <summary>
        /// MovePanAbsoluteFromInputAsync 이동 함수.
        /// </summary>
        private Task MovePanAbsoluteFromInputAsync()
        {
            if (!PanAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[MOVE CONTROL] PAN ABSOLUTE FAILED : EMPTY VALUE");

                return Task.CompletedTask;
            }

            double targetPan =
                Clamp(
                    RoundAngleToProtocolScale(
                        PanAbsoluteValue.Value),
                    MoveControlPanMinimum,
                    MoveControlPanMaximum);

            CancelMoveControlPanOperation();

            bool speedResult =
                ApplyPanTiltPositionSpeedFromUi(
                    out double positionSpeed);

            if (!speedResult)
            {
                return Task.CompletedTask;
            }

            bool modeResult =
                ApplySelectedPanTurnMode();

            if (!modeResult)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "[MOVE CONTROL] PAN ABSOLUTE FAILED : " +
                    "PAN TURN MODE COMMAND FAILED");

                ConsoleLogHelper.PrintLine();

                return Task.CompletedTask;
            }

            bool moveResult =
                _controlCommandService
                    .PanGoPosition(
                        targetPan);

            if (moveResult)
            {
                _lastPanAbsoluteTarget =
                    targetPan;

                _activePanAbsoluteTarget =
                    targetPan;
            }

            Console.WriteLine();
            Console.WriteLine(
                "[MOVE CONTROL] PAN ABSOLUTE / " +
                $"MODE={_panTurnMode}");

            Console.WriteLine(
                $"[MOVE CONTROL] CURRENT={_currentPan:F2} / " +
                $"TARGET={targetPan:F2} / " +
                $"UI SPEED={PanTiltSpeedLevel} / " +
                $"POSITION SPEED={positionSpeed:F2}deg/s / " +
                $"SPEED COMMAND={speedResult} / " +
                $"MODE COMMAND={modeResult} / " +
                $"MOVE COMMAND={moveResult}");

            ConsoleLogHelper.PrintLine();

            return Task.CompletedTask;
        }

        /// <summary>
        /// 현재 RadioButton에서 선택한 Pan 회전 모드를
        /// Pelco-D 확장 명령 0x4D로 장비에 적용한다.
        /// </summary>
        private bool ApplySelectedPanTurnMode()
        {
            bool result;

            if (_panTurnMode ==
                PanTurnMode.ViaZero)
            {
                result =
                    _controlCommandService
                        .SetPanViaZeroMode();

                Console.WriteLine(
                    "[MOVE CONTROL] PAN TURN MODE / " +
                    "VIA ZERO / CMD=0x4D / DATA1=0x01 / " +
                    $"RESULT={result}");
            }
            else
            {
                result =
                    _controlCommandService
                        .SetPanShortestPathMode();

                Console.WriteLine(
                    "[MOVE CONTROL] PAN TURN MODE / " +
                    "SHORTEST PATH / CMD=0x4D / DATA1=0x02 / " +
                    $"RESULT={result}");
            }

            return result;
        }

        /// <summary>
        /// Tilt Absolute 이동 요청
        /// </summary>
        private void MoveTiltAbsoluteFromInput()
        {
            if (!TiltAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[MOVE CONTROL] TILT ABSOLUTE FAILED : EMPTY VALUE");

                return;
            }

            double targetTilt =
                Clamp(
                    RoundAngleToProtocolScale(
                        TiltAbsoluteValue.Value),
                    MoveControlTiltMinimum,
                    MoveControlTiltMaximum);

            bool speedResult =
                ApplyPanTiltPositionSpeedFromUi(
                    out double positionSpeed);

            if (!speedResult)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(
                $"[MOVE CONTROL] TILT ABSOLUTE / TARGET={targetTilt:F2}");

            ConsoleLogHelper.PrintLine();

            bool moveResult =
                _controlCommandService
                    .TiltGoPosition(
                        targetTilt);

            if (moveResult)
            {
                _activeTiltAbsoluteTarget =
                    targetTilt;
            }

            Console.WriteLine(
                $"[MOVE CONTROL] UI SPEED={PanTiltSpeedLevel} / " +
                $"POSITION SPEED={positionSpeed:F2}deg/s / " +
                $"SPEED COMMAND={speedResult} / " +
                $"MOVE COMMAND={moveResult}");
        }


        /// <summary>
        /// PRESET 1 (LA TEST) 등록.
        ///
        /// LA 실제 구현 순서:
        /// 0x19 ID -> 0x91 Pan -> 0x93 Tilt -> 0x95 Zoom
        /// -> 0x97 Focus + AddPreset
        /// </summary>
        private void AddOrUpdateLaPresetPoint()
        {
            ConsoleLogHelper.Command(
                "PRESET L",
                "Register/update requested");

            int presetNumber =
                Math.Max(
                    1,
                    Math.Min(
                        99,
                        LaPresetSlotNumber));

            ushort zoom =
                GetCurrentPresetStandardZoom();

            ushort focus =
                GetCurrentPresetStandardFocus();

            int laInternalId =
                presetNumber -
                1;

            bool idResult =
                _controlCommandService
                    .SetLaPresetId(
                        (ushort)laInternalId);

            bool panResult =
                idResult &&
                _controlCommandService
                    .SetLaPresetPan(
                        _currentPan);

            bool tiltResult =
                panResult &&
                _controlCommandService
                    .SetLaPresetTilt(
                        _currentTilt);

            bool zoomResult =
                tiltResult &&
                _controlCommandService
                    .SetLaPresetZoom(
                        zoom);

            bool focusResult =
                zoomResult &&
                _controlCommandService
                    .SetLaPresetFocusAndCommit(
                        focus);

            bool result =
                idResult &&
                panResult &&
                tiltResult &&
                zoomResult &&
                focusResult;

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 1 / LA] ADD / UPDATE");

            Console.WriteLine(
                $"[PRESET 1 / LA] DISPLAY=P{presetNumber:00} / " +
                $"INTERNAL_ID={laInternalId} / " +
                $"PAN={_currentPan:F2} / " +
                $"TILT={_currentTilt:F2} / " +
                $"ZOOM={zoom} / FOCUS={focus} / " +
                $"RESULT={result}");

            ConsoleLogHelper.PrintLine();

            if (!result)
            {
                LaPresetCommandStatusText =
                    $"P{presetNumber:00} REGISTER SEND FAILED";

                return;
            }

            PresetPointOption newPreset =
                new PresetPointOption(
                    presetNumber,
                    _currentPan,
                    _currentTilt,
                    zoom.ToString(),
                    focus.ToString(),
                    GetCurrentIrZoomStandardPosition().ToString(),
                    GetCurrentIrFocusStandardPosition().ToString());

            UpsertLaPresetPoint(
                newPreset);

            LaPresetCommandStatusText =
                $"P{presetNumber:00} LA SCAN POINT REGISTERED";
        }

        /// <summary>
        /// PRESET 1 선택 지점으로 WPF 저장값 기준 PTZF 전체를 직접 복원한다.
        ///
        /// LA 0x05 한 번에 의존하지 않고 Pan, Tilt, EO/IR Zoom,
        /// EO/IR Focus를 현재 프로젝트의 실제 제어 경로로 각각 실행한다.
        /// </summary>
        private async Task MoveToSelectedLaPresetPointAsync()
        {
            PresetPointOption preset =
                SelectedLaPresetPoint;

            if (preset == null)
            {
                LaPresetCommandStatusText =
                    "MOVE FAILED : SELECT LA PRESET";

                return;
            }

            CancellationTokenSource moveCts =
                new CancellationTokenSource();

            CancellationTokenSource oldMoveCts =
                Interlocked.Exchange(
                    ref _laPresetSingleMoveCts,
                    moveCts);

            if (oldMoveCts != null)
            {
                oldMoveCts.Cancel();
            }

            LaPresetCommandStatusText =
                $"P{preset.Number:00} DIRECT PTZF MOVING";

            try
            {
                bool result =
                    await MoveLaPresetPointDirectAsync(
                        preset,
                        moveCts.Token);

                Console.WriteLine();
                Console.WriteLine(
                    "[PRESET 1 / WPF DIRECT] MOVE");

                Console.WriteLine(
                    $"[PRESET 1 / WPF DIRECT] DISPLAY=P{preset.Number:00} / " +
                    $"PAN={preset.Pan:F2} / TILT={preset.Tilt:F2} / " +
                    $"EO_ZOOM={preset.EoZoomText} / " +
                    $"EO_FOCUS={preset.EoFocusText} / " +
                    $"IR_ZOOM={preset.IrZoomText} / " +
                    $"IR_FOCUS={preset.IrFocusText} / RESULT={result}");

                ConsoleLogHelper.PrintLine();

                LaPresetCommandStatusText =
                    result
                        ? $"P{preset.Number:00} DIRECT PTZF MOVE COMPLETED"
                        : $"P{preset.Number:00} DIRECT PTZF MOVE INCOMPLETE";
            }
            catch (OperationCanceledException)
            {
                LaPresetCommandStatusText =
                    $"P{preset.Number:00} DIRECT PTZF MOVE STOPPED";
            }
            finally
            {
                if (ReferenceEquals(
                        Interlocked.CompareExchange(
                            ref _laPresetSingleMoveCts,
                            null,
                            moveCts),
                        moveCts))
                {
                    moveCts.Dispose();
                }

            }

        }

        /// <summary>
        /// 저장된 PRESET 1 한 지점의 PTZF 값을 직접 복원한다.
        /// </summary>
        private async Task<bool> MoveLaPresetPointDirectAsync(
            PresetPointOption preset,
            CancellationToken cancellationToken)
        {
            if (preset == null)
            {
                return false;
            }

            cancellationToken
                .ThrowIfCancellationRequested();

            int moveSpeed =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        LaPresetScanSpeed));

            int settleTimeoutMs =
                CalculatePresetPanTiltSettleTimeoutMs(
                    preset,
                    moveSpeed);

            bool panSpeedResult =
                _controlCommandService
                    .SetPanPositionSpeed(
                        moveSpeed);

            bool tiltSpeedResult =
                _controlCommandService
                    .SetTiltPositionSpeed(
                        moveSpeed);

            bool speedResult =
                panSpeedResult &&
                tiltSpeedResult;

            bool modeResult =
                ApplySelectedPanTurnMode();

            bool panResult =
                modeResult &&
                speedResult &&
                _controlCommandService
                    .PanGoPosition(
                        preset.Pan);

            bool tiltResult =
                speedResult &&
                _controlCommandService
                    .TiltGoPosition(
                        preset.Tilt);

            if (panResult)
            {
                _lastPanAbsoluteTarget =
                    preset.Pan;
            }

            bool panTiltSettled =
                panResult &&
                tiltResult &&
                await EnsurePresetPanTiltTargetAsync(
                    "PRESET L",
                    preset,
                    () =>
                    {
                        bool retryPanSpeedResult =
                            _controlCommandService
                                .SetPanPositionSpeed(
                                    moveSpeed);

                        bool retryTiltSpeedResult =
                            _controlCommandService
                                .SetTiltPositionSpeed(
                                    moveSpeed);

                        bool retryModeResult =
                            ApplySelectedPanTurnMode();

                        bool retryPanResult =
                            retryPanSpeedResult &&
                            retryTiltSpeedResult &&
                            retryModeResult &&
                            _controlCommandService
                                .PanGoPosition(
                                    preset.Pan);

                        bool retryTiltResult =
                            retryPanSpeedResult &&
                            retryTiltSpeedResult &&
                            _controlCommandService
                                .TiltGoPosition(
                                    preset.Tilt);

                        return retryModeResult &&
                               retryPanSpeedResult &&
                               retryTiltSpeedResult &&
                               retryPanResult &&
                               retryTiltResult;
                    },
                    settleTimeoutMs,
                    cancellationToken);

            int eoZoom =
                ParsePresetPosition(
                    preset.EoZoomText,
                    GetCurrentPresetStandardZoom());

            int eoFocus =
                ParsePresetPosition(
                    preset.EoFocusText,
                    GetCurrentPresetStandardFocus());

            int irZoom =
                ParsePresetPosition(
                    preset.IrZoomText,
                    GetCurrentIrZoomStandardPosition());

            int irFocus =
                ParsePresetPosition(
                    preset.IrFocusText,
                    GetCurrentIrFocusStandardPosition());

            bool zoomResult =
                await MoveLaPresetZoomDirectAsync(
                    eoZoom,
                    irZoom,
                    cancellationToken);

            bool focusResult =
                await MoveLaPresetFocusDirectAsync(
                    eoFocus,
                    irFocus,
                    cancellationToken);

            bool result =
                speedResult &&
                modeResult &&
                panResult &&
                tiltResult &&
                panTiltSettled &&
                zoomResult &&
                focusResult;

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 1 / WPF DIRECT] PTZF RESULT");

            Console.WriteLine(
                $"[PRESET 1 / WPF DIRECT] " +
                $"SPEED={moveSpeed}deg/s:{speedResult} / " +
                $"P={panResult} / T={tiltResult} / " +
                $"PT_SETTLED={panTiltSettled} / " +
                $"Z={zoomResult} / F={focusResult} / " +
                $"RESULT={result}");

            ConsoleLogHelper.PrintLine();

            return result;
        }

        /// <summary>
        /// 프리셋 Pan / Tilt 목표각 도달을 최신 상태 Packet으로 확인하고,
        /// 0.01° Protocol 위치가 일치하지 않으면 제한 횟수만 재보정한다.
        /// </summary>
        private async Task<bool> EnsurePresetPanTiltTargetAsync(
            string category,
            PresetPointOption preset,
            Func<bool> resendCommand,
            int settleTimeoutMs,
            CancellationToken cancellationToken)
        {
            for (int correctionCount = 0;
                 correctionCount <=
                 PresetPanTiltCorrectionRetryCount;
                 correctionCount++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (correctionCount > 0)
                {
                    double correctionPanDelta =
                        GetShortestPanDifference(
                            _currentPan,
                            preset.Pan);

                    double correctionTiltDelta =
                        _currentTilt -
                        preset.Tilt;

                    bool resendResult =
                        resendCommand != null &&
                        resendCommand();

                    ConsoleLogHelper.Command(
                        category,
                        $"Pan/Tilt correction / " +
                        $"COUNT={correctionCount}/" +
                        $"{PresetPanTiltCorrectionRetryCount} / " +
                        $"TARGET={preset.Pan:F2},{preset.Tilt:F2} / " +
                        $"ACTUAL={_currentPan:F2},{_currentTilt:F2} / " +
                        $"DELTA={correctionPanDelta:+0.00;-0.00;0.00}," +
                        $"{correctionTiltDelta:+0.00;-0.00;0.00} / " +
                        $"RESULT={resendResult}");

                    if (!resendResult)
                    {
                        return false;
                    }

                }

                bool settled =
                    await WaitForPresetPanTiltTargetAsync(
                        category,
                        preset,
                        correctionCount,
                        settleTimeoutMs,
                        cancellationToken);

                if (settled)
                {
                    return true;
                }

            }

            double finalPanDelta =
                GetShortestPanDifference(
                    _currentPan,
                    preset.Pan);

            double finalTiltDelta =
                _currentTilt -
                preset.Tilt;

            ConsoleLogHelper.Warning(
                category,
                $"Pan/Tilt settle incomplete / " +
                $"TARGET_PAN={preset.Pan:F2} / " +
                $"ACTUAL_PAN={_currentPan:F2} / " +
                $"DELTA_PAN={finalPanDelta:+0.00;-0.00;0.00} / " +
                $"TARGET_TILT={preset.Tilt:F2} / " +
                $"ACTUAL_TILT={_currentTilt:F2} / " +
                $"DELTA_TILT={finalTiltDelta:+0.00;-0.00;0.00}");

            return false;
        }

        /// <summary>
        /// 명령 이후 새로 수신된 상태 Packet만 사용하여 목표각 안정화를 판정한다.
        /// </summary>
        private async Task<bool> WaitForPresetPanTiltTargetAsync(
            string category,
            PresetPointOption preset,
            int correctionCount,
            int settleTimeoutMs,
            CancellationToken cancellationToken)
        {
            long observedVersion =
                Interlocked.Read(
                    ref _panTiltStatusVersion);

            int stableCount = 0;
            int stationaryCount = 0;

            bool hasPreviousPosition =
                false;

            bool movementObserved =
                false;

            double previousPan = 0.0;
            double previousTilt = 0.0;

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   settleTimeoutMs)
            {
                await Task.Delay(
                    100,
                    cancellationToken);

                long currentVersion =
                    Interlocked.Read(
                        ref _panTiltStatusVersion);

                if (currentVersion ==
                    observedVersion)
                {
                    continue;
                }

                observedVersion =
                    currentVersion;

                double currentPan =
                    _currentPan;

                double currentTilt =
                    _currentTilt;

                double panDelta =
                    GetShortestPanDifference(
                        currentPan,
                        preset.Pan);

                double tiltDelta =
                    currentTilt -
                    preset.Tilt;

                bool isTargetSample =
                    Math.Abs(
                        panDelta) <=
                    PresetPanTiltTargetTolerance &&
                    Math.Abs(
                        tiltDelta) <=
                    PresetPanTiltTargetTolerance;

                stableCount =
                    isTargetSample
                        ? stableCount + 1
                        : 0;

                if (hasPreviousPosition)
                {
                    double panMovement =
                        Math.Abs(
                            GetShortestPanDifference(
                                currentPan,
                                previousPan));

                    double tiltMovement =
                        Math.Abs(
                            currentTilt -
                            previousTilt);

                    bool isMoving =
                        panMovement >
                            PresetPanTiltTargetTolerance ||
                        tiltMovement >
                            PresetPanTiltTargetTolerance;

                    if (isMoving)
                    {
                        movementObserved =
                            true;

                        stationaryCount =
                            0;
                    }
                    else if (movementObserved)
                    {
                        stationaryCount++;
                    }

                }

                previousPan =
                    currentPan;

                previousTilt =
                    currentTilt;

                hasPreviousPosition =
                    true;

                if (stableCount >=
                    PresetPanTiltStableSampleCount)
                {
                    ConsoleLogHelper.State(
                        category,
                        $"Pan/Tilt settled / " +
                        $"TARGET={preset.Pan:F2},{preset.Tilt:F2} / " +
                        $"ACTUAL={currentPan:F2},{currentTilt:F2} / " +
                        $"CORRECTION={correctionCount}");

                    return true;
                }

                if (movementObserved &&
                    stationaryCount >=
                    PresetPanTiltStationarySampleCount)
                {
                    return false;
                }

            }

            return false;
        }

        /// <summary>
        /// 현재 위치와 목표 위치의 각도 차이 및 설정 속도로 통신 장애
        /// 방지용 안전 제한시간을 계산한다. 정상 도착 시 즉시 반환되므로
        /// 이 값은 사용자 Delay에 추가되지 않는다.
        /// </summary>
        private int CalculatePresetPanTiltSettleTimeoutMs(
            PresetPointOption preset,
            double speedDegreesPerSecond)
        {
            double panDistance =
                Math.Abs(
                    GetShortestPanDifference(
                        _currentPan,
                        preset.Pan));

            double tiltDistance =
                Math.Abs(
                    _currentTilt -
                    preset.Tilt);

            double maximumDistance =
                Math.Max(
                    panDistance,
                    tiltDistance);

            double safeSpeed =
                Math.Max(
                    1.0,
                    speedDegreesPerSecond);

            int expectedTravelMs =
                (int)Math.Ceiling(
                    maximumDistance /
                    safeSpeed *
                    1000.0);

            return Math.Max(
                PresetPanTiltSettleTimeoutMinimumMs,
                Math.Min(
                    PresetPanTiltSettleTimeoutMaximumMs,
                    expectedTravelMs +
                    PresetPanTiltSafetyTimeoutMarginMs));
        }

        /// <summary>
        /// ±180° 경계를 고려한 Pan 최단 각도 차이를 반환한다.
        /// </summary>
        private static double GetShortestPanDifference(
            double currentPan,
            double targetPan)
        {
            double difference =
                currentPan -
                targetPan;

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
        /// ParsePresetPosition 데이터 해석 함수.
        /// </summary>
        private static int ParsePresetPosition(
            string text,
            int fallback)
        {
            int parsed;

            if (!int.TryParse(
                    text,
                    out parsed))
            {
                parsed =
                    fallback;
            }

            return Math.Max(
                MoveControlPositionMinimum,
                Math.Min(
                    MoveControlPositionMaximum,
                    parsed));
        }

        /// <summary>
        /// PRESET 복원 전에 EO/IR Zoom이 이미 목표 위치인지 확인한다.
        /// 이미 도착한 축에는 불필요한 Direct 명령과 완료 조회를 보내지 않는다.
        /// </summary>
        private bool IsPresetEoZoomAtTarget(
            int standardTarget)
        {
            if (SelectedEquipmentStatusMode !=
                    EquipmentStatusMode.Environment &&
                _connectedEoCtecSource != null)
            {
                int rawTarget =
                    ConvertStandardZoomToCtecRaw(
                        standardTarget);

                return Math.Abs(
                           _currentCtecEoZoomPosition -
                           rawTarget) <=
                       RooftopZoomSyncTolerance;
            }

            return Math.Abs(
                       _currentEoZoom -
                       standardTarget) <= 1;
        }

        /// <summary>
        /// IsPresetEoFocusAtTarget 상태 확인 함수.
        /// </summary>
        private bool IsPresetEoFocusAtTarget(
            int standardTarget)
        {
            if (SelectedEquipmentStatusMode !=
                    EquipmentStatusMode.Environment &&
                _connectedEoCtecSource != null)
            {
                int rawTarget =
                    ConvertStandardFocusToCtecRaw(
                        standardTarget);

                return Math.Abs(
                           _currentCtecEoFocusPosition -
                           rawTarget) <=
                       RooftopFocusSyncTolerance;
            }

            return Math.Abs(
                       _currentEoFocus -
                       standardTarget) <= 1;
        }

        /// <summary>
        /// AUTO SCAN 시작 시 첫 프리셋의 전체 PTZF가 이미 현재 상태와 같은지 확인한다.
        /// 같은 지점이면 불필요한 복원과 Delay 없이 다음 프리셋을 즉시 시작한다.
        /// </summary>
        private bool IsLaPresetPointAlreadyAtTarget(
            PresetPointOption preset)
        {
            int eoZoom =
                ParsePresetPosition(
                    preset.EoZoomText,
                    GetCurrentPresetStandardZoom());

            int eoFocus =
                ParsePresetPosition(
                    preset.EoFocusText,
                    GetCurrentPresetStandardFocus());

            int irZoom =
                ParsePresetPosition(
                    preset.IrZoomText,
                    GetCurrentIrZoomStandardPosition());

            int irFocus =
                ParsePresetPosition(
                    preset.IrFocusText,
                    GetCurrentIrFocusStandardPosition());

            return Math.Abs(
                       GetShortestPanDifference(
                           _currentPan,
                           preset.Pan)) <=
                       PresetPanTiltTargetTolerance &&
                   Math.Abs(
                       _currentTilt -
                       preset.Tilt) <=
                       PresetPanTiltTargetTolerance &&
                   IsPresetEoZoomAtTarget(
                       eoZoom) &&
                   IsPresetEoFocusAtTarget(
                       eoFocus) &&
                   Math.Abs(
                       GetCurrentIrZoomStandardPosition() -
                       irZoom) <= 1 &&
                   Math.Abs(
                       GetCurrentIrFocusStandardPosition() -
                       irFocus) <=
                       IrFocusSyncTolerance;
        }

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Zoom을 각각 복원한다.
        /// </summary>
        private async Task<bool> MoveLaPresetZoomDirectAsync(
            int eoStandardPosition,
            int irPosition,
            CancellationToken cancellationToken)
        {
            int safeEoPosition =
                Clamp(
                    eoStandardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            int safeIrPosition =
                Clamp(
                    irPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            bool eoAlreadyAtTarget =
                IsPresetEoZoomAtTarget(
                    safeEoPosition);

            bool irAlreadyAtTarget =
                Math.Abs(
                    GetCurrentIrZoomStandardPosition() -
                    safeIrPosition) <= 1;

            if (eoAlreadyAtTarget &&
                irAlreadyAtTarget)
            {
                Console.WriteLine(
                    "[PRESET 1 / WPF DIRECT] ZOOM " +
                    $"/ EO={safeEoPosition}:SKIPPED " +
                    $"/ IR={safeIrPosition}:SKIPPED");

                return true;
            }

            await StopZoomSyncAsync();

            cancellationToken
                .ThrowIfCancellationRequested();

            bool irResult =
                irAlreadyAtTarget ||
                _webAgentZoomControlService
                    .SetIrZoomPosition(
                        (short)safeIrPosition);

            bool eoResult;

            if (eoAlreadyAtTarget)
            {
                eoResult =
                    true;
            }
            else if (SelectedEquipmentStatusMode ==
                EquipmentStatusMode.Environment)
            {
                eoResult =
                    _controlCommandService
                        .EoZoomGoPosition(
                            (short)safeEoPosition);
            }
            else
            {
                RtspSourceOption ctecSource =
                    _connectedEoCtecSource;

                if (ctecSource == null)
                {
                    eoResult =
                        _controlCommandService
                            .EoZoomGoPosition(
                                (short)safeEoPosition);
                }
                else
                {
                    int eoRawTarget =
                        ConvertStandardZoomToCtecRaw(
                            safeEoPosition);

                    eoResult =
                        await MoveRooftopEoZoomToRawPositionAsync(
                            ctecSource,
                            eoRawTarget,
                            cancellationToken);
                }

            }

            Console.WriteLine(
                "[PRESET 1 / WPF DIRECT] ZOOM " +
                $"/ EO={safeEoPosition}:{eoResult} " +
                $"/ IR={safeIrPosition}:{irResult}");

            return eoResult &&
                irResult;
        }

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Focus를 각각 복원한다.
        /// </summary>
        private async Task<bool> MoveLaPresetFocusDirectAsync(
            int eoStandardPosition,
            int irStandardPosition,
            CancellationToken cancellationToken)
        {
            int safeEoPosition =
                Clamp(
                    eoStandardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            int safeIrStandardPosition =
                Clamp(
                    irStandardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            int irRawTargetPosition =
                ConvertIrFocusStandardToStatusPosition(
                    safeIrStandardPosition);

            bool eoAlreadyAtTarget =
                IsPresetEoFocusAtTarget(
                    safeEoPosition);

            bool irAlreadyAtTarget =
                Math.Abs(
                    GetCurrentIrFocusStandardPosition() -
                    safeIrStandardPosition) <=
                IrFocusSyncTolerance;

            if (eoAlreadyAtTarget &&
                irAlreadyAtTarget)
            {
                Console.WriteLine(
                    "[PRESET 1 / WPF DIRECT] FOCUS " +
                    $"/ EO={safeEoPosition}:SKIPPED " +
                    $"/ IR_STANDARD={safeIrStandardPosition}:SKIPPED");

                return true;
            }

            await StopFocusSyncAsync();

            cancellationToken
                .ThrowIfCancellationRequested();

            Task<bool> irMoveTask =
                irAlreadyAtTarget
                    ? Task.FromResult(
                        true)
                    : MoveIrFocusToPositionAsync(
                        irRawTargetPosition,
                        cancellationToken);

            bool eoResult;

            if (eoAlreadyAtTarget)
            {
                eoResult =
                    true;
            }
            else if (SelectedEquipmentStatusMode ==
                EquipmentStatusMode.Environment)
            {
                eoResult =
                    _controlCommandService
                        .EoFocusGoPosition(
                            (short)safeEoPosition);
            }
            else
            {
                RtspSourceOption ctecSource =
                    _connectedEoCtecSource;

                if (ctecSource == null)
                {
                    eoResult =
                        _controlCommandService
                            .EoFocusGoPosition(
                                (short)safeEoPosition);
                }
                else
                {
                    int eoRawTarget =
                        ConvertStandardFocusToCtecRaw(
                            safeEoPosition);

                    eoResult =
                        await MoveRooftopEoFocusToRawPositionAsync(
                            ctecSource,
                            eoRawTarget,
                            cancellationToken);
                }

            }

            bool irResult =
                await irMoveTask;

            Console.WriteLine(
                "[PRESET 1 / WPF DIRECT] FOCUS " +
                $"/ EO={safeEoPosition}:{eoResult} " +
                $"/ IR_STANDARD={safeIrStandardPosition} " +
                $"/ IR_RAW_TARGET={irRawTargetPosition}:{irResult}");

            return eoResult &&
                irResult;
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        private ushort GetCurrentPresetStandardZoom()
        {
            if (_connectedEoCtecSource != null)
            {
                return (ushort)Math.Max(
                    0,
                    Math.Min(
                        1000,
                        (int)Math.Round(
                            _currentCtecEoZoomPosition *
                            1000.0 /
                            CtecEoZoomPositionMax)));
            }

            return (ushort)Math.Max(
                0,
                Math.Min(
                    1000,
                    (int)_currentEoZoom));
        }

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        private ushort GetCurrentPresetStandardFocus()
        {
            if (_connectedEoCtecSource != null)
            {
                return (ushort)Math.Max(
                    0,
                    Math.Min(
                        1000,
                        (int)Math.Round(
                            _currentCtecEoFocusPosition *
                            1000.0 /
                            CtecEoFocusPositionMax)));
            }

            return (ushort)Math.Max(
                0,
                Math.Min(
                    1000,
                    (int)_currentEoFocus));
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다.
        ///
        /// ROOFTOP / LA AGENT:
        /// Raw 1000(Wide) -> Standard 0(Wide)
        /// Raw 0(Tele)    -> Standard 1000(Tele)
        ///
        /// ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        private int GetCurrentIrZoomStandardPosition()
        {
            int safeRaw =
                Clamp(
                    (int)_currentIrZoom,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            return SelectedEquipmentStatusMode ==
                EquipmentStatusMode.Rooftop
                    ? MoveControlPositionMaximum -
                      safeRaw
                    : safeRaw;
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다.
        ///
        /// ROOFTOP / LA AGENT:
        /// Raw 1000(Far) -> Standard 0(Far)
        /// Raw 0(Near)   -> Standard 1000(Near)
        ///
        /// ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        private int GetCurrentIrFocusStandardPosition()
        {
            int safeRaw =
                Clamp(
                    (int)_currentIrFocus,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            return SelectedEquipmentStatusMode ==
                EquipmentStatusMode.Rooftop
                    ? MoveControlPositionMaximum -
                      safeRaw
                    : safeRaw;
        }

        /// <summary>
        /// 화면의 IR Focus 표준 목표값을 현재 장비가 보고하는 상태 좌표로 변환한다.
        /// LA 상태 좌표만 반대 방향이며, Web Agent 상태 좌표는 그대로 사용한다.
        /// </summary>
        private int ConvertIrFocusStandardToStatusPosition(
            int standardPosition)
        {
            int safePosition =
                Clamp(
                    standardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            return SelectedEquipmentStatusMode ==
                EquipmentStatusMode.Rooftop
                    ? MoveControlPositionMaximum -
                      safePosition
                    : safePosition;
        }

        /// <summary>
        /// PRESET 1 화면 목록에 같은 번호가 있으면 갱신하고,
        /// 없으면 번호순으로 삽입한다.
        /// </summary>
        private void UpsertLaPresetPoint(
            PresetPointOption newPreset)
        {
            PresetPointOption existingPreset =
                LaPresetPoints
                    .FirstOrDefault(
                        preset =>
                            preset.Number ==
                            newPreset.Number);

            if (existingPreset != null)
            {
                LaPresetPoints.Remove(
                    existingPreset);
            }

            int insertIndex =
                0;

            while (insertIndex <
                       LaPresetPoints.Count &&
                   LaPresetPoints[insertIndex].Number <
                       newPreset.Number)
            {
                insertIndex++;
            }

            LaPresetPoints.Insert(
                insertIndex,
                newPreset);

            SelectedLaPresetPoint =
                newPreset;
        }

        /// <summary>
        /// ClearAllLaPresetPoints 동작 수행 함수.
        /// </summary>
        private void ClearAllLaPresetPoints()
        {
            StopLaPresetScan();

            bool result =
                _controlCommandService
                    .ClearAllLaPresets();

            if (result)
            {
                LaPresetPoints.Clear();
                SelectedLaPresetPoint =
                    null;

                IsLaPresetScanRunning =
                    false;
            }

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 1 / LA] CLEAR ALL");

            Console.WriteLine(
                $"[PRESET 1 / LA] RESULT={result}");

            ConsoleLogHelper.PrintLine();

            LaPresetCommandStatusText =
                result
                    ? "LA PRESET DATA CLEAR COMMAND SENT"
                    : "LA PRESET DATA CLEAR SEND FAILED";
        }

        /// <summary>
        /// 2026-08-28: PRESET L의 선택 항목만 자동 순회 목록에서 제거한다.
        /// LA Agent는 개별 삭제 명령을 제공하지 않으므로 장비 전체 프리셋은
        /// 유지하고, 현재 Viewer의 선택 슬롯만 제거한다.
        /// </summary>
        private void DeleteSelectedLaPresetPoint()
        {
            PresetPointOption selected =
                SelectedLaPresetPoint;

            if (selected == null)
            {
                LaPresetCommandStatusText =
                    "DELETE FAILED : SELECT PRESET";
                return;
            }

            LaPresetPoints.Remove(selected);
            SelectedLaPresetPoint =
                LaPresetPoints.FirstOrDefault();

            ConsoleLogHelper.State(
                "PRESET L",
                "Selected scan point removed / NUMBER=" + selected.Number +
                " / REMAINING=" + LaPresetPoints.Count);
            LaPresetCommandStatusText =
                $"P{selected.Number:00} REMOVED FROM SCAN LIST";
        }

        /// <summary>
        /// PRESET 1 저장 목록을 WPF가 직접 P01 -> P02 -> ... 순회한다.
        /// 각 지점마다 PTZF 전체 직접 복원 루틴을 사용한다.
        /// </summary>
        private async Task StartLaPresetScanAsync()
        {
            ConsoleLogHelper.Command(
                "PRESET L",
                "Scan start requested");

            if (LaPresetPoints.Count <
                2)
            {
                LaPresetCommandStatusText =
                    "SCAN START FAILED : REGISTER 2 OR MORE POINTS";

                return;
            }

            CancellationTokenSource oldCts =
                Interlocked.Exchange(
                    ref _laPresetDirectScanCts,
                    null);

            if (oldCts != null)
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            CancellationTokenSource scanCts =
                new CancellationTokenSource();

            _laPresetDirectScanCts =
                scanCts;

            int delaySeconds =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        LaPresetScanDelay));

            int speedDegreesPerSecond =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        LaPresetScanSpeed));

            IsLaPresetScanRunning =
                true;

            LaPresetCommandStatusText =
                $"WPF DIRECT SCAN START / {LaPresetPoints.Count} POINTS";

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 1 / WPF DIRECT SCAN] START");

            Console.WriteLine(
                $"[PRESET 1 / WPF DIRECT SCAN] " +
                $"POINTS={LaPresetPoints.Count} / " +
                $"SPEED={speedDegreesPerSecond}deg/s / " +
                $"DELAY={delaySeconds}s");

            ConsoleLogHelper.PrintLine();

            try
            {
                bool isFirstPointOfScan =
                    true;

                while (!scanCts.IsCancellationRequested)
                {
                    PresetPointOption[] points =
                        LaPresetPoints
                            .OrderBy(
                                point =>
                                    point.Number)
                            .ToArray();

                    foreach (PresetPointOption point in points)
                    {
                        scanCts.Token
                            .ThrowIfCancellationRequested();

                        if (isFirstPointOfScan &&
                            IsLaPresetPointAlreadyAtTarget(
                                point))
                        {
                            isFirstPointOfScan =
                                false;

                            SelectedLaPresetPoint =
                                point;

                            ConsoleLogHelper.Command(
                                "PRESET L",
                                $"Initial current point skipped / P{point.Number:00} / " +
                                "NEXT POINT STARTS IMMEDIATELY");

                            continue;
                        }

                        isFirstPointOfScan =
                            false;

                        SelectedLaPresetPoint =
                            point;

                        LaPresetCommandStatusText =
                            $"SCAN MOVING P{point.Number:00}";

                        bool moveResult =
                            await MoveLaPresetPointDirectAsync(
                                point,
                                scanCts.Token);

                        Console.WriteLine(
                            "[PRESET 1 / WPF DIRECT SCAN] " +
                            $"P{point.Number:00} MOVE RESULT={moveResult}");

                        await WaitAfterPresetMoveAsync(
                            LaPresetScanDelay,
                            scanCts.Token);
                    }

                }

            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "[PRESET 1 / WPF DIRECT SCAN] CANCELED");
            }
            finally
            {
                if (ReferenceEquals(
                        _laPresetDirectScanCts,
                        scanCts))
                {
                    _laPresetDirectScanCts =
                        null;

                    scanCts.Dispose();
                }

                IsLaPresetScanRunning =
                    false;

                LaPresetCommandStatusText =
                    "WPF DIRECT SCAN STOPPED";

                ConsoleLogHelper.PrintLine();
            }

        }

        /// <summary>
        /// 현재 프리셋의 PTZF 복원이 완료된 뒤 설정한 Delay만큼 정확히 대기한다.
        /// 첫 프리셋은 이 메서드를 거치기 전에 즉시 실행되므로 시작 전 대기는 없다.
        /// </summary>
        private async Task WaitAfterPresetMoveAsync(
            int configuredDelaySeconds,
            CancellationToken cancellationToken)
        {
            int safeDelaySeconds =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        configuredDelaySeconds));

            long delayStartedTimestamp =
                Stopwatch.GetTimestamp();

            await Task.Delay(
                safeDelaySeconds * 1000,
                cancellationToken);

            double actualDelaySeconds =
                (Stopwatch.GetTimestamp() - delayStartedTimestamp) /
                (double)Stopwatch.Frequency;

            ConsoleLogHelper.Command(
                "AUTO SCAN",
                $"Delay after move completed / " +
                $"CONFIGURED={safeDelaySeconds}s / " +
                $"ACTUAL={actualDelaySeconds:F3}s");
        }

        /// <summary>
        /// UpdateLaPresetScan 갱신 함수.
        /// </summary>
        private void UpdateLaPresetScan()
        {
            int speed =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        LaPresetScanSpeed));

            int delay =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        LaPresetScanDelay));

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 1 / WPF DIRECT SCAN] OPTION");

            Console.WriteLine(
                $"[PRESET 1 / WPF DIRECT SCAN] " +
                $"SPEED={speed}deg/s / DELAY={delay}s");

            ConsoleLogHelper.PrintLine();

            LaPresetCommandStatusText =
                $"SPEED={speed}deg/s NEXT POINT / " +
                $"DELAY={delay}s NEXT START";
        }

        /// <summary>
        /// StopLaPresetScan 중지 함수.
        /// </summary>
        private void StopLaPresetScan()
        {
            ConsoleLogHelper.Command(
                "PRESET L",
                "Auto scan stop requested / CMD2=0x9B");

            CancellationTokenSource scanCts =
                Interlocked.Exchange(
                    ref _laPresetDirectScanCts,
                    null);

            if (scanCts != null)
            {
                scanCts.Cancel();
            }

            bool stopResult =
                _controlCommandService
                    .StopPresetScan();

            IsLaPresetScanRunning =
                false;

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 1 / WPF DIRECT SCAN] STOP REQUEST");

            ConsoleLogHelper.PrintLine();

            LaPresetCommandStatusText =
                "AUTO SCAN STOP REQUESTED";

            ConsoleLogHelper.Command(
                "PRESET L",
                $"Auto scan stop sent / CMD2=0x9B / RESULT={stopResult}");
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다.
        /// AUTO SCAN 반복 상태에는 영향을 주지 않으며,
        /// Pan/Tilt 위치 이동 정지 명령(0x4F)을 각각 송신한다.
        /// </summary>
        private void StopLaPresetMove()
        {
            CancellationTokenSource moveCts =
                Interlocked.Exchange(
                    ref _laPresetSingleMoveCts,
                    null);

            if (moveCts != null)
            {
                moveCts.Cancel();
            }

            bool stopResult =
                _controlCommandService
                    .StopPanTiltPositionMove();

            LaPresetCommandStatusText =
                "PRESET L MOVE STOPPED";

            ConsoleLogHelper.Command(
                "PRESET L",
                $"Single move stop / CMD2=0x4F / RESULT={stopResult}");
        }

        /// <summary>
        /// PRESET W 이동에 사용할 Pan / Tilt 위치 속도를 장비에 적용한다.
        /// AUTO SCAN의 Speed(1 ~ 60)와 단일 MOVE TO PRESET이 같은 값을 사용한다.
        /// </summary>
        private bool ApplyPresetPositionSpeed(
            int speed)
        {
            int safeSpeed =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        speed));

            return _controlCommandService
                       .SetPanPositionSpeed(
                           safeSpeed) &&
                   _controlCommandService
                       .SetTiltPositionSpeed(
                           safeSpeed);
        }

        /// <summary>
        /// AddOrUpdatePresetPoint 동작 수행 함수.
        /// </summary>
        private void AddOrUpdatePresetPoint()
        {
            ConsoleLogHelper.Command(
                "PRESET W",
                "Set preset requested");

            int presetNumber =
                Math.Max(
                    1,
                    Math.Min(
                        63,
                        PresetSlotNumber));

            /*
             * PRESET 2는 WEB AGENT Pelco-D 프리셋 검증 모드다.
             * LA 전용 Scan Point(0x19/0x91/0x93/0x95/0x97)는 절대 등록하지 않는다.
             *
             * 문서 2.10:
             * Command2 = 0x03 : SET PRESET
             * Data2    = Preset Number
             */
            bool result =
                _controlCommandService
                    .AddPresetPoint(
                        (byte)presetNumber);

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 2 / WEB AGENT] SET PRESET");

            Console.WriteLine(
                $"[PRESET 2 / WEB AGENT] NUMBER={presetNumber} / " +
                $"PELCO_D=0x03 / RESULT={result}");

            ConsoleLogHelper.PrintLine();

            if (!result)
            {
                PresetCommandStatusText =
                    $"P{presetNumber:00} SET PRESET FAILED";

                return;
            }

            PresetPointOption newPreset =
                new PresetPointOption(
                    presetNumber,
                    _currentPan,
                    _currentTilt,
                    CurrentEoZoomText,
                    CurrentEoFocusText,
                    GetCurrentIrZoomStandardPosition().ToString(),
                    GetCurrentIrFocusStandardPosition().ToString());

            UpsertPresetPoint(
                newPreset);

            PresetCommandStatusText =
                $"P{presetNumber:00} SET PRESET SENT";
        }

        /// <summary>
        /// 선택 슬롯의 프리셋을 제거한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x05
        /// Data1    = 0x00
        /// Data2    = Preset Number (1 ~ 63)
        /// </summary>
        private void DeletePresetPoint()
        {
            if (SelectedPresetPoint == null)
            {
                PresetCommandStatusText =
                    "DELETE FAILED : SELECT PRESET";

                return;
            }

            int presetNumber =
                SelectedPresetPoint.Number;

            /*
             * PRESET 2 WEB AGENT Pelco-D:
             * Command2 = 0x05 : CLEAR PRESET
             * Data2    = Preset Number
             */
            bool result =
                _controlCommandService
                    .RemovePresetPoint(
                        (byte)presetNumber);

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 2 / WEB AGENT] CLEAR PRESET");

            Console.WriteLine(
                $"[PRESET 2 / WEB AGENT] NUMBER={presetNumber} / " +
                $"PELCO_D=0x05 / RESULT={result}");

            ConsoleLogHelper.PrintLine();

            if (!result)
            {
                PresetCommandStatusText =
                    $"P{presetNumber:00} CLEAR PRESET FAILED";

                return;
            }

            PresetPointOption removed =
                SelectedPresetPoint;

            PresetPoints.Remove(
                removed);

            SelectedPresetPoint =
                PresetPoints.FirstOrDefault();

            PresetCommandStatusText =
                $"P{presetNumber:00} CLEAR PRESET SENT";
        }

        /// <summary>
        /// 2026-08-28: PRESET W 전체 삭제를 각 슬롯의 표준 CLEAR PRESET 명령으로 수행한다.
        /// 일부 송신 실패 시 성공한 항목만 화면 목록에서 제거하고 실패 개수를 알린다.
        /// </summary>
        private void ClearAllPresetPoints()
        {
            StopPresetScan();

            PresetPointOption[] registered =
                PresetPoints.ToArray();
            int removedCount = 0;
            int failedCount = 0;

            foreach (PresetPointOption preset in registered)
            {
                try
                {
                    if (_controlCommandService.RemovePresetPoint((byte)preset.Number))
                    {
                        PresetPoints.Remove(preset);
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
                    ConsoleLogHelper.Error(
                        "PRESET W",
                        "Clear preset exception / NUMBER=" + preset.Number,
                        ex);
                }
            }

            SelectedPresetPoint =
                PresetPoints.FirstOrDefault();
            PresetCommandStatusText =
                failedCount == 0
                    ? "ALL REGISTERED PRESETS CLEARED"
                    : $"CLEAR COMPLETE : {removedCount} OK / {failedCount} FAILED";

            ConsoleLogHelper.State(
                "PRESET W",
                "Clear all completed / REMOVED=" + removedCount +
                " / FAILED=" + failedCount);
        }

        /// <summary>
        /// ComboBox에서 선택한 프리셋으로 이동한다.
        ///
        /// 현재 UI의 PAN TURN MODE를 먼저 송신한 뒤
        /// 프리셋 이동 명령을 송신한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x07
        /// Data1    = 0x00
        /// Data2    = Preset Number (1 ~ 63)
        /// </summary>
        private async void MoveToSelectedPresetPoint()
        {
            if (SelectedPresetPoint == null)
            {
                PresetCommandStatusText =
                    "MOVE FAILED : SELECT PRESET";

                Console.WriteLine(
                    "[PRESET 2 / WEB AGENT] GOTO FAILED : " +
                    "프리셋 미선택 상태");

                return;
            }

            int presetNumber =
                SelectedPresetPoint.Number;

            int moveSpeed =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        PresetScanSpeed));

            CancellationTokenSource moveCts =
                new CancellationTokenSource();

            CancellationTokenSource oldMoveCts =
                Interlocked.Exchange(
                    ref _presetSingleMoveCts,
                    moveCts);

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
            bool speedResult =
                ApplyPresetPositionSpeed(
                    moveSpeed);

            bool moveResult =
                speedResult &&
                _controlCommandService
                    .MoveToPresetPoint(
                        (byte)presetNumber);

            if (moveResult)
            {
                _lastPanAbsoluteTarget =
                    SelectedPresetPoint.Pan;
            }

            bool panTiltSettled =
                false;

            try
            {
                panTiltSettled =
                    moveResult &&
                    await EnsurePresetPanTiltTargetAsync(
                        "PRESET W",
                        SelectedPresetPoint,
                        () =>
                            ApplyPresetPositionSpeed(
                                moveSpeed) &&
                            _controlCommandService
                                .MoveToPresetPoint(
                                    (byte)presetNumber),
                        CalculatePresetPanTiltSettleTimeoutMs(
                            SelectedPresetPoint,
                            moveSpeed),
                        moveCts.Token);
            }
            catch (OperationCanceledException)
            {
                PresetCommandStatusText =
                    $"P{presetNumber:00} GOTO PRESET STOPPED";

                return;
            }
            finally
            {
                if (ReferenceEquals(
                        Interlocked.CompareExchange(
                            ref _presetSingleMoveCts,
                            null,
                            moveCts),
                        moveCts))
                {
                    moveCts.Dispose();
                }

            }

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 2 / WEB AGENT] GOTO PRESET");

            Console.WriteLine(
                $"[PRESET 2 / WEB AGENT] NUMBER={presetNumber} / " +
                $"SPEED={moveSpeed} / PELCO_D=0x07 / RESULT={moveResult} / " +
                $"PT_SETTLED={panTiltSettled}");

            ConsoleLogHelper.PrintLine();

            PresetCommandStatusText =
                moveResult &&
                panTiltSettled
                    ? $"P{presetNumber:00} GOTO PRESET COMPLETED"
                    : $"P{presetNumber:00} GOTO PRESET INCOMPLETE";
        }

        /// <summary>
        /// 프리셋 오토 스캔을 시작한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x99
        /// Data1    = Speed (1 ~ 60)
        /// Data2    = Delay (1 ~ 60)
        /// </summary>
        private void StartPresetScan()
        {
            ConsoleLogHelper.Command(
                "PRESET W",
                "Loop start requested");

            if (PresetPoints.Count <= 0)
            {
                PresetCommandStatusText =
                    "START FAILED : NO PRESET";

                return;
            }

            // 기존 PRESET 2 루프만 취소한다.
            // START 시 장비 Stop 명령을 먼저 보내지 않는다.
            CancelPreset2DirectScan();

            CancellationTokenSource scanCts =
                new CancellationTokenSource();

            _presetDirectScanCts =
                scanCts;

            IsPresetScanRunning =
                true;

            int delaySeconds =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        PresetScanDelay));

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 2 / WEB AGENT] LOOP START");

            Console.WriteLine(
                $"[PRESET 2 / WEB AGENT] COUNT={PresetPoints.Count} / " +
                $"DELAY={delaySeconds}s / " +
                "COMMAND=POSITION_SPEED_THEN_GOTO_PRESET_0x07");

            ConsoleLogHelper.PrintLine();

            PresetCommandStatusText =
                "WEB AGENT PRESET LOOP STARTED";

            _ =
                RunPreset2DirectScanAsync(
                    scanCts);
        }

        /// <summary>
        /// 실행 중인 PRESET 2 WPF 순회 Task만 취소한다.
        /// 장비 제어 명령은 전송하지 않는다.
        /// </summary>
        private bool CancelPreset2DirectScan()
        {
            CancellationTokenSource scanCts =
                Interlocked.Exchange(
                    ref _presetDirectScanCts,
                    null);

            if (scanCts == null)
            {
                return false;
            }

            scanCts.Cancel();
            scanCts.Dispose();

            return true;
        }

        /// <summary>
        /// PRESET 2는 LA Auto Scan(0x99)을 사용하지 않는다.
        /// WPF가 등록 목록을 순회하며 WEB AGENT에 Pelco-D GOTO PRESET(0x07)을 반복 송신한다.
        /// </summary>
        private async Task RunPreset2DirectScanAsync(
            CancellationTokenSource scanCts)
        {
            CancellationToken cancellationToken =
                scanCts.Token;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    PresetPointOption[] snapshot =
                        PresetPoints
                            .OrderBy(
                                preset =>
                                    preset.Number)
                            .ToArray();

                    foreach (PresetPointOption preset in snapshot)
                    {
                        cancellationToken
                            .ThrowIfCancellationRequested();

                        int moveSpeed =
                            Math.Max(
                                1,
                                Math.Min(
                                    60,
                                    PresetScanSpeed));

                        bool speedResult =
                            ApplyPresetPositionSpeed(
                                moveSpeed);

                        bool result =
                            speedResult &&
                            _controlCommandService
                                .MoveToPresetPoint(
                                    (byte)preset.Number);

                        Console.WriteLine();
                        Console.WriteLine(
                            "[PRESET 2 / WEB AGENT] LOOP GOTO PRESET");

                        Console.WriteLine(
                            $"[PRESET 2 / WEB AGENT] NUMBER={preset.Number} / " +
                            $"SPEED={moveSpeed} / PELCO_D=0x07 / RESULT={result}");

                        ConsoleLogHelper.PrintLine();

                        if (!result)
                        {
                            PresetCommandStatusText =
                                $"P{preset.Number:00} GOTO FAILED";

                            return;
                        }

                        bool panTiltSettled =
                            await EnsurePresetPanTiltTargetAsync(
                                "PRESET W",
                                preset,
                                () =>
                                    ApplyPresetPositionSpeed(
                                        moveSpeed) &&
                                    _controlCommandService
                                        .MoveToPresetPoint(
                                            (byte)preset.Number),
                                CalculatePresetPanTiltSettleTimeoutMs(
                                    preset,
                                    moveSpeed),
                                cancellationToken);

                        if (!panTiltSettled)
                        {
                            PresetCommandStatusText =
                                $"P{preset.Number:00} PAN/TILT SETTLE INCOMPLETE";

                            ConsoleLogHelper.Warning(
                                "PRESET W",
                                $"Loop continuing after settle limit / " +
                                $"P{preset.Number:00}");
                        }

                        PresetCommandStatusText =
                            panTiltSettled
                                ? $"P{preset.Number:00} GOTO COMPLETED"
                                : $"P{preset.Number:00} GOTO INCOMPLETE";

                        int delaySeconds =
                            Math.Max(
                                1,
                                Math.Min(
                                    60,
                                    PresetScanDelay));

                        await WaitAfterPresetMoveAsync(
                            delaySeconds,
                            cancellationToken);
                    }

                }

            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(
                        Interlocked.CompareExchange(
                            ref _presetDirectScanCts,
                            null,
                            scanCts),
                        scanCts))
                {
                    scanCts.Dispose();

                    IsPresetScanRunning =
                        false;
                }

            }

        }

        /// <summary>
        /// 실행 중인 스캔의 속도 / 정지시간을 변경한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x9D
        /// Data1    = Speed (1 ~ 60)
        /// Data2    = Delay (1 ~ 60)
        /// </summary>
        private void UpdatePresetScan()
        {
            int speed =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        PresetScanSpeed));

            int delay =
                Math.Max(
                    1,
                    Math.Min(
                        60,
                        PresetScanDelay));

            /*
             * PRESET 2는 0x99/0x9D LA Auto Scan을 사용하지 않는다.
             * APPLY 이후 다음 단일/반복 이동부터 Speed와 Delay를 사용한다.
             * Speed는 GOTO PRESET(0x07) 직전에 Pan/Tilt 위치 속도 명령으로 적용한다.
             */
            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 2 / WEB AGENT] LOOP OPTION APPLY");

            Console.WriteLine(
                $"[PRESET 2 / WEB AGENT] DELAY={delay}s / " +
                $"MOVE_SPEED={speed} / " +
                "APPLIES_TO=NEXT_SINGLE_OR_SCAN_MOVE");

            ConsoleLogHelper.PrintLine();

            PresetCommandStatusText =
                $"MOVE SPEED={speed} / LOOP DELAY={delay}s APPLIED";
        }

        /// <summary>
        /// 프리셋 오토 스캔을 정지한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x9B
        /// Data1    = 0x00
        /// Data2    = 0x00
        ///
        /// Data2 = 0x01인 프리셋 데이터 초기화는
        /// 오조작 위험이 있으므로 UI에 제공하지 않는다.
        /// </summary>
        private void StopPresetScan()
        {
            ConsoleLogHelper.Command(
                "PRESET W",
                "Auto scan stop requested / CMD2=0x9B");

            bool hadRunningLoop =
                CancelPreset2DirectScan();

            bool stopResult =
                _controlCommandService
                    .StopPresetScan();

            /*
             * 상단 STOP AUTO SCAN은 반복 순회만 종료하고
             * 전용 Auto Scan Stop(0x9B)을 송신한다.
             * 진행 중인 단일 위치 이동은 STOP MOVE(0x4F)에서만 정지한다.
             */
            IsPresetScanRunning =
                false;

            Console.WriteLine();
            Console.WriteLine(
                "[PRESET 2 / WEB AGENT] LOOP STOP");

            Console.WriteLine(
                $"[PRESET 2 / WEB AGENT] SCAN_ACTIVE={hadRunningLoop} / " +
                "AUTO_SCAN_STOP_0x9B_SENT=True / " +
                $"STOP_RESULT={stopResult} / " +
                "POSITION_STOP_0x4F_SENT=False");

            ConsoleLogHelper.PrintLine();

            PresetCommandStatusText =
                "AUTO SCAN STOP REQUESTED";
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다.
        /// AUTO SCAN 반복 상태에는 영향을 주지 않으며,
        /// Pan/Tilt 위치 이동 정지 명령(0x4F)을 각각 송신한다.
        /// </summary>
        private void StopPresetMove()
        {
            CancellationTokenSource moveCts =
                Interlocked.Exchange(
                    ref _presetSingleMoveCts,
                    null);

            moveCts?.Cancel();

            bool stopResult =
                _controlCommandService
                    .StopPanTiltPositionMove();

            PresetCommandStatusText =
                "PRESET W MOVE STOPPED";

            ConsoleLogHelper.Command(
                "PRESET W",
                $"Single move stop / CMD2=0x4F / RESULT={stopResult}");
        }

        /// <summary>
        /// 잠금 Overlay의 공통 STOP 버튼에서 현재 실행 중인
        /// PRESET L 또는 PRESET W AUTO SCAN을 정지한다.
        /// </summary>
        private void StopActivePresetScan()
        {
            if (IsLaPresetScanRunning)
            {
                StopLaPresetScan();
            }

            if (IsPresetScanRunning)
            {
                StopPresetScan();
            }

        }

        /// <summary>
        /// 화면 확인용 프리셋 목록에 슬롯을 추가하거나 갱신한다.
        ///
        /// 슬롯 번호 오름차순으로 유지하여
        /// ComboBox에서 P01, P02, P03 순으로 표시한다.
        /// </summary>
        private void UpsertPresetPoint(
            PresetPointOption newPreset)
        {
            PresetPointOption existingPreset =
                PresetPoints
                    .FirstOrDefault(
                        preset =>
                            preset.Number ==
                            newPreset.Number);

            if (existingPreset != null)
            {
                PresetPoints.Remove(
                    existingPreset);
            }

            int insertIndex =
                0;

            while (insertIndex <
                       PresetPoints.Count &&
                   PresetPoints[insertIndex].Number <
                       newPreset.Number)
            {
                insertIndex++;
            }

            PresetPoints.Insert(
                insertIndex,
                newPreset);

            SelectedPresetPoint =
                newPreset;
        }

        /// <summary>
        /// Zoom Position 입력값 적용
        /// </summary>
        private async Task SetZoomPositionFromInputAsync()
        {
            if (!ZoomPositionValue.HasValue)
            {
                Console.WriteLine(
                    "[MOVE CONTROL] ZOOM POSITION FAILED : EMPTY VALUE");

                return;
            }

            int standardPosition =
                Clamp(
                    ZoomPositionValue.Value,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            await ApplyMoveControlZoomPositionAsync(
                standardPosition,
                "POSITION");
        }

        /// <summary>
        /// Zoom Ratio 입력값 적용
        ///
        /// 입력값은 EO 광학 배율 1.0 ~ 50.0배 기준이다.
        /// EO 배율을 공통 Position 0 ~ 1000으로 변환한 뒤
        /// EO와 IR에 동일한 진행률을 적용한다.
        ///
        /// EO 1.0배  -> Position 0    -> IR 1.0배
        /// EO 50.0배 -> Position 1000 -> IR 5.0배
        /// </summary>
        private async Task SetZoomRatioFromInputAsync()
        {
            if (!ZoomRatioValue.HasValue)
            {
                Console.WriteLine(
                    "[MOVE CONTROL] ZOOM RATIO FAILED : EMPTY VALUE");

                return;
            }

            double zoomRatio =
                Clamp(
                    ZoomRatioValue.Value,
                    MoveControlMinimumZoomRatio,
                    MoveControlEoMaximumZoomRatio);

            int standardPosition =
                ConvertEoZoomRatioToStandardPosition(
                    zoomRatio);

            await ApplyMoveControlZoomPositionAsync(
                standardPosition,
                $"RATIO {zoomRatio:F1}x");
        }

        /// <summary>
        /// 이동 제어 Zoom Position을 현재 장비 구성에 적용한다.
        ///
        /// 환경장비:
        /// EO / IR Control Agent Position 명령
        ///
        /// 옥상장비:
        /// EO CTEC Direct Position + IR Control Agent Position 명령
        /// </summary>
        private async Task ApplyMoveControlZoomPositionAsync(
            int standardPosition,
            string requestType)
        {
            int safePosition =
                Clamp(
                    standardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            await StopZoomSyncAsync();

            Console.WriteLine();
            Console.WriteLine(
                $"[MOVE CONTROL] ZOOM {requestType} " +
                $"/ STANDARD POSITION={safePosition} " +
                $"/ EO RATIO={ConvertStandardPositionToEoZoomRatio(safePosition):F1}x " +
                $"/ IR RATIO={ConvertStandardPositionToIrZoomRatio(safePosition):F1}x");

            ConsoleLogHelper.PrintLine();

            if (SelectedEquipmentStatusMode ==
                EquipmentStatusMode.Environment)
            {
                bool result =
                    _webAgentZoomControlService
                        .ApplySynchronizedZoom(
                            (short)safePosition);

                Console.WriteLine(
                    $"[MOVE CONTROL] ENVIRONMENT ZOOM RESULT : {result}");

                ConsoleLogHelper.PrintLine();

                return;
            }

            bool irResult =
                _webAgentZoomControlService
                    .SetIrZoomPosition(
                        (short)safePosition);

            RtspSourceOption ctecSource =
                _connectedEoCtecSource;

            if (ctecSource == null)
            {
                bool eoFallbackResult =
                    _controlCommandService
                        .EoZoomGoPosition(
                            (short)safePosition);

                Console.WriteLine(
                    "[MOVE CONTROL] ROOFTOP ZOOM FALLBACK " +
                    $"/ EO={eoFallbackResult} / IR={irResult}");

                ConsoleLogHelper.PrintLine();

                return;
            }

            int eoRawTarget =
                ConvertStandardZoomToCtecRaw(
                    safePosition);

            CancellationTokenSource zoomCts =
                new CancellationTokenSource();

            _rooftopZoomSyncCts =
                zoomCts;

            bool eoResult;

            try
            {
                eoResult =
                    await MoveRooftopEoZoomToRawPositionAsync(
                        ctecSource,
                        eoRawTarget,
                        zoomCts.Token);
            }
            finally
            {
                if (ReferenceEquals(
                        _rooftopZoomSyncCts,
                        zoomCts))
                {
                    _rooftopZoomSyncCts =
                        null;

                    zoomCts.Dispose();
                }

            }

            Console.WriteLine(
                "[MOVE CONTROL] ROOFTOP ZOOM RESULT " +
                $"/ EO={eoResult} / IR={irResult}");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// Focus Position 입력값 적용
        ///
        /// 표준 방향:
        /// 0    = Far
        /// 1000 = Near
        ///
        /// IR Raw 방향:
        /// 1000 = Far
        /// 0    = Near
        /// </summary>
        private async Task SetFocusPositionFromInputAsync()
        {
            if (!FocusPositionValue.HasValue)
            {
                Console.WriteLine(
                    "[MOVE CONTROL] FOCUS POSITION FAILED : EMPTY VALUE");

                return;
            }

            int standardPosition =
                Clamp(
                    FocusPositionValue.Value,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            await ApplyMoveControlFocusPositionAsync(
                standardPosition);
        }

        /// <summary>
        /// 이동 제어 Focus Position을 EO / IR에 동시에 적용한다.
        ///
        /// IR Focus Absolute 0x28 명령은 Pan / Tilt 오동작 이력이 있으므로
        /// 절대 사용하지 않고, 기존 검증된 Near / Far 연속 명령과
        /// Function 0x07 상태 피드백을 사용한다.
        /// </summary>
        private async Task ApplyMoveControlFocusPositionAsync(
            int standardPosition)
        {
            int safePosition =
                Clamp(
                    standardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            await StopFocusSyncAsync();

            CancellationTokenSource focusCts =
                new CancellationTokenSource();

            _rooftopFocusSyncCts =
                focusCts;

            bool eoResult =
                false;

            bool irResult =
                false;

            try
            {
                int irRawTargetPosition =
                    ConvertIrFocusStandardToStatusPosition(
                        safePosition);

                Task<bool> irMoveTask =
                    MoveIrFocusToPositionAsync(
                        irRawTargetPosition,
                        focusCts.Token);

                if (SelectedEquipmentStatusMode ==
                    EquipmentStatusMode.Environment)
                {
                    eoResult =
                        _controlCommandService
                            .EoFocusGoPosition(
                                (short)safePosition);

                    irResult =
                        await irMoveTask;
                }
                else
                {
                    RtspSourceOption ctecSource =
                        _connectedEoCtecSource;

                    if (ctecSource == null)
                    {
                        eoResult =
                            _controlCommandService
                                .EoFocusGoPosition(
                                    (short)safePosition);

                        irResult =
                            await irMoveTask;
                    }
                    else
                    {
                        int eoRawTarget =
                            ConvertStandardFocusToCtecRaw(
                                safePosition);

                        Task<bool> eoMoveTask =
                            MoveRooftopEoFocusToRawPositionAsync(
                                ctecSource,
                                eoRawTarget,
                                focusCts.Token);

                        bool[] moveResults =
                            await Task.WhenAll(
                                eoMoveTask,
                                irMoveTask);

                        eoResult =
                            moveResults[0];

                        irResult =
                            moveResults[1];
                    }

                }

            }
            finally
            {
                if (ReferenceEquals(
                        _rooftopFocusSyncCts,
                        focusCts))
                {
                    _rooftopFocusSyncCts =
                        null;

                    focusCts.Dispose();
                }

            }

            Console.WriteLine();
            Console.WriteLine(
                "[MOVE CONTROL] FOCUS POSITION RESULT " +
                $"/ STANDARD={safePosition} " +
                $"/ EO={eoResult} / IR={irResult}");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 기존 VIA 0 Pan 이동 작업 취소
        /// </summary>
        private void CancelMoveControlPanOperation()
        {
            CancellationTokenSource cts =
                Interlocked.Exchange(
                    ref _moveControlPanCts,
                    null);

            if (cts == null)
            {
                return;
            }

            cts.Cancel();
        }

        /// <summary>
        /// EO 광학 배율 1.0 ~ 50.0을
        /// 공통 표준 Position 0 ~ 1000으로 변환한다.
        ///
        /// 이 Position을 EO와 IR에 동일하게 적용하여
        /// 두 렌즈의 기계적 Zoom 진행률을 맞춘다.
        /// </summary>
        private static int ConvertEoZoomRatioToStandardPosition(
            double eoZoomRatio)
        {
            double safeRatio =
                Clamp(
                    eoZoomRatio,
                    MoveControlMinimumZoomRatio,
                    MoveControlEoMaximumZoomRatio);

            double normalized =
                (safeRatio -
                 MoveControlMinimumZoomRatio) /
                (MoveControlEoMaximumZoomRatio -
                 MoveControlMinimumZoomRatio);

            return (int)Math.Round(
                normalized *
                MoveControlPositionMaximum,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 표준 Position 0 ~ 1000을
        /// EO 광학 배율 1.0 ~ 50.0으로 변환한다.
        /// </summary>
        private static double ConvertStandardPositionToEoZoomRatio(
            int standardPosition)
        {
            int safePosition =
                Clamp(
                    standardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            double normalized =
                safePosition /
                (double)MoveControlPositionMaximum;

            return MoveControlMinimumZoomRatio +
                   normalized *
                   (MoveControlEoMaximumZoomRatio -
                    MoveControlMinimumZoomRatio);
        }

        /// <summary>
        /// 표준 Position 0 ~ 1000을
        /// IR 광학 배율 1.0 ~ 5.0으로 변환한다.
        ///
        /// 실제 장비 명령은 기존과 동일하게 Position 기준으로 보내며,
        /// 이 값은 UI 및 로그에 표시하는 예상 광학 배율이다.
        /// </summary>
        private static double ConvertStandardPositionToIrZoomRatio(
            int standardPosition)
        {
            int safePosition =
                Clamp(
                    standardPosition,
                    MoveControlPositionMinimum,
                    MoveControlPositionMaximum);

            double normalized =
                safePosition /
                (double)MoveControlPositionMaximum;

            return MoveControlMinimumZoomRatio +
                   normalized *
                   (MoveControlIrMaximumZoomRatio -
                    MoveControlMinimumZoomRatio);
        }

        /// <summary>
        /// 각도 입력값을 프로토콜 소수점 둘째 자리 기준으로 반올림한다.
        /// </summary>
        private static double RoundAngleToProtocolScale(
            double angle)
        {
            return Math.Round(
                angle,
                2,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Nullable 각도 입력값 반올림
        /// </summary>
        private static double? RoundNullableAngle(
            double? angle)
        {
            return angle.HasValue
                ? RoundAngleToProtocolScale(
                    angle.Value)
                : (double?)null;
        }

        /// <summary>
        /// double 범위 제한
        /// </summary>
        private static double Clamp(
            double value,
            double min,
            double max)
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
        private static int Clamp(
            int value,
            int min,
            int max)
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
        #endregion
    }

}
