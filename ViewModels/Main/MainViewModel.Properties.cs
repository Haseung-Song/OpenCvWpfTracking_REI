using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Models.Main;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        #region [ICommand]

        #region [Display Overlay Commands]

        /// <summary>
        /// [EO / IR] 영상 중앙 십자선 표시 상태 전환 [Command]
        /// </summary>
        public ICommand ToggleCrosshairCommand { get; }

        /// <summary>
        /// CURRENT STATUS를 옥상장비 상태로 전환 [Command]
        /// </summary>
        public ICommand ShowRooftopStatusCommand { get; }

        /// <summary>
        /// CURRENT STATUS를 환경장비 상태로 전환 [Command]
        /// </summary>
        public ICommand ShowEnvironmentStatusCommand { get; }

        /// <summary>
        /// Zoom Sync 이전 Level 선택 [Command]
        /// </summary>
        public ICommand PreviousZoomSyncLevelCommand { get; }

        /// <summary>
        /// Zoom Sync 다음 Level 선택 [Command]
        /// </summary>
        public ICommand NextZoomSyncLevelCommand { get; }

        /// <summary>
        /// 선택한 Zoom Sync Level 적용 [Command]
        /// </summary>
        public ICommand ApplyZoomSyncCommand { get; }

        /// <summary>
        /// 진행 중인 Zoom Sync 정지 [Command]
        /// </summary>
        public ICommand StopZoomSyncCommand { get; }

        /// <summary>
        /// Focus Sync 이전 Level 선택 [Command]
        /// </summary>
        public ICommand PreviousFocusSyncLevelCommand { get; }

        /// <summary>
        /// Focus Sync 다음 Level 선택 [Command]
        /// </summary>
        public ICommand NextFocusSyncLevelCommand { get; }

        /// <summary>
        /// 선택한 Focus Sync Level 적용 [Command]
        /// </summary>
        public ICommand ApplyFocusSyncCommand { get; }

        /// <summary>
        /// 진행 중인 Focus Sync 정지 [Command]
        /// </summary>
        public ICommand StopFocusSyncCommand { get; }

        #endregion

        #region [Video Commands]

        /// <summary>
        /// 영상 [Connect] 버튼 [Command]
        /// </summary>
        public ICommand ConnectCommand { get; }

        /// <summary>
        /// 영상 [Disconnect] 버튼 [Command]
        /// </summary>
        public ICommand DisconnectCommand { get; }

        #endregion

        #region [Pan / Tilt Commands]

        /// <summary>
        /// [PAN] 왼쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand PanLeftCommand { get; }

        /// <summary>
        /// [PAN] 오른쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand PanRightCommand { get; }

        /// <summary>
        /// [TILT] 위쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand TiltUpCommand { get; }

        /// <summary>
        /// [TILT] 아래쪽 위치 이동 테스트 [Command]
        /// </summary>
        public ICommand TiltDownCommand { get; }

        #endregion

        #region [Zoom / Focus Commands]

        /// <summary>
        /// [ZOOM] 확대 테스트 [Command]
        /// </summary>
        public ICommand ZoomInCommand { get; }

        /// <summary>
        /// [ZOOM] 축소 테스트 [Command]
        /// </summary>
        public ICommand ZoomOutCommand { get; }

        /// <summary>
        /// [FOCUS] [Far] 테스트 [Command]
        /// </summary>
        public ICommand FocusFarCommand { get; }

        /// <summary>
        /// [FOCUS] [Near] 테스트 [Command]
        /// </summary>
        public ICommand FocusNearCommand { get; }

        #endregion

        #region [Move Control Commands]

        /// <summary>
        /// Pan / Tilt Home Position 실행 [Command]
        /// </summary>
        public ICommand MoveHomePositionCommand { get; }

        /// <summary>
        /// Pan 현재 위치를 0으로 설정 [Command]
        /// </summary>
        public ICommand SetPanZeroCommand { get; }

        /// <summary>
        /// Tilt 현재 위치를 0으로 설정 [Command]
        /// </summary>
        public ICommand SetTiltZeroCommand { get; }

        /// <summary>
        /// Pan Absolute 이동 [Command]
        /// </summary>
        public ICommand MovePanAbsoluteCommand { get; }

        /// <summary>
        /// Tilt Absolute 이동 [Command]
        /// </summary>
        public ICommand MoveTiltAbsoluteCommand { get; }

        /// <summary>
        /// [PAN] / [TILT] Absolute 위치 이동 정지 [Command]
        /// </summary>
        public ICommand StopAbsoluteMoveCommand { get; }

        /// <summary>
        /// EO / IR Zoom Position 이동 [Command]
        /// </summary>
        public ICommand SetZoomPositionCommand { get; }

        /// <summary>
        /// EO / IR Zoom Ratio 이동 [Command]
        /// </summary>
        public ICommand SetZoomRatioCommand { get; }

        /// <summary>
        /// EO / IR Focus Position 이동 [Command]
        /// </summary>
        public ICommand SetFocusPositionCommand { get; }

        /// <summary>
        /// 이동 제어 입력값 초기화 [Command]
        /// </summary>
        public ICommand ResetPositionInputCommand { get; }


        /// <summary>
        /// PRESET 1 (LA TEST) 현재 PTZF를 LA 스캔 프리셋으로 등록
        /// </summary>
        public ICommand AddOrUpdateLaPresetCommand { get; }

        public ICommand ClearAllLaPresetsCommand { get; }

        public ICommand MoveToLaPresetCommand { get; }

        public ICommand StartLaPresetScanCommand { get; }

        public ICommand UpdateLaPresetScanCommand { get; }

        public ICommand StopLaPresetScanCommand { get; }

        public ICommand StopLaPresetMoveCommand { get; }

        /// <summary>
        /// 현재 PTZF 위치를 선택 슬롯에 프리셋으로 추가 / 갱신
        /// </summary>
        public ICommand AddOrUpdatePresetCommand { get; }

        /// <summary>
        /// 선택 슬롯 프리셋 제거
        /// </summary>
        public ICommand DeletePresetCommand { get; }

        /// <summary>
        /// ComboBox에서 선택한 프리셋으로 이동
        /// </summary>
        public ICommand MoveToPresetCommand { get; }

        /// <summary>
        /// 프리셋 오토 스캔 시작
        /// </summary>
        public ICommand StartPresetScanCommand { get; }

        /// <summary>
        /// 실행 중인 스캔 속도 / 정지시간 변경
        /// </summary>
        public ICommand UpdatePresetScanCommand { get; }

        /// <summary>
        /// 프리셋 오토 스캔 정지
        /// </summary>
        public ICommand StopPresetScanCommand { get; }

        public ICommand StopPresetMoveCommand { get; }

        public ICommand StopActivePresetScanCommand { get; }

        #endregion

        #region [LRF Commands]

        /// <summary>
        /// [LRF] 거리측정 [1회] 요청 [Command]
        /// </summary>
        public ICommand LrfMeasureCommand { get; }

        #endregion

        #region [STOP Commands]

        /// <summary>
        /// [PT] 연속 이동 정지 [Command]
        /// </summary>
        public ICommand StopMoveCommand { get; }

        #endregion

        #region [AI Detector Setting Commands]

        /// <summary>
        /// [AI Detector Agent] 수동 연결 [Command]
        /// </summary>
        public ICommand ConnectAiAgentCommand { get; }

        /// <summary>
        /// [AI Detector Agent] 수동 연결 해제 [Command]
        /// </summary>
        public ICommand DisconnectAiAgentCommand { get; }

        /// <summary>
        /// [AI Detector Agent] [RTSP] 주소 설정 적용 [Command]
        /// </summary>
        public ICommand ApplyAiRtspCommand { get; }

        /// <summary>
        /// [AI Detector Agent] [RTSP] / [ONNX] Mapping 설정 적용 [Command]
        /// </summary>
        public ICommand ApplyAiMappingCommand { get; }

        /// <summary>
        /// [AI Detector Agent] 현재 설정 조회 [Command]
        /// </summary>
        public ICommand RefreshAiSettingCommand { get; }

        #endregion

        #endregion

        #region [Equipment Status / Zoom Sync Properties]

        /// <summary>
        /// [Environment Equipment / Zoom Synchronization]
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
        public ObservableCollection<ZoomSyncLevelOption> ZoomSyncLevelOptions { get; }

        /// <summary>
        /// [Environment Equipment / Focus Synchronization]
        ///
        /// EO / IR Focus Position을 0부터 1000까지
        /// 100 단위로 구분한 총 11개의 단계 목록이다.
        ///
        /// LEVEL 0  = 0
        /// LEVEL 1  = 100
        /// ...
        /// LEVEL 10 = 1000
        /// </summary>
        public ObservableCollection<ZoomSyncLevelOption> FocusSyncLevelOptions { get; }

        /// <summary>
        /// CURRENT STATUS 화면에 표시할 장비 구성
        /// </summary>
        public EquipmentStatusMode SelectedEquipmentStatusMode
        {
            get => _selectedEquipmentStatusMode;
            set
            {
                if (_selectedEquipmentStatusMode == value)
                {
                    return;
                }

                _selectedEquipmentStatusMode = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRooftopStatusSelected));
                OnPropertyChanged(nameof(IsEnvironmentStatusSelected));
                OnPropertyChanged(nameof(IsRooftopThermalPaletteVisible));
                OnPropertyChanged(nameof(IsEnvironmentThermalPaletteVisible));
                OnPropertyChanged(nameof(CurrentStatusEquipmentText));
                OnPropertyChanged(nameof(IsHomeZeroVisible));
                OnPropertyChanged(nameof(CurrentIrZoomText));
                OnPropertyChanged(nameof(CurrentIrFocusText));
                OnPropertyChanged(nameof(RooftopIrZoomStatusText));
                OnPropertyChanged(nameof(RooftopIrFocusStatusText));
                OnPropertyChanged(nameof(EnvironmentIrZoomStatusText));
                OnPropertyChanged(nameof(EnvironmentIrFocusStatusText));
                OnPropertyChanged(nameof(CurrentLaPresetSnapshotText));
                OnPropertyChanged(nameof(CurrentPresetSnapshotText));

            }

        }

        public bool IsRooftopStatusSelected =>
            SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop;

        public bool IsEnvironmentStatusSelected =>
            SelectedEquipmentStatusMode == EquipmentStatusMode.Environment;

        public bool IsRooftopThermalPaletteVisible => IsRooftopStatusSelected;

        public bool IsEnvironmentThermalPaletteVisible => IsEnvironmentStatusSelected;

        public string CurrentStatusEquipmentText =>
            IsRooftopStatusSelected
                ? "ROOFTOP EQUIPMENT / LA AGENT"
                : "ENVIRONMENT EQUIPMENT / WEB AGENT";

        /// <summary>
        /// HOME / ZERO UI 표시 여부.
        ///
        /// HOME / ZERO는 MCB/LA 전용 기능이므로
        /// ROOFTOP / LA AGENT 선택 상태에서만 표시한다.
        /// </summary>
        public bool IsHomeZeroVisible =>
            IsRooftopStatusSelected;

        /// <summary>
        /// HOME / ZERO 잠금 화면의 작업 제목.
        /// HOME POSITION, PAN ZERO, TILT ZERO를 각각 구분하여 표시한다.
        /// </summary>
        public string HomeZeroLockTitle
        {
            get => _homeZeroLockTitle;
            private set
            {
                if (_homeZeroLockTitle == value)
                {
                    return;
                }

                _homeZeroLockTitle = value;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// HOME / ZERO 잠금 화면의 현재 처리 단계.
        /// </summary>
        public string HomeZeroLockMessage
        {
            get => _homeZeroLockMessage;
            private set
            {
                if (_homeZeroLockMessage == value)
                {
                    return;
                }

                _homeZeroLockMessage = value;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// HOME / ZERO 작업 진행 여부.
        /// </summary>
        public bool IsHomePositionMoving
        {
            get => _isHomePositionMoving;
            private set
            {
                if (_isHomePositionMoving == value)
                {
                    return;
                }

                _isHomePositionMoving = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(IsMainControlEnabled));
                OnPropertyChanged(
                    nameof(IsOperationCommandEnabled));
                OnPropertyChanged(
                    nameof(IsPanTiltSpeedControlEnabled));
                OnPropertyChanged(
                    nameof(IsControlInputLocked));
                OnPropertyChanged(
                    nameof(ControlLockTitle));
                OnPropertyChanged(
                    nameof(ControlLockMessage));
            }

        }

        /// <summary>
        /// 우측 상태 확인 UI 활성 여부.
        /// HOME / ZERO 중에는 기존 전체 잠금을 유지하지만,
        /// AUTO SCAN 중에는 CURRENT STATUS와 PRESET 화면을 계속 확인할 수 있다.
        /// </summary>
        public bool IsMainControlEnabled =>
            !IsHomePositionMoving;

        /// <summary>
        /// 실제 장비 명령을 발생시키는 UI 활성 여부.
        /// HOME / ZERO 및 AUTO SCAN 중에는 키보드와 함께 잠근다.
        /// </summary>
        public bool IsOperationCommandEnabled =>
            !IsControlInputLocked;

        /// <summary>
        /// 우측 상위 탭 선택 상태.
        /// PAN / TILT SPEED는 운용 제어 탭에서만 사용할 수 있다.
        /// </summary>
        public int SelectedRightPanelTabIndex
        {
            get =>
                _selectedRightPanelTabIndex;

            set
            {
                if (_selectedRightPanelTabIndex == value)
                {
                    return;
                }

                _selectedRightPanelTabIndex =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(IsPanTiltSpeedControlEnabled));
            }

        }

        /// <summary>
        /// 운용 제어 하위 탭 선택 상태.
        /// 0번 PTZF 탭에서만 수동 PAN / TILT SPEED를 사용할 수 있다.
        /// </summary>
        public int SelectedOperationControlTabIndex
        {
            get =>
                _selectedOperationControlTabIndex;

            set
            {
                if (_selectedOperationControlTabIndex == value)
                {
                    return;
                }

                _selectedOperationControlTabIndex =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(IsPanTiltSpeedControlEnabled));
            }

        }

        /// <summary>
        /// 상단 PAN / TILT SPEED 슬라이더 활성 여부.
        /// 수동 PTZF용 속도와 PRESET용 Speed(1 ~ 60)를 명확히 분리한다.
        /// </summary>
        public bool IsPanTiltSpeedControlEnabled =>
            IsOperationCommandEnabled &&
            SelectedRightPanelTabIndex == 1 &&
            SelectedOperationControlTabIndex == 0;

        public bool IsPresetScanControlLocked =>
            IsLaPresetScanRunning ||
            IsPresetScanRunning;

        public bool IsControlInputLocked =>
            IsHomePositionMoving ||
            IsPresetScanControlLocked;

        public string ControlLockTitle =>
            IsPresetScanControlLocked
                ? "AUTO SCAN OPERATION"
                : HomeZeroLockTitle;

        public string ControlLockMessage =>
            IsLaPresetScanRunning
                ? "PRESET L AUTO SCAN RUNNING"
                : IsPresetScanRunning
                    ? "PRESET W AUTO SCAN RUNNING"
                    : HomeZeroLockMessage;

        /// <summary>
        /// 현재 선택된 Zoom Sync Level
        /// </summary>
        public ZoomSyncLevelOption SelectedZoomSyncLevel
        {
            get => _selectedZoomSyncLevel;
            set
            {
                if (_selectedZoomSyncLevel == value)
                {
                    return;
                }

                _selectedZoomSyncLevel = value;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(SelectedZoomSyncPositionText));
            }

        }

        public string SelectedZoomSyncPositionText =>
            SelectedZoomSyncLevel == null
                ? "0 / 1000"
                : $"{SelectedZoomSyncLevel.Position} / 1000";

        public string ZoomSyncStatusText
        {
            get => _zoomSyncStatusText;
            private set
            {
                if (_zoomSyncStatusText == value)
                {
                    return;
                }

                _zoomSyncStatusText = value;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// 현재 선택된 Focus Sync Level
        /// </summary>
        public ZoomSyncLevelOption SelectedFocusSyncLevel
        {
            get => _selectedFocusSyncLevel;
            set
            {
                if (_selectedFocusSyncLevel == value)
                {
                    return;
                }

                _selectedFocusSyncLevel = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(SelectedFocusSyncPositionText));
            }

        }

        /// <summary>
        /// Focus Sync 표준 위치 표시 문자열
        /// </summary>
        public string SelectedFocusSyncPositionText =>
            SelectedFocusSyncLevel == null
                ? "0 / 1000"
                : $"{SelectedFocusSyncLevel.Position} / 1000";

        /// <summary>
        /// Focus Sync 실행 상태 표시 문자열
        /// </summary>
        public string FocusSyncStatusText
        {
            get => _focusSyncStatusText;
            private set
            {
                if (_focusSyncStatusText == value)
                {
                    return;
                }

                _focusSyncStatusText = value;
                OnPropertyChanged();
            }

        }

        public string RooftopEoZoomStatusText =>
            $"{GetCurrentPresetStandardZoom()} / 1000";

        public string RooftopEoFocusStatusText =>
            $"{GetCurrentPresetStandardFocus()} / 1000";

        public string RooftopIrZoomStatusText =>
            $"{GetCurrentIrZoomStandardPosition()} / 1000";

        public string RooftopIrFocusStatusText =>
            $"{GetCurrentIrFocusStandardPosition()} / 1000";

        public string EnvironmentEoZoomStatusText =>
            $"{GetCurrentPresetStandardZoom()} / 1000";

        public string EnvironmentEoFocusStatusText =>
            $"{GetCurrentPresetStandardFocus()} / 1000";

        public string EnvironmentIrZoomStatusText =>
            $"{GetCurrentIrZoomStandardPosition()} / 1000";

        public string EnvironmentIrFocusStatusText =>
            $"{GetCurrentIrFocusStandardPosition()} / 1000";

        /// <summary>
        /// Home / Zero 실행 상태 표시
        /// </summary>
        public string HomeZeroStatusText
        {
            get =>
                _homeZeroStatusText;

            private set
            {
                if (_homeZeroStatusText ==
                    value)
                {
                    return;
                }

                _homeZeroStatusText =
                    value;

                OnPropertyChanged();
            }

        }

        #endregion

        #region [Move Control Properties]

        /// <summary>
        /// Pan Absolute 이동 시 장비 최단거리 모드(0x4D / 0x02)를 사용하는지 여부
        /// </summary>
        public bool IsPanTurnShortMode
        {
            get =>
                _panTurnMode ==
                PanTurnMode.Short;

            set
            {
                if (!value ||
                    _panTurnMode ==
                        PanTurnMode.Short)
                {
                    return;
                }

                _panTurnMode =
                    PanTurnMode.Short;

                ApplySelectedPanTurnMode();

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(IsPanTurnViaZeroMode));
            }

        }

        /// <summary>
        /// Pan Absolute 이동 시 장비 원점 통과 모드(0x4D / 0x01)를 사용하는지 여부
        /// </summary>
        public bool IsPanTurnViaZeroMode
        {
            get =>
                _panTurnMode ==
                PanTurnMode.ViaZero;

            set
            {
                if (!value ||
                    _panTurnMode ==
                        PanTurnMode.ViaZero)
                {
                    return;
                }

                _panTurnMode =
                    PanTurnMode.ViaZero;

                ApplySelectedPanTurnMode();

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(IsPanTurnShortMode));
            }

        }

        /// <summary>
        /// Pan Absolute 목표값
        /// </summary>
        public double? PanAbsoluteValue
        {
            get =>
                _panAbsoluteValue;

            set
            {
                double? roundedValue =
                    RoundNullableAngle(
                        value);

                if (_panAbsoluteValue ==
                    roundedValue)
                {
                    return;
                }

                _panAbsoluteValue =
                    roundedValue;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// Tilt Absolute 목표값
        /// </summary>
        public double? TiltAbsoluteValue
        {
            get =>
                _tiltAbsoluteValue;

            set
            {
                double? roundedValue =
                    RoundNullableAngle(
                        value);

                if (_tiltAbsoluteValue ==
                    roundedValue)
                {
                    return;
                }

                _tiltAbsoluteValue =
                    roundedValue;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// EO / IR 공통 Zoom Position
        /// </summary>
        public int? ZoomPositionValue
        {
            get =>
                _zoomPositionValue;

            set
            {
                if (_zoomPositionValue ==
                    value)
                {
                    return;
                }

                _zoomPositionValue =
                    value;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// 이동 제어 EO 기준 Zoom Ratio
        ///
        /// 입력 범위:
        /// EO 1.0 ~ 50.0배
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
            get =>
                _zoomRatioValue;

            set
            {
                double? roundedValue =
                    value.HasValue
                        ? Math.Round(
                            value.Value,
                            1,
                            MidpointRounding.AwayFromZero)
                        : (double?)null;

                if (_zoomRatioValue ==
                    roundedValue)
                {
                    return;
                }

                _zoomRatioValue =
                    roundedValue;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// EO / IR 공통 Focus Position
        /// </summary>
        public int? FocusPositionValue
        {
            get =>
                _focusPositionValue;

            set
            {
                if (_focusPositionValue ==
                    value)
                {
                    return;
                }

                _focusPositionValue =
                    value;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// PRESET 1 (LA TEST) ID 선택 목록
        /// </summary>
        public ObservableCollection<int> LaPresetSlotOptions { get; } =
            new ObservableCollection<int>(
                Enumerable.Range(
                    1,
                    99));

        public ObservableCollection<PresetPointOption> LaPresetPoints { get; } =
            new ObservableCollection<PresetPointOption>();

        public int LaPresetSlotNumber
        {
            get =>
                _laPresetSlotNumber;

            set
            {
                int safeValue =
                    Math.Max(
                        0,
                        Math.Min(
                            99,
                            value));

                if (_laPresetSlotNumber ==
                    safeValue)
                {
                    return;
                }

                _laPresetSlotNumber =
                    safeValue;

                OnPropertyChanged();

                SelectedLaPresetPoint =
                    LaPresetPoints
                        .FirstOrDefault(
                            preset =>
                                preset.Number ==
                                safeValue);
            }

        }

        public PresetPointOption SelectedLaPresetPoint
        {
            get =>
                _selectedLaPresetPoint;

            set
            {
                if (ReferenceEquals(
                    _selectedLaPresetPoint,
                    value))
                {
                    return;
                }

                _selectedLaPresetPoint =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(SelectedLaPresetDetailText));

                if (value != null &&
                    _laPresetSlotNumber !=
                        value.Number)
                {
                    _laPresetSlotNumber =
                        value.Number;

                    OnPropertyChanged(
                        nameof(LaPresetSlotNumber));
                }

            }

        }

        public string CurrentLaPresetSnapshotText =>
            $"PAN      : {_currentPan:F2}°\n" +
            $"TILT     : {_currentTilt:F2}°\n" +
            $"EO ZOOM  : {CurrentEoZoomText} / 1000\n" +
            $"EO FOCUS : {CurrentEoFocusText} / 1000\n" +
            $"IR ZOOM  : {CurrentIrZoomText} / 1000\n" +
            $"IR FOCUS : {CurrentIrFocusText} / 1000";

        public string SelectedLaPresetDetailText =>
            SelectedLaPresetPoint == null
                ? "프리셋 미등록 상태. 등록된 프리셋을 선택하세요."
                : SelectedLaPresetPoint.DetailText;

        public int LaPresetScanSpeed
        {
            get =>
                _laPresetScanSpeed;

            set
            {
                int safeValue =
                    Math.Max(
                        1,
                        Math.Min(
                            60,
                            value));

                if (_laPresetScanSpeed ==
                    safeValue)
                {
                    return;
                }

                _laPresetScanSpeed =
                    safeValue;

                OnPropertyChanged();
            }

        }

        public int LaPresetScanDelay
        {
            get =>
                _laPresetScanDelay;

            set
            {
                int safeValue =
                    Math.Max(
                        1,
                        Math.Min(
                            60,
                            value));

                if (_laPresetScanDelay ==
                    safeValue)
                {
                    return;
                }

                _laPresetScanDelay =
                    safeValue;

                OnPropertyChanged();
            }

        }

        public string LaPresetCommandStatusText
        {
            get =>
                _laPresetCommandStatusText;

            private set
            {
                if (_laPresetCommandStatusText ==
                    value)
                {
                    return;
                }

                _laPresetCommandStatusText =
                    value;

                OnPropertyChanged();
            }

        }

        public bool IsLaPresetScanRunning
        {
            get =>
                _isLaPresetScanRunning;

            private set
            {
                if (_isLaPresetScanRunning ==
                    value)
                {
                    return;
                }

                _isLaPresetScanRunning =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPresetScanControlLocked));
                OnPropertyChanged(nameof(IsControlInputLocked));
                OnPropertyChanged(nameof(IsMainControlEnabled));
                OnPropertyChanged(nameof(IsOperationCommandEnabled));
                OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
                OnPropertyChanged(nameof(ControlLockTitle));
                OnPropertyChanged(nameof(ControlLockMessage));
            }

        }

        /// <summary>
        /// 프리셋 슬롯 선택 목록
        ///
        /// 프리셋 추가 / 제거 / 이동 명령에서 사용할 수 있는
        /// 1 ~ 63 슬롯을 제공한다.
        /// </summary>
        public ObservableCollection<int> PresetSlotOptions { get; } =
            new ObservableCollection<int>(
                Enumerable.Range(
                    1,
                    63));

        /// <summary>
        /// 현재 프로그램에서 등록 명령을 송신한 프리셋 목록
        ///
        /// TORUSS 응답 프로토콜에는 프리셋 목록 조회 응답이 없으므로
        /// 이 목록은 장비 전체 프리셋 데이터가 아니라
        /// 현재 프로그램 세션에서 관리하는 화면 확인용 목록이다.
        /// </summary>
        public ObservableCollection<PresetPointOption> PresetPoints { get; } =
            new ObservableCollection<PresetPointOption>();

        /// <summary>
        /// 스캔 속도 / 정지시간 선택 목록
        ///
        /// TORUSS 문서 기준:
        /// 1 ~ 60
        /// </summary>
        public ObservableCollection<int> PresetScanValueOptions { get; } =
            new ObservableCollection<int>(
                Enumerable.Range(
                    1,
                    60));

        /// <summary>
        /// 프리셋 추가 / 제거 대상 슬롯 번호
        /// </summary>
        public int PresetSlotNumber
        {
            get =>
                _presetSlotNumber;

            set
            {
                int safeValue =
                    Math.Max(
                        1,
                        Math.Min(
                            63,
                            value));

                if (_presetSlotNumber ==
                    safeValue)
                {
                    return;
                }

                _presetSlotNumber =
                    safeValue;

                OnPropertyChanged();

                PresetPointOption existingPreset =
                    PresetPoints
                        .FirstOrDefault(
                            preset =>
                                preset.Number ==
                                safeValue);

                /*
                 * 등록되지 않은 새 슬롯을 선택한 경우에는
                 * 기존 ComboBox 선택이 남아 잘못 이동하지 않도록
                 * 선택 프리셋을 null로 초기화한다.
                 */
                SelectedPresetPoint =
                    existingPreset;
            }

        }

        /// <summary>
        /// 등록된 프리셋 ComboBox 선택값
        /// </summary>
        public PresetPointOption SelectedPresetPoint
        {
            get =>
                _selectedPresetPoint;

            set
            {
                if (ReferenceEquals(
                    _selectedPresetPoint,
                    value))
                {
                    return;
                }

                _selectedPresetPoint =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(SelectedPresetDetailText));

                if (value != null &&
                    _presetSlotNumber !=
                        value.Number)
                {
                    _presetSlotNumber =
                        value.Number;

                    OnPropertyChanged(
                        nameof(PresetSlotNumber));
                }

            }

        }

        /// <summary>
        /// 현재 PTZF 상태 Snapshot 표시
        ///
        /// ADD / UPDATE 명령을 송신하면
        /// 이 상태를 선택 슬롯의 화면 확인용 정보로 저장한다.
        /// </summary>
        public string CurrentPresetSnapshotText =>
            $"PAN      : {_currentPan:F2}°\n" +
            $"TILT     : {_currentTilt:F2}°\n" +
            $"EO ZOOM  : {CurrentEoZoomText} / 1000\n" +
            $"EO FOCUS : {CurrentEoFocusText} / 1000\n" +
            $"IR ZOOM  : {CurrentIrZoomText} / 1000\n" +
            $"IR FOCUS : {CurrentIrFocusText} / 1000";

        /// <summary>
        /// ComboBox에서 선택한 프리셋 상세값
        /// </summary>
        public string SelectedPresetDetailText =>
            SelectedPresetPoint == null
                ? "프리셋 미등록 상태. 등록된 프리셋을 선택하세요."
                : SelectedPresetPoint.DetailText;

        /// <summary>
        /// 오토 스캔 이동 속도
        ///
        /// 범위:
        /// 1 ~ 60
        /// </summary>
        public int PresetScanSpeed
        {
            get =>
                _presetScanSpeed;

            set
            {
                int safeValue =
                    Math.Max(
                        1,
                        Math.Min(
                            60,
                            value));

                if (_presetScanSpeed ==
                    safeValue)
                {
                    return;
                }

                _presetScanSpeed =
                    safeValue;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// 오토 스캔 프리셋 정지시간
        ///
        /// 범위:
        /// 1 ~ 60초
        /// </summary>
        public int PresetScanDelay
        {
            get =>
                _presetScanDelay;

            set
            {
                int safeValue =
                    Math.Max(
                        1,
                        Math.Min(
                            60,
                            value));

                if (_presetScanDelay ==
                    safeValue)
                {
                    return;
                }

                _presetScanDelay =
                    safeValue;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// 프리셋 / 스캔 마지막 명령 상태
        ///
        /// 별도 ACK가 없으므로 TCP 송신 결과를 표시한다.
        /// </summary>
        public string PresetCommandStatusText
        {
            get =>
                _presetCommandStatusText;

            private set
            {
                if (_presetCommandStatusText ==
                    value)
                {
                    return;
                }

                _presetCommandStatusText =
                    value;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// 현재 프로그램에서 스캔 시작 명령을 송신한 상태
        /// </summary>
        public bool IsPresetScanRunning
        {
            get =>
                _isPresetScanRunning;

            private set
            {
                if (_isPresetScanRunning ==
                    value)
                {
                    return;
                }

                _isPresetScanRunning =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPresetScanControlLocked));
                OnPropertyChanged(nameof(IsControlInputLocked));
                OnPropertyChanged(nameof(IsMainControlEnabled));
                OnPropertyChanged(nameof(IsOperationCommandEnabled));
                OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
                OnPropertyChanged(nameof(ControlLockTitle));
                OnPropertyChanged(nameof(ControlLockMessage));
            }

        }

        #endregion

        #region [Bindable Properties]

        #region [Source Address Properties]

        /// <summary>
        /// [EO] 주간 [RTSP] 주소
        ///
        /// 통신 설정 탭의 EO 카메라 선택 ComboBox와 양방향 바인딩한다.
        /// </summary>
        public string EoSourceAddress
        {
            get => _eoSourceAddress;

            set
            {
                if (_eoSourceAddress ==
                    value)
                {
                    return;
                }

                _eoSourceAddress =
                    value;

                OnPropertyChanged();

                /*
                 * EO RTSP 주소가 외부 로직에서 변경된 경우에도
                 * 통신 설정 ComboBox 선택 항목을 함께 갱신한다.
                 */
                OnPropertyChanged(
                    nameof(SelectedEoRtspSource));
            }

        }

        /// <summary>
        /// 통신 설정 탭에서 선택된 EO RTSP 프리셋
        ///
        /// 기존에는 SelectedValue로 주소만 바인딩했지만,
        /// 옥상 GOP EO 카메라의 CTEC CGI 직접 제어 여부까지 판단해야 하므로
        /// 선택 항목 전체를 SelectedItem으로 바인딩한다.
        /// </summary>
        private RtspSourceOption _selectedEoRtspSource;
        private RtspSourceOption _selectedIrRtspSource;
        private RtspSourceOption _selectedAiEoRtspSource;
        private RtspSourceOption _selectedAiIrRtspSource;

        public RtspSourceOption SelectedEoRtspSource
        {
            get
            {
                return _selectedEoRtspSource ?? EoRtspSourceOptions
                    .FirstOrDefault(
                        option =>
                            string.Equals(
                                option.Address,
                                EoSourceAddress,
                                StringComparison.OrdinalIgnoreCase))
                    ?? EoRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                _selectedEoRtspSource = value;

                if (!value.IsDirectInput)
                {
                    EoSourceAddress =
                        value.Address;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEoRtspDirectInput));
            }

        }

        /// <summary>
        /// [IR] 열상 [RTSP] 주소
        ///
        /// 통신 설정 탭의 IR 카메라 선택 ComboBox와 양방향 바인딩한다.
        /// </summary>
        public string IrSourceAddress
        {
            get => _irSourceAddress;

            set
            {
                if (_irSourceAddress ==
                    value)
                {
                    return;
                }

                _irSourceAddress =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedIrRtspSource));
                OnPropertyChanged(nameof(IsIrRtspDirectInput));
            }

        }

        /// <summary>
        /// 통신 설정 탭에서 선택된 IR RTSP 프리셋
        /// </summary>
        public RtspSourceOption SelectedIrRtspSource
        {
            get => _selectedIrRtspSource ?? IrRtspSourceOptions.FirstOrDefault(
                       option => string.Equals(
                           option.Address,
                           IrSourceAddress,
                           StringComparison.OrdinalIgnoreCase))
                   ?? IrRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
            set
            {
                if (value == null)
                {
                    return;
                }

                _selectedIrRtspSource = value;

                if (!value.IsDirectInput)
                {
                    IrSourceAddress =
                        value.Address;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIrRtspDirectInput));
            }
        }

        public bool IsEoRtspDirectInput =>
            SelectedEoRtspSource?.IsDirectInput == true;

        public bool IsIrRtspDirectInput =>
            SelectedIrRtspSource?.IsDirectInput == true;

        #endregion

        #region [Control Agent Setting Properties]

        /// <summary>
        /// Control Agent 제어 TCP 연결 IP
        /// </summary>
        public string ControlAgentIp
        {
            get => _controlControlAgentIp;

            set
            {
                if (_controlControlAgentIp ==
                    value)
                {
                    return;
                }

                _controlControlAgentIp =
                    value;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// 옥상 MCB 유지보수 직접 연결 IP
        ///
        /// Control Agent IP와 독립적으로 관리하며,
        /// Pan/Tilt Zero 및 Home fallback 직접 명령에 사용한다.
        /// </summary>
        public string McbMaintenanceIpAddress
        {
            get => _mcbMaintenanceIpAddress;

            set
            {
                if (_mcbMaintenanceIpAddress ==
                    value)
                {
                    return;
                }

                _mcbMaintenanceIpAddress =
                    value;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// Control Agent 제어 TCP 연결 Port 입력 문자열
        ///
        /// 연결 시점에 int.TryParse로 검증한다.
        /// </summary>
        public string ControlAgentPortText
        {
            get => _controlControlAgentPortText;

            set
            {
                if (_controlControlAgentPortText ==
                    value)
                {
                    return;
                }

                _controlControlAgentPortText =
                    value;

                OnPropertyChanged();
            }

        }

        #endregion

        #region [Image Properties]

        /// <summary>
        /// [EOCameraImage] 값 변경 시,
        /// [XAML]의 [Image Source]가 갱신된다.
        /// </summary>
        public BitmapSource EOCameraImage
        {
            get => _eoCameraImage;
            private set
            {
                if (_eoCameraImage != value)
                {
                    _eoCameraImage = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [IRCameraImage] 값 변경 시,
        /// [XAML]의 [Image Source]가 갱신된다.
        /// </summary>
        public BitmapSource IRCameraImage
        {
            get => _irCameraImage;
            private set
            {
                if (_irCameraImage != value)
                {
                    _irCameraImage = value;
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [AI Overlay Video Size Properties]

        /// <summary>
        /// [EO] [RTSP] 원본 영상 너비
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// [AI] [Bounding Box] [Overlay] 기준 너비로 사용한다.
        /// </summary>
        public int EoVideoWidth
        {
            get => _eoVideoWidth;
            set
            {
                _eoVideoWidth = value;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// [EO] [RTSP] 원본 영상 높이
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// [AI] [Bounding Box] [Overlay] 기준 높이로 사용한다.
        /// </summary>
        public int EoVideoHeight
        {
            get => _eoVideoHeight;
            set
            {
                _eoVideoHeight = value;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// [IR] [RTSP] 원본 영상 너비
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// [AI] [Bounding Box] [Overlay] 기준 너비로 사용한다.
        /// </summary>
        public int IrVideoWidth
        {
            get => _irVideoWidth;
            set
            {
                _irVideoWidth = value;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// [IR] [RTSP] 원본 영상 높이
        ///
        /// [FFmpegDecoderService]에서 읽은 실제 [RTSP] 원본 해상도를
        /// [AI] [Bounding Box] [Overlay] 기준 높이로 사용한다.
        /// </summary>
        public int IrVideoHeight
        {
            get => _irVideoHeight;
            set
            {
                _irVideoHeight = value;
                OnPropertyChanged();
            }

        }

        #endregion

        #region [AI Detector Setting Properties]

        /// <summary>
        /// [AI Detector Agent] 연결 [IP]
        /// </summary>
        public string AiControlAgentIp
        {
            get => _aiControlAgentIp;
            set
            {
                if (_aiControlAgentIp != value)
                {
                    _aiControlAgentIp = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [AI Detector Agent] 연결 [Port]
        /// </summary>
        public int AiAgentPort
        {
            get => _aiAgentPort;
            set
            {
                if (_aiAgentPort != value)
                {
                    _aiAgentPort = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [AI Detector Agent] [RTSP Index 0] 주소
        /// </summary>
        public string AiRtsp0Address
        {
            get => _aiRtsp0Address;
            set
            {
                if (_aiRtsp0Address != value)
                {
                    _aiRtsp0Address = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedAiEoRtspSource));
                    OnPropertyChanged(nameof(IsAiEoRtspDirectInput));
                }

            }

        }

        /// <summary>
        /// [AI Detector Agent] [RTSP Index 1] 주소
        /// </summary>
        public string AiRtsp1Address
        {
            get => _aiRtsp1Address;
            set
            {
                if (_aiRtsp1Address != value)
                {
                    _aiRtsp1Address = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedAiIrRtspSource));
                    OnPropertyChanged(nameof(IsAiIrRtspDirectInput));
                }

            }

        }

        public RtspSourceOption SelectedAiEoRtspSource
        {
            get => _selectedAiEoRtspSource ?? EoRtspSourceOptions.FirstOrDefault(
                       option => string.Equals(
                           option.Address,
                           AiRtsp0Address,
                           StringComparison.OrdinalIgnoreCase))
                   ?? EoRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
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

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAiEoRtspDirectInput));
            }
        }

        public RtspSourceOption SelectedAiIrRtspSource
        {
            get => _selectedAiIrRtspSource ?? IrRtspSourceOptions.FirstOrDefault(
                       option => string.Equals(
                           option.Address,
                           AiRtsp1Address,
                           StringComparison.OrdinalIgnoreCase))
                   ?? IrRtspSourceOptions.FirstOrDefault(option => option.IsDirectInput);
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

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAiIrRtspDirectInput));
            }
        }

        public bool IsAiEoRtspDirectInput =>
            SelectedAiEoRtspSource?.IsDirectInput == true;

        public bool IsAiIrRtspDirectInput =>
            SelectedAiIrRtspSource?.IsDirectInput == true;

        /// <summary>
        /// [RTSP Index 0]에 적용할 [ONNX Index]
        /// </summary>
        public int AiRtsp0OnnxIndex
        {
            get => _aiRtsp0OnnxIndex;
            set
            {
                if (_aiRtsp0OnnxIndex != value)
                {
                    _aiRtsp0OnnxIndex = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [RTSP Index 1]에 적용할 [ONNX Index]
        /// </summary>
        public int AiRtsp1OnnxIndex
        {
            get => _aiRtsp1OnnxIndex;
            set
            {
                if (_aiRtsp1OnnxIndex != value)
                {
                    _aiRtsp1OnnxIndex = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [AI Detector] [Mapping Confidence] 기준값
        /// </summary>
        public double AiMappingConfidence
        {
            get => _aiMappingConfidence;
            set
            {
                if (_aiMappingConfidence != value)
                {
                    _aiMappingConfidence = value;
                    OnPropertyChanged();
                    OnPropertyChanged(
                        nameof(AiMappingConfidenceText));
                }

            }

        }

        /// <summary>
        /// [AI Detector] Mapping Confidence 화면 표시 문자열
        /// </summary>
        public string AiMappingConfidenceText =>
            AiMappingConfidence.ToString("0.00");

        /// <summary>
        /// [AI Detector] [Mapping IOU] 기준값
        /// </summary>
        public double AiMappingIou
        {
            get => _aiMappingIou;
            set
            {
                if (_aiMappingIou != value)
                {
                    _aiMappingIou = value;
                    OnPropertyChanged();
                    OnPropertyChanged(
                        nameof(AiMappingIouText));
                }

            }

        }

        /// <summary>
        /// [AI Detector] Mapping IOU 화면 표시 문자열
        /// </summary>
        public string AiMappingIouText =>
            AiMappingIou.ToString("0.00");

        /// <summary>
        /// 화면 표시용 [Bounding Box] 최소 신뢰도 기준값
        /// </summary>
        public double AiDisplayConfidenceThreshold
        {
            get => _aiDisplayConfidenceThreshold;
            set
            {
                if (_aiDisplayConfidenceThreshold != value)
                {
                    _aiDisplayConfidenceThreshold = value;
                    OnPropertyChanged();
                    OnPropertyChanged(
                        nameof(AiDisplayConfidenceThresholdText));
                }

            }

        }

        /// <summary>
        /// [AI Detector] 화면 표시용 [Bounding Box] 최소 신뢰도 표시 문자열
        /// </summary>
        public string AiDisplayConfidenceThresholdText =>
            AiDisplayConfidenceThreshold.ToString("0.00");

        /// <summary>
        /// [AI Detector Setting] 상태 표시 문자열
        /// </summary>
        public string AiSettingStatusText
        {
            get => _aiSettingStatusText;
            private set
            {
                if (_aiSettingStatusText != value)
                {
                    _aiSettingStatusText = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [AI Detector Agent] 연결 상태 화면 표시 문자열
        ///
        /// AI 연결 확인 전에는 OFF,
        /// 연결 확인 완료 후에는 ON으로 표시한다.
        /// </summary>
        public string AiPowerStatusText
        {
            get => _aiPowerStatusText;
            private set
            {
                if (_aiPowerStatusText != value)
                {
                    _aiPowerStatusText = value;
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        /// <summary>
        /// [AI Tracking] 자동 추적 사용 여부
        /// </summary>
        public bool IsAutoTrackingEnabled
        {
            get => _isAutoTrackingEnabled;
            set
            {
                if (_isAutoTrackingEnabled != value)
                {
                    _isAutoTrackingEnabled = value;
                    OnPropertyChanged();
                }

            }

        }

        #region [Display Overlay Properties]

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
            get =>
                _isCrosshairVisible;

            set
            {
                if (_isCrosshairVisible ==
                    value)
                {
                    return;
                }

                _isCrosshairVisible =
                    value;

                if (value)
                {
                    _hasCrosshairBeenDisplayed =
                        true;
                }

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(CrosshairButtonText));
            }

        }

        /// <summary>
        /// [EO / IR] 중앙 십자선 Toggle 버튼 표시 문자열
        ///
        /// 현재 활성 상태를 버튼 자체에서 바로 확인할 수 있도록
        /// ENABLED / DISABLED 상태 문자열을 반환한다.
        /// </summary>
        public string CrosshairButtonText =>
            IsCrosshairVisible
                ? $"CROSSHAIR : ENABLED ({CrosshairColorName})"
                : "CROSSHAIR : DISABLED";

        /// <summary>
        /// EO / IR 메인 화면과 영상 분리 창에 공통 적용되는 십자선 색상.
        /// </summary>
        public Brush CrosshairBrush
        {
            get =>
                _crosshairBrush;

            private set
            {
                _crosshairBrush =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(CrosshairButtonText));
            }

        }

        public string CrosshairColorName
        {
            get
            {
                string[] colorNames =
                {
                    "RED",
                    "ORANGE",
                    "YELLOW",
                    "GREEN",
                    "CYAN",
                    "BLUE",
                    "PURPLE"
                };
                return colorNames[_crosshairColorIndex];
            }

        }

        /// <summary>
        /// 십자선을 다시 활성화할 때 사용할 다음 색상으로 전환한다.
        /// </summary>
        private void AdvanceCrosshairColor()
        {
            _crosshairColorIndex =
                (_crosshairColorIndex + 1) % 7;

            Color[] colors =
            {
                Color.FromRgb(0xFF, 0x3B, 0x30),
                Color.FromRgb(0xFF, 0x95, 0x00),
                Color.FromRgb(0xFF, 0xD6, 0x0A),
                Color.FromRgb(0x34, 0xC7, 0x59),
                Color.FromRgb(0x32, 0xAD, 0xE6),
                Color.FromRgb(0x00, 0x7A, 0xFF),
                Color.FromRgb(0xAF, 0x52, 0xDE)
            };

            CrosshairBrush =
                new SolidColorBrush(
                    colors[_crosshairColorIndex]);
        }

        #endregion

        #region [Control Agent Communication Properties]

        /// <summary>
        /// Control Agent TCP 연결 상태 표시 문자열
        /// </summary>
        public string ControlAgentConnectionStatusText
        {
            get =>
                _controlAgentConnectionStatusText;

            private set
            {
                if (_controlAgentConnectionStatusText ==
                    value)
                {
                    return;
                }

                _controlAgentConnectionStatusText =
                    value;

                OnPropertyChanged();
            }

        }

        /// <summary>
        /// Control Agent TCP 연결 상태 표시 색상
        ///
        /// XAML Ellipse Fill에 바인딩한다.
        /// </summary>
        public string ControlAgentConnectionStatusColor
        {
            get =>
                _controlAgentConnectionStatusColor;

            private set
            {
                if (_controlAgentConnectionStatusColor ==
                    value)
                {
                    return;
                }

                _controlAgentConnectionStatusColor =
                    value;

                OnPropertyChanged();
            }

        }

        #endregion

        #region [Control Display Properties]

        /// <summary>
        /// [PAN / TILT] 속도제어 현재 속도 [Level]
        ///
        /// [XAML] [UI]와 바인딩하여 현재 속도값을 표시하거나 변경할 때 사용한다.
        /// 2026-08-14: UI 운용 범위는 [5 ~ 50], 5단위이며 Pelco-D 허용 범위 안에서 사용한다.
        /// </summary>
        public byte PanTiltSpeedLevel
        {
            get => _panTiltSpeedLevel;
            set
            {
                byte normalizedValue = (byte)Math.Max(5, Math.Min(50, ((value + 2) / 5) * 5));

                if (_panTiltSpeedLevel !=
                    normalizedValue)
                {
                    _panTiltSpeedLevel =
                        normalizedValue;

                    OnPropertyChanged();

                    ApplyPanTiltSpeedWhileMoving();
                }

            }

        }

        /// <summary>
        /// [LRF] 최근 거리측정 값 표시 문자열
        ///
        /// 거리측정 응답 수신 시 갱신되며,
        /// [XAML] [TextBlock]과 바인딩하여 화면에 표시한다.
        /// </summary>
        public string LrfDistanceText
        {
            get => _lrfDistanceText;
            private set
            {
                if (_lrfDistanceText != value)
                {
                    _lrfDistanceText = value;
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [Status Properties]

        public string EoStatusText
        {
            get => _eoStatusText;

            private set
            {
                if (_eoStatusText ==
                    value)
                {
                    return;
                }

                _eoStatusText =
                    value;

                /*
                 * 영상 화면 하단의 EO 상태 문자열 갱신
                 */
                OnPropertyChanged();

                /*
                 * 통신 설정 화면의 EO RTSP 상태 문자열 및
                 * 상태 표시 색상을 함께 갱신한다.
                 */
                OnPropertyChanged(
                    nameof(EoConnectionStatusText));

                OnPropertyChanged(
                    nameof(EoConnectionStatusColor));

                /*
                 * CONNECTION STATUS 영역의
                 * EO 상태 표시를 함께 갱신한다.
                 */
                OnPropertyChanged(
                    nameof(CurrentPowerText));

                OnPropertyChanged(
                    nameof(CurrentEoPowerText));
            }

        }

        public string IrStatusText
        {
            get => _irStatusText;

            private set
            {
                if (_irStatusText ==
                    value)
                {
                    return;
                }

                _irStatusText =
                    value;

                /*
                 * 영상 화면 하단의 IR 상태 문자열 갱신
                 */
                OnPropertyChanged();

                /*
                 * 통신 설정 화면의 IR RTSP 상태 문자열 및
                 * 상태 표시 색상을 함께 갱신한다.
                 */
                OnPropertyChanged(
                    nameof(IrConnectionStatusText));

                OnPropertyChanged(
                    nameof(IrConnectionStatusColor));

                /*
                 * CONNECTION STATUS 영역의
                 * IR 상태 표시를 함께 갱신한다.
                 */
                OnPropertyChanged(
                    nameof(CurrentPowerText));

                OnPropertyChanged(
                    nameof(CurrentIrPowerText));
            }

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
            get
            {
                return GetRtspConnectionStatusText(
                    EoStatusText,
                    "[EO]");
            }

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
            get
            {
                return GetRtspConnectionStatusColor(
                    EoConnectionStatusText);
            }

        }

        /// <summary>
        /// [IR RTSP] 연결 상태 표시 문자열
        ///
        /// 기존 IrStatusText의 "[IR]" 장비 구분 문구를 제거하고
        /// 통신 설정 화면에 표시할 상태 문자열만 반환한다.
        /// </summary>
        public string IrConnectionStatusText
        {
            get
            {
                return GetRtspConnectionStatusText(
                    IrStatusText,
                    "[IR]");
            }

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
            get
            {
                return GetRtspConnectionStatusColor(
                    IrConnectionStatusText);
            }

        }

        /// <summary>
        /// 영상 화면 상태 문자열을
        /// 통신 설정 화면용 RTSP 연결 상태 문자열로 변환
        ///
        /// 실제 영상 상태 문자열에는 재연결 횟수 또는
        /// 부가 문구가 포함될 수 있으므로 완전 일치가 아닌
        /// 상태 키워드 포함 여부를 기준으로 판단한다.
        ///
        /// 예시:
        /// "[EO] Connected"
        ///     -> "Connected"
        ///
        /// "[EO] Connecting..."
        ///     -> "Connecting"
        ///
        /// "[EO] Reconnecting... (4)"
        ///     -> "Reconnecting"
        ///
        /// "[IR] Disconnected"
        ///     -> "Disconnected"
        /// </summary>
        private static string GetRtspConnectionStatusText(
            string statusText,
            string cameraPrefix)
        {
            if (string.IsNullOrWhiteSpace(
                    statusText))
            {
                return "Disconnected";
            }

            string normalizedStatus =
                statusText
                    .Replace(
                        cameraPrefix,
                        string.Empty)
                    .Trim();

            /*
             * Reconnecting 문자열 안에는
             * Connecting 문자열이 포함되므로
             * 반드시 Reconnecting을 먼저 확인해야 한다.
             */
            if (normalizedStatus.IndexOf(
                    "Reconnecting",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Reconnecting";
            }

            if (normalizedStatus.IndexOf(
                    "Connecting",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Connecting";
            }

            if (normalizedStatus.IndexOf(
                    "Disconnected",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Disconnected";
            }

            if (normalizedStatus.IndexOf(
                    "Connected",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Connected";
            }

            return "Disconnected";
        }

        /// <summary>
        /// RTSP 연결 상태 문자열에 맞는
        /// 화면 표시 색상 반환
        ///
        /// Control Agent 연결 상태와 동일한 색상 기준을 사용한다.
        /// </summary>
        private static string GetRtspConnectionStatusColor(
            string connectionStatusText)
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

        #region [Current Device Status Properties]

        /// <summary>
        /// 현재 Pan 위치 표시 문자열
        /// </summary>
        public string CurrentPanText =>
            $"{_currentPan:F2}°";

        /// <summary>
        /// 현재 Tilt 위치 표시 문자열
        /// </summary>
        public string CurrentTiltText =>
            $"{_currentTilt:F2}°";

        /// <summary>
        /// 현재 EO Zoom 상태 표시 문자열
        ///
        /// 옥상 GOP EO CTEC 직접 제어 장비는
        /// 원시 Position / 최대 Position / 퍼센트를 함께 표시한다.
        ///
        /// 예: 8192 / 16384 (50.0%)
        ///
        /// 그 외 장비는 기존 Control Agent 상태값을 그대로 표시한다.
        /// </summary>
        public string CurrentEoZoomText =>
            GetCurrentPresetStandardZoom()
                .ToString();

        /// <summary>
        /// 현재 EO Focus 표준 위치 0~1000 표시
        /// </summary>
        public string CurrentEoFocusText =>
            GetCurrentPresetStandardFocus()
                .ToString();

        /// <summary>
        /// 현재 장비 구성에 맞춘 IR Zoom 위치 표시.
        /// LA 상태만 역변환하고 Web Agent 상태는 그대로 표시한다.
        /// </summary>
        public string CurrentIrZoomText =>
            GetCurrentIrZoomStandardPosition()
                .ToString();

        /// <summary>
        /// 현재 장비 구성에 맞춘 IR Focus 위치 표시.
        /// LA 상태만 역변환하고 Web Agent 상태는 그대로 표시한다.
        /// </summary>
        public string CurrentIrFocusText =>
            GetCurrentIrFocusStandardPosition()
                .ToString();

        /// <summary>
        /// CTEC 원시 위치를 로그 확인용 현재값 / 최대값 / 퍼센트 문자열로 만든다.
        /// 화면 표시는 0~1000으로 통일하지만 수신 로그에는 원시값을 유지한다.
        /// </summary>
        private static string BuildCtecPositionText(
            int rawPosition,
            int maxPosition)
        {
            if (maxPosition <= 0)
            {
                return rawPosition.ToString();
            }

            double percent =
                rawPosition /
                (double)maxPosition *
                100.0;

            percent =
                Math.Max(
                    0.0,
                    Math.Min(
                        100.0,
                        percent));

            return
                $"{rawPosition} / {maxPosition} ({percent:F1}%)";
        }

        /// <summary>
        /// 현재 주요 장비 상태 표시 문자열
        ///
        /// PT는 Control Agent Power Status 비트를 사용하고,
        /// EO / IR은 각 RTSP 영상 연결 상태를 기준으로 표시한다.
        /// </summary>
        public string CurrentPowerText
        {
            get
            {
                bool isPanOn =
                    (_currentPowerStatus & 0x80) != 0;

                bool isTiltOn =
                    (_currentPowerStatus & 0x40) != 0;

                bool isEoOn =
                    EoStatusText ==
                    "[EO] Connected";

                bool isIrOn =
                    IrStatusText ==
                    "[IR] Connected";

                return
                    $"CONTROL:{ToOnOff(isPanOn && isTiltOn)} / " +
                    $"EO:{ToOnOff(isEoOn)} / " +
                    $"IR:{ToOnOff(isIrOn)}";
            }

        }

        /// <summary>
        /// CONTROL 전원 상태 표시 문자열
        /// </summary>
        public string CurrentControlPowerText
        {
            get
            {
                bool isPanOn =
                    (_currentPowerStatus & 0x80) != 0;

                bool isTiltOn =
                    (_currentPowerStatus & 0x40) != 0;

                return ToOnOff(
                    isPanOn &&
                    isTiltOn);
            }

        }

        /// <summary>
        /// EO 연결 상태 표시 문자열
        /// </summary>
        public string CurrentEoPowerText
        {
            get
            {
                bool isEoOn =
                    EoStatusText ==
                    "[EO] Connected";

                return ToOnOff(
                    isEoOn);
            }

        }

        /// <summary>
        /// IR 연결 상태 표시 문자열
        /// </summary>
        public string CurrentIrPowerText
        {
            get
            {
                bool isIrOn =
                    IrStatusText ==
                    "[IR] Connected";

                return ToOnOff(
                    isIrOn);
            }

        }
        private static string ToOnOff(
            bool isOn)
        {
            return isOn
                ? "ON"
                : "OFF";
        }

        #endregion

        #endregion

        #endregion

        #region [Binding Collections]

        /// <summary>
        /// 통신 설정 탭의 [EO RTSP] 카메라 선택 목록
        ///
        /// 기존 InitializeDefaultSourceAddress()에서 주석을 변경하며 사용하던
        /// [1층 ADS] / [옥상 GOP] / [환경부 PTZ] 주소를 UI에서 선택하도록 제공한다.
        /// </summary>
        public ObservableCollection<RtspSourceOption> EoRtspSourceOptions { get; }
            = new ObservableCollection<RtspSourceOption>
            {
                new RtspSourceOption(
                    "1층 생산팀 ADS 주간(EO)",
                    AdsEoRtspAddress),

                new RtspSourceOption(
                    "옥상 GOP 주간(EO)",
                    GopEoRtspAddress,
                    CameraControlType.CtecCgi,
                    GopEoControlIp,
                    GopEoControlUserName,
                    GopEoControlPassword,
                    GopEoControlUseHttps),

                new RtspSourceOption(
                    "4층 환경부 PTZ 주간(EO)",
                    MoeEoRtspAddress),

                new RtspSourceOption(
                    "직접 입력",
                    string.Empty,
                    isDirectInput: true)
            };

        /// <summary>
        /// 통신 설정 탭의 [IR RTSP] 카메라 선택 목록
        ///
        /// EO와 별도로 IR 카메라를 선택할 수 있으며,
        /// 선택된 Address가 IrSourceAddress에 반영된다.
        /// </summary>
        public ObservableCollection<RtspSourceOption> IrRtspSourceOptions { get; }
            = new ObservableCollection<RtspSourceOption>
            {
                new RtspSourceOption(
                    "1층 생산팀 ADS 열상(IR)",
                    AdsIrRtspAddress),

                new RtspSourceOption(
                    "옥상 GOP 열상(IR)",
                    GopIrRtspAddress),

                new RtspSourceOption(
                    "4층 환경부 PTZ 열상(IR)",
                    MoeIrRtspAddress),

                new RtspSourceOption(
                    "직접 입력",
                    string.Empty,
                    isDirectInput: true)
            };

        /// <summary>
        /// [EO] 화면에 표시할 [AI Detector] [Bounding Box] 목록
        /// </summary>
        public ObservableCollection<AiDetectionBox> EoDetectionBoxes { get; }
            = new ObservableCollection<AiDetectionBox>();

        /// <summary>
        /// [IR] 화면에 표시할 [AI Detector] [Bounding Box] 목록
        /// </summary>
        public ObservableCollection<AiDetectionBox> IrDetectionBoxes { get; }
            = new ObservableCollection<AiDetectionBox>();

        /// <summary>
        /// [AI Detector Agent]에서 조회한 [RTSP] 목록
        ///
        /// [CMD 52] 응답 결과를 화면에 표시하기 위해 사용한다.
        /// </summary>
        public ObservableCollection<AiRtspInfo> AiRtspList { get; }
            = new ObservableCollection<AiRtspInfo>();

        /// <summary>
        /// [AI Detector Agent]에서 조회한 [ONNX] 모델 목록
        ///
        /// [CMD 53] 응답 결과를 화면에 표시하기 위해 사용한다.
        /// </summary>
        public ObservableCollection<AiOnnxInfo> AiOnnxList { get; }
            = new ObservableCollection<AiOnnxInfo>();

        /// <summary>
        /// [AI Detector Agent]에서 조회한 [RTSP] / [ONNX] Mapping 목록
        ///
        /// [CMD 56] 응답 결과를 화면에 표시하기 위해 사용한다.
        /// </summary>
        public ObservableCollection<AiMappingInfo> AiMappingList { get; }
            = new ObservableCollection<AiMappingInfo>();

        #endregion
    }

}
