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

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        #region [시험 기능 상태]

        private bool _v292Enabled = true;

        private bool _af292ActivatedForSession;

        private int _v292Generation;

        private DispatcherTimer _v292Timer;

        private bool _precisionActive;

        private bool _precisionStopUncertain;

        private bool _precisionJog;

        private bool _precisionPreparing;

        private byte _precisionDirection;

        private bool _precisionTilt;

        private double _precisionTarget;

        private double _precisionDeadline;

        private double _precisionSentAt;

        private long _precisionPoseTicks;

        private int _precisionArrivalSamples;

        private int _precisionOperation;

        private string _precisionStatus = "시험 기능 꺼짐 / 원본 제어 유지";

        private readonly AutoFocusSnapshot[] _af292 = new AutoFocusSnapshot[2];

        private readonly int[] _af292Pending = { -1, -1 };

        private readonly double[] _af292Deadline = new double[2];

        private readonly bool[] _af292Uncertain = new bool[2];

        private readonly bool[] _af292FocusUnknown = new bool[2];

        private readonly string[] _af292Text = { "미조회", "미조회" };

        private ICommand _precisionAbsoluteCommand;

        private ICommand _precisionStopCommand;

        private ICommand _af292Command;

        public string PrecisionPanSpeed { get; set; } = "0.010";

        public string PrecisionTiltSpeed { get; set; } = "0.010";

        // 실장비 상한을 확인하지 않은 상태에서 90도/초를 허용하지 않는다.
        private const double PrecisionMaximumSpeed = 60.0;

        /// <summary>AF 화면과 명령의 지원 대상은 선택한 WEB AGENT 프로필로 결정한다.</summary>
        public bool IsAf292PanelVisible => IsSelectedControlAgentWebAgent;

        public bool V292Enabled
        {
            get => IsAf292PanelVisible && _v292Enabled;
            set
            {
                if (_v292Enabled == value)
                    return;
                if (value && !IsAf292PanelVisible)
                {
                    SetPrecisionStatus("WEB AGENT 연결 필요");
                    OnPropertyChanged();
                    return;
                }

                if (!value)
                    StopPrecisionMotion("시험 기능 해제");
                _v292Enabled = value;
                EnsureV292Timer();
                OnPropertyChanged();
                RefreshV292Controls();
                if (value)
                    ActivateAf292Session(true);
            }
        }

        public string PrecisionStatusText => _precisionStatus;

        public string EoAf292StatusText => "EO AF : " + _af292Text[0];

        public string IrAf292StatusText => "IR AF : " + _af292Text[1];

        public bool CanUsePrecisionJogButton => CanStartPrecisionMotion || (_precisionActive && _precisionJog);

        // 정밀 이동은 실장비 검증 보류로 프로토콜만 유지하고 실행 경로는 잠근다.
        public bool CanStartPrecisionMotion => false;

        private bool V292HasOtherOperation => _laPresetSingleMoveCts != null || _presetSingleMoveCts != null ||
            _rooftopZoomSyncCts != null || _rooftopFocusSyncCts != null || _moveControlPanCts != null ||
            _lensSyncOperationLock.CurrentCount == 0;

        private bool V292Ready => V292Enabled && CameraFeatureTransportReady;

        public ICommand PrecisionAbsoluteCommand => _precisionAbsoluteCommand ??
            (_precisionAbsoluteCommand = new ParameterRelayCommand(p => StartPrecisionAbsolute(Convert.ToString(p)), p => CanStartPrecisionMotion));

        public ICommand PrecisionStopCommand => _precisionStopCommand ??
            (_precisionStopCommand = new RelayCommand(() => StopPrecisionMotion("수동 정지", true)));

        public ICommand Af292Command => _af292Command ??
            (_af292Command = new ParameterRelayCommand(ExecuteAf292, CanExecuteAf292));

        #endregion

        #region [정밀 이동 및 정지]

        /// <summary>
        /// 보관된 장비 위치가 정밀 동작 판단에 사용할 만큼 최근에 수신되었는지 확인한다.
        /// </summary>
        private bool IsPrecisionPoseFresh()
        {
            var pose = Volatile.Read(ref _videoCenterPose);
            return pose != null && Math.Abs(DateTime.UtcNow.Ticks - pose.Item3) < TimeSpan.FromSeconds(2).Ticks;
        }

        /// <summary>
        /// 정밀 속도 문자열을 해석하고 프로토콜·장비 상한 범위 안인지 확인한다.
        /// </summary>
        private bool TryPrecisionSpeed(bool tilt, out double speed)
        {
            string input = tilt ? PrecisionTiltSpeed : PrecisionPanSpeed;
            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out speed))
            {
                SetPrecisionStatus("속도 입력 오류 / 예: 0.001 또는 0.010");
                return false;
            }

            try
            {
                WebAgentV292Protocol.EncodeSpeed(speed, PrecisionMaximumSpeed);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                SetPrecisionStatus("속도 범위: 0.001 ~ 60.000도/초");
                return false;
            }
        }

        /// <summary>
        /// 정밀 절대 이동의 보관 경로이며 현재는 상위 실행 허용값이 false여서 송신하지 않는다.
        /// </summary>
        private async void StartPrecisionAbsolute(string axis)
        {
            if (!CanStartPrecisionMotion || (axis != "PAN" && axis != "TILT"))
                return;
            bool tilt = axis == "TILT";
            double? value = tilt ? TiltAbsoluteValue : PanAbsoluteValue;
            double speed;
            if (!value.HasValue || !TryPrecisionSpeed(tilt, out speed))
                return;

            double target = value.Value;
            double minimum = tilt ? CurrentMoveControlTiltMinimum : CurrentMoveControlPanMinimum;
            double maximum = tilt ? CurrentMoveControlTiltMaximum : CurrentMoveControlPanMaximum;
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
            int generation = _v292Generation;
            RefreshV292Controls();
            try
            {
                if (!tilt && !ApplySelectedPanTurnMode())
                {
                    StopPrecisionMotion("회전 경로 설정 송신 실패");
                    return;
                }

                if (!tilt)
                    await Task.Delay(100);
                if (generation != _v292Generation || operation != _precisionOperation || !V292Ready || !_precisionActive)
                    return;
                _precisionPreparing = false;
                if (!_controlCommandService.SendPrecisionAbsolute(tilt, _precisionTarget, speed, PrecisionMaximumSpeed))
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

        /// <summary>
        /// 정밀 조그의 보관 경로이며 현재는 상위 실행 허용값이 false여서 시작하지 않는다.
        /// </summary>
        public bool StartPrecisionJog(byte direction)
        {
            if (!CanStartPrecisionMotion || direction == 0)
                return false;
            double pan = 0;
            double tilt = 0;
            if (((direction & 6) != 0 && !TryPrecisionSpeed(false, out pan)) ||
                ((direction & 24) != 0 && !TryPrecisionSpeed(true, out tilt)))
                return false;

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
                RefreshV292Controls();
                if (!_controlCommandService.SendPrecisionJog(direction, pan, tilt, PrecisionMaximumSpeed))
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

        /// <summary>
        /// 조그 방향이 현재 팬·틸트의 설정 한계를 벗어나는지 확인한다.
        /// </summary>
        private bool IsPrecisionJogWithinLimits(byte direction)
        {
            var pose = Volatile.Read(ref _videoCenterPose);
            if (pose == null)
                return false;
            double panSpeed;
            double tiltSpeed;
            if (!double.TryParse(PrecisionPanSpeed, NumberStyles.Float, CultureInfo.InvariantCulture, out panSpeed) ||
                !double.TryParse(PrecisionTiltSpeed, NumberStyles.Float, CultureInfo.InvariantCulture, out tiltSpeed))
                return false;

            // 상태 지연과 다음 타이머 주기 동안 이동할 거리를 남기고 한계보다 먼저 정지한다.
            double lead = Math.Max(0, (DateTime.UtcNow.Ticks - pose.Item3) / (double)TimeSpan.TicksPerSecond) + 0.3;
            double panMargin = Math.Abs(panSpeed) * lead + 0.1;
            double tiltMargin = Math.Abs(tiltSpeed) * lead + 0.1;
            return ((direction & 2) == 0 || pose.Item1 > CurrentMoveControlPanMinimum + panMargin) &&
                ((direction & 4) == 0 || pose.Item1 < CurrentMoveControlPanMaximum - panMargin) &&
                ((direction & 8) == 0 || pose.Item2 < CurrentMoveControlTiltMaximum - tiltMargin) &&
                ((direction & 16) == 0 || pose.Item2 > CurrentMoveControlTiltMinimum + tiltMargin);
        }

        /// <summary>
        /// 진행 중 정밀 동작 상태를 정리하고 필요할 때 양 축 정지를 요청한다.
        /// </summary>
        public void StopPrecisionMotion(string reason, bool force = false)
        {
            bool wasActive = _precisionActive;
            bool shouldSend = wasActive || (force && (V292Ready || _precisionStopUncertain));
            ++_precisionOperation;
            _precisionActive = false;
            _precisionPreparing = false;
            _precisionJog = false;
            if (shouldSend && _laTcpService.IsConnected && IsEnvironmentStatusSelected)
            {
                // 두 축 검증 후 순차 실행되는 조그의 부분 실패에도 정지 요청을 두 경로로 보낸다.
                bool jogStop = _controlCommandService.SendPrecisionJog(0, 0, 0, PrecisionMaximumSpeed);
                bool axisStop = _controlCommandService.StopPanTiltPositionMove();
                _precisionStopUncertain = !(jogStop && axisStop);
                SetPrecisionStatus(reason + " / 정지 송신=" + (jogStop && axisStop) + " / 실제 정지는 장비 상태로 확인");
            }
            else if (wasActive)
            {
                _precisionStopUncertain = true;
                SetPrecisionStatus(reason + " / 통신 불가: 장비 정지 여부를 현장에서 확인하세요.");
            }

            RefreshV292Controls();
        }

        #endregion

        #region [자동 초점 요청 및 응답]

        /// <summary>
        /// 연결·지원 기능·응답 대기·제어 잠금을 확인해 대상별 AF 명령 실행 가능 여부를 판단한다.
        /// </summary>
        private bool CanExecuteAf292(object parameter)
        {
            string text = Convert.ToString(parameter);
            if (string.IsNullOrEmpty(text) || text.Length != 2 || text[0] < '0' || text[0] > '1')
                return false;
            int target = text[0] - '0';
            int action = text[1] - '0';
            if (!V292Ready || !Af292TargetUsesAgent(target) || V292HasOtherOperation || _controlInputs.Count != 0 ||
                _currentMoveType != ContinuousMoveType.None || _af292Uncertain[target] || _af292Pending[target] >= 0 ||
                !IsOperationCommandEnabled || IsControlInputLocked)
                return false;
            if (action == 0)
                return true;
            if (_af292FocusUnknown[target])
                return false;
            var state = _af292[target];
            return state != null && state.Result == 0 &&
                (action == 1 || action == 2 ? state.ContinuousSupported : action == 3 && state.OnePushSupported && !state.Running);
        }

        /// <summary>
        /// 선택한 EO/IR AF 버튼 인수를 조회·설정·단발 초점 요청으로 변환한다.
        /// </summary>
        private void ExecuteAf292(object parameter)
        {
            if (!CanExecuteAf292(parameter))
                return;
            string text = Convert.ToString(parameter);
            int action = text[1] - '0';
            SendAf292(text[0] - '0', action == 0 ? 0 : action == 3 ? 2 : 1, action == 1 ? 1 : 0);
        }

        /// <summary>
        /// 대상별 응답 대기 상태와 제한 시간을 설정한 뒤 AF 패킷을 송신한다.
        /// </summary>
        private void SendAf292(int target, int operation, int value)
        {
            if (!V292Ready || target < 0 || target > 1 || !Af292TargetUsesAgent(target) || _af292Pending[target] >= 0 || _af292Uncertain[target])
                return;
            _af292Pending[target] = operation;
            _af292Deadline[target] = TelemetryClock.MonotonicMilliseconds + 5000;
            byte command = operation == 0 ? (byte)0xCB : operation == 1 ? (byte)0xC7 : (byte)0xC9;
            bool sent = _controlCommandService.SendAutoFocusV292(command, (byte)target, (byte)value);
            if (!sent)
                _af292Pending[target] = -1;
            _af292Text[target] = sent ? "응답 대기" : "송신 실패";
            RefreshV292Controls();
        }

        /// <summary>
        /// AF 응답의 형식·세션·대상·작업을 확인하고 지원 여부와 실제 상태를 반영한다.
        /// </summary>
        private void ParseAutoFocusV292(byte[] payload)
        {
            AutoFocusSnapshot state;
            if (!WebAgentV292Protocol.TryReadAutoFocus(payload, out state))
            {
                ConsoleLogHelper.Warning("WEB AGENT AF", "형식·버전·필드 범위를 벗어난 응답 거부");
                return;
            }

            int generation = _v292Generation;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _v292Generation || !_af292ActivatedForSession || !IsAf292PanelVisible || !CameraFeatureTransportReady || !Af292TargetUsesAgent(state.Target))
                    return;
                int target = state.Target;
                bool expected = _af292Pending[target] == state.Operation;
                bool notification = state.Operation == 3;
                if (_af292Uncertain[target] || (!expected && !notification))
                {
                    ConsoleLogHelper.Warning("WEB AGENT AF", "늦거나 대기 작업과 일치하지 않는 응답 / 화면 상태에 미적용");
                    return;
                }

                // 변경 알림은 조회·설정 응답 대기를 해제하지 않는다.
                if (expected)
                    _af292Pending[target] = -1;
                if (state.Result == 0)
                {
                    _af292[target] = state;
                    _af292FocusUnknown[target] = false;
                }
                else
                {
                    // 오류 응답의 빈 플래그로 이전 AF ON·검색 상태를 OFF로 덮어쓰지 않는다.
                    _af292FocusUnknown[target] = !(state.Result == 2 && state.Operation == 0 && _af292[target] == null);
                }
                // 접수·검색·전달 상태는 완료로 표시하지 않는다.
                string[] names = { "대기", "검색 중", "처리 중", "완료 미확인", "실행 오류", "미지원" };
                string mode = state.ContinuousSupported ? ((state.Flags & 4) != 0 ? "ON" : "OFF") : "단발";
                _af292Text[target] = state.Result == 0
                    ? mode + " · " + (state.ConfirmedComplete ? "완료 확인" : names[state.State])
                    : state.Result == 2 ? "미지원" : state.Result == 1 ? "입력 오류" : "오류 · 상태 재조회";
                ConsoleLogHelper.State("WEB AGENT AF", "대상=" + target + " / 작업=" + state.Operation +
                    " / 결과=" + state.Result + " / 플래그=" + state.Flags.ToString("X2") +
                    " / 실행주체=" + state.Owner + " / 경로=" + state.Transport + " / 상태=" + state.State);
                RefreshV292Controls();
            }));
        }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다.
        /// </summary>
        private bool IsAf292FocusBlocked(bool ir)
        {
            if (!IsAf292PanelVisible || !CameraFeatureTransportReady || !Af292TargetUsesAgent(ir ? 1 : 0))
                return false;
            int target = ir ? 1 : 0;
            var state = _af292[target];
            bool blocked = _af292Uncertain[target] || _af292FocusUnknown[target] || _af292Pending[target] >= 0 ||
                (state != null && (state.Running || (state.Result == 0 && (state.Flags & 4) != 0)));
            if (blocked)
                ConsoleLogHelper.Warning("WEB AGENT AF", (ir ? "IR" : "EO") + " 자동 초점 동작·응답 대기 중 수동 초점 충돌 방지 / AF OFF 및 상태 확인 필요");
            return blocked;
        }

        /// <summary>
        /// EO 직접 제어 구성을 구분해 WEB AGENT AF 명령을 적용할 대상인지 확인한다.
        /// </summary>
        private bool Af292TargetUsesAgent(int target)
        {
            if (!IsAf292PanelVisible)
                return false;
            if (target == 1)
                return true;
            RtspSourceOption source;
            return !TryGetSelectedEoCtecSource(out source);
        }

        #endregion

        #region [세션 및 안전 타이머]

        /// <summary>
        /// AF 응답 제한 시간과 보관된 정밀 동작 안전 상태를 확인할 타이머를 준비한다.
        /// </summary>
        /// <summary>지원 장비 연결 뒤 한 번만 상태를 조회하며 AF 모드를 강제로 ON으로 바꾸지 않는다.</summary>
        private void ActivateAf292Session(bool refresh = false)
        {
            if (!V292Ready || (!refresh && _af292ActivatedForSession) ||
                !IsOperationCommandEnabled || IsControlInputLocked)
                return;
            _af292ActivatedForSession = true;
            if (!Af292TargetUsesAgent(0))
                _af292Text[0] = "직접 제어 · 기존 AF 사용";
            SendAf292(0, 0, 0);
            SendAf292(1, 0, 0);
            ConsoleLogHelper.State("WEB AGENT AF", "지원 장비 AF 제어 사용 / 상태 자동 조회");
        }

        private void EnsureV292Timer()
        {
            if (_v292Timer == null)
            {
                _v292Timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(100) };
                _v292Timer.Tick += (sender, args) => TickV292Safety();
            }

            _v292Timer.Start();
        }

        /// <summary>
        /// 세션 변경·응답 시간 초과·위치 유효성·동작 한계를 주기적으로 확인한다.
        /// </summary>
        private void TickV292Safety()
        {
            if (!IsAf292PanelVisible || !CameraFeatureTransportReady)
            {
                if (_af292ActivatedForSession)
                    ResetV292Session();
                return;
            }

            if (_v292Enabled && !_af292ActivatedForSession)
                ActivateAf292Session();

            double now = TelemetryClock.MonotonicMilliseconds;
            for (int i = 0; i < 2; i++)
            {
                if (_af292Pending[i] >= 0 && now >= _af292Deadline[i])
                {
                    _af292Pending[i] = -1;
                    _af292Uncertain[i] = true;
                    _af292Text[i] = "미응답 · 재연결 필요";
                    ConsoleLogHelper.Warning("WEB AGENT AF", "응답 시간 초과 / 대상=" + i + " / 요청 번호가 없어 재송신 금지");
                }
            }

            if (_precisionActive && !_precisionPreparing)
            {
                if (!IsPrecisionPoseFresh())
                    StopPrecisionMotion("위치 상태 갱신 중단");
                else if (now >= _precisionDeadline)
                    StopPrecisionMotion("시험 안전 시간 초과 (도착 완료 아님)");
                else if (_precisionJog && !IsPrecisionJogWithinLimits(_precisionDirection))
                    StopPrecisionMotion("장비 각도 한계 접근");
                else if (!_precisionJog && now - _precisionSentAt > 500)
                {
                    var pose = Volatile.Read(ref _videoCenterPose);
                    double actual = _precisionTilt ? pose.Item2 : pose.Item1;
                    double error = Math.Abs(actual - _precisionTarget);
                    if (!_precisionTilt)
                        error = Math.Min(error, 360 - error);
                    if (pose.Item3 > _precisionPoseTicks)
                    {
                        _precisionPoseTicks = pose.Item3;
                        _precisionArrivalSamples = error <= 0.03 ? _precisionArrivalSamples + 1 : 0;
                        if (_precisionArrivalSamples >= 3)
                            StopPrecisionMotion("수신 위치가 목표 허용 오차에 도달");
                    }
                }
            }

            RefreshV292Controls();
            if (!_v292Enabled && !_precisionActive && _af292Pending[0] < 0 && _af292Pending[1] < 0)
                _v292Timer.Stop();
        }

        /// <summary>
        /// 연결 세대를 변경하고 이전 장비의 AF 상태·대기 요청·실행 확인을 초기화한다.
        /// </summary>
        private void ResetV292Session()
        {
            int generation = Interlocked.Increment(ref _v292Generation);
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _v292Generation)
                    return;
                // 새 연결에 이전 장비용 정지 명령을 뒤늦게 전송하지 않는다.
                bool active = _precisionActive || _precisionStopUncertain;
                _precisionActive = false;
                _precisionStopUncertain = false;
                _precisionPreparing = false;
                _precisionJog = false;
                ++_precisionOperation;
                _v292Enabled = true;
                _af292ActivatedForSession = false;
                for (int i = 0; i < 2; i++)
                {
                    _af292[i] = null;
                    _af292Pending[i] = -1;
                    _af292Uncertain[i] = false;
                    _af292FocusUnknown[i] = false;
                    _af292Text[i] = "조회 전";
                }

                _v292Timer?.Stop();
                if (IsAf292PanelVisible)
                    EnsureV292Timer();
                SetPrecisionStatus(active ? "연결 변경 / 이전 장비 정지 여부를 현장에서 확인하세요." : "WEB AGENT AF 기본 사용");
                OnPropertyChanged(nameof(V292Enabled));
                RefreshV292Controls();
            }));
        }

        /// <summary>
        /// 정밀 명령의 실행 불가 응답을 확인해 보관된 동작 상태와 정지 처리를 정리한다.
        /// </summary>
        private void HandlePrecisionUnavailable(byte[] payload)
        {
            if (payload.Length < 6 || payload[0] != 1 ||
                (payload[3] != 0xC1 && payload[3] != 0xC3 && payload[3] != 0xC5))
                return;
            int generation = _v292Generation;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _v292Generation || !V292Ready || !_precisionActive)
                    return;
                StopPrecisionMotion("정밀 이동 거부·실행 오류 / 상태=" + payload[1]);
            }));
        }

        /// <summary>
        /// 정밀 동작 진단 상태를 갱신하고 변경 알림을 보낸다.
        /// </summary>
        private void SetPrecisionStatus(string text)
        {
            _precisionStatus = text;
            OnPropertyChanged(nameof(PrecisionStatusText));
            ConsoleLogHelper.State("WEB AGENT 정밀 이동", text);
        }

        /// <summary>
        /// AF 상태·수동 초점 잠금·명령 실행 가능 여부의 화면 알림을 갱신한다.
        /// </summary>
        private void RefreshV292Controls()
        {
            OnPropertyChanged(nameof(IsAf292PanelVisible));
            OnPropertyChanged(nameof(V292Enabled));
            OnPropertyChanged(nameof(CanStartPrecisionMotion));
            OnPropertyChanged(nameof(CanUsePrecisionJogButton));
            OnPropertyChanged(nameof(EoAf292StatusText));
            OnPropertyChanged(nameof(IrAf292StatusText));
            OnPropertyChanged(nameof(IsOperationCommandEnabled));
            OnPropertyChanged(nameof(IsControlInputLocked));
            OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
            OnPropertyChanged(nameof(IsOperationLockOverlayVisible));
            OnPropertyChanged(nameof(ControlLockTitle));
            OnPropertyChanged(nameof(ControlLockMessage));
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion
    }
}
