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

        /// <summary>
        /// 위치 스냅샷을 생성할 장비 상태 공급자를 연결한다.
        /// </summary>
        public PositionSnapshotService(TimeSpan maximumPtzLensAge, TimeSpan maximumTelemetryAge)
        {
            _maximumPtzLensAge = maximumPtzLensAge > TimeSpan.Zero ? maximumPtzLensAge : TimeSpan.FromSeconds(3);

            _maximumTelemetryAge = maximumTelemetryAge > TimeSpan.Zero ? maximumTelemetryAge : TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 장비 연결 상태를 위치 데이터 유효성 관리에 반영한다.
        /// </summary>
        public void SetConnected(bool connected)
        {
            _state.SetConnected(connected);
        }

        /// <summary>
        /// 수신한 PTZ와 EO 렌즈 값을 위치 상태에 반영한다.
        /// </summary>
        public void UpdatePtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus)
        {
            _state.UpdatePtzAndEoLens(pan, tilt, eoZoom, eoFocus, DateTimeOffset.Now);
        }

        /// <summary>분리 PT 수신은 EO 렌즈를 최신 상태로 승격하지 않는다.</summary>
        public void UpdatePtz(double pan, double tilt, DateTimeOffset receivedAt)
        {
            _state.UpdatePtz(pan, tilt, receivedAt);
        }

        /// <summary>
        /// EO 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateEoLens(int eoZoom, int eoFocus)
        {
            _state.UpdateEoLens(eoZoom, eoFocus, DateTimeOffset.Now);
        }

        /// <summary>
        /// IR 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateIrLens(int irZoom, int irFocus)
        {
            _state.UpdateIrLens(irZoom, irFocus, DateTimeOffset.Now);
        }

        /// <summary>
        /// GPS 위치·이동·수신 품질을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateGps(double latitude, double longitude, double altitude, double speed, double course, double hdop, int satelliteCount, int fixStatus, bool valid)
        {
            _state.UpdateGps(latitude, longitude, altitude, speed, course, hdop, satelliteCount, fixStatus, valid, DateTimeOffset.Now);
        }

        /// <summary>
        /// IMU Roll·Pitch·Yaw 응답을 위치 상태에 반영한다.
        /// </summary>
        public void UpdateImu(double roll, double pitch, double yaw, bool valid)
        {
            _state.UpdateImu(roll, pitch, yaw, valid, DateTimeOffset.Now);
        }

        /// <summary>
        /// 현재 시각의 좌표·렌즈·GPS·IMU 값과 유효 상태를 하나의 위치 스냅샷으로 만든다.
        /// </summary>
        public PositionSnapshot Capture(string source, int? presetId = null)
        {
            return _state.Capture(source, presetId, _maximumPtzLensAge, _maximumTelemetryAge, DateTimeOffset.Now);
        }
    }
}
