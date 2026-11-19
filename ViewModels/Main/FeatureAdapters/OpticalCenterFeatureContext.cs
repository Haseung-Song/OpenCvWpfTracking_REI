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

        int IOpticalCenterFeatureContext.ActivePanTiltZoomSource
        {
            get { return _features.ContinuousControlFeature._activePanTiltZoomSource; }
            set { _features.ContinuousControlFeature._activePanTiltZoomSource = value; }
        }

        double IOpticalCenterFeatureContext.CurrentPan
        {
            get { return _features.ScreenStateFeature._currentPan; }
            set { _features.ScreenStateFeature._currentPan = value; }
        }

        double IOpticalCenterFeatureContext.CurrentTilt
        {
            get { return _features.ScreenStateFeature._currentTilt; }
            set { _features.ScreenStateFeature._currentTilt = value; }
        }

        byte[] IOpticalCenterFeatureContext.DigitalCaps
        {
            get { return _features.CameraFeatureControlFeature._digitalCaps; }
        }

        bool[] IOpticalCenterFeatureContext.DigitalCapsKnown
        {
            get { return _features.CameraFeatureControlFeature._digitalCapsKnown; }
        }

        int[] IOpticalCenterFeatureContext.DigitalState
        {
            get { return _features.CameraFeatureControlFeature._digitalState; }
        }

        Services.Configuration.DigitalZoomEstimate IOpticalCenterFeatureContext.EoDigitalEstimate
        {
            get { return _features.CameraFeatureControlFeature._eoDigitalEstimate; }
        }

        Services.Configuration.DigitalZoomEstimate IOpticalCenterFeatureContext.IrDigitalEstimate
        {
            get { return _features.CameraFeatureControlFeature._irDigitalEstimate; }
        }

        ControlAgentProfileOption IOpticalCenterFeatureContext.RequestedControlProfile
        {
            get { return _features.RtspSettingsFeature._requestedControlProfile; }
            set { _features.RtspSettingsFeature._requestedControlProfile = value; }
        }

        long[] IOpticalCenterFeatureContext.VideoCenterFrameTicks
        {
            get { return _features.CameraFeatureControlFeature._videoCenterFrameTicks; }
        }

        ref global::System.Tuple<double, double, long> IOpticalCenterFeatureContext.VideoCenterPose
        {
            get { return ref _features.CameraFeatureControlFeature._videoCenterPose; }
        }

        string IOpticalCenterFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string IOpticalCenterFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Services.Configuration.LensControlTuning IOpticalCenterFeatureContext.ControlTuningFor(bool ir)
        {
            return _features.InputCoordinatorFeature.ControlTuningFor(ir);
        }

        string IOpticalCenterFeatureContext.EoConnectionStatusText
        {
            get { return _features.PropertiesFeature.EoConnectionStatusText; }
        }

        string IOpticalCenterFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IOpticalCenterFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IOpticalCenterFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        string IOpticalCenterFeatureContext.IrConnectionStatusText
        {
            get { return _features.PropertiesFeature.IrConnectionStatusText; }
        }

        string IOpticalCenterFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        /// <summary>
        /// 연결 요청과 마지막 렌즈 응답 시간을 기준으로 유효성을 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IOpticalCenterFeatureContext.IsLensFresh(bool ir)
        {
            return _features.InputCoordinatorFeature.IsLensFresh(ir);
        }

        ControlAgentProfileOption IOpticalCenterFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }
    }
}
