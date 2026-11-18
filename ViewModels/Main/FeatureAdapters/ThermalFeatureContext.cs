using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class ThermalFeatureContext : IThermalFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal ThermalFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Services.Communication.IControlCommandServicePort IThermalFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ref int IThermalFeatureContext._visionSettingsGeneration
        {
            get { return ref _features._fireEventsFeature._visionSettingsGeneration; }
        }

        Services.Communication.WebAgent.IWebAgentThermalPaletteServicePort IThermalFeatureContext._webAgentThermalPaletteService
        {
            get { return _features.Runtime._webAgentThermalPaletteService; }
        }

        bool IThermalFeatureContext.CameraFeatureTransportReady
        {
            get { return _features._cameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        void IThermalFeatureContext.ClearDisabledVisionEvents(bool fire, bool writeAudit)
        {
            _features._fireEventsFeature.ClearDisabledVisionEvents(fire, writeAudit);
        }

        string IThermalFeatureContext.FireAlertStatusText
        {
            get { return _features._fireEventsFeature.FireAlertStatusText; }
        }

        bool IThermalFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IThermalFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IThermalFeatureContext.IsSelectedConnectionTarget
        {
            get { return _features._rtspSettingsFeature.IsSelectedConnectionTarget; }
        }

        bool IThermalFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        void IThermalFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        void IThermalFeatureContext.ResetFireSmokeFrameAnalysis(string reason)
        {
            _features._smokeFeature.ResetFireSmokeFrameAnalysis(reason);
        }

        void IThermalFeatureContext.UpdateVisionBBoxEvents(string camera, string detectionType, System.Collections.Generic.IList<OpenCvSharp.Rect> candidates, System.Collections.Generic.IList<double> candidateScores, string detectionSource, Services.Position.ZoneFrameContext zoneFrame)
        {
            _features._fireEventsFeature.UpdateVisionBBoxEvents(camera, detectionType, candidates, candidateScores, detectionSource, zoneFrame);
        }
    }
}
