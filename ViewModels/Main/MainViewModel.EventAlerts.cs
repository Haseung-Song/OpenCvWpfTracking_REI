using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        internal sealed class AiEventTrack
        {
            internal int RtspIndex { get; set; }

            internal long ObjectId { get; set; }

            internal int ClassIndex { get; set; }

            internal DateTime LastSeen { get; set; }

            internal FireEventRecord Event { get; set; }
        }

        public ObservableCollection<FireEventRecord> AiDetectionEvents
        {
            get { return _eventAlertsFeature.AiDetectionEvents; }
            private set { _eventAlertsFeature.AiDetectionEvents = value; }
        }

        public ObservableCollection<FireEventRecord> FireDetectionEvents
        {
            get { return _eventAlertsFeature.FireDetectionEvents; }
        }

        /// <summary>
        /// 2026-08-24: 가장 최근에 발생한 이벤트 종류의 하위 탭을 선택한다.
        /// 0은 AI DETECTION, 1은 FIRE DETECTION이다.
        /// </summary>
        public int SelectedEventAlertTabIndex
        {
            get { return _eventAlertsFeature.SelectedEventAlertTabIndex; }
            set { _eventAlertsFeature.SelectedEventAlertTabIndex = value; }
        }

        public ICommand SaveAiEventsCommand
        {
            get { return _eventAlertsFeature.SaveAiEventsCommand; }
            private set { _eventAlertsFeature.SaveAiEventsCommand = value; }
        }

        public ICommand LoadAiEventsCommand
        {
            get { return _eventAlertsFeature.LoadAiEventsCommand; }
            private set { _eventAlertsFeature.LoadAiEventsCommand = value; }
        }

        public ICommand ClearAiEventsCommand
        {
            get { return _eventAlertsFeature.ClearAiEventsCommand; }
            private set { _eventAlertsFeature.ClearAiEventsCommand = value; }
        }

        public int ActiveAiCount
        {
            get { return _eventAlertsFeature.ActiveAiCount; }
            private set { _eventAlertsFeature.ActiveAiCount = value; }
        }

        public string LastAiEventTimeText
        {
            get { return _eventAlertsFeature.LastAiEventTimeText; }
        }

        public string AiAlertStatusText
        {
            get { return _eventAlertsFeature.AiAlertStatusText; }
        }

        /// <summary>
        /// 2026-08-25: AI 활성 이벤트는 FIRE 빨간색과 구분되는 시안색으로 강조한다.
        /// </summary>
        public Brush AiAlertHeaderBrush
        {
            get { return _eventAlertsFeature.AiAlertHeaderBrush; }
        }

        /// <summary>
        /// 2026-08-26: 가장 최근 탐지로 선택된 하위 탭의 색상을 상위 탭에 즉시 반영한다.
        /// </summary>
        public Brush EventAlertHeaderBrush
        {
            get { return _eventAlertsFeature.EventAlertHeaderBrush; }
        }

        public string EventAlertHeaderText
        {
            get { return _eventAlertsFeature.EventAlertHeaderText; }
        }

        internal static string GetAiEventKey(int rtspIndex, AiDetectionBox box)
        {
            return EventAlertsFeature.GetAiEventKey(rtspIndex, box);
        }

        /// <summary>
        /// 2026-08-24: AI 이벤트 CSV 저장 결과를 로그와 알림창으로 안내한다.
        /// </summary>
        private void SaveAiEvents()
        {
            _eventAlertsFeature.SaveAiEvents();
        }

        /// <summary>
        /// FIRE/SMOKE CSV와 동일하게 AI CSV 불러오기 성공 건수도 즉시 안내한다.
        /// </summary>
        private void LoadAiEvents()
        {
            _eventAlertsFeature.LoadAiEvents();
        }

        private void ClearAiEvents()
        {
            _eventAlertsFeature.ClearAiEvents();
        }

        internal static void TrimEventCollection(ObservableCollection<FireEventRecord> events)
        {
            EventAlertsFeature.TrimEventCollection(events);
        }

    }
}
