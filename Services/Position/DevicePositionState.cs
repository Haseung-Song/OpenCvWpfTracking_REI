using OpenCvWpfTracking.Models.Position;
using System;

namespace OpenCvWpfTracking.Services.Position
{
    /// <summary>
    /// WebAgent 상태 패킷으로 갱신되는 현재 장비 상태를 보관한다.
    /// 모든 갱신과 캡처는 짧은 lock 구간에서 수행하며 파일·UI 작업은 하지 않는다.
    /// </summary>
    public sealed class DevicePositionState
    {
        private readonly object _sync = new object();

        private bool _connected;

        private double? _pan;

        private double? _tilt;

        private int? _eoZoom;

        private int? _eoFocus;

        private int? _irZoom;

        private int? _irFocus;

        private double? _latitude;

        private double? _longitude;

        private double? _altitude;

        private double? _gpsSpeed;

        private double? _gpsCourse;

        private double? _gpsHdop;

        private int? _gpsSatelliteCount;

        private int? _gpsFixStatus;

        private double? _roll;

        private double? _pitch;

        private double? _yaw;

        private DateTimeOffset? _ptzUpdatedAt;

        private DateTimeOffset? _eoLensUpdatedAt;

        private DateTimeOffset? _irLensUpdatedAt;

        private DateTimeOffset? _gpsUpdatedAt;

        private DateTimeOffset? _imuUpdatedAt;

        private bool _gpsValueValid;

        private bool _imuValueValid;

        /// <summary>
        /// 장비 연결 상태를 위치 데이터 유효성 관리에 반영한다.
        /// </summary>
        public void SetConnected(bool connected)
        {
            lock (_sync)
            {
                if (_connected == connected)
                {
                    return;
                }

                _connected = connected;

                if (!connected)
                {
                    // 마지막 숫자는 진단용으로 남기되 다음 연결의 정상값으로 사용하지 않는다.
                    _ptzUpdatedAt = null;
                    _eoLensUpdatedAt = null;
                    _irLensUpdatedAt = null;
                    _gpsUpdatedAt = null;
                    _imuUpdatedAt = null;
                    _gpsValueValid = false;
                    _imuValueValid = false;
                }
            }
        }

        /// <summary>
        /// 수신한 PTZ와 EO 렌즈 값을 위치 상태에 반영한다.
        /// </summary>
        public void UpdatePtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _pan = pan;
                _tilt = tilt;
                _eoZoom = eoZoom;
                _eoFocus = eoFocus;
                _ptzUpdatedAt = receivedAt;
                _eoLensUpdatedAt = receivedAt;
            }
        }

        /// <summary>
        /// 분리 텔레메트리의 PT만 갱신한다. 렌즈의 수신 시각은 변경하지 않는다.
        /// </summary>
        public void UpdatePtz(double pan, double tilt, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _pan = pan;
                _tilt = tilt;
                _ptzUpdatedAt = receivedAt;
            }
        }

        /// <summary>
        /// EO 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateEoLens(int eoZoom, int eoFocus, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _eoZoom = eoZoom;
                _eoFocus = eoFocus;
                _eoLensUpdatedAt = receivedAt;
            }
        }

        /// <summary>
        /// IR 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateIrLens(int irZoom, int irFocus, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _irZoom = irZoom;
                _irFocus = irFocus;
                _irLensUpdatedAt = receivedAt;
            }
        }

        /// <summary>
        /// GPS 위치·이동·수신 품질을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateGps(double latitude, double longitude, double altitude, double speed, double course, double hdop, int satelliteCount, int fixStatus, bool valid, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _latitude = latitude;
                _longitude = longitude;
                _altitude = altitude;
                _gpsSpeed = speed;
                _gpsCourse = course;
                _gpsHdop = hdop;
                _gpsSatelliteCount = satelliteCount;
                _gpsFixStatus = fixStatus;
                _gpsValueValid = valid;
                _gpsUpdatedAt = receivedAt;
            }
        }

        /// <summary>
        /// IMU Roll·Pitch·Yaw 응답을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateImu(double roll, double pitch, double yaw, bool valid, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _roll = roll;
                _pitch = pitch;
                _yaw = yaw;
                _imuValueValid = valid;
                _imuUpdatedAt = receivedAt;
            }
        }

        /// <summary>
        /// 현재 시각의 좌표·렌즈·GPS·IMU 값과 유효 상태를 하나의 위치 스냅샷으로 만든다.
        /// </summary>
        public PositionSnapshot Capture(string source, int? presetId, TimeSpan maximumPtzLensAge, TimeSpan maximumTelemetryAge, DateTimeOffset capturedAt)
        {
            lock (_sync)
            {
                PositionDataStatus ptzStatus = ResolveStatus(_ptzUpdatedAt, true, maximumPtzLensAge, capturedAt);

                PositionDataStatus eoLensStatus = ResolveStatus(_eoLensUpdatedAt, true, maximumPtzLensAge, capturedAt);

                PositionDataStatus irLensStatus = ResolveStatus(_irLensUpdatedAt, true, maximumPtzLensAge, capturedAt);

                PositionDataStatus gpsStatus = ResolveStatus(_gpsUpdatedAt, _gpsValueValid, maximumTelemetryAge, capturedAt);

                PositionDataStatus imuStatus = ResolveStatus(_imuUpdatedAt, _imuValueValid, maximumTelemetryAge, capturedAt);

                return new PositionSnapshot(capturedAt, source, presetId, ptzStatus == PositionDataStatus.Valid ? _pan : null, ptzStatus == PositionDataStatus.Valid ? _tilt : null, eoLensStatus == PositionDataStatus.Valid ? _eoZoom : null, eoLensStatus == PositionDataStatus.Valid ? _eoFocus : null, irLensStatus == PositionDataStatus.Valid ? _irZoom : null, irLensStatus == PositionDataStatus.Valid ? _irFocus : null, gpsStatus == PositionDataStatus.Valid ? _latitude : null, gpsStatus == PositionDataStatus.Valid ? _longitude : null, gpsStatus == PositionDataStatus.Valid ? _altitude : null, gpsStatus == PositionDataStatus.Valid ? _gpsSpeed : null, gpsStatus == PositionDataStatus.Valid ? _gpsCourse : null, gpsStatus == PositionDataStatus.Valid ? _gpsHdop : null, gpsStatus == PositionDataStatus.Valid ? _gpsSatelliteCount : null, gpsStatus == PositionDataStatus.Valid ? _gpsFixStatus : null, imuStatus == PositionDataStatus.Valid ? _roll : null, imuStatus == PositionDataStatus.Valid ? _pitch : null, imuStatus == PositionDataStatus.Valid ? _yaw : null, ptzStatus, eoLensStatus, irLensStatus, gpsStatus, imuStatus, _connected);
            }
        }

        /// <summary>
        /// 연결 여부와 마지막 수신 시각을 바탕으로 데이터 유효 상태를 결정한다.
        /// </summary>
        private PositionDataStatus ResolveStatus(DateTimeOffset? updatedAt, bool valueValid, TimeSpan maximumAge, DateTimeOffset now)
        {
            if (!_connected)
            {
                return PositionDataStatus.Disconnected;
            }

            if (!updatedAt.HasValue)
            {
                return PositionDataStatus.NotReceived;
            }

            if (!valueValid)
            {
                return PositionDataStatus.Invalid;
            }

            if (now - updatedAt.Value > maximumAge)
            {
                return PositionDataStatus.Stale;
            }
            else
            {
                return PositionDataStatus.Valid;
            }
        }
    }
}
