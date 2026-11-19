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

        ref double? ISmokeFeatureContext.ActivePanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activePanAbsoluteTarget; }
        }

        ref double? ISmokeFeatureContext.ActiveTiltAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activeTiltAbsoluteTarget; }
        }

        ContinuousMoveType ISmokeFeatureContext.CurrentMoveType
        {
            get { return _features.ScreenStateFeature._currentMoveType; }
            set { _features.ScreenStateFeature._currentMoveType = value; }
        }

        ThermalFireDetectionService ISmokeFeatureContext.EoFireDetectionService
        {
            get { return _features.ThermalFeature._eoFireDetectionService; }
        }

        ThermalFireDetectionService ISmokeFeatureContext.IrFireDetectionService
        {
            get { return _features.ThermalFeature._irFireDetectionService; }
        }

        ref int ISmokeFeatureContext.VisionSettingsGeneration
        {
            get { return ref _features.FireEventsFeature._visionSettingsGeneration; }
        }

        double ISmokeFeatureContext.AiDisplayConfidenceThreshold
        {
            get { return _features.PropertiesFeature.AiDisplayConfidenceThreshold; }
            set { _features.PropertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        string ISmokeFeatureContext.AiPowerStatusText
        {
            get { return _features.PropertiesFeature.AiPowerStatusText; }
            set { _features.PropertiesFeature.AiPowerStatusText = value; }
        }

        /// <summary>
        /// 검출 OFF는 해당 종류의 ACTIVE만 해제한다. 이력과 다른 검출 종류는 보존한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ISmokeFeatureContext.ClearDisabledVisionEvents(bool fire, bool writeAudit)
        {
            _features.FireEventsFeature.ClearDisabledVisionEvents(fire, writeAudit);
        }

        string ISmokeFeatureContext.FireAlertStatusText
        {
            get { return _features.FireEventsFeature.FireAlertStatusText; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int ISmokeFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort ISmokeFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        bool ISmokeFeatureContext.IsAutoTrackingEnabled
        {
            get { return _features.PropertiesFeature.IsAutoTrackingEnabled; }
            set { _features.PropertiesFeature.IsAutoTrackingEnabled = value; }
        }

        bool ISmokeFeatureContext.IsControlInputLocked
        {
            get { return _features.PropertiesFeature.IsControlInputLocked; }
        }

        bool ISmokeFeatureContext.IsLaPresetScanRunning
        {
            get { return _features.PropertiesFeature.IsLaPresetScanRunning; }
            set { _features.PropertiesFeature.IsLaPresetScanRunning = value; }
        }

        bool ISmokeFeatureContext.IsPanoramaCaptureRunning
        {
            get { return _features.PanoramaFeature.IsPanoramaCaptureRunning; }
            set { _features.PanoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        /// <summary>
        /// 파노라마 촬영 이동 때문에 로컬 화재·연기 분석을 억제해야 하는지 확인한다.
        /// </summary>
        bool ISmokeFeatureContext.IsPanoramaMotionDetectionSuppressed()
        {
            return _features.PanoramaFeature.IsPanoramaMotionDetectionSuppressed();
        }

        bool ISmokeFeatureContext.IsPanoramaProcessingRunning
        {
            get { return _features.PanoramaFeature.IsPanoramaProcessingRunning; }
        }

        bool ISmokeFeatureContext.IsPresetScanRunning
        {
            get { return _features.PropertiesFeature.IsPresetScanRunning; }
            set { _features.PropertiesFeature.IsPresetScanRunning = value; }
        }

        bool ISmokeFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features.ThermalFeature.IsThermalFireDetectionEnabled; }
            set { _features.ThermalFeature.IsThermalFireDetectionEnabled = value; }
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
            return _features.AiFeature.TryRunAiUiAction(action, operationName);
        }
    }
}
