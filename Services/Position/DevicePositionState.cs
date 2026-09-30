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

        public void UpdatePtzAndEoLens(
            double pan,
            double tilt,
            int eoZoom,
            int eoFocus,
            DateTimeOffset receivedAt)
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

        public void UpdateEoLens(int eoZoom, int eoFocus, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _eoZoom = eoZoom;
                _eoFocus = eoFocus;
                _eoLensUpdatedAt = receivedAt;
            }
        }

        public void UpdateIrLens(int irZoom, int irFocus, DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _irZoom = irZoom;
                _irFocus = irFocus;
                _irLensUpdatedAt = receivedAt;
            }
        }

        public void UpdateGps(
            double latitude,
            double longitude,
            double altitude,
            bool valid,
            DateTimeOffset receivedAt)
        {
            lock (_sync)
            {
                _latitude = latitude;
                _longitude = longitude;
                _altitude = altitude;
                _gpsValueValid = valid;
                _gpsUpdatedAt = receivedAt;
            }
        }

        public void UpdateImu(
            double roll,
            double pitch,
            double yaw,
            bool valid,
            DateTimeOffset receivedAt)
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

        public PositionSnapshot Capture(
            string source,
            int? presetId,
            TimeSpan maximumPtzLensAge,
            TimeSpan maximumTelemetryAge,
            DateTimeOffset capturedAt)
        {
            lock (_sync)
            {
                PositionDataStatus ptzStatus = ResolveStatus(_ptzUpdatedAt, true, maximumPtzLensAge, capturedAt);
                PositionDataStatus eoLensStatus = ResolveStatus(_eoLensUpdatedAt, true, maximumPtzLensAge, capturedAt);
                PositionDataStatus irLensStatus = ResolveStatus(_irLensUpdatedAt, true, maximumPtzLensAge, capturedAt);
                PositionDataStatus gpsStatus = ResolveStatus(_gpsUpdatedAt, _gpsValueValid, maximumTelemetryAge, capturedAt);
                PositionDataStatus imuStatus = ResolveStatus(_imuUpdatedAt, _imuValueValid, maximumTelemetryAge, capturedAt);

                return new PositionSnapshot(
                    capturedAt,
                    source,
                    presetId,
                    ptzStatus == PositionDataStatus.Valid ? _pan : null,
                    ptzStatus == PositionDataStatus.Valid ? _tilt : null,
                    eoLensStatus == PositionDataStatus.Valid ? _eoZoom : null,
                    eoLensStatus == PositionDataStatus.Valid ? _eoFocus : null,
                    irLensStatus == PositionDataStatus.Valid ? _irZoom : null,
                    irLensStatus == PositionDataStatus.Valid ? _irFocus : null,
                    gpsStatus == PositionDataStatus.Valid ? _latitude : null,
                    gpsStatus == PositionDataStatus.Valid ? _longitude : null,
                    gpsStatus == PositionDataStatus.Valid ? _altitude : null,
                    imuStatus == PositionDataStatus.Valid ? _roll : null,
                    imuStatus == PositionDataStatus.Valid ? _pitch : null,
                    imuStatus == PositionDataStatus.Valid ? _yaw : null,
                    ptzStatus,
                    eoLensStatus,
                    irLensStatus,
                    gpsStatus,
                    imuStatus,
                    _connected);
            }
        }

        private PositionDataStatus ResolveStatus(
            DateTimeOffset? updatedAt,
            bool valueValid,
            TimeSpan maximumAge,
            DateTimeOffset now)
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

            return now - updatedAt.Value > maximumAge
                ? PositionDataStatus.Stale
                : PositionDataStatus.Valid;
        }
    }
}
