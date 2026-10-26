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

        private const int PresetEoLensTolerance = 25;

        private const int PresetIrLensTolerance = 35;

        #endregion

        #region [기능 처리 함수]

        // 프리셋 전용 시간 제한이며 렌즈 보정과 대기는 설정한 지연 시간을 공유한다.
        // PAN/TILT 이동 시간은 이 제한에서 제외하며 일반 렌즈 제어의 기존 허용 오차는 유지한다.
        /// <summary>
        /// 프리셋의 설정 대기 시간 안에서 렌즈 위치 복원을 수행한다.
        /// </summary>
        private async Task<bool> RestorePresetLensWithinDelayAsync(
            int ez,
            int ef,
            int iz,
            int iff,
            int delaySeconds,
            bool waitRemaining,
            CancellationToken token,
            bool? onlyIr = null
        )
        {
            int budgetMs = Math.Max(1, Math.Min(60, delaySeconds)) * 1000;
            var timer = Stopwatch.StartNew();
            bool zoom = await RunPresetLensStageAsync(
                true,
                Math.Max(
                1,
                budgetMs / 2
            ),
                ct => MoveLaPresetZoomDirectAsync(
                ez,
                iz,
                ct,
                PresetEoLensTolerance,
                PresetIrLensTolerance,
                onlyIr
            ),
                token
            );
            token.ThrowIfCancellationRequested();
            bool focus = await RunPresetLensStageAsync(
                false,
                Math.Max(
                1,
                budgetMs - (int)timer.ElapsedMilliseconds
            ),
                ct => MoveLaPresetFocusDirectAsync(
                ef,
                iff,
                ct,
                PresetEoLensTolerance,
                PresetIrLensTolerance,
                onlyIr
            ),
                token
            );
            long correctionMs = timer.ElapsedMilliseconds;
            ConsoleLogHelper.State(
                "PRESET LENS BUDGET",
                "BUDGET_MS=" + budgetMs + " / CORRECTION_MS=" + correctionMs + " / ZOOM=" + zoom + " / FOCUS=" + focus + " / EO_TARGET=" + ez + "," + ef + " / IR_TARGET=" + iz + "," + iff + " / EO_ACTUAL=" + GetCurrentPresetStandardZoom() + "," + GetCurrentPresetStandardFocus() + " / IR_ACTUAL=" + GetCurrentIrZoomStandardPosition() + "," + GetCurrentIrFocusStandardPosition()
            );

            if (waitRemaining && timer.ElapsedMilliseconds < budgetMs)
                await Task.Delay(budgetMs - (int)timer.ElapsedMilliseconds, token);

            token.ThrowIfCancellationRequested();
            return zoom && focus;
        }

        /// <summary>
        /// 허용 시간 안에서 프리셋 렌즈 단계의 완료 여부를 확인한다.
        /// </summary>
        private async Task<bool> RunPresetLensStageAsync(
            bool zoom,
            int budgetMs,
            Func<CancellationToken, Task<bool>> move,
            CancellationToken token
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
                        "PRESET LENS BUDGET",
                        (zoom ? "ZOOM" : "FOCUS") + " limit reached; incomplete, continue without precision retry / LIMIT_MS=" + budgetMs
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
                            _controlCommandService.StopIrZoom();
                            _controlCommandService.StopEoZoom();
                        }
                        else
                        {
                            _controlCommandService.StopIrFocus();
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
            return await EnsurePresetIrLensFeedbackAsync(
                true,
                token
            ) &&
                await MoveIrZoomToPositionAsync(
                rawTarget,
                token,
                standardTarget,
                tolerance
            ) &&
                HasFreshPresetLens(
                true,
                true
            ) &&
                IsIrZoomTargetReached(
                rawTarget,
                _currentIrZoom,
                standardTarget,
                tolerance
            );
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

            bool result = await RestorePresetLensWithinDelayAsync(ez, ef, iz, iff, PresetScanDelay, scanTiming, token);
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
