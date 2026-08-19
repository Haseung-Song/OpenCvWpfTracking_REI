using OpenCvSharp;
using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Video
{
    /// <summary>
    /// IR 영상의 상대 고온 색상, 국부 명암 차이, 형상 및 지속 시간을 조합하여
    /// 화재 가능성이 있는 영역만 시험용 후보로 표시한다.
    ///
    /// 방사 온도 원본이 없는 일반 RTSP 영상 기반이므로 실제 온도를 판정하지 않으며,
    /// 결과는 확정 화재가 아닌 FIRE CANDIDATE로만 사용한다.
    /// </summary>
    internal sealed class ThermalFireDetectionService
    {
        private const int ConfirmFrameCount = 6;
        private const int ClearFrameCount = 8;

        private int _candidateFrameCount;
        private int _clearFrameCount;
        private bool _isFireCandidateDetected;
        private Rect _trackedCandidateRect = Rect.Empty;
        // 2026-08-14: Static hot roofs/ground are rejected using inter-frame motion.
        private Mat _previousGray = new Mat();
        // 2026-08-18: 팔레트상 계속 붉게 보이는 건물/지면과 실제로 형상이
        // 흔들리는 화염을 구분하기 위한 직전 후보 마스크이다.
        private Mat _previousCandidateMask = new Mat();

        /// <summary>
        /// Process 처리 함수.
        /// </summary>
        internal ThermalFireDetectionResult Process(
            Mat frame,
            bool isEnabled,
            double hotThresholdRatio,
            double minimumAreaRatio,
            int fireBoxGroupingMode)
        {
            if (!isEnabled || frame == null || frame.Empty())
            {
                return ResetDetectionState();
            }

            int threshold =
                (int)Math.Round(Math.Max(0, Math.Min(1, hotThresholdRatio)) * 255);

            double frameArea = frame.Width * frame.Height;
            double minimumArea = Math.Max(64, frameArea * minimumAreaRatio);
            // 2026-08-14: 큰 실제 화염이 분할되어 누락되지 않도록 상한을 확장한다.
            double maximumArea = frameArea * 0.95;

            using (Mat currentGray = new Mat())
            using (Mat motionMask = new Mat())
            using (Mat candidateChangeMask = new Mat())
            using (Mat mask = CreateHotPixelMask(
                       frame,
                       threshold))
            using (Mat cleanedMask = new Mat())
            using (Mat openKernel = Cv2.GetStructuringElement(
                       MorphShapes.Ellipse,
                       new Size(3, 3)))
            using (Mat closeKernel = Cv2.GetStructuringElement(
                       MorphShapes.Ellipse,
                       new Size(9, 9)))
            {
                if (frame.Channels() == 1)
                {
                    frame.CopyTo(currentGray);
                }
                else
                {
                    Cv2.CvtColor(frame, currentGray, ColorConversionCodes.BGR2GRAY);
                }

                // 2026-08-18: RTSP 압축 노이즈를 움직임으로 오인하지 않도록
                // 프레임 차분 전 소형 Gaussian Blur를 적용한다.
                Cv2.GaussianBlur(currentGray, currentGray, new Size(5, 5), 0);

                bool hasMotionReference =
                    _previousGray != null && !_previousGray.Empty() &&
                    _previousGray.Size() == currentGray.Size();
                double globalMotionRatio = 0;
                if (hasMotionReference)
                {
                    Cv2.Absdiff(currentGray, _previousGray, motionMask);
                    Cv2.Threshold(motionMask, motionMask, 12, 255, ThresholdTypes.Binary);
                    globalMotionRatio = Cv2.CountNonZero(motionMask) / frameArea;
                }

                Cv2.MorphologyEx(mask, cleanedMask, MorphTypes.Open, openKernel);
                Cv2.MorphologyEx(cleanedMask, cleanedMask, MorphTypes.Close, closeKernel);

                bool hasCandidateReference =
                    _previousCandidateMask != null && !_previousCandidateMask.Empty() &&
                    _previousCandidateMask.Size() == cleanedMask.Size();

                if (hasCandidateReference)
                {
                    Cv2.Absdiff(
                        cleanedMask,
                        _previousCandidateMask,
                        candidateChangeMask);
                }

                Cv2.FindContours(
                    cleanedMask,
                    out Point[][] contours,
                    out _,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);

                List<Rect> candidateRects = new List<Rect>();
                double selectedScore = 0;
                double selectedArea = 0;

                foreach (Point[] contour in contours)
                {
                    double area = Cv2.ContourArea(contour);

                    if (area < minimumArea || area > maximumArea)
                    {
                        continue;
                    }

                    Rect rect = Cv2.BoundingRect(contour);
                    double rectangleArea = Math.Max(1, rect.Width * rect.Height);
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

                    // 2026-08-18: 색상만 고온인 고정 구조물은 후보가 아니다.
                    // 실제 화염은 국부 픽셀 움직임과 외곽 형상 변화가 함께
                    // 지속되어야 하며, 카메라 전체가 움직인 프레임도 제외한다.
                    if (fillRatio < 0.005 ||
                        (rectangleAreaRatio > 0.75 && fillRatio > 0.30) ||
                        (rectangleAreaRatio > 0.08 && aspectRatio > 2.2 &&
                         fillRatio > 0.18 && solidity > 0.72) ||
                        (solidity > 0.90 && fillRatio > 0.50 && irregularity < 1.25) ||
                        !hasMotionReference ||
                        !hasCandidateReference ||
                        globalMotionRatio > 0.25 ||
                        motionRatio < 0.010 ||
                        hotMotionRatio < 0.018 ||
                        shapeChangeRatio < 0.020 ||
                        (rectangleAreaRatio > 0.12 && shapeChangeRatio < 0.060) ||
                        aspectRatio < 0.05 || aspectRatio > 20.0)
                    {
                        continue;
                    }

                    candidateRects.Add(rect);
                }

                List<Rect> mergedRects = MergeNearbyRects(
                    candidateRects,
                    frame.Width,
                    frame.Height,
                    fireBoxGroupingMode);

                Rect selectedRect = Rect.Empty;

                foreach (Rect rect in mergedRects)
                {
                    double area = rect.Width * rect.Height;
                    double centerBias = 1.0;

                    if (_trackedCandidateRect != Rect.Empty)
                    {
                        centerBias += IntersectionOverUnion(
                            rect,
                            _trackedCandidateRect) * 2.0;
                    }

                    double score = area * centerBias;

                    if (score <= selectedScore)
                    {
                        continue;
                    }

                    selectedRect = rect;
                    selectedArea = area;
                    selectedScore = score;
                }

                if (selectedRect != Rect.Empty)
                {
                    selectedRect = SmoothTrackedRect(
                        selectedRect,
                        frame.Width,
                        frame.Height);
                }

                bool previousState = _isFireCandidateDetected;
                UpdateConfirmation(selectedRect != Rect.Empty);

                currentGray.CopyTo(_previousGray);
                cleanedMask.CopyTo(_previousCandidateMask);

                if (_isFireCandidateDetected && selectedRect != Rect.Empty)
                {
                    if (mergedRects.Count == 1)
                    {
                        DrawDetectionBox(frame, selectedRect);
                    }
                    else
                    {
                        foreach (Rect rect in mergedRects)
                        {
                            DrawDetectionBox(frame, rect);
                        }

                    }

                }

                return new ThermalFireDetectionResult(
                    _isFireCandidateDetected,
                    previousState != _isFireCandidateDetected,
                    selectedArea);
            }

        }

        /// <summary>
        /// MergeNearbyRects 동작 수행 함수.
        /// </summary>
        private static List<Rect> MergeNearbyRects(
            IList<Rect> source,
            int frameWidth,
            int frameHeight,
            int fireBoxGroupingMode)
        {
            List<Rect> merged = new List<Rect>(source);

            // 2026-08-14: Mode 1 encloses every detected flame candidate in one BBox.
            if (fireBoxGroupingMode == 1 && merged.Count > 0)
            {
                Rect unified = merged[0];
                for (int index = 1; index < merged.Count; index++)
                {
                    unified = Union(unified, merged[index]);
                }

                return new List<Rect>
                {
                    ExpandRect(unified, 4, 4, frameWidth, frameHeight)
                };
            }
            // 2026-08-14: 인접한 고온 화염 조각은 하나의 BBox로 병합한다.
            int horizontalGap = Math.Max(6, frameWidth / 120);
            int verticalGap = Math.Max(8, frameHeight / 100);

            bool changed;

            do
            {
                changed = false;

                for (int first = 0; first < merged.Count && !changed; first++)
                {
                    Rect expandedFirst = ExpandRect(
                        merged[first],
                        horizontalGap,
                        verticalGap,
                        frameWidth,
                        frameHeight);

                    for (int second = first + 1; second < merged.Count; second++)
                    {
                        if (!Intersects(expandedFirst, merged[second]))
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

            for (int index = 0; index < merged.Count; index++)
            {
                merged[index] = ExpandRect(merged[index], 4, 4, frameWidth, frameHeight);
            }

            merged.Sort((left, right) =>
                (right.Width * right.Height).CompareTo(left.Width * left.Height));
            if (merged.Count > 8)
            {
                merged.RemoveRange(8, merged.Count - 8);
            }

            return merged;
        }

        /// <summary>
        /// DrawDetectionBox 동작 수행 함수.
        /// </summary>
        private static void DrawDetectionBox(Mat frame, Rect rect)
        {
            Cv2.Rectangle(frame, rect, new Scalar(0, 0, 255), 3);
            Cv2.PutText(
                frame,
                "FIRE DETECTION",
                new Point(rect.X, Math.Max(24, rect.Y - 8)),
                HersheyFonts.HersheySimplex,
                0.8,
                new Scalar(0, 0, 255),
                2);
        }

        /// <summary>
        /// SmoothTrackedRect 동작 수행 함수.
        /// </summary>
        private Rect SmoothTrackedRect(Rect current, int width, int height)
        {
            if (_trackedCandidateRect == Rect.Empty ||
                IntersectionOverUnion(current, _trackedCandidateRect) < 0.08)
            {
                _trackedCandidateRect = current;
                return current;
            }

            const double currentWeight = 0.40;
            Rect smoothed = new Rect(
                (int)Math.Round(_trackedCandidateRect.X * (1 - currentWeight) + current.X * currentWeight),
                (int)Math.Round(_trackedCandidateRect.Y * (1 - currentWeight) + current.Y * currentWeight),
                (int)Math.Round(_trackedCandidateRect.Width * (1 - currentWeight) + current.Width * currentWeight),
                (int)Math.Round(_trackedCandidateRect.Height * (1 - currentWeight) + current.Height * currentWeight));

            _trackedCandidateRect = ExpandRect(smoothed, 4, 4, width, height);
            return _trackedCandidateRect;
        }

        /// <summary>
        /// ExpandRect 동작 수행 함수.
        /// </summary>
        private static Rect ExpandRect(Rect rect, int xPadding, int yPadding, int width, int height)
        {
            int left = Math.Max(0, rect.X - xPadding);
            int top = Math.Max(0, rect.Y - yPadding);
            int right = Math.Min(width, rect.X + rect.Width + xPadding);
            int bottom = Math.Min(height, rect.Y + rect.Height + yPadding);
            return new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        /// <summary>
        /// Intersects 동작 수행 함수.
        /// </summary>
        private static bool Intersects(Rect first, Rect second) =>
            first.X < second.X + second.Width &&
            first.X + first.Width > second.X &&
            first.Y < second.Y + second.Height &&
            first.Y + first.Height > second.Y;

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
        /// IntersectionOverUnion 동작 수행 함수.
        /// </summary>
        private static double IntersectionOverUnion(Rect first, Rect second)
        {
            int left = Math.Max(first.X, second.X);
            int top = Math.Max(first.Y, second.Y);
            int right = Math.Min(first.X + first.Width, second.X + second.Width);
            int bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
            double intersection = Math.Max(0, right - left) * Math.Max(0, bottom - top);
            double union = first.Width * first.Height + second.Width * second.Height - intersection;
            return union <= 0 ? 0 : intersection / union;
        }

        /// <summary>
        /// CreateHotPixelMask 생성 및 변환 함수.
        /// </summary>
        private static Mat CreateHotPixelMask(
            Mat frame,
            int threshold)
        {
            using (Mat grayscale = new Mat())
            using (Mat blurred = new Mat())
            using (Mat brightContrast = new Mat())
            using (Mat darkContrast = new Mat())
            using (Mat brightContrastMask = new Mat())
            using (Mat darkContrastMask = new Mat())
            using (Mat contrastMask = new Mat())
            {
                if (frame.Channels() == 1)
                {
                    frame.CopyTo(grayscale);
                }
                else
                {
                    Cv2.CvtColor(frame, grayscale, ColorConversionCodes.BGR2GRAY);
                }

                Cv2.GaussianBlur(grayscale, blurred, new Size(31, 31), 0);

                Cv2.Subtract(grayscale, blurred, brightContrast);
                Cv2.Subtract(blurred, grayscale, darkContrast);
                Cv2.Threshold(brightContrast, brightContrastMask, 12, 255, ThresholdTypes.Binary);
                Cv2.Threshold(darkContrast, darkContrastMask, 12, 255, ThresholdTypes.Binary);
                Cv2.BitwiseOr(brightContrastMask, darkContrastMask, contrastMask);

                if (frame.Channels() == 1)
                {
                    Mat brightIntensityMask = new Mat();
                    Mat darkIntensityMask = new Mat();
                    Cv2.Threshold(grayscale, brightIntensityMask, threshold, 255, ThresholdTypes.Binary);
                    Cv2.Threshold(grayscale, darkIntensityMask, 255 - threshold, 255, ThresholdTypes.BinaryInv);
                    Cv2.BitwiseAnd(brightIntensityMask, brightContrastMask, brightIntensityMask);
                    Cv2.BitwiseAnd(darkIntensityMask, darkContrastMask, darkIntensityMask);
                    Mat intensityMask = Cv2.CountNonZero(brightIntensityMask) <= Cv2.CountNonZero(darkIntensityMask)
                        ? brightIntensityMask.Clone()
                        : darkIntensityMask.Clone();
                    brightIntensityMask.Dispose();
                    darkIntensityMask.Dispose();
                    return intensityMask;
                }

                using (Mat hsv = new Mat())
                using (Mat redLow = new Mat())
                using (Mat redHigh = new Mat())
                using (Mat orange = new Mat())
                {
                    Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);
                    Cv2.InRange(hsv, new Scalar(0, 100, threshold), new Scalar(12, 255, 255), redLow);
                    Cv2.InRange(hsv, new Scalar(170, 100, threshold), new Scalar(179, 255, 255), redHigh);
                    Cv2.InRange(hsv, new Scalar(13, 100, threshold), new Scalar(35, 255, 255), orange);

                    Mat combinedMask = new Mat();
                    Cv2.BitwiseOr(redLow, redHigh, combinedMask);
                    Cv2.BitwiseOr(combinedMask, orange, combinedMask);
                    Cv2.BitwiseAnd(combinedMask, contrastMask, combinedMask);
                    return combinedMask;
                }

            }

        }

        /// <summary>
        /// UpdateConfirmation 갱신 함수.
        /// </summary>
        private void UpdateConfirmation(bool hasCandidate)
        {
            bool previous = _isFireCandidateDetected;

            if (hasCandidate)
            {
                _candidateFrameCount++;
                _clearFrameCount = 0;

                if (_candidateFrameCount >= ConfirmFrameCount)
                {
                    _isFireCandidateDetected = true;
                }

            }
            else
            {
                _candidateFrameCount = 0;
                _clearFrameCount++;

                if (_clearFrameCount >= ClearFrameCount)
                {
                    _isFireCandidateDetected = false;
                }

            }

            if (previous != _isFireCandidateDetected)
            {
                ConsoleLogHelper.State(
                    "THERMAL FIRE",
                    _isFireCandidateDetected
                        ? "Candidate confirmed / PERSISTENCE=6 frames"
                        : "Candidate cleared / PERSISTENCE=8 frames");
            }

        }

        /// <summary>
        /// ResetDetectionState 동작 수행 함수.
        /// </summary>
        private ThermalFireDetectionResult ResetDetectionState()
        {
            bool changed = _isFireCandidateDetected;
            _candidateFrameCount = 0;
            _clearFrameCount = 0;
            _isFireCandidateDetected = false;
            _trackedCandidateRect = Rect.Empty;
            if (_previousGray != null)
            {
                _previousGray.Dispose();
            }
            _previousGray = new Mat();
            if (_previousCandidateMask != null)
            {
                _previousCandidateMask.Dispose();
            }
            _previousCandidateMask = new Mat();

            return new ThermalFireDetectionResult(false, changed, 0);
        }

    }

    internal struct ThermalFireDetectionResult
    {
        /// <summary>
        /// ThermalFireDetectionResult 동작 수행 함수.
        /// </summary>
        internal ThermalFireDetectionResult(
            bool isDetected,
            bool stateChanged,
            double candidateArea)
        {
            IsDetected = isDetected;
            StateChanged = stateChanged;
            CandidateArea = candidateArea;
        }

        internal bool IsDetected { get; }
        internal bool StateChanged { get; }
        internal double CandidateArea { get; }
    }

}
