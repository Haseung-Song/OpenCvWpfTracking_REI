using OpenCvSharp;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.Services.Video
{
    /// <summary>
    /// 2026-08-18: EO 정지 프레임을 특징점 기반 구면 파노라마로 합성한다.
    /// 단순 좌우 배치가 아니라 OpenCV Panorama Stitcher의 특징점 정합,
    /// 노출 보정, Seam 탐색 및 블렌딩 파이프라인을 사용한다.
    /// </summary>
    public sealed class EoPanoramaStitchingService
    {
        private const int MaximumInputWidth = 1920;

        // Legacy Panorama_View.cpp에서 사용하던 카메라 각도 개념.
        // 실제 영상을 다시 투영하지 않고 검증/정렬 prior로만 사용한다.
        private const double LegacyPanAovDegrees = 26.0;
        private const double LegacyVerticalAovDegrees = 42.5;

        /// <summary>
        /// StitchAndSave 동작 수행 함수.
        /// </summary>
        public BitmapSource StitchAndSave(
            IEnumerable<BitmapSource> sourceFrames,
            string outputPath)
        {
            /*
             * 2026-08-18: App.OnStartup 설정에 대한 방어 코드.
             * 파노라마 Service가 별도 Test Host에서 직접 실행되어도 OpenCL 대신
             * CPU Mat 경로를 사용하도록 Stitcher 생성 전에 다시 지정한다.
             */
            DisableOpenClForCurrentProcess();

            if (sourceFrames == null)
            {
                throw new ArgumentNullException(nameof(sourceFrames));
            }

            List<Mat> frames =
                sourceFrames
                    .Where(frame => frame != null)
                    .Select(ConvertToBgrMat)
                    .Where(frame => frame != null && !frame.Empty())
                    .ToList();

            if (frames.Count < 2)
            {
                DisposeAll(frames);
                throw new InvalidOperationException(
                    "파노라마 합성에는 EO 프레임이 2장 이상 필요합니다.");
            }

            Mat panorama = new Mat();

            try
            {
                Stitcher.Status panoramaStatus;

                panoramaStatus =
                    RunStitcherCpuSafe(
                        frames,
                        panorama,
                        Stitcher.Mode.Panorama);

                /*
                 * 2026-08-18: 저대비 장면 또는 카메라 파라미터 추정 실패 시
                 * 단순 실패로 끝내지 않고 affine 기반 Scans 모드로 한 번 더
                 * 복구한다. 정상 장면에서는 구면 Panorama 결과를 우선한다.
                 */
                if (panoramaStatus != Stitcher.Status.OK ||
                    panorama.Empty())
                {
                    throw new InvalidOperationException(
                        "360도 구면 파노라마 정합에 실패했습니다. " +
                        "불완전한 Scans 결과는 저장하지 않습니다. " +
                        "EO Zoom을 광각으로 맞추고 고정 물체가 충분히 " +
                        "겹치도록 다시 촬영하십시오. (Panorama: " +
                        panoramaStatus + ")");
                }

                using (Mat cropped = CropOuterBlackBorder(panorama))
                {
                    string directory =
                        Path.GetDirectoryName(outputPath);

                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!Cv2.ImWrite(
                        outputPath,
                        cropped,
                        new ImageEncodingParam(
                            ImwriteFlags.JpegQuality,
                            97)))
                    {
                        throw new IOException(
                            "파노라마 JPG 파일 저장에 실패했습니다.");
                    }

                    BitmapSource bitmap =
                        MatToBitmapSourceConverter.Convert(cropped);

                    if (bitmap != null &&
                        bitmap.CanFreeze &&
                        !bitmap.IsFrozen)
                    {
                        bitmap.Freeze();
                    }

                    return bitmap;
                }

            }
            finally
            {
                panorama.Dispose();
                DisposeAll(frames);
            }

        }

        /// <summary>
        /// 2026-08-18: 서로 다른 Tilt에서 촬영한 각 행을 Pan 촬영 순서대로
        /// 원본 해상도에서 먼저 360° 정합한다. 두 장뿐인 세로 열에 Stitcher를
        /// 반복 적용하지 않으므로 ErrorNeedMoreImgs, SHRT_MAX 및 열 누락으로
        /// 인한 360° 단절을 방지한다. 완성된 행은 마지막에만 세로 블렌딩한다.
        /// </summary>
        public BitmapSource StitchRowsAndSave(
            IEnumerable<IEnumerable<BitmapSource>> sourceRows,
            string outputPath)
        {
            DisableOpenClForCurrentProcess();

            Stopwatch totalStopwatch =
                Stopwatch.StartNew();

            if (sourceRows == null)
            {
                throw new ArgumentNullException(nameof(sourceRows));
            }

            List<List<Mat>> frameRows =
                new List<List<Mat>>();

            Mat panorama = null;

            try
            {
                ConsoleLogHelper.Info(
                    "EO PANORAMA / STITCH",
                    "Panorama processing started / MODE=SHARED_LONGITUDE_ROW_FIRST" +
                    " / OUTPUT=" + outputPath);

                int sourceRowIndex = 0;

                foreach (IEnumerable<BitmapSource> sourceRow in sourceRows)
                {
                    List<Mat> rowFrames =
                        sourceRow
                            .Where(frame => frame != null)
                            .Select(ConvertToBgrMat)
                            .Where(frame => frame != null && !frame.Empty())
                            .ToList();

                    if (rowFrames.Count < 2)
                    {
                        DisposeAll(rowFrames);
                        throw new InvalidOperationException(
                            "각 세로 촬영 행에는 EO 프레임이 2장 이상 필요합니다.");
                    }

                    frameRows.Add(rowFrames);

                    ConsoleLogHelper.State(
                        "EO PANORAMA / STITCH",
                        "Input row prepared / ROW=" + (sourceRowIndex + 1) +
                        " / FRAMES=" + rowFrames.Count +
                        " / SIZE=" + rowFrames[0].Width + "x" + rowFrames[0].Height);

                    sourceRowIndex++;
                }

                if (frameRows.Count < 1)
                {
                    throw new InvalidOperationException(
                        "파노라마에는 Tilt 촬영 행이 1개 이상 필요합니다.");
                }

#if DEBUG
                SavePanoramaDebugCapture(frameRows, outputPath);
#endif

                // 2026-09-09: 같은 Pan의 서로 다른 Tilt 원본을 먼저 합치면 원근/시차가
                // 픽셀에 고정되어 난간과 건물이 잘린다. 36개 Pan을 공통 각도 폭으로
                // 행별 완성한 후 동일 경도 좌표에서만 세로 결합한다.
                bool canUseSharedGlobalColumn =
                    frameRows.All(row => row.Count == frameRows[0].Count) &&
                    frameRows[0].Count >= 24;

                if (canUseSharedGlobalColumn)
                {
                    try
                    {
                        panorama = frameRows.Count == 1
                            ? ComposeSharedLongitudeSingleRow(frameRows[0])
                            : ComposeSharedLongitudeRowFirstPanorama(frameRows, outputPath);

                        ConsoleLogHelper.State(
                            "EO PANORAMA / ROW",
                            "Shared-longitude row-first panorama completed" +
                            " / ROWS=" + frameRows.Count +
                            " / FRAMES_PER_ROW=" + frameRows[0].Count +
                            " / RESULT=" + panorama.Width + "x" + panorama.Height);
                    }
                    catch (Exception rowFirstException)
                    {
                        panorama?.Dispose();
                        panorama = null;

                        ConsoleLogHelper.Warning(
                            "EO PANORAMA / FALLBACK",
                            "Shared-longitude row-first failed; trying retained column-first" +
                            " / TYPE=" + rowFirstException.GetType().Name +
                            " / MESSAGE=" + rowFirstException.Message);

                        try
                        {
                            bool usedFixedAngleFallback;
                            panorama = frameRows.Count == 2
                                ? ComposeV21TwoRowColumnFirst(
                                    frameRows[0], frameRows[1], out usedFixedAngleFallback)
                                : ComposeColumnFirstFullCircle(
                                    frameRows, outputPath, out usedFixedAngleFallback);

                            if (usedFixedAngleFallback)
                                throw new InvalidOperationException(
                                    "Column-first 특징점 정합 대신 고정각 복구가 선택되었습니다.");
                        }
                        catch (Exception columnException)
                        {
                            panorama?.Dispose();
                            panorama = null;
                            throw new InvalidOperationException(
                                "공통 경도 Row-first와 보존된 Column-first 정합이 모두 실패했습니다. " +
                                "잘못 결합된 영상은 저장하지 않습니다.",
                                new AggregateException(rowFirstException, columnException));
                        }

                    }

                }

                if (panorama == null ||
                    panorama.Empty())
                {
                    throw new InvalidOperationException(
                        "공통 좌표계 파노라마 결과가 비어 있습니다.");
                }

#if DEBUG
                SavePanoramaDebugMat(outputPath, "output", "panorama_before_crop.jpg", panorama);
#endif
                BitmapSource result =
                    SaveAndConvert(
                        panorama,
                        outputPath);
#if DEBUG
                SavePanoramaDebugMat(outputPath, "output", "panorama_final.jpg", panorama);
#endif

                ConsoleLogHelper.State(
                    "EO PANORAMA / SAVE",
                    "Panorama saved / OUTPUT=" + outputPath +
                    " / SIZE=" + panorama.Width + "x" + panorama.Height +
                    " / ELAPSED_MS=" + totalStopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "EO PANORAMA / STITCH",
                    "Panorama processing failed / OUTPUT=" + outputPath,
                    ex);
                throw;
            }
            finally
            {
                panorama?.Dispose();

                foreach (List<Mat> row in frameRows)
                {
                    DisposeAll(row);
                }

                ConsoleLogHelper.Info(
                    "EO PANORAMA / STITCH",
                    "Panorama processing resources released / ELAPSED_MS=" +
                    totalStopwatch.ElapsedMilliseconds);
            }

        }

        /// <summary>
        /// 2026-09-09: 제공된 V21 R3의 검증된 2행 기본 경로를 보존한다.
        /// 같은 Pan의 Upper/Lower를 먼저 합치고 단 하나의 Stitcher 좌표계로 정합한다.
        /// </summary>
        private static Mat ComposeV21TwoRowColumnFirst(
            IList<Mat> upperFrames,
            IList<Mat> lowerFrames,
            out bool usedFixedAngleFallback)
        {
            usedFixedAngleFallback = false;
            if (upperFrames == null || lowerFrames == null ||
                upperFrames.Count != lowerFrames.Count || upperFrames.Count < 24)
                throw new InvalidOperationException(
                    "V21 2행 파노라마에는 동일 개수의 Upper/Lower 프레임이 필요합니다.");

            ValidateColumnPairFrameSizes(upperFrames, lowerFrames);
            int nominalOverlap = Math.Max(24, (int)Math.Round(
                Math.Min(upperFrames[0].Height, lowerFrames[0].Height) * 0.38));
            nominalOverlap = Math.Min(nominalOverlap,
                Math.Min(upperFrames[0].Height, lowerFrames[0].Height) - 1);

            int stableOverlap;
            int stableVerticalOffset;
            EstimateStableColumnPairGeometry(upperFrames, lowerFrames,
                nominalOverlap, out stableOverlap, out stableVerticalOffset);

            List<Mat> columns = new List<Mat>(upperFrames.Count);
            try
            {
                for (int index = 0; index < upperFrames.Count; index++)
                    columns.Add(MergeTiltPairAtSamePan(
                        upperFrames[index], lowerFrames[index],
                        stableOverlap, stableVerticalOffset));

                return StitchMatsWithFallback(
                    columns, "V21_TWO_ROW_COLUMN_FIRST",
                    out usedFixedAngleFallback, false);
            }
            finally
            {
                DisposeAll(columns);
            }

        }

        /// <summary>
        /// 같은 Pan index의 Upper/Lower Tilt Frame을 먼저 하나의 세로 Column으로 만든 뒤
        /// 36개의 Column을 Pan 순서대로 360° 정합한다.
        /// Column별 독립 Y 보정은 하지 않고 모든 Column에 공통 Geometry만 적용한다.
        /// </summary>
        private static Mat ComposeColumnFirstFullCircle(
            IList<List<Mat>> frameRows,
            string outputPath,
            out bool usedFixedAngleFallback)
        {
            usedFixedAngleFallback = false;

            if (frameRows == null ||
                frameRows.Count < 1 ||
                frameRows[0] == null ||
                frameRows[0].Count < 24 ||
                frameRows.Any(row => row == null || row.Count != frameRows[0].Count))
            {
                throw new InvalidOperationException(
                    "Column-first 파노라마에는 동일 개수의 N-Row Pan 프레임이 필요합니다.");
            }

            int frameCount =
                frameRows[0].Count;

            for (int rowIndex = 1; rowIndex < frameRows.Count; rowIndex++)
            {
                ValidateColumnPairFrameSizes(frameRows[0], frameRows[rowIndex]);
            }

            List<int> stableOverlaps = new List<int>(Math.Max(0, frameRows.Count - 1));
            List<int> stableVerticalOffsets = new List<int>(Math.Max(0, frameRows.Count - 1));
            List<double> stablePairExposureGains = new List<double>(Math.Max(0, frameRows.Count - 1));

            for (int rowIndex = 0; rowIndex < frameRows.Count - 1; rowIndex++)
            {
                IList<Mat> upperFrames = frameRows[rowIndex];
                IList<Mat> lowerFrames = frameRows[rowIndex + 1];
                int nominalOverlap = CalculateNominalVerticalOverlap(
                    Math.Min(upperFrames[0].Height, lowerFrames[0].Height),
                    frameRows.Count);
                nominalOverlap = Math.Min(
                    nominalOverlap,
                    Math.Min(upperFrames[0].Height, lowerFrames[0].Height) - 1);

                int stableOverlap;
                int stableVerticalOffset;
                EstimateStableColumnPairGeometry(
                    upperFrames,
                    lowerFrames,
                    nominalOverlap,
                    out stableOverlap,
                    out stableVerticalOffset);
                stableOverlaps.Add(stableOverlap);
                stableVerticalOffsets.Add(stableVerticalOffset);
                stablePairExposureGains.Add(EstimateStableRowExposureGain(
                    upperFrames, lowerFrames, stableOverlap));
#if DEBUG
                SavePanoramaDebugGeometry(outputPath, rowIndex, nominalOverlap,
                    stableOverlap, stableVerticalOffset);
#endif

                ConsoleLogHelper.State(
                    "EO PANORAMA / COLUMN",
                    "Shared adjacent-row geometry estimated" +
                    " / ROW_PAIR=" + (rowIndex + 1) + "-" + (rowIndex + 2) +
                    " / COLUMNS=" + frameCount +
                    " / NOMINAL_OVERLAP=" + nominalOverlap +
                    " / STABLE_OVERLAP=" + stableOverlap +
                    " / GLOBAL_Y_OFFSET_PX=" + stableVerticalOffset +
                    " / LOCAL_WARP=DISABLED");
            }

            Mat[] columns = new Mat[frameCount];
            double[] rowExposureGains = BuildCenterAnchoredRowGains(
                stablePairExposureGains, frameRows.Count);

            ConsoleLogHelper.State(
                "EO PANORAMA / EXPOSURE",
                "Global center-anchored row gains / VALUES=[" +
                string.Join(", ", rowExposureGains.Select(gain => gain.ToString("F3"))) + "]");

            try
            {
                // 독립 Pan column을 2개씩 병렬 처리해 144/180장 처리시간을 제한한다.
                System.Threading.Tasks.Parallel.For(0, frameCount,
                    new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 2 },
                    index =>
                {
                    List<Mat> normalizedRows = frameRows
                        .Select(row => row[index].Clone())
                        .ToList();
                    for (int rowIndex = 0; rowIndex < normalizedRows.Count; rowIndex++)
                        ApplyUniformExposureGain(normalizedRows[rowIndex], rowExposureGains[rowIndex]);

                    Mat column = normalizedRows[0].Clone();

                    try
                    {
                        for (int rowIndex = 1; rowIndex < frameRows.Count; rowIndex++)
                        {
                            Mat combined = MergeTiltRowIntoColumn(
                                column,
                                normalizedRows[rowIndex],
                                stableOverlaps[rowIndex - 1],
                                stableVerticalOffsets[rowIndex - 1]);
                            column.Dispose();
                            column = combined;
                        }

                        columns[index] = column;
#if DEBUG
                        if (index == 0 || index == 9 || index == 18 || index == 27)
                        {
                            SavePanoramaDebugMat(outputPath, "columns",
                                "column_" + index.ToString("D2") + ".jpg", column);
                        }
#endif
                        column = null;
                    }
                    finally
                    {
                        column?.Dispose();
                        DisposeAll(normalizedRows);
                    }

                    if (index == 0 ||
                        index == frameCount - 1 ||
                        index % 6 == 0)
                    {
                        ConsoleLogHelper.State(
                            "EO PANORAMA / COLUMN",
                            "Tilt column merged" +
                            " / INDEX=" + index +
                            " / ROWS=" + frameRows.Count +
                            " / PAN_STEP_INDEX=" + index +
                            " / RESULT=" + columns[index].Width + "x" +
                            columns[index].Height);
                    }

                });

                Stopwatch horizontalStopwatch = Stopwatch.StartNew();
                Mat panorama = StitchMatsWithFallback(
                    columns,
                    "SHARED_GLOBAL_COLUMN_" + frameRows.Count + "ROW",
                    out usedFixedAngleFallback,
                    false);
                horizontalStopwatch.Stop();
                ConsoleLogHelper.State(
                    "EO PANORAMA / PERFORMANCE",
                    "Shared-global multi-row feature composition completed" +
                    " / COLUMNS=" + columns.Length +
                    " / ELAPSED_MS=" + horizontalStopwatch.ElapsedMilliseconds);

                return panorama;
            }
            finally
            {
                DisposeAll(columns);
            }

        }

        private static int CalculateNominalVerticalOverlap(int frameHeight, int rowCount)
        {
            // 2 Row는 현장 검증된 기존 38% Geometry를 그대로 보존한다.
            if (rowCount <= 2) return Math.Max(24, (int)Math.Round(frameHeight * 0.38));

            double tiltStepDegrees = rowCount * 12.0 / (rowCount - 1);
            double overlapRatio = (LegacyVerticalAovDegrees - tiltStepDegrees) /
                                  LegacyVerticalAovDegrees;
            overlapRatio = Math.Max(0.45, Math.Min(0.72, overlapRatio));
            return Math.Max(24, (int)Math.Round(frameHeight * overlapRatio));
        }

        private static Mat MergeTiltRowIntoColumn(
            Mat accumulatedUpper,
            Mat nextRow,
            int overlap,
            int verticalOffset)
        {
            if (accumulatedUpper == null || nextRow == null ||
                accumulatedUpper.Empty() || nextRow.Empty() ||
                accumulatedUpper.Width != nextRow.Width ||
                accumulatedUpper.Type() != nextRow.Type())
            {
                throw new InvalidOperationException(
                    "N-Row Column-first 입력 Frame의 크기 또는 형식이 올바르지 않습니다.");
            }

            int safeOverlap = Math.Max(
                24,
                Math.Min(overlap, Math.Min(accumulatedUpper.Height, nextRow.Height) - 1));

            using (Mat shiftedNext = ShiftRowVertically(nextRow, verticalOffset))
            {
                // 각 원본 Row는 Center Anchor 기준으로 이미 한 번만 정규화되었다.
                return MergeRowsOnAdaptiveHorizontalSeam(
                    accumulatedUpper,
                    shiftedNext,
                    safeOverlap);
            }

        }

        /// <summary>
        /// Column-first에서 36개 Pan 방향 모두 같은 세로 geometry를 사용하도록
        /// 여러 방향에서 overlap/Y offset을 측정하고 중앙값 하나만 선택한다.
        /// 방향별 독립 보정값은 적용하지 않는다.
        /// </summary>
        private static void EstimateStableColumnPairGeometry(
            IList<Mat> upperFrames,
            IList<Mat> lowerFrames,
            int nominalOverlap,
            out int stableOverlap,
            out int stableVerticalOffset)
        {
            List<int> overlapSamples =
                new List<int>();

            int sampleStep =
                Math.Max(
                    1,
                    upperFrames.Count / 9);

            for (int index = 0;
                 index < upperFrames.Count;
                 index += sampleStep)
            {
                try
                {
                    int overlap =
                        EstimateVerticalRowOverlap(
                            upperFrames[index],
                            lowerFrames[index],
                            nominalOverlap);

                    overlapSamples.Add(overlap);
                }
                catch
                {
                    // 한 방향의 저대비/특징 부족은 전체 geometry 추정을 중단시키지 않는다.
                }

            }

            if (overlapSamples.Count < 3)
            {
                stableOverlap =
                    nominalOverlap;
            }
            else
            {
                overlapSamples.Sort();
                stableOverlap =
                    overlapSamples[overlapSamples.Count / 2];
            }

            List<int> offsetSamples =
                new List<int>();

            for (int index = 0;
                 index < upperFrames.Count;
                 index += sampleStep)
            {
                try
                {
                    int offset =
                        EstimateGlobalVerticalOffset(
                            upperFrames[index],
                            lowerFrames[index],
                            stableOverlap);

                    offsetSamples.Add(offset);
                }
                catch
                {
                    // Global Y는 방향별로 적용하지 않으므로 실패 샘플은 제외한다.
                }

            }

            if (offsetSamples.Count < 3)
            {
                stableVerticalOffset =
                    0;
            }
            else
            {
                offsetSamples.Sort();
                stableVerticalOffset =
                    offsetSamples[offsetSamples.Count / 2];
            }

            stableVerticalOffset =
                Math.Max(
                    -8,
                    Math.Min(
                        8,
                        stableVerticalOffset));

            string overlapSampleLog = string.Join(",", overlapSamples);
            string offsetSampleLog = string.Join(",", offsetSamples);
            int overlapMedian = stableOverlap;
            int offsetMedian = stableVerticalOffset;
            double overlapMad = overlapSamples.Count == 0 ? 0.0 :
                overlapSamples.Select(value => Math.Abs(value - overlapMedian)).OrderBy(value => value)
                    .ElementAt(overlapSamples.Count / 2);
            double offsetMad = offsetSamples.Count == 0 ? 0.0 :
                offsetSamples.Select(value => Math.Abs(value - offsetMedian)).OrderBy(value => value)
                    .ElementAt(offsetSamples.Count / 2);
            double sampleCoverage = Math.Min(overlapSamples.Count, offsetSamples.Count) /
                                    (double)Math.Max(1, (upperFrames.Count + sampleStep - 1) / sampleStep);
            double stability = 1.0 / (1.0 + overlapMad / 12.0 + offsetMad / 3.0);
            double confidence = Math.Max(0.0, Math.Min(1.0, sampleCoverage * stability));
            ConsoleLogHelper.State(
                "EO PANORAMA / ROW GEOMETRY",
                "VALID_SAMPLES=" + Math.Min(overlapSamples.Count, offsetSamples.Count) +
                " / OVERLAP_SAMPLES=[" + overlapSampleLog + "]" +
                " / OFFSET_SAMPLES=[" + offsetSampleLog + "]" +
                " / OVERLAP=" + stableOverlap +
                " / Y_OFFSET=" + stableVerticalOffset +
                " / OVERLAP_MAD=" + overlapMad.ToString("F1") +
                " / OFFSET_MAD=" + offsetMad.ToString("F1") +
                " / CONFIDENCE=" + confidence.ToString("F2") +
                " / FALLBACK=" + (overlapSamples.Count < 3 || offsetSamples.Count < 3));
        }

        /// <summary>
        /// 동일 Pan에서 촬영한 Upper/Lower 두 프레임을 하나의 세로 Column으로 합친다.
        /// Horizontal/Perspective/Local Warp 없이 공통 overlap + 공통 Y offset만 사용한다.
        /// </summary>
        private static Mat MergeTiltPairAtSamePan(
            Mat upper,
            Mat lower,
            int overlap,
            int verticalOffset)
        {
            if (upper == null ||
                lower == null ||
                upper.Empty() ||
                lower.Empty())
            {
                throw new InvalidOperationException(
                    "Column-first Tilt pair 입력 영상이 비어 있습니다.");
            }

            if (upper.Width != lower.Width ||
                upper.Height != lower.Height ||
                upper.Type() != lower.Type())
            {
                throw new InvalidOperationException(
                    "Column-first Upper/Lower Frame 크기 또는 형식이 서로 다릅니다.");
            }

            int safeOverlap =
                Math.Max(
                    30,
                    Math.Min(
                        overlap,
                        Math.Min(
                            upper.Height,
                            lower.Height) - 1));

            using (Mat shiftedLower =
                ShiftRowVertically(
                    lower,
                    verticalOffset))
            {
                /*
                 * 같은 Pan pair에서 exposure만 가볍게 맞춘다.
                 * geometry에는 영향을 주지 않는다.
                 */
                ApplyRowExposureGain(
                    upper,
                    shiftedLower,
                    safeOverlap);

                return
                    MergeRowsOnAdaptiveHorizontalSeam(
                        upper,
                        shiftedLower,
                        safeOverlap);
            }

        }

        /// <summary>
        /// Column-first 입력이 일정한 Frame geometry인지 확인한다.
        /// Fixed-angle fallback도 동일 크기 입력을 요구하므로 사전에 검증한다.
        /// </summary>
        private static void ValidateColumnPairFrameSizes(
            IList<Mat> upperFrames,
            IList<Mat> lowerFrames)
        {
            Mat reference =
                upperFrames[0];

            for (int index = 0;
                 index < upperFrames.Count;
                 index++)
            {
                Mat upper =
                    upperFrames[index];

                Mat lower =
                    lowerFrames[index];

                if (upper.Width != reference.Width ||
                    upper.Height != reference.Height ||
                    upper.Type() != reference.Type() ||
                    lower.Width != reference.Width ||
                    lower.Height != reference.Height ||
                    lower.Type() != reference.Type())
                {
                    throw new InvalidOperationException(
                        "Column-first 입력 Frame의 해상도/형식이 일정하지 않습니다. INDEX=" +
                        index);
                }

            }

        }

        /// <summary>
        /// 모든 행에 동일한 10도당 픽셀 폭과 동일한 시작 경도를 적용한다.
        /// 행마다 독립 Stitcher가 폭/배율을 바꾸지 않으므로 과거 Row-first의
        /// 수평 띠와 경도 불일치를 막고, Column-first의 선행 구조물 절단도 피한다.
        /// </summary>
        private static Mat ComposeSharedLongitudeRowFirstPanorama(
            IList<List<Mat>> frameRows,
            string outputPath)
        {
            if (frameRows == null || frameRows.Count < 2 ||
                frameRows.Any(row => row == null || row.Count != frameRows[0].Count || row.Count < 24))
                throw new InvalidOperationException(
                    "공통 경도 Row-first에는 동일 개수의 10도 간격 프레임이 필요합니다.");

            List<int> contributionCandidates = frameRows
                .Select((row, index) =>
                    EstimateFixedAngleContributionWidth(
                        row,
                        "SHARED_ROW_GEOMETRY=" + (index + 1)))
                .OrderBy(value => value)
                .ToList();

            int sharedContributionWidth = contributionCandidates[contributionCandidates.Count / 2];
            int centerRowIndex = (frameRows.Count - 1) / 2;

            double[] sharedPanGains = EstimateCyclicExposureGains(
                frameRows[centerRowIndex],
                sharedContributionWidth,
                sharedContributionWidth);

            List<double> pairRowGains = new List<double>(frameRows.Count - 1);
            List<int> sharedVerticalOverlaps = new List<int>(frameRows.Count - 1);
            List<int> sharedVerticalOffsets = new List<int>(frameRows.Count - 1);

            int exposureOverlap = CalculateNominalVerticalOverlap(
                frameRows[0][0].Height,
                frameRows.Count);

            for (int rowIndex = 0; rowIndex < frameRows.Count - 1; rowIndex++)
            {
                int stableOverlap;
                int stableOffset;

                EstimateStableColumnPairGeometry(
                    frameRows[rowIndex],
                    frameRows[rowIndex + 1],
                    exposureOverlap,
                    out stableOverlap,
                    out stableOffset);

                sharedVerticalOverlaps.Add(stableOverlap);
                sharedVerticalOffsets.Add(stableOffset);
                pairRowGains.Add(EstimateStableRowExposureGain(
                    frameRows[rowIndex],
                    frameRows[rowIndex + 1],
                    stableOverlap));
            }

            double[] sharedRowGains = BuildCenterAnchoredRowGains(
                pairRowGains,
                frameRows.Count);

            Mat[] stitchedRows = new Mat[frameRows.Count];
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                System.Threading.Tasks.Parallel.For(0, frameRows.Count,
                    new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 2 },
                    rowIndex =>
                    {
                        stitchedRows[rowIndex] = ComposeFixedAngleFullCircle(
                            frameRows[rowIndex],
                            "SHARED_ROW_FIRST=" + (rowIndex + 1),
                            "COMMON_10_DEGREE_GEOMETRY",
                            sharedContributionWidth,
                            sharedPanGains,
                            sharedRowGains[rowIndex]);
#if DEBUG
                        SavePanoramaDebugMat(outputPath, "rows",
                            "row_" + (rowIndex + 1).ToString("D2") + "_360.jpg",
                            stitchedRows[rowIndex]);
#endif
                    });

                if (stitchedRows.Any(row => row == null || row.Empty()) ||
                    stitchedRows.Any(row => row.Width != stitchedRows[0].Width))
                    throw new InvalidOperationException(
                        "행별 360도 결과가 동일한 공통 경도 폭을 유지하지 못했습니다.");

                Mat result = MergeSharedLongitudeRows(
                    stitchedRows,
                    sharedVerticalOverlaps,
                    sharedVerticalOffsets,
                    outputPath);

                ValidateFullCircleRow(
                    result,
                    frameRows[0],
                    "SHARED_LONGITUDE_ROW_FIRST_" + frameRows.Count + "ROW");

                stopwatch.Stop();

                ConsoleLogHelper.State(
                    "EO PANORAMA / PERFORMANCE",
                    "Shared-longitude Row-first composition completed" +
                    " / ROWS=" + frameRows.Count +
                    " / FRAMES=" + frameRows.Sum(row => row.Count) +
                    " / COMMON_CONTRIBUTION_PX=" + sharedContributionWidth +
                    " / CANDIDATES=[" + string.Join(",", contributionCandidates) + "]" +
                    " / ROW_GAINS=[" + string.Join(",",
                        sharedRowGains.Select(value => value.ToString("F3"))) + "]" +
                    " / OVERLAPS=[" + string.Join(",", sharedVerticalOverlaps) + "]" +
                    " / ELAPSED_MS=" + stopwatch.ElapsedMilliseconds);
