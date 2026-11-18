using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Models.Main;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>화면 바인딩 상태에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IPropertiesFeatureContext
    {
        int _activePanTiltZoomSource { get; set; }

        int _aiAgentPort { get; set; }

        string _aiControlAgentIp { get; set; }

        double _aiDisplayConfidenceThreshold { get; set; }

        double _aiMappingConfidence { get; set; }

        double _aiMappingIou { get; set; }

        string _aiPowerStatusText { get; set; }

        string _aiRtsp0Address { get; set; }

        int _aiRtsp0OnnxIndex { get; set; }

        string _aiRtsp1Address { get; set; }

        int _aiRtsp1OnnxIndex { get; set; }

        string _aiSettingStatusText { get; set; }

        string _controlAgentConnectionStatusColor { get; set; }

        string _controlAgentConnectionStatusText { get; set; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        string _controlControlAgentIp { get; set; }

        string _controlControlAgentPortText { get; set; }

        Brush _crosshairBrush { get; set; }

        byte _currentMcbPowerStatus { get; set; }

        double _currentPan { get; set; }

        byte _currentPowerStatus { get; set; }

        byte _currentScbPowerStatus { get; set; }

        double _currentTilt { get; set; }

        bool _eoAlreadyConnectedNotice { get; set; }

        BitmapSource _eoCameraImage { get; set; }

        string _eoSourceAddress { get; set; }

        string _eoStatusText { get; set; }

        int _eoVideoHeight { get; set; }

        int _eoVideoWidth { get; set; }

        ref int? _focusPositionValue { get; }

        string _focusSyncStatusText { get; set; }

        bool _hasDevicePowerStatus { get; set; }

        string _homeZeroLockMessage { get; set; }

        string _homeZeroLockTitle { get; set; }

        string _homeZeroStatusText { get; set; }

        bool _irAlreadyConnectedNotice { get; set; }

        BitmapSource _irCameraImage { get; set; }

        string _irSourceAddress { get; set; }

        string _irStatusText { get; set; }

        int _irVideoHeight { get; set; }

        int _irVideoWidth { get; set; }

        bool _isAutoTrackingEnabled { get; set; }

        bool _isCrosshairVisible { get; set; }

        bool _isDeviceConnectionRequested { get; set; }

        bool _isDevicePowerStatusStale { get; set; }

        bool _isHomePositionMoving { get; set; }

        bool _isLaPresetScanRunning { get; set; }

        bool _isPresetScanRunning { get; set; }

        string _laPresetCommandStatusText { get; set; }

        int _laPresetScanDelay { get; set; }

        int _laPresetScanSpeed { get; set; }

        int _laPresetSlotNumber { get; set; }

        string _lrfDistanceText { get; set; }

        string _mcbMaintenanceIpAddress { get; set; }

        ref double? _panAbsoluteValue { get; }

        byte _panTiltSpeedLevel { get; set; }

        PanTurnMode _panTurnMode { get; set; }

        bool _precisionActive { get; set; }

        bool _precisionStopUncertain { get; set; }

        string _presetCommandStatusText { get; set; }

        int _presetScanDelay { get; set; }

        PresetScanOrderMode _presetScanOrderMode { get; set; }

        int _presetScanSpeed { get; set; }

        int _presetSlotNumber { get; set; }

        ControlAgentProfileOption _selectedControlAgentProfile { get; set; }

        EquipmentStatusMode _selectedEquipmentStatusMode { get; set; }

        ZoomSyncLevelOption _selectedFocusSyncLevel { get; set; }

        PresetPointOption _selectedLaPresetPoint { get; set; }

        int _selectedOperationControlTabIndex { get; set; }

        PresetPointOption _selectedPresetPoint { get; set; }

        int _selectedRightPanelTabIndex { get; set; }

        ZoomSyncLevelOption _selectedZoomSyncLevel { get; set; }

        ref double? _tiltAbsoluteValue { get; }

        ref int? _zoomPositionValue { get; }

        ref double? _zoomRatioValue { get; }

        string _zoomSyncStatusText { get; set; }

        string AiAlertStatusText { get; }

        void ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile);

        void ApplyPanTiltSpeedWhileMoving();

        bool ApplySelectedPanTurnMode();

        byte ApplyZoomAdaptivePanTiltSpeed(byte baseProtocolSpeed, out int standardZoom, out string activeCamera, out double opticalZoomRatio, out double speedScale);

        byte ConvertPanTiltSpeedLevel(int uiSpeed);

        string CurrentEoPowerStatusColor { get; }

        string CurrentIrPowerStatusColor { get; }

        double CurrentMoveControlTiltMaximum { get; }

        double CurrentMoveControlTiltMinimum { get; }

        string DeviceControlConnectionStatusText { get; }

        void EnsureVisibleCommunicationSettingsTab();

        string EoPowerControlStatusText { get; }

        int GetCurrentIrFocusStandardPosition();

        int GetCurrentIrZoomStandardPosition();

        ushort GetCurrentPresetStandardFocus();

        ushort GetCurrentPresetStandardZoom();

        string IrPowerControlStatusText { get; }

        bool IsGpsTabVisible { get; }

        bool IsImuRpyTabVisible { get; }

        bool IsPanoramaCancellationCompleted { get; }

        bool IsPanoramaCancellationRequested { get; }

        bool IsPanoramaCaptureRunning { get; set; }

        bool IsPanoramaCompleted { get; }

        bool IsPanoramaPresetRunning { get; }

        bool IsPanoramaProcessingRunning { get; }

        bool IsRouteRunning { get; }

        bool IsSelectedConnectionTarget { get; }

        bool IsSystemDateTimeTabVisible { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        int PresetIrZoomPosition(PresetPointOption preset);

        void QuerySpecialFeatureSupport();

        void RememberCameraPower(string camera, bool on);

        bool SavePresetStorage();

        void SaveRtspCommunicationSettings();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        int SelectedEventAlertTabIndex { get; set; }

        void ZoneChannelChanged(string camera);

        string ZoomRatioStatus(bool ir);
    }

    /// <summary>화면 바인딩 상태 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PropertiesFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPropertiesFeatureContext _context;

        internal PropertiesFeature(IPropertiesFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            LaPresetSlotOptions = new ObservableCollection<int>(Enumerable.Range(1, 100));

            LaPresetPoints = new ObservableCollection<PresetPointOption>();

            PresetSlotOptions = new ObservableCollection<int>(Enumerable.Range(1, 100));

            PresetPoints = new ObservableCollection<PresetPointOption>();

            PresetScanValueOptions = new ObservableCollection<int>(Enumerable.Range(1, 60));

            EoRtspSourceOptions = new ObservableCollection<RtspSourceOption>(OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Entries.Select(e => new RtspSourceOption(e.EoLabel, e.EoAddress, e.EoControlType, e.ControlIp, e.ControlUser, e.ControlPassword, e.UseHttps)).Concat(new[] { new RtspSourceOption("직접 입력", string.Empty, isDirectInput: true) }));

            IrRtspSourceOptions = new ObservableCollection<RtspSourceOption>(OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Entries.Select(e => new RtspSourceOption(e.IrLabel, e.IrAddress)).Concat(new[] { new RtspSourceOption("직접 입력", string.Empty, isDirectInput: true) }));

            EoDetectionBoxes = new ObservableCollection<AiDetectionBox>();

            IrDetectionBoxes = new ObservableCollection<AiDetectionBox>();

            EoFireSmokeDetectionBoxes = new ObservableCollection<VisionDetectionBox>();

            IrFireSmokeDetectionBoxes = new ObservableCollection<VisionDetectionBox>();

            AiRtspList = new ObservableCollection<AiRtspInfo>();

            AiOnnxList = new ObservableCollection<AiOnnxInfo>();

            AiMappingList = new ObservableCollection<AiMappingInfo>();
        }

        #endregion

        #region [상태 필드]

        internal RtspSourceOption _selectedEoRtspSource;

        internal RtspSourceOption _selectedIrRtspSource;

        internal RtspSourceOption _selectedAiEoRtspSource;

        internal RtspSourceOption _selectedAiIrRtspSource;

        internal bool _hasEoLocalFireDetection;

        internal bool _hasIrLocalFireDetection;

        internal bool _hasEoLocalSmokeDetection;

        internal bool _hasIrLocalSmokeDetection;

        internal bool _lastEoFireWarningVisible;

        internal bool _lastIrFireWarningVisible;

        internal bool _lastEoSmokeWarningVisible;

        internal bool _lastIrSmokeWarningVisible;

        #endregion

        #region [화면 속성과 이벤트]

        internal ICommand ToggleCrosshairCommand { get; set; }

        internal ICommand ShowRooftopStatusCommand { get; set; }

        internal ICommand ShowEnvironmentStatusCommand { get; set; }

        internal ICommand PreviousZoomSyncLevelCommand { get; set; }

        internal ICommand NextZoomSyncLevelCommand { get; set; }

        internal ICommand ApplyZoomSyncCommand { get; set; }

        internal ICommand StopZoomSyncCommand { get; set; }

        internal ICommand PreviousFocusSyncLevelCommand { get; set; }

        internal ICommand NextFocusSyncLevelCommand { get; set; }

        internal ICommand ApplyFocusSyncCommand { get; set; }

        internal ICommand StopFocusSyncCommand { get; set; }

        internal ICommand ConnectCommand { get; set; }

        internal ICommand DisconnectCommand { get; set; }

        internal ICommand PanLeftCommand { get; set; }

        internal ICommand PanRightCommand { get; set; }

        internal ICommand TiltUpCommand { get; set; }

        internal ICommand TiltDownCommand { get; set; }

        internal ICommand ZoomInCommand { get; set; }

        internal ICommand ZoomOutCommand { get; set; }

        internal ICommand FocusFarCommand { get; set; }

        internal ICommand FocusNearCommand { get; set; }

        internal ICommand MoveHomePositionCommand { get; set; }

        internal ICommand SetPanZeroCommand { get; set; }

        internal ICommand SetTiltZeroCommand { get; set; }

        internal ICommand MovePanAbsoluteCommand { get; set; }

        internal ICommand MoveTiltAbsoluteCommand { get; set; }

        internal ICommand StopAbsoluteMoveCommand { get; set; }

        internal ICommand SetZoomPositionCommand { get; set; }

        internal ICommand SetZoomRatioCommand { get; set; }

        internal ICommand SetFocusPositionCommand { get; set; }

        internal ICommand ResetPositionInputCommand { get; set; }

        internal ICommand AddOrUpdateLaPresetCommand { get; set; }

        internal ICommand ClearAllLaPresetsCommand { get; set; }

        internal ICommand DeleteSelectedLaPresetCommand { get; set; }

        internal ICommand MoveToLaPresetCommand { get; set; }

        internal ICommand StartLaPresetScanCommand { get; set; }

        internal ICommand UpdateLaPresetScanCommand { get; set; }

        internal ICommand StopLaPresetScanCommand { get; set; }

        internal ICommand StopLaPresetMoveCommand { get; set; }

        internal ICommand AddOrUpdatePresetCommand { get; set; }

        internal ICommand DeletePresetCommand { get; set; }

        internal ICommand ClearAllPresetsCommand { get; set; }

        internal ICommand MoveToPresetCommand { get; set; }

        internal ICommand StartPresetScanCommand { get; set; }

        internal ICommand UpdatePresetScanCommand { get; set; }

        internal ICommand StopPresetScanCommand { get; set; }

        internal ICommand StopPresetMoveCommand { get; set; }

        internal ICommand StopActivePresetScanCommand { get; set; }

        internal ICommand LrfMeasureCommand { get; set; }

        internal ICommand StopMoveCommand { get; set; }

        internal ICommand ConnectAiAgentCommand { get; set; }

        internal ICommand DisconnectAiAgentCommand { get; set; }

        internal ICommand ApplyAiRtspCommand { get; set; }

        internal ICommand ApplyAiMappingCommand { get; set; }

        internal ICommand RefreshAiSettingCommand { get; set; }

        internal ObservableCollection<ZoomSyncLevelOption> ZoomSyncLevelOptions { get; set; }

        internal ObservableCollection<ZoomSyncLevelOption> FocusSyncLevelOptions { get; set; }

        internal EquipmentStatusMode SelectedEquipmentStatusMode
        {
            get { return _context._selectedEquipmentStatusMode; }

            set
            {
                if (_context._selectedEquipmentStatusMode == value)
                {
                    return;
                }

                _context._selectedEquipmentStatusMode = value;

                if (_context._controlCommandService != null)
                {
                    // 상태 화면이나 EO 카메라 경로는 PT 프로토콜을 결정하지 않는다.
                    // 연결 중에는 고정한 Agent 프로필의 좌표계를 계속 사용한다.
                    _context.ApplyControlAgentPanCoordinateMode(_context.SelectedControlAgentProfile);
                }

                ConvertPanTiltValuesForSelectedCoordinateMode();

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsRooftopStatusSelected));

                _context.OnPropertyChanged(nameof(IsEnvironmentStatusSelected));

                _context.OnPropertyChanged(nameof(IsRooftopThermalPaletteVisible));

                _context.OnPropertyChanged(nameof(IsEnvironmentThermalPaletteVisible));

                _context.OnPropertyChanged(nameof(CurrentStatusEquipmentText));

                _context.OnPropertyChanged(nameof(IsHomeZeroVisible));

                _context.OnPropertyChanged(nameof(IsPanTiltZeroVisible));

                _context.OnPropertyChanged(nameof(IsPanTiltZeroCommandEnabled));

                _context.OnPropertyChanged(nameof(HomeControlHeaderText));

                _context.OnPropertyChanged(nameof(CurrentIrZoomText));

                _context.OnPropertyChanged(nameof(CurrentIrFocusText));

                _context.OnPropertyChanged(nameof(RooftopIrZoomStatusText));

                _context.OnPropertyChanged(nameof(RooftopIrFocusStatusText));

                _context.OnPropertyChanged(nameof(EnvironmentIrZoomStatusText));

                _context.OnPropertyChanged(nameof(EnvironmentIrFocusStatusText));

                _context.OnPropertyChanged(nameof(CurrentLaPresetSnapshotText));

                _context.OnPropertyChanged(nameof(CurrentPresetSnapshotText));

                _context.OnPropertyChanged(nameof(PanTiltCoordinateRangeText));

                _context.OnPropertyChanged(nameof(PanAbsoluteRangeLabel));

                _context.OnPropertyChanged(nameof(TiltAbsoluteRangeLabel));

                _context.OnPropertyChanged(nameof(_context.IsSystemDateTimeTabVisible));

                _context.OnPropertyChanged(nameof(_context.IsGpsTabVisible));
            }
        }

        internal bool IsRooftopStatusSelected
        {
            get
            {
                return SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop;
            }
        }

        internal bool IsEnvironmentStatusSelected
        {
            get
            {
                return SelectedEquipmentStatusMode == EquipmentStatusMode.Environment;
            }
        }

        internal string PanTiltCoordinateRangeText
        {
            get
            {
                return "PAN : -180° ~ 180°   /   TILT : " + $"{_context.CurrentMoveControlTiltMinimum:F2}° ~ " + $"{_context.CurrentMoveControlTiltMaximum:F2}°";
            }
        }

        internal string PanAbsoluteRangeLabel
        {
            get { return "Pan Target  (-180° ~ 180°)"; }
        }

        internal string TiltAbsoluteRangeLabel
        {
            get
            {
                return "Tilt Target  (" + $"{_context.CurrentMoveControlTiltMinimum:F2}° ~ " + $"{_context.CurrentMoveControlTiltMaximum:F2}°)";
            }
        }

        internal bool IsRooftopThermalPaletteVisible
        {
            get { return IsRooftopStatusSelected; }
        }

        internal bool IsEnvironmentThermalPaletteVisible
        {
            get { return IsEnvironmentStatusSelected; }
        }

        internal string CurrentStatusEquipmentText
        {
            get
            {
                if (IsRooftopStatusSelected)
                {
                    return "LA AGENT ACTIVE";
                }
                else
                {
                    return "WEB AGENT ACTIVE";
                }
            }
        }

        internal bool IsHomeZeroVisible
        {
            get { return true; }
        }

        internal bool IsPanTiltZeroVisible
        {
            get { return true; }
        }

        internal bool IsPanTiltZeroCommandEnabled
        {
            get { return true; }
        }

        internal string PanTiltZeroRouteText
        {
            get
            {
                if (_context._selectedControlAgentProfile?.AgentType == ControlAgentType.LaAgent)
                {
                    return "LA AGENT : MCB DIRECT ZERO";
                }
                else
                {
                    return "WEB AGENT : ZERO REQUEST (0xB3)";
                }
            }
        }

        internal string HomeControlHeaderText
        {
            get { return "HOME / ZERO"; }
        }

        internal string HomeZeroLockTitle
        {
            get { return _context._homeZeroLockTitle; }

            set
            {
                if (_context._homeZeroLockTitle == value)
                {
                    return;
                }

                _context._homeZeroLockTitle = value;

                _context.OnPropertyChanged();
            }
        }

        internal string HomeZeroLockMessage
        {
            get { return _context._homeZeroLockMessage; }

            set
            {
                if (_context._homeZeroLockMessage == value)
                {
                    return;
                }

                _context._homeZeroLockMessage = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsHomePositionMoving
        {
            get { return _context._isHomePositionMoving; }

            set
            {
                if (_context._isHomePositionMoving == value)
                {
                    return;
                }

                _context._isHomePositionMoving = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsMainControlEnabled));

                _context.OnPropertyChanged(nameof(IsOperationCommandEnabled));

                _context.OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));

                _context.OnPropertyChanged(nameof(IsControlInputLocked));

                _context.OnPropertyChanged(nameof(IsEventAlertControlEnabled));

                _context.OnPropertyChanged(nameof(IsOperationLockOverlayVisible));

                _context.OnPropertyChanged(nameof(ControlLockTitle));

                _context.OnPropertyChanged(nameof(ControlLockMessage));
            }
        }

        internal bool IsMainControlEnabled
        {
            get { return true; }
        }

        internal bool IsOperationCommandEnabled
        {
            get
            {
                return !_context.IsPanoramaPresetRunning && !_context._precisionActive && !_context._precisionStopUncertain && !IsHomePositionMoving && !IsPresetScanControlLocked && !_context.IsPanoramaCaptureRunning && !_context.IsPanoramaProcessingRunning;
            }
        }

        internal bool IsOperationTabNavigationEnabled
        {
            get { return true; }
        }

        internal bool IsEventAlertControlEnabled
        {
            get
            {
                return !_context.IsPanoramaPresetRunning && !IsHomePositionMoving && !IsPresetScanControlLocked && !_context.IsPanoramaCaptureRunning && !_context.IsPanoramaProcessingRunning;
            }
        }

        internal bool IsOperationLockOverlayVisible
        {
            get
            {
                return !IsOperationCommandEnabled;
            }
        }

        internal int SelectedRightPanelTabIndex
        {
            get { return _context._selectedRightPanelTabIndex; }

            set
            {
                if (_context._selectedRightPanelTabIndex == value)
                {
                    return;
                }

                _context._selectedRightPanelTabIndex = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));

                // 2026-08-24: 이벤트 알림 상위 탭을 열 때 가장 최근 이벤트 종류를 다시 적용한다.
                // 사용자가 이전에 다른 하위 탭을 보았더라도 AI/FIRE 최신 발생 탭이 즉시 표시된다.
                if (value == 2)
                {
                    _context.QuerySpecialFeatureSupport();
                }

                if (value == 3)
                {
                    _context.OnPropertyChanged(nameof(_context.SelectedEventAlertTabIndex));
                }
            }
        }

        internal int SelectedOperationControlTabIndex
        {
            get { return _context._selectedOperationControlTabIndex; }

            set
            {
                if (_context._selectedOperationControlTabIndex == value)
                {
                    return;
                }

                _context._selectedOperationControlTabIndex = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
            }
        }

        internal bool IsPanTiltSpeedControlEnabled
        {
            get { return IsOperationCommandEnabled; }
        }

        internal bool IsPresetScanControlLocked
        {
            get
            {
                return _context.IsRouteRunning || IsLaPresetScanRunning || IsPresetScanRunning;
            }
        }

        internal bool IsControlInputLocked
        {
            get
            {
                return _context.IsPanoramaPresetRunning || _context._precisionActive || _context._precisionStopUncertain || IsHomePositionMoving || IsPresetScanControlLocked || _context.IsPanoramaCaptureRunning || _context.IsPanoramaProcessingRunning;
            }
        }

        internal string ControlLockTitle
        {
            get
            {
                if (_context.IsPanoramaPresetRunning)
                {
                    return "PANORAMA PRESET MOVING";
                }
                else
                {
                    return _context._precisionStopUncertain ? "정밀 이동 정지 송신 미확인" : _context._precisionActive ? "정밀 이동 시험 중" : _context.IsPanoramaCompleted ? "360° PANORAMA COMPLETED" : _context.IsPanoramaCancellationCompleted ? "360° PANORAMA CANCELED" : _context.IsPanoramaCancellationRequested ? "360° PANORAMA CANCELING" : _context.IsPanoramaProcessingRunning ? "360° PANORAMA OPERATION" : IsPresetScanControlLocked ? "AUTO SCAN OPERATION" : HomeZeroLockTitle;
                }
            }
        }

        internal string ControlLockMessage
        {
            get
            {
                if (_context.IsPanoramaPresetRunning)
                {
                    return "STOP TO CANCEL PRESET MOVE";
                }
                else
                {
                    return _context._precisionStopUncertain ? "다른 제어 잠금 / STOP 재시도 및 장비 정지 여부 현장 확인 필요" : _context._precisionActive ? "다른 제어 잠금 / 정밀 이동 STOP 또는 수동 STOP 사용" : _context.IsPanoramaCompleted ? "FEATURE MATCHING / BLENDING / SAVING COMPLETED" : _context.IsPanoramaCancellationCompleted ? "CAPTURE CANCELED / START POSITION RESTORED" : _context.IsPanoramaCancellationRequested ? "CANCELING / RETURNING TO START POSITION..." : _context.IsPanoramaProcessingRunning ? _context.IsPanoramaCaptureRunning ? "EO CAMERA AUTO PAN / CAPTURING..." : "FEATURE MATCHING / BLENDING / SAVING..." : IsLaPresetScanRunning ? "PRESET L AUTO SCAN RUNNING" : IsPresetScanRunning ? "PRESET W AUTO SCAN RUNNING" : HomeZeroLockMessage;
                }
            }
        }

        internal ZoomSyncLevelOption SelectedZoomSyncLevel
        {
            get { return _context._selectedZoomSyncLevel; }

            set
            {
                if (_context._selectedZoomSyncLevel == value)
                {
                    return;
                }

                _context._selectedZoomSyncLevel = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SelectedZoomSyncLevelText));
            }
        }

        internal string SelectedZoomSyncLevelText
        {
            get
            {
                if (SelectedZoomSyncLevel == null)
                {
                    return "SELECTED LEVEL : --";
                }
                else
                {
                    return $"SELECTED LEVEL : {SelectedZoomSyncLevel.DisplayText}";
                }
            }
        }

        internal string ZoomSyncStatusText
        {
            get { return _context._zoomSyncStatusText; }

            set
            {
                if (_context._zoomSyncStatusText == value)
                {
                    return;
                }

                _context._zoomSyncStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal ZoomSyncLevelOption SelectedFocusSyncLevel
        {
            get { return _context._selectedFocusSyncLevel; }

            set
            {
                if (_context._selectedFocusSyncLevel == value)
                {
                    return;
                }

                _context._selectedFocusSyncLevel = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SelectedFocusSyncPositionText));
            }
        }

        internal string SelectedFocusSyncPositionText
        {
            get
            {
                if (SelectedFocusSyncLevel == null)
                {
                    return "0 / 1000";
                }
                else
                {
                    return $"{SelectedFocusSyncLevel.Position} / 1000";
                }
            }
        }

        internal string FocusSyncStatusText
        {
            get { return _context._focusSyncStatusText; }

            set
            {
                if (_context._focusSyncStatusText == value)
                {
                    return;
                }

                _context._focusSyncStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal string RooftopEoZoomStatusText
        {
            get
            {
                return _context.ZoomRatioStatus(false);
            }
        }

        internal string RooftopEoFocusStatusText
        {
            get
            {
                return $"{_context.GetCurrentPresetStandardFocus()} / 1000";
            }
        }

        internal string RooftopIrZoomStatusText
        {
            get
            {
                return _context.ZoomRatioStatus(true);
            }
        }

        internal string RooftopIrFocusStatusText
        {
            get
            {
                return $"{_context.GetCurrentIrFocusStandardPosition()} / 1000";
            }
        }

        internal string EnvironmentEoZoomStatusText
        {
            get
            {
                return _context.ZoomRatioStatus(false);
            }
        }

        internal string EnvironmentEoFocusStatusText
        {
            get
            {
                return $"{_context.GetCurrentPresetStandardFocus()} / 1000";
            }
        }

        internal string EnvironmentIrZoomStatusText
        {
            get
            {
                return _context.ZoomRatioStatus(true);
            }
        }

        internal string EnvironmentIrFocusStatusText
        {
            get
            {
                return $"{_context.GetCurrentIrFocusStandardPosition()} / 1000";
            }
        }

        internal string HomeZeroStatusText
        {
            get { return _context._homeZeroStatusText; }

            set
            {
                if (_context._homeZeroStatusText == value)
                {
                    return;
                }

                _context._homeZeroStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsPanTurnShortMode
        {
            get
            {
                return _context._panTurnMode == PanTurnMode.Short;
            }

            set
            {
                if (!value || _context._panTurnMode == PanTurnMode.Short)
                {
                    return;
                }

                _context._panTurnMode = PanTurnMode.Short;

                _context.ApplySelectedPanTurnMode();

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPanTurnViaZeroMode));
            }
        }

        internal bool IsPanTurnViaZeroMode
        {
            get
            {
                return _context._panTurnMode == PanTurnMode.ViaZero;
            }

            set
            {
                if (!value || _context._panTurnMode == PanTurnMode.ViaZero)
                {
                    return;
                }

                _context._panTurnMode = PanTurnMode.ViaZero;

                _context.ApplySelectedPanTurnMode();

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPanTurnShortMode));
            }
        }

        internal bool IsPresetSavedOrderMode
        {
            get
            {
                return _context._presetScanOrderMode == PresetScanOrderMode.SavedOrder;
            }

            set
            {
                if (!value || _context._presetScanOrderMode == PresetScanOrderMode.SavedOrder)
                {
                    return;
                }

                _context._presetScanOrderMode = PresetScanOrderMode.SavedOrder;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPresetNearestOrderMode));

                _context.SavePresetStorage();
            }
        }

        internal bool IsPresetNearestOrderMode
        {
            get
            {
                return _context._presetScanOrderMode == PresetScanOrderMode.NearestOrder;
            }

            set
            {
                if (!value || _context._presetScanOrderMode == PresetScanOrderMode.NearestOrder)
                {
                    return;
                }

                _context._presetScanOrderMode = PresetScanOrderMode.NearestOrder;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPresetSavedOrderMode));

                _context.SavePresetStorage();
            }
        }

        internal double? PanAbsoluteValue
        {
            get { return _context._panAbsoluteValue; }

            set
            {
                double? roundedValue = RoundNullableAngle(value);

                if (_context._panAbsoluteValue == roundedValue)
                {
                    return;
                }

                _context._panAbsoluteValue = roundedValue;

                _context.OnPropertyChanged();
            }
        }

        internal double? TiltAbsoluteValue
        {
            get { return _context._tiltAbsoluteValue; }

            set
            {
                double? roundedValue = RoundNullableAngle(value);

                if (_context._tiltAbsoluteValue == roundedValue)
                {
                    return;
                }

                _context._tiltAbsoluteValue = roundedValue;

                _context.OnPropertyChanged();
            }
        }

        internal int? ZoomPositionValue
        {
            get { return _context._zoomPositionValue; }

            set
            {
                if (_context._zoomPositionValue == value)
                {
                    return;
                }

                _context._zoomPositionValue = value;

                _context.OnPropertyChanged();
            }
        }

        internal double? ZoomRatioValue
        {
            get { return _context._zoomRatioValue; }

            set
            {
                double? roundedValue = value.HasValue ? Math.Round(value.Value, 1, MidpointRounding.AwayFromZero) : (double?)null;

                if (_context._zoomRatioValue == roundedValue)
                {
                    return;
                }

                _context._zoomRatioValue = roundedValue;

                _context.OnPropertyChanged();
            }
        }

        internal int? FocusPositionValue
        {
            get { return _context._focusPositionValue; }

            set
            {
                if (_context._focusPositionValue == value)
                {
                    return;
                }

                _context._focusPositionValue = value;

                _context.OnPropertyChanged();
            }
        }

        internal ObservableCollection<int> LaPresetSlotOptions { get; set; }

        internal ObservableCollection<PresetPointOption> LaPresetPoints { get; set; }

        internal int LaPresetSlotNumber
        {
            get { return _context._laPresetSlotNumber; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(100, value));

                if (_context._laPresetSlotNumber == safeValue)
                {
                    return;
                }

                _context._laPresetSlotNumber = safeValue;

                _context.OnPropertyChanged();

                SelectedLaPresetPoint = LaPresetPoints.FirstOrDefault(preset => preset.Number == safeValue);
            }
        }

        internal PresetPointOption SelectedLaPresetPoint
        {
            get { return _context._selectedLaPresetPoint; }

            set
            {
                if (ReferenceEquals(_context._selectedLaPresetPoint, value))
                {
                    return;
                }

                _context._selectedLaPresetPoint = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SelectedLaPresetDetailText));

                if (value != null && _context._laPresetSlotNumber != value.Number)
                {
                    _context._laPresetSlotNumber = value.Number;

                    _context.OnPropertyChanged(nameof(LaPresetSlotNumber));
                }
            }
        }

        internal string CurrentLaPresetSnapshotText
        {
            get
            {
                return $"PAN      : {_context._currentPan:F2}°\n" + $"TILT     : {_context._currentTilt:F2}°\n" + $"EO ZOOM  : {CurrentEoZoomText} / 1000\n" + $"EO FOCUS : {CurrentEoFocusText} / 1000\n" + $"IR ZOOM  : {CurrentIrZoomText} / 1000\n" + $"IR FOCUS : {CurrentIrFocusText} / 1000";
            }
        }

        internal string SelectedLaPresetDetailText
        {
            get
            {
                if (SelectedLaPresetPoint == null)
                {
                    return "프리셋 미등록 상태. 등록된 프리셋을 선택하세요.";
                }
                else
                {
                    return SelectedLaPresetPoint.DetailText;
                }
            }
        }

        internal int LaPresetScanSpeed
        {
            get { return _context._laPresetScanSpeed; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context._laPresetScanSpeed == safeValue)
                {
                    return;
                }

                _context._laPresetScanSpeed = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal int LaPresetScanDelay
        {
            get { return _context._laPresetScanDelay; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context._laPresetScanDelay == safeValue)
                {
                    return;
                }

                _context._laPresetScanDelay = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal string LaPresetCommandStatusText
        {
            get { return _context._laPresetCommandStatusText; }

            set
            {
                if (_context._laPresetCommandStatusText == value)
                {
                    return;
                }

                _context._laPresetCommandStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsLaPresetScanRunning
        {
            get { return _context._isLaPresetScanRunning; }

            set
            {
                if (_context._isLaPresetScanRunning == value)
                {
                    return;
                }

                _context._isLaPresetScanRunning = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPresetScanControlLocked));

                _context.OnPropertyChanged(nameof(IsControlInputLocked));

                _context.OnPropertyChanged(nameof(IsMainControlEnabled));

                _context.OnPropertyChanged(nameof(IsOperationCommandEnabled));

                _context.OnPropertyChanged(nameof(IsEventAlertControlEnabled));

                _context.OnPropertyChanged(nameof(IsOperationLockOverlayVisible));

                _context.OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));

                _context.OnPropertyChanged(nameof(ControlLockTitle));

                _context.OnPropertyChanged(nameof(ControlLockMessage));
            }
        }

        internal ObservableCollection<int> PresetSlotOptions { get; set; }

        internal ObservableCollection<PresetPointOption> PresetPoints { get; set; }

        internal ObservableCollection<int> PresetScanValueOptions { get; set; }

        internal int PresetSlotNumber
        {
            get { return _context._presetSlotNumber; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(100, value));

                if (_context._presetSlotNumber == safeValue)
                {
                    return;
                }

                _context._presetSlotNumber = safeValue;

                _context.OnPropertyChanged();

                PresetPointOption existingPreset = PresetPoints.FirstOrDefault(preset => preset.Number == safeValue);

                /*
                 * 등록되지 않은 새 슬롯을 선택한 경우에는
                 * 기존 ComboBox 선택이 남아 잘못 이동하지 않도록
                 * 선택 프리셋을 null로 초기화한다.
                 */
                SelectedPresetPoint = existingPreset;
            }
        }

        internal PresetPointOption SelectedPresetPoint
        {
            get { return _context._selectedPresetPoint; }

            set
            {
                if (ReferenceEquals(_context._selectedPresetPoint, value))
                {
                    return;
                }

                _context._selectedPresetPoint = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SelectedPresetDetailText));

                if (value != null && _context._presetSlotNumber != value.Number)
                {
                    _context._presetSlotNumber = value.Number;

                    _context.OnPropertyChanged(nameof(PresetSlotNumber));
                }
            }
        }

        internal string CurrentPresetSnapshotText
        {
            get
            {
                return $"PAN      : {_context._currentPan:F2}°\n" + $"TILT     : {_context._currentTilt:F2}°\n" + $"EO ZOOM  : {CurrentEoZoomText} / 1000\n" + $"EO FOCUS : {CurrentEoFocusText} / 1000\n" + $"IR ZOOM  : {CurrentIrZoomText} / 1000\n" + $"IR FOCUS : {CurrentIrFocusText} / 1000";
            }
        }

        internal string SelectedPresetDetailText
        {
            get
            {
                if (SelectedPresetPoint == null)
                {
                    return "WEB 프리셋 미등록 / 연결 후 REGISTER / UPDATE로 등록하세요.";
                }
                else
                {
                    return SelectedPresetPoint.DetailText.Replace("IR ZOOM  : " + SelectedPresetPoint.IrZoomText, "IR ZOOM  : " + _context.PresetIrZoomPosition(SelectedPresetPoint));
                }
            }
        }

        internal int PresetScanSpeed
        {
            get { return _context._presetScanSpeed; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context._presetScanSpeed == safeValue)
                {
                    return;
                }

                _context._presetScanSpeed = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal int PresetScanDelay
        {
            get { return _context._presetScanDelay; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context._presetScanDelay == safeValue)
                {
                    return;
                }

                _context._presetScanDelay = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal string PresetCommandStatusText
        {
            get { return _context._presetCommandStatusText; }

            set
            {
                if (_context._presetCommandStatusText == value)
                {
                    return;
                }

                _context._presetCommandStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsPresetScanRunning
        {
            get { return _context._isPresetScanRunning; }

            set
            {
                if (_context._isPresetScanRunning == value)
                {
                    return;
                }

                _context._isPresetScanRunning = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPresetScanControlLocked));

                _context.OnPropertyChanged(nameof(IsControlInputLocked));

                _context.OnPropertyChanged(nameof(IsMainControlEnabled));

                _context.OnPropertyChanged(nameof(IsOperationCommandEnabled));

                _context.OnPropertyChanged(nameof(IsEventAlertControlEnabled));

                _context.OnPropertyChanged(nameof(IsOperationLockOverlayVisible));

                _context.OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));

                _context.OnPropertyChanged(nameof(ControlLockTitle));

                _context.OnPropertyChanged(nameof(ControlLockMessage));
            }
        }

        internal string EoSourceDisplayAddress
        {
            get
            {
                return OpenCvWpfTracking.Services.Video.RtspUriNormalization.Display(EoSourceAddress);
            }

            set
            {
                EoSourceAddress = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(value);
            }
        }

        internal string EoSourceAddress
        {
            get { return _context._eoSourceAddress; }

            set
            {
                if (_context._eoSourceAddress == value)
                {
                    return;
                }

                _context._eoSourceAddress = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(value);

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(EoSourceDisplayAddress));

                /*
                 * EO RTSP 주소가 외부 로직에서 변경된 경우에도
                 * 통신 설정 ComboBox 선택 항목을 함께 갱신한다.
                 */
                _context.OnPropertyChanged(nameof(SelectedEoRtspSource));
            }
        }

        internal RtspSourceOption SelectedEoRtspSource
        {
            get
            {
                return _selectedEoRtspSource ?? EoRtspSourceOptions.FirstOrDefault(option => string.Equals(OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(option.Address), EoSourceAddress, StringComparison.OrdinalIgnoreCase)) ?? EoRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                if (ReferenceEquals(_selectedEoRtspSource, value))
                {
                    return;
                }

                _selectedEoRtspSource = value;

                if (!value.IsDirectInput)
                {
                    EoSourceAddress = value.Address;
                }

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsEoRtspDirectInput));

                _context.SaveRtspCommunicationSettings();
            }
        }

        internal string IrSourceDisplayAddress
        {
            get
            {
                return OpenCvWpfTracking.Services.Video.RtspUriNormalization.Display(IrSourceAddress);
            }

            set
            {
                IrSourceAddress = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(value);
            }
        }

        internal string IrSourceAddress
        {
            get { return _context._irSourceAddress; }

            set
            {
                if (_context._irSourceAddress == value)
                {
                    return;
                }

                _context._irSourceAddress = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(value);

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IrSourceDisplayAddress));

                _context.OnPropertyChanged(nameof(SelectedIrRtspSource));

                _context.OnPropertyChanged(nameof(IsIrRtspDirectInput));
            }
        }

        internal RtspSourceOption SelectedIrRtspSource
        {
            get
            {
                return _selectedIrRtspSource ?? IrRtspSourceOptions.FirstOrDefault(option => string.Equals(OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(option.Address), IrSourceAddress, StringComparison.OrdinalIgnoreCase)) ?? IrRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                if (ReferenceEquals(_selectedIrRtspSource, value))
                {
                    return;
                }

                _selectedIrRtspSource = value;

                if (!value.IsDirectInput)
                {
                    IrSourceAddress = value.Address;
                }

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsIrRtspDirectInput));

                _context.SaveRtspCommunicationSettings();
            }
        }

        internal bool IsEoRtspDirectInput
        {
            get
            {
                return SelectedEoRtspSource?.IsDirectInput == true;
            }
        }

        internal bool IsIrRtspDirectInput
        {
            get
            {
                return SelectedIrRtspSource?.IsDirectInput == true;
            }
        }

        internal string ControlAgentIp
        {
            get { return _context._controlControlAgentIp; }

            set
            {
                if (_context._controlControlAgentIp == value)
                {
                    return;
                }

                _context._controlControlAgentIp = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(_context.IsImuRpyTabVisible));

                _context.OnPropertyChanged(nameof(_context.IsGpsTabVisible)); // 추가

                _context.EnsureVisibleCommunicationSettingsTab();
            }
        }

        internal string McbMaintenanceIpAddress
        {
            get { return _context._mcbMaintenanceIpAddress; }

            set
            {
                if (_context._mcbMaintenanceIpAddress == value)
                {
                    return;
                }

                _context._mcbMaintenanceIpAddress = value;

                _context.OnPropertyChanged();
            }
        }

        internal string ControlAgentPortText
        {
            get { return _context._controlControlAgentPortText; }

            set
            {
                if (_context._controlControlAgentPortText == value)
                {
                    return;
                }

                _context._controlControlAgentPortText = value;

                _context.OnPropertyChanged();
            }
        }

        internal BitmapSource EOCameraImage
        {
            get { return _context._eoCameraImage; }

            set
            {
                if (_context._eoCameraImage != value)
                {
                    _context._eoCameraImage = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal BitmapSource IRCameraImage
        {
            get { return _context._irCameraImage; }

            set
            {
                if (_context._irCameraImage != value)
                {
                    _context._irCameraImage = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal int EoVideoWidth
        {
            get { return _context._eoVideoWidth; }

            set
            {
                _context._eoVideoWidth = value;

                _context.OnPropertyChanged();
            }
        }

        internal int EoVideoHeight
        {
            get { return _context._eoVideoHeight; }

            set
            {
                _context._eoVideoHeight = value;

                _context.OnPropertyChanged();
            }
        }

        internal int IrVideoWidth
        {
            get { return _context._irVideoWidth; }

            set
            {
                _context._irVideoWidth = value;

                _context.OnPropertyChanged();
            }
        }

        internal int IrVideoHeight
        {
            get { return _context._irVideoHeight; }

            set
            {
                _context._irVideoHeight = value;

                _context.OnPropertyChanged();
            }
        }

        internal string AiControlAgentIp
        {
            get { return _context._aiControlAgentIp; }

            set
            {
                if (_context._aiControlAgentIp != value)
                {
                    _context._aiControlAgentIp = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal int AiAgentPort
        {
            get { return _context._aiAgentPort; }

            set
            {
                if (_context._aiAgentPort != value)
                {
                    _context._aiAgentPort = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal string AiRtsp0Address
        {
            get { return _context._aiRtsp0Address; }

            set
            {
                if (_context._aiRtsp0Address != value)
                {
                    _context._aiRtsp0Address = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(SelectedAiEoRtspSource));

                    _context.OnPropertyChanged(nameof(IsAiEoRtspDirectInput));
                }
            }
        }

        internal string AiRtsp1Address
        {
            get { return _context._aiRtsp1Address; }

            set
            {
                if (_context._aiRtsp1Address != value)
                {
                    _context._aiRtsp1Address = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(SelectedAiIrRtspSource));

                    _context.OnPropertyChanged(nameof(IsAiIrRtspDirectInput));
                }
            }
        }

        internal RtspSourceOption SelectedAiEoRtspSource
        {
            get
            {
                return _selectedAiEoRtspSource ?? EoRtspSourceOptions.FirstOrDefault(option => string.Equals(option.Address, AiRtsp0Address, StringComparison.OrdinalIgnoreCase)) ?? EoRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                _selectedAiEoRtspSource = value;

                if (!value.IsDirectInput)
                {
                    AiRtsp0Address = value.Address;
                }

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsAiEoRtspDirectInput));

                _context.SaveRtspCommunicationSettings();
            }
        }

        internal RtspSourceOption SelectedAiIrRtspSource
        {
            get
            {
                return _selectedAiIrRtspSource ?? IrRtspSourceOptions.FirstOrDefault(option => string.Equals(option.Address, AiRtsp1Address, StringComparison.OrdinalIgnoreCase)) ?? IrRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                _selectedAiIrRtspSource = value;

                if (!value.IsDirectInput)
                {
                    AiRtsp1Address = value.Address;
                }

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsAiIrRtspDirectInput));

                _context.SaveRtspCommunicationSettings();
            }
        }

        internal bool IsAiEoRtspDirectInput
        {
            get
            {
                return SelectedAiEoRtspSource?.IsDirectInput == true;
            }
        }

        internal bool IsAiIrRtspDirectInput
        {
            get
            {
                return SelectedAiIrRtspSource?.IsDirectInput == true;
            }
        }

        internal int AiRtsp0OnnxIndex
        {
            get { return _context._aiRtsp0OnnxIndex; }

            set
            {
                if (_context._aiRtsp0OnnxIndex != value)
                {
                    _context._aiRtsp0OnnxIndex = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal int AiRtsp1OnnxIndex
        {
            get { return _context._aiRtsp1OnnxIndex; }

            set
            {
                if (_context._aiRtsp1OnnxIndex != value)
                {
                    _context._aiRtsp1OnnxIndex = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal double AiMappingConfidence
        {
            get { return _context._aiMappingConfidence; }

            set
            {
                if (_context._aiMappingConfidence != value)
                {
                    _context._aiMappingConfidence = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(AiMappingConfidenceText));
                }
            }
        }

        internal string AiMappingConfidenceText
        {
            get
            {
                return AiMappingConfidence.ToString("0.00");
            }
        }

        internal double AiMappingIou
        {
            get { return _context._aiMappingIou; }

            set
            {
                if (_context._aiMappingIou != value)
                {
                    _context._aiMappingIou = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(AiMappingIouText));
                }
            }
        }

        internal string AiMappingIouText
        {
            get
            {
                return AiMappingIou.ToString("0.00");
            }
        }

        internal double AiDisplayConfidenceThreshold
        {
            get { return _context._aiDisplayConfidenceThreshold; }

            set
            {
                if (_context._aiDisplayConfidenceThreshold != value)
                {
                    _context._aiDisplayConfidenceThreshold = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(AiDisplayConfidenceThresholdText));
                }
            }
        }

        internal string AiDisplayConfidenceThresholdText
        {
            get
            {
                return AiDisplayConfidenceThreshold.ToString("0.00");
            }
        }

        internal string AiSettingStatusText
        {
            get { return _context._aiSettingStatusText; }

            set
            {
                if (_context._aiSettingStatusText != value)
                {
                    _context._aiSettingStatusText = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal string AiPowerStatusText
        {
            get { return _context._aiPowerStatusText; }

            set
            {
                if (_context._aiPowerStatusText != value)
                {
                    _context._aiPowerStatusText = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(_context.AiAlertStatusText));
                }
            }
        }

        internal bool IsAutoTrackingEnabled
        {
            get { return _context._isAutoTrackingEnabled; }

            set
            {
                if (_context._isAutoTrackingEnabled != value)
                {
                    _context._isAutoTrackingEnabled = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal bool IsCrosshairVisible
        {
            get { return _context._isCrosshairVisible; }

            set
            {
                if (_context._isCrosshairVisible == value)
                {
                    return;
                }

                _context._isCrosshairVisible = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(CrosshairButtonText));
            }
        }

        internal string CrosshairButtonText
        {
            get
            {
                if (IsCrosshairVisible)
                {
                    return "CROSSHAIR : ENABLED";
                }
                else
                {
                    return "CROSSHAIR : DISABLED";
                }
            }
        }

        internal Brush CrosshairBrush
        {
            get { return _context._crosshairBrush; }
        }

        internal bool IsContextPanelsVisible
        {
            get
            {
                return string.Equals(ControlAgentConnectionStatusText, "Connected", StringComparison.Ordinal);
            }
        }

        internal string ControlAgentConnectionStatusText
        {
            get { return _context._controlAgentConnectionStatusText; }

            set
            {
                if (_context._controlAgentConnectionStatusText == value)
                {
                    return;
                }

                _context._controlAgentConnectionStatusText = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(_context.DeviceControlConnectionStatusText));

                _context.OnPropertyChanged(nameof(IsContextPanelsVisible));
            }
        }

        internal string ControlAgentConnectionStatusColor
        {
            get { return _context._controlAgentConnectionStatusColor; }

            set
            {
                if (_context._controlAgentConnectionStatusColor == value)
                {
                    return;
                }

                _context._controlAgentConnectionStatusColor = value;

                _context.OnPropertyChanged();
            }
        }

        internal byte PanTiltSpeedLevel
        {
            get { return _context._panTiltSpeedLevel; }

            set
            {
                byte normalizedValue = (byte)Math.Max(1, Math.Min(60, (int)value));

                if (_context._panTiltSpeedLevel != normalizedValue)
                {
                    _context._panTiltSpeedLevel = normalizedValue;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(EffectivePanTiltSpeedLevel));

                    _context.OnPropertyChanged(nameof(PanTiltSpeedDisplayText));

                    _context.ApplyPanTiltSpeedWhileMoving();
                }
            }
        }

        internal byte EffectivePanTiltSpeedLevel
        {
            get
            {
                byte baseProtocolSpeed = _context.ConvertPanTiltSpeedLevel(PanTiltSpeedLevel);

                if (baseProtocolSpeed == 0)
                {
                    return 0;
                }

                // 연결 해제 상태의 EO/IR은 캐시 렌즈 감속값 대신 설정된 기본값을 표시한다.
                // 연결 중 이동은 기존 배율 기반 속도 안전 계산을 유지한다.
                if (!_context._isDeviceConnectionRequested)
                {
                    return baseProtocolSpeed;
                }

                return _context.ApplyZoomAdaptivePanTiltSpeed(baseProtocolSpeed, out _, out _, out _, out _);
            }
        }

        internal string PanTiltSpeedDisplayText
        {
            get
            {
                return $"SET : {PanTiltSpeedLevel} / " + $"ACTIVE({(_context._activePanTiltZoomSource == 1 ? "IR" : "EO")}) : " + $"{EffectivePanTiltSpeedLevel} / 60";
            }
        }

        internal string LrfDistanceText
        {
            get { return _context._lrfDistanceText; }

            set
            {
                if (_context._lrfDistanceText != value)
                {
                    _context._lrfDistanceText = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal string EoStatusText
        {
            get
            {
                if (_context._eoAlreadyConnectedNotice)
                {
                    return "[EO] Already Connected...";
                }
                else
                {
                    return _context._eoStatusText;
                }
            }

            set
            {
                _context._eoAlreadyConnectedNotice = false;

                if (_context._eoStatusText == value)
                {
                    return;
                }

                _context._eoStatusText = value;

                if (_context.IsSelectedConnectionTarget && value == "[EO] Connected" && (!_context.IsGpsTabVisible || !_context._hasDevicePowerStatus || _context._isDevicePowerStatusStale))
                {
                    _context.RememberCameraPower("EO", true);
                }

                _context.ZoneChannelChanged("EO");

                /*
                 * 영상 화면 하단의 EO 상태 문자열 갱신
                 */
                _context.OnPropertyChanged();

                /*
                 * 통신 설정 화면의 EO RTSP 상태 문자열 및
                 * 상태 표시 색상을 함께 갱신한다.
                 */
                _context.OnPropertyChanged(nameof(EoConnectionStatusText));

                _context.OnPropertyChanged(nameof(EoConnectionStatusColor));

                /*
                 * CONNECTION STATUS 영역의
                 * EO 상태 표시를 함께 갱신한다.
                 */
                _context.OnPropertyChanged(nameof(CurrentPowerText));

                _context.OnPropertyChanged(nameof(CurrentEoPowerText));

                _context.OnPropertyChanged(nameof(_context.EoPowerControlStatusText));

                _context.OnPropertyChanged(nameof(_context.CurrentEoPowerStatusColor));
            }
        }

        internal string IrStatusText
        {
            get
            {
                if (_context._irAlreadyConnectedNotice)
                {
                    return "[IR] Already Connected...";
                }
                else
                {
                    return _context._irStatusText;
                }
            }

            set
            {
                _context._irAlreadyConnectedNotice = false;

                if (_context._irStatusText == value)
                {
                    return;
                }

                _context._irStatusText = value;

                if (_context.IsSelectedConnectionTarget && value == "[IR] Connected" && (!_context.IsGpsTabVisible || !_context._hasDevicePowerStatus || _context._isDevicePowerStatusStale))
                {
                    _context.RememberCameraPower("IR", true);
                }

                _context.ZoneChannelChanged("IR");

                /*
                 * 영상 화면 하단의 IR 상태 문자열 갱신
                 */
                _context.OnPropertyChanged();

                /*
                 * 통신 설정 화면의 IR RTSP 상태 문자열 및
                 * 상태 표시 색상을 함께 갱신한다.
                 */
                _context.OnPropertyChanged(nameof(IrConnectionStatusText));

                _context.OnPropertyChanged(nameof(IrConnectionStatusColor));

                /*
                 * CONNECTION STATUS 영역의
                 * IR 상태 표시를 함께 갱신한다.
                 */
                _context.OnPropertyChanged(nameof(CurrentPowerText));

                _context.OnPropertyChanged(nameof(CurrentIrPowerText));

                _context.OnPropertyChanged(nameof(_context.IrPowerControlStatusText));

                _context.OnPropertyChanged(nameof(_context.CurrentIrPowerStatusColor));
            }
        }

        internal string EoConnectionStatusText
        {
            get
            {
                return GetRtspConnectionStatusText(_context._eoStatusText, "[EO]");
            }
        }

        internal string EoConnectionStatusColor
        {
            get
            {
                return GetRtspConnectionStatusColor(EoConnectionStatusText);
            }
        }

        internal string IrConnectionStatusText
        {
            get
            {
                return GetRtspConnectionStatusText(_context._irStatusText, "[IR]");
            }
        }

        internal string IrConnectionStatusColor
        {
            get
            {
                return GetRtspConnectionStatusColor(IrConnectionStatusText);
            }
        }

        internal string CurrentPanText
        {
            get
            {
                return $"{NormalizeToSignedAngle(_context._currentPan):F2}°";
            }
        }

        internal string CurrentTiltText
        {
            get
            {
                return $"{_context._currentTilt:F2}°";
            }
        }

        internal string CurrentEoZoomText
        {
            get
            {
                return _context.GetCurrentPresetStandardZoom().ToString();
            }
        }

        internal string CurrentEoFocusText
        {
            get
            {
                return _context.GetCurrentPresetStandardFocus().ToString();
            }
        }

        internal string CurrentIrZoomText
        {
            get
            {
                return _context.GetCurrentIrZoomStandardPosition().ToString();
            }
        }

        internal string CurrentIrFocusText
        {
            get
            {
                return _context.GetCurrentIrFocusStandardPosition().ToString();
            }
        }

        internal string CurrentPowerText
        {
            get
            {
                if (_context._controlCommandService != null && _context.IsGpsTabVisible && _context._hasDevicePowerStatus)
                {
                    // 2026-10-06: WebAgent는 Function 0x31의 보드 상태를 단일 기준으로 사용한다.
                    // 첫 상태 패킷 전에는 OFF로 오인하지 않도록 UNKNOWN을 유지한다.
                    return $"CONTROL:{CurrentControlPowerText} / " + $"EO:{CurrentEoPowerText} / " + $"IR:{CurrentIrPowerText}";
                }

                bool isPanOn = (_context._currentPowerStatus & 0x80) != 0;

                bool isTiltOn = (_context._currentPowerStatus & 0x40) != 0;

                bool isEoOn = _context._eoStatusText == "[EO] Connected";

                bool isIrOn = _context._irStatusText == "[IR] Connected";

                return $"CONTROL:{ToOnOff(isPanOn && isTiltOn)} / " + $"EO:{ToOnOff(isEoOn)} / " + $"IR:{ToOnOff(isIrOn)}";
            }
        }

        internal string CurrentControlPowerText
        {
            get
            {
                if (_context._controlCommandService != null && _context.IsGpsTabVisible && _context._hasDevicePowerStatus)
                {
                    if (_context._hasDevicePowerStatus && !_context._isDevicePowerStatusStale)
                    {
                        return ToOnOff((_context._currentMcbPowerStatus & 0x01) != 0);
                    }
                    else
                    {
                        return "UNKNOWN";
                    }
                }

                bool isPanOn = (_context._currentPowerStatus & 0x80) != 0;

                bool isTiltOn = (_context._currentPowerStatus & 0x40) != 0;

                return ToOnOff(isPanOn && isTiltOn);
            }
        }

        internal string CurrentEoPowerText
        {
            get
            {
                if (_context._controlCommandService != null && _context.IsGpsTabVisible && _context._hasDevicePowerStatus)
                {
                    if (_context._hasDevicePowerStatus && !_context._isDevicePowerStatusStale)
                    {
                        return ToOnOff((_context._currentScbPowerStatus & 0x01) != 0);
                    }
                    else
                    {
                        return "UNKNOWN";
                    }
                }

                return GetLegacyCameraPowerText(_context._eoStatusText, "EO");
            }
        }

        internal string CurrentIrPowerText
        {
            get
            {
                if (_context._controlCommandService != null && _context.IsGpsTabVisible && _context._hasDevicePowerStatus)
                {
                    if (_context._hasDevicePowerStatus && !_context._isDevicePowerStatusStale)
                    {
                        return ToOnOff((_context._currentMcbPowerStatus & 0x20) != 0);
                    }
                    else
                    {
                        return "UNKNOWN";
                    }
                }

                return GetLegacyCameraPowerText(_context._irStatusText, "IR");
            }
        }

        internal ObservableCollection<RtspSourceOption> EoRtspSourceOptions { get; set; }

        internal ObservableCollection<RtspSourceOption> IrRtspSourceOptions { get; set; }

        internal ObservableCollection<AiDetectionBox> EoDetectionBoxes { get; set; }

        internal ObservableCollection<AiDetectionBox> IrDetectionBoxes { get; set; }

        internal ObservableCollection<VisionDetectionBox> EoFireSmokeDetectionBoxes { get; set; }

        internal ObservableCollection<VisionDetectionBox> IrFireSmokeDetectionBoxes { get; set; }

        internal bool IsEoFireWarningVisible
        {
            get
            {
                return _hasEoLocalFireDetection || HasAiDetection(EoDetectionBoxes, "FIRE", "FLAME");
            }
        }

        internal bool IsIrFireWarningVisible
        {
            get
            {
                return _hasIrLocalFireDetection || HasAiDetection(IrDetectionBoxes, "FIRE", "FLAME");
            }
        }

        internal bool IsEoSmokeWarningVisible
        {
            get
            {
                return _hasEoLocalSmokeDetection || HasAiDetection(EoDetectionBoxes, "SMOKE");
            }
        }

        internal bool IsIrSmokeWarningVisible
        {
            get
            {
                return _hasIrLocalSmokeDetection || HasAiDetection(IrDetectionBoxes, "SMOKE");
            }
        }

        internal ObservableCollection<AiRtspInfo> AiRtspList { get; set; }

        internal ObservableCollection<AiOnnxInfo> AiOnnxList { get; set; }

        internal ObservableCollection<AiMappingInfo> AiMappingList { get; set; }

        #endregion

        #region [동작 처리]

        internal void ConvertPanTiltValuesForSelectedCoordinateMode()
        {
            _context._currentPan = NormalizeToSignedAngle(_context._currentPan);

            _context._currentTilt = Math.Max(_context.CurrentMoveControlTiltMinimum, Math.Min(_context.CurrentMoveControlTiltMaximum, NormalizeToSignedAngle(_context._currentTilt)));

            PanAbsoluteValue = PanAbsoluteValue.HasValue ? NormalizeToSignedAngle(PanAbsoluteValue.Value) : (double?)null;

            TiltAbsoluteValue = TiltAbsoluteValue.HasValue ? Math.Max(_context.CurrentMoveControlTiltMinimum, Math.Min(_context.CurrentMoveControlTiltMaximum, NormalizeToSignedAngle(TiltAbsoluteValue.Value))) : (double?)null;

            _context.OnPropertyChanged(nameof(CurrentPanText));

            _context.OnPropertyChanged(nameof(CurrentTiltText));
        }

        internal static double NormalizeToUnsignedAngle(double angle)
        {
            double normalized = angle % 360.0;

            if (normalized < 0.0)
            {
                return normalized + 360.0;
            }
            else
            {
                return normalized;
            }
        }

        internal static double NormalizeToSignedAngle(double angle)
        {
            double normalized = NormalizeToUnsignedAngle(angle);

            if (normalized > 180.0)
            {
                return normalized - 360.0;
            }
            else
            {
                return normalized;
            }
        }

        internal static string GetRtspConnectionStatusText(string statusText, string cameraPrefix)
        {
            if (string.IsNullOrWhiteSpace(statusText))
            {
                return "Disconnected";
            }

            string normalizedStatus = statusText.Replace(cameraPrefix, string.Empty).Trim();

            /*
             * Reconnecting 문자열 안에는
             * Connecting 문자열이 포함되므로
             * 반드시 Reconnecting을 먼저 확인해야 한다.
             */
            if (normalizedStatus.IndexOf("Reconnecting", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Reconnecting";
            }

            if (normalizedStatus.IndexOf("Connecting", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Connecting";
            }

            if (normalizedStatus.IndexOf("Disconnected", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Disconnected";
            }

            if (normalizedStatus.IndexOf("Connected", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Connected";
            }

            return "Disconnected";
        }

        internal static string GetRtspConnectionStatusColor(string connectionStatusText)
        {
            switch (connectionStatusText)
            {
                case "Connected":
                    return "#68D391";
                case "Connecting":
                case "Reconnecting":
                    return "#F6E05E";
                case "Disconnected":
                default:
                    return "#FF6B6B";
            }
        }

        internal static string BuildCtecPositionText(int rawPosition, int maxPosition)
        {
            if (maxPosition <= 0)
            {
                return rawPosition.ToString();
            }

            double percent = rawPosition / (double)maxPosition * 100.0;

            percent = Math.Max(0.0, Math.Min(100.0, percent));

            return $"{rawPosition} / {maxPosition} ({percent:F1}%)";
        }

        internal static string ToOnOff(bool isOn)
        {
            if (isOn)
            {
                return "ON";
            }
            else
            {
                return "OFF";
            }
        }

        internal static bool HasAiDetection(IEnumerable<AiDetectionBox> boxes, params string[] alarmClasses)
        {
            if (boxes == null || alarmClasses == null || alarmClasses.Length == 0)
            {
                return false;
            }

            return boxes.Any(box => box != null && alarmClasses.Any(alarmClass => string.Equals(box.ClassName?.Trim(), alarmClass, StringComparison.OrdinalIgnoreCase)));
        }

        internal void NotifyFireSmokeWarningStateChanged(int rtspIndex)
        {
            bool isInfrared = rtspIndex == 1;

            string channel = isInfrared ? "IR" : "EO";

            bool localFire = isInfrared ? _hasIrLocalFireDetection : _hasEoLocalFireDetection;

            bool localSmoke = isInfrared ? _hasIrLocalSmokeDetection : _hasEoLocalSmokeDetection;

            IEnumerable<AiDetectionBox> aiBoxes = isInfrared ? IrDetectionBoxes : EoDetectionBoxes;

            bool aiFire = HasAiDetection(aiBoxes, "FIRE", "FLAME");

            bool aiSmoke = HasAiDetection(aiBoxes, "SMOKE");

            bool fireVisible = localFire || aiFire;

            bool smokeVisible = localSmoke || aiSmoke;

            if (isInfrared)
            {
                LogDetectionWarningTransition("FIRE", channel, fireVisible, localFire, aiFire, ref _lastIrFireWarningVisible);

                LogDetectionWarningTransition("SMOKE", channel, smokeVisible, localSmoke, aiSmoke, ref _lastIrSmokeWarningVisible);
            }
            else
            {
                LogDetectionWarningTransition("FIRE", channel, fireVisible, localFire, aiFire, ref _lastEoFireWarningVisible);

                LogDetectionWarningTransition("SMOKE", channel, smokeVisible, localSmoke, aiSmoke, ref _lastEoSmokeWarningVisible);
            }

            _context.OnPropertyChanged(isInfrared ? nameof(IsIrFireWarningVisible) : nameof(IsEoFireWarningVisible));

            _context.OnPropertyChanged(isInfrared ? nameof(IsIrSmokeWarningVisible) : nameof(IsEoSmokeWarningVisible));
        }

        internal static void LogDetectionWarningTransition(string detectionType, string channel, bool current, bool local, bool ai, ref bool previous)
        {
            if (current == previous)
            {
                return;
            }

            previous = current;

            ConsoleLogHelper.State(detectionType + " ALERT", (current ? "Warning displayed" : "Warning cleared") + " / CHANNEL=" + channel + " / LOCAL=" + local + " / AI=" + ai);
        }

        internal void SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke)
        {
            if (rtspIndex == 1)
            {
                _hasIrLocalFireDetection = hasFire;
                _hasIrLocalSmokeDetection = hasSmoke;
            }
            else
            {
                _hasEoLocalFireDetection = hasFire;
                _hasEoLocalSmokeDetection = hasSmoke;
            }

            NotifyFireSmokeWarningStateChanged(rtspIndex);
        }

        #endregion

    }
}
