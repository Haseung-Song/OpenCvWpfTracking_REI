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

        bool IInputCoordinatorFeatureContext.IsAutoTrackingActive
        {
            get { return _features.PropertiesFeature.IsAutoTrackingEnabled; }
        }

        /// <summary>현재 제어 작업의 진입 사유를 전달하고 자동 추적을 종료한다.</summary>
        void IInputCoordinatorFeatureContext.StopAutoTracking(string reason)
        {
            _features.AutoTrackingFeature.Stop(reason);
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal InputCoordinatorFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref global::OpenCvWpfTracking.Models.Main.RtspSourceOption IInputCoordinatorFeatureContext.ActiveEoCtecSource
        {
            get { return ref _features.ScreenStateFeature._activeEoCtecSource; }
        }

        ref double? IInputCoordinatorFeatureContext.ActivePanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activePanAbsoluteTarget; }
        }

        ref double? IInputCoordinatorFeatureContext.ActiveTiltAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._activeTiltAbsoluteTarget; }
        }

        Services.Communication.AI.IAiDetectorClientServicePort IInputCoordinatorFeatureContext.AiDetectorClientService
        {
            get { return _features.Runtime._aiDetectorClientService; }
        }

        Services.Communication.IControlCommandServicePort IInputCoordinatorFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        ContinuousMoveType IInputCoordinatorFeatureContext.CurrentMoveType
        {
            get { return _features.ScreenStateFeature._currentMoveType; }
            set { _features.ScreenStateFeature._currentMoveType = value; }
        }

        bool IInputCoordinatorFeatureContext.IsDeviceConnectionRequested
        {
            get { return _features.ScreenStateFeature._isDeviceConnectionRequested; }
            set { _features.ScreenStateFeature._isDeviceConnectionRequested = value; }
        }

        Services.Communication.ITcpClientServicePort IInputCoordinatorFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ControlAgentProfileOption IInputCoordinatorFeatureContext.RequestedControlProfile
        {
            get { return _features.RtspSettingsFeature._requestedControlProfile; }
            set { _features.RtspSettingsFeature._requestedControlProfile = value; }
        }

        bool IInputCoordinatorFeatureContext.VideoCenterBusy
        {
            get { return _features.CameraFeatureControlFeature._videoCenterBusy; }
            set { _features.CameraFeatureControlFeature._videoCenterBusy = value; }
        }

        string IInputCoordinatorFeatureContext.AiPowerStatusText
        {
            get { return _features.PropertiesFeature.AiPowerStatusText; }
            set { _features.PropertiesFeature.AiPowerStatusText = value; }
        }

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.ClearActivePanTiltAbsoluteMove()
        {
            _features.MoveControlFeature.ClearActivePanTiltAbsoluteMove();
        }

        /// <summary>
        /// 2026-08-25: AI 연결 전환 또는 해제 시 화면 BBox와 ACTIVE 이벤트 상태를 UI Dispatcher에서 함께 정리하여
        /// 연결이 끊긴 뒤 이전 탐지 결과가 남지 않게 한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.ClearAiDetectionState(string reason, bool writeAudit)
        {
            _features.AiFeature.ClearAiDetectionState(reason, writeAudit);
        }

        byte IInputCoordinatorFeatureContext.EffectivePanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.EffectivePanTiltSpeedLevel; }
        }

        double IInputCoordinatorFeatureContext.EoDisplayZoomRatio
        {
            get { return _features.CameraFeatureControlFeature.EoDisplayZoomRatio; }
        }

        string IInputCoordinatorFeatureContext.EoPowerControlStatusText
        {
            get { return _features.DevicePowerFeature.EoPowerControlStatusText; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IInputCoordinatorFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IInputCoordinatorFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        double IInputCoordinatorFeatureContext.IrDisplayZoomRatio
        {
            get { return _features.CameraFeatureControlFeature.IrDisplayZoomRatio; }
        }

        string IInputCoordinatorFeatureContext.IrPowerControlStatusText
        {
            get { return _features.DevicePowerFeature.IrPowerControlStatusText; }
        }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IInputCoordinatorFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features.WebAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IInputCoordinatorFeatureContext.IsControlInputLocked
        {
            get { return _features.PropertiesFeature.IsControlInputLocked; }
        }

        bool IInputCoordinatorFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IInputCoordinatorFeatureContext.PanTiltSpeedDisplayText
        {
            get { return _features.PropertiesFeature.PanTiltSpeedDisplayText; }
        }

        byte IInputCoordinatorFeatureContext.PanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.PanTiltSpeedLevel; }
            set { _features.PropertiesFeature.PanTiltSpeedLevel = value; }
        }

        /// <summary>
        /// 접속 및 RTSP URL 변경 때 이전 원본 크기를 무효화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.ResetAiRtspVideoSizes()
        {
            _features.AiFeature.ResetAiRtspVideoSizes();
        }

        ControlAgentProfileOption IInputCoordinatorFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        /// <summary>
        /// 2026-10-01: 메인 화면의 활성 EO/IR 영상 변경을 PT 감쇄 기준에 반영한다. 신규 WebAgent 프로토콜 없이 기존 PT
        /// 이동 패킷의 속도값만 조정한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.SetActivePanTiltZoomSource(bool isInfrared)
        {
            _features.ContinuousControlFeature.SetActivePanTiltZoomSource(isInfrared);
        }

        /// <summary>
        /// AI 연결 상태에 맞춰 로컬 화재·연기 탐지기 사용 상태를 조정한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.SetFireSmokeDetectorsForAiConnection(bool enabled, string reason)
        {
            _features.SmokeFeature.SetFireSmokeDetectorsForAiConnection(enabled, reason);
        }

        /// <summary>
        /// EO 초점을 Far 방향으로 연속 이동하기 시작한다.
        /// </summary>
        Task IInputCoordinatorFeatureContext.StartEoFocusFarMoveAsync()
        {
            return _features.ContinuousControlFeature.StartEoFocusFarMoveAsync();
        }

        /// <summary>
        /// EO 초점을 Near 방향으로 연속 이동하기 시작한다.
        /// </summary>
        Task IInputCoordinatorFeatureContext.StartEoFocusNearMoveAsync()
        {
            return _features.ContinuousControlFeature.StartEoFocusNearMoveAsync();
        }

        /// <summary>
        /// EO 줌 확대 연속 이동을 시작한다.
        /// </summary>
        Task IInputCoordinatorFeatureContext.StartEoZoomInMoveAsync()
        {
            return _features.ContinuousControlFeature.StartEoZoomInMoveAsync();
        }

        /// <summary>
        /// EO 줌 축소 연속 이동을 시작한다.
        /// </summary>
        Task IInputCoordinatorFeatureContext.StartEoZoomOutMoveAsync()
        {
            return _features.ContinuousControlFeature.StartEoZoomOutMoveAsync();
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Far] 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartIrFocusFarMove()
        {
            _features.ContinuousControlFeature.StartIrFocusFarMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Near] 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartIrFocusNearMove()
        {
            _features.ContinuousControlFeature.StartIrFocusNearMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Tele] 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartIrZoomInMove()
        {
            _features.ContinuousControlFeature.StartIrZoomInMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Wide] 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartIrZoomOutMove()
        {
            _features.ContinuousControlFeature.StartIrZoomOutMove();
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 좌측 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 좌측 방향으로 연속 이동
        /// 명령을 송신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartPanLeftMove()
        {
            _features.ContinuousControlFeature.StartPanLeftMove();
        }

        /// <summary>
        /// [EO/IR] 좌측 하단 대각선 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartPanLeftTiltDownMove()
        {
            _features.ContinuousControlFeature.StartPanLeftTiltDownMove();
        }

        /// <summary>
        /// [EO/IR] 좌측 상단 대각선 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartPanLeftTiltUpMove()
        {
            _features.ContinuousControlFeature.StartPanLeftTiltUpMove();
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 우측 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 우측 방향으로 연속 이동
        /// 명령을 송신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartPanRightMove()
        {
            _features.ContinuousControlFeature.StartPanRightMove();
        }

        /// <summary>
        /// [EO/IR] 우측 하단 대각선 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartPanRightTiltDownMove()
        {
            _features.ContinuousControlFeature.StartPanRightTiltDownMove();
        }

        /// <summary>
        /// [EO/IR] 우측 상단 대각선 연속 이동 시작 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartPanRightTiltUpMove()
        {
            _features.ContinuousControlFeature.StartPanRightTiltUpMove();
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 아래쪽 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 아래 방향으로 연속
        /// 이동 명령을 송신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartTiltDownMove()
        {
            _features.ContinuousControlFeature.StartTiltDownMove();
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 위쪽 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 위쪽 방향으로 연속 이동
        /// 명령을 송신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StartTiltUpMove()
        {
            _features.ContinuousControlFeature.StartTiltUpMove();
        }

        /// <summary>
        /// 2026-09-15: 화면 중앙 STOP 버튼 전용 PTZF 전체 정지. 현재 GUI 이동 상태와 관계없이 Pelco-D 전체 STOP FF
        /// 01 00 00 00 00 01을 반드시 1회 송신한다. 옥상 CTEC EO 렌즈가 직접 제어 중이면 해당 렌즈도 함께 정지한다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StopAllPtzfMove()
        {
            _features.ContinuousControlFeature.StopAllPtzfMove();
        }

        /// <summary>
        /// 현재 연속 이동을 중지하고 필요한 위치 응답의 안정화를 확인한다.
        /// </summary>
        Task IInputCoordinatorFeatureContext.StopContinuousMoveAsync(bool waitForStatus)
        {
            return _features.ContinuousControlFeature.StopContinuousMoveAsync(waitForStatus);
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] 연속 이동 정지 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StopIrFocusMove()
        {
            _features.ContinuousControlFeature.StopIrFocusMove();
        }

        /// <summary>
        /// [IR] [ZOOM] 연속 이동 정지 IR Zoom 버튼 [MouseUp] 시에만 호출한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StopIrZoomMove()
        {
            _features.ContinuousControlFeature.StopIrZoomMove();
        }

        /// <summary>
        /// Pan·Tilt 연속 이동을 중지한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StopPanTiltContinuousMove()
        {
            _features.ContinuousControlFeature.StopPanTiltContinuousMove();
        }

        /// <summary>
        /// 경로 실행 취소를 요청하고 진행 중 이동을 정리한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.StopRoute()
        {
            _features.RoutesFeature.StopRoute();
        }

        /// <summary>
        /// 단조 시계의 경과 시간을 계산해 EO/IR 디지털 추정 모델을 함께 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.TickDigitalEstimate()
        {
            _features.CameraFeatureControlFeature.TickDigitalEstimate();
        }

        /// <summary>
        /// 클릭 이동 기한, D-ZOOM 응답 대기 및 주기 조회를 기존 간격으로 감시한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.TickDigitalZoom()
        {
            _features.CameraFeatureControlFeature.TickDigitalZoom();
        }

        /// <summary>
        /// 기존 250ms UI 타이머를 사용한다. 일괄 조회도 한 틱당 한 건으로 제한한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IInputCoordinatorFeatureContext.TickSpecialFeatures()
        {
            _features.SpecialFeaturesFeature.TickSpecialFeatures();
        }
    }
}
