using OpenCvWpfTracking.Models.Position;
using System;

namespace OpenCvWpfTracking.Services.Position
{
    /// <summary>
    /// 실시간 장비 상태 갱신과 필요한 순간의 불변 스냅샷 생성을 연결한다.
    /// </summary>
    public sealed class PositionSnapshotService
    {
        private readonly DevicePositionState _state = new DevicePositionState();

        private readonly TimeSpan _maximumPtzLensAge;

        private readonly TimeSpan _maximumTelemetryAge;

        public PositionSnapshotService(
            TimeSpan maximumPtzLensAge,
            TimeSpan maximumTelemetryAge)
        {
            _maximumPtzLensAge = maximumPtzLensAge > TimeSpan.Zero
                ? maximumPtzLensAge
                : TimeSpan.FromSeconds(3);

            _maximumTelemetryAge = maximumTelemetryAge > TimeSpan.Zero
                ? maximumTelemetryAge
                : TimeSpan.FromSeconds(30);
        }

        public void SetConnected(bool connected)
        {
            _state.SetConnected(connected);
        }

        public void UpdatePtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus)
        {
            _state.UpdatePtzAndEoLens(pan, tilt, eoZoom, eoFocus, DateTimeOffset.Now);
        }

        public void UpdateEoLens(int eoZoom, int eoFocus)
        {
            _state.UpdateEoLens(eoZoom, eoFocus, DateTimeOffset.Now);
        }

        public void UpdateIrLens(int irZoom, int irFocus)
        {
            _state.UpdateIrLens(irZoom, irFocus, DateTimeOffset.Now);
        }

        public void UpdateGps(double latitude, double longitude, double altitude, bool valid)
        {
            _state.UpdateGps(latitude, longitude, altitude, valid, DateTimeOffset.Now);
        }

        public void UpdateImu(double roll, double pitch, double yaw, bool valid)
        {
            _state.UpdateImu(roll, pitch, yaw, valid, DateTimeOffset.Now);
        }

        public PositionSnapshot Capture(string source, int? presetId = null)
        {
            return _state.Capture(
                source,
                presetId,
                _maximumPtzLensAge,
                _maximumTelemetryAge,
                DateTimeOffset.Now);
        }
    }
}
