using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        // 화면에서 사용하는 기능별 연결 목록이다. 실제 생성은 FeatureAdapters/FeatureComposition이 담당한다.
        private readonly WebAgentMotionFocusFeature _webAgentMotionFocusFeature;
        private readonly WebAgentEquipmentFeature _webAgentEquipmentFeature;
        private readonly PanoramaReferenceFeature _panoramaReferenceFeature;
        private readonly PanoramaPresetsFeature _panoramaPresetsFeature;
        private readonly SpecialFeaturesFeature _specialFeaturesFeature;
        private readonly PresetLensFeature _presetLensFeature;
        private readonly InputCoordinatorFeature _inputCoordinatorFeature;
        private readonly CameraFeatureControlFeature _cameraFeatureControlFeature;
        private readonly OpticalCenterFeature _opticalCenterFeature;
        private readonly ScanTraversalFeature _scanTraversalFeature;
        private readonly RoutesFeature _routesFeature;
        private readonly ZonesFeature _zonesFeature;
        private readonly AiFeature _aiFeature;
        private readonly ContinuousControlFeature _continuousControlFeature;
        private readonly ControlAgentFeature _controlAgentFeature;
        private readonly WebAgentProtocolFeature _webAgentProtocolFeature;
        private readonly FireEventsFeature _fireEventsFeature;
        private readonly EventAlertsFeature _eventAlertsFeature;
        private readonly MoveControlFeature _moveControlFeature;
        private readonly PanoramaFeature _panoramaFeature;
        private readonly PositionFeature _positionFeature;
        private readonly PresetStorageFeature _presetStorageFeature;
        private readonly PropertiesFeature _propertiesFeature;
        private readonly RtspSettingsFeature _rtspSettingsFeature;
        private readonly RpySettingsFeature _rpySettingsFeature;
        private readonly GpsSettingsFeature _gpsSettingsFeature;
        private readonly DevicePowerFeature _devicePowerFeature;
        private readonly SystemDateTimeFeature _systemDateTimeFeature;
        private readonly SmokeFeature _smokeFeature;
        private readonly SyncFeature _syncFeature;
        private readonly ThermalFeature _thermalFeature;
        private readonly VideoFeature _videoFeature;
        private readonly ScreenStateFeature _screenStateFeature;

        // 2026-11-12: 생성자는 객체 합성과 서비스 연결 순서를 보이고, 명령 구성은 아래 기능별 단계에서 읽는다.
        /// <summary>
        /// 기능 객체의 초기 상태를 기존 순서로 준비한다. 모든 기능 객체 생성 후 한 번만 호출한다.
        /// </summary>
        private void InitializeFeatureState()
        {
            _webAgentMotionFocusFeature.InitializeState();
            _webAgentEquipmentFeature.InitializeState();
            _inputCoordinatorFeature.InitializeState();
            _cameraFeatureControlFeature.InitializeState();
            _routesFeature.InitializeState();
            _zonesFeature.InitializeState();
            _aiFeature.InitializeState();
            _webAgentProtocolFeature.InitializeState();
            _fireEventsFeature.InitializeState();
            _eventAlertsFeature.InitializeState();
            _panoramaFeature.InitializeState();
            _propertiesFeature.InitializeState();
            _rtspSettingsFeature.InitializeState();
            _rpySettingsFeature.InitializeState();
            _gpsSettingsFeature.InitializeState();
            _devicePowerFeature.InitializeState();
            _systemDateTimeFeature.InitializeState();
            _smokeFeature.InitializeState();
            _thermalFeature.InitializeState();
            _videoFeature.InitializeState();
            _screenStateFeature.InitializeState();
        }

        /// <summary>
        /// 십자선 표시와 줌 동기화 선택·실행 명령을 화면에 연결한다.
        /// </summary>
        private void InitializeDisplayAndSyncCommands()
        {
            // [EO / IR] 중앙 십자선 표시 상태 전환
            //
            // 버튼을 누르면 EO / IR 영상의 빨간색 십자선 표시 여부를 전환한다.
            //
            ToggleCrosshairCommand = new RelayCommand(() =>
            {
                IsCrosshairVisible = !IsCrosshairVisible;

                // 기존 고정 빨강 십자선 표시 동작을 유지한다.
                ConsoleLogHelper.Command("DISPLAY OVERLAY", CrosshairButtonText + " / fixed RED");
            });

            // [환경 장비 / 줌 동기화]
            //
            // Web Agent 기준 EO / IR Zoom Position을
            // 0부터 1000까지 100 단위로 생성한다.
            //
            // LEVEL과 Position을 동일한 기준으로 사용한다.
            //
            // LEVEL 0  = 0
            // LEVEL 1  = 100
            // LEVEL 2  = 200
            // ...
            // LEVEL 10 = 1000
            //
            // 2026-10-02: LEVEL 0 실측 Anchor를 GUI 선택 목록에도 포함한다.
            // Enumerable.Range(0, 11):
            // LEVEL 0부터 LEVEL 10까지 총 11개의 항목을 생성한다.
            //
            ZoomSyncLevelOptions = new ObservableCollection<ZoomSyncLevelOption>(Enumerable.Range(0, 11).Select(level => new ZoomSyncLevelOption(level, (short)(level * 100))));

            SelectedZoomSyncLevel = ZoomSyncLevelOptions[0];

            ShowRooftopStatusCommand = new RelayCommand(() => SelectedEquipmentStatusMode = EquipmentStatusMode.Rooftop);

            ShowEnvironmentStatusCommand = new RelayCommand(() => SelectedEquipmentStatusMode = EquipmentStatusMode.Environment);

            PreviousZoomSyncLevelCommand = new RelayCommand(SelectPreviousZoomSyncLevel);

            NextZoomSyncLevelCommand = new RelayCommand(SelectNextZoomSyncLevel);

            ApplyZoomSyncCommand = new AsyncRelayCommand(ApplySelectedZoomSyncLevelAsync);

            StopZoomSyncCommand = new AsyncRelayCommand(StopZoomSyncAsync);

            // 2026-09-21: Focus Sync는 현재 운용 UI에서 제외한다.
            // 아래 초기화 코드는 향후 요구가 생기면 #if를 제거해 즉시 복구한다.
#if false
            /// <summary>
            /// [EO / IR 초점 동기화]
            ///
            /// Focus 역시 Web Agent 표준 범위 0 ~ 1000을 사용하며,
            /// Zoom Sync와 동일하게 LEVEL 0 ~ LEVEL 10으로 구성한다.
            /// </summary>
            FocusSyncLevelOptions =
                new ObservableCollection<ZoomSyncLevelOption>(
                    Enumerable.Range(
                            0,
                            11)
                        .Select(level =>
                            new ZoomSyncLevelOption(
                                level,
                                (short)(
                                    level *
                                    100))));

            SelectedFocusSyncLevel =
                FocusSyncLevelOptions[0];

            PreviousFocusSyncLevelCommand =
                new RelayCommand(
                    SelectPreviousFocusSyncLevel);

            NextFocusSyncLevelCommand =
                new RelayCommand(
                    SelectNextFocusSyncLevel);

            ApplyFocusSyncCommand =
                new AsyncRelayCommand(
                    ApplySelectedFocusSyncLevelAsync);

            StopFocusSyncCommand =
                new AsyncRelayCommand(
                    StopFocusSyncAsync);
#endif
        }

        /// <summary>
        /// 연결·해제 명령과 시간·자세·GPS·전원 설정 명령을 준비한다.
        /// </summary>
        private void InitializeConnectionSettingCommands()
        {
            // [Connect] 버튼 클릭 시 호출
            //
            // 영상 스트림 및 [CONTROL AGENT] [TCP] 통신 연결을 시작한다.
            //
            ConnectCommand = new RelayCommand(Connect);

            // [Disconnect] 버튼 클릭 시 호출
            //
            // 영상 스트림 및 [CONTROL AGENT] [TCP] 통신 연결을 종료한다.
            //
            DisconnectCommand = new RelayCommand(Disconnect);

            // 2026-10-01: WebAgent Build 86 RPY Offset 및 자동 전송 주파수 명령 초기화.
            InitializeRpySettings();

            // 2026-10-01: WebAgent 시스템 UTC 조회 및 PC 시간 설정 명령 초기화.
            InitializeSystemDateTimeSettings();

            // 2026-10-02: GPS 20Byte 상태 및 0~10Hz 자동 전송 설정/조회 초기화.
            InitializeGpsSettings();

            // 2026-10-06: WebAgent EO/IR 전원 제어 및 Function 0x31 상태 동기화.
            InitializeDevicePowerSettings();
        }

        /// <summary>
        /// AI 연결·영상 주소·매핑·설정 조회 명령을 준비한다. 기존 요청 순서와 대기는 유지한다.
        /// </summary>
        private void InitializeAiSettingCommands()
        {
            // [AI Detector Agent] 수동 연결
            //
            // 기존 [AI Detector Agent] 연결 및 자동 재연결 루프를 정리한 뒤,
            // UI에 입력된 [IP] / [Port] 기준으로 수동 1회 연결을 수행한다.
            //
            ConnectAiAgentCommand = new AsyncRelayCommand(ConnectAiAgentFromSettingAsync);

            DisconnectAiAgentCommand = new RelayCommand(DisconnectAiAgent);

            // [AI Detector Agent] [RTSP] 주소 적용
            //
            // UI에 입력한 [RTSP 0] / [RTSP 1] 주소를
            // [CMD 02] 요청 Packet으로 송신한다.
            //
            ApplyAiRtspCommand = new AsyncRelayCommand(async () =>
            {
                AiSettingStatusText = "[AI] Apply RTSP...";

                bool applied = await RequestAiDetectorRtspAddressSetAsync();

                AiSettingStatusText = applied ? "[AI] RTSP Apply Complete" : "[AI] RTSP Apply Failed / Check configuration";
            });

            // [AI Detector Agent] Mapping 설정 적용
            //
            // UI에 입력한 [ONNX Index] / [Confidence] / [IOU] 값을
            // [CMD 05] 요청 Packet으로 송신한다.
            //
            ApplyAiMappingCommand = new AsyncRelayCommand(async () =>
            {
                AiSettingStatusText = "[AI] Apply Mapping...";

                await RequestAiDetectorMappingSetAsync();

                AiSettingStatusText = "[AI] Mapping Apply Complete";
            });

            // [AI Detector Agent] 현재 설정 조회
            //
            // [Detector Info] / [RTSP List] / [ONNX List] / [Mapping Info]를 순차 조회한다.
            //
            RefreshAiSettingCommand = new AsyncRelayCommand(async () =>
            {
                if (!_aiDetectorClientService.IsConnected)
                {
                    AiSettingStatusText = "[AI] Not Connected";

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine("[AI TCP] Refresh Failed : Not Connected");

                    ConsoleLogHelper.PrintLine();

                    return;
                }

                AiSettingStatusText = "[AI] Refresh Setting...";

                int refreshGeneration = Volatile.Read(ref _aiManualConnectionGeneration);

                await RequestAiDetectorInfoAsync();

                await Task.Delay(200);

                await RequestAiDetectorRtspAddressAsync();

                await Task.Delay(200);

                await RequestAiDetectorOnnxListAsync();

                await Task.Delay(200);

                await RequestAiDetectorMappingAsync();

                if (refreshGeneration != Volatile.Read(ref _aiManualConnectionGeneration) || !_aiDetectorClientService.IsConnected)
                {
                    return;
                }

                await RequestAiDetectorRtspVideoSizeAsync(0);

                await RequestAiDetectorRtspVideoSizeAsync(1);

                AiSettingStatusText = "[AI] Refresh Complete";
            });
        }

        /// <summary>
        /// 팬·틸트·줌·초점의 상대 이동 명령을 준비한다. 실제 제어는 기존 서비스에 위임한다.
        /// </summary>
        private void InitializeRelativeMovementCommands()
        {
            // [PAN] 왼쪽 상대 이동 테스트
            //
            // 현재 [PAN] 값에서 [1.0]도 감소한 값을 목표 각도로 송신한다.
            //
            PanLeftCommand = new RelayCommand(() =>
            {
                double targetPan = _currentPan - PanTiltMoveStep;

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] PAN -{PanTiltMoveStep} => Target : {targetPan:F2}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.PanGoPosition(targetPan);
            });

            // [PAN] 오른쪽 상대 이동 테스트
            //
            // 현재 [PAN] 값에서 [1.0]도 증가한 값을 목표 각도로 송신한다.
            //
            PanRightCommand = new RelayCommand(() =>
            {
                double targetPan = _currentPan + PanTiltMoveStep;

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] PAN +{PanTiltMoveStep} => Target : {targetPan:F2}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.PanGoPosition(targetPan);
            });

            // [TILT] 위쪽 상대 이동 테스트
            //
            // 현재 [TILT] 값에서 [1.0]도 증가한 값을 목표 각도로 송신한다.
            //
            TiltUpCommand = new RelayCommand(() =>
            {
                double targetTilt = _currentTilt + PanTiltMoveStep;

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] TILT +{PanTiltMoveStep} => Target : {targetTilt:F2}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.TiltGoPosition(targetTilt);
            });

            // [TILT] 아래쪽 상대 이동 테스트
            //
            // 현재 [TILT] 값에서 [1.0]도 감소한 값을 목표 각도로 송신한다.
            //
            TiltDownCommand = new RelayCommand(() =>
            {
                double targetTilt = _currentTilt - PanTiltMoveStep;

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] TILT -{PanTiltMoveStep} => Target : {targetTilt:F2}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.TiltGoPosition(targetTilt);
            });

            // [ZOOM] 확대 상대 이동 테스트
            //
            // 현재 [ZOOM] 값에서 [1] 증가한 값을 목표 위치로 송신한다.
            //
            ZoomInCommand = new RelayCommand(() =>
            {
                short targetZoom = (short)(_currentEoZoom + ZoomMoveStep);

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] ZOOM +{ZoomMoveStep} => Target : {targetZoom}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.EoZoomGoPosition(targetZoom);
            });

            // [ZOOM] 축소 상대 이동 테스트
            //
            // 현재 [ZOOM] 값에서 [1] 감소한 값을 목표 위치로 송신한다.
            //
            ZoomOutCommand = new RelayCommand(() =>
            {
                short targetZoom = (short)(_currentEoZoom - ZoomMoveStep);

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] ZOOM -{ZoomMoveStep} => Target : {targetZoom}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.EoZoomGoPosition(targetZoom);
            });

            FocusFarCommand = new RelayCommand(() =>
            {
                int targetFocus = Math.Max(0, _currentEoFocus - FocusMoveStep);

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] EO FOCUS FAR : " + $"{_currentEoFocus} -> {targetFocus}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.EoFocusGoPosition((short)targetFocus);
            });

            FocusNearCommand = new RelayCommand(() =>
            {
                int targetFocus = Math.Min(1000, _currentEoFocus + FocusMoveStep);

                Console.WriteLine();

                Console.WriteLine($"[CONTROL] EO FOCUS NEAR : " + $"{_currentEoFocus} -> {targetFocus}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.EoFocusGoPosition((short)targetFocus);
            });
        }

        /// <summary>
        /// HOME·ZERO·절대 이동·프리셋·자동 순회 명령을 화면에 연결한다.
        /// </summary>
        private void InitializePositionAndPresetCommands()
        {
            // VertiportNexus 이동 제어 기능을 현재 프로젝트 구조에 맞춰 연결한다.
            //
            // 포함 기능:
            // - Home Position : 현재 좌표계 기준 Pan 0° / Tilt 0° 절대 이동
            // - Pan Zero / Tilt Zero : MCB 직접 Set0 명령
            //
            MoveHomePositionCommand = new AsyncRelayCommand(MoveHomePositionAsync);

            SetPanZeroCommand = new AsyncRelayCommand(SetPanZeroAsync);

            SetTiltZeroCommand = new AsyncRelayCommand(SetTiltZeroAsync);

            MovePanAbsoluteCommand = new AsyncRelayCommand(MovePanAbsoluteFromInputAsync);

            MoveTiltAbsoluteCommand = new RelayCommand(MoveTiltAbsoluteFromInput);

            StopAbsoluteMoveCommand = new RelayCommand(StopAbsoluteMove);

            SetZoomPositionCommand = new AsyncRelayCommand(SetZoomPositionFromInputAsync);

            SetZoomRatioCommand = new AsyncRelayCommand(SetZoomRatioFromInputAsync);

            SetFocusPositionCommand = new AsyncRelayCommand(SetFocusPositionFromInputAsync);

            ResetPositionInputCommand = new RelayCommand(ResetMoveControlInput);

            AddOrUpdateLaPresetCommand = new RelayCommand(AddOrUpdateLaPresetPoint);

            ClearAllLaPresetsCommand = new RelayCommand(ClearAllLaPresetPoints);

            DeleteSelectedLaPresetCommand = new RelayCommand(DeleteSelectedLaPresetPoint);

            MoveToLaPresetCommand = new AsyncRelayCommand(MoveToSelectedLaPresetPointAsync);

            StartLaPresetScanCommand = new AsyncRelayCommand(StartLaPresetScanAsync);

            UpdateLaPresetScanCommand = new RelayCommand(UpdateLaPresetScan);

            StopLaPresetScanCommand = new RelayCommand(StopLaPresetScan);

            StopLaPresetMoveCommand = new RelayCommand(StopLaPresetMove);

            AddOrUpdatePresetCommand = new RelayCommand(AddOrUpdatePresetPoint);

            DeletePresetCommand = new RelayCommand(DeletePresetPoint);

            ClearAllPresetsCommand = new RelayCommand(ClearAllPresetPoints);

            MoveToPresetCommand = new RelayCommand(MoveToSelectedPresetPoint);

            StartPresetScanCommand = new RelayCommand(StartPresetScan);

            UpdatePresetScanCommand = new RelayCommand(UpdatePresetScan);

            StopPresetScanCommand = new RelayCommand(StopPresetScan);

            StopPresetMoveCommand = new RelayCommand(StopPresetMove);

            StopActivePresetScanCommand = new RelayCommand(StopActivePresetScan);
        }

        /// <summary>
        /// 거리 측정과 전체 정지 명령을 준비한다.
        /// </summary>
        private void InitializeMeasureAndStopCommands()
        {
            // [LRF] 거리측정 [1회] 요청
            //
            // 버튼 클릭 시
            // 거리측정기 [1회 측정] [Packet]을 송신한다.
            //
            LrfMeasureCommand = new RelayCommand(() =>
            {
                Console.WriteLine();

                ConsoleLogHelper.PrintLine();

                Console.WriteLine("[CONTROL] LRF MEASURE REQUEST");

                ConsoleLogHelper.PrintLine();

                _controlCommandService.ReadOnceLrfValue();
            });

            // [PT] 연속 이동 정지
            //
            // 현재 진행 중인
            // [PAN] / [TILT] / [Zoom] / [Focus]
            // 연속 이동을 정지한다.
            //
            StopMoveCommand = new RelayCommand(() =>
            {
                Console.WriteLine();

                Console.WriteLine("[CONTROL] STOP MOVE");

                ConsoleLogHelper.PrintLine();

                StopAllPtzfMove();
            });
        }
    }
}
