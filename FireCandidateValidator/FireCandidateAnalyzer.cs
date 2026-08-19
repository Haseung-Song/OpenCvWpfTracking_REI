using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace FireCandidateValidator
{
    /// <summary>
    /// 방사 온도값이 없는 IR 표시 영상에서 상대적으로 뜨겁게 표현된 영역을 찾는다.
    ///
    /// 이 결과는 실제 화재 확정값이 아니며, 현장 임계값 조정과 AI 결과 비교를 위한
    /// FIRE CANDIDATE 시험 결과로만 사용한다.
    /// </summary>
    internal sealed class FireCandidateAnalyzer
    {
        private int _continuousCandidateFrames;
        // 2026-08-18: 동영상에서는 색상 자체가 아니라 실제 화염의 프레임간
        // 움직임과 외곽 형상 변화까지 확인한다.
        private Mat _previousGray = new Mat();
        private Mat _previousCandidateMask = new Mat();

        /// <summary>
        /// Analyze 동작 수행 함수.
        /// </summary>
        internal FireCandidateAnalysis Analyze(
            Mat source,
            double thresholdRatio,
            double minimumAreaRatio,
            int confirmationFrameCount,
            int fireBoxGroupingMode)
        {
            if (source == null || source.Empty())
            {
                return FireCandidateAnalysis.Empty;
            }

            double safeThreshold = Math.Max(0.05, Math.Min(0.99, thresholdRatio));
            double safeAreaRatio = Math.Max(0.0001, Math.Min(0.20, minimumAreaRatio));
            int threshold = (int)Math.Round(safeThreshold * 255.0);
            double frameArea = Math.Max(1.0, source.Width * source.Height);
            double minimumArea = Math.Max(16.0, frameArea * safeAreaRatio);
            // 2026-08-14: 큰 화염도 하나의 검출 영역으로 유지한다.
            // 2026-08-14: Accept a fire covering almost the entire still image.
            double maximumArea = frameArea * 0.95;

            Mat mask = CreateCandidateMask(source, threshold);
            Mat cleanedMask = new Mat();
            Mat currentGray = new Mat();
            Mat motionMask = new Mat();
            Mat candidateChangeMask = new Mat();

            using (Mat openKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
            // 2026-08-14: 분리된 불꽃을 연결해 전체 화염 BBox를 만든다.
            using (Mat closeKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(9, 9)))
            {
                Cv2.MorphologyEx(mask, cleanedMask, MorphTypes.Open, openKernel);
                Cv2.MorphologyEx(cleanedMask, cleanedMask, MorphTypes.Close, closeKernel);
            }

            mask.Dispose();

            if (source.Channels() == 1)
            {
                source.CopyTo(currentGray);
            }
            else if (source.Channels() == 4)
            {
                Cv2.CvtColor(source, currentGray, ColorConversionCodes.BGRA2GRAY);
            }
            else
            {
                Cv2.CvtColor(source, currentGray, ColorConversionCodes.BGR2GRAY);
            }

            Cv2.GaussianBlur(currentGray, currentGray, new Size(5, 5), 0);

            bool requiresTemporalMotion = confirmationFrameCount > 1;
            bool hasMotionReference =
                _previousGray != null && !_previousGray.Empty() &&
                _previousGray.Size() == currentGray.Size();
            bool hasCandidateReference =
                _previousCandidateMask != null && !_previousCandidateMask.Empty() &&
                _previousCandidateMask.Size() == cleanedMask.Size();
            double globalMotionRatio = 0.0;

            if (hasMotionReference)
            {
                Cv2.Absdiff(currentGray, _previousGray, motionMask);
                Cv2.Threshold(motionMask, motionMask, 14, 255, ThresholdTypes.Binary);
                globalMotionRatio = Cv2.CountNonZero(motionMask) / frameArea;
            }

            if (hasCandidateReference)
            {
                Cv2.Absdiff(cleanedMask, _previousCandidateMask, candidateChangeMask);
            }

            Cv2.FindContours(
                cleanedMask,
                out Point[][] contours,
                out _,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            List<Rect> rawCandidates = new List<Rect>();
            double largestAreaRatio = 0.0;

            foreach (Point[] contour in contours)
            {
                double area = Cv2.ContourArea(contour);

                if (area < minimumArea || area > maximumArea)
                {
                    continue;
                }

                Rect rect = Cv2.BoundingRect(contour);
                double rectangleArea = Math.Max(1.0, rect.Width * rect.Height);
                double fillRatio = area / rectangleArea;
                double aspectRatio = rect.Width / (double)Math.Max(1, rect.Height);
                double rectangleAreaRatio = rectangleArea / frameArea;
                Point[] convexHull = Cv2.ConvexHull(contour);
                double hullArea = Math.Max(1.0, Cv2.ContourArea(convexHull));
                double solidity = area / hullArea;
                double perimeter = Math.Max(1.0, Cv2.ArcLength(contour, true));
                double irregularity =
                    perimeter * perimeter /
                    Math.Max(1.0, 4.0 * Math.PI * area);
                double motionRatio = 0.0;
                double hotMotionRatio = 0.0;
                double shapeChangeRatio = 0.0;

                if (hasMotionReference && hasCandidateReference)
                {
                    using (Mat motionRoi = new Mat(motionMask, rect))
                    using (Mat candidateRoi = new Mat(cleanedMask, rect))
                    using (Mat changeRoi = new Mat(candidateChangeMask, rect))
                    using (Mat hotMotion = new Mat())
                    {
                        motionRatio = Cv2.CountNonZero(motionRoi) / rectangleArea;
                        Cv2.BitwiseAnd(motionRoi, candidateRoi, hotMotion);
                        hotMotionRatio =
                            Cv2.CountNonZero(hotMotion) /
                            Math.Max(1.0, Cv2.CountNonZero(candidateRoi));
                        shapeChangeRatio =
                            Cv2.CountNonZero(changeRoi) /
                            Math.Max(1.0, Cv2.CountNonZero(candidateRoi));
                    }

                }

                // 넓은 건물 외벽, 수평 띠 및 작은 점 노이즈를 후보에서 제외한다.
                // 2026-08-14: The former 60% size limit rejected the real large fire.
                if (fillRatio < 0.005 ||
                    (rectangleAreaRatio > 0.75 && fillRatio > 0.30) ||
                    (rectangleAreaRatio > 0.08 && aspectRatio > 2.2 &&
                     fillRatio > 0.18 && solidity > 0.72) ||
                    (solidity > 0.90 && fillRatio > 0.50 && irregularity < 1.25) ||
                    (requiresTemporalMotion &&
                     (!hasMotionReference ||
                      !hasCandidateReference ||
                      globalMotionRatio > 0.25 ||
                      motionRatio < 0.010 ||
                      hotMotionRatio < 0.018 ||
                      shapeChangeRatio < 0.020 ||
                      (rectangleAreaRatio > 0.12 && shapeChangeRatio < 0.060))) ||
                    aspectRatio < 0.05 || aspectRatio > 20.0)
                {
                    continue;
                }

                rawCandidates.Add(rect);
                largestAreaRatio = Math.Max(largestAreaRatio, area / frameArea);
            }

            List<Rect> candidates = MergeCandidates(
                rawCandidates,
                source.Width,
                source.Height,
                fireBoxGroupingMode);

            if (candidates.Count > 0)
            {
                _continuousCandidateFrames++;
            }
            else
            {
                _continuousCandidateFrames = 0;
            }

            bool isConfirmed =
                candidates.Count > 0 &&
                _continuousCandidateFrames >= Math.Max(1, confirmationFrameCount);

            currentGray.CopyTo(_previousGray);
            cleanedMask.CopyTo(_previousCandidateMask);
            currentGray.Dispose();
            motionMask.Dispose();
            candidateChangeMask.Dispose();

            return new FireCandidateAnalysis(
                cleanedMask,
                candidates,
                isConfirmed,
                _continuousCandidateFrames,
                largestAreaRatio);
        }

        /// <summary>
        /// Reset 동작 수행 함수.
        /// </summary>
        internal void Reset()
        {
            _continuousCandidateFrames = 0;
            _previousGray.Dispose();
            _previousCandidateMask.Dispose();
            _previousGray = new Mat();
            _previousCandidateMask = new Mat();
        }

        /// <summary>
        /// MergeCandidates 동작 수행 함수.
        /// </summary>
        private static List<Rect> MergeCandidates(
            IList<Rect> source,
            int frameWidth,
            int frameHeight,
            int fireBoxGroupingMode)
        {
            if (source == null || source.Count == 0)
            {
                return new List<Rect>();
            }

            // 2026-08-14: Mode 1 encloses all detected flame candidates in one BBox.
            if (fireBoxGroupingMode == 1)
            {
                Rect unified = source[0];
                for (int index = 1; index < source.Count; index++)
                {
                    unified = Union(unified, source[index]);
                }

                return new List<Rect>
                {
                    Expand(unified, 4, 4, frameWidth, frameHeight)
                };
            }

            // 2026-08-14: Merge only nearby flame fragments and preserve separated fires.
            List<Rect> grouped = new List<Rect>(source);
            int horizontalGap = Math.Max(6, frameWidth / 120);
            int verticalGap = Math.Max(8, frameHeight / 100);
            bool mergedAny;

            do
            {
                mergedAny = false;
                for (int first = 0; first < grouped.Count && !mergedAny; first++)
                {
                    Rect expanded = Expand(grouped[first], horizontalGap, verticalGap, frameWidth, frameHeight);
                    for (int second = first + 1; second < grouped.Count; second++)
                    {
                        if (!Intersects(expanded, grouped[second]))
                        {
                            continue;
                        }

                        grouped[first] = Union(grouped[first], grouped[second]);
                        grouped.RemoveAt(second);
                        mergedAny = true;
                        break;
                    }

                }

            }
            while (mergedAny);

            for (int index = 0; index < grouped.Count; index++)
            {
                grouped[index] = Expand(grouped[index], 4, 4, frameWidth, frameHeight);
            }

            grouped.Sort((left, right) =>
                (right.Width * right.Height).CompareTo(left.Width * left.Height));
            if (grouped.Count > 8)
            {
                grouped.RemoveRange(8, grouped.Count - 8);
            }

            return grouped;

            /* Legacy proximity merge retained below for reference only.
            List<Rect> merged = new List<Rect>(source);
            // 2026-08-14: 같은 화염의 인접 후보를 병합한다.
            int xPadding = Math.Max(16, frameWidth / 28);
            int yPadding = Math.Max(16, frameHeight / 24);
            bool changed;

            do
            {
                changed = false;

                for (int first = 0; first < merged.Count && !changed; first++)
                {
                    Rect expanded = Expand(merged[first], xPadding, yPadding, frameWidth, frameHeight);

                    for (int second = first + 1; second < merged.Count; second++)
                    {
                        if (!Intersects(expanded, merged[second]))
                        {
                            continue;
                        }

                        merged[first] = Union(merged[first], merged[second]);
                        merged.RemoveAt(second);
                        changed = true;
                        break;
                    }

                }

            }
            while (changed);

            if (merged.Count <= 1)
            {
                return merged;
            }

            Rect largest = merged[0];

            for (int index = 1; index < merged.Count; index++)
            {
                if (merged[index].Width * merged[index].Height > largest.Width * largest.Height)
                {
                    largest = merged[index];
                }

            }

            return new List<Rect> { largest };
            */
        }

        /// <summary>
        /// Expand 동작 수행 함수.
        /// </summary>
        private static Rect Expand(Rect rect, int x, int y, int width, int height)
        {
            int left = Math.Max(0, rect.X - x);
            int top = Math.Max(0, rect.Y - y);
            int right = Math.Min(width, rect.X + rect.Width + x);
            int bottom = Math.Min(height, rect.Y + rect.Height + y);
            return new Rect(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// Intersects 동작 수행 함수.
        /// </summary>
        private static bool Intersects(Rect first, Rect second) =>
            first.X < second.X + second.Width && first.X + first.Width > second.X &&
            first.Y < second.Y + second.Height && first.Y + first.Height > second.Y;

        /// <summary>
        /// Union 동작 수행 함수.
        /// </summary>
        private static Rect Union(Rect first, Rect second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int right = Math.Max(first.X + first.Width, second.X + second.Width);
            int bottom = Math.Max(first.Y + first.Height, second.Y + second.Height);
            return new Rect(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// CreateCandidateMask 생성 및 변환 함수.
        /// </summary>
        private static Mat CreateCandidateMask(Mat source, int threshold)
        {
            using (Mat bgr = EnsureBgr(source))
            using (Mat grayscale = new Mat())
            using (Mat blurred = new Mat())
            using (Mat localContrast = new Mat())
            using (Mat contrastMask = new Mat())
            using (Mat intensityMask = new Mat())
            using (Mat hsv = new Mat())
            using (Mat redLow = new Mat())
            using (Mat redHigh = new Mat())
            using (Mat orange = new Mat())
            using (Mat colorMask = new Mat())
            {
                Cv2.CvtColor(bgr, grayscale, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(grayscale, blurred, new Size(31, 31), 0);
                Cv2.Subtract(grayscale, blurred, localContrast);
                Cv2.Threshold(localContrast, contrastMask, 10, 255, ThresholdTypes.Binary);
                Cv2.Threshold(grayscale, intensityMask, threshold, 255, ThresholdTypes.Binary);

                // 팔레트 색상과 무관하게 원본 IR의 상대 밝기와 국부 대비만 분석한다.
                // Offline validation must work without a device connection:
                // retain locally bright targets even when they are not above
                // the absolute threshold selected for another palette.
                Cv2.BitwiseOr(intensityMask, contrastMask, colorMask);

                // BLACK HOT target: dark compared with its local background.
                Cv2.Subtract(blurred, grayscale, localContrast);
                Cv2.Threshold(localContrast, contrastMask, 10, 255, ThresholdTypes.Binary);
                Cv2.BitwiseOr(colorMask, contrastMask, colorMask);

                // RAINBOW and other colour thermal images: red/orange target.
                Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
                Cv2.InRange(hsv, new Scalar(0, 90, 120), new Scalar(12, 255, 255), redLow);
                Cv2.InRange(hsv, new Scalar(165, 90, 120), new Scalar(179, 255, 255), redHigh);
                Cv2.InRange(hsv, new Scalar(13, 90, 150), new Scalar(35, 255, 255), orange);
                Cv2.BitwiseOr(redLow, redHigh, intensityMask);
                Cv2.BitwiseOr(intensityMask, orange, intensityMask);
                // 2026-08-14: 색상 화염이 충분하면 배경 전체를 포함하는
                // 밝기/대비 마스크 대신 온색 영역만 사용한다.
                if (Cv2.CountNonZero(intensityMask) >= Math.Max(32, source.Width * source.Height / 5000))
                {
                    intensityMask.CopyTo(colorMask);
                }
                else
                {
                    Cv2.BitwiseOr(colorMask, intensityMask, colorMask);
                }

                // 2026-08-14: For grayscale IR, select the sparser hot polarity.
                // This prevents a bright or dark background from becoming one full-frame box.
                if (Cv2.Mean(hsv).Val1 < 35)
                {
                    Cv2.Subtract(grayscale, blurred, localContrast);
                    Cv2.Threshold(localContrast, contrastMask, 10, 255, ThresholdTypes.Binary);
                    Cv2.Threshold(grayscale, intensityMask, threshold, 255, ThresholdTypes.Binary);
                    if (Cv2.CountNonZero(intensityMask) > source.Width * source.Height * 0.45)
                    {
                        contrastMask.CopyTo(redLow);
                    }
                    else
                    {
                        Cv2.BitwiseOr(intensityMask, contrastMask, redLow);
                    }

                    Cv2.Subtract(blurred, grayscale, localContrast);
                    Cv2.Threshold(localContrast, contrastMask, 10, 255, ThresholdTypes.Binary);
                    Cv2.Threshold(grayscale, intensityMask, 255 - threshold, 255, ThresholdTypes.BinaryInv);
                    if (Cv2.CountNonZero(intensityMask) > source.Width * source.Height * 0.45)
                    {
                        contrastMask.CopyTo(redHigh);
                    }
                    else
                    {
                        Cv2.BitwiseOr(intensityMask, contrastMask, redHigh);
                    }

                    if (Cv2.CountNonZero(redLow) <= Cv2.CountNonZero(redHigh))
                    {
                        redLow.CopyTo(colorMask);
                    }
                    else
                    {
                        redHigh.CopyTo(colorMask);
                    }

                }
                return colorMask.Clone();
            }

        }

        /// <summary>
        /// EnsureBgr 동작 수행 함수.
        /// </summary>
        private static Mat EnsureBgr(Mat source)
        {
            Mat result = new Mat();

            if (source.Channels() == 1)
            {
                Cv2.CvtColor(source, result, ColorConversionCodes.GRAY2BGR);
            }
            else if (source.Channels() == 4)
            {
                Cv2.CvtColor(source, result, ColorConversionCodes.BGRA2BGR);
            }
            else
            {
                source.CopyTo(result);
            }

            return result;
        }

    }

    internal sealed class FireCandidateAnalysis : IDisposable
    {
        internal static FireCandidateAnalysis Empty
        {
            get
            {
                return new FireCandidateAnalysis(
                    new Mat(),
                    new List<Rect>(),
                    false,
                    0,
                    0.0);
            }

        }

        /// <summary>
        /// FireCandidateAnalysis 동작 수행 함수.
        /// </summary>
        internal FireCandidateAnalysis(
            Mat mask,
            IList<Rect> candidates,
            bool isConfirmed,
            int continuousFrames,
            double largestAreaRatio)
        {
            Mask = mask;
            Candidates = candidates;
            IsConfirmed = isConfirmed;
            ContinuousFrames = continuousFrames;
            LargestAreaRatio = largestAreaRatio;
        }

        internal Mat Mask { get; private set; }
        internal IList<Rect> Candidates { get; private set; }
        internal bool IsConfirmed { get; private set; }
        internal int ContinuousFrames { get; private set; }
        internal double LargestAreaRatio { get; private set; }

        /// <summary>
        /// Dispose 종료 및 자원 해제 함수.
        /// </summary>
        public void Dispose()
        {
            if (Mask != null)
            {
                Mask.Dispose();
                Mask = null;
            }

        }

    }

}
