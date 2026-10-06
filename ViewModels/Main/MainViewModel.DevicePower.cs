using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// WebAgent EO/IR 전원 제어와 Function 0x31 MCB/SCB 상태 동기화.
    /// </summary>
    public partial class MainViewModel
    {
        private const byte EoPowerDeviceCode = 0x00;
        private const byte IrPowerDeviceCode = 0x01;
        private static readonly TimeSpan DevicePowerCommandTimeout = TimeSpan.FromSeconds(90);

        private bool _hasDevicePowerStatus;
        private byte _currentMcbPowerStatus;
        private byte _currentScbPowerStatus;
        private DateTime _devicePowerStatusReceivedAt;
        private bool _isDevicePowerStatusStale;
        private DispatcherTimer _devicePowerFreshnessTimer;
        private ControlAgentProfileOption _connectedControlAgentProfile;
        private string _connectedControlAgentEndpoint = "-";
        private string _connectionSessionId = "-";
        private bool _eoPowerCommandPending;
        private bool _irPowerCommandPending;
        private bool _eoPowerCommandTargetOn;
        private bool _irPowerCommandTargetOn;
        private DateTime _eoPowerCommandSentAt;
        private DateTime _irPowerCommandSentAt;

        public ICommand EoPowerOnCommand { get; private set; }
        public ICommand EoPowerOffCommand { get; private set; }
        public ICommand IrPowerOnCommand { get; private set; }
        public ICommand IrPowerOffCommand { get; private set; }

        public string DevicePowerStatusText =>
            !_hasDevicePowerStatus
                ? "WAITING FOR FUNCTION 0x31"
                : _isDevicePowerStatusStale
                    ? "FUNCTION 0x31 STALE"
                : string.Format(
                    "MCB 0x{0:X2} / SCB 0x{1:X2} / RX {2:HH:mm:ss.fff}",
                    _currentMcbPowerStatus,
                    _currentScbPowerStatus,
                    _devicePowerStatusReceivedAt);

        public string DevicePowerFreshnessText =>
            !IsGpsTabVisible ? "LEGACY" :
            !_hasDevicePowerStatus ? "WAITING" :
            _isDevicePowerStatusStale ? "STALE" : "FRESH";

        public string DeviceControlConnectionStatusText =>
            ToOnOff(string.Equals(ControlAgentConnectionStatusText, "Connected",
                StringComparison.OrdinalIgnoreCase));

        public string CurrentGpsPowerText =>
            _hasDevicePowerStatus && !_isDevicePowerStatusStale
                ? ToOnOff((_currentScbPowerStatus & 0x02) != 0)
                : "UNKNOWN";

        public string EoPowerControlStatusText =>
            _eoPowerCommandPending ? "WAIT" : CurrentEoPowerText;
        public string IrPowerControlStatusText =>
            _irPowerCommandPending ? "WAIT" : CurrentIrPowerText;
        public string CurrentEoPowerStatusColor => GetPowerStatusColor(EoPowerControlStatusText);
        public string CurrentIrPowerStatusColor => GetPowerStatusColor(IrPowerControlStatusText);
        public bool IsEoPowerControlEnabled => IsOperationCommandEnabled &&
            (EoPowerControlStatusText == "ON" || EoPowerControlStatusText == "OFF");
        public bool IsIrPowerControlEnabled => IsOperationCommandEnabled &&
            (IrPowerControlStatusText == "ON" || IrPowerControlStatusText == "OFF");

        private static string GetPowerStatusColor(string value) =>
            value == "ON" ? "#69E39B" : value == "OFF" ? "#FF7B72" : "#FDE68A";

        private static string GetLegacyCameraPowerText(string status, string camera)
        {
            if (string.Equals(status, "[" + camera + "] Connected", StringComparison.OrdinalIgnoreCase))
                return "ON";
            if (!string.IsNullOrEmpty(status) &&
                (status.StartsWith("[" + camera + "] Connecting", StringComparison.OrdinalIgnoreCase) ||
                 status.StartsWith("[" + camera + "] Reconnecting", StringComparison.OrdinalIgnoreCase) ||
                 status.StartsWith("[" + camera + "] Waiting", StringComparison.OrdinalIgnoreCase)))
                return "WAIT";
            return "OFF";
        }

        public string DevicePowerDetailText =>
            !_hasDevicePowerStatus
                ? "전원 상태 미수신 (OFF로 간주하지 않음)"
                : string.Format(
                    "DRIVER {0} / EO {1} / IR {2} / GPS {3}",
                    ToOnOff((_currentMcbPowerStatus & 0x01) != 0),
                    ToOnOff((_currentScbPowerStatus & 0x01) != 0),
                    ToOnOff((_currentMcbPowerStatus & 0x20) != 0),
                    ToOnOff((_currentScbPowerStatus & 0x02) != 0));

        private void InitializeDevicePowerSettings()
        {
            EoPowerOnCommand = new RelayCommand(
                () => SendDevicePower(EoPowerDeviceCode, true, "EO"), () => IsEoPowerControlEnabled);
            EoPowerOffCommand = new RelayCommand(
                () => SendDevicePower(EoPowerDeviceCode, false, "EO"), () => IsEoPowerControlEnabled);
            IrPowerOnCommand = new RelayCommand(
                () => SendDevicePower(IrPowerDeviceCode, true, "IR"), () => IsIrPowerControlEnabled);
            IrPowerOffCommand = new RelayCommand(
                () => SendDevicePower(IrPowerDeviceCode, false, "IR"), () => IsIrPowerControlEnabled);

            _devicePowerFreshnessTimer = new DispatcherTimer(
                DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _devicePowerFreshnessTimer.Tick += DevicePowerFreshnessTimer_Tick;

            VerifyDevicePowerProtocol();
        }

        /// <summary>
        /// 2026-10-06: 전원 요청 4종과 고정 13Byte Function 0x31 분할 수신을
        /// 시작 시 자체 검증한다. 실패 시 기능 전체를 중단하지 않고 진단 로그를 남긴다.
        /// </summary>
        private static void VerifyDevicePowerProtocol()
        {
            try
            {
                VerifyDevicePowerPacket("EO ON", 0x00, true,
                    new byte[] { 0xFF, 0x01, 0x88, 0x00, 0x00, 0x00, 0x89 });
                VerifyDevicePowerPacket("EO OFF", 0x00, false,
                    new byte[] { 0xFF, 0x01, 0x08, 0x00, 0x00, 0x00, 0x09 });
                VerifyDevicePowerPacket("IR ON", 0x01, true,
                    new byte[] { 0xFF, 0x01, 0x88, 0x00, 0x01, 0x00, 0x8A });
                VerifyDevicePowerPacket("IR OFF", 0x01, false,
                    new byte[] { 0xFF, 0x01, 0x08, 0x00, 0x01, 0x00, 0x0A });

                byte[] statusPacket =
                {
                    0xFF, 0x31,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x1E, 0x0C, 0x5B
                };
                LAPacketParser parser = new LAPacketParser();
                if (parser.Parse(statusPacket.Take(5).ToArray()).Count != 0)
                {
                    throw new InvalidOperationException("0x31 split prefix was emitted early.");
                }

                var parsed = parser.Parse(statusPacket.Skip(5).ToArray());
                if (parsed.Count != 1 || !parsed[0].IsValid ||
                    !parsed[0].IsDevicePowerStatus || parsed[0].RawData.Length != 13)
                {
                    throw new InvalidOperationException("0x31 fixed frame assembly failed.");
                }

                ConsoleLogHelper.State(
                    "DEVICE POWER SELF TEST",
                    "EO/IR ON/OFF checksum + Function 0x31 split frame verified");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "DEVICE POWER SELF TEST",
                    "Protocol verification failed",
                    ex);
            }
        }

        private static void VerifyDevicePowerPacket(
            string name,
            byte deviceCode,
            bool turnOn,
            byte[] expected)
        {
            byte[] actual = ControlCommandService.BuildDevicePowerPacket(deviceCode, turnOn);
            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidOperationException(name + " packet mismatch.");
            }
        }

        private void SendDevicePower(byte deviceCode, bool turnOn, string deviceName)
        {
            // UI 외 직접 호출에서도 WAIT/UNKNOWN 중 중복·반대 명령을 차단한다.
            if (deviceCode == EoPowerDeviceCode ? !IsEoPowerControlEnabled : !IsIrPowerControlEnabled)
                return;
            // 2026-10-06: 파노라마/AUTO SCAN/HOME/ZERO 동작 중 전원 명령 차단.
            if (!IsOperationCommandEnabled)
            {
                ConsoleLogHelper.Warning(
                    "DEVICE POWER",
                    deviceName + " " + (turnOn ? "ON" : "OFF") +
                    " rejected / operation command lock is active");
                return;
            }

            if (_laTcpService == null || !_laTcpService.IsConnected)
            {
                ConsoleLogHelper.Warning(
                    "DEVICE POWER",
                    deviceName + " " + (turnOn ? "ON" : "OFF") +
                    " rejected / Control Agent is not connected");
                return;
            }

            if (_controlCommandService == null)
            {
                ConsoleLogHelper.Warning(
                    "DEVICE POWER",
                    "EO/IR power control service is not initialized");
                return;
            }

            try
            {
                bool sent = _controlCommandService.SetDevicePower(deviceCode, turnOn);
                if (!sent)
                {
                    ConsoleLogHelper.Warning(
                        "DEVICE POWER",
                        deviceName + " " + (turnOn ? "ON" : "OFF") +
                        " send failed");
                    return;
                }

                ConsoleLogHelper.State(
                    "DEVICE POWER",
                    deviceName + " " + (turnOn ? "ON" : "OFF") +
                    " request sent / waiting for Function 0x31 board status");

                // 2026-10-06: LA/WebAgent 공통으로 명령 직후 노란 WAIT 상태를 표시한다.
                // 실제 상태 응답 또는 제한시간 경과 시 수신 상태 표시로 복귀한다.
                if (deviceCode == EoPowerDeviceCode)
                {
                    _eoPowerCommandPending = true;
                    _eoPowerCommandTargetOn = turnOn;
                    _eoPowerCommandSentAt = DateTime.Now;
                    OnPropertyChanged(nameof(EoPowerControlStatusText));
                    OnPropertyChanged(nameof(CurrentEoPowerStatusColor));
                }
                else
                {
                    _irPowerCommandPending = true;
                    _irPowerCommandTargetOn = turnOn;
                    _irPowerCommandSentAt = DateTime.Now;
                    OnPropertyChanged(nameof(IrPowerControlStatusText));
                    OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "DEVICE POWER",
                    deviceName + " power command failed",
                    ex);
            }
        }

        /// <summary>
        /// 2026-10-06: 0x31의 MCB/SCB 원본 상태를 보존한다.
        /// 요청 송신 여부로 상태를 추정하지 않고 이 수신값만 표시 기준으로 사용한다.
        /// </summary>
        private void ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus)
        {
            bool shouldLog =
                !_hasDevicePowerStatus ||
                _currentMcbPowerStatus != mcbStatus ||
                _currentScbPowerStatus != scbStatus;

            _currentMcbPowerStatus = mcbStatus;
            _currentScbPowerStatus = scbStatus;
            _devicePowerStatusReceivedAt = DateTime.Now;
            _hasDevicePowerStatus = true;
            _isDevicePowerStatusStale = false;
            // 주기 응답에 이전 상태가 담겨 있어도 요청 목표에 도달할 때까지 WAIT 유지.
            if (IsGpsTabVisible && _eoPowerCommandPending &&
                ((scbStatus & 0x01) != 0) == _eoPowerCommandTargetOn)
                _eoPowerCommandPending = false;
            if (IsGpsTabVisible && _irPowerCommandPending &&
                ((mcbStatus & 0x20) != 0) == _irPowerCommandTargetOn)
                _irPowerCommandPending = false;

            // 2026-10-06: 500 ms 상태 프레임의 로그 폭주를 막기 위해 최초 수신과
            // 상태 변화만 기록한다. 장비별 0x31 실제 수신 여부를 로그로 판별할 수 있다.
            if (shouldLog)
            {
                ConsoleLogHelper.State(
                    "DEVICE STATUS RX",
                    string.Format(
                        "SESSION={0} / ACTIVE_ENDPOINT={1} / CONNECTED_PROFILE={2} / SELECTED_PROFILE={3} / FUNCTION=0x31 / MCB=0x{4:X2} / SCB=0x{5:X2} / RX={6:HH:mm:ss.fff}",
                        _connectionSessionId,
                        _connectedControlAgentEndpoint,
                        _connectedControlAgentProfile?.DisplayName ?? "UNKNOWN",
                        SelectedControlAgentProfile?.DisplayName ?? "UNKNOWN",
                        mcbStatus,
                        scbStatus,
                        _devicePowerStatusReceivedAt));
            }

            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
            {
                return;
            }

            _ = dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    OnPropertyChanged(nameof(DevicePowerStatusText));
                    OnPropertyChanged(nameof(DevicePowerFreshnessText));
                    OnPropertyChanged(nameof(DevicePowerDetailText));
                    OnPropertyChanged(nameof(CurrentPowerText));
                    OnPropertyChanged(nameof(CurrentControlPowerText));
                    OnPropertyChanged(nameof(CurrentEoPowerText));
                    OnPropertyChanged(nameof(CurrentIrPowerText));
                    OnPropertyChanged(nameof(CurrentGpsPowerText));
                    OnPropertyChanged(nameof(EoPowerControlStatusText));
                    OnPropertyChanged(nameof(IrPowerControlStatusText));
                    OnPropertyChanged(nameof(CurrentEoPowerStatusColor));
                    OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
                }));
        }

        private void ResetDevicePowerStatus()
        {
            _hasDevicePowerStatus = false;
            _isDevicePowerStatusStale = false;
            _eoPowerCommandPending = false;
            _irPowerCommandPending = false;
            _currentMcbPowerStatus = 0;
            _currentScbPowerStatus = 0;
            _devicePowerStatusReceivedAt = DateTime.MinValue;
            OnPropertyChanged(nameof(DevicePowerStatusText));
            OnPropertyChanged(nameof(DevicePowerFreshnessText));
            OnPropertyChanged(nameof(DevicePowerDetailText));
            OnPropertyChanged(nameof(CurrentPowerText));
            OnPropertyChanged(nameof(CurrentControlPowerText));
            OnPropertyChanged(nameof(CurrentEoPowerText));
            OnPropertyChanged(nameof(CurrentIrPowerText));
            OnPropertyChanged(nameof(CurrentGpsPowerText));
            OnPropertyChanged(nameof(CurrentEoPowerStatusColor));
            OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
            OnPropertyChanged(nameof(EoPowerControlStatusText));
            OnPropertyChanged(nameof(IrPowerControlStatusText));
        }

        /// <summary>
        /// 2026-10-06: 선택 대상과 실제 연결 세션을 분리해 늦은 0x31이
        /// 새 ComboBox 프로필 소유로 기록되는 문제를 방지한다.
        /// </summary>
        private void BeginConnectedControlAgentSession()
        {
            if (_connectedControlAgentProfile != null) return;
            _connectedControlAgentProfile = SelectedControlAgentProfile;
            _connectedControlAgentEndpoint = ControlAgentIp + ":" + ControlAgentPortText;
            _connectionSessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            ResetDevicePowerStatus();
            _devicePowerFreshnessTimer?.Start();
            ConsoleLogHelper.State("CONTROL SESSION", "START / SESSION=" +
                _connectionSessionId + " / ENDPOINT=" + _connectedControlAgentEndpoint +
                " / PROFILE=" + (_connectedControlAgentProfile?.DisplayName ?? "UNKNOWN"));
        }

        private void EndConnectedControlAgentSession()
        {
            _devicePowerFreshnessTimer?.Stop();
            _connectedControlAgentProfile = null;
            _connectedControlAgentEndpoint = "-";
            _connectionSessionId = "-";
            ResetDevicePowerStatus();
        }

        private void DevicePowerFreshnessTimer_Tick(object sender, EventArgs e)
        {
            bool pendingChanged = false;
            if (_eoPowerCommandPending &&
                ((!IsGpsTabVisible && CurrentEoPowerText == ToOnOff(_eoPowerCommandTargetOn)) ||
                 DateTime.Now - _eoPowerCommandSentAt >= DevicePowerCommandTimeout))
            {
                _eoPowerCommandPending = false;
                pendingChanged = true;
            }
            if (_irPowerCommandPending &&
                ((!IsGpsTabVisible && CurrentIrPowerText == ToOnOff(_irPowerCommandTargetOn)) ||
                 DateTime.Now - _irPowerCommandSentAt >= DevicePowerCommandTimeout))
            {
                _irPowerCommandPending = false;
                pendingChanged = true;
            }
            if (pendingChanged)
            {
                OnPropertyChanged(nameof(EoPowerControlStatusText));
                OnPropertyChanged(nameof(IrPowerControlStatusText));
                OnPropertyChanged(nameof(CurrentEoPowerStatusColor));
                OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
            }

            if (!_hasDevicePowerStatus || _isDevicePowerStatusStale ||
                DateTime.Now - _devicePowerStatusReceivedAt < TimeSpan.FromSeconds(2)) return;
            _isDevicePowerStatusStale = true;
            ConsoleLogHelper.Warning("DEVICE STATUS RX", "FUNCTION=0x31 STALE / SESSION=" +
                _connectionSessionId + " / LAST_RX=" +
                _devicePowerStatusReceivedAt.ToString("HH:mm:ss.fff"));
            OnPropertyChanged(nameof(DevicePowerStatusText));
            OnPropertyChanged(nameof(DevicePowerFreshnessText));
            OnPropertyChanged(nameof(CurrentControlPowerText));
            OnPropertyChanged(nameof(CurrentEoPowerText));
            OnPropertyChanged(nameof(CurrentIrPowerText));
            OnPropertyChanged(nameof(CurrentGpsPowerText));
            OnPropertyChanged(nameof(CurrentEoPowerStatusColor));
            OnPropertyChanged(nameof(CurrentIrPowerStatusColor));
        }
    }
}
