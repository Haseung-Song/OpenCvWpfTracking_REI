using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>공통 화면 상태에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IScreenStateFeatureContext
    {
        IControlCommandServicePort _controlCommandService { get; }

        double AiDisplayConfidenceThreshold { get; set; }

        double AiMappingConfidence { get; set; }

        double AiMappingIou { get; set; }

        string AiRtsp0Address { get; set; }

        int AiRtsp0OnnxIndex { get; set; }

        string AiRtsp1Address { get; set; }

        int AiRtsp1OnnxIndex { get; set; }

        string ControlAgentConnectionStatusColor { get; set; }

        string ControlAgentConnectionStatusText { get; set; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        Services.Configuration.LensControlTuning ControlTuningFor(bool ir);

        string EoSourceAddress { get; set; }

        string IrSourceAddress { get; set; }

        string McbMaintenanceIpAddress { get; set; }
    }

    /// <summary>공통 화면 상태 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class ScreenStateFeature
    {

        #region [협력 객체와 초기화]

        private readonly IScreenStateFeatureContext _context;

        internal ScreenStateFeature(IScreenStateFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _irZoomObservedStopLag = IrZoomMinimumStopLead;
            _environmentIrZoomPhysicalWideRaw = 61;
            _environmentIrZoomPhysicalTeleRaw = 934;
            _environmentIrManualMoveType = ContinuousMoveType.None;
            _lastEoFocusCommandName = "NONE";
            _currentMoveType = ContinuousMoveType.None;
            _ptzPerformanceScenario = "IDLE";
            _selectedEquipmentStatusMode = EquipmentStatusMode.Rooftop;
            _zoomSyncStatusText = "READY";
            _focusSyncStatusText = "READY";
            _currentKeyboardPanTiltDirection = KeyboardPanTiltDirection.None;
            _activePanTiltMoveDirection = KeyboardPanTiltDirection.None;
            _panTiltSpeedLevel = 30;
            _lastEoFocusCommandTime = DateTime.MinValue;
            _lrfDistanceText = "DISTANCE : - m";
            _webAgentTiltMinimum = MoveControlTiltMinimum;
            _webAgentTiltMaximum = MoveControlTiltMaximum;
            _panTurnMode = PanTurnMode.Short;
            _presetScanOrderMode = PresetScanOrderMode.SavedOrder;
            _panAbsoluteValue = 0.0;
            _tiltAbsoluteValue = 0.0;
            _zoomPositionValue = 0;
            _zoomRatioValue = 1.0;
            _focusPositionValue = 0;
            _homeZeroStatusText = "READY";
            _homeZeroLockTitle = "HOME / ZERO OPERATION";
            _homeZeroLockMessage = "PROCESSING...";
            _laPresetSlotNumber = 1;
            _laPresetScanSpeed = 10;
            _laPresetScanDelay = 1;
            _laPresetCommandStatusText = "READY";
            _presetSlotNumber = 1;
            _presetScanSpeed = 10;
            _presetScanDelay = 1;
            _presetCommandStatusText = "READY";
            _lastLaStatusLogTime = DateTime.MinValue;
            _lastLaExtendedStatusLogTime = DateTime.MinValue;
            _lastAiDetectorLogTime = DateTime.MinValue;
            _aiControlAgentIp = "192.168.20.165";
            _aiAgentPort = 5055;
            _aiSettingStatusText = "AI Setting Ready";
            _aiPowerStatusText = "OFF";
            _isAutoTrackingEnabled = true;
            _isCrosshairVisible = false;

            _crosshairBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x3B, 0x30));

            _eoStatusText = "[EO] Disconnected";
            _irStatusText = "[IR] Disconnected";
            _controlAgentConnectionStatusText = "Disconnected";
            _controlAgentConnectionStatusColor = "#FF6B6B";
        }

        #endregion

        #region [상태 필드]

        internal bool _isEoFrameDisplayed;

        internal bool _isIrFrameDisplayed;

        internal int _isEoFrameDispatchPending;

        internal int _isIrFrameDispatchPending;

        internal double _currentPan;

        internal double? _lastPanAbsoluteTarget;

        internal double? _activePanAbsoluteTarget;

        internal double? _activeTiltAbsoluteTarget;

        internal double _currentTilt;

        internal long _panTiltStatusVersion;

        internal long _irLensStatusVersion;

        internal long _eoLensStatusVersion;

        internal double _irZoomObservedStopLag;

        internal int _environmentIrZoomPhysicalWideRaw;

        internal int _environmentIrZoomPhysicalTeleRaw;

        internal DateTime _environmentIrManualMoveStartedUtc;

        internal ContinuousMoveType _environmentIrManualMoveType;

        internal int _environmentIrManualMoveDirection;

        internal long _lastEoFocusCommandElapsedMs;

        internal string _lastEoFocusCommandName;

        internal int _eoFocusCommandSequence;

        internal int _eoFocusReceiveSequence;

        internal ContinuousMoveType _currentMoveType;

        internal bool _isPanTiltMoveActive;

        internal string _ptzPerformanceScenario;

        internal long _function01ReceiveCount;

        internal long _ptzfStatusUpdateCount;

        internal long _propertyChangedCount;

        internal long _panPropertyChangedCount;

        internal long _tiltPropertyChangedCount;

        internal long _zoomPropertyChangedCount;

        internal long _focusPropertyChangedCount;

        internal long _panStartTxCount;

        internal long _panStopTxCount;

        internal long _tiltStartTxCount;

        internal long _tiltStopTxCount;

        internal long _zoomStartTxCount;

        internal long _zoomStopTxCount;

        internal RtspSourceOption _activeEoCtecSource;

        internal RtspSourceOption _connectedEoCtecSource;

        internal CancellationTokenSource _ctecEoPositionPollingCts;

        internal long _ctecEoPositionOperationGeneration;

        internal EquipmentStatusMode _selectedEquipmentStatusMode;

        internal ZoomSyncLevelOption _selectedZoomSyncLevel;

        internal string _zoomSyncStatusText;

        internal CancellationTokenSource _rooftopZoomSyncCts;

        internal ZoomSyncLevelOption _selectedFocusSyncLevel;

        internal string _focusSyncStatusText;

        internal CancellationTokenSource _rooftopFocusSyncCts;

        internal ushort _currentCtecEoZoomPosition;

        internal ushort _currentCtecEoFocusPosition;

        internal byte _currentCtecEoFocusMode;

        internal bool _isKeyboardPanLeftPressed;

        internal bool _isKeyboardPanRightPressed;

        internal bool _isKeyboardTiltUpPressed;

        internal bool _isKeyboardTiltDownPressed;

        internal KeyboardPanTiltDirection _currentKeyboardPanTiltDirection;

        internal KeyboardPanTiltDirection _activePanTiltMoveDirection;

        internal string _eoSourceAddress;

        internal string _irSourceAddress;

        internal string _controlControlAgentIp;

        internal string _mcbMaintenanceIpAddress;

        internal string _controlControlAgentPortText;

        internal byte _panTiltSpeedLevel;

        internal short _currentEoZoom;

        internal short _currentEoFocus;

        internal DateTime _lastEoFocusCommandTime;

        internal string _lrfDistanceText;

        internal byte _currentPowerStatus;

        internal ushort _currentIrZoom;

        internal ushort _currentIrFocus;

        internal double _webAgentTiltMinimum;

        internal double _webAgentTiltMaximum;

        internal bool _hasWebAgentTiltLimits;

        internal int _webAgentTiltLimitQueryRequested;

        internal PanTurnMode _panTurnMode;

        internal PresetScanOrderMode _presetScanOrderMode;

        internal bool _isLoadingPresetStorage;

        internal double? _panAbsoluteValue;

        internal double? _tiltAbsoluteValue;

        internal int? _zoomPositionValue;

        internal double? _zoomRatioValue;

        internal int? _focusPositionValue;

        internal string _homeZeroStatusText;

        internal string _homeZeroLockTitle;

        internal string _homeZeroLockMessage;

        internal bool _isHomePositionMoving;

        internal int _laPresetSlotNumber;

        internal PresetPointOption _selectedLaPresetPoint;

        internal int _laPresetScanSpeed;

        internal int _laPresetScanDelay;

        internal string _laPresetCommandStatusText;

        internal bool _isLaPresetScanRunning;

        internal int _presetSlotNumber;

        internal PresetPointOption _selectedPresetPoint;

        internal int _presetScanSpeed;

        internal int _presetScanDelay;

        internal string _presetCommandStatusText;

        internal bool _isPresetScanRunning;

        internal CancellationTokenSource _moveControlPanCts;

        internal CancellationTokenSource _laPresetDirectScanCts;

        internal CancellationTokenSource _presetDirectScanCts;

        internal CancellationTokenSource _laPresetSingleMoveCts;

        internal CancellationTokenSource _presetSingleMoveCts;

        internal int _selectedRightPanelTabIndex;

        internal int _selectedOperationControlTabIndex;

        internal DateTime _lastLaStatusLogTime;

        internal DateTime _lastLaExtendedStatusLogTime;

        internal DateTime _lastAiDetectorLogTime;

        internal string _aiControlAgentIp;

        internal int _aiAgentPort;

        internal string _aiRtsp0Address;

        internal string _aiRtsp1Address;

        internal int _aiRtsp0OnnxIndex;

        internal int _aiRtsp1OnnxIndex;

        internal double _aiMappingConfidence;

        internal double _aiMappingIou;

        internal double _aiDisplayConfidenceThreshold;

        internal string _aiSettingStatusText;

        internal string _aiPowerStatusText;

        internal bool _isAutoTrackingEnabled;

        internal bool _isCrosshairVisible;

        internal Brush _crosshairBrush;

        internal int _eoVideoWidth;

        internal int _eoVideoHeight;

        internal int _irVideoWidth;

        internal int _irVideoHeight;

        internal CancellationTokenSource _cts;

        internal CancellationTokenSource _controlAgentReconnectCts;

        internal CancellationTokenSource _videoReconnectCts;

        internal bool _isDeviceConnectionRequested;

        internal BitmapSource _eoCameraImage;

        internal BitmapSource _irCameraImage;

        internal string _eoStatusText;

        internal string _irStatusText;

        internal string _controlAgentConnectionStatusText;

        internal string _controlAgentConnectionStatusColor;

        internal bool _isVideoConnecting;

        #endregion

        #region [화면 속성과 이벤트]

        internal static string RooftopMr300EoRtspAddress
        {
            get { return OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_TEST").EoAddress; }
        }

        internal static string RooftopMr300IrRtspAddress
        {
            get { return OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_TEST").IrAddress; }
        }

        internal static string RooftopMr500EoRtspAddress
        {
            get { return OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR500_MOE").EoAddress; }
        }

        internal static string RooftopMr500IrRtspAddress
        {
            get { return OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR500_MOE").IrAddress; }
        }

        internal double MoveControlMinimumZoomRatio
        {
            get { return _context.ControlTuningFor(false).MinimumRatio; }
        }

        internal double MoveControlEoMaximumZoomRatio
        {
            get { return _context.ControlTuningFor(false).MaximumRatio; }
        }

        internal double CurrentMoveControlPanMinimum
        {
            get { return MoveControlPanMinimum; }
        }

        internal double CurrentMoveControlPanMaximum
        {
            get { return MoveControlPanMaximum; }
        }

        internal double CurrentMoveControlTiltMinimum
        {
            get
            {
                if (_context._controlCommandService != null && _context._controlCommandService.UseUnsignedWebAgentTiltCoordinates && _hasWebAgentTiltLimits)
                {
                    return _webAgentTiltMinimum;
                }
                else
                {
                    return MoveControlTiltMinimum;
                }
            }
        }

        internal double CurrentMoveControlTiltMaximum
        {
            get
            {
                if (_context._controlCommandService != null && _context._controlCommandService.UseUnsignedWebAgentTiltCoordinates && _hasWebAgentTiltLimits)
                {
                    return _webAgentTiltMaximum;
                }
                else
                {
                    return MoveControlTiltMaximum;
                }
            }
        }

        #endregion

        #region [동작 처리]

        internal void InitializeControlAgentSetting()
        {
            // 1-1. 환경부 실장비 Control Agent(Web Agent) IP
            //    "192.168.20.161";
            // 1-2. 환경부 실장비 Control Agent(Web Agent) Port
            //    "5005";
            // 2-1. 옥상 MR300 장비 Local Control Agent(LA) IP
            _context.ControlAgentIp = "127.0.0.1";

            // 2-2. 옥상 MR300 장비 Control Agent(LA) Port
            _context.ControlAgentPortText = "5001";

            // 2026-08-26: 옥상 MR300 MCB 실장비 IP를 192.168.0.112로 변경한다.
            _context.McbMaintenanceIpAddress = "192.168.0.112";
            _context.ControlAgentConnectionStatusText = "Disconnected";
            _context.ControlAgentConnectionStatusColor = "#FF6B6B";
        }

        internal void InitializeDefaultSourceAddress()
        {
            /*
             * 프로그램 시작 기본 선택값
             *
             * 기존 하드코딩 주소 중 현재 테스트에 사용 중인
             * 1. 주간(EO): 옥상 MR300 주간(EO) 카메라를 기본값으로 설정한다.
             * 2. 열상(IR): 옥상 MR300 열상(IR) 카메라를 기본값으로 설정한다.
             *
             * 이후에는 소스코드 주석을 변경하지 않고
             * 통신 설정 탭의 EO / IR ComboBox에서 개별 선택한다.
             */
            _context.EoSourceAddress = RooftopMr300EoRtspAddress;
            _context.IrSourceAddress = RooftopMr300IrRtspAddress;
        }

        internal void InitializeAiDetectorSetting()
        {
            _context.AiRtsp0Address = _context.EoSourceAddress;
            _context.AiRtsp1Address = _context.IrSourceAddress;
            _context.AiRtsp0OnnxIndex = 1;
            _context.AiRtsp1OnnxIndex = 1;

            // 2026-09-02: Agent 추론 하한은 0.15로 유지해 16% 수준의 실제 송신
            // 후보를 보존하고, NMS IOU는 0.50으로 완화해 인접 플룸을 과도하게
            // 제거하지 않는다. Viewer 표시 하한은 0.12로 두어 Agent와 UI 차이를 줄인다.
            _context.AiMappingConfidence = 0.15;
            _context.AiMappingIou = 0.50;
            _context.AiDisplayConfidenceThreshold = 0.12;
        }

        #endregion

    }
}
