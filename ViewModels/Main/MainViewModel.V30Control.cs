using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Configuration;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        // 2026-10-08 V30_0: all input owners share one independent axis ledger.
        private readonly Dictionary<string, Tuple<string, int, int>> _controlInputs = new Dictionary<string, Tuple<string, int, int>>();
        private readonly Dictionary<string, Tuple<int, int>> _issuedControlAxes = new Dictionary<string, Tuple<int, int>>();
        private readonly SemaphoreSlim _inputReconcile = new SemaphoreSlim(1, 1);
        private bool _isFovSpeedEnabled = true, _isGlobalControlEnabled, _isVideoClickCenterEnabled = true;
        private bool _isZoomRatioOverlayVisible = true;
        private int _inputEpoch;
        private long _eoLensReceivedTicks, _irLensReceivedTicks;
        private DispatcherTimer _v30StatusTimer;
        private string _lastZoomNotification, _lastPowerNotification;
        private bool _replayControlInputs;
        private readonly Dictionary<string, RtspSourceOption> _axisCtecSources = new Dictionary<string, RtspSourceOption>();

        // 2026-10-13 V31_1: notification order/content unchanged; reuse immutable name tables.
        private static readonly string[] ZoomStatusPropertyNames =
        {
            nameof(EoZoomRatioOverlayText), nameof(IrZoomRatioOverlayText),
            nameof(RooftopEoZoomStatusText), nameof(RooftopIrZoomStatusText),
            nameof(EnvironmentEoZoomStatusText), nameof(EnvironmentIrZoomStatusText)
        };
        private static readonly string[] PowerStatusPropertyNames =
        {
            nameof(EoPowerControlStatusText), nameof(IrPowerControlStatusText),
            nameof(CurrentEoPowerStatusColor), nameof(CurrentIrPowerStatusColor),
            nameof(IsEoPowerOnEnabled), nameof(IsEoPowerOffEnabled),
            nameof(IsIrPowerOnEnabled), nameof(IsIrPowerOffEnabled)
        };

        /// <summary>연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다.</summary>
        private LensControlTuning ControlTuningFor(bool ir)
        {
            var profile = _requestedControlProfile ?? SelectedControlAgentProfile;
            var device = DeviceCatalog.Entries.FirstOrDefault(x => x.Label + " EO/IR" == profile?.DisplayName);
            return ControlTuning.Get(device?.Id, ir);
        }
        public bool IsFovSpeedEnabled
        {
            get => _isFovSpeedEnabled;
            set { if (_isFovSpeedEnabled == value) return; _isFovSpeedEnabled = value; OnPropertyChanged(); RefreshControlSpeed(); ConsoleLogHelper.State("FOV SPEED", value ? "GUI enabled / estimated ratio" : "GUI disabled / agent-side compensation may be used"); }
        }
        public bool IsGlobalControlEnabled
        {
            get => _isGlobalControlEnabled;
            set { if (_isGlobalControlEnabled == value) return; ReleaseAllControlInputs("Global control changed"); _isGlobalControlEnabled = value; OnPropertyChanged(); OnPropertyChanged(nameof(GlobalControlStatusText)); }
        }
        public bool IsVideoClickCenterEnabled
        {
            get => _isVideoClickCenterEnabled;
            set { if (_isVideoClickCenterEnabled == value) return; _isVideoClickCenterEnabled = value; OnPropertyChanged(); ConsoleLogHelper.State("VIDEO CLICK CENTER",value?"Enabled":"Disabled"); }
        }
        /// <summary>2026-10-14 V31_6: 모든 EO/IR 메인·서브·분리 영상의 배율 배지만 표시/숨긴다. 제어값은 바꾸지 않는다.</summary>
        public bool IsZoomRatioOverlayVisible
        {
            get => _isZoomRatioOverlayVisible;
            set { if (_isZoomRatioOverlayVisible == value) return; _isZoomRatioOverlayVisible = value; OnPropertyChanged(); }
        }
        public string GlobalControlStatusText => IsGlobalControlEnabled ? "GLOBAL PTZF ON" : "GLOBAL PTZF OFF";
        public double EoOpticalRatioMinimum => ControlTuningFor(false).MinimumRatio;
        public double EoOpticalRatioMaximum => ControlTuningFor(false).MaximumRatio;
        public string ZoomOpticalStandardText => $"EO {ControlTuningFor(false).MaximumRatio:0.#}x / IR {ControlTuningFor(true).MaximumRatio:0.#} OPTICAL (임시 환산)";
        public string ZoomOpticalRangeText => $"EO {ControlTuningFor(false).MinimumRatio:0.#}~{ControlTuningFor(false).MaximumRatio:0.#}x / IR {ControlTuningFor(true).MinimumRatio:0.#}~{ControlTuningFor(true).MaximumRatio:0.#}x 동일 진행률";
        public double EoSpeedExponent { get => ControlTuningFor(false).Exponent; set => SetExponent(false, value); }
        public double IrSpeedExponent { get => ControlTuningFor(true).Exponent; set => SetExponent(true, value); }
        public int EoMinimumSpeed { get => ControlTuningFor(false).MinimumSpeed; set => SetMinimum(false, value); }
        public int IrMinimumSpeed { get => ControlTuningFor(true).MinimumSpeed; set => SetMinimum(true, value); }
        /// <summary>유효 범위의 채널별 감속 지수를 반영하고 현재 PT 속도를 갱신한다.</summary>
        private void SetExponent(bool ir, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 1 || value > 4) return;
            ControlTuningFor(ir).Exponent = value; OnPropertyChanged(ir ? nameof(IrSpeedExponent) : nameof(EoSpeedExponent)); RefreshControlSpeed();
        }
        /// <summary>채널별 최소 PT 속도를 1~60 범위에서 갱신한다.</summary>
        private void SetMinimum(bool ir, int value)
        {
            if (value < 1 || value > 60) return;
            ControlTuningFor(ir).MinimumSpeed = value; OnPropertyChanged(ir ? nameof(IrMinimumSpeed) : nameof(EoMinimumSpeed)); RefreshControlSpeed();
        }
        /// <summary>Z/C 반복 입력을 포함하여 공통 설정 속도를 1~60으로 제한해 증감한다.</summary>
        public void AdjustControlSpeed(int delta) => PanTiltSpeedLevel = (byte)Math.Max(1, Math.Min(60, PanTiltSpeedLevel + delta));
        /// <summary>표시 속도를 갱신하고 이미 동작 중인 PT 축에만 변경 속도를 재적용한다.</summary>
        private void RefreshControlSpeed()
        {
            OnPropertyChanged(nameof(EffectivePanTiltSpeedLevel)); OnPropertyChanged(nameof(PanTiltSpeedDisplayText));
            if (_issuedControlAxes.ContainsKey("PT")) { _replayControlInputs = true; ReconcileControlInputs(); }
        }
        private bool IsLensFresh(bool ir) => _isDeviceConnectionRequested && DateTime.UtcNow.Ticks - (ir ? Interlocked.Read(ref _irLensReceivedTicks) : Interlocked.Read(ref _eoLensReceivedTicks)) < TimeSpan.FromSeconds(3).Ticks;
        public string EoZoomRatioOverlayText => IsLensFresh(false) ? "EO x" + EoDisplayZoomRatio.ToString("F1") : "EO —";
        public string IrZoomRatioOverlayText => IsLensFresh(true) ? "IR x" + IrDisplayZoomRatio.ToString("F1") : "IR —";
        private string ZoomRatioStatus(bool ir) => (ir ? GetCurrentIrZoomStandardPosition() : GetCurrentPresetStandardZoom()) + " / 1000 (" + (IsLensFresh(ir) ? "x" + (ir ? IrDisplayZoomRatio : EoDisplayZoomRatio).ToString("F1") : "배율 미확인") + ")";
        private string _lastOpticalSpeedSignature;
        private void MarkLensEvidence(bool ir) { if (ir) Interlocked.Exchange(ref _irLensReceivedTicks, DateTime.UtcNow.Ticks); else Interlocked.Exchange(ref _eoLensReceivedTicks, DateTime.UtcNow.Ticks); }
        /// <summary>250ms 상태 표시 감시를 한 번만 시작한다. 디지털 추정으로 PT를 재송신하지 않는다.</summary>
        public void InitializeV30Status()
        {
            if (_v30StatusTimer != null) return;
            _v30StatusTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(250) };
            _v30StatusTimer.Tick += (s, e) =>
            {
                TickDigitalZoom();
                TickSpecialFeatures();
                if(AiPowerStatusText=="ON" && !_aiDetectorClientService.IsConnected)
                { AiPowerStatusText="OFF"; ClearAiDetectionState("AI transport lost"); SetFireSmokeDetectorsForAiConnection(false,"AI transport lost"); }
                TickDigitalEstimate();
                string zoom = EoZoomRatioOverlayText + IrZoomRatioOverlayText;
                if (zoom != _lastZoomNotification)
                {
                    _lastZoomNotification = zoom;
                    foreach (var name in ZoomStatusPropertyNames) OnPropertyChanged(name);
                    // Display-only digital estimates must not trigger PT command replays.
                    string optical=GetCurrentPresetStandardZoom()+"/"+GetCurrentIrZoomStandardPosition()+"/"+IsLensFresh(false)+"/"+IsLensFresh(true);
                    if(optical!=_lastOpticalSpeedSignature){_lastOpticalSpeedSignature=optical;RefreshControlSpeed();}
                }
                string power = EoPowerControlStatusText + "/" + IrPowerControlStatusText;
                if (power != _lastPowerNotification)
                {
                    _lastPowerNotification = power;
                    foreach (var name in PowerStatusPropertyNames) OnPropertyChanged(name);
                    ConsoleLogHelper.State("POWER EVIDENCE", power + " / RTSP loss is not proof of OFF");
                }
                if ((!_isDeviceConnectionRequested || !_laTcpService.IsConnected) && _controlInputs.Count != 0) ReleaseAllControlInputs("Disconnected");
            };
            _v30StatusTimer.Start();
        }
        /// <summary>상태 타이머를 중지하고 종료 시 남은 제어 입력을 해제한다.</summary>
        public void ShutdownV30Control() { StopRoute(); _v30StatusTimer?.Stop(); ReleaseAllControlInputs("Shutdown"); }

        /// <summary>입력 소유자/채널별 키를 축 명령으로 변환하고 키 해제 및 반복을 처리한다.</summary>
        public bool HandleControlKey(string owner, Key key, bool down, bool ir, bool repeat = false)
        {
            string token = owner + "/" + key;
            if (!down) { if (_controlInputs.ContainsKey(token)) { ReleaseControlInput(token); return true; } return false; }
            if (key == Key.Z || key == Key.C) { AdjustControlSpeed(key == Key.Z ? -1 : 1); return true; }
            if (repeat) return key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down ||
                key == Key.W || key == Key.S || key == Key.A || key == Key.D || key == Key.Z || key == Key.C;
            switch (key)
            {
                case Key.Left: PressControlInput(token, "PT", -1, 0, ir); break;
                case Key.Right: PressControlInput(token, "PT", 1, 0, ir); break;
                case Key.Up: PressControlInput(token, "PT", 0, 1, ir); break;
                case Key.Down: PressControlInput(token, "PT", 0, -1, ir); break;
                case Key.W: PressControlInput(token, ir ? "IZ" : "EZ", 1, 0, ir); break;
                case Key.S: PressControlInput(token, ir ? "IZ" : "EZ", -1, 0, ir); break;
                case Key.A: PressControlInput(token, ir ? "IF" : "EF", ir ? -1 : 1, 0, ir); break;
                case Key.D: PressControlInput(token, ir ? "IF" : "EF", ir ? 1 : -1, 0, ir); break;
                default: return false;
            }
            return true;
        }
        /// <summary>연결/잠금 검증 후 누름 입력을 등록하며 수동 입력은 절대 위치 이동보다 우선한다.</summary>
        public void PressControlInput(string token, string axis, int x, int y, bool ir)
        {
            // 2026-10-12: rejected GUI clicks must be diagnosable, not silently ignored.
            string blocked = IsControlInputLocked ? "CONTROL_LOCK" : !IsOperationCommandEnabled ? "OPERATION_LOCK" : !_isDeviceConnectionRequested ? "NO_REQUESTED_CONNECTION" : !_laTcpService.IsConnected ? "TCP_DISCONNECTED" : null;
            if (blocked != null) { ConsoleLogHelper.Warning("PTZF INPUT", "REJECTED / OWNER=" + token + " / AXIS=" + axis + " / CAMERA=" + (ir ? "IR" : "EO") + " / REASON=" + blocked); return; }
            if (_controlInputs.ContainsKey(token)) return;
            // 2026-10-12: manual input supersedes queued/active click-to-center absolute movement.
            if (_videoCenterBusy || _activePanAbsoluteTarget.HasValue || _activeTiltAbsoluteTarget.HasValue)
            { ++_inputEpoch; _controlCommandService.StopPanTiltPositionMove(); ClearActivePanTiltAbsoluteMove(); }
            SetActivePanTiltZoomSource(ir);
            _controlInputs[token] = Tuple.Create(axis, x, y);
            ConsoleLogHelper.Command("PTZF INPUT", "PRESS / OWNER=" + token + " / AXIS=" + axis + " / CAMERA=" + (ir ? "IR" : "EO") + " / X=" + x + " / Y=" + y);
            ReconcileControlInputs();
        }
        /// <summary>해당 토큰만 해제하고 다른 소유자가 누르고 있는 축은 유지한다.</summary>
        public void ReleaseControlInput(string token) { if (_controlInputs.Remove(token)) { ConsoleLogHelper.Command("PTZF INPUT", "RELEASE / OWNER=" + token); ReconcileControlInputs(); } }
        /// <summary>지정 창/입력 소유자의 토큰만 일괄 해제한다.</summary>
        public void ReleaseControlOwner(string owner)
        {
            string prefix = owner + "/";
            foreach (var token in _controlInputs.Keys.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToArray()) _controlInputs.Remove(token);
            ReconcileControlInputs();
        }
        /// <summary>입력 세대를 갱신해 대기 명령을 무효화하고 필요할 때 전체 PTZF를 정지한다.</summary>
        public void ReleaseAllControlInputs(string reason)
        {
            ++_inputEpoch; bool active = _controlInputs.Count != 0 || _issuedControlAxes.Count != 0 || _videoCenterBusy || _activePanAbsoluteTarget.HasValue || _activeTiltAbsoluteTarget.HasValue;
            _controlInputs.Clear(); _issuedControlAxes.Clear();
            if (active) { StopAllPtzfMove(); ConsoleLogHelper.State("INPUT STOP", reason); }
            else _axisCtecSources.Clear();
        }
        /// <summary>같은 축의 누름 방향을 합산하고 서로 상쇄된 축은 제외한다.</summary>
        internal Dictionary<string, Tuple<int, int>> DesiredControlAxes()
        {
            var result = new Dictionary<string, Tuple<int, int>>();
            if (_controlInputs.Count == 0) return result;
            foreach (var group in _controlInputs.Values.GroupBy(x => x.Item1))
            {
                int x = Math.Sign(group.Sum(v => v.Item2)), y = Math.Sign(group.Sum(v => v.Item3));
                if (x != 0 || y != 0) result[group.Key] = Tuple.Create(x, y);
            }
            return result;
        }
        /// <summary>축 변경을 직렬 송신한다. await 이후에는 세대와 최신 누름 상태를 다시 검증한다.</summary>
        private async void ReconcileControlInputs()
        {
            await _inputReconcile.WaitAsync();
            int epoch = _inputEpoch;
            try
            {
                if (IsControlInputLocked || !_isDeviceConnectionRequested) { ReleaseAllControlInputs("Input not available"); return; }
                var desired = DesiredControlAxes(); bool restore = _replayControlInputs; _replayControlInputs = false;
                foreach (var axis in _issuedControlAxes.Keys.ToArray())
                {
                    if (desired.ContainsKey(axis) && desired[axis].Equals(_issuedControlAxes[axis])) continue;
                    await StopControlAxis(axis);
                    if (epoch != _inputEpoch) return;
                    _issuedControlAxes.Remove(axis);
                    // 2026-10-08: agent versions may reset other axes on a scoped
                    // STOP too. Restore only currently held axes after every stop.
                    restore = true;
                }
                foreach (var item in DesiredControlAxes())
                {
                    Tuple<int, int> previous;
                    if (!restore && _issuedControlAxes.TryGetValue(item.Key, out previous) && previous.Equals(item.Value)) continue;
                    if (epoch != _inputEpoch || IsControlInputLocked || !_isDeviceConnectionRequested) return;
                    if (!DesiredControlAxes().TryGetValue(item.Key, out var heldNow) || !heldNow.Equals(item.Value)) continue;
                    await StartControlAxis(item.Key, item.Value.Item1, item.Value.Item2);
                    if (epoch != _inputEpoch) { StopAllPtzfMove(); return; }
                    _issuedControlAxes[item.Key] = item.Value;
                    if (!DesiredControlAxes().TryGetValue(item.Key, out heldNow) || !heldNow.Equals(item.Value))
                    {
                        await StopControlAxis(item.Key); _issuedControlAxes.Remove(item.Key);
                        _replayControlInputs = true; ReconcileControlInputs(); continue;
                    }
                    ConsoleLogHelper.Command("PTZF INPUT", "AXIS=" + item.Key + " / X=" + item.Value.Item1 + " / Y=" + item.Value.Item2 + " / RESTORE=" + restore);
                }
            }
            catch (Exception ex) { ReleaseAllControlInputs("Input failure"); ConsoleLogHelper.Warning("PTZF INPUT", ex.GetType().Name + " / HRESULT=0x" + ex.HResult.ToString("X8")); }
            finally { _inputReconcile.Release(); }
        }
        /// <summary>축 코드별 기존 PT/줌/포커스 시작 함수를 호출하고 EO 제어 소스를 보존한다.</summary>
        private async Task StartControlAxis(string axis, int x, int y)
        {
            switch (axis)
            {
                case "PT":
                    if (x < 0 && y > 0) StartPanLeftTiltUpMove(); else if (x > 0 && y > 0) StartPanRightTiltUpMove();
                    else if (x < 0 && y < 0) StartPanLeftTiltDownMove(); else if (x > 0 && y < 0) StartPanRightTiltDownMove();
                    else if (x < 0) StartPanLeftMove(); else if (x > 0) StartPanRightMove(); else if (y > 0) StartTiltUpMove(); else if (y < 0) StartTiltDownMove(); break;
                case "EZ": if (x > 0) await StartEoZoomInMoveAsync(); else await StartEoZoomOutMoveAsync(); _axisCtecSources[axis] = _activeEoCtecSource; break;
                case "EF": _currentMoveType = ContinuousMoveType.None; if (x > 0) await StartEoFocusNearMoveAsync(); else await StartEoFocusFarMoveAsync(); _axisCtecSources[axis] = _activeEoCtecSource; break;
                case "IZ": if (x > 0) StartIrZoomInMove(); else StartIrZoomOutMove(); break;
                case "IF": if (x > 0) StartIrFocusNearMove(); else StartIrFocusFarMove(); break;
            }
        }
        /// <summary>해당 축만 정지하며 EO는 시작 당시 제어 소스로 정지 명령을 보낸다.</summary>
        private async Task StopControlAxis(string axis)
        {
            switch (axis)
            {
                case "PT": StopPanTiltContinuousMove(); break;
                case "IZ": StopIrZoomMove(); break;
                case "IF": StopIrFocusMove(); break;
                case "EZ":
                case "EF":
                    _axisCtecSources.TryGetValue(axis, out _activeEoCtecSource);
                    _currentMoveType = axis == "EZ" ? ContinuousMoveType.EoZoom : ContinuousMoveType.EoFocus;
                    // 2026-10-08: restore held axes before telemetry-settling waits.
                    await StopContinuousMoveAsync(false); _axisCtecSources.Remove(axis); break;
            }
        }
    }
}
