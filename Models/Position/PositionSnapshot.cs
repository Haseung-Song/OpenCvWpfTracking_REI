using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenCvWpfTracking.Models.Position
{
    /// <summary>
    /// 위치 데이터가 스냅샷 생성 시점에 사용할 수 있는 상태인지 나타낸다.
    /// 값 0과 미수신을 혼동하지 않도록 값과 상태를 별도로 보관한다.
    /// </summary>
    public enum PositionDataStatus
    {
        NotReceived,
        Valid,
        Stale,
        Disconnected,
        Invalid
    }

    /// <summary>
    /// PRESET 저장 또는 Event 최초 생성 순간의 장비 방향과 렌즈 상태를 고정한다.
    /// 생성 후 값이 바뀌지 않으며 RTSP 프레임과는 독립적으로 사용한다.
    /// </summary>
    public sealed class PositionSnapshot
    {
        public const int ExportFieldCount = 20;

        public DateTimeOffset CapturedAt { get; }

        public string Source { get; }

        public int? PresetId { get; }

        public double? Pan { get; }

        public double? Tilt { get; }

        public int? EoZoom { get; }

        public int? EoFocus { get; }

        public int? IrZoom { get; }

        public int? IrFocus { get; }

        public double? Latitude { get; }

        public double? Longitude { get; }

        public double? Altitude { get; }

        public double? Roll { get; }

        public double? Pitch { get; }

        public double? Yaw { get; }

        public PositionDataStatus PtzStatus { get; }

        public PositionDataStatus EoLensStatus { get; }

        public PositionDataStatus IrLensStatus { get; }

        public PositionDataStatus GpsStatus { get; }

        public PositionDataStatus ImuStatus { get; }

        public bool WebAgentConnected { get; }

        public PositionSnapshot(
            DateTimeOffset capturedAt,
            string source,
            int? presetId,
            double? pan,
            double? tilt,
            int? eoZoom,
            int? eoFocus,
            int? irZoom,
            int? irFocus,
            double? latitude,
            double? longitude,
            double? altitude,
            double? roll,
            double? pitch,
            double? yaw,
            PositionDataStatus ptzStatus,
            PositionDataStatus eoLensStatus,
            PositionDataStatus irLensStatus,
            PositionDataStatus gpsStatus,
            PositionDataStatus imuStatus,
            bool webAgentConnected)
        {
            CapturedAt = capturedAt;
            Source = string.IsNullOrWhiteSpace(source) ? "UNKNOWN" : source.Trim();
            PresetId = presetId;
            Pan = pan;
            Tilt = tilt;
            EoZoom = eoZoom;
            EoFocus = eoFocus;
            IrZoom = irZoom;
            IrFocus = irFocus;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
            PtzStatus = ptzStatus;
            EoLensStatus = eoLensStatus;
            IrLensStatus = irLensStatus;
            GpsStatus = gpsStatus;
            ImuStatus = imuStatus;
            WebAgentConnected = webAgentConnected;
        }

        public string PositionSummary =>
            "PAN " + FormatDouble(Pan, PtzStatus, "F2") +
            " / TILT " + FormatDouble(Tilt, PtzStatus, "F2");

        public string[] ToExportFields()
        {
            return new[]
            {
                CapturedAt.ToString("O", CultureInfo.InvariantCulture),
                Source,
                PresetId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                FormatExportDouble(Pan, PtzStatus),
                FormatExportDouble(Tilt, PtzStatus),
                FormatExportInt(EoZoom, EoLensStatus),
                FormatExportInt(EoFocus, EoLensStatus),
                FormatExportInt(IrZoom, IrLensStatus),
                FormatExportInt(IrFocus, IrLensStatus),
                FormatExportDouble(Latitude, GpsStatus),
                FormatExportDouble(Longitude, GpsStatus),
                FormatExportDouble(Altitude, GpsStatus),
                FormatExportDouble(Roll, ImuStatus),
                FormatExportDouble(Pitch, ImuStatus),
                FormatExportDouble(Yaw, ImuStatus),
                PtzStatus.ToString(),
                EoLensStatus.ToString(),
                IrLensStatus.ToString(),
                GpsStatus.ToString(),
                ImuStatus.ToString()
            };
        }

        public static bool TryParseExportFields(
            IList<string> fields,
            int startIndex,
            out PositionSnapshot snapshot)
        {
            snapshot = null;
            if (fields == null || startIndex < 0 || fields.Count < startIndex + ExportFieldCount)
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(
                    fields[startIndex],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset capturedAt))
            {
                return false;
            }

            string source = fields[startIndex + 1];
            int? presetId = ParseNullableInt(fields[startIndex + 2]);
            double? pan = ParseNullableDouble(fields[startIndex + 3]);
            double? tilt = ParseNullableDouble(fields[startIndex + 4]);
            int? eoZoom = ParseNullableInt(fields[startIndex + 5]);
            int? eoFocus = ParseNullableInt(fields[startIndex + 6]);
            int? irZoom = ParseNullableInt(fields[startIndex + 7]);
            int? irFocus = ParseNullableInt(fields[startIndex + 8]);
            double? latitude = ParseNullableDouble(fields[startIndex + 9]);
            double? longitude = ParseNullableDouble(fields[startIndex + 10]);
            double? altitude = ParseNullableDouble(fields[startIndex + 11]);
            double? roll = ParseNullableDouble(fields[startIndex + 12]);
            double? pitch = ParseNullableDouble(fields[startIndex + 13]);
            double? yaw = ParseNullableDouble(fields[startIndex + 14]);

            if (!TryParseStatus(fields[startIndex + 15], out PositionDataStatus ptzStatus) ||
                !TryParseStatus(fields[startIndex + 16], out PositionDataStatus eoLensStatus) ||
                !TryParseStatus(fields[startIndex + 17], out PositionDataStatus irLensStatus) ||
                !TryParseStatus(fields[startIndex + 18], out PositionDataStatus gpsStatus) ||
                !TryParseStatus(fields[startIndex + 19], out PositionDataStatus imuStatus))
            {
                return false;
            }

            snapshot = new PositionSnapshot(
                capturedAt,
                source,
                presetId,
                pan,
                tilt,
                eoZoom,
                eoFocus,
                irZoom,
                irFocus,
                latitude,
                longitude,
                altitude,
                roll,
                pitch,
                yaw,
                ptzStatus,
                eoLensStatus,
                irLensStatus,
                gpsStatus,
                imuStatus,
                ptzStatus != PositionDataStatus.Disconnected);

            return true;
        }

        private static bool TryParseStatus(string value, out PositionDataStatus status)
        {
            return Enum.TryParse(value, true, out status);
        }

        private static int? ParseNullableInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : (int?)null;
        }

        private static double? ParseNullableDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : (double?)null;
        }

        private static string FormatExportInt(int? value, PositionDataStatus status)
        {
            return status == PositionDataStatus.Valid && value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string FormatExportDouble(double? value, PositionDataStatus status)
        {
            return status == PositionDataStatus.Valid && value.HasValue
                ? value.Value.ToString("R", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string FormatDouble(
            double? value,
            PositionDataStatus status,
            string format)
        {
            return status == PositionDataStatus.Valid && value.HasValue
                ? value.Value.ToString(format, CultureInfo.InvariantCulture) + "°"
                : "N/A";
        }
    }
}
