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

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int ICameraFeatureControlFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features._moveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort ICameraFeatureControlFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// 현재 장비와 줌 위치에 대응하는 십자선 중심을 반환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
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

        /// <summary>
        /// 연결 요청과 마지막 렌즈 응답 시간을 기준으로 유효성을 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool ICameraFeatureControlFeatureContext.IsLensFresh(bool ir)
        {
            return _features._inputCoordinatorFeature.IsLensFresh(ir);
        }

        bool ICameraFeatureControlFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ICameraFeatureControlFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        byte ICameraFeatureControlFeatureContext.PanTiltSpeedLevel
        {
            get { return _features._propertiesFeature.PanTiltSpeedLevel; }
            set { _features._propertiesFeature.PanTiltSpeedLevel = value; }
        }

        /// <summary>
        /// 이전 연결의 프리셋 렌즈 응답 증거를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ICameraFeatureControlFeatureContext.ResetPresetLensEvidence()
        {
            _features._presetLensFeature.ResetPresetLensEvidence();
        }

        /// <summary>
        /// 연결/모드 변경 시 이전 장비 capability와 지연된 UI 응답의 적용을 무효화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ICameraFeatureControlFeatureContext.ResetSpecialFeatureSession()
        {
            _features._specialFeaturesFeature.ResetSpecialFeatureSession();
        }

        /// <summary>
        /// 연결 세대를 변경하고 이전 장비의 AF 상태·대기 요청·실행 확인을 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ICameraFeatureControlFeatureContext.ResetWebAgentControlSession()
        {
            _features._webAgentMotionFocusFeature.ResetWebAgentControlSession();
        }

        ControlAgentProfileOption ICameraFeatureControlFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        /// <summary>
        /// 2026-10-01: 메인 화면의 활성 EO/IR 영상 변경을 PT 감쇄 기준에 반영한다. 신규 WebAgent 프로토콜 없이 기존 PT
        /// 이동 패킷의 속도값만 조정한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ICameraFeatureControlFeatureContext.SetActivePanTiltZoomSource(bool isInfrared)
        {
            _features._continuousControlFeature.SetActivePanTiltZoomSource(isInfrared);
        }

        /// <summary>
        /// 2026-09-15: 화면 중앙 STOP 버튼 전용 PTZF 전체 정지. 현재 GUI 이동 상태와 관계없이 Pelco-D 전체 STOP FF
        /// 01 00 00 00 00 01을 반드시 1회 송신한다. 옥상 CTEC EO 렌즈가 직접 제어 중이면 해당 렌즈도 함께 정지한다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void ICameraFeatureControlFeatureContext.StopAllPtzfMove()
        {
            _features._continuousControlFeature.StopAllPtzfMove();
        }
    }
}
