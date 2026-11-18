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

        void ApplyEnvironmentIrCommandedPosition(int? zoomPosition, int? focusPosition, string reason);

        bool ApplyPanTiltPositionSpeedFromUi(out double positionSpeed);

        bool AutoFocusTargetUsesAgent(int target);

        void CancelMoveControlPanOperation();

        void CancelPanoramaPresets();

        void ClearActivePanTiltAbsoluteMove();

        Services.Configuration.LensControlTuning ControlTuningFor(bool ir);

        string CurrentEoFocusText { get; }

        string CurrentEoZoomText { get; }

        string CurrentLaPresetSnapshotText { get; }

        string CurrentPresetSnapshotText { get; }

        byte EffectivePanTiltSpeedLevel { get; }

        string EnvironmentEoFocusStatusText { get; }

        string EnvironmentEoZoomStatusText { get; }

        void ExecuteAutoFocus(object parameter);

        int GetCurrentIrFocusStandardPosition();

        int GetCurrentIrZoomStandardPosition();

        ushort GetCurrentPresetStandardFocus();

        ushort GetCurrentPresetStandardZoom();

        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsControlInputLocked { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        bool IsFovSpeedEnabled { get; set; }

        bool IsWebAgentFocusControlReady { get; }

        void MarkLensEvidence(bool ir);

        void MarkPanTiltMoveCommandIssued();

        void MarkPresetLensEvidence(bool ir, bool zoom, bool focus);

        void NotifyEoCurrentStatusChanged(bool panChanged = true, bool tiltChanged = true, bool zoomChanged = true, bool focusChanged = true, bool powerChanged = true);

        void NotifyIrCurrentStatusChanged();

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanTiltSpeedDisplayText { get; }

        byte PanTiltSpeedLevel { get; set; }

        void ReapplyActivePanTiltAbsoluteTarget(double positionSpeed);

        string RooftopEoFocusStatusText { get; }

        string RooftopEoZoomStatusText { get; }

        RtspSourceOption SelectedEoRtspSource { get; set; }

        EquipmentStatusMode SelectedEquipmentStatusMode { get; set; }

        void SetDigitalEstimateDirection(int direction, bool ir = false);

        void StopPrecisionMotion(string reason, bool force = false);

        string ZoneKey(string camera);
    }

    /// <summary>수동 연속 제어 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class ContinuousControlFeature
    {

        #region [협력 객체와 초기화]

        private readonly IContinuousControlFeatureContext _context;

        internal ContinuousControlFeature(IContinuousControlFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal int _activePanTiltZoomSource;

        #endregion

        #region [동작 처리]

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

        internal void ResetAllKeyboardControlState()
        {
            ResetKeyboardPanTiltState();
        }

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

        internal bool IsPanTiltKeyboardKey(Key key)
        {
            return OpenCvWpfTracking.Common.ControlShortcutKeys.IsPanTiltKey(key);
        }

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

        internal void ClearKeyboardPanTiltPressedState()
        {
            _context._isKeyboardPanLeftPressed = false;
            _context._isKeyboardPanRightPressed = false;
            _context._isKeyboardTiltUpPressed = false;
            _context._isKeyboardTiltDownPressed = false;
        }

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

        internal byte ConvertPanTiltSpeedLevel(int uiSpeed)
        {
            if (uiSpeed <= 0)
            {
                return 0;
            }

            return (byte)Math.Min(60, uiSpeed);
        }

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

        internal async void StartEoZoomInMove()
        {
            await StartEoZoomInMoveAsync();
        }

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

        internal async void StartEoZoomOutMove()
        {
            await StartEoZoomOutMoveAsync();
        }

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

        internal async void StartEoFocusNearMove()
        {
            await StartEoFocusNearMoveAsync();
        }

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

        internal async void StartEoFocusFarMove()
        {
            await StartEoFocusFarMoveAsync();
        }

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

        internal bool TryGetSelectedEoCtecSource(out RtspSourceOption sourceOption)
        {
            sourceOption = _context.SelectedEoRtspSource;

            return sourceOption != null && sourceOption.ControlType == CameraControlType.CtecCgi && !string.IsNullOrWhiteSpace(sourceOption.ControlIp);
        }

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

        internal void StopIrFocusMove()
        {
            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR FOCUS STOP");

            ConsoleLogHelper.PrintLine();

            bool result = _context._controlCommandService.StopIrFocus();

            CompleteEnvironmentIrManualEstimate(ContinuousMoveType.IrFocus, result);

            _context._currentMoveType = ContinuousMoveType.None;
        }

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

        internal void StartIrDigitalZoomInMove()
        {
            _context._currentMoveType = ContinuousMoveType.IrDigitalZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR DIGITAL ZOOM IN START");

            Console.WriteLine($"[CONTROL] Current Common Zoom : {_context._currentEoZoom}");

            ConsoleLogHelper.PrintLine();

            _context._controlCommandService.StartIrDigitalZoomIn();
        }

        internal void StartIrDigitalZoomOutMove()
        {
            _context._currentMoveType = ContinuousMoveType.IrDigitalZoom;

            Console.WriteLine();

            Console.WriteLine("[CONTROL] IR DIGITAL ZOOM OUT START");

            Console.WriteLine($"[CONTROL] Current Common Zoom : {_context._currentEoZoom}");

            ConsoleLogHelper.PrintLine();

            _context._controlCommandService.StartIrDigitalZoomOut();
        }

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

        internal async void StopContinuousMove()
        {
            await StopContinuousMoveAsync();
        }

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
