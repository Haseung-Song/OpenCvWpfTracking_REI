using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;

// 2026-10-16: 프리셋 렌즈 복원과 수신 증거·지연 시간 관리.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        #region [필드 및 상태 정보]

        /// <summary>과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다.</summary>
        private int PresetIrZoomPosition(PresetPointOption preset)
        {
            int position = ParsePresetPosition(preset.IrZoomText, GetCurrentIrZoomStandardPosition());
            if (!preset.UsesLegacyIrZoomCalibration)
                return position;

            // 과거 저장 형식의 기준을 고정해 이후 SYNC 끝점 학습이 기존 프리셋을 바꾸지 않게 한다.
            const int legacyWide = 61;
            const int legacyTele = 934;
            int? received = preset.PositionSnapshot?.IrZoom;
            if (received.HasValue && received.Value >= 0 && received.Value <= 1000 &&
                preset.PositionSnapshot.IrLensStatus == OpenCvWpfTracking.Models.Position.PositionDataStatus.Valid)
                return received.Value;

            return legacyWide + (int)Math.Round(position * (legacyTele - legacyWide) / 1000.0);
        }

        private int _presetSkippedFocusMask;

        /// <summary>자동 초점 또는 상태 미확인으로 저장 초점을 적용하지 않은 채널을 구분한다.</summary>
        public string PresetFocusSkipText => _presetSkippedFocusMask == 3 ? "EO/IR FOCUS SKIPPED" :
            _presetSkippedFocusMask == 1 ? "EO FOCUS SKIPPED" :
            _presetSkippedFocusMask == 2 ? "IR FOCUS SKIPPED" : string.Empty;

        /// <summary>프리셋에서는 자동 초점 채널의 초점만 제외하며 팬·틸트·줌 이동은 차단하지 않는다.</summary>
        private bool IsPresetFocusProtected(bool ir)
        {
            if (!IsAf292PanelVisible || !CameraFeatureTransportReady || !Af292TargetUsesAgent(ir ? 1 : 0))
            {
                return false;
            }

            return IsAf292FocusBlocked(ir, false) || (!_af292ActivatedForSession && _af292[ir ? 1 : 0] == null);
        }

        /// <summary>장비 프리셋 명령이 내부 초점을 복원하지 않도록 보호 채널은 직접 PTZ 경로로 이동한다.</summary>
        private bool RequiresDirectPresetRestore =>
            IsSelectedControlAgentWebAgent || Math.Abs(PanoramaReference.PanOffset) > 0.000001 ||
            Math.Abs(PanoramaReference.TiltOffset) > 0.000001 ||
            IsPresetFocusProtected(false) || IsPresetFocusProtected(true);

        /// <summary>완료한 이동과 적용하지 않은 저장 초점을 화면에서 구분한다.</summary>
        private string FormatPresetCompletion(string completed)
        {
            return _presetSkippedFocusMask == 0 ? completed : completed.Replace("PTZF", "PTZ")
                .Replace(" COMPLETED", " (" + PresetFocusSkipText + ") COMPLETED");
        }

        private const int PresetEoLensTolerance = 25;

        private const int PresetIrLensTolerance = 35;

        #endregion

        #region [기능 처리 함수]

        // 줌 복원 제한은 운용자가 설정한 정지 시간과 분리한다.
        // 초점은 기존 저장값으로 복원하되 자동 순회의 정지 시간 시작 조건으로 사용하지 않는다.
        private const int PresetZoomRestoreTimeoutMs = 45000;

        private const int PresetFocusRestoreTimeoutMs = 15000;

        /// <summary>
        /// 선택 채널의 줌 도착을 확인한 뒤 정지 시간을 시작하고 그동안 저장 초점을 복원한다.
        /// </summary>
        private async Task<bool> RestorePresetLensAsync(
            int ez,
            int ef,
            int iz,
            int iff,
            int delaySeconds,
            bool scanTiming,
            CancellationToken token,
            bool? onlyIr = null)
        {
            var zoomTimer = Stopwatch.StartNew();
            bool zoom = await RunPresetLensStageAsync(
                true,
                PresetZoomRestoreTimeoutMs,
                async ct => await MoveLaPresetZoomDirectAsync(
                    ez, iz, ct, PresetEoLensTolerance, PresetIrLensTolerance, onlyIr) &&
                    await WaitPresetZoomArrivalAsync(ez, iz, onlyIr, ct),
                token,
                onlyIr);

            token.ThrowIfCancellationRequested();

            if (!zoom)
            {
                ConsoleLogHelper.Warning("PRESET ZOOM ARRIVAL",
                    "줌 도착 미확인 / 다음 지점으로 이동하지 않습니다. / " +
                    "AGENT=" + (IsSelectedControlAgentWebAgent ? "WEB" : "LA") +
                    " / CHANNEL=" + (onlyIr.HasValue ? (onlyIr.Value ? "IR" : "EO") : "EO/IR") +
                    " / EO_TARGET=" + ez + " / EO_ACTUAL=" + GetCurrentPresetStandardZoom() +
                    " / IR_TARGET=" + iz + " / IR_ACTUAL=" + GetCurrentIrZoomStandardPosition());
                return false;
            }

            int delayMs = Math.Max(1, Math.Min(60, delaySeconds)) * 1000;
            var dwell = Stopwatch.StartNew();
            ConsoleLogHelper.State("PRESET ZOOM ARRIVAL",
                "선택 채널 줌 도착 확인 / " + (scanTiming ? "지금부터 정지 시간 시작" : "저장 초점 복원 시작") +
                " / AGENT=" + (IsSelectedControlAgentWebAgent ? "WEB" : "LA") +
                " / ZOOM_MOVE_MS=" + zoomTimer.ElapsedMilliseconds +
                " / DELAY_MS=" + (scanTiming ? delayMs : 0));

            // 순회 중 초점 복원은 정지 시간과 병행한다. 초점이 미도달해도 다음 지점의 줌 대기를 막지 않는다.
            bool focus = await RunPresetLensStageAsync(
                false,
                scanTiming ? delayMs : PresetFocusRestoreTimeoutMs,
                ct => MoveLaPresetFocusDirectAsync(
                    ef, iff, ct, PresetEoLensTolerance, PresetIrLensTolerance, onlyIr),
                token,
                onlyIr);

            if (scanTiming && dwell.ElapsedMilliseconds < delayMs)
            {
                await Task.Delay(delayMs - (int)dwell.ElapsedMilliseconds, token);
            }

            token.ThrowIfCancellationRequested();
            ConsoleLogHelper.State("PRESET LENS RESULT",
                "줌 도착 후 처리 완료 / ZOOM=" + zoom + " / FOCUS=" + focus +
                " / SCAN=" + scanTiming + " / DWELL_MS=" + dwell.ElapsedMilliseconds +
                " / CONFIGURED_DELAY_MS=" + (scanTiming ? delayMs : 0));
            return scanTiming || focus;
        }

        /// <summary>
        /// 선택한 모든 채널의 최신 줌 응답이 두 번 연속 목표 범위에 들어오는지 확인한다.
        /// </summary>
        private async Task<bool> WaitPresetZoomArrivalAsync(int eoTarget, int irTarget, bool? onlyIr, CancellationToken token)
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
                bool eoReached = onlyIr == true || HasFreshPresetLens(false, true) &&
                    Math.Abs(GetCurrentPresetStandardZoom() - eoTarget) <= PresetEoLensTolerance;
                bool irReached = onlyIr == false || HasFreshPresetLens(true, true) &&
                    Math.Abs(GetCurrentIrZoomStandardPosition() - irTarget) <= PresetIrLensTolerance;

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
        private async Task<bool> RunPresetLensStageAsync(
            bool zoom,
            int budgetMs,
            Func<CancellationToken, Task<bool>> move,
            CancellationToken token,
            bool? onlyIr = null
        )
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
                    ConsoleLogHelper.Warning(
                        "PRESET LENS TIMEOUT",
                        (zoom ? "줌 복원 제한시간 초과 / 순회 중단" : "초점 복원 제한시간 종료 / 줌 도착 기준 유지") + " / LIMIT_MS=" + budgetMs
                    );
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
                                _controlCommandService.StopIrZoom();
                            if (onlyIr != true)
                                _controlCommandService.StopEoZoom();
                        }
                        else
                        {
                            if (onlyIr != false && !IsPresetFocusProtected(true))
                                _controlCommandService.StopIrFocus();
                            if (onlyIr != true && !IsPresetFocusProtected(false))
                                _controlCommandService.StopEoFocus();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 프리셋에 저장한 IR 광학 줌 위치를 복원한다.
        /// </summary>
        private async Task<bool> RestorePresetIrZoomAsync(int rawTarget, int standardTarget, int tolerance, CancellationToken token)
        {
            if (!await EnsurePresetIrLensFeedbackAsync(true, token))
            {
                return false;
            }

            // LA는 화각 절대 이동을 사용한다. WEB 절대 위치와 기존 SYNC 보정은 변경하지 않는다.
            bool moved = IsSelectedControlAgentWebAgent
                ? await MoveIrZoomToPositionAsync(rawTarget, token, standardTarget, tolerance)
                : await MoveLaPresetIrZoomAbsoluteAsync(rawTarget, standardTarget, tolerance, token);

            return moved && HasFreshPresetLens(true, true) &&
                IsIrZoomTargetReached(rawTarget, _currentIrZoom, standardTarget, tolerance);
        }

        /// <summary>
        /// LA 프리셋은 규격의 화각 위치 이동 0x29를 한 번 송신하고 실제 도착을 확인한다.
        /// </summary>
        private async Task<bool> MoveLaPresetIrZoomAbsoluteAsync(
            int rawTarget, int standardTarget, int tolerance, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (IsSelectedControlAgentWebAgent)
            {
                return false;
            }

            int start = _currentIrZoom;
            if (HasFreshPresetLens(true, true) &&
                IsIrZoomTargetReached(rawTarget, start, standardTarget, tolerance))
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
                started = _controlCommandService.LaIrZoomGoPosition(standardTarget);
                if (!started)
                {
                    return false;
                }

                ConsoleLogHelper.Command("LA PRESET IR ZOOM",
                    "LA 줌 절대 이동 송신 / CMD2=0x29 / WIRE_POSITION=" + standardTarget + " / EXPECTED_RAW=" + rawTarget +
                    " / RAW_START=" + start + " / TARGET=" + standardTarget);

                while (timer.ElapsedMilliseconds < IrZoomSyncTimeoutMs)
                {
                    token.ThrowIfCancellationRequested();
                    long next = PresetLensTicks(true, true);
                    if (next > sequence)
                    {
                        sequence = next;
                        lastFeedbackMs = timer.ElapsedMilliseconds;
                        int actual = _currentIrZoom;
                        if ((actual - start) * rawDirection < -tolerance)
                        {
                            ConsoleLogHelper.Warning("LA PRESET IR ZOOM", "반대 방향 응답 / 정지 후 순회 중단");
                            return false;
                        }

                        if (IsIrZoomTargetReached(rawTarget, actual, standardTarget, tolerance))
                        {
                            reached = true;
                            break;
                        }
                    }

                    if (timer.ElapsedMilliseconds - lastFeedbackMs > 3000)
                    {
                        ConsoleLogHelper.Warning("LA PRESET IR ZOOM", "3초간 새 줌 응답 없음 / 정지 후 순회 중단");
                        return false;
                    }

                    await Task.Delay(25, token);
                }

                if (!reached)
                {
                    ConsoleLogHelper.Warning("LA PRESET IR ZOOM", "목표 도착 미확인 / 순회 중단");
                    return false;
                }

                int settled = await WaitForIrLensSettledPositionAsync(true, token);
                bool confirmed = HasFreshPresetLens(true, true) &&
                    IsIrZoomTargetReached(rawTarget, settled, standardTarget, tolerance);
                ConsoleLogHelper.State("LA PRESET IR ZOOM",
                    "화각 절대 이동 확인 / 재송신·중간 정지 없음 / TARGET=" + standardTarget +
                    " / ACTUAL=" + GetCurrentIrZoomStandardPosition() + " / CONFIRMED=" + confirmed);
                completed = confirmed;
                return confirmed;
            }
            finally
            {
                // 정상 절대 이동에는 STOP을 끼워 넣지 않는다. 취소·미도달·통신 단절 시에만 안전 정지한다.
                if (started && !completed)
                {
                    _controlCommandService.StopIrZoom();
                }
            }
        }

        #endregion

        #region [필드 및 상태 정보]

        private long _presetEoZoomTicks, _presetEoFocusTicks, _presetIrZoomTicks, _presetIrFocusTicks;

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 2026-10-14 V31_5: 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다.
        /// </summary>
        private void MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            long now = DateTime.UtcNow.Ticks;

            if (ir)
            {
                if (zoom)
                    Interlocked.Exchange(ref _presetIrZoomTicks, now);

                if (focus)
                    Interlocked.Exchange(ref _presetIrFocusTicks, now);
            }
            else
            {
                if (zoom)
                    Interlocked.Exchange(ref _presetEoZoomTicks, now);

                if (focus)
                    Interlocked.Exchange(ref _presetEoFocusTicks, now);
            }
        }

        /// <summary>
        /// 채널과 축별 마지막 렌즈 응답 시간을 조회한다.
        /// </summary>
        private long PresetLensTicks(bool ir, bool zoom) =>
            ir ? (zoom ? Interlocked.Read(ref _presetIrZoomTicks) : Interlocked.Read(ref _presetIrFocusTicks)) : (zoom ? Interlocked.Read(ref _presetEoZoomTicks) : Interlocked.Read(ref _presetEoFocusTicks));

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다.
        /// </summary>
        private bool HasFreshPresetLens(bool ir, bool zoom)
        {
            long ticks = PresetLensTicks(ir, zoom);
            return ticks > 0 && DateTime.UtcNow.Ticks - ticks <= TimeSpan.FromSeconds(3).Ticks;
        }

        /// <summary>
        /// 이전 연결의 프리셋 렌즈 응답 증거를 초기화한다.
        /// </summary>
        private void ResetPresetLensEvidence()
        {
            Interlocked.Exchange(ref _presetEoZoomTicks, 0);
            Interlocked.Exchange(ref _presetEoFocusTicks, 0);
            Interlocked.Exchange(ref _presetIrZoomTicks, 0);
            Interlocked.Exchange(ref _presetIrFocusTicks, 0);

            // 연결 세션 종료 시 이전 프리셋의 지연된 렌즈 명령이 새 세션으로 넘어가지 않도록 취소한다.
            foreach (var cts in new[]
            {
                _laPresetSingleMoveCts,
                _presetSingleMoveCts,
                _laPresetDirectScanCts,
                _presetDirectScanCts
            }

            )
                if (cts != null)
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    { /* 이미 완료된 소유 Task의 취소 경쟁 */
                    }
        }

        /// <summary>

        /// IR 피드백을 확보하지 못하면 절대위치 성공/시간추정으로 프리셋 완료를 표시하지 않는다.

        /// </summary>
        private async Task<bool> EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token)
        {
            if (HasFreshPresetLens(true, zoom))
                return true;

            if (IsEnvironmentStatusSelected)
                _controlCommandService.RequestLensCapabilityAndTelemetry(1);

            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 1800)
            {
                token.ThrowIfCancellationRequested();

                if (HasFreshPresetLens(true, zoom))
                    return true;

                await Task.Delay(30, token);
            }

            ConsoleLogHelper.Warning(
                "PRESET LENS",
                "IR " + (zoom ? "ZOOM" : "FOCUS") + " feedback missing; restore unconfirmed, no timed estimate"
            );
            return false;
        }

        /// <summary>

        /// EO 단발 Direct 이후 해당 축의 새 상태가 두 번 목표에 도달했는지 확인한다.

        /// </summary>
        private async Task<bool> WaitPresetEoLensTargetAsync(
            int target,
            bool zoom,
            long afterTicks,
            CancellationToken token,
            int tolerance = LensSyncTargetTolerance
        )
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
                    int actual = zoom ? _currentEoZoom : _currentEoFocus;
                    stable = Math.Abs(actual - target) <= tolerance ? stable + 1 : 0;

                    if (stable >= 2)
                        return true;
                }

                await Task.Delay(25, token);
            }

            ConsoleLogHelper.Warning(
                "PRESET LENS",
                "EO " + (zoom ? "ZOOM" : "FOCUS") + " target unconfirmed / TARGET=" + target + " / ACTUAL=" + (zoom ? _currentEoZoom : _currentEoFocus)
            );
            return false;
        }

        /// <summary>

        /// 장비 저장 PRESET W도 로컬에 저장한 네 렌즈 값을 복원·검증한 뒤 완료 처리한다.

        /// </summary>
        private async Task<bool> RestoreStoredPresetLensAsync(
            PresetPointOption preset,
            CancellationToken token,
            bool scanTiming = false
        )
        {
            if (preset == null)
                return false;

            int ez, ef, iz, iff;

            if (!TryPresetLensValue(
                preset.EoZoomText,
                out ez
            ) ||
                !TryPresetLensValue(
                preset.EoFocusText,
                out ef
            ) ||
                !TryPresetLensValue(
                preset.IrZoomText,
                out iz
            ) ||
                !TryPresetLensValue(
                preset.IrFocusText,
                out iff
            ))
            {
                ConsoleLogHelper.Warning(
                    "PRESET LENS",
                    "P" + preset.Number + " missing/invalid stored EO/IR lens values; re-register preset"
                );
                return false;
            }

            iz = PresetIrZoomPosition(preset);
            bool result = await RestorePresetLensAsync(ez, ef, iz, iff, PresetScanDelay, scanTiming, token);
            ConsoleLogHelper.State(
                "PRESET LENS",
                "P" + preset.Number + " / EO_TARGET=" + ez + "," + ef + " / IR_TARGET=" + iz + "," + iff + " / EO_ACTUAL=" + GetCurrentPresetStandardZoom() + "," + GetCurrentPresetStandardFocus() + " / IR_ACTUAL=" + GetCurrentIrZoomStandardPosition() + "," + GetCurrentIrFocusStandardPosition() + " / RESULT=" + result
            );
            return result;
        }

        /// <summary>
        /// 프리셋의 렌즈 값을 허용 범위 안에서 읽는다.
        /// </summary>
        private static bool TryPresetLensValue(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0 && value <= 1000;
        }

        #endregion
    }
}


