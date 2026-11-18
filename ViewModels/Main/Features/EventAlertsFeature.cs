using Microsoft.Win32;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>이벤트 알림 표시에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IEventAlertsFeatureContext
    {
        List<VisionBBoxEventTrack> _activeVisionBBoxEvents { get; }

        bool _isFireCsvHistoryLoaded { get; set; }

        ref global::System.DateTime? _lastFireDetectedTime { get; }

        int _nextFireEventId { get; set; }

        int ActiveFireCount { get; set; }

        double AiDisplayConfidenceThreshold { get; set; }

        string AiPowerStatusText { get; set; }

        Services.Position.ZoneAssessment AssessAiZone(AiDetectionResult result, AiDetectionBox box, string type);

        Models.Position.PositionSnapshot CapturePositionSnapshot(string source, int? presetId = null);

        ObservableCollection<FireEventRecord> FireEvents { get; }

        void NotifyFireEventSummaryChanged();

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        Action SaveAiEventsCallback { get; }

        Action LoadAiEventsCallback { get; }

        Action ClearAiEventsCallback { get; }
    }

    /// <summary>이벤트 알림 표시 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class EventAlertsFeature
    {

        #region [협력 객체와 초기화]

        private readonly IEventAlertsFeatureContext _context;

        internal EventAlertsFeature(IEventAlertsFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _nextAiEventId = 1;

            AiDetectionEvents = new ObservableCollection<FireEventRecord>();
        }

        #endregion

        #region [상태 필드]

        internal int _nextAiEventId;

        internal bool _isAiCsvHistoryLoaded;

        internal readonly Dictionary<string, AiEventTrack> _activeAiEvents = new Dictionary<string, AiEventTrack>(StringComparer.Ordinal);

        internal DispatcherTimer _testProgramEventTimer;

        internal int _processedTestProgramEventLineCount;

        internal readonly List<FireEventRecord> _activeTestFireEvents = new List<FireEventRecord>();

        internal int _activeAiCount;

        internal DateTime? _lastAiDetectedTime;

        internal int _selectedEventAlertTabIndex;

        #endregion

        #region [화면 속성과 이벤트]

        internal ObservableCollection<FireEventRecord> AiDetectionEvents { get; set; }

        internal ObservableCollection<FireEventRecord> FireDetectionEvents
        {
            get { return _context.FireEvents; }
        }

        internal int SelectedEventAlertTabIndex
        {
            get { return _selectedEventAlertTabIndex; }

            set
            {
                int normalizedValue = value == 1 ? 1 : 0;

                if (_selectedEventAlertTabIndex == normalizedValue)
                {
                    return;
                }

                _selectedEventAlertTabIndex = normalizedValue;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(EventAlertHeaderText));

                _context.OnPropertyChanged(nameof(EventAlertHeaderBrush));

                ConsoleLogHelper.Info("EVENT UI", "Latest event tab selected / INDEX=" + normalizedValue);
            }
        }

        internal ICommand SaveAiEventsCommand { get; set; }

        internal ICommand LoadAiEventsCommand { get; set; }

        internal ICommand ClearAiEventsCommand { get; set; }

        internal int ActiveAiCount
        {
            get { return _activeAiCount; }

            set
            {
                if (_activeAiCount == value)
                {
                    return;
                }

                _activeAiCount = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(AiAlertStatusText));

                _context.OnPropertyChanged(nameof(AiAlertHeaderBrush));

                _context.OnPropertyChanged(nameof(EventAlertHeaderText));

                _context.OnPropertyChanged(nameof(EventAlertHeaderBrush));
            }
        }

        internal string LastAiEventTimeText
        {
            get
            {
                if (_lastAiDetectedTime.HasValue)
                {
                    return _lastAiDetectedTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    return "-";
                }
            }
        }

        internal string AiAlertStatusText
        {
            get
            {
                if (ActiveAiCount > 0)
                {
                    return "AI OBJECT DETECTED";
                }
                else
                {
                    return _context.AiPowerStatusText == "ON" ? "MONITORING" : "DISCONNECTED";
                }
            }
        }

        internal Brush AiAlertHeaderBrush
        {
            get
            {
                return ActiveAiCount > 0 ? LegacyVideoIntegration.EventBrush("AI") : new SolidColorBrush(Color.FromRgb(208, 215, 222));
            }
        }

        internal Brush EventAlertHeaderBrush
        {
            get
            {
                if (SelectedEventAlertTabIndex == 1 && _context.ActiveFireCount > 0)
                {
                    return new SolidColorBrush(Color.FromRgb(255, 143, 163));
                }
                else
                {
                    return SelectedEventAlertTabIndex == 0 && ActiveAiCount > 0 ? new SolidColorBrush(Color.FromRgb(94, 231, 247)) : _context.ActiveFireCount > 0 ? new SolidColorBrush(Color.FromRgb(255, 143, 163)) : ActiveAiCount > 0 ? new SolidColorBrush(Color.FromRgb(94, 231, 247)) : new SolidColorBrush(Color.FromRgb(208, 215, 222));
                }
            }
        }

        internal string EventAlertHeaderText
        {
            get
            {
                // 2026-08-26: 상위 탭 숫자는 BBox 객체 수가 아니라 현재 하위 탭의 실제 이벤트 행 수다.
                int activeCount = SelectedEventAlertTabIndex == 1 ? _context.ActiveFireCount : ActiveAiCount;

                if (activeCount == 0)
                {
                    activeCount = _context.ActiveFireCount > 0 ? _context.ActiveFireCount : ActiveAiCount;
                }

                if (activeCount == 0)
                {
                    return "이벤트 알림";
                }

                return "이벤트 알림 (" + activeCount + ")";
            }
        }

        #endregion

        #region [동작 처리]

        internal void InitializeEventAlertFeatures()
        {
            SaveAiEventsCommand = new RelayCommand(_context.SaveAiEventsCallback);

            LoadAiEventsCommand = new RelayCommand(_context.LoadAiEventsCallback);

            ClearAiEventsCommand = new RelayCommand(_context.ClearAiEventsCallback);

            string bridgePath = GetTestProgramEventBridgePath();

            try
            {
                _processedTestProgramEventLineCount = File.Exists(bridgePath) ? File.ReadAllLines(bridgePath).Length : 0;
            }
            catch (IOException)
            {
                _processedTestProgramEventLineCount = 0;
            }

            _testProgramEventTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _testProgramEventTimer.Tick += (sender, args) => PollTestProgramEvents();

            _testProgramEventTimer.Start();
        }

        internal void UpdateAiDetectionEvent(AiDetectionResult result, DateTime receiveTime)
        {
            List<AiDetectionBox> boxes = result.Boxes.Where(box => box.NormalizedConfidence >= _context.AiDisplayConfidenceThreshold).ToList();

            HashSet<string> seenKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (AiDetectionBox box in boxes)
            {
                string key = GetAiEventKey(result.RtspIndex, box);

                seenKeys.Add(key);

                string resolvedDetectionType = GetAiEventDetectionType(box);

                if (_activeAiEvents.TryGetValue(key, out AiEventTrack existingTrack))
                {
                    existingTrack.LastSeen = receiveTime;

                    existingTrack.Event.UpdateObjectCount(1);

                    existingTrack.Event.UpdateDetectionType(resolvedDetectionType);

                    existingTrack.Event.UpdateAiSnapshot(box.NormalizedConfidence * 100.0, box.Width, box.Height);

                    box.DetectionEventId = existingTrack.Event.EventId;

                    existingTrack.Event.UpdateZoneAssessment(_context.AssessAiZone(result, box, resolvedDetectionType));

                    continue;
                }

                string camera = result.RtspIndex == 0 ? "EO" : "IR";

                FireEventRecord aiEvent = new FireEventRecord(_nextAiEventId++, receiveTime, null, camera, resolvedDetectionType, (box.NormalizedConfidence * 100).ToString("F1", CultureInfo.InvariantCulture) + "%", 1, Math.Max(0, box.Width), Math.Max(0, box.Height), Math.Max(0, box.Width) * (double)Math.Max(0, box.Height), "AI AGENT", "ACTIVE", _context.CapturePositionSnapshot("AI_EVENT"));

                aiEvent.UpdateZoneAssessment(_context.AssessAiZone(result, box, resolvedDetectionType));

                if (_isAiCsvHistoryLoaded)
                {
                    aiEvent.MarkLiveAfterCsvLoad();
                }

                _activeAiEvents[key] = new AiEventTrack
                {
                    RtspIndex = result.RtspIndex,
                    ObjectId = box.ObjectId,
                    ClassIndex = box.ClassIndex,
                    LastSeen = receiveTime,
                    Event = aiEvent
                };

                box.DetectionEventId = aiEvent.EventId;

                AiDetectionEvents.Insert(0, aiEvent);

                TrimEventCollection(AiDetectionEvents);

                _lastAiDetectedTime = receiveTime;

                AppendFireEventAudit(aiEvent, "DETECTED");

                ConsoleLogHelper.Warning("AI EVENT", "ACTION=CREATE / EVENT_ID=" + aiEvent.EventId + " / CAMERA=" + camera + " / OBJECT_ID=" + box.ObjectId + " / CLASS=" + resolvedDetectionType);
            }

            foreach (KeyValuePair<string, AiEventTrack> pair in _activeAiEvents.Where(item => item.Value.RtspIndex == result.RtspIndex && !seenKeys.Contains(item.Key) && (receiveTime - item.Value.LastSeen).TotalMilliseconds >= AiDisplayHoldMilliseconds).ToList())
            {
                pair.Value.Event.MarkCleared(receiveTime);

                AppendFireEventAudit(pair.Value.Event, "CLEARED");

                _activeAiEvents.Remove(pair.Key);

                ConsoleLogHelper.State("AI EVENT", "ACTION=CLEAR / EVENT_ID=" + pair.Value.Event.EventId + " / OBJECT_ID=" + pair.Value.ObjectId + " / REASON=HOLD_EXPIRED");
            }

            ActiveAiCount = _activeAiEvents.Count;

            NotifyAiEventSummaryChanged();
        }

        internal static string GetAiEventKey(int rtspIndex, AiDetectionBox box)
        {
            return rtspIndex.ToString(CultureInfo.InvariantCulture) + ":" + box.ObjectId.ToString(CultureInfo.InvariantCulture) + ":" + box.ClassIndex.ToString(CultureInfo.InvariantCulture);
        }

        internal void PollTestProgramEvents()
        {
            string bridgePath = GetTestProgramEventBridgePath();

            if (!File.Exists(bridgePath))
            {
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(bridgePath);

                if (_processedTestProgramEventLineCount > lines.Length)
                {
                    _processedTestProgramEventLineCount = 0;
                }

                for (int index = _processedTestProgramEventLineCount; index < lines.Length; index++)
                {
                    ProcessTestProgramEventLine(lines[index]);
                }

                _processedTestProgramEventLineCount = lines.Length;
            }
            catch (IOException)
            {
                // 테스트 프로그램이 같은 순간에 기록 중이면 다음 Tick에서 다시 읽는다.
            }
        }

        internal void ProcessTestProgramEventLine(string line)
        {
            string[] fields = (line ?? string.Empty).Split('|');

            if (fields.Length < 7 || !DateTime.TryParse(fields[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime eventTime))
            {
                return;
            }

            string transition = fields[1];

            int.TryParse(fields[2], out int objectCount);

            int.TryParse(fields[3], out int width);

            int.TryParse(fields[4], out int height);

            double.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double area);

            // 2026-08-31: 신규 9필드 형식은 V.SCORE와 FIRE/SMOKE 종류를 포함한다.
            // 기존 7/8필드 기록도 계속 읽어 이전 테스트 결과와 호환한다.
            double visionScore = 0.0;

            bool hasVisionScore = fields.Length >= 9 && double.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out visionScore);

            string scoreText = hasVisionScore ? visionScore.ToString("F1", CultureInfo.InvariantCulture) + "%" : "N/A";

            int detectionTypeIndex = hasVisionScore ? 7 : 6;

            int sourceNameIndex = hasVisionScore ? 8 : 7;

            string detectionType = fields.Length > detectionTypeIndex && string.Equals(fields[detectionTypeIndex], "SMOKE", StringComparison.OrdinalIgnoreCase) ? "SMOKE" : "FIRE";

            string sourceName = fields.Length > sourceNameIndex ? fields[sourceNameIndex] : fields[Math.Min(6, fields.Length - 1)];

            if (transition == "DETECTED")
            {
                FireEventRecord testEvent = new FireEventRecord(_context._nextFireEventId++, eventTime, null, "TEST", detectionType, scoreText, Math.Max(1, objectCount), Math.Max(0, width), Math.Max(0, height), Math.Max(0, area), string.IsNullOrWhiteSpace(sourceName) ? "TEST PROGRAM" : sourceName, "ACTIVE", _context.CapturePositionSnapshot("TEST_" + detectionType + "_EVENT"));

                // 2026-08-26: FIRE CSV 복원 이후 테스트 프로그램에서 추가된 신규 행도 청록색으로 구분한다.
                if (_context._isFireCsvHistoryLoaded)
                {
                    testEvent.MarkLiveAfterCsvLoad();
                }

                _activeTestFireEvents.Add(testEvent);

                _context.FireEvents.Insert(0, testEvent);

                TrimEventCollection(_context.FireEvents);

                _context._lastFireDetectedTime = eventTime;

                AppendFireEventAudit(testEvent, "DETECTED");

                RefreshActiveFireCount();

                _context.NotifyFireEventSummaryChanged();

                ConsoleLogHelper.Warning("FIRE/SMOKE EVENT", "Test program candidate detected / TYPE=" + detectionType + " / EVENT_ID=" + testEvent.EventId + " / CSV_LIVE=" + testEvent.IsLiveAfterCsvLoad + " / OBJECTS=" + testEvent.ObjectCount + " / BBOX=" + testEvent.PixelSizeText + " / V.SCORE=" + testEvent.Confidence + " / PIXEL_AREA=" + testEvent.PixelArea.ToString("F0", CultureInfo.InvariantCulture));

                return;
            }

            if (transition == "CLEARED" && _activeTestFireEvents.Count > 0)
            {
                List<FireEventRecord> clearedEvents = _activeTestFireEvents.Where(item => string.Equals(item.DetectionType, detectionType, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (FireEventRecord clearedEvent in clearedEvents)
                {
                    _activeTestFireEvents.Remove(clearedEvent);
                }

                foreach (FireEventRecord clearedEvent in clearedEvents)
                {
                    clearedEvent.MarkCleared(eventTime);

                    AppendFireEventAudit(clearedEvent, "CLEARED");
                }

                RefreshActiveFireCount();

                _context.NotifyFireEventSummaryChanged();

                ConsoleLogHelper.State("FIRE/SMOKE EVENT", "Test program candidate cleared / TYPE=" + detectionType + " / COUNT=" + clearedEvents.Count);
            }
        }

        internal void RefreshActiveFireCount()
        {
            _context.ActiveFireCount = _activeTestFireEvents.Count + _context._activeVisionBBoxEvents.Count;

            _context.OnPropertyChanged(nameof(EventAlertHeaderText));
        }

        internal void SaveAiEvents()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "AI 이벤트 CSV 저장",
                Filter = "CSV file|*.csv",
                FileName = "AiEvent_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                WriteFireEventCsv(dialog.FileName, AiDetectionEvents.OrderBy(item => item.EventId));

                ConsoleLogHelper.State("AI EVENT", "Event CSV saved / PATH=" + dialog.FileName + " / COUNT=" + AiDetectionEvents.Count);

                MessageBox.Show("AI 이벤트 CSV 저장이 완료되었습니다.", "CSV 저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error("AI EVENT", "Event CSV save failed", exception);

                MessageBox.Show("AI 이벤트 CSV 저장에 실패했습니다.\n" + exception.Message, "CSV 저장 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void LoadAiEvents()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "AI 이벤트 CSV 불러오기",
                Filter = "CSV file|*.csv|All files|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                IList<FireEventRecord> loaded = ReadFireEventCsv(dialog.FileName);

                ValidateEventCsvKind(loaded, true);

                AiDetectionEvents.Clear();

                foreach (FireEventRecord item in loaded.Where(item => string.Equals(item.DetectionSource, "AI AGENT", StringComparison.OrdinalIgnoreCase) || string.Equals(item.DetectionType, "AI", StringComparison.OrdinalIgnoreCase)).OrderByDescending(item => item.EventId).Take(MaximumEventHistoryCount))
                {
                    item.MarkImportedFromCsv();

                    AiDetectionEvents.Add(item);
                }

                _activeAiEvents.Clear();

                _nextAiEventId = AiDetectionEvents.Count == 0 ? 1 : AiDetectionEvents.Max(item => item.EventId) + 1;

                _isAiCsvHistoryLoaded = true;
                ActiveAiCount = 0;

                _lastAiDetectedTime = AiDetectionEvents.Count == 0 ? (DateTime?)null : AiDetectionEvents.Max(item => item.DetectedTime);

                NotifyAiEventSummaryChanged();

                ConsoleLogHelper.State("AI EVENT", "Event CSV loaded / PATH=" + dialog.FileName + " / COUNT=" + AiDetectionEvents.Count);

                MessageBox.Show("AI 이벤트 CSV 불러오기가 완료되었습니다.\n" + "불러온 이벤트: " + AiDetectionEvents.Count + "건", "CSV 불러오기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error("AI EVENT", "Event CSV load failed", exception);

                MessageBox.Show(exception is FormatException ? exception.Message : "CSV를 읽지 못했습니다.\n파일을 확인하세요.", "CSV 불러오기 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal static string GetAiEventDetectionType(AiDetectionBox box)
        {
            string className = (box?.ClassName ?? string.Empty).Trim();

            if (className.StartsWith("Class ", StringComparison.OrdinalIgnoreCase))
            {
                return "CLASS" + className.Substring(6).Trim();
            }

            if (string.IsNullOrWhiteSpace(className))
            {
                return "AI";
            }
            else
            {
                return className.ToUpperInvariant();
            }
        }

        internal void ClearAiEvents()
        {
            AiDetectionEvents.Clear();

            _nextAiEventId = 1;
            _isAiCsvHistoryLoaded = false;

            _activeAiEvents.Clear();

            ActiveAiCount = 0;
            _lastAiDetectedTime = null;

            NotifyAiEventSummaryChanged();

            ConsoleLogHelper.State("AI EVENT", "Event list cleared / NEXT_ID=1");
        }

        internal void NotifyAiEventSummaryChanged()
        {
            _context.OnPropertyChanged(nameof(LastAiEventTimeText));

            _context.OnPropertyChanged(nameof(AiAlertStatusText));

            _context.OnPropertyChanged(nameof(AiAlertHeaderBrush));

            _context.OnPropertyChanged(nameof(EventAlertHeaderText));

            _context.OnPropertyChanged(nameof(EventAlertHeaderBrush));
        }

        internal static void TrimEventCollection(ObservableCollection<FireEventRecord> events)
        {
            int removedCount = 0;

            while (events.Count > MaximumEventHistoryCount)
            {
                events.RemoveAt(events.Count - 1);

                removedCount++;
            }

            if (removedCount > 0)
            {
                // 2026-08-26: 100페이지 초과 시 신규 이벤트는 유지하고 삭제된 과거 이력을 기록한다.
                ConsoleLogHelper.State("EVENT RETENTION", "Oldest event removed / REMOVED=" + removedCount + " / RETAINED=" + events.Count);
            }
        }

        internal static string GetTestProgramEventBridgePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OpenCvWpfTracking", "FireEvents", "TestProgramLiveEvents.txt");
        }

        #endregion

    }
}
