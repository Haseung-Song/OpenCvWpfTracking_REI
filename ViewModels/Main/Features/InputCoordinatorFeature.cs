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
        ref global::OpenCvWpfTracking.Models.Main.RtspSourceOption _activeEoCtecSource { get; }

        ref double? _activePanAbsoluteTarget { get; }

        ref double? _activeTiltAbsoluteTarget { get; }

        Services.Communication.AI.IAiDetectorClientServicePort _aiDetectorClientService { get; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        ContinuousMoveType _currentMoveType { get; set; }

        bool _isDeviceConnectionRequested { get; set; }

        Services.Communication.ITcpClientServicePort _laTcpService { get; }

        ControlAgentProfileOption _requestedControlProfile { get; set; }

        bool _videoCenterBusy { get; set; }

        string AiPowerStatusText { get; set; }

        void ClearActivePanTiltAbsoluteMove();

        void ClearAiDetectionState(string reason, bool writeAudit = true);

        byte EffectivePanTiltSpeedLevel { get; }

        double EoDisplayZoomRatio { get; }

        string EoPowerControlStatusText { get; }

        int GetCurrentIrZoomStandardPosition();

        ushort GetCurrentPresetStandardZoom();

        double IrDisplayZoomRatio { get; }

        string IrPowerControlStatusText { get; }

        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsControlInputLocked { get; }

        bool IsOperationCommandEnabled { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanTiltSpeedDisplayText { get; }

        byte PanTiltSpeedLevel { get; set; }

        void ResetAiRtspVideoSizes();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        void SetActivePanTiltZoomSource(bool isInfrared);

        void SetFireSmokeDetectorsForAiConnection(bool enabled, string reason);

        Task StartEoFocusFarMoveAsync();

        Task StartEoFocusNearMoveAsync();

        Task StartEoZoomInMoveAsync();

        Task StartEoZoomOutMoveAsync();

        void StartIrFocusFarMove();

        void StartIrFocusNearMove();

        void StartIrZoomInMove();

        void StartIrZoomOutMove();

        void StartPanLeftMove();

        void StartPanLeftTiltDownMove();

        void StartPanLeftTiltUpMove();

        void StartPanRightMove();

        void StartPanRightTiltDownMove();

        void StartPanRightTiltUpMove();

        void StartTiltDownMove();

        void StartTiltUpMove();

        void StopAllPtzfMove();

        Task StopContinuousMoveAsync(bool waitForStatus = true);

        void StopIrFocusMove();

        void StopIrZoomMove();

        void StopPanTiltContinuousMove();

        void StopRoute();

        void TickDigitalEstimate();

        void TickDigitalZoom();

        void TickSpecialFeatures();
    }

    /// <summary>입력 허용과 정지 조정 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class InputCoordinatorFeature
    {

        #region [협력 객체와 초기화]

        private readonly IInputCoordinatorFeatureContext _context;

        internal InputCoordinatorFeature(IInputCoordinatorFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

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

        internal LensControlTuning ControlTuningFor(bool ir)
        {
            var profile = _context._requestedControlProfile ?? _context.SelectedControlAgentProfile;

            return ControlTuning.Get(profile?.DeviceId, ir);
        }

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

        internal void AdjustControlSpeed(int delta)
        {
            _context.PanTiltSpeedLevel = (byte)Math.Max(1, Math.Min(60, _context.PanTiltSpeedLevel + delta));
        }

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

        internal bool IsLensFresh(bool ir)
        {
            return _context._isDeviceConnectionRequested && DateTime.UtcNow.Ticks - (ir ? Interlocked.Read(ref _irLensReceivedTicks) : Interlocked.Read(ref _eoLensReceivedTicks)) < TimeSpan.FromSeconds(3).Ticks;
        }

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

        internal string ZoomRatioStatus(bool ir)
        {
            return (ir ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom()) + " / 1000 (" + (IsLensFresh(ir) ? DisplayZoomRatioText(ir) : "배율 미확인") + ")";
        }

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

                if (_context.AiPowerStatusText == "ON" && !_context._aiDetectorClientService.IsConnected)
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

                if ((!_context._isDeviceConnectionRequested || !_context._laTcpService.IsConnected) && _controlInputs.Count != 0)
                {
                    ReleaseAllControlInputs("Disconnected");
                }
            };

            _controlStatusTimer.Start();
        }

        internal void ShutdownControlInputMonitor()
        {
            _context.StopRoute();

            _controlStatusTimer?.Stop();

            ReleaseAllControlInputs("Shutdown");
        }

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

        internal void PressControlInput(string token, string axis, int x, int y, bool ir)
        {
            if ((axis == "EF" && _context.IsAutoFocusBlockingManualFocus(false)) || (axis == "IF" && _context.IsAutoFocusBlockingManualFocus(true)))
            {
                return;
            }

            // 2026-10-12: 거절한 화면 입력은 원인을 진단할 수 있도록 기록한다.
            string blocked = _context.IsControlInputLocked ? "CONTROL_LOCK" : !_context.IsOperationCommandEnabled ? "OPERATION_LOCK" : !_context._isDeviceConnectionRequested ? "NO_REQUESTED_CONNECTION" : !_context._laTcpService.IsConnected ? "TCP_DISCONNECTED" : null;

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
            if (_context._videoCenterBusy || _context._activePanAbsoluteTarget.HasValue || _context._activeTiltAbsoluteTarget.HasValue)
            {
                ++_inputEpoch;

                _context._controlCommandService.StopPanTiltPositionMove();

                _context.ClearActivePanTiltAbsoluteMove();
            }

            _context.SetActivePanTiltZoomSource(ir);

            _controlInputs[token] = Tuple.Create(axis, x, y);

            ConsoleLogHelper.Command("PTZF INPUT", "PRESS / OWNER=" + token + " / AXIS=" + axis + " / CAMERA=" + (ir ? "IR" : "EO") + " / X=" + x + " / Y=" + y);

            ReconcileControlInputs();
        }

        internal void ReleaseControlInput(string token)
        {
            if (_controlInputs.Remove(token))
            {
                ConsoleLogHelper.Command("PTZF INPUT", "RELEASE / OWNER=" + token);

                ReconcileControlInputs();
            }
        }

        internal void ReleaseControlOwner(string owner)
        {
            string prefix = owner + "/";

            foreach (var token in _controlInputs.Keys.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                _controlInputs.Remove(token);
            }

            ReconcileControlInputs();
        }

        internal void ReleaseAllControlInputs(string reason)
        {
            ++_inputEpoch;

            bool active = _controlInputs.Count != 0 || _issuedControlAxes.Count != 0 || _context._videoCenterBusy || _context._activePanAbsoluteTarget.HasValue || _context._activeTiltAbsoluteTarget.HasValue;

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

        internal async void ReconcileControlInputs()
        {
            await _inputReconcile.WaitAsync();

            int epoch = _inputEpoch;

            try
            {
                if (_context.IsControlInputLocked || !_context._isDeviceConnectionRequested)
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

                    if (epoch != _inputEpoch || _context.IsControlInputLocked || !_context._isDeviceConnectionRequested)
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

                    _axisCtecSources[axis] = _context._activeEoCtecSource;
                    break;
                case "EF":
                    _context._currentMoveType = ContinuousMoveType.None;
                    if (x > 0)
                    {
                        await _context.StartEoFocusNearMoveAsync();
                    }
                    else
                    {
                        await _context.StartEoFocusFarMoveAsync();
                    }

                    _axisCtecSources[axis] = _context._activeEoCtecSource;
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
                    _axisCtecSources.TryGetValue(axis, out _context._activeEoCtecSource);
                    _context._currentMoveType = axis == "EZ" ? ContinuousMoveType.EoZoom : ContinuousMoveType.EoFocus;

                    // 2026-10-08: 상태 응답 안정화를 기다리기 전에 현재 누름 축을 복원한다.
                    await _context.StopContinuousMoveAsync(false);

                    _axisCtecSources.Remove(axis);
                    break;
            }
        }

        #endregion

    }
}
