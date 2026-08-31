using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace FireCandidateValidator
{
    /// <summary>
    /// 2026-08-27: EO 저채도 연무와 IR 저대비 플룸 후보를 시간축으로 검증하기 위한
    /// 공통 영상 분석기이다. 실제 온도나 확정 화재를 산출하지 않는다.
    /// </summary>
    internal sealed class SmokeCandidateAnalyzer
    {
        private Mat _previousGray = new Mat();
        private Mat _phaseWindow = new Mat();
        private Mat _temporalCandidateMask = new Mat();
        private int _referenceFrameAge;
        private int _continuousCandidateFrames;
        private readonly List<SmokeCandidateTrack> _tracks =
            new List<SmokeCandidateTrack>();

        internal SmokeCandidateAnalysis Analyze(
            Mat source,
            bool isInfrared,
            double minimumAreaRatio,
            double changeThresholdRatio,
            int confirmationFrameCount,
            bool compensateCameraMotion = false)
        {
            if (source == null || source.Empty())
            {
                Reset();
                return SmokeCandidateAnalysis.Empty();
            }

            Mat gray = new Mat();
            Mat blurred = new Mat();
            Mat candidateMask = new Mat();
            Mat changeMask = new Mat();
            Mat brightChangeMask = new Mat();
            Mat darkChangeMask = new Mat();
            Mat neutralMask = new Mat();
            Mat darkNeutralMask = new Mat();
            Mat hsv = new Mat();
            Mat infraredHotMask = new Mat();
            Mat structureEdges = new Mat();
            Mat motionCompensatedReference = new Mat();

            try
            {
                if (source.Channels() == 1)
                {
                    source.CopyTo(gray);
                }
                else
                {
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }

                Cv2.GaussianBlur(gray, blurred, new Size(9, 9), 0);

                bool hasReference =
                    _previousGray != null &&
                    !_previousGray.Empty() &&
                    _previousGray.Size() == blurred.Size();

                if (!hasReference)
                {
                    blurred.CopyTo(_previousGray);
                    return new SmokeCandidateAnalysis(
                        candidateMask,
                        new List<Rect>(),
                        false,
                        0,
                        0.0);
                }

                Mat referenceFrame = _previousGray;
                if (compensateCameraMotion &&
                    TryCompensateCameraMotion(
                        blurred,
                        motionCompensatedReference))
                {
                    referenceFrame = motionCompensatedReference;
                }

                /*
                 * 2026-08-31: EO 연기를 밝아지는 흰 연기와 어두워지는 검은 연기로
                 * 분리 분석한다. 검은 연기는 그림자·노출 변화 오탐을 줄이기 위해
                 * 흰 연기보다 높은 변화 임계값과 별도의 무채색 범위를 사용한다.
                 */
                if (isInfrared || source.Channels() == 1)
                {
                    Cv2.Absdiff(
                        blurred,
                        referenceFrame,
                        changeMask);
                }
                else
                {
                    Cv2.Subtract(
                        blurred,
                        referenceFrame,
                        brightChangeMask);
                    Cv2.Subtract(
                        referenceFrame,
                        blurred,
                        darkChangeMask);
                }
                int changeThreshold =
                    Math.Max(
                        4,
                        Math.Min(
                            40,
                            (int)Math.Round(changeThresholdRatio * 255.0)));
                if (isInfrared || source.Channels() == 1)
                {
                    Cv2.Threshold(
                        changeMask,
                        changeMask,
                        changeThreshold,
                        255,
                        ThresholdTypes.Binary);
                }
                else
                {
                    int darkChangeThreshold =
                        Math.Max(
                            changeThreshold + 3,
                            (int)Math.Round(changeThreshold * 1.35));

                    Cv2.Threshold(
                        brightChangeMask,
                        brightChangeMask,
                        changeThreshold,
                        255,
                        ThresholdTypes.Binary);
                    Cv2.Threshold(
                        darkChangeMask,
                        darkChangeMask,
                        darkChangeThreshold,
                        255,
                        ThresholdTypes.Binary);
                    Cv2.BitwiseOr(
                        brightChangeMask,
                        darkChangeMask,
                        changeMask);
                }

                if (isInfrared || source.Channels() == 1)
                {
                    // 2026-08-27: IR 연기 후보는 저대비 이동 영역만 사용한다.
                    // 프레임 평균보다 현저히 밝은 고온 핵과 주변부는 FIRE 판정을 우선하도록 제외한다.
                    changeMask.CopyTo(candidateMask);

                    Cv2.MeanStdDev(
                        blurred,
                        out Scalar infraredMean,
                        out Scalar infraredDeviation);

                    double hotPixelThreshold =
                        Math.Min(
                            250.0,
                            Math.Max(
                                175.0,
                                infraredMean.Val0 +
                                Math.Max(
                                    18.0,
                                    infraredDeviation.Val0 * 1.35)));

                    Cv2.Threshold(
                        blurred,
                        infraredHotMask,
                        hotPixelThreshold,
                        255,
                        ThresholdTypes.Binary);

                    using (Mat hotCoreKernel =
                           Cv2.GetStructuringElement(
                               MorphShapes.Ellipse,
                               new Size(9, 9)))
                    {
                        Cv2.Dilate(
                            infraredHotMask,
                            infraredHotMask,
                            hotCoreKernel);
                    }

                    candidateMask.SetTo(
                        Scalar.Black,
                        infraredHotMask);
                }
                else
                {
                    Cv2.CvtColor(source, hsv, ColorConversionCodes.BGR2HSV);
                    Cv2.InRange(
                        hsv,
                        new Scalar(0, 0, 35),
                        new Scalar(179, 105, 245),
                        neutralMask);
                    Cv2.BitwiseAnd(
                        brightChangeMask,
                        neutralMask,
                        candidateMask);

                    /*
                     * 2026-08-31: 검은 연기는 현재 프레임이 장기 기준보다 어두워진
                     * 저채도 영역에서 추출한다. 완전 암부와 과도한 채도 영역은 제외하고,
                     * 이후 공통 형상·이동·확산·상향 지속성 검증을 그대로 통과시킨다.
                     */
                    Cv2.InRange(
                        hsv,
                        new Scalar(0, 0, 15),
                        new Scalar(179, 135, 210),
                        darkNeutralMask);

                    using (Mat darkCandidateMask = new Mat())
                    {
                        Cv2.BitwiseAnd(
                            darkChangeMask,
                            darkNeutralMask,
                            darkCandidateMask);
                        Cv2.BitwiseOr(
                            candidateMask,
                            darkCandidateMask,
                            candidateMask);
                    }

                    /*
                     * 2026-08-28: 건물 모서리, 창틀 및 흔들리는 수목의 선명한
                     * 윤곽은 연기보다 Canny 경계 밀도가 높다. 강한 경계를 넓혀
                     * 후보에서 제외하되 연기 내부의 완만한 밝기 변화는 유지한다.
                     */
                    using (Mat expandedEdges = new Mat())
                    using (Mat edgeKernel =
                           Cv2.GetStructuringElement(
                               MorphShapes.Ellipse,
                               new Size(3, 3)))
                    {
                        Cv2.Canny(
                            blurred,
                            structureEdges,
                            70.0,
                            150.0);
                        Cv2.Dilate(
                            structureEdges,
                            expandedEdges,
                            edgeKernel);
                        candidateMask.SetTo(
                            Scalar.Black,
                            expandedEdges);
                    }
                }

                using (Mat openKernel =
                       Cv2.GetStructuringElement(
                           MorphShapes.Ellipse,
                           new Size(3, 3)))
                using (Mat closeKernel =
                       Cv2.GetStructuringElement(
                           MorphShapes.Ellipse,
                           new Size(13, 13)))
                {
                    Cv2.MorphologyEx(
                        candidateMask,
                        candidateMask,
                        MorphTypes.Open,
                        openKernel);
                    Cv2.MorphologyEx(
                        candidateMask,
                        candidateMask,
                        MorphTypes.Close,
                        closeKernel);
                }

                /*
                 * 2026-08-31: 연기는 프레임마다 외곽과 내부 농도가 달라져 단일
                 * 차분 마스크만 사용하면 BBox가 깜빡인다. 최근 후보를 감쇠 누적해
                 * 짧게 끊긴 몸통과 인접 조각을 유지하되 오래된 후보는 자동 소멸시킨다.
                 */
                if (_temporalCandidateMask.Empty() ||
                    _temporalCandidateMask.Size() != candidateMask.Size())
                {
                    _temporalCandidateMask.Dispose();
                    _temporalCandidateMask =
                        new Mat(candidateMask.Size(), MatType.CV_8UC1, Scalar.Black);
                }

                Cv2.AddWeighted(
                    _temporalCandidateMask,
                    0.82,
                    candidateMask,
                    1.0,
                    0.0,
                    _temporalCandidateMask);
                Cv2.Threshold(
                    _temporalCandidateMask,
                    candidateMask,
                    42.0,
                    255.0,
                    ThresholdTypes.Binary);

                double frameArea = Math.Max(1.0, source.Width * source.Height);
                double globalChangeRatio = Cv2.CountNonZero(changeMask) / frameArea;
                List<Rect> candidates = new List<Rect>();
                double largestAreaRatio = 0.0;

                // 2026-08-28: 정합 후에도 장면 대부분이 변하면 프리셋 도착 또는
                // Zoom 변화로 판단하고 현재 프레임을 새 장기 기준으로 사용한다.
                if (compensateCameraMotion &&
                    globalChangeRatio > 0.18)
                {
                    blurred.CopyTo(_previousGray);
                    _referenceFrameAge = 0;
                    _tracks.Clear();
                    _temporalCandidateMask.SetTo(Scalar.Black);
                    candidateMask.SetTo(Scalar.Black);
                    return new SmokeCandidateAnalysis(
                        candidateMask,
                        new List<Rect>(),
                        false,
                        0,
                        0.0);
                }

                // 2026-08-27: PTZ 잔진동·노출 변화처럼 화면 넓은 영역이 동시에
                // 변하는 경우에는 개별 SMOKE 후보를 만들지 않는다.
                if (globalChangeRatio <= 0.18)
                {
                    Cv2.FindContours(
                        candidateMask,
                        out Point[][] contours,
                        out _,
                        RetrievalModes.External,
                        ContourApproximationModes.ApproxSimple);

                    double minimumArea =
                        Math.Max(
                            96.0,
                            frameArea * Math.Max(0.0005, minimumAreaRatio));
                    double maximumArea =
                        frameArea * (isInfrared ? 0.45 : 0.25);

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

                        double minimumFillRatio =
                            isInfrared ? 0.035 : 0.100;
                        double maximumRectangleAreaRatio =
                            isInfrared ? 0.35 : 0.18;

                        if (fillRatio < minimumFillRatio ||
                            fillRatio > 0.92 ||
                            aspectRatio < 0.08 ||
                            aspectRatio > (isInfrared ? 8.0 : 5.0) ||
                            rectangleAreaRatio > maximumRectangleAreaRatio ||
                            rect.Height < Math.Max(10, source.Height / 90))
                        {
                            continue;
                        }

                        /*
                         * 2026-08-28: EO 건물 외벽·창틀은 후보 마스크가 일부
                         * 끊겨도 원본 ROI에 선명한 직선 경계가 조밀하게 남는다.
                         * 희박한 후보이면서 구조 경계 밀도가 높은 영역만 제외하여
                         * 완만하고 비정형적인 실제 연기 변화는 유지한다.
                         */
                        if (!isInfrared && !structureEdges.Empty())
                        {
                            using (Mat edgeRegion = new Mat(structureEdges, rect))
                            {
                                double edgeDensity =
                                    Cv2.CountNonZero(edgeRegion) / rectangleArea;

                                if (edgeDensity > 0.16 && fillRatio < 0.34)
                                {
                                    continue;
                                }
                            }
                        }

                        candidates.Add(rect);
                        largestAreaRatio =
                            Math.Max(
                                largestAreaRatio,
                                rectangleArea / frameArea);
                    }
                }

                candidates =
                    MergeNearbyCandidates(
                        candidates,
                        source.Width,
                        source.Height);

                candidates.Sort((left, right) =>
                    (right.Width * right.Height).CompareTo(left.Width * left.Height));
                if (candidates.Count > 6)
                {
                    candidates.RemoveRange(6, candidates.Count - 6);
                }

                List<Rect> confirmedCandidates =
                    UpdateCandidateTracks(
                        candidates,
                        Math.Max(1, confirmationFrameCount),
                        source.Width,
                        source.Height);

                _continuousCandidateFrames = 0;
                foreach (SmokeCandidateTrack track in _tracks)
                {
                    if (track.MissingFrames == 0)
                    {
                        _continuousCandidateFrames =
                            Math.Max(
                                _continuousCandidateFrames,
                                track.SeenFrames);
                    }
                }

                bool isConfirmed = confirmedCandidates.Count > 0;

                /*
                 * 2026-08-28: 바로 이전 프레임 대신 약 0.8초 범위의 장기 기준을
                 * 유지하여 천천히 퍼지는 흰 연기의 누적 밝기 변화를 확보한다.
                 * 기준 갱신 프레임은 Track의 짧은 Missing 허용으로 연속성을 유지한다.
                 */
                _referenceFrameAge++;
                if (_referenceFrameAge >= 60)
                {
                    blurred.CopyTo(_previousGray);
                    _referenceFrameAge = 0;
                }

                return new SmokeCandidateAnalysis(
                    candidateMask,
                    confirmedCandidates,
                    isConfirmed,
                    _continuousCandidateFrames,
                    largestAreaRatio);
            }
            catch
            {
                candidateMask.Dispose();
                throw;
            }
            finally
            {
                gray.Dispose();
                blurred.Dispose();
                changeMask.Dispose();
                brightChangeMask.Dispose();
                darkChangeMask.Dispose();
                neutralMask.Dispose();
                darkNeutralMask.Dispose();
                hsv.Dispose();
                infraredHotMask.Dispose();
                structureEdges.Dispose();
                motionCompensatedReference.Dispose();
            }
        }

        /// <summary>
        /// 2026-08-28: AUTO SCAN 및 파노라마 촬영 중 인접 프레임의 전역 평행 이동을
        /// 위상 상관으로 추정하고, 두 이동 방향 중 실제 차이가 작은 정렬 결과를 사용한다.
        /// </summary>
        private bool TryCompensateCameraMotion(
            Mat currentGray,
            Mat alignedPrevious)
        {
            const int AnalysisWidth = 480;
            double scale =
                Math.Min(
                    1.0,
                    AnalysisWidth / (double)currentGray.Width);
            Size analysisSize =
                new Size(
                    Math.Max(1, (int)Math.Round(currentGray.Width * scale)),
                    Math.Max(1, (int)Math.Round(currentGray.Height * scale)));

            using (Mat previousSmall = new Mat())
            using (Mat currentSmall = new Mat())
            using (Mat previousFloat = new Mat())
            using (Mat currentFloat = new Mat())
            using (Mat transform =
                   Mat.Eye(2, 3, MatType.CV_64FC1).ToMat())
            {
                Cv2.Resize(
                    _previousGray,
                    previousSmall,
                    analysisSize,
                    0.0,
                    0.0,
                    InterpolationFlags.Area);
                Cv2.Resize(
                    currentGray,
                    currentSmall,
                    analysisSize,
                    0.0,
                    0.0,
                    InterpolationFlags.Area);
                previousSmall.ConvertTo(
                    previousFloat,
                    MatType.CV_32FC1);
                currentSmall.ConvertTo(
                    currentFloat,
                    MatType.CV_32FC1);
                if (_phaseWindow.Empty() ||
                    _phaseWindow.Size() != analysisSize)
                {
                    _phaseWindow.Dispose();
                    _phaseWindow = new Mat();
                    Cv2.CreateHanningWindow(
                        _phaseWindow,
                        analysisSize,
                        MatType.CV_32FC1);
                }

                Point2d shift =
                    Cv2.PhaseCorrelate(
                        previousFloat,
                        currentFloat,
                        _phaseWindow,
                        out double response);

                if (response < 0.08 ||
                    double.IsNaN(shift.X) ||
                    double.IsNaN(shift.Y) ||
                    Math.Abs(shift.X) > analysisSize.Width * 0.45 ||
                    Math.Abs(shift.Y) > analysisSize.Height * 0.45)
                {
                    return false;
                }

                transform.Set(0, 2, shift.X / scale);
                transform.Set(1, 2, shift.Y / scale);

                Cv2.WarpAffine(
                    _previousGray,
                    alignedPrevious,
                    transform,
                    currentGray.Size(),
                    InterpolationFlags.Linear,
                    BorderTypes.Reflect101);
                return true;
            }
        }

        internal void Reset()
        {
            _continuousCandidateFrames = 0;
            _referenceFrameAge = 0;
            _tracks.Clear();

            if (_temporalCandidateMask != null)
            {
                _temporalCandidateMask.Dispose();
            }

            _temporalCandidateMask = new Mat();

            if (_previousGray != null)
            {
                _previousGray.Dispose();
            }

            _previousGray = new Mat();
        }

        /// <summary>
        /// 2026-08-27: 같은 연기 덩어리가 작은 조각으로 분리된 경우
        /// 근접·중첩 후보를 하나로 병합하여 이벤트와 BBox 중복을 줄인다.
        /// </summary>
        private static List<Rect> MergeNearbyCandidates(
            IList<Rect> source,
            int frameWidth,
            int frameHeight)
        {
            List<Rect> merged =
                new List<Rect>();

            int horizontalGap = Math.Max(10, frameWidth / 60);
            int verticalGap = Math.Max(10, frameHeight / 45);

            foreach (Rect candidate in source)
            {
                Rect current = candidate;

                for (int index = merged.Count - 1; index >= 0; index--)
                {
                    Rect existing = merged[index];
                    Rect expanded =
                        ExpandRect(
                            existing,
                            horizontalGap,
                            verticalGap,
                            frameWidth,
                            frameHeight);

                    if (IntersectionOverUnion(existing, current) < 0.08 &&
                        (expanded & current) == Rect.Empty)
                    {
                        continue;
                    }

                    current = existing | current;
                    merged.RemoveAt(index);
                }

                merged.Add(current);
            }

            return merged;
        }

        /// <summary>
        /// 2026-08-27: 후보별 IoU·중심점·면적 변화와 이동 방향을 추적한다.
        /// 일정 프레임 지속하지 않는 노이즈, 급격한 크기 변화 및 장시간 고정 물체는
        /// 최종 연기 후보에서 제외한다.
        /// </summary>
        private List<Rect> UpdateCandidateTracks(
            IList<Rect> candidates,
            int confirmationFrameCount,
            int frameWidth,
            int frameHeight)
        {
            foreach (SmokeCandidateTrack track in _tracks)
            {
                track.Matched = false;
            }

            foreach (Rect candidate in candidates)
            {
                SmokeCandidateTrack bestTrack = null;
                double bestScore = 0.0;
                Point candidateCenter = GetCenter(candidate);

                foreach (SmokeCandidateTrack track in _tracks)
                {
                    if (track.Matched)
                    {
                        continue;
                    }

                    double intersectionScore =
                        IntersectionOverUnion(
                            track.Rectangle,
                            candidate);
                    Point previousCenter =
                        GetCenter(track.Rectangle);
                    double centerDistance =
                        Math.Sqrt(
                            Math.Pow(candidateCenter.X - previousCenter.X, 2) +
                            Math.Pow(candidateCenter.Y - previousCenter.Y, 2));
                    double allowedDistance =
                        Math.Max(
                            18.0,
                            Math.Max(
                                track.Rectangle.Width,
                                track.Rectangle.Height) * 0.85);
                    double score = intersectionScore;

                    if (score < 0.08 && centerDistance <= allowedDistance)
                    {
                        score = 0.08 + (1.0 - centerDistance / allowedDistance) * 0.20;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTrack = track;
                    }
                }

                if (bestTrack == null || bestScore < 0.08)
                {
                    _tracks.Add(
                        new SmokeCandidateTrack(candidate));
                    continue;
                }

                double previousArea =
                    Math.Max(
                        1.0,
                        bestTrack.Rectangle.Width * bestTrack.Rectangle.Height);
                double currentArea =
                    Math.Max(
                        1.0,
                        candidate.Width * candidate.Height);
                double areaChangeRatio =
                    Math.Max(previousArea, currentArea) /
                    Math.Min(previousArea, currentArea);

                Point previousTrackCenter =
                    GetCenter(bestTrack.Rectangle);
                double deltaX = candidateCenter.X - previousTrackCenter.X;
                double deltaY = candidateCenter.Y - previousTrackCenter.Y;
                double motion = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                /*
                 * 2026-08-31: 실제 연기는 분리·병합 때문에 BBox 면적이 한 프레임에
                 * 크게 바뀔 수 있다. 면적 급변만으로 SeenFrames를 초기화하지 않고
                 * 동적 형상 표본으로 반영하여 같은 Track의 연속성을 유지한다.
                 */
                bestTrack.SeenFrames++;

                if (areaChangeRatio > 2.5)
                {
                    bestTrack.DynamicSamples++;
                    if (currentArea > previousArea)
                    {
                        bestTrack.ExpansionSamples++;
                    }

                    bestTrack.StationaryFrames = 0;
                }
                else
                {
                    double verticalThreshold =
                        Math.Max(1.5, candidate.Height * 0.015);

                    if (deltaY <= -verticalThreshold)
                    {
                        bestTrack.UpwardSamples++;
                    }
                    else if (deltaY >= verticalThreshold)
                    {
                        bestTrack.DownwardSamples++;
                    }

                    double areaDelta =
                        Math.Abs(currentArea - previousArea) / previousArea;

                    if (motion >= 1.5 || areaDelta >= 0.030)
                    {
                        bestTrack.DynamicSamples++;
                    }

                    if (currentArea >= previousArea * 1.025)
                    {
                        bestTrack.ExpansionSamples++;
                    }

                    bestTrack.StationaryFrames =
                        motion < 2.0 && areaDelta < 0.025
                            ? bestTrack.StationaryFrames + 1
                            : 0;
                }

                bestTrack.Rectangle =
                    SmoothRectangle(
                        bestTrack.Rectangle,
                        candidate,
                        frameWidth,
                        frameHeight);
                // 2026-08-31: 작은 분할 조각만 표시하지 않고 같은 Track이 최근에
                // 통과한 영역을 누적하여 실제 연기 기둥의 전체 외곽에 가깝게 복원한다.
                // 장시간 누적 또는 화면의 22%를 넘는 경우 현재 외곽부터 다시 시작해
                // 이동·오탐 때문에 BBox가 화면 전체로 계속 커지는 현상을 제한한다.
                Rect envelope = bestTrack.EnvelopeRectangle | bestTrack.Rectangle;
                double envelopeRatio =
                    envelope.Width * envelope.Height /
                    Math.Max(1.0, frameWidth * (double)frameHeight);
                if (bestTrack.EnvelopeFrames >= 120 || envelopeRatio > 0.22)
                {
                    bestTrack.EnvelopeRectangle = bestTrack.Rectangle;
                    bestTrack.EnvelopeFrames = 1;
                }
                else
                {
                    bestTrack.EnvelopeRectangle = envelope;
                    bestTrack.EnvelopeFrames++;
                }
                bestTrack.MissingFrames = 0;
                bestTrack.Matched = true;
            }

            for (int index = _tracks.Count - 1; index >= 0; index--)
            {
                SmokeCandidateTrack track = _tracks[index];

                if (!track.Matched)
                {
                    track.MissingFrames++;
                }

                if (track.MissingFrames > Math.Max(24, confirmationFrameCount))
                {
                    _tracks.RemoveAt(index);
                }
            }

            List<Rect> confirmed =
                new List<Rect>();
            int stationaryLimit =
                Math.Max(18, confirmationFrameCount * 2);

            foreach (SmokeCandidateTrack track in _tracks)
            {
                int directionalSamples =
                    track.UpwardSamples + track.DownwardSamples;
                bool directionAccepted =
                    directionalSamples < 3 ||
                    track.UpwardSamples + 1 >= track.DownwardSamples;
                int requiredDynamicSamples =
                    Math.Max(4, confirmationFrameCount / 5);
                int requiredPlumeSamples =
                    Math.Max(2, confirmationFrameCount / 10);
                bool plumeEvolutionAccepted =
                    track.DynamicSamples >= requiredDynamicSamples &&
                    track.UpwardSamples + track.ExpansionSamples >=
                    requiredPlumeSamples;

                if (track.MissingFrames <= Math.Max(12, confirmationFrameCount / 3) &&
                    track.SeenFrames >= confirmationFrameCount &&
                    track.StationaryFrames < confirmationFrameCount &&
                    track.StationaryFrames < stationaryLimit &&
                    directionAccepted &&
                    plumeEvolutionAccepted)
                {
                    confirmed.Add(track.EnvelopeRectangle);
                }
            }

            return confirmed;
        }

        /// <summary>
        /// 2026-08-31: 프레임별 연기 분할 변화가 화면 BBox 진동으로 전달되지 않도록
        /// 이전 Track과 현재 후보의 위치·크기를 가중 평균한다.
        /// </summary>
        private static Rect SmoothRectangle(
            Rect previous,
            Rect current,
            int frameWidth,
            int frameHeight)
        {
            const double CurrentWeight = 0.58;
            int x = (int)Math.Round(previous.X * (1.0 - CurrentWeight) + current.X * CurrentWeight);
            int y = (int)Math.Round(previous.Y * (1.0 - CurrentWeight) + current.Y * CurrentWeight);
            int width = (int)Math.Round(previous.Width * (1.0 - CurrentWeight) + current.Width * CurrentWeight);
            int height = (int)Math.Round(previous.Height * (1.0 - CurrentWeight) + current.Height * CurrentWeight);

            return new Rect(
                Math.Max(0, Math.Min(x, frameWidth - 1)),
                Math.Max(0, Math.Min(y, frameHeight - 1)),
                Math.Max(1, Math.Min(width, frameWidth - Math.Max(0, x))),
                Math.Max(1, Math.Min(height, frameHeight - Math.Max(0, y))));
        }

        private static Point GetCenter(Rect rectangle)
        {
            return new Point(
                rectangle.X + rectangle.Width / 2,
                rectangle.Y + rectangle.Height / 2);
        }

        private static double IntersectionOverUnion(Rect left, Rect right)
        {
            Rect intersection = left & right;
            double intersectionArea =
                Math.Max(0, intersection.Width) *
                Math.Max(0, intersection.Height);
            double unionArea =
                Math.Max(
                    1.0,
                    left.Width * left.Height +
                    right.Width * right.Height -
                    intersectionArea);

            return intersectionArea / unionArea;
        }

        private static Rect ExpandRect(
            Rect rectangle,
            int horizontal,
            int vertical,
            int frameWidth,
            int frameHeight)
        {
            int left = Math.Max(0, rectangle.X - horizontal);
            int top = Math.Max(0, rectangle.Y - vertical);
            int right = Math.Min(frameWidth, rectangle.Right + horizontal);
            int bottom = Math.Min(frameHeight, rectangle.Bottom + vertical);

            return new Rect(
                left,
                top,
                Math.Max(1, right - left),
                Math.Max(1, bottom - top));
        }

        private sealed class SmokeCandidateTrack
        {
            internal SmokeCandidateTrack(Rect rectangle)
            {
                Rectangle = rectangle;
                EnvelopeRectangle = rectangle;
                EnvelopeFrames = 1;
                SeenFrames = 1;
                Matched = true;
            }

            internal Rect Rectangle { get; set; }

            internal Rect EnvelopeRectangle { get; set; }

            internal int EnvelopeFrames { get; set; }

            internal int SeenFrames { get; set; }

            internal int MissingFrames { get; set; }

            internal int UpwardSamples { get; set; }

            internal int DownwardSamples { get; set; }

            internal int StationaryFrames { get; set; }

            internal int DynamicSamples { get; set; }

            internal int ExpansionSamples { get; set; }

            internal bool Matched { get; set; }
        }

    }

    /// <summary>
    /// 2026-08-27: 연기 후보 마스크와 시간축 확인 결과를 한 프레임 단위로 전달한다.
    /// </summary>
    internal sealed class SmokeCandidateAnalysis : IDisposable
    {
        internal SmokeCandidateAnalysis(
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

        internal Mat Mask { get; }

        internal IList<Rect> Candidates { get; }

        internal bool IsConfirmed { get; }

        internal int ContinuousFrames { get; }

        internal double LargestAreaRatio { get; }

        internal static SmokeCandidateAnalysis Empty()
        {
            return new SmokeCandidateAnalysis(
                new Mat(),
                new List<Rect>(),
                false,
                0,
                0.0);
        }

        public void Dispose()
        {
            Mask?.Dispose();
        }

    }

}
