using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>프리셋 렌즈 도착 확인에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IPresetLensFeatureContext
    {
        // 연결을 다시 맺은 뒤 이전 이동 작업이 새 세션에 정지 명령을 보내지 않게 한다.
        string _connectionSessionId { get; }

        Services.Communication.AutoFocusSnapshot[] _autoFocus { get; }

        bool _autoFocusActivatedForSession { get; set; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        short _currentEoFocus { get; set; }

        short _currentEoZoom { get; set; }

        ushort _currentIrZoom { get; set; }

        CancellationTokenSource _laPresetDirectScanCts { get; set; }

        CancellationTokenSource _laPresetSingleMoveCts { get; set; }

        CancellationTokenSource _presetDirectScanCts { get; set; }

        CancellationTokenSource _presetSingleMoveCts { get; set; }

        /// <summary>
        /// EO 직접 제어 구성을 구분해 WEB AGENT AF 명령을 적용할 대상인지 확인한다.
        /// </summary>
        bool AutoFocusTargetUsesAgent(int target);

        bool CameraFeatureTransportReady { get; }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// ->; Standard 0(Far) Raw 0(Near) ->; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrFocusStandardPosition();

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrZoomStandardPosition();

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardFocus();

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardZoom();

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다.
        /// </summary>
        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsAutoFocusPanelVisible { get; }

        bool IsEnvironmentStatusSelected { get; }

        /// <summary>
        /// 2026-09-18: 화면/명령의 표준 좌표가 제공되면 장비 Raw 물리 끝점을 공통 정규화한 값으로 판정한다. Raw 좌표 전용 호출만
        /// 기존 허용 오차를 유지한다.
        /// </summary>
        bool IsIrZoomTargetReached(int target, int actual, int standardTarget = -1, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false);

        bool IsSelectedControlAgentWebAgent { get; }

        /// <summary>
        /// 2026-10-16: 환경부 장비는 신규 CMD2=0xB7 절대위치를 우선 사용하고 실제 IR 렌즈 피드백으로 도달 여부를 확인한다. 새
        /// 피드백이 계속 수신되는데도 위치가 전혀 변하지 않을 때만 0x31 폐루프 경로로 전환한다.
        /// </summary>
        Task<bool> MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget = -1, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false, bool explicitAbsolute = false);

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Focus를 각각 복원한다.
        /// </summary>
        Task<bool> MoveLaPresetFocusDirectAsync(int eoStandardPosition, int irStandardPosition, CancellationToken cancellationToken, int eoTolerance = LensSyncTargetTolerance, int irTolerance = IrFocusSyncTolerance, bool? onlyIr = null);

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Zoom을 각각 복원한다.
        /// </summary>
        Task<bool> MoveLaPresetZoomDirectAsync(int eoStandardPosition, int irPosition, CancellationToken cancellationToken, int eoTolerance = LensSyncTargetTolerance, int irTolerance = 1, bool? onlyIr = null);

        Services.Video.PanoramaReferenceState PanoramaReference { get; }

        int PresetScanDelay { get; set; }

        /// <summary>
        /// IR 렌즈의 새 응답에서 이동이 멈춘 정착 위치를 확인한다.
        /// </summary>
        Task<int> WaitForIrLensSettledPositionAsync(bool zoom, CancellationToken cancellationToken);
    }

    /// <summary>프리셋 렌즈 도착 확인 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PresetLensFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPresetLensFeatureContext _context;

        /// <summary>
        /// 프리셋 렌즈 복원 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal PresetLensFeature(IPresetLensFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal int _presetSkippedFocusMask;

        internal long _presetEoZoomTicks;

        internal long _presetEoFocusTicks;

        internal long _presetIrZoomTicks;

        internal long _presetIrFocusTicks;

        #endregion

        #region [화면 속성과 이벤트]

        internal string PresetFocusSkipText
        {
            get
            {
                if (_presetSkippedFocusMask == 3)
                {
                    return "EO/IR FOCUS SKIPPED";
                }
                else
                {
                    return _presetSkippedFocusMask == 1 ? "EO FOCUS SKIPPED" : _presetSkippedFocusMask == 2 ? "IR FOCUS SKIPPED" : string.Empty;
                }
            }
        }

        internal bool RequiresDirectPresetRestore
        {
            get
            {
                return _context.IsSelectedControlAgentWebAgent || Math.Abs(_context.PanoramaReference.PanOffset) > 0.000001 || Math.Abs(_context.PanoramaReference.TiltOffset) > 0.000001 || IsPresetFocusProtected(false) || IsPresetFocusProtected(true);
            }
        }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다.
        /// </summary>
        internal int PresetIrZoomPosition(PresetPointOption preset)
        {
            int position = ParsePresetPosition(preset.IrZoomText, _context.GetCurrentIrZoomStandardPosition());

            if (!preset.UsesLegacyIrZoomCalibration)
            {
                return position;
            }

            // 과거 저장 형식의 기준을 고정해 이후 SYNC 끝점 학습이 기존 프리셋을 바꾸지 않게 한다.
            const int legacyWide = 61;

            const int legacyTele = 934;

            int? received = preset.PositionSnapshot?.IrZoom;

            if (received.HasValue && received.Value >= 0 && received.Value <= 1000 && preset.PositionSnapshot.IrLensStatus == OpenCvWpfTracking.Models.Position.PositionDataStatus.Valid)
            {
                return received.Value;
            }

            return legacyWide + (int)Math.Round(position * (legacyTele - legacyWide) / 1000.0);
        }

        /// <summary>
        /// 프리셋에서는 자동 초점 채널의 초점만 제외하며 팬·틸트·줌 이동은 차단하지 않는다.
        /// </summary>
        internal bool IsPresetFocusProtected(bool ir)
        {
            if (!_context.IsAutoFocusPanelVisible || !_context.CameraFeatureTransportReady || !_context.AutoFocusTargetUsesAgent(ir ? 1 : 0))
            {
                return false;
            }

            return _context.IsAutoFocusBlockingManualFocus(ir, false) || (!_context._autoFocusActivatedForSession && _context._autoFocus[ir ? 1 : 0] == null);
        }

        /// <summary>
        /// 완료한 이동과 적용하지 않은 저장 초점을 화면에서 구분한다.
        /// </summary>
        internal string FormatPresetCompletion(string completed)
        {
            if (_presetSkippedFocusMask == 0)
            {
                return completed;
            }
            else
            {
                return completed.Replace("PTZF", "PTZ").Replace(" COMPLETED", " (" + PresetFocusSkipText + ") COMPLETED");
            }
        }

        /// <summary>
        /// 선택 채널의 줌 도착을 확인한 뒤 정지 시간을 시작하고 그동안 저장 초점을 복원한다.
        /// </summary>
        internal async Task<bool> RestorePresetLensAsync(int ez, int ef, int iz, int iff, int delaySeconds, bool scanTiming, CancellationToken token, bool? onlyIr = null)
        {
            var zoomTimer = Stopwatch.StartNew();

            bool zoom = await RunPresetLensStageAsync(true, PresetZoomRestoreTimeoutMs, async ct => await _context.MoveLaPresetZoomDirectAsync(ez, iz, ct, PresetEoLensTolerance, PresetIrLensTolerance, onlyIr) && await WaitPresetZoomArrivalAsync(ez, iz, onlyIr, ct), token, onlyIr);

            token.ThrowIfCancellationRequested();

            if (!zoom)
            {
                ConsoleLogHelper.Warning("PRESET ZOOM ARRIVAL", "줌 도착 미확인 / 다음 지점으로 이동하지 않습니다. / " + "AGENT=" + (_context.IsSelectedControlAgentWebAgent ? "WEB" : "LA") + " / CHANNEL=" + (onlyIr.HasValue ? (onlyIr.Value ? "IR" : "EO") : "EO/IR") + " / EO_TARGET=" + ez + " / EO_ACTUAL=" + _context.GetCurrentPresetStandardZoom() + " / IR_TARGET=" + iz + " / IR_ACTUAL=" + _context.GetCurrentIrZoomStandardPosition());

                return false;
            }

            int delayMs = Math.Max(1, Math.Min(60, delaySeconds)) * 1000;

            var dwell = Stopwatch.StartNew();

            ConsoleLogHelper.State("PRESET ZOOM ARRIVAL", "선택 채널 줌 도착 확인 / " + (scanTiming ? "지금부터 정지 시간 시작" : "저장 초점 복원 시작") + " / AGENT=" + (_context.IsSelectedControlAgentWebAgent ? "WEB" : "LA") + " / ZOOM_MOVE_MS=" + zoomTimer.ElapsedMilliseconds + " / DELAY_MS=" + (scanTiming ? delayMs : 0));

            // 순회 중 초점 복원은 정지 시간과 병행한다. 초점이 미도달해도 다음 지점의 줌 대기를 막지 않는다.
            bool focus = await RunPresetLensStageAsync(false, scanTiming ? delayMs : PresetFocusRestoreTimeoutMs, ct => _context.MoveLaPresetFocusDirectAsync(ef, iff, ct, PresetEoLensTolerance, PresetIrLensTolerance, onlyIr), token, onlyIr);

            if (scanTiming && dwell.ElapsedMilliseconds < delayMs)
            {
                await Task.Delay(delayMs - (int)dwell.ElapsedMilliseconds, token);
            }

            token.ThrowIfCancellationRequested();

            ConsoleLogHelper.State("PRESET LENS RESULT", "줌 도착 후 처리 완료 / ZOOM=" + zoom + " / FOCUS=" + focus + " / SCAN=" + scanTiming + " / DWELL_MS=" + dwell.ElapsedMilliseconds + " / CONFIGURED_DELAY_MS=" + (scanTiming ? delayMs : 0));

            return scanTiming || focus;
        }

        /// <summary>
        /// 선택한 모든 채널의 최신 줌 응답이 두 번 연속 목표 범위에 들어오는지 확인한다.
        /// </summary>
        internal async Task<bool> WaitPresetZoomArrivalAsync(int eoTarget, int irTarget, bool? onlyIr, CancellationToken token)
        {
            var timer = Stopwatch.StartNew();

            long eoTicks = PresetLensTicks(false, true);

            long irTicks = PresetLensTicks(true, true);

            int stable = 0;

            while (timer.ElapsedMilliseconds < 5000)
            {
                token.ThrowIfCancellationRequested();

                long nextEoTicks = PresetLensTicks(false, true);

                long nextIrTicks = PresetLensTicks(true, true);

                bool newEo = onlyIr == true || nextEoTicks > eoTicks;

                bool newIr = onlyIr == false || nextIrTicks > irTicks;

                bool eoReached = onlyIr == true || HasFreshPresetLens(false, true) && Math.Abs(_context.GetCurrentPresetStandardZoom() - eoTarget) <= PresetEoLensTolerance;

                bool irReached = onlyIr == false || HasFreshPresetLens(true, true) && Math.Abs(_context.GetCurrentIrZoomStandardPosition() - irTarget) <= PresetIrLensTolerance;

                if (!eoReached || !irReached)
                {
                    stable = 0;
                }

                if (newEo && newIr)
                {
                    eoTicks = nextEoTicks;
                    irTicks = nextIrTicks;

                    stable = eoReached && irReached ? stable + 1 : 0;

                    if (stable >= 2)
                    {
                        return true;
                    }
                }

                await Task.Delay(25, token);
            }

            return false;
        }

        /// <summary>
        /// 허용 시간 안에서 프리셋 렌즈 단계의 완료 여부를 확인한다.
        /// </summary>
        internal async Task<bool> RunPresetLensStageAsync(bool zoom, int budgetMs, Func<CancellationToken, Task<bool>> move, CancellationToken token, bool? onlyIr = null)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(budgetMs);

                try
                {
                    bool result = await move(deadline.Token);

                    token.ThrowIfCancellationRequested();

                    return result && !deadline.IsCancellationRequested;
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    ConsoleLogHelper.Warning("PRESET LENS TIMEOUT", (zoom ? "줌 복원 제한시간 초과 / 순회 중단" : "초점 복원 제한시간 종료 / 줌 도착 기준 유지") + " / LIMIT_MS=" + budgetMs);

                    return false;
                }
                finally
                {
                    // 시간 초과된 연속 렌즈 명령이 다음 프리셋 지점까지 이어지지 않도록 정지한다.
                    if (deadline.IsCancellationRequested)
                    {
                        if (zoom)
                        {
                            if (onlyIr != false)
                            {
                                _context._controlCommandService.StopIrZoom();
                            }

                            if (onlyIr != true)
                            {
                                _context._controlCommandService.StopEoZoom();
                            }
                        }
                        else
                        {
                            if (onlyIr != false && !IsPresetFocusProtected(true))
                            {
                                _context._controlCommandService.StopIrFocus();
                            }

                            if (onlyIr != true && !IsPresetFocusProtected(false))
                            {
                                _context._controlCommandService.StopEoFocus();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 프리셋에 저장한 IR 광학 줌 위치를 복원한다.
        /// </summary>
        internal async Task<bool> RestorePresetIrZoomAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token)
        {
            if (!await EnsurePresetIrLensFeedbackAsync(true, token))
            {
                return false;
            }

            // LA는 화각 절대 이동을 사용한다. WEB 절대 위치와 기존 SYNC 보정은 변경하지 않는다.
            bool moved = _context.IsSelectedControlAgentWebAgent ? await _context.MoveIrZoomToPositionAsync(rawTarget, token, standardTarget, tolerance) : await MoveLaIrZoomAbsoluteAsync(rawTarget, standardTarget, tolerance, token);
            return moved && HasFreshPresetLens(true, true) && _context.IsIrZoomTargetReached(rawTarget, _context._currentIrZoom, standardTarget, tolerance);
        }

        /// <summary>
        /// LA 프리셋·직접 입력은 기존 화각 위치 이동 0x29를 한 번 송신하고 실제 도착을 확인한다.
        /// </summary>
        internal async Task<bool> MoveLaIrZoomAbsoluteAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token, string requestType = "PRESET")
        {
            token.ThrowIfCancellationRequested();
            if (_context.IsSelectedControlAgentWebAgent)
            {
                return false;
            }

            int start = _context._currentIrZoom;
            string session = _context._connectionSessionId;
            if (HasFreshPresetLens(true, true) && _context.IsIrZoomTargetReached(rawTarget, start, standardTarget, tolerance))
            {
                return true;
            }

            // LA는 제어값과 수신값의 방향이 반대다. 송신은 저장된 표준 제어값, 판정은 반전된 수신 목표를 사용한다.
            bool tele = rawTarget < start;
            int rawDirection = tele ? -1 : 1;
            long sequence = PresetLensTicks(true, true);
            var timer = Stopwatch.StartNew();
            long lastFeedbackMs = 0;
            bool started = false;
            bool completed = false;
            bool reached = false;
            try
            {
                started = _context._controlCommandService.LaIrZoomGoPosition(standardTarget);
                if (!started)
                {
                    return false;
                }

                ConsoleLogHelper.Command("LA IR ZOOM ABSOLUTE", "REQUEST=" + requestType + " / AGENT=LA / CMD2=0x29 / WIRE_POSITION=" + standardTarget + " / EXPECTED_RAW=" + rawTarget + " / RAW_START=" + start + " / TARGET=" + standardTarget);
                while (timer.ElapsedMilliseconds < IrZoomSyncTimeoutMs)
                {
                    token.ThrowIfCancellationRequested();
                    if (session != _context._connectionSessionId)
                        throw new OperationCanceledException("LA IR Zoom 이동 중 연결 세션이 변경되었습니다.");
                    long next = PresetLensTicks(true, true);
                    if (next > sequence)
                    {
                        sequence = next;
                        lastFeedbackMs = timer.ElapsedMilliseconds;
                        int actual = _context._currentIrZoom;
                        if ((actual - start) * rawDirection < -tolerance)
                        {
                            ConsoleLogHelper.Warning("LA IR ZOOM ABSOLUTE", "RESULT=DIRECTION_MISMATCH / EXPECTED_RAW=" + rawTarget + " / ACTUAL_RAW=" + actual);
                            return false;
                        }

                        if (_context.IsIrZoomTargetReached(rawTarget, actual, standardTarget, tolerance))
                        {
                            reached = true;
                            break;
                        }
                    }

                    if (timer.ElapsedMilliseconds - lastFeedbackMs > 3000)
                    {
                        ConsoleLogHelper.Warning("LA IR ZOOM ABSOLUTE", "RESULT=FEEDBACK_TIMEOUT / 3초간 새 줌 응답 없음");
                        return false;
                    }

                    await Task.Delay(25, token);
                }

                if (!reached)
                {
                    ConsoleLogHelper.Warning("LA IR ZOOM ABSOLUTE", "RESULT=TARGET_NOT_REACHED");
                    return false;
                }

                int settled = await _context.WaitForIrLensSettledPositionAsync(true, token);
                bool confirmed = session == _context._connectionSessionId && HasFreshPresetLens(true, true) && _context.IsIrZoomTargetReached(rawTarget, settled, standardTarget, tolerance);
                ConsoleLogHelper.State("LA IR ZOOM ABSOLUTE", "REQUEST=" + requestType + " / RESULT=" + (confirmed ? "COMPLETED" : "UNCONFIRMED") + " / TARGET=" + standardTarget + " / ACTUAL=" + _context.GetCurrentIrZoomStandardPosition() + " / CONFIRMED=" + confirmed);
                completed = confirmed;
                return confirmed;
            }
            finally
            {
                // 정상 절대 이동에는 STOP을 끼워 넣지 않는다. 취소·미도달·통신 단절 시에만 안전 정지한다.
                if (started && !completed && session == _context._connectionSessionId)
                {
                    _context._controlCommandService.StopIrZoom();
                }
            }
        }

        /// <summary>
        /// 2026-10-14 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다.
        /// </summary>
        internal void MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            long now = DateTime.UtcNow.Ticks;

            if (ir)
            {
                if (zoom)
                {
                    Interlocked.Exchange(ref _presetIrZoomTicks, now);
                }

                if (focus)
                {
                    Interlocked.Exchange(ref _presetIrFocusTicks, now);
                }
            }
            else
            {
                if (zoom)
                {
                    Interlocked.Exchange(ref _presetEoZoomTicks, now);
                }

                if (focus)
                {
                    Interlocked.Exchange(ref _presetEoFocusTicks, now);
                }
            }
        }

        /// <summary>
        /// 채널과 축별 마지막 렌즈 응답 시간을 조회한다.
        /// </summary>
        internal long PresetLensTicks(bool ir, bool zoom)
        {
            if (ir)
            {
                return (zoom ? Interlocked.Read(ref _presetIrZoomTicks) : Interlocked.Read(ref _presetIrFocusTicks));
            }
            else
            {
                return (zoom ? Interlocked.Read(ref _presetEoZoomTicks) : Interlocked.Read(ref _presetEoFocusTicks));
            }
        }

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다.
        /// </summary>
        internal bool HasFreshPresetLens(bool ir, bool zoom)
        {
            long ticks = PresetLensTicks(ir, zoom);

            return ticks > 0 && DateTime.UtcNow.Ticks - ticks <= TimeSpan.FromSeconds(3).Ticks;
        }

        /// <summary>
        /// 이전 연결의 프리셋 렌즈 응답 증거를 초기화한다.
        /// </summary>
        internal void ResetPresetLensEvidence()
        {
            Interlocked.Exchange(ref _presetEoZoomTicks, 0);

            Interlocked.Exchange(ref _presetEoFocusTicks, 0);

            Interlocked.Exchange(ref _presetIrZoomTicks, 0);

            Interlocked.Exchange(ref _presetIrFocusTicks, 0);

            // 연결 세션 종료 시 이전 프리셋의 지연된 렌즈 명령이 새 세션으로 넘어가지 않도록 취소한다.
            foreach (var cts in new[]
            {
                _context._laPresetSingleMoveCts,
                _context._presetSingleMoveCts,
                _context._laPresetDirectScanCts,
                _context._presetDirectScanCts
            }

            )
            {
                if (cts != null)
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    { /* 이미 완료된 소유 Task의 취소 경쟁 */
                    }
                }
            }
        }

        /// <summary>
        /// IR 피드백을 확보하지 못하면 절대위치 성공/시간추정으로 프리셋 완료를 표시하지 않는다.
        /// </summary>
        internal async Task<bool> EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token)
        {
            if (HasFreshPresetLens(true, zoom))
            {
                return true;
            }

            if (_context.IsEnvironmentStatusSelected)
            {
                _context._controlCommandService.RequestLensCapabilityAndTelemetry(1);
            }

            var watch = Stopwatch.StartNew();

            while (watch.ElapsedMilliseconds < 1800)
            {
                token.ThrowIfCancellationRequested();

                if (HasFreshPresetLens(true, zoom))
                {
                    return true;
                }

                await Task.Delay(30, token);
            }

            ConsoleLogHelper.Warning("PRESET LENS", "IR " + (zoom ? "ZOOM" : "FOCUS") + " feedback missing; restore unconfirmed, no timed estimate");

            return false;
        }

        /// <summary>
        /// EO 단발 Direct 이후 해당 축의 새 상태가 두 번 목표에 도달했는지 확인한다.
        /// </summary>
        internal async Task<bool> WaitPresetEoLensTargetAsync(int target, bool zoom, long afterTicks, CancellationToken token, int tolerance = LensSyncTargetTolerance)
        {
            var watch = Stopwatch.StartNew();

            int stable = 0;

            while (watch.ElapsedMilliseconds < 15000)
            {
                token.ThrowIfCancellationRequested();

                long ticks = PresetLensTicks(false, zoom);

                if (ticks > afterTicks)
                {
                    afterTicks = ticks;

                    int actual = zoom ? _context._currentEoZoom : _context._currentEoFocus;

                    stable = Math.Abs(actual - target) <= tolerance ? stable + 1 : 0;

                    if (stable >= 2)
                    {
                        return true;
                    }
                }

                await Task.Delay(25, token);
            }

            ConsoleLogHelper.Warning("PRESET LENS", "EO " + (zoom ? "ZOOM" : "FOCUS") + " target unconfirmed / TARGET=" + target + " / ACTUAL=" + (zoom ? _context._currentEoZoom : _context._currentEoFocus));

            return false;
        }

        /// <summary>
        /// 장비 저장 PRESET W도 로컬에 저장한 네 렌즈 값을 복원·검증한 뒤 완료 처리한다.
        /// </summary>
        internal async Task<bool> RestoreStoredPresetLensAsync(PresetPointOption preset, CancellationToken token, bool scanTiming = false)
        {
            if (preset == null)
            {
                return false;
            }

            int ez, ef, iz, iff;

            if (!TryPresetLensValue(preset.EoZoomText, out ez) || !TryPresetLensValue(preset.EoFocusText, out ef) || !TryPresetLensValue(preset.IrZoomText, out iz) || !TryPresetLensValue(preset.IrFocusText, out iff))
            {
                ConsoleLogHelper.Warning("PRESET LENS", "P" + preset.Number + " missing/invalid stored EO/IR lens values; re-register preset");

                return false;
            }

            iz = PresetIrZoomPosition(preset);

            bool result = await RestorePresetLensAsync(ez, ef, iz, iff, _context.PresetScanDelay, scanTiming, token);

            ConsoleLogHelper.State("PRESET LENS", "P" + preset.Number + " / EO_TARGET=" + ez + "," + ef + " / IR_TARGET=" + iz + "," + iff + " / EO_ACTUAL=" + _context.GetCurrentPresetStandardZoom() + "," + _context.GetCurrentPresetStandardFocus() + " / IR_ACTUAL=" + _context.GetCurrentIrZoomStandardPosition() + "," + _context.GetCurrentIrFocusStandardPosition() + " / RESULT=" + result);

            return result;
        }

        /// <summary>
        /// 프리셋의 렌즈 값을 허용 범위 안에서 읽는다.
        /// </summary>
        internal static bool TryPresetLensValue(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0 && value <= 1000;
        }

        #endregion

    }
}
