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

        internal ThermalFireDetectionResult Process(
            Mat frame,
            bool isEnabled,
            double hotThresholdRatio,
            double minimumAreaRatio)
        {
            if (!isEnabled || frame == null || frame.Empty())
            {
                return ResetDetectionState();
            }

            int threshold =
                (int)Math.Round(Math.Max(0, Math.Min(1, hotThresholdRatio)) * 255);

            double frameArea = frame.Width * frame.Height;
            double minimumArea = Math.Max(64, frameArea * minimumAreaRatio);
            double maximumArea = frameArea * 0.18;

            using (Mat mask = CreateHotPixelMask(
                       frame,
                       threshold))
            using (Mat cleanedMask = new Mat())
            using (Mat openKernel = Cv2.GetStructuringElement(
                       MorphShapes.Ellipse,
                       new Size(3, 3)))
            using (Mat closeKernel = Cv2.GetStructuringElement(
                       MorphShapes.Ellipse,
                       new Size(5, 5)))
            {
                Cv2.MorphologyEx(mask, cleanedMask, MorphTypes.Open, openKernel);
                Cv2.MorphologyEx(cleanedMask, cleanedMask, MorphTypes.Close, closeKernel);

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

                    if (fillRatio < 0.12 || fillRatio > 0.88 ||
                        aspectRatio < 0.20 || aspectRatio > 5.0 ||
                        rect.Width > frame.Width * 0.65 ||
                        rect.Height > frame.Height * 0.65)
                    {
                        continue;
                    }

                    candidateRects.Add(rect);
                }

                List<Rect> mergedRects = MergeNearbyRects(
                    candidateRects,
                    frame.Width,
                    frame.Height);

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

                if (_isFireCandidateDetected && selectedRect != Rect.Empty)
                {
                    Cv2.Rectangle(frame, selectedRect, new Scalar(0, 0, 255), 3);
                    Cv2.PutText(
                        frame,
                        "FIRE DETECTOR",
                        new Point(selectedRect.X, Math.Max(24, selectedRect.Y - 8)),
                        HersheyFonts.HersheySimplex,
                        0.8,
                        new Scalar(0, 0, 255),
                        2);
                }

                return new ThermalFireDetectionResult(
                    _isFireCandidateDetected,
                    previousState != _isFireCandidateDetected,
                    selectedArea);
            }
        }

        private static List<Rect> MergeNearbyRects(
            IList<Rect> source,
            int frameWidth,
            int frameHeight)
        {
            List<Rect> merged = new List<Rect>(source);
            int horizontalGap = Math.Max(8, frameWidth / 80);
            int verticalGap = Math.Max(8, frameHeight / 60);

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

            return merged;
        }

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

        private static Rect ExpandRect(Rect rect, int xPadding, int yPadding, int width, int height)
        {
            int left = Math.Max(0, rect.X - xPadding);
            int top = Math.Max(0, rect.Y - yPadding);
            int right = Math.Min(width, rect.X + rect.Width + xPadding);
            int bottom = Math.Min(height, rect.Y + rect.Height + yPadding);
            return new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        private static bool Intersects(Rect first, Rect second) =>
            first.X < second.X + second.Width &&
            first.X + first.Width > second.X &&
            first.Y < second.Y + second.Height &&
            first.Y + first.Height > second.Y;

        private static Rect Union(Rect first, Rect second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int right = Math.Max(first.X + first.Width, second.X + second.Width);
            int bottom = Math.Max(first.Y + first.Height, second.Y + second.Height);
            return new Rect(left, top, right - left, bottom - top);
        }

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
                    Mat intensityMask = new Mat();
                    Cv2.Threshold(grayscale, brightIntensityMask, threshold, 255, ThresholdTypes.Binary);
                    Cv2.Threshold(grayscale, darkIntensityMask, 255 - threshold, 255, ThresholdTypes.BinaryInv);
                    Cv2.BitwiseOr(brightIntensityMask, darkIntensityMask, intensityMask);
                    brightIntensityMask.Dispose();
                    darkIntensityMask.Dispose();
                    Cv2.BitwiseAnd(intensityMask, contrastMask, intensityMask);
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

        private ThermalFireDetectionResult ResetDetectionState()
        {
            bool changed = _isFireCandidateDetected;
            _candidateFrameCount = 0;
            _clearFrameCount = 0;
            _isFireCandidateDetected = false;
            _trackedCandidateRect = Rect.Empty;

            return new ThermalFireDetectionResult(false, changed, 0);
        }
    }

    internal struct ThermalFireDetectionResult
    {
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
