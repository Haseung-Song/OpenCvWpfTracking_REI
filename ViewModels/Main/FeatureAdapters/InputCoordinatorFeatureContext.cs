using System;
using System.Threading.Tasks;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class InputCoordinatorFeatureContext : IInputCoordinatorFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal InputCoordinatorFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref global::OpenCvWpfTracking.Models.Main.RtspSourceOption IInputCoordinatorFeatureContext._activeEoCtecSource
        {
            get { return ref _features._screenStateFeature._activeEoCtecSource; }
        }

        ref double? IInputCoordinatorFeatureContext._activePanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activePanAbsoluteTarget; }
        }

        ref double? IInputCoordinatorFeatureContext._activeTiltAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activeTiltAbsoluteTarget; }
        }

        Services.Communication.AI.IAiDetectorClientServicePort IInputCoordinatorFeatureContext._aiDetectorClientService
        {
            get { return _features.Runtime._aiDetectorClientService; }
        }

        Services.Communication.IControlCommandServicePort IInputCoordinatorFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ContinuousMoveType IInputCoordinatorFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        bool IInputCoordinatorFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        Services.Communication.ITcpClientServicePort IInputCoordinatorFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ControlAgentProfileOption IInputCoordinatorFeatureContext._requestedControlProfile
        {
            get { return _features._rtspSettingsFeature._requestedControlProfile; }
            set { _features._rtspSettingsFeature._requestedControlProfile = value; }
        }

        bool IInputCoordinatorFeatureContext._videoCenterBusy
        {
            get { return _features._cameraFeatureControlFeature._videoCenterBusy; }
            set { _features._cameraFeatureControlFeature._videoCenterBusy = value; }
        }

        string IInputCoordinatorFeatureContext.AiPowerStatusText
        {
            get { return _features._propertiesFeature.AiPowerStatusText; }
            set { _features._propertiesFeature.AiPowerStatusText = value; }
        }

        void IInputCoordinatorFeatureContext.ClearActivePanTiltAbsoluteMove()
        {
            _features._moveControlFeature.ClearActivePanTiltAbsoluteMove();
        }

        void IInputCoordinatorFeatureContext.ClearAiDetectionState(string reason, bool writeAudit)
        {
            _features._aiFeature.ClearAiDetectionState(reason, writeAudit);
        }

        byte IInputCoordinatorFeatureContext.EffectivePanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.EffectivePanTiltSpeedLevel; }
        }

        double IInputCoordinatorFeatureContext.EoDisplayZoomRatio
        {
            get { return _features._cameraFeatureControlFeature.EoDisplayZoomRatio; }
        }

        string IInputCoordinatorFeatureContext.EoPowerControlStatusText
        {
            get { return _features._devicePowerFeature.EoPowerControlStatusText; }
        }

        int IInputCoordinatorFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort IInputCoordinatorFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        double IInputCoordinatorFeatureContext.IrDisplayZoomRatio
        {
            get { return _features._cameraFeatureControlFeature.IrDisplayZoomRatio; }
        }

        string IInputCoordinatorFeatureContext.IrPowerControlStatusText
        {
            get { return _features._devicePowerFeature.IrPowerControlStatusText; }
        }

        bool IInputCoordinatorFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features._webAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IInputCoordinatorFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool IInputCoordinatorFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        void IInputCoordinatorFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IInputCoordinatorFeatureContext.PanTiltSpeedDisplayText
        {
            get { return _features._propertiesFeature.PanTiltSpeedDisplayText; }
        }

        byte IInputCoordinatorFeatureContext.PanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.PanTiltSpeedLevel; }
            set { _features._propertiesFeature.PanTiltSpeedLevel = value; }
        }

        void IInputCoordinatorFeatureContext.ResetAiRtspVideoSizes()
        {
            _features._aiFeature.ResetAiRtspVideoSizes();
        }

        ControlAgentProfileOption IInputCoordinatorFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        void IInputCoordinatorFeatureContext.SetActivePanTiltZoomSource(bool isInfrared)
        {
            _features._continuousControlFeature.SetActivePanTiltZoomSource(isInfrared);
        }

        void IInputCoordinatorFeatureContext.SetFireSmokeDetectorsForAiConnection(bool enabled, string reason)
        {
            _features._smokeFeature.SetFireSmokeDetectorsForAiConnection(enabled, reason);
        }

        Task IInputCoordinatorFeatureContext.StartEoFocusFarMoveAsync()
        {
            return _features._continuousControlFeature.StartEoFocusFarMoveAsync();
        }

        Task IInputCoordinatorFeatureContext.StartEoFocusNearMoveAsync()
        {
            return _features._continuousControlFeature.StartEoFocusNearMoveAsync();
        }

        Task IInputCoordinatorFeatureContext.StartEoZoomInMoveAsync()
        {
            return _features._continuousControlFeature.StartEoZoomInMoveAsync();
        }

        Task IInputCoordinatorFeatureContext.StartEoZoomOutMoveAsync()
        {
            return _features._continuousControlFeature.StartEoZoomOutMoveAsync();
        }

        void IInputCoordinatorFeatureContext.StartIrFocusFarMove()
        {
            _features._continuousControlFeature.StartIrFocusFarMove();
        }

        void IInputCoordinatorFeatureContext.StartIrFocusNearMove()
        {
            _features._continuousControlFeature.StartIrFocusNearMove();
        }

        void IInputCoordinatorFeatureContext.StartIrZoomInMove()
        {
            _features._continuousControlFeature.StartIrZoomInMove();
        }

        void IInputCoordinatorFeatureContext.StartIrZoomOutMove()
        {
            _features._continuousControlFeature.StartIrZoomOutMove();
        }

        void IInputCoordinatorFeatureContext.StartPanLeftMove()
        {
            _features._continuousControlFeature.StartPanLeftMove();
        }

        void IInputCoordinatorFeatureContext.StartPanLeftTiltDownMove()
        {
            _features._continuousControlFeature.StartPanLeftTiltDownMove();
        }

        void IInputCoordinatorFeatureContext.StartPanLeftTiltUpMove()
        {
            _features._continuousControlFeature.StartPanLeftTiltUpMove();
        }

        void IInputCoordinatorFeatureContext.StartPanRightMove()
        {
            _features._continuousControlFeature.StartPanRightMove();
        }

        void IInputCoordinatorFeatureContext.StartPanRightTiltDownMove()
        {
            _features._continuousControlFeature.StartPanRightTiltDownMove();
        }

        void IInputCoordinatorFeatureContext.StartPanRightTiltUpMove()
        {
            _features._continuousControlFeature.StartPanRightTiltUpMove();
        }

        void IInputCoordinatorFeatureContext.StartTiltDownMove()
        {
            _features._continuousControlFeature.StartTiltDownMove();
        }

        void IInputCoordinatorFeatureContext.StartTiltUpMove()
        {
            _features._continuousControlFeature.StartTiltUpMove();
        }

        void IInputCoordinatorFeatureContext.StopAllPtzfMove()
        {
            _features._continuousControlFeature.StopAllPtzfMove();
        }

        Task IInputCoordinatorFeatureContext.StopContinuousMoveAsync(bool waitForStatus)
        {
            return _features._continuousControlFeature.StopContinuousMoveAsync(waitForStatus);
        }

        void IInputCoordinatorFeatureContext.StopIrFocusMove()
        {
            _features._continuousControlFeature.StopIrFocusMove();
        }

        void IInputCoordinatorFeatureContext.StopIrZoomMove()
        {
            _features._continuousControlFeature.StopIrZoomMove();
        }

        void IInputCoordinatorFeatureContext.StopPanTiltContinuousMove()
        {
            _features._continuousControlFeature.StopPanTiltContinuousMove();
        }

        void IInputCoordinatorFeatureContext.StopRoute()
        {
            _features._routesFeature.StopRoute();
        }

        void IInputCoordinatorFeatureContext.TickDigitalEstimate()
        {
            _features._cameraFeatureControlFeature.TickDigitalEstimate();
        }

        void IInputCoordinatorFeatureContext.TickDigitalZoom()
        {
            _features._cameraFeatureControlFeature.TickDigitalZoom();
        }

        void IInputCoordinatorFeatureContext.TickSpecialFeatures()
        {
            _features._specialFeaturesFeature.TickSpecialFeatures();
        }
    }
}
