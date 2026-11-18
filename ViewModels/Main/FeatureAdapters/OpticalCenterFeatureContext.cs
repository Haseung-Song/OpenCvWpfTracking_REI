using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class OpticalCenterFeatureContext : IOpticalCenterFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal OpticalCenterFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        int IOpticalCenterFeatureContext._activePanTiltZoomSource
        {
            get { return _features._continuousControlFeature._activePanTiltZoomSource; }
            set { _features._continuousControlFeature._activePanTiltZoomSource = value; }
        }

        double IOpticalCenterFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        double IOpticalCenterFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        byte[] IOpticalCenterFeatureContext._digitalCaps
        {
            get { return _features._cameraFeatureControlFeature._digitalCaps; }
        }

        bool[] IOpticalCenterFeatureContext._digitalCapsKnown
        {
            get { return _features._cameraFeatureControlFeature._digitalCapsKnown; }
        }

        int[] IOpticalCenterFeatureContext._digitalState
        {
            get { return _features._cameraFeatureControlFeature._digitalState; }
        }

        Services.Configuration.DigitalZoomEstimate IOpticalCenterFeatureContext._eoDigitalEstimate
        {
            get { return _features._cameraFeatureControlFeature._eoDigitalEstimate; }
        }

        Services.Configuration.DigitalZoomEstimate IOpticalCenterFeatureContext._irDigitalEstimate
        {
            get { return _features._cameraFeatureControlFeature._irDigitalEstimate; }
        }

        ControlAgentProfileOption IOpticalCenterFeatureContext._requestedControlProfile
        {
            get { return _features._rtspSettingsFeature._requestedControlProfile; }
            set { _features._rtspSettingsFeature._requestedControlProfile = value; }
        }

        long[] IOpticalCenterFeatureContext._videoCenterFrameTicks
        {
            get { return _features._cameraFeatureControlFeature._videoCenterFrameTicks; }
        }

        ref global::System.Tuple<double, double, long> IOpticalCenterFeatureContext._videoCenterPose
        {
            get { return ref _features._cameraFeatureControlFeature._videoCenterPose; }
        }

        string IOpticalCenterFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IOpticalCenterFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        Services.Configuration.LensControlTuning IOpticalCenterFeatureContext.ControlTuningFor(bool ir)
        {
            return _features._inputCoordinatorFeature.ControlTuningFor(ir);
        }

        string IOpticalCenterFeatureContext.EoConnectionStatusText
        {
            get { return _features._propertiesFeature.EoConnectionStatusText; }
        }

        string IOpticalCenterFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        int IOpticalCenterFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        ushort IOpticalCenterFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        string IOpticalCenterFeatureContext.IrConnectionStatusText
        {
            get { return _features._propertiesFeature.IrConnectionStatusText; }
        }

        string IOpticalCenterFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        bool IOpticalCenterFeatureContext.IsLensFresh(bool ir)
        {
            return _features._inputCoordinatorFeature.IsLensFresh(ir);
        }

        ControlAgentProfileOption IOpticalCenterFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }
    }
}
