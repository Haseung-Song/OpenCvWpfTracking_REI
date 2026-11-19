using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class EventAlertsFeatureContext : IEventAlertsFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal EventAlertsFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        List<VisionBBoxEventTrack> IEventAlertsFeatureContext.ActiveVisionBBoxEvents
        {
            get { return _features.FireEventsFeature._activeVisionBBoxEvents; }
        }

        bool IEventAlertsFeatureContext.IsFireCsvHistoryLoaded
        {
            get { return _features.FireEventsFeature._isFireCsvHistoryLoaded; }
            set { _features.FireEventsFeature._isFireCsvHistoryLoaded = value; }
        }

        ref global::System.DateTime? IEventAlertsFeatureContext.LastFireDetectedTime
        {
            get { return ref _features.FireEventsFeature._lastFireDetectedTime; }
        }

        int IEventAlertsFeatureContext.NextFireEventId
        {
            get { return _features.FireEventsFeature._nextFireEventId; }
            set { _features.FireEventsFeature._nextFireEventId = value; }
        }

        int IEventAlertsFeatureContext.ActiveFireCount
        {
            get { return _features.FireEventsFeature.ActiveFireCount; }
            set { _features.FireEventsFeature.ActiveFireCount = value; }
        }

        double IEventAlertsFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features.PropertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features.PropertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        string IEventAlertsFeatureContext.AiPowerStatusText
        {
            get { return _features.PropertiesFeature.AiPowerStatusText; }
            set { _features.PropertiesFeature.AiPowerStatusText = value; }
        }

        /// <summary>
        /// AI 탐지 사각형과 현재 위치를 이용해 감시·제외 구역 포함 여부를 판정한다.
        /// </summary>
        Services.Position.ZoneAssessment IEventAlertsFeatureContext.AssessAiZone(AiDetectionResult result, AiDetectionBox box, string type)
        {
            return _features.ZonesFeature.AssessAiZone(result, box, type);
        }

        /// <summary>
        /// 현재 위치·센서·렌즈 상태를 일관된 스냅샷으로 취득한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Models.Position.PositionSnapshot IEventAlertsFeatureContext.CapturePositionSnapshot(string source, int? presetId)
        {
            return _features.PositionFeature.CapturePositionSnapshot(source, presetId);
        }

        ObservableCollection<FireEventRecord> IEventAlertsFeatureContext.FireEvents
        {
            get { return _features.FireEventsFeature.FireEvents; }
        }

        /// <summary>
        /// 알림 탭의 집계 Binding을 한 번에 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IEventAlertsFeatureContext.NotifyFireEventSummaryChanged()
        {
            _features.FireEventsFeature.NotifyFireEventSummaryChanged();
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IEventAlertsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        Action IEventAlertsFeatureContext.SaveAiEventsCallback
        {
            get { return _features.Callbacks.SaveAiEventsCallback; }
        }

        Action IEventAlertsFeatureContext.LoadAiEventsCallback
        {
            get { return _features.Callbacks.LoadAiEventsCallback; }
        }

        Action IEventAlertsFeatureContext.ClearAiEventsCallback
        {
            get { return _features.Callbacks.ClearAiEventsCallback; }
        }
    }
}
