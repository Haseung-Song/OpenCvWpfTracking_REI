using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Models.Position;

using OpenCvWpfTracking.Services.Position;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
        public Brush EventAccentBrush
        {
            get
            {
                return LegacyVideoIntegration.EventBrush(DetectionType);
            }
        }

        public Brush EventActiveBackground
        {
            get
            {
                return LegacyVideoIntegration.EventBackground(DetectionType);
            }
        }

        private string _status;

        private DateTime? _clearedTime;

        private int _displayIndex;

        private bool _isLiveAfterCsvLoad;

        private bool _isImportedFromCsv;

        public bool IsImportedFromCsv
        {
            get { return _isImportedFromCsv; }
        }

        internal void MarkImportedFromCsv()
        {
            _isImportedFromCsv = true;

            OnPropertyChanged(nameof(IsImportedFromCsv));
        }

        private int _objectCount;

        private string _detectionType;

        private string _confidence;

        private int _pixelWidth;

        private int _pixelHeight;

        private double _pixelArea;

        internal FireEventRecord(int eventId, DateTime detectedTime, DateTime? clearedTime, string camera, string detectionType, string confidence, int objectCount, int pixelWidth, int pixelHeight, double pixelArea, string detectionSource, string status, PositionSnapshot firstPositionSnapshot = null)
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
            DetectionSource = string.Equals(detectionSource, "TEST PROGRAM", StringComparison.OrdinalIgnoreCase) ? "TEST" : detectionSource;

            _status = status;
            FirstPositionSnapshot = firstPositionSnapshot;
        }

        public int EventId { get; }

        /// <summary>
        /// 2026-08-26: CSV 복원 후 실시간으로 추가된 행을 구분한다.
        /// </summary>
        public bool IsLiveAfterCsvLoad
        {
            get { return _isLiveAfterCsvLoad; }
        }

        /// <summary>
        /// 2026-08-26: 현재 정렬/페이지 안에서 표시하는 1~22 행 번호이다.
        /// </summary>
        public int DisplayIndex
        {
            get { return _displayIndex; }

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
        public string DetectedTimeText
        {
            get
            {
                return DetectedTime.ToString("yy-MM-dd HH:mm:ss");
            }
        }

        public string DetectedTimeShortText
        {
            get { return DetectedTimeText; }
        }

        public DateTime? ClearedTime
        {
            get { return _clearedTime; }
        }

        public string ClearedTimeText
        {
            get
            {
                if (_clearedTime.HasValue)
                {
                    return _clearedTime.Value.ToString("yy-MM-dd HH:mm:ss");
                }
                else
                {
                    return "-";
                }
            }
        }

        public string Camera { get; }

        public string DetectionType
        {
            get { return _detectionType; }
        }

        public string Confidence
        {
            get { return _confidence; }
        }

        public int ObjectCount
        {
            get { return _objectCount; }
        }

        public int PixelWidth
        {
            get { return _pixelWidth; }
        }

        public int PixelHeight
        {
            get { return _pixelHeight; }
        }

        public double PixelArea
        {
            get { return _pixelArea; }
        }

        public string PixelSizeText
        {
            get
            {
                return PixelWidth + " x " + PixelHeight;
            }
        }

        public string PixelSizeCompactText
        {
            get
            {
                return PixelWidth + " x " + PixelHeight + " px";
            }
        }

        public string PixelAreaDisplayText
        {
            get
            {
                return PixelArea.ToString("F0", CultureInfo.InvariantCulture) + " px²";
            }
        }

        public string DetectionSource { get; }

        /// <summary>
        /// 이벤트 최초 생성 순간의 위치 상태. ACTIVE 갱신과 카메라 이동으로 변경하지 않는다.
        /// </summary>
        public PositionSnapshot FirstPositionSnapshot { get; }

        public ZoneAssessment FirstZoneAssessment { get; private set; }

        public ZoneAssessment CurrentZoneAssessment { get; private set; }

        public string ZoneDetailText
        {
            get
            {
                return "위치 구역 판정 (안전 판정 아님)\n최초: " + (FirstZoneAssessment?.Detail ?? "미확인 / 기존 기록 또는 TEST 입력") + "\n\n현재: " + (CurrentZoneAssessment?.Detail ?? "미확인") + "\n설정 버전: " + (CurrentZoneAssessment?.Revision ?? "--");
            }
        }

        internal void UpdateZoneAssessment(ZoneAssessment assessment)
        {
            if (assessment == null)
            {
                return;
            }

            if (FirstZoneAssessment != null && CurrentZoneAssessment != null && CurrentZoneAssessment.Level == assessment.Level && CurrentZoneAssessment.Zone == assessment.Zone && CurrentZoneAssessment.Reason == assessment.Reason && CurrentZoneAssessment.Revision == assessment.Revision && CurrentZoneAssessment.Context == assessment.Context)
            {
                return;
            }

            if (FirstZoneAssessment == null)
            {
                FirstZoneAssessment = assessment;
            }

            CurrentZoneAssessment = assessment;

            OnPropertyChanged(nameof(ZoneDetailText));
        }

        internal void RestoreZoneAssessment(ZoneAssessment first, ZoneAssessment current)
        {
            FirstZoneAssessment = first;
            CurrentZoneAssessment = current;
        }

        public string Status
        {
            get { return _status; }

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
            string value = Math.Max(0.0, Math.Min(100.0, score)).ToString("F1", CultureInfo.InvariantCulture) + "%";

            if (_confidence == value)
            {
                return;
            }

            _confidence = value;

            OnPropertyChanged(nameof(Confidence));
        }

        internal void UpdateVisionSnapshot(double score, int pixelWidth, int pixelHeight)
        {
            UpdateVisionScore(score);

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
        /// 2026-09-02: AI 화면 BBox와 ACTIVE 이벤트 행이 서로 다른 시점의
        /// CONF/BBOX를 표시하지 않도록 대표 객체의 최신 스냅샷을 반영한다.
        /// ID와 최초 탐지 시각은 유지하며 CLEARED 뒤에는 마지막 값이 고정된다.
        /// </summary>
        internal void UpdateAiSnapshot(double confidence, int pixelWidth, int pixelHeight)
        {
            string confidenceText = Math.Max(0.0, Math.Min(100.0, confidence)).ToString("F1", CultureInfo.InvariantCulture) + "%";

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
            if (string.IsNullOrWhiteSpace(detectionType) || string.Equals(_detectionType, detectionType, StringComparison.OrdinalIgnoreCase))
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

            if (!double.TryParse(numericText, NumberStyles.Float, CultureInfo.InvariantCulture, out double percentage))
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainViewModel
    {
        internal const int MaximumEventHistoryCount = 9999;

        internal sealed class VisionBBoxEventTrack
        {
            internal long TrackId { get; set; }

            internal string Camera { get; set; }

            internal string DetectionType { get; set; }

            internal CvRect Rectangle { get; set; }

            internal DateTime LastSeen { get; set; }

            internal bool Matched { get; set; }

            internal FireEventRecord Event { get; set; }
        }

        public ObservableCollection<FireEventRecord> FireEvents
        {
            get { return _fireEventsFeature.FireEvents; }
            private set { _fireEventsFeature.FireEvents = value; }
        }

        public ICommand SaveFireEventsCommand
        {
            get { return _fireEventsFeature.SaveFireEventsCommand; }
            private set { _fireEventsFeature.SaveFireEventsCommand = value; }
        }

        public ICommand LoadFireEventsCommand
        {
            get { return _fireEventsFeature.LoadFireEventsCommand; }
            private set { _fireEventsFeature.LoadFireEventsCommand = value; }
        }

        public ICommand ClearFireEventsCommand
        {
            get { return _fireEventsFeature.ClearFireEventsCommand; }
            private set { _fireEventsFeature.ClearFireEventsCommand = value; }
        }

        public int ActiveFireCount
        {
            get { return _fireEventsFeature.ActiveFireCount; }
            private set { _fireEventsFeature.ActiveFireCount = value; }
        }

        public int TotalFireEventCount
        {
            get { return _fireEventsFeature.TotalFireEventCount; }
        }

        public string LastFireEventTimeText
        {
            get { return _fireEventsFeature.LastFireEventTimeText; }
        }

        public string FireAlertHeaderText
        {
            get { return _fireEventsFeature.FireAlertHeaderText; }
        }

        public string FireAlertStatusText
        {
            get { return _fireEventsFeature.FireAlertStatusText; }
        }

        public Brush FireAlertHeaderBrush
        {
            get { return _fireEventsFeature.FireAlertHeaderBrush; }
        }

        /// <summary>
        /// 화재·연기 이벤트 저장·불러오기 명령을 초기화한다.
        /// </summary>
        private void InitializeFireEventFeatures()
        {
            _fireEventsFeature.InitializeFireEventFeatures();
        }

        internal static void SuppressLocalBoxesOverlappingAi(ObservableCollection<VisionDetectionBox> localBoxes, ObservableCollection<AiDetectionBox> aiBoxes)
        {
            FireEventsFeature.SuppressLocalBoxesOverlappingAi(localBoxes, aiBoxes);
        }

        /// <summary>
        /// 현재 이벤트 목록을 Excel에서 바로 열 수 있는 UTF-8 CSV로 저장한다.
        /// </summary>
        private void SaveFireEvents()
        {
            _fireEventsFeature.SaveFireEvents();
        }

        /// <summary>
        /// 이전에 저장한 화재·연기 이벤트 CSV를 현재 목록으로 불러온다.
        /// </summary>
        private void LoadFireEvents()
        {
            _fireEventsFeature.LoadFireEvents();
        }

        /// <summary>
        /// 화면의 이벤트 목록만 초기화한다. 자동 감사 CSV는 보존한다.
        /// </summary>
        private void ClearFireEvents()
        {
            _fireEventsFeature.ClearFireEvents();
        }

        internal static void AppendFireEventAudit(FireEventRecord fireEvent, string transition)
        {
            FireEventsFeature.AppendFireEventAudit(fireEvent, transition);
        }

        internal static void WriteFireEventCsv(string path, IEnumerable<FireEventRecord> events)
        {
            FireEventsFeature.WriteFireEventCsv(path, events);
        }

        internal static IList<FireEventRecord> ReadFireEventCsv(string path)
        {
            return FireEventsFeature.ReadFireEventCsv(path);
        }

        internal static void ValidateEventCsvKind(IList<FireEventRecord> records, bool ai)
        {
            FireEventsFeature.ValidateEventCsvKind(records, ai);
        }

    }
}
