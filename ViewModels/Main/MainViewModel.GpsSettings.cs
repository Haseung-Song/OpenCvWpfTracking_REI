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

        public ICommand RequestGpsTelemetryCommand
        {
            get { return _gpsSettingsFeature.RequestGpsTelemetryCommand; }
            private set { _gpsSettingsFeature.RequestGpsTelemetryCommand = value; }
        }

        public ICommand ApplyGpsAutoTxRateCommand
        {
            get { return _gpsSettingsFeature.ApplyGpsAutoTxRateCommand; }
            private set { _gpsSettingsFeature.ApplyGpsAutoTxRateCommand = value; }
        }

        public ICommand RequestGpsAutoTxRateCommand
        {
            get { return _gpsSettingsFeature.RequestGpsAutoTxRateCommand; }
            private set { _gpsSettingsFeature.RequestGpsAutoTxRateCommand = value; }
        }

        public string GpsAutoTxRateInput
        {
            get { return _gpsSettingsFeature.GpsAutoTxRateInput; }
            set { _gpsSettingsFeature.GpsAutoTxRateInput = value; }
        }

        public string GpsStatusText
        {
            get { return _gpsSettingsFeature.GpsStatusText; }
            private set { _gpsSettingsFeature.GpsStatusText = value; }
        }

        public string GpsCoordinatesDisplay
        {
            get { return _gpsSettingsFeature.GpsCoordinatesDisplay; }
            private set { _gpsSettingsFeature.GpsCoordinatesDisplay = value; }
        }

        public string GpsMotionDisplay
        {
            get { return _gpsSettingsFeature.GpsMotionDisplay; }
            private set { _gpsSettingsFeature.GpsMotionDisplay = value; }
        }

        public string GpsQualityDisplay
        {
            get { return _gpsSettingsFeature.GpsQualityDisplay; }
            private set { _gpsSettingsFeature.GpsQualityDisplay = value; }
        }

        public string GpsRxTimeDisplay
        {
            get { return _gpsSettingsFeature.GpsRxTimeDisplay; }
            private set { _gpsSettingsFeature.GpsRxTimeDisplay = value; }
        }

        public string GpsHeadingReferenceDisplay
        {
            get { return _gpsSettingsFeature.GpsHeadingReferenceDisplay; }
            private set { _gpsSettingsFeature.GpsHeadingReferenceDisplay = value; }
        }

        public string GpsRateStatusText
        {
            get { return _gpsSettingsFeature.GpsRateStatusText; }
            private set { _gpsSettingsFeature.GpsRateStatusText = value; }
        }

        public string CurrentGpsRateDisplay
        {
            get { return _gpsSettingsFeature.CurrentGpsRateDisplay; }
            private set { _gpsSettingsFeature.CurrentGpsRateDisplay = value; }
        }

        /// <summary>
        /// GPS 조회·전송 주기 명령과 현재 상태 표시를 준비한다.
        /// </summary>
        private void InitializeGpsSettings()
        {
            _gpsSettingsFeature.InitializeGpsSettings();
        }

        /// <summary>
        /// 현재 연결 장비에 GPS 상태 조회를 요청한다.
        /// </summary>
        private void RequestGpsTelemetry()
        {
            _gpsSettingsFeature.RequestGpsTelemetry();
        }

        /// <summary>
        /// 입력한 GPS 자동 전송 주기를 검증한 뒤 장비에 설정을 요청한다.
        /// </summary>
        private void ApplyGpsAutoTxRate()
        {
            _gpsSettingsFeature.ApplyGpsAutoTxRate();
        }

        /// <summary>
        /// 장비에 저장된 GPS 자동 전송 주기를 조회한다.
        /// </summary>
        private void RequestGpsAutoTxRate()
        {
            _gpsSettingsFeature.RequestGpsAutoTxRate();
        }

        private void UpdateGpsHeadingReferenceDisplay()
        {
            _gpsSettingsFeature.UpdateGpsHeadingReferenceDisplay();
        }

        internal sealed class GpsTelemetryData
        {
            public GpsTelemetryData(double latitude, double longitude, double altitude, double speed, double courseDegrees, double hdop, byte satelliteCount, byte fixStatus)
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
