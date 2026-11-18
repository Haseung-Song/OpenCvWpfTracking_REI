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

        IControlCommandServicePort IScreenStateFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        double IScreenStateFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features._propertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features._propertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        double IScreenStateFeatureContext.AiMappingConfidence
        {
            get { return _features._propertiesFeature.AiMappingConfidence; }
            set { _features._propertiesFeature.AiMappingConfidence = value; }
        }

        double IScreenStateFeatureContext.AiMappingIou
        {
            get { return _features._propertiesFeature.AiMappingIou; }
            set { _features._propertiesFeature.AiMappingIou = value; }
        }

        string IScreenStateFeatureContext.AiRtsp0Address
        {
            get { return _features._propertiesFeature.AiRtsp0Address; }
            set { _features._propertiesFeature.AiRtsp0Address = value; }
        }

        int IScreenStateFeatureContext.AiRtsp0OnnxIndex
        {
            get { return _features._propertiesFeature.AiRtsp0OnnxIndex; }
            set { _features._propertiesFeature.AiRtsp0OnnxIndex = value; }
        }

        string IScreenStateFeatureContext.AiRtsp1Address
        {
            get { return _features._propertiesFeature.AiRtsp1Address; }
            set { _features._propertiesFeature.AiRtsp1Address = value; }
        }

        int IScreenStateFeatureContext.AiRtsp1OnnxIndex
        {
            get { return _features._propertiesFeature.AiRtsp1OnnxIndex; }
            set { _features._propertiesFeature.AiRtsp1OnnxIndex = value; }
        }

        string IScreenStateFeatureContext.ControlAgentConnectionStatusColor
        {
            get { return _features._propertiesFeature.ControlAgentConnectionStatusColor; }
            set { _features._propertiesFeature.ControlAgentConnectionStatusColor = value; }
        }

        string IScreenStateFeatureContext.ControlAgentConnectionStatusText
        {
            get { return _features._propertiesFeature.ControlAgentConnectionStatusText; }
            set { _features._propertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        string IScreenStateFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IScreenStateFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        Services.Configuration.LensControlTuning IScreenStateFeatureContext.ControlTuningFor(bool ir)
        {
            return _features._inputCoordinatorFeature.ControlTuningFor(ir);
        }

        string IScreenStateFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        string IScreenStateFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        string IScreenStateFeatureContext.McbMaintenanceIpAddress
        {
            get { return _features._propertiesFeature.McbMaintenanceIpAddress; }
            set { _features._propertiesFeature.McbMaintenanceIpAddress = value; }
        }
    }
}
