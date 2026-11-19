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

        /// <summary>
        /// 검출 OFF는 해당 종류의 ACTIVE만 해제한다. 이력과 다른 검출 종류는 보존한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ISmokeFeatureContext.ClearDisabledVisionEvents(bool fire, bool writeAudit)
        {
            _features._fireEventsFeature.ClearDisabledVisionEvents(fire, writeAudit);
        }

        string ISmokeFeatureContext.FireAlertStatusText
        {
            get { return _features._fireEventsFeature.FireAlertStatusText; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// -&gt; Standard 0(Wide) Raw 0(Tele) -&gt; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int ISmokeFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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

        /// <summary>
        /// 파노라마 촬영 이동 때문에 로컬 화재·연기 분석을 억제해야 하는지 확인한다.
        /// </summary>
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

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ISmokeFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        /// <summary>
        /// 2026-08-26: Application/Dispatcher 종료 상태를 확인한 뒤 UI 작업을 수행한다. 종료 이후 지연
        /// Callback이 Application.Current를 참조하여 발생하던 예외를 방지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool ISmokeFeatureContext.TryRunAiUiAction(Action action, string operationName)
        {
            return _features._aiFeature.TryRunAiUiAction(action, operationName);
        }
    }
}
