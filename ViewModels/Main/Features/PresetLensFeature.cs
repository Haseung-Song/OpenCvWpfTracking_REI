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

        bool AutoFocusTargetUsesAgent(int target);

        bool CameraFeatureTransportReady { get; }

        int GetCurrentIrFocusStandardPosition();

        int GetCurrentIrZoomStandardPosition();

        ushort GetCurrentPresetStandardFocus();

        ushort GetCurrentPresetStandardZoom();

        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsAutoFocusPanelVisible { get; }

        bool IsEnvironmentStatusSelected { get; }

        bool IsIrZoomTargetReached(int target, int actual, int standardTarget = -1, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false);

        bool IsSelectedControlAgentWebAgent { get; }

        Task<bool> MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget = -1, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false, bool explicitAbsolute = false);

        Task<bool> MoveLaPresetFocusDirectAsync(int eoStandardPosition, int irStandardPosition, CancellationToken cancellationToken, int eoTolerance = LensSyncTargetTolerance, int irTolerance = IrFocusSyncTolerance, bool? onlyIr = null);

        Task<bool> MoveLaPresetZoomDirectAsync(int eoStandardPosition, int irPosition, CancellationToken cancellationToken, int eoTolerance = LensSyncTargetTolerance, int irTolerance = 1, bool? onlyIr = null);

        Services.Video.PanoramaReferenceState PanoramaReference { get; }

        int PresetScanDelay { get; set; }

        Task<int> WaitForIrLensSettledPositionAsync(bool zoom, CancellationToken cancellationToken);
    }

    /// <summary>프리셋 렌즈 도착 확인 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PresetLensFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPresetLensFeatureContext _context;

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

        internal bool IsPresetFocusProtected(bool ir)
        {
            if (!_context.IsAutoFocusPanelVisible || !_context.CameraFeatureTransportReady || !_context.AutoFocusTargetUsesAgent(ir ? 1 : 0))
            {
                return false;
            }

            return _context.IsAutoFocusBlockingManualFocus(ir, false) || (!_context._autoFocusActivatedForSession && _context._autoFocus[ir ? 1 : 0] == null);
        }

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

        internal bool HasFreshPresetLens(bool ir, bool zoom)
        {
            long ticks = PresetLensTicks(ir, zoom);

            return ticks > 0 && DateTime.UtcNow.Ticks - ticks <= TimeSpan.FromSeconds(3).Ticks;
        }

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

        internal static bool TryPresetLensValue(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0 && value <= 1000;
        }

        #endregion

    }
}
