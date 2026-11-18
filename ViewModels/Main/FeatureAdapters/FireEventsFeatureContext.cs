using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Models.Position;
using OpenCvWpfTracking.Services.Position;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class FireEventsFeatureContext : IFireEventsFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal FireEventsFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        List<FireEventRecord> IFireEventsFeatureContext._activeTestFireEvents
        {
            get { return _features._eventAlertsFeature._activeTestFireEvents; }
        }

        bool IFireEventsFeatureContext._hasEoLocalFireDetection
        {
            get { return _features._propertiesFeature._hasEoLocalFireDetection; }
            set { _features._propertiesFeature._hasEoLocalFireDetection = value; }
        }

        bool IFireEventsFeatureContext._hasEoLocalSmokeDetection
        {
            get { return _features._propertiesFeature._hasEoLocalSmokeDetection; }
            set { _features._propertiesFeature._hasEoLocalSmokeDetection = value; }
        }

        bool IFireEventsFeatureContext._hasIrLocalFireDetection
        {
            get { return _features._propertiesFeature._hasIrLocalFireDetection; }
            set { _features._propertiesFeature._hasIrLocalFireDetection = value; }
        }

        bool IFireEventsFeatureContext._hasIrLocalSmokeDetection
        {
            get { return _features._propertiesFeature._hasIrLocalSmokeDetection; }
            set { _features._propertiesFeature._hasIrLocalSmokeDetection = value; }
        }

        bool IFireEventsFeatureContext._isEoFireCandidateDetected
        {
            get { return _features._thermalFeature._isEoFireCandidateDetected; }
            set { _features._thermalFeature._isEoFireCandidateDetected = value; }
        }

        bool IFireEventsFeatureContext._isIrFireCandidateDetected
        {
            get { return _features._thermalFeature._isIrFireCandidateDetected; }
            set { _features._thermalFeature._isIrFireCandidateDetected = value; }
        }

        ZoneAssessment IFireEventsFeatureContext.AssessZone(string camera, string type, double left, double top, double right, double bottom, ZoneFrameContext f)
        {
            return _features._zonesFeature.AssessZone(camera, type, left, top, right, bottom, f);
        }

        PositionSnapshot IFireEventsFeatureContext.CapturePositionSnapshot(string source, int? presetId)
        {
            return _features._positionFeature.CapturePositionSnapshot(source, presetId);
        }

        ObservableCollection<AiDetectionBox> IFireEventsFeatureContext.EoDetectionBoxes
        {
            get { return _features._propertiesFeature.EoDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IFireEventsFeatureContext.EoFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.EoFireSmokeDetectionBoxes; }
        }

        int IFireEventsFeatureContext.EoVideoHeight
        {
            get { return _features._propertiesFeature.EoVideoHeight; }
            set { _features._propertiesFeature.EoVideoHeight = value; }
        }

        int IFireEventsFeatureContext.EoVideoWidth
        {
            get { return _features._propertiesFeature.EoVideoWidth; }
            set { _features._propertiesFeature.EoVideoWidth = value; }
        }

        Brush IFireEventsFeatureContext.EventAlertHeaderBrush
        {
            get { return _features._eventAlertsFeature.EventAlertHeaderBrush; }
        }

        string IFireEventsFeatureContext.EventAlertHeaderText
        {
            get { return _features._eventAlertsFeature.EventAlertHeaderText; }
        }

        ObservableCollection<FireEventRecord> IFireEventsFeatureContext.FireDetectionEvents
        {
            get { return _features._eventAlertsFeature.FireDetectionEvents; }
        }

        void IFireEventsFeatureContext.InitializeEventAlertFeatures()
        {
            _features._eventAlertsFeature.InitializeEventAlertFeatures();
        }

        ObservableCollection<AiDetectionBox> IFireEventsFeatureContext.IrDetectionBoxes
        {
            get { return _features._propertiesFeature.IrDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IFireEventsFeatureContext.IrFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.IrFireSmokeDetectionBoxes; }
        }

        int IFireEventsFeatureContext.IrVideoHeight
        {
            get { return _features._propertiesFeature.IrVideoHeight; }
            set { _features._propertiesFeature.IrVideoHeight = value; }
        }

        int IFireEventsFeatureContext.IrVideoWidth
        {
            get { return _features._propertiesFeature.IrVideoWidth; }
            set { _features._propertiesFeature.IrVideoWidth = value; }
        }

        bool IFireEventsFeatureContext.IsEoFireWarningVisible
        {
            get { return _features._propertiesFeature.IsEoFireWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsEoSmokeWarningVisible
        {
            get { return _features._propertiesFeature.IsEoSmokeWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsIrFireWarningVisible
        {
            get { return _features._propertiesFeature.IsIrFireWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsIrSmokeWarningVisible
        {
            get { return _features._propertiesFeature.IsIrSmokeWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsSmokeDetectionEnabled
        {
            get { return _features._smokeFeature.IsSmokeDetectionEnabled; }
            set { _features._smokeFeature.IsSmokeDetectionEnabled = value; }
        }

        bool IFireEventsFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features._thermalFeature.IsThermalFireDetectionEnabled; }
            set { _features._thermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        void IFireEventsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        void IFireEventsFeatureContext.RefreshActiveFireCount()
        {
            _features._eventAlertsFeature.RefreshActiveFireCount();
        }

        void IFireEventsFeatureContext.SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke)
        {
            _features._propertiesFeature.SetLocalFireSmokeDetectionState(rtspIndex, hasFire, hasSmoke);
        }

        Action IFireEventsFeatureContext.SaveFireEventsCallback
        {
            get { return _features.Callbacks.SaveFireEventsCallback; }
        }

        Action IFireEventsFeatureContext.LoadFireEventsCallback
        {
            get { return _features.Callbacks.LoadFireEventsCallback; }
        }

        Action IFireEventsFeatureContext.ClearFireEventsCallback
        {
            get { return _features.Callbacks.ClearFireEventsCallback; }
        }
    }
}
