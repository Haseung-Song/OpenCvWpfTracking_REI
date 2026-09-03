using FireCandidateValidator;
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
    /// 2026-08-27: EO 연기 후보와 IR 플룸 보조 후보를 시간축으로 확인하고
    /// 영상 Overlay 및 이벤트 상태를 생성한다.
    /// </summary>
    internal sealed class SmokeDetectionService
    {
        private readonly SmokeCandidateAnalyzer _analyzer =
            new SmokeCandidateAnalyzer();
        private bool _isDetected;
        private int _clearFrameCount;
        private int _visionStableFrames;
        private bool _wasAiSmokeSuppressionActive;
        private bool _wasAiVehicleSuppressionActive;
        private bool? _lastStandaloneFallbackMode;
        private int _lastReportedConfirmationFrames = -1;
        private double _latchedVisionScore;
        private readonly List<VisionScoreTrack> _visionScoreTracks = new List<VisionScoreTrack>();
        private readonly List<Rect> _lastVisibleCandidates =
            new List<Rect>();
        private DateTime _lastErrorLogTime = DateTime.MinValue;
        private DateTime _lastRigidMotionLogTime = DateTime.MinValue;
        private DateTime _lastContinuityLogTime = DateTime.MinValue;
        private int _lastReportedVisibleCandidateCount = -1;
        private bool _lastDetectionWasVerified;
        private readonly object _diagnosticSync = new object();
        private StreamWriter _diagnosticWriter;
        private string _diagnosticDirectory;
        private string _diagnosticChannel;
        private int _diagnosticFrameIndex;

        /// <summary>
        /// 2026-09-03: 실영상 진단은 명시적으로 켠 동안에만 CSV와 단계별 마스크를 기록한다.
        /// EO/IR 서비스가 각자 전용 폴더와 Writer를 사용해 프레임 스레드 간 혼합을 방지한다.
        /// </summary>
        internal void StartDiagnostic(string directory, string channel)
        {
            lock (_diagnosticSync)
            {
                StopDiagnosticCore();
                Directory.CreateDirectory(directory);
                _diagnosticDirectory = directory;
                _diagnosticChannel = channel;
                _diagnosticFrameIndex = 0;
                _diagnosticWriter = new StreamWriter(
                    Path.Combine(directory, "smoke_candidates.csv"),
                    false,
                    new UTF8Encoding(true));
                _diagnosticWriter.WriteLine(
                    "FRAME,INPUT,STAGE,CANDIDATE,X,Y,WIDTH,HEIGHT,AREA,FILL,ASPECT,EDGE_DENSITY," +
                    "SEEN,MISSING,DYNAMIC,UPWARD,EXPANSION,DEFORMATION,STATIONARY,RESULT,REJECT_REASON");
                _diagnosticWriter.Flush();
            }

            ConsoleLogHelper.State(
                "SMOKE DIAGNOSTIC",
                "Live diagnostic started / CHANNEL=" + channel + " / PATH=" + directory);
        }

        internal void StopDiagnostic()
        {
            string channel;
            lock (_diagnosticSync)
            {
                channel = _diagnosticChannel;
                StopDiagnosticCore();
            }

            if (!string.IsNullOrWhiteSpace(channel))
            {
                ConsoleLogHelper.State(
                    "SMOKE DIAGNOSTIC",
                    "Live diagnostic stopped / CHANNEL=" + channel);
            }
        }

        private void StopDiagnosticCore()
        {
            if (_diagnosticWriter != null)
            {
                _diagnosticWriter.Flush();
                _diagnosticWriter.Dispose();
                _diagnosticWriter = null;
            }

            _diagnosticDirectory = null;
            _diagnosticChannel = null;
            _diagnosticFrameIndex = 0;
        }

        private SmokeDiagnosticCapture CreateDiagnosticCapture()
        {
            lock (_diagnosticSync)
            {
                if (_diagnosticWriter == null)
                {
                    return null;
                }

                _diagnosticFrameIndex++;
                return new SmokeDiagnosticCapture();
            }
        }

        private void WriteDiagnosticFrame(
            SmokeDiagnosticCapture diagnostic,
            Mat frame,
            bool isInfrared)
        {
            if (diagnostic == null)
            {
                return;
            }

            diagnostic.CaptureStage("RAW", frame);

            lock (_diagnosticSync)
            {
                if (_diagnosticWriter == null || string.IsNullOrWhiteSpace(_diagnosticDirectory))
                {
                    return;
                }

                int candidateIndex = 0;
                foreach (SmokeDiagnosticRecord record in diagnostic.Records)
                {
                    candidateIndex++;
                    Rect rectangle = record.Rectangle;
                    _diagnosticWriter.WriteLine(string.Join(",", new[]
                    {
                        _diagnosticFrameIndex.ToString(CultureInfo.InvariantCulture),
                        isInfrared ? "IR" : "EO",
                        record.Stage,
                        candidateIndex.ToString(CultureInfo.InvariantCulture),
                        rectangle.X.ToString(CultureInfo.InvariantCulture),
                        rectangle.Y.ToString(CultureInfo.InvariantCulture),
                        rectangle.Width.ToString(CultureInfo.InvariantCulture),
                        rectangle.Height.ToString(CultureInfo.InvariantCulture),
                        record.Area.ToString("0.###", CultureInfo.InvariantCulture),
                        record.FillRatio.ToString("0.####", CultureInfo.InvariantCulture),
                        record.AspectRatio.ToString("0.####", CultureInfo.InvariantCulture),
                        record.EdgeDensity.ToString("0.####", CultureInfo.InvariantCulture),
                        record.SeenFrames.ToString(CultureInfo.InvariantCulture),
                        record.MissingFrames.ToString(CultureInfo.InvariantCulture),
                        record.DynamicSamples.ToString(CultureInfo.InvariantCulture),
                        record.UpwardSamples.ToString(CultureInfo.InvariantCulture),
                        record.ExpansionSamples.ToString(CultureInfo.InvariantCulture),
                        record.DeformationSamples.ToString(CultureInfo.InvariantCulture),
                        record.StationaryFrames.ToString(CultureInfo.InvariantCulture),
                        record.Result,
                        record.Reason
                    }));
                }

                // 실시간 영상 처리 지연을 제한하기 위해 단계 이미지는 30프레임 간격으로만 저장한다.
                if (_diagnosticFrameIndex == 1 || _diagnosticFrameIndex % 30 == 0)
                {
                    string frameDirectory = Path.Combine(
                        _diagnosticDirectory,
                        "frame_" + _diagnosticFrameIndex.ToString("D6", CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(frameDirectory);
                    foreach (KeyValuePair<string, Mat> stage in diagnostic.StageMasks)
                    {
                        Cv2.ImWrite(Path.Combine(frameDirectory, stage.Key + ".png"), stage.Value);
                    }
                }

                if (_diagnosticFrameIndex % 30 == 0)
                {
                    _diagnosticWriter.Flush();
                }
            }
        }
        /// <summary>
        /// 2026-08-27: PTZ 이동·Palette·NUC 등 화면 전체 변화 뒤 기준 프레임을 폐기한다.
        /// </summary>
        internal void Reset()
        {
            ResetDetectionState(false);
        }

        internal SmokeDetectionResult Process(
            Mat frame,
            bool isEnabled,
            bool isInfrared,
            double minimumAreaRatio,
            double changeThresholdRatio,
            Rect fireCandidateRect,
            IList<Rect> aiSmokeCandidates,
            IList<Rect> aiVehicleCandidates,
            int smokeBoxGroupingMode,
            bool compensateCameraMotion = false,
            bool aiDetectorAvailable = true)
        {
            if (!isEnabled || frame == null || frame.Empty())
            {
                return ResetDetectionState(isInfrared);
            }

            try
            {
                // 2026-08-27: 건물 경계와 순간 노출 변화를 연기로 확정하지 않도록
                // 실제 Viewer에서는 후보가 충분히 지속된 경우에만 ACTIVE로 승격한다.
                // 2026-08-28: EO는 24프레임 장기 기준 갱신을 넘어 36프레임
                // 지속되는 변화만 확정한다. 기준 갱신 뒤 사라지는 건물·창틀의
                // 일시적인 밝기 변화는 이벤트로 승격하지 않는다.
                // IR 보조 후보는 기존 14프레임 기준을 유지한다.
                bool standaloneFallbackMode = !aiDetectorAvailable;
                int confirmationFrames = isInfrared
                    ? 14
                    : standaloneFallbackMode ? 24 : 36;
                if (_lastStandaloneFallbackMode != standaloneFallbackMode ||
                    _lastReportedConfirmationFrames != confirmationFrames)
                {
                    _lastStandaloneFallbackMode = standaloneFallbackMode;
                    _lastReportedConfirmationFrames = confirmationFrames;
                    ConsoleLogHelper.State(
                        "SMOKE FALLBACK",
                        "AI-independent confirmation mode / CHANNEL=" +
                        (isInfrared ? "IR" : "EO") +
                        " / ACTIVE=" + standaloneFallbackMode +
                        " / CONFIRM_FRAMES=" + confirmationFrames);
                }

                using (SmokeDiagnosticCapture diagnostic = CreateDiagnosticCapture())
                using (SmokeCandidateAnalysis analysis =
                       _analyzer.Analyze(
                           frame,
                           isInfrared,
                           minimumAreaRatio,
                           changeThresholdRatio,
                           confirmationFrames,
                           compensateCameraMotion,
                           diagnostic))
                {
                    WriteDiagnosticFrame(diagnostic, frame, isInfrared);
                    // 2026-09-02: 강체 이동 후보 억제는 정상적인 필터 동작이므로
                    // 이벤트 오류로 기록하지 않고 2초 제한 상태 로그로 남긴다.
                    DateTime now = DateTime.Now;
                    if ((analysis.RigidMotionSuppressedCount > 0 ||
                         analysis.MovingSourceSuppressedCount > 0 ||
                         analysis.TrafficAggregateSuppressedCount > 0) &&
                        (now - _lastRigidMotionLogTime).TotalSeconds >= 2.0)
                    {
                        _lastRigidMotionLogTime = now;
                        ConsoleLogHelper.State(
                            "SMOKE ROAD MOTION",
                            "Moving-object/source candidate suppressed / CHANNEL=" +
                            (isInfrared ? "IR" : "EO") +
                            " / RIGID=" + analysis.RigidMotionSuppressedCount +
                            " / SOURCE=" + analysis.MovingSourceSuppressedCount +
                            " / TRAFFIC=" + analysis.TrafficAggregateSuppressedCount);
                    }

                    bool previousState = _isDetected;
                    IList<Rect> visibleCandidates =
                        RemoveFireOverlappingCandidates(
                            analysis.Candidates,
                            fireCandidateRect);
                    // 2026-08-31: AI SMOKE와 같은 영역은 영상처리 BBox/이벤트를 중복 생성하지 않는다.
                    int candidatesBeforeAiFilter = visibleCandidates.Count;
                    visibleCandidates = RemoveAiOverlappingCandidates(
                        visibleCandidates, aiSmokeCandidates);
                    bool aiSuppressionActive = visibleCandidates.Count < candidatesBeforeAiFilter;
                    if (aiSuppressionActive != _wasAiSmokeSuppressionActive)
                    {
                        _wasAiSmokeSuppressionActive = aiSuppressionActive;
                        ConsoleLogHelper.State("SMOKE HYBRID",
                            "AI overlap suppression " + (aiSuppressionActive ? "started" : "ended") +
                            " / CHANNEL=" + (isInfrared ? "IR" : "EO"));
                    }
                    if (aiSmokeCandidates != null && aiSmokeCandidates.Count > 0)
                    {
                        IList<Rect> retained = RemoveAiOverlappingCandidates(
                            _lastVisibleCandidates, aiSmokeCandidates);
                        _lastVisibleCandidates.Clear();
                        _lastVisibleCandidates.AddRange(retained);
                        if (visibleCandidates.Count == 0 && _lastVisibleCandidates.Count == 0)
                        {
                            _isDetected = false;
                            _clearFrameCount = 0;
                            _visionStableFrames = 0;
                        }
                    }

                    // 2026-09-02: AI가 CAR/TRUCK/BUS 등으로 확인한 주변은
                    // 차량 본체·그림자·배경 가림 변화가 SMOKE 마스크가 되기 쉽다.
                    // 차량 BBox를 확장한 동적 도로 활동 구간과 겹치는 후보를 제거한다.
                    int candidatesBeforeVehicleFilter = visibleCandidates.Count;
                    visibleCandidates = RemoveAiVehicleOverlappingCandidates(
                        visibleCandidates,
                        aiVehicleCandidates);
                    bool vehicleSuppressionActive =
                        visibleCandidates.Count < candidatesBeforeVehicleFilter;
                    if (vehicleSuppressionActive != _wasAiVehicleSuppressionActive)
                    {
                        _wasAiVehicleSuppressionActive = vehicleSuppressionActive;
                        ConsoleLogHelper.State(
                            "SMOKE VEHICLE FUSION",
                            "AI vehicle corridor suppression " +
                            (vehicleSuppressionActive ? "started" : "ended") +
                            " / CHANNEL=" + (isInfrared ? "IR" : "EO"));
                    }

                    if (aiVehicleCandidates != null && aiVehicleCandidates.Count > 0)
                    {
                        IList<Rect> retained = RemoveAiVehicleOverlappingCandidates(
                            _lastVisibleCandidates,
                            aiVehicleCandidates);
                        _lastVisibleCandidates.Clear();
                        _lastVisibleCandidates.AddRange(retained);
                        if (visibleCandidates.Count == 0 && _lastVisibleCandidates.Count == 0)
                        {
                            _isDetected = false;
                            _clearFrameCount = 0;
                            _visionStableFrames = 0;
                        }
                    }
                    // 2026-08-31: 구분 방식 1은 화면 내 확정 연기를 하나의 외곽으로,
                    // 구분 방식 2는 독립된 연기 기둥별 외곽으로 표시한다.
                    visibleCandidates = ApplySmokeGroupingMode(
                        visibleCandidates,
                        smokeBoxGroupingMode);

                    // 2026-09-02: 독립 연기 Track 수와 일시 누락 중 유지되는 BBox 수를
                    // 제한 로그로 남겨 다중 플룸의 동시 ACTIVE 연속성을 확인한다.
                    bool candidateCountChanged =
                        visibleCandidates.Count != _lastReportedVisibleCandidateCount;
                    if ((candidateCountChanged || analysis.ContinuityHeldCount > 0) &&
                        (now - _lastContinuityLogTime).TotalSeconds >=
                            (candidateCountChanged ? 0.5 : 2.0))
                    {
                        _lastContinuityLogTime = now;
                        _lastReportedVisibleCandidateCount = visibleCandidates.Count;
                        ConsoleLogHelper.State(
                            "SMOKE TRACK CONTINUITY",
                            "Independent plume tracks / CHANNEL=" +
                            (isInfrared ? "IR" : "EO") +
                            " / VISIBLE=" + visibleCandidates.Count +
                            " / HELD=" + analysis.ContinuityHeldCount +
                            " / VERIFIED=" + analysis.VerifiedVisibleCount);
                    }

                    if (analysis.IsConfirmed &&
                        visibleCandidates.Count > 0)
                    {
                        _isDetected = true;
                        _visionStableFrames++;
                        _clearFrameCount = 0;
                        _lastVisibleCandidates.Clear();
                        _lastVisibleCandidates.AddRange(visibleCandidates);
                        _lastDetectionWasVerified =
                            analysis.VerifiedVisibleCount > 0;
                    }
                    else if (_isDetected)
                    {
                        _clearFrameCount++;
                        // 2026-09-03: 짧게 통과한 일반 후보는 빠르게 해제하되,
                        // 장시간 확산·변형이 검증된 플룸만 AI BBox처럼 더 오래 유지한다.
                        int clearFrameLimit = _lastDetectionWasVerified
                            ? (isInfrared ? 90 : 120)
                            : (isInfrared ? 45 : 60);
                        if (_clearFrameCount >= clearFrameLimit)
                        {
                            _isDetected = false;
                            _clearFrameCount = 0;
                            _lastVisibleCandidates.Clear();
                            _visionStableFrames = 0;
                            _lastDetectionWasVerified = false;
                        }
                    }

                    // 2026-08-31: 확정 연기의 마스크가 잠시 끊겨도 마지막 안정 BBox를
                    // 유지하여 Overlay와 이벤트가 프레임 단위로 깜빡이지 않도록 한다.
                    IList<Rect> displayCandidates =
                        visibleCandidates.Count > 0
                            ? visibleCandidates
                            : _isDetected
                                ? _lastVisibleCandidates
                                : visibleCandidates;

                    IList<double> displayScores = _isDetected
                        ? AssignVisionScores(displayCandidates, frame.Width, frame.Height, isInfrared)
                        : new List<double>();
                    _latchedVisionScore = displayScores.Count > 0 ? displayScores[0] : 0.0;

                    if (_isDetected)
                    {
                        DrawCandidates(
                            frame,
                            displayCandidates,
                            isInfrared,
                            displayScores);
                    }

                    Rect largest = Rect.Empty;
                    double largestArea = 0.0;
                    foreach (Rect candidate in displayCandidates)
                    {
                        double area = candidate.Width * candidate.Height;
                        if (area > largestArea)
                        {
                            largest = candidate;
                            largestArea = area;
                        }
                    }

                    return new SmokeDetectionResult(
                        _isDetected,
                        previousState != _isDetected,
                        largestArea,
                        largest,
                        displayCandidates.Count,
                        isInfrared,
                        frame.Width,
                        frame.Height,
                        _latchedVisionScore,
                        displayCandidates,
                        displayScores);
                }
            }
            catch (Exception exception)
            {
                DateTime now = DateTime.Now;
                if ((now - _lastErrorLogTime).TotalSeconds >= 5)
                {
                    _lastErrorLogTime = now;
                    ConsoleLogHelper.Error(
                        "SMOKE DETECTOR",
                        "Candidate processing failed / CHANNEL=" +
                        (isInfrared ? "IR" : "EO"),
                        exception);
                }

                return ResetDetectionState(isInfrared);
            }
        }

        private SmokeDetectionResult ResetDetectionState(bool isInfrared)
        {
            bool stateChanged = _isDetected;
            _isDetected = false;
            _clearFrameCount = 0;
            _visionStableFrames = 0;
            _wasAiSmokeSuppressionActive = false;
            _wasAiVehicleSuppressionActive = false;
            _lastStandaloneFallbackMode = null;
            _lastReportedConfirmationFrames = -1;
            _latchedVisionScore = 0.0;
            _visionScoreTracks.Clear();
            _lastVisibleCandidates.Clear();
            _lastContinuityLogTime = DateTime.MinValue;
            _lastReportedVisibleCandidateCount = -1;
            _lastDetectionWasVerified = false;
            _analyzer.Reset();

            return new SmokeDetectionResult(
                isInfrared,
                stateChanged,
                0.0,
                Rect.Empty,
                0,
                false,
                0,
                0,
                0.0,
                new List<Rect>(),
                new List<double>());
        }

        private static IList<Rect> ApplySmokeGroupingMode(
            IList<Rect> candidates,
            int mode)
        {
            if (candidates == null || candidates.Count <= 1 || mode != 1)
            {
                return candidates ?? new List<Rect>();
            }

            Rect unified = candidates[0];
            for (int index = 1; index < candidates.Count; index++)
            {
                unified |= candidates[index];
            }

            return new List<Rect> { unified };
        }

        private static void DrawCandidates(
            Mat frame,
            IList<Rect> candidates,
            bool isInfrared,
            IList<double> visionScores)
        {
            Scalar color = isInfrared
                ? new Scalar(0, 215, 255)
                : new Scalar(255, 255, 0);
            double scale =
                Math.Max(
                    1.0,
                    Math.Max(frame.Width / 1280.0, frame.Height / 720.0));
            int thickness = Math.Max(3, (int)Math.Round(2.0 * scale));
            // 2026-09-02 V17: AI Overlay의 16px 라벨과 축소 표시 크기를 맞춘다.
            double fontScale = Math.Max(0.36, 0.34 * scale);
            int fontThickness = Math.Max(1, (int)Math.Round(scale));

            for (int index = 0; index < candidates.Count; index++)
            {
                Rect rect = candidates[index];
                double visionScore = index < visionScores.Count ? visionScores[index] : 0.0;
                string label =
                    "VP #" + (index + 1) + " | " +
                    (isInfrared ? "IR Smoke Candidate" : "Smoke") + " " +
                    visionScore.ToString("F1") + "%";
                int baseline;
                Size labelSize =
                    Cv2.GetTextSize(
                        label,
                        HersheyFonts.HersheySimplex,
                        fontScale,
                        fontThickness,
                        out baseline);
                int labelX =
                    Math.Max(0, Math.Min(rect.X, frame.Width - labelSize.Width - 6));
                // 2026-09-02 V17: 작은 BBox에서 라벨이 영상을 가리지 않도록
                // 기존처럼 BBox 위쪽 외부에 두고 화면 상단에서만 내부로 보정한다.
                int labelY = Math.Max(
                    labelSize.Height + 6,
                    Math.Min(rect.Y - 7, frame.Height - baseline - 2));
                int labelTop = Math.Max(0, labelY - labelSize.Height - 5);

                Cv2.Rectangle(frame, rect, color, thickness);
                Cv2.Rectangle(
                    frame,
                    new Rect(
                        labelX,
                        labelTop,
                        Math.Min(labelSize.Width + 6, frame.Width - labelX),
                        Math.Min(
                            labelSize.Height + baseline + 7,
                            frame.Height - labelTop)),
                    new Scalar(24, 24, 24),
                    -1);
                Cv2.PutText(
                    frame,
                    label,
                    new Point(labelX + 3, labelY),
                    HersheyFonts.HersheySimplex,
                    fontScale,
                    color,
                    fontThickness);
            }
        }

        private static IList<Rect> RemoveAiOverlappingCandidates(
            IList<Rect> visionCandidates,
            IList<Rect> aiCandidates)
        {
            if (visionCandidates == null || visionCandidates.Count == 0 ||
                aiCandidates == null || aiCandidates.Count == 0)
            {
                return visionCandidates ?? new List<Rect>();
            }

            List<Rect> filtered = new List<Rect>();
            foreach (Rect vision in visionCandidates)
            {
                bool duplicate = false;
                foreach (Rect ai in aiCandidates)
                {
                    Rect intersection = vision & ai;
                    double intersectionArea = Math.Max(0, intersection.Width) * Math.Max(0, intersection.Height);
                    double visionArea = Math.Max(1.0, vision.Width * vision.Height);
                    double aiArea = Math.Max(1.0, ai.Width * ai.Height);
                    double unionArea = Math.Max(1.0, visionArea + aiArea - intersectionArea);
                    if (intersectionArea / Math.Min(visionArea, aiArea) >= 0.35 ||
                        intersectionArea / unionArea >= 0.12)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    filtered.Add(vision);
                }
            }

            return filtered;
        }

        private static IList<Rect> RemoveAiVehicleOverlappingCandidates(
            IList<Rect> smokeCandidates,
            IList<Rect> vehicleCandidates)
        {
            if (smokeCandidates == null || smokeCandidates.Count == 0 ||
                vehicleCandidates == null || vehicleCandidates.Count == 0)
            {
                return smokeCandidates ?? new List<Rect>();
            }

            List<Rect> filtered = new List<Rect>();
            foreach (Rect smoke in smokeCandidates)
            {
                bool vehicleGeneratedChange = false;
                Point smokeCenter = new Point(
                    smoke.X + smoke.Width / 2,
                    smoke.Y + smoke.Height / 2);
                Point smokeSource = new Point(
                    smoke.X + smoke.Width / 2,
                    smoke.Bottom);

                foreach (Rect vehicle in vehicleCandidates)
                {
                    int horizontalMargin = Math.Max(16, (int)Math.Round(vehicle.Width * 0.80));
                    int topMargin = Math.Max(12, (int)Math.Round(vehicle.Height * 0.45));
                    int bottomMargin = Math.Max(12, (int)Math.Round(vehicle.Height * 0.35));
                    Rect activityCorridor = new Rect(
                        vehicle.X - horizontalMargin,
                        vehicle.Y - topMargin,
                        vehicle.Width + horizontalMargin * 2,
                        vehicle.Height + topMargin + bottomMargin);

                    Rect intersection = smoke & activityCorridor;
                    double intersectionArea =
                        Math.Max(0, intersection.Width) *
                        Math.Max(0, intersection.Height);
                    double smokeArea = Math.Max(1.0, smoke.Width * (double)smoke.Height);
                    bool centerInside = activityCorridor.Contains(smokeCenter);
                    bool sourceInside = activityCorridor.Contains(smokeSource);

                    if (centerInside ||
                        sourceInside ||
                        intersectionArea / smokeArea >= 0.12)
                    {
                        vehicleGeneratedChange = true;
                        break;
                    }
                }

                if (!vehicleGeneratedChange)
                {
                    filtered.Add(smoke);
                }
            }

            return filtered;
        }

        private IList<double> AssignVisionScores(
            IList<Rect> candidates, int frameWidth, int frameHeight, bool isInfrared)
        {
            DateTime nowUtc = DateTime.UtcNow;
            foreach (VisionScoreTrack track in _visionScoreTracks)
            {
                track.Matched = false;
            }

            List<double> scores = new List<double>();
            foreach (Rect candidate in candidates)
            {
                VisionScoreTrack matched = null;
                double bestMatch = 0.0;
                foreach (VisionScoreTrack track in _visionScoreTracks)
                {
                    if (track.Matched)
                    {
                        continue;
                    }

                    double match = CalculateRectMatch(track.Rectangle, candidate);
                    if (match > bestMatch)
                    {
                        bestMatch = match;
                        matched = track;
                    }
                }

                if (matched == null || bestMatch < 0.25)
                {
                    matched = new VisionScoreTrack
                    {
                        InitialRectangle = candidate,
                        FirstSeenUtc = nowUtc
                    };
                    _visionScoreTracks.Add(matched);
                }

                if (!matched.IsScoreFinalized)
                {
                    double elapsedSeconds = Math.Max(0.0,
                        (nowUtc - matched.FirstSeenUtc).TotalSeconds);
                    matched.Score = CalculateCandidateVisionScore(
                        matched, candidate, frameWidth, frameHeight,
                        isInfrared, elapsedSeconds);
                    if (elapsedSeconds >= 1.5)
                    {
                        matched.IsScoreFinalized = true;
                        ConsoleLogHelper.State("SMOKE V.SCORE",
                            "Track score finalized / CAMERA=" + (isInfrared ? "IR" : "EO") +
                            " / SCORE=" + matched.Score.ToString("F1") +
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

        // 2026-08-31: V.SCORE는 확률이 아니라 Track의 시공간 연기 증거 점수이다.
        // 최초 약 1~2초 동안 지속·상향 이동·확산·형상 변화를 누적한 뒤 고정한다.
        private static double CalculateCandidateVisionScore(
            VisionScoreTrack track, Rect candidate,
            int frameWidth, int frameHeight, bool isInfrared,
            double elapsedSeconds)
        {
            double frameArea = Math.Max(1.0, frameWidth * (double)frameHeight);
            double areaRatio = candidate.Width * candidate.Height /
                frameArea;
            double verticality = candidate.Height /
                (double)Math.Max(1, candidate.Width + candidate.Height);
            double aspectBalance = Math.Min(candidate.Width, candidate.Height) /
                (double)Math.Max(1, Math.Max(candidate.Width, candidate.Height));
            double initialArea = Math.Max(1.0,
                track.InitialRectangle.Width * (double)track.InitialRectangle.Height);
            double currentArea = Math.Max(1.0, candidate.Width * (double)candidate.Height);
            double expansionRatio = currentArea / initialArea - 1.0;

            double initialCenterX = track.InitialRectangle.X + track.InitialRectangle.Width / 2.0;
            double initialCenterY = track.InitialRectangle.Y + track.InitialRectangle.Height / 2.0;
            double currentCenterX = candidate.X + candidate.Width / 2.0;
            double currentCenterY = candidate.Y + candidate.Height / 2.0;
            double deltaX = currentCenterX - initialCenterX;
            double deltaY = currentCenterY - initialCenterY;
            double frameDiagonal = Math.Max(1.0,
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

            double score = (isInfrared ? 20.0 : 22.0) +
                Math.Min(12.0, Math.Sqrt(Math.Max(0.0, areaRatio)) * 46.0) +
                Math.Min(4.0, verticality * 7.0) +
                Math.Min(3.0, aspectBalance * 3.0) +
                Math.Min(20.0, elapsedSeconds / 1.5 * 20.0) +
                Math.Min(8.0, upwardRatio * 160.0) +
                Math.Min(10.0, Math.Max(0.0, expansionRatio) * 12.0) +
                Math.Min(5.0, motionRatio * 100.0) +
                Math.Min(5.0, shapeChange * 8.0);

            // 센서 좌표에 고정되고 크기·형상이 거의 변하지 않는 후보는
            // 물방울·렌즈 얼룩 가능성이 높으므로 해당 후보만 감점한다.
            if (elapsedSeconds >= 0.5 &&
                motionRatio < 0.006 &&
                Math.Abs(expansionRatio) < 0.10 &&
                shapeChange < 0.10)
            {
                score -= 15.0;
            }

            return Math.Max(5.0, Math.Min(92.0, score));
        }

        private static double CalculateRectMatch(Rect left, Rect right)
        {
            Rect intersection = left & right;
            double intersectionArea = Math.Max(0, intersection.Width) * Math.Max(0, intersection.Height);
            double smallerArea = Math.Max(1.0, Math.Min(
                left.Width * (double)left.Height, right.Width * (double)right.Height));
            return intersectionArea / smallerArea;
        }

        private sealed class VisionScoreTrack
        {
            internal Rect InitialRectangle { get; set; }
            internal Rect Rectangle { get; set; }
            internal double Score { get; set; }
            internal DateTime FirstSeenUtc { get; set; }
            internal bool IsScoreFinalized { get; set; }
            internal bool Matched { get; set; }
        }

        /// <summary>
        /// 2026-08-27: IR 고온 FIRE 후보와 같은 위치의 연기 후보를 제거한다.
        /// 동일 영역이 두 분석기에 동시에 포함되면 FIRE 판정을 우선한다.
        /// </summary>
        private static IList<Rect> RemoveFireOverlappingCandidates(
            IList<Rect> smokeCandidates,
            Rect fireCandidate)
        {
            if (fireCandidate == Rect.Empty ||
                smokeCandidates == null ||
                smokeCandidates.Count == 0)
            {
                return smokeCandidates ?? new List<Rect>();
            }

            List<Rect> filtered =
                new List<Rect>();

            foreach (Rect smokeCandidate in smokeCandidates)
            {
                Rect intersection = fireCandidate & smokeCandidate;
                double intersectionArea =
                    Math.Max(0, intersection.Width) *
                    Math.Max(0, intersection.Height);
                double smallerArea =
                    Math.Max(
                        1.0,
                        Math.Min(
                            fireCandidate.Width * fireCandidate.Height,
                            smokeCandidate.Width * smokeCandidate.Height));
                Point smokeCenter =
                    new Point(
                        smokeCandidate.X + smokeCandidate.Width / 2,
                        smokeCandidate.Y + smokeCandidate.Height / 2);

                if (intersectionArea / smallerArea < 0.25 &&
                    !fireCandidate.Contains(smokeCenter))
                {
                    filtered.Add(smokeCandidate);
                }
            }

            return filtered;
        }

    }

    /// <summary>
    /// 2026-08-27: 메인 Viewer에 전달되는 연기 후보 상태이다.
    /// </summary>
    internal struct SmokeDetectionResult
    {
        internal SmokeDetectionResult(
            bool isDetected,
            bool stateChanged,
            double candidateArea,
            Rect candidateRect,
            int candidateCount,
            bool isInfraredSupport,
            int frameWidth,
            int frameHeight,
            double visionScore,
            IList<Rect> candidateRects,
            IList<double> candidateScores)
        {
            IsDetected = isDetected;
            StateChanged = stateChanged;
            CandidateArea = candidateArea;
            CandidateRect = candidateRect;
            CandidateCount = candidateCount;
            IsInfraredSupport = isInfraredSupport;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
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

        internal bool IsInfraredSupport { get; }

        internal int FrameWidth { get; }

        internal int FrameHeight { get; }

        internal double VisionScore { get; }

        internal IList<Rect> CandidateRects { get; }

        internal IList<double> CandidateScores { get; }
    }

}
