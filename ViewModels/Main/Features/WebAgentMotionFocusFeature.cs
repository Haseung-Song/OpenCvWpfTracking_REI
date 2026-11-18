using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Communication;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>정밀 이동과 자동 초점에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IWebAgentMotionFocusFeatureContext
    {
        IControlCommandServicePort _controlCommandService { get; }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> _controlInputs { get; }

        ContinuousMoveType _currentMoveType { get; set; }

        CancellationTokenSource _laPresetSingleMoveCts { get; set; }

        ITcpClientServicePort _laTcpService { get; }

        SemaphoreSlim _lensSyncOperationLock { get; }

        CancellationTokenSource _moveControlPanCts { get; set; }

        CancellationTokenSource _presetSingleMoveCts { get; set; }

        CancellationTokenSource _rooftopFocusSyncCts { get; set; }

        CancellationTokenSource _rooftopZoomSyncCts { get; set; }

        ref global::System.Tuple<double, double, long> _videoCenterPose { get; }

        bool ApplySelectedPanTurnMode();

        bool CameraFeatureTransportReady { get; }

        string ControlLockMessage { get; }

        string ControlLockTitle { get; }

        double CurrentMoveControlPanMaximum { get; }

        double CurrentMoveControlPanMinimum { get; }

        double CurrentMoveControlTiltMaximum { get; }

        double CurrentMoveControlTiltMinimum { get; }

        bool IsControlInputLocked { get; }

        bool IsEnvironmentStatusSelected { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsOperationLockOverlayVisible { get; }

        bool IsPanTiltSpeedControlEnabled { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        double? PanAbsoluteValue { get; set; }

        double? TiltAbsoluteValue { get; set; }

        bool TryGetSelectedEoCtecSource(out RtspSourceOption sourceOption);

        Action<object> ExecuteAutoFocusCallback { get; }

        Predicate<object> CanExecuteAutoFocusCallback { get; }
    }

    /// <summary>정밀 이동과 자동 초점 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class WebAgentMotionFocusFeature
    {

        #region [협력 객체와 초기화]

        private readonly IWebAgentMotionFocusFeatureContext _context;

        internal WebAgentMotionFocusFeature(IWebAgentMotionFocusFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _isWebAgentFocusControlEnabled = true;
            _precisionStatus = "시험 기능 꺼짐 / 원본 제어 유지";
            PrecisionPanSpeed = "0.010";
            PrecisionTiltSpeed = "0.010";
        }

        #endregion

        #region [상태 필드]

        internal bool _isWebAgentFocusControlEnabled;

        internal bool _autoFocusActivatedForSession;

        internal int _webAgentControlSessionRevision;

        internal DispatcherTimer _webAgentControlSafetyTimer;

        internal bool _precisionActive;

        internal bool _precisionStopUncertain;

        internal bool _precisionJog;

        internal bool _precisionPreparing;

        internal byte _precisionDirection;

        internal bool _precisionTilt;

        internal double _precisionTarget;

        internal double _precisionDeadline;

        internal double _precisionSentAt;

        internal long _precisionPoseTicks;

        internal int _precisionArrivalSamples;

        internal int _precisionOperation;

        internal string _precisionStatus;

        internal readonly AutoFocusSnapshot[] _autoFocus = new AutoFocusSnapshot[2];

        internal readonly int[] _autoFocusPending =
        {
            -1,
            -1
        };

        internal readonly double[] _autoFocusDeadline = new double[2];

        internal readonly bool[] _autoFocusUncertain = new bool[2];

        internal readonly bool[] _autoFocusStateUnknown = new bool[2];

        internal readonly bool[] _autoFocusDefaultOffRequired =
        {
            true,
            true
        };

        internal readonly bool[] _autoFocusDefaultOffSent = new bool[2];

        internal readonly string[] _autoFocusText =
        {
            "미조회",
            "미조회"
        };

        internal ICommand _precisionAbsoluteCommand;

        internal ICommand _precisionStopCommand;

        internal ICommand _autoFocusCommand;

        #endregion

        #region [화면 속성과 이벤트]

        internal string PrecisionPanSpeed { get; set; }

        internal string PrecisionTiltSpeed { get; set; }

        internal bool IsAutoFocusPanelVisible
        {
            get { return _context.IsSelectedControlAgentWebAgent; }
        }

        internal bool IsWebAgentFocusControlEnabled
        {
            get
            {
                return IsAutoFocusPanelVisible && _isWebAgentFocusControlEnabled;
            }

            set
            {
                if (_isWebAgentFocusControlEnabled == value)
                {
                    return;
                }

                if (value && !IsAutoFocusPanelVisible)
                {
                    SetPrecisionStatus("WEB AGENT 연결 필요");

                    _context.OnPropertyChanged();

                    return;
                }

                if (!value)
                {
                    StopPrecisionMotion("시험 기능 해제");
                }

                _isWebAgentFocusControlEnabled = value;

                EnsureWebAgentControlSafetyTimer();

                _context.OnPropertyChanged();

                RefreshWebAgentFocusControls();

                if (value)
                {
                    ActivateAutoFocusSession(true);
                }
            }
        }

        internal string PrecisionStatusText
        {
            get { return _precisionStatus; }
        }

        internal string EoAutoFocusStatusText
        {
            get
            {
                return "EO AF : " + _autoFocusText[0];
            }
        }

        internal string IrAutoFocusStatusText
        {
            get
            {
                return "IR AF : " + _autoFocusText[1];
            }
        }

        internal bool CanUsePrecisionJogButton
        {
            get
            {
                return CanStartPrecisionMotion || (_precisionActive && _precisionJog);
            }
        }

        internal bool CanStartPrecisionMotion
        {
            get { return false; }
        }

        internal bool HasConflictingLensOrMotionOperation
        {
            get
            {
                return _context._laPresetSingleMoveCts != null || _context._presetSingleMoveCts != null || _context._rooftopZoomSyncCts != null || _context._rooftopFocusSyncCts != null || _context._moveControlPanCts != null || _context._lensSyncOperationLock.CurrentCount == 0;
            }
        }

        internal bool IsWebAgentFocusControlReady
        {
            get
            {
                return IsWebAgentFocusControlEnabled && _context.CameraFeatureTransportReady;
            }
        }

        internal ICommand PrecisionAbsoluteCommand
        {
            get
            {
                return _precisionAbsoluteCommand ?? (_precisionAbsoluteCommand = new ParameterRelayCommand(p => StartPrecisionAbsolute(Convert.ToString(p)), p => CanStartPrecisionMotion));
            }
        }

        internal ICommand PrecisionStopCommand
        {
            get
            {
                return _precisionStopCommand ?? (_precisionStopCommand = new RelayCommand(() => StopPrecisionMotion("수동 정지", true)));
            }
        }

        internal ICommand AutoFocusCommand
        {
            get
            {
                return _autoFocusCommand ?? (_autoFocusCommand = new ParameterRelayCommand(_context.ExecuteAutoFocusCallback, _context.CanExecuteAutoFocusCallback));
            }
        }

        #endregion

        #region [동작 처리]

        internal bool IsPrecisionPoseFresh()
        {
            var pose = Volatile.Read(ref _context._videoCenterPose);

            return pose != null && Math.Abs(DateTime.UtcNow.Ticks - pose.Item3) < TimeSpan.FromSeconds(2).Ticks;
        }

        internal bool TryPrecisionSpeed(bool tilt, out double speed)
        {
            string input = tilt ? PrecisionTiltSpeed : PrecisionPanSpeed;

            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out speed))
            {
                SetPrecisionStatus("속도 입력 오류 / 예: 0.001 또는 0.010");

                return false;
            }

            try
            {
                WebAgentMotionFocusProtocol.EncodeSpeed(speed, PrecisionMaximumSpeed);

                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                SetPrecisionStatus("속도 범위: 0.001 ~ 60.000도/초");

                return false;
            }
        }

        internal async void StartPrecisionAbsolute(string axis)
        {
            if (!CanStartPrecisionMotion || (axis != "PAN" && axis != "TILT"))
            {
                return;
            }

            bool tilt = axis == "TILT";

            double? value = tilt ? _context.TiltAbsoluteValue : _context.PanAbsoluteValue;

            double speed;

            if (!value.HasValue || !TryPrecisionSpeed(tilt, out speed))
            {
                return;
            }

            double target = value.Value;

            double minimum = tilt ? _context.CurrentMoveControlTiltMinimum : _context.CurrentMoveControlPanMinimum;

            double maximum = tilt ? _context.CurrentMoveControlTiltMaximum : _context.CurrentMoveControlPanMaximum;

            if (double.IsNaN(target) || double.IsInfinity(target) || target < minimum || target > maximum)
            {
                SetPrecisionStatus("목표 각도가 기존 장비 허용 범위를 벗어났습니다.");

                return;
            }

            // 기존 절대 이동의 재전송 대상에는 등록하지 않아 속도 슬라이더 변경에 섞이지 않게 한다.
            _precisionActive = true;
            _precisionPreparing = true;
            _precisionJog = false;
            _precisionTilt = tilt;

            _precisionTarget = Math.Round(target, 2, MidpointRounding.AwayFromZero);

            int operation = ++_precisionOperation;

            int generation = _webAgentControlSessionRevision;

            RefreshWebAgentFocusControls();

            try
            {
                if (!tilt && !_context.ApplySelectedPanTurnMode())
                {
                    StopPrecisionMotion("회전 경로 설정 송신 실패");

                    return;
                }

                if (!tilt)
                {
                    await Task.Delay(100);
                }

                if (generation != _webAgentControlSessionRevision || operation != _precisionOperation || !IsWebAgentFocusControlReady || !_precisionActive)
                {
                    return;
                }

                _precisionPreparing = false;

                if (!_context._controlCommandService.SendPrecisionAbsolute(tilt, _precisionTarget, speed, PrecisionMaximumSpeed))
                {
                    StopPrecisionMotion("정밀 절대 이동 송신 실패");

                    return;
                }

                _precisionSentAt = TelemetryClock.MonotonicMilliseconds;
                _precisionArrivalSamples = 0;
                _precisionPoseTicks = 0;

                // 저속 이동을 기존 1도/초 기준으로 계산하지 않는다. 시험용 최대 대기는 별도로 30분이다.
                _precisionDeadline = _precisionSentAt + 30 * 60 * 1000;

                SetPrecisionStatus(axis + " 정밀 절대 이동 전송 / 수신 위치 확인 중 / 최대 30분 안전 정지");
            }
            catch (Exception ex)
            {
                StopPrecisionMotion("정밀 이동 예외 / " + ex.Message);
            }
        }

        internal bool StartPrecisionJog(byte direction)
        {
            if (!CanStartPrecisionMotion || direction == 0)
            {
                return false;
            }

            double pan = 0;

            double tilt = 0;

            if (((direction & 6) != 0 && !TryPrecisionSpeed(false, out pan)) || ((direction & 24) != 0 && !TryPrecisionSpeed(true, out tilt)))
            {
                return false;
            }

            if (!IsPrecisionJogWithinLimits(direction))
            {
                SetPrecisionStatus("장비 각도 한계 방향의 조그 입력을 거부했습니다.");

                return false;
            }

            try
            {
                _precisionActive = true;
                _precisionJog = true;
                _precisionDirection = direction;
                _precisionPreparing = false;

                ++_precisionOperation;

                _precisionDeadline = TelemetryClock.MonotonicMilliseconds + 30000;

                RefreshWebAgentFocusControls();

                if (!_context._controlCommandService.SendPrecisionJog(direction, pan, tilt, PrecisionMaximumSpeed))
                {
                    StopPrecisionMotion("정밀 조그 송신 실패");

                    return false;
                }

                SetPrecisionStatus("정밀 조그 전송 / 누르는 동안만 이동 / 30초 안전 정지");

                return true;
            }
            catch (Exception ex)
            {
                StopPrecisionMotion("정밀 조그 예외 / " + ex.Message);

                return false;
            }
        }

        internal bool IsPrecisionJogWithinLimits(byte direction)
        {
            var pose = Volatile.Read(ref _context._videoCenterPose);

            if (pose == null)
            {
                return false;
            }

            double panSpeed;

            double tiltSpeed;

            if (!double.TryParse(PrecisionPanSpeed, NumberStyles.Float, CultureInfo.InvariantCulture, out panSpeed) || !double.TryParse(PrecisionTiltSpeed, NumberStyles.Float, CultureInfo.InvariantCulture, out tiltSpeed))
            {
                return false;
            }

            // 상태 지연과 다음 타이머 주기 동안 이동할 거리를 남기고 한계보다 먼저 정지한다.
            double lead = Math.Max(0, (DateTime.UtcNow.Ticks - pose.Item3) / (double)TimeSpan.TicksPerSecond) + 0.3;

            double panMargin = Math.Abs(panSpeed) * lead + 0.1;

            double tiltMargin = Math.Abs(tiltSpeed) * lead + 0.1;

            return ((direction & 2) == 0 || pose.Item1 > _context.CurrentMoveControlPanMinimum + panMargin) && ((direction & 4) == 0 || pose.Item1 < _context.CurrentMoveControlPanMaximum - panMargin) && ((direction & 8) == 0 || pose.Item2 < _context.CurrentMoveControlTiltMaximum - tiltMargin) && ((direction & 16) == 0 || pose.Item2 > _context.CurrentMoveControlTiltMinimum + tiltMargin);
        }

        internal void StopPrecisionMotion(string reason, bool force = false)
        {
            bool wasActive = _precisionActive;

            bool shouldSend = wasActive || (force && (IsWebAgentFocusControlReady || _precisionStopUncertain));

            ++_precisionOperation;

            _precisionActive = false;
            _precisionPreparing = false;
            _precisionJog = false;

            if (shouldSend && _context._laTcpService.IsConnected && _context.IsEnvironmentStatusSelected)
            {
                // 두 축 검증 후 순차 실행되는 조그의 부분 실패에도 정지 요청을 두 경로로 보낸다.
                bool jogStop = _context._controlCommandService.SendPrecisionJog(0, 0, 0, PrecisionMaximumSpeed);

                bool axisStop = _context._controlCommandService.StopPanTiltPositionMove();

                _precisionStopUncertain = !(jogStop && axisStop);

                SetPrecisionStatus(reason + " / 정지 송신=" + (jogStop && axisStop) + " / 실제 정지는 장비 상태로 확인");
            }
            else if (wasActive)
            {
                _precisionStopUncertain = true;

                SetPrecisionStatus(reason + " / 통신 불가: 장비 정지 여부를 현장에서 확인하세요.");
            }

            RefreshWebAgentFocusControls();
        }

        internal bool CanExecuteAutoFocus(object parameter)
        {
            string text = Convert.ToString(parameter);

            if (string.IsNullOrEmpty(text) || text.Length != 2 || text[0] < '0' || text[0] > '1')
            {
                return false;
            }

            int target = text[0] - '0';

            int action = text[1] - '0';

            if (!IsWebAgentFocusControlReady || !AutoFocusTargetUsesAgent(target) || HasConflictingLensOrMotionOperation || _context._controlInputs.Count != 0 || _context._currentMoveType != ContinuousMoveType.None || _autoFocusUncertain[target] || _autoFocusPending[target] >= 0 || !_context.IsOperationCommandEnabled || _context.IsControlInputLocked)
            {
                return false;
            }

            if (action == 0)
            {
                return true;
            }

            if (_autoFocusStateUnknown[target])
            {
                return false;
            }

            var state = _autoFocus[target];

            return state != null && state.Result == 0 && (action == 1 || action == 2 ? state.ContinuousSupported : action == 3 && state.OnePushSupported && !state.Running);
        }

        internal void ExecuteAutoFocus(object parameter)
        {
            if (!CanExecuteAutoFocus(parameter))
            {
                return;
            }

            string text = Convert.ToString(parameter);

            int action = text[1] - '0';

            SendAutoFocus(text[0] - '0', action == 0 ? 0 : action == 3 ? 2 : 1, action == 1 ? 1 : 0);
        }

        internal void SendAutoFocus(int target, int operation, int value)
        {
            if (!IsWebAgentFocusControlReady || target < 0 || target > 1 || !AutoFocusTargetUsesAgent(target) || _autoFocusPending[target] >= 0 || _autoFocusUncertain[target])
            {
                return;
            }

            _autoFocusPending[target] = operation;

            _autoFocusDeadline[target] = TelemetryClock.MonotonicMilliseconds + 5000;

            byte command = operation == 0 ? (byte)0xCB : operation == 1 ? (byte)0xC7 : (byte)0xC9;

            bool sent = _context._controlCommandService.SendAutoFocusCommand(command, (byte)target, (byte)value);

            if (!sent)
            {
                _autoFocusPending[target] = -1;
            }

            _autoFocusText[target] = sent ? "응답 대기" : "송신 실패";

            RefreshWebAgentFocusControls();
        }

        internal void ParseAutoFocusResponse(byte[] payload)
        {
            AutoFocusSnapshot state;

            if (!WebAgentMotionFocusProtocol.TryReadAutoFocus(payload, out state))
            {
                ConsoleLogHelper.Warning("WEB AGENT AF", "형식·버전·필드 범위를 벗어난 응답 거부");

                return;
            }

            int generation = _webAgentControlSessionRevision;

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _webAgentControlSessionRevision || !_autoFocusActivatedForSession || !IsAutoFocusPanelVisible || !_context.CameraFeatureTransportReady || !AutoFocusTargetUsesAgent(state.Target))
                {
                    return;
                }

                int target = state.Target;

                bool expected = _autoFocusPending[target] == state.Operation;

                bool notification = state.Operation == 3;

                if (_autoFocusUncertain[target] || (!expected && !notification))
                {
                    ConsoleLogHelper.Warning("WEB AGENT AF", "늦거나 대기 작업과 일치하지 않는 응답 / 화면 상태에 미적용");

                    return;
                }

                // 변경 알림은 조회·설정 응답 대기를 해제하지 않는다.
                if (expected)
                {
                    _autoFocusPending[target] = -1;
                }

                if (state.Result == 0)
                {
                    _autoFocus[target] = state;

                    _autoFocusStateUnknown[target] = false;
                }
                else
                {
                    // 오류 응답의 빈 플래그로 이전 AF ON·검색 상태를 OFF로 덮어쓰지 않는다.
                    _autoFocusStateUnknown[target] = !(state.Result == 2 && state.Operation == 0 && _autoFocus[target] == null);
                }

                // 접수·검색·전달 상태는 완료로 표시하지 않는다.
                string[] names =
                {
                    "대기",
                    "검색 중",
                    "처리 중",
                    "완료 미확인",
                    "실행 오류",
                    "미지원"
                };

                string mode = state.ContinuousSupported ? ((state.Flags & 4) != 0 ? "ON" : "OFF") : "단발";

                _autoFocusText[target] = state.Result == 0 ? mode + " · " + (state.ConfirmedComplete ? "완료 확인" : names[state.State]) : state.Result == 2 ? "미지원" : state.Result == 1 ? "입력 오류" : "오류 · 상태 재조회";

                ConsoleLogHelper.State("WEB AGENT AF", "대상=" + target + " / 작업=" + state.Operation + " / 결과=" + state.Result + " / 플래그=" + state.Flags.ToString("X2") + " / 실행주체=" + state.Owner + " / 경로=" + state.Transport + " / 상태=" + state.State);

                ApplyAutoFocusDefaultOff(state);

                RefreshWebAgentFocusControls();
            }));
        }

        internal void ApplyAutoFocusDefaultOff(AutoFocusSnapshot state)
        {
            int target = state.Target;

            if (!_autoFocusDefaultOffRequired[target] || _autoFocusPending[target] >= 0)
            {
                // 변경 알림만으로 아직 대기 중인 최초 조회·설정 응답을 대신하지 않는다.
                return;
            }

            if (state.Result != 0)
            {
                // 최초 조회에서 미지원이 명시된 채널에는 설정 패킷을 보내지 않는다.
                if (state.Operation == 0 && state.Result == 2 && _autoFocus[target] == null)
                {
                    _autoFocusDefaultOffRequired[target] = false;
                }

                return;
            }

            if (!state.ContinuousSupported)
            {
                _autoFocusDefaultOffRequired[target] = false;

                return;
            }

            if ((state.Flags & 4) == 0 && !state.Running)
            {
                _autoFocusDefaultOffRequired[target] = false;

                ConsoleLogHelper.State("WEB AGENT AF", $"기본 OFF 확인 / 대상={target} / 프리셋 초점 복원 허용");

                return;
            }

            // 자동 설정은 세션당 한 번만 요청한다. 미응답·오류를 반복 송신으로 덮지 않는다.
            if (_autoFocusPending[target] >= 0 || _autoFocusDefaultOffSent[target])
            {
                return;
            }

            _autoFocusDefaultOffSent[target] = true;

            SendAutoFocus(target, 1, 0);

            ConsoleLogHelper.State("WEB AGENT AF", $"기본 OFF 설정 요청 / 대상={target} / 응답 확인 전 초점 복원 대기");
        }

        internal bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true)
        {
            if (!IsAutoFocusPanelVisible || !_context.CameraFeatureTransportReady || !AutoFocusTargetUsesAgent(ir ? 1 : 0))
            {
                return false;
            }

            int target = ir ? 1 : 0;

            var state = _autoFocus[target];

            bool blocked = (_autoFocusActivatedForSession && _autoFocusDefaultOffRequired[target]) || _autoFocusUncertain[target] || _autoFocusStateUnknown[target] || _autoFocusPending[target] >= 0 || (state != null && (state.Running || (state.Result == 0 && (state.Flags & 4) != 0)));

            if (blocked && logWarning)
            {
                ConsoleLogHelper.Warning("WEB AGENT AF", (ir ? "IR" : "EO") + " 자동 초점 동작·응답 대기 중 수동 초점 충돌 방지 / AF OFF 및 상태 확인 필요");
            }

            return blocked;
        }

        internal bool AutoFocusTargetUsesAgent(int target)
        {
            if (!IsAutoFocusPanelVisible)
            {
                return false;
            }

            if (target == 1)
            {
                return true;
            }

            RtspSourceOption source;

            return !_context.TryGetSelectedEoCtecSource(out source);
        }

        internal void ActivateAutoFocusSession(bool refresh = false)
        {
            if (!IsWebAgentFocusControlReady || (!refresh && _autoFocusActivatedForSession) || !_context.IsOperationCommandEnabled || _context.IsControlInputLocked)
            {
                return;
            }

            _autoFocusActivatedForSession = true;

            if (!AutoFocusTargetUsesAgent(0))
            {
                _autoFocusText[0] = "직접 제어 · 기존 AF 사용";
            }

            SendAutoFocus(0, 0, 0);

            SendAutoFocus(1, 0, 0);

            ConsoleLogHelper.State("WEB AGENT AF", "지원 장비 AF 제어 사용 / 상태 조회 후 연속 AF 기본 OFF 확인");
        }

        internal void EnsureWebAgentControlSafetyTimer()
        {
            if (_webAgentControlSafetyTimer == null)
            {
                _webAgentControlSafetyTimer = new DispatcherTimer(DispatcherPriority.Normal)
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };

                _webAgentControlSafetyTimer.Tick += (sender, args) => CheckWebAgentControlSafety();
            }

            _webAgentControlSafetyTimer.Start();
        }

        internal void CheckWebAgentControlSafety()
        {
            if (!IsAutoFocusPanelVisible || !_context.CameraFeatureTransportReady)
            {
                if (_autoFocusActivatedForSession)
                {
                    ResetWebAgentControlSession();
                }

                return;
            }

            if (_isWebAgentFocusControlEnabled && !_autoFocusActivatedForSession)
            {
                ActivateAutoFocusSession();
            }

            double now = TelemetryClock.MonotonicMilliseconds;

            for (int i = 0; i < 2; i++)
            {
                if (_autoFocusPending[i] >= 0 && now >= _autoFocusDeadline[i])
                {
                    _autoFocusPending[i] = -1;

                    _autoFocusUncertain[i] = true;

                    _autoFocusText[i] = "미응답 · 재연결 필요";

                    ConsoleLogHelper.Warning("WEB AGENT AF", "응답 시간 초과 / 대상=" + i + " / 요청 번호가 없어 재송신 금지");
                }
            }

            if (_precisionActive && !_precisionPreparing)
            {
                if (!IsPrecisionPoseFresh())
                {
                    StopPrecisionMotion("위치 상태 갱신 중단");
                }
                else if (now >= _precisionDeadline)
                {
                    StopPrecisionMotion("시험 안전 시간 초과 (도착 완료 아님)");
                }
                else if (_precisionJog && !IsPrecisionJogWithinLimits(_precisionDirection))
                {
                    StopPrecisionMotion("장비 각도 한계 접근");
                }
                else if (!_precisionJog && now - _precisionSentAt > 500)
                {
                    var pose = Volatile.Read(ref _context._videoCenterPose);

                    double actual = _precisionTilt ? pose.Item2 : pose.Item1;

                    double error = Math.Abs(actual - _precisionTarget);

                    if (!_precisionTilt)
                    {
                        error = Math.Min(error, 360 - error);
                    }

                    if (pose.Item3 > _precisionPoseTicks)
                    {
                        _precisionPoseTicks = pose.Item3;

                        _precisionArrivalSamples = error <= 0.03 ? _precisionArrivalSamples + 1 : 0;

                        if (_precisionArrivalSamples >= 3)
                        {
                            StopPrecisionMotion("수신 위치가 목표 허용 오차에 도달");
                        }
                    }
                }
            }

            RefreshWebAgentFocusControls();

            if (!_isWebAgentFocusControlEnabled && !_precisionActive && _autoFocusPending[0] < 0 && _autoFocusPending[1] < 0)
            {
                _webAgentControlSafetyTimer.Stop();
            }
        }

        internal void ResetWebAgentControlSession()
        {
            int generation = Interlocked.Increment(ref _webAgentControlSessionRevision);

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _webAgentControlSessionRevision)
                {
                    return;
                }

                // 새 연결에 이전 장비용 정지 명령을 뒤늦게 전송하지 않는다.
                bool active = _precisionActive || _precisionStopUncertain;

                _precisionActive = false;
                _precisionStopUncertain = false;
                _precisionPreparing = false;
                _precisionJog = false;

                ++_precisionOperation;

                _isWebAgentFocusControlEnabled = true;
                _autoFocusActivatedForSession = false;

                for (int i = 0; i < 2; i++)
                {
                    _autoFocus[i] = null;

                    _autoFocusPending[i] = -1;

                    _autoFocusUncertain[i] = false;

                    _autoFocusStateUnknown[i] = false;

                    _autoFocusDefaultOffRequired[i] = true;

                    _autoFocusDefaultOffSent[i] = false;

                    _autoFocusText[i] = "조회 전";
                }

                _webAgentControlSafetyTimer?.Stop();

                if (IsAutoFocusPanelVisible)
                {
                    EnsureWebAgentControlSafetyTimer();
                }

                SetPrecisionStatus(active ? "연결 변경 / 이전 장비 정지 여부를 현장에서 확인하세요." : "WEB AGENT 연속 AF 기본 OFF");

                _context.OnPropertyChanged(nameof(IsWebAgentFocusControlEnabled));

                RefreshWebAgentFocusControls();
            }));
        }

        internal void HandlePrecisionUnavailable(byte[] payload)
        {
            if (payload.Length < 6 || payload[0] != 1 || (payload[3] != 0xC1 && payload[3] != 0xC3 && payload[3] != 0xC5))
            {
                return;
            }

            int generation = _webAgentControlSessionRevision;

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _webAgentControlSessionRevision || !IsWebAgentFocusControlReady || !_precisionActive)
                {
                    return;
                }

                StopPrecisionMotion("정밀 이동 거부·실행 오류 / 상태=" + payload[1]);
            }));
        }

        internal void SetPrecisionStatus(string text)
        {
            _precisionStatus = text;

            _context.OnPropertyChanged(nameof(PrecisionStatusText));

            ConsoleLogHelper.State("WEB AGENT 정밀 이동", text);
        }

        internal void RefreshWebAgentFocusControls()
        {
            _context.OnPropertyChanged(nameof(IsAutoFocusPanelVisible));

            _context.OnPropertyChanged(nameof(IsWebAgentFocusControlEnabled));

            _context.OnPropertyChanged(nameof(CanStartPrecisionMotion));

            _context.OnPropertyChanged(nameof(CanUsePrecisionJogButton));

            _context.OnPropertyChanged(nameof(EoAutoFocusStatusText));

            _context.OnPropertyChanged(nameof(IrAutoFocusStatusText));

            _context.OnPropertyChanged(nameof(_context.IsOperationCommandEnabled));

            _context.OnPropertyChanged(nameof(_context.IsControlInputLocked));

            _context.OnPropertyChanged(nameof(_context.IsPanTiltSpeedControlEnabled));

            _context.OnPropertyChanged(nameof(_context.IsOperationLockOverlayVisible));

            _context.OnPropertyChanged(nameof(_context.ControlLockTitle));

            _context.OnPropertyChanged(nameof(_context.ControlLockMessage));

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

    }
}
