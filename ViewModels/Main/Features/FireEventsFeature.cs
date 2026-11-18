using Microsoft.Win32;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Models.Position;
using OpenCvWpfTracking.Services.Video;
using OpenCvWpfTracking.Services.Position;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CvRect = OpenCvSharp.Rect;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>화재·연기 이벤트에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IFireEventsFeatureContext
    {
        List<FireEventRecord> _activeTestFireEvents { get; }

        bool _hasEoLocalFireDetection { get; set; }

        bool _hasEoLocalSmokeDetection { get; set; }

        bool _hasIrLocalFireDetection { get; set; }

        bool _hasIrLocalSmokeDetection { get; set; }

        bool _isEoFireCandidateDetected { get; set; }

        bool _isIrFireCandidateDetected { get; set; }

        ZoneAssessment AssessZone(string camera, string type, double left, double top, double right, double bottom, ZoneFrameContext f);

        PositionSnapshot CapturePositionSnapshot(string source, int? presetId = null);

        ObservableCollection<AiDetectionBox> EoDetectionBoxes { get; }

        ObservableCollection<VisionDetectionBox> EoFireSmokeDetectionBoxes { get; }

        int EoVideoHeight { get; set; }

        int EoVideoWidth { get; set; }

        Brush EventAlertHeaderBrush { get; }

        string EventAlertHeaderText { get; }

        ObservableCollection<FireEventRecord> FireDetectionEvents { get; }

        void InitializeEventAlertFeatures();

        ObservableCollection<AiDetectionBox> IrDetectionBoxes { get; }

        ObservableCollection<VisionDetectionBox> IrFireSmokeDetectionBoxes { get; }

        int IrVideoHeight { get; set; }

        int IrVideoWidth { get; set; }

        bool IsEoFireWarningVisible { get; }

        bool IsEoSmokeWarningVisible { get; }

        bool IsIrFireWarningVisible { get; }

        bool IsIrSmokeWarningVisible { get; }

        bool IsSmokeDetectionEnabled { get; set; }

        bool IsThermalFireDetectionEnabled { get; set; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        void RefreshActiveFireCount();

        void SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke);

        Action SaveFireEventsCallback { get; }

        Action LoadFireEventsCallback { get; }

        Action ClearFireEventsCallback { get; }
    }

    /// <summary>화재·연기 이벤트 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class FireEventsFeature
    {

        #region [협력 객체와 초기화]

        private readonly IFireEventsFeatureContext _context;

        internal FireEventsFeature(IFireEventsFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _nextFireEventId = 1;
            _nextVisionTrackId = 1;

            FireEvents = new ObservableCollection<FireEventRecord>();
        }

        #endregion

        #region [상태 필드]

        internal int _nextFireEventId;

        internal bool _isFireCsvHistoryLoaded;

        internal int _activeFireCount;

        internal DateTime? _lastFireDetectedTime;

        internal long _nextVisionTrackId;

        internal readonly List<VisionBBoxEventTrack> _activeVisionBBoxEvents = new List<VisionBBoxEventTrack>();

        internal int _visionSettingsGeneration;

        #endregion

        #region [화면 속성과 이벤트]

        internal ObservableCollection<FireEventRecord> FireEvents { get; set; }

        internal ICommand SaveFireEventsCommand { get; set; }

        internal ICommand LoadFireEventsCommand { get; set; }

        internal ICommand ClearFireEventsCommand { get; set; }

        internal int ActiveFireCount
        {
            get { return _activeFireCount; }

            set
            {
                if (_activeFireCount == value)
                {
                    return;
                }

                _activeFireCount = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(FireAlertHeaderText));

                _context.OnPropertyChanged(nameof(FireAlertStatusText));

                _context.OnPropertyChanged(nameof(FireAlertHeaderBrush));

                _context.OnPropertyChanged(nameof(_context.EventAlertHeaderText));

                _context.OnPropertyChanged(nameof(_context.EventAlertHeaderBrush));
            }
        }

        internal int TotalFireEventCount
        {
            get { return FireEvents.Count; }
        }

        internal string LastFireEventTimeText
        {
            get
            {
                if (_lastFireDetectedTime.HasValue)
                {
                    return _lastFireDetectedTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    return "-";
                }
            }
        }

        internal string FireAlertHeaderText
        {
            get
            {
                if (ActiveFireCount > 0)
                {
                    return "화재 / 연기 알림 (" + ActiveFireCount + ")";
                }
                else
                {
                    return "화재 / 연기 알림";
                }
            }
        }

        internal string FireAlertStatusText
        {
            get
            {
                if (ActiveFireCount > 0)
                {
                    return "FIRE / SMOKE DETECTED";
                }
                else
                {
                    return !_context.IsThermalFireDetectionEnabled && !_context.IsSmokeDetectionEnabled ? "FIRE / SMOKE OFF" : "MONITORING";
                }
            }
        }

        internal Brush FireAlertHeaderBrush
        {
            get
            {
                return ActiveFireCount > 0 // 2026-08-27: FIRE와 흰색 SMOKE를 함께 나타내는 밝은 코럴 색상이다.
 ? LegacyVideoIntegration.EventBrush(_context.FireDetectionEvents.Any(e => e.Status == "ACTIVE" && e.DetectionType == "FIRE") ? "FIRE" : "SMOKE") : new SolidColorBrush(Color.FromRgb(208, 215, 222));
            }
        }

        #endregion

        #region [동작 처리]

        internal void InitializeFireEventFeatures()
        {
            SaveFireEventsCommand = new RelayCommand(_context.SaveFireEventsCallback);

            LoadFireEventsCommand = new RelayCommand(_context.LoadFireEventsCallback);

            ClearFireEventsCommand = new RelayCommand(_context.ClearFireEventsCallback);

            _context.InitializeEventAlertFeatures();
        }

        internal void UpdateFireSmokeDetectionOverlay(string camera, ThermalFireDetectionResult thermalResult, SmokeDetectionResult smokeResult)
        {
            if (!_context.IsThermalFireDetectionEnabled)
            {
                thermalResult = default(ThermalFireDetectionResult);
            }

            if (!_context.IsSmokeDetectionEnabled)
            {
                smokeResult = default(SmokeDetectionResult);
            }

            ObservableCollection<VisionDetectionBox> target = string.Equals(camera, "IR", StringComparison.OrdinalIgnoreCase) ? _context.IrFireSmokeDetectionBoxes : _context.EoFireSmokeDetectionBoxes;

            int frameWidth = string.Equals(camera, "IR", StringComparison.OrdinalIgnoreCase) ? Math.Max(1, _context.IrVideoWidth) : Math.Max(1, _context.EoVideoWidth);

            int frameHeight = string.Equals(camera, "IR", StringComparison.OrdinalIgnoreCase) ? Math.Max(1, _context.IrVideoHeight) : Math.Max(1, _context.EoVideoHeight);

            bool isInfrared = string.Equals(camera, "IR", StringComparison.OrdinalIgnoreCase);

            bool hasLocalFire = thermalResult.CandidateRects != null && thermalResult.CandidateRects.Count > 0;

            bool hasLocalSmoke = smokeResult.CandidateRects != null && smokeResult.CandidateRects.Count > 0;

            // 2026-09-22: 중복 제거 전 원본 탐지 상태를 보존한다.
            // 화면용 BBox가 AI 우선 정책으로 제거되어도 경고는 실제 탐지 상태를 유지한다.
            _context.SetLocalFireSmokeDetectionState(isInfrared ? 1 : 0, hasLocalFire, hasLocalSmoke);

            target.Clear();

            int displayOrder = 1;

            // EO 영상처리 화재도 IR과 같은 검출 경로를 사용하므로 EO 상자를 버리지 않는다.
            AddVisionOverlayBoxes(target, thermalResult.CandidateRects, thermalResult.CandidateScores, "FIRE", frameWidth, frameHeight, ref displayOrder);

            AddVisionOverlayBoxes(target, smokeResult.CandidateRects, smokeResult.CandidateScores, "SMOKE", frameWidth, frameHeight, ref displayOrder);

            // 2026-09-17: AI Agent와 로컬 분석이 동일 객체를 검출한 경우
            // 화면에는 AI BBox를 우선 표시하여 라벨/박스 중복을 방지한다.
            SuppressLocalBoxesOverlappingAi(target, string.Equals(camera, "IR", StringComparison.OrdinalIgnoreCase) ? _context.IrDetectionBoxes : _context.EoDetectionBoxes);

            _context.OnPropertyChanged(isInfrared ? nameof(_context.IsIrFireWarningVisible) : nameof(_context.IsEoFireWarningVisible));

            _context.OnPropertyChanged(isInfrared ? nameof(_context.IsIrSmokeWarningVisible) : nameof(_context.IsEoSmokeWarningVisible));
        }

        internal void ClearDisabledVisionEvents(bool fire, bool writeAudit = true)
        {
            string kind = fire ? "FIRE" : "SMOKE";

            DateTime now = DateTime.Now;

            foreach (var track in _activeVisionBBoxEvents.Where(x => x.DetectionType.IndexOf(kind, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
            {
                track.Event.MarkCleared(now);

                if (writeAudit)
                {
                    AppendFireEventAudit(track.Event, "CLEARED");
                }

                _activeVisionBBoxEvents.Remove(track);
            }

            foreach (var record in _context._activeTestFireEvents.Where(x => x.DetectionType.IndexOf(kind, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
            {
                record.MarkCleared(now);

                if (writeAudit)
                {
                    AppendFireEventAudit(record, "CLEARED");
                }

                _context._activeTestFireEvents.Remove(record);
            }

            foreach (var boxes in new[]
            {
                _context.EoFireSmokeDetectionBoxes,
                _context.IrFireSmokeDetectionBoxes
            }

            )
            {
                for (int i = boxes.Count - 1; i >= 0; i--)
                {
                    if (boxes[i].DetectionType.IndexOf(kind, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        boxes.RemoveAt(i);
                    }
                }
            }

            if (fire)
            {
                _context._isEoFireCandidateDetected = false;
                _context._isIrFireCandidateDetected = false;
            }

            _context.SetLocalFireSmokeDetectionState(0, fire ? false : _context._hasEoLocalFireDetection, fire ? _context._hasEoLocalSmokeDetection : false);

            _context.SetLocalFireSmokeDetectionState(1, fire ? false : _context._hasIrLocalFireDetection, fire ? _context._hasIrLocalSmokeDetection : false);

            _context.RefreshActiveFireCount();

            NotifyFireEventSummaryChanged();

            ConsoleLogHelper.State("VISION BBOX EVENT", "ACTION=CLEAR / TYPE=" + kind + " / REASON=DETECTOR_DISABLED / HISTORY_RETAINED");
        }

        internal static void SuppressLocalBoxesOverlappingAi(ObservableCollection<VisionDetectionBox> localBoxes, ObservableCollection<AiDetectionBox> aiBoxes)
        {
            if (aiBoxes == null || aiBoxes.Count == 0)
            {
                return;
            }

            for (int index = localBoxes.Count - 1; index >= 0; index--)
            {
                VisionDetectionBox local = localBoxes[index];

                double localArea = Math.Max(1, local.Width * local.Height);

                foreach (AiDetectionBox ai in aiBoxes)
                {
                    int width = Math.Max(0, Math.Min(local.Right, ai.Right) - Math.Max(local.Left, ai.Left));

                    int height = Math.Max(0, Math.Min(local.Bottom, ai.Bottom) - Math.Max(local.Top, ai.Top));

                    if ((width * height) / localArea >= 0.35)
                    {
                        localBoxes.RemoveAt(index);

                        break;
                    }
                }
            }
        }

        internal static void AddVisionOverlayBoxes(ObservableCollection<VisionDetectionBox> target, IList<CvRect> rectangles, IList<double> scores, string detectionType, int frameWidth, int frameHeight, ref int displayOrder)
        {
            if (rectangles == null)
            {
                return;
            }

            for (int index = 0; index < rectangles.Count; index++)
            {
                CvRect rectangle = rectangles[index];

                // 2026-09-17: Stroke 중심선이 영상 경계 밖으로 나가 잘리지 않도록
                // 2px 안전 여백 안으로 좌표를 제한한다. 검출 좌표 자체는 유지한다.
                int insetX = frameWidth > 4 ? 2 : 0;

                int insetY = frameHeight > 4 ? 2 : 0;

                int left = Math.Max(insetX, Math.Min(frameWidth - insetX - 1, rectangle.X));

                int top = Math.Max(insetY, Math.Min(frameHeight - insetY - 1, rectangle.Y));

                int right = Math.Max(left + 1, Math.Min(frameWidth - insetX, rectangle.Right));

                int bottom = Math.Max(top + 1, Math.Min(frameHeight - insetY, rectangle.Bottom));

                int currentOrder = displayOrder++;

                double confidence = scores != null && index < scores.Count ? scores[index] : 0.0;

                string displayText = string.Format(CultureInfo.InvariantCulture, "{0} #{1} | {2:F1}%", detectionType, currentOrder, confidence);

                int labelWidth = Math.Max(40, Math.Min((int)Math.Ceiling(displayText.Length * 9.5 + 12.0), frameWidth - 4));

                const int labelHeight = 24;

                int labelLeft = Math.Max(2, Math.Min(frameWidth - labelWidth - 2, left));

                int labelTop = top - labelHeight >= 2 ? top - labelHeight : Math.Max(2, Math.Min(frameHeight - labelHeight - 2, top + 2));

                int overlayLeft = Math.Min(left, labelLeft);

                int overlayTop = Math.Min(top, labelTop);

                int overlayRight = Math.Max(right, labelLeft + labelWidth);

                int overlayBottom = Math.Max(bottom, labelTop + labelHeight);

                target.Add(new VisionDetectionBox
                {
                    DisplayOrder = currentOrder,
                    DetectionType = detectionType,
                    ConfidencePercent = confidence,
                    Left = left,
                    Top = top,
                    Right = right,
                    Bottom = bottom, // 라벨 예상 폭보다 우측 여유가 작으면 BBox 우측에 맞춰 왼쪽으로 펼친다.
                    PlaceLabelOnRight = right > frameWidth - 190,
                    LabelWidth = labelWidth,
                    OverlayLeft = overlayLeft,
                    OverlayTop = overlayTop,
                    OverlayWidth = Math.Max(1, overlayRight - overlayLeft),
                    OverlayHeight = Math.Max(1, overlayBottom - overlayTop),
                    BoxOffsetX = left - overlayLeft,
                    BoxOffsetY = top - overlayTop,
                    LabelOffsetX = labelLeft - overlayLeft,
                    LabelOffsetY = labelTop - overlayTop
                });
            }
        }

        internal void UpdateVisionBBoxEvents(string camera, string detectionType, IList<CvRect> candidates, IList<double> candidateScores, string detectionSource, ZoneFrameContext zoneFrame = null)
        {
            DateTime now = DateTime.Now;

            bool enabled = detectionType.IndexOf("SMOKE", StringComparison.OrdinalIgnoreCase) >= 0 ? _context.IsSmokeDetectionEnabled : _context.IsThermalFireDetectionEnabled;

            IList<CvRect> safeCandidates = enabled ? candidates ?? new List<CvRect>() : new List<CvRect>();

            foreach (VisionBBoxEventTrack track in _activeVisionBBoxEvents.Where(item => item.Camera == camera && item.DetectionType == detectionType))
            {
                track.Matched = false;
            }

            // 2026-09-09: 후보 순서에 따라 Track을 잘못 빼앗지 않도록 모든 조합을
            // 점수순으로 먼저 매칭한다. 검출기가 Track ID를 제공하지 않아 내부 ID를 사용한다.
            List<Tuple<double, VisionBBoxEventTrack, int>> matches = new List<Tuple<double, VisionBBoxEventTrack, int>>();

            List<VisionBBoxEventTrack> channelTracks = _activeVisionBBoxEvents.Where(item => item.Camera == camera && item.DetectionType == detectionType).ToList();

            for (int candidateIndex = 0; candidateIndex < safeCandidates.Count; candidateIndex++)
            {
                foreach (VisionBBoxEventTrack track in channelTracks)
                {
                    double score = VisionBBoxMatchRatio(track.Rectangle, safeCandidates[candidateIndex]);

                    if (score >= 0.20)
                    {
                        matches.Add(Tuple.Create(score, track, candidateIndex));
                    }
                }
            }

            HashSet<int> matchedCandidateIndexes = new HashSet<int>();

            foreach (Tuple<double, VisionBBoxEventTrack, int> match in matches.OrderByDescending(item => item.Item1))
            {
                if (match.Item2.Matched || matchedCandidateIndexes.Contains(match.Item3))
                {
                    continue;
                }

                CvRect candidate = safeCandidates[match.Item3];

                double score = candidateScores != null && match.Item3 < candidateScores.Count ? candidateScores[match.Item3] : 0.0;

                match.Item2.Rectangle = candidate;
                match.Item2.LastSeen = now;
                match.Item2.Matched = true;

                match.Item2.Event.UpdateVisionSnapshot(score, candidate.Width, candidate.Height);

                match.Item2.Event.UpdateZoneAssessment(_context.AssessZone(camera, detectionType, candidate.Left, candidate.Top, candidate.Right, candidate.Bottom, zoneFrame));

                matchedCandidateIndexes.Add(match.Item3);
            }

            for (int candidateIndex = 0; candidateIndex < safeCandidates.Count; candidateIndex++)
            {
                if (matchedCandidateIndexes.Contains(candidateIndex))
                {
                    continue;
                }

                CvRect candidate = safeCandidates[candidateIndex];

                double visionScore = candidateScores != null && candidateIndex < candidateScores.Count ? candidateScores[candidateIndex] : 0.0;

                FireEventRecord record = new FireEventRecord(_nextFireEventId++, now, null, camera, detectionType, visionScore.ToString("F1", CultureInfo.InvariantCulture) + "%", 1, candidate.Width, candidate.Height, candidate.Width * (double)candidate.Height, detectionSource, "ACTIVE", _context.CapturePositionSnapshot("VISION_" + detectionType + "_EVENT"));

                record.UpdateZoneAssessment(_context.AssessZone(camera, detectionType, candidate.Left, candidate.Top, candidate.Right, candidate.Bottom, zoneFrame));

                if (_isFireCsvHistoryLoaded)
                {
                    record.MarkLiveAfterCsvLoad();
                }

                _activeVisionBBoxEvents.Add(new VisionBBoxEventTrack { TrackId = _nextVisionTrackId++, Camera = camera, DetectionType = detectionType, Rectangle = candidate, LastSeen = now, Matched = true, Event = record });

                FireEvents.Insert(0, record);

                TrimEventCollection(FireEvents);

                _lastFireDetectedTime = now;

                AppendFireEventAudit(record, "DETECTED");

                ConsoleLogHelper.Warning("VISION BBOX EVENT", "ACTION=CREATE / EVENT_ID=" + record.EventId + " / TRACK_ID=" + (_nextVisionTrackId - 1) + " / CAMERA=" + camera + " / TYPE=" + detectionType + " / SCORE=" + record.Confidence + " / BBOX=" + record.PixelSizeCompactText);
            }

            foreach (VisionBBoxEventTrack expired in _activeVisionBBoxEvents.Where(item => item.Camera == camera && item.DetectionType == detectionType && !item.Matched).ToList())
            {
                expired.Event.MarkCleared(now);

                AppendFireEventAudit(expired.Event, "CLEARED");

                _activeVisionBBoxEvents.Remove(expired);

                ConsoleLogHelper.State("VISION BBOX EVENT", "ACTION=CLEAR / EVENT_ID=" + expired.Event.EventId + " / TRACK_ID=" + expired.TrackId + " / CAMERA=" + camera + " / TYPE=" + detectionType + " / REASON=TRACK_EXPIRED");
            }

            _context.RefreshActiveFireCount();

            NotifyFireEventSummaryChanged();

#if DEBUG
            int channelTrackCount = _activeVisionBBoxEvents.Count(item => item.Camera == camera && item.DetectionType == detectionType);

            if (channelTrackCount != safeCandidates.Count)
            {
                ConsoleLogHelper.Warning("EVENT SYNC WARNING", "VISION_BBOX_ACTIVE=" + safeCandidates.Count + " / VISION_EVENT_ACTIVE=" + channelTrackCount + " / CAMERA=" + camera + " / TYPE=" + detectionType);
            }
#endif
        }

        internal static double VisionBBoxMatchRatio(CvRect left, CvRect right)
        {
            CvRect intersection = left & right;

            double intersectionArea = Math.Max(0, intersection.Width) * Math.Max(0, intersection.Height);

            double smallerArea = Math.Max(1.0, Math.Min(left.Width * (double)left.Height, right.Width * (double)right.Height));

            double overlap = intersectionArea / smallerArea;

            double deltaX = (left.X + left.Width / 2.0) - (right.X + right.Width / 2.0);

            double deltaY = (left.Y + left.Height / 2.0) - (right.Y + right.Height / 2.0);

            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            double allowedDistance = Math.Max(24.0, Math.Max(Math.Max(left.Width, left.Height), Math.Max(right.Width, right.Height)) * 1.10);

            if (overlap >= 0.20 || distance > allowedDistance)
            {
                return overlap;
            }
            else
            {
                return 0.20 + (1.0 - distance / allowedDistance) * 0.35;
            }
        }

        internal void NotifyFireEventSummaryChanged()
        {
            _context.OnPropertyChanged(nameof(TotalFireEventCount));

            _context.OnPropertyChanged(nameof(LastFireEventTimeText));

            _context.OnPropertyChanged(nameof(FireAlertHeaderText));

            _context.OnPropertyChanged(nameof(FireAlertStatusText));

            _context.OnPropertyChanged(nameof(FireAlertHeaderBrush));

            _context.OnPropertyChanged(nameof(_context.EventAlertHeaderText));

            _context.OnPropertyChanged(nameof(_context.EventAlertHeaderBrush));
        }

        internal void SaveFireEvents()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "화재 / 연기 이벤트 CSV 저장",
                Filter = "CSV file|*.csv",
                FileName = "FireSmokeEvent_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                WriteFireEventCsv(dialog.FileName, FireEvents.OrderBy(item => item.EventId));

                ConsoleLogHelper.State("FIRE EVENT", "Event CSV saved / PATH=" + dialog.FileName + " / COUNT=" + FireEvents.Count);

                // 2026-08-27: 통합된 화재·연기 이벤트 명칭으로 저장 결과를 안내한다.
                MessageBox.Show("화재 / 연기 이벤트 CSV 저장이 완료되었습니다.", "CSV 저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("FIRE / SMOKE EVENT", "Event CSV save failed", ex);

                MessageBox.Show("화재 / 연기 이벤트 CSV 저장에 실패했습니다.\n" + ex.Message, "FIRE / SMOKE EVENT", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void LoadFireEvents()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "화재 / 연기 이벤트 CSV 불러오기",
                Filter = "CSV file|*.csv|All files|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                IList<FireEventRecord> loaded = ReadFireEventCsv(dialog.FileName);

                ValidateEventCsvKind(loaded, false);

                FireEvents.Clear();

                foreach (FireEventRecord fireEvent in loaded.Where(item => string.Equals(item.DetectionType, "FIRE", StringComparison.OrdinalIgnoreCase) || string.Equals(item.DetectionType, "SMOKE", StringComparison.OrdinalIgnoreCase)).OrderByDescending(item => item.EventId).Take(MaximumEventHistoryCount))
                {
                    fireEvent.MarkImportedFromCsv();

                    FireEvents.Add(fireEvent);
                }

                _nextFireEventId = FireEvents.Count == 0 ? 1 : FireEvents.Max(item => item.EventId) + 1;

                _isFireCsvHistoryLoaded = true;

                _context._activeTestFireEvents.Clear();

                _activeVisionBBoxEvents.Clear();

                ActiveFireCount = 0;

                _lastFireDetectedTime = FireEvents.Count == 0 ? (DateTime?)null : FireEvents.Max(item => item.DetectedTime);

                NotifyFireEventSummaryChanged();

                ConsoleLogHelper.State("FIRE / SMOKE EVENT", "Event CSV loaded / PATH=" + dialog.FileName + " / COUNT=" + FireEvents.Count);

                // 2026-08-27: 저장과 동일하게 불러오기 결과와 건수를 즉시 안내한다.
                MessageBox.Show("화재 / 연기 이벤트 CSV 불러오기가 완료되었습니다.\n" + "불러온 이벤트: " + FireEvents.Count + "건", "CSV 불러오기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("FIRE / SMOKE EVENT", "Event CSV load failed", ex);

                MessageBox.Show(ex is FormatException ? ex.Message : "CSV를 읽지 못했습니다.\n파일을 확인하세요.", "FIRE / SMOKE EVENT", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void ClearFireEvents()
        {
            FireEvents.Clear();

            _nextFireEventId = 1;
            _isFireCsvHistoryLoaded = false;

            _context._activeTestFireEvents.Clear();

            _activeVisionBBoxEvents.Clear();

            _context.EoFireSmokeDetectionBoxes.Clear();

            _context.IrFireSmokeDetectionBoxes.Clear();

            _context.SetLocalFireSmokeDetectionState(0, false, false);

            _context.SetLocalFireSmokeDetectionState(1, false, false);

            _context.RefreshActiveFireCount();

            _lastFireDetectedTime = null;

            NotifyFireEventSummaryChanged();

            ConsoleLogHelper.State("FIRE EVENT", "Event list cleared / NEXT_ID=1");
        }

        internal static void AppendFireEventAudit(FireEventRecord fireEvent, string transition)
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OpenCvWpfTracking", "FireEvents");

                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory, "FireEvent_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");

                // XLSX 읽기와 CSV 헤더·스키마 확인 및 추가 기록이 충돌하지 않게 한다.
                // 기본 일별 경로를 먼저 잠그고 필요하면 이전 스키마 대체 경로를 잠근다.
                lock (OpenCvWpfTracking.Services.Configuration.CsvExcelCompanion.GetCsvFileLock(path))
                {
                    // 이전 확장 스키마 헤더 아래에 기본 행 형식을 추가하지 않는다.
                    string auditHeader = GetFireEventCsvHeader() + ",Transition";

                    if (File.Exists(path) && new FileInfo(path).Length > 0)
                    {
                        using (var reader = new StreamReader(path, Encoding.UTF8))
                        {
                            if (reader.ReadLine() != auditHeader)

                            // 이전에 저장한 감사 CSV와 같은 파일을 이어 쓰기 위한 호환 파일명이다. 프로그램 버전 표시는 아니다.
                            {
                                path = Path.Combine(directory, "FireEvent_" + DateTime.Now.ToString("yyyyMMdd") + "_V29_5.csv");
                            }
                        }
                    }

                    lock (OpenCvWpfTracking.Services.Configuration.CsvExcelCompanion.GetCsvFileLock(path))
                    {
                        bool writeHeader = !File.Exists(path) || new FileInfo(path).Length == 0;

                        using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(true)))
                        {
                            if (writeHeader)
                            {
                                writer.WriteLine(auditHeader);
                            }

                            writer.WriteLine(ToFireEventCsvLine(fireEvent) + "," + EscapeCsv(transition));
                        }
                    }
                }

                OpenCvWpfTracking.Services.Configuration.CsvExcelCompanion.Schedule(path);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("CSV AUDIT", "Automatic FIRE/SMOKE event audit CSV append failed; in-memory event remains", ex);
            }
        }

        internal static void WriteFireEventCsv(string path, IEnumerable<FireEventRecord> events)
        {
            // 수동 내보내기가 진행 중인 자동 저장과 동일 CSV를 대상으로 할 수 있다.
            lock (OpenCvWpfTracking.Services.Configuration.CsvExcelCompanion.GetCsvFileLock(path))
            {
                using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
                {
                    writer.WriteLine(GetFireEventCsvHeader());

                    foreach (FireEventRecord fireEvent in events)
                    {
                        writer.WriteLine(ToFireEventCsvLine(fireEvent));
                    }
                }
            }

            OpenCvWpfTracking.Services.Configuration.CsvExcelCompanion.Create(path);
        }

        internal static string GetFireEventCsvHeader()
        {
            return "EventId,DetectedTime,ClearedTime,Camera,DetectionType,VisionScore,ObjectCount,PixelWidth,PixelHeight,PixelArea,DetectionSource,Status";
        }

        internal static string ToFireEventCsvLine(FireEventRecord fireEvent)
        {
            List<string> fields = new List<string>
            {
                fireEvent.EventId.ToString(CultureInfo.InvariantCulture),
                EscapeCsv(ToExcelTextCell(fireEvent.DetectedTimeText)),
                EscapeCsv(ToExcelTextCell(fireEvent.ClearedTimeText)),
                EscapeCsv(fireEvent.Camera),
                EscapeCsv(fireEvent.DetectionType),
                EscapeCsv(fireEvent.Confidence),
                fireEvent.ObjectCount.ToString(CultureInfo.InvariantCulture),
                fireEvent.PixelWidth.ToString(CultureInfo.InvariantCulture),
                fireEvent.PixelHeight.ToString(CultureInfo.InvariantCulture),
                fireEvent.PixelArea.ToString("F0", CultureInfo.InvariantCulture),
                EscapeCsv(fireEvent.DetectionSource),
                EscapeCsv(fireEvent.Status)
            };

            return string.Join(",", fields);
        }

        internal static IList<FireEventRecord> ReadFireEventCsv(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            List<FireEventRecord> result = new List<FireEventRecord>();

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }

                IList<string> fields = ParseCsvLine(lines[lineIndex]);

                if (fields.Count < 12)
                {
                    continue;
                }

                if (!int.TryParse(fields[0], out int eventId) || !TryParseEventTime(fields[1], out DateTime detectedTime))
                {
                    continue;
                }

                DateTime? clearedTime = null;

                if (fields[2] != "-" && TryParseEventTime(fields[2], out DateTime parsedClearedTime))
                {
                    clearedTime = parsedClearedTime;
                }

                int.TryParse(fields[6], out int objectCount);

                int.TryParse(fields[7], out int pixelWidth);

                int.TryParse(fields[8], out int pixelHeight);

                double.TryParse(fields[9], NumberStyles.Float, CultureInfo.InvariantCulture, out double pixelArea);

                PositionSnapshot.TryParseExportFields(fields, 12, out PositionSnapshot firstPositionSnapshot);

                result.Add(new FireEventRecord(eventId, detectedTime, clearedTime, fields[3], fields[4], fields[5], objectCount, pixelWidth, pixelHeight, pixelArea, fields[10], fields[11], firstPositionSnapshot));

                if (fields.Count >= 47)
                {
                    var first = new ZoneAssessment
                    {
                        Level = fields[37],
                        Zone = fields[38],
                        Reason = fields[39],
                        Revision = fields[40],
                        Context = fields[41]
                    };

                    var current = new ZoneAssessment
                    {
                        Level = fields[42],
                        Zone = fields[43],
                        Reason = fields[44],
                        Revision = fields[45],
                        Context = fields[46]
                    };

                    if (!string.IsNullOrWhiteSpace(first.Level))
                    {
                        result[result.Count - 1].RestoreZoneAssessment(first, current);
                    }
                }
            }

            return result;
        }

        internal static void ValidateEventCsvKind(IList<FireEventRecord> records, bool ai)
        {
            if (records.Count == 0)
            {
                throw new FormatException("이벤트 데이터가 없습니다.");
            }

            bool valid = records.All(item => (string.Equals(item.DetectionType, "AI", StringComparison.OrdinalIgnoreCase) || string.Equals(item.DetectionSource, "AI AGENT", StringComparison.OrdinalIgnoreCase)) == ai && (ai || string.Equals(item.DetectionType, "FIRE", StringComparison.OrdinalIgnoreCase) || string.Equals(item.DetectionType, "SMOKE", StringComparison.OrdinalIgnoreCase)));

            if (!valid)
            {
                throw new FormatException(ai ? "AI 이벤트 CSV만 선택하세요." : "FIRE/SMOKE CSV만 선택하세요.");
            }
        }

        internal static bool TryParseEventTime(string value, out DateTime parsed)
        {
            value = NormalizeExcelTextCell(value);

            string[] supportedFormats =
            {
                "yy-MM-dd HH:mm:ss",
                "yy.MM.dd.HH.mm.ss",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-dd HH:mm:ss"
            };

            return DateTime.TryParseExact(value, supportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed) || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed);
        }

        internal static string ToExcelTextCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
            {
                return value;
            }

            return "=\"" + value.Replace("\"", "\"\"") + "\"";
        }

        internal static string NormalizeExcelTextCell(string value)
        {
            string normalized = (value ?? string.Empty).Trim();

            if (normalized.Length >= 3 && normalized.StartsWith("=\"", StringComparison.Ordinal) && normalized.EndsWith("\"", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2, normalized.Length - 3).Replace("\"\"", "\"");
            }

            return normalized.TrimStart('\'');
        }

        internal static IList<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();

            StringBuilder current = new StringBuilder();

            bool inQuotes = false;

            for (int index = 0; index < line.Length; index++)
            {
                char character = line[index];

                if (character == '"')
                {
                    if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current.Append('"');

                        index++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (character == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());

                    current.Clear();

                    continue;
                }

                current.Append(character);
            }

            fields.Add(current.ToString());

            return fields;
        }

        internal static string EscapeCsv(string value)
        {
            string safe = value ?? string.Empty;

            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }

        #endregion

    }
}
