using Microsoft.Win32;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CvRect = OpenCvSharp.Rect;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// DB 없이 운용하는 화재·연기 탐지 이벤트 한 건의 상태와 검출 크기를 보관한다.
    /// 동일 후보가 유지되는 동안 새 행을 추가하지 않고 DETECTED에서 CLEARED로 갱신한다.
    /// </summary>
    public sealed class FireEventRecord : INotifyPropertyChanged
    {
        private string _status;
        private DateTime? _clearedTime;
        private int _displayIndex;
        private bool _isLiveAfterCsvLoad;
        private int _objectCount;
        private string _detectionType;
        private string _confidence;
        private int _pixelWidth;
        private int _pixelHeight;
        private double _pixelArea;

        internal FireEventRecord(
            int eventId,
            DateTime detectedTime,
            DateTime? clearedTime,
            string camera,
            string detectionType,
            string confidence,
            int objectCount,
            int pixelWidth,
            int pixelHeight,
            double pixelArea,
            string detectionSource,
            string status)
        {
            EventId = eventId;
            DetectedTime = detectedTime;
            _clearedTime = clearedTime;
            Camera = camera;
            _detectionType = detectionType;
            _confidence = NormalizeConfidenceText(detectionType, confidence);
            _objectCount = objectCount;
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _pixelArea = pixelArea;
            // 2026-08-21: 협소한 이벤트 UI와 CSV에서 테스트 입력 경로를 간결하게 통일한다.
            DetectionSource = string.Equals(
                detectionSource,
                "TEST PROGRAM",
                StringComparison.OrdinalIgnoreCase)
                ? "TEST"
                : detectionSource;
            _status = status;
        }

        public int EventId { get; }

        /// <summary>
        /// 2026-08-26: CSV 복원 후 실시간으로 추가된 행을 구분한다.
        /// </summary>
        public bool IsLiveAfterCsvLoad => _isLiveAfterCsvLoad;

        /// <summary>
        /// 2026-08-26: 현재 정렬/페이지 안에서 표시하는 1~22 행 번호이다.
        /// </summary>
        public int DisplayIndex
        {
            get => _displayIndex;
            internal set
            {
                if (_displayIndex == value)
                {
                    return;
                }

                _displayIndex = value;
                OnPropertyChanged(nameof(DisplayIndex));
            }

        }

        public DateTime DetectedTime { get; }

        // 2026-08-28: 화면과 CSV에서 같은 날짜·시간 형식을 사용해
        // 날짜가 다른 이벤트도 협소한 목록에서 즉시 구분할 수 있도록 한다.
        public string DetectedTimeText => DetectedTime.ToString("yy-MM-dd HH:mm:ss");

        public string DetectedTimeShortText => DetectedTimeText;

        public DateTime? ClearedTime => _clearedTime;

        public string ClearedTimeText => _clearedTime.HasValue
            ? _clearedTime.Value.ToString("yy-MM-dd HH:mm:ss")
            : "-";

        public string Camera { get; }

        public string DetectionType => _detectionType;

        public string Confidence => _confidence;

        public int ObjectCount => _objectCount;

        public int PixelWidth => _pixelWidth;

        public int PixelHeight => _pixelHeight;

        public double PixelArea => _pixelArea;

        public string PixelSizeText => PixelWidth + " x " + PixelHeight;

        public string PixelSizeCompactText => PixelWidth + " x " + PixelHeight + " px";

        public string PixelAreaDisplayText =>
            PixelArea.ToString("F0", CultureInfo.InvariantCulture) + " px²";

        public string DetectionSource { get; }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged(nameof(Status));
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void MarkCleared(DateTime clearedTime)
        {
            _clearedTime = clearedTime;
            Status = "CLEARED";
            OnPropertyChanged(nameof(ClearedTime));
            OnPropertyChanged(nameof(ClearedTimeText));
        }

        /// <summary>
        /// 2026-08-26: 동일 ACTIVE 구간의 최신 실시간 BBox/후보 개수를 갱신한다.
        /// </summary>
        internal void UpdateObjectCount(int objectCount)
        {
            int normalizedCount = Math.Max(0, objectCount);
            if (_objectCount == normalizedCount)
            {
                return;
            }

            _objectCount = normalizedCount;
            OnPropertyChanged(nameof(ObjectCount));
        }

        // 2026-08-31: 영상처리 V.SCORE는 Track 초기 누적 평가 구간에만 갱신되고
        // 서비스에서 점수가 확정된 뒤에는 같은 값이 유지된다.
        internal void UpdateVisionScore(double score)
        {
            string value = Math.Max(0.0, Math.Min(100.0, score))
                .ToString("F1", CultureInfo.InvariantCulture) + "%";
            if (_confidence == value)
            {
                return;
            }

            _confidence = value;
            OnPropertyChanged(nameof(Confidence));
        }

        /// <summary>
        /// 2026-09-02 V17: AI 화면 BBox와 ACTIVE 이벤트 행이 서로 다른 시점의
        /// CONF/BBOX를 표시하지 않도록 대표 객체의 최신 스냅샷을 반영한다.
        /// ID와 최초 탐지 시각은 유지하며 CLEARED 뒤에는 마지막 값이 고정된다.
        /// </summary>
        internal void UpdateAiSnapshot(double confidence, int pixelWidth, int pixelHeight)
        {
            string confidenceText = Math.Max(0.0, Math.Min(100.0, confidence))
                .ToString("F1", CultureInfo.InvariantCulture) + "%";
            if (_confidence != confidenceText)
            {
                _confidence = confidenceText;
                OnPropertyChanged(nameof(Confidence));
            }

            int normalizedWidth = Math.Max(0, pixelWidth);
            int normalizedHeight = Math.Max(0, pixelHeight);
            if (_pixelWidth == normalizedWidth && _pixelHeight == normalizedHeight)
            {
                return;
            }

            _pixelWidth = normalizedWidth;
            _pixelHeight = normalizedHeight;
            _pixelArea = normalizedWidth * (double)normalizedHeight;
            OnPropertyChanged(nameof(PixelWidth));
            OnPropertyChanged(nameof(PixelHeight));
            OnPropertyChanged(nameof(PixelArea));
            OnPropertyChanged(nameof(PixelSizeText));
            OnPropertyChanged(nameof(PixelSizeCompactText));
            OnPropertyChanged(nameof(PixelAreaDisplayText));
        }

        /// <summary>
        /// 2026-08-31: AI 모델의 현재 Class Index가 실제 클래스명으로 해석되면
        /// ACTIVE 이벤트 TYPE도 같은 이름으로 즉시 갱신한다.
        /// </summary>
        internal void UpdateDetectionType(string detectionType)
        {
            if (string.IsNullOrWhiteSpace(detectionType) ||
                string.Equals(_detectionType, detectionType, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _detectionType = detectionType;
            OnPropertyChanged(nameof(DetectionType));
        }

        internal void MarkLiveAfterCsvLoad()
        {
            _isLiveAfterCsvLoad = true;
            OnPropertyChanged(nameof(IsLiveAfterCsvLoad));
        }

        /// <summary>
        /// 2026-08-26: 기존 CSV의 49600% 형식과 신규 49.6% 형식을 동일하게 복원한다.
        /// </summary>
        private static string NormalizeConfidenceText(string detectionType, string confidence)
        {
            if (!string.Equals(detectionType, "AI", StringComparison.OrdinalIgnoreCase))
            {
                return confidence;
            }

            string numericText = (confidence ?? string.Empty).Trim().TrimEnd('%');
            if (!double.TryParse(
                    numericText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double percentage))
            {
                return confidence;
            }

            if (percentage > 100.0)
            {
                percentage /= 1000.0;
            }

            return percentage.ToString("F1", CultureInfo.InvariantCulture) + "%";
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

    }

    public partial class MainViewModel
    {
        /// <summary>
        /// 2026-08-26: 이벤트 탭은 페이지당 22건, 최대 100페이지를 보존한다.
        /// 2,200건을 초과하면 신규 이벤트는 유지하고 가장 오래된 이벤트부터 제거한다.
        /// AI/FIRE 이벤트 목록에 동일한 보존 정책을 적용한다.
        /// </summary>
        private const int MaximumEventHistoryCount = 22 * 100;
        private int _nextFireEventId = 1;
        private bool _isFireCsvHistoryLoaded;
        private int _activeFireCount;
        private DateTime? _lastFireDetectedTime;
        private readonly List<VisionBBoxEventTrack> _activeVisionBBoxEvents =
            new List<VisionBBoxEventTrack>();

        private sealed class VisionBBoxEventTrack
        {
            internal string Camera { get; set; }
            internal string DetectionType { get; set; }
            internal CvRect Rectangle { get; set; }
            internal DateTime LastSeen { get; set; }
            internal bool Matched { get; set; }
            internal FireEventRecord Event { get; set; }
        }

        public ObservableCollection<FireEventRecord> FireEvents { get; } =
            new ObservableCollection<FireEventRecord>();

        public ICommand SaveFireEventsCommand { get; private set; }

        public ICommand LoadFireEventsCommand { get; private set; }

        public ICommand ClearFireEventsCommand { get; private set; }

        public int ActiveFireCount
        {
            get => _activeFireCount;
            private set
            {
                if (_activeFireCount == value)
                {
                    return;
                }

                _activeFireCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FireAlertHeaderText));
                OnPropertyChanged(nameof(FireAlertStatusText));
                OnPropertyChanged(nameof(FireAlertHeaderBrush));
                OnPropertyChanged(nameof(EventAlertHeaderText));
                OnPropertyChanged(nameof(EventAlertHeaderBrush));
            }

        }

        public int TotalFireEventCount => FireEvents.Count;

        public string LastFireEventTimeText =>
            _lastFireDetectedTime.HasValue
                ? _lastFireDetectedTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : "-";

        public string FireAlertHeaderText =>
            ActiveFireCount > 0
                ? "화재 / 연기 알림 (" + ActiveFireCount + ")"
                : "화재 / 연기 알림";

        public string FireAlertStatusText =>
            ActiveFireCount > 0
                ? "FIRE / SMOKE DETECTED"
                : !IsThermalFireDetectionEnabled && !IsSmokeDetectionEnabled
                    ? "FIRE / SMOKE OFF"
                    : "MONITORING";

        public Brush FireAlertHeaderBrush =>
            ActiveFireCount > 0
                // 2026-08-27: FIRE와 흰색 SMOKE를 함께 나타내는 밝은 코럴 색상이다.
                ? new SolidColorBrush(Color.FromRgb(255, 143, 163))
                : new SolidColorBrush(Color.FromRgb(208, 215, 222));

        /// <summary>
        /// 화재·연기 이벤트 저장·불러오기 명령을 초기화한다.
        /// </summary>
        private void InitializeFireEventFeatures()
        {
            SaveFireEventsCommand =
                new RelayCommand(
                    SaveFireEvents);

            LoadFireEventsCommand =
                new RelayCommand(
                    LoadFireEvents);

            ClearFireEventsCommand =
                new RelayCommand(
                    ClearFireEvents);

            InitializeEventAlertFeatures();
        }

        // 2026-08-31: FIRE/SMOKE는 채널 상태 한 행이 아니라 화면에 표시된 BBox별로 기록한다.
        // 위치가 이어지는 동일 BBox는 기존 행을 유지하고 새 위치의 후보만 새 행을 만든다.
        private void UpdateVisionBBoxEvents(
            string camera,
            string detectionType,
            IList<CvRect> candidates,
            IList<double> candidateScores,
            string detectionSource)
        {
            DateTime now = DateTime.Now;
            IList<CvRect> safeCandidates = candidates ?? new List<CvRect>();
            foreach (VisionBBoxEventTrack track in _activeVisionBBoxEvents.Where(
                item => item.Camera == camera && item.DetectionType == detectionType))
            {
                track.Matched = false;
            }

            for (int candidateIndex = 0; candidateIndex < safeCandidates.Count; candidateIndex++)
            {
                CvRect candidate = safeCandidates[candidateIndex];
                double visionScore = candidateScores != null && candidateIndex < candidateScores.Count
                    ? candidateScores[candidateIndex]
                    : 0.0;
                VisionBBoxEventTrack matched = _activeVisionBBoxEvents
                    .Where(item => !item.Matched && item.Camera == camera && item.DetectionType == detectionType)
                    .OrderByDescending(item => VisionBBoxMatchRatio(item.Rectangle, candidate))
                    .FirstOrDefault(item => VisionBBoxMatchRatio(item.Rectangle, candidate) >= 0.25);

                if (matched != null)
                {
                    matched.Rectangle = candidate;
                    matched.LastSeen = now;
                    matched.Matched = true;
                    matched.Event.UpdateVisionScore(visionScore);
                    continue;
                }

                FireEventRecord record = new FireEventRecord(
                    _nextFireEventId++, now, null, camera, detectionType,
                    visionScore.ToString("F1", CultureInfo.InvariantCulture) + "%",
                    1, candidate.Width, candidate.Height,
                    candidate.Width * (double)candidate.Height,
                    detectionSource, "ACTIVE");
                if (_isFireCsvHistoryLoaded)
                {
                    record.MarkLiveAfterCsvLoad();
                }

                _activeVisionBBoxEvents.Add(new VisionBBoxEventTrack
                {
                    Camera = camera,
                    DetectionType = detectionType,
                    Rectangle = candidate,
                    LastSeen = now,
                    Matched = true,
                    Event = record
                });
                FireEvents.Insert(0, record);
                TrimEventCollection(FireEvents);
                _lastFireDetectedTime = now;
                AppendFireEventAudit(record, "DETECTED");
                ConsoleLogHelper.Warning("VISION BBOX EVENT",
                    "BBox registered / EVENT_ID=" + record.EventId +
                    " / CAMERA=" + camera + " / TYPE=" + detectionType +
                    " / SCORE=" + record.Confidence +
                    " / BBOX=" + record.PixelSizeCompactText);
            }

            foreach (VisionBBoxEventTrack expired in _activeVisionBBoxEvents
                .Where(item => item.Camera == camera && item.DetectionType == detectionType &&
                               !item.Matched && (now - item.LastSeen).TotalSeconds >= 1.0)
                .ToList())
            {
                expired.Event.MarkCleared(now);
                AppendFireEventAudit(expired.Event, "CLEARED");
                _activeVisionBBoxEvents.Remove(expired);
                ConsoleLogHelper.State("VISION BBOX EVENT",
                    "BBox cleared / EVENT_ID=" + expired.Event.EventId +
                    " / CAMERA=" + camera + " / TYPE=" + detectionType);
            }

            RefreshActiveFireCount();
            NotifyFireEventSummaryChanged();
        }

        private static double VisionBBoxMatchRatio(CvRect left, CvRect right)
        {
            CvRect intersection = left & right;
            double intersectionArea = Math.Max(0, intersection.Width) * Math.Max(0, intersection.Height);
            double smallerArea = Math.Max(1.0, Math.Min(
                left.Width * (double)left.Height, right.Width * (double)right.Height));
            double overlap = intersectionArea / smallerArea;
            double deltaX = (left.X + left.Width / 2.0) - (right.X + right.Width / 2.0);
            double deltaY = (left.Y + left.Height / 2.0) - (right.Y + right.Height / 2.0);
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            double allowedDistance = Math.Max(20.0,
                Math.Max(Math.Max(left.Width, left.Height), Math.Max(right.Width, right.Height)) * 0.65);
            return overlap >= 0.25 || distance > allowedDistance
                ? overlap
                : 0.25 + (1.0 - distance / allowedDistance) * 0.20;
        }

        /// <summary>
        /// 알림 탭의 집계 Binding을 한 번에 갱신한다.
        /// </summary>
        private void NotifyFireEventSummaryChanged()
        {
            OnPropertyChanged(nameof(TotalFireEventCount));
            OnPropertyChanged(nameof(LastFireEventTimeText));
            OnPropertyChanged(nameof(FireAlertHeaderText));
            OnPropertyChanged(nameof(FireAlertStatusText));
            OnPropertyChanged(nameof(FireAlertHeaderBrush));
            OnPropertyChanged(nameof(EventAlertHeaderText));
            OnPropertyChanged(nameof(EventAlertHeaderBrush));
        }

        /// <summary>
        /// 현재 이벤트 목록을 Excel에서 바로 열 수 있는 UTF-8 CSV로 저장한다.
        /// </summary>
        private void SaveFireEvents()
        {
            SaveFileDialog dialog =
                new SaveFileDialog
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
                WriteFireEventCsv(
                    dialog.FileName,
                    FireEvents.OrderBy(item => item.EventId));

                ConsoleLogHelper.State(
                    "FIRE EVENT",
                    "Event CSV saved / PATH=" + dialog.FileName +
                    " / COUNT=" + FireEvents.Count);

                // 2026-08-27: 통합된 화재·연기 이벤트 명칭으로 저장 결과를 안내한다.
                MessageBox.Show(
                    "화재 / 연기 이벤트 CSV 저장이 완료되었습니다.",
                    "CSV 저장 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "FIRE / SMOKE EVENT",
                    "Event CSV save failed",
                    ex);

                MessageBox.Show(
                    "화재 / 연기 이벤트 CSV 저장에 실패했습니다.\n" + ex.Message,
                    "FIRE / SMOKE EVENT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// 이전에 저장한 화재·연기 이벤트 CSV를 현재 목록으로 불러온다.
        /// </summary>
        private void LoadFireEvents()
        {
            OpenFileDialog dialog =
                new OpenFileDialog
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
                IList<FireEventRecord> loaded =
                    ReadFireEventCsv(dialog.FileName);

                FireEvents.Clear();

                foreach (FireEventRecord fireEvent in
                         loaded.Where(item =>
                             string.Equals(item.DetectionType, "FIRE", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(item.DetectionType, "SMOKE", StringComparison.OrdinalIgnoreCase))
                               .OrderByDescending(item => item.EventId))
                {
                    FireEvents.Add(fireEvent);
                }

                _nextFireEventId =
                    FireEvents.Count == 0
                        ? 1
                        : FireEvents.Max(item => item.EventId) + 1;
                _isFireCsvHistoryLoaded = true;

                _activeTestFireEvents.Clear();
                _activeVisionBBoxEvents.Clear();
                ActiveFireCount = 0;
                _lastFireDetectedTime =
                    FireEvents.Count == 0
                        ? (DateTime?)null
                        : FireEvents.Max(item => item.DetectedTime);

                NotifyFireEventSummaryChanged();

                ConsoleLogHelper.State(
                    "FIRE / SMOKE EVENT",
                    "Event CSV loaded / PATH=" + dialog.FileName +
                    " / COUNT=" + FireEvents.Count);

                // 2026-08-27: 저장과 동일하게 불러오기 결과와 건수를 즉시 안내한다.
                MessageBox.Show(
                    "화재 / 연기 이벤트 CSV 불러오기가 완료되었습니다.\n" +
                    "불러온 이벤트: " + FireEvents.Count + "건",
                    "CSV 불러오기 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "FIRE / SMOKE EVENT",
                    "Event CSV load failed",
                    ex);

                MessageBox.Show(
                    "화재 / 연기 이벤트 CSV 불러오기에 실패했습니다.\n" + ex.Message,
                    "FIRE / SMOKE EVENT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// 화면의 이벤트 목록만 초기화한다. 자동 감사 CSV는 보존한다.
        /// </summary>
        private void ClearFireEvents()
        {
            FireEvents.Clear();
            _nextFireEventId = 1;
            _isFireCsvHistoryLoaded = false;
            _activeTestFireEvents.Clear();
            _activeVisionBBoxEvents.Clear();
            RefreshActiveFireCount();
            _lastFireDetectedTime = null;
            NotifyFireEventSummaryChanged();

            ConsoleLogHelper.State(
                "FIRE EVENT",
                "Event list cleared / NEXT_ID=1");
        }

        /// <summary>
        /// 상태 전환을 문서 폴더의 날짜별 감사 CSV에도 즉시 추가한다.
        /// </summary>
        private static void AppendFireEventAudit(
            FireEventRecord fireEvent,
            string transition)
        {
            try
            {
                string directory =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "OpenCvWpfTracking",
                        "FireEvents");

                Directory.CreateDirectory(directory);

                string path =
                    Path.Combine(
                        directory,
                        "FireEvent_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");

                bool writeHeader =
                    !File.Exists(path);

                using (StreamWriter writer =
                       new StreamWriter(
                           path,
                           true,
                           new UTF8Encoding(true)))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine(GetFireEventCsvHeader() + ",Transition");
                    }

                    writer.WriteLine(
                        ToFireEventCsvLine(fireEvent) +
                        "," + EscapeCsv(transition));
                }

            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "FIRE EVENT",
                    "Automatic event audit append failed",
                    ex);
            }

        }

        private static void WriteFireEventCsv(
            string path,
            IEnumerable<FireEventRecord> events)
        {
            using (StreamWriter writer =
                   new StreamWriter(
                       path,
                       false,
                       new UTF8Encoding(true)))
            {
                writer.WriteLine(GetFireEventCsvHeader());

                foreach (FireEventRecord fireEvent in events)
                {
                    writer.WriteLine(
                        ToFireEventCsvLine(fireEvent));
                }

            }

        }

        private static string GetFireEventCsvHeader()
        {
            return "EventId,DetectedTime,ClearedTime,Camera,DetectionType,VisionScore,ObjectCount,PixelWidth,PixelHeight,PixelArea,DetectionSource,Status";
        }

        private static string ToFireEventCsvLine(
            FireEventRecord fireEvent)
        {
            return string.Join(
                ",",
                new[]
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
                });
        }

        private static IList<FireEventRecord> ReadFireEventCsv(
            string path)
        {
            string[] lines =
                File.ReadAllLines(
                    path,
                    Encoding.UTF8);

            List<FireEventRecord> result =
                new List<FireEventRecord>();

            for (int lineIndex = 1;
                 lineIndex < lines.Length;
                 lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }

                IList<string> fields =
                    ParseCsvLine(lines[lineIndex]);

                if (fields.Count < 12)
                {
                    continue;
                }

                if (!int.TryParse(fields[0], out int eventId) ||
                    !TryParseEventTime(fields[1], out DateTime detectedTime))
                {
                    continue;
                }

                DateTime? clearedTime = null;
                if (fields[2] != "-" &&
                    TryParseEventTime(fields[2], out DateTime parsedClearedTime))
                {
                    clearedTime = parsedClearedTime;
                }

                int.TryParse(fields[6], out int objectCount);
                int.TryParse(fields[7], out int pixelWidth);
                int.TryParse(fields[8], out int pixelHeight);
                double.TryParse(
                    fields[9],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double pixelArea);

                result.Add(
                    new FireEventRecord(
                        eventId,
                        detectedTime,
                        clearedTime,
                        fields[3],
                        fields[4],
                        fields[5],
                        objectCount,
                        pixelWidth,
                        pixelHeight,
                        pixelArea,
                        fields[10],
                        fields[11]));
            }

            return result;
        }

        /// <summary>
        /// 2026-08-28: 신규 날짜·시간 형식과 기존 밀리초 형식을 모두 복원한다.
        /// 기존 V9 이전 CSV를 계속 불러올 수 있도록 하위 호환성을 유지한다.
        /// </summary>
        private static bool TryParseEventTime(string value, out DateTime parsed)
        {
            value = NormalizeExcelTextCell(value);

            string[] supportedFormats =
            {
                "yy-MM-dd HH:mm:ss",
                "yy.MM.dd.HH.mm.ss",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-dd HH:mm:ss"
            };

            return DateTime.TryParseExact(
                       value,
                       supportedFormats,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.AssumeLocal,
                       out parsed) ||
                   DateTime.TryParse(
                       value,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.AssumeLocal,
                   out parsed);
        }

        /// <summary>
        /// [2026-08-24] Excel이 이벤트 시각을 임의의 분:초 형식으로 바꾸지 않도록 텍스트 셀로 저장한다.
        /// </summary>
        private static string ToExcelTextCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
            {
                return value;
            }

            return "=\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// [2026-08-24] Excel 텍스트 수식 및 기존 CSV 문자열을 동일한 시각 파서로 복원한다.
        /// </summary>
        private static string NormalizeExcelTextCell(string value)
        {
            string normalized = (value ?? string.Empty).Trim();

            if (normalized.Length >= 3 &&
                normalized.StartsWith("=\"", StringComparison.Ordinal) &&
                normalized.EndsWith("\"", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2, normalized.Length - 3)
                                       .Replace("\"\"", "\"");
            }

            return normalized.TrimStart('\'');
        }

        private static IList<string> ParseCsvLine(
            string line)
        {
            List<string> fields =
                new List<string>();

            StringBuilder current =
                new StringBuilder();

            bool inQuotes = false;

            for (int index = 0;
                 index < line.Length;
                 index++)
            {
                char character = line[index];

                if (character == '"')
                {
                    if (inQuotes &&
                        index + 1 < line.Length &&
                        line[index + 1] == '"')
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

        private static string EscapeCsv(
            string value)
        {
            string safe =
                value ?? string.Empty;

            return "\"" +
                   safe.Replace("\"", "\"\"") +
                   "\"";
        }

    }

}
