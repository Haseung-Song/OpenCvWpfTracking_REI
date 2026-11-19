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
        int ActivePanTiltZoomSource { get; set; }

        int AiAgentPort { get; set; }

        string AiControlAgentIpState { get; set; }

        double AiDisplayConfidenceThreshold { get; set; }

        double AiMappingConfidence { get; set; }

        double AiMappingIou { get; set; }

        string AiPowerStatusText { get; set; }

        string AiRtsp0Address { get; set; }

        int AiRtsp0OnnxIndex { get; set; }

        string AiRtsp1Address { get; set; }

        int AiRtsp1OnnxIndex { get; set; }

        string AiSettingStatusText { get; set; }

        string ControlAgentConnectionStatusColor { get; set; }

        string ControlAgentConnectionStatusText { get; set; }

        Services.Communication.IControlCommandServicePort ControlCommandService { get; }

        string ControlControlAgentIp { get; set; }

        string ControlControlAgentPortText { get; set; }

        Brush CrosshairBrush { get; set; }

        byte CurrentMcbPowerStatus { get; set; }

        double CurrentPan { get; set; }

        byte CurrentPowerStatus { get; set; }

        byte CurrentScbPowerStatus { get; set; }

        double CurrentTilt { get; set; }

        bool EoAlreadyConnectedNotice { get; set; }

        BitmapSource EoCameraImage { get; set; }

        string EoSourceAddress { get; set; }

        string EoStatusText { get; set; }

        int EoVideoHeight { get; set; }

        int EoVideoWidth { get; set; }

        ref int? FocusPositionValue { get; }

        string FocusSyncStatusText { get; set; }

        bool HasDevicePowerStatus { get; set; }

        string HomeZeroLockMessage { get; set; }

        string HomeZeroLockTitle { get; set; }

        string HomeZeroStatusText { get; set; }

        bool IrAlreadyConnectedNotice { get; set; }

        BitmapSource IrCameraImage { get; set; }

        string IrSourceAddress { get; set; }

        string IrStatusText { get; set; }

        int IrVideoHeight { get; set; }

        int IrVideoWidth { get; set; }

        bool IsAutoTrackingEnabled { get; set; }

        bool IsCrosshairVisible { get; set; }

        bool IsDeviceConnectionRequested { get; set; }

        bool IsDevicePowerStatusStale { get; set; }

        bool IsHomePositionMoving { get; set; }

        bool IsLaPresetScanRunning { get; set; }

        bool IsPresetScanRunning { get; set; }

        string LaPresetCommandStatusTextState { get; set; }

        int LaPresetScanDelayState { get; set; }

        int LaPresetScanSpeedState { get; set; }

        int LaPresetSlotNumberState { get; set; }

        string LrfDistanceText { get; set; }

        string McbMaintenanceIpAddress { get; set; }

        ref double? PanAbsoluteValue { get; }

        byte PanTiltSpeedLevel { get; set; }

        PanTurnMode PanTurnMode { get; set; }

        bool PrecisionActive { get; set; }

        bool PrecisionStopUncertain { get; set; }

        string PresetCommandStatusTextState { get; set; }

        int PresetScanDelayState { get; set; }

        PresetScanOrderMode PresetScanOrderMode { get; set; }

        int PresetScanSpeedState { get; set; }

        int PresetSlotNumberState { get; set; }

        ControlAgentProfileOption SelectedControlAgentProfileState { get; set; }

        EquipmentStatusMode SelectedEquipmentStatusMode { get; set; }

        ZoomSyncLevelOption SelectedFocusSyncLevel { get; set; }

        PresetPointOption SelectedLaPresetPoint { get; set; }

        int SelectedOperationControlTabIndex { get; set; }

        PresetPointOption SelectedPresetPoint { get; set; }

        int SelectedRightPanelTabIndex { get; set; }

        ZoomSyncLevelOption SelectedZoomSyncLevel { get; set; }

        ref double? TiltAbsoluteValue { get; }

        ref int? ZoomPositionValue { get; }

        ref double? ZoomRatioValue { get; }

        string ZoomSyncStatusText { get; set; }

        string AiAlertStatusText { get; }

        /// <summary>
        /// 2026-09-15: REI 통합본에서는 Web Agent 프로필에만 unsigned Pan 좌표를 적용한다.
        /// </summary>
        void ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile);

        /// <summary>
        /// Pan / Tilt 연속 이동 중 Slider 값이 바뀌면 현재 방향을 유지한 채 Pelco-D Data1 / Data2 속도값만 갱신하여
        /// 즉시 재송신한다.
        /// </summary>
        void ApplyPanTiltSpeedWhileMoving();

        /// <summary>
        /// 현재 RadioButton에서 선택한 Pan 회전 모드를 Pelco-D 확장 명령 0x4D로 장비에 적용한다.
        /// </summary>
        bool ApplySelectedPanTurnMode();

        /// <summary>
        /// 2026-10-01: 현재 활성 EO/IR 카메라의 광학 줌이 커질수록 같은 PT 속도도 화면에서는 더 크게 이동해 보이므로 실제 전송
        /// 속도를 낮춘다. UI Slider 값은 사용자가 선택한 기준 속도로 유지하며 광각(1x)에서는 기존 속도를 그대로 사용한다.
        /// 2026-10-01 실장비 로그에서 EO 최대 줌 시 20% 하한(30→6)이 빠르게 보이는 것을 확인하여 5% 안전 하한으로 완화한다.
        /// 실제 EO 90x 계산값은 약 10.5%이므로 기준 속도 30에서 약 3이 적용된다.
        /// </summary>
        byte ApplyZoomAdaptivePanTiltSpeed(byte baseProtocolSpeed, out int standardZoom, out string activeCamera, out double opticalZoomRatio, out double speedScale);

        /// <summary>
        /// 2026-09-17: Web Agent 합의 규격인 UI 속도 [0 ~ 60]을 그대로 사용한다. UI 0은 이동 속도로 사용하지 않으며
        /// 정지 요청으로 처리한다.
        /// </summary>
        byte ConvertPanTiltSpeedLevel(int uiSpeed);

        string CurrentEoPowerStatusColor { get; }

        string CurrentIrPowerStatusColor { get; }

        double CurrentMoveControlTiltMaximum { get; }

        double CurrentMoveControlTiltMinimum { get; }

        string DeviceControlConnectionStatusText { get; }

        /// <summary>
        /// 장비 모드 변경으로 숨겨진 통신 탭이 선택된 경우 표시 가능한 탭으로 복귀한다.
        /// </summary>
        void EnsureVisibleCommunicationSettingsTab();

        string EoPowerControlStatusText { get; }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// ->; Standard 0(Far) Raw 0(Near) ->; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrFocusStandardPosition();

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrZoomStandardPosition();

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardFocus();

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
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

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다.
        /// </summary>
        int PresetIrZoomPosition(PresetPointOption preset);

        /// <summary>
        /// EO와 IR 기능 지원 조회를 요청하고 응답 기한을 기록한다.
        /// </summary>
        void QuerySpecialFeatureSupport();

        /// <summary>
        /// 카메라별로 마지막 확인된 전원 상태와 확인 시점을 보관한다.
        /// </summary>
        void RememberCameraPower(string camera, bool on);

        /// <summary>
        /// 현재 프리셋과 순서 정보를 저장하며 기존 파일 보존 규칙을 적용한다.
        /// </summary>
        bool SavePresetStorage();

        /// <summary>
        /// 현재 선택값을 다음 연결용 설정으로 저장하며 로그의 인증정보는 가린다.
        /// </summary>
        void SaveRtspCommunicationSettings();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        int SelectedEventAlertTabIndex { get; set; }

        /// <summary>
        /// 선택한 영상 채널에 맞춰 구역 참조와 표시 상태를 갱신한다.
        /// </summary>
        void ZoneChannelChanged(string camera);

        /// <summary>
        /// 표준 줌 위치와 확인된 배율을 상태 문자열로 구성한다.
        /// </summary>
        string ZoomRatioStatus(bool ir);
    }

    /// <summary>화면 바인딩 상태 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PropertiesFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPropertiesFeatureContext _context;

        /// <summary>
        /// 화면 상태·속성 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal PropertiesFeature(IPropertiesFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 기능 객체가 사용하는 초기 상태·목록·타이머를 준비한다.
        /// </summary>
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
            get { return _context.SelectedEquipmentStatusMode; }

            set
            {
                if (_context.SelectedEquipmentStatusMode == value)
                {
                    return;
                }

                _context.SelectedEquipmentStatusMode = value;

                if (_context.ControlCommandService != null)
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
                if (_context.SelectedControlAgentProfileState?.AgentType == ControlAgentType.LaAgent)
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
            get { return _context.HomeZeroLockTitle; }

            set
            {
                if (_context.HomeZeroLockTitle == value)
                {
                    return;
                }

                _context.HomeZeroLockTitle = value;

                _context.OnPropertyChanged();
            }
        }

        internal string HomeZeroLockMessage
        {
            get { return _context.HomeZeroLockMessage; }

            set
            {
                if (_context.HomeZeroLockMessage == value)
                {
                    return;
                }

                _context.HomeZeroLockMessage = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsHomePositionMoving
        {
            get { return _context.IsHomePositionMoving; }

            set
            {
                if (_context.IsHomePositionMoving == value)
                {
                    return;
                }

                _context.IsHomePositionMoving = value;

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
                return !_context.IsPanoramaPresetRunning && !_context.PrecisionActive && !_context.PrecisionStopUncertain && !IsHomePositionMoving && !IsPresetScanControlLocked && !_context.IsPanoramaCaptureRunning && !_context.IsPanoramaProcessingRunning;
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
            get { return _context.SelectedRightPanelTabIndex; }

            set
            {
                if (_context.SelectedRightPanelTabIndex == value)
                {
                    return;
                }

                _context.SelectedRightPanelTabIndex = value;

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
            get { return _context.SelectedOperationControlTabIndex; }

            set
            {
                if (_context.SelectedOperationControlTabIndex == value)
                {
                    return;
                }

                _context.SelectedOperationControlTabIndex = value;

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
                return _context.IsPanoramaPresetRunning || _context.PrecisionActive || _context.PrecisionStopUncertain || IsHomePositionMoving || IsPresetScanControlLocked || _context.IsPanoramaCaptureRunning || _context.IsPanoramaProcessingRunning;
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
                    return _context.PrecisionStopUncertain ? "정밀 이동 정지 송신 미확인" : _context.PrecisionActive ? "정밀 이동 시험 중" : _context.IsPanoramaCompleted ? "360° PANORAMA COMPLETED" : _context.IsPanoramaCancellationCompleted ? "360° PANORAMA CANCELED" : _context.IsPanoramaCancellationRequested ? "360° PANORAMA CANCELING" : _context.IsPanoramaProcessingRunning ? "360° PANORAMA OPERATION" : IsPresetScanControlLocked ? "AUTO SCAN OPERATION" : HomeZeroLockTitle;
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
                    return _context.PrecisionStopUncertain ? "다른 제어 잠금 / STOP 재시도 및 장비 정지 여부 현장 확인 필요" : _context.PrecisionActive ? "다른 제어 잠금 / 정밀 이동 STOP 또는 수동 STOP 사용" : _context.IsPanoramaCompleted ? "FEATURE MATCHING / BLENDING / SAVING COMPLETED" : _context.IsPanoramaCancellationCompleted ? "CAPTURE CANCELED / START POSITION RESTORED" : _context.IsPanoramaCancellationRequested ? "CANCELING / RETURNING TO START POSITION..." : _context.IsPanoramaProcessingRunning ? _context.IsPanoramaCaptureRunning ? "EO CAMERA AUTO PAN / CAPTURING..." : "FEATURE MATCHING / BLENDING / SAVING..." : IsLaPresetScanRunning ? "PRESET L AUTO SCAN RUNNING" : IsPresetScanRunning ? "PRESET W AUTO SCAN RUNNING" : HomeZeroLockMessage;
                }
            }
        }

        internal ZoomSyncLevelOption SelectedZoomSyncLevel
        {
            get { return _context.SelectedZoomSyncLevel; }

            set
            {
                if (_context.SelectedZoomSyncLevel == value)
                {
                    return;
                }

                _context.SelectedZoomSyncLevel = value;

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
            get { return _context.ZoomSyncStatusText; }

            set
            {
                if (_context.ZoomSyncStatusText == value)
                {
                    return;
                }

                _context.ZoomSyncStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal ZoomSyncLevelOption SelectedFocusSyncLevel
        {
            get { return _context.SelectedFocusSyncLevel; }

            set
            {
                if (_context.SelectedFocusSyncLevel == value)
                {
                    return;
                }

                _context.SelectedFocusSyncLevel = value;

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
            get { return _context.FocusSyncStatusText; }

            set
            {
                if (_context.FocusSyncStatusText == value)
                {
                    return;
                }

                _context.FocusSyncStatusText = value;

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
            get { return _context.HomeZeroStatusText; }

            set
            {
                if (_context.HomeZeroStatusText == value)
                {
                    return;
                }

                _context.HomeZeroStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsPanTurnShortMode
        {
            get
            {
                return _context.PanTurnMode == PanTurnMode.Short;
            }

            set
            {
                if (!value || _context.PanTurnMode == PanTurnMode.Short)
                {
                    return;
                }

                _context.PanTurnMode = PanTurnMode.Short;

                _context.ApplySelectedPanTurnMode();

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPanTurnViaZeroMode));
            }
        }

        internal bool IsPanTurnViaZeroMode
        {
            get
            {
                return _context.PanTurnMode == PanTurnMode.ViaZero;
            }

            set
            {
                if (!value || _context.PanTurnMode == PanTurnMode.ViaZero)
                {
                    return;
                }

                _context.PanTurnMode = PanTurnMode.ViaZero;

                _context.ApplySelectedPanTurnMode();

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPanTurnShortMode));
            }
        }

        internal bool IsPresetSavedOrderMode
        {
            get
            {
                return _context.PresetScanOrderMode == PresetScanOrderMode.SavedOrder;
            }

            set
            {
                if (!value || _context.PresetScanOrderMode == PresetScanOrderMode.SavedOrder)
                {
                    return;
                }

                _context.PresetScanOrderMode = PresetScanOrderMode.SavedOrder;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPresetNearestOrderMode));

                _context.SavePresetStorage();
            }
        }

        internal bool IsPresetNearestOrderMode
        {
            get
            {
                return _context.PresetScanOrderMode == PresetScanOrderMode.NearestOrder;
            }

            set
            {
                if (!value || _context.PresetScanOrderMode == PresetScanOrderMode.NearestOrder)
                {
                    return;
                }

                _context.PresetScanOrderMode = PresetScanOrderMode.NearestOrder;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsPresetSavedOrderMode));

                _context.SavePresetStorage();
            }
        }

        internal double? PanAbsoluteValue
        {
            get { return _context.PanAbsoluteValue; }

            set
            {
                double? roundedValue = RoundNullableAngle(value);

                if (_context.PanAbsoluteValue == roundedValue)
                {
                    return;
                }

                _context.PanAbsoluteValue = roundedValue;

                _context.OnPropertyChanged();
            }
        }

        internal double? TiltAbsoluteValue
        {
            get { return _context.TiltAbsoluteValue; }

            set
            {
                double? roundedValue = RoundNullableAngle(value);

                if (_context.TiltAbsoluteValue == roundedValue)
                {
                    return;
                }

                _context.TiltAbsoluteValue = roundedValue;

                _context.OnPropertyChanged();
            }
        }

        internal int? ZoomPositionValue
        {
            get { return _context.ZoomPositionValue; }

            set
            {
                if (_context.ZoomPositionValue == value)
                {
                    return;
                }

                _context.ZoomPositionValue = value;

                _context.OnPropertyChanged();
            }
        }

        internal double? ZoomRatioValue
        {
            get { return _context.ZoomRatioValue; }

            set
            {
                double? roundedValue = value.HasValue ? Math.Round(value.Value, 1, MidpointRounding.AwayFromZero) : (double?)null;

                if (_context.ZoomRatioValue == roundedValue)
                {
                    return;
                }

                _context.ZoomRatioValue = roundedValue;

                _context.OnPropertyChanged();
            }
        }

        internal int? FocusPositionValue
        {
            get { return _context.FocusPositionValue; }

            set
            {
                if (_context.FocusPositionValue == value)
                {
                    return;
                }

                _context.FocusPositionValue = value;

                _context.OnPropertyChanged();
            }
        }

        internal ObservableCollection<int> LaPresetSlotOptions { get; set; }

        internal ObservableCollection<PresetPointOption> LaPresetPoints { get; set; }

        internal int LaPresetSlotNumber
        {
            get { return _context.LaPresetSlotNumberState; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(100, value));

                if (_context.LaPresetSlotNumberState == safeValue)
                {
                    return;
                }

                _context.LaPresetSlotNumberState = safeValue;

                _context.OnPropertyChanged();

                SelectedLaPresetPoint = LaPresetPoints.FirstOrDefault(preset => preset.Number == safeValue);
            }
        }

        internal PresetPointOption SelectedLaPresetPoint
        {
            get { return _context.SelectedLaPresetPoint; }

            set
            {
                if (ReferenceEquals(_context.SelectedLaPresetPoint, value))
                {
                    return;
                }

                _context.SelectedLaPresetPoint = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SelectedLaPresetDetailText));

                if (value != null && _context.LaPresetSlotNumberState != value.Number)
                {
                    _context.LaPresetSlotNumberState = value.Number;

                    _context.OnPropertyChanged(nameof(LaPresetSlotNumber));
                }
            }
        }

        internal string CurrentLaPresetSnapshotText
        {
            get
            {
                return $"PAN      : {_context.CurrentPan:F2}°\n" + $"TILT     : {_context.CurrentTilt:F2}°\n" + $"EO ZOOM  : {CurrentEoZoomText} / 1000\n" + $"EO FOCUS : {CurrentEoFocusText} / 1000\n" + $"IR ZOOM  : {CurrentIrZoomText} / 1000\n" + $"IR FOCUS : {CurrentIrFocusText} / 1000";
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
            get { return _context.LaPresetScanSpeedState; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context.LaPresetScanSpeedState == safeValue)
                {
                    return;
                }

                _context.LaPresetScanSpeedState = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal int LaPresetScanDelay
        {
            get { return _context.LaPresetScanDelayState; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context.LaPresetScanDelayState == safeValue)
                {
                    return;
                }

                _context.LaPresetScanDelayState = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal string LaPresetCommandStatusText
        {
            get { return _context.LaPresetCommandStatusTextState; }

            set
            {
                if (_context.LaPresetCommandStatusTextState == value)
                {
                    return;
                }

                _context.LaPresetCommandStatusTextState = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsLaPresetScanRunning
        {
            get { return _context.IsLaPresetScanRunning; }

            set
            {
                if (_context.IsLaPresetScanRunning == value)
                {
                    return;
                }

                _context.IsLaPresetScanRunning = value;

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
            get { return _context.PresetSlotNumberState; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(100, value));

                if (_context.PresetSlotNumberState == safeValue)
                {
                    return;
                }

                _context.PresetSlotNumberState = safeValue;

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
            get { return _context.SelectedPresetPoint; }

            set
            {
                if (ReferenceEquals(_context.SelectedPresetPoint, value))
                {
                    return;
                }

                _context.SelectedPresetPoint = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SelectedPresetDetailText));

                if (value != null && _context.PresetSlotNumberState != value.Number)
                {
                    _context.PresetSlotNumberState = value.Number;

                    _context.OnPropertyChanged(nameof(PresetSlotNumber));
                }
            }
        }

        internal string CurrentPresetSnapshotText
        {
            get
            {
                return $"PAN      : {_context.CurrentPan:F2}°\n" + $"TILT     : {_context.CurrentTilt:F2}°\n" + $"EO ZOOM  : {CurrentEoZoomText} / 1000\n" + $"EO FOCUS : {CurrentEoFocusText} / 1000\n" + $"IR ZOOM  : {CurrentIrZoomText} / 1000\n" + $"IR FOCUS : {CurrentIrFocusText} / 1000";
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
            get { return _context.PresetScanSpeedState; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context.PresetScanSpeedState == safeValue)
                {
                    return;
                }

                _context.PresetScanSpeedState = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal int PresetScanDelay
        {
            get { return _context.PresetScanDelayState; }

            set
            {
                int safeValue = Math.Max(1, Math.Min(60, value));

                if (_context.PresetScanDelayState == safeValue)
                {
                    return;
                }

                _context.PresetScanDelayState = safeValue;

                _context.OnPropertyChanged();

                _context.SavePresetStorage();
            }
        }

        internal string PresetCommandStatusText
        {
            get { return _context.PresetCommandStatusTextState; }

            set
            {
                if (_context.PresetCommandStatusTextState == value)
                {
                    return;
                }

                _context.PresetCommandStatusTextState = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsPresetScanRunning
        {
            get { return _context.IsPresetScanRunning; }

            set
            {
                if (_context.IsPresetScanRunning == value)
                {
                    return;
                }

                _context.IsPresetScanRunning = value;

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
            get { return _context.EoSourceAddress; }

            set
            {
                if (_context.EoSourceAddress == value)
                {
                    return;
                }

                _context.EoSourceAddress = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(value);

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
            get { return _context.IrSourceAddress; }

            set
            {
                if (_context.IrSourceAddress == value)
                {
                    return;
                }

                _context.IrSourceAddress = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(value);

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
            get { return _context.ControlControlAgentIp; }

            set
            {
                if (_context.ControlControlAgentIp == value)
                {
                    return;
                }

                _context.ControlControlAgentIp = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(_context.IsImuRpyTabVisible));

                _context.OnPropertyChanged(nameof(_context.IsGpsTabVisible)); // 추가

                _context.EnsureVisibleCommunicationSettingsTab();
            }
        }

        internal string McbMaintenanceIpAddress
        {
            get { return _context.McbMaintenanceIpAddress; }

            set
            {
                if (_context.McbMaintenanceIpAddress == value)
                {
                    return;
                }

                _context.McbMaintenanceIpAddress = value;

                _context.OnPropertyChanged();
            }
        }

        internal string ControlAgentPortText
        {
            get { return _context.ControlControlAgentPortText; }

            set
            {
                if (_context.ControlControlAgentPortText == value)
                {
                    return;
                }

                _context.ControlControlAgentPortText = value;

                _context.OnPropertyChanged();
            }
        }

        internal BitmapSource EOCameraImage
        {
            get { return _context.EoCameraImage; }

            set
            {
                if (_context.EoCameraImage != value)
                {
                    _context.EoCameraImage = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal BitmapSource IRCameraImage
        {
            get { return _context.IrCameraImage; }

            set
            {
                if (_context.IrCameraImage != value)
                {
                    _context.IrCameraImage = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal int EoVideoWidth
        {
            get { return _context.EoVideoWidth; }

            set
            {
                _context.EoVideoWidth = value;

                _context.OnPropertyChanged();
            }
        }

        internal int EoVideoHeight
        {
            get { return _context.EoVideoHeight; }

            set
            {
                _context.EoVideoHeight = value;

                _context.OnPropertyChanged();
            }
        }

        internal int IrVideoWidth
        {
            get { return _context.IrVideoWidth; }

            set
            {
                _context.IrVideoWidth = value;

                _context.OnPropertyChanged();
            }
        }

        internal int IrVideoHeight
        {
            get { return _context.IrVideoHeight; }

            set
            {
                _context.IrVideoHeight = value;

                _context.OnPropertyChanged();
            }
        }

        internal string AiControlAgentIp
        {
            get { return _context.AiControlAgentIpState; }

            set
            {
                if (_context.AiControlAgentIpState != value)
                {
                    _context.AiControlAgentIpState = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal int AiAgentPort
        {
            get { return _context.AiAgentPort; }

            set
            {
                if (_context.AiAgentPort != value)
                {
                    _context.AiAgentPort = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal string AiRtsp0Address
        {
            get { return _context.AiRtsp0Address; }

            set
            {
                if (_context.AiRtsp0Address != value)
                {
                    _context.AiRtsp0Address = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(SelectedAiEoRtspSource));

                    _context.OnPropertyChanged(nameof(IsAiEoRtspDirectInput));
                }
            }
        }

        internal string AiRtsp1Address
        {
            get { return _context.AiRtsp1Address; }

            set
            {
                if (_context.AiRtsp1Address != value)
                {
                    _context.AiRtsp1Address = value;

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
            get { return _context.AiRtsp0OnnxIndex; }

            set
            {
                if (_context.AiRtsp0OnnxIndex != value)
                {
                    _context.AiRtsp0OnnxIndex = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal int AiRtsp1OnnxIndex
        {
            get { return _context.AiRtsp1OnnxIndex; }

            set
            {
                if (_context.AiRtsp1OnnxIndex != value)
                {
                    _context.AiRtsp1OnnxIndex = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal double AiMappingConfidence
        {
            get { return _context.AiMappingConfidence; }

            set
            {
                if (_context.AiMappingConfidence != value)
                {
                    _context.AiMappingConfidence = value;

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
            get { return _context.AiMappingIou; }

            set
            {
                if (_context.AiMappingIou != value)
                {
                    _context.AiMappingIou = value;

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
            get { return _context.AiDisplayConfidenceThreshold; }

            set
            {
                if (_context.AiDisplayConfidenceThreshold != value)
                {
                    _context.AiDisplayConfidenceThreshold = value;

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
            get { return _context.AiSettingStatusText; }

            set
            {
                if (_context.AiSettingStatusText != value)
                {
                    _context.AiSettingStatusText = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal string AiPowerStatusText
        {
            get { return _context.AiPowerStatusText; }

            set
            {
                if (_context.AiPowerStatusText != value)
                {
                    _context.AiPowerStatusText = value;

                    _context.OnPropertyChanged();

                    _context.OnPropertyChanged(nameof(_context.AiAlertStatusText));
                }
            }
        }

        internal bool IsAutoTrackingEnabled
        {
            get { return _context.IsAutoTrackingEnabled; }

            set
            {
                if (_context.IsAutoTrackingEnabled != value)
                {
                    _context.IsAutoTrackingEnabled = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal bool IsCrosshairVisible
        {
            get { return _context.IsCrosshairVisible; }

            set
            {
                if (_context.IsCrosshairVisible == value)
                {
                    return;
                }

                _context.IsCrosshairVisible = value;

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
            get { return _context.CrosshairBrush; }
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
            get { return _context.ControlAgentConnectionStatusText; }

            set
            {
                if (_context.ControlAgentConnectionStatusText == value)
                {
                    return;
                }

                _context.ControlAgentConnectionStatusText = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(_context.DeviceControlConnectionStatusText));

                _context.OnPropertyChanged(nameof(IsContextPanelsVisible));
            }
        }

        internal string ControlAgentConnectionStatusColor
        {
            get { return _context.ControlAgentConnectionStatusColor; }

            set
            {
                if (_context.ControlAgentConnectionStatusColor == value)
                {
                    return;
                }

                _context.ControlAgentConnectionStatusColor = value;

                _context.OnPropertyChanged();
            }
        }

        internal byte PanTiltSpeedLevel
        {
            get { return _context.PanTiltSpeedLevel; }

            set
            {
                byte normalizedValue = (byte)Math.Max(1, Math.Min(60, (int)value));

                if (_context.PanTiltSpeedLevel != normalizedValue)
                {
                    _context.PanTiltSpeedLevel = normalizedValue;

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
                if (!_context.IsDeviceConnectionRequested)
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
                return $"SET : {PanTiltSpeedLevel} / " + $"ACTIVE({(_context.ActivePanTiltZoomSource == 1 ? "IR" : "EO")}) : " + $"{EffectivePanTiltSpeedLevel} / 60";
            }
        }

        internal string LrfDistanceText
        {
            get { return _context.LrfDistanceText; }

            set
            {
                if (_context.LrfDistanceText != value)
                {
                    _context.LrfDistanceText = value;

                    _context.OnPropertyChanged();
                }
            }
        }

        internal string EoStatusText
        {
            get
            {
                if (_context.EoAlreadyConnectedNotice)
                {
                    return "[EO] Already Connected...";
                }
                else
                {
                    return _context.EoStatusText;
                }
            }

            set
            {
                _context.EoAlreadyConnectedNotice = false;

                if (_context.EoStatusText == value)
                {
                    return;
                }

                _context.EoStatusText = value;

                if (_context.IsSelectedConnectionTarget && value == "[EO] Connected" && (!_context.IsGpsTabVisible || !_context.HasDevicePowerStatus || _context.IsDevicePowerStatusStale))
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
                if (_context.IrAlreadyConnectedNotice)
                {
                    return "[IR] Already Connected...";
                }
                else
                {
                    return _context.IrStatusText;
                }
            }

            set
            {
                _context.IrAlreadyConnectedNotice = false;

                if (_context.IrStatusText == value)
                {
                    return;
                }

                _context.IrStatusText = value;

                if (_context.IsSelectedConnectionTarget && value == "[IR] Connected" && (!_context.IsGpsTabVisible || !_context.HasDevicePowerStatus || _context.IsDevicePowerStatusStale))
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
                return GetRtspConnectionStatusText(_context.EoStatusText, "[EO]");
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
                return GetRtspConnectionStatusText(_context.IrStatusText, "[IR]");
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
                return $"{NormalizeToSignedAngle(_context.CurrentPan):F2}°";
            }
        }

        internal string CurrentTiltText
        {
            get
            {
                return $"{_context.CurrentTilt:F2}°";
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
                if (_context.ControlCommandService != null && _context.IsGpsTabVisible && _context.HasDevicePowerStatus)
                {
                    // 2026-10-06: WebAgent는 Function 0x31의 보드 상태를 단일 기준으로 사용한다.
                    // 첫 상태 패킷 전에는 OFF로 오인하지 않도록 UNKNOWN을 유지한다.
                    return $"CONTROL:{CurrentControlPowerText} / " + $"EO:{CurrentEoPowerText} / " + $"IR:{CurrentIrPowerText}";
                }

                bool isPanOn = (_context.CurrentPowerStatus & 0x80) != 0;

                bool isTiltOn = (_context.CurrentPowerStatus & 0x40) != 0;

                bool isEoOn = _context.EoStatusText == "[EO] Connected";

                bool isIrOn = _context.IrStatusText == "[IR] Connected";

                return $"CONTROL:{ToOnOff(isPanOn && isTiltOn)} / " + $"EO:{ToOnOff(isEoOn)} / " + $"IR:{ToOnOff(isIrOn)}";
            }
        }

        internal string CurrentControlPowerText
        {
            get
            {
                if (_context.ControlCommandService != null && _context.IsGpsTabVisible && _context.HasDevicePowerStatus)
                {
                    if (_context.HasDevicePowerStatus && !_context.IsDevicePowerStatusStale)
                    {
                        return ToOnOff((_context.CurrentMcbPowerStatus & 0x01) != 0);
                    }
                    else
                    {
                        return "UNKNOWN";
                    }
                }

                bool isPanOn = (_context.CurrentPowerStatus & 0x80) != 0;

                bool isTiltOn = (_context.CurrentPowerStatus & 0x40) != 0;

                return ToOnOff(isPanOn && isTiltOn);
            }
        }

        internal string CurrentEoPowerText
        {
            get
            {
                if (_context.ControlCommandService != null && _context.IsGpsTabVisible && _context.HasDevicePowerStatus)
                {
                    if (_context.HasDevicePowerStatus && !_context.IsDevicePowerStatusStale)
                    {
                        return ToOnOff((_context.CurrentScbPowerStatus & 0x01) != 0);
                    }
                    else
                    {
                        return "UNKNOWN";
                    }
                }

                return GetLegacyCameraPowerText(_context.EoStatusText, "EO");
            }
        }

        internal string CurrentIrPowerText
        {
            get
            {
                if (_context.ControlCommandService != null && _context.IsGpsTabVisible && _context.HasDevicePowerStatus)
                {
                    if (_context.HasDevicePowerStatus && !_context.IsDevicePowerStatusStale)
                    {
                        return ToOnOff((_context.CurrentMcbPowerStatus & 0x20) != 0);
                    }
                    else
                    {
                        return "UNKNOWN";
                    }
                }

                return GetLegacyCameraPowerText(_context.IrStatusText, "IR");
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

        /// <summary>
        /// ROOFTOP / ENVIRONMENT 전환 직후에도 현재값과 입력값이 새 좌표계로 즉시 표시되게 한다. 실제 장비 상태는 다음 수신
        /// 패킷에서 다시 갱신된다.
        /// </summary>
        internal void ConvertPanTiltValuesForSelectedCoordinateMode()
        {
            _context.CurrentPan = NormalizeToSignedAngle(_context.CurrentPan);

            _context.CurrentTilt = Math.Max(_context.CurrentMoveControlTiltMinimum, Math.Min(_context.CurrentMoveControlTiltMaximum, NormalizeToSignedAngle(_context.CurrentTilt)));

            PanAbsoluteValue = PanAbsoluteValue.HasValue ? NormalizeToSignedAngle(PanAbsoluteValue.Value) : (double?)null;

            TiltAbsoluteValue = TiltAbsoluteValue.HasValue ? Math.Max(_context.CurrentMoveControlTiltMinimum, Math.Min(_context.CurrentMoveControlTiltMaximum, NormalizeToSignedAngle(TiltAbsoluteValue.Value))) : (double?)null;

            _context.OnPropertyChanged(nameof(CurrentPanText));

            _context.OnPropertyChanged(nameof(CurrentTiltText));
        }

        /// <summary>
        /// 각도를 0~360도 범위로 정규화한다.
        /// </summary>
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

        /// <summary>
        /// 각도를 -180~180도 부호 있는 범위로 정규화한다.
        /// </summary>
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

        /// <summary>
        /// 영상 화면 상태 문자열을 통신 설정 화면용 RTSP 연결 상태 문자열로 변환 실제 영상 상태 문자열에는 재연결 횟수 또는 부가 문구가 포함될
        /// 수 있으므로 완전 일치가 아닌 상태 키워드 포함 여부를 기준으로 판단한다. 예시: "[EO] Connected" ->; "Connected"
        /// "[EO] Connecting..." ->; "Connecting" "[EO] Reconnecting... (4)" ->;
        /// "Reconnecting" "[IR] Disconnected" ->; "Disconnected"
        /// </summary>
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

        /// <summary>
        /// RTSP 연결 상태 문자열에 맞는 화면 표시 색상 반환 Control Agent 연결 상태와 동일한 색상 기준을 사용한다.
        /// </summary>
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

        /// <summary>
        /// CTEC 원시 위치를 로그 확인용 현재값 / 최대값 / 퍼센트 문자열로 만든다. 화면 표시는 0~1000으로 통일하지만 수신 로그에는
        /// 원시값을 유지한다.
        /// </summary>
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

        /// <summary>
        /// 불리언 상태를 ON 또는 OFF 문구로 변환한다.
        /// </summary>
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

        /// <summary>
        /// 해당 채널에 현재 표시할 AI 탐지가 있는지 확인한다.
        /// </summary>
        internal static bool HasAiDetection(IEnumerable<AiDetectionBox> boxes, params string[] alarmClasses)
        {
            if (boxes == null || alarmClasses == null || alarmClasses.Length == 0)
            {
                return false;
            }

            return boxes.Any(box => box != null && alarmClasses.Any(alarmClass => string.Equals(box.ClassName?.Trim(), alarmClass, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// AI BBox 갱신·Hold 만료·연결 해제 시 계산 속성의 UI 바인딩을 갱신한다. Collection은 기존 모든 AI 클래스를 그대로
        /// 유지하고 경고 속성만 다시 계산한다.
        /// </summary>
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

        /// <summary>
        /// 탐지 경고 상태가 바뀐 순간에만 전환 로그를 남긴다.
        /// </summary>
        internal static void LogDetectionWarningTransition(string detectionType, string channel, bool current, bool local, bool ai, ref bool previous)
        {
            if (current == previous)
            {
                return;
            }

            previous = current;

            ConsoleLogHelper.State(detectionType + " ALERT", (current ? "Warning displayed" : "Warning cleared") + " / CHANNEL=" + channel + " / LOCAL=" + local + " / AI=" + ai);
        }

        /// <summary>
        /// 로컬 화재·연기 탐지의 사용 여부를 관련 상태에 반영한다.
        /// </summary>
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
