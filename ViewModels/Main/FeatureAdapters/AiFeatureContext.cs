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

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal AiFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Dictionary<string, AiEventTrack> IAiFeatureContext._activeAiEvents
        {
            get { return _features._eventAlertsFeature._activeAiEvents; }
        }

        string IAiFeatureContext._aiControlAgentIp
        {
            get { return _features._screenStateFeature._aiControlAgentIp; }
            set { _features._screenStateFeature._aiControlAgentIp = value; }
        }

        Services.Communication.AI.IAiDetectorClientServicePort IAiFeatureContext._aiDetectorClientService
        {
            get { return _features.Runtime._aiDetectorClientService; }
        }

        Services.Communication.AI.IAiDetectorPacketParserPort IAiFeatureContext._aiDetectorPacketParser
        {
            get { return _features.Runtime._aiDetectorPacketParser; }
        }

        Services.Communication.AI.IAiDetectorPacketBuilderPort IAiFeatureContext._aiPacketBuilder
        {
            get { return _features.Runtime._aiPacketBuilder; }
        }

        CancellationTokenSource IAiFeatureContext._controlAgentReconnectCts
        {
            get { return _features._screenStateFeature._controlAgentReconnectCts; }
            set { _features._screenStateFeature._controlAgentReconnectCts = value; }
        }

        CancellationTokenSource IAiFeatureContext._cts
        {
            get { return _features._screenStateFeature._cts; }
            set { _features._screenStateFeature._cts = value; }
        }

        bool IAiFeatureContext._isEoFrameDisplayed
        {
            get { return _features._screenStateFeature._isEoFrameDisplayed; }
            set { _features._screenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IAiFeatureContext._isIrFrameDisplayed
        {
            get { return _features._screenStateFeature._isIrFrameDisplayed; }
            set { _features._screenStateFeature._isIrFrameDisplayed = value; }
        }

        ref global::System.DateTime IAiFeatureContext._lastAiDetectorLogTime
        {
            get { return ref _features._screenStateFeature._lastAiDetectorLogTime; }
        }

        Models.Main.RtspSourceOption IAiFeatureContext._selectedEoRtspSource
        {
            get { return _features._propertiesFeature._selectedEoRtspSource; }
            set { _features._propertiesFeature._selectedEoRtspSource = value; }
        }

        Models.Main.RtspSourceOption IAiFeatureContext._selectedIrRtspSource
        {
            get { return _features._propertiesFeature._selectedIrRtspSource; }
            set { _features._propertiesFeature._selectedIrRtspSource = value; }
        }

        DispatcherTimer IAiFeatureContext._testProgramEventTimer
        {
            get { return _features._eventAlertsFeature._testProgramEventTimer; }
            set { _features._eventAlertsFeature._testProgramEventTimer = value; }
        }

        CancellationTokenSource IAiFeatureContext._videoReconnectCts
        {
            get { return _features._screenStateFeature._videoReconnectCts; }
            set { _features._screenStateFeature._videoReconnectCts = value; }
        }

        int IAiFeatureContext.ActiveAiCount
        {
            get { return _features._eventAlertsFeature.ActiveAiCount; }
            set { _features._eventAlertsFeature.ActiveAiCount = value; }
        }

        int IAiFeatureContext.AiAgentPort
        {
            get { return _features._propertiesFeature.AiAgentPort; }
            set { _features._propertiesFeature.AiAgentPort = value; }
        }

        string IAiFeatureContext.AiControlAgentIp
        {
            get { return _features._propertiesFeature.AiControlAgentIp; }
            set { _features._propertiesFeature.AiControlAgentIp = value; }
        }

        double IAiFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features._propertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features._propertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        double IAiFeatureContext.AiMappingConfidence
        {
            get { return _features._propertiesFeature.AiMappingConfidence; }
            set { _features._propertiesFeature.AiMappingConfidence = value; }
        }

        double IAiFeatureContext.AiMappingIou
        {
            get { return _features._propertiesFeature.AiMappingIou; }
            set { _features._propertiesFeature.AiMappingIou = value; }
        }

        ObservableCollection<AiMappingInfo> IAiFeatureContext.AiMappingList
        {
            get { return _features._propertiesFeature.AiMappingList; }
        }

        ObservableCollection<AiOnnxInfo> IAiFeatureContext.AiOnnxList
        {
            get { return _features._propertiesFeature.AiOnnxList; }
        }

        string IAiFeatureContext.AiPowerStatusText
        {
            get { return _features._propertiesFeature.AiPowerStatusText; }
            set { _features._propertiesFeature.AiPowerStatusText = value; }
        }

        string IAiFeatureContext.AiRtsp0Address
        {
            get { return _features._propertiesFeature.AiRtsp0Address; }
            set { _features._propertiesFeature.AiRtsp0Address = value; }
        }

        int IAiFeatureContext.AiRtsp0OnnxIndex
        {
            get { return _features._propertiesFeature.AiRtsp0OnnxIndex; }
            set { _features._propertiesFeature.AiRtsp0OnnxIndex = value; }
        }

        string IAiFeatureContext.AiRtsp1Address
        {
            get { return _features._propertiesFeature.AiRtsp1Address; }
            set { _features._propertiesFeature.AiRtsp1Address = value; }
        }

        int IAiFeatureContext.AiRtsp1OnnxIndex
        {
            get { return _features._propertiesFeature.AiRtsp1OnnxIndex; }
            set { _features._propertiesFeature.AiRtsp1OnnxIndex = value; }
        }

        ObservableCollection<AiRtspInfo> IAiFeatureContext.AiRtspList
        {
            get { return _features._propertiesFeature.AiRtspList; }
        }

        string IAiFeatureContext.AiSettingStatusText
        {
            get { return _features._propertiesFeature.AiSettingStatusText; }
            set { _features._propertiesFeature.AiSettingStatusText = value; }
        }

        /// <summary>
        /// AI 연기 후보의 최근 프레임 정보를 비운다.
        /// </summary>
        void IAiFeatureContext.ClearAiSmokeCandidateSnapshots()
        {
            _features._smokeFeature.ClearAiSmokeCandidateSnapshots();
        }

        /// <summary>
        /// 검출 OFF는 해당 종류의 ACTIVE만 해제한다. 이력과 다른 검출 종류는 보존한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.ClearDisabledVisionEvents(bool fire, bool writeAudit)
        {
            _features._fireEventsFeature.ClearDisabledVisionEvents(fire, writeAudit);
        }

        /// <summary>
        /// 영상 연결 해제 함수 1. [CaptureLoop] 종료 요청 2. [VD__VideoCapture] 해제 3. [FFmpeg] [EO /
        /// IR] [RTSP] Decoder 해제 4. [상태 문자열 갱신] 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.Disconnect()
        {
            _features._videoFeature.Disconnect();
        }

        ObservableCollection<AiDetectionBox> IAiFeatureContext.EoDetectionBoxes
        {
            get { return _features._propertiesFeature.EoDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IAiFeatureContext.EoFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.EoFireSmokeDetectionBoxes; }
        }

        string IAiFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        int IAiFeatureContext.EoVideoHeight
        {
            get { return _features._propertiesFeature.EoVideoHeight; }
            set { _features._propertiesFeature.EoVideoHeight = value; }
        }

        int IAiFeatureContext.EoVideoWidth
        {
            get { return _features._propertiesFeature.EoVideoWidth; }
            set { _features._propertiesFeature.EoVideoWidth = value; }
        }

        ObservableCollection<AiDetectionBox> IAiFeatureContext.IrDetectionBoxes
        {
            get { return _features._propertiesFeature.IrDetectionBoxes; }
        }

        ObservableCollection<VisionDetectionBox> IAiFeatureContext.IrFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.IrFireSmokeDetectionBoxes; }
        }

        string IAiFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        int IAiFeatureContext.IrVideoHeight
        {
            get { return _features._propertiesFeature.IrVideoHeight; }
            set { _features._propertiesFeature.IrVideoHeight = value; }
        }

        int IAiFeatureContext.IrVideoWidth
        {
            get { return _features._propertiesFeature.IrVideoWidth; }
            set { _features._propertiesFeature.IrVideoWidth = value; }
        }

        bool IAiFeatureContext.IsAutoTrackingEnabled
        {
            get { return _features._propertiesFeature.IsAutoTrackingEnabled; }
            set { _features._propertiesFeature.IsAutoTrackingEnabled = value; }
        }

        bool IAiFeatureContext.IsFireDiagnosticEnabled
        {
            get { return _features._thermalFeature.IsFireDiagnosticEnabled; }
            set { _features._thermalFeature.IsFireDiagnosticEnabled = value; }
        }

        /// <summary>
        /// 파노라마 촬영 이동 때문에 로컬 화재·연기 분석을 억제해야 하는지 확인한다.
        /// </summary>
        bool IAiFeatureContext.IsPanoramaMotionDetectionSuppressed()
        {
            return _features._panoramaFeature.IsPanoramaMotionDetectionSuppressed();
        }

        bool IAiFeatureContext.IsSmokeDetectionEnabled
        {
            get { return _features._smokeFeature.IsSmokeDetectionEnabled; }
            set { _features._smokeFeature.IsSmokeDetectionEnabled = value; }
        }

        bool IAiFeatureContext.IsSmokeDiagnosticEnabled
        {
            get { return _features._smokeFeature.IsSmokeDiagnosticEnabled; }
            set { _features._smokeFeature.IsSmokeDiagnosticEnabled = value; }
        }

        bool IAiFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features._thermalFeature.IsThermalFireDetectionEnabled; }
            set { _features._thermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        /// <summary>
        /// AI 이벤트 수와 요약에 연결된 화면 속성의 변경을 알린다.
        /// </summary>
        void IAiFeatureContext.NotifyAiEventSummaryChanged()
        {
            _features._eventAlertsFeature.NotifyAiEventSummaryChanged();
        }

        /// <summary>
        /// AI BBox 갱신·Hold 만료·연결 해제 시 계산 속성의 UI 바인딩을 갱신한다. Collection은 기존 모든 AI 클래스를 그대로
        /// 유지하고 경고 속성만 다시 계산한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.NotifyFireSmokeWarningStateChanged(int rtspIndex)
        {
            _features._propertiesFeature.NotifyFireSmokeWarningStateChanged(rtspIndex);
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
            _features._rtspSettingsFeature.SaveRtspCommunicationSettings();
        }

        Models.Main.RtspSourceOption IAiFeatureContext.SelectedAiEoRtspSource
        {
            get { return _features._propertiesFeature.SelectedAiEoRtspSource; }
            set { _features._propertiesFeature.SelectedAiEoRtspSource = value; }
        }

        Models.Main.RtspSourceOption IAiFeatureContext.SelectedAiIrRtspSource
        {
            get { return _features._propertiesFeature.SelectedAiIrRtspSource; }
            set { _features._propertiesFeature.SelectedAiIrRtspSource = value; }
        }

        /// <summary>
        /// AI 연결 상태에 맞춰 로컬 화재·연기 탐지기 사용 상태를 조정한다.
        /// </summary>
        void IAiFeatureContext.SetFireSmokeDetectorsForAiConnection(bool enabled, string reason)
        {
            _features._smokeFeature.SetFireSmokeDetectorsForAiConnection(enabled, reason);
        }

        /// <summary>
        /// AI Agent의 프레임 결과를 RTSP 채널별 이벤트 생명주기로 변환한다. 프레임마다 행을 추가하지 않고 탐지 시작과 해제만 기록한다.
        /// 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IAiFeatureContext.UpdateAiDetectionEvent(AiDetectionResult result, DateTime receiveTime)
        {
            _features._eventAlertsFeature.UpdateAiDetectionEvent(result, receiveTime);
        }

        /// <summary>
        /// 채널별 최근 AI 연기 후보와 수신 시각을 보관한다.
        /// </summary>
        void IAiFeatureContext.UpdateAiSmokeCandidateSnapshot(AiDetectionResult result, DateTime receiveTime)
        {
            _features._smokeFeature.UpdateAiSmokeCandidateSnapshot(result, receiveTime);
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
