using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 2026-10-01: WebAgent Build 86 이상에서 제공하는 IMU RPY Offset과
    /// 자동 전송 주파수 설정을 관리한다. Function 0x24 수신값은 WebAgent가
    /// 이미 보정한 값이므로 REI에서 Offset을 다시 적용하지 않는다.
    /// </summary>
    public partial class MainViewModel
    {
        private const int MinimumRpySettingsBuild = 86;

        private const int MinimumRpyStoredSettingsQueryBuild = 93;

        private readonly object _rpyTelemetrySync = new object();

        private readonly Stopwatch _rpyRxRateWindow = Stopwatch.StartNew();

        private int _rpyRxCount;

        private long _lastRpyUiUpdateTimestamp;

        private int? _webAgentBuildNumber;

        private int _storedRpySettingsQueryRequested;

        private int? _storedRpyAutoTxRate;

        private double? _measuredRpyReceiveRate;

        private double? _storedRpyRollOffset;

        private double? _storedRpyPitchOffset;

        private double? _storedRpyYawOffset;

        private string _rpyRollOffsetInput = "0.00";

        private string _rpyPitchOffsetInput = "0.00";

        private string _rpyYawOffsetInput = "0.00";

        private string _rpyAutoTxRateInput = "1";

        private string _currentRpyDisplay = "RPY 수신 대기";

        private string _currentRpyOffsetDisplay =
            "DB 저장 Offset: 조회 대기";

        private string _currentRpyRateDisplay =
            "DB 저장 주파수: 조회 대기 / 실측 수신률: 측정 대기";

        private string _rpyOffsetStatusText = "Ready";

        private string _rpyRateStatusText = "Ready";

        private string _webAgentBuildStatusText = "WebAgent Build 확인 대기";

        public ICommand ApplyRpyOffsetCommand { get; private set; }

        public ICommand ApplyRpyAutoTxRateCommand { get; private set; }

        public ICommand RequestRpyTelemetryCommand { get; private set; }

        public ICommand RequestStoredRpyOffsetCommand { get; private set; }

        public ICommand RequestStoredRpyRateCommand { get; private set; }

        public string RpyRollOffsetInput
        {
            get => _rpyRollOffsetInput;
            set
            {
                if (_rpyRollOffsetInput == value)
                {
                    return;
                }

                _rpyRollOffsetInput = value;
                OnPropertyChanged();
            }
        }

        public string RpyPitchOffsetInput
        {
            get => _rpyPitchOffsetInput;
            set
            {
                if (_rpyPitchOffsetInput == value)
                {
                    return;
                }

                _rpyPitchOffsetInput = value;
                OnPropertyChanged();
            }
        }

        public string RpyYawOffsetInput
        {
            get => _rpyYawOffsetInput;
            set
            {
                if (_rpyYawOffsetInput == value)
                {
                    return;
                }

                _rpyYawOffsetInput = value;
                OnPropertyChanged();
            }
        }

        public string RpyAutoTxRateInput
        {
            get => _rpyAutoTxRateInput;
            set
            {
                if (_rpyAutoTxRateInput == value)
                {
                    return;
                }

                _rpyAutoTxRateInput = value;
                OnPropertyChanged();
            }
        }

        public string CurrentRpyDisplay
        {
            get => _currentRpyDisplay;
            private set
            {
                if (_currentRpyDisplay == value)
                {
                    return;
                }

                _currentRpyDisplay = value;
                OnPropertyChanged();
            }
        }

        public string CurrentRpyOffsetDisplay
        {
            get => _currentRpyOffsetDisplay;
            private set
            {
                if (_currentRpyOffsetDisplay == value)
                {
                    return;
                }

                _currentRpyOffsetDisplay = value;
                OnPropertyChanged();
            }
        }

        public string CurrentRpyRateDisplay
        {
            get => _currentRpyRateDisplay;
            private set
            {
                if (_currentRpyRateDisplay == value)
                {
                    return;
                }

                _currentRpyRateDisplay = value;
                OnPropertyChanged();
            }
        }

        public string RpyOffsetStatusText
        {
            get => _rpyOffsetStatusText;
            private set
            {
                if (_rpyOffsetStatusText == value)
                {
                    return;
                }

                _rpyOffsetStatusText = value;
                OnPropertyChanged();
            }
        }

        public string RpyRateStatusText
        {
            get => _rpyRateStatusText;
            private set
            {
                if (_rpyRateStatusText == value)
                {
                    return;
                }

                _rpyRateStatusText = value;
                OnPropertyChanged();
            }
        }

        public string WebAgentBuildStatusText
        {
            get => _webAgentBuildStatusText;
            private set
            {
                if (_webAgentBuildStatusText == value)
                {
                    return;
                }

                _webAgentBuildStatusText = value;
                OnPropertyChanged();
            }
        }

        private void InitializeRpySettings()
        {
            ApplyRpyOffsetCommand = new RelayCommand(ApplyRpyOffset);
            ApplyRpyAutoTxRateCommand = new RelayCommand(ApplyRpyAutoTxRate);
            RequestRpyTelemetryCommand = new RelayCommand(RequestRpyTelemetry);
            RequestStoredRpyOffsetCommand = new RelayCommand(RequestStoredRpyOffset);
            RequestStoredRpyRateCommand = new RelayCommand(RequestStoredRpyRate);

            VerifyRpyPacketEncoding();
        }

        private void BeginWebAgentBuildCheck()
        {
            _webAgentBuildNumber = null;
            Interlocked.Exchange(ref _storedRpySettingsQueryRequested, 0);
            _storedRpyAutoTxRate = null;
            _measuredRpyReceiveRate = null;
            _storedRpyRollOffset = null;
            _storedRpyPitchOffset = null;
            _storedRpyYawOffset = null;
            WebAgentBuildStatusText = "WebAgent Build 확인 중";
            CurrentRpyOffsetDisplay = "DB 저장 Offset: 조회 대기";
            UpdateRpyRateDisplay();
        }

        private void ApplyRpyOffset()
        {
            if (!CanUseRpySettings(out string unavailableReason))
            {
                RpyOffsetStatusText = unavailableReason;
                ConsoleLogHelper.Warning("RPY OFFSET", unavailableReason);
                return;
            }

            if (!TryParseAngle(RpyRollOffsetInput, -180.0, 180.0, out double roll) ||
                !TryParseAngle(RpyPitchOffsetInput, -180.0, 180.0, out double pitch) ||
                !TryParseAngle(RpyYawOffsetInput, 0.0, 359.99, out double yaw))
            {
                RpyOffsetStatusText = "Failed: 입력 범위를 확인하세요";
                ConsoleLogHelper.Warning(
                    "RPY OFFSET",
                    "Invalid input / Roll, Pitch=-180.00~180.00 / Yaw=0.00~359.99");
                return;
            }

            short rollHundredths = ToSignedHundredths(roll);
            short pitchHundredths = ToSignedHundredths(pitch);
            ushort yawHundredths = ToUnsignedHundredths(yaw);
            byte[] packet = ControlCommandService.BuildRpyOffsetPacket(
                rollHundredths,
                pitchHundredths,
                yawHundredths);

            RpyOffsetStatusText = "Applying";
            bool sent = _controlCommandService.SetRpyOffsets(
                rollHundredths,
                pitchHundredths,
                yawHundredths);

            ConsoleLogHelper.Command(
                "RPY OFFSET TX",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "ROLL={0:F2} / PITCH={1:F2} / YAW={2:F2} / PACKET={3} / SENT={4}",
                    roll,
                    pitch,
                    yaw,
                    ToHex(packet),
                    sent));

            if (!sent)
            {
                RpyOffsetStatusText = "Failed: 전송 실패";
            }
        }

        private void ApplyRpyAutoTxRate()
        {
            if (!CanUseRpySettings(out string unavailableReason))
            {
                RpyRateStatusText = unavailableReason;
                ConsoleLogHelper.Warning("RPY RATE", unavailableReason);
                return;
            }

            if (!int.TryParse(
                    RpyAutoTxRateInput,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int frequencyHz) ||
                frequencyHz < 0 ||
                frequencyHz > 30)
            {
                RpyRateStatusText = "Failed: 0~30 정수만 입력하세요";
                ConsoleLogHelper.Warning("RPY RATE", "Invalid input / expected integer 0~30 Hz");
                return;
            }

            byte frequency = (byte)frequencyHz;
            byte[] packet = ControlCommandService.BuildRpyAutoTxRatePacket(frequency);
            RpyRateStatusText = "Applying";
            bool sent = _controlCommandService.SetRpyAutoTxRate(frequency);

            ConsoleLogHelper.Command(
                "RPY RATE TX",
                "HZ=" + frequencyHz +
                " / PACKET=" + ToHex(packet) +
                " / SENT=" + sent);

            if (!sent)
            {
                RpyRateStatusText = "Failed: 전송 실패";
            }
        }

        private void RequestRpyTelemetry()
        {
            if (!CanUseRpySettings(out string unavailableReason))
            {
                RpyRateStatusText = unavailableReason;
                ConsoleLogHelper.Warning("RPY QUERY", unavailableReason);
                return;
            }

            bool sent = _controlCommandService.RequestImuTelemetry();
            ConsoleLogHelper.Command(
                "RPY QUERY",
                "Function 0x24 single query / CMD2=0xE5 / SENT=" + sent);

            if (!sent)
            {
                RpyRateStatusText = "Failed: 단건 조회 전송 실패";
            }
        }

        private void RequestStoredRpyOffset()
        {
            if (!CanQueryStoredRpySettings(out string unavailableReason))
            {
                RpyOffsetStatusText = unavailableReason;
                ConsoleLogHelper.Warning("RPY OFFSET DB QUERY", unavailableReason);
                return;
            }

            RunOnUiThread(() => RpyOffsetStatusText = "DB 저장값 조회 중");
            bool sent = _controlCommandService.RequestStoredRpyOffsets();
            ConsoleLogHelper.Command(
                "RPY OFFSET DB QUERY TX",
                "PACKET=FF 01 00 F3 00 00 F4 / SENT=" + sent);

            if (!sent)
            {
                RunOnUiThread(() => RpyOffsetStatusText = "Failed: DB 조회 전송 실패");
            }
        }

        private void RequestStoredRpyRate()
        {
            if (!CanQueryStoredRpySettings(out string unavailableReason))
            {
                RpyRateStatusText = unavailableReason;
                ConsoleLogHelper.Warning("RPY RATE DB QUERY", unavailableReason);
                return;
            }

            RunOnUiThread(() => RpyRateStatusText = "DB 저장값 조회 중");
            bool sent = _controlCommandService.RequestStoredRpyAutoTxRate();
            ConsoleLogHelper.Command(
                "RPY RATE DB QUERY TX",
                "PACKET=FF 01 00 F5 00 00 F6 / SENT=" + sent);

            if (!sent)
            {
                RunOnUiThread(() => RpyRateStatusText = "Failed: DB 조회 전송 실패");
            }
        }

        private bool CanUseRpySettings(out string reason)
        {
            if (!IsEnvironmentStatusSelected)
            {
                reason = "WebAgent 전용 기능";
                return false;
            }

            if (_laTcpService == null || !_laTcpService.IsConnected)
            {
                reason = "Failed: WebAgent 미연결";
                return false;
            }

            if (!_webAgentBuildNumber.HasValue)
            {
                reason = "WebAgent Build 확인 대기";
                return false;
            }

            if (_webAgentBuildNumber.Value < MinimumRpySettingsBuild)
            {
                reason = "WebAgent Build 미충족";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool CanQueryStoredRpySettings(out string reason)
        {
            if (!CanUseRpySettings(out reason))
            {
                return false;
            }

            if (_webAgentBuildNumber.Value < MinimumRpyStoredSettingsQueryBuild)
            {
                reason = "WebAgent Build 93 이상 필요";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private void UpdateWebAgentBuild(string buildText)
        {
            int? parsedBuild = ParseBuildNumber(buildText);
            _webAgentBuildNumber = parsedBuild;

            RunOnUiThread(() =>
            {
                if (!parsedBuild.HasValue)
                {
                    WebAgentBuildStatusText = "WebAgent Build 확인 실패: " + buildText;
                }
                else if (parsedBuild.Value < MinimumRpySettingsBuild)
                {
                    WebAgentBuildStatusText =
                        "WebAgent Build " + parsedBuild.Value + " / 기능 미지원";
                }
                else
                {
                    WebAgentBuildStatusText =
                        "WebAgent Build " + parsedBuild.Value +
                        (parsedBuild.Value >= MinimumRpyStoredSettingsQueryBuild
                            ? " / DB 조회 지원"
                            : " / 설정 지원·DB 조회 미지원");
                }
            });

            if (parsedBuild.HasValue &&
                parsedBuild.Value >= MinimumRpyStoredSettingsQueryBuild &&
                Interlocked.Exchange(ref _storedRpySettingsQueryRequested, 1) == 0)
            {
                RequestStoredRpyOffset();
                RequestStoredRpyRate();
            }
        }

        private void ParseWebAgentRpyRateAck(byte[] payload)
        {
            if (payload == null || payload.Length < 2)
            {
                SetRpyRateFailure("Failed: 0x2B 응답 길이 오류");
                return;
            }

            byte result = payload[0];
            byte frequencyHz = payload[1];
            bool success = result == 0x01 && frequencyHz <= 30;

            RunOnUiThread(() =>
            {
                RpyRateStatusText = success ? "Applied" : "Failed";
                if (success)
                {
                    RpyAutoTxRateInput = frequencyHz.ToString(CultureInfo.InvariantCulture);
                }
            });

            ConsoleLogHelper.State(
                "RPY RATE ACK",
                "RESULT=" + (success ? "SUCCESS" : "FAILED") +
                " / CODE=0x" + result.ToString("X2") +
                " / HZ=" + frequencyHz);

            if (success && _webAgentBuildNumber >= MinimumRpyStoredSettingsQueryBuild)
            {
                RequestStoredRpyRate();
            }
        }

        private void ParseWebAgentRpyOffsetAck(byte[] payload)
        {
            if (payload == null || payload.Length < 7)
            {
                SetRpyOffsetFailure("Failed: 0x2C 응답 길이 오류");
                return;
            }

            byte result = payload[0];
            double roll = BitConverter.ToInt16(payload, 1) / 100.0;
            double pitch = BitConverter.ToInt16(payload, 3) / 100.0;
            double yaw = BitConverter.ToUInt16(payload, 5) / 100.0;
            bool success =
                result == 0x01 &&
                roll >= -180.0 && roll <= 180.0 &&
                pitch >= -180.0 && pitch <= 180.0 &&
                yaw >= 0.0 && yaw <= 359.99;

            RunOnUiThread(() =>
            {
                RpyOffsetStatusText = success ? "Applied" : "Failed";
                if (success)
                {
                    _storedRpyRollOffset = roll;
                    _storedRpyPitchOffset = pitch;
                    _storedRpyYawOffset = yaw;
                    CurrentRpyOffsetDisplay = string.Format(
                        CultureInfo.InvariantCulture,
                        "현재 Offset: R={0:+0.00;-0.00;0.00}° / P={1:+0.00;-0.00;0.00}° / Y={2:0.00}°",
                        roll,
                        pitch,
                        yaw);
                    RpyRollOffsetInput = roll.ToString("0.00", CultureInfo.InvariantCulture);
                    RpyPitchOffsetInput = pitch.ToString("0.00", CultureInfo.InvariantCulture);
                    RpyYawOffsetInput = yaw.ToString("0.00", CultureInfo.InvariantCulture);
                }
            });

            ConsoleLogHelper.State(
                "RPY OFFSET ACK",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "RESULT={0} / CODE=0x{1:X2} / ROLL={2:F2} / PITCH={3:F2} / YAW={4:F2}",
                    success ? "SUCCESS" : "FAILED",
                    result,
                    roll,
                    pitch,
                    yaw));

            if (success && _webAgentBuildNumber >= MinimumRpyStoredSettingsQueryBuild)
            {
                RequestStoredRpyOffset();
            }
        }

        private void ParseStoredRpyOffsetResponse(byte[] payload)
        {
            if (payload == null || payload.Length < 7)
            {
                SetRpyOffsetFailure("Failed: 0x2E 응답 길이 오류");
                return;
            }

            byte version = payload[0];
            double roll = BitConverter.ToInt16(payload, 1) / 100.0;
            double pitch = BitConverter.ToInt16(payload, 3) / 100.0;
            double yaw = BitConverter.ToUInt16(payload, 5) / 100.0;
            bool valid =
                version == 0x01 &&
                roll >= -180.0 && roll <= 180.0 &&
                pitch >= -180.0 && pitch <= 180.0 &&
                yaw >= 0.0 && yaw <= 359.99;

            if (!valid)
            {
                SetRpyOffsetFailure("Failed: 0x2E DB 저장값 범위/버전 오류");
                return;
            }

            RunOnUiThread(() =>
            {
                _storedRpyRollOffset = roll;
                _storedRpyPitchOffset = pitch;
                _storedRpyYawOffset = yaw;
                RpyRollOffsetInput = roll.ToString("0.00", CultureInfo.InvariantCulture);
                RpyPitchOffsetInput = pitch.ToString("0.00", CultureInfo.InvariantCulture);
                RpyYawOffsetInput = yaw.ToString("0.00", CultureInfo.InvariantCulture);
                RpyOffsetStatusText = "DB 저장값 조회 완료";
                CurrentRpyOffsetDisplay = string.Format(
                    CultureInfo.InvariantCulture,
                    "DB 저장 Offset: R={0:+0.00;-0.00;0.00}° / P={1:+0.00;-0.00;0.00}° / Y={2:0.00}°",
                    roll,
                    pitch,
                    yaw);
            });

            ConsoleLogHelper.State(
                "RPY OFFSET DB RX",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "FUNCTION=0x2E / VERSION={0} / ROLL={1:F2} / PITCH={2:F2} / YAW={3:F2}",
                    version,
                    roll,
                    pitch,
                    yaw));
        }

        private void ParseStoredRpyRateResponse(byte[] payload)
        {
            if (payload == null || payload.Length < 2)
            {
                SetRpyRateFailure("Failed: 0x2F 응답 길이 오류");
                return;
            }

            byte version = payload[0];
            byte frequencyHz = payload[1];
            if (version != 0x01 || frequencyHz > 30)
            {
                SetRpyRateFailure("Failed: 0x2F DB 저장값 범위/버전 오류");
                return;
            }

            _storedRpyAutoTxRate = frequencyHz;
            RunOnUiThread(() =>
            {
                RpyAutoTxRateInput = frequencyHz.ToString(CultureInfo.InvariantCulture);
                RpyRateStatusText = "DB 저장값 조회 완료";
                UpdateRpyRateDisplay();
            });

            ConsoleLogHelper.State(
                "RPY RATE DB RX",
                "FUNCTION=0x2F / VERSION=" + version + " / HZ=" + frequencyHz);
        }

        /// <summary>
        /// 0x24는 최대 30Hz이므로 내부 위치 상태는 즉시 갱신하되,
        /// 추가 RPY UI 문자열은 최대 10Hz로 제한하고 진단 로그는 약 1Hz로 제한한다.
        /// </summary>
        private void RecordRpyTelemetry(double roll, double pitch, double yaw)
        {
            bool updateUi = false;
            bool writeRateLog = false;
            double receiveRate = 0.0;
            long now = Stopwatch.GetTimestamp();

            lock (_rpyTelemetrySync)
            {
                _rpyRxCount++;

                if (now - _lastRpyUiUpdateTimestamp >= Stopwatch.Frequency / 10)
                {
                    _lastRpyUiUpdateTimestamp = now;
                    updateUi = true;
                }

                if (_rpyRxRateWindow.ElapsedMilliseconds >= 1000)
                {
                    receiveRate = _rpyRxCount / _rpyRxRateWindow.Elapsed.TotalSeconds;
                    _rpyRxCount = 0;
                    _rpyRxRateWindow.Restart();
                    writeRateLog = true;
                }
            }

            if (updateUi)
            {
                RunOnUiThread(() =>
                {
                    CurrentRpyDisplay = string.Format(
                        CultureInfo.InvariantCulture,
                        "보정 R {0:F2}° / P {1:F2}° / Y {2:F2}°",
                        roll,
                        pitch,
                        yaw);
                });
            }

            if (writeRateLog)
            {
                _measuredRpyReceiveRate = receiveRate;
                RunOnUiThread(() =>
                {
                    UpdateRpyRateDisplay();
                });

                ConsoleLogHelper.State(
                    "RPY RX",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "ROLL={0:F4} / PITCH={1:F4} / YAW={2:F4} / RX_RATE={3:F1}Hz",
                        roll,
                        pitch,
                        yaw,
                        receiveRate));
            }
        }

        private void UpdateRpyRateDisplay()
        {
            string storedText = _storedRpyAutoTxRate.HasValue
                ? _storedRpyAutoTxRate.Value.ToString(CultureInfo.InvariantCulture) + " Hz"
                : "조회 대기";
            string measuredText = _measuredRpyReceiveRate.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "약 {0:F1} Hz", _measuredRpyReceiveRate.Value)
                : "측정 대기";

            CurrentRpyRateDisplay =
                "DB 저장 주파수: " + storedText +
                " / 실측 수신률: " + measuredText;
        }

        private void SetRpyRateFailure(string message)
        {
            RunOnUiThread(() => RpyRateStatusText = message);
            ConsoleLogHelper.Warning("RPY RATE ACK", message);
        }

        private void SetRpyOffsetFailure(string message)
        {
            RunOnUiThread(() => RpyOffsetStatusText = message);
            ConsoleLogHelper.Warning("RPY OFFSET ACK", message);
        }

        private static bool TryParseAngle(
            string text,
            double minimum,
            double maximum,
            out double value)
        {
            bool parsed = double.TryParse(
                              text,
                              NumberStyles.Float,
                              CultureInfo.CurrentCulture,
                              out value) ||
                          double.TryParse(
                              text,
                              NumberStyles.Float,
                              CultureInfo.InvariantCulture,
                              out value);

            return parsed &&
                   !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value >= minimum &&
                   value <= maximum;
        }

        private static short ToSignedHundredths(double value)
        {
            return checked((short)Math.Round(
                value * 100.0,
                MidpointRounding.AwayFromZero));
        }

        private static ushort ToUnsignedHundredths(double value)
        {
            return checked((ushort)Math.Round(
                value * 100.0,
                MidpointRounding.AwayFromZero));
        }

        private static string ToHex(byte[] packet)
        {
            return string.Join(" ", packet.Select(value => value.ToString("X2")));
        }

        private static int? ParseBuildNumber(string buildText)
        {
            if (string.IsNullOrWhiteSpace(buildText))
            {
                return null;
            }

            string digits = new string(buildText.Where(char.IsDigit).ToArray());
            return int.TryParse(
                digits,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int buildNumber)
                    ? buildNumber
                    : (int?)null;
        }

        private static void RunOnUiThread(Action action)
        {
            Dispatcher dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }

        private static void VerifyRpyPacketEncoding()
        {
            byte[] offsetPacket = ControlCommandService.BuildRpyOffsetPacket(
                100,
                -200,
                3000);
            byte[] ratePacket = ControlCommandService.BuildRpyAutoTxRatePacket(10);
            byte[] offsetQueryPacket = ControlCommandService.BuildStoredRpyOffsetQueryPacket();
            byte[] rateQueryPacket = ControlCommandService.BuildStoredRpyAutoTxRateQueryPacket();

            string offsetHex = ToHex(offsetPacket);
            string rateHex = ToHex(ratePacket);
            string offsetQueryHex = ToHex(offsetQueryPacket);
            string rateQueryHex = ToHex(rateQueryPacket);
            bool valid =
                offsetHex == "FF 01 00 ED 64 00 38 FF B8 0B 4C" &&
                rateHex == "FF 01 00 EB 0A 00 F6" &&
                offsetQueryHex == "FF 01 00 F3 00 00 F4" &&
                rateQueryHex == "FF 01 00 F5 00 00 F6";

            if (!valid)
            {
                ConsoleLogHelper.Error(
                    "RPY PROTOCOL",
                    "Packet encoding self-test failed / OFFSET=" + offsetHex +
                    " / RATE=" + rateHex +
                    " / OFFSET_QUERY=" + offsetQueryHex +
                    " / RATE_QUERY=" + rateQueryHex,
                    new InvalidOperationException("RPY packet sample mismatch."));
            }
        }
    }
}
