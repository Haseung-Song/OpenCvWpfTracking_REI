using System;

namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>
    /// 2026-09-21: 실장비 화면을 기준으로 확인한 EO/IR 10단계 화각 보정표를 사용한다.
    /// UI의 단계 표시는 100 단위를 유지하지만 실제 명령은 장비가 정착한 실측값을 쓴다.
    /// </summary>
    public sealed class FieldOfViewSyncService
    {
        public const double AllowedErrorPercent = 10.0;

        private static readonly short[] CalibratedEoPositions =
        {
            0, 410, 449, 492, 531, 596, 646, 705, 764, 809, 929
        };

        private static readonly short[] CalibratedIrPositions =
        {
            0, 102, 205, 299, 400, 502, 597, 699, 804, 905, 1000
        };

        // XV-Z2090HC: 6~540 mm, HFOV 65.24~0.82 deg.
        private const double EoMinFocalMm = 6.0;
        private const double EoMaxFocalMm = 540.0;
        private static readonly double EoSensorWidthMm = 2.0 * EoMinFocalMm * Math.Tan(65.24 * Math.PI / 360.0);

        // Infra-LWZ-25-225-AF1 + 640x512/17um detector: 25~225 mm,
        // catalog HFOV 24.5~2.7 deg. Sensor width = 640 * 0.017 = 10.88 mm.
        private const double IrMinFocalMm = 25.0;
        private const double IrMaxFocalMm = 225.0;
        private const double IrSensorWidthMm = 10.88;

        public ZoomFovTarget CreateTarget(int level)
        {
            int safeLevel = Math.Max(0, Math.Min(10, level));
            short eoPosition = CalibratedEoPositions[safeLevel];
            short irPosition = CalibratedIrPositions[safeLevel];
            double irHfov = PositionToHfov(irPosition, IrMinFocalMm, IrMaxFocalMm, IrSensorWidthMm);
            double eoHfov = PositionToHfov(eoPosition, EoMinFocalMm, EoMaxFocalMm, EoSensorWidthMm);

            return new ZoomFovTarget(safeLevel, eoPosition, irPosition, eoHfov, irHfov);
        }

        public double GetEoHfov(short normalizedPosition)
        {
            return PositionToHfov(normalizedPosition, EoMinFocalMm, EoMaxFocalMm, EoSensorWidthMm);
        }

        public double GetIrHfov(short normalizedPosition)
        {
            return PositionToHfov(normalizedPosition, IrMinFocalMm, IrMaxFocalMm, IrSensorWidthMm);
        }

        public static double GetErrorPercent(double firstHfov, double secondHfov)
        {
            double reference = Math.Max(0.0001, secondHfov);
            return Math.Abs(firstHfov - secondHfov) / reference * 100.0;
        }

        private static double PositionToHfov(short position, double minFocal, double maxFocal, double sensorWidth)
        {
            double ratio = Math.Max(0.0, Math.Min(1.0, position / 1000.0));
            double focal = minFocal + ((maxFocal - minFocal) * ratio);
            return 2.0 * Math.Atan(sensorWidth / (2.0 * focal)) * 180.0 / Math.PI;
        }

        private static short HfovToPosition(double hfov, double minFocal, double maxFocal, double sensorWidth)
        {
            double radians = Math.Max(0.0001, hfov) * Math.PI / 180.0;
            double focal = sensorWidth / (2.0 * Math.Tan(radians / 2.0));
            double ratio = (focal - minFocal) / (maxFocal - minFocal);
            return (short)Math.Round(Math.Max(0.0, Math.Min(1.0, ratio)) * 1000.0);
        }
    }

    public sealed class ZoomFovTarget
    {
        public int Level { get; }
        public short EoPosition { get; }
        public short IrPosition { get; }
        public double EoHfov { get; }
        public double IrHfov { get; }

        public ZoomFovTarget(int level, short eoPosition, short irPosition, double eoHfov, double irHfov)
        {
            Level = level;
            EoPosition = eoPosition;
            IrPosition = irPosition;
            EoHfov = eoHfov;
            IrHfov = irHfov;
        }
    }
}
