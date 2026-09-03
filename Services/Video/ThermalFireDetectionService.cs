using OpenCvSharp;
using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

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
        private const int ConfirmFrameCount = 4;
        private const int ClearFrameCount = 45;
        private const int CandidateHoldFrameCount = 45;

        private int _candidateFrameCount;
        private int _clearFrameCount;
        private double _latchedVisionScore;
        private readonly List<FireVisionScoreTrack> _visionScoreTracks =
            new List<FireVisionScoreTrack>();
        private bool _isFireCandidateDetected;
        private Rect _trackedCandidateRect = Rect.Empty;
        private readonly List<FireCandidateTrack> _candidateTracks =
            new List<FireCandidateTrack>();
        private DateTime _lastTrackContinuityLogTime = DateTime.MinValue;
        private int _lastReportedTrackCount = -1;
        private bool _wasAiFireSuppressionActive;
        // 2026-08-25: REI/MOE가 동일한 화재 후보 알고리즘과 오류 처리 정책을
        // 사용하도록 공통화하였다. 반복 오류 로그는 5초 간격으로 제한한다.
        private DateTime _lastProcessErrorLogTime = DateTime.MinValue;
        // 2026-08-14: Static hot roofs/ground are rejected using inter-frame motion.
        private Mat _previousGray = new Mat();
        // 2026-08-18: 팔레트상 계속 붉게 보이는 건물/지면과 실제로 형상이
        // 흔들리는 화염을 구분하기 위한 직전 후보 마스크이다.
        private Mat _previousCandidateMask = new Mat();
        private readonly object _diagnosticSync = new object();
        private StreamWriter _diagnosticWriter;
        private string _diagnosticDirectory;
        private string _diagnosticChannel;
        private int _diagnosticFrameIndex;

        internal void StartDiagnostic(string directory, string channel)
        {
            lock (_diagnosticSync)
            {
                StopDiagnosticCore();
                Directory.CreateDirectory(directory);
                _diagnosticDirectory = directory;
                _diagnosticChannel = channel;
                _diagnosticWriter = new StreamWriter(
                    Path.Combine(directory, "fire_candidates.csv"),
                    false,
                    new UTF8Encoding(true));
                _diagnosticWriter.WriteLine(
                    "FRAME,INPUT,CANDIDATE,X,Y,WIDTH,HEIGHT,AREA_RATIO,DETECTED");
            }
        }

        internal void StopDiagnostic()
        {
            lock (_diagnosticSync)
            {
                StopDiagnosticCore();
            }
        }

        private void StopDiagnosticCore()
        {
            _diagnosticWriter?.Flush();
            _diagnosticWriter?.Dispose();
            _diagnosticWriter = null;
            _diagnosticDirectory = null;
            _diagnosticChannel = null;
            _diagnosticFrameIndex = 0;
        }

        private void WriteDiagnosticFrame(
            Mat frame,
            Mat candidateMask,
            IList<Rect> candidates,
            bool detected)
        {
            lock (_diagnosticSync)
            {
                if (_diagnosticWriter == null || string.IsNullOrWhiteSpace(_diagnosticDirectory))
                {
                    return;
                }

                _diagnosticFrameIndex++;
                double frameArea = Math.Max(1.0, frame.Width * (double)frame.Height);
                if (candidates == null || candidates.Count == 0)
                {
                    _diagnosticWriter.WriteLine(string.Join(",", new[]
                    {
                        _diagnosticFrameIndex.ToString(CultureInfo.InvariantCulture),
                        _diagnosticChannel, "0", "0", "0", "0", "0", "0",
                        detected ? "TRUE" : "FALSE"
                    }));
                }
                else
                {
                    for (int index = 0; index < candidates.Count; index++)
                    {
                        Rect rectangle = candidates[index];
                        double areaRatio = rectangle.Width * (double)rectangle.Height / frameArea;
                        _diagnosticWriter.WriteLine(string.Join(",", new[]
                        {
                            _diagnosticFrameIndex.ToString(CultureInfo.InvariantCulture),
                            _diagnosticChannel,
                            (index + 1).ToString(CultureInfo.InvariantCulture),
                            rectangle.X.ToString(CultureInfo.InvariantCulture),
                            rectangle.Y.ToString(CultureInfo.InvariantCulture),
                            rectangle.Width.ToString(CultureInfo.InvariantCulture),
                            rectangle.Height.ToString(CultureInfo.InvariantCulture),
                            areaRatio.ToString("0.######", CultureInfo.InvariantCulture),
                            detected ? "TRUE" : "FALSE"
                        }));
                    }
                }

                if (_diagnosticFrameIndex == 1 || _diagnosticFrameIndex % 30 == 0)
                {
                    string frameDirectory = Path.Combine(
                        _diagnosticDirectory,
                        "frame_" + _diagnosticFrameIndex.ToString("D6", CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(frameDirectory);
                    Cv2.ImWrite(Path.Combine(frameDirectory, "RAW.png"), frame);
                    Cv2.ImWrite(Path.Combine(frameDirectory, "CANDIDATE_MASK.png"), candidateMask);
                    using (Mat finalMask = new Mat(frame.Size(), MatType.CV_8UC1, Scalar.Black))
                    {
                        if (candidates != null)
                        {
                            foreach (Rect rectangle in candidates)
                            {
                                Cv2.Rectangle(finalMask, rectangle, Scalar.White, -1);
                            }
                        }
                        Cv2.ImWrite(Path.Combine(frameDirectory, "FINAL.png"), finalMask);
                    }
                }

                if (_diagnosticFrameIndex % 30 == 0)
                {
                    _diagnosticWriter.Flush();
                }
            }
        }

        /// <summary>
        /// 2026-08-27: Palette·NUC 등 영상 조건 변경 뒤 시간축 후보를 초기화한다.
        /// </summary>
        internal void Reset()
        {
            ResetDetectionState();
        }

        /// <summary>
        /// Process 처리 함수.
        /// </summary>
        internal ThermalFireDetectionResult Process(
            Mat frame,
            bool isEnabled,
            double hotThresholdRatio,
            double minimumAreaRatio,
            int fireBoxGroupingMode,
            IList<Rect> aiFireCandidates)
        {
            try
            {
                return ProcessCore(
                    frame,
                    isEnabled,
                    hotThresholdRatio,
                    minimumAreaRatio,
                    fireBoxGroupingMode,
                    aiFireCandidates);
            }
            catch (Exception ex)
            {
                DateTime now = DateTime.Now;
                if ((now - _lastProcessErrorLogTime).TotalSeconds >= 5)
                {
                    _lastProcessErrorLogTime = now;
                    ConsoleLogHelper.Error(
                        "THERMAL FIRE",
                        "Candidate processing failed / " + ex.Message);
                }

                return ResetDetectionState();
            }

        }

        /// <summary>
        /// 2026-08-25 REI/MOE 공통 화재 후보 판정 본체.
        /// </summary>
        private ThermalFireDetectionResult ProcessCore(
            Mat frame,
            bool isEnabled,
            double hotThresholdRatio,
            double minimumAreaRatio,
            int fireBoxGroupingMode,
            IList<Rect> aiFireCandidates)
        {
            if (!isEnabled || frame == null || frame.Empty())
            {
                return ResetDetectionState();
            }

            int threshold =
                (int)Math.Round(Math.Max(0, Math.Min(1, hotThresholdRatio)) * 255);

            double frameArea = frame.Width * frame.Height;
            // 2026-08-21: 10~30 px 화염은 Bounding Box 한 변의 크기를 뜻한다.
            // 면적은 대략 100~900 px이므로 화면비 기반 최소값과 별도로
            // 24 px contour부터 검증하는 Small Fire lane을 유지한다.
            double configuredMinimumArea =
                Math.Max(64, frameArea * minimumAreaRatio);
            double minimumArea = 24;
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
                    bool isSmallFireCandidate =
                        rect.Width <= 36 &&
                        rect.Height <= 36 &&
                        rectangleArea <= 1296;
                    // 2026-08-24: 큰 세로형·불규칙 화염은 RTSP 압축으로 프레임 차가
                    // 작아져도 보존한다. 작은 고정 점광원에는 기존 시간축 검사를 유지한다.
                    bool isStrongLargeFireCandidate =
                        rectangleAreaRatio >= 0.0015 &&
                        rect.Height >= Math.Max(32, frame.Height * 0.04) &&
                        aspectRatio >= 0.08 && aspectRatio <= 1.60 &&
                        fillRatio >= 0.025 &&
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

                    // 2026-08-18: 색상만 고온인 고정 구조물은 후보가 아니다.
                    // 실제 화염은 국부 픽셀 움직임과 외곽 형상 변화가 함께
                    // 지속되어야 하며, 카메라 전체가 움직인 프레임도 제외한다.
                    if (fillRatio < 0.005 ||
                        (rectangleAreaRatio > 0.75 && fillRatio > 0.30) ||
                        (rectangleAreaRatio > 0.08 && aspectRatio > 2.2 &&
                         fillRatio > 0.18 && solidity > 0.72) ||
                        (!isSmallFireCandidate &&
                         solidity > 0.90 && fillRatio > 0.50 && irregularity < 1.25) ||
                        // 2026-08-24: 작은 정적 전등도 시간축 검사를 반드시 통과시킨다.
                        // 실제 작은 화염은 미세 움직임 또는 외곽 변화 중 하나로 유지한다.
                        (!isStrongLargeFireCandidate &&
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

                    candidateRects.Add(rect);
                }

                List<Rect> mergedRects = MergeNearbyRects(
                    candidateRects,
                    frame.Width,
                    frame.Height,
                    fireBoxGroupingMode);
                int candidatesBeforeAiFilter = mergedRects.Count;
                mergedRects = new List<Rect>(RemoveAiFireOverlappingCandidates(
                    mergedRects,
                    aiFireCandidates));
                RemoveAiFireOverlappingTracks(aiFireCandidates);
                bool aiFireSuppressionActive =
                    mergedRects.Count < candidatesBeforeAiFilter;
                if (aiFireSuppressionActive != _wasAiFireSuppressionActive)
                {
                    _wasAiFireSuppressionActive = aiFireSuppressionActive;
                    ConsoleLogHelper.State(
                        "THERMAL FIRE HYBRID",
                        "AI FIRE overlap suppression " +
                        (aiFireSuppressionActive ? "started" : "ended") +
                        " / CHANNEL=IR");
                }
                int heldFireCandidateCount;
                IList<Rect> persistentRects = UpdatePersistentCandidateTracks(
                    mergedRects,
                    frame.Width,
                    frame.Height,
                    out heldFireCandidateCount);

                DateTime continuityNow = DateTime.Now;
                bool fireTrackCountChanged =
                    persistentRects.Count != _lastReportedTrackCount;
                if ((fireTrackCountChanged || heldFireCandidateCount > 0) &&
                    (continuityNow - _lastTrackContinuityLogTime).TotalSeconds >=
                        (fireTrackCountChanged ? 0.5 : 2.0))
                {
                    _lastTrackContinuityLogTime = continuityNow;
                    _lastReportedTrackCount = persistentRects.Count;
                    ConsoleLogHelper.State(
                        "THERMAL FIRE TRACK",
                        "Independent fire candidates / VISIBLE=" +
                        persistentRects.Count +
                        " / HELD=" + heldFireCandidateCount);
                }

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
                try
                {
                    WriteDiagnosticFrame(frame, cleanedMask, persistentRects, _isFireCandidateDetected);
                }
                catch (Exception diagnosticException)
                {
                    StopDiagnostic();
                    ConsoleLogHelper.Error(
                        "FIRE DIAGNOSTIC",
                        "Live diagnostic write failed",
                        diagnosticException);
                }
                IList<double> fireVisionScores = _isFireCandidateDetected
                    ? AssignVisionScores(persistentRects, frame.Width, frame.Height)
                    : new List<double>();
                if (!_isFireCandidateDetected && _visionScoreTracks.Count > 0)
                {
                    _visionScoreTracks.Clear();
                }
                _latchedVisionScore = fireVisionScores.Count > 0
                    ? fireVisionScores[0]
                    : 0.0;

                currentGray.CopyTo(_previousGray);
                cleanedMask.CopyTo(_previousCandidateMask);

                if (_isFireCandidateDetected && persistentRects.Count > 0)
                {
                    for (int index = 0; index < persistentRects.Count; index++)
                    {
                        DrawDetectionBox(
                            frame,
                            persistentRects[index],
                            index + 1,
                            index < fireVisionScores.Count
                                ? fireVisionScores[index]
                                : _latchedVisionScore);
                    }
                }

                Rect resultRect = selectedRect;
                double resultArea = selectedArea;
                if (_isFireCandidateDetected && persistentRects.Count > 0)
                {
                    resultRect = persistentRects[0];
                    resultArea = resultRect.Width * resultRect.Height;
                    for (int index = 1; index < persistentRects.Count; index++)
                    {
                        Rect candidate = persistentRects[index];
                        double area = candidate.Width * candidate.Height;
                        if (area > resultArea)
                        {
                            resultRect = candidate;
                            resultArea = area;
                        }
                    }
                }

                return new ThermalFireDetectionResult(
                    _isFireCandidateDetected,
                    previousState != _isFireCandidateDetected,
                    resultArea,
                    resultRect,
                    persistentRects.Count,
                    _latchedVisionScore,
                    _isFireCandidateDetected ? persistentRects : new List<Rect>(),
                    _isFireCandidateDetected ? fireVisionScores : new List<double>());
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
            // 2026-08-21: 실제 IR에서도 큰 화염 내부 조각을 대표 BBox 하나로 병합한다.
            int horizontalGap = Math.Max(6, frameWidth / 120);
            int verticalGap = Math.Max(8, frameHeight / 100);
            merged.Sort((left, right) =>
                (right.Width * right.Height).CompareTo(left.Width * left.Height));

            bool changed;

            do
            {
                changed = false;

                for (int first = 0; first < merged.Count && !changed; first++)
                {
                    int fragmentHorizontalGap =
                        Math.Max(horizontalGap, (int)Math.Round(merged[first].Width * 0.45));
                    int fragmentVerticalGap =
                        Math.Max(verticalGap, (int)Math.Round(merged[first].Height * 0.75));
                    Rect expandedFirst = ExpandRect(
                        merged[first],
                        fragmentHorizontalGap,
                        fragmentVerticalGap,
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
        /// 2026-08-26: FIRE 후보는 형광 마젠타 BBox와 프레임 내 순번으로 표시하여
        /// 형광 라임 AI BBox와 즉시 구분한다.
        /// </summary>
        private static void DrawDetectionBox(Mat frame, Rect rect, int detectionOrder, double visionScore)
        {
            double displayScale =
                Math.Max(
                    1.0,
                    Math.Max(
                        frame.Width / 1280.0,
                        frame.Height / 720.0));
            int lineThickness =
                Math.Max(3, (int)Math.Round(2.0 * displayScale));
            double labelFontScale = Math.Max(0.36, 0.34 * displayScale);
            Rect displayRect =
                CreateVisibleDetectionRect(
                    rect,
                    frame.Width,
                    frame.Height,
                    displayScale);

            Scalar fireOverlayColor = new Scalar(255, 0, 255);
            Cv2.Rectangle(
                frame,
                displayRect,
                fireOverlayColor,
                lineThickness);
            string label =
                "VP #" + detectionOrder + " | Fire " +
                visionScore.ToString("F1") + "%";
            int baseline;
            int fontThickness = Math.Max(1, (int)Math.Round(displayScale));
            Size labelSize = Cv2.GetTextSize(
                label,
                HersheyFonts.HersheySimplex,
                labelFontScale,
                fontThickness,
                out baseline);
            int labelX = Math.Max(0, Math.Min(displayRect.X, frame.Width - labelSize.Width - 6));
            int labelY = Math.Max(
                labelSize.Height + 6,
                Math.Min(displayRect.Y - 7, frame.Height - baseline - 2));
            int labelTop = Math.Max(0, labelY - labelSize.Height - 5);
            Cv2.Rectangle(
                frame,
                new Rect(
                    labelX,
                    labelTop,
                    Math.Min(labelSize.Width + 6, frame.Width - labelX),
                    Math.Min(labelSize.Height + baseline + 7, frame.Height - labelTop)),
                new Scalar(24, 24, 24),
                -1);
            Cv2.PutText(
                frame,
                label,
                new Point(labelX + 3, labelY),
                HersheyFonts.HersheySimplex,
                labelFontScale,
                fireOverlayColor,
                fontThickness);
        }

        /// <summary>
        /// 4K 영상의 10~30px 후보가 축소 화면에서도 사라지지 않도록
        /// 표시용 사각형에만 최소 크기를 적용한다. 실제 BBox 수치는 유지한다.
        /// </summary>
        private static Rect CreateVisibleDetectionRect(
            Rect source,
            int frameWidth,
            int frameHeight,
            double displayScale)
        {
            int minimumSide =
                Math.Max(28, (int)Math.Round(24 * displayScale));
            int targetWidth = Math.Max(source.Width, minimumSide);
            int targetHeight = Math.Max(source.Height, minimumSide);
            int centerX = source.X + source.Width / 2;
            int centerY = source.Y + source.Height / 2;
            int x = Math.Max(0, centerX - targetWidth / 2);
            int y = Math.Max(0, centerY - targetHeight / 2);

            targetWidth = Math.Min(targetWidth, frameWidth - x);
            targetHeight = Math.Min(targetHeight, frameHeight - y);

            return new Rect(x, y, targetWidth, targetHeight);
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
        /// 2026-09-02 V17: FIRE도 SMOKE와 동일하게 BBox Track별로 1.5초 동안
        /// 면적·지속·상향·확산·이동·형상 변화를 합산하고 이후 점수를 고정한다.
        /// </summary>
        private IList<double> AssignVisionScores(
            IList<Rect> candidates,
            int frameWidth,
            int frameHeight)
        {
            DateTime nowUtc = DateTime.UtcNow;
            foreach (FireVisionScoreTrack track in _visionScoreTracks)
            {
                track.Matched = false;
            }

            List<double> scores = new List<double>();
            foreach (Rect candidate in candidates ?? new List<Rect>())
            {
                FireVisionScoreTrack matched = null;
                double bestMatch = 0.0;
                foreach (FireVisionScoreTrack track in _visionScoreTracks)
                {
                    if (track.Matched)
                    {
                        continue;
                    }

                    Rect intersection = track.Rectangle & candidate;
                    double intersectionArea = Math.Max(0, intersection.Width) *
                        Math.Max(0, intersection.Height);
                    double smallerArea = Math.Max(
                        1.0,
                        Math.Min(
                            track.Rectangle.Width * (double)track.Rectangle.Height,
                            candidate.Width * (double)candidate.Height));
                    double match = intersectionArea / smallerArea;
                    if (match > bestMatch)
                    {
                        bestMatch = match;
                        matched = track;
                    }
                }

                if (matched == null || bestMatch < 0.25)
                {
                    matched = new FireVisionScoreTrack
                    {
                        InitialRectangle = candidate,
                        FirstSeenUtc = nowUtc
                    };
                    _visionScoreTracks.Add(matched);
                }

                if (!matched.IsScoreFinalized)
                {
                    double elapsedSeconds = Math.Max(
                        0.0,
                        (nowUtc - matched.FirstSeenUtc).TotalSeconds);
                    matched.Score = CalculateVisionScore(
                        matched,
                        candidate,
                        frameWidth,
                        frameHeight,
                        elapsedSeconds);
                    if (elapsedSeconds >= 1.5)
                    {
                        matched.IsScoreFinalized = true;
                        ConsoleLogHelper.State(
                            "FIRE V.SCORE",
                            "Track score finalized / CAMERA=IR / SCORE=" +
                            matched.Score.ToString("F1") +
                            " / BBOX=" + candidate.Width + "x" + candidate.Height);
                    }
                }

                matched.Rectangle = candidate;
                matched.Matched = true;
                scores.Add(matched.Score);
            }

            _visionScoreTracks.RemoveAll(track => !track.Matched);
            return scores;
        }

        private static double CalculateVisionScore(
            FireVisionScoreTrack track,
            Rect candidate,
            int frameWidth,
            int frameHeight,
            double elapsedSeconds)
        {
            double frameArea = Math.Max(1.0, frameWidth * (double)frameHeight);
            double areaRatio = candidate.Width * candidate.Height / frameArea;
            double verticality = candidate.Height /
                (double)Math.Max(1, candidate.Width + candidate.Height);
            double aspectBalance = Math.Min(candidate.Width, candidate.Height) /
                (double)Math.Max(1, Math.Max(candidate.Width, candidate.Height));
            double initialArea = Math.Max(
                1.0,
                track.InitialRectangle.Width * (double)track.InitialRectangle.Height);
            double currentArea = Math.Max(1.0, candidate.Width * (double)candidate.Height);
            double expansionRatio = currentArea / initialArea - 1.0;
            double initialCenterX = track.InitialRectangle.X + track.InitialRectangle.Width / 2.0;
            double initialCenterY = track.InitialRectangle.Y + track.InitialRectangle.Height / 2.0;
            double currentCenterX = candidate.X + candidate.Width / 2.0;
            double currentCenterY = candidate.Y + candidate.Height / 2.0;
            double deltaX = currentCenterX - initialCenterX;
            double deltaY = currentCenterY - initialCenterY;
            double frameDiagonal = Math.Max(
                1.0,
                Math.Sqrt(frameWidth * (double)frameWidth + frameHeight * (double)frameHeight));
            double motionRatio = Math.Sqrt(deltaX * deltaX + deltaY * deltaY) / frameDiagonal;
            double upwardRatio = Math.Max(0.0, initialCenterY - currentCenterY) /
                Math.Max(1.0, frameHeight);
            double initialAspect = track.InitialRectangle.Width /
                (double)Math.Max(1, track.InitialRectangle.Height);
            double currentAspect = candidate.Width /
                (double)Math.Max(1, candidate.Height);
            double shapeChange = Math.Abs(currentAspect - initialAspect) /
                Math.Max(0.25, initialAspect);

            double score = 20.0 +
                Math.Min(12.0, Math.Sqrt(Math.Max(0.0, areaRatio)) * 46.0) +
                Math.Min(4.0, verticality * 7.0) +
                Math.Min(3.0, aspectBalance * 3.0) +
                Math.Min(20.0, elapsedSeconds / 1.5 * 20.0) +
                Math.Min(8.0, upwardRatio * 160.0) +
                Math.Min(10.0, Math.Max(0.0, expansionRatio) * 12.0) +
                Math.Min(5.0, motionRatio * 100.0) +
                Math.Min(5.0, shapeChange * 8.0);

            if (elapsedSeconds >= 0.5 &&
                motionRatio < 0.006 &&
                Math.Abs(expansionRatio) < 0.10 &&
                shapeChange < 0.10)
            {
                score -= 15.0;
            }

            return Math.Max(5.0, Math.Min(92.0, score));
        }

        /// <summary>
        /// 2026-09-02: 여러 화점 후보를 독립 Track으로 관리한다. 한 후보가 잠깐
        /// 약해져도 다른 화점의 검출 여부와 무관하게 마지막 BBox를 유지한다.
        /// </summary>
        private IList<Rect> UpdatePersistentCandidateTracks(
            IList<Rect> candidates,
            int frameWidth,
            int frameHeight,
            out int heldCandidateCount)
        {
            foreach (FireCandidateTrack track in _candidateTracks)
            {
                track.Matched = false;
            }

            foreach (Rect candidate in candidates ?? new List<Rect>())
            {
                FireCandidateTrack bestTrack = null;
                double bestScore = 0.0;
                double candidateCenterX = candidate.X + candidate.Width / 2.0;
                double candidateCenterY = candidate.Y + candidate.Height / 2.0;

                foreach (FireCandidateTrack track in _candidateTracks)
                {
                    if (track.Matched)
                    {
                        continue;
                    }

                    double iou = IntersectionOverUnion(track.Rectangle, candidate);
                    double trackCenterX = track.Rectangle.X + track.Rectangle.Width / 2.0;
                    double trackCenterY = track.Rectangle.Y + track.Rectangle.Height / 2.0;
                    double centerDistance = Math.Sqrt(
                        Math.Pow(candidateCenterX - trackCenterX, 2) +
                        Math.Pow(candidateCenterY - trackCenterY, 2));
                    double allowedDistance = Math.Max(
                        18.0,
                        Math.Max(track.Rectangle.Width, track.Rectangle.Height) * 0.65);
                    double score = iou;
                    if (score < 0.08 && centerDistance <= allowedDistance)
                    {
                        score = 0.08 +
                            (1.0 - centerDistance / allowedDistance) * 0.20;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTrack = track;
                    }
                }

                if (bestTrack == null || bestScore < 0.08)
                {
                    _candidateTracks.Add(new FireCandidateTrack(candidate));
                    continue;
                }

                const double currentWeight = 0.45;
                Rect previous = bestTrack.Rectangle;
                bestTrack.Rectangle = new Rect(
                    Math.Max(0, (int)Math.Round(
                        previous.X * (1.0 - currentWeight) + candidate.X * currentWeight)),
                    Math.Max(0, (int)Math.Round(
                        previous.Y * (1.0 - currentWeight) + candidate.Y * currentWeight)),
                    Math.Max(1, (int)Math.Round(
                        previous.Width * (1.0 - currentWeight) + candidate.Width * currentWeight)),
                    Math.Max(1, (int)Math.Round(
                        previous.Height * (1.0 - currentWeight) + candidate.Height * currentWeight)));
                bestTrack.Rectangle = ExpandRect(
                    bestTrack.Rectangle,
                    0,
                    0,
                    frameWidth,
                    frameHeight);
                bestTrack.SeenFrames++;
                bestTrack.MissingFrames = 0;
                bestTrack.Matched = true;
            }

            for (int index = _candidateTracks.Count - 1; index >= 0; index--)
            {
                FireCandidateTrack track = _candidateTracks[index];
                if (!track.Matched)
                {
                    track.MissingFrames++;
                }

                int removalLimit = track.SeenFrames >= ConfirmFrameCount
                    ? CandidateHoldFrameCount
                    : ConfirmFrameCount * 2;
                if (track.MissingFrames > removalLimit)
                {
                    _candidateTracks.RemoveAt(index);
                }
            }

            heldCandidateCount = 0;
            List<Rect> visible = new List<Rect>();
            foreach (FireCandidateTrack track in _candidateTracks)
            {
                if (track.SeenFrames < ConfirmFrameCount ||
                    track.MissingFrames > CandidateHoldFrameCount)
                {
                    continue;
                }

                visible.Add(track.Rectangle);
                if (!track.Matched)
                {
                    heldCandidateCount++;
                }
            }

            return visible;
        }

        /// <summary>
        /// AI가 이미 FIRE/FLAME으로 표시한 영역은 자체 영상처리 BBox와 이벤트에서
        /// 제외하여 하이브리드 결과가 동일 위치에 중복 표시되지 않도록 한다.
        /// </summary>
        private static IList<Rect> RemoveAiFireOverlappingCandidates(
            IList<Rect> candidates,
            IList<Rect> aiFireCandidates)
        {
            if (candidates == null || candidates.Count == 0 ||
                aiFireCandidates == null || aiFireCandidates.Count == 0)
            {
                return candidates ?? new List<Rect>();
            }

            List<Rect> filtered = new List<Rect>();
            foreach (Rect candidate in candidates)
            {
                bool overlapsAi = false;
                foreach (Rect aiFire in aiFireCandidates)
                {
                    Rect intersection = candidate & aiFire;
                    double intersectionArea =
                        Math.Max(0, intersection.Width) * Math.Max(0, intersection.Height);
                    double smallerArea = Math.Max(
                        1.0,
                        Math.Min(
                            candidate.Width * (double)candidate.Height,
                            aiFire.Width * (double)aiFire.Height));
                    double centerX = candidate.X + candidate.Width / 2.0;
                    double centerY = candidate.Y + candidate.Height / 2.0;
                    bool centerInside =
                        centerX >= aiFire.X && centerX <= aiFire.Right &&
                        centerY >= aiFire.Y && centerY <= aiFire.Bottom;

                    if (IntersectionOverUnion(candidate, aiFire) >= 0.08 ||
                        intersectionArea / smallerArea >= 0.30 ||
                        centerInside)
                    {
                        overlapsAi = true;
                        break;
                    }
                }

                if (!overlapsAi)
                {
                    filtered.Add(candidate);
                }
            }

            return filtered;
        }

        private void RemoveAiFireOverlappingTracks(IList<Rect> aiFireCandidates)
        {
            if (aiFireCandidates == null || aiFireCandidates.Count == 0 ||
                _candidateTracks.Count == 0)
            {
                return;
            }

            for (int index = _candidateTracks.Count - 1; index >= 0; index--)
            {
                Rect tracked = _candidateTracks[index].Rectangle;
                if (RemoveAiFireOverlappingCandidates(
                        new List<Rect> { tracked },
                        aiFireCandidates).Count == 0)
                {
                    _candidateTracks.RemoveAt(index);
                }
            }
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
                    Mat intensityMask = new Mat();
                    Cv2.BitwiseOr(
                        brightIntensityMask,
                        darkIntensityMask,
                        intensityMask);
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
                    // 화면 RGB만 제공되는 장비에서도 White/Black Hot 및
                    // Iron/Rainbow/Lava/Arctic/Fusion 계열의 국부 Hotspot을
                    // 유지하도록 색상 후보와 양극성 local contrast를 결합한다.
                    Cv2.BitwiseOr(combinedMask, contrastMask, combinedMask);
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
                        ? "Candidate confirmed / PERSISTENCE=4 frames"
                        : "Candidate cleared / PERSISTENCE=" + ClearFrameCount + " frames");
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
            _latchedVisionScore = 0.0;
            _visionScoreTracks.Clear();
            _trackedCandidateRect = Rect.Empty;
            _candidateTracks.Clear();
            _lastTrackContinuityLogTime = DateTime.MinValue;
            _lastReportedTrackCount = -1;
            _wasAiFireSuppressionActive = false;
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

            return new ThermalFireDetectionResult(
                false,
                changed,
                0,
                Rect.Empty,
                0,
                0.0,
                new List<Rect>(),
                new List<double>());
        }

        private sealed class FireVisionScoreTrack
        {
            internal Rect InitialRectangle { get; set; }
            internal Rect Rectangle { get; set; }
            internal double Score { get; set; }
            internal DateTime FirstSeenUtc { get; set; }
            internal bool IsScoreFinalized { get; set; }
            internal bool Matched { get; set; }
        }

        private sealed class FireCandidateTrack
        {
            internal FireCandidateTrack(Rect rectangle)
            {
                Rectangle = rectangle;
                SeenFrames = 1;
                Matched = true;
            }

            internal Rect Rectangle { get; set; }

            internal int SeenFrames { get; set; }

            internal int MissingFrames { get; set; }

            internal bool Matched { get; set; }
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
            double candidateArea,
            Rect candidateRect,
            int candidateCount,
            double visionScore,
            IList<Rect> candidateRects,
            IList<double> candidateScores)
        {
            IsDetected = isDetected;
            StateChanged = stateChanged;
            CandidateArea = candidateArea;
            CandidateRect = candidateRect;
            CandidateCount = candidateCount;
            VisionScore = visionScore;
            CandidateRects = candidateRects == null
                ? new List<Rect>()
                : new List<Rect>(candidateRects);
            CandidateScores = candidateScores == null
                ? new List<double>()
                : new List<double>(candidateScores);
        }

        internal bool IsDetected { get; }
        internal bool StateChanged { get; }
        internal double CandidateArea { get; }
        internal Rect CandidateRect { get; }
        internal int CandidateCount { get; }
        internal double VisionScore { get; }
        internal IList<Rect> CandidateRects { get; }
        internal IList<double> CandidateScores { get; }
    }

}
