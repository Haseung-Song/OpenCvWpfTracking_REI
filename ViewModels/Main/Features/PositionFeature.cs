using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Position;
using OpenCvWpfTracking.Services.Position;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>수신 위치 상태에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IPositionFeatureContext
    {
        ref int _zoneEpoch { get; }

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);
    }

    /// <summary>수신 위치 상태 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PositionFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPositionFeatureContext _context;

        /// <summary>
        /// 장비 위치 상태 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal PositionFeature(IPositionFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal readonly PositionSnapshotService _positionSnapshotService = new PositionSnapshotService(PositionStateMaximumAge, PositionTelemetryMaximumAge);

        internal PositionSnapshot _lastPositionSnapshot;

        internal int _positionStatusNotificationPending;

        internal int _positionLastSnapshotNotificationDirty;

        #endregion

        #region [화면 속성과 이벤트]

        internal string PositionStatusText
        {
            get
            {
                PositionSnapshot snapshot = _positionSnapshotService.Capture("UI_STATUS");

                return "PTZ " + ToUiStatus(snapshot.PtzStatus) + "  /  EO " + ToUiStatus(snapshot.EoLensStatus) + "  /  IR " + ToUiStatus(snapshot.IrLensStatus) + "  /  GPS " + ToUiStatus(snapshot.GpsStatus) + "  /  IMU " + ToUiStatus(snapshot.ImuStatus);
            }
        }

        internal string LastPositionSnapshotText
        {
            get
            {
                if (_lastPositionSnapshot == null)
                {
                    return "LAST SNAPSHOT : --";
                }
                else
                {
                    return "LAST SNAPSHOT : " + _lastPositionSnapshot.Source + "  /  " + _lastPositionSnapshot.CapturedAt.ToString("HH:mm:ss") + "  /  " + _lastPositionSnapshot.PositionSummary;
                }
            }
        }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 위치 상태 저장소에 현재 장비의 연결 여부를 반영한다.
        /// </summary>
        internal void SetPositionConnectionState(bool connected)
        {
            System.Threading.Interlocked.Increment(ref _context._zoneEpoch);

            _positionSnapshotService.SetConnected(connected);

            QueuePositionStatusNotification();
        }

        /// <summary>
        /// 수신한 PTZ와 EO 렌즈 값을 위치 상태에 반영한다.
        /// </summary>
        internal void UpdatePositionPtzAndEoLens(double pan, double tilt, int eoZoom, int eoFocus)
        {
            _positionSnapshotService.UpdatePtzAndEoLens(pan, tilt, eoZoom, eoFocus);

            QueuePositionStatusNotification();
        }

        /// <summary>
        /// EO 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        internal void UpdatePositionEoLens(int eoZoom, int eoFocus)
        {
            _positionSnapshotService.UpdateEoLens(eoZoom, eoFocus);

            QueuePositionStatusNotification();
        }

        /// <summary>
        /// IR 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        internal void UpdatePositionIrLens(int irZoom, int irFocus)
        {
            _positionSnapshotService.UpdateIrLens(irZoom, irFocus);

            QueuePositionStatusNotification();
        }

        /// <summary>
        /// GPS 위치·이동·수신 품질을 위치 상태에 반영한다.
        /// </summary>
        internal void UpdatePositionGps(double latitude, double longitude, double altitude, double speed, double course, double hdop, int satelliteCount, int fixStatus, bool valid)
        {
            _positionSnapshotService.UpdateGps(latitude, longitude, altitude, speed, course, hdop, satelliteCount, fixStatus, valid);

            QueuePositionStatusNotification();
        }

        /// <summary>
        /// IMU Roll·Pitch·Yaw 응답을 위치 상태에 반영한다.
        /// </summary>
        internal void UpdatePositionImu(double roll, double pitch, double yaw, bool valid)
        {
            _positionSnapshotService.UpdateImu(roll, pitch, yaw, valid);

            QueuePositionStatusNotification();
        }

        /// <summary>
        /// 현재 위치·센서·렌즈 상태를 일관된 스냅샷으로 취득한다.
        /// </summary>
        internal PositionSnapshot CapturePositionSnapshot(string source, int? presetId = null)
        {
            try
            {
                PositionSnapshot snapshot = _positionSnapshotService.Capture(source, presetId);

                _lastPositionSnapshot = snapshot;

                QueuePositionStatusNotification(true);

                LogPositionSnapshot(snapshot);

                return snapshot;
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error("POSITION SNAPSHOT", "Capture failed / SOURCE=" + source, exception);

                return null;
            }
        }

        /// <summary>
        /// 위치 상태 알림을 화면 스레드에 예약해 수신 스레드의 직접 화면 접근을 피한다.
        /// </summary>
        internal void QueuePositionStatusNotification(bool includeLastSnapshot = false)
        {
            Dispatcher dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null)
            {
                return;
            }

            if (includeLastSnapshot)
            {
                Interlocked.Exchange(ref _positionLastSnapshotNotificationDirty, 1);
            }

            if (Interlocked.CompareExchange(ref _positionStatusNotificationPending, 1, 0) != 0)
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

                    _ = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        Interlocked.Exchange(ref _positionStatusNotificationPending, 0);

                        _context.OnPropertyChanged(nameof(PositionStatusText));

                        if (Interlocked.Exchange(ref _positionLastSnapshotNotificationDirty, 0) != 0)
                        {
                            _context.OnPropertyChanged(nameof(LastPositionSnapshotText));
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

        /// <summary>
        /// 위치 데이터의 유효 상태를 화면 표시 문구로 변환한다.
        /// </summary>
        internal static string ToUiStatus(PositionDataStatus status)
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

        /// <summary>
        /// 현재 위치 스냅샷의 자세·렌즈·GPS·IMU 값을 진단 로그로 남긴다.
        /// </summary>
        internal static void LogPositionSnapshot(PositionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            ConsoleLogHelper.State("POSITION SNAPSHOT", "SOURCE=" + snapshot.Source + " / PRESET=" + (snapshot.PresetId?.ToString(CultureInfo.InvariantCulture) ?? "-") + " / PAN=" + FormatSnapshotValue(snapshot.Pan, snapshot.PtzStatus, "F2") + " / TILT=" + FormatSnapshotValue(snapshot.Tilt, snapshot.PtzStatus, "F2") + " / EO_ZOOM=" + FormatSnapshotValue(snapshot.EoZoom, snapshot.EoLensStatus) + " / IR_ZOOM=" + FormatSnapshotValue(snapshot.IrZoom, snapshot.IrLensStatus) + " / PTZ=" + snapshot.PtzStatus + " / EO_LENS=" + snapshot.EoLensStatus + " / IR_LENS=" + snapshot.IrLensStatus + " / GPS=" + snapshot.GpsStatus + " / GPS_COURSE=" + FormatSnapshotValue(snapshot.GpsCourse, snapshot.GpsStatus, "F2") + " / GPS_HDOP=" + FormatSnapshotValue(snapshot.GpsHdop, snapshot.GpsStatus, "F2") + " / GPS_SAT=" + FormatSnapshotValue(snapshot.GpsSatelliteCount, snapshot.GpsStatus) + " / GPS_FIX=" + FormatSnapshotValue(snapshot.GpsFixStatus, snapshot.GpsStatus) + " / IMU=" + snapshot.ImuStatus + " / CONNECTED=" + snapshot.WebAgentConnected + " / CAPTURED_AT=" + snapshot.CapturedAt.ToString("O", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 스냅샷 수치와 유효 상태를 사람이 읽을 표시 문자열로 변환한다.
        /// </summary>
        internal static string FormatSnapshotValue(double? value, PositionDataStatus status, string format)
        {
            if (status == PositionDataStatus.Valid && value.HasValue)
            {
                return value.Value.ToString(format, CultureInfo.InvariantCulture);
            }
            else
            {
                return "N/A";
            }
        }

        /// <summary>
        /// 스냅샷 수치와 유효 상태를 사람이 읽을 표시 문자열로 변환한다.
        /// </summary>
        internal static string FormatSnapshotValue(int? value, PositionDataStatus status)
        {
            if (status == PositionDataStatus.Valid && value.HasValue)
            {
                return value.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                return "N/A";
            }
        }

        #endregion

    }
}
