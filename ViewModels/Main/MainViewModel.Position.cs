using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Position;
using OpenCvWpfTracking.Services.Position;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 장비 상태를 PRESET과 Event가 공통으로 사용하는 위치 스냅샷으로 변환한다.
    /// RTSP 프레임 처리와 분리하여 영상 수신·렌더링 부하에 영향을 주지 않는다.
    /// </summary>
    public partial class MainViewModel
    {
        private static readonly TimeSpan PositionStateMaximumAge =
            TimeSpan.FromSeconds(3);

        private static readonly TimeSpan PositionTelemetryMaximumAge =
            TimeSpan.FromSeconds(30);

        private readonly PositionSnapshotService _positionSnapshotService =
            new PositionSnapshotService(
                PositionStateMaximumAge,
                PositionTelemetryMaximumAge);

        private PositionSnapshot _lastPositionSnapshot;

        private int _positionStatusNotificationPending;

        /// <summary>
        /// CURRENT STATUS 하단에서 최근 상태 패킷의 유효성을 간단히 확인한다.
        /// 실제 값은 기존 PTZF 행을 유지하고 여기서는 유효성만 표시한다.
        /// </summary>
        public string PositionStatusText
        {
            get
            {
                PositionSnapshot snapshot =
                    _positionSnapshotService.Capture("UI_STATUS");

                return "PTZ " + ToUiStatus(snapshot.PtzStatus) +
                       "  /  EO " + ToUiStatus(snapshot.EoLensStatus) +
                       "  /  IR " + ToUiStatus(snapshot.IrLensStatus) +
                       "  /  GPS " + ToUiStatus(snapshot.GpsStatus) +
                       "  /  IMU " + ToUiStatus(snapshot.ImuStatus);
            }
        }

        public string LastPositionSnapshotText =>
            _lastPositionSnapshot == null
                ? "LAST SNAPSHOT : --"
                : "LAST SNAPSHOT : " + _lastPositionSnapshot.Source +
                  "  /  " + _lastPositionSnapshot.CapturedAt.ToString("HH:mm:ss") +
                  "  /  " + _lastPositionSnapshot.PositionSummary;

        private void SetPositionConnectionState(bool connected)
        {
            _positionSnapshotService.SetConnected(connected);
            QueuePositionStatusNotification();
        }

        private void UpdatePositionPtzAndEoLens(
            double pan,
            double tilt,
            int eoZoom,
            int eoFocus)
        {
            _positionSnapshotService.UpdatePtzAndEoLens(
                pan,
                tilt,
                eoZoom,
                eoFocus);

            QueuePositionStatusNotification();
        }

        private void UpdatePositionEoLens(int eoZoom, int eoFocus)
        {
            _positionSnapshotService.UpdateEoLens(eoZoom, eoFocus);
            QueuePositionStatusNotification();
        }

        private void UpdatePositionIrLens(int irZoom, int irFocus)
        {
            _positionSnapshotService.UpdateIrLens(irZoom, irFocus);
            QueuePositionStatusNotification();
        }

        private void UpdatePositionGps(
            double latitude,
            double longitude,
            double altitude,
            double speed,
            double course,
            double hdop,
            int satelliteCount,
            int fixStatus,
            bool valid)
        {
            _positionSnapshotService.UpdateGps(
                latitude,
                longitude,
                altitude,
                speed,
                course,
                hdop,
                satelliteCount,
                fixStatus,
                valid);

            QueuePositionStatusNotification();
        }

        private void UpdatePositionImu(
            double roll,
            double pitch,
            double yaw,
            bool valid)
        {
            _positionSnapshotService.UpdateImu(
                roll,
                pitch,
                yaw,
                valid);

            QueuePositionStatusNotification();
        }

        private PositionSnapshot CapturePositionSnapshot(
            string source,
            int? presetId = null)
        {
            try
            {
                PositionSnapshot snapshot =
                    _positionSnapshotService.Capture(source, presetId);

                _lastPositionSnapshot = snapshot;
                QueuePositionStatusNotification(true);
                LogPositionSnapshot(snapshot);
                return snapshot;
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error(
                    "POSITION SNAPSHOT",
                    "Capture failed / SOURCE=" + source,
                    exception);

                return null;
            }
        }

        private void QueuePositionStatusNotification(bool includeLastSnapshot = false)
        {
            Dispatcher dispatcher =
                System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null)
            {
                return;
            }

            if (includeLastSnapshot)
            {
                Interlocked.Exchange(ref _positionLastSnapshotNotificationDirty, 1);
            }

            if (Interlocked.CompareExchange(
                    ref _positionStatusNotificationPending,
                    1,
                    0) != 0)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                bool queued = false;
                try
                {
                    await Task.Delay(StatusUiCoalesceMilliseconds).ConfigureAwait(false);
                    if (dispatcher.HasShutdownStarted)
                    {
                        return;
                    }

                    _ = dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            Interlocked.Exchange(ref _positionStatusNotificationPending, 0);
                            OnPropertyChanged(nameof(PositionStatusText));
                            if (Interlocked.Exchange(
                                    ref _positionLastSnapshotNotificationDirty,
                                    0) != 0)
                            {
                                OnPropertyChanged(nameof(LastPositionSnapshotText));
                            }
                        }));

                    queued = true;
                }
                finally
                {
                    if (!queued)
                    {
                        Interlocked.Exchange(ref _positionStatusNotificationPending, 0);
                    }
                }
            });
        }

        private int _positionLastSnapshotNotificationDirty;

        private static string ToUiStatus(PositionDataStatus status)
        {
            switch (status)
            {
                case PositionDataStatus.Valid:
                    return "OK";
                case PositionDataStatus.Stale:
                    return "STALE";
                case PositionDataStatus.Invalid:
                    return "INVALID";
                case PositionDataStatus.Disconnected:
                    return "OFF";
                default:
                    return "N/A";
            }
        }

        private static void LogPositionSnapshot(PositionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            ConsoleLogHelper.State(
                "POSITION SNAPSHOT",
                "SOURCE=" + snapshot.Source +
                " / PRESET=" + (snapshot.PresetId?.ToString(CultureInfo.InvariantCulture) ?? "-") +
                " / PAN=" + FormatSnapshotValue(snapshot.Pan, snapshot.PtzStatus, "F2") +
                " / TILT=" + FormatSnapshotValue(snapshot.Tilt, snapshot.PtzStatus, "F2") +
                " / EO_ZOOM=" + FormatSnapshotValue(snapshot.EoZoom, snapshot.EoLensStatus) +
                " / IR_ZOOM=" + FormatSnapshotValue(snapshot.IrZoom, snapshot.IrLensStatus) +
                " / PTZ=" + snapshot.PtzStatus +
                " / EO_LENS=" + snapshot.EoLensStatus +
                " / IR_LENS=" + snapshot.IrLensStatus +
                " / GPS=" + snapshot.GpsStatus +
                " / GPS_COURSE=" + FormatSnapshotValue(snapshot.GpsCourse, snapshot.GpsStatus, "F2") +
                " / GPS_HDOP=" + FormatSnapshotValue(snapshot.GpsHdop, snapshot.GpsStatus, "F2") +
                " / GPS_SAT=" + FormatSnapshotValue(snapshot.GpsSatelliteCount, snapshot.GpsStatus) +
                " / GPS_FIX=" + FormatSnapshotValue(snapshot.GpsFixStatus, snapshot.GpsStatus) +
                " / IMU=" + snapshot.ImuStatus +
                " / CONNECTED=" + snapshot.WebAgentConnected +
                " / CAPTURED_AT=" + snapshot.CapturedAt.ToString("O", CultureInfo.InvariantCulture));
        }

        private static string FormatSnapshotValue(
            double? value,
            PositionDataStatus status,
            string format)
        {
            return status == PositionDataStatus.Valid && value.HasValue
                ? value.Value.ToString(format, CultureInfo.InvariantCulture)
                : "N/A";
        }

        private static string FormatSnapshotValue(
            int? value,
            PositionDataStatus status)
        {
            return status == PositionDataStatus.Valid && value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : "N/A";
        }
    }
}
