using OpenCvWpfTracking.Services.Video;
using System;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class SmokeFeatureContext : ISmokeFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal SmokeFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? ISmokeFeatureContext._activePanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activePanAbsoluteTarget; }
        }

        ref double? ISmokeFeatureContext._activeTiltAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activeTiltAbsoluteTarget; }
        }

        ContinuousMoveType ISmokeFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        ThermalFireDetectionService ISmokeFeatureContext._eoFireDetectionService
        {
            get { return _features._thermalFeature._eoFireDetectionService; }
        }

        ThermalFireDetectionService ISmokeFeatureContext._irFireDetectionService
        {
            get { return _features._thermalFeature._irFireDetectionService; }
        }

        ref int ISmokeFeatureContext._visionSettingsGeneration
        {
            get { return ref _features._fireEventsFeature._visionSettingsGeneration; }
        }

        double ISmokeFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features._propertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features._propertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        string ISmokeFeatureContext.AiPowerStatusText
        {
            get { return _features._propertiesFeature.AiPowerStatusText; }
            set { _features._propertiesFeature.AiPowerStatusText = value; }
        }

        void ISmokeFeatureContext.ClearDisabledVisionEvents(bool fire, bool writeAudit)
        {
            _features._fireEventsFeature.ClearDisabledVisionEvents(fire, writeAudit);
        }

        string ISmokeFeatureContext.FireAlertStatusText
        {
            get { return _features._fireEventsFeature.FireAlertStatusText; }
        }

        int ISmokeFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort ISmokeFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        bool ISmokeFeatureContext.IsAutoTrackingEnabled
        {
            get { return _features._propertiesFeature.IsAutoTrackingEnabled; }
            set { _features._propertiesFeature.IsAutoTrackingEnabled = value; }
        }

        bool ISmokeFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool ISmokeFeatureContext.IsLaPresetScanRunning
        {
            get { return _features._propertiesFeature.IsLaPresetScanRunning; }
            set { _features._propertiesFeature.IsLaPresetScanRunning = value; }
        }

        bool ISmokeFeatureContext.IsPanoramaCaptureRunning
        {
            get { return _features._panoramaFeature.IsPanoramaCaptureRunning; }
            set { _features._panoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        bool ISmokeFeatureContext.IsPanoramaMotionDetectionSuppressed()
        {
            return _features._panoramaFeature.IsPanoramaMotionDetectionSuppressed();
        }

        bool ISmokeFeatureContext.IsPanoramaProcessingRunning
        {
            get { return _features._panoramaFeature.IsPanoramaProcessingRunning; }
        }

        bool ISmokeFeatureContext.IsPresetScanRunning
        {
            get { return _features._propertiesFeature.IsPresetScanRunning; }
            set { _features._propertiesFeature.IsPresetScanRunning = value; }
        }

        bool ISmokeFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features._thermalFeature.IsThermalFireDetectionEnabled; }
            set { _features._thermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        void ISmokeFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        bool ISmokeFeatureContext.TryRunAiUiAction(Action action, string operationName)
        {
            return _features._aiFeature.TryRunAiUiAction(action, operationName);
        }
    }
}
