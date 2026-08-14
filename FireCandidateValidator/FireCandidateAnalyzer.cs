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

        internal FireCandidateAnalysis Analyze(
            Mat source,
            double thresholdRatio,
            double minimumAreaRatio,
            int confirmationFrameCount)
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
            double maximumArea = frameArea * 0.20;

            Mat mask = CreateCandidateMask(source, threshold);
            Mat cleanedMask = new Mat();

            using (Mat openKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
            using (Mat closeKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(7, 7)))
            {
                Cv2.MorphologyEx(mask, cleanedMask, MorphTypes.Open, openKernel);
                Cv2.MorphologyEx(cleanedMask, cleanedMask, MorphTypes.Close, closeKernel);
            }

            mask.Dispose();

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

                // 넓은 건물 외벽, 수평 띠 및 작은 점 노이즈를 후보에서 제외한다.
                if (fillRatio < 0.03 || fillRatio > 0.96 ||
                    aspectRatio < 0.15 || aspectRatio > 5.0 ||
                    rect.Width > source.Width * 0.60 ||
                    rect.Height > source.Height * 0.60)
                {
                    continue;
                }

                rawCandidates.Add(rect);
                largestAreaRatio = Math.Max(largestAreaRatio, area / frameArea);
            }

            List<Rect> candidates = MergeCandidates(
                rawCandidates,
                source.Width,
                source.Height);

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

            return new FireCandidateAnalysis(
                cleanedMask,
                candidates,
                isConfirmed,
                _continuousCandidateFrames,
                largestAreaRatio);
        }

        internal void Reset()
        {
            _continuousCandidateFrames = 0;
        }

        private static List<Rect> MergeCandidates(
            IList<Rect> source,
            int frameWidth,
            int frameHeight)
        {
            List<Rect> merged = new List<Rect>(source);
            int xPadding = Math.Max(10, frameWidth / 70);
            int yPadding = Math.Max(10, frameHeight / 55);
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
        }

        private static Rect Expand(Rect rect, int x, int y, int width, int height)
        {
            int left = Math.Max(0, rect.X - x);
            int top = Math.Max(0, rect.Y - y);
            int right = Math.Min(width, rect.X + rect.Width + x);
            int bottom = Math.Min(height, rect.Y + rect.Height + y);
            return new Rect(left, top, right - left, bottom - top);
        }

        private static bool Intersects(Rect first, Rect second) =>
            first.X < second.X + second.Width && first.X + first.Width > second.X &&
            first.Y < second.Y + second.Height && first.Y + first.Height > second.Y;

        private static Rect Union(Rect first, Rect second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int right = Math.Max(first.X + first.Width, second.X + second.Width);
            int bottom = Math.Max(first.Y + first.Height, second.Y + second.Height);
            return new Rect(left, top, right - left, bottom - top);
        }

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
                Cv2.BitwiseOr(colorMask, intensityMask, colorMask);
                return colorMask.Clone();
            }
        }

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
