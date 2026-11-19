using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Configuration;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>입력 허용과 정지 조정에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IInputCoordinatorFeatureContext
    {
        /// <summary>다른 제어 작업 전에 추적을 종료하고 보류 이동을 취소한다.</summary>
        void StopAutoTracking(string reason);

        /// <summary>추적 중에는 사용자 PTZF 입력과 다른 이동 시작을 차단한다.</summary>
        bool IsAutoTrackingActive { get; }

        ref global::OpenCvWpfTracking.Models.Main.RtspSourceOption ActiveEoCtecSource { get; }

        ref double? ActivePanAbsoluteTarget { get; }

        ref double? ActiveTiltAbsoluteTarget { get; }

        Services.Communication.AI.IAiDetectorClientServicePort AiDetectorClientService { get; }

        Services.Communication.IControlCommandServicePort ControlCommandService { get; }

        ContinuousMoveType CurrentMoveType { get; set; }

        bool IsDeviceConnectionRequested { get; set; }

        Services.Communication.ITcpClientServicePort LaTcpService { get; }

        ControlAgentProfileOption RequestedControlProfile { get; set; }

        bool VideoCenterBusy { get; set; }

        string AiPowerStatusText { get; set; }

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다.
        /// </summary>
        void ClearActivePanTiltAbsoluteMove();

        /// <summary>
        /// 2026-08-25: AI 연결 전환 또는 해제 시 화면 BBox와 ACTIVE 이벤트 상태를 UI Dispatcher에서 함께 정리하여
        /// 연결이 끊긴 뒤 이전 탐지 결과가 남지 않게 한다.
        /// </summary>
        void ClearAiDetectionState(string reason, bool writeAudit = true);

        byte EffectivePanTiltSpeedLevel { get; }

        double EoDisplayZoomRatio { get; }

        string EoPowerControlStatusText { get; }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrZoomStandardPosition();

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardZoom();

        double IrDisplayZoomRatio { get; }

        string IrPowerControlStatusText { get; }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다.
        /// </summary>
        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsControlInputLocked { get; }

        bool IsOperationCommandEnabled { get; }

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanTiltSpeedDisplayText { get; }

        byte PanTiltSpeedLevel { get; set; }

        /// <summary>
        /// 접속 및 RTSP URL 변경 때 이전 원본 크기를 무효화한다.
        /// </summary>
        void ResetAiRtspVideoSizes();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        /// <summary>
        /// 2026-10-01: 메인 화면의 활성 EO/IR 영상 변경을 PT 감쇄 기준에 반영한다. 신규 WebAgent 프로토콜 없이 기존 PT
        /// 이동 패킷의 속도값만 조정한다.
        /// </summary>
        void SetActivePanTiltZoomSource(bool isInfrared);

        /// <summary>
        /// AI 연결 상태에 맞춰 로컬 화재·연기 탐지기 사용 상태를 조정한다.
        /// </summary>
        void SetFireSmokeDetectorsForAiConnection(bool enabled, string reason);

        /// <summary>
        /// EO 초점을 Far 방향으로 연속 이동하기 시작한다.
        /// </summary>
        Task StartEoFocusFarMoveAsync();

        /// <summary>
        /// EO 초점을 Near 방향으로 연속 이동하기 시작한다.
        /// </summary>
        Task StartEoFocusNearMoveAsync();

        /// <summary>
        /// EO 줌 확대 연속 이동을 시작한다.
        /// </summary>
        Task StartEoZoomInMoveAsync();

        /// <summary>
        /// EO 줌 축소 연속 이동을 시작한다.
        /// </summary>
        Task StartEoZoomOutMoveAsync();

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Far] 연속 이동 시작
        /// </summary>
        void StartIrFocusFarMove();

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Near] 연속 이동 시작
        /// </summary>
        void StartIrFocusNearMove();

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Tele] 연속 이동 시작
        /// </summary>
        void StartIrZoomInMove();

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Wide] 연속 이동 시작
        /// </summary>
        void StartIrZoomOutMove();

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 좌측 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 좌측 방향으로 연속 이동
        /// 명령을 송신한다.
        /// </summary>
        void StartPanLeftMove();

        /// <summary>
        /// [EO/IR] 좌측 하단 대각선 연속 이동 시작
        /// </summary>
        void StartPanLeftTiltDownMove();

        /// <summary>
        /// [EO/IR] 좌측 상단 대각선 연속 이동 시작
        /// </summary>
        void StartPanLeftTiltUpMove();

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 우측 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 우측 방향으로 연속 이동
        /// 명령을 송신한다.
        /// </summary>
        void StartPanRightMove();

        /// <summary>
        /// [EO/IR] 우측 하단 대각선 연속 이동 시작
        /// </summary>
        void StartPanRightTiltDownMove();

        /// <summary>
        /// [EO/IR] 우측 상단 대각선 연속 이동 시작
        /// </summary>
        void StartPanRightTiltUpMove();

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 아래쪽 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 아래 방향으로 연속
        /// 이동 명령을 송신한다.
        /// </summary>
        void StartTiltDownMove();

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 위쪽 연속 이동 시작 [PanTiltSpeedLevel] 값을 사용하여 위쪽 방향으로 연속 이동
        /// 명령을 송신한다.
        /// </summary>
        void StartTiltUpMove();

        /// <summary>
        /// 2026-09-15: 화면 중앙 STOP 버튼 전용 PTZF 전체 정지. 현재 GUI 이동 상태와 관계없이 Pelco-D 전체 STOP FF
        /// 01 00 00 00 00 01을 반드시 1회 송신한다. 옥상 CTEC EO 렌즈가 직접 제어 중이면 해당 렌즈도 함께 정지한다.
        /// </summary>
        void StopAllPtzfMove();

        /// <summary>
        /// 현재 연속 이동을 중지하고 필요한 위치 응답의 안정화를 확인한다.
        /// </summary>
        Task StopContinuousMoveAsync(bool waitForStatus = true);

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] 연속 이동 정지
        /// </summary>
        void StopIrFocusMove();

        /// <summary>
        /// [IR] [ZOOM] 연속 이동 정지 IR Zoom 버튼 [MouseUp] 시에만 호출한다.
        /// </summary>
        void StopIrZoomMove();

        /// <summary>
        /// Pan·Tilt 연속 이동을 중지한다.
        /// </summary>
        void StopPanTiltContinuousMove();

        /// <summary>
        /// 경로 실행 취소를 요청하고 진행 중 이동을 정리한다.
        /// </summary>
        void StopRoute();

        /// <summary>
        /// 단조 시계의 경과 시간을 계산해 EO/IR 디지털 추정 모델을 함께 갱신한다.
        /// </summary>
        void TickDigitalEstimate();

        /// <summary>
        /// 클릭 이동 기한, D-ZOOM 응답 대기 및 주기 조회를 기존 간격으로 감시한다.
        /// </summary>
        void TickDigitalZoom();

        /// <summary>
        /// 기존 250ms UI 타이머를 사용한다. 일괄 조회도 한 틱당 한 건으로 제한한다.
        /// </summary>
        void TickSpecialFeatures();
    }

    /// <summary>입력 허용과 정지 조정 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class InputCoordinatorFeature
    {

        #region [협력 객체와 초기화]

        private readonly IInputCoordinatorFeatureContext _context;

        /// <summary>
        /// 조작 입력 조정 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal InputCoordinatorFeature(IInputCoordinatorFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 기능 객체가 사용하는 초기 상태·목록·타이머를 준비한다.
        /// </summary>
        internal void InitializeState()
        {
            _isFovSpeedEnabled = true;
            _isVideoClickCenterEnabled = true;
            _isZoomRatioOverlayVisible = true;
        }

        #endregion

        #region [상태 필드]

        internal readonly Dictionary<string, Tuple<string, int, int>> _controlInputs = new Dictionary<string, Tuple<string, int, int>>();

        internal readonly Dictionary<string, Tuple<int, int>> _issuedControlAxes = new Dictionary<string, Tuple<int, int>>();

        internal readonly SemaphoreSlim _inputReconcile = new SemaphoreSlim(1, 1);

        internal bool _isFovSpeedEnabled;

        internal bool _isGlobalControlEnabled;

        internal bool _isVideoClickCenterEnabled;

        internal bool _isZoomRatioOverlayVisible;

        internal int _inputEpoch;

        internal long _eoLensReceivedTicks;

        internal long _irLensReceivedTicks;

        internal DispatcherTimer _controlStatusTimer;

        internal string _lastZoomNotification;

        internal string _lastPowerNotification;

        internal bool _replayControlInputs;

        internal readonly Dictionary<string, RtspSourceOption> _axisCtecSources = new Dictionary<string, RtspSourceOption>();

        internal string _lastOpticalSpeedSignature;

        #endregion

        #region [화면 속성과 이벤트]

        internal bool IsFovSpeedEnabled
        {
            get { return _isFovSpeedEnabled; }

            set
            {
                if (_isFovSpeedEnabled == value)
                {
                    return;
                }

                _isFovSpeedEnabled = value;

                _context.OnPropertyChanged();

                RefreshControlSpeed();

                ConsoleLogHelper.State("FOV SPEED", value ? "GUI enabled / estimated ratio" : "GUI disabled / agent-side compensation may be used");
            }
        }

        internal bool IsGlobalControlEnabled
        {
            get { return _isGlobalControlEnabled; }

            set
            {
                if (_isGlobalControlEnabled == value)
                {
                    return;
                }

                ReleaseAllControlInputs("Global control changed");

                _isGlobalControlEnabled = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(GlobalControlStatusText));
            }
        }

        internal bool IsVideoClickCenterEnabled
        {
            get { return _isVideoClickCenterEnabled; }

            set
            {
                if (_isVideoClickCenterEnabled == value)
                {
                    return;
                }

                _isVideoClickCenterEnabled = value;

                _context.OnPropertyChanged();

                ConsoleLogHelper.State("VIDEO CLICK CENTER", value ? "Enabled" : "Disabled");
            }
        }

        internal bool IsZoomRatioOverlayVisible
        {
            get { return _isZoomRatioOverlayVisible; }

            set
            {
                if (_isZoomRatioOverlayVisible == value)
                {
                    return;
                }

                _isZoomRatioOverlayVisible = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GlobalControlStatusText
        {
            get
            {
                if (IsGlobalControlEnabled)
                {
                    return "GLOBAL PTZF ON";
                }
                else
                {
                    return "GLOBAL PTZF OFF";
                }
            }
        }

        internal double EoOpticalRatioMinimum
        {
            get { return ControlTuningFor(false).MinimumRatio; }
        }

        internal double EoOpticalRatioMaximum
        {
            get { return ControlTuningFor(false).MaximumRatio; }
        }

        internal string ZoomOpticalStandardText
        {
            get
            {
                return $"EO {ControlTuningFor(false).MaximumRatio:F1}x / IR {ZoomDisplayMaximum(true):F1}x OPTICAL";
            }
        }

        internal string ZoomOpticalRangeText
        {
            get
            {
                return $"EO {ControlTuningFor(false).MinimumRatio:F1}~{ControlTuningFor(false).MaximumRatio:F1}x / IR {ControlTuningFor(true).MinimumRatio:F1}~{ZoomDisplayMaximum(true):F1}x\nEnter EO ratio; IR follows the same position %.";
            }
        }

        internal double EoSpeedExponent
        {
            get { return ControlTuningFor(false).Exponent; }

            set
            {
                SetExponent(false, value);
            }
        }

        internal double IrSpeedExponent
        {
            get { return ControlTuningFor(true).Exponent; }

            set
            {
                SetExponent(true, value);
            }
        }

        internal int EoMinimumSpeed
        {
            get { return ControlTuningFor(false).MinimumSpeed; }

            set
            {
                SetMinimum(false, value);
            }
        }

        internal int IrMinimumSpeed
        {
            get { return ControlTuningFor(true).MinimumSpeed; }

            set
            {
                SetMinimum(true, value);
            }
        }

        internal string EoZoomRatioOverlayText
        {
            get
            {
                if (IsLensFresh(false))
                {
                    return "EO " + DisplayZoomRatioText(false);
                }
                else
                {
                    return "EO —";
                }
            }
        }

        internal string IrZoomRatioOverlayText
        {
            get
            {
                if (IsLensFresh(true))
                {
                    return "IR " + DisplayZoomRatioText(true);
                }
                else
                {
                    return "IR —";
                }
            }
        }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다.
        /// </summary>
        internal LensControlTuning ControlTuningFor(bool ir)
        {
            var profile = _context.RequestedControlProfile ?? _context.SelectedControlAgentProfile;

            return ControlTuning.Get(profile?.DeviceId, ir);
        }

        /// <summary>
        /// 유효 범위의 채널별 감속 지수를 반영하고 현재 PT 속도를 갱신한다.
        /// </summary>
        internal void SetExponent(bool ir, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 1 || value > 4)
            {
                return;
            }

            ControlTuningFor(ir).Exponent = value;

            _context.OnPropertyChanged(ir ? nameof(IrSpeedExponent) : nameof(EoSpeedExponent));

            RefreshControlSpeed();
        }

        /// <summary>
        /// 채널별 최소 PT 속도를 1~60 범위에서 갱신한다.
        /// </summary>
        internal void SetMinimum(bool ir, int value)
        {
            if (value < 1 || value > 60)
            {
                return;
            }

            ControlTuningFor(ir).MinimumSpeed = value;

            _context.OnPropertyChanged(ir ? nameof(IrMinimumSpeed) : nameof(EoMinimumSpeed));

            RefreshControlSpeed();
        }

        /// <summary>
        /// Z/C 반복 입력을 포함하여 공통 설정 속도를 1~60으로 제한해 증감한다.
        /// </summary>
        internal void AdjustControlSpeed(int delta)
        {
            if (_context.IsAutoTrackingActive)
            {
                return;
            }

            _context.PanTiltSpeedLevel = (byte)Math.Max(1, Math.Min(60, _context.PanTiltSpeedLevel + delta));
        }

        /// <summary>
        /// 표시 속도를 갱신하고 이미 동작 중인 PT 축에만 변경 속도를 재적용한다.
        /// </summary>
        internal void RefreshControlSpeed()
        {
            _context.OnPropertyChanged(nameof(_context.EffectivePanTiltSpeedLevel));

            _context.OnPropertyChanged(nameof(_context.PanTiltSpeedDisplayText));

            if (_issuedControlAxes.ContainsKey("PT"))
            {
                _replayControlInputs = true;

                ReconcileControlInputs();
            }
        }

        /// <summary>
        /// 연결 요청과 마지막 렌즈 응답 시간을 기준으로 유효성을 확인한다.
        /// </summary>
        internal bool IsLensFresh(bool ir)
        {
            return _context.IsDeviceConnectionRequested && DateTime.UtcNow.Ticks - (ir ? Interlocked.Read(ref _irLensReceivedTicks) : Interlocked.Read(ref _eoLensReceivedTicks)) < TimeSpan.FromSeconds(3).Ticks;
        }

        /// <summary>
        /// 영상 배지와 상태값의 배율을 소수점 첫째 자리로 통일한다.
        /// </summary>
        internal string DisplayZoomRatioText(bool ir)
        {
            var curve = ControlTuningFor(ir).Calibration;

            double ratio = ir ? _context.IrDisplayZoomRatio : _context.EoDisplayZoomRatio;

            // 공통 IR 기준표의 끝점 8.15배는 계산에 보존하고 화면 상한만 8.0배로 표시한다.
            if (ir && curve != null)
            {
                ratio = Math.Min(ratio, ZoomDisplayMaximum(true));
            }

            // EO 30~90배는 기존 선형 추정값을 쓰되 화면에서는 같은 한 자리 형식을 사용한다.
            return "x" + ratio.ToString("F1");
        }

        /// <summary>
        /// 설정 안내와 영상 배지에 같은 표시 상한을 사용하며 내부 배율 곡선은 바꾸지 않는다.
        /// </summary>
        internal double ZoomDisplayMaximum(bool ir)
        {
            var tuning = ControlTuningFor(ir);

            if (ir && tuning.Calibration != null)
            {
                return Math.Floor(tuning.MaximumRatio);
            }
            else
            {
                return tuning.MaximumRatio;
            }
        }

        /// <summary>
        /// 표준 줌 위치와 확인된 배율을 상태 문자열로 구성한다.
        /// </summary>
        internal string ZoomRatioStatus(bool ir)
        {
            return (ir ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom()) + " / 1000 (" + (IsLensFresh(ir) ? DisplayZoomRatioText(ir) : "배율 미확인") + ")";
        }

        /// <summary>
        /// 채널별 렌즈 응답 시각을 원자적으로 기록한다.
        /// </summary>
        internal void MarkLensEvidence(bool ir)
        {
            if (ir)
            {
                Interlocked.Exchange(ref _irLensReceivedTicks, DateTime.UtcNow.Ticks);
            }
            else
            {
                Interlocked.Exchange(ref _eoLensReceivedTicks, DateTime.UtcNow.Ticks);
            }
        }

        /// <summary>
        /// 250ms 상태 표시 감시를 한 번만 시작한다. 디지털 추정으로 PT를 재송신하지 않는다.
        /// </summary>
        internal void InitializeControlStatusMonitor()
        {
            if (_controlStatusTimer != null)
            {
                return;
            }

            _controlStatusTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            _controlStatusTimer.Tick += (s, e) =>
            {
                _context.TickDigitalZoom();

                _context.TickSpecialFeatures();

                if (_context.AiPowerStatusText == "ON" && !_context.AiDetectorClientService.IsConnected)
                {
                    _context.AiPowerStatusText = "OFF";

                    _context.ClearAiDetectionState("AI transport lost");

                    _context.SetFireSmokeDetectorsForAiConnection(false, "AI transport lost");

                    _context.ResetAiRtspVideoSizes();
                }

                _context.TickDigitalEstimate();

                string zoom = EoZoomRatioOverlayText + IrZoomRatioOverlayText;

                if (zoom != _lastZoomNotification)
                {
                    _lastZoomNotification = zoom;

                    foreach (var name in ZoomStatusPropertyNames)
                    {
                        _context.OnPropertyChanged(name);
                    }

                    // 표시용 디지털 배율 추정은 PT 명령 재전송을 유발하지 않는다.
                    string optical = _context.GetCurrentPresetStandardZoom() + "/" + _context.GetCurrentIrZoomStandardPosition() + "/" + IsLensFresh(false) + "/" + IsLensFresh(true);

                    if (optical != _lastOpticalSpeedSignature)
                    {
                        _lastOpticalSpeedSignature = optical;

                        RefreshControlSpeed();
                    }
                }

                string power = _context.EoPowerControlStatusText + "/" + _context.IrPowerControlStatusText;

                if (power != _lastPowerNotification)
                {
                    _lastPowerNotification = power;

                    foreach (var name in PowerStatusPropertyNames)
                    {
                        _context.OnPropertyChanged(name);
                    }

                    ConsoleLogHelper.State("POWER EVIDENCE", power + " / RTSP loss is not proof of OFF");
                }

                if ((!_context.IsDeviceConnectionRequested || !_context.LaTcpService.IsConnected) && _controlInputs.Count != 0)
                {
                    ReleaseAllControlInputs("Disconnected");
                }
            };

            _controlStatusTimer.Start();
        }

        /// <summary>
        /// 상태 타이머를 중지하고 종료 시 남은 제어 입력을 해제한다.
        /// </summary>
        internal void ShutdownControlInputMonitor()
        {
            _context.StopAutoTracking("ShutdownControlInputMonitor");

            _context.StopRoute();

            _controlStatusTimer?.Stop();

            ReleaseAllControlInputs("Shutdown");
        }

        /// <summary>
        /// 입력 소유자/채널별 키를 축 명령으로 변환하고 키 해제 및 반복을 처리한다.
        /// </summary>
        internal bool HandleControlKey(string owner, Key key, bool down, bool ir, bool repeat = false)
        {
            string token = owner + "/" + key;

            if (!down)
            {
                if (_controlInputs.ContainsKey(token))
                {
                    ReleaseControlInput(token);

                    return true;
                }

                return false;
            }

            // 추적 잠금 중에는 키 반복과 Z/C 속도 변경까지 소비하고 장비 명령을 만들지 않는다.
            if (_context.IsAutoTrackingActive && (key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down ||
                key == Key.W || key == Key.S || key == Key.A || key == Key.D || key == Key.R || key == Key.T || key == Key.Z || key == Key.C))
            {
                return true;
            }

            if (key == Key.Z || key == Key.C)
            {
                AdjustControlSpeed(key == Key.Z ? -1 : 1);

                return true;
            }

            if (repeat)
            {
                return key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down || key == Key.W || key == Key.S || key == Key.A || key == Key.D || key == Key.Z || key == Key.C;
            }

            switch (key)
            {
                case Key.Left:
                    PressControlInput(token, "PT", -1, 0, ir);
                    break;
                case Key.Right:
                    PressControlInput(token, "PT", 1, 0, ir);
                    break;
                case Key.Up:
                    PressControlInput(token, "PT", 0, 1, ir);
                    break;
                case Key.Down:
                    PressControlInput(token, "PT", 0, -1, ir);
                    break;
                case Key.W:
                    PressControlInput(token, ir ? "IZ" : "EZ", 1, 0, ir);
                    break;
                case Key.S:
                    PressControlInput(token, ir ? "IZ" : "EZ", -1, 0, ir);
                    break;
                case Key.A:
                    PressControlInput(token, ir ? "IF" : "EF", ir ? -1 : 1, 0, ir);
                    break;
                case Key.D:
                    PressControlInput(token, ir ? "IF" : "EF", ir ? 1 : -1, 0, ir);
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 연결/잠금 검증 후 누름 입력을 등록하며 수동 입력은 절대 위치 이동보다 우선한다.
        /// </summary>
        internal void PressControlInput(string token, string axis, int x, int y, bool ir)
        {
            if ((axis == "EF" && _context.IsAutoFocusBlockingManualFocus(false)) || (axis == "IF" && _context.IsAutoFocusBlockingManualFocus(true)))
            {
                return;
            }

            // 2026-10-12: 거절한 화면 입력은 원인을 진단할 수 있도록 기록한다.
            string blocked = _context.IsControlInputLocked ? "CONTROL_LOCK" : !_context.IsOperationCommandEnabled ? "OPERATION_LOCK" : !_context.IsDeviceConnectionRequested ? "NO_REQUESTED_CONNECTION" : !_context.LaTcpService.IsConnected ? "TCP_DISCONNECTED" : null;

            if (blocked != null)
            {
                ConsoleLogHelper.Warning("PTZF INPUT", "REJECTED / OWNER=" + token + " / AXIS=" + axis + " / CAMERA=" + (ir ? "IR" : "EO") + " / REASON=" + blocked);

                return;
            }

            if (_controlInputs.ContainsKey(token))
            {
                return;
            }

            // 2026-10-12: 수동 입력은 대기 중이거나 실행 중인 클릭 중심 이동보다 우선한다.
            if (_context.VideoCenterBusy || _context.ActivePanAbsoluteTarget.HasValue || _context.ActiveTiltAbsoluteTarget.HasValue)
            {
                ++_inputEpoch;

                _context.ControlCommandService.StopPanTiltPositionMove();

                _context.ClearActivePanTiltAbsoluteMove();
            }

            _context.SetActivePanTiltZoomSource(ir);

            _controlInputs[token] = Tuple.Create(axis, x, y);

            ConsoleLogHelper.Command("PTZF INPUT", "PRESS / OWNER=" + token + " / AXIS=" + axis + " / CAMERA=" + (ir ? "IR" : "EO") + " / X=" + x + " / Y=" + y);

            ReconcileControlInputs();
        }

        /// <summary>
        /// 해당 토큰만 해제하고 다른 소유자가 누르고 있는 축은 유지한다.
        /// </summary>
        internal void ReleaseControlInput(string token)
        {
            if (_controlInputs.Remove(token))
            {
                ConsoleLogHelper.Command("PTZF INPUT", "RELEASE / OWNER=" + token);

                ReconcileControlInputs();
            }
        }

        /// <summary>
        /// 지정 창/입력 소유자의 토큰만 일괄 해제한다.
        /// </summary>
        internal void ReleaseControlOwner(string owner)
        {
            string prefix = owner + "/";

            foreach (var token in _controlInputs.Keys.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                _controlInputs.Remove(token);
            }

            ReconcileControlInputs();
        }

        /// <summary>
        /// 입력 세대를 갱신해 대기 명령을 무효화하고 필요할 때 전체 PTZF를 정지한다.
        /// </summary>
        internal void ReleaseAllControlInputs(string reason)
        {
            ++_inputEpoch;

            bool active = _controlInputs.Count != 0 || _issuedControlAxes.Count != 0 || _context.VideoCenterBusy || _context.ActivePanAbsoluteTarget.HasValue || _context.ActiveTiltAbsoluteTarget.HasValue;

            _controlInputs.Clear();

            _issuedControlAxes.Clear();

            if (active)
            {
                _context.StopAllPtzfMove();

                ConsoleLogHelper.State("INPUT STOP", reason);
            }
            else
            {
                _axisCtecSources.Clear();
            }
        }

        /// <summary>
        /// 같은 축의 누름 방향을 합산하고 서로 상쇄된 축은 제외한다.
        /// </summary>
        internal Dictionary<string, Tuple<int, int>> DesiredControlAxes()
        {
            var result = new Dictionary<string, Tuple<int, int>>();

            if (_controlInputs.Count == 0)
            {
                return result;
            }

            foreach (var group in _controlInputs.Values.GroupBy(x => x.Item1))
            {
                int x = Math.Sign(group.Sum(v => v.Item2)), y = Math.Sign(group.Sum(v => v.Item3));

                if (x != 0 || y != 0)
                {
                    result[group.Key] = Tuple.Create(x, y);
                }
            }

            return result;
        }

        /// <summary>
        /// 축 변경을 직렬 송신한다. await 이후에는 세대와 최신 누름 상태를 다시 검증한다.
        /// </summary>
        internal async void ReconcileControlInputs()
        {
            await _inputReconcile.WaitAsync();

            int epoch = _inputEpoch;

            try
            {
                if (_context.IsControlInputLocked || !_context.IsDeviceConnectionRequested)
                {
                    ReleaseAllControlInputs("Input not available");

                    return;
                }

                var desired = DesiredControlAxes();

                bool restore = _replayControlInputs;

                _replayControlInputs = false;

                foreach (var axis in _issuedControlAxes.Keys.ToArray())
                {
                    if (desired.ContainsKey(axis) && desired[axis].Equals(_issuedControlAxes[axis]))
                    {
                        continue;
                    }

                    await StopControlAxis(axis);

                    if (epoch != _inputEpoch)
                    {
                        return;
                    }

                    _issuedControlAxes.Remove(axis);

                    // 2026-10-08: 에이전트에 따라 특정 축 정지 시 다른 축까지 초기화될 수 있다.
                    // 정지 이후에는 현재 누르고 있는 축만 복원한다.
                    restore = true;
                }

                foreach (var item in DesiredControlAxes())
                {
                    Tuple<int, int> previous;

                    if (!restore && _issuedControlAxes.TryGetValue(item.Key, out previous) && previous.Equals(item.Value))
                    {
                        continue;
                    }

                    if (epoch != _inputEpoch || _context.IsControlInputLocked || !_context.IsDeviceConnectionRequested)
                    {
                        return;
                    }

                    if (!DesiredControlAxes().TryGetValue(item.Key, out var heldNow) || !heldNow.Equals(item.Value))
                    {
                        continue;
                    }

                    await StartControlAxis(item.Key, item.Value.Item1, item.Value.Item2);

                    if (epoch != _inputEpoch)
                    {
                        _context.StopAllPtzfMove();

                        return;
                    }

                    _issuedControlAxes[item.Key] = item.Value;

                    if (!DesiredControlAxes().TryGetValue(item.Key, out heldNow) || !heldNow.Equals(item.Value))
                    {
                        await StopControlAxis(item.Key);

                        _issuedControlAxes.Remove(item.Key);

                        _replayControlInputs = true;

                        ReconcileControlInputs();

                        continue;
                    }

                    ConsoleLogHelper.Command("PTZF INPUT", "AXIS=" + item.Key + " / X=" + item.Value.Item1 + " / Y=" + item.Value.Item2 + " / RESTORE=" + restore);
                }
            }
            catch (Exception ex)
            {
                ReleaseAllControlInputs("Input failure");

                ConsoleLogHelper.Warning("PTZF INPUT", ex.GetType().Name + " / HRESULT=0x" + ex.HResult.ToString("X8"));
            }
            finally
            {
                _inputReconcile.Release();
            }
        }

        /// <summary>
        /// 축 코드별 기존 PT/줌/포커스 시작 함수를 호출하고 EO 제어 소스를 보존한다.
        /// </summary>
        internal async Task StartControlAxis(string axis, int x, int y)
        {
            switch (axis)
            {
                case "PT":
                    if (x < 0 && y > 0)
                    {
                        _context.StartPanLeftTiltUpMove();
                    }
                    else if (x > 0 && y > 0)
                    {
                        _context.StartPanRightTiltUpMove();
                    }
                    else if (x < 0 && y < 0)
                    {
                        _context.StartPanLeftTiltDownMove();
                    }
                    else if (x > 0 && y < 0)
                    {
                        _context.StartPanRightTiltDownMove();
                    }
                    else if (x < 0)
                    {
                        _context.StartPanLeftMove();
                    }
                    else if (x > 0)
                    {
                        _context.StartPanRightMove();
                    }
                    else if (y > 0)
                    {
                        _context.StartTiltUpMove();
                    }
                    else if (y < 0)
                    {
                        _context.StartTiltDownMove();
                    }

                    break;
                case "EZ":
                    if (x > 0)
                    {
                        await _context.StartEoZoomInMoveAsync();
                    }
                    else
                    {
                        await _context.StartEoZoomOutMoveAsync();
                    }

                    _axisCtecSources[axis] = _context.ActiveEoCtecSource;
                    break;
                case "EF":
                    _context.CurrentMoveType = ContinuousMoveType.None;
                    if (x > 0)
                    {
                        await _context.StartEoFocusNearMoveAsync();
                    }
                    else
                    {
                        await _context.StartEoFocusFarMoveAsync();
                    }

                    _axisCtecSources[axis] = _context.ActiveEoCtecSource;
                    break;
                case "IZ":
                    if (x > 0)
                    {
                        _context.StartIrZoomInMove();
                    }
                    else
                    {
                        _context.StartIrZoomOutMove();
                    }

                    break;
                case "IF":
                    if (x > 0)
                    {
                        _context.StartIrFocusNearMove();
                    }
                    else
                    {
                        _context.StartIrFocusFarMove();
                    }

                    break;
            }
        }

        /// <summary>
        /// 해당 축만 정지하며 EO는 시작 당시 제어 소스로 정지 명령을 보낸다.
        /// </summary>
        internal async Task StopControlAxis(string axis)
        {
            switch (axis)
            {
                case "PT":
                    _context.StopPanTiltContinuousMove();
                    break;
                case "IZ":
                    _context.StopIrZoomMove();
                    break;
                case "IF":
                    _context.StopIrFocusMove();
                    break;
                case "EZ":
                case "EF":
                    _axisCtecSources.TryGetValue(axis, out _context.ActiveEoCtecSource);
                    _context.CurrentMoveType = axis == "EZ" ? ContinuousMoveType.EoZoom : ContinuousMoveType.EoFocus;

                    // 2026-10-08: 상태 응답 안정화를 기다리기 전에 현재 누름 축을 복원한다.
                    await _context.StopContinuousMoveAsync(false);

                    _axisCtecSources.Remove(axis);
                    break;
            }
        }

        #endregion

    }
}
