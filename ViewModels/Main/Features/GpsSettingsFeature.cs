using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>위치 센서 설정에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IGpsSettingsFeatureContext
    {
        IControlCommandServicePort _controlCommandService { get; }

        ITcpClientServicePort _laTcpService { get; }

        ref double? _storedRpyYawOffset { get; }

        bool IsEnvironmentStatusSelected { get; }

        bool IsOperationCommandEnabled { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        void UpdatePositionGps(double latitude, double longitude, double altitude, double speed, double course, double hdop, int satelliteCount, int fixStatus, bool valid);

        Action RequestGpsTelemetryCallback { get; }

        Action ApplyGpsAutoTxRateCallback { get; }

        Action RequestGpsAutoTxRateCallback { get; }

        Action UpdateGpsHeadingReferenceDisplayCallback { get; }
    }

    /// <summary>위치 센서 설정 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class GpsSettingsFeature
    {

        #region [협력 객체와 초기화]

        private readonly IGpsSettingsFeatureContext _context;

        internal GpsSettingsFeature(IGpsSettingsFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _gpsAutoTxRateInput = "1";
            _gpsStatusText = "GPS 수신 대기";
            _gpsCoordinatesDisplay = "LAT -- / LON -- / ALT --";
            _gpsMotionDisplay = "SPEED -- / GPS COURSE --";
            _gpsQualityDisplay = "HDOP -- / SAT -- / FIX --";
            _gpsRxTimeDisplay = "GUI RX TIME --";
            _gpsHeadingReferenceDisplay = "GPS COURSE -- / IMU RAW YAW --";
            _gpsRateStatusText = "Ready";
            _currentGpsRateDisplay = "현재 설정: 조회 대기 / GUI 수신 패킷률: 측정 대기";
        }

        #endregion

        #region [상태 필드]

        internal readonly object _gpsTelemetrySync = new object();

        internal readonly Stopwatch _gpsRxRateWindow = Stopwatch.StartNew();

        internal int _gpsRxCount;

        internal long _lastGpsUiUpdateTimestamp;

        internal long _lastGpsLogTimestamp;

        internal double? _latestGpsCourseDegrees;

        internal double? _latestImuRawYawDegrees;

        internal int? _gpsAutoTxRate;

        internal double? _measuredGpsPacketRate;

        internal string _gpsAutoTxRateInput;

        internal string _gpsStatusText;

        internal string _gpsCoordinatesDisplay;

        internal string _gpsMotionDisplay;

        internal string _gpsQualityDisplay;

        internal string _gpsRxTimeDisplay;

        internal string _gpsHeadingReferenceDisplay;

        internal string _gpsRateStatusText;

        internal string _currentGpsRateDisplay;

        #endregion

        #region [화면 속성과 이벤트]

        internal ICommand RequestGpsTelemetryCommand { get; set; }

        internal ICommand ApplyGpsAutoTxRateCommand { get; set; }

        internal ICommand RequestGpsAutoTxRateCommand { get; set; }

        internal string GpsAutoTxRateInput
        {
            get { return _gpsAutoTxRateInput; }

            set
            {
                if (_gpsAutoTxRateInput == value)
                {
                    return;
                }

                _gpsAutoTxRateInput = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GpsStatusText
        {
            get { return _gpsStatusText; }

            set
            {
                if (_gpsStatusText == value)
                {
                    return;
                }

                _gpsStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GpsCoordinatesDisplay
        {
            get { return _gpsCoordinatesDisplay; }

            set
            {
                if (_gpsCoordinatesDisplay == value)
                {
                    return;
                }

                _gpsCoordinatesDisplay = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GpsMotionDisplay
        {
            get { return _gpsMotionDisplay; }

            set
            {
                if (_gpsMotionDisplay == value)
                {
                    return;
                }

                _gpsMotionDisplay = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GpsQualityDisplay
        {
            get { return _gpsQualityDisplay; }

            set
            {
                if (_gpsQualityDisplay == value)
                {
                    return;
                }

                _gpsQualityDisplay = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GpsRxTimeDisplay
        {
            get { return _gpsRxTimeDisplay; }

            set
            {
                if (_gpsRxTimeDisplay == value)
                {
                    return;
                }

                _gpsRxTimeDisplay = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GpsHeadingReferenceDisplay
        {
            get { return _gpsHeadingReferenceDisplay; }

            set
            {
                if (_gpsHeadingReferenceDisplay == value)
                {
                    return;
                }

                _gpsHeadingReferenceDisplay = value;

                _context.OnPropertyChanged();
            }
        }

        internal string GpsRateStatusText
        {
            get { return _gpsRateStatusText; }

            set
            {
                if (_gpsRateStatusText == value)
                {
                    return;
                }

                _gpsRateStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        internal string CurrentGpsRateDisplay
        {
            get { return _currentGpsRateDisplay; }

            set
            {
                if (_currentGpsRateDisplay == value)
                {
                    return;
                }

                _currentGpsRateDisplay = value;

                _context.OnPropertyChanged();
            }
        }

        #endregion

        #region [동작 처리]

        internal void InitializeGpsSettings()
        {
            RequestGpsTelemetryCommand = new RelayCommand(_context.RequestGpsTelemetryCallback);

            ApplyGpsAutoTxRateCommand = new RelayCommand(_context.ApplyGpsAutoTxRateCallback);

            RequestGpsAutoTxRateCommand = new RelayCommand(_context.RequestGpsAutoTxRateCallback);

            VerifyGpsProtocolEncoding();
        }

        internal bool CanUseGpsSettings(out string reason)
        {
            // 2026-10-06: HOME/ZERO, AUTO SCAN, panorama capture/processing 중에는
            // 다른 장비 명령과 동일하게 GPS 조회 및 설정 송신을 차단한다.
            if (!_context.IsOperationCommandEnabled)
            {
                reason = "운용 작업 중 GPS 제어 잠금";

                return false;
            }

            if (!_context.IsEnvironmentStatusSelected)
            {
                reason = "WebAgent 전용 기능";

                return false;
            }

            if (_context._laTcpService == null || !_context._laTcpService.IsConnected)
            {
                reason = "Failed: WebAgent 미연결";

                return false;
            }

            reason = string.Empty;

            return true;
        }

        internal void RequestGpsTelemetry()
        {
            if (!CanUseGpsSettings(out string reason))
            {
                GpsStatusText = reason;

                ConsoleLogHelper.Warning("GPS QUERY", reason);

                return;
            }

            GpsStatusText = "단건 조회 응답 대기";

            bool sent = _context._controlCommandService.RequestGpsTelemetry();

            ConsoleLogHelper.Command("GPS QUERY TX", "CMD2=0xA3 / PACKET=FF 01 00 A3 00 00 A4 / SENT=" + sent);

            if (!sent)
            {
                GpsStatusText = "Failed: 단건 조회 전송 실패";
            }
        }

        internal void ApplyGpsAutoTxRate()
        {
            if (!CanUseGpsSettings(out string reason))
            {
                GpsRateStatusText = reason;

                ConsoleLogHelper.Warning("GPS RATE", reason);

                return;
            }

            if (!int.TryParse(GpsAutoTxRateInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out int frequencyHz) || frequencyHz < 0 || frequencyHz > 10)
            {
                GpsRateStatusText = "Failed: 0~10 정수만 입력하세요";

                ConsoleLogHelper.Warning("GPS RATE", "Invalid input / expected integer 0~10 Hz");

                return;
            }

            byte frequency = (byte)frequencyHz;

            byte[] packet = ControlCommandService.BuildGpsAutoTxRatePacket(frequency);

            GpsRateStatusText = "설정 응답 대기";

            bool sent = _context._controlCommandService.SetGpsAutoTxRate(frequency);

            ConsoleLogHelper.Command("GPS RATE SET TX", "HZ=" + frequencyHz + " / PACKET=" + ToHex(packet) + " / SENT=" + sent);

            if (!sent)
            {
                GpsRateStatusText = "Failed: 주파수 설정 전송 실패";
            }
        }

        internal void RequestGpsAutoTxRate()
        {
            if (!CanUseGpsSettings(out string reason))
            {
                GpsRateStatusText = reason;

                ConsoleLogHelper.Warning("GPS RATE QUERY", reason);

                return;
            }

            GpsRateStatusText = "현재 설정 조회 중";

            bool sent = _context._controlCommandService.RequestGpsAutoTxRate();

            ConsoleLogHelper.Command("GPS RATE QUERY TX", "CMD2=0xF9 / PACKET=FF 01 00 F9 00 00 FA / SENT=" + sent);

            if (!sent)
            {
                GpsRateStatusText = "Failed: 주파수 조회 전송 실패";
            }
        }

        internal void RequestGpsAutoTxRateAfterConnection()
        {
            if (_context._laTcpService == null || !_context._laTcpService.IsConnected)
            {
                return;
            }

            bool sent = _context._controlCommandService.RequestGpsAutoTxRate();

            ConsoleLogHelper.Command("GPS RATE CONNECT QUERY TX", "PACKET=FF 01 00 F9 00 00 FA / SENT=" + sent);
        }

        internal void ParseWebAgentGps(byte[] payload)
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
            _context.UpdatePositionGps(gps.Latitude, gps.Longitude, gps.Altitude, gps.Speed, gps.CourseDegrees, gps.Hdop, gps.SatelliteCount, gps.FixStatus, fixValid);

            RecordGpsTelemetry(gps, receivedAt, fixValid);
        }

        internal void RecordGpsTelemetry(GpsTelemetryData gps, DateTimeOffset receivedAt, bool fixValid)
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

                    GpsCoordinatesDisplay = string.Format(CultureInfo.InvariantCulture, "LAT {0:F7}° / LON {1:F7}° / ALT {2:F3} m", gps.Latitude, gps.Longitude, gps.Altitude);

                    GpsMotionDisplay = string.Format(CultureInfo.InvariantCulture, "SPEED {0:F2} (MCB ×0.01) / GPS COURSE {1:F2}°", gps.Speed, gps.CourseDegrees);

                    GpsQualityDisplay = string.Format(CultureInfo.InvariantCulture, "HDOP {0:F2} / SAT {1} / FIX {2}", gps.Hdop, gps.SatelliteCount, gps.FixStatus);

                    GpsRxTimeDisplay = "GUI RX TIME " + receivedAt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

                    UpdateGpsHeadingReferenceDisplay();

                    UpdateGpsRateDisplay();
                });
            }

            if (writeLog)
            {
                ConsoleLogHelper.State("WEB AGENT GPS RX", string.Format(CultureInfo.InvariantCulture, "LAT={0:F7} / LON={1:F7} / ALT={2:F3}m / SPEED={3:F2}(MCB) / COURSE={4:F2} / HDOP={5:F2} / SAT={6} / FIX={7} / RX_RATE={8:F1}Hz", gps.Latitude, gps.Longitude, gps.Altitude, gps.Speed, gps.CourseDegrees, gps.Hdop, gps.SatelliteCount, gps.FixStatus, _measuredGpsPacketRate ?? 0.0));
            }
        }

        internal void UpdateGpsImuRawReference(double yaw)
        {
            lock (_gpsTelemetrySync)
            {
                _latestImuRawYawDegrees = yaw;
            }

            RunOnUiThread(_context.UpdateGpsHeadingReferenceDisplayCallback);
        }

        internal void UpdateGpsHeadingReferenceDisplay()
        {
            string gpsText = _latestGpsCourseDegrees.HasValue ? _latestGpsCourseDegrees.Value.ToString("F2", CultureInfo.InvariantCulture) + "°" : "--";

            string imuText = _latestImuRawYawDegrees.HasValue ? _latestImuRawYawDegrees.Value.ToString("F2", CultureInfo.InvariantCulture) + "°" : "--";

            double? trueNorth = _latestImuRawYawDegrees.HasValue && _context._storedRpyYawOffset.HasValue ? NormalizeHeadingDegrees(_latestImuRawYawDegrees.Value + _context._storedRpyYawOffset.Value) : (double?)null;

            string offsetText = _context._storedRpyYawOffset.HasValue ? _context._storedRpyYawOffset.Value.ToString("F2", CultureInfo.InvariantCulture) + "°" : "--";

            string trueNorthText = trueNorth.HasValue ? trueNorth.Value.ToString("F2", CultureInfo.InvariantCulture) + "°" : "--";

            GpsHeadingReferenceDisplay = "GPS COURSE " + gpsText + " / IMU RAW YAW " + imuText + " / OFFSET " + offsetText + " / 진북 " + trueNorthText;
        }

        internal static double NormalizeHeadingDegrees(double value)
        {
            double normalized = value % 360.0;

            if (normalized < 0.0)
            {
                return normalized + 360.0;
            }
            else
            {
                return normalized;
            }
        }

        internal void ParseGpsAutoTxRateResponse(byte[] payload)
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

            ConsoleLogHelper.State("GPS RATE RX", "FUNCTION=0x30 / VERSION=" + version + " / HZ=" + frequencyHz);
        }

        internal void UpdateGpsRateDisplay()
        {
            string configured = _gpsAutoTxRate.HasValue ? _gpsAutoTxRate.Value.ToString(CultureInfo.InvariantCulture) + " Hz" : "조회 대기";

            string measured = _measuredGpsPacketRate.HasValue ? string.Format(CultureInfo.InvariantCulture, "약 {0:F1} Hz", _measuredGpsPacketRate.Value) : "측정 대기";

            CurrentGpsRateDisplay = "현재 설정: " + configured + " / GUI 수신 패킷률: " + measured;
        }

        internal void HandleGpsUnavailable(byte[] payload)
        {
            if (payload == null || payload.Length < 6)
            {
                return;
            }

            byte status = payload[1];

            byte command2 = payload[3];

            string reason = status == 0x00 ? "잘못된 요청" : status == 0x01 ? "GPS 미수신 또는 조회 실패" : "상태 0x" + status.ToString("X2");

            if (command2 == 0xA3)
            {
                SetGpsFailure("Failed: " + reason);
            }
            else if (command2 == 0xF7 || command2 == 0xF9)
            {
                SetGpsRateFailure("Failed: " + reason);
            }
        }

        internal void SetGpsFailure(string message)
        {
            RunOnUiThread(() => GpsStatusText = message);

            ConsoleLogHelper.Warning("GPS RX", message);
        }

        internal void SetGpsRateFailure(string message)
        {
            RunOnUiThread(() => GpsRateStatusText = message);

            ConsoleLogHelper.Warning("GPS RATE RX", message);
        }

        internal static bool TryParseGpsPayload(byte[] payload, out GpsTelemetryData gps, out string error)
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

            if (latitude < -90.0 || latitude > 90.0 || longitude < -180.0 || longitude > 180.0)
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

            gps = new GpsTelemetryData(latitude, longitude, altitude, speed, course, hdop, payload[18], payload[19]);

            return true;
        }

        internal static void VerifyGpsProtocolEncoding()
        {
            try
            {
                VerifyGpsPacket("GPS QUERY", ControlCommandService.BuildGpsTelemetryQueryPacket(), "FF 01 00 A3 00 00 A4");

                VerifyGpsPacket("GPS RATE 0HZ", ControlCommandService.BuildGpsAutoTxRatePacket(0), "FF 01 00 F7 00 00 F8");

                VerifyGpsPacket("GPS RATE 1HZ", ControlCommandService.BuildGpsAutoTxRatePacket(1), "FF 01 00 F7 01 00 F9");

                VerifyGpsPacket("GPS RATE 10HZ", ControlCommandService.BuildGpsAutoTxRatePacket(10), "FF 01 00 F7 0A 00 02");

                VerifyGpsPacket("GPS RATE QUERY", ControlCommandService.BuildGpsAutoTxRateQueryPacket(), "FF 01 00 F9 00 00 FA");

                LAPacketParser parser = new LAPacketParser();

                byte[] rateResponse =
                {
                    0xFF,
                    0x30,
                    0x02,
                    0x00,
                    0x01,
                    0x0A,
                    0x3D
                };

                if (parser.Parse(new[] { rateResponse[0], rateResponse[1], rateResponse[2] }).Count != 0 || parser.Parse(new[] { rateResponse[3], rateResponse[4], rateResponse[5], rateResponse[6] }).Count != 1)
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

                if (!TryParseGpsPayload(sample, out GpsTelemetryData gps, out string error) || Math.Abs(gps.Latitude - 36.4186235) > 0.00000001 || Math.Abs(gps.Longitude - 127.4118881) > 0.00000001 || Math.Abs(gps.CourseDegrees - 123.45) > 0.001 || gps.SatelliteCount != 12 || gps.FixStatus != 3)
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

        internal static void VerifyGpsPacket(string name, byte[] packet, string expected)
        {
            string actual = ToHex(packet);

            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(name + " mismatch / EXPECTED=" + expected + " / ACTUAL=" + actual);
            }
        }

        #endregion

    }
}
