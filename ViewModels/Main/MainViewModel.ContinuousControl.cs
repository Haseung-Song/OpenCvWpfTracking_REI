using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 키보드 및 버튼 기반 Pan/Tilt/Zoom/Focus 연속 제어와 공통 Stop을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Continuous Move Control Methods]

        #region [Keyboard Pan / Tilt Control Methods]

        /// <summary>
        /// Keyboard 방향키 KeyDown 처리
        ///
        /// 방향키 눌림 상태를 저장한 뒤,
        /// 현재 눌린 전체 방향키 조합에 따라
        /// 단일 방향 또는 대각선 이동 명령을 송신한다.
        /// </summary>
        public void HandlePanTiltKeyDown(
            Key key)
        {
            if (IsControlInputLocked)
            {
                return;
            }

            if (!IsPanTiltKeyboardKey(
                    key))
            {
                return;
            }

            /*
             * EO / IR Zoom 또는 Focus가 동작 중일 때
             * Keyboard Pan / Tilt 입력이 들어오면
             * 공통 Stop 명령과 충돌할 수 있으므로 무시한다.
             */
            if (_currentMoveType !=
                    ContinuousMoveType.None &&
                _currentMoveType !=
                    ContinuousMoveType.PanTilt)
            {
                return;
            }

            SetKeyboardPanTiltPressedState(
                key,
                true);

            UpdateKeyboardPanTiltMove();
        }

        /// <summary>
        /// Keyboard 방향키 KeyUp 처리
        ///
        /// 해제된 방향키 상태를 제거한 뒤,
        /// 아직 누르고 있는 나머지 방향키 기준으로
        /// 이동 방향을 다시 계산한다.
        /// </summary>
        public void HandlePanTiltKeyUp(
            Key key)
        {
            if (IsControlInputLocked)
            {
                ClearKeyboardPanTiltPressedState();
                _currentKeyboardPanTiltDirection =
                    KeyboardPanTiltDirection.None;

                return;
            }

            if (!IsPanTiltKeyboardKey(
                    key))
            {
                return;
            }

            SetKeyboardPanTiltPressedState(
                key,
                false);

            UpdateKeyboardPanTiltMove();
        }

        /// <summary>
        /// 장비 제어용 키보드 상태 전체 초기화
        ///
        /// HOME POSITION 시작/완료/실패/Timeout 시 공통으로 호출한다.
        ///
        /// 현재 구현:
        /// - 방향키 Pan/Tilt 상태 초기화
        /// - WASD Pan/Tilt 상태 초기화
        ///
        /// 방향키와 WASD는 동일한 Pan/Tilt 상태 필드를 사용하므로
        /// ResetKeyboardPanTiltState 호출 한 번으로 함께 초기화된다.
        ///
        /// Zoom/Focus 단축키는 현재 별도 눌림 상태를 보관하지 않고
        /// Window PreviewKeyDown/PreviewKeyUp 입구에서 HOME Lock 중 전체 차단한다.
        /// 추후 Zoom/Focus 키 상태 필드가 추가되면 반드시 이 함수에서 같이 초기화한다.
        /// </summary>
        public void ResetAllKeyboardControlState()
        {
            ResetKeyboardPanTiltState();
        }

        /// <summary>
        /// Keyboard Pan / Tilt 상태 초기화
        ///
        /// Window Focus 이탈로 KeyUp 이벤트가 누락될 경우
        /// 모든 방향키 상태를 초기화하고
        /// 현재 키보드 Pan / Tilt 이동을 정지한다.
        /// </summary>
        public void ResetKeyboardPanTiltState()
        {
            bool wasKeyboardMoveActive =
                _currentKeyboardPanTiltDirection !=
                KeyboardPanTiltDirection.None;

            ClearKeyboardPanTiltPressedState();

            _currentKeyboardPanTiltDirection =
                KeyboardPanTiltDirection.None;

            if (!wasKeyboardMoveActive)
            {
                return;
            }

            // HOME / ZERO 또는 AUTO SCAN 중 Focus 이탈/KeyUp이 발생해도
            // Stop 명령을 송신하지 않아 진행 중인 동작을 보호한다.
            if (IsControlInputLocked)
            {
                return;
            }

            if (_currentMoveType !=
                ContinuousMoveType.PanTilt)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(
                "[CONTROL] KEYBOARD PAN / TILT RESET");

            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StopMove();

            _currentMoveType =
                ContinuousMoveType.None;
        }

        /// <summary>
        /// Pan / Tilt Keyboard 제어 키 여부 확인
        /// </summary>
        private bool IsPanTiltKeyboardKey(
            Key key)
        {
            return key == Key.Left ||
                   key == Key.Right ||
                   key == Key.Up ||
                   key == Key.Down;
        }

        /// <summary>
        /// Keyboard 방향키 입력 상태 반영
        /// </summary>
        private void SetKeyboardPanTiltPressedState(
            Key key,
            bool isPressed)
        {
            switch (key)
            {
                case Key.Left:

                    _isKeyboardPanLeftPressed =
                        isPressed;

                    break;

                case Key.Right:

                    _isKeyboardPanRightPressed =
                        isPressed;

                    break;

                case Key.Up:

                    _isKeyboardTiltUpPressed =
                        isPressed;

                    break;

                case Key.Down:

                    _isKeyboardTiltDownPressed =
                        isPressed;

                    break;
            }

        }

        /// <summary>
        /// Keyboard 방향키 입력 상태 초기화
        /// </summary>
        private void ClearKeyboardPanTiltPressedState()
        {
            _isKeyboardPanLeftPressed =
                false;

            _isKeyboardPanRightPressed =
                false;

            _isKeyboardTiltUpPressed =
                false;

            _isKeyboardTiltDownPressed =
                false;
        }

        /// <summary>
        /// 현재 Keyboard 입력 조합에 맞춰
        /// Pan / Tilt 이동 방향 갱신
        ///
        /// 동일 방향이 유지되는 경우에는
        /// KeyDown 자동 반복으로 인한 중복 패킷을 송신하지 않는다.
        /// </summary>
        private void UpdateKeyboardPanTiltMove()
        {
            KeyboardPanTiltDirection targetDirection =
                GetKeyboardPanTiltDirection();

            if (_currentKeyboardPanTiltDirection ==
                targetDirection)
            {
                return;
            }

            _currentKeyboardPanTiltDirection =
                targetDirection;

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
        /// 현재 눌린 방향키 조합을
        /// Pan / Tilt 이동 방향으로 변환
        /// </summary>
        private KeyboardPanTiltDirection
            GetKeyboardPanTiltDirection()
        {
            bool moveLeft =
                _isKeyboardPanLeftPressed &&
                !_isKeyboardPanRightPressed;

            bool moveRight =
                _isKeyboardPanRightPressed &&
                !_isKeyboardPanLeftPressed;

            bool moveUp =
                _isKeyboardTiltUpPressed &&
                !_isKeyboardTiltDownPressed;

            bool moveDown =
                _isKeyboardTiltDownPressed &&
                !_isKeyboardTiltUpPressed;

            if (moveLeft &&
                moveUp)
            {
                return KeyboardPanTiltDirection
                    .PanLeftTiltUp;
            }

            if (moveRight &&
                moveUp)
            {
                return KeyboardPanTiltDirection
                    .PanRightTiltUp;
            }

            if (moveLeft &&
                moveDown)
            {
                return KeyboardPanTiltDirection
                    .PanLeftTiltDown;
            }

            if (moveRight &&
                moveDown)
            {
                return KeyboardPanTiltDirection
                    .PanRightTiltDown;
            }

            if (moveLeft)
            {
                return KeyboardPanTiltDirection
                    .PanLeft;
            }

            if (moveRight)
            {
                return KeyboardPanTiltDirection
                    .PanRight;
            }

            if (moveUp)
            {
                return KeyboardPanTiltDirection
                    .TiltUp;
            }

            if (moveDown)
            {
                return KeyboardPanTiltDirection
                    .TiltDown;
            }

            return KeyboardPanTiltDirection.None;
        }

        /// <summary>
        /// Keyboard Pan / Tilt 이동 정지
        /// </summary>
        private void StopKeyboardPanTiltMove()
        {
            if (_currentMoveType !=
                ContinuousMoveType.PanTilt)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(
                "[CONTROL] KEYBOARD PAN / TILT STOP");

            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StopMove();

            _currentMoveType =
                ContinuousMoveType.None;
        }

        #endregion

        #region [EO/IR] [Pan / Tilt Continuous Move]

        /// <summary>
        /// UI 속도 Level [1 ~ 50]을 Pelco-D 속도 Level [1 ~ 63]으로 환산한다.
        ///
        /// UI 0은 이동 속도로 사용하지 않으며 정지 요청으로 처리한다.
        /// </summary>
        private byte ConvertPanTiltSpeedLevel(
            int uiSpeed)
        {
            if (uiSpeed <= 0)
            {
                return 0;
            }

            const int uiMaximum = 50;
            const int protocolMinimum = 1;
            const int protocolMaximum = 63;

            int normalizedUiSpeed =
                Math.Min(
                    uiMaximum,
                    uiSpeed);

            double normalized =
                (normalizedUiSpeed - 1.0) /
                (uiMaximum - 1.0);

            int converted =
                protocolMinimum +
                (int)Math.Round(
                    normalized *
                    (protocolMaximum -
                     protocolMinimum));

            return (byte)Math.Max(
                protocolMinimum,
                Math.Min(
                    protocolMaximum,
                    converted));
        }

        /// <summary>
        /// Pan / Tilt 이동에 사용할 Pelco-D 속도를 조회한다.
        ///
        /// UI 속도가 0이면 이동 패킷을 보내지 않고 STOP을 송신한 뒤
        /// 남아 있는 연속 이동 상태를 초기화한다.
        /// </summary>
        private bool TryGetPanTiltProtocolSpeed(
            out byte protocolSpeed)
        {
            protocolSpeed =
                ConvertPanTiltSpeedLevel(
                    PanTiltSpeedLevel);

            if (protocolSpeed > 0)
            {
                ClearActivePanTiltAbsoluteMove();

                return true;
            }

            bool stopResult =
                _controlCommandService
                    .StopMove();

            _currentMoveType =
                ContinuousMoveType.None;

            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection.None;

            ConsoleLogHelper.Warning(
                "PAN / TILT",
                "Move blocked / UI_SPEED=0 / " +
                $"STOP_RESULT={stopResult}");

            return false;
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 좌측 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 좌측 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartPanLeftMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType = ContinuousMoveType.PanTilt;
            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection.PanLeft;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] PAN LEFT START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");
            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartPanLeft(
                    protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 우측 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 우측 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartPanRightMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType = ContinuousMoveType.PanTilt;
            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection.PanRight;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] PAN RIGHT START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");
            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartPanRight(
                    protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 위쪽 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 위쪽 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartTiltUpMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType = ContinuousMoveType.PanTilt;
            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection.TiltUp;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] TILT UP START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");
            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartTiltUp(
                    protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 아래쪽 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 아래 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartTiltDownMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType = ContinuousMoveType.PanTilt;
            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection.TiltDown;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] TILT DOWN START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");
            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartTiltDown(
                    protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 좌측 상단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltUpMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType =
                ContinuousMoveType.PanTilt;

            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection
                    .PanLeftTiltUp;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] PAN LEFT + TILT UP START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartPanLeftTiltUp(
                    protocolSpeed,
                    protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 우측 상단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltUpMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType =
                ContinuousMoveType.PanTilt;

            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection
                    .PanRightTiltUp;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] PAN RIGHT + TILT UP START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartPanRightTiltUp(
                    protocolSpeed,
                    protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 좌측 하단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltDownMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType =
                ContinuousMoveType.PanTilt;

            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection
                    .PanLeftTiltDown;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] PAN LEFT + TILT DOWN START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartPanLeftTiltDown(
                    protocolSpeed,
                    protocolSpeed);
        }

        /// <summary>
        /// [EO/IR] 우측 하단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltDownMove()
        {
            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            _currentMoveType =
                ContinuousMoveType.PanTilt;

            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection
                    .PanRightTiltDown;

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] [EO/IR] PAN RIGHT + TILT DOWN START / " +
                $"UI SPEED : {PanTiltSpeedLevel} / " +
                $"PROTOCOL SPEED : {protocolSpeed}");

            ConsoleLogHelper.PrintLine();

            _controlCommandService
                .StartPanRightTiltDown(
                    protocolSpeed,
                    protocolSpeed);
        }

        #endregion

        /// <summary>
        /// Pan / Tilt 연속 이동 중 Slider 값이 바뀌면 현재 방향을 유지한 채
        /// Pelco-D Data1 / Data2 속도값만 갱신하여 즉시 재송신한다.
        /// </summary>
        private void ApplyPanTiltSpeedWhileMoving()
        {
            if (IsControlInputLocked)
            {
                return;
            }

            // 위치 이동 속도(0x49/0x4B)는 이동 전 설정뿐 아니라
            // ABSOLUTE 이동 중 Slider 변경에도 즉시 반영한다.
            if (!ApplyPanTiltPositionSpeedFromUi(
                    out double positionSpeed))
            {
                return;
            }

            // 장비가 새 위치 속도를 현재 이동에 적용하도록
            // 진행 중인 ABSOLUTE 목표(0x45/0x47)를 그대로 재송신한다.
            ReapplyActivePanTiltAbsoluteTarget(
                positionSpeed);

            if (_currentMoveType !=
                    ContinuousMoveType.PanTilt ||
                _activePanTiltMoveDirection ==
                    KeyboardPanTiltDirection.None)
            {
                return;
            }

            if (!TryGetPanTiltProtocolSpeed(
                    out byte protocolSpeed))
            {
                return;
            }

            bool result;

            switch (_activePanTiltMoveDirection)
            {
                case KeyboardPanTiltDirection.PanLeft:
                    result =
                        _controlCommandService
                            .StartPanLeft(
                                protocolSpeed);
                    break;

                case KeyboardPanTiltDirection.PanRight:
                    result =
                        _controlCommandService
                            .StartPanRight(
                                protocolSpeed);
                    break;

                case KeyboardPanTiltDirection.TiltUp:
                    result =
                        _controlCommandService
                            .StartTiltUp(
                                protocolSpeed);
                    break;

                case KeyboardPanTiltDirection.TiltDown:
                    result =
                        _controlCommandService
                            .StartTiltDown(
                                protocolSpeed);
                    break;

                case KeyboardPanTiltDirection.PanLeftTiltUp:
                    result =
                        _controlCommandService
                            .StartPanLeftTiltUp(
                                protocolSpeed,
                                protocolSpeed);
                    break;

                case KeyboardPanTiltDirection.PanRightTiltUp:
                    result =
                        _controlCommandService
                            .StartPanRightTiltUp(
                                protocolSpeed,
                                protocolSpeed);
                    break;

                case KeyboardPanTiltDirection.PanLeftTiltDown:
                    result =
                        _controlCommandService
                            .StartPanLeftTiltDown(
                                protocolSpeed,
                                protocolSpeed);
                    break;

                case KeyboardPanTiltDirection.PanRightTiltDown:
                    result =
                        _controlCommandService
                            .StartPanRightTiltDown(
                                protocolSpeed,
                                protocolSpeed);
                    break;

                default:
                    return;
            }

            ConsoleLogHelper.Command(
                "PAN / TILT SPEED",
                $"Updated while moving / UI_SPEED={PanTiltSpeedLevel} / " +
                $"PROTOCOL_SPEED={protocolSpeed} / " +
                $"DIRECTION={_activePanTiltMoveDirection} / RESULT={result}");
        }

        #region [EO] [Zoom / Focus Continuous Move]

        /// <summary>
        /// [EO] 주간 카메라 [ZOOM] [Tele] 연속 이동 시작
        ///
        /// 옥상 GOP EO 카메라 선택 시:
        /// - XV-Z4850HC CTEC CGI 직접 제어
        ///
        /// 그 외 EO 카메라 선택 시:
        /// - 기존 Control Agent 제어 유지
        /// </summary>
        public async void StartEoZoomInMove()
        {
            /*
             * Zoom 동작 시 카메라가 Focus를 자동 변경할 수 있으므로
             * 다음 Focus 입력은 새 상태값에서 다시 시작하도록 초기화한다.
             */
            _lastEoFocusCommandTime =
                DateTime.MinValue;

            _currentMoveType =
                ContinuousMoveType.EoZoom;

            Console.WriteLine();
            Console.WriteLine(
                "[CONTROL] EO ZOOM TELE START");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(
                    out RtspSourceOption ctecSource))
            {
                _activeEoCtecSource =
                    ctecSource;

                Console.WriteLine(
                    "[CONTROL] EO ZOOM ROUTE : CTEC CGI DIRECT");

                result =
                    await _ctecCameraCommandService
                        .StartZoomTeleAsync(
                            ctecSource.ControlIp,
                            ctecSource.ControlUserName,
                            ctecSource.ControlPassword,
                            ctecSource.UseHttps,
                            GopEoCtecControlSpeed);
            }
            else
            {
                _activeEoCtecSource =
                    null;

                Console.WriteLine(
                    "[CONTROL] EO ZOOM ROUTE : CONTROL AGENT");

                result =
                    _controlCommandService
                        .StartEoZoomTele();
            }

            Console.WriteLine(
                $"[CONTROL] EO ZOOM TELE SEND RESULT : {result}");

            ConsoleLogHelper.PrintLine();

            if (result &&
                _activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(
                    ContinuousMoveType.EoZoom,
                    _activeEoCtecSource);
            }

            if (!result &&
                _currentMoveType ==
                    ContinuousMoveType.EoZoom)
            {
                _currentMoveType =
                    ContinuousMoveType.None;

                _activeEoCtecSource =
                    null;
            }

        }

        /// <summary>
        /// [EO] 주간 카메라 [ZOOM] [Wide] 연속 이동 시작
        ///
        /// 선택된 EO 프리셋에 따라
        /// CTEC CGI 직접 제어 또는 Control Agent 제어로 분기한다.
        /// </summary>
        public async void StartEoZoomOutMove()
        {
            /*
             * Zoom 동작 시 카메라가 Focus를 자동 변경할 수 있으므로
             * 다음 Focus 입력은 새 상태값에서 다시 시작하도록 초기화한다.
             */
            _lastEoFocusCommandTime =
                DateTime.MinValue;

            _currentMoveType =
                ContinuousMoveType.EoZoom;

            Console.WriteLine();
            Console.WriteLine(
                "[CONTROL] EO ZOOM WIDE START");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(
                    out RtspSourceOption ctecSource))
            {
                _activeEoCtecSource =
                    ctecSource;

                Console.WriteLine(
                    "[CONTROL] EO ZOOM ROUTE : CTEC CGI DIRECT");

                result =
                    await _ctecCameraCommandService
                        .StartZoomWideAsync(
                            ctecSource.ControlIp,
                            ctecSource.ControlUserName,
                            ctecSource.ControlPassword,
                            ctecSource.UseHttps,
                            GopEoCtecControlSpeed);
            }
            else
            {
                _activeEoCtecSource =
                    null;

                Console.WriteLine(
                    "[CONTROL] EO ZOOM ROUTE : CONTROL AGENT");

                result =
                    _controlCommandService
                        .StartEoZoomWide();
            }

            Console.WriteLine(
                $"[CONTROL] EO ZOOM WIDE SEND RESULT : {result}");

            ConsoleLogHelper.PrintLine();

            if (result &&
                _activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(
                    ContinuousMoveType.EoZoom,
                    _activeEoCtecSource);
            }

            if (!result &&
                _currentMoveType ==
                    ContinuousMoveType.EoZoom)
            {
                _currentMoveType =
                    ContinuousMoveType.None;

                _activeEoCtecSource =
                    null;
            }

        }

        /// <summary>
        /// [EO] 주간 카메라 Focus Near 연속 이동 시작
        ///
        /// 옥상 GOP EO 선택 시:
        /// Focus Manual -> Focus Near 순서로 CTEC CGI 직접 송신한다.
        ///
        /// 그 외 EO 선택 시:
        /// 기존 Control Agent Focus Near 명령을 유지한다.
        /// </summary>
        public async void StartEoFocusNearMove()
        {
            if (_currentMoveType !=
                ContinuousMoveType.None)
            {
                return;
            }

            int sequence =
                Interlocked.Increment(
                    ref _eoFocusCommandSequence);

            _lastEoFocusCommandName =
                "NEAR";

            _lastEoFocusCommandElapsedMs =
                _focusLogStopwatch.ElapsedMilliseconds;

            _currentMoveType =
                ContinuousMoveType.EoFocus;

            Console.WriteLine();
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] " +
                $"[FOCUS COMMAND #{sequence}] " +
                $"NEAR START / " +
                $"ELAPSED={_lastEoFocusCommandElapsedMs}ms / " +
                $"CURRENT={_currentEoFocus}");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(
                    out RtspSourceOption ctecSource))
            {
                _activeEoCtecSource =
                    ctecSource;

                Console.WriteLine(
                    "[CONTROL] EO FOCUS ROUTE : CTEC CGI DIRECT");

                result =
                    await _ctecCameraCommandService
                        .StartFocusNearAsync(
                            ctecSource.ControlIp,
                            ctecSource.ControlUserName,
                            ctecSource.ControlPassword,
                            ctecSource.UseHttps,
                            GopEoCtecControlSpeed);
            }
            else
            {
                _activeEoCtecSource =
                    null;

                Console.WriteLine(
                    "[CONTROL] EO FOCUS ROUTE : CONTROL AGENT");

                result =
                    _controlCommandService
                        .StartEoFocusNear();
            }

            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] " +
                $"[FOCUS COMMAND #{sequence}] " +
                $"SEND RESULT={result}");

            if (result &&
                _activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(
                    ContinuousMoveType.EoFocus,
                    _activeEoCtecSource);
            }

            if (!result &&
                _currentMoveType ==
                    ContinuousMoveType.EoFocus)
            {
                _currentMoveType =
                    ContinuousMoveType.None;

                _activeEoCtecSource =
                    null;
            }

        }

        /// <summary>
        /// [EO] 주간 카메라 Focus Far 연속 이동 시작
        ///
        /// 옥상 GOP EO 선택 시:
        /// Focus Manual -> Focus Far 순서로 CTEC CGI 직접 송신한다.
        ///
        /// 그 외 EO 선택 시:
        /// 기존 Control Agent Focus Far 명령을 유지한다.
        /// </summary>
        public async void StartEoFocusFarMove()
        {
            if (_currentMoveType !=
                ContinuousMoveType.None)
            {
                return;
            }

            int sequence =
                Interlocked.Increment(
                    ref _eoFocusCommandSequence);

            _lastEoFocusCommandName =
                "FAR";

            _lastEoFocusCommandElapsedMs =
                _focusLogStopwatch.ElapsedMilliseconds;

            _currentMoveType =
                ContinuousMoveType.EoFocus;

            Console.WriteLine();
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] " +
                $"[FOCUS COMMAND #{sequence}] " +
                $"FAR START / " +
                $"ELAPSED={_lastEoFocusCommandElapsedMs}ms / " +
                $"CURRENT={_currentEoFocus}");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(
                    out RtspSourceOption ctecSource))
            {
                _activeEoCtecSource =
                    ctecSource;

                Console.WriteLine(
                    "[CONTROL] EO FOCUS ROUTE : CTEC CGI DIRECT");

                result =
                    await _ctecCameraCommandService
                        .StartFocusFarAsync(
                            ctecSource.ControlIp,
                            ctecSource.ControlUserName,
                            ctecSource.ControlPassword,
                            ctecSource.UseHttps,
                            GopEoCtecControlSpeed);
            }
            else
            {
                _activeEoCtecSource =
                    null;

                Console.WriteLine(
                    "[CONTROL] EO FOCUS ROUTE : CONTROL AGENT");

                result =
                    _controlCommandService
                        .StartEoFocusFar();
            }

            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] " +
                $"[FOCUS COMMAND #{sequence}] " +
                $"SEND RESULT={result}");

            if (result &&
                _activeEoCtecSource != null)
            {
                StartCtecEoPositionPolling(
                    ContinuousMoveType.EoFocus,
                    _activeEoCtecSource);
            }

            if (!result &&
                _currentMoveType ==
                    ContinuousMoveType.EoFocus)
            {
                _currentMoveType =
                    ContinuousMoveType.None;

                _activeEoCtecSource =
                    null;
            }

        }

        /// <summary>
        /// [EO] 주간 카메라 [One Push Focus] 요청
        ///
        /// 옥상 GOP EO 선택 시 CTEC CGI 직접 제어,
        /// 그 외 EO 선택 시 기존 Control Agent 명령을 사용한다.
        /// </summary>
        public async void StartEoAutoFocusMove()
        {
            Console.WriteLine();
            Console.WriteLine(
                "[CONTROL] EO ONE PUSH FOCUS REQUEST");

            ConsoleLogHelper.PrintLine();

            bool result;

            if (TryGetSelectedEoCtecSource(
                    out RtspSourceOption ctecSource))
            {
                result =
                    await _ctecCameraCommandService
                        .OnePushFocusAsync(
                            ctecSource.ControlIp,
                            ctecSource.ControlUserName,
                            ctecSource.ControlPassword,
                            ctecSource.UseHttps);
            }
            else
            {
                result =
                    _controlCommandService
                        .StartEoAutoFocus();
            }

            Console.WriteLine(
                $"[CONTROL] EO ONE PUSH FOCUS RESULT : {result}");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 현재 선택된 EO 프리셋이
        /// CTEC CGI 직접 제어 대상인지 확인한다.
        ///
        /// EO 주소가 프리셋 외부 값으로 변경된 경우에는
        /// 잘못된 카메라로 명령이 송신되지 않도록
        /// 기존 Control Agent 경로를 사용한다.
        /// </summary>
        private bool TryGetSelectedEoCtecSource(
            out RtspSourceOption sourceOption)
        {
            sourceOption =
                SelectedEoRtspSource;

            return sourceOption != null &&
                   sourceOption.ControlType ==
                       CameraControlType.CtecCgi &&
                   !string.IsNullOrWhiteSpace(
                       sourceOption.ControlIp);
        }

        /// <summary>
        /// 현재 선택된 EO 프리셋 기준으로
        /// CTEC Response TCP Port 9000 연결 시작
        ///
        /// 옥상 GOP EO CTEC 직접 제어 프리셋이 아니면
        /// 기존 Response 연결을 종료하고 별도 TCP 연결을 생성하지 않는다.
        /// </summary>
        private async Task StartSelectedEoCtecResponseAsync()
        {
            if (!TryGetSelectedEoCtecSource(
                    out RtspSourceOption sourceOption))
            {
                _connectedEoCtecSource =
                    null;

                SelectedEquipmentStatusMode =
                    EquipmentStatusMode.Environment;

                _ctecCameraResponseService.Stop();

                OnPropertyChanged(
                    nameof(CurrentEoZoomText));

                OnPropertyChanged(
                    nameof(CurrentEoFocusText));

                return;
            }

            _connectedEoCtecSource =
                sourceOption;

            SelectedEquipmentStatusMode =
                EquipmentStatusMode.Rooftop;

            _currentCtecEoZoomPosition =
                0;

            _currentCtecEoFocusPosition =
                0;

            _currentCtecEoFocusMode =
                0;

            OnPropertyChanged(
                nameof(CurrentEoZoomText));

            OnPropertyChanged(
                nameof(EnvironmentEoZoomStatusText));

            OnPropertyChanged(
                nameof(CurrentEoFocusText));

            OnPropertyChanged(
                nameof(EnvironmentEoFocusStatusText));

            Console.WriteLine();
            Console.WriteLine(
                $"[CTEC RESPONSE] START : " +
                $"{sourceOption.ControlIp}:{GopEoCtecResponsePort}");

            ConsoleLogHelper.PrintLine();

            await _ctecCameraResponseService
                .StartAsync(
                    sourceOption.ControlIp,
                    GopEoCtecResponsePort);
        }

        /// <summary>
        /// CTEC Response TCP 연결 상태 변경 처리
        ///
        /// Connected 상태가 되면 현재 Zoom / Focus Position 및
        /// Focus Mode Inquiry를 순차 송신하여 초기 상태값을 조회한다.
        /// </summary>
        private void OnCtecCameraResponseConnectionStatusChanged(
            string status)
        {
            Console.WriteLine(
                $"[CTEC RESPONSE] STATUS : {status}");

            ConsoleLogHelper.PrintLine();

            if (!string.Equals(
                    status,
                    "Connected",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _ = RequestConnectedEoCtecStatusAsync();
        }

        /// <summary>
        /// 현재 연결된 옥상 GOP EO 카메라 상태 조회
        ///
        /// Inquiry 명령은 CGI로 송신하고,
        /// 실제 응답은 TCP Port 9000 수신 서비스에서 처리한다.
        /// </summary>
        private async Task RequestConnectedEoCtecStatusAsync()
        {
            RtspSourceOption sourceOption =
                _connectedEoCtecSource;

            if (sourceOption == null ||
                !_ctecCameraResponseService.IsConnected)
            {
                return;
            }

            await _ctecCameraCommandService
                .RequestZoomPositionAsync(
                    sourceOption.ControlIp,
                    sourceOption.ControlUserName,
                    sourceOption.ControlPassword,
                    sourceOption.UseHttps);

            await Task.Delay(
                100);

            await _ctecCameraCommandService
                .RequestFocusPositionAsync(
                    sourceOption.ControlIp,
                    sourceOption.ControlUserName,
                    sourceOption.ControlPassword,
                    sourceOption.UseHttps);

            await Task.Delay(
                100);

            await _ctecCameraCommandService
                .RequestFocusModeAsync(
                    sourceOption.ControlIp,
                    sourceOption.ControlUserName,
                    sourceOption.ControlPassword,
                    sourceOption.UseHttps);
        }

        /// <summary>
        /// CTEC Camera Response Packet 수신 처리
        ///
        /// 공통 Header:
        /// 0x99 0x55
        ///
        /// Command Code:
        /// 0x47 = Zoom Position
        /// 0x48 = Focus Position
        /// 0x38 = Focus Mode
        /// </summary>
        private void OnCtecCameraResponsePacketReceived(
            byte[] packet)
        {
            if (packet == null ||
                packet.Length < 7 ||
                packet[0] != 0x99 ||
                packet[1] != 0x55 ||
                packet[packet.Length - 1] != 0xFF)
            {
                Console.WriteLine(
                    "[CTEC RESPONSE] Invalid Packet");

                ConsoleLogHelper.PrintLine();

                return;
            }

            switch (packet[2])
            {
                case 0x47:
                    {
                        ushort zoomPosition =
                            (ushort)((packet[4] << 8) |
                                     packet[5]);

                        _currentCtecEoZoomPosition =
                            zoomPosition;

                        OnPropertyChanged(
                            nameof(CurrentEoZoomText));

                        OnPropertyChanged(
                            nameof(RooftopEoZoomStatusText));

                        OnPropertyChanged(
                            nameof(CurrentPresetSnapshotText));

                        OnPropertyChanged(
                            nameof(CurrentLaPresetSnapshotText));

                        Console.WriteLine(
                            $"[CTEC RESPONSE] EO ZOOM POSITION : " +
                            $"{BuildCtecPositionText(zoomPosition, CtecEoZoomPositionMax)} " +
                            $"/ HEX=0x{zoomPosition:X4}");

                        break;
                    }

                case 0x48:
                    {
                        ushort focusPosition =
                            (ushort)((packet[4] << 8) |
                                     packet[5]);

                        _currentCtecEoFocusPosition =
                            focusPosition;

                        OnPropertyChanged(
                            nameof(CurrentEoFocusText));

                        OnPropertyChanged(
                            nameof(RooftopEoFocusStatusText));

                        OnPropertyChanged(
                            nameof(CurrentPresetSnapshotText));

                        OnPropertyChanged(
                            nameof(CurrentLaPresetSnapshotText));

                        Console.WriteLine(
                            $"[CTEC RESPONSE] EO FOCUS POSITION : " +
                            $"{BuildCtecPositionText(focusPosition, CtecEoFocusPositionMax)} " +
                            $"/ HEX=0x{focusPosition:X4}");

                        break;
                    }

                case 0x38:
                    {
                        _currentCtecEoFocusMode =
                            packet[5];

                        string focusModeText =
                            _currentCtecEoFocusMode == 0x02
                                ? "AUTO"
                                : _currentCtecEoFocusMode == 0x03
                                    ? "MANUAL"
                                    : $"UNKNOWN(0x{_currentCtecEoFocusMode:X2})";

                        Console.WriteLine(
                            $"[CTEC RESPONSE] EO FOCUS MODE : " +
                            $"{focusModeText}");

                        break;
                    }

                default:

                    Console.WriteLine(
                        $"[CTEC RESPONSE] UNHANDLED CODE : " +
                        $"0x{packet[2]:X2}");

                    break;
            }
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [IR] [Zoom / Focus Continuous Move]

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Tele] 연속 이동 시작
        /// </summary>
        public void StartIrZoomInMove()
        {
            _currentMoveType = ContinuousMoveType.IrZoom;

            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR ZOOM IN START");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StartIrZoomTele();
        }

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Wide] 연속 이동 시작
        /// </summary>
        public void StartIrZoomOutMove()
        {
            _currentMoveType = ContinuousMoveType.IrZoom;

            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR ZOOM OUT START");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StartIrZoomWide();
        }

        /// <summary>
        /// [IR] [ZOOM] 연속 이동 정지
        ///
        /// IR Zoom 버튼 [MouseUp] 시에만 호출한다.
        /// </summary>
        public void StopIrZoomMove()
        {
            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR ZOOM STOP");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StopIrZoom();

            _currentMoveType = ContinuousMoveType.None;
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Near] 연속 이동 시작
        /// </summary>
        public void StartIrFocusNearMove()
        {
            _currentMoveType = ContinuousMoveType.IrFocus;

            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR FOCUS NEAR START");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StartIrFocusNear();
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Far] 연속 이동 시작
        /// </summary>
        public void StartIrFocusFarMove()
        {
            _currentMoveType = ContinuousMoveType.IrFocus;

            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR FOCUS FAR START");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StartIrFocusFar();
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] 연속 이동 정지
        /// </summary>
        public void StopIrFocusMove()
        {
            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR FOCUS STOP");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StopIrFocus();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Digital Zoom] 확대 시작
        /// </summary>
        public void StartIrDigitalZoomInMove()
        {
            _currentMoveType = ContinuousMoveType.IrDigitalZoom;

            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR DIGITAL ZOOM IN START");
            Console.WriteLine($"[CONTROL] Current Common Zoom : {_currentEoZoom}");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StartIrDigitalZoomIn();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Digital Zoom] 축소 시작
        /// </summary>
        public void StartIrDigitalZoomOutMove()
        {
            _currentMoveType = ContinuousMoveType.IrDigitalZoom;

            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR DIGITAL ZOOM OUT START");
            Console.WriteLine($"[CONTROL] Current Common Zoom : {_currentEoZoom}");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StartIrDigitalZoomOut();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Auto Focus] 요청
        /// </summary>
        public void StartIrAutoFocusMove()
        {
            Console.WriteLine();
            Console.WriteLine("[CONTROL] IR AUTO FOCUS REQUEST");
            Console.WriteLine($"[CONTROL] Current Common Focus : {_currentEoFocus}");
            ConsoleLogHelper.PrintLine();

            _controlCommandService.StartIrAutoFocus();
        }

        #endregion

        #region [CTEC EO Zoom / Focus Real-time Position Polling]

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 시작
        ///
        /// 기존 Polling이 남아 있으면 먼저 종료한 뒤,
        /// 현재 이동 종류에 맞는 Inquiry를 200ms 간격으로 반복한다.
        ///
        /// 주의:
        /// - Zoom / Focus 이동 명령을 반복 송신하지 않는다.
        /// - Position Inquiry만 반복 송신한다.
        /// - 실제 값 갱신은 TCP Port 9000 응답 수신부에서 처리한다.
        /// </summary>
        private void StartCtecEoPositionPolling(
            ContinuousMoveType moveType,
            RtspSourceOption sourceOption)
        {
            StopCtecEoPositionPolling();

            if (sourceOption == null ||
                (moveType != ContinuousMoveType.EoZoom &&
                 moveType != ContinuousMoveType.EoFocus))
            {
                return;
            }

            CancellationTokenSource pollingCts =
                new CancellationTokenSource();

            _ctecEoPositionPollingCts =
                pollingCts;

            long operationGeneration =
                Interlocked.Increment(
                    ref _ctecEoPositionOperationGeneration);

            Console.WriteLine(
                $"[CTEC POLLING] START : {moveType} / " +
                $"INTERVAL={CtecEoPositionPollingIntervalMs}ms");

            ConsoleLogHelper.PrintLine();

            _ = PollCtecEoPositionAsync(
                moveType,
                sourceOption,
                operationGeneration,
                pollingCts.Token);
        }

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 Loop
        /// </summary>
        private async Task PollCtecEoPositionAsync(
            ContinuousMoveType moveType,
            RtspSourceOption sourceOption,
            long operationGeneration,
            CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_currentMoveType != moveType ||
                        !_ctecCameraResponseService.IsConnected ||
                        operationGeneration !=
                            Interlocked.Read(
                                ref _ctecEoPositionOperationGeneration))
                    {
                        break;
                    }

                    int? position =
                        await RequestAndWaitCtecEoPositionAsync(
                            moveType,
                            sourceOption,
                            cancellationToken);

                    if (!position.HasValue)
                    {
                        Console.WriteLine(
                            $"[CTEC POLLING] RESPONSE TIMEOUT : {moveType}");
                    }

                    /*
                     * 다음 Inquiry는 이전 TCP Position 응답을 받은 뒤에만 송신한다.
                     * 따라서 Camera 내부 응답이 늦어져도 미응답 Inquiry가 누적되지 않는다.
                     */
                    await Task.Delay(
                        CtecEoPositionPollingIntervalMs,
                        cancellationToken);
                }

            }
            catch (OperationCanceledException)
            {
                // MouseUp / MouseLeave / Disconnect에 의한 정상 종료
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[CTEC POLLING] ERROR : {moveType} / {ex.Message}");

                ConsoleLogHelper.PrintLine();
            }
            finally
            {
                Console.WriteLine(
                    $"[CTEC POLLING] END : {moveType}");

                ConsoleLogHelper.PrintLine();
            }

        }

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다.
        ///
        /// HTTP 200 OK만으로 조회 완료 처리하지 않는다.
        /// Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다.
        /// </summary>
        private async Task<int?> RequestAndWaitCtecEoPositionAsync(
            ContinuousMoveType moveType,
            RtspSourceOption sourceOption,
            CancellationToken cancellationToken)
        {
            await _ctecEoPositionQueryLock.WaitAsync(
                cancellationToken);

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
                    responseTask =
                        _ctecCameraResponseService
                            .WaitForNextZoomPositionAsync(
                                CtecEoPositionResponseTimeoutMs,
                                CancellationToken.None);

                    inquiryResult =
                        await _ctecCameraCommandService
                            .RequestZoomPositionAsync(
                                sourceOption.ControlIp,
                                sourceOption.ControlUserName,
                                sourceOption.ControlPassword,
                                sourceOption.UseHttps);
                }
                else if (moveType == ContinuousMoveType.EoFocus)
                {
                    responseTask =
                        _ctecCameraResponseService
                            .WaitForNextFocusPositionAsync(
                                CtecEoPositionResponseTimeoutMs,
                                CancellationToken.None);

                    inquiryResult =
                        await _ctecCameraCommandService
                            .RequestFocusPositionAsync(
                                sourceOption.ControlIp,
                                sourceOption.ControlUserName,
                                sourceOption.ControlPassword,
                                sourceOption.UseHttps);
                }
                else
                {
                    return null;
                }

                if (!inquiryResult)
                {
                    Console.WriteLine(
                        $"[CTEC INQUIRY] SEND FAILED : {moveType}");

                    return null;
                }

                return await responseTask;
            }
            finally
            {
                _ctecEoPositionQueryLock.Release();
            }

        }

        /// <summary>
        /// [CTEC Stop] Stop 송신 이후 실제 Position 값이 안정될 때까지 확인한다.
        ///
        /// 단일 조회값을 최종 위치로 확정하지 않는다.
        /// 
        /// 카메라 상태 갱신이 늦으면 이전 이동 방향의 값이 뒤늦게 반환될 수 있으므로,
        /// 연속 두 값이 허용 오차 안에 들어올 때 최종 위치로 판단한다.
        /// </summary>
        private async Task WaitForCtecEoPositionStableAsync(
            ContinuousMoveType moveType,
            RtspSourceOption sourceOption,
            long operationGeneration)
        {
            int? previousPosition = null;

            for (int count = 0;
                 count < CtecEoPositionSettleMaximumCount;
                 count++)
            {
                if (operationGeneration !=
                    Interlocked.Read(
                        ref _ctecEoPositionOperationGeneration) ||
                    !_ctecCameraResponseService.IsConnected)
                {
                    return;
                }

                int? currentPosition =
                    await RequestAndWaitCtecEoPositionAsync(
                        moveType,
                        sourceOption,
                        CancellationToken.None);

                if (currentPosition.HasValue &&
                    previousPosition.HasValue &&
                    Math.Abs(
                        currentPosition.Value -
                        previousPosition.Value) <=
                    CtecEoPositionStableTolerance)
                {
                    Console.WriteLine(
                        $"[CTEC POSITION] STABLE : {moveType} / " +
                        $"POSITION={currentPosition.Value} / " +
                        $"COUNT={count + 1}");

                    ConsoleLogHelper.PrintLine();

                    return;
                }

                previousPosition =
                    currentPosition;

                await Task.Delay(
                    CtecEoPositionPollingIntervalMs);
            }

            Console.WriteLine(
                $"[CTEC POSITION] SETTLE LIMIT : {moveType} / " +
                $"LAST={previousPosition?.ToString() ?? "NONE"}");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 종료
        /// </summary>
        private void StopCtecEoPositionPolling()
        {
            CancellationTokenSource pollingCts =
                Interlocked.Exchange(
                    ref _ctecEoPositionPollingCts,
                    null);

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

        #endregion

        #region [Common Stop Continuous Move]

        /// <summary>
        /// 연속 이동 정지
        ///
        /// 버튼 [MouseUp] 또는 [MouseLeave] 시 호출된다.
        ///
        /// 옥상 GOP EO Zoom / Focus는 CTEC CGI 전용 Stop 명령을 송신하고,
        /// 그 외 제어는 기존 Control Agent Stop 명령을 유지한다.
        /// </summary>
        public async void StopContinuousMove()
        {
            // HOME / ZERO 또는 AUTO SCAN 명령 이후 MouseUp/MouseLeave/Focus 이탈로
            // 공통 Stop이 뒤늦게 들어오면 진행 중인 동작이 중단될 수 있다.
            // 제어 Lock 중에는 외부 Stop 요청을 무시한다.
            if (IsControlInputLocked)
            {
                return;
            }

            /*
             * 이동 제어 VIA 0 Pan 작업이 실행 중이면
             * Stop 버튼에서도 동일하게 취소되도록 먼저 종료한다.
             */
            CancelMoveControlPanOperation();

            ContinuousMoveType moveType =
                _currentMoveType;

            if (moveType ==
                ContinuousMoveType.None)
            {
                return;
            }

            /*
             * Zoom / Focus 위치 조회 Loop를 먼저 종료한다.
             * 이후 Stop 명령과 최종 Inquiry가 Polling 요청 사이에 섞이지 않도록 한다.
             */
            StopCtecEoPositionPolling();

            long stopOperationGeneration =
                Interlocked.Increment(
                    ref _ctecEoPositionOperationGeneration);

            /*
             * Stop 중복 호출을 방지하기 위해
             * 실제 비동기 송신 전에 이동 상태를 먼저 초기화한다.
             */
            _currentMoveType =
                ContinuousMoveType.None;

            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection.None;

            RtspSourceOption activeEoCtecSource =
                _activeEoCtecSource;

            _activeEoCtecSource =
                null;

            Console.WriteLine();

            Console.WriteLine(
                $"[CONTROL] MOVE STOP: {moveType}");

            ConsoleLogHelper.PrintLine();

            switch (moveType)
            {
                case ContinuousMoveType.PanTilt:

                    _controlCommandService
                        .StopMove();

                    break;

                case ContinuousMoveType.EoZoom:
                    {
                        if (activeEoCtecSource !=
                            null)
                        {
                            bool stopResult =
                                await _ctecCameraCommandService
                                    .StopZoomAsync(
                                        activeEoCtecSource.ControlIp,
                                        activeEoCtecSource.ControlUserName,
                                        activeEoCtecSource.ControlPassword,
                                        activeEoCtecSource.UseHttps);

                            Console.WriteLine(
                                $"[CONTROL] EO CTEC ZOOM STOP RESULT : {stopResult}");

                            if (stopResult &&
                                _ctecCameraResponseService.IsConnected)
                            {
                                await WaitForCtecEoPositionStableAsync(
                                    ContinuousMoveType.EoZoom,
                                    activeEoCtecSource,
                                    stopOperationGeneration);
                            }

                        }
                        else
                        {
                            _controlCommandService
                                .StopMove();
                        }

                        break;
                    }

                case ContinuousMoveType.EoFocus:
                    {
                        long elapsedMs =
                            _focusLogStopwatch.ElapsedMilliseconds;

                        long commandDurationMs =
                            elapsedMs -
                            _lastEoFocusCommandElapsedMs;

                        Console.WriteLine();
                        Console.WriteLine(
                            $"[{DateTime.Now:HH:mm:ss.fff}] " +
                            $"[FOCUS COMMAND] STOP / " +
                            $"DIRECTION={_lastEoFocusCommandName} / " +
                            $"HELD={commandDurationMs}ms / " +
                            $"CURRENT={_currentEoFocus}");

                        ConsoleLogHelper.PrintLine();

                        bool stopResult;

                        if (activeEoCtecSource !=
                            null)
                        {
                            stopResult =
                                await _ctecCameraCommandService
                                    .StopFocusAsync(
                                        activeEoCtecSource.ControlIp,
                                        activeEoCtecSource.ControlUserName,
                                        activeEoCtecSource.ControlPassword,
                                        activeEoCtecSource.UseHttps);
                        }
                        else
                        {
                            stopResult =
                                _controlCommandService
                                    .StopMove();
                        }

                        Console.WriteLine(
                            $"[{DateTime.Now:HH:mm:ss.fff}] " +
                            $"[FOCUS COMMAND] STOP RESULT={stopResult}");

                        if (stopResult &&
                            activeEoCtecSource != null &&
                            _ctecCameraResponseService.IsConnected)
                        {
                            await WaitForCtecEoPositionStableAsync(
                                ContinuousMoveType.EoFocus,
                                activeEoCtecSource,
                                stopOperationGeneration);
                        }

                        break;
                    }

                case ContinuousMoveType.IrZoom:

                    _controlCommandService
                        .StopIrZoom();

                    break;

                case ContinuousMoveType.IrFocus:

                    _controlCommandService
                        .StopIrFocus();

                    break;

                case ContinuousMoveType.IrDigitalZoom:

                    _controlCommandService
                        .StopIrDigitalZoom();

                    break;
            }

        }

        #endregion

        #endregion
    }

}