#if DEBUG
                SavePanoramaDebugMat(outputPath, "row_first", "shared_row_first.jpg", result);
#endif
                return result;
            }
            finally
            {
                DisposeAll(stitchedRows);
            }

        }

        /// <summary>
        /// 단일 행도 36개 입력 전체와 명시적인 10도 경도를 사용한다.
        /// </summary>
        private static Mat ComposeSharedLongitudeSingleRow(IList<Mat> frames)
        {
            int contributionWidth = EstimateFixedAngleContributionWidth(
                frames, "SHARED_SINGLE_ROW_GEOMETRY");
            return ComposeFixedAngleFullCircle(frames, "SHARED_SINGLE_ROW",
                "EXPLICIT_10_DEGREE_GEOMETRY", contributionWidth);
        }

        private static Mat MergeSharedLongitudeRows(
            IList<Mat> rows,
            IList<int> overlaps,
            IList<int> verticalOffsets,
            string outputPath)
        {
            if (rows == null || rows.Count < 2 ||
                overlaps == null || overlaps.Count != rows.Count - 1 ||
                verticalOffsets == null || verticalOffsets.Count != rows.Count - 1 ||
                rows.Any(row => row == null || row.Empty() || row.Width != rows[0].Width))
                throw new InvalidOperationException(
                    "공통 경도 세로 결합 입력 또는 원본 기반 Geometry가 올바르지 않습니다.");

            Mat result = rows[0].Clone();
            try
            {
                for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
                {
                    int overlap = Math.Max(24, Math.Min(overlaps[rowIndex - 1],
                        Math.Min(result.Height, rows[rowIndex].Height) - 1));
                    using (Mat alignedNext = ShiftRowVertically(
                        rows[rowIndex], verticalOffsets[rowIndex - 1]))
                    {
                        Mat combined = MergeRowsOnAdaptiveHorizontalSeam(
                            result, alignedNext, overlap, outputPath, rowIndex - 1);
#if DEBUG
                        SavePanoramaDebugMat(outputPath, "merged_rows",
                            "through_row_" + (rowIndex + 1).ToString("D2") + ".jpg",
                            combined);
#endif
                        result.Dispose();
                        result = combined;
                    }

                }

                return result.Clone();
            }
            finally
            {
                result.Dispose();
            }

        }

        /// <summary>
        /// StitchMatsWithFallback 동작 수행 함수.
        /// </summary>
        private static Mat StitchMatsWithFallback(
            IList<Mat> frames,
            string stage,
            out bool usedFixedAngleFallback,
            bool allowFixedAngleFallback = true)
        {
            usedFixedAngleFallback = false;

            Mat panorama =
                new Mat();

            IList<Mat> keyFrames =
                SelectAngleSpacedKeyFrames(frames);

            try
            {
                ConsoleLogHelper.State(
                    "EO PANORAMA / STITCH",
                    stage + " / Angle-aware key frames selected" +
                    " / INPUT=" + frames.Count +
                    " / USED=" + keyFrames.Count +
                    " / CAPTURE_STEP=10deg" +
                    " / STITCH_STEP=" +
                    (keyFrames.Count == frames.Count
                        ? "10deg"
                        : keyFrames.Count >= 24
                            ? "10/20deg DENSE"
                            : "20deg"));

                Stitcher.Status panoramaStatus;

                try
                {
                    panoramaStatus =
                        RunStitcherCpuSafe(
                            keyFrames,
                            panorama,
                            Stitcher.Mode.Panorama);
                }
                catch (Exception stitchException)
                when (stitchException is OpenCVException ||
                      stitchException is InvalidOperationException)
                {
                    if (!allowFixedAngleFallback)
                        throw new InvalidOperationException(
                            stage + " 특징점 파노라마 정합 중 예외가 발생했습니다.",
                            stitchException);

                    ConsoleLogHelper.Warning(
                        "EO PANORAMA / STITCH",
                        stage + " / Panorama pipeline exception; " +
                        "switching to fixed-angle full-circle fallback" +
                        " / TYPE=" + stitchException.GetType().Name +
                        " / MESSAGE=" + stitchException.Message);

                    panorama.Dispose();
                    panorama =
                        ComposeFixedAngleFullCircle(
                            frames,
                            stage,
                            stitchException.GetType().Name);

                    usedFixedAngleFallback = true;

                    ValidateFullCircleRow(
                        panorama,
                        frames,
                        stage + " / FALLBACK");

                    return panorama.Clone();
                }

                ConsoleLogHelper.State(
                    "EO PANORAMA / STITCH",
                    stage + " / MODE=Panorama / STATUS=" + panoramaStatus +
                    " / EMPTY=" + panorama.Empty());

                if (panoramaStatus != Stitcher.Status.OK ||
                    panorama.Empty())
                {
                    if (!allowFixedAngleFallback)
                        throw new InvalidOperationException(
                            stage + " 특징점 파노라마 정합 실패: " + panoramaStatus);

                    ConsoleLogHelper.Warning(
                        "EO PANORAMA / STITCH",
                        stage + " / 360-degree Panorama mode failed; " +
                        "switching to fixed-angle full-circle fallback" +
                        " / STATUS=" + panoramaStatus);

                    panorama.Dispose();
                    panorama =
                        ComposeFixedAngleFullCircle(
                            frames,
                            stage,
                            panoramaStatus.ToString());

                    usedFixedAngleFallback = true;

                    ValidateFullCircleRow(
                        panorama,
                        frames,
                        stage + " / FALLBACK");

                    return panorama.Clone();
                }

                using (Mat cropped = CropOuterBlackBorder(panorama))
                {
                    try
                    {
                        ValidateFullCircleRow(
                            cropped,
                            keyFrames,
                            stage);
                    }
                    catch (InvalidOperationException validationException)
                    {
                        if (!allowFixedAngleFallback)
                            throw;

                        ConsoleLogHelper.Warning(
                            "EO PANORAMA / STITCH",
                            stage + " / Feature panorama coverage rejected; " +
                            "switching to fixed-angle full-circle fallback" +
                            " / REASON=" + validationException.Message);

                        using (Mat fallback =
                            ComposeFixedAngleFullCircle(
                                frames,
                                stage,
                                "COVERAGE_REJECTED"))
                        {
                            usedFixedAngleFallback = true;

                            ValidateFullCircleRow(
                                fallback,
                                frames,
                                stage + " / FALLBACK");

                            return fallback.Clone();
                        }

                    }

                    return cropped.Clone();
                }

            }
            finally
            {
                panorama.Dispose();
            }

        }

        /// <summary>
        /// PTZ가 10° 간격으로 촬영한 36장을 모두 Stitcher에 넣으면 처리시간이 크게
        /// 증가하고, 2장마다 1장(20°)만 쓰면 근거리 사다리/기둥처럼 parallax가 큰
        /// 구조물에서 내부 seam이 물체를 가르는 문제가 생긴다.
        /// 따라서 3장 중 2장을 사용하여 10°/20° 간격이 번갈아 나오도록 24장을
        /// 선택한다. 18장보다 인접 중첩 정보를 늘리되 36장 전체 정합은 피한다.
        /// </summary>
        private static IList<Mat> SelectAngleSpacedKeyFrames(
            IList<Mat> frames)
        {
            if (frames == null || frames.Count < 30)
            {
                return frames;
            }

            /*
             * 근거리 난간/사다리/건물 모서리는 20° 간격만 연속되면
             * parallax 때문에 seam 선택이 어려워진다.
             * 36장 중 24장(0,1,3,4,6,7...)을 사용하여
             * 10°/20° 간격을 번갈아 유지한다.
             *
             * 시간은 Stitcher 해상도를 더 낮춰 상쇄한다.
             */
            List<Mat> selected =
                new List<Mat>((frames.Count * 2 + 2) / 3);

            for (int index = 0; index < frames.Count; index++)
            {
                if (index % 3 != 2)
                {
                    selected.Add(frames[index]);
                }

            }

            return selected;
        }

        /// <summary>
        /// 특징점 기반 카메라 파라미터 추정이 실패해도 10° 간격의 PTZ 촬영 순서를
        /// 이용해 전체 36개 방향을 빠짐없이 합성한다. 각 프레임의 왜곡이 가장 작은
        /// 중앙 10° 영역을 사용하고 경계는 adaptive feathering하여 Scans 모드처럼
        /// 일부 방향만 남는 결과를 만들지 않는다.
        /// </summary>
        private static Mat ComposeFixedAngleFullCircle(
            IList<Mat> frames,
            string stage,
            string reason)
        {
            return ComposeFixedAngleFullCircle(frames, stage, reason, 0);
        }

        private static Mat ComposeFixedAngleFullCircle(
            IList<Mat> frames,
            string stage,
            string reason,
            int forcedContributionWidth)
        {
            return ComposeFixedAngleFullCircle(
                frames, stage, reason, forcedContributionWidth, null, 1.0);
        }

        private static Mat ComposeFixedAngleFullCircle(
            IList<Mat> frames,
            string stage,
            string reason,
            int forcedContributionWidth,
            double[] sharedPanGains,
            double rowGain)
        {
            if (frames == null || frames.Count < 24)
            {
                throw new InvalidOperationException(
                    "고정각 360도 복구에는 10도 간격 촬영 프레임이 필요합니다.");
            }

            Mat reference = frames[0];
            int maximumContributionWidth = Math.Max(24, (reference.Width - 2) / 3);
            int contributionWidth = forcedContributionWidth > 0
                ? Math.Max(24, Math.Min(maximumContributionWidth, forcedContributionWidth))
                : EstimateFixedAngleContributionWidth(frames, stage);

            // 2026-09-08: 출력 Pan strip 전체를 실제 좌/우 overlap으로 합성한다.
            // 일부 경계만 blend하고 중앙 strip을 그대로 복사할 때 생긴 사각형을 제거한다.
            int blendWidth = contributionWidth;

            double[] cyclicExposureGains =
                sharedPanGains != null && sharedPanGains.Length == frames.Count
                    ? sharedPanGains
                    : EstimateCyclicExposureGains(frames, contributionWidth, blendWidth);
            List<Mat> normalizedFrames = frames.Select(frame => frame.Clone()).ToList();
            for (int index = 0; index < normalizedFrames.Count; index++)
                ApplyUniformExposureGain(normalizedFrames[index], Math.Max(0.70,
                    Math.Min(1.35, cyclicExposureGains[index] * rowGain)));
            reference = normalizedFrames[0];

            ConsoleLogHelper.State(
                "EO PANORAMA / EXPOSURE",
                stage + " / Cyclic Pan gains / VALUES=[" +
                string.Join(", ", cyclicExposureGains.Select(gain => gain.ToString("F3"))) + "]");

            int outputWidth =
                contributionWidth * frames.Count;

            Mat result =
                new Mat(
                    reference.Height,
                    outputWidth,
                    reference.Type(),
                    Scalar.Black);

            try
            {
                for (int index = 0;
                     index < frames.Count;
                     index++)
                {
                    Mat frame = normalizedFrames[index];

                    if (frame.Width != reference.Width ||
                        frame.Height != reference.Height ||
                        frame.Type() != reference.Type())
                    {
                        throw new InvalidOperationException(
                            "고정각 360도 복구 입력 프레임의 크기 또는 형식이 서로 다릅니다.");
                    }

                    int sourceStart =
                        (frame.Width - contributionWidth) / 2;

                    using (Mat sourceStrip = new Mat(
                        frame,
                        new Rect(
                            sourceStart,
                            0,
                            contributionWidth,
                            frame.Height)))
                    using (Mat targetStrip = new Mat(
                        result,
                        new Rect(
                            index * contributionWidth,
                            0,
                            contributionWidth,
                            result.Height)))
                    {
                        sourceStrip.CopyTo(targetStrip);
                    }

                }

                for (int currentIndex = 0;
                     currentIndex < frames.Count;
                     currentIndex++)
                {
                    int previousIndex =
                        (currentIndex - 1 + frames.Count) % frames.Count;

                    Mat previousFrame = normalizedFrames[previousIndex];
                    Mat currentFrame = normalizedFrames[currentIndex];
                    int centerX = reference.Width / 2;
                    int previousStart =
                        centerX + contributionWidth / 2;
                    int currentStart =
                        centerX - contributionWidth / 2;

                    using (Mat previousEdge = new Mat(
                        previousFrame,
                        new Rect(
                            previousStart,
                            0,
                            blendWidth,
                            reference.Height)))
                    using (Mat currentEdge = new Mat(
                        currentFrame,
                        new Rect(
                            currentStart,
                            0,
                            blendWidth,
                            reference.Height)))
                    using (Mat blendedEdge =
                        BlendOnAdaptiveVerticalSeam(
                            previousEdge,
                            currentEdge))
                    using (Mat targetEdge = new Mat(
                        result,
                        new Rect(
                            currentIndex * contributionWidth,
                            0,
                            blendWidth,
                            result.Height)))
                    {
                        blendedEdge.CopyTo(targetEdge);
                    }

                }

                ConsoleLogHelper.State(
                    "EO PANORAMA / FALLBACK",
                    stage + " / Fixed-angle full-circle fallback completed" +
                    " / REASON=" + reason +
                    " / FRAMES=" + frames.Count +
                    " / CONTRIBUTION_PX=" + contributionWidth +
                    " / ADAPTIVE_SEAM_PX=" + blendWidth +
                    " / RESULT=" + result.Width + "x" + result.Height);

                return result.Clone();
            }
            finally
            {
                result.Dispose();
                DisposeAll(normalizedFrames);
            }

        }

        // 2026-09-08: 동일 장면인 인접 Pan overlap만 비교하여 실제 장면의 명암은
        // 보존하고 프레임별 자동노출 차이만 원형(cyclic) 최소 드리프트로 보정한다.
        private static double[] EstimateCyclicExposureGains(
            IList<Mat> frames, int contributionWidth, int blendWidth)
        {
            int count = frames.Count;
            double[] transitions = new double[count];
            int centerX = frames[0].Width / 2;
            int previousStart = centerX + contributionWidth / 2;
            int currentStart = centerX - contributionWidth / 2;
            int sampleWidth = Math.Max(8, Math.Min(blendWidth, frames[0].Width - previousStart));

            for (int current = 0; current < count; current++)
            {
                int previous = (current - 1 + count) % count;
                using (Mat previousRoi = new Mat(frames[previous],
                    new Rect(previousStart, 0, sampleWidth, frames[previous].Height)))
                using (Mat currentRoi = new Mat(frames[current],
                    new Rect(currentStart, 0, sampleWidth, frames[current].Height)))
                using (Mat previousGray = new Mat())
                using (Mat currentGray = new Mat())
                {
                    Cv2.CvtColor(previousRoi, previousGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(currentRoi, currentGray, ColorConversionCodes.BGR2GRAY);
                    double previousMean = Math.Max(12.0, Cv2.Mean(previousGray).Val0);
                    double currentMean = Math.Max(12.0, Cv2.Mean(currentGray).Val0);
                    transitions[current] = Math.Log(Math.Max(0.88,
                        Math.Min(1.14, previousMean / currentMean)));
                }

            }

            double[] logGains = new double[count];
            for (int index = 1; index < count; index++)
                logGains[index] = logGains[index - 1] + transitions[index];

            double closureError = logGains[count - 1] + transitions[0];
            for (int index = 0; index < count; index++)
                logGains[index] -= closureError * index / count;

            // 장면 내용 차이로 발생한 단일 Pan 보정 급변은 원형 저역통과로 억제한다.
            for (int pass = 0; pass < 2; pass++)
            {
                double[] smoothed = new double[count];
                for (int index = 0; index < count; index++)
                    smoothed[index] = (logGains[(index - 1 + count) % count] +
                        2.0 * logGains[index] + logGains[(index + 1) % count]) / 4.0;
                logGains = smoothed;
            }
            double meanLog = logGains.Average();

            return logGains.Select(value => Math.Max(0.78,
                Math.Min(1.28, Math.Exp(value - meanLog)))).ToArray();
        }

        /// <summary>
        /// 인접 촬영 프레임의 실제 영상 이동량을 분석하여 10°당 출력 폭을 계산한다.
        /// </summary>
        private static int EstimateFixedAngleContributionWidth(
            IList<Mat> frames,
            string stage)
        {
            const int AnalysisWidth = 480;
            const double MinimumConfidence = 0.25;

            List<int> estimatedShifts =
                new List<int>();

            for (int index = 1;
                 index < frames.Count;
                 index++)
            {
                Mat previous = frames[index - 1];
                Mat current = frames[index];
                int analysisHeight =
                    Math.Max(
                        120,
                        (int)Math.Round(
                            previous.Height *
                            AnalysisWidth /
                            (double)previous.Width));

                using (Mat previousSmall = new Mat())
                using (Mat currentSmall = new Mat())
                using (Mat previousGray = new Mat())
                using (Mat currentGray = new Mat())
                {
                    Cv2.Resize(
                        previous,
                        previousSmall,
                        new Size(AnalysisWidth, analysisHeight),
                        0,
                        0,
                        InterpolationFlags.Area);

                    Cv2.Resize(
                        current,
                        currentSmall,
                        new Size(AnalysisWidth, analysisHeight),
                        0,
                        0,
                        InterpolationFlags.Area);

                    Cv2.CvtColor(
                        previousSmall,
                        previousGray,
                        ColorConversionCodes.BGR2GRAY);

                    Cv2.CvtColor(
                        currentSmall,
                        currentGray,
                        ColorConversionCodes.BGR2GRAY);

                    int roiY = analysisHeight / 5;
                    int roiHeight = analysisHeight * 3 / 5;
                    int templateX = AnalysisWidth * 3 / 10;
                    int templateWidth = AnalysisWidth * 4 / 10;

                    using (Mat previousRoi = new Mat(
                        previousGray,
                        new Rect(0, roiY, AnalysisWidth, roiHeight)))
                    using (Mat currentRoi = new Mat(
                        currentGray,
                        new Rect(0, roiY, AnalysisWidth, roiHeight)))
                    using (Mat template = new Mat(
                        previousRoi,
                        new Rect(
                            templateX,
                            0,
                            templateWidth,
                            roiHeight)))
                    using (Mat matchResult = new Mat())
                    {
                        Cv2.MeanStdDev(
                            template,
                            out _,
                            out Scalar templateStandardDeviation);

                        if (templateStandardDeviation.Val0 < 8.0)
                        {
                            continue;
                        }

                        Cv2.MatchTemplate(
                            currentRoi,
                            template,
                            matchResult,
                            TemplateMatchModes.CCoeffNormed);

                        Cv2.MinMaxLoc(
                            matchResult,
                            out _,
                            out double maximumValue,
                            out _,
                            out Point maximumLocation);

                        int analysisShift =
                            Math.Abs(
                                templateX -
                                maximumLocation.X);

                        if (!double.IsNaN(maximumValue) &&
                            maximumValue >= MinimumConfidence &&
                            analysisShift >= AnalysisWidth / 40 &&
                            analysisShift <= AnalysisWidth / 3)
                        {
                            estimatedShifts.Add(
                                (int)Math.Round(
                                    analysisShift *
                                    previous.Width /
                                    (double)AnalysisWidth));
                        }

                    }

                }

            }

            int fallbackWidth =
                frames[0].Width / 10;

            int contributionWidth =
                estimatedShifts.Count == 0
                    ? fallbackWidth
                    : estimatedShifts
                        .OrderBy(value => value)
                        .ElementAt(estimatedShifts.Count / 2);

            contributionWidth =
                Math.Max(
                    frames[0].Width / 12,
                    Math.Min(
                        frames[0].Width / 5,
                        contributionWidth));

            ConsoleLogHelper.State(
                "EO PANORAMA / FALLBACK",
                stage + " / Actual 10-degree image displacement estimated" +
                " / VALID_PAIRS=" + estimatedShifts.Count +
                " / CONTRIBUTION_PX=" + contributionWidth +
                " / DEFAULT_PX=" + fallbackWidth);

            return contributionWidth;
        }

        /// <summary>
        /// 두 겹침 영상의 차이가 가장 작은 수직 경로를 찾고 좁은 범위만 feathering한다.
        /// </summary>
        private static Mat BlendOnAdaptiveVerticalSeam(
            Mat previous,
            Mat current)
        {
            int[] seam =
                FindLowCostVerticalSeam(
                    previous,
                    current);

            // 2026-09-09: 수직 Pan 경계만 더 깊은 피라미드로 혼합한다.
            // 고주파 구조(난간/건물)는 seam 가까이에서 전환하고, 저주파 밝기/감마는
            // 더 넓게 완화하여 확대 시 보이는 직사각형 프레임 경계를 줄인다.
            using (Mat previousTransposed = new Mat())
            using (Mat currentTransposed = new Mat())
            using (Mat blendedTransposed = new Mat())
            {
                Cv2.Transpose(previous, previousTransposed);
                Cv2.Transpose(current, currentTransposed);
                using (Mat multiBand = BlendRowsMultiBand(
                    previousTransposed,
                    currentTransposed,
                    seam,
                    6,
                    16))
                {
                    Cv2.Transpose(multiBand, blendedTransposed);
                    return blendedTransposed.Clone();
                }

            }

        }

        /// <summary>
        /// 영상 구조물을 가로지르는 절단선을 피하도록 위에서 아래로 최소 비용 seam을 찾는다.
        /// </summary>
        private static int[] FindLowCostVerticalSeam(
            Mat previous,
            Mat current)
        {
            using (Mat difference = new Mat())
            using (Mat grayDifference = new Mat())
            using (Mat previousGray = new Mat())
            using (Mat currentGray = new Mat())
            using (Mat previousEdges = new Mat())
            using (Mat currentEdges = new Mat())
            using (Mat combinedEdges = new Mat())
            using (Mat parallaxMask = new Mat())
            {
                Cv2.Absdiff(
                    previous,
                    current,
                    difference);

                Cv2.CvtColor(
                    difference,
                    grayDifference,
                    ColorConversionCodes.BGR2GRAY);

                Cv2.GaussianBlur(
                    grayDifference,
                    grayDifference,
                    new Size(3, 3),
                    0);

                // 2026-09-10 R23: 근거리 난간/옥상 경계의 잔여 시차 영역을 넓게
                // 보호하여 수직 Pan seam이 구조의 중간을 가르는 경우를 줄인다.
                Cv2.Threshold(grayDifference, parallaxMask, 24, 255,
                    ThresholdTypes.Binary);
                using (Mat parallaxKernel = Cv2.GetStructuringElement(
                    MorphShapes.Rect, new Size(15, 15)))
                {
                    Cv2.Dilate(parallaxMask, parallaxMask, parallaxKernel);
                }
                Cv2.AddWeighted(grayDifference, 1.0, parallaxMask, 1.25, 0.0,
                    grayDifference);

                // 사다리/안테나/건물 외곽처럼 강한 구조를 seam이 직접
                // 통과하지 않도록 양쪽 영상의 edge 주변에 보호 비용을 준다.
                Cv2.CvtColor(previous, previousGray, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(current, currentGray, ColorConversionCodes.BGR2GRAY);
                Cv2.Canny(previousGray, previousEdges, 55, 140);
                Cv2.Canny(currentGray, currentEdges, 55, 140);

                using (Mat kernel = Cv2.GetStructuringElement(
                    MorphShapes.Rect,
                    new Size(9, 9)))
                {
                    Cv2.Dilate(previousEdges, previousEdges, kernel);
                    Cv2.Dilate(currentEdges, currentEdges, kernel);
                }

                Cv2.Max(previousEdges, currentEdges, combinedEdges);
                Cv2.AddWeighted(
                    grayDifference,
                    1.0,
                    combinedEdges,
                    1.75,
                    0.0,
                    grayDifference);

                grayDifference.GetArray(out byte[] costs);

                int width = grayDifference.Width;
                int height = grayDifference.Height;
                int stableSeamAnchor = FindStableVerticalSeamAnchor(
                    costs, width, height);
                int[] previousRow = new int[width];
                int[] currentRow = new int[width];
                sbyte[] parentDirections =
                    new sbyte[width * height];

                for (int x = 0; x < width; x++)
                {
                    previousRow[x] =
                        costs[x] +
                        Math.Abs(x - stableSeamAnchor) / 8;
                }

                for (int y = 1; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bestPreviousX = x;
                        int bestCost = previousRow[x];

                        if (x > 0 &&
                            previousRow[x - 1] + 10 < bestCost)
                        {
                            bestCost = previousRow[x - 1] + 10;
                            bestPreviousX = x - 1;
                        }

                        if (x + 1 < width &&
                            previousRow[x + 1] + 10 < bestCost)
                        {
                            bestCost = previousRow[x + 1] + 10;
                            bestPreviousX = x + 1;
                        }

                        currentRow[x] =
                            bestCost +
                            costs[y * width + x] +
                            Math.Abs(x - stableSeamAnchor) / 8;

                        parentDirections[y * width + x] =
                            (sbyte)(bestPreviousX - x);
                    }

                    int[] swap = previousRow;
                    previousRow = currentRow;
                    currentRow = swap;
                }

                int endX = 0;
                for (int x = 1; x < width; x++)
                {
                    if (previousRow[x] < previousRow[endX])
                    {
                        endX = x;
                    }

                }

                int[] seam = new int[height];
                seam[height - 1] = endX;

                for (int y = height - 1; y > 0; y--)
                {
                    seam[y - 1] =
                        seam[y] +
                        parentDirections[y * width + seam[y]];
                }

                int[] smoothed = new int[height];
                const int smoothingRadius = 12;
                long windowSum = 0;
                int windowStart = 0;
                int windowEnd = -1;
                for (int y = 0; y < height; y++)
                {
                    int desiredStart = Math.Max(0, y - smoothingRadius);
                    int desiredEnd = Math.Min(height - 1, y + smoothingRadius);
                    while (windowEnd < desiredEnd)
                    {
                        windowEnd++;
                        windowSum += seam[windowEnd];
                    }
                    while (windowStart < desiredStart)
                    {
                        windowSum -= seam[windowStart];
                        windowStart++;
                    }
                    smoothed[y] = Math.Max(1, Math.Min(width - 2,
                        (int)Math.Round(windowSum /
                            (double)(windowEnd - windowStart + 1))));
                }

                return smoothed;
            }

        }

        private static int FindStableVerticalSeamAnchor(
            byte[] costs,
            int width,
            int height)
        {
            if (costs == null || costs.Length < width * height ||
                width <= 2 || height <= 0)
                return Math.Max(1, width / 2);

            double[] columnCosts = new double[width];
            int sampleStep = Math.Max(1, height / 800);
            int sampleCount = (height + sampleStep - 1) / sampleStep;
            for (int x = 0; x < width; x++)
            {
                long sum = 0;
                for (int y = 0; y < height; y += sampleStep)
                    sum += costs[y * width + x];
                columnCosts[x] = sum / (double)Math.Max(1, sampleCount);
            }

            int radius = Math.Max(3, width / 24);
            int minimumX = Math.Max(radius + 1, (int)Math.Round(width * 0.15));
            int maximumX = Math.Min(width - radius - 2,
                (int)Math.Round(width * 0.85));
            if (minimumX > maximumX) return width / 2;

            int bestX = width / 2;
            double bestCost = double.MaxValue;
            for (int x = minimumX; x <= maximumX; x++)
            {
                double sum = 0.0;
                for (int offset = -radius; offset <= radius; offset++)
                    sum += columnCosts[x + offset];
                double score = sum / (radius * 2 + 1) +
                               Math.Abs(x - width / 2) / 40.0;
                if (score < bestCost)
                {
                    bestCost = score;
                    bestX = x;
                }

            }
            return bestX;
        }

        /// <summary>
        /// ValidateFullCircleRow 상태 확인 함수.
        /// </summary>
        private static void ValidateFullCircleRow(
            Mat panorama,
            IList<Mat> sourceFrames,
            string stage)
        {
            int sourceWidth =
                sourceFrames.Count == 0
                    ? 0
                    : sourceFrames[0].Width;

            int sourceHeight =
                sourceFrames.Count == 0
                    ? 0
                    : sourceFrames[0].Height;

            double aspectRatio =
                panorama.Height <= 0
                    ? 0.0
                    : panorama.Width / (double)panorama.Height;

            // 2026-09-09: 3~5행은 세로 화각 증가로 결과 높이가 커지므로
            // Width/Height를 수평 360° 범위 판정에 사용하지 않는다.
            // OpenCV STATUS=OK 이후 최소 수평 폭은 계속 엄격하게 검사한다.
            bool isSharedLongitudeRowFirst =
                !string.IsNullOrWhiteSpace(stage) &&
                stage.IndexOf("SHARED_LONGITUDE_ROW_FIRST_", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasVariableMultiRowHeight =
                !string.IsNullOrWhiteSpace(stage) &&
                (isSharedLongitudeRowFirst ||
                 stage.IndexOf("_3ROW", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 stage.IndexOf("_4ROW", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 stage.IndexOf("_5ROW", StringComparison.OrdinalIgnoreCase) >= 0);

            /*
             * 2026-09-09: CompositingResol은 전체 합성 픽셀 예산을 제한하므로
             * 3→4→5행으로 Column 높이가 증가하면 정상 360° 결과의 폭은
             * 높이 비율의 제곱근에 반비례해 감소한다.
             *
             * 실제 측정:
             * 3행 Column 1920x2078 → Panorama 3377x610
             * 4행 Column 1920x2523 → Panorama 3077x652
             *
             * 따라서 고정 폭이나 행별 magic number 대신 sqrt(W/H)로 기준을
             * 연속 보정한다. 2400px 절대 하한은 부분 파노라마 저장을 막는다.
             */
            double multiRowWidthScale =
                hasVariableMultiRowHeight && sourceHeight > 0
                    ? Math.Sqrt(sourceWidth / (double)sourceHeight)
                    : 1.0;

            int minimumWidth =
                Math.Max(
                    hasVariableMultiRowHeight ? 2400 : 3000,
                    (int)Math.Round(
                        sourceWidth * 1.75 * multiRowWidthScale));

            bool aspectRatioPassed =
                hasVariableMultiRowHeight ||
                aspectRatio >= 5.5;

            bool isFullCircle =
                panorama.Width >= minimumWidth &&
                aspectRatioPassed;

            double legacyExpectedWidth =
                sourceWidth /
                LegacyPanAovDegrees *
                360.0;

            double legacyWidthRatio =
                legacyExpectedWidth <= 1.0
                    ? 1.0
                    : panorama.Width /
                      legacyExpectedWidth;

            ConsoleLogHelper.State(
                "EO PANORAMA / VALIDATE",
                stage + " / Full-circle coverage validation" +
                " / RESULT=" + panorama.Width + "x" + panorama.Height +
                " / ASPECT=" + aspectRatio.ToString("F2") +
                " / ASPECT_GATE=" +
                (hasVariableMultiRowHeight ? "INFORMATIONAL_MULTIROW" : "MIN_5.5") +
                " / SOURCE_COLUMN=" + sourceWidth + "x" + sourceHeight +
                " / WIDTH_SCALE=" + multiRowWidthScale.ToString("F3") +
                " / MIN_WIDTH=" + minimumWidth +
                " / PASS=" + isFullCircle);

            if (!isFullCircle)
            {
                throw new InvalidOperationException(
                    "360도 전체 범위를 충족하지 못한 부분 파노라마가 생성되어 " +
                    "저장을 중단했습니다. (" + stage +
                    ", 결과 " + panorama.Width + "x" + panorama.Height + ")");
            }

        }

        /// <summary>
        /// 상/하 Tilt 행의 실제 겹침 높이를 축소 구조 영상으로 빠르게 추정한다.
        /// 기존 38% 고정값을 중심으로 제한된 범위만 탐색하므로 처리시간 증가를
        /// 억제하면서 Tilt 기준점 변화로 생기는 수평 seam 절단을 완화한다.
        /// </summary>
        private static int EstimateVerticalRowOverlap(
            Mat upper,
            Mat lower,
            int nominalOverlap)
        {
            const int AnalysisWidth = 420;
            const double MinimumOverlapRatio = 0.26;
            // N-Row의 명목 overlap은 3/4/5행에서 약 58/62/65%이다.
            // 과거 56% 상한은 실제 후보를 잘라 동일 난간과 옥상 구조가 두 번 남았다.
            const double MaximumOverlapRatio = 0.72;

            int minimumHeight =
                Math.Min(upper.Height, lower.Height);

            if (minimumHeight < 80)
            {
                return Math.Max(
                    24,
                    Math.Min(nominalOverlap, minimumHeight - 1));
            }

            double scale =
                AnalysisWidth / (double)Math.Max(upper.Width, lower.Width);

            int upperAnalysisHeight =
                Math.Max(80, (int)Math.Round(upper.Height * scale));
            int lowerAnalysisHeight =
                Math.Max(80, (int)Math.Round(lower.Height * scale));

            using (Mat upperGray = new Mat())
            using (Mat lowerGray = new Mat())
            using (Mat upperSmall = new Mat())
            using (Mat lowerSmall = new Mat())
            using (Mat upperStructure = new Mat())
            using (Mat lowerStructure = new Mat())
            {
                Cv2.CvtColor(upper, upperGray, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(lower, lowerGray, ColorConversionCodes.BGR2GRAY);
                Cv2.Resize(
                    upperGray,
                    upperSmall,
                    new Size(AnalysisWidth, upperAnalysisHeight),
                    0,
                    0,
                    InterpolationFlags.Area);
                Cv2.Resize(
                    lowerGray,
                    lowerSmall,
                    new Size(AnalysisWidth, lowerAnalysisHeight),
                    0,
                    0,
                    InterpolationFlags.Area);

                BuildStructuralMap(upperSmall, upperStructure);
                BuildStructuralMap(lowerSmall, lowerStructure);

                upperStructure.GetArray(out byte[] upperPixels);
                lowerStructure.GetArray(out byte[] lowerPixels);

                int analysisMinimumHeight =
                    Math.Min(upperAnalysisHeight, lowerAnalysisHeight);

                int nominalAnalysisOverlap =
                    Math.Max(
                        8,
                        (int)Math.Round(
                            nominalOverlap *
                            analysisMinimumHeight /
                            (double)minimumHeight));

                int minimumOverlap =
                    Math.Max(
                        12,
                        (int)Math.Round(
                            analysisMinimumHeight * MinimumOverlapRatio));
                int maximumOverlap =
                    Math.Min(
                        analysisMinimumHeight - 4,
                        (int)Math.Round(
                            analysisMinimumHeight * MaximumOverlapRatio));

                double bestScore = double.MaxValue;
                int bestOverlap = nominalAnalysisOverlap;
                int marginX = AnalysisWidth / 12;

                for (int candidate = minimumOverlap;
                     candidate <= maximumOverlap;
                     candidate += 2)
                {
                    long differenceCost = 0;
                    long structureEnergy = 0;
                    int sampleCount = 0;

                    int upperStart = upperAnalysisHeight - candidate;

                    for (int y = 2; y < candidate - 2; y += 2)
                    {
                        int upperOffset = (upperStart + y) * AnalysisWidth;
                        int lowerOffset = y * AnalysisWidth;

                        for (int x = marginX; x < AnalysisWidth - marginX; x += 4)
                        {
                            int upperValue = upperPixels[upperOffset + x];
                            int lowerValue = lowerPixels[lowerOffset + x];

                            differenceCost += Math.Abs(upperValue - lowerValue);
                            structureEnergy += Math.Max(upperValue, lowerValue);
                            sampleCount++;
                        }

                    }

                    if (sampleCount == 0)
                    {
                        continue;
                    }

                    double averageDifference =
                        differenceCost / (double)sampleCount;
                    double averageEnergy =
                        structureEnergy / (double)sampleCount;

                    // 구조 정보가 거의 없는 하늘/평탄 영역은 정합 근거로 약하게 본다.
                    double lowTexturePenalty =
                        averageEnergy < 10.0
                            ? (10.0 - averageEnergy) * 2.5
                            : 0.0;

                    // 약한 prior를 두어 저대비 장면에서 overlap이 과도하게 튀는 것을 방지한다.
                    double nominalPenalty =
                        Math.Abs(candidate - nominalAnalysisOverlap) * 0.10;

                    double score =
                        averageDifference +
                        lowTexturePenalty +
                        nominalPenalty;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestOverlap = candidate;
                    }

                }

                int estimatedOverlap =
                    (int)Math.Round(
                        bestOverlap *
                        minimumHeight /
                        (double)analysisMinimumHeight);

                int minimumFullOverlap =
                    Math.Max(24, (int)Math.Round(minimumHeight * MinimumOverlapRatio));
                int maximumFullOverlap =
                    Math.Min(
                        minimumHeight - 1,
                        (int)Math.Round(minimumHeight * MaximumOverlapRatio));

                return Math.Max(
                    minimumFullOverlap,
                    Math.Min(maximumFullOverlap, estimatedOverlap));
            }

        }

        /// <summary>
        /// 밝기 변화보다 건물 윤곽/사다리/안테나 같은 구조를 우선하도록
        /// Sobel X/Y 경사도를 결합한 저비용 구조 영상을 만든다.
        /// </summary>
        private static void BuildStructuralMap(
            Mat gray,
            Mat destination)
        {
            using (Mat blurred = new Mat())
            using (Mat gradientX16 = new Mat())
            using (Mat gradientY16 = new Mat())
            using (Mat gradientX = new Mat())
            using (Mat gradientY = new Mat())
            {
                Cv2.GaussianBlur(gray, blurred, new Size(3, 3), 0);
                Cv2.Sobel(
                    blurred,
                    gradientX16,
                    MatType.CV_16SC1,
                    1,
                    0,
                    3);
                Cv2.Sobel(
                    blurred,
                    gradientY16,
                    MatType.CV_16SC1,
                    0,
                    1,
                    3);
                Cv2.ConvertScaleAbs(gradientX16, gradientX);
                Cv2.ConvertScaleAbs(gradientY16, gradientY);
                Cv2.AddWeighted(
                    gradientX,
                    0.5,
                    gradientY,
                    0.5,
                    0.0,
                    destination);
            }

        }

        /// <summary>
        /// 상/하 Tilt overlap의 중앙 구조를 축소 비교하여 lower row에 적용할
        /// 단일 Y offset을 찾는다. Local warp를 하지 않으므로 울렁임을 만들지 않는다.
        /// </summary>
        private static int EstimateGlobalVerticalOffset(
            Mat upper,
            Mat lower,
            int overlap)
        {
            if (upper == null || lower == null ||
                upper.Empty() || lower.Empty() ||
                overlap < 48)
            {
                return 0;
            }

            const int AnalysisWidth = 640;
            const int MaxFullOffset = 8;

            int analysisHeight =
                Math.Max(
                    72,
                    (int)Math.Round(
                        overlap * AnalysisWidth /
                        (double)upper.Width));

            double yScale =
                overlap / (double)analysisHeight;

            int maxOffset =
                Math.Max(
                    2,
                    (int)Math.Ceiling(
                        MaxFullOffset / yScale));

            using (Mat upperRoi = new Mat(
                upper,
                new Rect(
                    0,
                    upper.Height - overlap,
                    upper.Width,
                    overlap)))
            using (Mat lowerRoi = new Mat(
                lower,
                new Rect(
                    0,
                    0,
                    lower.Width,
                    overlap)))
            using (Mat upperGray = new Mat())
            using (Mat lowerGray = new Mat())
            using (Mat upperSmall = new Mat())
            using (Mat lowerSmall = new Mat())
            using (Mat upperGy16 = new Mat())
            using (Mat lowerGy16 = new Mat())
            using (Mat upperGy = new Mat())
            using (Mat lowerGy = new Mat())
            {
                Cv2.CvtColor(
                    upperRoi,
                    upperGray,
                    ColorConversionCodes.BGR2GRAY);

                Cv2.CvtColor(
                    lowerRoi,
                    lowerGray,
                    ColorConversionCodes.BGR2GRAY);

                Cv2.Resize(
                    upperGray,
                    upperSmall,
                    new Size(
                        AnalysisWidth,
                        analysisHeight),
                    0,
                    0,
                    InterpolationFlags.Area);

                Cv2.Resize(
                    lowerGray,
                    lowerSmall,
                    new Size(
                        AnalysisWidth,
                        analysisHeight),
                    0,
                    0,
                    InterpolationFlags.Area);

                // Horizontal structures: railing tops / roof lines / horizon.
                Cv2.Sobel(
                    upperSmall,
                    upperGy16,
                    MatType.CV_16SC1,
                    0,
                    1,
                    3);

                Cv2.Sobel(
                    lowerSmall,
                    lowerGy16,
                    MatType.CV_16SC1,
                    0,
                    1,
                    3);

                Cv2.ConvertScaleAbs(
                    upperGy16,
                    upperGy);

                Cv2.ConvertScaleAbs(
                    lowerGy16,
                    lowerGy);

                upperGy.GetArray(
                    out byte[] upperPixels);

                lowerGy.GetArray(
                    out byte[] lowerPixels);

                long bestCost =
                    long.MaxValue;

                int bestOffset = 0;
                int marginX = AnalysisWidth / 12;
                int marginY = Math.Max(5, analysisHeight / 10);

                for (int candidate = -maxOffset;
                     candidate <= maxOffset;
                     candidate++)
                {
                    long cost = 0;
                    int samples = 0;

                    for (int y = marginY;
                         y < analysisHeight - marginY;
                         y += 2)
                    {
                        int lowerY = y + candidate;

                        if (lowerY < marginY ||
                            lowerY >= analysisHeight - marginY)
                        {
                            continue;
                        }

                        int upperRow = y * AnalysisWidth;
                        int lowerRow = lowerY * AnalysisWidth;

                        for (int x = marginX;
                             x < AnalysisWidth - marginX;
                             x += 3)
                        {
                            int a = upperPixels[upperRow + x];
                            int b = lowerPixels[lowerRow + x];

                            if (Math.Max(a, b) < 28)
                            {
                                continue;
                            }

                            cost += Math.Abs(a - b);
                            samples++;
                        }

                    }

                    if (samples == 0)
                    {
                        continue;
                    }

                    cost /= samples;

                    /*
                     * C++ legacy의 Tilt/AOV 철학을 약한 prior로 사용:
                     * 이미 row overlap을 찾은 뒤이므로 큰 Y 이동보다 0 근처를 우선.
                     */
                    cost += (long)(
                        Math.Abs(candidate) *
                        Math.Max(
                            1.0,
                            LegacyVerticalAovDegrees / 42.5));

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestOffset = candidate;
                    }

                }

                int fullOffset =
                    (int)Math.Round(
                        bestOffset * yScale);

                return Math.Max(
                    -MaxFullOffset,
                    Math.Min(
                        MaxFullOffset,
                        fullOffset));
            }

        }

        /// <summary>
        /// lower row 전체를 동일 Y offset으로 평행 이동한다.
        /// Perspective/Remap/구간별 warp를 사용하지 않는다.
        /// </summary>
        private static Mat ShiftRowVertically(
            Mat source,
            int offset)
        {
            if (source == null ||
                source.Empty() ||
                offset == 0)
            {
                return source.Clone();
            }

            int safeOffset =
                Math.Max(
                    -source.Height + 2,
                    Math.Min(
                        source.Height - 2,
                        offset));

            Mat shifted =
                new Mat(
                    source.Size(),
                    source.Type(),
                    Scalar.Black);

            int sourceY =
                safeOffset < 0
                    ? -safeOffset
                    : 0;

            int targetY =
                safeOffset > 0
                    ? safeOffset
                    : 0;

            int copyHeight =
                source.Height -
                Math.Abs(safeOffset);

            using (Mat sourceRoi =
                new Mat(
                    source,
                    new Rect(
                        0,
                        sourceY,
                        source.Width,
                        copyHeight)))
            using (Mat targetRoi =
                new Mat(
                    shifted,
                    new Rect(
                        0,
                        targetY,
                        source.Width,
                        copyHeight)))
            {
                sourceRoi.CopyTo(
                    targetRoi);
            }

            return shifted;
        }

        /// <summary>
        /// 상/하 row overlap의 평균 밝기만 가볍게 맞춘다.
        /// 기하를 건드리지 않고 gain은 ±6%로 제한한다.
        /// </summary>
        // 2026-09-08: Brown-Lowe gain compensation 원칙을 Row 전체의 공통 gain으로 제한 적용한다.
        // Column마다 서로 다른 gain을 적용하면 360° 결과에 세로 밝기 띠가 생기므로 중앙값 하나만 사용한다.
        private static double EstimateStableRowExposureGain(
            IList<Mat> upperFrames, IList<Mat> lowerFrames, int overlap)
        {
            List<double> gains = new List<double>();
            int sampleStep = Math.Max(1, upperFrames.Count / 12);
            for (int index = 0; index < upperFrames.Count; index += sampleStep)
            {
                Mat upper = upperFrames[index];
                Mat lower = lowerFrames[index];
                int safeOverlap = Math.Min(overlap, Math.Min(upper.Height, lower.Height) - 1);
                int margin = Math.Max(4, safeOverlap / 8);
                int sampleHeight = Math.Max(1, safeOverlap - margin * 2);
                using (Mat upperRoi = new Mat(upper,
                    new Rect(0, upper.Height - safeOverlap + margin, upper.Width, sampleHeight)))
                using (Mat lowerRoi = new Mat(lower,
                    new Rect(0, margin, lower.Width, sampleHeight)))
                using (Mat upperGray = new Mat())
                using (Mat lowerGray = new Mat())
                {
                    Cv2.CvtColor(upperRoi, upperGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(lowerRoi, lowerGray, ColorConversionCodes.BGR2GRAY);
                    double upperMean = Cv2.Mean(upperGray).Val0;
                    double lowerMean = Cv2.Mean(lowerGray).Val0;
                    if (upperMean >= 12.0 && lowerMean >= 12.0)
                        gains.Add(upperMean / lowerMean);
                }

            }

            if (gains.Count < 3) return 1.0;
            gains.Sort();
            return Math.Max(0.85, Math.Min(1.15, gains[gains.Count / 2]));
        }

        private static double[] BuildCenterAnchoredRowGains(
            IList<double> pairGains, int rowCount)
        {
            double[] result = Enumerable.Repeat(1.0, rowCount).ToArray();
            int center = (rowCount - 1) / 2;
            for (int row = center + 1; row < rowCount; row++)
                result[row] = Math.Max(0.80, Math.Min(1.25,
                    result[row - 1] * pairGains[row - 1]));
            for (int row = center - 1; row >= 0; row--)
                result[row] = Math.Max(0.80, Math.Min(1.25,
                    result[row + 1] / pairGains[row]));
            return result;
        }

        private static void ApplyUniformExposureGain(Mat image, double gain)
        {
            if (image == null || image.Empty() || Math.Abs(gain - 1.0) < 0.002) return;
            image.ConvertTo(image, image.Type(), gain, 0.0);
        }

        private static void ApplyRowExposureGain(
            Mat upper,
            Mat lower,
            int overlap)
        {
            if (upper == null || lower == null ||
                upper.Empty() || lower.Empty() ||
                overlap < 24)
            {
                return;
            }

            int margin =
                Math.Max(
                    4,
                    overlap / 8);

            int sampleHeight =
                Math.Max(
                    1,
                    overlap -
                    margin * 2);

            using (Mat upperRoi =
                new Mat(
                    upper,
                    new Rect(
                        0,
                        upper.Height - overlap + margin,
                        upper.Width,
                        sampleHeight)))
            using (Mat lowerRoi =
                new Mat(
                    lower,
                    new Rect(
                        0,
                        margin,
                        lower.Width,
                        sampleHeight)))
            using (Mat upperGray = new Mat())
            using (Mat lowerGray = new Mat())
            {
                Cv2.CvtColor(
                    upperRoi,
                    upperGray,
                    ColorConversionCodes.BGR2GRAY);

                Cv2.CvtColor(
                    lowerRoi,
                    lowerGray,
                    ColorConversionCodes.BGR2GRAY);

                double upperMean =
                    Cv2.Mean(
                        upperGray).Val0;

                double lowerMean =
                    Cv2.Mean(
                        lowerGray).Val0;

                if (upperMean < 12.0 ||
                    lowerMean < 12.0)
                {
                    return;
                }

                double gain =
                    upperMean /
                    lowerMean;

                gain =
                    Math.Max(
                        0.96,
                        Math.Min(
                            1.04,
                            gain));

                if (Math.Abs(gain - 1.0) >= 0.004)
                {
                    lower.ConvertTo(
                        lower,
                        lower.Type(),
                        gain,
                        0.0);
                }

            }

        }

#if DEBUG
        private static string GetPanoramaDebugRoot(string outputPath)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(directory)) directory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(directory, "panorama_debug");
        }

        private static void SavePanoramaDebugCapture(IList<List<Mat>> rows, string outputPath)
        {
            try
            {
                string directory = Path.Combine(GetPanoramaDebugRoot(outputPath), "capture");
                Directory.CreateDirectory(directory);
                for (int row = 0; row < rows.Count; row++)
                    for (int pan = 0; pan < rows[row].Count; pan++)
                        Cv2.ImWrite(Path.Combine(directory,
                            "row_" + row.ToString("D2") + "_pan_" + pan.ToString("D3") + ".jpg"),
                            rows[row][pan]);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("EO PANORAMA / DEBUG", "Capture debug save skipped / " + ex.Message);
            }

        }

        private static void SavePanoramaDebugMat(string outputPath, string folder, string name, Mat image)
        {
            if (image == null || image.Empty()) return;
            try
            {
                string directory = Path.Combine(GetPanoramaDebugRoot(outputPath), folder);
                Directory.CreateDirectory(directory);
                Cv2.ImWrite(Path.Combine(directory, name), image,
                    new ImageEncodingParam(ImwriteFlags.JpegQuality, 92));
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("EO PANORAMA / DEBUG", "Intermediate debug save skipped / " + ex.Message);
            }

        }

        private static void SavePanoramaDebugGeometry(string outputPath, int pairIndex,
            int nominalOverlap, int stableOverlap, int verticalOffset)
        {
            try
            {
                string directory = Path.Combine(GetPanoramaDebugRoot(outputPath), "geometry");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,
                    "row_pair_" + (pairIndex + 1) + "_" + (pairIndex + 2) + ".txt"),
                    "PAIR=" + (pairIndex + 1) + "-" + (pairIndex + 2) + Environment.NewLine +
                    "NOMINAL_OVERLAP=" + nominalOverlap + Environment.NewLine +
                    "STABLE_OVERLAP=" + stableOverlap + Environment.NewLine +
                    "GLOBAL_Y_OFFSET_PX=" + verticalOffset + Environment.NewLine +
                    "LOCAL_WARP=DISABLED" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("EO PANORAMA / DEBUG", "Geometry debug save skipped / " + ex.Message);
            }

        }
#endif

        /// <summary>
        /// 상·하단 행에서 영상 차이와 구조물 윤곽 비용이 가장 작은 수평 이음선을
        /// 선택하고 제한된 폭만 smooth feathering하여 절단과 이중 흐림을 줄인다.
        /// </summary>
        private static Mat MergeRowsOnAdaptiveHorizontalSeam(
            Mat upper,
            Mat lower,
            int overlap,
            string debugOutputPath = null,
            int debugPairIndex = -1)
        {
            int[] seam =
                FindLowCostSeam(
                    upper,
                    lower,
                    overlap,
                    debugOutputPath,
                    debugPairIndex);

            Mat combined =
                new Mat(
                    upper.Height + lower.Height - overlap,
                    upper.Width,
                    MatType.CV_8UC3,
                    Scalar.Black);

            using (Mat upperTarget = new Mat(
                combined,
                new Rect(0, 0, upper.Width, upper.Height)))
            {
                upper.CopyTo(upperTarget);
            }

            int lowerTailHeight =
                lower.Height - overlap;

            if (lowerTailHeight > 0)
            {
                using (Mat lowerTail = new Mat(
                    lower,
                    new Rect(0, overlap, lower.Width, lowerTailHeight)))
                using (Mat lowerTarget = new Mat(
                    combined,
                    new Rect(0, upper.Height, lower.Width, lowerTailHeight)))
                {
                    lowerTail.CopyTo(lowerTarget);
                }

            }

            using (Mat upperOverlap = new Mat(upper,
                new Rect(0, upper.Height - overlap, upper.Width, overlap)))
            using (Mat lowerOverlap = new Mat(lower,
                new Rect(0, 0, lower.Width, overlap)))
            using (Mat blendedOverlap = BlendRowsMultiBand(upperOverlap, lowerOverlap, seam))
            using (Mat overlapTarget = new Mat(combined,
                new Rect(0, upper.Height - overlap, upper.Width, overlap)))
            {
                blendedOverlap.CopyTo(overlapTarget);
            }

            return combined;
        }

        /// <summary>
        /// 2026-09-08: Burt-Adelson multiresolution spline을 세로 Row overlap에 적용한다.
        /// 고주파 디테일은 seam 가까이에서 전환하고 저주파 밝기 차이는 넓게 완화한다.
        /// </summary>
        private static Mat BlendRowsMultiBand(Mat upper, Mat lower, int[] seam)
        {
            // 2026-09-09 R19: 행 사이의 저주파 밝기 차가 적응형 seam 모양을 따라
            // 대각선으로 드러나지 않도록 Pan 경계와 같은 깊이까지 완화한다.
            // Laplacian 고주파 대역은 여전히 좁은 seam을 사용하므로 구조물 형상은 보존한다.
            return BlendRowsMultiBand(upper, lower, seam, 6, 16);
        }

        private static Mat BlendRowsMultiBand(
            Mat upper,
            Mat lower,
            int[] seam,
            int maximumLevels,
            int minimumPyramidSize)
        {
            maximumLevels = Math.Max(1, maximumLevels);
            minimumPyramidSize = Math.Max(8, minimumPyramidSize);
            List<Mat> upperGaussian = new List<Mat>();
            List<Mat> lowerGaussian = new List<Mat>();
            List<Mat> maskGaussian = new List<Mat>();
            List<Mat> upperLaplacian = new List<Mat>();
            List<Mat> lowerLaplacian = new List<Mat>();
            List<Mat> blendedLevels = new List<Mat>();
            Mat reconstructed = null;
            try
            {
                Mat upperFloat = new Mat();
                Mat lowerFloat = new Mat();
                upper.ConvertTo(upperFloat, MatType.CV_32FC3, 1.0 / 255.0);
                lower.ConvertTo(lowerFloat, MatType.CV_32FC3, 1.0 / 255.0);
                upperGaussian.Add(upperFloat); lowerGaussian.Add(lowerFloat);

                float[] weights = new float[upper.Width * upper.Height];
                int radius = Math.Max(4, Math.Min(12, upper.Height / 48));
                for (int x = 0; x < upper.Width; x++)
                {
                    int seamY = Math.Max(radius + 1, Math.Min(upper.Height - radius - 1, seam[x]));
                    for (int y = 0; y < upper.Height; y++)
                    {
                        double value = (seamY + radius - y) / (double)(radius * 2);
                        value = Math.Max(0.0, Math.Min(1.0, value));
                        weights[y * upper.Width + x] = (float)(value * value * (3.0 - 2.0 * value));
                    }

                }
                Mat baseMask = new Mat(upper.Height, upper.Width, MatType.CV_32FC1);
                baseMask.SetArray(weights); maskGaussian.Add(baseMask);

                int levels = 1;
                while (levels < maximumLevels && Math.Min(
                           upperGaussian[levels - 1].Width, upperGaussian[levels - 1].Height) >= minimumPyramidSize)
                {
                    Mat nextUpper = new Mat();
                    Mat nextLower = new Mat();
                    Mat nextMask = new Mat();
                    Cv2.PyrDown(upperGaussian[levels - 1], nextUpper);
                    Cv2.PyrDown(lowerGaussian[levels - 1], nextLower);
                    Cv2.PyrDown(maskGaussian[levels - 1], nextMask);
                    upperGaussian.Add(nextUpper); lowerGaussian.Add(nextLower); maskGaussian.Add(nextMask);
                    levels++;
                }

                for (int level = 0; level < levels - 1; level++)
                {
                    Mat expandedUpper = new Mat();
                    Mat expandedLower = new Mat();
                    Cv2.PyrUp(upperGaussian[level + 1], expandedUpper, upperGaussian[level].Size());
                    Cv2.PyrUp(lowerGaussian[level + 1], expandedLower, lowerGaussian[level].Size());
                    Mat upperBand = new Mat();
                    Mat lowerBand = new Mat();
                    Cv2.Subtract(upperGaussian[level], expandedUpper, upperBand);
                    Cv2.Subtract(lowerGaussian[level], expandedLower, lowerBand);
                    expandedUpper.Dispose(); expandedLower.Dispose();
                    upperLaplacian.Add(upperBand); lowerLaplacian.Add(lowerBand);
                }
                upperLaplacian.Add(upperGaussian[levels - 1].Clone());
                lowerLaplacian.Add(lowerGaussian[levels - 1].Clone());

                for (int level = 0; level < levels; level++)
                {
                    using (Mat mask3 = new Mat())
                    using (Mat ones = Mat.Ones(maskGaussian[level].Size(), MatType.CV_32FC1))
                    using (Mat inverseMask = new Mat())
                    using (Mat inverseMask3 = new Mat())
                    using (Mat weightedUpper = new Mat())
                    using (Mat weightedLower = new Mat())
                    {
                        Cv2.Merge(new[] { maskGaussian[level], maskGaussian[level], maskGaussian[level] }, mask3);
                        Cv2.Subtract(ones, maskGaussian[level], inverseMask);
                        Cv2.Merge(new[] { inverseMask, inverseMask, inverseMask }, inverseMask3);
                        Cv2.Multiply(upperLaplacian[level], mask3, weightedUpper);
                        Cv2.Multiply(lowerLaplacian[level], inverseMask3, weightedLower);
                        Mat blended = new Mat();
                        Cv2.Add(weightedUpper, weightedLower, blended);
                        blendedLevels.Add(blended);
                    }

                }

                reconstructed = blendedLevels[levels - 1].Clone();
                for (int level = levels - 2; level >= 0; level--)
                {
                    Mat expanded = new Mat();
                    Cv2.PyrUp(reconstructed, expanded, blendedLevels[level].Size());
                    reconstructed.Dispose(); reconstructed = new Mat();
                    Cv2.Add(expanded, blendedLevels[level], reconstructed); expanded.Dispose();
                }
                Mat output = new Mat();
                reconstructed.ConvertTo(output, MatType.CV_8UC3, 255.0);
                return output;
            }
            finally
            {
                reconstructed?.Dispose(); DisposeAll(upperGaussian); DisposeAll(lowerGaussian);
                DisposeAll(maskGaussian); DisposeAll(upperLaplacian); DisposeAll(lowerLaplacian);
                DisposeAll(blendedLevels);
            }

        }

        /// <summary>
        /// FindLowCostSeam 조회 함수.
        /// </summary>
        private static int[] FindLowCostSeam(
            Mat upper,
            Mat lower,
            int overlap,
            string debugOutputPath = null,
            int debugPairIndex = -1)
        {
            int analysisWidth =
                Math.Min(1600, upper.Width);
            int analysisHeight =
                Math.Min(256, overlap);

            using (Mat upperOverlap = new Mat(
                upper,
                new Rect(0, upper.Height - overlap, upper.Width, overlap)))
            using (Mat lowerOverlap = new Mat(
                lower,
                new Rect(0, 0, lower.Width, overlap)))
            using (Mat upperSmall = new Mat())
            using (Mat lowerSmall = new Mat())
            using (Mat difference = new Mat())
            using (Mat grayDifference = new Mat())
            using (Mat upperGray = new Mat())
            using (Mat lowerGray = new Mat())
            using (Mat upperEdges = new Mat())
            using (Mat lowerEdges = new Mat())
            using (Mat parallaxMask = new Mat())
            {
                Cv2.Resize(
                    upperOverlap,
                    upperSmall,
                    new Size(analysisWidth, analysisHeight));
                Cv2.Resize(
                    lowerOverlap,
                    lowerSmall,
                    new Size(analysisWidth, analysisHeight));
                Cv2.Absdiff(upperSmall, lowerSmall, difference);
                Cv2.CvtColor(
                    difference,
                    grayDifference,
                    ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(
                    grayDifference,
                    grayDifference,
                    new Size(5, 5),
                    0);

                /*
                 * PARALLAX PROTECTION
                 *
                 * 난간/옥상/건물처럼 상·하 Tilt에서 위치가 달라진 구조는
                 * 두 영상의 차이 영역이 얇은 edge가 아니라 "띠" 형태로 생긴다.
                 * 단순 edge 비용만 주면 seam이 그 두 구조 사이를 지나면서
                 * 위쪽 구조와 아래쪽 구조가 동시에 남아 이중상처럼 보일 수 있다.
                 *
                 * 따라서 큰 차이 영역을 넓게 보호하여 seam이 그 구조 자체를
                 * 가르지 않고 위/아래의 한쪽 배경으로 우회하도록 한다.
                 */
                Cv2.Threshold(
                    grayDifference,
                    parallaxMask,
                    24,
                    255,
                    ThresholdTypes.Binary);

                using (Mat parallaxKernel =
                    Cv2.GetStructuringElement(
                        MorphShapes.Rect,
                        new Size(11, 17)))
                {
                    Cv2.Dilate(
                        parallaxMask,
                        parallaxMask,
                        parallaxKernel);
                }

                Cv2.AddWeighted(
                    grayDifference,
                    1.0,
                    parallaxMask,
                    1.15,
                    0.0,
                    grayDifference);

                Cv2.CvtColor(
                    upperSmall,
                    upperGray,
                    ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(
                    lowerSmall,
                    lowerGray,
                    ColorConversionCodes.BGR2GRAY);
                Cv2.Canny(upperGray, upperEdges, 55, 140);
                Cv2.Canny(lowerGray, lowerEdges, 55, 140);

                using (Mat kernel = Cv2.GetStructuringElement(
                    MorphShapes.Rect,
                    new Size(7, 7)))
                {
                    Cv2.Dilate(upperEdges, upperEdges, kernel);
                    Cv2.Dilate(lowerEdges, lowerEdges, kernel);
                }

                Cv2.AddWeighted(
                    grayDifference,
                    1.0,
                    upperEdges,
                    1.35,
                    0.0,
                    grayDifference);
                Cv2.AddWeighted(
                    grayDifference,
                    1.0,
                    lowerEdges,
                    1.35,
                    0.0,
                    grayDifference);

                grayDifference.GetArray(out byte[] costPixels);

                double[] previous = new double[analysisHeight];
                double[] current = new double[analysisHeight];
                sbyte[] directions =
                    new sbyte[analysisWidth * analysisHeight];

                for (int y = 0; y < analysisHeight; y++)
                {
                    previous[y] = costPixels[y * analysisWidth];
                }

                for (int x = 1; x < analysisWidth; x++)
                {
                    for (int y = 0; y < analysisHeight; y++)
                    {
                        int bestPreviousY = y;
                        double bestPreviousCost = previous[y];

                        if (y > 0 && previous[y - 1] + 14.0 < bestPreviousCost)
                        {
                            bestPreviousCost = previous[y - 1] + 14.0;
                            bestPreviousY = y - 1;
                        }

                        if (y + 1 < analysisHeight &&
                            previous[y + 1] + 14.0 < bestPreviousCost)
                        {
                            bestPreviousCost = previous[y + 1] + 14.0;
                            bestPreviousY = y + 1;
                        }

                        // 겹침 중앙을 향한 약한 prior로 저텍스처 바닥/하늘에서 seam이
                        // 장거리 대각선으로 표류하는 현상만 억제한다. 구조물 회피 비용보다
                        // 충분히 작아서 난간/건물 보호 경로는 그대로 우선한다.
                        current[y] =
                            bestPreviousCost +
                            costPixels[y * analysisWidth + x] +
                            Math.Abs(y - analysisHeight / 2) / 16.0;
                        directions[x * analysisHeight + y] =
                            (sbyte)(bestPreviousY - y);
                    }

                    double[] swap = previous;
                    previous = current;
                    current = swap;
                }

                int bestY = 0;
                for (int y = 1; y < analysisHeight; y++)
                {
                    if (previous[y] < previous[bestY])
                    {
                        bestY = y;
                    }

                }

                int[] reducedSeam = new int[analysisWidth];
                reducedSeam[analysisWidth - 1] = bestY;

                for (int x = analysisWidth - 1; x > 0; x--)
                {
                    bestY += directions[x * analysisHeight + bestY];
                    reducedSeam[x - 1] = bestY;
                }

                // 축소 좌표에서 약 33열 이동 평균을 적용해 짧은 지그재그와 꺾임을 제거한다.
                // 긴 구조물을 피하기 위한 완만한 경로 변화는 보존한다.
                int[] smoothedSeam = new int[analysisWidth];
                const int seamSmoothingRadius = 16;
                long seamWindowSum = 0;
                int seamWindowStart = 0;
                int seamWindowEnd = -1;
                for (int x = 0; x < analysisWidth; x++)
                {
                    int desiredStart = Math.Max(0, x - seamSmoothingRadius);
                    int desiredEnd = Math.Min(analysisWidth - 1, x + seamSmoothingRadius);
                    while (seamWindowEnd < desiredEnd)
                    {
                        seamWindowEnd++;
                        seamWindowSum += reducedSeam[seamWindowEnd];
                    }
                    while (seamWindowStart < desiredStart)
                    {
                        seamWindowSum -= reducedSeam[seamWindowStart];
                        seamWindowStart++;
                    }
                    smoothedSeam[x] = (int)Math.Round(
                        seamWindowSum / (double)(seamWindowEnd - seamWindowStart + 1));
                }
                reducedSeam = smoothedSeam;

                int[] fullSeam = new int[upper.Width];

                for (int x = 0; x < upper.Width; x++)
                {
                    int reducedX =
                        Math.Min(
                            analysisWidth - 1,
                            (int)(x * analysisWidth / (double)upper.Width));

                    fullSeam[x] =
                        Math.Max(
                            1,
                            Math.Min(
                                overlap - 2,
                                (int)Math.Round(
                                    reducedSeam[reducedX] *
                                    overlap / (double)analysisHeight)));
                }

#if DEBUG
                if (!string.IsNullOrWhiteSpace(debugOutputPath) && debugPairIndex >= 0)
                {
                    string prefix = "pair_" + (debugPairIndex + 1) + "_" +
                                    (debugPairIndex + 2) + "_";
                    SavePanoramaDebugMat(debugOutputPath, "seams", prefix + "upper_overlap.jpg", upperSmall);
                    SavePanoramaDebugMat(debugOutputPath, "seams", prefix + "lower_overlap.jpg", lowerSmall);
                    SavePanoramaDebugMat(debugOutputPath, "seams", prefix + "difference_cost.jpg", grayDifference);
                    SavePanoramaDebugMat(debugOutputPath, "seams", prefix + "parallax_mask.jpg", parallaxMask);
                    using (Mat seamVisualization = new Mat())
                    {
                        Cv2.CvtColor(grayDifference, seamVisualization, ColorConversionCodes.GRAY2BGR);
                        for (int x = 1; x < analysisWidth; x++)
                            Cv2.Line(seamVisualization,
                                new Point(x - 1, reducedSeam[x - 1]),
                                new Point(x, reducedSeam[x]),
                                new Scalar(0, 0, 255), 2);
                        SavePanoramaDebugMat(debugOutputPath, "seams", prefix + "selected_seam.jpg", seamVisualization);
                    }

                }
#endif

                return fullSeam;
            }

        }

        /// <summary>
        /// SaveAndConvert 저장 함수.
        /// </summary>
        private static BitmapSource SaveAndConvert(
            Mat panorama,
            string outputPath)
        {
            string directory =
                Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!Cv2.ImWrite(
                outputPath,
                panorama,
                new ImageEncodingParam(
                    ImwriteFlags.JpegQuality,
                    97)))
            {
                throw new IOException(
                    "파노라마 JPG 파일 저장에 실패했습니다.");
            }

            BitmapSource bitmap =
                MatToBitmapSourceConverter.Convert(panorama);

            if (bitmap != null &&
                bitmap.CanFreeze &&
                !bitmap.IsFrozen)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }

        /// <summary>
        /// 2026-08-18: OpenCL Command Queue 오류 방지를 위한 Process 단위 설정.
        /// 변경된 실행 파일은 완전히 종료한 뒤 다시 실행해야 적용된다.
        /// </summary>
        private static void DisableOpenClForCurrentProcess()
        {
            Environment.SetEnvironmentVariable(
                "OPENCV_OPENCL_RUNTIME",
                "disabled",
                EnvironmentVariableTarget.Process);

            Environment.SetEnvironmentVariable(
                "OPENCV_OPENCL_DEVICE",
                "disabled",
                EnvironmentVariableTarget.Process);

            Environment.SetEnvironmentVariable(
                "OPENCV_OPENCL_CACHE_ENABLE",
                "0",
                EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// 2026-08-18: Stitch 예외와 Dispose 예외가 연속 발생할 때 최초 원인을
        /// 가리지 않도록 Dispose의 OpenCV 예외는 경고 처리한다.
        /// </summary>
        private static Stitcher.Status RunStitcherCpuSafe(
            IEnumerable<Mat> frames,
            Mat panorama,
            Stitcher.Mode mode)
        {
            Stitcher stitcher =
                Stitcher.Create(mode);

            try
            {
                /*
                 * OpenCV 기본값은 등록 약 0.6MP, seam 약 0.1MP라서
                 * 난간/건물처럼 가는 전경 구조물의 이음선을 지나치게 거칠게
                 * 고를 수 있다. 특징점 등록과 seam 영상을 높여 유령상과
                 * 직선 절단 자국을 줄이고 최종 합성은 원본 입력 해상도로 한다.
                 * Stitcher Panorama 모드는 내부적으로 구면 warping,
                 * 노출 보정, graph-cut seam 및 multi-band blending을 수행한다.
                 */
                /*
                 * 2026-08-19: 18장/20° 정합에서 사다리·기둥 절단이 반복되어
                 * key frame을 24장으로 늘린다. 처리시간을 비슷하게 유지하도록
                 * 특징점 등록/최종 합성 해상도는 소폭 낮추고, seam 해상도는
                 * 조금 높여 가는 구조물 경계의 절단을 완화한다.
                 */
                stitcher.RegistrationResol = 0.22;
                stitcher.SeamEstimationResol = 0.10;
                stitcher.CompositingResol = 0.48;
                stitcher.PanoConfidenceThresh =
                    mode == Stitcher.Mode.Panorama
                        ? 0.9
                        : 0.7;
                stitcher.WaveCorrection =
                    mode == Stitcher.Mode.Panorama;
                stitcher.WaveCorrectKind =
                    OpenCvSharp.Detail.WaveCorrectKind.Horizontal;

                ConsoleLogHelper.State(
                    "EO PANORAMA / STITCH",
                    "High-quality stitch pipeline configured" +
                    " / MODE=" + mode +
                    " / REG_MP=" + stitcher.RegistrationResol.ToString("F1") +
                    " / SEAM_MP=" + stitcher.SeamEstimationResol.ToString("F1") +
                    " / COMPOSE_MP=" + stitcher.CompositingResol.ToString("F1") +
                    " / CONFIDENCE=" + stitcher.PanoConfidenceThresh.ToString("F1") +
                    " / WAVE=" + stitcher.WaveCorrection);

                return stitcher.Stitch(
                    frames,
                    panorama);
            }
            catch (OpenCVException ex)
            {
                if (ex.Message != null &&
                    ex.Message.IndexOf(
                        "OpenCL",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new InvalidOperationException(
                        "OpenCV OpenCL 드라이버 오류가 발생했습니다. " +
                        "수정된 프로그램을 완전히 종료한 뒤 다시 실행하십시오. " +
                        "재실행 후 파노라마는 CPU 방식으로 처리됩니다.",
                        ex);
                }

                if (ex.Message != null &&
                    ex.Message.IndexOf(
                        "SHRT_MAX",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new InvalidOperationException(
                        "파노라마 중간 영상 크기가 OpenCV 처리 한계를 넘었습니다. " +
                        "입력 축소 또는 세로 열 단순 블렌딩으로 복구합니다.",
                        ex);
                }

                throw;
            }
            finally
            {
                try
                {
                    stitcher.Dispose();
                }
                catch (OpenCVException disposeException)
                {
                    ConsoleLogHelper.Warning(
                        "EO PANORAMA",
                        "Stitcher dispose warning / " +
                        disposeException.Message);
                }

            }

        }

        /// <summary>
        /// ConvertToBgrMat 생성 및 변환 함수.
        /// </summary>
        private static Mat ConvertToBgrMat(
            BitmapSource source)
        {
            BitmapSource converted = source;

            if (source.Format != PixelFormats.Bgr24)
            {
                FormatConvertedBitmap formatConverted =
                    new FormatConvertedBitmap(
                        source,
                        PixelFormats.Bgr24,
                        null,
                        0);

                if (formatConverted.CanFreeze)
                {
                    formatConverted.Freeze();
                }

                converted = formatConverted;
            }

            int stride =
                converted.PixelWidth * 3;

            byte[] pixels =
                new byte[stride * converted.PixelHeight];

            converted.CopyPixels(
                pixels,
                stride,
                0);

            Mat fullSize =
                new Mat(
                    converted.PixelHeight,
                    converted.PixelWidth,
                    MatType.CV_8UC3);

            Marshal.Copy(
                pixels,
                0,
                fullSize.Data,
                pixels.Length);

            if (fullSize.Width <= MaximumInputWidth)
            {
                return fullSize;
            }

            double scale =
                MaximumInputWidth /
                (double)fullSize.Width;

            Mat resized = new Mat();

            Cv2.Resize(
                fullSize,
                resized,
                Size.Zero,
                scale,
                scale,
                InterpolationFlags.Area);

            fullSize.Dispose();
            return resized;
        }

        /// <summary>
        /// 2026-08-18: 구면 Warping 뒤 생기는 바깥쪽 검은 여백만 제거한다.
        /// 내부의 실제 검은 피사체는 BoundingRect 내부에 남는다.
        /// </summary>
        private static Mat CropOuterBlackBorder(
            Mat panorama)
        {
            using (Mat gray = new Mat())
            using (Mat mask = new Mat())
            using (Mat nonZero = new Mat())
            {
                Cv2.CvtColor(
                    panorama,
                    gray,
                    ColorConversionCodes.BGR2GRAY);

                Cv2.Threshold(
                    gray,
                    mask,
                    2,
                    255,
                    ThresholdTypes.Binary);

                Cv2.FindNonZero(
                    mask,
                    nonZero);

                if (nonZero.Empty())
                {
                    return panorama.Clone();
                }

                Rect bounds =
                    Cv2.BoundingRect(nonZero);

                /*
                 * 2026-08-18: BoundingRect만 사용하면 구면 Warping 경계가
                 * 곡선인 경우 하단/상단의 검은 쐐기 영역이 그대로 남는다.
                 * 유효 픽셀 비율이 99.5% 미만인 바깥쪽 행/열을 반복 제거해
                 * 실제 영상으로 채워진 안전 사각 영역만 최종 저장한다.
                 */
                bounds =
                    TrimIncompleteOuterEdges(
                        mask,
                        bounds);

                return new Mat(
                    panorama,
                    bounds).Clone();
            }

        }

        /// <summary>
        /// TrimIncompleteOuterEdges 동작 수행 함수.
        /// </summary>
        private static Rect TrimIncompleteOuterEdges(
            Mat validMask,
            Rect initialBounds)
        {
            const double RequiredValidRatio = 0.995;
            const int MinimumWidth = 64;
            const int MinimumHeight = 32;

            int left = initialBounds.Left;
            int top = initialBounds.Top;
            int right = initialBounds.Right;
            int bottom = initialBounds.Bottom;

            while (right - left > MinimumWidth &&
                   bottom - top > MinimumHeight)
            {
                double topRatio =
                    GetHorizontalValidRatio(
                        validMask,
                        left,
                        right,
                        top);

                double bottomRatio =
                    GetHorizontalValidRatio(
                        validMask,
                        left,
                        right,
                        bottom - 1);

                double leftRatio =
                    GetVerticalValidRatio(
                        validMask,
                        top,
                        bottom,
                        left);

                double rightRatio =
                    GetVerticalValidRatio(
                        validMask,
                        top,
                        bottom,
                        right - 1);

                double worstRatio =
                    Math.Min(
                        Math.Min(topRatio, bottomRatio),
                        Math.Min(leftRatio, rightRatio));

                if (worstRatio >= RequiredValidRatio)
                {
                    break;
                }

                if (worstRatio == topRatio)
                {
                    top++;
                }
                else if (worstRatio == bottomRatio)
                {
                    bottom--;
                }
                else if (worstRatio == leftRatio)
                {
                    left++;
                }
                else
                {
                    right--;
                }

            }

            return new Rect(
                left,
                top,
                right - left,
                bottom - top);
        }

        /// <summary>
        /// GetHorizontalValidRatio 조회 함수.
        /// </summary>
        private static double GetHorizontalValidRatio(
            Mat mask,
            int left,
            int right,
            int y)
        {
            using (Mat row =
                new Mat(
                    mask,
                    new Rect(
                        left,
                        y,
                        right - left,
                        1)))
            {
                return Cv2.CountNonZero(row) /
                    (double)(right - left);
            }

        }

        /// <summary>
        /// GetVerticalValidRatio 조회 함수.
        /// </summary>
        private static double GetVerticalValidRatio(
            Mat mask,
            int top,
            int bottom,
            int x)
        {
            using (Mat column =
                new Mat(
                    mask,
                    new Rect(
                        x,
                        top,
                        1,
                        bottom - top)))
            {
                return Cv2.CountNonZero(column) /
                    (double)(bottom - top);
            }

        }

        /// <summary>
        /// DisposeAll 종료 및 자원 해제 함수.
        /// </summary>
        private static void DisposeAll(
            IEnumerable<Mat> frames)
        {
            foreach (Mat frame in frames)
            {
                frame?.Dispose();
            }

        }

    }

}
