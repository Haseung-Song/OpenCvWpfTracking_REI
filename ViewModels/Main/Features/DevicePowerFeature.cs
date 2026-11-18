using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>장비 전원 상태에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IDevicePowerFeatureContext
    {
        IControlCommandServicePort _controlCommandService { get; }

        string _eoStatusText { get; set; }

        string _irStatusText { get; set; }

        ITcpClientServicePort _laTcpService { get; }

        ControlAgentProfileOption _requestedControlProfile { get; set; }

        string ControlAgentConnectionStatusText { get; set; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        string CurrentControlPowerText { get; }

        string CurrentEoPowerText { get; }

        string CurrentIrPowerText { get; }

        string CurrentPowerText { get; }

        string EoSourceAddress { get; set; }

        string IrSourceAddress { get; set; }

        bool IsGpsTabVisible { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsSelectedConnectionTarget { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        void RefreshWebAgentEquipmentState();

        void ResetWebAgentTelemetrySession();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        string SessionControlIp { get; }

        string SessionControlPort { get; }

        EventHandler HandleDevicePowerFreshnessTickCallback { get; }
    }

    /// <summary>장비 전원 상태 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class DevicePowerFeature
    {

        #region [협력 객체와 초기화]

        private readonly IDevicePowerFeatureContext _context;

        internal DevicePowerFeature(IDevicePowerFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _connectedControlAgentEndpoint = "-";
            _connectionSessionId = "-";
        }

        #endregion

        #region [상태 필드]

        internal bool _hasDevicePowerStatus;

        internal byte _currentMcbPowerStatus;

        internal byte _currentScbPowerStatus;

        internal DateTime _devicePowerStatusReceivedAt;

        internal bool _isDevicePowerStatusStale;

        internal DispatcherTimer _devicePowerFreshnessTimer;

        internal ControlAgentProfileOption _connectedControlAgentProfile;

        internal string _connectedControlAgentEndpoint;

        internal string _connectionSessionId;

        internal bool _eoPowerCommandPending;

        internal bool _irPowerCommandPending;

        internal bool _eoPowerCommandTargetOn;

        internal bool _irPowerCommandTargetOn;

        internal DateTime _eoPowerCommandSentAt;

        internal DateTime _irPowerCommandSentAt;

        internal readonly Dictionary<string, string[]> _lastCameraPower = new Dictionary<string, string[]>();

        #endregion

        #region [화면 속성과 이벤트]

        internal string PowerTargetKey
        {
            get
            {
                return _context.SelectedControlAgentProfile.DisplayName + "|" + _context.ControlAgentIp + "|" + _context.ControlAgentPortText + "|" + _context.EoSourceAddress + "|" + _context.IrSourceAddress;
            }
        }

        internal string CameraPowerStateHelpText
        {
            get
            {
                return "전원 상태와 영상 연결 상태는 다릅니다. 연결 해제는 POWER OFF가 아닙니다.\n" + "통신 해제/응답 지연 시 전원은 UNKNOWN입니다. 장비에서 다시 상태를 받으면 갱신합니다.\n" + "비기동형은 영상 수신으로 ON만 확인합니다. 명시적 전원 응답이 없으면 OFF는 확정하지 않습니다.";
            }
        }

        internal ICommand EoPowerOnCommand { get; set; }

        internal ICommand EoPowerOffCommand { get; set; }

        internal ICommand IrPowerOnCommand { get; set; }

        internal ICommand IrPowerOffCommand { get; set; }

        internal string DevicePowerStatusText
        {
            get
            {
                if (!_hasDevicePowerStatus)
                {
                    return "WAITING FOR FUNCTION 0x31";
                }
                else
                {
                    return _isDevicePowerStatusStale ? "FUNCTION 0x31 STALE" : string.Format("MCB 0x{0:X2} / SCB 0x{1:X2} / RX {2:HH:mm:ss.fff}", _currentMcbPowerStatus, _currentScbPowerStatus, _devicePowerStatusReceivedAt);
                }
            }
        }

        internal string DevicePowerFreshnessText
        {
            get
            {
                if (!_context.IsGpsTabVisible)
                {
                    return "LEGACY";
                }
                else
                {
                    return !_hasDevicePowerStatus ? "WAITING" : _isDevicePowerStatusStale ? "STALE" : "FRESH";
                }
            }
        }

        internal string DeviceControlConnectionStatusText
        {
            get
            {
                if (string.Equals(_context.ControlAgentConnectionStatusText, "Connected", StringComparison.OrdinalIgnoreCase))
                {
                    return "Connected";
                }
                else
                {
                    return "Disconnected";
                }
            }
        }

        internal string CurrentGpsPowerText
        {
            get
            {
                if (_hasDevicePowerStatus && !_isDevicePowerStatusStale)
                {
                    return ToOnOff((_currentScbPowerStatus & 0x02) != 0);
                }
                else
                {
                    return "UNKNOWN";
                }
            }
        }

        internal string EoPowerControlStatusText
        {
            get
            {
                if (_eoPowerCommandPending)
                {
                    return "WAIT";
                }
                else
                {
                    return GetCameraPowerControlText("EO");
                }
            }
        }

        internal string IrPowerControlStatusText
        {
            get
            {
                if (_irPowerCommandPending)
                {
                    return "WAIT";
                }
                else
                {
                    return GetCameraPowerControlText("IR");
                }
            }
        }

        internal string CurrentEoPowerStatusColor
        {
            get
            {
                return GetPowerStatusColor(EoPowerControlStatusText);
            }
        }

        internal string CurrentIrPowerStatusColor
        {
            get
            {
                return GetPowerStatusColor(IrPowerControlStatusText);
            }
        }

        internal bool IsEoPowerControlEnabled
        {
            get { return _context.IsOperationCommandEnabled; }
        }

        internal bool IsIrPowerControlEnabled
        {
            get { return _context.IsOperationCommandEnabled; }
        }

        internal bool IsEoPowerOnEnabled
        {
            get
            {
                return IsEoPowerControlEnabled && EoPowerControlStatusText != "ON";
            }
        }

        internal bool IsEoPowerOffEnabled
        {
            get
            {
                return IsEoPowerControlEnabled && EoPowerControlStatusText != "OFF";
            }
        }

        internal bool IsIrPowerOnEnabled
        {
            get
            {
                return IsIrPowerControlEnabled && IrPowerControlStatusText != "ON";
            }
        }

        internal bool IsIrPowerOffEnabled
        {
            get
            {
                return IsIrPowerControlEnabled && IrPowerControlStatusText != "OFF";
            }
        }

        internal string DevicePowerDetailText
        {
            get
            {
                if (!_hasDevicePowerStatus)
                {
                    return "전원 상태 미수신 (OFF로 간주하지 않음)";
                }
                else
                {
                    return string.Format("DRIVER {0} / EO {1} / IR {2} / GPS {3}", ToOnOff((_currentMcbPowerStatus & 0x01) != 0), ToOnOff((_currentScbPowerStatus & 0x01) != 0), ToOnOff((_currentMcbPowerStatus & 0x20) != 0), ToOnOff((_currentScbPowerStatus & 0x02) != 0));
                }
            }
        }

        #endregion

        #region [동작 처리]

        internal void RememberCameraPower(string camera, bool on)
        {
            lock (_lastCameraPower)
            {
                if (!_lastCameraPower.TryGetValue(PowerTargetKey, out string[] values))
                {
                    _lastCameraPower[PowerTargetKey] = values = new[]
                    {
                        "UNKNOWN",
                        "UNKNOWN"
                    };
                }

                values[camera == "IR" ? 1 : 0] = on ? "ON" : "OFF";
            }
        }

        internal string GetCameraPowerControlText(string camera)
        {
            if (!_context.IsSelectedConnectionTarget)
            {
                return "UNKNOWN";
            }

            if (!_context.IsGpsTabVisible && (camera == "IR" ? _context._irStatusText : _context._eoStatusText) == "[" + camera + "] Connected")
            {
                return "ON";
            }

            if (_context.IsGpsTabVisible && _hasDevicePowerStatus && !_isDevicePowerStatusStale && _connectedControlAgentProfile == _context.SelectedControlAgentProfile && _connectedControlAgentEndpoint == _context.ControlAgentIp + ":" + _context.ControlAgentPortText)
            {
                if (camera == "IR")
                {
                    return ToOnOff((_currentMcbPowerStatus & 0x20) != 0);
                }
                else
                {
                    return ToOnOff((_currentScbPowerStatus & 0x01) != 0);
                }
            }

            // 오래된 전원 이력은 현재 전원 상태의 근거로 사용하지 않는다.
            // RTSP 단절만으로 실제 전원 꺼짐과 통신 장애를 구분할 수 없다.
            return "UNKNOWN";
        }

        internal static string GetPowerStatusColor(string value)
        {
            if (value == "ON")
            {
                return "#69E39B";
            }
            else
            {
                return value == "OFF" ? "#FF7B72" : "#FDE68A";
            }
        }

        internal static string GetLegacyCameraPowerText(string status, string camera)
        {
            if (string.Equals(status, "[" + camera + "] Connected", StringComparison.OrdinalIgnoreCase))
            {
                return "ON";
            }

            if (!string.IsNullOrEmpty(status) && (status.StartsWith("[" + camera + "] Connecting", StringComparison.OrdinalIgnoreCase) || status.StartsWith("[" + camera + "] Reconnecting", StringComparison.OrdinalIgnoreCase) || status.StartsWith("[" + camera + "] Waiting", StringComparison.OrdinalIgnoreCase)))
            {
                return "WAIT";
            }

            return "OFF";
        }

        internal void InitializeDevicePowerSettings()
        {
            EoPowerOnCommand = new RelayCommand(() => SendDevicePower(EoPowerDeviceCode, true, "EO"), () => IsEoPowerOnEnabled);

            EoPowerOffCommand = new RelayCommand(() => SendDevicePower(EoPowerDeviceCode, false, "EO"), () => IsEoPowerOffEnabled);

            IrPowerOnCommand = new RelayCommand(() => SendDevicePower(IrPowerDeviceCode, true, "IR"), () => IsIrPowerOnEnabled);

            IrPowerOffCommand = new RelayCommand(() => SendDevicePower(IrPowerDeviceCode, false, "IR"), () => IsIrPowerOffEnabled);

            _devicePowerFreshnessTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _devicePowerFreshnessTimer.Tick += _context.HandleDevicePowerFreshnessTickCallback;

            VerifyDevicePowerProtocol();
        }

        internal static void VerifyDevicePowerProtocol()
        {
            try
            {
                VerifyDevicePowerPacket("EO ON", 0x00, true, new byte[] { 0xFF, 0x01, 0x88, 0x00, 0x00, 0x00, 0x89 });

                VerifyDevicePowerPacket("EO OFF", 0x00, false, new byte[] { 0xFF, 0x01, 0x08, 0x00, 0x00, 0x00, 0x09 });

                VerifyDevicePowerPacket("IR ON", 0x01, true, new byte[] { 0xFF, 0x01, 0x88, 0x00, 0x01, 0x00, 0x8A });

                VerifyDevicePowerPacket("IR OFF", 0x01, false, new byte[] { 0xFF, 0x01, 0x08, 0x00, 0x01, 0x00, 0x0A });

                byte[] statusPacket =
                {
                    0xFF,
                    0x31,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x1E,
                    0x0C,
                    0x5B
                };

                LAPacketParser parser = new LAPacketParser();

                if (parser.Parse(statusPacket.Take(5).ToArray()).Count != 0)
                {
                    throw new InvalidOperationException("0x31 split prefix was emitted early.");
                }

                var parsed = parser.Parse(statusPacket.Skip(5).ToArray());

                if (parsed.Count != 1 || !parsed[0].IsValid || !parsed[0].IsDevicePowerStatus || parsed[0].RawData.Length != 13)
                {
                    throw new InvalidOperationException("0x31 fixed frame assembly failed.");
                }

                ConsoleLogHelper.State("DEVICE POWER SELF TEST", "EO/IR ON/OFF checksum + Function 0x31 split frame verified");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("DEVICE POWER SELF TEST", "Protocol verification failed", ex);
            }
        }

        internal static void VerifyDevicePowerPacket(string name, byte deviceCode, bool turnOn, byte[] expected)
        {
            byte[] actual = ControlCommandService.BuildDevicePowerPacket(deviceCode, turnOn);

            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidOperationException(name + " packet mismatch.");
            }
        }

        internal void SendDevicePower(byte deviceCode, bool turnOn, string deviceName)
        {
            if (!_context.IsSelectedConnectionTarget)
            {
                ConsoleLogHelper.Warning("DEVICE POWER", "Selected next target differs from active session / command not sent");

                return;
            }

            // 2026-10-06: 파노라마/AUTO SCAN/HOME/ZERO 동작 중 전원 명령 차단.
            if (!_context.IsOperationCommandEnabled)
            {
                ConsoleLogHelper.Warning("DEVICE POWER", deviceName + " " + (turnOn ? "ON" : "OFF") + " rejected / operation command lock is active");

                return;
            }

            if (_context._laTcpService == null || !_context._laTcpService.IsConnected)
            {
                ConsoleLogHelper.Warning("DEVICE POWER", deviceName + " " + (turnOn ? "ON" : "OFF") + " rejected / Control Agent is not connected");

                return;
            }

            if (_context._controlCommandService == null)
            {
                ConsoleLogHelper.Warning("DEVICE POWER", "EO/IR power control service is not initialized");

                return;
            }

            try
            {
                bool sent = _context._controlCommandService.SetDevicePower(deviceCode, turnOn);

                if (!sent)
                {
                    ConsoleLogHelper.Warning("DEVICE POWER", deviceName + " " + (turnOn ? "ON" : "OFF") + " send failed");

                    return;
                }

                ConsoleLogHelper.State("DEVICE POWER", deviceName + " " + (turnOn ? "ON" : "OFF") + " request sent / waiting for Function 0x31 board status");

                // 2026-10-06: LA/WebAgent 공통으로 명령 직후 노란 WAIT 상태를 표시한다.
                // 실제 상태 응답 또는 제한시간 경과 시 수신 상태 표시로 복귀한다.
                if (deviceCode == EoPowerDeviceCode)
                {
                    _eoPowerCommandPending = true;
                    _eoPowerCommandTargetOn = turnOn;
                    _eoPowerCommandSentAt = DateTime.Now;

                    _context.OnPropertyChanged(nameof(EoPowerControlStatusText));

                    _context.OnPropertyChanged(nameof(CurrentEoPowerStatusColor));
                }
                else
                {
                    _irPowerCommandPending = true;
                    _irPowerCommandTargetOn = turnOn;
                    _irPowerCommandSentAt = DateTime.Now;

                    _context.OnPropertyChanged(nameof(IrPowerControlStatusText));

                    _context.OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("DEVICE POWER", deviceName + " power command failed", ex);
            }
        }

        internal void ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime = null, string sourceFunction = "0x31")
        {
            string powerResponseSession = _connectionSessionId;

            bool shouldLog = !_hasDevicePowerStatus || _currentMcbPowerStatus != mcbStatus || _currentScbPowerStatus != scbStatus;

            _currentMcbPowerStatus = mcbStatus;
            _currentScbPowerStatus = scbStatus;

            _devicePowerStatusReceivedAt = sourceTime ?? DateTime.Now;

            _hasDevicePowerStatus = true;

            _isDevicePowerStatusStale = DateTime.Now - _devicePowerStatusReceivedAt > TimeSpan.FromSeconds(2);

            // 주기 응답에 이전 상태가 담겨 있어도 요청 목표에 도달할 때까지 WAIT 유지.
            if (!_isDevicePowerStatusStale && _devicePowerStatusReceivedAt >= _eoPowerCommandSentAt && _context.IsGpsTabVisible && _context.IsSelectedConnectionTarget && _eoPowerCommandPending && ((scbStatus & 0x01) != 0) == _eoPowerCommandTargetOn)
            {
                _eoPowerCommandPending = false;
            }

            if (!_isDevicePowerStatusStale && _devicePowerStatusReceivedAt >= _irPowerCommandSentAt && _context.IsGpsTabVisible && _context.IsSelectedConnectionTarget && _irPowerCommandPending && ((mcbStatus & 0x20) != 0) == _irPowerCommandTargetOn)
            {
                _irPowerCommandPending = false;
            }

            // 2026-10-06: 500 ms 상태 프레임의 로그 폭주를 막기 위해 최초 수신과
            // 상태 변화만 기록한다. 장비별 0x31 실제 수신 여부를 로그로 판별할 수 있다.
            if (shouldLog)
            {
                ConsoleLogHelper.State("DEVICE STATUS RX", string.Format("SESSION={0} / ACTIVE_ENDPOINT={1} / CONNECTED_PROFILE={2} / SELECTED_PROFILE={3} / FUNCTION={7} / MCB=0x{4:X2} / SCB=0x{5:X2} / RX={6:HH:mm:ss.fff}", _connectionSessionId, _connectedControlAgentEndpoint, _connectedControlAgentProfile?.DisplayName ?? "UNKNOWN", _context.SelectedControlAgentProfile?.DisplayName ?? "UNKNOWN", mcbStatus, scbStatus, _devicePowerStatusReceivedAt, sourceFunction));
            }

            Dispatcher dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.HasShutdownStarted)
            {
                return;
            }

            _ = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (!_isDevicePowerStatusStale && _context.IsGpsTabVisible && _context.IsSelectedConnectionTarget && powerResponseSession == _connectionSessionId && _connectedControlAgentProfile == _context.SelectedControlAgentProfile && _connectedControlAgentEndpoint == _context.ControlAgentIp + ":" + _context.ControlAgentPortText)
                {
                    RememberCameraPower("EO", (scbStatus & 0x01) != 0);

                    RememberCameraPower("IR", (mcbStatus & 0x20) != 0);
                }

                _context.OnPropertyChanged(nameof(DevicePowerStatusText));

                _context.OnPropertyChanged(nameof(DevicePowerFreshnessText));

                _context.OnPropertyChanged(nameof(DevicePowerDetailText));

                _context.OnPropertyChanged(nameof(_context.CurrentPowerText));

                _context.OnPropertyChanged(nameof(_context.CurrentControlPowerText));

                _context.OnPropertyChanged(nameof(_context.CurrentEoPowerText));

                _context.OnPropertyChanged(nameof(_context.CurrentIrPowerText));

                _context.OnPropertyChanged(nameof(CurrentGpsPowerText));

                _context.OnPropertyChanged(nameof(EoPowerControlStatusText));

                _context.OnPropertyChanged(nameof(IrPowerControlStatusText));

                _context.OnPropertyChanged(nameof(CurrentEoPowerStatusColor));

                _context.OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
            }));
        }

        internal void ResetDevicePowerStatus()
        {
            _hasDevicePowerStatus = false;
            _isDevicePowerStatusStale = false;
            _eoPowerCommandPending = false;
            _irPowerCommandPending = false;
            _currentMcbPowerStatus = 0;
            _currentScbPowerStatus = 0;
            _devicePowerStatusReceivedAt = DateTime.MinValue;

            _context.OnPropertyChanged(nameof(DevicePowerStatusText));

            _context.OnPropertyChanged(nameof(DevicePowerFreshnessText));

            _context.OnPropertyChanged(nameof(DevicePowerDetailText));

            _context.OnPropertyChanged(nameof(_context.CurrentPowerText));

            _context.OnPropertyChanged(nameof(_context.CurrentControlPowerText));

            _context.OnPropertyChanged(nameof(_context.CurrentEoPowerText));

            _context.OnPropertyChanged(nameof(_context.CurrentIrPowerText));

            _context.OnPropertyChanged(nameof(CurrentGpsPowerText));

            _context.OnPropertyChanged(nameof(CurrentEoPowerStatusColor));

            _context.OnPropertyChanged(nameof(CurrentIrPowerStatusColor));

            _context.OnPropertyChanged(nameof(EoPowerControlStatusText));

            _context.OnPropertyChanged(nameof(IrPowerControlStatusText));
        }

        internal void BeginConnectedControlAgentSession()
        {
            if (_connectedControlAgentProfile != null)
            {
                return;
            }

            _connectedControlAgentProfile = _context._requestedControlProfile ?? _context.SelectedControlAgentProfile;

            _connectedControlAgentEndpoint = _context.SessionControlIp + ":" + _context.SessionControlPort;

            _connectionSessionId = Guid.NewGuid().ToString("N").Substring(0, 12);

            _context.ResetWebAgentTelemetrySession();

            ResetDevicePowerStatus();

            _devicePowerFreshnessTimer?.Start();

            ConsoleLogHelper.State("CONTROL SESSION", "START / SESSION=" + _connectionSessionId + " / ENDPOINT=" + _connectedControlAgentEndpoint + " / PROFILE=" + (_connectedControlAgentProfile?.DisplayName ?? "UNKNOWN"));
        }

        internal void EndConnectedControlAgentSession()
        {
            _devicePowerFreshnessTimer?.Stop();

            _connectedControlAgentProfile = null;
            _connectedControlAgentEndpoint = "-";
            _connectionSessionId = "-";

            _context.ResetWebAgentTelemetrySession();

            ResetDevicePowerStatus();
        }

        internal void DevicePowerFreshnessTimer_Tick(object sender, EventArgs e)
        {
            _context.RefreshWebAgentEquipmentState();

            bool pendingChanged = false;

            if (_eoPowerCommandPending && ((!_context.IsGpsTabVisible && _context.IsSelectedConnectionTarget && _eoPowerCommandTargetOn && _context._eoStatusText == "[EO] Connected") || DateTime.Now - _eoPowerCommandSentAt >= DevicePowerCommandTimeout))
            {
                if (!_context.IsGpsTabVisible && _context.IsSelectedConnectionTarget && _eoPowerCommandTargetOn && _context.CurrentEoPowerText == "ON")
                {
                    RememberCameraPower("EO", _eoPowerCommandTargetOn);
                }

                if (DateTime.Now - _eoPowerCommandSentAt >= DevicePowerCommandTimeout)
                {
                    ConsoleLogHelper.Warning("DEVICE POWER", "EO confirmation timeout / SESSION=" + _connectionSessionId + " / ENDPOINT=" + _connectedControlAgentEndpoint);
                }

                _eoPowerCommandPending = false;
                pendingChanged = true;
            }

            if (_irPowerCommandPending && ((!_context.IsGpsTabVisible && _context.IsSelectedConnectionTarget && _irPowerCommandTargetOn && _context._irStatusText == "[IR] Connected") || DateTime.Now - _irPowerCommandSentAt >= DevicePowerCommandTimeout))
            {
                if (!_context.IsGpsTabVisible && _context.IsSelectedConnectionTarget && _irPowerCommandTargetOn && _context.CurrentIrPowerText == "ON")
                {
                    RememberCameraPower("IR", _irPowerCommandTargetOn);
                }

                if (DateTime.Now - _irPowerCommandSentAt >= DevicePowerCommandTimeout)
                {
                    ConsoleLogHelper.Warning("DEVICE POWER", "IR confirmation timeout / SESSION=" + _connectionSessionId + " / ENDPOINT=" + _connectedControlAgentEndpoint);
                }

                _irPowerCommandPending = false;
                pendingChanged = true;
            }

            if (pendingChanged)
            {
                _context.OnPropertyChanged(nameof(EoPowerControlStatusText));

                _context.OnPropertyChanged(nameof(IrPowerControlStatusText));

                _context.OnPropertyChanged(nameof(CurrentEoPowerStatusColor));

                _context.OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
            }

            if (!_hasDevicePowerStatus || _isDevicePowerStatusStale || DateTime.Now - _devicePowerStatusReceivedAt < TimeSpan.FromSeconds(2))
            {
                return;
            }

            _isDevicePowerStatusStale = true;

            ConsoleLogHelper.Warning("DEVICE STATUS RX", "FUNCTION=0x31 STALE / SESSION=" + _connectionSessionId + " / LAST_RX=" + _devicePowerStatusReceivedAt.ToString("HH:mm:ss.fff"));

            _context.OnPropertyChanged(nameof(DevicePowerStatusText));

            _context.OnPropertyChanged(nameof(DevicePowerFreshnessText));

            _context.OnPropertyChanged(nameof(_context.CurrentControlPowerText));

            _context.OnPropertyChanged(nameof(_context.CurrentEoPowerText));

            _context.OnPropertyChanged(nameof(_context.CurrentIrPowerText));

            _context.OnPropertyChanged(nameof(CurrentGpsPowerText));

            _context.OnPropertyChanged(nameof(CurrentEoPowerStatusColor));

            _context.OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
        }

        #endregion

    }
}
