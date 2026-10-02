using System;

namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>
    /// 2026-10-02: 동일 건물 기준으로 재측정한 Level 0~10 실측 Anchor를 사용한다.
    /// Anchor 사이는 IR Position을 기준으로 EO Position을 구간 선형 역산한다.
    /// </summary>
    public sealed class FieldOfViewSyncService
    {
        public const double AllowedErrorPercent = 10.0;

        private static readonly short[] CalibratedEoPositions =
        {
            0, 399, 443, 492, 536, 588, 636, 693, 765, 834, 940
        };

        private static readonly short[] CalibratedIrPositions =
        {
            0, 99, 200, 302, 397, 501, 599, 700, 801, 900, 1000
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
            short irPosition = CalibratedIrPositions[safeLevel];
            short eoPosition = GetEoPositionForIr(irPosition);
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

        /// <summary>
        /// IR Position이 속한 두 실측 Anchor 사이를 선형 보간하여 EO Position을 역산한다.
        /// EO = EO1 + (IR - IR1) * (EO2 - EO1) / (IR2 - IR1)
        /// </summary>
        public short GetEoPositionForIr(short irPosition)
        {
            // 2026-10-02: 비정상 입력은 장비 표준 범위로 제한한다.
            // Anchor는 11개뿐이므로 할당 없는 최대 10회 순회가 이진 탐색보다 단순하고 충분히 빠르다.
            int safeIr = Math.Max(0, Math.Min(1000, (int)irPosition));

            for (int index = 0; index < CalibratedIrPositions.Length - 1; index++)
            {
                int irStart = CalibratedIrPositions[index];
                int irEnd = CalibratedIrPositions[index + 1];

                if (safeIr > irEnd)
                {
                    continue;
                }

                int eoStart = CalibratedEoPositions[index];
                int eoEnd = CalibratedEoPositions[index + 1];
                int irSpan = irEnd - irStart;
                if (irSpan <= 0)
                {
                    // 보정표가 잘못된 경우 0으로 나누지 않고 다음 정상 구간을 찾는다.
                    continue;
                }

                double ratio = (safeIr - irStart) / (double)irSpan;
                return (short)Math.Round(eoStart + ((eoEnd - eoStart) * ratio));
            }

            // 정상 표에서는 IR 1000 Anchor로만 도달한다. 표 이상 시에도 안전한 끝값을 반환한다.
            return CalibratedEoPositions[CalibratedEoPositions.Length - 1];
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
