using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>수동 연속 제어에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IContinuousControlFeatureContext
    {
        RtspSourceOption _activeEoCtecSource { get; set; }

        KeyboardPanTiltDirection _activePanTiltMoveDirection { get; set; }

        System.Collections.Generic.Dictionary<string, RtspSourceOption> _axisCtecSources { get; }

        long _centerDeadlineTicks { get; set; }

        RtspSourceOption _connectedEoCtecSource { get; set; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> _controlInputs { get; }

        Services.Communication.ICtecCameraCommandServicePort _ctecCameraCommandService { get; }

        Services.Communication.ICtecCameraResponseServicePort _ctecCameraResponseService { get; }

        ref long _ctecEoPositionOperationGeneration { get; }

        ref global::System.Threading.CancellationTokenSource _ctecEoPositionPollingCts { get; }

        SemaphoreSlim _ctecEoPositionQueryLock { get; }

        byte _currentCtecEoFocusMode { get; set; }

        ushort _currentCtecEoFocusPosition { get; set; }

        ushort _currentCtecEoZoomPosition { get; set; }

        short _currentEoFocus { get; set; }

        short _currentEoZoom { get; set; }

        ushort _currentIrFocus { get; set; }

        ushort _currentIrZoom { get; set; }

        KeyboardPanTiltDirection _currentKeyboardPanTiltDirection { get; set; }

        ContinuousMoveType _currentMoveType { get; set; }

        int _environmentIrManualMoveDirection { get; set; }

        ref global::System.DateTime _environmentIrManualMoveStartedUtc { get; }

        ContinuousMoveType _environmentIrManualMoveType { get; set; }

        ref int _eoFocusCommandSequence { get; }

        System.Diagnostics.Stopwatch _focusLogStopwatch { get; }

        int _inputEpoch { get; set; }

        ref long _irLensStatusVersion { get; }

        bool _isDeviceConnectionRequested { get; set; }

        bool _isKeyboardPanLeftPressed { get; set; }

        bool _isKeyboardPanRightPressed { get; set; }

        bool _isKeyboardTiltDownPressed { get; set; }

        bool _isKeyboardTiltUpPressed { get; set; }

        bool _isPanTiltMoveActive { get; set; }

        System.Collections.Generic.Dictionary<string, Tuple<int, int>> _issuedControlAxes { get; }

        long _lastEoFocusCommandElapsedMs { get; set; }

        string _lastEoFocusCommandName { get; set; }

        ref global::System.DateTime _lastEoFocusCommandTime { get; }

        ref long _panStopTxCount { get; }

        ref long _panTiltStatusVersion { get; }

        bool _precisionStopUncertain { get; set; }

        string _ptzPerformanceScenario { get; set; }

        ref long _tiltStopTxCount { get; }

        bool _videoCenterBusy { get; set; }

        ref global::System.DateTimeOffset _zoneCtecZoomAt { get; }

        int _zoneCtecZoomEpoch { get; set; }

        string _zoneCtecZoomKey { get; set; }

        int _zoneEpoch { get; set; }

        ref long _zoomStartTxCount { get; }

        ref long _zoomStopTxCount { get; }

        /// <summary>
        /// 환경장비 Web Agent가 IR 상태 패킷을 보내지 않는 경우에만 사용하는 성공한 명령값 기반 UI 상태 보완이다. 실제 Function
        /// 0x07 패킷이 수신되면 실측값으로 다시 덮어쓴다.
        /// </summary>
        void ApplyEnvironmentIrCommandedPosition(int? zoomPosition, int? focusPosition, string reason);

        /// <summary>
        /// 현재 PAN / TILT SPEED UI 값으로 위치 이동 속도를 설정한다. Pan Position Speed : Command2 0x49
        /// Tilt Position Speed: Command2 0x4B UI 1~60 값을 위치 이동 속도 1~60 deg/s로 적용하며, UI 0은
        /// 위치 이동을 시작하지 않고 STOP으로 처리한다.
        /// </summary>
        bool ApplyPanTiltPositionSpeedFromUi(out double positionSpeed);

        /// <summary>
        /// EO 직접 제어 구성을 구분해 WEB AGENT AF 명령을 적용할 대상인지 확인한다.
        /// </summary>
        bool AutoFocusTargetUsesAgent(int target);

        /// <summary>
        /// 기존 VIA 0 Pan 이동 작업 취소
        /// </summary>
        void CancelMoveControlPanOperation();

        /// <summary>
        /// 연동 버튼과 공통 정지는 기존 단일 프리셋 이동의 취소 경로를 그대로 사용한다.
        /// </summary>
        void CancelPanoramaPresets();

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다.
        /// </summary>
        void ClearActivePanTiltAbsoluteMove();

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다.
        /// </summary>
        Services.Configuration.LensControlTuning ControlTuningFor(bool ir);

        string CurrentEoFocusText { get; }

        string CurrentEoZoomText { get; }

        string CurrentLaPresetSnapshotText { get; }

        string CurrentPresetSnapshotText { get; }

        byte EffectivePanTiltSpeedLevel { get; }

        string EnvironmentEoFocusStatusText { get; }

        string EnvironmentEoZoomStatusText { get; }

        /// <summary>
        /// 선택한 EO/IR AF 버튼 인수를 조회·설정·단발 초점 요청으로 변환한다.
        /// </summary>
        void ExecuteAutoFocus(object parameter);

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

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다.
        /// </summary>
        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsControlInputLocked { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        bool IsFovSpeedEnabled { get; set; }

        bool IsWebAgentFocusControlReady { get; }

        /// <summary>
        /// 채널별 렌즈 응답 시각을 원자적으로 기록한다.
        /// </summary>
        void MarkLensEvidence(bool ir);

        /// <summary>
        /// 팬·틸트 이동 명령 시점을 기록해 이전 수신 위치를 새 이동의 완료로 오인하지 않는다.
        /// </summary>
        void MarkPanTiltMoveCommandIssued();

        /// <summary>
        /// 2026-10-14 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다.
        /// </summary>
        void MarkPresetLensEvidence(bool ir, bool zoom, bool focus);

        /// <summary>
        /// EO 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void NotifyEoCurrentStatusChanged(bool panChanged = true, bool tiltChanged = true, bool zoomChanged = true, bool focusChanged = true, bool powerChanged = true);

        /// <summary>
        /// IR 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void NotifyIrCurrentStatusChanged();

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanTiltSpeedDisplayText { get; }

        byte PanTiltSpeedLevel { get; set; }

        /// <summary>
        /// 위치 이동 중 속도 변경 시 현재 Absolute 목표를 그대로 재송신한다. 일부 장비는 0x49 / 0x4B 속도 설정을 다음 위치 이동
        /// 명령부터 적용하므로, 진행 중인 0x45 / 0x47 목표를 다시 보내야 변경된 속도가 현재 이동에도 즉시 반영된다.
        /// </summary>
        void ReapplyActivePanTiltAbsoluteTarget(double positionSpeed);

        string RooftopEoFocusStatusText { get; }

        string RooftopEoZoomStatusText { get; }

        RtspSourceOption SelectedEoRtspSource { get; set; }

        EquipmentStatusMode SelectedEquipmentStatusMode { get; set; }

        /// <summary>
        /// 이전 방향의 경과 시간을 먼저 반영한 후 추정 배율의 증감 방향을 변경한다.
        /// </summary>
        void SetDigitalEstimateDirection(int direction, bool ir = false);

        /// <summary>
        /// 진행 중 정밀 동작 상태를 정리하고 필요할 때 양 축 정지를 요청한다.
        /// </summary>
        void StopPrecisionMotion(string reason, bool force = false);

        /// <summary>
        /// 현재 장비와 채널에 대응하는 구역 저장 키를 만든다.
        /// </summary>
        string ZoneKey(string camera);
    }

    /// <summary>수동 연속 제어 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class ContinuousControlFeature
    {

        #region [협력 객체와 초기화]

        private readonly IContinuousControlFeatureContext _context;

        /// <summary>
        /// 연속 이동과 정지 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal ContinuousControlFeature(IContinuousControlFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal int _activePanTiltZoomSource;

        #endregion

        #region [동작 처리]

        /// <summary>
        /// Keyboard 방향키 KeyDown 처리 방향키 눌림 상태를 저장한 뒤, 현재 눌린 전체 방향키 조합에 따라 단일 방향 또는 대각선 이동
        /// 명령을 송신한다.
        /// </summary>
        internal void HandlePanTiltKeyDown(Key key)
        {
            if (_context.IsControlInputLocked)
            {
                return;
            }

            if (!IsPanTiltKeyboardKey(key))
            {
                return;
            }

            /*
             * EO / IR Zoom 또는 Focus가 동작 중일 때
             * Keyboard Pan / Tilt 입력이 들어오면
             * 공통 Stop 명령과 충돌할 수 있으므로 무시한다.
             */
            // 2026-09-18: Lens 동작과 Pan/Tilt는 독립 축으로 동시에 허용한다.
            SetKeyboardPanTiltPressedState(key, true);

            UpdateKeyboardPanTiltMove();
        }

        /// <summary>
        /// Keyboard 방향키 KeyUp 처리 해제된 방향키 상태를 제거한 뒤, 아직 누르고 있는 나머지 방향키 기준으로 이동 방향을 다시
        /// 계산한다.
        /// </summary>
        internal void HandlePanTiltKeyUp(Key key)
        {
            if (_context.IsControlInputLocked)
            {
                ClearKeyboardPanTiltPressedState();

                _context._currentKeyboardPanTiltDirection = KeyboardPanTiltDirection.None;

                return;
            }

            if (!IsPanTiltKeyboardKey(key))
            {
                return;
            }

            SetKeyboardPanTiltPressedState(key, false);

            UpdateKeyboardPanTiltMove();
        }

        /// <summary>
        /// 장비 제어용 키보드 상태 전체 초기화 HOME POSITION 시작/완료/실패/Timeout 시 공통으로 호출한다. 현재 구현: - 방향키
        /// Pan/Tilt 상태 초기화 - WASD Pan/Tilt 상태 초기화 방향키와 WASD는 동일한 Pan/Tilt 상태 필드를 사용하므로
        /// ResetKeyboardPanTiltState 호출 한 번으로 함께 초기화된다. Zoom/Focus 단축키는 현재 별도 눌림 상태를 보관하지
        /// 않고 Window PreviewKeyDown/PreviewKeyUp 입구에서 HOME Lock 중 전체 차단한다. 추후 Zoom/Focus
        /// 키 상태 필드가 추가되면 반드시 이 함수에서 같이 초기화한다.
        /// </summary>
        internal void ResetAllKeyboardControlState()
        {
            ResetKeyboardPanTiltState();
        }

        /// <summary>
        /// Keyboard Pan / Tilt 상태 초기화 Window Focus 이탈로 KeyUp 이벤트가 누락될 경우 모든 방향키 상태를 초기화하고
        /// 현재 키보드 Pan / Tilt 이동을 정지한다.
        /// </summary>
        internal void ResetKeyboardPanTiltState()
        {
            bool wasKeyboardMoveActive = _context._currentKeyboardPanTiltDirection != KeyboardPanTiltDirection.None;

            KeyboardPanTiltDirection stopDirection = _context._activePanTiltMoveDirection;

            ClearKeyboardPanTiltPressedState();

            _context._currentKeyboardPanTiltDirection = KeyboardPanTiltDirection.None;

            if (!wasKeyboardMoveActive)
            {
                return;
            }

            // HOME / ZERO 또는 AUTO SCAN 중 Focus 이탈/KeyUp이 발생해도
            // Stop 명령을 송신하지 않아 진행 중인 동작을 보호한다.
            if (_context.IsControlInputLocked)
            {
                return;
            }

            if (!_context._isPanTiltMoveActive)
            {
                return;
            }

            Console.WriteLine();

            Console.WriteLine("[CONTROL] KEYBOARD PAN / TILT RESET");

            ConsoleLogHelper.PrintLine();

            StopPanTiltMoveByDirection(stopDirection);

            _context._isPanTiltMoveActive = false;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.None;
            _context._ptzPerformanceScenario = "IDLE";
        }

        /// <summary>
        /// Pan / Tilt Keyboard 제어 키 여부 확인
        /// </summary>
        internal bool IsPanTiltKeyboardKey(Key key)
        {
            return OpenCvWpfTracking.Common.ControlShortcutKeys.IsPanTiltKey(key);
        }

        /// <summary>
        /// Keyboard 방향키 입력 상태 반영
        /// </summary>
        internal void SetKeyboardPanTiltPressedState(Key key, bool isPressed)
        {
            switch (key)
            {
                case Key.Left:
                    _context._isKeyboardPanLeftPressed = isPressed;
                    break;
                case Key.Right:
                    _context._isKeyboardPanRightPressed = isPressed;
                    break;
                case Key.Up:
                    _context._isKeyboardTiltUpPressed = isPressed;
                    break;
                case Key.Down:
                    _context._isKeyboardTiltDownPressed = isPressed;
                    break;
            }
        }

        /// <summary>
        /// Keyboard 방향키 입력 상태 초기화
        /// </summary>
        internal void ClearKeyboardPanTiltPressedState()
        {
            _context._isKeyboardPanLeftPressed = false;
            _context._isKeyboardPanRightPressed = false;
            _context._isKeyboardTiltUpPressed = false;
            _context._isKeyboardTiltDownPressed = false;
        }

        /// <summary>
        /// 현재 Keyboard 입력 조합에 맞춰 Pan / Tilt 이동 방향 갱신 동일 방향이 유지되는 경우에는 KeyDown 자동 반복으로 인한
        /// 중복 패킷을 송신하지 않는다.
        /// </summary>
        internal void UpdateKeyboardPanTiltMove()
        {
            KeyboardPanTiltDirection targetDirection = GetKeyboardPanTiltDirection();

            if (_context._currentKeyboardPanTiltDirection == targetDirection)
            {
                return;
            }

            _context._currentKeyboardPanTiltDirection = targetDirection;

            switch (targetDirection)
            {
                case KeyboardPanTiltDirection.PanLeft:
                    StartPanLeftMove();
                    break;
                case KeyboardPanTiltDirection.PanRight:
                    StartPanRightMove();
                    break;
                case KeyboardPanTiltDirection.TiltUp:
                    StartTiltUpMove();
                    break;
                case KeyboardPanTiltDirection.TiltDown:
                    StartTiltDownMove();
                    break;
                case KeyboardPanTiltDirection.PanLeftTiltUp:
                    StartPanLeftTiltUpMove();
                    break;
                case KeyboardPanTiltDirection.PanRightTiltUp:
                    StartPanRightTiltUpMove();
                    break;
                case KeyboardPanTiltDirection.PanLeftTiltDown:
                    StartPanLeftTiltDownMove();
                    break;
                case KeyboardPanTiltDirection.PanRightTiltDown:
                    StartPanRightTiltDownMove();
                    break;
                case KeyboardPanTiltDirection.None:
                    StopKeyboardPanTiltMove();
                    break;
            }
        }

        /// <summary>
        /// 현재 눌린 방향키 조합을 Pan / Tilt 이동 방향으로 변환
        /// </summary>
        internal KeyboardPanTiltDirection GetKeyboardPanTiltDirection()
        {
            bool moveLeft = _context._isKeyboardPanLeftPressed && !_context._isKeyboardPanRightPressed;

            bool moveRight = _context._isKeyboardPanRightPressed && !_context._isKeyboardPanLeftPressed;

            bool moveUp = _context._isKeyboardTiltUpPressed && !_context._isKeyboardTiltDownPressed;

            bool moveDown = _context._isKeyboardTiltDownPressed && !_context._isKeyboardTiltUpPressed;

            if (moveLeft && moveUp)
            {
                return KeyboardPanTiltDirection.PanLeftTiltUp;
            }

            if (moveRight && moveUp)
            {
                return KeyboardPanTiltDirection.PanRightTiltUp;
            }

            if (moveLeft && moveDown)
            {
                return KeyboardPanTiltDirection.PanLeftTiltDown;
            }

            if (moveRight && moveDown)
            {
                return KeyboardPanTiltDirection.PanRightTiltDown;
            }

            if (moveLeft)
            {
                return KeyboardPanTiltDirection.PanLeft;
            }

            if (moveRight)
            {
                return KeyboardPanTiltDirection.PanRight;
            }

            if (moveUp)
            {
                return KeyboardPanTiltDirection.TiltUp;
            }

            if (moveDown)
            {
                return KeyboardPanTiltDirection.TiltDown;
            }

            return KeyboardPanTiltDirection.None;
        }

        /// <summary>
        /// Keyboard Pan / Tilt 이동 정지
        /// </summary>
        internal void StopKeyboardPanTiltMove()
        {
            if (!_context._isPanTiltMoveActive)
            {
                return;
            }

            Console.WriteLine();

            Console.WriteLine("[CONTROL] KEYBOARD PAN / TILT STOP");

            ConsoleLogHelper.PrintLine();

            StopPanTiltMoveByDirection(_context._activePanTiltMoveDirection);

            _context._isPanTiltMoveActive = false;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.None;
            _context._ptzPerformanceScenario = "IDLE";
        }

        /// <summary>
        /// 2026-09-15: Web Agent Pan/Tilt 연속 이동 해제 시 마지막 이동 축에 맞는 TORUSS 위치 정지 명령을 송신한다.
        /// 대각선은 두 축을 각각 정지하며, 방향 상태가 없을 때만 안전을 위해 기존 전체 정지 명령을 사용한다.
        /// </summary>
        internal bool StopPanTiltMoveByDirection(KeyboardPanTiltDirection direction)
        {
            switch (direction)
            {
                case KeyboardPanTiltDirection.PanLeft:
                case KeyboardPanTiltDirection.PanRight:
                    Interlocked.Increment(ref _context._panStopTxCount);
                    break;
                case KeyboardPanTiltDirection.TiltUp:
                case KeyboardPanTiltDirection.TiltDown:
                    Interlocked.Increment(ref _context._tiltStopTxCount);
                    break;
                default:
                    Interlocked.Increment(ref _context._panStopTxCount);

                    Interlocked.Increment(ref _context._tiltStopTxCount);
                    break;
            }

            // 옥상 LA 프로필은 기존 Pelco-D 전체 STOP을 유지한다.
            // 축별 0x4F 정지는 0~360 좌표계를 사용하는 Web Agent에만 적용한다.
            if (!_context._controlCommandService.UseUnsignedWebAgentPanCoordinates && !_context._controlCommandService.UseUnsignedWebAgentTiltCoordinates)
            {
                bool laResult = _context._controlCommandService.StopMove();

                ConsoleLogHelper.State("PAN / TILT STOP", $"DIRECTION={direction} / TYPE=LA_GLOBAL / RESULT={laResult}");

                return laResult;
            }

            bool result;

            string stopType;

            switch (direction)
            {
                case KeyboardPanTiltDirection.PanLeft:
                case KeyboardPanTiltDirection.PanRight:
                    stopType = "PAN";
                    result = _context._controlCommandService.StopPanPositionMove();
                    break;
                case KeyboardPanTiltDirection.TiltUp:
                case KeyboardPanTiltDirection.TiltDown:
                    stopType = "TILT";
                    result = _context._controlCommandService.StopTiltPositionMove();
                    break;
                case KeyboardPanTiltDirection.PanLeftTiltUp:
                case KeyboardPanTiltDirection.PanRightTiltUp:
                case KeyboardPanTiltDirection.PanLeftTiltDown:
                case KeyboardPanTiltDirection.PanRightTiltDown:
                    stopType = "PAN+TILT";
                    result = _context._controlCommandService.StopPanTiltPositionMove();
                    break;
                default:
                    stopType = "GLOBAL_FALLBACK";
                    result = _context._controlCommandService.StopMove();
                    break;
            }

            ConsoleLogHelper.State("PAN / TILT STOP", $"DIRECTION={direction} / TYPE={stopType} / RESULT={result}");

            return result;
        }

        /// <summary>
        /// 2026-09-17: Web Agent 합의 규격인 UI 속도 [0 ~ 60]을 그대로 사용한다. UI 0은 이동 속도로 사용하지 않으며
        /// 정지 요청으로 처리한다.
        /// </summary>
        internal byte ConvertPanTiltSpeedLevel(int uiSpeed)
        {
            if (uiSpeed <= 0)
            {
                return 0;
            }

            return (byte)Math.Min(60, uiSpeed);
        }

        /// <summary>
        /// 2026-10-01: 현재 활성 EO/IR 카메라의 광학 줌이 커질수록 같은 PT 속도도 화면에서는 더 크게 이동해 보이므로 실제 전송
        /// 속도를 낮춘다. UI Slider 값은 사용자가 선택한 기준 속도로 유지하며 광각(1x)에서는 기존 속도를 그대로 사용한다.
        /// 2026-10-01 실장비 로그에서 EO 최대 줌 시 20% 하한(30→6)이 빠르게 보이는 것을 확인하여 5% 안전 하한으로 완화한다.
        /// 실제 EO 90x 계산값은 약 10.5%이므로 기준 속도 30에서 약 3이 적용된다.
        /// </summary>
        internal byte ApplyZoomAdaptivePanTiltSpeed(byte baseProtocolSpeed, out int standardZoom, out string activeCamera, out double opticalZoomRatio, out double speedScale)
        {
            bool isInfrared = Volatile.Read(ref _activePanTiltZoomSource) == 1;

            activeCamera = isInfrared ? "IR" : "EO";

            standardZoom = isInfrared ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom();

            standardZoom = Math.Max(MoveControlPositionMinimum, Math.Min(MoveControlPositionMaximum, standardZoom));

            // 전체 범위 감속을 적용하고 로그에는 제한 적용 후 실제 프로토콜 속도를 남긴다.
            var tuning = _context.ControlTuningFor(isInfrared);

            opticalZoomRatio = tuning.Ratio(standardZoom);

            byte speed = tuning.Speed(baseProtocolSpeed, standardZoom, _context.IsFovSpeedEnabled);

            speedScale = baseProtocolSpeed == 0 ? 0 : speed / (double)baseProtocolSpeed;

            return speed;
        }

        /// <summary>
        /// 2026-10-01: 메인 화면의 활성 EO/IR 영상 변경을 PT 감쇄 기준에 반영한다. 신규 WebAgent 프로토콜 없이 기존 PT
        /// 이동 패킷의 속도값만 조정한다.
        /// </summary>
        internal void SetActivePanTiltZoomSource(bool isInfrared)
        {
            int nextSource = isInfrared ? 1 : 0;

            int previousSource = Interlocked.Exchange(ref _activePanTiltZoomSource, nextSource);

            if (previousSource == nextSource)
            {
                return;
            }

            _context.OnPropertyChanged(nameof(_context.EffectivePanTiltSpeedLevel));

            _context.OnPropertyChanged(nameof(_context.PanTiltSpeedDisplayText));

            int standardZoom = isInfrared ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom();

            ConsoleLogHelper.State("PAN / TILT SPEED SOURCE", $"ACTIVE_CAMERA={(isInfrared ? "IR" : "EO")} / " + $"ZOOM={standardZoom}/1000");
        }

        /// <summary>
        /// Pan / Tilt 이동에 사용할 Pelco-D 속도를 조회한다. UI 속도가 0이면 이동 패킷을 보내지 않고 STOP을 송신한 뒤 남아
        /// 있는 연속 이동 상태를 초기화한다.
        /// </summary>
        internal bool TryGetPanTiltProtocolSpeed(out byte protocolSpeed)
        {
            byte baseProtocolSpeed = ConvertPanTiltSpeedLevel(_context.PanTiltSpeedLevel);

            protocolSpeed = baseProtocolSpeed;

            if (baseProtocolSpeed > 0)
            {
                protocolSpeed = ApplyZoomAdaptivePanTiltSpeed(baseProtocolSpeed, out int standardZoom, out string activeCamera, out double opticalZoomRatio, out double speedScale);

                _context.ClearActivePanTiltAbsoluteMove();

                ConsoleLogHelper.State("PAN / TILT SPEED", $"Jog speed / UI_SPEED={_context.PanTiltSpeedLevel} / " + $"BASE_PROTOCOL_SPEED={baseProtocolSpeed} / " + $"ACTIVE_CAMERA={activeCamera} / ZOOM={standardZoom}/1000 / " + $"OPTICAL_ZOOM={opticalZoomRatio:F2}x / " + $"SCALE={speedScale:F3} / EFFECTIVE_PROTOCOL_SPEED={protocolSpeed} / " + $"MODE=GUI_ZOOM_ADAPTIVE / " + $"TRANSPORT={(_context._controlCommandService.UseUnsignedWebAgentPanCoordinates ? "WEB_AGENT" : "LA_AGENT")}");

                return true;
            }

            bool stopResult = StopPanTiltMoveByDirection(_context._activePanTiltMoveDirection);

            _context._isPanTiltMoveActive = false;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.None;

            ConsoleLogHelper.Warning("PAN / TILT", "Move blocked / UI_SPEED=0 / " + $"STOP_RESULT={stopResult}");

            return false;
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 좌측 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 좌측 방향으로 연속 이동
        /// 명령을 송신한다.
        /// </summary>
        internal void StartPanLeftMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.PanLeft;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] PAN LEFT START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartPanLeft(protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 우측 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 우측 방향으로 연속 이동
        /// 명령을 송신한다.
        /// </summary>
        internal void StartPanRightMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.PanRight;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] PAN RIGHT START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartPanRight(protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 위쪽 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 위쪽 방향으로 연속 이동
        /// 명령을 송신한다.
        /// </summary>
        internal void StartTiltUpMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.TiltUp;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] TILT UP START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartTiltUp(protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 아래쪽 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 아래 방향으로 연속
        /// 이동 명령을 송신한다.
        /// </summary>
        internal void StartTiltDownMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.TiltDown;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] TILT DOWN START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartTiltDown(protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 좌측 상단 대각선 연속 이동 시작
        /// </summary>
        internal void StartPanLeftTiltUpMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.PanLeftTiltUp;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] PAN LEFT + TILT UP START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartPanLeftTiltUp(protocolSpeed, protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 우측 상단 대각선 연속 이동 시작
        /// </summary>
        internal void StartPanRightTiltUpMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.PanRightTiltUp;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] PAN RIGHT + TILT UP START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartPanRightTiltUp(protocolSpeed, protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 좌측 하단 대각선 연속 이동 시작
        /// </summary>
        internal void StartPanLeftTiltDownMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.PanLeftTiltDown;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] PAN LEFT + TILT DOWN START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartPanLeftTiltDown(protocolSpeed, protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 우측 하단 대각선 연속 이동 시작
        /// </summary>
        internal void StartPanRightTiltDownMove()
        {
            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            _context._isPanTiltMoveActive = true;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.PanRightTiltDown;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] [EO/IR] PAN RIGHT + TILT DOWN START / " + $"UI SPEED : {_context.PanTiltSpeedLevel} / " + $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _context.MarkPanTiltMoveCommandIssued();

            _context._controlCommandService.StartPanRightTiltDown(protocolSpeed, protocolSpeed);
        }

        /// <summary>
        /// Pan / Tilt 연속 이동 중 Slider 값이 바뀌면 현재 방향을 유지한 채 Pelco-D Data1 / Data2 속도값만 갱신하여
        /// 즉시 재송신한다.
        /// </summary>
        internal void ApplyPanTiltSpeedWhileMoving()
        {
            if (_context.IsControlInputLocked)
            {
                return;
            }

            // 위치 이동 속도(0x49/0x4B)는 이동 전 설정뿐 아니라
            // ABSOLUTE 이동 중 Slider 변경에도 즉시 반영한다.
            if (!_context.ApplyPanTiltPositionSpeedFromUi(out double positionSpeed))
            {
                return;
            }

            // 장비가 새 위치 속도를 현재 이동에 적용하도록
            // 진행 중인 ABSOLUTE 목표(0x45/0x47)를 그대로 재송신한다.
            _context.ReapplyActivePanTiltAbsoluteTarget(positionSpeed);

            if (!_context._isPanTiltMoveActive || _context._activePanTiltMoveDirection == KeyboardPanTiltDirection.None)
            {
                return;
            }

            if (!TryGetPanTiltProtocolSpeed(out byte protocolSpeed))
            {
                return;
            }

            bool result;

            switch (_context._activePanTiltMoveDirection)
            {
                case KeyboardPanTiltDirection.PanLeft:
                    result = _context._controlCommandService.StartPanLeft(protocolSpeed);
                    break;
                case KeyboardPanTiltDirection.PanRight:
                    result = _context._controlCommandService.StartPanRight(protocolSpeed);
                    break;
                case KeyboardPanTiltDirection.TiltUp:
                    result = _context._controlCommandService.StartTiltUp(protocolSpeed);
                    break;
                case KeyboardPanTiltDirection.TiltDown:
                    result = _context._controlCommandService.StartTiltDown(protocolSpeed);
                    break;
                case KeyboardPanTiltDirection.PanLeftTiltUp:
                    result = _context._controlCommandService.StartPanLeftTiltUp(protocolSpeed, protocolSpeed);
                    break;
                case KeyboardPanTiltDirection.PanRightTiltUp:
                    result = _context._controlCommandService.StartPanRightTiltUp(protocolSpeed, protocolSpeed);
                    break;
                case KeyboardPanTiltDirection.PanLeftTiltDown:
                    result = _context._controlCommandService.StartPanLeftTiltDown(protocolSpeed, protocolSpeed);
                    break;
                case KeyboardPanTiltDirection.PanRightTiltDown:
                    result = _context._controlCommandService.StartPanRightTiltDown(protocolSpeed, protocolSpeed);
                    break;
                default:
                    return;
            }

            ConsoleLogHelper.Command("PAN / TILT SPEED", $"Updated while moving / UI_SPEED={_context.PanTiltSpeedLevel} / " + $"PROTOCOL_SPEED={protocolSpeed} / " + $"DIRECTION={_context._activePanTiltMoveDirection} / RESULT={result}");
        }

        /// <summary>
        /// [EO] 주간 카메라 [ZOOM] [Tele] 연속 이동 시작 옥상 MR300 EO 카메라 선택 시: - XV-Z4850HC CTEC CGI
        /// 직접 제어 그 외 EO 카메라 선택 시: - 기존 Control Agent 제어 유지
        /// </summary>
        internal async void StartEoZoomInMove()
        {
            await StartEoZoomInMoveAsync();
        }

        /// <summary>
        /// EO 줌 확대 연속 이동을 시작한다.
        /// </summary>
        internal async Task StartEoZoomInMoveAsync()
        {
            // 2026-10-01: 사용자가 조작한 Lens 채널을 PT 감쇄 기준과 UI 표시에 즉시 반영한다.
            SetActivePanTiltZoomSource(false);

            _context._ptzPerformanceScenario = "ZOOM_IN";

            Interlocked.Increment(ref _context._zoomStartTxCount);

            /*
             * Zoom 동작 시 카메라가 Focus를 자동 변경할 수 있으므로
             * 다음 Focus 입력은 새 상태값에서 다시 시작하도록 초기화한다.
             */
            _context._lastEoFocusCommandTime = DateTime.MinValue;
            _context._currentMoveType = ContinuousMoveType.EoZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] EO ZOOM TELE START");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(out RtspSourceOption ctecSource))
            {
                _context._activeEoCtecSource = ctecSource;

                Console.WriteLine("[CONTROL] EO ZOOM ROUTE : CTEC CGI DIRECT");

                result = await _context._ctecCameraCommandService.StartZoomTeleAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps, RooftopMr300EoCtecControlSpeed);

                if (!result)
                {
                    _context._activeEoCtecSource = null;

                    Console.WriteLine("[CONTROL] EO ZOOM CTEC FAILED; FALLBACK : CONTROL AGENT");

                    result = _context._controlCommandService.StartEoZoomTele();
                }
            }
            else
            {
                _context._activeEoCtecSource = null;

                Console.WriteLine("[CONTROL] EO ZOOM ROUTE : CONTROL AGENT");

                result = _context._controlCommandService.StartEoZoomTele();
            }

            Console.WriteLine($"[CONTROL] EO ZOOM TELE SEND RESULT : {result}");

            _context.SetDigitalEstimateDirection(result ? 1 : 0);

            ConsoleLogHelper.PrintLine();

            if (result && _context._activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(ContinuousMoveType.EoZoom, _context._activeEoCtecSource);
            }

            if (!result && _context._currentMoveType == ContinuousMoveType.EoZoom)
            {
                _context._currentMoveType = ContinuousMoveType.None;
                _context._activeEoCtecSource = null;
            }
        }

        /// <summary>
        /// [EO] 주간 카메라 [ZOOM] [Wide] 연속 이동 시작 선택된 EO 프리셋에 따라 CTEC CGI 직접 제어 또는 Control
        /// Agent 제어로 분기한다.
        /// </summary>
        internal async void StartEoZoomOutMove()
        {
            await StartEoZoomOutMoveAsync();
        }

        /// <summary>
        /// EO 줌 축소 연속 이동을 시작한다.
        /// </summary>
        internal async Task StartEoZoomOutMoveAsync()
        {
            SetActivePanTiltZoomSource(false);

            _context._ptzPerformanceScenario = "ZOOM_OUT";

            Interlocked.Increment(ref _context._zoomStartTxCount);

            /*
             * Zoom 동작 시 카메라가 Focus를 자동 변경할 수 있으므로
             * 다음 Focus 입력은 새 상태값에서 다시 시작하도록 초기화한다.
             */
            _context._lastEoFocusCommandTime = DateTime.MinValue;
            _context._currentMoveType = ContinuousMoveType.EoZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] EO ZOOM WIDE START");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(out RtspSourceOption ctecSource))
            {
                _context._activeEoCtecSource = ctecSource;

                Console.WriteLine("[CONTROL] EO ZOOM ROUTE : CTEC CGI DIRECT");

                result = await _context._ctecCameraCommandService.StartZoomWideAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps, RooftopMr300EoCtecControlSpeed);

                if (!result)
                {
                    _context._activeEoCtecSource = null;

                    Console.WriteLine("[CONTROL] EO ZOOM CTEC FAILED; FALLBACK : CONTROL AGENT");

                    result = _context._controlCommandService.StartEoZoomWide();
                }
            }
            else
            {
                _context._activeEoCtecSource = null;

                Console.WriteLine("[CONTROL] EO ZOOM ROUTE : CONTROL AGENT");

                result = _context._controlCommandService.StartEoZoomWide();
            }

            Console.WriteLine($"[CONTROL] EO ZOOM WIDE SEND RESULT : {result}");

            _context.SetDigitalEstimateDirection(result ? -1 : 0);

            ConsoleLogHelper.PrintLine();

            if (result && _context._activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(ContinuousMoveType.EoZoom, _context._activeEoCtecSource);
            }

            if (!result && _context._currentMoveType == ContinuousMoveType.EoZoom)
            {
                _context._currentMoveType = ContinuousMoveType.None;
                _context._activeEoCtecSource = null;
            }
        }

        /// <summary>
        /// [EO] 주간 카메라 Focus Near 연속 이동 시작 옥상 MR300 EO 선택 시: Focus Manual ->; Focus Near
        /// 순서로 CTEC CGI 직접 송신한다. 그 외 EO 선택 시: 기존 Control Agent Focus Near 명령을 유지한다.
        /// </summary>
        internal async void StartEoFocusNearMove()
        {
            await StartEoFocusNearMoveAsync();
        }

        /// <summary>
        /// EO 초점을 Near 방향으로 연속 이동하기 시작한다.
        /// </summary>
        internal async Task StartEoFocusNearMoveAsync()
        {
            if (_context.IsAutoFocusBlockingManualFocus(false))
            {
                return;
            }

            if (_context._currentMoveType != ContinuousMoveType.None)
            {
                return;
            }

            int sequence = Interlocked.Increment(ref _context._eoFocusCommandSequence);

            _context._lastEoFocusCommandName = "NEAR";
            _context._lastEoFocusCommandElapsedMs = _context._focusLogStopwatch.ElapsedMilliseconds;
            _context._currentMoveType = ContinuousMoveType.EoFocus;

            Console.WriteLine();

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + $"[FOCUS COMMAND #{sequence}] " + $"NEAR START / " + $"ELAPSED={_context._lastEoFocusCommandElapsedMs}ms / " + $"CURRENT={_context._currentEoFocus}");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(out RtspSourceOption ctecSource))
            {
                _context._activeEoCtecSource = ctecSource;

                Console.WriteLine("[CONTROL] EO FOCUS ROUTE : CTEC CGI DIRECT");

                result = await _context._ctecCameraCommandService.StartFocusNearAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps, RooftopMr300EoCtecControlSpeed);

                if (!result)
                {
                    _context._activeEoCtecSource = null;

                    Console.WriteLine("[CONTROL] EO FOCUS CTEC FAILED; FALLBACK : CONTROL AGENT");

                    result = _context._controlCommandService.StartEoFocusNear();
                }
            }
            else
            {
                _context._activeEoCtecSource = null;

                Console.WriteLine("[CONTROL] EO FOCUS ROUTE : CONTROL AGENT");

                result = _context._controlCommandService.StartEoFocusNear();
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + $"[FOCUS COMMAND #{sequence}] " + $"SEND RESULT={result}");

            if (result && _context._activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(ContinuousMoveType.EoFocus, _context._activeEoCtecSource);
            }

            if (!result && _context._currentMoveType == ContinuousMoveType.EoFocus)
            {
                _context._currentMoveType = ContinuousMoveType.None;
                _context._activeEoCtecSource = null;
            }
        }

        /// <summary>
        /// [EO] 주간 카메라 Focus Far 연속 이동 시작 옥상 MR300 EO 선택 시: Focus Manual ->; Focus Far 순서로
        /// CTEC CGI 직접 송신한다. 그 외 EO 선택 시: 기존 Control Agent Focus Far 명령을 유지한다.
        /// </summary>
        internal async void StartEoFocusFarMove()
        {
            await StartEoFocusFarMoveAsync();
        }

        /// <summary>
        /// EO 초점을 Far 방향으로 연속 이동하기 시작한다.
        /// </summary>
        internal async Task StartEoFocusFarMoveAsync()
        {
            if (_context.IsAutoFocusBlockingManualFocus(false))
            {
                return;
            }

            if (_context._currentMoveType != ContinuousMoveType.None)
            {
                return;
            }

            int sequence = Interlocked.Increment(ref _context._eoFocusCommandSequence);

            _context._lastEoFocusCommandName = "FAR";
            _context._lastEoFocusCommandElapsedMs = _context._focusLogStopwatch.ElapsedMilliseconds;
            _context._currentMoveType = ContinuousMoveType.EoFocus;

            Console.WriteLine();

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + $"[FOCUS COMMAND #{sequence}] " + $"FAR START / " + $"ELAPSED={_context._lastEoFocusCommandElapsedMs}ms / " + $"CURRENT={_context._currentEoFocus}");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(out RtspSourceOption ctecSource))
            {
                _context._activeEoCtecSource = ctecSource;

                Console.WriteLine("[CONTROL] EO FOCUS ROUTE : CTEC CGI DIRECT");

                result = await _context._ctecCameraCommandService.StartFocusFarAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps, RooftopMr300EoCtecControlSpeed);

                if (!result)
                {
                    _context._activeEoCtecSource = null;

                    Console.WriteLine("[CONTROL] EO FOCUS CTEC FAILED; FALLBACK : CONTROL AGENT");

                    result = _context._controlCommandService.StartEoFocusFar();
                }
            }
            else
            {
                _context._activeEoCtecSource = null;

                Console.WriteLine("[CONTROL] EO FOCUS ROUTE : CONTROL AGENT");

                result = _context._controlCommandService.StartEoFocusFar();
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + $"[FOCUS COMMAND #{sequence}] " + $"SEND RESULT={result}");

            if (result && _context._activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(ContinuousMoveType.EoFocus, _context._activeEoCtecSource);
            }

            if (!result && _context._currentMoveType == ContinuousMoveType.EoFocus)
            {
                _context._currentMoveType = ContinuousMoveType.None;
                _context._activeEoCtecSource = null;
            }
        }

        /// <summary>
        /// [EO] 주간 카메라 [One Push Focus] 요청 옥상 MR300 EO 선택 시 CTEC CGI 직접 제어, 그 외 EO 선택 시
        /// 기존 Control Agent 명령을 사용한다.
        /// </summary>
        internal async void StartEoAutoFocusMove()
        {
            // 기존 LENS 단발 자동 초점은 LA/WEB 공통으로 유지하고 새 AF 영역의 지원 조건과 분리한다.
            if (_context.IsWebAgentFocusControlReady && _context.AutoFocusTargetUsesAgent(0))
            {
                _context.ExecuteAutoFocus("03");

                return;
            }

            Console.WriteLine();

            Console.WriteLine("[CONTROL] EO ONE PUSH FOCUS REQUEST");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(out RtspSourceOption ctecSource))
            {
                result = await _context._ctecCameraCommandService.OnePushFocusAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps);
            }
            else
            {
                result = _context._controlCommandService.StartEoAutoFocus();
            }

            Console.WriteLine($"[CONTROL] EO ONE PUSH FOCUS RESULT : {result}");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 현재 선택된 EO 프리셋이 CTEC CGI 직접 제어 대상인지 확인한다. EO 주소가 프리셋 외부 값으로 변경된 경우에는 잘못된 카메라로
        /// 명령이 송신되지 않도록 기존 Control Agent 경로를 사용한다.
        /// </summary>
        internal bool TryGetSelectedEoCtecSource(out RtspSourceOption sourceOption)
        {
            sourceOption = _context.SelectedEoRtspSource;

            return sourceOption != null && sourceOption.ControlType == CameraControlType.CtecCgi && !string.IsNullOrWhiteSpace(sourceOption.ControlIp);
        }

        /// <summary>
        /// 현재 선택된 EO 프리셋 기준으로 CTEC Response TCP Port 9000 연결 시작 옥상 MR300 EO CTEC 직접 제어
        /// 프리셋이 아니면 기존 Response 연결을 종료하고 별도 TCP 연결을 생성하지 않는다.
        /// </summary>
        internal async Task StartSelectedEoCtecResponseAsync()
        {
            if (!TryGetSelectedEoCtecSource(out RtspSourceOption sourceOption))
            {
                _context._connectedEoCtecSource = null;

                // CTEC 수신 지원 여부는 EO 렌즈 경로이며 PT Agent 모드와 무관하다.
                _context._ctecCameraResponseService.Stop();

                _context.OnPropertyChanged(nameof(_context.CurrentEoZoomText));

                _context.OnPropertyChanged(nameof(_context.CurrentEoFocusText));

                return;
            }

            _context._connectedEoCtecSource = sourceOption;

            // LA/WEB 상태 모드는 Control Agent 프로필에서만 선택한다.
            _context._currentCtecEoZoomPosition = 0;
            _context._currentCtecEoFocusPosition = 0;
            _context._currentCtecEoFocusMode = 0;

            _context.OnPropertyChanged(nameof(_context.CurrentEoZoomText));

            _context.OnPropertyChanged(nameof(_context.EnvironmentEoZoomStatusText));

            _context.OnPropertyChanged(nameof(_context.CurrentEoFocusText));

            _context.OnPropertyChanged(nameof(_context.EnvironmentEoFocusStatusText));

            Console.WriteLine();

            Console.WriteLine($"[CTEC RESPONSE] START : " + $"{sourceOption.ControlIp}:{RooftopMr300EoCtecResponsePort}");

            ConsoleLogHelper.PrintLine();

            await _context._ctecCameraResponseService.StartAsync(sourceOption.ControlIp, RooftopMr300EoCtecResponsePort);
        }

        /// <summary>
        /// CTEC Response TCP 연결 상태 변경 처리 Connected 상태가 되면 현재 Zoom / Focus Position 및
        /// Focus Mode Inquiry를 순차 송신하여 초기 상태값을 조회한다.
        /// </summary>
        internal void OnCtecCameraResponseConnectionStatusChanged(string status)
        {
            Console.WriteLine($"[CTEC RESPONSE] STATUS : {status}");

            ConsoleLogHelper.PrintLine();

            if (!string.Equals(status, "Connected", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _ = RequestConnectedEoCtecStatusAsync();
        }

        /// <summary>
        /// 현재 연결된 옥상 MR300 EO 카메라 상태 조회 Inquiry 명령은 CGI로 송신하고, 실제 응답은 TCP Port 9000 수신
        /// 서비스에서 처리한다.
        /// </summary>
        internal async Task RequestConnectedEoCtecStatusAsync()
        {
            RtspSourceOption sourceOption = _context._connectedEoCtecSource;

            if (sourceOption == null || !_context._ctecCameraResponseService.IsConnected)
            {
                return;
            }

            await _context._ctecCameraCommandService.RequestZoomPositionAsync(sourceOption.ControlIp, sourceOption.ControlUserName, sourceOption.ControlPassword, sourceOption.UseHttps);

            await Task.Delay(100);

            await _context._ctecCameraCommandService.RequestFocusPositionAsync(sourceOption.ControlIp, sourceOption.ControlUserName, sourceOption.ControlPassword, sourceOption.UseHttps);

            await Task.Delay(100);

            await _context._ctecCameraCommandService.RequestFocusModeAsync(sourceOption.ControlIp, sourceOption.ControlUserName, sourceOption.ControlPassword, sourceOption.UseHttps);
        }

        /// <summary>
        /// CTEC Camera Response Packet 수신 처리 공통 Header: 0x99 0x55 Command Code: 0x47 =
        /// Zoom Position 0x48 = Focus Position 0x38 = Focus Mode
        /// </summary>
        internal void OnCtecCameraResponsePacketReceived(byte[] packet)
        {
            if (packet == null || packet.Length < 7 || packet[0] != 0x99 || packet[1] != 0x55 || packet[packet.Length - 1] != 0xFF)
            {
                Console.WriteLine("[CTEC RESPONSE] Invalid Packet");

                ConsoleLogHelper.PrintLine();

                return;
            }

            switch (packet[2])
            {
                case 0x47:
                    {
                        ushort zoomPosition = (ushort)((packet[4] << 8) | packet[5]);

                        _context._currentCtecEoZoomPosition = zoomPosition;

                        if (zoomPosition <= CtecEoZoomPositionMax)
                        {
                            _context.MarkLensEvidence(false);
                        }

                        _context.MarkPresetLensEvidence(false, zoomPosition <= CtecEoZoomPositionMax, false);

                        if (_context._connectedEoCtecSource != null)
                        {
                            _context._zoneCtecZoomAt = DateTimeOffset.Now;
                            _context._zoneCtecZoomEpoch = _context._zoneEpoch;

                            _context._zoneCtecZoomKey = _context.ZoneKey("EO");
                        }

                        _context.OnPropertyChanged(nameof(_context.CurrentEoZoomText));

                        _context.OnPropertyChanged(nameof(_context.RooftopEoZoomStatusText));

                        _context.OnPropertyChanged(nameof(_context.CurrentPresetSnapshotText));

                        _context.OnPropertyChanged(nameof(_context.CurrentLaPresetSnapshotText));

                        Console.WriteLine($"[CTEC RESPONSE] EO ZOOM POSITION : " + $"{BuildCtecPositionText(zoomPosition, CtecEoZoomPositionMax)} " + $"/ HEX=0x{zoomPosition:X4}");

                        break;
                    }

                case 0x48:
                    {
                        ushort focusPosition = (ushort)((packet[4] << 8) | packet[5]);

                        _context._currentCtecEoFocusPosition = focusPosition;

                        _context.MarkPresetLensEvidence(false, false, focusPosition <= CtecEoFocusPositionMax);

                        _context.OnPropertyChanged(nameof(_context.CurrentEoFocusText));

                        _context.OnPropertyChanged(nameof(_context.RooftopEoFocusStatusText));

                        _context.OnPropertyChanged(nameof(_context.CurrentPresetSnapshotText));

                        _context.OnPropertyChanged(nameof(_context.CurrentLaPresetSnapshotText));

                        Console.WriteLine($"[CTEC RESPONSE] EO FOCUS POSITION : " + $"{BuildCtecPositionText(focusPosition, CtecEoFocusPositionMax)} " + $"/ HEX=0x{focusPosition:X4}");

                        break;
                    }

                case 0x38:
                    {
                        _context._currentCtecEoFocusMode = packet[5];

                        string focusModeText = _context._currentCtecEoFocusMode == 0x02 ? "AUTO" : _context._currentCtecEoFocusMode == 0x03 ? "MANUAL" : $"UNKNOWN(0x{_context._currentCtecEoFocusMode:X2})";

                        Console.WriteLine($"[CTEC RESPONSE] EO FOCUS MODE : " + $"{focusModeText}");

                        break;
                    }

                default:
                    Console.WriteLine($"[CTEC RESPONSE] UNHANDLED CODE : " + $"0x{packet[2]:X2}");
                    break;
            }

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Tele] 연속 이동 시작
        /// </summary>
        internal void StartIrZoomInMove()
        {
            // 2026-10-01: IR Zoom 조작 중에는 IR 실제 줌값으로 감쇄하고 ACTIVE(IR)로 표시한다.
            SetActivePanTiltZoomSource(true);

            _context._ptzPerformanceScenario = "ZOOM_IN";

            Interlocked.Increment(ref _context._zoomStartTxCount);

            _context._currentMoveType = ContinuousMoveType.IrZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR ZOOM IN START");

            ConsoleLogHelper.PrintLine();

            bool result = _context._controlCommandService.StartIrZoomTele();

            _context.SetDigitalEstimateDirection(result ? 1 : 0, true);

            BeginEnvironmentIrManualEstimate(ContinuousMoveType.IrZoom, +1, result);
        }

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Wide] 연속 이동 시작
        /// </summary>
        internal void StartIrZoomOutMove()
        {
            SetActivePanTiltZoomSource(true);

            _context._ptzPerformanceScenario = "ZOOM_OUT";

            Interlocked.Increment(ref _context._zoomStartTxCount);

            _context._currentMoveType = ContinuousMoveType.IrZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR ZOOM OUT START");

            ConsoleLogHelper.PrintLine();

            bool result = _context._controlCommandService.StartIrZoomWide();

            _context.SetDigitalEstimateDirection(result ? -1 : 0, true);

            BeginEnvironmentIrManualEstimate(ContinuousMoveType.IrZoom, -1, result);
        }

        /// <summary>
        /// [IR] [ZOOM] 연속 이동 정지 IR Zoom 버튼 [MouseUp] 시에만 호출한다.
        /// </summary>
        internal void StopIrZoomMove()
        {
            _context._ptzPerformanceScenario = "IDLE";

            Interlocked.Increment(ref _context._zoomStopTxCount);

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR ZOOM STOP");

            ConsoleLogHelper.PrintLine();

            bool result = _context._controlCommandService.StopIrZoom();

            _context.SetDigitalEstimateDirection(0, true);

            CompleteEnvironmentIrManualEstimate(ContinuousMoveType.IrZoom, result);

            _context._currentMoveType = ContinuousMoveType.None;
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Near] 연속 이동 시작
        /// </summary>
        internal void StartIrFocusNearMove()
        {
            if (_context.IsAutoFocusBlockingManualFocus(true))
            {
                return;
            }

            _context._currentMoveType = ContinuousMoveType.IrFocus;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR FOCUS NEAR START");

            ConsoleLogHelper.PrintLine();

            // IR FOCUS - uses this entry point. LA display position is inverted
            // from raw feedback, so only LA manual controls exchange Near/Far.
            bool result = _context.IsSelectedControlAgentWebAgent
                ? _context._controlCommandService.StartIrFocusNear()
                : _context._controlCommandService.StartIrFocusFar();

            BeginEnvironmentIrManualEstimate(ContinuousMoveType.IrFocus, +1, result);
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Far] 연속 이동 시작
        /// </summary>
        internal void StartIrFocusFarMove()
        {
            if (_context.IsAutoFocusBlockingManualFocus(true))
            {
                return;
            }

            _context._currentMoveType = ContinuousMoveType.IrFocus;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR FOCUS FAR START");

            ConsoleLogHelper.PrintLine();

            // IR FOCUS +: preserve WEB, reverse LA relative to raw feedback.
            bool result = _context.IsSelectedControlAgentWebAgent
                ? _context._controlCommandService.StartIrFocusFar()
                : _context._controlCommandService.StartIrFocusNear();

            BeginEnvironmentIrManualEstimate(ContinuousMoveType.IrFocus, -1, result);
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] 연속 이동 정지
        /// </summary>
        internal void StopIrFocusMove()
        {
            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR FOCUS STOP");

            ConsoleLogHelper.PrintLine();

            bool result = _context._controlCommandService.StopIrFocus();

            CompleteEnvironmentIrManualEstimate(ContinuousMoveType.IrFocus, result);

            _context._currentMoveType = ContinuousMoveType.None;
        }

        /// <summary>
        /// IR 수동 이동 시작 시 위치 추정의 기준과 방향을 기록한다.
        /// </summary>
        internal void BeginEnvironmentIrManualEstimate(ContinuousMoveType moveType, int direction, bool commandResult)
        {
            if (!commandResult || _context.SelectedEquipmentStatusMode != EquipmentStatusMode.Environment || Interlocked.Read(ref _context._irLensStatusVersion) > 0)
            {
                return;
            }

            _context._environmentIrManualMoveType = moveType;
            _context._environmentIrManualMoveDirection = direction;
            _context._environmentIrManualMoveStartedUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// IR 수동 이동 종료 후 추정 위치와 상태 응답을 정리한다.
        /// </summary>
        internal void CompleteEnvironmentIrManualEstimate(ContinuousMoveType moveType, bool commandResult)
        {
            if (!commandResult || _context._environmentIrManualMoveType != moveType || _context._environmentIrManualMoveStartedUtc == default(DateTime) || Interlocked.Read(ref _context._irLensStatusVersion) > 0)
            {
                return;
            }

            double elapsedMs = Math.Max(0.0, (DateTime.UtcNow - _context._environmentIrManualMoveStartedUtc).TotalMilliseconds);

            int fullTravelMs = moveType == ContinuousMoveType.IrZoom ? EnvironmentIrZoomFullTravelMs : EnvironmentIrFocusFullTravelMs;

            int delta = (int)Math.Round(elapsedMs * 1000.0 / fullTravelMs) * _context._environmentIrManualMoveDirection;

            if (moveType == ContinuousMoveType.IrZoom)
            {
                _context.ApplyEnvironmentIrCommandedPosition(_context._currentIrZoom + delta, null, "ENVIRONMENT MANUAL ZOOM ESTIMATE");
            }
            else
            {
                _context.ApplyEnvironmentIrCommandedPosition(null, _context._currentIrFocus + delta, "ENVIRONMENT MANUAL FOCUS ESTIMATE");
            }

            _context._environmentIrManualMoveType = ContinuousMoveType.None;

            _context._environmentIrManualMoveStartedUtc = default(DateTime);

            _context._environmentIrManualMoveDirection = 0;
        }

        /// <summary>
        /// [IR] 열상 카메라 [Digital Zoom] 확대 시작
        /// </summary>
        internal void StartIrDigitalZoomInMove()
        {
            _context._currentMoveType = ContinuousMoveType.IrDigitalZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR DIGITAL ZOOM IN START");

            Console.WriteLine($"[CONTROL] Current Common Zoom : {_context._currentEoZoom}");

            ConsoleLogHelper.PrintLine();

            _context._controlCommandService.StartIrDigitalZoomIn();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Digital Zoom] 축소 시작
        /// </summary>
        internal void StartIrDigitalZoomOutMove()
        {
            _context._currentMoveType = ContinuousMoveType.IrDigitalZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR DIGITAL ZOOM OUT START");

            Console.WriteLine($"[CONTROL] Current Common Zoom : {_context._currentEoZoom}");

            ConsoleLogHelper.PrintLine();

            _context._controlCommandService.StartIrDigitalZoomOut();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Auto Focus] 요청
        /// </summary>
        internal void StartIrAutoFocusMove()
        {
            // 기존 LENS 단발 자동 초점은 LA/WEB 공통으로 유지하고 새 AF 영역의 지원 조건과 분리한다.
            if (_context.IsWebAgentFocusControlReady)
            {
                _context.ExecuteAutoFocus("13");

                return;
            }

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR AUTO FOCUS REQUEST");

            Console.WriteLine($"[CONTROL] Current Common Focus : {_context._currentEoFocus}");

            ConsoleLogHelper.PrintLine();

            _context._controlCommandService.StartIrAutoFocus();
        }

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 시작 기존 Polling이 남아 있으면 먼저 종료한 뒤, 현재 이동
        /// 종류에 맞는 Inquiry를 200ms 간격으로 반복한다. 주의: - Zoom / Focus 이동 명령을 반복 송신하지 않는다. -
        /// Position Inquiry만 반복 송신한다. - 실제 값 갱신은 TCP Port 9000 응답 수신부에서 처리한다.
        /// </summary>
        internal void StartCtecEoPositionPolling(ContinuousMoveType moveType, RtspSourceOption sourceOption)
        {
            StopCtecEoPositionPolling();

            if (sourceOption == null || (moveType != ContinuousMoveType.EoZoom && moveType != ContinuousMoveType.EoFocus))
            {
                return;
            }

            CancellationTokenSource pollingCts = new CancellationTokenSource();

            _context._ctecEoPositionPollingCts = pollingCts;

            long operationGeneration = Interlocked.Increment(ref _context._ctecEoPositionOperationGeneration);

            Console.WriteLine($"[CTEC POLLING] START : {moveType} / " + $"INTERVAL={CtecEoPositionPollingIntervalMs}ms");

            ConsoleLogHelper.PrintLine();

            _ = PollCtecEoPositionAsync(moveType, sourceOption, operationGeneration, pollingCts.Token);
        }

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 Loop
        /// </summary>
        internal async Task PollCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, long operationGeneration, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_context._currentMoveType != moveType || !_context._ctecCameraResponseService.IsConnected || operationGeneration != Interlocked.Read(ref _context._ctecEoPositionOperationGeneration))
                    {
                        break;
                    }

                    int? position = await RequestAndWaitCtecEoPositionAsync(moveType, sourceOption, cancellationToken);

                    if (!position.HasValue)
                    {
                        Console.WriteLine($"[CTEC POLLING] RESPONSE TIMEOUT : {moveType}");
                    }

                    /*
                     * 다음 Inquiry는 이전 TCP Position 응답을 받은 뒤에만 송신한다.
                     * 따라서 Camera 내부 응답이 늦어져도 미응답 Inquiry가 누적되지 않는다.
                     */
                    await Task.Delay(CtecEoPositionPollingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // MouseUp / MouseLeave / Disconnect에 의한 정상 종료
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CTEC POLLING] ERROR : {moveType} / {ex.Message}");

                ConsoleLogHelper.PrintLine();
            }
            finally
            {
                Console.WriteLine($"[CTEC POLLING] END : {moveType}");

                ConsoleLogHelper.PrintLine();
            }
        }

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다. HTTP 200
        /// OK만으로 조회 완료 처리하지 않는다. Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다.
        /// </summary>
        internal async Task<int?> RequestAndWaitCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, CancellationToken cancellationToken)
        {
            await _context._ctecEoPositionQueryLock.WaitAsync(cancellationToken);

            try
            {
                /*
                 * Query Lock을 획득한 뒤 송신된 Inquiry는
                 * MouseUp Cancel이 발생해도 해당 TCP 응답 또는 Timeout까지 소비한다.
                 *
                 * 여기서 응답 대기까지 즉시 취소하면 이전 Inquiry 응답이
                 * 다음 Stop 안정화 Inquiry를 잘못 완료시키는 문제가 다시 발생한다.
                 */
                Task<int?> responseTask;

                bool inquiryResult;

                if (moveType == ContinuousMoveType.EoZoom)
                {
                    responseTask = _context._ctecCameraResponseService.WaitForNextZoomPositionAsync(CtecEoPositionResponseTimeoutMs, CancellationToken.None);

                    inquiryResult = await _context._ctecCameraCommandService.RequestZoomPositionAsync(sourceOption.ControlIp, sourceOption.ControlUserName, sourceOption.ControlPassword, sourceOption.UseHttps);
                }
                else if (moveType == ContinuousMoveType.EoFocus)
                {
                    responseTask = _context._ctecCameraResponseService.WaitForNextFocusPositionAsync(CtecEoPositionResponseTimeoutMs, CancellationToken.None);

                    inquiryResult = await _context._ctecCameraCommandService.RequestFocusPositionAsync(sourceOption.ControlIp, sourceOption.ControlUserName, sourceOption.ControlPassword, sourceOption.UseHttps);
                }
                else
                {
                    return null;
                }

                if (!inquiryResult)
                {
                    Console.WriteLine($"[CTEC INQUIRY] SEND FAILED : {moveType}");

                    return null;
                }

                return await responseTask;
            }
            finally
            {
                _context._ctecEoPositionQueryLock.Release();
            }
        }

        /// <summary>
        /// [CTEC Stop] Stop 송신 이후 실제 Position 값이 안정될 때까지 확인한다. 단일 조회값을 최종 위치로 확정하지 않는다.
        /// 카메라 상태 갱신이 늦으면 이전 이동 방향의 값이 뒤늦게 반환될 수 있으므로, 연속 두 값이 허용 오차 안에 들어올 때 최종 위치로
        /// 판단한다.
        /// </summary>
        internal async Task WaitForCtecEoPositionStableAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, long operationGeneration)
        {
            int? previousPosition = null;

            for (int count = 0; count < CtecEoPositionSettleMaximumCount; count++)
            {
                if (operationGeneration != Interlocked.Read(ref _context._ctecEoPositionOperationGeneration) || !_context._ctecCameraResponseService.IsConnected)
                {
                    return;
                }

                int? currentPosition = await RequestAndWaitCtecEoPositionAsync(moveType, sourceOption, CancellationToken.None);

                if (currentPosition.HasValue && previousPosition.HasValue && Math.Abs(currentPosition.Value - previousPosition.Value) <= CtecEoPositionStableTolerance)
                {
                    Console.WriteLine($"[CTEC POSITION] STABLE : {moveType} / " + $"POSITION={currentPosition.Value} / " + $"COUNT={count + 1}");

                    ConsoleLogHelper.PrintLine();

                    return;
                }

                previousPosition = currentPosition;

                await Task.Delay(CtecEoPositionPollingIntervalMs);
            }

            Console.WriteLine($"[CTEC POSITION] SETTLE LIMIT : {moveType} / " + $"LAST={previousPosition?.ToString() ?? "NONE"}");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 종료
        /// </summary>
        internal void StopCtecEoPositionPolling()
        {
            CancellationTokenSource pollingCts = Interlocked.Exchange(ref _context._ctecEoPositionPollingCts, null);

            if (pollingCts == null)
            {
                return;
            }

            try
            {
                pollingCts.Cancel();
            }
            finally
            {
                pollingCts.Dispose();
            }
        }

        /// <summary>
        /// Pan·Tilt 연속 이동을 중지한다.
        /// </summary>
        internal void StopPanTiltContinuousMove()
        {
            if (!_context._isPanTiltMoveActive)
            {
                return;
            }

            StopPanTiltMoveByDirection(_context._activePanTiltMoveDirection);

            _context._isPanTiltMoveActive = false;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.None;

            if (_context._currentMoveType == ContinuousMoveType.None)
            {
                _context._ptzPerformanceScenario = "IDLE";
            }
        }

        /// <summary>
        /// 2026-09-15: 화면 중앙 STOP 버튼 전용 PTZF 전체 정지. 현재 GUI 이동 상태와 관계없이 Pelco-D 전체 STOP FF
        /// 01 00 00 00 00 01을 반드시 1회 송신한다. 옥상 CTEC EO 렌즈가 직접 제어 중이면 해당 렌즈도 함께 정지한다.
        /// </summary>
        internal async void StopAllPtzfMove()
        {
            _context.CancelPanoramaPresets();

            _context.StopPrecisionMotion("전체 수동 STOP", _context._precisionStopUncertain);

            _context.SetDigitalEstimateDirection(0);

            _context.SetDigitalEstimateDirection(0, true);

            // LA와 WEB AGENT 모두 절대 팬·틸트로 클릭 이동을 처리한다.
            if (_context._videoCenterBusy || _context._centerDeadlineTicks != 0)
            {
                _context._controlCommandService.StopPanTiltPositionMove();

                _context._centerDeadlineTicks = 0;
            }

            // 긴급 정지는 대기 중이거나 재실행될 입력까지 무효화한다.
            _context._axisCtecSources.TryGetValue("EZ", out RtspSourceOption zoomStopSource);

            _context._axisCtecSources.TryGetValue("EF", out RtspSourceOption focusStopSource);

            ++_context._inputEpoch;

            _context._controlInputs.Clear();

            _context._issuedControlAxes.Clear();

            _context._axisCtecSources.Clear();

            ContinuousMoveType moveType = _context._currentMoveType;

            // 2026-09-28: WebAgent는 Pan/Tilt 조그 정지에 축별 0x4F를 사용한다.
            // 중앙 STOP은 현재 UI 상태가 유실된 경우에도 두 축 정지를 모두
            // 송신한 다음 기존 전체 STOP을 보내도록 하여 장비 정지를 보장한다.
            bool positionStopResult = true;

            if (_context._controlCommandService.UseUnsignedWebAgentPanCoordinates || _context._controlCommandService.UseUnsignedWebAgentTiltCoordinates)
            {
                positionStopResult = _context._controlCommandService.StopPanTiltPositionMove();

                Interlocked.Increment(ref _context._panStopTxCount);

                Interlocked.Increment(ref _context._tiltStopTxCount);
            }

            if (moveType == ContinuousMoveType.EoZoom || moveType == ContinuousMoveType.IrZoom)
            {
                Interlocked.Increment(ref _context._zoomStopTxCount);
            }

            RtspSourceOption activeEoCtecSource = _context._activeEoCtecSource;

            _context.CancelMoveControlPanOperation();

            StopCtecEoPositionPolling();

            Interlocked.Increment(ref _context._ctecEoPositionOperationGeneration);

            ClearKeyboardPanTiltPressedState();

            _context._currentKeyboardPanTiltDirection = KeyboardPanTiltDirection.None;
            _context._currentMoveType = ContinuousMoveType.None;
            _context._isPanTiltMoveActive = false;
            _context._activePanTiltMoveDirection = KeyboardPanTiltDirection.None;
            _context._activeEoCtecSource = null;

            _context.ClearActivePanTiltAbsoluteMove();

            bool tcpStopResult = _context._controlCommandService.StopMove();

            bool ctecStopResult = true;

            if (zoomStopSource != null)
            {
                ctecStopResult &= await _context._ctecCameraCommandService.StopZoomAsync(zoomStopSource.ControlIp, zoomStopSource.ControlUserName, zoomStopSource.ControlPassword, zoomStopSource.UseHttps);
            }

            if (focusStopSource != null)
            {
                ctecStopResult &= await _context._ctecCameraCommandService.StopFocusAsync(focusStopSource.ControlIp, focusStopSource.ControlUserName, focusStopSource.ControlPassword, focusStopSource.UseHttps);
            }

            if (activeEoCtecSource != null)
            {
                if (moveType == ContinuousMoveType.EoZoom)
                {
                    ctecStopResult = await _context._ctecCameraCommandService.StopZoomAsync(activeEoCtecSource.ControlIp, activeEoCtecSource.ControlUserName, activeEoCtecSource.ControlPassword, activeEoCtecSource.UseHttps);
                }
                else if (moveType == ContinuousMoveType.EoFocus)
                {
                    ctecStopResult = await _context._ctecCameraCommandService.StopFocusAsync(activeEoCtecSource.ControlIp, activeEoCtecSource.ControlUserName, activeEoCtecSource.ControlPassword, activeEoCtecSource.UseHttps);
                }
            }

            ConsoleLogHelper.Command("PTZF GLOBAL STOP", $"PACKET=FF 01 00 00 00 00 01 / TCP={tcpStopResult} / " + $"POSITION_STOP_0x4F={positionStopResult} / " + $"PREVIOUS_MOVE={moveType} / CTEC={ctecStopResult}");
        }

        /// <summary>
        /// 연속 이동 정지 버튼 [MouseUp] 또는 [MouseLeave] 시 호출된다. 옥상 MR300 EO Zoom / Focus는 CTEC
        /// CGI 전용 Stop 명령을 송신하고, 그 외 제어는 기존 Control Agent Stop 명령을 유지한다.
        /// </summary>
        internal async void StopContinuousMove()
        {
            await StopContinuousMoveAsync();
        }

        /// <summary>
        /// 현재 연속 이동을 중지하고 필요한 위치 응답의 안정화를 확인한다.
        /// </summary>
        internal async Task StopContinuousMoveAsync(bool waitForStatus = true)
        {
            // HOME / ZERO 또는 AUTO SCAN 명령 이후 MouseUp/MouseLeave/Focus 이탈로
            // 공통 Stop이 뒤늦게 들어오면 진행 중인 동작이 중단될 수 있다.
            // 제어 Lock 중에는 외부 Stop 요청을 무시한다.
            if (_context.IsControlInputLocked)
            {
                return;
            }

            _context._ptzPerformanceScenario = "IDLE";

            /*
             * 이동 제어 VIA 0 Pan 작업이 실행 중이면
             * Stop 버튼에서도 동일하게 취소되도록 먼저 종료한다.
             */
            _context.CancelMoveControlPanOperation();

            ContinuousMoveType moveType = _context._currentMoveType;

            KeyboardPanTiltDirection panTiltDirection = _context._activePanTiltMoveDirection;

            if (moveType == ContinuousMoveType.None)
            {
                return;
            }

            /*
             * Zoom / Focus 위치 조회 Loop를 먼저 종료한다.
             * 이후 Stop 명령과 최종 Inquiry가 Polling 요청 사이에 섞이지 않도록 한다.
             */
            StopCtecEoPositionPolling();

            long stopOperationGeneration = Interlocked.Increment(ref _context._ctecEoPositionOperationGeneration);

            /*
             * Stop 중복 호출을 방지하기 위해
             * 실제 비동기 송신 전에 이동 상태를 먼저 초기화한다.
             */
            _context._currentMoveType = ContinuousMoveType.None;

            RtspSourceOption activeEoCtecSource = _context._activeEoCtecSource;

            _context._activeEoCtecSource = null;

            Console.WriteLine();

            Console.WriteLine($"[CONTROL] MOVE STOP: {moveType}");

            ConsoleLogHelper.PrintLine();

            switch (moveType)
            {
                case ContinuousMoveType.PanTilt:
                    StopPanTiltMoveByDirection(panTiltDirection);
                    break;
                case ContinuousMoveType.EoZoom:
                    {
                        _context.SetDigitalEstimateDirection(0);

                        if (activeEoCtecSource != null)
                        {
                            bool stopResult = await _context._ctecCameraCommandService.StopZoomAsync(activeEoCtecSource.ControlIp, activeEoCtecSource.ControlUserName, activeEoCtecSource.ControlPassword, activeEoCtecSource.UseHttps);

                            Console.WriteLine($"[CONTROL] EO CTEC ZOOM STOP RESULT : {stopResult}");

                            if (waitForStatus && stopResult && _context._ctecCameraResponseService.IsConnected)
                            {
                                await WaitForCtecEoPositionStableAsync(ContinuousMoveType.EoZoom, activeEoCtecSource, stopOperationGeneration);
                            }
                        }
                        else
                        {
                            // 2026-09-17: 실장비 로그에서 v1.8 EO Zoom 전용 STOP
                            // (00/51/01/00) 송신 후에도 Zoom 값이 계속 변하는 것을 확인했다.
                            // 장비 안전을 위해 Web Agent 구현이 확인될 때까지 검증된 전체 STOP을 사용한다.
                            bool stopResult = _context._controlCommandService.StopMove();

                            ConsoleLogHelper.Command("EO ZOOM STOP", $"TYPE=GLOBAL_COMPAT / PACKET=FF 01 00 00 00 00 01 / " + $"REASON=WEB_AGENT_00_51_01_00_NOT_APPLIED / RESULT={stopResult}");

                            if (waitForStatus && stopResult)
                            {
                                await WaitForControlAgentLensStatusAfterStopAsync(ContinuousMoveType.EoZoom);
                            }
                        }

                        break;
                    }

                case ContinuousMoveType.EoFocus:
                    {
                        long elapsedMs = _context._focusLogStopwatch.ElapsedMilliseconds;

                        long commandDurationMs = elapsedMs - _context._lastEoFocusCommandElapsedMs;

                        Console.WriteLine();

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + $"[FOCUS COMMAND] STOP / " + $"DIRECTION={_context._lastEoFocusCommandName} / " + $"HELD={commandDurationMs}ms / " + $"CURRENT={_context._currentEoFocus}");

                        ConsoleLogHelper.PrintLine();

                        bool stopResult;

                        if (activeEoCtecSource != null)
                        {
                            stopResult = await _context._ctecCameraCommandService.StopFocusAsync(activeEoCtecSource.ControlIp, activeEoCtecSource.ControlUserName, activeEoCtecSource.ControlPassword, activeEoCtecSource.UseHttps);
                        }
                        else
                        {
                            // 2026-09-17: EO Zoom 전용 STOP 미적용 실장비와 동일 계열인
                            // EO Focus도 검증 전까지 전체 STOP을 사용하여 연속 구동을 방지한다.
                            stopResult = _context._controlCommandService.StopMove();

                            ConsoleLogHelper.Command("EO FOCUS STOP", $"TYPE=GLOBAL_COMPAT / PACKET=FF 01 00 00 00 00 01 / " + $"REASON=WEB_AGENT_LENS_STOP_UNVERIFIED / RESULT={stopResult}");
                        }

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + $"[FOCUS COMMAND] STOP RESULT={stopResult}");

                        if (waitForStatus && stopResult && activeEoCtecSource != null && _context._ctecCameraResponseService.IsConnected)
                        {
                            await WaitForCtecEoPositionStableAsync(ContinuousMoveType.EoFocus, activeEoCtecSource, stopOperationGeneration);
                        }
                        else if (waitForStatus && stopResult && activeEoCtecSource == null)
                        {
                            await WaitForControlAgentLensStatusAfterStopAsync(ContinuousMoveType.EoFocus);
                        }

                        break;
                    }

                case ContinuousMoveType.IrZoom:
                    {
                        bool stopResult = _context._controlCommandService.StopIrZoom();

                        if (stopResult)
                        {
                            await WaitForControlAgentLensStatusAfterStopAsync(ContinuousMoveType.IrZoom);
                        }

                        break;
                    }

                case ContinuousMoveType.IrFocus:
                    {
                        bool stopResult = _context._controlCommandService.StopIrFocus();

                        if (stopResult)
                        {
                            await WaitForControlAgentLensStatusAfterStopAsync(ContinuousMoveType.IrFocus);
                        }

                        break;
                    }

                case ContinuousMoveType.IrDigitalZoom:
                    _context._controlCommandService.StopIrDigitalZoom();
                    break;
            }
        }

        /// <summary>
        /// 2026-08-27: Control Agent 연속 Zoom / Focus STOP 이후 STOP보다 나중에 수신된 실제 장비 상태
        /// Packet을 확인한다. Control Agent 프로토콜에는 별도 Lens Inquiry 명령이 없으므로 임의 예측값을 만들지 않고 주기
        /// 상태 Packet을 최대 600ms만 기다린다. 새 이동이 시작되면 이전 확인 작업은 즉시 종료한다.
        /// </summary>
        internal async Task WaitForControlAgentLensStatusAfterStopAsync(ContinuousMoveType moveType)
        {
            bool isIr = moveType == ContinuousMoveType.IrZoom || moveType == ContinuousMoveType.IrFocus;

            long observedVersion = isIr ? Interlocked.Read(ref _context._irLensStatusVersion) : Interlocked.Read(ref _context._panTiltStatusVersion);

            const int maximumAttempts = 6;

            for (int attempt = 1; attempt <= maximumAttempts; attempt++)
            {
                await Task.Delay(100);

                if (_context._currentMoveType != ContinuousMoveType.None || !_context._isDeviceConnectionRequested)
                {
                    return;
                }

                long currentVersion = isIr ? Interlocked.Read(ref _context._irLensStatusVersion) : Interlocked.Read(ref _context._panTiltStatusVersion);

                if (currentVersion == observedVersion)
                {
                    continue;
                }

                if (isIr)
                {
                    _context.NotifyIrCurrentStatusChanged();
                }
                else
                {
                    _context.NotifyEoCurrentStatusChanged();
                }

                ConsoleLogHelper.State("LENS STATUS", "Post-stop device status applied / TYPE=" + moveType + " / ATTEMPT=" + attempt + " / EO_ZOOM=" + _context.GetCurrentPresetStandardZoom() + " / EO_FOCUS=" + _context.GetCurrentPresetStandardFocus() + " / IR_ZOOM=" + _context.GetCurrentIrZoomStandardPosition() + " / IR_FOCUS=" + _context.GetCurrentIrFocusStandardPosition());

                return;
            }

            ConsoleLogHelper.Warning("LENS STATUS", "Post-stop status packet timeout / TYPE=" + moveType + " / WAIT_MS=" + (maximumAttempts * 100));
        }

        #endregion

    }
}
