using OpenCvSharp;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class VideoFeatureContext : IVideoFeatureContext
    {
        private readonly FeatureComposition _features;

        /// <summary>현재 제어 작업의 진입 사유를 전달하고 자동 추적을 종료한다.</summary>
        void IVideoFeatureContext.StopAutoTracking(string reason)
        {
            _features.AutoTrackingFeature.Stop(reason);
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal VideoFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Models.Main.RtspSourceOption IVideoFeatureContext.ActiveEoCtecSource
        {
            get { return _features.ScreenStateFeature._activeEoCtecSource; }
            set { _features.ScreenStateFeature._activeEoCtecSource = value; }
        }

        Models.Main.RtspSourceOption IVideoFeatureContext.ConnectedEoCtecSource
        {
            get { return _features.ScreenStateFeature._connectedEoCtecSource; }
            set { _features.ScreenStateFeature._connectedEoCtecSource = value; }
        }

        CancellationTokenSource IVideoFeatureContext.ControlAgentReconnectCts
        {
            get { return _features.ScreenStateFeature._controlAgentReconnectCts; }
            set { _features.ScreenStateFeature._controlAgentReconnectCts = value; }
        }

        Services.Communication.ICtecCameraResponseServicePort IVideoFeatureContext.CtecCameraResponseService
        {
            get { return _features.Runtime._ctecCameraResponseService; }
        }

        CancellationTokenSource IVideoFeatureContext.Cts
        {
            get { return _features.ScreenStateFeature._cts; }
            set { _features.ScreenStateFeature._cts = value; }
        }

        byte IVideoFeatureContext.CurrentCtecEoFocusMode
        {
            get { return _features.ScreenStateFeature._currentCtecEoFocusMode; }
            set { _features.ScreenStateFeature._currentCtecEoFocusMode = value; }
        }

        ushort IVideoFeatureContext.CurrentCtecEoFocusPosition
        {
            get { return _features.ScreenStateFeature._currentCtecEoFocusPosition; }
            set { _features.ScreenStateFeature._currentCtecEoFocusPosition = value; }
        }

        ushort IVideoFeatureContext.CurrentCtecEoZoomPosition
        {
            get { return _features.ScreenStateFeature._currentCtecEoZoomPosition; }
            set { _features.ScreenStateFeature._currentCtecEoZoomPosition = value; }
        }

        short IVideoFeatureContext.CurrentEoFocus
        {
            get { return _features.ScreenStateFeature._currentEoFocus; }
            set { _features.ScreenStateFeature._currentEoFocus = value; }
        }

        short IVideoFeatureContext.CurrentEoZoom
        {
            get { return _features.ScreenStateFeature._currentEoZoom; }
            set { _features.ScreenStateFeature._currentEoZoom = value; }
        }

        ushort IVideoFeatureContext.CurrentIrFocus
        {
            get { return _features.ScreenStateFeature._currentIrFocus; }
            set { _features.ScreenStateFeature._currentIrFocus = value; }
        }

        ushort IVideoFeatureContext.CurrentIrZoom
        {
            get { return _features.ScreenStateFeature._currentIrZoom; }
            set { _features.ScreenStateFeature._currentIrZoom = value; }
        }

        KeyboardPanTiltDirection IVideoFeatureContext.CurrentKeyboardPanTiltDirection
        {
            get { return _features.ScreenStateFeature._currentKeyboardPanTiltDirection; }
            set { _features.ScreenStateFeature._currentKeyboardPanTiltDirection = value; }
        }

        ContinuousMoveType IVideoFeatureContext.CurrentMoveType
        {
            get { return _features.ScreenStateFeature._currentMoveType; }
            set { _features.ScreenStateFeature._currentMoveType = value; }
        }

        double IVideoFeatureContext.CurrentPan
        {
            get { return _features.ScreenStateFeature._currentPan; }
            set { _features.ScreenStateFeature._currentPan = value; }
        }

        byte IVideoFeatureContext.CurrentPowerStatus
        {
            get { return _features.ScreenStateFeature._currentPowerStatus; }
            set { _features.ScreenStateFeature._currentPowerStatus = value; }
        }

        double IVideoFeatureContext.CurrentTilt
        {
            get { return _features.ScreenStateFeature._currentTilt; }
            set { _features.ScreenStateFeature._currentTilt = value; }
        }

        ref int IVideoFeatureContext.DeviceConnectRequested
        {
            get { return ref _features.RtspSettingsFeature._deviceConnectRequested; }
        }

        IFFmpegDecoderServicePort IVideoFeatureContext.EoDecoder
        {
            get { return _features.Runtime._eoDecoder; }
        }

        ThermalFireDetectionService IVideoFeatureContext.EoFireDetectionService
        {
            get { return _features.ThermalFeature._eoFireDetectionService; }
        }

        ref long IVideoFeatureContext.EoLensReceivedTicks
        {
            get { return ref _features.InputCoordinatorFeature._eoLensReceivedTicks; }
        }

        ref long IVideoPerformanceContext.FocusPropertyChangedCount
        {
            get { return ref _features.ScreenStateFeature._focusPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext.Function01ReceiveCount
        {
            get { return ref _features.ScreenStateFeature._function01ReceiveCount; }
        }

        IFFmpegDecoderServicePort IVideoFeatureContext.IrDecoder
        {
            get { return _features.Runtime._irDecoder; }
        }

        ThermalFireDetectionService IVideoFeatureContext.IrFireDetectionService
        {
            get { return _features.ThermalFeature._irFireDetectionService; }
        }

        ref long IVideoFeatureContext.IrLensReceivedTicks
        {
            get { return ref _features.InputCoordinatorFeature._irLensReceivedTicks; }
        }

        bool IVideoFeatureContext.IsDeviceConnectionRequested
        {
            get { return _features.ScreenStateFeature._isDeviceConnectionRequested; }
            set { _features.ScreenStateFeature._isDeviceConnectionRequested = value; }
        }

        ref int IVideoFeatureContext.IsEoFrameDispatchPending
        {
            get { return ref _features.ScreenStateFeature._isEoFrameDispatchPending; }
        }

        bool IVideoFeatureContext.IsEoFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isEoFrameDisplayed; }
            set { _features.ScreenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IVideoFeatureContext.IsEoRtspAddressValid
        {
            get { return _features.ControlAgentFeature._isEoRtspAddressValid; }
            set { _features.ControlAgentFeature._isEoRtspAddressValid = value; }
        }

        ref int IVideoFeatureContext.IsIrFrameDispatchPending
        {
            get { return ref _features.ScreenStateFeature._isIrFrameDispatchPending; }
        }

        bool IVideoFeatureContext.IsIrFrameDisplayed
        {
            get { return _features.ScreenStateFeature._isIrFrameDisplayed; }
            set { _features.ScreenStateFeature._isIrFrameDisplayed = value; }
        }

        bool IVideoFeatureContext.IsIrRtspAddressValid
        {
            get { return _features.ControlAgentFeature._isIrRtspAddressValid; }
            set { _features.ControlAgentFeature._isIrRtspAddressValid = value; }
        }

        bool IVideoFeatureContext.IsVideoConnecting
        {
            get { return _features.ScreenStateFeature._isVideoConnecting; }
            set { _features.ScreenStateFeature._isVideoConnecting = value; }
        }

        Services.Communication.ILAPacketParserPort IVideoFeatureContext.LaPacketParser
        {
            get { return _features.Runtime._laPacketParser; }
        }

        ref double? IVideoFeatureContext.LastPanAbsoluteTarget
        {
            get { return ref _features.ScreenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort IVideoFeatureContext.LaTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref long IVideoPerformanceContext.PanPropertyChangedCount
        {
            get { return ref _features.ScreenStateFeature._panPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext.PanStartTxCount
        {
            get { return ref _features.ScreenStateFeature._panStartTxCount; }
        }

        ref long IVideoPerformanceContext.PanStopTxCount
        {
            get { return ref _features.ScreenStateFeature._panStopTxCount; }
        }

        ref long IVideoPerformanceContext.PropertyChangedCount
        {
            get { return ref _features.ScreenStateFeature._propertyChangedCount; }
        }

        ref long IVideoPerformanceContext.PtzfStatusUpdateCount
        {
            get { return ref _features.ScreenStateFeature._ptzfStatusUpdateCount; }
        }

        string IVideoPerformanceContext.PtzPerformanceScenario
        {
            get { return _features.ScreenStateFeature._ptzPerformanceScenario; }
            set { _features.ScreenStateFeature._ptzPerformanceScenario = value; }
        }

        string IVideoFeatureContext.RequestedControlIp
        {
            get { return _features.RtspSettingsFeature._requestedControlIp; }
            set { _features.RtspSettingsFeature._requestedControlIp = value; }
        }

        string IVideoFeatureContext.RequestedControlPort
        {
            get { return _features.RtspSettingsFeature._requestedControlPort; }
            set { _features.RtspSettingsFeature._requestedControlPort = value; }
        }

        ControlAgentProfileOption IVideoFeatureContext.RequestedControlProfile
        {
            get { return _features.RtspSettingsFeature._requestedControlProfile; }
            set { _features.RtspSettingsFeature._requestedControlProfile = value; }
        }

        string IVideoFeatureContext.RequestedEoAddress
        {
            get { return _features.RtspSettingsFeature._requestedEoAddress; }
            set { _features.RtspSettingsFeature._requestedEoAddress = value; }
        }

        string IVideoFeatureContext.RequestedIrAddress
        {
            get { return _features.RtspSettingsFeature._requestedIrAddress; }
            set { _features.RtspSettingsFeature._requestedIrAddress = value; }
        }

        ref long IVideoPerformanceContext.TiltPropertyChangedCount
        {
            get { return ref _features.ScreenStateFeature._tiltPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext.TiltStartTxCount
        {
            get { return ref _features.ScreenStateFeature._tiltStartTxCount; }
        }

        ref long IVideoPerformanceContext.TiltStopTxCount
        {
            get { return ref _features.ScreenStateFeature._tiltStopTxCount; }
        }

        long[] IVideoFeatureContext.VideoCenterFrameTicks
        {
            get { return _features.CameraFeatureControlFeature._videoCenterFrameTicks; }
        }

        CancellationTokenSource IVideoFeatureContext.VideoReconnectCts
        {
            get { return _features.ScreenStateFeature._videoReconnectCts; }
            set { _features.ScreenStateFeature._videoReconnectCts = value; }
        }

        ref int IVideoFeatureContext.VisionSettingsGeneration
        {
            get { return ref _features.FireEventsFeature._visionSettingsGeneration; }
        }

        ref long IVideoPerformanceContext.ZoomPropertyChangedCount
        {
            get { return ref _features.ScreenStateFeature._zoomPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext.ZoomStartTxCount
        {
            get { return ref _features.ScreenStateFeature._zoomStartTxCount; }
        }

        ref long IVideoPerformanceContext.ZoomStopTxCount
        {
            get { return ref _features.ScreenStateFeature._zoomStopTxCount; }
        }

        string IVideoFeatureContext.AiPowerStatusText
        {
            get { return _features.PropertiesFeature.AiPowerStatusText; }
            set { _features.PropertiesFeature.AiPowerStatusText = value; }
        }

        /// <summary>
        /// 2026-09-15: REI 통합본에서는 Web Agent 프로필에만 unsigned Pan 좌표를 적용한다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
            _features.RtspSettingsFeature.ApplyControlAgentPanCoordinateMode(profile);
        }

        /// <summary>
        /// 기존 VIA 0 Pan 이동 작업 취소 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.CancelMoveControlPanOperation()
        {
            _features.MoveControlFeature.CancelMoveControlPanOperation();
        }

        /// <summary>
        /// 구역 참조에 사용할 채널 영상 프레임을 안전하게 복사한다.
        /// </summary>
        Services.Position.ZoneFrameContext IVideoFeatureContext.CaptureZoneFrame(string camera)
        {
            return _features.ZonesFeature.CaptureZoneFrame(camera);
        }

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ClearActivePanTiltAbsoluteMove()
        {
            _features.MoveControlFeature.ClearActivePanTiltAbsoluteMove();
        }

        /// <summary>
        /// Keyboard 방향키 입력 상태 초기화 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ClearKeyboardPanTiltPressedState()
        {
            _features.ContinuousControlFeature.ClearKeyboardPanTiltPressedState();
        }

        /// <summary>
        /// [Control Agent] 제어 TCP 연결 기존 고흥 제어 구조는 유지하며, 운용 환경에 따라 연결 대상 IP / Port만 변경하여
        /// 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IVideoFeatureContext.ConnectLaAsync()
        {
            return _features.ControlAgentFeature.ConnectLaAsync();
        }

        string IVideoFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string IVideoFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        /// <summary>
        /// [AI DISCONNECT] 버튼 기준 수동 연결 해제 자동 재연결 요청을 함께 중단하여 사용자가 다시 [AI CONNECT]를 누르기
        /// 전에는 AI Agent에 재접속하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.DisconnectAiAgent()
        {
            _features.AiFeature.DisconnectAiAgent();
        }

        BitmapSource IVideoFeatureContext.EOCameraImage
        {
            get { return _features.PropertiesFeature.EOCameraImage; }
            set { _features.PropertiesFeature.EOCameraImage = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> IVideoFeatureContext.EoDetectionBoxes
        {
            get { return _features.PropertiesFeature.EoDetectionBoxes; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> IVideoFeatureContext.EoFireSmokeDetectionBoxes
        {
            get { return _features.PropertiesFeature.EoFireSmokeDetectionBoxes; }
        }

        string IVideoFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        string IVideoFeatureContext.EoStatusText
        {
            get { return _features.PropertiesFeature.EoStatusText; }
            set { _features.PropertiesFeature.EoStatusText = value; }
        }

        int IVideoFeatureContext.EoVideoHeight
        {
            get { return _features.PropertiesFeature.EoVideoHeight; }
            set { _features.PropertiesFeature.EoVideoHeight = value; }
        }

        int IVideoFeatureContext.EoVideoWidth
        {
            get { return _features.PropertiesFeature.EoVideoWidth; }
            set { _features.PropertiesFeature.EoVideoWidth = value; }
        }

        /// <summary>
        /// 2026-09-02: AI FIRE/FLAME BBox를 영상처리 FIRE 중복 억제에 사용한다. Snapshot은 오래된 좌표가 남지
        /// 않도록 2초까지만 유효하다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        IList<Rect> IVideoFeatureContext.GetRecentAiFireCandidates(bool isInfrared)
        {
            return _features.SmokeFeature.GetRecentAiFireCandidates(isInfrared);
        }

        bool IVideoFeatureContext.HasActiveDeviceSession
        {
            get { return _features.RtspSettingsFeature.HasActiveDeviceSession; }
        }

        string IVideoFeatureContext.HomeZeroStatusText
        {
            get { return _features.PropertiesFeature.HomeZeroStatusText; }
            set { _features.PropertiesFeature.HomeZeroStatusText = value; }
        }

        /// <summary>
        /// 장비 연결 후 IR 기본 팔레트 Black Hot 적용을 준비한다.
        /// </summary>
        void IVideoFeatureContext.InitializeThermalBlackHotAfterDeviceConnected()
        {
            _features.ThermalFeature.InitializeThermalBlackHotAfterDeviceConnected();
        }

        /// <summary>
        /// 새 WEB AGENT 연결의 조회·상태 관리 세션을 초기화한다.
        /// </summary>
        void IVideoFeatureContext.InitializeWebAgentSession()
        {
            _features.WebAgentProtocolFeature.InitializeWebAgentSession();
        }

        BitmapSource IVideoFeatureContext.IRCameraImage
        {
            get { return _features.PropertiesFeature.IRCameraImage; }
            set { _features.PropertiesFeature.IRCameraImage = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> IVideoFeatureContext.IrDetectionBoxes
        {
            get { return _features.PropertiesFeature.IrDetectionBoxes; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> IVideoFeatureContext.IrFireSmokeDetectionBoxes
        {
            get { return _features.PropertiesFeature.IrFireSmokeDetectionBoxes; }
        }

        string IVideoFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        string IVideoFeatureContext.IrStatusText
        {
            get { return _features.PropertiesFeature.IrStatusText; }
            set { _features.PropertiesFeature.IrStatusText = value; }
        }

        int IVideoFeatureContext.IrVideoHeight
        {
            get { return _features.PropertiesFeature.IrVideoHeight; }
            set { _features.PropertiesFeature.IrVideoHeight = value; }
        }

        int IVideoFeatureContext.IrVideoWidth
        {
            get { return _features.PropertiesFeature.IrVideoWidth; }
            set { _features.PropertiesFeature.IrVideoWidth = value; }
        }

        bool IVideoFeatureContext.IsCrosshairVisible
        {
            get { return _features.PropertiesFeature.IsCrosshairVisible; }
            set { _features.PropertiesFeature.IsCrosshairVisible = value; }
        }

        bool IVideoFeatureContext.IsDeviceSettingsLocked
        {
            get { return _features.RtspSettingsFeature.IsDeviceSettingsLocked; }
        }

        /// <summary>
        /// 2026-08-28: AUTO SCAN과 파노라마 촬영 중에도 FIRE/SMOKE 분석 및 이벤트 갱신을 유지하고, 일반 수동 이동 중에만
        /// 분석을 보류한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IVideoFeatureContext.IsFireSmokeFrameAnalysisAllowed()
        {
            return _features.SmokeFeature.IsFireSmokeFrameAnalysisAllowed();
        }

        /// <summary>
        /// 지정한 영상 채널과 탐지 종류가 현재 사용 대상인지 확인한다.
        /// </summary>
        bool IVideoFeatureContext.IsFireSourceEnabled(bool isInfrared)
        {
            return _features.ThermalFeature.IsFireSourceEnabled(isInfrared);
        }

        bool IVideoFeatureContext.IsPanoramaCaptureRunning
        {
            get { return _features.PanoramaFeature.IsPanoramaCaptureRunning; }
            set { _features.PanoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        bool IVideoFeatureContext.IsPresetScanRunning
        {
            get { return _features.PropertiesFeature.IsPresetScanRunning; }
            set { _features.PropertiesFeature.IsPresetScanRunning = value; }
        }

        bool IVideoFeatureContext.IsRooftopStatusSelected
        {
            get { return _features.PropertiesFeature.IsRooftopStatusSelected; }
        }

        bool IVideoFeatureContext.IsSmokeDetectionEnabled
        {
            get { return _features.SmokeFeature.IsSmokeDetectionEnabled; }
            set { _features.SmokeFeature.IsSmokeDetectionEnabled = value; }
        }

        bool IVideoFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features.ThermalFeature.IsThermalFireDetectionEnabled; }
            set { _features.ThermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        /// <summary>
        /// 현재 설정된 Pan / Tilt 좌표계의 0°, 0° 위치로 절대 이동한다. 기존 LA 0xB1 명령은 기계식
        /// Homing(StartHoming) 명령이므로 카메라가 홈 센서 방향으로 이동한 뒤 해당 위치를 원점으로 잡는다. UI의 HOME
        /// POSITION은 사용자가 설정한 좌표 원점으로 복귀하는 기능이므로 Pan 0°(0x45), Tilt 0°(0x47) 절대 이동 명령을
        /// 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IVideoFeatureContext.MoveHomePositionAsync()
        {
            return _features.MoveControlFeature.MoveHomePositionAsync();
        }

        /// <summary>
        /// EO 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IVideoFeatureContext.NotifyEoCurrentStatusChanged(bool panChanged, bool tiltChanged, bool zoomChanged, bool focusChanged, bool powerChanged)
        {
            _features.ControlAgentFeature.NotifyEoCurrentStatusChanged(panChanged, tiltChanged, zoomChanged, focusChanged, powerChanged);
        }

        /// <summary>
        /// IR 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IVideoFeatureContext.NotifyIrCurrentStatusChanged()
        {
            _features.ControlAgentFeature.NotifyIrCurrentStatusChanged();
        }

        string IVideoFeatureContext.PresetCommandStatusText
        {
            get { return _features.PropertiesFeature.PresetCommandStatusText; }
            set { _features.PropertiesFeature.PresetCommandStatusText = value; }
        }

        /// <summary>
        /// 현재 영상 프레임을 연기 탐지기로 분석하고 표시·이벤트 갱신에 전달한다.
        /// </summary>
        SmokeDetectionResult IVideoFeatureContext.ProcessSmokeFrame(Mat frame, string streamName, Rect fireCandidateRect)
        {
            return _features.SmokeFeature.ProcessSmokeFrame(frame, streamName, fireCandidateRect);
        }

        /// <summary>
        /// 연결 세션 변경 시 이전 전원 근거와 진행 중 명령 상태를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ResetDevicePowerStatus()
        {
            _features.DevicePowerFeature.ResetDevicePowerStatus();
        }

        /// <summary>
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ResetDigitalZoomSession()
        {
            _features.CameraFeatureControlFeature.ResetDigitalZoomSession();
        }

        /// <summary>
        /// 2026-08-27: 카메라 이동 또는 영상 조건 변경 뒤 EO/IR 기준 프레임과 시간축 후보를 함께 초기화하여 화면 전체 변화가 이벤트가
        /// 되지 않게 한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ResetFireSmokeFrameAnalysis(string reason)
        {
            _features.SmokeFeature.ResetFireSmokeFrameAnalysis(reason);
        }

        /// <summary>
        /// 현재 선택값을 다음 연결용 설정으로 저장하며 로그의 인증정보는 가린다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.SaveRtspCommunicationSettings()
        {
            _features.RtspSettingsFeature.SaveRtspCommunicationSettings();
        }

        ControlAgentProfileOption IVideoFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        string IVideoFeatureContext.SessionEoAddress
        {
            get { return _features.RtspSettingsFeature.SessionEoAddress; }
        }

        string IVideoFeatureContext.SessionIrAddress
        {
            get { return _features.RtspSettingsFeature.SessionIrAddress; }
        }

        /// <summary>
        /// Control Agent TCP 연결 상태 UI 갱신 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.SetControlAgentConnectionStatus(string statusText, string statusColor, CancellationToken token)
        {
            _features.ControlAgentFeature.SetControlAgentConnectionStatus(statusText, statusColor, token);
        }

        /// <summary>
        /// 로컬 화재·연기 탐지의 사용 여부를 관련 상태에 반영한다.
        /// </summary>
        void IVideoFeatureContext.SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke)
        {
            _features.PropertiesFeature.SetLocalFireSmokeDetectionState(rtspIndex, hasFire, hasSmoke);
        }

        /// <summary>
        /// 2026-08-31: 탐지 Overlay 적용 전 EO 프레임을 파노라마 전용 버퍼에 보관한다. Frozen Bitmap만 공유하여 수신
        /// Thread와 촬영 Thread 간 접근을 안전하게 한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.SetRawEoPanoramaFrame(BitmapSource frame)
        {
            _features.PanoramaFeature.SetRawEoPanoramaFrame(frame);
        }

        /// <summary>
        /// 활성 세션을 변경하지 않고 중복 연결 안내만 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ShowAlreadyConnected()
        {
            _features.RtspSettingsFeature.ShowAlreadyConnected();
        }

        /// <summary>
        /// 현재 선택된 EO 프리셋 기준으로 CTEC Response TCP Port 9000 연결 시작 옥상 MR300 EO CTEC 직접 제어
        /// 프리셋이 아니면 기존 Response 연결을 종료하고 별도 TCP 연결을 생성하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IVideoFeatureContext.StartSelectedEoCtecResponseAsync()
        {
            return _features.ContinuousControlFeature.StartSelectedEoCtecResponseAsync();
        }

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 종료 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.StopCtecEoPositionPolling()
        {
            _features.ContinuousControlFeature.StopCtecEoPositionPolling();
        }

        /// <summary>
        /// 진행 중인 Focus Sync 확인 작업과 장비 이동을 정지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IVideoFeatureContext.StopFocusSyncAsync(bool stopEo, bool stopIr)
        {
            return _features.SyncFeature.StopFocusSyncAsync(stopEo, stopIr);
        }

        /// <summary>
        /// 경로 실행 취소를 요청하고 진행 중 이동을 정리한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.StopRoute()
        {
            _features.RoutesFeature.StopRoute();
        }

        /// <summary>
        /// 진행 중인 줌 동기화를 취소하고 관련 렌즈 이동을 중지한다.
        /// </summary>
        Task IVideoFeatureContext.StopZoomSyncAsync()
        {
            return _features.SyncFeature.StopZoomSyncAsync();
        }

        int IVideoFeatureContext.ThermalFireBoxGroupingMode
        {
            get { return _features.ThermalFeature.ThermalFireBoxGroupingMode; }
        }

        double IVideoFeatureContext.ThermalHotThresholdRatio
        {
            get { return _features.ThermalFeature.ThermalHotThresholdRatio; }
            set { _features.ThermalFeature.ThermalHotThresholdRatio = value; }
        }

        double IVideoFeatureContext.ThermalMinimumAreaRatio
        {
            get { return _features.ThermalFeature.ThermalMinimumAreaRatio; }
            set { _features.ThermalFeature.ThermalMinimumAreaRatio = value; }
        }

        /// <summary>
        /// 통신 설정 탭에서 선택된 EO / IR RTSP 주소 검증 선택값이 없거나 rtsp / rtsps 형식이 아닌 주소는 실제 FFmpeg 연결
        /// 전에 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IVideoFeatureContext.TryGetRtspEndpoints(out string eoRtspAddress, out string irRtspAddress)
        {
            return _features.ControlAgentFeature.TryGetRtspEndpoints(out eoRtspAddress, out irRtspAddress);
        }

        /// <summary>
        /// 2026-09-17: 로컬 FIRE/SMOKE 분석 결과를 영상 오버레이 컬렉션에 반영한다. 이벤트 목록만 갱신되고 영상 BBox가 누락되던
        /// 경로를 복구하며, AI Agent BBox 컬렉션은 변경하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.UpdateFireSmokeDetectionOverlay(string camera, ThermalFireDetectionResult thermalResult, SmokeDetectionResult smokeResult)
        {
            _features.FireEventsFeature.UpdateFireSmokeDetectionOverlay(camera, thermalResult, smokeResult);
        }

        /// <summary>
        /// 열상 화재 후보의 위치·확정 상태를 표시와 이벤트 판정에 반영한다.
        /// </summary>
        void IVideoFeatureContext.UpdateThermalFireCandidateState(string camera, ThermalFireDetectionResult result, Services.Position.ZoneFrameContext zoneFrame)
        {
            _features.ThermalFeature.UpdateThermalFireCandidateState(camera, result, zoneFrame);
        }

        /// <summary>
        /// 로컬 영상 후보와 기존 추적 이벤트를 대응시켜 생성·유지·해제를 반영한다.
        /// </summary>
        void IVideoFeatureContext.UpdateVisionBBoxEvents(string camera, string detectionType, IList<Rect> candidates, IList<double> candidateScores, string detectionSource, Services.Position.ZoneFrameContext zoneFrame)
        {
            _features.FireEventsFeature.UpdateVisionBBoxEvents(camera, detectionType, candidates, candidateScores, detectionSource, zoneFrame);
        }
    }
}
