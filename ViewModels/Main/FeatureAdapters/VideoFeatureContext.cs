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

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal VideoFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Models.Main.RtspSourceOption IVideoFeatureContext._activeEoCtecSource
        {
            get { return _features._screenStateFeature._activeEoCtecSource; }
            set { _features._screenStateFeature._activeEoCtecSource = value; }
        }

        Models.Main.RtspSourceOption IVideoFeatureContext._connectedEoCtecSource
        {
            get { return _features._screenStateFeature._connectedEoCtecSource; }
            set { _features._screenStateFeature._connectedEoCtecSource = value; }
        }

        CancellationTokenSource IVideoFeatureContext._controlAgentReconnectCts
        {
            get { return _features._screenStateFeature._controlAgentReconnectCts; }
            set { _features._screenStateFeature._controlAgentReconnectCts = value; }
        }

        Services.Communication.ICtecCameraResponseServicePort IVideoFeatureContext._ctecCameraResponseService
        {
            get { return _features.Runtime._ctecCameraResponseService; }
        }

        CancellationTokenSource IVideoFeatureContext._cts
        {
            get { return _features._screenStateFeature._cts; }
            set { _features._screenStateFeature._cts = value; }
        }

        byte IVideoFeatureContext._currentCtecEoFocusMode
        {
            get { return _features._screenStateFeature._currentCtecEoFocusMode; }
            set { _features._screenStateFeature._currentCtecEoFocusMode = value; }
        }

        ushort IVideoFeatureContext._currentCtecEoFocusPosition
        {
            get { return _features._screenStateFeature._currentCtecEoFocusPosition; }
            set { _features._screenStateFeature._currentCtecEoFocusPosition = value; }
        }

        ushort IVideoFeatureContext._currentCtecEoZoomPosition
        {
            get { return _features._screenStateFeature._currentCtecEoZoomPosition; }
            set { _features._screenStateFeature._currentCtecEoZoomPosition = value; }
        }

        short IVideoFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IVideoFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IVideoFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort IVideoFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        KeyboardPanTiltDirection IVideoFeatureContext._currentKeyboardPanTiltDirection
        {
            get { return _features._screenStateFeature._currentKeyboardPanTiltDirection; }
            set { _features._screenStateFeature._currentKeyboardPanTiltDirection = value; }
        }

        ContinuousMoveType IVideoFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        double IVideoFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        byte IVideoFeatureContext._currentPowerStatus
        {
            get { return _features._screenStateFeature._currentPowerStatus; }
            set { _features._screenStateFeature._currentPowerStatus = value; }
        }

        double IVideoFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        ref int IVideoFeatureContext._deviceConnectRequested
        {
            get { return ref _features._rtspSettingsFeature._deviceConnectRequested; }
        }

        IFFmpegDecoderServicePort IVideoFeatureContext._eoDecoder
        {
            get { return _features.Runtime._eoDecoder; }
        }

        ThermalFireDetectionService IVideoFeatureContext._eoFireDetectionService
        {
            get { return _features._thermalFeature._eoFireDetectionService; }
        }

        ref long IVideoFeatureContext._eoLensReceivedTicks
        {
            get { return ref _features._inputCoordinatorFeature._eoLensReceivedTicks; }
        }

        ref long IVideoPerformanceContext._focusPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._focusPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._function01ReceiveCount
        {
            get { return ref _features._screenStateFeature._function01ReceiveCount; }
        }

        IFFmpegDecoderServicePort IVideoFeatureContext._irDecoder
        {
            get { return _features.Runtime._irDecoder; }
        }

        ThermalFireDetectionService IVideoFeatureContext._irFireDetectionService
        {
            get { return _features._thermalFeature._irFireDetectionService; }
        }

        ref long IVideoFeatureContext._irLensReceivedTicks
        {
            get { return ref _features._inputCoordinatorFeature._irLensReceivedTicks; }
        }

        bool IVideoFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        ref int IVideoFeatureContext._isEoFrameDispatchPending
        {
            get { return ref _features._screenStateFeature._isEoFrameDispatchPending; }
        }

        bool IVideoFeatureContext._isEoFrameDisplayed
        {
            get { return _features._screenStateFeature._isEoFrameDisplayed; }
            set { _features._screenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IVideoFeatureContext._isEoRtspAddressValid
        {
            get { return _features._controlAgentFeature._isEoRtspAddressValid; }
            set { _features._controlAgentFeature._isEoRtspAddressValid = value; }
        }

        ref int IVideoFeatureContext._isIrFrameDispatchPending
        {
            get { return ref _features._screenStateFeature._isIrFrameDispatchPending; }
        }

        bool IVideoFeatureContext._isIrFrameDisplayed
        {
            get { return _features._screenStateFeature._isIrFrameDisplayed; }
            set { _features._screenStateFeature._isIrFrameDisplayed = value; }
        }

        bool IVideoFeatureContext._isIrRtspAddressValid
        {
            get { return _features._controlAgentFeature._isIrRtspAddressValid; }
            set { _features._controlAgentFeature._isIrRtspAddressValid = value; }
        }

        bool IVideoFeatureContext._isVideoConnecting
        {
            get { return _features._screenStateFeature._isVideoConnecting; }
            set { _features._screenStateFeature._isVideoConnecting = value; }
        }

        Services.Communication.ILAPacketParserPort IVideoFeatureContext._laPacketParser
        {
            get { return _features.Runtime._laPacketParser; }
        }

        ref double? IVideoFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort IVideoFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref long IVideoPerformanceContext._panPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._panPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._panStartTxCount
        {
            get { return ref _features._screenStateFeature._panStartTxCount; }
        }

        ref long IVideoPerformanceContext._panStopTxCount
        {
            get { return ref _features._screenStateFeature._panStopTxCount; }
        }

        ref long IVideoPerformanceContext._propertyChangedCount
        {
            get { return ref _features._screenStateFeature._propertyChangedCount; }
        }

        ref long IVideoPerformanceContext._ptzfStatusUpdateCount
        {
            get { return ref _features._screenStateFeature._ptzfStatusUpdateCount; }
        }

        string IVideoPerformanceContext._ptzPerformanceScenario
        {
            get { return _features._screenStateFeature._ptzPerformanceScenario; }
            set { _features._screenStateFeature._ptzPerformanceScenario = value; }
        }

        string IVideoFeatureContext._requestedControlIp
        {
            get { return _features._rtspSettingsFeature._requestedControlIp; }
            set { _features._rtspSettingsFeature._requestedControlIp = value; }
        }

        string IVideoFeatureContext._requestedControlPort
        {
            get { return _features._rtspSettingsFeature._requestedControlPort; }
            set { _features._rtspSettingsFeature._requestedControlPort = value; }
        }

        ControlAgentProfileOption IVideoFeatureContext._requestedControlProfile
        {
            get { return _features._rtspSettingsFeature._requestedControlProfile; }
            set { _features._rtspSettingsFeature._requestedControlProfile = value; }
        }

        string IVideoFeatureContext._requestedEoAddress
        {
            get { return _features._rtspSettingsFeature._requestedEoAddress; }
            set { _features._rtspSettingsFeature._requestedEoAddress = value; }
        }

        string IVideoFeatureContext._requestedIrAddress
        {
            get { return _features._rtspSettingsFeature._requestedIrAddress; }
            set { _features._rtspSettingsFeature._requestedIrAddress = value; }
        }

        ref long IVideoPerformanceContext._tiltPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._tiltPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._tiltStartTxCount
        {
            get { return ref _features._screenStateFeature._tiltStartTxCount; }
        }

        ref long IVideoPerformanceContext._tiltStopTxCount
        {
            get { return ref _features._screenStateFeature._tiltStopTxCount; }
        }

        long[] IVideoFeatureContext._videoCenterFrameTicks
        {
            get { return _features._cameraFeatureControlFeature._videoCenterFrameTicks; }
        }

        CancellationTokenSource IVideoFeatureContext._videoReconnectCts
        {
            get { return _features._screenStateFeature._videoReconnectCts; }
            set { _features._screenStateFeature._videoReconnectCts = value; }
        }

        ref int IVideoFeatureContext._visionSettingsGeneration
        {
            get { return ref _features._fireEventsFeature._visionSettingsGeneration; }
        }

        ref long IVideoPerformanceContext._zoomPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._zoomPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._zoomStartTxCount
        {
            get { return ref _features._screenStateFeature._zoomStartTxCount; }
        }

        ref long IVideoPerformanceContext._zoomStopTxCount
        {
            get { return ref _features._screenStateFeature._zoomStopTxCount; }
        }

        string IVideoFeatureContext.AiPowerStatusText
        {
            get { return _features._propertiesFeature.AiPowerStatusText; }
            set { _features._propertiesFeature.AiPowerStatusText = value; }
        }

        /// <summary>
        /// 2026-09-15: REI 통합본에서는 Web Agent 프로필에만 unsigned Pan 좌표를 적용한다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
            _features._rtspSettingsFeature.ApplyControlAgentPanCoordinateMode(profile);
        }

        /// <summary>
        /// 기존 VIA 0 Pan 이동 작업 취소 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.CancelMoveControlPanOperation()
        {
            _features._moveControlFeature.CancelMoveControlPanOperation();
        }

        /// <summary>
        /// 구역 참조에 사용할 채널 영상 프레임을 안전하게 복사한다.
        /// </summary>
        Services.Position.ZoneFrameContext IVideoFeatureContext.CaptureZoneFrame(string camera)
        {
            return _features._zonesFeature.CaptureZoneFrame(camera);
        }

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ClearActivePanTiltAbsoluteMove()
        {
            _features._moveControlFeature.ClearActivePanTiltAbsoluteMove();
        }

        /// <summary>
        /// Keyboard 방향키 입력 상태 초기화 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ClearKeyboardPanTiltPressedState()
        {
            _features._continuousControlFeature.ClearKeyboardPanTiltPressedState();
        }

        /// <summary>
        /// [Control Agent] 제어 TCP 연결 기존 고흥 제어 구조는 유지하며, 운용 환경에 따라 연결 대상 IP / Port만 변경하여
        /// 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IVideoFeatureContext.ConnectLaAsync()
        {
            return _features._controlAgentFeature.ConnectLaAsync();
        }

        string IVideoFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IVideoFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        /// <summary>
        /// [AI DISCONNECT] 버튼 기준 수동 연결 해제 자동 재연결 요청을 함께 중단하여 사용자가 다시 [AI CONNECT]를 누르기
        /// 전에는 AI Agent에 재접속하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.DisconnectAiAgent()
        {
            _features._aiFeature.DisconnectAiAgent();
        }

        BitmapSource IVideoFeatureContext.EOCameraImage
        {
            get { return _features._propertiesFeature.EOCameraImage; }
            set { _features._propertiesFeature.EOCameraImage = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> IVideoFeatureContext.EoDetectionBoxes
        {
            get { return _features._propertiesFeature.EoDetectionBoxes; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> IVideoFeatureContext.EoFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.EoFireSmokeDetectionBoxes; }
        }

        string IVideoFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        string IVideoFeatureContext.EoStatusText
        {
            get { return _features._propertiesFeature.EoStatusText; }
            set { _features._propertiesFeature.EoStatusText = value; }
        }

        int IVideoFeatureContext.EoVideoHeight
        {
            get { return _features._propertiesFeature.EoVideoHeight; }
            set { _features._propertiesFeature.EoVideoHeight = value; }
        }

        int IVideoFeatureContext.EoVideoWidth
        {
            get { return _features._propertiesFeature.EoVideoWidth; }
            set { _features._propertiesFeature.EoVideoWidth = value; }
        }

        /// <summary>
        /// 2026-09-02: AI FIRE/FLAME BBox를 영상처리 FIRE 중복 억제에 사용한다. Snapshot은 오래된 좌표가 남지
        /// 않도록 2초까지만 유효하다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        IList<Rect> IVideoFeatureContext.GetRecentAiFireCandidates(bool isInfrared)
        {
            return _features._smokeFeature.GetRecentAiFireCandidates(isInfrared);
        }

        bool IVideoFeatureContext.HasActiveDeviceSession
        {
            get { return _features._rtspSettingsFeature.HasActiveDeviceSession; }
        }

        string IVideoFeatureContext.HomeZeroStatusText
        {
            get { return _features._propertiesFeature.HomeZeroStatusText; }
            set { _features._propertiesFeature.HomeZeroStatusText = value; }
        }

        /// <summary>
        /// 장비 연결 후 IR 기본 팔레트 Black Hot 적용을 준비한다.
        /// </summary>
        void IVideoFeatureContext.InitializeThermalBlackHotAfterDeviceConnected()
        {
            _features._thermalFeature.InitializeThermalBlackHotAfterDeviceConnected();
        }

        /// <summary>
        /// 새 WEB AGENT 연결의 조회·상태 관리 세션을 초기화한다.
        /// </summary>
        void IVideoFeatureContext.InitializeWebAgentSession()
        {
            _features._webAgentProtocolFeature.InitializeWebAgentSession();
        }

        BitmapSource IVideoFeatureContext.IRCameraImage
        {
            get { return _features._propertiesFeature.IRCameraImage; }
            set { _features._propertiesFeature.IRCameraImage = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> IVideoFeatureContext.IrDetectionBoxes
        {
            get { return _features._propertiesFeature.IrDetectionBoxes; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> IVideoFeatureContext.IrFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.IrFireSmokeDetectionBoxes; }
        }

        string IVideoFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        string IVideoFeatureContext.IrStatusText
        {
            get { return _features._propertiesFeature.IrStatusText; }
            set { _features._propertiesFeature.IrStatusText = value; }
        }

        int IVideoFeatureContext.IrVideoHeight
        {
            get { return _features._propertiesFeature.IrVideoHeight; }
            set { _features._propertiesFeature.IrVideoHeight = value; }
        }

        int IVideoFeatureContext.IrVideoWidth
        {
            get { return _features._propertiesFeature.IrVideoWidth; }
            set { _features._propertiesFeature.IrVideoWidth = value; }
        }

        bool IVideoFeatureContext.IsCrosshairVisible
        {
            get { return _features._propertiesFeature.IsCrosshairVisible; }
            set { _features._propertiesFeature.IsCrosshairVisible = value; }
        }

        bool IVideoFeatureContext.IsDeviceSettingsLocked
        {
            get { return _features._rtspSettingsFeature.IsDeviceSettingsLocked; }
        }

        /// <summary>
        /// 2026-08-28: AUTO SCAN과 파노라마 촬영 중에도 FIRE/SMOKE 분석 및 이벤트 갱신을 유지하고, 일반 수동 이동 중에만
        /// 분석을 보류한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IVideoFeatureContext.IsFireSmokeFrameAnalysisAllowed()
        {
            return _features._smokeFeature.IsFireSmokeFrameAnalysisAllowed();
        }

        /// <summary>
        /// 지정한 영상 채널과 탐지 종류가 현재 사용 대상인지 확인한다.
        /// </summary>
        bool IVideoFeatureContext.IsFireSourceEnabled(bool isInfrared)
        {
            return _features._thermalFeature.IsFireSourceEnabled(isInfrared);
        }

        bool IVideoFeatureContext.IsPanoramaCaptureRunning
        {
            get { return _features._panoramaFeature.IsPanoramaCaptureRunning; }
            set { _features._panoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        bool IVideoFeatureContext.IsPresetScanRunning
        {
            get { return _features._propertiesFeature.IsPresetScanRunning; }
            set { _features._propertiesFeature.IsPresetScanRunning = value; }
        }

        bool IVideoFeatureContext.IsRooftopStatusSelected
        {
            get { return _features._propertiesFeature.IsRooftopStatusSelected; }
        }

        bool IVideoFeatureContext.IsSmokeDetectionEnabled
        {
            get { return _features._smokeFeature.IsSmokeDetectionEnabled; }
            set { _features._smokeFeature.IsSmokeDetectionEnabled = value; }
        }

        bool IVideoFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features._thermalFeature.IsThermalFireDetectionEnabled; }
            set { _features._thermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        /// <summary>
        /// 현재 설정된 Pan / Tilt 좌표계의 0°, 0° 위치로 절대 이동한다. 기존 LA 0xB1 명령은 기계식
        /// Homing(StartHoming) 명령이므로 카메라가 홈 센서 방향으로 이동한 뒤 해당 위치를 원점으로 잡는다. UI의 HOME
        /// POSITION은 사용자가 설정한 좌표 원점으로 복귀하는 기능이므로 Pan 0°(0x45), Tilt 0°(0x47) 절대 이동 명령을
        /// 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IVideoFeatureContext.MoveHomePositionAsync()
        {
            return _features._moveControlFeature.MoveHomePositionAsync();
        }

        /// <summary>
        /// EO 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IVideoFeatureContext.NotifyEoCurrentStatusChanged(bool panChanged, bool tiltChanged, bool zoomChanged, bool focusChanged, bool powerChanged)
        {
            _features._controlAgentFeature.NotifyEoCurrentStatusChanged(panChanged, tiltChanged, zoomChanged, focusChanged, powerChanged);
        }

        /// <summary>
        /// IR 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IVideoFeatureContext.NotifyIrCurrentStatusChanged()
        {
            _features._controlAgentFeature.NotifyIrCurrentStatusChanged();
        }

        string IVideoFeatureContext.PresetCommandStatusText
        {
            get { return _features._propertiesFeature.PresetCommandStatusText; }
            set { _features._propertiesFeature.PresetCommandStatusText = value; }
        }

        /// <summary>
        /// 현재 영상 프레임을 연기 탐지기로 분석하고 표시·이벤트 갱신에 전달한다.
        /// </summary>
        SmokeDetectionResult IVideoFeatureContext.ProcessSmokeFrame(Mat frame, string streamName, Rect fireCandidateRect)
        {
            return _features._smokeFeature.ProcessSmokeFrame(frame, streamName, fireCandidateRect);
        }

        /// <summary>
        /// 연결 세션 변경 시 이전 전원 근거와 진행 중 명령 상태를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ResetDevicePowerStatus()
        {
            _features._devicePowerFeature.ResetDevicePowerStatus();
        }

        /// <summary>
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ResetDigitalZoomSession()
        {
            _features._cameraFeatureControlFeature.ResetDigitalZoomSession();
        }

        /// <summary>
        /// 2026-08-27: 카메라 이동 또는 영상 조건 변경 뒤 EO/IR 기준 프레임과 시간축 후보를 함께 초기화하여 화면 전체 변화가 이벤트가
        /// 되지 않게 한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ResetFireSmokeFrameAnalysis(string reason)
        {
            _features._smokeFeature.ResetFireSmokeFrameAnalysis(reason);
        }

        /// <summary>
        /// 현재 선택값을 다음 연결용 설정으로 저장하며 로그의 인증정보는 가린다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.SaveRtspCommunicationSettings()
        {
            _features._rtspSettingsFeature.SaveRtspCommunicationSettings();
        }

        ControlAgentProfileOption IVideoFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        string IVideoFeatureContext.SessionEoAddress
        {
            get { return _features._rtspSettingsFeature.SessionEoAddress; }
        }

        string IVideoFeatureContext.SessionIrAddress
        {
            get { return _features._rtspSettingsFeature.SessionIrAddress; }
        }

        /// <summary>
        /// Control Agent TCP 연결 상태 UI 갱신 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.SetControlAgentConnectionStatus(string statusText, string statusColor, CancellationToken token)
        {
            _features._controlAgentFeature.SetControlAgentConnectionStatus(statusText, statusColor, token);
        }

        /// <summary>
        /// 로컬 화재·연기 탐지의 사용 여부를 관련 상태에 반영한다.
        /// </summary>
        void IVideoFeatureContext.SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke)
        {
            _features._propertiesFeature.SetLocalFireSmokeDetectionState(rtspIndex, hasFire, hasSmoke);
        }

        /// <summary>
        /// 2026-08-31: 탐지 Overlay 적용 전 EO 프레임을 파노라마 전용 버퍼에 보관한다. Frozen Bitmap만 공유하여 수신
        /// Thread와 촬영 Thread 간 접근을 안전하게 한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.SetRawEoPanoramaFrame(BitmapSource frame)
        {
            _features._panoramaFeature.SetRawEoPanoramaFrame(frame);
        }

        /// <summary>
        /// 활성 세션을 변경하지 않고 중복 연결 안내만 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.ShowAlreadyConnected()
        {
            _features._rtspSettingsFeature.ShowAlreadyConnected();
        }

        /// <summary>
        /// 현재 선택된 EO 프리셋 기준으로 CTEC Response TCP Port 9000 연결 시작 옥상 MR300 EO CTEC 직접 제어
        /// 프리셋이 아니면 기존 Response 연결을 종료하고 별도 TCP 연결을 생성하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IVideoFeatureContext.StartSelectedEoCtecResponseAsync()
        {
            return _features._continuousControlFeature.StartSelectedEoCtecResponseAsync();
        }

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 종료 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.StopCtecEoPositionPolling()
        {
            _features._continuousControlFeature.StopCtecEoPositionPolling();
        }

        /// <summary>
        /// 진행 중인 Focus Sync 확인 작업과 장비 이동을 정지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task IVideoFeatureContext.StopFocusSyncAsync(bool stopEo, bool stopIr)
        {
            return _features._syncFeature.StopFocusSyncAsync(stopEo, stopIr);
        }

        /// <summary>
        /// 경로 실행 취소를 요청하고 진행 중 이동을 정리한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.StopRoute()
        {
            _features._routesFeature.StopRoute();
        }

        /// <summary>
        /// 진행 중인 줌 동기화를 취소하고 관련 렌즈 이동을 중지한다.
        /// </summary>
        Task IVideoFeatureContext.StopZoomSyncAsync()
        {
            return _features._syncFeature.StopZoomSyncAsync();
        }

        int IVideoFeatureContext.ThermalFireBoxGroupingMode
        {
            get { return _features._thermalFeature.ThermalFireBoxGroupingMode; }
        }

        double IVideoFeatureContext.ThermalHotThresholdRatio
        {
            get { return _features._thermalFeature.ThermalHotThresholdRatio; }
            set { _features._thermalFeature.ThermalHotThresholdRatio = value; }
        }

        double IVideoFeatureContext.ThermalMinimumAreaRatio
        {
            get { return _features._thermalFeature.ThermalMinimumAreaRatio; }
            set { _features._thermalFeature.ThermalMinimumAreaRatio = value; }
        }

        /// <summary>
        /// 통신 설정 탭에서 선택된 EO / IR RTSP 주소 검증 선택값이 없거나 rtsp / rtsps 형식이 아닌 주소는 실제 FFmpeg 연결
        /// 전에 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IVideoFeatureContext.TryGetRtspEndpoints(out string eoRtspAddress, out string irRtspAddress)
        {
            return _features._controlAgentFeature.TryGetRtspEndpoints(out eoRtspAddress, out irRtspAddress);
        }

        /// <summary>
        /// 2026-09-17: 로컬 FIRE/SMOKE 분석 결과를 영상 오버레이 컬렉션에 반영한다. 이벤트 목록만 갱신되고 영상 BBox가 누락되던
        /// 경로를 복구하며, AI Agent BBox 컬렉션은 변경하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IVideoFeatureContext.UpdateFireSmokeDetectionOverlay(string camera, ThermalFireDetectionResult thermalResult, SmokeDetectionResult smokeResult)
        {
            _features._fireEventsFeature.UpdateFireSmokeDetectionOverlay(camera, thermalResult, smokeResult);
        }

        /// <summary>
        /// 열상 화재 후보의 위치·확정 상태를 표시와 이벤트 판정에 반영한다.
        /// </summary>
        void IVideoFeatureContext.UpdateThermalFireCandidateState(string camera, ThermalFireDetectionResult result, Services.Position.ZoneFrameContext zoneFrame)
        {
            _features._thermalFeature.UpdateThermalFireCandidateState(camera, result, zoneFrame);
        }

        /// <summary>
        /// 로컬 영상 후보와 기존 추적 이벤트를 대응시켜 생성·유지·해제를 반영한다.
        /// </summary>
        void IVideoFeatureContext.UpdateVisionBBoxEvents(string camera, string detectionType, IList<Rect> candidates, IList<double> candidateScores, string detectionSource, Services.Position.ZoneFrameContext zoneFrame)
        {
            _features._fireEventsFeature.UpdateVisionBBoxEvents(camera, detectionType, candidates, candidateScores, detectionSource, zoneFrame);
        }
    }
}
