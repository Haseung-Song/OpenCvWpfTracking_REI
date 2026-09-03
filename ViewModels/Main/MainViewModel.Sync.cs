using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
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

            await StopZoomSyncAsync();

            short standardPosition =
                selectedLevel.Position;

            ZoomSyncStatusText =
                $"APPLYING LEVEL {selectedLevel.Level}";

            if (SelectedEquipmentStatusMode ==
                EquipmentStatusMode.Environment)
            {
                bool environmentEoResult =
                    _webAgentZoomControlService
                        .SetEoZoomPosition(
                            standardPosition);

                bool environmentIrResult =
                    _webAgentZoomControlService
                        .SetIrZoomPosition(
                            standardPosition);

                environmentEoResult = environmentEoResult &&
                    (_eoDecoder.IsOpened || _isEoFrameDisplayed);
                environmentIrResult = environmentIrResult &&
                    (_irDecoder.IsOpened || _isIrFrameDisplayed);

                ZoomSyncStatusText =
                    environmentEoResult && environmentIrResult
                        ? $"COMPLETED / LEVEL {selectedLevel.Level}"
                        : $"INCOMPLETE / EO={environmentEoResult} / IR={environmentIrResult}";

                return;
            }

            bool irResult =
                _webAgentZoomControlService
                    .SetIrZoomPosition(
                        standardPosition);

            irResult = irResult &&
                (_irDecoder.IsOpened || _isIrFrameDisplayed);

            RtspSourceOption ctecSource =
                _connectedEoCtecSource;

            if (ctecSource == null)
            {
                ZoomSyncStatusText =
                    $"INCOMPLETE / EO=False / IR={irResult}";

                return;
            }

            int eoRawTarget =
                ConvertStandardZoomToCtecRaw(
                    standardPosition);

            CancellationTokenSource zoomSyncCts =
                new CancellationTokenSource();

            _rooftopZoomSyncCts =
                zoomSyncCts;

            bool eoResult;

            try
            {
                eoResult =
                    await MoveRooftopEoZoomToRawPositionAsync(
                        ctecSource,
                        eoRawTarget,
                        zoomSyncCts.Token);
            }
            finally
            {
                /// <summary>
                /// 현재 Apply 작업이 여전히 등록된 작업인 경우에만 해제한다.
                ///
                /// 사용자가 새 Level을 적용하거나 STOP을 누른 경우에는
                /// StopZoomSyncAsync가 기존 Token을 먼저 취소 / 해제하므로
                /// 새 작업의 Token을 잘못 지우지 않도록 참조를 비교한다.
                /// </summary>
                if (ReferenceEquals(
                        _rooftopZoomSyncCts,
                        zoomSyncCts))
                {
                    _rooftopZoomSyncCts =
                        null;

                    zoomSyncCts.Dispose();
                }

            }

            eoResult = eoResult &&
                (_eoDecoder.IsOpened || _isEoFrameDisplayed);

            ZoomSyncStatusText =
                eoResult && irResult
                    ? $"COMPLETED / LEVEL {selectedLevel.Level}"
                    : $"INCOMPLETE / EO={eoResult} / IR={irResult}";
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
                cts.Dispose();
            }

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

            try
            {
                /// <summary>
                /// IR Focus 목표값을 현재 장비의 상태 좌표로 변환한다.
                /// LA 상태만 표준 방향과 반대이며 Web Agent 상태는 동일하다.
                /// </summary>
                int irRawTargetPosition =
                    ConvertIrFocusStandardToStatusPosition(
                        standardPosition);

                Task<bool> irMoveTask =
                    isIrConnected
                        ? MoveIrFocusToPositionAsync(
                            irRawTargetPosition,
                            focusSyncCts.Token)
                        : Task.FromResult(
                            false);

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

                    irResult =
                        await irMoveTask;
                }
                else
                {
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

            }
            finally
            {
                if (ReferenceEquals(
                        _rooftopFocusSyncCts,
                        focusSyncCts))
                {
                    _rooftopFocusSyncCts =
                        null;

                    focusSyncCts.Dispose();
                }

            }

            eoResult = eoResult &&
                (_eoDecoder.IsOpened || _isEoFrameDisplayed);

            irResult = irResult &&
                (_irDecoder.IsOpened || _isIrFrameDisplayed);

            FocusSyncStatusText =
                eoResult && irResult
                    ? $"COMPLETED / LEVEL {selectedLevel.Level}"
                    : $"INCOMPLETE / EO={eoResult} / IR={irResult}";
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

                bool moveNear =
                    SelectedEquipmentStatusMode ==
                        EquipmentStatusMode.Rooftop
                            ? safeTargetPosition <
                              startPosition
                            : safeTargetPosition >
                              startPosition;

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

                        int dynamicStopLead =
                            Math.Max(
                                stopLead,
                                Math.Min(
                                    55,
                                    largestObservedStep +
                                    8));

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

                        if (reachedStopZone ||
                            passedTarget)
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
                    await WaitForIrFocusSettledPositionAsync(
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

            return false;
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
                cts.Dispose();
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
