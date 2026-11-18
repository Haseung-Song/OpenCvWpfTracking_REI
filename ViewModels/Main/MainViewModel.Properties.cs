using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Models.Main;

using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// XAML 바인딩 Command, 속성, 상태 문자열과 화면용 Collection을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// [EO / IR] 영상 중앙 십자선 표시 상태 전환 [Command]
        /// </summary>
        public ICommand ToggleCrosshairCommand
        {
            get { return _propertiesFeature.ToggleCrosshairCommand; }
            private set { _propertiesFeature.ToggleCrosshairCommand = value; }
        }

        /// <summary>
        /// CURRENT STATUS를 옥상장비 상태로 전환 [Command]
        /// </summary>
        public ICommand ShowRooftopStatusCommand
        {
            get { return _propertiesFeature.ShowRooftopStatusCommand; }
            private set { _propertiesFeature.ShowRooftopStatusCommand = value; }
        }

        /// <summary>
        /// CURRENT STATUS를 환경장비 상태로 전환 [Command]
        /// </summary>
        public ICommand ShowEnvironmentStatusCommand
        {
            get { return _propertiesFeature.ShowEnvironmentStatusCommand; }
            private set { _propertiesFeature.ShowEnvironmentStatusCommand = value; }
        }

        /// <summary>
        /// Zoom Sync 이전 Level 선택 [Command]
        /// </summary>
        public ICommand PreviousZoomSyncLevelCommand
        {
            get { return _propertiesFeature.PreviousZoomSyncLevelCommand; }
            private set { _propertiesFeature.PreviousZoomSyncLevelCommand = value; }
        }

        /// <summary>
        /// Zoom Sync 다음 Level 선택 [Command]
        /// </summary>
        public ICommand NextZoomSyncLevelCommand
        {
            get { return _propertiesFeature.NextZoomSyncLevelCommand; }
            private set { _propertiesFeature.NextZoomSyncLevelCommand = value; }
        }

        /// <summary>
        /// 선택한 Zoom Sync Level 적용 [Command]
        /// </summary>
        public ICommand ApplyZoomSyncCommand
        {
            get { return _propertiesFeature.ApplyZoomSyncCommand; }
            private set { _propertiesFeature.ApplyZoomSyncCommand = value; }
        }

        /// <summary>
        /// 진행 중인 Zoom Sync 정지 [Command]
        /// </summary>
        public ICommand StopZoomSyncCommand
        {
            get { return _propertiesFeature.StopZoomSyncCommand; }
            private set { _propertiesFeature.StopZoomSyncCommand = value; }
        }

        /// <summary>
        /// Focus Sync 이전 Level 선택 [Command]
        /// </summary>
        public ICommand PreviousFocusSyncLevelCommand
        {
            get { return _propertiesFeature.PreviousFocusSyncLevelCommand; }
            private set { _propertiesFeature.PreviousFocusSyncLevelCommand = value; }
        }

        /// <summary>
        /// Focus Sync 다음 Level 선택 [Command]
        /// </summary>
        public ICommand NextFocusSyncLevelCommand
        {
            get { return _propertiesFeature.NextFocusSyncLevelCommand; }
            private set { _propertiesFeature.NextFocusSyncLevelCommand = value; }
        }

        /// <summary>
        /// 선택한 Focus Sync Level 적용 [Command]
        /// </summary>
        public ICommand ApplyFocusSyncCommand
        {
            get { return _propertiesFeature.ApplyFocusSyncCommand; }
            private set { _propertiesFeature.ApplyFocusSyncCommand = value; }
        }

        /// <summary>
        /// 진행 중인 Focus Sync 정지 [Command]
        /// </summary>
        public ICommand StopFocusSyncCommand
        {
            get { return _propertiesFeature.StopFocusSyncCommand; }
            private set { _propertiesFeature.StopFocusSyncCommand = value; }
        }

        /// <summary>
        /// 영상 [Connect] 버튼 [Command]
        /// </summary>
        public ICommand ConnectCommand
        {
            get { return _propertiesFeature.ConnectCommand; }
            private set { _propertiesFeature.ConnectCommand = value; }
        }

        /// <summary>
        /// 영상 [Disconnect] 버튼 [Command]
        /// </summary>
        public ICommand DisconnectCommand
        {
            get { return _propertiesFeature.DisconnectCommand; }
            private set { _propertiesFeature.DisconnectCommand = value; }
        }

        /// <summary>
        /// [PAN] 왼쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand PanLeftCommand
        {
            get { return _propertiesFeature.PanLeftCommand; }
            private set { _propertiesFeature.PanLeftCommand = value; }
        }

        /// <summary>
        /// [PAN] 오른쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand PanRightCommand
        {
            get { return _propertiesFeature.PanRightCommand; }
            private set { _propertiesFeature.PanRightCommand = value; }
        }

        /// <summary>
        /// [TILT] 위쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand TiltUpCommand
        {
            get { return _propertiesFeature.TiltUpCommand; }
            private set { _propertiesFeature.TiltUpCommand = value; }
        }

        /// <summary>
        /// [TILT] 아래쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand TiltDownCommand
        {
            get { return _propertiesFeature.TiltDownCommand; }
            private set { _propertiesFeature.TiltDownCommand = value; }
        }

        /// <summary>
        /// [ZOOM] 확대 테스트 [Command]
        /// </summary>
        public ICommand ZoomInCommand
        {
            get { return _propertiesFeature.ZoomInCommand; }
            private set { _propertiesFeature.ZoomInCommand = value; }
        }

        /// <summary>
        /// [ZOOM] 축소 테스트 [Command]
        /// </summary>
        public ICommand ZoomOutCommand
        {
            get { return _propertiesFeature.ZoomOutCommand; }
            private set { _propertiesFeature.ZoomOutCommand = value; }
        }

        /// <summary>
        /// [FOCUS] [Far] 테스트 [Command]
        /// </summary>
        public ICommand FocusFarCommand
        {
            get { return _propertiesFeature.FocusFarCommand; }
            private set { _propertiesFeature.FocusFarCommand = value; }
        }

        /// <summary>
        /// [FOCUS] [Near] 테스트 [Command]
        /// </summary>
        public ICommand FocusNearCommand
        {
            get { return _propertiesFeature.FocusNearCommand; }
            private set { _propertiesFeature.FocusNearCommand = value; }
        }

        /// <summary>
        /// Pan / Tilt Home Position 실행 [Command]
        /// </summary>
        public ICommand MoveHomePositionCommand
        {
            get { return _propertiesFeature.MoveHomePositionCommand; }
            private set { _propertiesFeature.MoveHomePositionCommand = value; }
        }

        /// <summary>
        /// Pan 현재 위치를 0으로 설정 [Command]
        /// </summary>
        public ICommand SetPanZeroCommand
        {
            get { return _propertiesFeature.SetPanZeroCommand; }
            private set { _propertiesFeature.SetPanZeroCommand = value; }
        }

        /// <summary>
        /// Tilt 현재 위치를 0으로 설정 [Command]
        /// </summary>
        public ICommand SetTiltZeroCommand
        {
            get { return _propertiesFeature.SetTiltZeroCommand; }
            private set { _propertiesFeature.SetTiltZeroCommand = value; }
        }

        /// <summary>
        /// Pan Absolute 이동 [Command]
        /// </summary>
        public ICommand MovePanAbsoluteCommand
        {
            get { return _propertiesFeature.MovePanAbsoluteCommand; }
            private set { _propertiesFeature.MovePanAbsoluteCommand = value; }
        }

        /// <summary>
        /// Tilt Absolute 이동 [Command]
        /// </summary>
        public ICommand MoveTiltAbsoluteCommand
        {
            get { return _propertiesFeature.MoveTiltAbsoluteCommand; }
            private set { _propertiesFeature.MoveTiltAbsoluteCommand = value; }
        }

        /// <summary>
        /// [PAN] / [TILT] Absolute 위치 이동 정지 [Command]
        /// </summary>
        public ICommand StopAbsoluteMoveCommand
        {
            get { return _propertiesFeature.StopAbsoluteMoveCommand; }
            private set { _propertiesFeature.StopAbsoluteMoveCommand = value; }
        }

        /// <summary>
        /// EO / IR Zoom Position 이동 [Command]
        /// </summary>
        public ICommand SetZoomPositionCommand
        {
            get { return _propertiesFeature.SetZoomPositionCommand; }
            private set { _propertiesFeature.SetZoomPositionCommand = value; }
        }

        /// <summary>
        /// EO / IR Zoom Ratio 이동 [Command]
        /// </summary>
        public ICommand SetZoomRatioCommand
        {
            get { return _propertiesFeature.SetZoomRatioCommand; }
            private set { _propertiesFeature.SetZoomRatioCommand = value; }
        }

        /// <summary>
        /// EO / IR Focus Position 이동 [Command]
        /// </summary>
        public ICommand SetFocusPositionCommand
        {
            get { return _propertiesFeature.SetFocusPositionCommand; }
            private set { _propertiesFeature.SetFocusPositionCommand = value; }
        }

        /// <summary>
        /// 이동 제어 입력값 초기화 [Command]
        /// </summary>
        public ICommand ResetPositionInputCommand
        {
            get { return _propertiesFeature.ResetPositionInputCommand; }
            private set { _propertiesFeature.ResetPositionInputCommand = value; }
        }

        /// <summary>
        /// PRESET 1 (LA TEST) 현재 PTZF를 LA 스캔 프리셋으로 등록
        /// </summary>
        public ICommand AddOrUpdateLaPresetCommand
        {
            get { return _propertiesFeature.AddOrUpdateLaPresetCommand; }
            private set { _propertiesFeature.AddOrUpdateLaPresetCommand = value; }
        }

        public ICommand ClearAllLaPresetsCommand
        {
            get { return _propertiesFeature.ClearAllLaPresetsCommand; }
            private set { _propertiesFeature.ClearAllLaPresetsCommand = value; }
        }

        /// <summary>
        /// 2026-08-28: PRESET L 목록에서 선택한 한 지점만 제거한다.
        /// </summary>
        public ICommand DeleteSelectedLaPresetCommand
        {
            get { return _propertiesFeature.DeleteSelectedLaPresetCommand; }
            private set { _propertiesFeature.DeleteSelectedLaPresetCommand = value; }
        }

        public ICommand MoveToLaPresetCommand
        {
            get { return _propertiesFeature.MoveToLaPresetCommand; }
            private set { _propertiesFeature.MoveToLaPresetCommand = value; }
        }

        public ICommand StartLaPresetScanCommand
        {
            get { return _propertiesFeature.StartLaPresetScanCommand; }
            private set { _propertiesFeature.StartLaPresetScanCommand = value; }
        }

        public ICommand UpdateLaPresetScanCommand
        {
            get { return _propertiesFeature.UpdateLaPresetScanCommand; }
            private set { _propertiesFeature.UpdateLaPresetScanCommand = value; }
        }

        public ICommand StopLaPresetScanCommand
        {
            get { return _propertiesFeature.StopLaPresetScanCommand; }
            private set { _propertiesFeature.StopLaPresetScanCommand = value; }
        }

        public ICommand StopLaPresetMoveCommand
        {
            get { return _propertiesFeature.StopLaPresetMoveCommand; }
            private set { _propertiesFeature.StopLaPresetMoveCommand = value; }
        }

        /// <summary>
        /// 현재 PTZF 위치를 선택 슬롯에 프리셋으로 추가 / 갱신
        /// </summary>
        public ICommand AddOrUpdatePresetCommand
        {
            get { return _propertiesFeature.AddOrUpdatePresetCommand; }
            private set { _propertiesFeature.AddOrUpdatePresetCommand = value; }
        }

        /// <summary>
        /// 선택 슬롯 프리셋 제거
        /// </summary>
        public ICommand DeletePresetCommand
        {
            get { return _propertiesFeature.DeletePresetCommand; }
            private set { _propertiesFeature.DeletePresetCommand = value; }
        }

        /// <summary>
        /// 2026-08-28: 등록된 PRESET W 전체를 장비와 화면 목록에서 제거한다.
        /// </summary>
        public ICommand ClearAllPresetsCommand
        {
            get { return _propertiesFeature.ClearAllPresetsCommand; }
            private set { _propertiesFeature.ClearAllPresetsCommand = value; }
        }

        /// <summary>
        /// ComboBox에서 선택한 프리셋으로 이동
        /// </summary>
        public ICommand MoveToPresetCommand
        {
            get { return _propertiesFeature.MoveToPresetCommand; }
            private set { _propertiesFeature.MoveToPresetCommand = value; }
        }

        /// <summary>
        /// 프리셋 오토 스캔 시작
        /// </summary>
        public ICommand StartPresetScanCommand
        {
            get { return _propertiesFeature.StartPresetScanCommand; }
            private set { _propertiesFeature.StartPresetScanCommand = value; }
        }

        /// <summary>
        /// 실행 중인 스캔 속도 / 정지시간 변경
        /// </summary>
        public ICommand UpdatePresetScanCommand
        {
            get { return _propertiesFeature.UpdatePresetScanCommand; }
            private set { _propertiesFeature.UpdatePresetScanCommand = value; }
        }

        /// <summary>
        /// 프리셋 오토 스캔 정지
        /// </summary>
        public ICommand StopPresetScanCommand
        {
            get { return _propertiesFeature.StopPresetScanCommand; }
            private set { _propertiesFeature.StopPresetScanCommand = value; }
        }

        public ICommand StopPresetMoveCommand
        {
            get { return _propertiesFeature.StopPresetMoveCommand; }
            private set { _propertiesFeature.StopPresetMoveCommand = value; }
        }

        public ICommand StopActivePresetScanCommand
        {
            get { return _propertiesFeature.StopActivePresetScanCommand; }
            private set { _propertiesFeature.StopActivePresetScanCommand = value; }
        }

        /// <summary>
        /// [LRF] 거리측정 [1회] 요청 [Command]
        /// </summary>
        public ICommand LrfMeasureCommand
        {
            get { return _propertiesFeature.LrfMeasureCommand; }
            private set { _propertiesFeature.LrfMeasureCommand = value; }
        }

        /// <summary>
        /// [PT] 연속 이동 정지 [Command]
        /// </summary>
        public ICommand StopMoveCommand
        {
            get { return _propertiesFeature.StopMoveCommand; }
            private set { _propertiesFeature.StopMoveCommand = value; }
        }

        /// <summary>
        /// [AI Detector Agent] 수동 연결 [Command]
        /// </summary>
        public ICommand ConnectAiAgentCommand
        {
            get { return _propertiesFeature.ConnectAiAgentCommand; }
            private set { _propertiesFeature.ConnectAiAgentCommand = value; }
        }

        /// <summary>
        /// [AI Detector Agent] 수동 연결 해제 [Command]
        /// </summary>
        public ICommand DisconnectAiAgentCommand
        {
            get { return _propertiesFeature.DisconnectAiAgentCommand; }
            private set { _propertiesFeature.DisconnectAiAgentCommand = value; }
        }

        /// <summary>
        /// [AI Detector Agent] [RTSP] 주소 설정 적용 [Command]
        /// </summary>
        public ICommand ApplyAiRtspCommand
        {
            get { return _propertiesFeature.ApplyAiRtspCommand; }
            private set { _propertiesFeature.ApplyAiRtspCommand = value; }
        }

        /// <summary>
        /// [AI Detector Agent] [RTSP] / [ONNX] Mapping 설정 적용 [Command]
        /// </summary>
        public ICommand ApplyAiMappingCommand
        {
            get { return _propertiesFeature.ApplyAiMappingCommand; }
            private set { _propertiesFeature.ApplyAiMappingCommand = value; }
        }

        /// <summary>
        /// [AI Detector Agent] 현재 설정 조회 [Command]
        /// </summary>
        public ICommand RefreshAiSettingCommand
        {
            get { return _propertiesFeature.RefreshAiSettingCommand; }
            private set { _propertiesFeature.RefreshAiSettingCommand = value; }
        }

        /// <summary>
        /// [환경 장비 / 줌 동기화]
        ///
        /// Web Agent 기준 EO / IR Zoom Position을
        /// 0부터 1000까지 100 단위로 구분한 목록
        ///
        /// LEVEL 0  = 0
        /// LEVEL 1  = 100
        /// LEVEL 2  = 200
        /// LEVEL 3  = 300
        /// LEVEL 4  = 400
        /// LEVEL 5  = 500
        /// LEVEL 6  = 600
        /// LEVEL 7  = 700
        /// LEVEL 8  = 800
        /// LEVEL 9  = 900
        /// LEVEL 10 = 1000
        ///
        /// 총 11개 항목을 사용한다.
        /// </summary>
        public ObservableCollection<ZoomSyncLevelOption> ZoomSyncLevelOptions
        {
            get { return _propertiesFeature.ZoomSyncLevelOptions; }
            private set { _propertiesFeature.ZoomSyncLevelOptions = value; }
        }

        /// <summary>
        /// [환경 장비 / 초점 동기화]
        ///
        /// EO / IR Focus Position을 0부터 1000까지
        /// 100 단위로 구분한 총 11개의 단계 목록이다.
        ///
        /// LEVEL 0  = 0
        /// LEVEL 1  = 100
        /// ...
        /// LEVEL 10 = 1000
        /// </summary>
        public ObservableCollection<ZoomSyncLevelOption> FocusSyncLevelOptions
        {
            get { return _propertiesFeature.FocusSyncLevelOptions; }
            private set { _propertiesFeature.FocusSyncLevelOptions = value; }
        }

        /// <summary>
        /// CURRENT STATUS 화면에 표시할 장비 구성
        /// </summary>
        public EquipmentStatusMode SelectedEquipmentStatusMode
        {
            get { return _propertiesFeature.SelectedEquipmentStatusMode; }
            set { _propertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        public bool IsRooftopStatusSelected
        {
            get { return _propertiesFeature.IsRooftopStatusSelected; }
        }

        public bool IsEnvironmentStatusSelected
        {
            get { return _propertiesFeature.IsEnvironmentStatusSelected; }
        }

        public string PanTiltCoordinateRangeText
        {
            get { return _propertiesFeature.PanTiltCoordinateRangeText; }
        }

        public string PanAbsoluteRangeLabel
        {
            get { return _propertiesFeature.PanAbsoluteRangeLabel; }
        }

        public string TiltAbsoluteRangeLabel
        {
            get { return _propertiesFeature.TiltAbsoluteRangeLabel; }
        }

        internal static double NormalizeToSignedAngle(double angle)
        {
            return PropertiesFeature.NormalizeToSignedAngle(angle);
        }

        public bool IsRooftopThermalPaletteVisible
        {
            get { return _propertiesFeature.IsRooftopThermalPaletteVisible; }
        }

        public bool IsEnvironmentThermalPaletteVisible
        {
            get { return _propertiesFeature.IsEnvironmentThermalPaletteVisible; }
        }

        public string CurrentStatusEquipmentText
        {
            get { return _propertiesFeature.CurrentStatusEquipmentText; }
        }

        /// <summary>
        /// HOME / ZERO UI 표시 여부.
        ///
        /// 2026-09-16: HOME POSITION은 ROOFTOP과 ENVIRONMENT가 공통 사용한다.
        /// PAN ZERO/TILT ZERO만 기존 MCB/LA 전용으로 유지한다.
        /// </summary>
        public bool IsHomeZeroVisible
        {
            get { return _propertiesFeature.IsHomeZeroVisible; }
        }

        // 2026-09-29: ZERO 버튼은 Agent 종류에 따라 송신 경로만 분리한다.
        // LA Agent는 기존 MCB 유지보수 직접 시퀀스를 보존하고,
        // WebAgent는 GUI-Agent 7바이트 ZERO 요청을 전송한다.
        public bool IsPanTiltZeroVisible
        {
            get { return _propertiesFeature.IsPanTiltZeroVisible; }
        }

        public bool IsPanTiltZeroCommandEnabled
        {
            get { return _propertiesFeature.IsPanTiltZeroCommandEnabled; }
        }

        public string PanTiltZeroRouteText
        {
            get { return _propertiesFeature.PanTiltZeroRouteText; }
        }

        public string HomeControlHeaderText
        {
            get { return _propertiesFeature.HomeControlHeaderText; }
        }

        /// <summary>
        /// HOME / ZERO 잠금 화면의 작업 제목.
        /// HOME POSITION, PAN ZERO, TILT ZERO를 각각 구분하여 표시한다.
        /// </summary>
        public string HomeZeroLockTitle
        {
            get { return _propertiesFeature.HomeZeroLockTitle; }
            private set { _propertiesFeature.HomeZeroLockTitle = value; }
        }

        /// <summary>
        /// HOME / ZERO 잠금 화면의 현재 처리 단계.
        /// </summary>
        public string HomeZeroLockMessage
        {
            get { return _propertiesFeature.HomeZeroLockMessage; }
            private set { _propertiesFeature.HomeZeroLockMessage = value; }
        }

        /// <summary>
        /// HOME / ZERO 작업 진행 여부.
        /// </summary>
        public bool IsHomePositionMoving
        {
            get { return _propertiesFeature.IsHomePositionMoving; }
            private set { _propertiesFeature.IsHomePositionMoving = value; }
        }

        /// <summary>
        /// 우측 상태 확인 UI 활성 여부.
        /// HOME / ZERO 중에는 기존 전체 잠금을 유지하지만,
        /// AUTO SCAN 중에는 CURRENT STATUS와 PRESET 화면을 계속 확인할 수 있다.
        /// </summary>
        public bool IsMainControlEnabled
        {
            get { return _propertiesFeature.IsMainControlEnabled; }
        }

        /// <summary>
        /// 실제 장비 명령을 발생시키는 UI 활성 여부.
        /// HOME / ZERO, 파노라마 처리 및 AUTO SCAN 중에는 장비 명령을 잠근다.
        /// 2026-08-24: AUTO SCAN 중 PRESET L / PRESET W 탭 전환과 상태 확인만 허용한다.
        /// </summary>
        public bool IsOperationCommandEnabled
        {
            get { return _propertiesFeature.IsOperationCommandEnabled; }
        }

        /// <summary>
        /// 2026-08-24: 운용 제어 하위 탭 자체의 이동 가능 여부.
        /// AUTO SCAN 중에는 PRESET L / PRESET W 화면 확인과 스크롤을 허용하지만,
        /// 파노라마 촬영·정합·저장 및 HOME / ZERO 중에는 탭 이동까지 차단한다.
        /// </summary>
        public bool IsOperationTabNavigationEnabled
        {
            get { return _propertiesFeature.IsOperationTabNavigationEnabled; }
        }

        /// <summary>
        /// 2026-08-21: HOME, AUTO SCAN, 파노라마 작업 중 이벤트 목록 변경 버튼을 잠근다.
        /// </summary>
        public bool IsEventAlertControlEnabled
        {
            get { return _propertiesFeature.IsEventAlertControlEnabled; }
        }

        /// <summary>
        /// 2026-08-26: 작업 중 PAN/TILT SPEED 안내 문구를 유지하고 명령 입력을 차단한다.
        /// </summary>
        public bool IsOperationLockOverlayVisible
        {
            get { return _propertiesFeature.IsOperationLockOverlayVisible; }
        }

        /// <summary>
        /// 우측 상위 탭 선택 상태.
        /// PAN / TILT SPEED는 운용 제어 탭에서만 사용할 수 있다.
        /// </summary>
        public int SelectedRightPanelTabIndex
        {
            get { return _propertiesFeature.SelectedRightPanelTabIndex; }
            set { _propertiesFeature.SelectedRightPanelTabIndex = value; }
        }

        /// <summary>
        /// 운용 제어 하위 탭 선택 상태.
        /// 2026-08-21: 상단 PAN / TILT SPEED는 운용 제어의 모든 하위 탭에서 사용한다.
        /// </summary>
        public int SelectedOperationControlTabIndex
        {
            get { return _propertiesFeature.SelectedOperationControlTabIndex; }
            set { _propertiesFeature.SelectedOperationControlTabIndex = value; }
        }

        /// <summary>
        /// 상단 PAN / TILT SPEED 슬라이더 활성 여부.
        /// 수동 PTZF용 속도와 PRESET용 Speed(1 ~ 60)를 명확히 분리한다.
        /// </summary>
        public bool IsPanTiltSpeedControlEnabled
        {
            get { return _propertiesFeature.IsPanTiltSpeedControlEnabled; }
        }

        public bool IsPresetScanControlLocked
        {
            get { return _propertiesFeature.IsPresetScanControlLocked; }
        }

        /// <summary>
        /// 2026-08-24: HOME, PAN/TILT ZERO, AUTO SCAN 및 파노라마 작업 중
        /// 방향키, 렌즈키와 R/T 영상 전환을 포함한 모든 키보드 장비 제어를 차단한다.
        /// 화면의 작업 중지 버튼은 별도의 마우스 명령으로 계속 사용할 수 있다.
        /// </summary>
        public bool IsControlInputLocked
        {
            get { return _propertiesFeature.IsControlInputLocked; }
        }

        public string ControlLockTitle
        {
            get { return _propertiesFeature.ControlLockTitle; }
        }

        public string ControlLockMessage
        {
            get { return _propertiesFeature.ControlLockMessage; }
        }

        /// <summary>
        /// 현재 선택된 Zoom Sync Level
        /// </summary>
        public ZoomSyncLevelOption SelectedZoomSyncLevel
        {
            get { return _propertiesFeature.SelectedZoomSyncLevel; }
            set { _propertiesFeature.SelectedZoomSyncLevel = value; }
        }

        public string SelectedZoomSyncLevelText
        {
            get { return _propertiesFeature.SelectedZoomSyncLevelText; }
        }

        public string ZoomSyncStatusText
        {
            get { return _propertiesFeature.ZoomSyncStatusText; }
            private set { _propertiesFeature.ZoomSyncStatusText = value; }
        }

        /// <summary>
        /// 현재 선택된 Focus Sync Level
        /// </summary>
        public ZoomSyncLevelOption SelectedFocusSyncLevel
        {
            get { return _propertiesFeature.SelectedFocusSyncLevel; }
            set { _propertiesFeature.SelectedFocusSyncLevel = value; }
        }

        /// <summary>
        /// Focus Sync 표준 위치 표시 문자열
        /// </summary>
        public string SelectedFocusSyncPositionText
        {
            get { return _propertiesFeature.SelectedFocusSyncPositionText; }
        }

        /// <summary>
        /// Focus Sync 실행 상태 표시 문자열
        /// </summary>
        public string FocusSyncStatusText
        {
            get { return _propertiesFeature.FocusSyncStatusText; }
            private set { _propertiesFeature.FocusSyncStatusText = value; }
        }

        public string RooftopEoZoomStatusText
        {
            get { return _propertiesFeature.RooftopEoZoomStatusText; }
        }

        public string RooftopEoFocusStatusText
        {
            get { return _propertiesFeature.RooftopEoFocusStatusText; }
        }

        public string RooftopIrZoomStatusText
        {
            get { return _propertiesFeature.RooftopIrZoomStatusText; }
        }

        public string RooftopIrFocusStatusText
        {
            get { return _propertiesFeature.RooftopIrFocusStatusText; }
        }

        public string EnvironmentEoZoomStatusText
        {
            get { return _propertiesFeature.EnvironmentEoZoomStatusText; }
        }

        public string EnvironmentEoFocusStatusText
        {
            get { return _propertiesFeature.EnvironmentEoFocusStatusText; }
        }

        public string EnvironmentIrZoomStatusText
        {
            get { return _propertiesFeature.EnvironmentIrZoomStatusText; }
        }

        public string EnvironmentIrFocusStatusText
        {
            get { return _propertiesFeature.EnvironmentIrFocusStatusText; }
        }

        /// <summary>
        /// Home / Zero 실행 상태 표시
        /// </summary>
        public string HomeZeroStatusText
        {
            get { return _propertiesFeature.HomeZeroStatusText; }
            private set { _propertiesFeature.HomeZeroStatusText = value; }
        }

        /// <summary>
        /// Pan Absolute 이동 시 장비 최단거리 모드(0x4D / 0x02)를 사용하는지 여부
        /// </summary>
        public bool IsPanTurnShortMode
        {
            get { return _propertiesFeature.IsPanTurnShortMode; }
            set { _propertiesFeature.IsPanTurnShortMode = value; }
        }

        /// <summary>
        /// Pan Absolute 이동 시 장비 원점 통과 모드(0x4D / 0x01)를 사용하는지 여부
        /// </summary>
        public bool IsPanTurnViaZeroMode
        {
            get { return _propertiesFeature.IsPanTurnViaZeroMode; }
            set { _propertiesFeature.IsPanTurnViaZeroMode = value; }
        }

        public bool IsPresetSavedOrderMode
        {
            get { return _propertiesFeature.IsPresetSavedOrderMode; }
            set { _propertiesFeature.IsPresetSavedOrderMode = value; }
        }

        public bool IsPresetNearestOrderMode
        {
            get { return _propertiesFeature.IsPresetNearestOrderMode; }
            set { _propertiesFeature.IsPresetNearestOrderMode = value; }
        }

        /// <summary>
        /// Pan Absolute 목표값
        /// </summary>
        public double? PanAbsoluteValue
        {
            get { return _propertiesFeature.PanAbsoluteValue; }
            set { _propertiesFeature.PanAbsoluteValue = value; }
        }

        /// <summary>
        /// Tilt Absolute 목표값
        /// </summary>
        public double? TiltAbsoluteValue
        {
            get { return _propertiesFeature.TiltAbsoluteValue; }
            set { _propertiesFeature.TiltAbsoluteValue = value; }
        }

        /// <summary>
        /// EO / IR 공통 Zoom Position
        /// </summary>
        public int? ZoomPositionValue
        {
            get { return _propertiesFeature.ZoomPositionValue; }
            set { _propertiesFeature.ZoomPositionValue = value; }
        }

        /// <summary>
        /// 이동 제어 EO 기준 Zoom Ratio
        ///
        /// 입력 범위:
        /// EO 1.0 ~ 90.0배
        ///
        /// 입력한 EO 배율을 0 ~ 1000 진행률로 변환하고,
        /// 같은 진행률을 IR에 적용한다.
        ///
        /// 예:
        /// EO 1.0배  / IR 1.0배  -> Position 0
        /// EO 50.0배 / IR 5.0배  -> Position 1000
        /// </summary>
        public double? ZoomRatioValue
        {
            get { return _propertiesFeature.ZoomRatioValue; }
            set { _propertiesFeature.ZoomRatioValue = value; }
        }

        /// <summary>
        /// EO / IR 공통 Focus Position
        /// </summary>
        public int? FocusPositionValue
        {
            get { return _propertiesFeature.FocusPositionValue; }
            set { _propertiesFeature.FocusPositionValue = value; }
        }

        /// <summary>
        /// PRESET 1 (LA TEST) ID 선택 목록
        /// </summary>
        public ObservableCollection<int> LaPresetSlotOptions
        {
            get { return _propertiesFeature.LaPresetSlotOptions; }
            private set { _propertiesFeature.LaPresetSlotOptions = value; }
        }

        public ObservableCollection<PresetPointOption> LaPresetPoints
        {
            get { return _propertiesFeature.LaPresetPoints; }
            private set { _propertiesFeature.LaPresetPoints = value; }
        }

        public int LaPresetSlotNumber
        {
            get { return _propertiesFeature.LaPresetSlotNumber; }
            set { _propertiesFeature.LaPresetSlotNumber = value; }
        }

        public PresetPointOption SelectedLaPresetPoint
        {
            get { return _propertiesFeature.SelectedLaPresetPoint; }
            set { _propertiesFeature.SelectedLaPresetPoint = value; }
        }

        public string CurrentLaPresetSnapshotText
        {
            get { return _propertiesFeature.CurrentLaPresetSnapshotText; }
        }

        public string SelectedLaPresetDetailText
        {
            get { return _propertiesFeature.SelectedLaPresetDetailText; }
        }

        public int LaPresetScanSpeed
        {
            get { return _propertiesFeature.LaPresetScanSpeed; }
            set { _propertiesFeature.LaPresetScanSpeed = value; }
        }

        public int LaPresetScanDelay
        {
            get { return _propertiesFeature.LaPresetScanDelay; }
            set { _propertiesFeature.LaPresetScanDelay = value; }
        }

        public string LaPresetCommandStatusText
        {
            get { return _propertiesFeature.LaPresetCommandStatusText; }
            private set { _propertiesFeature.LaPresetCommandStatusText = value; }
        }

        public bool IsLaPresetScanRunning
        {
            get { return _propertiesFeature.IsLaPresetScanRunning; }
            private set { _propertiesFeature.IsLaPresetScanRunning = value; }
        }

        /// <summary>
        /// 프리셋 슬롯 선택 목록
        ///
        /// 프리셋 추가 / 제거 / 이동 명령에서 사용할 수 있는
        /// 1 ~ 100 슬롯을 제공한다.
        /// </summary>
        public ObservableCollection<int> PresetSlotOptions
        {
            get { return _propertiesFeature.PresetSlotOptions; }
            private set { _propertiesFeature.PresetSlotOptions = value; }
        }

        /// <summary>
        /// 현재 프로그램에서 등록 명령을 송신한 프리셋 목록
        ///
        /// TORUSS 응답 프로토콜에는 프리셋 목록 조회 응답이 없으므로
        /// 이 목록은 장비 전체 프리셋 데이터가 아니라
        /// 현재 프로그램 세션에서 관리하는 화면 확인용 목록이다.
        /// </summary>
        public ObservableCollection<PresetPointOption> PresetPoints
        {
            get { return _propertiesFeature.PresetPoints; }
            private set { _propertiesFeature.PresetPoints = value; }
        }

        /// <summary>
        /// 스캔 속도 / 정지시간 선택 목록
        ///
        /// TORUSS 문서 기준:
        /// 1 ~ 60
        /// </summary>
        public ObservableCollection<int> PresetScanValueOptions
        {
            get { return _propertiesFeature.PresetScanValueOptions; }
            private set { _propertiesFeature.PresetScanValueOptions = value; }
        }

        /// <summary>
        /// 프리셋 추가 / 제거 대상 슬롯 번호
        /// </summary>
        public int PresetSlotNumber
        {
            get { return _propertiesFeature.PresetSlotNumber; }
            set { _propertiesFeature.PresetSlotNumber = value; }
        }

        /// <summary>
        /// 등록된 프리셋 ComboBox 선택값
        /// </summary>
        public PresetPointOption SelectedPresetPoint
        {
            get { return _propertiesFeature.SelectedPresetPoint; }
            set { _propertiesFeature.SelectedPresetPoint = value; }
        }

        /// <summary>
        /// 현재 PTZF 상태 Snapshot 표시
        ///
        /// ADD / UPDATE 명령을 송신하면
        /// 이 상태를 선택 슬롯의 화면 확인용 정보로 저장한다.
        /// </summary>
        public string CurrentPresetSnapshotText
        {
            get { return _propertiesFeature.CurrentPresetSnapshotText; }
        }

        /// <summary>
        /// ComboBox에서 선택한 프리셋 상세값
        /// </summary>
        public string SelectedPresetDetailText
        {
            get { return _propertiesFeature.SelectedPresetDetailText; }
        }

        /// <summary>
        /// 오토 스캔 이동 속도
        ///
        /// 범위:
        /// 1 ~ 60
        /// </summary>
        public int PresetScanSpeed
        {
            get { return _propertiesFeature.PresetScanSpeed; }
            set { _propertiesFeature.PresetScanSpeed = value; }
        }

        /// <summary>
        /// 오토 스캔 프리셋 정지시간
        ///
        /// 범위:
        /// 1 ~ 60초
        /// </summary>
        public int PresetScanDelay
        {
            get { return _propertiesFeature.PresetScanDelay; }
            set { _propertiesFeature.PresetScanDelay = value; }
        }

        /// <summary>
        /// 프리셋 / 스캔 마지막 명령 상태
        ///
        /// 별도 ACK가 없으므로 TCP 송신 결과를 표시한다.
        /// </summary>
        public string PresetCommandStatusText
        {
            get { return _propertiesFeature.PresetCommandStatusText; }
            private set { _propertiesFeature.PresetCommandStatusText = value; }
        }

        /// <summary>
        /// 현재 프로그램에서 스캔 시작 명령을 송신한 상태
        /// </summary>
        public bool IsPresetScanRunning
        {
            get { return _propertiesFeature.IsPresetScanRunning; }
            private set { _propertiesFeature.IsPresetScanRunning = value; }
        }

        /// <summary>
        /// [EO] 주간 [RTSP] 주소
        ///
        /// 통신 설정 탭의 EO 카메라 선택 ComboBox와 양방향 바인딩한다.
        /// </summary>
        /// <summary>UI 전용 주소는 인증정보만 사람이 읽는 형태로 표시한다.</summary>
        public string EoSourceDisplayAddress
        {
            get { return _propertiesFeature.EoSourceDisplayAddress; }
            set { _propertiesFeature.EoSourceDisplayAddress = value; }
        }

        /// <summary>프로필·AI 연동·연결에 사용하는 내부 인코딩 주소다.</summary>
        public string EoSourceAddress
        {
            get { return _propertiesFeature.EoSourceAddress; }
            set { _propertiesFeature.EoSourceAddress = value; }
        }

        public RtspSourceOption SelectedEoRtspSource
        {
            get { return _propertiesFeature.SelectedEoRtspSource; }
            set { _propertiesFeature.SelectedEoRtspSource = value; }
        }

        /// <summary>
        /// [IR] 열상 [RTSP] 주소
        ///
        /// 통신 설정 탭의 IR 카메라 선택 ComboBox와 양방향 바인딩한다.
        /// </summary>
        /// <summary>IR 직접 입력 UI와 내부 연결 주소를 분리한다.</summary>
        public string IrSourceDisplayAddress
        {
            get { return _propertiesFeature.IrSourceDisplayAddress; }
            set { _propertiesFeature.IrSourceDisplayAddress = value; }
        }

        /// <summary>내부 IR 연결과 설정 저장에는 인코딩 주소를 유지한다.</summary>
        public string IrSourceAddress
        {
            get { return _propertiesFeature.IrSourceAddress; }
            set { _propertiesFeature.IrSourceAddress = value; }
        }

        /// <summary>
        /// 통신 설정 탭에서 선택된 IR RTSP 프리셋
        /// </summary>
        public RtspSourceOption SelectedIrRtspSource
        {
            get { return _propertiesFeature.SelectedIrRtspSource; }
            set { _propertiesFeature.SelectedIrRtspSource = value; }
        }

        public bool IsEoRtspDirectInput
        {
            get { return _propertiesFeature.IsEoRtspDirectInput; }
        }

        public bool IsIrRtspDirectInput
        {
            get { return _propertiesFeature.IsIrRtspDirectInput; }
        }

        /// <summary>
        /// Control Agent 제어 TCP 연결 IP
        /// </summary>
        public string ControlAgentIp
        {
            get { return _propertiesFeature.ControlAgentIp; }
            set { _propertiesFeature.ControlAgentIp = value; }
        }

        /// <summary>
        /// 옥상 MCB 유지보수 직접 연결 IP
        ///
        /// Control Agent IP와 독립적으로 관리하며,
        /// Pan/Tilt Zero 및 Home fallback 직접 명령에 사용한다.
        /// </summary>
        public string McbMaintenanceIpAddress
        {
            get { return _propertiesFeature.McbMaintenanceIpAddress; }
            set { _propertiesFeature.McbMaintenanceIpAddress = value; }
        }

        /// <summary>
        /// Control Agent 제어 TCP 연결 Port 입력 문자열
        ///
        /// 연결 시점에 int.TryParse로 검증한다.
        /// </summary>
        public string ControlAgentPortText
        {
            get { return _propertiesFeature.ControlAgentPortText; }
            set { _propertiesFeature.ControlAgentPortText = value; }
        }

        /// <summary>
        /// [EOCameraImage] 값 변경 시,
        /// [XAML]의 [Image Source]가 갱신된다.
        /// </summary>
        public BitmapSource EOCameraImage
        {
            get { return _propertiesFeature.EOCameraImage; }
            private set { _propertiesFeature.EOCameraImage = value; }
        }

        /// <summary>
        /// [IRCameraImage] 값 변경 시,
        /// [XAML]의 [Image Source]가 갱신된다.
        /// </summary>
        public BitmapSource IRCameraImage
        {
            get { return _propertiesFeature.IRCameraImage; }
            private set { _propertiesFeature.IRCameraImage = value; }
        }

        /// <summary>
        /// [EO] [RTSP] 원본 영상 너비
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// AI 검출 상자 오버레이 기준 너비로 사용한다.
        /// </summary>
        public int EoVideoWidth
        {
            get { return _propertiesFeature.EoVideoWidth; }
            set { _propertiesFeature.EoVideoWidth = value; }
        }

        /// <summary>
        /// [EO] [RTSP] 원본 영상 높이
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// AI 검출 상자 오버레이 기준 높이로 사용한다.
        /// </summary>
        public int EoVideoHeight
        {
            get { return _propertiesFeature.EoVideoHeight; }
            set { _propertiesFeature.EoVideoHeight = value; }
        }

        /// <summary>
        /// [IR] [RTSP] 원본 영상 너비
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// AI 검출 상자 오버레이 기준 너비로 사용한다.
        /// </summary>
        public int IrVideoWidth
        {
            get { return _propertiesFeature.IrVideoWidth; }
            set { _propertiesFeature.IrVideoWidth = value; }
        }

        /// <summary>
        /// [IR] [RTSP] 원본 영상 높이
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// AI 검출 상자 오버레이 기준 높이로 사용한다.
        /// </summary>
        public int IrVideoHeight
        {
            get { return _propertiesFeature.IrVideoHeight; }
            set { _propertiesFeature.IrVideoHeight = value; }
        }

        /// <summary>
        /// [AI Detector Agent] 연결 [IP]
        /// </summary>
        public string AiControlAgentIp
        {
            get { return _propertiesFeature.AiControlAgentIp; }
            set { _propertiesFeature.AiControlAgentIp = value; }
        }

        /// <summary>
        /// [AI Detector Agent] 연결 [Port]
        /// </summary>
        public int AiAgentPort
        {
            get { return _propertiesFeature.AiAgentPort; }
            set { _propertiesFeature.AiAgentPort = value; }
        }

        /// <summary>
        /// [AI Detector Agent] [RTSP Index 0] 주소
        /// </summary>
        public string AiRtsp0Address
        {
            get { return _propertiesFeature.AiRtsp0Address; }
            set { _propertiesFeature.AiRtsp0Address = value; }
        }

        /// <summary>
        /// [AI Detector Agent] [RTSP Index 1] 주소
        /// </summary>
        public string AiRtsp1Address
        {
            get { return _propertiesFeature.AiRtsp1Address; }
            set { _propertiesFeature.AiRtsp1Address = value; }
        }

        public RtspSourceOption SelectedAiEoRtspSource
        {
            get { return _propertiesFeature.SelectedAiEoRtspSource; }
            set { _propertiesFeature.SelectedAiEoRtspSource = value; }
        }

        public RtspSourceOption SelectedAiIrRtspSource
        {
            get { return _propertiesFeature.SelectedAiIrRtspSource; }
            set { _propertiesFeature.SelectedAiIrRtspSource = value; }
        }

        public bool IsAiEoRtspDirectInput
        {
            get { return _propertiesFeature.IsAiEoRtspDirectInput; }
        }

        public bool IsAiIrRtspDirectInput
        {
            get { return _propertiesFeature.IsAiIrRtspDirectInput; }
        }

        /// <summary>
        /// [RTSP Index 0]에 적용할 [ONNX Index]
        /// </summary>
        public int AiRtsp0OnnxIndex
        {
            get { return _propertiesFeature.AiRtsp0OnnxIndex; }
            set { _propertiesFeature.AiRtsp0OnnxIndex = value; }
        }

        /// <summary>
        /// [RTSP Index 1]에 적용할 [ONNX Index]
        /// </summary>
        public int AiRtsp1OnnxIndex
        {
            get { return _propertiesFeature.AiRtsp1OnnxIndex; }
            set { _propertiesFeature.AiRtsp1OnnxIndex = value; }
        }

        /// <summary>
        /// [AI Detector] [Mapping Confidence] 기준값
        /// </summary>
        public double AiMappingConfidence
        {
            get { return _propertiesFeature.AiMappingConfidence; }
            set { _propertiesFeature.AiMappingConfidence = value; }
        }

        /// <summary>
        /// [AI Detector] Mapping Confidence 화면 표시 문자열
        /// </summary>
        public string AiMappingConfidenceText
        {
            get { return _propertiesFeature.AiMappingConfidenceText; }
        }

        /// <summary>
        /// [AI Detector] [Mapping IOU] 기준값
        /// </summary>
        public double AiMappingIou
        {
            get { return _propertiesFeature.AiMappingIou; }
            set { _propertiesFeature.AiMappingIou = value; }
        }

        /// <summary>
        /// [AI Detector] Mapping IOU 화면 표시 문자열
        /// </summary>
        public string AiMappingIouText
        {
            get { return _propertiesFeature.AiMappingIouText; }
        }

        /// <summary>
        /// 화면 표시용 [Bounding Box] 최소 신뢰도 기준값
        /// </summary>
        public double AiDisplayConfidenceThreshold
        {
            get { return _propertiesFeature.AiDisplayConfidenceThreshold; }
            set { _propertiesFeature.AiDisplayConfidenceThreshold = value; }
        }

        /// <summary>
        /// [AI Detector] 화면 표시용 [Bounding Box] 최소 신뢰도 표시 문자열
        /// </summary>
        public string AiDisplayConfidenceThresholdText
        {
            get { return _propertiesFeature.AiDisplayConfidenceThresholdText; }
        }

        /// <summary>
        /// [AI Detector Setting] 상태 표시 문자열
        /// </summary>
        public string AiSettingStatusText
        {
            get { return _propertiesFeature.AiSettingStatusText; }
            private set { _propertiesFeature.AiSettingStatusText = value; }
        }

        /// <summary>
        /// [AI Detector Agent] 연결 상태 화면 표시 문자열
        ///
        /// AI 연결 확인 전에는 OFF,
        /// 연결 확인 완료 후에는 ON으로 표시한다.
        /// </summary>
        public string AiPowerStatusText
        {
            get { return _propertiesFeature.AiPowerStatusText; }
            private set { _propertiesFeature.AiPowerStatusText = value; }
        }

        /// <summary>
        /// [AI Tracking] 자동 추적 사용 여부
        /// </summary>
        public bool IsAutoTrackingEnabled
        {
            get { return _propertiesFeature.IsAutoTrackingEnabled; }
            set { _propertiesFeature.IsAutoTrackingEnabled = value; }
        }

        /// <summary>
        /// [EO / IR] 영상 중앙 십자선 표시 여부
        ///
        /// 운용 제어 탭의 [CROSSHAIR] Toggle 버튼과
        /// EO / IR 영상의 십자선 Overlay가 동일한 값에 바인딩된다.
        ///
        /// Zoom In / Out 중에도 십자선은 화면 중앙에 고정되며,
        /// 영상 중심 및 광축 정렬 상태 확인 기준점으로 사용한다.
        /// </summary>
        public bool IsCrosshairVisible
        {
            get { return _propertiesFeature.IsCrosshairVisible; }
            set { _propertiesFeature.IsCrosshairVisible = value; }
        }

        /// <summary>
        /// [EO / IR] 중앙 십자선 Toggle 버튼 표시 문자열
        ///
        /// 현재 활성 상태를 버튼 자체에서 바로 확인할 수 있도록
        /// ENABLED / DISABLED 상태 문자열을 반환한다.
        /// </summary>
        public string CrosshairButtonText
        {
            get { return _propertiesFeature.CrosshairButtonText; }
        }

        /// <summary>
        /// EO / IR 메인 화면과 영상 분리 창에 공통 적용되는 십자선 색상.
        /// </summary>
        public Brush CrosshairBrush
        {
            get { return _propertiesFeature.CrosshairBrush; }
        }

        /// <summary>
        /// Control Agent TCP 연결 상태 표시 문자열
        /// </summary>
         // 지도·파노라마는 LA/WEB 공통으로 Control Agent TCP 연결 완료 시 표시한다.
        // RTSP 영상 수신 여부와 저장된 사진·프리셋의 보존 여부는 이 조건과 분리한다.
        public bool IsContextPanelsVisible
        {
            get { return _propertiesFeature.IsContextPanelsVisible; }
        }

        public string ControlAgentConnectionStatusText
        {
            get { return _propertiesFeature.ControlAgentConnectionStatusText; }
            private set { _propertiesFeature.ControlAgentConnectionStatusText = value; }
        }

        /// <summary>
        /// Control Agent TCP 연결 상태 표시 색상
        ///
        /// XAML Ellipse Fill에 바인딩한다.
        /// </summary>
        public string ControlAgentConnectionStatusColor
        {
            get { return _propertiesFeature.ControlAgentConnectionStatusColor; }
            private set { _propertiesFeature.ControlAgentConnectionStatusColor = value; }
        }

        /// <summary>
        /// [PAN / TILT] 속도제어 현재 속도 [Level]
        ///
        /// [XAML] [UI]와 바인딩하여 현재 속도값을 표시하거나 변경할 때 사용한다.
        /// 2026-10-01: 줌 연동 감쇄 결과를 세밀하게 선택·표시할 수 있도록
        /// Web Agent 기준 UI 운용 범위 [0 ~ 60]을 1단위로 사용하며 0은 STOP이다.
        /// </summary>
        public byte PanTiltSpeedLevel
        {
            get { return _propertiesFeature.PanTiltSpeedLevel; }
            set { _propertiesFeature.PanTiltSpeedLevel = value; }
        }

        /// <summary>
        /// 2026-10-01: 현재 메인 활성 EO/IR 줌값을 적용한 실제 PT 전송 속도.
        /// 사용자 SET 값은 보존하고 화면에는 감쇄 결과를 별도로 표시한다.
        /// </summary>
        public byte EffectivePanTiltSpeedLevel
        {
            get { return _propertiesFeature.EffectivePanTiltSpeedLevel; }
        }

        public string PanTiltSpeedDisplayText
        {
            get { return _propertiesFeature.PanTiltSpeedDisplayText; }
        }

        /// <summary>
        /// [LRF] 최근 거리측정 값 표시 문자열
        ///
        /// 거리측정 응답 수신 시 갱신되며,
        /// [XAML] [TextBlock]과 바인딩하여 화면에 표시한다.
        /// </summary>
        public string LrfDistanceText
        {
            get { return _propertiesFeature.LrfDistanceText; }
            private set { _propertiesFeature.LrfDistanceText = value; }
        }

        public string EoStatusText
        {
            get { return _propertiesFeature.EoStatusText; }
            private set { _propertiesFeature.EoStatusText = value; }
        }

        public string IrStatusText
        {
            get { return _propertiesFeature.IrStatusText; }
            private set { _propertiesFeature.IrStatusText = value; }
        }

        /// <summary>
        /// [EO RTSP] 연결 상태 표시 문자열
        ///
        /// 기존 EoStatusText는 영상 화면 하단 상태 표시용으로
        /// "[EO] Connected" 형식을 사용한다.
        ///
        /// 통신 설정 화면에서는 장비 구분 문구를 제외하고
        /// Connected / Connecting / Reconnecting / Disconnected
        /// 상태 문자열만 표시한다.
        /// </summary>
        public string EoConnectionStatusText
        {
            get { return _propertiesFeature.EoConnectionStatusText; }
        }

        /// <summary>
        /// [EO RTSP] 연결 상태 표시 색상
        ///
        /// Connected    : Green
        /// Connecting   : Yellow
        /// Reconnecting : Yellow
        /// Disconnected : Red
        ///
        /// XAML의 상태 표시 Ellipse Fill과
        /// 상태 문자열 Foreground에 함께 바인딩한다.
        /// </summary>
        public string EoConnectionStatusColor
        {
            get { return _propertiesFeature.EoConnectionStatusColor; }
        }

        /// <summary>
        /// [IR RTSP] 연결 상태 표시 문자열
        ///
        /// 기존 IrStatusText의 "[IR]" 장비 구분 문구를 제거하고
        /// 통신 설정 화면에 표시할 상태 문자열만 반환한다.
        /// </summary>
        public string IrConnectionStatusText
        {
            get { return _propertiesFeature.IrConnectionStatusText; }
        }

        /// <summary>
        /// [IR RTSP] 연결 상태 표시 색상
        ///
        /// Connected    : Green
        /// Connecting   : Yellow
        /// Reconnecting : Yellow
        /// Disconnected : Red
        ///
        /// XAML의 상태 표시 Ellipse Fill과
        /// 상태 문자열 Foreground에 함께 바인딩한다.
        /// </summary>
        public string IrConnectionStatusColor
        {
            get { return _propertiesFeature.IrConnectionStatusColor; }
        }

        /// <summary>
        /// 현재 Pan 위치 표시 문자열
        /// </summary>
        public string CurrentPanText
        {
            get { return _propertiesFeature.CurrentPanText; }
        }

        /// <summary>
        /// 현재 Tilt 위치 표시 문자열
        /// </summary>
        public string CurrentTiltText
        {
            get { return _propertiesFeature.CurrentTiltText; }
        }

        /// <summary>
        /// 현재 EO Zoom 상태 표시 문자열
        ///
        /// 옥상 MR300 EO CTEC 직접 제어 장비는
        /// 원시 Position / 최대 Position / 퍼센트를 함께 표시한다.
        ///
        /// 예: 8192 / 16384 (50.0%)
        ///
        /// 그 외 장비는 기존 Control Agent 상태값을 그대로 표시한다.
        /// </summary>
        public string CurrentEoZoomText
        {
            get { return _propertiesFeature.CurrentEoZoomText; }
        }

        /// <summary>
        /// 현재 EO Focus 표준 위치 0~1000 표시
        /// </summary>
        public string CurrentEoFocusText
        {
            get { return _propertiesFeature.CurrentEoFocusText; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춘 IR Zoom 위치 표시.
        /// LA 상태만 역변환하고 Web Agent 상태는 그대로 표시한다.
        /// </summary>
        public string CurrentIrZoomText
        {
            get { return _propertiesFeature.CurrentIrZoomText; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춘 IR Focus 위치 표시.
        /// LA 상태만 역변환하고 Web Agent 상태는 그대로 표시한다.
        /// </summary>
        public string CurrentIrFocusText
        {
            get { return _propertiesFeature.CurrentIrFocusText; }
        }

        internal static string BuildCtecPositionText(int rawPosition, int maxPosition)
        {
            return PropertiesFeature.BuildCtecPositionText(rawPosition, maxPosition);
        }

        /// <summary>
        /// 현재 주요 장비 상태 표시 문자열
        ///
        /// PT는 Control Agent Power Status 비트를 사용하고,
        /// EO / IR은 각 RTSP 영상 연결 상태를 기준으로 표시한다.
        /// </summary>
        public string CurrentPowerText
        {
            get { return _propertiesFeature.CurrentPowerText; }
        }

        /// <summary>
        /// CONTROL 전원 상태 표시 문자열
        /// </summary>
        public string CurrentControlPowerText
        {
            get { return _propertiesFeature.CurrentControlPowerText; }
        }

        /// <summary>
        /// EO 연결 상태 표시 문자열
        /// </summary>
        public string CurrentEoPowerText
        {
            get { return _propertiesFeature.CurrentEoPowerText; }
        }

        /// <summary>
        /// IR 연결 상태 표시 문자열
        /// </summary>
        public string CurrentIrPowerText
        {
            get { return _propertiesFeature.CurrentIrPowerText; }
        }

        internal static string ToOnOff(bool isOn)
        {
            return PropertiesFeature.ToOnOff(isOn);
        }

        /// <summary>
        /// 통신 설정 탭의 [EO RTSP] 카메라 선택 목록
        ///
        /// Control Agent 프로필과 같은 순서로
        /// 옥상 MR300(테스트, LA) / 옥상 MR500(환경부, WebAgent) /
        /// 4층 LR1000(기동형, WebAgent) / 4층 MR300(ER-WATCHER, WebAgent)의
        /// 장비 주소를 UI에서 선택하도록 제공한다.
        /// </summary>
        public ObservableCollection<RtspSourceOption> EoRtspSourceOptions
        {
            get { return _propertiesFeature.EoRtspSourceOptions; }
            private set { _propertiesFeature.EoRtspSourceOptions = value; }
        }

        /// <summary>
        /// 통신 설정 탭의 [IR RTSP] 카메라 선택 목록
        ///
        /// EO와 별도로 IR 카메라를 선택할 수 있으며,
        /// 선택된 Address가 IrSourceAddress에 반영된다.
        /// </summary>
        public ObservableCollection<RtspSourceOption> IrRtspSourceOptions
        {
            get { return _propertiesFeature.IrRtspSourceOptions; }
            private set { _propertiesFeature.IrRtspSourceOptions = value; }
        }

        /// <summary>
        /// [EO] 화면에 표시할 [AI Detector] [Bounding Box] 목록
        /// </summary>
        public ObservableCollection<AiDetectionBox> EoDetectionBoxes
        {
            get { return _propertiesFeature.EoDetectionBoxes; }
            private set { _propertiesFeature.EoDetectionBoxes = value; }
        }

        /// <summary>
        /// [IR] 화면에 표시할 [AI Detector] [Bounding Box] 목록
        /// </summary>
        public ObservableCollection<AiDetectionBox> IrDetectionBoxes
        {
            get { return _propertiesFeature.IrDetectionBoxes; }
            private set { _propertiesFeature.IrDetectionBoxes = value; }
        }

        /// <summary>
        /// 2026-09-17: AI Agent와 독립적으로 표시하는 EO 로컬 FIRE/SMOKE BBox.
        /// </summary>
        public ObservableCollection<VisionDetectionBox> EoFireSmokeDetectionBoxes
        {
            get { return _propertiesFeature.EoFireSmokeDetectionBoxes; }
            private set { _propertiesFeature.EoFireSmokeDetectionBoxes = value; }
        }

        /// <summary>
        /// 2026-09-17: AI Agent와 독립적으로 표시하는 IR 로컬 FIRE/SMOKE BBox.
        /// </summary>
        public ObservableCollection<VisionDetectionBox> IrFireSmokeDetectionBoxes
        {
            get { return _propertiesFeature.IrFireSmokeDetectionBoxes; }
            private set { _propertiesFeature.IrFireSmokeDetectionBoxes = value; }
        }

        /// <summary>
        /// 2026-09-22: 상단 FIRE/SMOKE 경고는 로컬 영상처리와 AI Agent 중
        /// 어느 한쪽이라도 해당 클래스를 현재 BBox로 유지하면 표시한다.
        /// AI의 CAR/DRONE/PERSON 등 다른 클래스는 기존 BBox와 이벤트에는 남지만
        /// 이 경고 상태에는 영향을 주지 않는다.
        /// </summary>
        public bool IsEoFireWarningVisible
        {
            get { return _propertiesFeature.IsEoFireWarningVisible; }
        }

        public bool IsIrFireWarningVisible
        {
            get { return _propertiesFeature.IsIrFireWarningVisible; }
        }

        public bool IsEoSmokeWarningVisible
        {
            get { return _propertiesFeature.IsEoSmokeWarningVisible; }
        }

        public bool IsIrSmokeWarningVisible
        {
            get { return _propertiesFeature.IsIrSmokeWarningVisible; }
        }

        /// <summary>
        /// [AI Detector Agent]에서 조회한 [RTSP] 목록
        ///
        /// [CMD 52] 응답 결과를 화면에 표시하기 위해 사용한다.
        /// </summary>
        public ObservableCollection<AiRtspInfo> AiRtspList
        {
            get { return _propertiesFeature.AiRtspList; }
            private set { _propertiesFeature.AiRtspList = value; }
        }

        /// <summary>
        /// [AI Detector Agent]에서 조회한 [ONNX] 모델 목록
        ///
        /// [CMD 53] 응답 결과를 화면에 표시하기 위해 사용한다.
        /// </summary>
        public ObservableCollection<AiOnnxInfo> AiOnnxList
        {
            get { return _propertiesFeature.AiOnnxList; }
            private set { _propertiesFeature.AiOnnxList = value; }
        }

        /// <summary>
        /// [AI Detector Agent]에서 조회한 [RTSP] / [ONNX] Mapping 목록
        ///
        /// [CMD 56] 응답 결과를 화면에 표시하기 위해 사용한다.
        /// </summary>
        public ObservableCollection<AiMappingInfo> AiMappingList
        {
            get { return _propertiesFeature.AiMappingList; }
            private set { _propertiesFeature.AiMappingList = value; }
        }
    }
}
