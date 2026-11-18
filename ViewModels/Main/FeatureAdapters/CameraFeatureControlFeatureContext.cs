using System;
using System.Windows;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class CameraFeatureControlFeatureContext : ICameraFeatureControlFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal CameraFeatureControlFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? ICameraFeatureControlFeatureContext._activePanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activePanAbsoluteTarget; }
        }

        ref double? ICameraFeatureControlFeatureContext._activeTiltAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activeTiltAbsoluteTarget; }
        }

        Services.Communication.IControlCommandServicePort ICameraFeatureControlFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> ICameraFeatureControlFeatureContext._controlInputs
        {
            get { return _features._inputCoordinatorFeature._controlInputs; }
        }

        int ICameraFeatureControlFeatureContext._inputEpoch
        {
            get { return _features._inputCoordinatorFeature._inputEpoch; }
            set { _features._inputCoordinatorFeature._inputEpoch = value; }
        }

        ref long ICameraFeatureControlFeatureContext._irLensReceivedTicks
        {
            get { return ref _features._inputCoordinatorFeature._irLensReceivedTicks; }
        }

        bool ICameraFeatureControlFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        ref double? ICameraFeatureControlFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort ICameraFeatureControlFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ControlAgentProfileOption ICameraFeatureControlFeatureContext._requestedControlProfile
        {
            get { return _features._rtspSettingsFeature._requestedControlProfile; }
            set { _features._rtspSettingsFeature._requestedControlProfile = value; }
        }

        Services.Configuration.LensControlTuning ICameraFeatureControlFeatureContext.ControlTuningFor(bool ir)
        {
            return _features._inputCoordinatorFeature.ControlTuningFor(ir);
        }

        double ICameraFeatureControlFeatureContext.CurrentMoveControlTiltMaximum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMaximum; }
        }

        double ICameraFeatureControlFeatureContext.CurrentMoveControlTiltMinimum
        {
            get { return _features._screenStateFeature.CurrentMoveControlTiltMinimum; }
        }

        string ICameraFeatureControlFeatureContext.EoStatusText
        {
            get { return _features._propertiesFeature.EoStatusText; }
            set { _features._propertiesFeature.EoStatusText = value; }
        }

        int ICameraFeatureControlFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort ICameraFeatureControlFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        Point ICameraFeatureControlFeatureContext.GetOpticalCenter(bool ir)
        {
            return _features._opticalCenterFeature.GetOpticalCenter(ir);
        }

        string ICameraFeatureControlFeatureContext.IrStatusText
        {
            get { return _features._propertiesFeature.IrStatusText; }
            set { _features._propertiesFeature.IrStatusText = value; }
        }

        bool ICameraFeatureControlFeatureContext.IsControlInputLocked
        {
            get { return _features._propertiesFeature.IsControlInputLocked; }
        }

        bool ICameraFeatureControlFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        bool ICameraFeatureControlFeatureContext.IsFovSpeedEnabled
        {
            get { return _features._inputCoordinatorFeature.IsFovSpeedEnabled; }
            set { _features._inputCoordinatorFeature.IsFovSpeedEnabled = value; }
        }

        bool ICameraFeatureControlFeatureContext.IsLensFresh(bool ir)
        {
            return _features._inputCoordinatorFeature.IsLensFresh(ir);
        }

        bool ICameraFeatureControlFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        void ICameraFeatureControlFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        byte ICameraFeatureControlFeatureContext.PanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.PanTiltSpeedLevel; }
            set { _features._propertiesFeature.PanTiltSpeedLevel = value; }
        }

        void ICameraFeatureControlFeatureContext.ResetPresetLensEvidence()
        {
            _features._presetLensFeature.ResetPresetLensEvidence();
        }

        void ICameraFeatureControlFeatureContext.ResetSpecialFeatureSession()
        {
            _features._specialFeaturesFeature.ResetSpecialFeatureSession();
        }

        void ICameraFeatureControlFeatureContext.ResetWebAgentControlSession()
        {
            _features._webAgentMotionFocusFeature.ResetWebAgentControlSession();
        }

        ControlAgentProfileOption ICameraFeatureControlFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        void ICameraFeatureControlFeatureContext.SetActivePanTiltZoomSource(bool isInfrared)
        {
            _features._continuousControlFeature.SetActivePanTiltZoomSource(isInfrared);
        }

        void ICameraFeatureControlFeatureContext.StopAllPtzfMove()
        {
            _features._continuousControlFeature.StopAllPtzfMove();
        }
    }
}
