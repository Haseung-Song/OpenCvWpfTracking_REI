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

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private int _nextAiEventId = 1;
        private bool _isAiCsvHistoryLoaded;
        private readonly Dictionary<int, FireEventRecord> _activeAiEvents =
            new Dictionary<int, FireEventRecord>();
        private DispatcherTimer _testProgramEventTimer;
        private int _processedTestProgramEventLineCount;
        private readonly List<FireEventRecord> _activeTestFireEvents =
            new List<FireEventRecord>();
        private int _activeAiCount;
        private DateTime? _lastAiDetectedTime;
        private int _selectedEventAlertTabIndex;

        public ObservableCollection<FireEventRecord> AiDetectionEvents { get; } =
            new ObservableCollection<FireEventRecord>();

        public ObservableCollection<FireEventRecord> FireDetectionEvents =>
            FireEvents;

        /// <summary>
        /// 2026-08-24: 가장 최근에 발생한 이벤트 종류의 하위 탭을 선택한다.
        /// 0은 AI DETECTION, 1은 FIRE DETECTION이다.
        /// </summary>
        public int SelectedEventAlertTabIndex
        {
            get => _selectedEventAlertTabIndex;
            set
            {
                int normalizedValue = value == 1 ? 1 : 0;
                if (_selectedEventAlertTabIndex == normalizedValue)
                {
                    return;
                }

                _selectedEventAlertTabIndex = normalizedValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EventAlertHeaderText));
                OnPropertyChanged(nameof(EventAlertHeaderBrush));
                ConsoleLogHelper.Info(
                    "EVENT UI",
                    "Latest event tab selected / INDEX=" + normalizedValue);
            }

        }

        public ICommand SaveAiEventsCommand { get; private set; }
        public ICommand LoadAiEventsCommand { get; private set; }
        public ICommand ClearAiEventsCommand { get; private set; }

        public int ActiveAiCount
        {
            get => _activeAiCount;
            private set
            {
                if (_activeAiCount == value)
                {
                    return;
                }

                _activeAiCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AiAlertStatusText));
                OnPropertyChanged(nameof(AiAlertHeaderBrush));
                OnPropertyChanged(nameof(EventAlertHeaderText));
                OnPropertyChanged(nameof(EventAlertHeaderBrush));
            }

        }

        public string LastAiEventTimeText =>
            _lastAiDetectedTime.HasValue
                ? _lastAiDetectedTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : "-";

        public string AiAlertStatusText =>
            ActiveAiCount > 0
                ? "AI OBJECT DETECTED"
                : AiPowerStatusText == "ON"
                    ? "MONITORING"
                    : "DISCONNECTED";

        /// <summary>
        /// 2026-08-25: AI 활성 이벤트는 FIRE 빨간색과 구분되는 시안색으로 강조한다.
        /// </summary>
        public Brush AiAlertHeaderBrush =>
            ActiveAiCount > 0
                ? new SolidColorBrush(Color.FromRgb(94, 231, 247))
                : new SolidColorBrush(Color.FromRgb(208, 215, 222));

        /// <summary>
        /// 2026-08-26: 가장 최근 탐지로 선택된 하위 탭의 색상을 상위 탭에 즉시 반영한다.
        /// </summary>
        public Brush EventAlertHeaderBrush =>
            SelectedEventAlertTabIndex == 1 && ActiveFireCount > 0
                ? new SolidColorBrush(Color.FromRgb(255, 82, 82))
                : SelectedEventAlertTabIndex == 0 && ActiveAiCount > 0
                    ? new SolidColorBrush(Color.FromRgb(94, 231, 247))
                    : ActiveFireCount > 0
                        ? new SolidColorBrush(Color.FromRgb(255, 82, 82))
                        : ActiveAiCount > 0
                            ? new SolidColorBrush(Color.FromRgb(94, 231, 247))
                            : new SolidColorBrush(Color.FromRgb(208, 215, 222));

        public string EventAlertHeaderText
        {
            get
            {
                // 2026-08-26: 상위 탭 숫자는 BBox 객체 수가 아니라 현재 하위 탭의 실제 이벤트 행 수다.
                int activeCount = SelectedEventAlertTabIndex == 1
                    ? ActiveFireCount
                    : ActiveAiCount;

                if (activeCount == 0)
                {
                    activeCount = ActiveFireCount > 0
                        ? ActiveFireCount
                        : ActiveAiCount;
                }

                if (activeCount == 0)
                {
                    return "이벤트 알림";
                }

                return "이벤트 알림 (" + activeCount + ")";
            }

        }

        /// <summary>
        /// AI 이벤트 명령과 테스트 프로그램 실시간 이벤트 감시를 초기화한다.
        /// </summary>
        private void InitializeEventAlertFeatures()
        {
            SaveAiEventsCommand = new RelayCommand(SaveAiEvents);
            LoadAiEventsCommand = new RelayCommand(LoadAiEvents);
            ClearAiEventsCommand = new RelayCommand(ClearAiEvents);

            string bridgePath = GetTestProgramEventBridgePath();
            try
            {
                _processedTestProgramEventLineCount =
                    File.Exists(bridgePath)
                        ? File.ReadAllLines(bridgePath).Length
                        : 0;
            }
            catch (IOException)
            {
                _processedTestProgramEventLineCount = 0;
            }

            _testProgramEventTimer =
                new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
            _testProgramEventTimer.Tick +=
                (sender, args) => PollTestProgramEvents();
            _testProgramEventTimer.Start();
        }

        /// <summary>
        /// AI Agent의 프레임 결과를 RTSP 채널별 이벤트 생명주기로 변환한다.
        /// 프레임마다 행을 추가하지 않고 탐지 시작과 해제만 기록한다.
        /// </summary>
        private void UpdateAiDetectionEvent(
            AiDetectionResult result,
            DateTime receiveTime)
        {
            List<AiDetectionBox> boxes =
                result.Boxes
                    .Where(box => box.NormalizedConfidence >= AiDisplayConfidenceThreshold)
                    .ToList();

            if (boxes.Count == 0)
            {
                if (!_activeAiEvents.TryGetValue(
                        result.RtspIndex,
                        out FireEventRecord clearedEvent))
                {
                    return;
                }

                clearedEvent.MarkCleared(receiveTime);
                _activeAiEvents.Remove(result.RtspIndex);
                ActiveAiCount = _activeAiEvents.Count;
                AppendFireEventAudit(clearedEvent, "CLEARED");
                NotifyAiEventSummaryChanged();
                ConsoleLogHelper.State(
                    "AI EVENT",
                    "AI cleared / EVENT_ID=" + clearedEvent.EventId +
                    " / CAMERA=" + clearedEvent.Camera);
                return;
            }

            if (_activeAiEvents.ContainsKey(result.RtspIndex))
            {
                // 2026-08-26: BBox 수는 행 정보에만 갱신하고 ACTIVE 알림 수는 활성 이벤트 행 수로 유지한다.
                _activeAiEvents[result.RtspIndex].UpdateObjectCount(boxes.Count);
                ActiveAiCount = _activeAiEvents.Count;
                return;
            }

            AiDetectionBox largestBox =
                boxes
                    .OrderByDescending(box => Math.Max(0, box.Width) * Math.Max(0, box.Height))
                    .First();
            double maximumConfidence = boxes.Max(box => box.NormalizedConfidence);
            string camera = result.RtspIndex == 0 ? "EO" : "IR";

            FireEventRecord aiEvent =
                new FireEventRecord(
                    _nextAiEventId++,
                    receiveTime,
                    null,
                    camera,
                    "AI",
                    (maximumConfidence * 100).ToString("F1", CultureInfo.InvariantCulture) + "%",
                    boxes.Count,
                    Math.Max(0, largestBox.Width),
                    Math.Max(0, largestBox.Height),
                    Math.Max(0, largestBox.Width) * Math.Max(0, largestBox.Height),
                    "AI AGENT",
                    "ACTIVE");

            if (_isAiCsvHistoryLoaded)
            {
                aiEvent.MarkLiveAfterCsvLoad();
            }

            _activeAiEvents[result.RtspIndex] = aiEvent;
            AiDetectionEvents.Insert(0, aiEvent);
            TrimEventCollection(AiDetectionEvents);
            _lastAiDetectedTime = receiveTime;
            ActiveAiCount = _activeAiEvents.Count;
            AppendFireEventAudit(aiEvent, "DETECTED");
            NotifyAiEventSummaryChanged();

            ConsoleLogHelper.Warning(
                "AI EVENT",
                "AI detected / EVENT_ID=" + aiEvent.EventId +
                " / CAMERA=" + camera +
                " / OBJECTS=" + aiEvent.ObjectCount +
                " / CONFIDENCE=" + aiEvent.Confidence +
                " / BBOX=" + aiEvent.PixelSizeText);
        }

        /// <summary>
        /// FireCandidateValidator가 기록한 상태 전환을 실시간으로 읽는다.
        /// </summary>
        private void PollTestProgramEvents()
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

                for (int index = _processedTestProgramEventLineCount;
                     index < lines.Length;
                     index++)
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

        /// <summary>
        /// 테스트 프로그램의 한 줄 이벤트를 FIRE 목록에 반영한다.
        /// </summary>
        private void ProcessTestProgramEventLine(string line)
        {
            string[] fields = (line ?? string.Empty).Split('|');
            if (fields.Length < 7 ||
                !DateTime.TryParse(
                    fields[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime eventTime))
            {
                return;
            }

            string transition = fields[1];
            int.TryParse(fields[2], out int objectCount);
            int.TryParse(fields[3], out int width);
            int.TryParse(fields[4], out int height);
            double.TryParse(
                fields[5],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double area);
            string sourceName = fields[6];

            if (transition == "DETECTED")
            {
                FireEventRecord testEvent =
                    new FireEventRecord(
                        _nextFireEventId++,
                        eventTime,
                        null,
                        "TEST",
                        "FIRE",
                        "N/A",
                        Math.Max(1, objectCount),
                        Math.Max(0, width),
                        Math.Max(0, height),
                        Math.Max(0, area),
                        string.IsNullOrWhiteSpace(sourceName) ? "TEST PROGRAM" : sourceName,
                        "ACTIVE");

                // 2026-08-26: FIRE CSV 복원 이후 테스트 프로그램에서 추가된 신규 행도 청록색으로 구분한다.
                if (_isFireCsvHistoryLoaded)
                {
                    testEvent.MarkLiveAfterCsvLoad();
                }

                _activeTestFireEvents.Add(testEvent);
                FireEvents.Insert(0, testEvent);
                TrimEventCollection(FireEvents);
                _lastFireDetectedTime = eventTime;
                AppendFireEventAudit(testEvent, "DETECTED");
                RefreshActiveFireCount();
                NotifyFireEventSummaryChanged();
                ConsoleLogHelper.Warning(
                    "FIRE EVENT",
                    "Test program fire detected / EVENT_ID=" + testEvent.EventId +
                    " / CSV_LIVE=" + testEvent.IsLiveAfterCsvLoad +
                    " / OBJECTS=" + testEvent.ObjectCount +
                    " / BBOX=" + testEvent.PixelSizeText +
                    " / PIXEL_AREA=" + testEvent.PixelArea.ToString("F0", CultureInfo.InvariantCulture));
                return;
            }

            if (transition == "CLEARED" && _activeTestFireEvents.Count > 0)
            {
                List<FireEventRecord> clearedEvents =
                    new List<FireEventRecord>(_activeTestFireEvents);
                _activeTestFireEvents.Clear();
                foreach (FireEventRecord clearedEvent in clearedEvents)
                {
                    clearedEvent.MarkCleared(eventTime);
                    AppendFireEventAudit(clearedEvent, "CLEARED");
                }
                RefreshActiveFireCount();
                NotifyFireEventSummaryChanged();
                ConsoleLogHelper.State(
                    "FIRE EVENT",
                    "Test program fire cleared / COUNT=" + clearedEvents.Count);
            }

        }

        /// <summary>
        /// 2026-08-26: BBox 개수가 아닌 현재 ACTIVE FIRE 이벤트 행 개수를 합산한다.
        /// </summary>
        private void RefreshActiveFireCount()
        {
            int count = 0;
            if (_activeThermalFireEvent != null)
            {
                count++;
            }

            count += _activeTestFireEvents.Count;

            ActiveFireCount = count;
            OnPropertyChanged(nameof(EventAlertHeaderText));
        }

        /// <summary>
        /// 2026-08-24: AI 이벤트 CSV 저장 결과를 로그와 알림창으로 안내한다.
        /// </summary>
        private void SaveAiEvents()
        {
            SaveFileDialog dialog =
                new SaveFileDialog
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
                WriteFireEventCsv(
                    dialog.FileName,
                    AiDetectionEvents.OrderBy(item => item.EventId));

                ConsoleLogHelper.State(
                    "AI EVENT",
                    "Event CSV saved / PATH=" + dialog.FileName +
                    " / COUNT=" + AiDetectionEvents.Count);
                MessageBox.Show(
                    "AI 이벤트 CSV 저장이 완료되었습니다.",
                    "CSV 저장 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error("AI EVENT", "Event CSV save failed", exception);
                MessageBox.Show(
                    "AI 이벤트 CSV 저장에 실패했습니다.\n" + exception.Message,
                    "CSV 저장 실패",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// 2026-08-24: AI 이벤트 CSV를 불러오며 실패한 경우에만 알림창을 표시한다.
        /// </summary>
        private void LoadAiEvents()
        {
            OpenFileDialog dialog =
                new OpenFileDialog
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
                AiDetectionEvents.Clear();
                foreach (FireEventRecord item in
                         loaded.Where(item => item.DetectionType == "AI")
                               .OrderByDescending(item => item.EventId))
                {
                    AiDetectionEvents.Add(item);
                }

                _activeAiEvents.Clear();
                _nextAiEventId =
                    AiDetectionEvents.Count == 0
                        ? 1
                        : AiDetectionEvents.Max(item => item.EventId) + 1;
                _isAiCsvHistoryLoaded = true;
                ActiveAiCount = 0;
                _lastAiDetectedTime =
                    AiDetectionEvents.Count == 0
                        ? (DateTime?)null
                        : AiDetectionEvents.Max(item => item.DetectedTime);
                NotifyAiEventSummaryChanged();
                ConsoleLogHelper.State(
                    "AI EVENT",
                    "Event CSV loaded / PATH=" + dialog.FileName +
                    " / COUNT=" + AiDetectionEvents.Count);
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error("AI EVENT", "Event CSV load failed", exception);
                MessageBox.Show(
                    "AI 이벤트 CSV 불러오기에 실패했습니다.\n" + exception.Message,
                    "CSV 불러오기 실패",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }

        private void ClearAiEvents()
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

        private void NotifyAiEventSummaryChanged()
        {
            OnPropertyChanged(nameof(LastAiEventTimeText));
            OnPropertyChanged(nameof(AiAlertStatusText));
            OnPropertyChanged(nameof(AiAlertHeaderBrush));
            OnPropertyChanged(nameof(EventAlertHeaderText));
            OnPropertyChanged(nameof(EventAlertHeaderBrush));
        }

        private static void TrimEventCollection(
            ObservableCollection<FireEventRecord> events)
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
                ConsoleLogHelper.State(
                    "EVENT RETENTION",
                    "Oldest event removed / REMOVED=" + removedCount +
                    " / RETAINED=" + events.Count);
            }

        }

        internal static string GetTestProgramEventBridgePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "OpenCvWpfTracking",
                "FireEvents",
                "TestProgramLiveEvents.txt");
        }

    }

}
