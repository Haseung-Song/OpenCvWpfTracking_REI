using System;
using System.Threading;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Communication;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class WebAgentMotionFocusFeatureContext : IWebAgentMotionFocusFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal WebAgentMotionFocusFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        IControlCommandServicePort IWebAgentMotionFocusFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> IWebAgentMotionFocusFeatureContext._controlInputs
        {
            get { return _features._inputCoordinatorFeature._controlInputs; }
        }

        ContinuousMoveType IWebAgentMotionFocusFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        CancellationTokenSource IWebAgentMotionFocusFeatureContext._laPresetSingleMoveCts
        {
            get { return _features._screenStateFeature._laPresetSingleMoveCts; }
            set { _features._screenStateFeature._laPresetSingleMoveCts = value; }
        }

        ITcpClientServicePort IWebAgentMotionFocusFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        SemaphoreSlim IWebAgentMotionFocusFeatureContext._lensSyncOperationLock
        {
            get { return _features.Runtime._lensSyncOperationLock; }
        }

        CancellationTokenSource IWebAgentMotionFocusFeatureContext._moveControlPanCts
        {
            get { return _features._screenStateFeature._moveControlPanCts; }
            set { _features._screenStateFeature._moveControlPanCts = value; }
        }

        CancellationTokenSource IWebAgentMotionFocusFeatureContext._presetSingleMoveCts
        {
            get { return _features._screenStateFeature._presetSingleMoveCts; }
            set { _features._screenStateFeature._presetSingleMoveCts = value; }
        }

        CancellationTokenSource IWebAgentMotionFocusFeatureContext._rooftopFocusSyncCts
        {
            get { return _features._screenStateFeature._rooftopFocusSyncCts; }
            set { _features._screenStateFeature._rooftopFocusSyncCts = value; }
        }

        CancellationTokenSource IWebAgentMotionFocusFeatureContext._rooftopZoomSyncCts
        {
            get { return _features._screenStateFeature._rooftopZoomSyncCts; }
            set { _features._screenStateFeature._rooftopZoomSyncCts = value; }
        }

        ref global::System.Tuple<double, double, long> IWebAgentMotionFocusFeatureContext._videoCenterPose
        {
            get { return ref _features._cameraFeatureControlFeature._videoCenterPose; }
        }

        /// <summary>
        /// 현재 RadioButton에서 선택한 Pan 회전 모드를 Pelco-D 확장 명령 0x4D로 장비에 적용한다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        bool IWebAgentMotionFocusFeatureContext.ApplySelectedPanTurnMode()
        {
            return _features._moveControlFeature.ApplySelectedPanTurnMode();
        }

        bool IWebAgentMotionFocusFeatureContext.CameraFeatureTransportReady
        {
            get { return _features._cameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        string IWebAgentMotionFocusFeatureContext.ControlLockMessage
        {
            get { return _features._propertiesFeature.ControlLockMessage; }
        }

        string IWebAgentMotionFocusFeatureContext.ControlLockTitle
        {
            get { return _features._propertiesFeature.ControlLockTitle; }
        }

        double IWebAgentMotionFocusFeatureContext.CurrentMoveControlPanMaximum
        {
            get { return _features._screenStateFeature.CurrentMoveControlPanMaximum; }
        }

        double IWebAgentMotionFocusFeatureContext.CurrentMoveControlPanMinimum
        {
            get { return _features._screenStateFeature.CurrentMoveControlPanMinimum; }
        }

        double IWebAgentMotionFocusFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double IWebAgentMotionFocusFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        bool IWebAgentMotionFocusFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IWebAgentMotionFocusFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool IWebAgentMotionFocusFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IWebAgentMotionFocusFeatureContext.IsOperationLockOverlayVisible
        {
            get { return _features._propertiesFeature.IsOperationLockOverlayVisible; }
        }

        bool IWebAgentMotionFocusFeatureContext.IsPanTiltSpeedControlEnabled
        {
            get { return _features._propertiesFeature.IsPanTiltSpeedControlEnabled; }
        }

        bool IWebAgentMotionFocusFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentMotionFocusFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        double? IWebAgentMotionFocusFeatureContext.PanAbsoluteValue
        {
            get { return _features._propertiesFeature.PanAbsoluteValue; }
            set { _features._propertiesFeature.PanAbsoluteValue = value; }
        }

        double? IWebAgentMotionFocusFeatureContext.TiltAbsoluteValue
        {
            get { return _features._propertiesFeature.TiltAbsoluteValue; }
            set { _features._propertiesFeature.TiltAbsoluteValue = value; }
        }

        /// <summary>
        /// 현재 선택된 EO 프리셋이 CTEC CGI 직접 제어 대상인지 확인한다. EO 주소가 프리셋 외부 값으로 변경된 경우에는 잘못된 카메라로
        /// 명령이 송신되지 않도록 기존 Control Agent 경로를 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IWebAgentMotionFocusFeatureContext.TryGetSelectedEoCtecSource(out RtspSourceOption sourceOption)
        {
            return _features._continuousControlFeature.TryGetSelectedEoCtecSource(out sourceOption);
        }

        Action<object> IWebAgentMotionFocusFeatureContext.ExecuteAutoFocusCallback
        {
            get { return _features.Callbacks.ExecuteAutoFocusCallback; }
        }

        Predicate<object> IWebAgentMotionFocusFeatureContext.CanExecuteAutoFocusCallback
        {
            get { return _features.Callbacks.CanExecuteAutoFocusCallback; }
        }
    }
}
