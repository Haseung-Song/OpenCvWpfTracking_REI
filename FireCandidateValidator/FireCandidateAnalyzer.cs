using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

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
        // 2026-08-25: REI/MOE 검증 프로그램도 동일한 화재 후보 알고리즘과
        // 오류 처리 정책을 사용하며, 반복 오류 출력은 5초 간격으로 제한한다.
        private DateTime _lastAnalyzeErrorLogTime = DateTime.MinValue;
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
            try
            {
                return AnalyzeCore(
                    source,
                    thresholdRatio,
                    minimumAreaRatio,
                    confirmationFrameCount,
                    fireBoxGroupingMode);
            }
            catch (Exception ex)
            {
                DateTime now = DateTime.Now;
                if ((now - _lastAnalyzeErrorLogTime).TotalSeconds >= 5)
                {
                    _lastAnalyzeErrorLogTime = now;
                    Console.Error.WriteLine(
                        "[FIRE CANDIDATE ERROR] Analyze failed / " +
                        ex.Message);
                }

                Reset();
                return FireCandidateAnalysis.Empty;
            }

        }

        /// <summary>
        /// 2026-08-25 REI/MOE 공통 검증용 화재 후보 판정 본체.
        /// </summary>
        private FireCandidateAnalysis AnalyzeCore(
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
            // 2026-08-21: 10~30 px는 Bounding Box 한 변 기준(약 100~900 px)이다.
            // 24 px contour부터 Small Fire lane에서 확인하고 그보다 큰 후보는
            // 기존 사용자가 정한 화면비 기반 최소 면적을 적용한다.
            double configuredMinimumArea =
                Math.Max(16.0, frameArea * safeAreaRatio);
            double minimumArea = 24.0;
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
                bool isSmallFireCandidate =
                    rect.Width <= 36 &&
                    rect.Height <= 36 &&
                    rectangleArea <= 1296;
                // 2026-08-24: 마스크에 충분히 크게 형성된 세로형·불규칙 화염은
                // 압축 영상에서 프레임 차가 작아도 실제 화염 후보로 보존한다.
                // 작은 점광원은 이 경로를 통과하지 않으므로 기존 시간축 검사를 유지한다.
                bool isStrongLargeFireCandidate =
                    IsStrongLargeFlameRect(rect, cleanedMask) &&
                    (irregularity >= 1.20 || solidity <= 0.92);
                if (!isSmallFireCandidate &&
                    area < configuredMinimumArea)
                {
                    continue;
                }
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
                    (!isSmallFireCandidate &&
                     solidity > 0.90 && fillRatio > 0.50 && irregularity < 1.25) ||
                    // 2026-08-24: 작은 정적 전등도 시간축 검사를 반드시 통과시킨다.
                    // 작은 실제 화염은 낮은 움직임 또는 외곽 변화 중 하나가 있으면 유지한다.
                    (requiresTemporalMotion &&
                     !isStrongLargeFireCandidate &&
                     (!hasMotionReference ||
                      !hasCandidateReference ||
                      globalMotionRatio > 0.25 ||
                      motionRatio < (isSmallFireCandidate ? 0.004 : 0.010) ||
                      (hotMotionRatio < (isSmallFireCandidate ? 0.006 : 0.018) &&
                       shapeChangeRatio < (isSmallFireCandidate ? 0.008 : 0.020)) ||
                      (rectangleAreaRatio > 0.12 && shapeChangeRatio < 0.060))) ||
                    aspectRatio < 0.05 || aspectRatio > 20.0)
                {
                    continue;
                }

                rawCandidates.Add(rect);
                largestAreaRatio = Math.Max(largestAreaRatio, area / frameArea);
            }

            IList<Rect> verticalFlameCandidates =
                ExtractVerticalFlameCandidates(source, threshold);
            if (requiresTemporalMotion)
            {
                // 2026-08-24: 충분히 큰 세로형 화염은 낮은 프레임 차에서도 유지하고,
                // 작은 후보만 시간축 검사를 거쳐 고정 전등 오탐을 억제한다.
                verticalFlameCandidates = verticalFlameCandidates
                    .Where(rect =>
                        IsStrongLargeFlameRect(rect, cleanedMask) ||
                        HasTemporalFlameEvidence(
                            rect,
                            motionMask,
                            cleanedMask,
                            candidateChangeMask,
                            hasMotionReference,
                            hasCandidateReference,
                            globalMotionRatio))
                    .ToList();
            }

            if (verticalFlameCandidates.Count > 0)
            {
                rawCandidates = new List<Rect>(verticalFlameCandidates);
            }

            List<Rect> candidates = MergeCandidates(
                SuppressNestedAndReflectionCandidates(rawCandidates),
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
        /// [2026-08-24] 세로 방향으로 충분히 크게 형성된 고온 마스크를
        /// 강한 대형 화염 증거로 판정한다. 작은 전등과 수평 구조물은 제외한다.
        /// </summary>
        private static bool IsStrongLargeFlameRect(Rect rect, Mat candidateMask)
        {
            if (candidateMask == null || candidateMask.Empty() ||
                rect.Width <= 0 || rect.Height <= 0)
            {
                return false;
            }

            double frameArea = Math.Max(1.0, candidateMask.Width * candidateMask.Height);
            double rectangleArea = Math.Max(1.0, rect.Width * rect.Height);
            double aspectRatio = rect.Width / (double)Math.Max(1, rect.Height);

            using (Mat candidateRoi = new Mat(candidateMask, rect))
            {
                double fillRatio = Cv2.CountNonZero(candidateRoi) / rectangleArea;

                return rectangleArea / frameArea >= 0.0015 &&
                       rect.Height >= Math.Max(32, candidateMask.Height * 0.04) &&
                       aspectRatio >= 0.08 && aspectRatio <= 1.60 &&
                       fillRatio >= 0.025;
            }

        }

        /// <summary>
        /// [2026-08-24] 동영상의 세로 화염 후보가 프레임 간 움직임 또는 외곽 변화를
        /// 포함하는지 검사한다. 정적인 전등 점광원은 제외하되 작은 화염은 보존한다.
        /// </summary>
        private static bool HasTemporalFlameEvidence(
            Rect rect,
            Mat motionMask,
            Mat candidateMask,
            Mat candidateChangeMask,
            bool hasMotionReference,
            bool hasCandidateReference,
            double globalMotionRatio)
        {
            if (!hasMotionReference ||
                !hasCandidateReference ||
                globalMotionRatio > 0.25 ||
                rect.Width <= 0 ||
                rect.Height <= 0)
            {
                return false;
            }

            using (Mat motionRoi = new Mat(motionMask, rect))
            using (Mat candidateRoi = new Mat(candidateMask, rect))
            using (Mat changeRoi = new Mat(candidateChangeMask, rect))
            using (Mat hotMotion = new Mat())
            {
                double rectangleArea = Math.Max(1.0, rect.Width * rect.Height);
                double candidatePixels = Math.Max(1.0, Cv2.CountNonZero(candidateRoi));
                double motionRatio = Cv2.CountNonZero(motionRoi) / rectangleArea;
                Cv2.BitwiseAnd(motionRoi, candidateRoi, hotMotion);
                double hotMotionRatio = Cv2.CountNonZero(hotMotion) / candidatePixels;
                double shapeChangeRatio = Cv2.CountNonZero(changeRoi) / candidatePixels;

                return motionRatio >= 0.004 &&
                       (hotMotionRatio >= 0.006 || shapeChangeRatio >= 0.008);
            }

        }

        /// <summary>
        /// 촛불 영상에서 원형 보케·촛농·받침 반사보다 세로로 긴 화염 본체를
        /// 우선 검출한다. 색상 팔레트와 흑백 WHITE/BLACK HOT을 모두 지원한다.
        /// </summary>
        private static IList<Rect> ExtractVerticalFlameCandidates(
            Mat source,
            int threshold)
        {
            using (Mat bgr = EnsureBgr(source))
            using (Mat hsv = new Mat())
            using (Mat gray = new Mat())
            using (Mat whiteCore = new Mat())
            using (Mat yellowCore = new Mat())
            using (Mat brightCore = new Mat())
            using (Mat darkCore = new Mat())
            using (Mat coreMask = new Mat())
            {
                Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

                if (Cv2.Mean(hsv).Val1 >= 20)
                {
                    Cv2.InRange(
                        hsv,
                        new Scalar(0, 0, Math.Max(200, threshold)),
                        new Scalar(179, 125, 255),
                        whiteCore);
                    Cv2.InRange(
                        hsv,
                        new Scalar(15, 45, Math.Max(180, threshold - 20)),
                        new Scalar(45, 255, 255),
                        yellowCore);
                    Cv2.BitwiseOr(whiteCore, yellowCore, coreMask);
                }
                else
                {
                    Cv2.Threshold(gray, brightCore, threshold, 255, ThresholdTypes.Binary);
                    Cv2.Threshold(gray, darkCore, 255 - threshold, 255, ThresholdTypes.BinaryInv);
                    int brightCount = Cv2.CountNonZero(brightCore);
                    int darkCount = Cv2.CountNonZero(darkCore);
                    if (brightCount > 0 && (darkCount == 0 || brightCount <= darkCount))
                    {
                        brightCore.CopyTo(coreMask);
                    }
                    else
                    {
                        darkCore.CopyTo(coreMask);
                    }

                }

                using (Mat openKernel =
                       Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                using (Mat closeKernel =
                       Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 9)))
                {
                    Cv2.MorphologyEx(coreMask, coreMask, MorphTypes.Open, openKernel);
                    Cv2.MorphologyEx(coreMask, coreMask, MorphTypes.Close, closeKernel);
                }

                Cv2.FindContours(
                    coreMask,
                    out Point[][] contours,
                    out _,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);

                List<Rect> flames = new List<Rect>();
                double frameArea = Math.Max(1.0, source.Width * source.Height);
                foreach (Point[] contour in contours)
                {
                    double area = Cv2.ContourArea(contour);
                    Rect rect = Cv2.BoundingRect(contour);
                    double fillRatio = area / Math.Max(1.0, rect.Width * rect.Height);
                    bool isSmallFlame =
                        rect.Width <= 36 &&
                        rect.Height <= 36 &&
                        rect.Width * rect.Height >= 64;
                    bool isVerticalFlame =
                        rect.Height >= 10 &&
                        rect.Height >= rect.Width * 1.25 &&
                        fillRatio >= 0.08 &&
                        rect.Width * rect.Height <= frameArea * 0.30;

                    if (area >= 20 && (isSmallFlame || isVerticalFlame))
                    {
                        flames.Add(
                            Expand(
                                rect,
                                3,
                                4,
                                source.Width,
                                source.Height));
                    }

                }

                return flames;
            }

        }

        /// <summary>
        /// 큰 화염 내부의 작은 조각과 화염 바로 아래의 짧은 반사광 후보를 제거한다.
        /// 실제로 떨어져 있는 독립 화염은 보존한다.
        /// </summary>
        private static IList<Rect> SuppressNestedAndReflectionCandidates(
            IList<Rect> source)
        {
            List<Rect> filtered = new List<Rect>();

            for (int candidateIndex = 0; candidateIndex < source.Count; candidateIndex++)
            {
                Rect candidate = source[candidateIndex];
                bool suppressed = false;

                for (int anchorIndex = 0; anchorIndex < source.Count; anchorIndex++)
                {
                    if (candidateIndex == anchorIndex)
                    {
                        continue;
                    }

                    Rect anchor = source[anchorIndex];
                    int intersectionWidth =
                        Math.Max(0, Math.Min(candidate.Right, anchor.Right) - Math.Max(candidate.Left, anchor.Left));
                    int intersectionHeight =
                        Math.Max(0, Math.Min(candidate.Bottom, anchor.Bottom) - Math.Max(candidate.Top, anchor.Top));
                    double candidateArea = Math.Max(1.0, candidate.Width * candidate.Height);
                    double coveredRatio = intersectionWidth * intersectionHeight / candidateArea;

                    bool nestedFragment =
                        anchor.Width * anchor.Height > candidateArea * 1.35 &&
                        coveredRatio >= 0.60;
                    bool shortReflectionBelowFlame =
                        anchor.Height >= anchor.Width * 1.25 &&
                        anchor.Height >= candidate.Height * 1.8 &&
                        candidate.Height <= anchor.Height * 0.45 &&
                        Math.Abs(
                            candidate.X + candidate.Width / 2.0 -
                            anchor.X - anchor.Width / 2.0) <=
                        Math.Max(anchor.Width, candidate.Width) * 0.75 &&
                        candidate.Top >= anchor.Top + anchor.Height * 0.55 &&
                        candidate.Top <= anchor.Bottom + Math.Max(30, anchor.Height / 2);

                    if (nestedFragment || shortReflectionBelowFlame)
                    {
                        suppressed = true;
                        break;
                    }

                }

                if (!suppressed)
                {
                    filtered.Add(candidate);
                }

            }

            return filtered;
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

            // 2026-08-21: 큰 화염 조각은 대표 BBox에 흡수하고 떨어진 촛불은 분리 유지한다.
            List<Rect> grouped = new List<Rect>(source);
            grouped.Sort((left, right) =>
                (right.Width * right.Height).CompareTo(left.Width * left.Height));
            int horizontalGap = Math.Max(6, frameWidth / 120);
            int verticalGap = Math.Max(8, frameHeight / 100);
            bool mergedAny;

            do
            {
                mergedAny = false;
                for (int first = 0; first < grouped.Count && !mergedAny; first++)
                {
                    int fragmentHorizontalGap =
                        Math.Max(horizontalGap, (int)Math.Round(grouped[first].Width * 0.45));
                    int fragmentVerticalGap =
                        Math.Max(verticalGap, (int)Math.Round(grouped[first].Height * 0.75));
                    Rect expanded = Expand(
                        grouped[first],
                        fragmentHorizontalGap,
                        fragmentVerticalGap,
                        frameWidth,
                        frameHeight);
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
            using (Mat brightMask = new Mat())
            using (Mat darkMask = new Mat())
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
                    Cv2.Threshold(grayscale, brightMask, threshold, 255, ThresholdTypes.Binary);
                    Cv2.BitwiseOr(brightMask, contrastMask, redLow);

                    Cv2.Subtract(blurred, grayscale, localContrast);
                    Cv2.Threshold(localContrast, contrastMask, 10, 255, ThresholdTypes.Binary);
                    Cv2.Threshold(grayscale, darkMask, 255 - threshold, 255, ThresholdTypes.BinaryInv);
                    Cv2.BitwiseOr(darkMask, contrastMask, redHigh);

                    int brightPixelCount = Cv2.CountNonZero(brightMask);
                    int darkPixelCount = Cv2.CountNonZero(darkMask);
                    bool useBrightPolarity =
                        brightPixelCount > 0 &&
                        (darkPixelCount == 0 || brightPixelCount <= darkPixelCount);

                    if (useBrightPolarity)
                    {
                        redLow.CopyTo(colorMask);
                    }
                    else
                    {
                        redHigh.CopyTo(colorMask);
                    }

                }
                // 이전 검증 결과 영상을 다시 열었을 때 이미 그려진 순수 적색
                // BBox/문구를 화염으로 재검출하지 않도록 주석 색상만 제외한다.
                using (Mat annotationMask = new Mat())
                using (Mat inverseAnnotationMask = new Mat())
                {
                    Cv2.InRange(
                        bgr,
                        new Scalar(0, 0, 215),
                        new Scalar(45, 45, 255),
                        annotationMask);
                    Cv2.BitwiseNot(annotationMask, inverseAnnotationMask);
                    Cv2.BitwiseAnd(colorMask, inverseAnnotationMask, colorMask);
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
