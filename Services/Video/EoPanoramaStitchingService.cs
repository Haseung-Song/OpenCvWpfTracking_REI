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
                Stitcher.Status fallbackStatus =
                    Stitcher.Status.OK;

                if (panoramaStatus != Stitcher.Status.OK ||
                    panorama.Empty())
                {
                    panorama.Dispose();
                    panorama = new Mat();

                    fallbackStatus =
                        RunStitcherCpuSafe(
                            frames,
                            panorama,
                            Stitcher.Mode.Scans);

                    if (fallbackStatus != Stitcher.Status.OK ||
                        panorama.Empty())
                    {
                        throw new InvalidOperationException(
                            "파노라마 특징점 정합에 실패했습니다. " +
                            "EO Zoom을 광각으로 맞추고 고정 물체가 30~40% 이상 " +
                            "겹치도록 다시 촬영하십시오. (Panorama: " +
                            panoramaStatus + ", Fallback: " +
                            fallbackStatus + ")");
                    }
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
                            95)))
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

            List<Mat> stitchedRows =
                new List<Mat>();

            Mat panorama = null;

            try
            {
                ConsoleLogHelper.Info(
                    "EO PANORAMA / STITCH",
                    "Row panorama processing started / OUTPUT=" + outputPath);

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

                if (frameRows.Count < 2)
                {
                    throw new InvalidOperationException(
                        "세로 화각 파노라마에는 서로 다른 Tilt 촬영 행이 2개 이상 필요합니다.");
                }

                for (int rowIndex = 0;
                     rowIndex < frameRows.Count;
                     rowIndex++)
                {
                    ConsoleLogHelper.Info(
                        "EO PANORAMA / STITCH",
                        "Horizontal row stitching started / ROW=" + (rowIndex + 1) +
                        " / FRAMES=" + frameRows[rowIndex].Count);

                    stitchedRows.Add(
                        StitchMatsWithFallback(
                            frameRows[rowIndex],
                            "ROW=" + (rowIndex + 1)));

                    Mat stitchedRow =
                        stitchedRows[stitchedRows.Count - 1];

                    ConsoleLogHelper.State(
                        "EO PANORAMA / STITCH",
                        "Horizontal row stitching completed / ROW=" + (rowIndex + 1) +
                        " / RESULT=" + stitchedRow.Width + "x" + stitchedRow.Height);
                }

                ConsoleLogHelper.Info(
                    "EO PANORAMA / SEAM",
                    "Vertical row alignment and optimal seam search started");

                panorama =
                    BlendRowsVertically(stitchedRows);

                ConsoleLogHelper.State(
                    "EO PANORAMA / SEAM",
                    "Vertical row merge completed / RESULT=" +
                    panorama.Width + "x" + panorama.Height);

                BitmapSource result = SaveAndConvert(
                    panorama,
                    outputPath);

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
                DisposeAll(stitchedRows);

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

        private static Mat StitchMatsWithFallback(
            IList<Mat> frames,
            string stage)
        {
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
                    (keyFrames.Count < frames.Count ? "30deg" : "10deg"));

                Stitcher.Status panoramaStatus =
                    RunStitcherCpuSafe(
                        keyFrames,
                        panorama,
                        Stitcher.Mode.Panorama);

                ConsoleLogHelper.State(
                    "EO PANORAMA / STITCH",
                    stage + " / MODE=Panorama / STATUS=" + panoramaStatus +
                    " / EMPTY=" + panorama.Empty());

                Stitcher.Status fallbackStatus =
                    Stitcher.Status.OK;

                if (panoramaStatus != Stitcher.Status.OK ||
                    panorama.Empty())
                {
                    panorama.Dispose();
                    panorama = new Mat();

                    fallbackStatus =
                        RunStitcherCpuSafe(
                            keyFrames,
                            panorama,
                            Stitcher.Mode.Scans);

                    ConsoleLogHelper.Warning(
                        "EO PANORAMA / STITCH",
                        stage + " / Panorama mode failed; Scans fallback completed" +
                        " / PANORAMA_STATUS=" + panoramaStatus +
                        " / SCANS_STATUS=" + fallbackStatus +
                        " / EMPTY=" + panorama.Empty());

                    if (fallbackStatus != Stitcher.Status.OK ||
                        panorama.Empty())
                    {
                        throw new InvalidOperationException(
                            "파노라마 특징점 정합에 실패했습니다. " +
                            "(Panorama: " + panoramaStatus +
                            ", Fallback: " + fallbackStatus + ")");
                    }
                }

                using (Mat cropped = CropOuterBlackBorder(panorama))
                {
                    return cropped.Clone();
                }
            }
            finally
            {
                panorama.Dispose();
            }
        }

        /// <summary>
        /// PTZ가 정확히 10° 간격으로 촬영하므로 36장을 모두 다시 비교하지 않고
        /// 30° 간격의 12장을 사용한다. 광각 제한(Zoom 0~100)에서는 인접 영상의
        /// 중첩을 유지하면서 특징점 비교량과 seam 처리량을 크게 줄인다.
        /// </summary>
        private static IList<Mat> SelectAngleSpacedKeyFrames(
            IList<Mat> frames)
        {
            if (frames == null || frames.Count < 24)
            {
                return frames;
            }

            List<Mat> selected =
                new List<Mat>((frames.Count + 2) / 3);

            for (int index = 0;
                 index < frames.Count;
                 index += 3)
            {
                selected.Add(frames[index]);
            }

            return selected;
        }

        private static Mat BlendRowsVertically(
            IList<Mat> rows)
        {
            int targetWidth =
                rows.Min(row => row.Width);

            List<Mat> normalizedRows =
                new List<Mat>();

            try
            {
                foreach (Mat row in rows)
                {
                    double scale =
                        targetWidth /
                        (double)row.Width;

                    Mat resized =
                        new Mat();

                    Cv2.Resize(
                        row,
                        resized,
                        new Size(
                            targetWidth,
                            Math.Max(
                                1,
                                (int)Math.Round(row.Height * scale))),
                        0,
                        0,
                        InterpolationFlags.Area);

                    normalizedRows.Add(resized);
                }

                Mat result =
                    normalizedRows[0].Clone();

                for (int rowIndex = 1;
                     rowIndex < normalizedRows.Count;
                     rowIndex++)
                {
                    Mat next =
                        normalizedRows[rowIndex];

                    int overlap =
                        Math.Max(
                            24,
                            (int)Math.Round(
                                Math.Min(result.Height, next.Height) *
                                0.38));

                    overlap =
                        Math.Min(
                            overlap,
                            Math.Min(result.Height, next.Height) - 1);

                    int shift =
                        EstimateCyclicHorizontalShift(
                            result,
                            next,
                            overlap);

                    Mat alignedUpper = null;
                    Mat alignedNext = null;

                    try
                    {
                        AlignRowsWithoutWrapSeam(
                            result,
                            next,
                            shift,
                            out alignedUpper,
                            out alignedNext);

                        ConsoleLogHelper.State(
                            "EO PANORAMA / SEAM",
                            "Rows aligned / LOWER_ROW=" + (rowIndex + 1) +
                            " / OVERLAP=" + overlap +
                            " / HORIZONTAL_SHIFT_PX=" + shift +
                            " / COMMON_WIDTH=" + alignedUpper.Width +
                            " / WRAP_SEAM=REMOVED");

                        Mat combined =
                            MergeRowsOnOptimalSeam(
                                alignedUpper,
                                alignedNext,
                                overlap);

                        result.Dispose();
                        result = combined;
                    }
                    finally
                    {
                        alignedUpper?.Dispose();
                        alignedNext?.Dispose();
                    }
                }

                return result;
            }
            finally
            {
                DisposeAll(normalizedRows);
            }
        }

        /// <summary>
        /// 두 360° 행의 시작 seam 위치 차이를 순환 이동으로 보정한다.
        /// 전체 해상도에서 비교하지 않고 축소된 겹침 영역의 평균 절대 오차를
        /// 사용하므로 큰 파노라마에서도 메모리와 처리 시간을 제한한다.
        /// </summary>
        private static int EstimateCyclicHorizontalShift(
            Mat upper,
            Mat lower,
            int overlap)
        {
            const int AnalysisWidth = 720;
            const int AnalysisHeight = 96;

            using (Mat upperOverlap = new Mat(
                upper,
                new Rect(0, upper.Height - overlap, upper.Width, overlap)))
            using (Mat lowerOverlap = new Mat(
                lower,
                new Rect(0, 0, lower.Width, overlap)))
            using (Mat upperGray = new Mat())
            using (Mat lowerGray = new Mat())
            using (Mat upperSmall = new Mat())
            using (Mat lowerSmall = new Mat())
            {
                Cv2.CvtColor(upperOverlap, upperGray, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(lowerOverlap, lowerGray, ColorConversionCodes.BGR2GRAY);
                Cv2.Resize(upperGray, upperSmall, new Size(AnalysisWidth, AnalysisHeight));
                Cv2.Resize(lowerGray, lowerSmall, new Size(AnalysisWidth, AnalysisHeight));

                upperSmall.GetArray(out byte[] upperPixels);
                lowerSmall.GetArray(out byte[] lowerPixels);

                int bestShift = 0;
                long bestCost = long.MaxValue;
                int maximumShift = AnalysisWidth / 8;

                for (int shift = -maximumShift; shift <= maximumShift; shift++)
                {
                    long cost = 0;

                    for (int y = 0; y < AnalysisHeight; y += 2)
                    {
                        int rowOffset = y * AnalysisWidth;

                        for (int x = 0; x < AnalysisWidth; x += 2)
                        {
                            int lowerX = (x + shift + AnalysisWidth) % AnalysisWidth;
                            cost += Math.Abs(
                                upperPixels[rowOffset + x] -
                                lowerPixels[rowOffset + lowerX]);
                        }
                    }

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestShift = shift;
                    }
                }

                return (int)Math.Round(
                    bestShift * upper.Width / (double)AnalysisWidth);
            }
        }

        private static Mat ShiftCyclicHorizontally(
            Mat source,
            int shift)
        {
            int normalizedShift =
                ((shift % source.Width) + source.Width) % source.Width;

            if (normalizedShift == 0)
            {
                return source.Clone();
            }

            Mat shifted =
                new Mat(source.Size(), source.Type(), Scalar.Black);

            int firstWidth =
                source.Width - normalizedShift;

            using (Mat sourceFirst = new Mat(
                source,
                new Rect(normalizedShift, 0, firstWidth, source.Height)))
            using (Mat targetFirst = new Mat(
                shifted,
                new Rect(0, 0, firstWidth, source.Height)))
            using (Mat sourceSecond = new Mat(
                source,
                new Rect(0, 0, normalizedShift, source.Height)))
            using (Mat targetSecond = new Mat(
                shifted,
                new Rect(firstWidth, 0, normalizedShift, source.Height)))
            {
                sourceFirst.CopyTo(targetFirst);
                sourceSecond.CopyTo(targetSecond);
            }

            return shifted;
        }

        /// <summary>
        /// 원형 이동은 한 행의 좌우 끝을 영상 중간에서 다시 연결하여 건물,
        /// 사다리와 안테나가 수직으로 잘리는 인위적 seam을 만든다. 대신 두 행의
        /// 실제 공통 수평 구간만 잘라 동일 좌표로 맞춘다. 폭은 |shift|만큼
        /// 줄지만 영상 내부에 wrap 경계가 삽입되지 않는다.
        /// </summary>
        private static void AlignRowsWithoutWrapSeam(
            Mat upper,
            Mat lower,
            int shift,
            out Mat alignedUpper,
            out Mat alignedLower)
        {
            int maximumShift =
                Math.Max(0, Math.Min(upper.Width, lower.Width) - 64);

            int safeShift =
                Math.Max(-maximumShift, Math.Min(maximumShift, shift));

            int upperX = safeShift < 0 ? -safeShift : 0;
            int lowerX = safeShift > 0 ? safeShift : 0;
            int commonWidth =
                Math.Min(
                    upper.Width - upperX,
                    lower.Width - lowerX);

            alignedUpper =
                new Mat(
                    upper,
                    new Rect(upperX, 0, commonWidth, upper.Height)).Clone();

            alignedLower =
                new Mat(
                    lower,
                    new Rect(lowerX, 0, commonWidth, lower.Height)).Clone();
        }

        /// <summary>
        /// 겹침 영역 전체를 반투명 합성하지 않고 영상 차이가 가장 작은
        /// 동적 이음선을 찾은 뒤 그 주변 8px에서만 feather blending한다.
        /// 난간·건물의 유령상과 전체 흐림을 줄이면서 경계 노출 차이는 숨긴다.
        /// </summary>
        private static Mat MergeRowsOnOptimalSeam(
            Mat upper,
            Mat lower,
            int overlap)
        {
            int[] seam =
                FindLowCostSeam(
                    upper,
                    lower,
                    overlap);

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

            const int BlendChunkHeight = 64;

            for (int chunkTop = 0;
                 chunkTop < overlap;
                 chunkTop += BlendChunkHeight)
            {
                const int FeatherRadius = 4;
                int chunkHeight =
                    Math.Min(
                        BlendChunkHeight,
                        overlap - chunkTop);

                float[] upperWeightValues =
                    new float[chunkHeight * upper.Width];
                float[] lowerWeightValues =
                    new float[chunkHeight * upper.Width];

                for (int x = 0; x < upper.Width; x++)
                {
                    int seamY = seam[x];

                    for (int localY = 0; localY < chunkHeight; localY++)
                    {
                        int y = chunkTop + localY;
                        int index = localY * upper.Width + x;
                        double lowerWeight =
                            (y - (seamY - FeatherRadius)) /
                            (double)(FeatherRadius * 2);

                        lowerWeight =
                            Math.Max(0.0, Math.Min(1.0, lowerWeight));

                        lowerWeightValues[index] = (float)lowerWeight;
                        upperWeightValues[index] = (float)(1.0 - lowerWeight);
                    }
                }

                using (Mat upperChunk = new Mat(
                    upper,
                    new Rect(
                        0,
                        upper.Height - overlap + chunkTop,
                        upper.Width,
                        chunkHeight)))
                using (Mat lowerChunk = new Mat(
                    lower,
                    new Rect(0, chunkTop, lower.Width, chunkHeight)))
                using (Mat blendedChunk = new Mat())
                using (Mat upperWeights = new Mat(
                    chunkHeight,
                    upper.Width,
                    MatType.CV_32FC1))
                using (Mat lowerWeights = new Mat(
                    chunkHeight,
                    upper.Width,
                    MatType.CV_32FC1))
                using (Mat chunkTarget = new Mat(
                    combined,
                    new Rect(
                        0,
                        upper.Height - overlap + chunkTop,
                        upper.Width,
                        chunkHeight)))
                {
                    upperWeights.SetArray(upperWeightValues);
                    lowerWeights.SetArray(lowerWeightValues);

                    Cv2.BlendLinear(
                        upperChunk,
                        lowerChunk,
                        upperWeights,
                        lowerWeights,
                        blendedChunk);

                    blendedChunk.CopyTo(chunkTarget);
                }
            }

            return combined;
        }

        private static int[] FindLowCostSeam(
            Mat upper,
            Mat lower,
            int overlap)
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

                        if (y > 0 && previous[y - 1] + 2.0 < bestPreviousCost)
                        {
                            bestPreviousCost = previous[y - 1] + 2.0;
                            bestPreviousY = y - 1;
                        }

                        if (y + 1 < analysisHeight &&
                            previous[y + 1] + 2.0 < bestPreviousCost)
                        {
                            bestPreviousCost = previous[y + 1] + 2.0;
                            bestPreviousY = y + 1;
                        }

                        current[y] =
                            bestPreviousCost + costPixels[y * analysisWidth + x];
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

                return fullSeam;
            }
        }

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
                    95)))
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
                stitcher.RegistrationResol = 0.35;
                stitcher.SeamEstimationResol = 0.1;
                stitcher.CompositingResol = 0.6;
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
