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

        List<VisionBBoxEventTrack> IEventAlertsFeatureContext._activeVisionBBoxEvents
        {
            get { return _features._fireEventsFeature._activeVisionBBoxEvents; }
        }

        bool IEventAlertsFeatureContext._isFireCsvHistoryLoaded
        {
            get { return _features._fireEventsFeature._isFireCsvHistoryLoaded; }
            set { _features._fireEventsFeature._isFireCsvHistoryLoaded = value; }
        }

        ref global::System.DateTime? IEventAlertsFeatureContext._lastFireDetectedTime
        {
            get { return ref _features._fireEventsFeature._lastFireDetectedTime; }
        }

        int IEventAlertsFeatureContext._nextFireEventId
        {
            get { return _features._fireEventsFeature._nextFireEventId; }
            set { _features._fireEventsFeature._nextFireEventId = value; }
        }

        int IEventAlertsFeatureContext.ActiveFireCount
        {
            get { return _features._fireEventsFeature.ActiveFireCount; }
            set { _features._fireEventsFeature.ActiveFireCount = value; }
        }

        double IEventAlertsFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features._propertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features._propertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        string IEventAlertsFeatureContext.AiPowerStatusText
        {
            get { return _features._propertiesFeature.AiPowerStatusText; }
            set { _features._propertiesFeature.AiPowerStatusText = value; }
        }

        Services.Position.ZoneAssessment IEventAlertsFeatureContext.AssessAiZone(AiDetectionResult result, AiDetectionBox box, string type)
        {
            return _features._zonesFeature.AssessAiZone(result, box, type);
        }

        Models.Position.PositionSnapshot IEventAlertsFeatureContext.CapturePositionSnapshot(string source, int? presetId)
        {
            return _features._positionFeature.CapturePositionSnapshot(source, presetId);
        }

        ObservableCollection<FireEventRecord> IEventAlertsFeatureContext.FireEvents
        {
            get { return _features._fireEventsFeature.FireEvents; }
        }

        void IEventAlertsFeatureContext.NotifyFireEventSummaryChanged()
        {
            _features._fireEventsFeature.NotifyFireEventSummaryChanged();
        }

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
