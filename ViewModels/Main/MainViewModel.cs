using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using OpenCvWpfTracking.Services.Communication.AI;
using OpenCvWpfTracking.Services.Communication.WebAgent;
using OpenCvWpfTracking.Services.Control;
using OpenCvWpfTracking.Services.Video;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 메인 화면의 바인딩과 기능 객체 연결을 담당합니다.
    /// 실제 동작과 상태는 Features 폴더의 담당 객체에 위임합니다.
    /// 독립 Context가 직접 사용하는 상태를 이 클래스에 다시 중계하지 않습니다.
    /// </summary>
    public partial class MainViewModel : INotifyPropertyChanged
    {
        internal enum ContinuousMoveType
        {
            None,
            PanTilt,
            EoZoom,
            EoFocus,
            IrZoom,
            IrFocus,
            IrDigitalZoom
        }

        internal enum KeyboardPanTiltDirection
        {
            None,
            PanLeft,
            PanRight,
            TiltUp,
            TiltDown,
            PanLeftTiltUp,
            PanRightTiltUp,
            PanLeftTiltDown,
            PanRightTiltDown
        }

        /// <summary>
        /// [EO] 주간 카메라 [RTSP] 영상 처리 객체
        ///
        /// [OpenCvSharp] [VideoCapture] [RTSP] 연결 실패로 인해
        /// 실제 [RTSP] 출력은 [FFmpegRtspDecoderService]를 사용한다.
        /// </summary>
        private readonly FFmpegDecoderService _eoDecoder;

        /// <summary>
        /// [IR] 열상 카메라 [RTSP] 영상 처리 객체
        ///
        /// [OpenCvSharp] [VideoCapture] [RTSP] 연결 실패로 인해
        /// 실제 [RTSP] 출력은 [FFmpegRtspDecoderService]를 사용한다.
        /// </summary>
        private readonly FFmpegDecoderService _irDecoder;

        internal static string RooftopMr300EoRtspAddress
        {
            get { return ScreenStateFeature.RooftopMr300EoRtspAddress; }
        }

        internal const byte RooftopMr300EoCtecControlSpeed = 7;

        internal const int RooftopMr300EoCtecResponsePort = 9000;

        internal static string RooftopMr300IrRtspAddress
        {
            get { return ScreenStateFeature.RooftopMr300IrRtspAddress; }
        }

        internal static string RooftopMr500EoRtspAddress
        {
            get { return ScreenStateFeature.RooftopMr500EoRtspAddress; }
        }

        internal static string RooftopMr500IrRtspAddress
        {
            get { return ScreenStateFeature.RooftopMr500IrRtspAddress; }
        }

        /// <summary>
        /// [Control Agent] 제어 [TCP] 통신 서비스 객체
        ///
        /// 기존 고흥 건의 LA 통신 구조를 유지하며,
        /// 운용 환경에 따라 Web Agent 또는 LA Agent 구현체와 연결한다.
        /// UI와 공통 코드 명칭은 Control Agent로 사용한다.
        /// </summary>
        private readonly TcpClientService _laTcpService;

        /// <summary>
        /// [TORUSS] 제어 명령 서비스
        ///
        /// [TORUSS] 제어 [Protocol] 기준 [7byte Packet] 생성 / 송신 담당
        /// </summary>
        private readonly ControlCommandService _controlCommandService;

        /// <summary>
        /// MCB Pan / Tilt Zero 및 Home fallback 직접 명령 서비스
        /// </summary>
        private readonly McbMaintenanceCommandService _mcbMaintenanceCommandService;

        internal const int McbMaintenancePort = 4001;

        /// <summary>
        /// 환경장비 Web Agent 기준 EO / IR Zoom 동기화 Adapter
        /// </summary>
        private readonly WebAgentZoomControlService _webAgentZoomControlService;

        private readonly WebAgentThermalPaletteService _webAgentThermalPaletteService;

        /// <summary>
        /// [옥상 MR300 EO] [XV-Z4850HC] CTEC CGI 직접 제어 서비스
        ///
        /// 선택된 EO 프리셋의 제어 방식이 CtecCgi인 경우에만 사용하며,
        /// 그 외 EO / Pan / Tilt / IR 제어는 기존 Control Agent 경로를 유지한다.
        /// </summary>
        private readonly CtecCameraCommandService _ctecCameraCommandService;

        /// <summary>
        /// [옥상 MR300 EO] [XV-Z4850HC] CTEC 응답 수신 서비스
        ///
        /// 카메라 IP의 TCP Port 9000에 Client로 연결하여
        /// CGI Inquiry 명령에 대한 [0x99 0x55 ... 0xFF] 응답을 수신한다.
        /// </summary>
        private readonly CtecCameraResponseService _ctecCameraResponseService;

        /// <summary>
        /// [AI] [Detector Agent] [TCP] 통신 서비스
        ///
        /// [AI] [Detector Agent]와 [TCP] 연결 후,
        /// 수신된 [AI Packet]을 [MainViewModel]로 전달한다.
        /// </summary>
        private readonly AiDetectorClientService _aiDetectorClientService;

        /// <summary>
        /// [AI 탐지 응답 패킷 해석]
        ///
        /// [AI] [Detector Agent]에서 수신한 [Packet]을
        /// [CMD] / [SIZE] / [Payload] / [Checksum] 기준으로 해석한다.
        /// </summary>
        private readonly AiDetectorPacketParser _aiDetectorPacketParser;

        /// <summary>
        /// [AI Detector Agent] 요청 [Packet] 생성 객체
        ///
        /// 향후 [ONNX] 목록 조회,
        /// [RTSP] 정보 조회 등의 요청 [Packet] 생성에 사용한다.
        /// </summary>
        private readonly AiDetectorPacketBuilder _aiPacketBuilder;

        internal const double PanTiltMoveStep = 1.0;

        /// <summary>
        /// 현재 [PAN] 각도 값(현재 위치 저장용)
        ///
        /// [LA Status Packet] 수신 시 갱신되고,
        /// 버튼 클릭 시 상대 이동 계산 기준값으로 사용한다.
        /// </summary>
        internal double _currentPan
        {
            get { return _screenStateFeature._currentPan; }
            set { _screenStateFeature._currentPan = value; }
        }

        /// <summary>
        /// 현재 [TILT] 각도 값(현재 위치 저장용)
        ///
        /// [LA Status Packet] 수신 시 갱신되고,
        /// 버튼 클릭 시 상대 이동 계산 기준값으로 사용한다.
        /// </summary>
        internal double _currentTilt
        {
            get { return _screenStateFeature._currentTilt; }
            set { _screenStateFeature._currentTilt = value; }
        }

        private readonly SemaphoreSlim _lensSyncOperationLock = new SemaphoreSlim(1, 1);

        internal const int LensSyncFeedbackTimeoutMs = 1200;

        internal const int LensSyncTargetTolerance = 12;

        internal const int IrZoomTargetTolerance = 25;

        internal const int IrZoomMinimumStopLead = 150;

        internal const int IrZoomMaximumStopLead = 360;

        internal const int IrZoomShortPulseThreshold = 180;

        internal const int IrZoomShortPulseMinimumMs = 100;

        internal const int IrZoomShortPulseMaximumMs = 650;

        internal const double IrZoomShortPulseScale = 0.58;

        internal const int IrZoomSyncMaxMoveAttempts = 4;

        internal const int IrZoomMaximumDirectionReversals = 2;

        internal const int IrZoomSyncTimeoutMs = 15000;

        internal const int EnvironmentIrZoomEndpointLearningLimit = 70;

        internal const int IrZoomSettleTimeoutMs = 2600;

        internal const int IrZoomStableSampleCount = 3;

        internal const int EnvironmentIrFocusFullTravelMs = 1600;

        internal const int EnvironmentIrZoomFullTravelMs = 5000;

        /// <summary>
        /// 프로그램 시작 이후 고정밀 경과시간 측정용
        /// </summary>
        private readonly Stopwatch _focusLogStopwatch = Stopwatch.StartNew();

        private readonly FieldOfViewSyncService _fieldOfViewSyncService = new FieldOfViewSyncService();

        internal const int CtecEoZoomPositionMax = 0x4000;

        internal const int CtecEoFocusPositionMax = 0x8000;

        internal const int CtecEoPositionPollingIntervalMs = 75;

        internal const int CtecEoPositionResponseTimeoutMs = 1000;

        internal const int CtecEoPositionSettleMaximumCount = 6;

        internal const int CtecEoPositionStableTolerance = 5;

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신부터 TCP 응답 수신까지 한 묶음으로 직렬화한다.
        ///
        /// Inquiry CGI 자체의 Lock만으로는 TCP 응답 대기 구간이 보호되지 않으므로,
        /// Zoom / Focus Polling과 Stop 후 최종 조회가 동시에 대기 작업을 만들지 않게 한다.
        /// </summary>
        private readonly SemaphoreSlim _ctecEoPositionQueryLock = new SemaphoreSlim(1, 1);

        internal const int RooftopZoomSyncTolerance = 120;

        internal const int RooftopZoomSyncInquiryIntervalMs = 100;

        internal const int RooftopZoomSyncTimeoutMs = 15000;

        internal const int RooftopFocusSyncTolerance = 240;

        internal const int RooftopFocusSyncInquiryIntervalMs = 100;

        internal const int RooftopFocusSyncTimeoutMs = 15000;

        internal const int IrFocusSyncTolerance = 8;

        internal const int IrFocusSyncSettleTimeoutMs = 700;

        internal const int IrFocusSyncStableSampleCount = 3;

        internal const int IrFocusSyncTimeoutMs = 90000;

        internal const int ControlAgentConnectingMinimumDisplayMs = 300;

        internal const short ZoomMoveStep = 10;

        internal const short FocusMoveStep = 5;

        /// <summary>
        /// [LA Status Packet]에서 수신한 [EO] [Zoom] 현재 값
        ///
        /// 일반 상태 [Packet]의 [Zoom] 값은
        ///
        /// [IR]이 아닌 [EO] 기준 값으로 확인되어
        /// [EO Zoom] 상태값으로 관리한다.
        /// </summary>
        internal short _currentEoZoom
        {
            get { return _screenStateFeature._currentEoZoom; }
            set { _screenStateFeature._currentEoZoom = value; }
        }

        /// <summary>
        /// [LA Status Packet]에서 수신한 [EO] [Focus] 현재 값
        ///
        /// 일반 상태 [Packet]의 [Focus] 값은
        ///
        /// [IR]이 아닌 [EO] 기준 값으로 확인되어
        /// [EO Focus] 상태값으로 관리한다.
        /// </summary>
        internal short _currentEoFocus
        {
            get { return _screenStateFeature._currentEoFocus; }
            set { _screenStateFeature._currentEoFocus = value; }
        }

        internal const int MoveControlPositionMinimum = 0;

        internal const int MoveControlPositionMaximum = 1000;

        internal const double MoveControlPanMinimum = -180.0;

        internal const double MoveControlPanMaximum = 180.0;

        internal const double MoveControlTiltMinimum = -90.0;

        internal const double MoveControlTiltMaximum = 90.0;

        /// <summary>
        /// HOME / PAN ZERO / TILT ZERO 작업을 직렬화한다.
        ///
        /// Zero 명령의 마지막 Motor On 안정화 대기까지 동일 Lock 범위에 포함하여
        /// HOME 또는 다른 Zero 명령이 중간에 겹치지 않도록 한다.
        /// </summary>
        private readonly SemaphoreSlim _homeZeroOperationLock = new SemaphoreSlim(1, 1);

        internal const int HomePositionTimeoutMs = 30000;

        internal const int HomePositionPollingIntervalMs = 100;

        internal const double HomePositionTargetTolerance = 0.05;

        internal const int HomePositionStableSampleCount = 5;

        internal const double HomePositionStableTolerance = 0.05;

        internal const double PresetPanTiltTargetTolerance = 0.015;

        internal const int PresetPanTiltStableSampleCount = 1;

        internal const int PresetPanTiltStationarySampleCount = 3;

        internal const int PresetPanTiltSafetyTimeoutMarginMs = 1500;

        internal const int PresetPanTiltSettleTimeoutMinimumMs = 2000;

        internal const int PresetPanTiltSettleTimeoutMaximumMs = 60000;

        internal const int PresetPanTiltCorrectionRetryCount = 1;

        /// <summary>
        /// [CONTROL AGENT] 수신 [Packet Parser]
        ///
        /// [TcpClientService]에서 받은 byte[] 데이터를
        /// [12byte] 단위의 [CONTROL AGENT] 응답 [Packet]으로 분리 / 검증하는 역할
        /// </summary>
        private readonly LAPacketParser _laPacketParser;

        internal const int LaLogIntervalSeconds = 1;

        internal const int AiDetectorLogIntervalSeconds = 3;

        /// <summary>
        /// [MainViewModel] 생성자 (초기화 역할)
        /// </summary>
        public MainViewModel()
        {
            // 먼저 기능 객체와 협력 계약을 연결한다. 여기서는 장비 연결이나 이동을 시작하지 않는다.
            _features = new FeatureComposition(CreateFeatureCallbacks());

            // 아래 필드는 새 기능 객체를 만드는 것이 아니라, 구성 객체가 만든 동일한 인스턴스를 가리킨다.
            // 화면의 기존 명령·속성 이름을 유지하면서 실제 처리는 담당 기능에 전달하기 위한 연결이다.
            _webAgentMotionFocusFeature = _features._webAgentMotionFocusFeature;
            _webAgentEquipmentFeature = _features._webAgentEquipmentFeature;
            _panoramaReferenceFeature = _features._panoramaReferenceFeature;
            _panoramaPresetsFeature = _features._panoramaPresetsFeature;
            _specialFeaturesFeature = _features._specialFeaturesFeature;
            _presetLensFeature = _features._presetLensFeature;
            _inputCoordinatorFeature = _features._inputCoordinatorFeature;
            _cameraFeatureControlFeature = _features._cameraFeatureControlFeature;
            _opticalCenterFeature = _features._opticalCenterFeature;
            _scanTraversalFeature = _features._scanTraversalFeature;
            _routesFeature = _features._routesFeature;
            _zonesFeature = _features._zonesFeature;
            _aiFeature = _features._aiFeature;
            _continuousControlFeature = _features._continuousControlFeature;
            _controlAgentFeature = _features._controlAgentFeature;
            _webAgentProtocolFeature = _features._webAgentProtocolFeature;
            _fireEventsFeature = _features._fireEventsFeature;
            _eventAlertsFeature = _features._eventAlertsFeature;
            _moveControlFeature = _features._moveControlFeature;
            _panoramaFeature = _features._panoramaFeature;
            _positionFeature = _features._positionFeature;
            _presetStorageFeature = _features._presetStorageFeature;
            _propertiesFeature = _features._propertiesFeature;
            _rtspSettingsFeature = _features._rtspSettingsFeature;
            _rpySettingsFeature = _features._rpySettingsFeature;
            _gpsSettingsFeature = _features._gpsSettingsFeature;
            _devicePowerFeature = _features._devicePowerFeature;
            _systemDateTimeFeature = _features._systemDateTimeFeature;
            _smokeFeature = _features._smokeFeature;
            _syncFeature = _features._syncFeature;
            _thermalFeature = _features._thermalFeature;
            _videoFeature = _features._videoFeature;
            _screenStateFeature = _features._screenStateFeature;

            // 잠금과 계측 시계도 기존 인스턴스를 공유한다. 복사하거나 새로 만들면 동시 제어 순서가 달라질 수 있다.
            _features.Runtime._lensSyncOperationLock = _lensSyncOperationLock;
            _features.Runtime._focusLogStopwatch = _focusLogStopwatch;
            _features.Runtime._fieldOfViewSyncService = _fieldOfViewSyncService;
            _features.Runtime._ctecEoPositionQueryLock = _ctecEoPositionQueryLock;
            _features.Runtime._homeZeroOperationLock = _homeZeroOperationLock;

            // 저장 대상 전환과 연관 속성 알림은 전담 객체가 처리한다. 이벤트 발행자는 이 화면 모델로 유지한다.
            _propertyChanges = new PropertyChangeCoordinator(new PropertyChangeContext(_features, PublishPropertyChanged));

            InitializeFeatureState();

            ConsoleLogHelper.InfoSection("MAIN", "MainViewModel initialization started");

            InitializeDisplayAndSyncCommands();

            InitializeConnectionSettingCommands();

            InitializeAiSettingCommands();

            InitializeRelativeMovementCommands();

            InitializePositionAndPresetCommands();

            InitializeMeasureAndStopCommands();

            // EO / IR FFmpeg RTSP Decoder 생성
            //
            _eoDecoder = new FFmpegDecoderService("EO");

            _features.Runtime._eoDecoder = _eoDecoder;

            _irDecoder = new FFmpegDecoderService("IR");

            _features.Runtime._irDecoder = _irDecoder;

            // [CONTROL AGENT] 통신 서비스 생성
            //
            _laTcpService = new TcpClientService();

            _features.Runtime._laTcpService = _laTcpService;

            // [TORUSS] 제어 명령 서비스 생성
            //
            _controlCommandService = new ControlCommandService(_laTcpService);

            _features.Runtime._controlCommandService = _controlCommandService;

            // MCB 유지보수 직접 명령 서비스 생성
            //
            _mcbMaintenanceCommandService = new McbMaintenanceCommandService();

            _features.Runtime._mcbMaintenanceCommandService = _mcbMaintenanceCommandService;

            // 환경장비 Web Agent Zoom Adapter 생성
            //
            _webAgentZoomControlService = new WebAgentZoomControlService(_controlCommandService);

            _features.Runtime._webAgentZoomControlService = _webAgentZoomControlService;

            _webAgentThermalPaletteService = new WebAgentThermalPaletteService(_controlCommandService);

            _features.Runtime._webAgentThermalPaletteService = _webAgentThermalPaletteService;

            // [옥상 MR300 EO] CTEC CGI 직접 제어 서비스 생성
            //
            _ctecCameraCommandService = new CtecCameraCommandService();

            _features.Runtime._ctecCameraCommandService = _ctecCameraCommandService;

            // [옥상 MR300 EO] CTEC Port 9000 응답 수신 서비스 생성
            //
            _ctecCameraResponseService = new CtecCameraResponseService();

            _features.Runtime._ctecCameraResponseService = _ctecCameraResponseService;

            // CTEC Camera Response Packet 수신 이벤트 연결
            //
            _ctecCameraResponseService.PacketReceived += OnCtecCameraResponsePacketReceived;

            // CTEC Response TCP 연결 상태 이벤트 연결
            //
            _ctecCameraResponseService.ConnectionStatusChanged += OnCtecCameraResponseConnectionStatusChanged;

            // [CONTROL AGENT] 수신 [Packet Parser] 생성
            //
            _laPacketParser = new LAPacketParser();

            _features.Runtime._laPacketParser = _laPacketParser;

            // [CONTROL AGENT] [TCP] 수신 이벤트 연결
            //
            // [TcpClientService]의 [ReceiveLoop]에서 데이터 수신 시
            // [OnLaMessageReceived] 함수가 호출된다.
            //
            _laTcpService.MessageReceived += OnLaMessageReceived;

            // [Control Agent] 서버가 연결을 종료한 경우
            // 장비 연결 요청 상태가 유지되어 있으면 자동 재연결을 시작한다.
            //
            _laTcpService.ConnectionClosed += OnControlAgentConnectionClosed;

            // [AI Detector] 통신 서비스 생성
            //
            _aiDetectorClientService = new AiDetectorClientService();

            _features.Runtime._aiDetectorClientService = _aiDetectorClientService;

            // [AI Detector] [Packet Parser] 생성
            //
            _aiDetectorPacketParser = new AiDetectorPacketParser();

            _features.Runtime._aiDetectorPacketParser = _aiDetectorPacketParser;

            // [AI Detector Agent] 요청 [Packet] 생성
            //
            _aiPacketBuilder = new AiDetectorPacketBuilder();

            _features.Runtime._aiPacketBuilder = _aiPacketBuilder;

            // [AI Detector] 수신 이벤트 연결
            //
            // [AiDetectorClientService]에서 완성 [Packet] 수신 시
            // [OnAiDetectorPacketReceived] 함수가 호출된다.
            //
            _aiDetectorClientService.PacketReceived += OnAiDetectorPacketReceived;

            // 기본 영상 주소 초기화
            //
            InitializeDefaultSourceAddress();

            // Control Agent 통신 설정 기본값 초기화
            //
            InitializeControlAgentSetting();

            // AI Detector 설정 기본값 초기화
            //
            InitializeAiDetectorSetting();

            LoadRtspCommunicationSettings();

            InitializeThermalFeatures();

            InitializeSmokeFeatures();

            InitializeFireEventFeatures();

            InitializeZoneFeatures();

            InitializeEquipmentPresetFolders();

            _presetStorageReady = true;

            SwitchEquipmentPresets();

            ConsoleLogHelper.PrintSection("[CONTROL AGENT]", "Service Initialize Complete");

            ConsoleLogHelper.StateSection("MAIN", "Initialization complete", string.Empty, $"EO      : {ConsoleLogHelper.MaskRtspPassword(EoSourceAddress)}", $"IR      : {ConsoleLogHelper.MaskRtspPassword(IrSourceAddress)}", $"CONTROL : {ControlAgentIp}:{ControlAgentPortText}");
        }

        /// <summary>
        /// Control Agent 통신 설정 기본값 초기화
        ///
        /// 통신 설정 탭의 IP / Port 입력창에
        /// 프로그램 시작 시 표시할 기본값을 설정한다.
        /// </summary>
        private void InitializeControlAgentSetting()
        {
            _screenStateFeature.InitializeControlAgentSetting();
        }

        /// <summary>
        /// 기본 EO / IR RTSP 선택값 초기화
        ///
        /// 통신 설정 탭에서 제공하는 카메라 프리셋:
        ///
        /// [1] 옥상 MR300 카메라
        /// - EO: 주간 카메라
        /// - IR: 열상 카메라
        ///
        /// [2] 옥상 MR500 카메라
        /// - EO: 주간 PTZ 카메라
        /// - IR: 열상 PTZ 카메라
        ///
        /// [3] 4층 LR1000 카메라
        /// - EO: 주간 카메라
        /// - IR: 열상 카메라
        ///
        /// 프로그램 시작 시에는 옥상 MR300 EO / IR 카메라를 기본 선택한다.
        /// </summary>
        private void InitializeDefaultSourceAddress()
        {
            _screenStateFeature.InitializeDefaultSourceAddress();
        }

        /// <summary>
        /// [AI Detector] 설정 기본값 초기화
        ///
        /// Viewer에서 사용하는 [EO] / [IR] 주소를
        /// [AI Detector Agent]의 [RTSP 0] / [RTSP 1] 기본값으로 복사한다.
        /// </summary>
        private void InitializeAiDetectorSetting()
        {
            _screenStateFeature.InitializeAiDetectorSetting();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            _propertyChanges.Handle(propertyName);
        }

        private readonly FeatureComposition _features;

        private readonly PropertyChangeCoordinator _propertyChanges;

        // 이벤트의 발행자를 기존 MainViewModel로 유지한다. 바인딩과 기존 구독자의 동작을 바꾸지 않는다.
        private void PublishPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 화면 대상 콜백을 한 번 만들어 보관한다. 이벤트 등록과 해제에 동일한 대상을 사용하기 위해서다.
        // 콜백 객체를 만드는 단계에서는 해당 메서드를 실행하지 않는다.
        private FeatureCallbacks CreateFeatureCallbacks()
        {
            return new FeatureCallbacks
            {
                Notify = OnPropertyChanged,
                DrainPendingAiUiUpdatesCallback = DrainPendingAiUiUpdates,
                OnAiDetectorPacketReceivedCallback = OnAiDetectorPacketReceived,
                HandleDevicePowerFreshnessTickCallback = DevicePowerFreshnessTimer_Tick,
                SaveAiEventsCallback = SaveAiEvents,
                LoadAiEventsCallback = LoadAiEvents,
                ClearAiEventsCallback = ClearAiEvents,
                SaveFireEventsCallback = SaveFireEvents,
                LoadFireEventsCallback = LoadFireEvents,
                ClearFireEventsCallback = ClearFireEvents,
                RequestGpsTelemetryCallback = RequestGpsTelemetry,
                ApplyGpsAutoTxRateCallback = ApplyGpsAutoTxRate,
                RequestGpsAutoTxRateCallback = RequestGpsAutoTxRate,
                UpdateGpsHeadingReferenceDisplayCallback = UpdateGpsHeadingReferenceDisplay,
                ApplyRpyOffsetCallback = ApplyRpyOffset,
                ApplyRpyAutoTxRateCallback = ApplyRpyAutoTxRate,
                RequestRpyTelemetryCallback = RequestRpyTelemetry,
                RequestStoredRpyOffsetCallback = RequestStoredRpyOffset,
                RequestStoredRpyRateCallback = RequestStoredRpyRate,
                QuerySpecialFeatureSupportCallback = QuerySpecialFeatureSupport,
                SendSpecialFeatureCallback = SendSpecialFeature,
                QuerySpecialFeatureStateCallback = QuerySpecialFeatureState,
                RequestSystemDateTimeCommandCallback = RequestSystemDateTime,
                SetSystemDateTimeFromPcCallback = SetSystemDateTimeFromPc,
                HandleSystemTimePollTickCallback = SystemTimePollTimer_Tick,
                RefreshSystemDateTimeAfterSetCallback = RequestSystemDateTime,
                QueryEquipmentTemperatureCallback = QueryEquipmentTemperature,
                ExecuteAutoFocusCallback = ExecuteAutoFocus,
                CanExecuteAutoFocusCallback = CanExecuteAutoFocus,
                ApplySpecialFeatureCapabilityCallback = ApplySpecialFeatureCapability,
                ApplySpecialFeatureStateCallback = ApplySpecialFeatureState,
                ApplySpecialFeatureUnavailableCallback = ApplySpecialFeatureUnavailable,
                IsZonePresetCurrentForDisplayCallback = ZonePresetCurrent,
                IsZonePresetCurrentForDetectionCallback = ZonePresetCurrent
            };
        }
    }
}
