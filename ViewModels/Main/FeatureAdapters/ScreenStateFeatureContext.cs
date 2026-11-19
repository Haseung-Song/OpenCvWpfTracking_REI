using OpenCvWpfTracking.Services.Communication;
using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class ScreenStateFeatureContext : IScreenStateFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal ScreenStateFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        IControlCommandServicePort IScreenStateFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        double IScreenStateFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features.PropertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features.PropertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        double IScreenStateFeatureContext.AiMappingConfidence
        {
            get { return _features.PropertiesFeature.AiMappingConfidence; }
            set { _features.PropertiesFeature.AiMappingConfidence = value; }
        }

        double IScreenStateFeatureContext.AiMappingIou
        {
            get { return _features.PropertiesFeature.AiMappingIou; }
            set { _features.PropertiesFeature.AiMappingIou = value; }
        }

        string IScreenStateFeatureContext.AiRtsp0Address
        {
            get { return _features.PropertiesFeature.AiRtsp0Address; }
            set { _features.PropertiesFeature.AiRtsp0Address = value; }
        }

        int IScreenStateFeatureContext.AiRtsp0OnnxIndex
        {
            get { return _features.PropertiesFeature.AiRtsp0OnnxIndex; }
            set { _features.PropertiesFeature.AiRtsp0OnnxIndex = value; }
        }

        string IScreenStateFeatureContext.AiRtsp1Address
        {
            get { return _features.PropertiesFeature.AiRtsp1Address; }
            set { _features.PropertiesFeature.AiRtsp1Address = value; }
        }

        int IScreenStateFeatureContext.AiRtsp1OnnxIndex
        {
            get { return _features.PropertiesFeature.AiRtsp1OnnxIndex; }
            set { _features.PropertiesFeature.AiRtsp1OnnxIndex = value; }
        }

        string IScreenStateFeatureContext.ControlAgentConnectionStatusColor
        {
            get { return _features.PropertiesFeature.ControlAgentConnectionStatusColor; }
            set { _features.PropertiesFeature.ControlAgentConnectionStatusColor = value; }
        }

        string IScreenStateFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features.PropertiesFeature.ControlAgentConnectionStatusText; }
            set { _features.PropertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IScreenStateFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string IScreenStateFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Services.Configuration.LensControlTuning IScreenStateFeatureContext.ControlTuningFor(bool ir)
        {
            return _features.InputCoordinatorFeature.ControlTuningFor(ir);
        }

        string IScreenStateFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        string IScreenStateFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        string IScreenStateFeatureContext.McbMaintenanceIpAddress
        {
            get { return _features.PropertiesFeature.McbMaintenanceIpAddress; }
            set { _features.PropertiesFeature.McbMaintenanceIpAddress = value; }
        }
    }
}
