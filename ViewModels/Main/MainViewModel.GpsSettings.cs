using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 2026-10-02: WebAgent GPS 20Byte 상태와 0~10Hz 공통 자동 전송 설정을 관리한다.
    /// GPS 진행 방향, IMU RAW Yaw, DB Offset은 서로 다른 자료로 유지한다.
    /// 2026-10-02 WebAgent GPS 화면/API 의미와 동일하게 IMU RAW Yaw에
    /// DB 저장 Yaw Offset을 GUI에서 순환 합산하여 진북 방위를 표시한다.
    /// </summary>
    public partial class MainViewModel
    {
        private readonly object _gpsTelemetrySync = new object();

        private readonly Stopwatch _gpsRxRateWindow = Stopwatch.StartNew();

        private int _gpsRxCount;

        private long _lastGpsUiUpdateTimestamp;

        private long _lastGpsLogTimestamp;

        private double? _latestGpsCourseDegrees;

        private double? _latestImuRawYawDegrees;

        private int? _gpsAutoTxRate;

        private double? _measuredGpsPacketRate;

        private string _gpsAutoTxRateInput = "1";

        private string _gpsStatusText = "GPS 수신 대기";

        private string _gpsCoordinatesDisplay = "LAT -- / LON -- / ALT --";

        private string _gpsMotionDisplay = "SPEED -- / GPS COURSE --";

        private string _gpsQualityDisplay = "HDOP -- / SAT -- / FIX --";

        private string _gpsRxTimeDisplay = "GUI RX TIME --";

        private string _gpsHeadingReferenceDisplay =
            "GPS COURSE -- / IMU RAW YAW --";

        private string _gpsRateStatusText = "Ready";

        private string _currentGpsRateDisplay =
            "현재 설정: 조회 대기 / GUI 수신 패킷률: 측정 대기";

        public ICommand RequestGpsTelemetryCommand { get; private set; }

        public ICommand ApplyGpsAutoTxRateCommand { get; private set; }

        public ICommand RequestGpsAutoTxRateCommand { get; private set; }

        public string GpsAutoTxRateInput
        {
            get => _gpsAutoTxRateInput;
            set
            {
                if (_gpsAutoTxRateInput == value) return;
                _gpsAutoTxRateInput = value;
                OnPropertyChanged();
            }
        }

        public string GpsStatusText
        {
            get => _gpsStatusText;
            private set
            {
                if (_gpsStatusText == value) return;
                _gpsStatusText = value;
                OnPropertyChanged();
            }
        }

        public string GpsCoordinatesDisplay
        {
            get => _gpsCoordinatesDisplay;
            private set
            {
                if (_gpsCoordinatesDisplay == value) return;
                _gpsCoordinatesDisplay = value;
                OnPropertyChanged();
            }
        }

        public string GpsMotionDisplay
        {
            get => _gpsMotionDisplay;
            private set
            {
                if (_gpsMotionDisplay == value) return;
                _gpsMotionDisplay = value;
                OnPropertyChanged();
            }
        }

        public string GpsQualityDisplay
        {
            get => _gpsQualityDisplay;
            private set
            {
                if (_gpsQualityDisplay == value) return;
                _gpsQualityDisplay = value;
                OnPropertyChanged();
            }
        }

        public string GpsRxTimeDisplay
        {
            get => _gpsRxTimeDisplay;
            private set
            {
                if (_gpsRxTimeDisplay == value) return;
                _gpsRxTimeDisplay = value;
                OnPropertyChanged();
            }
        }

        public string GpsHeadingReferenceDisplay
        {
            get => _gpsHeadingReferenceDisplay;
            private set
            {
                if (_gpsHeadingReferenceDisplay == value) return;
                _gpsHeadingReferenceDisplay = value;
                OnPropertyChanged();
            }
        }

        public string GpsRateStatusText
        {
            get => _gpsRateStatusText;
            private set
            {
                if (_gpsRateStatusText == value) return;
                _gpsRateStatusText = value;
                OnPropertyChanged();
            }
        }

        public string CurrentGpsRateDisplay
        {
            get => _currentGpsRateDisplay;
            private set
            {
                if (_currentGpsRateDisplay == value) return;
                _currentGpsRateDisplay = value;
                OnPropertyChanged();
            }
        }

        private void InitializeGpsSettings()
        {
            RequestGpsTelemetryCommand = new RelayCommand(RequestGpsTelemetry);
            ApplyGpsAutoTxRateCommand = new RelayCommand(ApplyGpsAutoTxRate);
            RequestGpsAutoTxRateCommand = new RelayCommand(RequestGpsAutoTxRate);
            VerifyGpsProtocolEncoding();
        }

        private bool CanUseGpsSettings(out string reason)
        {
            // 2026-10-06: HOME/ZERO, AUTO SCAN, panorama capture/processing 중에는
            // 다른 장비 명령과 동일하게 GPS 조회 및 설정 송신을 차단한다.
            if (!IsOperationCommandEnabled)
            {
                reason = "운용 작업 중 GPS 제어 잠금";
                return false;
            }

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

            reason = string.Empty;
            return true;
        }

        private void RequestGpsTelemetry()
        {
            if (!CanUseGpsSettings(out string reason))
            {
                GpsStatusText = reason;
                ConsoleLogHelper.Warning("GPS QUERY", reason);
                return;
            }

            GpsStatusText = "단건 조회 응답 대기";
            bool sent = _controlCommandService.RequestGpsTelemetry();
            ConsoleLogHelper.Command(
                "GPS QUERY TX",
                "CMD2=0xA3 / PACKET=FF 01 00 A3 00 00 A4 / SENT=" + sent);

            if (!sent)
            {
                GpsStatusText = "Failed: 단건 조회 전송 실패";
            }
        }

        private void ApplyGpsAutoTxRate()
        {
            if (!CanUseGpsSettings(out string reason))
            {
                GpsRateStatusText = reason;
                ConsoleLogHelper.Warning("GPS RATE", reason);
                return;
            }

            if (!int.TryParse(
                    GpsAutoTxRateInput,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int frequencyHz) ||
                frequencyHz < 0 || frequencyHz > 10)
            {
                GpsRateStatusText = "Failed: 0~10 정수만 입력하세요";
                ConsoleLogHelper.Warning("GPS RATE", "Invalid input / expected integer 0~10 Hz");
                return;
            }

            byte frequency = (byte)frequencyHz;
            byte[] packet = ControlCommandService.BuildGpsAutoTxRatePacket(frequency);
            GpsRateStatusText = "설정 응답 대기";
            bool sent = _controlCommandService.SetGpsAutoTxRate(frequency);

            ConsoleLogHelper.Command(
                "GPS RATE SET TX",
                "HZ=" + frequencyHz +
                " / PACKET=" + ToHex(packet) +
                " / SENT=" + sent);

            if (!sent)
            {
                GpsRateStatusText = "Failed: 주파수 설정 전송 실패";
            }
        }

        private void RequestGpsAutoTxRate()
        {
            if (!CanUseGpsSettings(out string reason))
            {
                GpsRateStatusText = reason;
                ConsoleLogHelper.Warning("GPS RATE QUERY", reason);
                return;
            }

            GpsRateStatusText = "현재 설정 조회 중";
            bool sent = _controlCommandService.RequestGpsAutoTxRate();
            ConsoleLogHelper.Command(
                "GPS RATE QUERY TX",
                "CMD2=0xF9 / PACKET=FF 01 00 F9 00 00 FA / SENT=" + sent);

            if (!sent)
            {
                GpsRateStatusText = "Failed: 주파수 조회 전송 실패";
            }
        }

        private void RequestGpsAutoTxRateAfterConnection()
        {
            if (_laTcpService == null || !_laTcpService.IsConnected)
            {
                return;
            }

            bool sent = _controlCommandService.RequestGpsAutoTxRate();
            ConsoleLogHelper.Command(
                "GPS RATE CONNECT QUERY TX",
                "PACKET=FF 01 00 F9 00 00 FA / SENT=" + sent);
        }

        private void ParseWebAgentGps(byte[] payload)
        {
            if (!TryParseGpsPayload(payload, out GpsTelemetryData gps, out string error))
            {
                SetGpsFailure("Failed: 0x23 " + error);
                return;
            }

            DateTimeOffset receivedAt = DateTimeOffset.Now;
            bool fixValid = gps.FixStatus != 0;

            // 내부 위치 상태는 모든 정상 구조 패킷에서 즉시 갱신한다.
            // Fix 0은 좌표 범위 오류가 아니라 정상 수신된 INVALID 상태다.
            UpdatePositionGps(
                gps.Latitude,
                gps.Longitude,
                gps.Altitude,
                gps.Speed,
                gps.CourseDegrees,
                gps.Hdop,
                gps.SatelliteCount,
                gps.FixStatus,
                fixValid);

            RecordGpsTelemetry(gps, receivedAt, fixValid);
        }

        private void RecordGpsTelemetry(
            GpsTelemetryData gps,
            DateTimeOffset receivedAt,
            bool fixValid)
        {
            bool updateUi = false;
            bool writeLog = false;
            double receiveRate = 0.0;
            long now = Stopwatch.GetTimestamp();

            lock (_gpsTelemetrySync)
            {
                _latestGpsCourseDegrees = gps.CourseDegrees;
                _gpsRxCount++;

                if (now - _lastGpsUiUpdateTimestamp >= Stopwatch.Frequency / 10)
                {
                    _lastGpsUiUpdateTimestamp = now;
                    updateUi = true;
                }

                if (_gpsRxRateWindow.ElapsedMilliseconds >= 1000)
                {
                    receiveRate = _gpsRxCount / _gpsRxRateWindow.Elapsed.TotalSeconds;
                    _gpsRxCount = 0;
                    _gpsRxRateWindow.Restart();
                    _measuredGpsPacketRate = receiveRate;
                }

                if (now - _lastGpsLogTimestamp >= Stopwatch.Frequency)
                {
                    _lastGpsLogTimestamp = now;
                    writeLog = true;
                }
            }

            if (updateUi)
            {
                RunOnUiThread(() =>
                {
                    GpsStatusText = fixValid ? "GPS DATA VALID" : "GPS DATA INVALID / FIX 0";
                    GpsCoordinatesDisplay = string.Format(
                        CultureInfo.InvariantCulture,
                        "LAT {0:F7}° / LON {1:F7}° / ALT {2:F3} m",
                        gps.Latitude,
                        gps.Longitude,
                        gps.Altitude);
                    GpsMotionDisplay = string.Format(
                        CultureInfo.InvariantCulture,
                        "SPEED {0:F2} (MCB ×0.01) / GPS COURSE {1:F2}°",
                        gps.Speed,
                        gps.CourseDegrees);
                    GpsQualityDisplay = string.Format(
                        CultureInfo.InvariantCulture,
                        "HDOP {0:F2} / SAT {1} / FIX {2}",
                        gps.Hdop,
                        gps.SatelliteCount,
                        gps.FixStatus);
                    GpsRxTimeDisplay =
                        "GUI RX TIME " + receivedAt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    UpdateGpsHeadingReferenceDisplay();
                    UpdateGpsRateDisplay();
                });
            }

            if (writeLog)
            {
                ConsoleLogHelper.State(
                    "WEB AGENT GPS RX",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "LAT={0:F7} / LON={1:F7} / ALT={2:F3}m / SPEED={3:F2}(MCB) / COURSE={4:F2} / HDOP={5:F2} / SAT={6} / FIX={7} / RX_RATE={8:F1}Hz",
                        gps.Latitude,
                        gps.Longitude,
                        gps.Altitude,
                        gps.Speed,
                        gps.CourseDegrees,
                        gps.Hdop,
                        gps.SatelliteCount,
                        gps.FixStatus,
                        _measuredGpsPacketRate ?? 0.0));
            }
        }

        private void UpdateGpsImuRawReference(double yaw)
        {
            lock (_gpsTelemetrySync)
            {
                _latestImuRawYawDegrees = yaw;
            }

            RunOnUiThread(UpdateGpsHeadingReferenceDisplay);
        }

        private void UpdateGpsHeadingReferenceDisplay()
        {
            string gpsText = _latestGpsCourseDegrees.HasValue
                ? _latestGpsCourseDegrees.Value.ToString("F2", CultureInfo.InvariantCulture) + "°"
                : "--";
            string imuText = _latestImuRawYawDegrees.HasValue
                ? _latestImuRawYawDegrees.Value.ToString("F2", CultureInfo.InvariantCulture) + "°"
                : "--";

            double? trueNorth =
                _latestImuRawYawDegrees.HasValue && _storedRpyYawOffset.HasValue
                    ? NormalizeHeadingDegrees(
                        _latestImuRawYawDegrees.Value + _storedRpyYawOffset.Value)
                    : (double?)null;
            string offsetText = _storedRpyYawOffset.HasValue
                ? _storedRpyYawOffset.Value.ToString("F2", CultureInfo.InvariantCulture) + "°"
                : "--";
            string trueNorthText = trueNorth.HasValue
                ? trueNorth.Value.ToString("F2", CultureInfo.InvariantCulture) + "°"
                : "--";

            GpsHeadingReferenceDisplay =
                "GPS COURSE " + gpsText +
                " / IMU RAW YAW " + imuText +
                " / OFFSET " + offsetText +
                " / 진북 " + trueNorthText;
        }

        /// <summary>
        /// WebAgent API의 trueNorthHeadingDegrees 계산 의미와 동일하게
        /// 각도를 0 이상 360 미만으로 정규화한다.
        /// </summary>
        private static double NormalizeHeadingDegrees(double value)
        {
            double normalized = value % 360.0;
            return normalized < 0.0 ? normalized + 360.0 : normalized;
        }

        private void ParseGpsAutoTxRateResponse(byte[] payload)
        {
            if (payload == null || payload.Length != 2)
            {
                SetGpsRateFailure("Failed: 0x30 응답 길이 오류");
                return;
            }

            byte version = payload[0];
            byte frequencyHz = payload[1];
            if (version != 0x01 || frequencyHz > 10)
            {
                SetGpsRateFailure("Failed: 0x30 버전/주파수 범위 오류");
                return;
            }

            _gpsAutoTxRate = frequencyHz;
            RunOnUiThread(() =>
            {
                GpsAutoTxRateInput = frequencyHz.ToString(CultureInfo.InvariantCulture);
                GpsRateStatusText = "현재 설정 조회 완료";
                UpdateGpsRateDisplay();
            });

            ConsoleLogHelper.State(
                "GPS RATE RX",
                "FUNCTION=0x30 / VERSION=" + version + " / HZ=" + frequencyHz);
        }

        private void UpdateGpsRateDisplay()
        {
            string configured = _gpsAutoTxRate.HasValue
                ? _gpsAutoTxRate.Value.ToString(CultureInfo.InvariantCulture) + " Hz"
                : "조회 대기";
            string measured = _measuredGpsPacketRate.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "약 {0:F1} Hz", _measuredGpsPacketRate.Value)
                : "측정 대기";

            CurrentGpsRateDisplay =
                "현재 설정: " + configured + " / GUI 수신 패킷률: " + measured;
        }

        private void HandleGpsUnavailable(byte[] payload)
        {
            if (payload == null || payload.Length < 6)
            {
                return;
            }

            byte status = payload[1];
            byte command2 = payload[3];
            string reason = status == 0x00
                ? "잘못된 요청"
                : status == 0x01
                    ? "GPS 미수신 또는 조회 실패"
                    : "상태 0x" + status.ToString("X2");

            if (command2 == 0xA3)
            {
                SetGpsFailure("Failed: " + reason);
            }
            else if (command2 == 0xF7 || command2 == 0xF9)
            {
                SetGpsRateFailure("Failed: " + reason);
            }
        }

        private void SetGpsFailure(string message)
        {
            RunOnUiThread(() => GpsStatusText = message);
            ConsoleLogHelper.Warning("GPS RX", message);
        }

        private void SetGpsRateFailure(string message)
        {
            RunOnUiThread(() => GpsRateStatusText = message);
            ConsoleLogHelper.Warning("GPS RATE RX", message);
        }

        private static bool TryParseGpsPayload(
            byte[] payload,
            out GpsTelemetryData gps,
            out string error)
        {
            gps = null;
            error = string.Empty;

            if (payload == null || payload.Length != 20)
            {
                error = "Payload 길이 오류 / expected 20";
                return false;
            }

            double latitude = BitConverter.ToInt32(payload, 0) / 10000000.0;
            double longitude = BitConverter.ToInt32(payload, 4) / 10000000.0;
            double altitude = BitConverter.ToInt32(payload, 8) / 1000.0;
            double speed = BitConverter.ToUInt16(payload, 12) / 100.0;
            double course = BitConverter.ToUInt16(payload, 14) / 100.0;
            double hdop = BitConverter.ToUInt16(payload, 16) / 100.0;

            if (latitude < -90.0 || latitude > 90.0 ||
                longitude < -180.0 || longitude > 180.0)
            {
                error = "위도/경도 범위 오류";
                return false;
            }

            if (course < 0.0 || course > 359.99)
            {
                error = "GPS 진행 방향 범위 오류";
                return false;
            }

            if (hdop < 0.0 || hdop > 655.35)
            {
                error = "HDOP 범위 오류";
                return false;
            }

            gps = new GpsTelemetryData(
                latitude,
                longitude,
                altitude,
                speed,
                course,
                hdop,
                payload[18],
                payload[19]);
            return true;
        }

        private static void VerifyGpsProtocolEncoding()
        {
            try
            {
                VerifyGpsPacket("GPS QUERY", ControlCommandService.BuildGpsTelemetryQueryPacket(),
                    "FF 01 00 A3 00 00 A4");
                VerifyGpsPacket("GPS RATE 0HZ", ControlCommandService.BuildGpsAutoTxRatePacket(0),
                    "FF 01 00 F7 00 00 F8");
                VerifyGpsPacket("GPS RATE 1HZ", ControlCommandService.BuildGpsAutoTxRatePacket(1),
                    "FF 01 00 F7 01 00 F9");
                VerifyGpsPacket("GPS RATE 10HZ", ControlCommandService.BuildGpsAutoTxRatePacket(10),
                    "FF 01 00 F7 0A 00 02");
                VerifyGpsPacket("GPS RATE QUERY", ControlCommandService.BuildGpsAutoTxRateQueryPacket(),
                    "FF 01 00 F9 00 00 FA");

                LAPacketParser parser = new LAPacketParser();
                byte[] rateResponse = { 0xFF, 0x30, 0x02, 0x00, 0x01, 0x0A, 0x3D };
                if (parser.Parse(new[] { rateResponse[0], rateResponse[1], rateResponse[2] }).Count != 0 ||
                    parser.Parse(new[] { rateResponse[3], rateResponse[4], rateResponse[5], rateResponse[6] }).Count != 1)
                {
                    throw new InvalidOperationException("Function 0x30 split-frame assembly failed.");
                }

                byte[] sample = new byte[20];
                Buffer.BlockCopy(BitConverter.GetBytes(364186235), 0, sample, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(1274118881), 0, sample, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(100123), 0, sample, 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)123), 0, sample, 12, 2);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)12345), 0, sample, 14, 2);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)85), 0, sample, 16, 2);
                sample[18] = 12;
                sample[19] = 3;

                if (!TryParseGpsPayload(sample, out GpsTelemetryData gps, out string error) ||
                    Math.Abs(gps.Latitude - 36.4186235) > 0.00000001 ||
                    Math.Abs(gps.Longitude - 127.4118881) > 0.00000001 ||
                    Math.Abs(gps.CourseDegrees - 123.45) > 0.001 ||
                    gps.SatelliteCount != 12 || gps.FixStatus != 3)
                {
                    throw new InvalidOperationException("GPS 20Byte sample parse failed / " + error);
                }

                if (TryParseGpsPayload(new byte[19], out _, out _))
                {
                    throw new InvalidOperationException("Invalid GPS payload length was accepted.");
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("GPS PROTOCOL SELF TEST", "Protocol verification failed", ex);
            }
        }

        private static void VerifyGpsPacket(string name, byte[] packet, string expected)
        {
            string actual = ToHex(packet);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    name + " mismatch / EXPECTED=" + expected + " / ACTUAL=" + actual);
            }
        }

        private sealed class GpsTelemetryData
        {
            public GpsTelemetryData(
                double latitude,
                double longitude,
                double altitude,
                double speed,
                double courseDegrees,
                double hdop,
                byte satelliteCount,
                byte fixStatus)
            {
                Latitude = latitude;
                Longitude = longitude;
                Altitude = altitude;
                Speed = speed;
                CourseDegrees = courseDegrees;
                Hdop = hdop;
                SatelliteCount = satelliteCount;
                FixStatus = fixStatus;
            }

            public double Latitude { get; }
            public double Longitude { get; }
            public double Altitude { get; }
            public double Speed { get; }
            public double CourseDegrees { get; }
            public double Hdop { get; }
            public byte SatelliteCount { get; }
            public byte FixStatus { get; }
        }
    }
}
