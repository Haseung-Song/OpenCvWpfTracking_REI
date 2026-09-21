using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Control;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// EO/IR Zoom 및 Focus 동기화 작업을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Equipment Status / Zoom Synchronization Methods]

        /// <summary>
        /// SelectPreviousZoomSyncLevel 동작 수행 함수.
        /// </summary>
        private void SelectPreviousZoomSyncLevel()
        {
            int currentIndex =
                ZoomSyncLevelOptions.IndexOf(
                    SelectedZoomSyncLevel);

            if (currentIndex > 0)
            {
                SelectedZoomSyncLevel =
                    ZoomSyncLevelOptions[currentIndex - 1];
            }

        }

        /// <summary>
        /// SelectNextZoomSyncLevel 동작 수행 함수.
        /// </summary>
        private void SelectNextZoomSyncLevel()
        {
            int currentIndex =
                ZoomSyncLevelOptions.IndexOf(
                    SelectedZoomSyncLevel);

            if (currentIndex >= 0 &&
                currentIndex < ZoomSyncLevelOptions.Count - 1)
            {
                SelectedZoomSyncLevel =
                    ZoomSyncLevelOptions[currentIndex + 1];
            }

        }

        /// <summary>
        /// 선택한 10단계 Zoom Position을 현재 장비 구성에 적용한다.
        ///
        /// 환경장비:
        /// - Web Agent 기준 EO / IR Position 0 ~ 1000을 동일하게 송신
        ///
        /// 옥상장비:
        /// - IR은 0 ~ 1000 Position 그대로 송신
        /// - EO는 표준 Position을 CTEC Raw 0 ~ 16384로 변환한 뒤
        ///   현재 위치 피드백을 보면서 Tele / Wide / Stop으로 이동
        /// </summary>
        private async Task ApplySelectedZoomSyncLevelAsync()
        {
            ConsoleLogHelper.Command(
                "ZOOM SYNC",
                $"Apply requested / EQUIPMENT={SelectedEquipmentStatusMode} / TARGET={SelectedZoomSyncLevel?.Position.ToString() ?? "NULL"}");

            ZoomSyncLevelOption selectedLevel =
                SelectedZoomSyncLevel;

            if (selectedLevel == null)
            {
                return;
            }

            if (!await _lensSyncOperationLock.WaitAsync(0))
            {
                ZoomSyncStatusText = "BUSY / OTHER LENS SYNC RUNNING";
                return;
            }

            await StopZoomSyncAsync();

            // 2026-09-18: 같은 0~1000 값을 두 카메라에 복사하지 않는다.
            // IR(25~225 mm, 640x512/17um)의 선택 단계에서 목표 HFOV를 구하고,
            // XV-Z2090HC(6~540 mm)가 같은 HFOV가 되는 별도 위치를 계산한다.
            ZoomFovTarget fovTarget = _fieldOfViewSyncService.CreateTarget(selectedLevel.Level);
            short eoStandardPosition = fovTarget.EoPosition;
            short irStandardPosition = fovTarget.IrPosition;
            int irRawTarget = ConvertIrZoomStandardToStatusPosition(irStandardPosition);
            CancellationTokenSource zoomSyncCts = new CancellationTokenSource();
            _rooftopZoomSyncCts = zoomSyncCts;
            ZoomSyncStatusText = $"APPLYING LEVEL {selectedLevel.Level}";
            _ptzPerformanceScenario = "ZOOM_SYNC";

            bool eoResult = false;
            bool irResult = false;

            try
            {
                long eoStartSequence = Interlocked.Read(ref _eoLensStatusVersion);

                Task<bool> irMoveTask = Interlocked.Read(ref _irLensStatusVersion) > 0
                    ? MoveIrZoomToPositionAsync(irRawTarget, zoomSyncCts.Token, irStandardPosition)
                    : Task.FromResult(false);

                if (SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
                {
                    bool eoCommanded = _webAgentZoomControlService.SetEoZoomPosition(eoStandardPosition);
                    Task<bool> eoMoveTask = eoCommanded
                        ? WaitForEnvironmentEoLensTargetAsync(
                            eoStandardPosition,
                            true,
                            eoStartSequence,
                            zoomSyncCts.Token)
                        : Task.FromResult(false);

                    bool[] results = await Task.WhenAll(eoMoveTask, irMoveTask);
                    eoResult = results[0];
                    irResult = results[1];
                }
                else
                {
                    RtspSourceOption ctecSource = _connectedEoCtecSource;
                    Task<bool> eoMoveTask = ctecSource == null
                        ? Task.FromResult(false)
                        : MoveRooftopEoZoomToRawPositionAsync(
                            ctecSource,
                            ConvertStandardZoomToCtecRaw(eoStandardPosition),
                            zoomSyncCts.Token);

                    bool[] results = await Task.WhenAll(eoMoveTask, irMoveTask);
                    eoResult = results[0];
                    irResult = results[1];
                }

                ConsoleLogHelper.State(
                    "ZOOM SYNC",
                    $"MODEL=MEASURED_10_LEVEL_260921 / LEVEL={selectedLevel.Level} / " +
                    $"EO_COMMANDED={eoStandardPosition} / IR_COMMANDED={irStandardPosition} / " +
                    $"IR_COMMAND_RAW_TARGET={irRawTarget} / EO_ACTUAL={_currentEoZoom} / " +
                    $"IR_SETTLED_RAW={_currentIrZoom} / IR_STANDARD_FINAL={NormalizeIrZoomStatusPosition(_currentIrZoom)} / " +
                    $"PHYSICAL_MIN={_environmentIrZoomPhysicalWideRaw} / PHYSICAL_MAX={_environmentIrZoomPhysicalTeleRaw} / " +
                    $"EO_POSITION_ERROR={Math.Abs(_currentEoZoom - eoStandardPosition)} / " +
                    $"IR_POSITION_ERROR={Math.Abs(NormalizeIrZoomStatusPosition(_currentIrZoom) - irStandardPosition)} / " +
                    $"EO_DONE={eoResult} / IR_DONE={irResult} / FINAL_RESULT={(eoResult && irResult ? "COMPLETED" : "INCOMPLETE")}");

                // 2026-09-18: 장비 기구 오차로 목표 허용범위를 조금 벗어난 경우를
                // 고장처럼 표시하지 않는다. INCOMPLETE는 IR 명령/피드백이 있어도
                // 실제 Zoom 위치가 전혀 움직이지 않은 경우에만 표시한다.
                ZoomSyncStatusText = !irResult
                    ? "INCOMPLETE / IR ZOOM NO MOVEMENT"
                    : $"COMPLETED / LEVEL {selectedLevel.Level}";
            }
            catch (OperationCanceledException)
            {
                ZoomSyncStatusText = "STOPPED";
            }
            catch (Exception ex)
            {
                ZoomSyncStatusText = "ERROR / " + ex.Message;
                ConsoleLogHelper.Error("ZOOM SYNC", ex.ToString());
            }
            finally
            {
                if (ReferenceEquals(_rooftopZoomSyncCts, zoomSyncCts))
                {
                    _rooftopZoomSyncCts = null;
                }

                zoomSyncCts.Dispose();

                _ptzPerformanceScenario = "IDLE";

                _lensSyncOperationLock.Release();
            }
        }

        /// <summary>
        /// 옥상장비 CTEC EO Zoom을 목표 Raw Position으로 직접 이동한다.
        ///
        /// 기존 TELE / WIDE 연속 이동 기반 Sync는 다음 문제가 있었다.
        ///
        /// 1. TCP Position 응답이 계단식으로 늦게 반영됨
        /// 2. 프로그램이 목표 통과를 늦게 확인함
        /// 3. 다음 단계에서 반대 방향 보정이 발생함
        /// 4. 화면이 확대 → 축소 → 확대 형태로 왕복함
        ///
        /// 현재 방식은 VISCA Zoom Direct 명령으로 목표 Raw Position을
        /// 한 번만 송신하고, 이후 Inquiry는 도착 확인 용도로만 사용한다.
        /// 따라서 Sync 이동 중 TELE / WIDE 방향 전환 및 재보정은 수행하지 않는다.
        /// </summary>
        private async Task<bool> MoveRooftopEoZoomToRawPositionAsync(
            RtspSourceOption ctecSource,
            int targetRawPosition,
            CancellationToken cancellationToken)
        {
            if (ctecSource == null)
            {
                return false;
            }

            int safeTargetRawPosition =
                Math.Max(
                    0,
                    Math.Min(
                        CtecEoZoomPositionMax,
                        targetRawPosition));

            try
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[ROOFTOP ZOOM SYNC] DIRECT START");

                Console.WriteLine(
                    $"[ROOFTOP ZOOM SYNC] TARGET : {safeTargetRawPosition}");

                ConsoleLogHelper.PrintLine();

                bool commandResult =
                    await _ctecCameraCommandService
                        .MoveZoomPositionAsync(
                            ctecSource.ControlIp,
                            ctecSource.ControlUserName,
                            ctecSource.ControlPassword,
                            ctecSource.UseHttps,
                            (ushort)safeTargetRawPosition);

                if (!commandResult)
                {
                    Console.WriteLine(
                        "[ROOFTOP ZOOM SYNC] DIRECT COMMAND FAILED");

                    ConsoleLogHelper.PrintLine();

                    return false;
                }

                Stopwatch timeout =
                    Stopwatch.StartNew();

                while (timeout.ElapsedMilliseconds <
                       RooftopZoomSyncTimeoutMs)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    int? currentPosition =
                        await RequestAndWaitCtecEoPositionAsync(
                            ContinuousMoveType.EoZoom,
                            ctecSource,
                            cancellationToken);

                    if (currentPosition.HasValue)
                    {
                        int error =
                            Math.Abs(
                                currentPosition.Value -
                                safeTargetRawPosition);

                        Console.WriteLine(
                            "[ROOFTOP ZOOM SYNC] DIRECT CHECK " +
                            $"/ POSITION={currentPosition.Value} " +
                            $"/ TARGET={safeTargetRawPosition} " +
                            $"/ ERROR={error}");

                        if (error <=
                            RooftopZoomSyncTolerance)
                        {
                            Console.WriteLine(
                                "[ROOFTOP ZOOM SYNC] DIRECT COMPLETED");

                            ConsoleLogHelper.PrintLine();

                            return true;
                        }

                    }

                    await Task.Delay(
                        RooftopZoomSyncInquiryIntervalMs,
                        cancellationToken);
                }

                Console.WriteLine(
                    "[ROOFTOP ZOOM SYNC] DIRECT TIMEOUT");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "[ROOFTOP ZOOM SYNC] DIRECT CANCELED");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[ROOFTOP ZOOM SYNC] DIRECT ERROR : " +
                    ex.Message);

                ConsoleLogHelper.PrintLine();

                return false;
            }

        }

        /// <summary>
        /// StopZoomSyncAsync 중지 함수.
        /// </summary>
        private async Task StopZoomSyncAsync()
        {
            CancellationTokenSource cts =
                _rooftopZoomSyncCts;

            _rooftopZoomSyncCts =
                null;

            if (cts != null)
            {
                cts.Cancel();
            }

            // 2026-09-17: IR 렌즈 전용 Stop만 사용한다.
            // PTZF 전체정지(FF 01 00 00 00 00 01)는 Pan/Tilt까지 중지시키므로
            // Zoom Sync 중지 경로에서는 절대 송신하지 않는다.
            _controlCommandService.StopIrZoom();

            RtspSourceOption ctecSource =
                _connectedEoCtecSource;

            if (ctecSource != null)
            {
                await _ctecCameraCommandService
                    .StopZoomAsync(
                        ctecSource.ControlIp,
                        ctecSource.ControlUserName,
                        ctecSource.ControlPassword,
                        ctecSource.UseHttps);
            }

            ZoomSyncStatusText =
                "STOPPED";
        }

        private int ConvertIrZoomStandardToStatusPosition(int standardPosition)
        {
            int safe = Math.Max(0, Math.Min(1000, standardPosition));
            if (SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop)
            {
                return 1000 - safe;
            }

            int span = Math.Max(1, _environmentIrZoomPhysicalTeleRaw - _environmentIrZoomPhysicalWideRaw);
            return _environmentIrZoomPhysicalWideRaw +
                (int)Math.Round(safe * span / 1000.0);
        }

        private int NormalizeIrZoomStatusPosition(int rawPosition)
        {
            int safeRaw = Math.Max(0, Math.Min(1000, rawPosition));
            if (SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop)
            {
                return 1000 - safeRaw;
            }

            int span = Math.Max(1, _environmentIrZoomPhysicalTeleRaw - _environmentIrZoomPhysicalWideRaw);
            return Math.Max(0, Math.Min(1000,
                (int)Math.Round((safeRaw - _environmentIrZoomPhysicalWideRaw) * 1000.0 / span)));
        }

        private void UpdateEnvironmentIrZoomEndpointCalibration(int standardTarget, int settledRaw)
        {
            if (SelectedEquipmentStatusMode != EquipmentStatusMode.Environment) return;
            if (standardTarget <= 0 && settledRaw >= 0 && settledRaw <= EnvironmentIrZoomEndpointLearningLimit)
            {
                _environmentIrZoomPhysicalWideRaw = settledRaw;
            }
            else if (standardTarget >= 1000 && settledRaw >= 930 && settledRaw <= 1000)
            {
                _environmentIrZoomPhysicalTeleRaw = settledRaw;
            }
            else
            {
                return;
            }

            ConsoleLogHelper.State("IR ZOOM CALIBRATION",
                $"STANDARD_TARGET={standardTarget} / SETTLED_RAW={settledRaw} / " +
                $"PHYSICAL_WIDE={_environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_environmentIrZoomPhysicalTeleRaw} / " +
                $"STANDARD_FINAL={NormalizeIrZoomStatusPosition(settledRaw)}");
        }

        /// <summary>
        /// 2026-09-17: 실장비에서 동작하지 않은 0x29 Absolute 대신 기존 수동
        /// 0x31 Tele/Wide/Stop 경로와 TARGET=0x01 실제 피드백으로 폐루프 제어한다.
        /// </summary>
        private async Task<bool> MoveIrZoomToPositionAsync(
            int targetPosition,
            CancellationToken cancellationToken,
            int standardTarget = -1)
        {
            int target = Math.Max(0, Math.Min(1000, targetPosition));
            Stopwatch total = Stopwatch.StartNew();
            int operationStart = _currentIrZoom;
            int retryCount = 0;
            int settled = operationStart;
            bool operationMovementObserved = false;
            int previousDirection = 0;
            int directionReversalCount = 0;
            string lastDirection = "NONE";

            if (IsIrZoomTargetReached(target, operationStart, standardTarget))
            {
                return true;
            }

            // 2026-09-21: 각 Pulse가 완전히 정착한 뒤 새 오차를 계산한다.
            // 장비 관성으로 목표를 통과해도 사용자가 다시 APPLY할 필요가 없도록
            // 같은 작업 안에서 최대 2회의 제한된 방향 반전을 허용한다.
            for (int attempt = 1; attempt <= IrZoomSyncMaxMoveAttempts; attempt++)
            {
                int start = _currentIrZoom;
                if (IsIrZoomTargetReached(target, start, standardTarget)) return true;

                int currentDirection = target > start ? 1 : -1;
                if (previousDirection != 0 && currentDirection != previousDirection)
                {
                    directionReversalCount++;
                    if (directionReversalCount > IrZoomMaximumDirectionReversals)
                    {
                        ConsoleLogHelper.Warning(
                            "IR ZOOM SYNC",
                            $"RESULT=REVERSAL_LIMIT / TARGET={target} / CURRENT_POSITION={start} / " +
                            $"REVERSAL_COUNT={directionReversalCount}");
                        settled = start;
                        break;
                    }
                }

                previousDirection = currentDirection;
                bool zoomIn = currentDirection > 0;
                string direction = zoomIn ? "ZoomIn" : "ZoomOut";
                lastDirection = direction;

                long startSequence = Interlocked.Read(ref _irLensStatusVersion);
                bool started = zoomIn
                    ? _controlCommandService.StartIrZoomTele()
                    : _controlCommandService.StartIrZoomWide();
                if (!started) return false;

                int previous = start;
                bool movementObserved = false;
                bool stopped = false;
                int unchangedFeedbackCount = 0;
                long lastFeedbackMs = total.ElapsedMilliseconds;
                int stopLead = IrZoomMinimumStopLead;

                // 2026-09-18: LEVEL 5~10 시험에서 목표와 50~170 step만 남은 상태로
                // 연속 구동을 시작하면 첫 500ms 피드백 전에 목표를 지나 100~300 step
                // 과주행했다. 짧은 거리는 상태 수신을 기다리는 연속 구동 대신 동일
                // 방향의 짧은 Pulse만 허용하고 정착값을 다시 판정한다.
                int startRemaining = zoomIn ? target - start : start - target;
                if (startRemaining <= IrZoomShortPulseThreshold)
                {
                    int physicalSpan = SelectedEquipmentStatusMode == EquipmentStatusMode.Environment
                        ? Math.Max(1, _environmentIrZoomPhysicalTeleRaw - _environmentIrZoomPhysicalWideRaw)
                        : 1000;
                    int pulseMilliseconds = Math.Max(
                        IrZoomShortPulseMinimumMs,
                        Math.Min(
                            IrZoomShortPulseMaximumMs,
                            (int)Math.Round(
                                startRemaining *
                                EnvironmentIrZoomFullTravelMs /
                                (double)physicalSpan *
                                IrZoomShortPulseScale)));
                    try
                    {
                        await Task.Delay(pulseMilliseconds, cancellationToken);
                    }
                    finally
                    {
                        stopped = _controlCommandService.StopIrZoom();
                    }

                    int pulseStopPosition = _currentIrZoom;
                    settled = await WaitForIrLensSettledPositionAsync(true, cancellationToken);
                    bool pulseMovementObserved = Math.Abs(settled - start) > 1;
                    operationMovementObserved |= pulseMovementObserved;
                    if (standardTarget >= 0) UpdateEnvironmentIrZoomEndpointCalibration(standardTarget, settled);
                    bool pulseReached = IsIrZoomTargetReached(target, settled, standardTarget);
                    ConsoleLogHelper.State(
                        "IR ZOOM SYNC",
                        $"STANDARD_TARGET={standardTarget} / COMMAND_RAW_TARGET={target} / START_POSITION={operationStart} / CURRENT_POSITION={start} / " +
                        $"DIRECTION={direction} / CONTROL_MODE=SHORT_PULSE / PULSE_MS={pulseMilliseconds} / " +
                        $"STOP_POSITION={pulseStopPosition} / SETTLED_RAW={settled} / " +
                        $"STANDARD_FINAL={NormalizeIrZoomStatusPosition(settled)} / " +
                        $"PHYSICAL_WIDE={_environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_environmentIrZoomPhysicalTeleRaw} / " +
                        $"NORMALIZED_ERROR={(standardTarget >= 0 ? Math.Abs(NormalizeIrZoomStatusPosition(settled) - standardTarget) : Math.Abs(settled - target))} / " +
                        $"MOVEMENT_OBSERVED={pulseMovementObserved} / REVERSAL_COUNT={directionReversalCount} / RETRY_COUNT={retryCount} / " +
                        $"RESULT={(pulseReached ? "COMPLETED" : "RETRY")}");

                    if (pulseReached) return true;
                    retryCount++;
                    await Task.Delay(250, cancellationToken);
                    continue;
                }

                try
                {
                    while (total.ElapsedMilliseconds < IrZoomSyncTimeoutMs)
                    {
                        bool feedback = await WaitForIrLensFeedbackAsync(startSequence, cancellationToken);
                        if (!feedback)
                        {
                            ConsoleLogHelper.Warning(
                                "IR ZOOM SYNC",
                                $"RESULT=FEEDBACK_TIMEOUT / TARGET={target} / START_POSITION={operationStart} / " +
                                $"CURRENT_POSITION={_currentIrZoom} / DIRECTION={direction} / RETRY_COUNT={retryCount}");
                            break;
                        }

                        long sequence = Interlocked.Read(ref _irLensStatusVersion);
                        int current = _currentIrZoom;
                        int delta = current - previous;
                        long nowMs = total.ElapsedMilliseconds;
                        long feedbackIntervalMs = Math.Max(1, nowMs - lastFeedbackMs);
                        lastFeedbackMs = nowMs;
                        startSequence = sequence;

                        if (delta != 0)
                        {
                            movementObserved = true;
                            operationMovementObserved = true;
                            unchangedFeedbackCount = 0;
                        }
                        else unchangedFeedbackCount++;

                        // 2026-09-18: 실장비 로그에서 STOP_POSITION=206 이후
                        // SETTLED_POSITION=397까지 약 190 step 이동했다. 직전 delta만
                        // 사용하던 120 cap은 LEVEL 2를 크게 통과하므로, 관측된 정지
                        // 지연과 상태 step을 함께 사용해 더 일찍 STOP한다.
                        int observedStopLag =
                            (int)Math.Round(_irZoomObservedStopLag);
                        stopLead = movementObserved
                            ? Math.Max(
                                IrZoomMinimumStopLead,
                                Math.Min(
                                    IrZoomMaximumStopLead,
                                    Math.Max(
                                        observedStopLag + 25,
                                        Math.Abs(delta) * 2 + 30)))
                            : IrZoomMinimumStopLead;
                        int remaining = zoomIn ? target - current : current - target;

                        ConsoleLogHelper.State(
                            "IR ZOOM SYNC",
                            $"TARGET={target} / START_POSITION={operationStart} / CURRENT_POSITION={current} / " +
                            $"DIRECTION={direction} / STATUS_STEP={delta} / FEEDBACK_INTERVAL_MS={feedbackIntervalMs} / " +
                            $"PREDICTED_STOP_LEAD={stopLead} / RETRY_COUNT={retryCount}");

                        if (movementObserved && remaining <= stopLead)
                        {
                            stopped = _controlCommandService.StopIrZoom();
                            break;
                        }

                        if (unchangedFeedbackCount >= 4)
                        {
                            ConsoleLogHelper.Warning(
                                "IR ZOOM SYNC",
                                $"RESULT=NO_POSITION_CHANGE / TARGET={target} / CURRENT_POSITION={current} / " +
                                $"DIRECTION={direction} / RETRY_COUNT={retryCount}");
                            break;
                        }

                        previous = current;
                    }
                }
                finally
                {
                    if (!stopped)
                    {
                        _controlCommandService.StopIrZoom();
                    }
                }

                int stopPosition = _currentIrZoom;
                settled = await WaitForIrLensSettledPositionAsync(true, cancellationToken);
                int stopTravel = zoomIn
                    ? settled - stopPosition
                    : stopPosition - settled;
                if (stopTravel >= 0 && stopTravel <= 500)
                {
                    _irZoomObservedStopLag =
                        _irZoomObservedStopLag * 0.65 +
                        stopTravel * 0.35;
                }
                if (standardTarget >= 0) UpdateEnvironmentIrZoomEndpointCalibration(standardTarget, settled);
                bool reached = IsIrZoomTargetReached(target, settled, standardTarget);
                int finalError = Math.Abs(settled - target);

                ConsoleLogHelper.State(
                    "IR ZOOM SYNC",
                    $"STANDARD_TARGET={standardTarget} / COMMAND_RAW_TARGET={target} / START_POSITION={operationStart} / DIRECTION={direction} / " +
                    $"PREDICTED_STOP_LEAD={stopLead} / STOP_POSITION={stopPosition} / " +
                    $"SETTLED_RAW={settled} / STANDARD_FINAL={NormalizeIrZoomStatusPosition(settled)} / STOP_TRAVEL={stopTravel} / " +
                    $"PHYSICAL_WIDE={_environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_environmentIrZoomPhysicalTeleRaw} / " +
                    $"OBSERVED_STOP_LAG={_irZoomObservedStopLag:F1} / RAW_ERROR={finalError} / " +
                    $"NORMALIZED_ERROR={(standardTarget >= 0 ? Math.Abs(NormalizeIrZoomStatusPosition(settled) - standardTarget) : finalError)} / RETRY_COUNT={retryCount} / " +
                    $"RESULT={(reached ? "COMPLETED" : "RETRY")}");

                if (reached) return true;

                if (!movementObserved)
                {
                    ConsoleLogHelper.Warning(
                        "IR ZOOM SYNC",
                        $"FEEDBACK RECEIVED BUT POSITION UNCHANGED / ATTEMPT={attempt} / POSITION={settled}");
                }

                retryCount++;
                await Task.Delay(250, cancellationToken);
            }

            bool positionMatched = IsIrZoomTargetReached(target, settled, standardTarget);
            bool finalResult = positionMatched || operationMovementObserved;
            ConsoleLogHelper.State(
                "IR ZOOM SYNC",
                $"STANDARD_TARGET={standardTarget} / COMMAND_RAW_TARGET={target} / START_POSITION={operationStart} / SETTLED_RAW={settled} / " +
                $"STANDARD_FINAL={NormalizeIrZoomStatusPosition(settled)} / " +
                $"PHYSICAL_WIDE={_environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_environmentIrZoomPhysicalTeleRaw} / " +
                $"NORMALIZED_ERROR={(standardTarget >= 0 ? Math.Abs(NormalizeIrZoomStatusPosition(settled) - standardTarget) : Math.Abs(settled - target))} / DIRECTION={lastDirection} / " +
                $"REVERSAL_COUNT={directionReversalCount} / RETRY_COUNT={retryCount} / POSITION_MATCH={positionMatched} / MOVEMENT_OBSERVED={operationMovementObserved} / " +
                $"RESULT={(finalResult ? "OPERATIONAL" : "NO_MOVEMENT")}");
            return finalResult;
        }

        /// <summary>
        /// 2026-09-18: 화면/명령의 표준 좌표가 제공되면 장비 Raw 물리 끝점을
        /// 공통 정규화한 값으로 판정한다. Raw 좌표 전용 호출만 기존 허용 오차를 유지한다.
        /// </summary>
        private bool IsIrZoomTargetReached(int target, int actual, int standardTarget = -1)
        {
            if (standardTarget >= 0)
            {
                return Math.Abs(NormalizeIrZoomStatusPosition(actual) - standardTarget) <= IrZoomTargetTolerance;
            }
            return Math.Abs(actual - target) <= IrZoomTargetTolerance;
        }

        private async Task<bool> WaitForIrLensFeedbackAsync(
            long afterSequence,
            CancellationToken cancellationToken)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < LensSyncFeedbackTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Interlocked.Read(ref _irLensStatusVersion) > afterSequence)
                {
                    return true;
                }
                await Task.Delay(20, cancellationToken);
            }
            return false;
        }

        private async Task<int> WaitForIrLensSettledPositionAsync(
            bool zoom,
            CancellationToken cancellationToken)
        {
            long sequence = Interlocked.Read(ref _irLensStatusVersion);
            int previous = zoom ? _currentIrZoom : _currentIrFocus;
            int stable = 0;
            Stopwatch timeout = Stopwatch.StartNew();
            int settleTimeoutMs = zoom
                ? IrZoomSettleTimeoutMs
                : IrFocusSyncSettleTimeoutMs;
            int requiredStableSamples = zoom
                ? IrZoomStableSampleCount
                : IrFocusSyncStableSampleCount;
            while (timeout.ElapsedMilliseconds < settleTimeoutMs)
            {
                if (!await WaitForIrLensFeedbackAsync(sequence, cancellationToken))
                {
                    break;
                }
                sequence = Interlocked.Read(ref _irLensStatusVersion);
                int current = zoom ? _currentIrZoom : _currentIrFocus;
                stable = Math.Abs(current - previous) <= 1 ? stable + 1 : 0;
                previous = current;
                if (stable >= requiredStableSamples) break;
            }
            return zoom ? _currentIrZoom : _currentIrFocus;
        }

        private async Task<bool> WaitForEnvironmentEoLensTargetAsync(
            int target,
            bool zoom,
            long afterSequence,
            CancellationToken cancellationToken)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            int stable = 0;
            while (timeout.ElapsedMilliseconds < RooftopZoomSyncTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long currentSequence = Interlocked.Read(ref _eoLensStatusVersion);
                if (currentSequence > afterSequence)
                {
                    afterSequence = currentSequence;
                    int actual = zoom ? _currentEoZoom : _currentEoFocus;
                    stable = Math.Abs(actual - target) <= LensSyncTargetTolerance
                        ? stable + 1
                        : 0;
                    if (stable >= 2) return true;
                }
                await Task.Delay(25, cancellationToken);
            }
            return false;
        }

        /// <summary>
        /// ConvertStandardZoomToCtecRaw 생성 및 변환 함수.
        /// </summary>
        private static int ConvertStandardZoomToCtecRaw(
            int standardPosition)
        {
            int safePosition =
                Math.Max(
                    0,
                    Math.Min(
                        1000,
                        standardPosition));

            return (int)Math.Round(
                safePosition *
                CtecEoZoomPositionMax /
                1000.0);
        }

        /// <summary>
        /// Focus Sync 이전 단계 선택
        /// </summary>
        private void SelectPreviousFocusSyncLevel()
        {
            int currentIndex =
                FocusSyncLevelOptions.IndexOf(
                    SelectedFocusSyncLevel);

            if (currentIndex > 0)
            {
                SelectedFocusSyncLevel =
                    FocusSyncLevelOptions[
                        currentIndex -
                        1];
            }

        }

        /// <summary>
        /// Focus Sync 다음 단계 선택
        /// </summary>
        private void SelectNextFocusSyncLevel()
        {
            int currentIndex =
                FocusSyncLevelOptions.IndexOf(
                    SelectedFocusSyncLevel);

            if (currentIndex >= 0 &&
                currentIndex <
                    FocusSyncLevelOptions.Count -
                    1)
            {
                SelectedFocusSyncLevel =
                    FocusSyncLevelOptions[
                        currentIndex +
                        1];
            }

        }

        /// <summary>
        /// 선택한 10단계 Focus Position을 현재 장비 구성에 적용한다.
        ///
        /// 표준 Focus 범위:
        /// 0    = Far
        /// 1000 = Near
        ///
        /// 환경장비:
        /// - EO Focus 0 ~ 1000 Absolute 명령
        /// - IR Focus 0 ~ 1000 Absolute 명령
        ///
        /// 옥상장비:
        /// - IR Focus는 0 ~ 1000 Absolute 명령
        /// - EO Focus는 표준값을 CTEC Raw 0 ~ 32768로 변환하여
        ///   VISCA Focus Direct Position 명령으로 한 번에 이동한다.
        /// </summary>
        private async Task ApplySelectedFocusSyncLevelAsync()
        {
            ConsoleLogHelper.Command(
                "FOCUS SYNC",
                $"Apply requested / EQUIPMENT={SelectedEquipmentStatusMode} / TARGET={SelectedFocusSyncLevel?.Position.ToString() ?? "NULL"}");

            ZoomSyncLevelOption selectedLevel =
                SelectedFocusSyncLevel;

            if (selectedLevel == null)
            {
                return;
            }

            if (!await _lensSyncOperationLock.WaitAsync(0))
            {
                FocusSyncStatusText = "BUSY / OTHER LENS SYNC RUNNING";
                return;
            }

            /// <summary>
            /// 이전 Focus Sync 확인 작업만 취소한다.
            ///
            /// 기존 구현처럼 APPLY 시작 시 StopFocusSyncAsync()를 호출하면
            /// 매 단계마다 IR Focus Stop 및 CTEC Focus Stop 명령이 먼저 송신된다.
            /// 새 명령 시작 전 불필요한 장비 명령을 보내지 않도록
            /// 기존 Token만 취소 / 정리한다.
            /// </summary>
            CancellationTokenSource previousCts =
                _rooftopFocusSyncCts;

            _rooftopFocusSyncCts =
                null;

            if (previousCts != null)
            {
                previousCts.Cancel();
                previousCts.Dispose();
            }

            short standardPosition =
                selectedLevel.Position;

            FocusSyncStatusText =
                $"APPLYING LEVEL {selectedLevel.Level}";
            _ptzPerformanceScenario = "FOCUS_SYNC";

            // APPLYING 문구를 먼저 화면에 반영한 뒤 Zoom Sync와 동일하게
            // 장비별 명령 결과와 영상 연결 상태를 합쳐 최종 결과를 표시한다.
            await Dispatcher.Yield(
                DispatcherPriority.Background);

            bool isIrConnected =
                _irDecoder.IsOpened ||
                _isIrFrameDisplayed;

            CancellationTokenSource focusSyncCts =
                new CancellationTokenSource();

            _rooftopFocusSyncCts =
                focusSyncCts;

            bool eoResult =
                false;

            bool irResult =
                false;

            long eoStartSequence =
                Interlocked.Read(ref _eoLensStatusVersion);

            bool wasCanceled = false;

            try
            {
                /// <summary>
                /// IR Focus 목표값을 현재 장비의 상태 좌표로 변환한다.
                /// LA 상태만 표준 방향과 반대이며 Web Agent 상태는 동일하다.
                /// </summary>
                int irRawTargetPosition =
                    ConvertIrFocusStandardToStatusPosition(
                        standardPosition);

                if (!isIrConnected)
                {
                    ConsoleLogHelper.Command(
                        "FOCUS SYNC",
                        "IR move skipped - camera disconnected");
                }

                if (SelectedEquipmentStatusMode ==
                    EquipmentStatusMode.Environment)
                {
                    /// <summary>
                    /// 환경장비 EO Focus는 기존 Web Agent Absolute 명령을 사용한다.
                    /// </summary>
                    eoResult =
                        _controlCommandService
                            .EoFocusGoPosition(
                                standardPosition);

                    if (!isIrConnected)
                    {
                        irResult = false;
                    }
                    else
                    {
                        irResult = Interlocked.Read(ref _irLensStatusVersion) > 0 &&
                            await MoveIrFocusToPositionAsync(
                                irRawTargetPosition,
                                focusSyncCts.Token);
                    }

                }
                else
                {
                    Task<bool> irMoveTask =
                        isIrConnected
                            ? MoveIrFocusToPositionAsync(
                                irRawTargetPosition,
                                focusSyncCts.Token)
                            : Task.FromResult(false);

                    RtspSourceOption ctecSource =
                        _connectedEoCtecSource;

                    if (ctecSource == null)
                    {
                        irResult =
                            await irMoveTask;
                    }
                    else
                    {
                        int eoRawTarget =
                            ConvertStandardFocusToCtecRaw(
                                standardPosition);

                        Task<bool> eoMoveTask =
                            MoveRooftopEoFocusToRawPositionAsync(
                                ctecSource,
                                eoRawTarget,
                                focusSyncCts.Token);

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

                if (SelectedEquipmentStatusMode == EquipmentStatusMode.Environment && eoResult)
                {
                    eoResult = await WaitForEnvironmentEoLensTargetAsync(
                        standardPosition,
                        false,
                        eoStartSequence,
                        focusSyncCts.Token);
                }

            }
            catch (OperationCanceledException)
            {
                wasCanceled = true;
                FocusSyncStatusText = "STOPPED";
                ConsoleLogHelper.State("FOCUS SYNC", "Canceled safely by STOP command");
            }
            catch (Exception exception)
            {
                FocusSyncStatusText = "ERROR / " + exception.Message;
                ConsoleLogHelper.Error("FOCUS SYNC", exception.ToString());
            }
            finally
            {
                if (ReferenceEquals(
                        _rooftopFocusSyncCts,
                        focusSyncCts))
                {
                    _rooftopFocusSyncCts =
                        null;

                }

                // 실행 중인 await가 모두 종료된 뒤 실행 작업이 토큰을 해제한다.
                focusSyncCts.Dispose();

                _ptzPerformanceScenario = "IDLE";

                _lensSyncOperationLock.Release();

            }

            eoResult = eoResult &&
                (_eoDecoder.IsOpened || _isEoFrameDisplayed);

            irResult = irResult &&
                (_irDecoder.IsOpened || _isIrFrameDisplayed);

            if (!wasCanceled && !FocusSyncStatusText.StartsWith("ERROR", StringComparison.Ordinal))
            {
                FocusSyncStatusText = !irResult
                    ? "INCOMPLETE / IR FOCUS NO MOVEMENT"
                    : eoResult
                        ? $"COMPLETED / LEVEL {selectedLevel.Level}"
                        : $"COMPLETED / LEVEL {selectedLevel.Level} / EO VERIFY WARNING";
            }
        }

        /// <summary>
        /// Function 0x07 피드백이 없는 환경장비에서 FAR 끝점을 확보한 뒤
        /// 목표 비율만큼 NEAR로 이동하는 MOE 공통 보완 경로이다.
        /// </summary>
        private async Task<bool> MoveEnvironmentIrFocusWithoutFeedbackAsync(
            int targetPosition,
            CancellationToken cancellationToken)
        {
            int safeTarget = Math.Max(0, Math.Min(1000, targetPosition));

            ConsoleLogHelper.Command(
                "IR FOCUS SYNC",
                $"No Function 0x07 feedback; timed fallback / TARGET={safeTarget}");

            bool homeStarted = _controlCommandService.StartIrFocusFar();
            if (!homeStarted)
            {
                return false;
            }

            try
            {
                await Task.Delay(EnvironmentIrFocusHomeMs, cancellationToken);
            }
            finally
            {
                _controlCommandService.StopIrFocus();
            }

            ApplyEnvironmentIrCommandedPosition(
                null,
                0,
                "ENVIRONMENT FOCUS HOME ESTIMATE");

            if (safeTarget == 0)
            {
                return true;
            }

            await Task.Delay(120, cancellationToken);

            bool moveStarted = _controlCommandService.StartIrFocusNear();
            if (!moveStarted)
            {
                return false;
            }

            int moveDurationMs = Math.Max(
                80,
                (int)Math.Round(
                    EnvironmentIrFocusFullTravelMs * safeTarget / 1000.0));

            try
            {
                await Task.Delay(moveDurationMs, cancellationToken);
            }
            finally
            {
                _controlCommandService.StopIrFocus();
            }

            ApplyEnvironmentIrCommandedPosition(
                null,
                safeTarget,
                "ENVIRONMENT FOCUS TIMED ESTIMATE");

            return true;
        }

        /// <summary>
        /// IR Focus를 목표 Position으로 이동한다.
        ///
        /// 사용 명령:
        /// - Near Start : Command2 0x31 / Data1 0x03
        /// - Far Start  : Command2 0x31 / Data1 0x04
        /// - Focus Stop : Command2 0x31 / Data1 0x05
        ///
        /// 상태 확인:
        /// - Function 0x07의 IR Focus Position 0 ~ 1000
        ///
        /// 기존의 0x28 Absolute 명령은 실장비에서 Pan / Tilt가 움직였으므로
        /// 이 경로에서는 절대 사용하지 않는다.
        /// </summary>
        private async Task<bool> MoveIrFocusToPositionAsync(
            int targetPosition,
            CancellationToken cancellationToken)
        {
            int safeTargetPosition =
                Math.Max(
                    0,
                    Math.Min(
                        1000,
                        targetPosition));

            Stopwatch totalTimeout =
                Stopwatch.StartNew();

            int lastFinalPosition =
                _currentIrFocus;

            bool operationMovementObserved = false;

            for (int attempt = 1;
                 attempt <= IrFocusSyncMaxMoveAttempts;
                 attempt++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                int startPosition =
                    _currentIrFocus;

                int initialError =
                    Math.Abs(
                        safeTargetPosition -
                        startPosition);

                if (initialError <=
                    IrFocusSyncTolerance)
                {
                    Console.WriteLine(
                        "[IR FOCUS SYNC] COMPLETED " +
                        $"/ ATTEMPT={attempt - 1} " +
                        $"/ POSITION={startPosition} " +
                        $"/ TARGET={safeTargetPosition} " +
                        $"/ ERROR={initialError}");

                    ConsoleLogHelper.PrintLine();

                    return true;
                }

                // 2026-09-17 실장비 로그: FAR 명령은 Raw Focus를 증가시킨다.
                // 따라서 낮은 Raw 목표는 NEAR, 높은 Raw 목표는 FAR로 이동한다.
                bool moveNear =
                    safeTargetPosition < startPosition;

                long feedbackSequence =
                    Interlocked.Read(ref _irLensStatusVersion);

                bool commandResult =
                    moveNear
                        ? _controlCommandService
                            .StartIrFocusNear()
                        : _controlCommandService
                            .StartIrFocusFar();

                if (!commandResult)
                {
                    return false;
                }

                int stopLead =
                    attempt == 1
                        ? IrFocusSyncInitialStopLead
                        : IrFocusSyncCorrectionStopLead;

                int previousPosition =
                    startPosition;

                int largestObservedStep =
                    0;

                bool stopRequested =
                    false;

                bool movementObserved =
                    false;

                int unchangedFeedbackCount = 0;

                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[IR FOCUS SYNC] START " +
                    $"/ ATTEMPT={attempt} " +
                    $"/ DIRECTION={(moveNear ? "NEAR" : "FAR")} " +
                    $"/ CURRENT={startPosition} " +
                    $"/ TARGET={safeTargetPosition} " +
                    $"/ BASE_LEAD={stopLead}");

                ConsoleLogHelper.PrintLine();

                try
                {
                    while (totalTimeout.ElapsedMilliseconds <
                           IrFocusSyncTimeoutMs)
                    {
                        cancellationToken
                            .ThrowIfCancellationRequested();

                        if (!await WaitForIrLensFeedbackAsync(
                                feedbackSequence,
                                cancellationToken))
                        {
                            ConsoleLogHelper.Warning(
                                "IR FOCUS SYNC",
                                $"FEEDBACK TIMEOUT / ATTEMPT={attempt} / CURRENT={_currentIrFocus} / TARGET={safeTargetPosition}");
                            break;
                        }

                        feedbackSequence =
                            Interlocked.Read(ref _irLensStatusVersion);

                        int currentPosition =
                            _currentIrFocus;

                        int movementStep =
                            Math.Abs(
                                currentPosition -
                                previousPosition);

                        if (movementStep >
                            largestObservedStep)
                        {
                            largestObservedStep =
                                movementStep;
                        }

                        if (movementStep > 0)
                        {
                            movementObserved = true;
                            operationMovementObserved = true;
                            unchangedFeedbackCount = 0;
                        }
                        else
                        {
                            unchangedFeedbackCount++;
                        }

                        if (unchangedFeedbackCount >= 6)
                        {
                            ConsoleLogHelper.Warning(
                                "IR FOCUS SYNC",
                                $"Position stalled; retry / ATTEMPT={attempt} / POSITION={currentPosition} / TARGET={safeTargetPosition}");
                            break;
                        }

                        int dynamicStopLead =
                            Math.Max(
                                stopLead,
                                Math.Min(
                                    24,
                                    largestObservedStep +
                                    4));

                        int remainingDistance =
                            moveNear
                                ? currentPosition -
                                  safeTargetPosition
                                : safeTargetPosition -
                                  currentPosition;

                        bool reachedStopZone =
                            remainingDistance <=
                            dynamicStopLead;

                        bool passedTarget =
                            moveNear
                                ? currentPosition <=
                                  safeTargetPosition
                                : currentPosition >=
                                  safeTargetPosition;

                        // START 직후 이전 상태가 반복된 STEP=0은 정지 근거가 아니다.
                        if (movementObserved &&
                            (reachedStopZone ||
                            passedTarget)
                           )
                        {
                            stopRequested =
                                _controlCommandService
                                    .StopIrFocus();

                            Console.WriteLine(
                                "[IR FOCUS SYNC] EARLY STOP " +
                                $"/ ATTEMPT={attempt} " +
                                $"/ POSITION={currentPosition} " +
                                $"/ TARGET={safeTargetPosition} " +
                                $"/ REMAIN={remainingDistance} " +
                                $"/ STEP={largestObservedStep} " +
                                $"/ LEAD={dynamicStopLead} " +
                                $"/ STOP={stopRequested}");

                            ConsoleLogHelper.PrintLine();

                            break;
                        }

                        previousPosition =
                            currentPosition;

                        await Task.Delay(
                            IrFocusSyncPollingIntervalMs,
                            cancellationToken);
                    }

                }
                catch (OperationCanceledException)
                {
                    _controlCommandService
                        .StopIrFocus();

                    return false;
                }
                finally
                {
                    if (!stopRequested)
                    {
                        _controlCommandService
                            .StopIrFocus();
                    }

                }

                int settledPosition =
                    await WaitForIrLensSettledPositionAsync(
                        false,
                        cancellationToken);

                lastFinalPosition =
                    settledPosition;

                int finalError =
                    Math.Abs(
                        safeTargetPosition -
                        settledPosition);

                Console.WriteLine(
                    "[IR FOCUS SYNC] SETTLED " +
                    $"/ ATTEMPT={attempt} " +
                    $"/ POSITION={settledPosition} " +
                    $"/ TARGET={safeTargetPosition} " +
                    $"/ ERROR={finalError}");

                ConsoleLogHelper.PrintLine();

                if (finalError <=
                    IrFocusSyncTolerance)
                {
                    Console.WriteLine(
                        "[IR FOCUS SYNC] COMPLETED " +
                        $"/ ATTEMPT={attempt} " +
                        $"/ POSITION={settledPosition} " +
                        $"/ TARGET={safeTargetPosition} " +
                        $"/ ERROR={finalError}");

                    ConsoleLogHelper.PrintLine();

                    return true;
                }

                await Task.Delay(
                    80,
                    cancellationToken);
            }

            Console.WriteLine(
                "[IR FOCUS SYNC] INCOMPLETE " +
                $"/ POSITION={lastFinalPosition} " +
                $"/ TARGET={safeTargetPosition} " +
                $"/ ERROR={Math.Abs(safeTargetPosition - lastFinalPosition)}");

            ConsoleLogHelper.PrintLine();

            return operationMovementObserved;
        }

        /// <summary>
        /// IR Focus Stop 이후 실제 위치가 안정될 때까지 기다린다.
        /// </summary>
        private async Task<int> WaitForIrFocusSettledPositionAsync(
            CancellationToken cancellationToken)
        {
            Stopwatch settleTimeout =
                Stopwatch.StartNew();

            int previousPosition =
                _currentIrFocus;

            int stableCount =
                0;

            while (settleTimeout.ElapsedMilliseconds <
                   IrFocusSyncSettleTimeoutMs)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                await Task.Delay(
                    IrFocusSyncSettlePollingIntervalMs,
                    cancellationToken);

                int currentPosition =
                    _currentIrFocus;

                if (Math.Abs(
                        currentPosition -
                        previousPosition) <= 1)
                {
                    stableCount++;
                }
                else
                {
                    stableCount =
                        0;
                }

                previousPosition =
                    currentPosition;

                if (stableCount >=
                    IrFocusSyncStableSampleCount)
                {
                    break;
                }

            }
            return _currentIrFocus;
        }

        /// <summary>
        /// 옥상장비 CTEC EO Focus를 목표 Raw Position으로 직접 이동한다.
        ///
        /// VISCA Focus Direct Position 명령을 한 번만 송신하고,
        /// TCP Port 9000 Focus Position Inquiry 응답은
        /// 목표 도착 확인 용도로만 사용한다.
        /// </summary>
        private async Task<bool> MoveRooftopEoFocusToRawPositionAsync(
            RtspSourceOption ctecSource,
            int targetRawPosition,
            CancellationToken cancellationToken)
        {
            if (ctecSource == null)
            {
                return false;
            }

            int safeTargetRawPosition =
                Math.Max(
                    0,
                    Math.Min(
                        CtecEoFocusPositionMax,
                        targetRawPosition));

            try
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[ROOFTOP FOCUS SYNC] DIRECT START");

                Console.WriteLine(
                    $"[ROOFTOP FOCUS SYNC] TARGET : " +
                    $"{safeTargetRawPosition}");

                ConsoleLogHelper.PrintLine();

                bool commandResult =
                    await _ctecCameraCommandService
                        .MoveFocusPositionAsync(
                            ctecSource.ControlIp,
                            ctecSource.ControlUserName,
                            ctecSource.ControlPassword,
                            ctecSource.UseHttps,
                            (ushort)safeTargetRawPosition);

                if (!commandResult)
                {
                    Console.WriteLine(
                        "[ROOFTOP FOCUS SYNC] " +
                        "DIRECT COMMAND FAILED");

                    ConsoleLogHelper.PrintLine();

                    return false;
                }

                Stopwatch timeout =
                    Stopwatch.StartNew();

                while (timeout.ElapsedMilliseconds <
                       RooftopFocusSyncTimeoutMs)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    int? currentPosition =
                        await RequestAndWaitCtecEoPositionAsync(
                            ContinuousMoveType.EoFocus,
                            ctecSource,
                            cancellationToken);

                    if (currentPosition.HasValue)
                    {
                        int error =
                            Math.Abs(
                                currentPosition.Value -
                                safeTargetRawPosition);

                        Console.WriteLine(
                            "[ROOFTOP FOCUS SYNC] DIRECT CHECK " +
                            $"/ POSITION={currentPosition.Value} " +
                            $"/ TARGET={safeTargetRawPosition} " +
                            $"/ ERROR={error}");

                        if (error <=
                            RooftopFocusSyncTolerance)
                        {
                            Console.WriteLine(
                                "[ROOFTOP FOCUS SYNC] " +
                                "DIRECT COMPLETED");

                            ConsoleLogHelper.PrintLine();

                            return true;
                        }

                    }

                    await Task.Delay(
                        RooftopFocusSyncInquiryIntervalMs,
                        cancellationToken);
                }

                Console.WriteLine(
                    "[ROOFTOP FOCUS SYNC] DIRECT TIMEOUT");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "[ROOFTOP FOCUS SYNC] DIRECT CANCELED");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[ROOFTOP FOCUS SYNC] DIRECT ERROR : " +
                    ex.Message);

                ConsoleLogHelper.PrintLine();

                return false;
            }

        }

        /// <summary>
        /// 진행 중인 Focus Sync 확인 작업과 장비 이동을 정지한다.
        /// </summary>
        private async Task StopFocusSyncAsync()
        {
            CancellationTokenSource cts =
                _rooftopFocusSyncCts;

            _rooftopFocusSyncCts =
                null;

            if (cts != null)
            {
                cts.Cancel();
            }

            /// <summary>
            /// IR Focus Sync는 Near / Far 연속 이동 방식이므로
            /// 사용자 STOP 또는 취소 시 반드시 Focus Stop을 송신한다.
            ///
            /// Pan / Tilt 오동작을 발생시킨 0x28 명령은 사용하지 않는다.
            /// </summary>
            _controlCommandService
                .StopIrFocus();

            RtspSourceOption ctecSource =
                _connectedEoCtecSource;

            if (ctecSource != null)
            {
                await _ctecCameraCommandService
                    .StopFocusAsync(
                        ctecSource.ControlIp,
                        ctecSource.ControlUserName,
                        ctecSource.ControlPassword,
                        ctecSource.UseHttps);
            }

            FocusSyncStatusText =
                "STOPPED";
        }

        /// <summary>
        /// Web Agent 표준 Focus Position 0 ~ 1000을
        /// CTEC EO Focus Raw Position 0 ~ 32768로 변환한다.
        /// </summary>
        private static int ConvertStandardFocusToCtecRaw(
            int standardPosition)
        {
            int safePosition =
                Math.Max(
                    0,
                    Math.Min(
                        1000,
                        standardPosition));

            return (int)Math.Round(
                safePosition *
                CtecEoFocusPositionMax /
                1000.0);
        }
        #endregion
    }

}
