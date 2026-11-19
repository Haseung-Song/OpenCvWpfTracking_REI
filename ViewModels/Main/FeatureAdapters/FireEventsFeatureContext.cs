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

        List<FireEventRecord> IFireEventsFeatureContext.ActiveTestFireEvents
        {
            get { return _features.EventAlertsFeature._activeTestFireEvents; }
        }

        bool IFireEventsFeatureContext.HasEoLocalFireDetection
        {
            get { return _features.PropertiesFeature._hasEoLocalFireDetection; }
            set { _features.PropertiesFeature._hasEoLocalFireDetection = value; }
        }

        bool IFireEventsFeatureContext.HasEoLocalSmokeDetection
        {
            get { return _features.PropertiesFeature._hasEoLocalSmokeDetection; }
            set { _features.PropertiesFeature._hasEoLocalSmokeDetection = value; }
        }

        bool IFireEventsFeatureContext.HasIrLocalFireDetection
        {
            get { return _features.PropertiesFeature._hasIrLocalFireDetection; }
            set { _features.PropertiesFeature._hasIrLocalFireDetection = value; }
        }

        bool IFireEventsFeatureContext.HasIrLocalSmokeDetection
        {
            get { return _features.PropertiesFeature._hasIrLocalSmokeDetection; }
            set { _features.PropertiesFeature._hasIrLocalSmokeDetection = value; }
        }

        bool IFireEventsFeatureContext.IsEoFireCandidateDetected
        {
            get { return _features.ThermalFeature._isEoFireCandidateDetected; }
            set { _features.ThermalFeature._isEoFireCandidateDetected = value; }
        }

        bool IFireEventsFeatureContext.IsIrFireCandidateDetected
        {
            get { return _features.ThermalFeature._isIrFireCandidateDetected; }
            set { _features.ThermalFeature._isIrFireCandidateDetected = value; }
        }

        /// <summary>
        /// 영상 탐지 사각형과 현재 위치를 이용해 구역 판정 결과를 구한다.
        /// </summary>
        ZoneAssessment IFireEventsFeatureContext.AssessZone(string camera, string type, double left, double top, double right, double bottom, ZoneFrameContext f)
        {
            return _features.ZonesFeature.AssessZone(camera, type, left, top, right, bottom, f);
        }

        /// <summary>
        /// 현재 위치·센서·렌즈 상태를 일관된 스냅샷으로 취득한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        PositionSnapshot IFireEventsFeatureContext.CapturePositionSnapshot(string source, int? presetId)
        {
            return _features.PositionFeature.CapturePositionSnapshot(source, presetId);
        }

        ObservableCollection<AiDetectionBox> IFireEventsFeatureContext.EoDetectionBoxes
        {
            get { return _features.PropertiesFeature.EoDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IFireEventsFeatureContext.EoFireSmokeDetectionBoxes
        {
            get { return _features.PropertiesFeature.EoFireSmokeDetectionBoxes; }
        }

        int IFireEventsFeatureContext.EoVideoHeight
        {
            get { return _features.PropertiesFeature.EoVideoHeight; }
            set { _features.PropertiesFeature.EoVideoHeight = value; }
        }

        int IFireEventsFeatureContext.EoVideoWidth
        {
            get { return _features.PropertiesFeature.EoVideoWidth; }
            set { _features.PropertiesFeature.EoVideoWidth = value; }
        }

        Brush IFireEventsFeatureContext.EventAlertHeaderBrush
        {
            get { return _features.EventAlertsFeature.EventAlertHeaderBrush; }
        }

        string IFireEventsFeatureContext.EventAlertHeaderText
        {
            get { return _features.EventAlertsFeature.EventAlertHeaderText; }
        }

        ObservableCollection<FireEventRecord> IFireEventsFeatureContext.FireDetectionEvents
        {
            get { return _features.EventAlertsFeature.FireDetectionEvents; }
        }

        /// <summary>
        /// AI 이벤트 명령과 테스트 프로그램 실시간 이벤트 감시를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IFireEventsFeatureContext.InitializeEventAlertFeatures()
        {
            _features.EventAlertsFeature.InitializeEventAlertFeatures();
        }

        ObservableCollection<AiDetectionBox> IFireEventsFeatureContext.IrDetectionBoxes
        {
            get { return _features.PropertiesFeature.IrDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IFireEventsFeatureContext.IrFireSmokeDetectionBoxes
        {
            get { return _features.PropertiesFeature.IrFireSmokeDetectionBoxes; }
        }

        int IFireEventsFeatureContext.IrVideoHeight
        {
            get { return _features.PropertiesFeature.IrVideoHeight; }
            set { _features.PropertiesFeature.IrVideoHeight = value; }
        }

        int IFireEventsFeatureContext.IrVideoWidth
        {
            get { return _features.PropertiesFeature.IrVideoWidth; }
            set { _features.PropertiesFeature.IrVideoWidth = value; }
        }

        bool IFireEventsFeatureContext.IsEoFireWarningVisible
        {
            get { return _features.PropertiesFeature.IsEoFireWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsEoSmokeWarningVisible
        {
            get { return _features.PropertiesFeature.IsEoSmokeWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsIrFireWarningVisible
        {
            get { return _features.PropertiesFeature.IsIrFireWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsIrSmokeWarningVisible
        {
            get { return _features.PropertiesFeature.IsIrSmokeWarningVisible; }
        }

        bool IFireEventsFeatureContext.IsSmokeDetectionEnabled
        {
            get { return _features.SmokeFeature.IsSmokeDetectionEnabled; }
            set { _features.SmokeFeature.IsSmokeDetectionEnabled = value; }
        }

        bool IFireEventsFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features.ThermalFeature.IsThermalFireDetectionEnabled; }
            set { _features.ThermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IFireEventsFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// 2026-08-27: BBox 개수가 아닌 현재 ACTIVE FIRE/SMOKE 이벤트 행 개수를 합산한다. 실제 처리는 연결된 기능 객체에
        /// 위임한다.
        /// </summary>
        void IFireEventsFeatureContext.RefreshActiveFireCount()
        {
            _features.EventAlertsFeature.RefreshActiveFireCount();
        }

        /// <summary>
        /// 로컬 화재·연기 탐지의 사용 여부를 관련 상태에 반영한다.
        /// </summary>
        void IFireEventsFeatureContext.SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke)
        {
            _features.PropertiesFeature.SetLocalFireSmokeDetectionState(rtspIndex, hasFire, hasSmoke);
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
