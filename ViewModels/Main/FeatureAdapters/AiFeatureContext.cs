using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class AiFeatureContext : IAiFeatureContext
    {
        private readonly FeatureComposition _features;

        /// <summary>UI 갱신에 사용하는 원본 탐지 결과를 추적 기능에 전달한다.</summary>
        void IAiFeatureContext.QueueAutoTracking(AiDetectionResult result, DateTime receiveTime)
        {
            _features.AutoTrackingFeature.Enqueue(result, receiveTime);
        }

        /// <summary>현재 제어 작업의 진입 사유를 전달하고 자동 추적을 종료한다.</summary>
        void IAiFeatureContext.StopAutoTracking(string reason)
        {
            _features.AutoTrackingFeature.Stop(reason);
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal AiFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Dictionary<string, AiEventTrack> IAiFeatureContext.ActiveAiEvents
        {
            get { return _features.EventAlertsFeature._activeAiEvents; }
        }

        string IAiFeatureContext.AiControlAgentIpState
        {
            get { return _features.ScreenStateFeature._aiControlAgentIp; }
            set { _features.ScreenStateFeature._aiControlAgentIp = value; }
        }

        Services.Communication.AI.IAiDetectorClientServicePort IAiFeatureContext.AiDetectorClientService
        {
            get { return _features.Runtime._aiDetectorClientService; }
        }

        Services.Communication.AI.IAiDetectorPacketParserPort IAiFeatureContext.AiDetectorPacketParser
        {
            get { return _features.Runtime._aiDetectorPacketParser; }
        }

        Services.Communication.AI.IAiDetectorPacketBuilderPort IAiFeatureContext.AiPacketBuilder
        {
            get { return _features.Runtime._aiPacketBuilder; }
        }

        CancellationTokenSource IAiFeatureContext.ControlAgentReconnectCts
        {
            get { return _features.ScreenStateFeature._controlAgentReconnectCts; }
            set { _features.ScreenStateFeature._controlAgentReconnectCts = value; }
        }

        CancellationTokenSource IAiFeatureContext.Cts
        {
            get { return _features.ScreenStateFeature._cts; }
            set { _features.ScreenStateFeature._cts = value; }
        }

        bool IAiFeatureContext.IsEoFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isEoFrameDisplayed; }
            set { _features.ScreenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IAiFeatureContext.IsIrFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isIrFrameDisplayed; }
            set { _features.ScreenStateFeature._isIrFrameDisplayed = value; }
        }

        ref global::System.DateTime IAiFeatureContext.LastAiDetectorLogTime
        {
            get { return ref _features.ScreenStateFeature._lastAiDetectorLogTime; }
        }

        Models.Main.RtspSourceOption IAiFeatureContext.SelectedEoRtspSourceState
        {
            get { return _features.PropertiesFeature._selectedEoRtspSource; }
            set { _features.PropertiesFeature._selectedEoRtspSource = value; }
        }

        Models.Main.RtspSourceOption IAiFeatureContext.SelectedIrRtspSourceState
        {
            get { return _features.PropertiesFeature._selectedIrRtspSource; }
            set { _features.PropertiesFeature._selectedIrRtspSource = value; }
        }

        DispatcherTimer IAiFeatureContext.TestProgramEventTimer
        {
            get { return _features.EventAlertsFeature._testProgramEventTimer; }
            set { _features.EventAlertsFeature._testProgramEventTimer = value; }
        }

        CancellationTokenSource IAiFeatureContext.VideoReconnectCts
        {
            get { return _features.ScreenStateFeature._videoReconnectCts; }
            set { _features.ScreenStateFeature._videoReconnectCts = value; }
        }

        int IAiFeatureContext.ActiveAiCount
        {
            get { return _features.EventAlertsFeature.ActiveAiCount; }
            set { _features.EventAlertsFeature.ActiveAiCount = value; }
        }

        int IAiFeatureContext.AiAgentPort
        {
            get { return _features.PropertiesFeature.AiAgentPort; }
            set { _features.PropertiesFeature.AiAgentPort = value; }
        }

        string IAiFeatureContext.AiControlAgentIp
        {
            get { return _features.PropertiesFeature.AiControlAgentIp; }
            set { _features.PropertiesFeature.AiControlAgentIp = value; }
        }

        double IAiFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features.PropertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features.PropertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        double IAiFeatureContext.AiMappingConfidence
        {
            get { return _features.PropertiesFeature.AiMappingConfidence; }
            set { _features.PropertiesFeature.AiMappingConfidence = value; }
        }

        double IAiFeatureContext.AiMappingIou
        {
            get { return _features.PropertiesFeature.AiMappingIou; }
            set { _features.PropertiesFeature.AiMappingIou = value; }
        }

        ObservableCollection<AiMappingInfo> IAiFeatureContext.AiMappingList
        {
            get { return _features.PropertiesFeature.AiMappingList; }
        }

        ObservableCollection<AiOnnxInfo> IAiFeatureContext.AiOnnxList
        {
            get { return _features.PropertiesFeature.AiOnnxList; }
        }

        string IAiFeatureContext.AiPowerStatusText
        {
            get { return _features.PropertiesFeature.AiPowerStatusText; }
            set { _features.PropertiesFeature.AiPowerStatusText = value; }
        }

        string IAiFeatureContext.AiRtsp0Address
        {
            get { return _features.PropertiesFeature.AiRtsp0Address; }
            set { _features.PropertiesFeature.AiRtsp0Address = value; }
        }

        int IAiFeatureContext.AiRtsp0OnnxIndex
        {
            get { return _features.PropertiesFeature.AiRtsp0OnnxIndex; }
            set { _features.PropertiesFeature.AiRtsp0OnnxIndex = value; }
        }

        string IAiFeatureContext.AiRtsp1Address
        {
            get { return _features.PropertiesFeature.AiRtsp1Address; }
            set { _features.PropertiesFeature.AiRtsp1Address = value; }
        }

        int IAiFeatureContext.AiRtsp1OnnxIndex
        {
            get { return _features.PropertiesFeature.AiRtsp1OnnxIndex; }
            set { _features.PropertiesFeature.AiRtsp1OnnxIndex = value; }
        }

        ObservableCollection<AiRtspInfo> IAiFeatureContext.AiRtspList
        {
            get { return _features.PropertiesFeature.AiRtspList; }
        }

        string IAiFeatureContext.AiSettingStatusText
        {
            get { return _features.PropertiesFeature.AiSettingStatusText; }
            set { _features.PropertiesFeature.AiSettingStatusText = value; }
        }

        /// <summary>
        /// AI 연기 후보의 최근 프레임 정보를 비운다.
        /// </summary>
        void IAiFeatureContext.ClearAiSmokeCandidateSnapshots()
        {
            _features.SmokeFeature.ClearAiSmokeCandidateSnapshots();
        }

        /// <summary>
        /// 검출 OFF는 해당 종류의 ACTIVE만 해제한다. 이력과 다른 검출 종류는 보존한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.ClearDisabledVisionEvents(bool fire, bool writeAudit)
        {
            _features.FireEventsFeature.ClearDisabledVisionEvents(fire, writeAudit);
        }

        /// <summary>
        /// 영상 연결 해제 함수 1. [CaptureLoop] 종료 요청 2. [VD__VideoCapture] 해제 3. [FFmpeg] [EO /
        /// IR] [RTSP] Decoder 해제 4. [상태 문자열 갱신] 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.Disconnect()
        {
            _features.VideoFeature.Disconnect();
        }

        ObservableCollection<AiDetectionBox> IAiFeatureContext.EoDetectionBoxes
        {
            get { return _features.PropertiesFeature.EoDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IAiFeatureContext.EoFireSmokeDetectionBoxes
        {
            get { return _features.PropertiesFeature.EoFireSmokeDetectionBoxes; }
        }

        string IAiFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        int IAiFeatureContext.EoVideoHeight
        {
            get { return _features.PropertiesFeature.EoVideoHeight; }
            set { _features.PropertiesFeature.EoVideoHeight = value; }
        }

        int IAiFeatureContext.EoVideoWidth
        {
            get { return _features.PropertiesFeature.EoVideoWidth; }
            set { _features.PropertiesFeature.EoVideoWidth = value; }
        }

        ObservableCollection<AiDetectionBox> IAiFeatureContext.IrDetectionBoxes
        {
            get { return _features.PropertiesFeature.IrDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IAiFeatureContext.IrFireSmokeDetectionBoxes
        {
            get { return _features.PropertiesFeature.IrFireSmokeDetectionBoxes; }
        }

        string IAiFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        int IAiFeatureContext.IrVideoHeight
        {
            get { return _features.PropertiesFeature.IrVideoHeight; }
            set { _features.PropertiesFeature.IrVideoHeight = value; }
        }

        int IAiFeatureContext.IrVideoWidth
        {
            get { return _features.PropertiesFeature.IrVideoWidth; }
            set { _features.PropertiesFeature.IrVideoWidth = value; }
        }

        bool IAiFeatureContext.IsAutoTrackingEnabled
        {
            get { return _features.PropertiesFeature.IsAutoTrackingEnabled; }
            set { _features.PropertiesFeature.IsAutoTrackingEnabled = value; }
        }

        bool IAiFeatureContext.IsFireDiagnosticEnabled
        {
            get { return _features.ThermalFeature.IsFireDiagnosticEnabled; }
            set { _features.ThermalFeature.IsFireDiagnosticEnabled = value; }
        }

        /// <summary>
        /// 파노라마 촬영 이동 때문에 로컬 화재·연기 분석을 억제해야 하는지 확인한다.
        /// </summary>
        bool IAiFeatureContext.IsPanoramaMotionDetectionSuppressed()
        {
            return _features.PanoramaFeature.IsPanoramaMotionDetectionSuppressed();
        }

        bool IAiFeatureContext.IsSmokeDetectionEnabled
        {
            get { return _features.SmokeFeature.IsSmokeDetectionEnabled; }
            set { _features.SmokeFeature.IsSmokeDetectionEnabled = value; }
        }

        bool IAiFeatureContext.IsSmokeDiagnosticEnabled
        {
            get { return _features.SmokeFeature.IsSmokeDiagnosticEnabled; }
            set { _features.SmokeFeature.IsSmokeDiagnosticEnabled = value; }
        }

        bool IAiFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features.ThermalFeature.IsThermalFireDetectionEnabled; }
            set { _features.ThermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        /// <summary>
        /// AI 이벤트 수와 요약에 연결된 화면 속성의 변경을 알린다.
        /// </summary>
        void IAiFeatureContext.NotifyAiEventSummaryChanged()
        {
            _features.EventAlertsFeature.NotifyAiEventSummaryChanged();
        }

        /// <summary>
        /// AI BBox 갱신·Hold 만료·연결 해제 시 계산 속성의 UI 바인딩을 갱신한다. Collection은 기존 모든 AI 클래스를 그대로
        /// 유지하고 경고 속성만 다시 계산한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.NotifyFireSmokeWarningStateChanged(int rtspIndex)
        {
            _features.PropertiesFeature.NotifyFireSmokeWarningStateChanged(rtspIndex);
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// 현재 선택값을 다음 연결용 설정으로 저장하며 로그의 인증정보는 가린다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.SaveRtspCommunicationSettings()
        {
            _features.RtspSettingsFeature.SaveRtspCommunicationSettings();
        }

        Models.Main.RtspSourceOption IAiFeatureContext.SelectedAiEoRtspSource
        {
            get { return _features.PropertiesFeature.SelectedAiEoRtspSource; }
            set { _features.PropertiesFeature.SelectedAiEoRtspSource = value; }
        }

        Models.Main.RtspSourceOption IAiFeatureContext.SelectedAiIrRtspSource
        {
            get { return _features.PropertiesFeature.SelectedAiIrRtspSource; }
            set { _features.PropertiesFeature.SelectedAiIrRtspSource = value; }
        }

        /// <summary>
        /// AI 연결 상태에 맞춰 로컬 화재·연기 탐지기 사용 상태를 조정한다.
        /// </summary>
        void IAiFeatureContext.SetFireSmokeDetectorsForAiConnection(bool enabled, string reason)
        {
            _features.SmokeFeature.SetFireSmokeDetectorsForAiConnection(enabled, reason);
        }

        /// <summary>
        /// AI Agent의 프레임 결과를 RTSP 채널별 이벤트 생명주기로 변환한다. 프레임마다 행을 추가하지 않고 탐지 시작과 해제만 기록한다.
        /// 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.UpdateAiDetectionEvent(AiDetectionResult result, DateTime receiveTime)
        {
            _features.EventAlertsFeature.UpdateAiDetectionEvent(result, receiveTime);
        }

        /// <summary>
        /// 채널별 최근 AI 연기 후보와 수신 시각을 보관한다.
        /// </summary>
        void IAiFeatureContext.UpdateAiSmokeCandidateSnapshot(AiDetectionResult result, DateTime receiveTime)
        {
            _features.SmokeFeature.UpdateAiSmokeCandidateSnapshot(result, receiveTime);
        }

        Action IAiFeatureContext.DrainPendingAiUiUpdatesCallback
        {
            get { return _features.Callbacks.DrainPendingAiUiUpdatesCallback; }
        }

        Action<byte[], DateTime> IAiFeatureContext.OnAiDetectorPacketReceivedCallback
        {
            get { return _features.Callbacks.OnAiDetectorPacketReceivedCallback; }
        }
    }
}
