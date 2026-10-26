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

        // EO 모터 위치는 광학 배율에 선형 비례하지 않으므로 실측 대응점을 사용한다.
        // EO/IR 실측 대응점과 IR 1~9배 광학 모델을 적용한다.
        // 대응하지 않는 광각 끝점과 EO 전용 90배 끝점은 별도로 유지한다.
        /// <summary>EO/IR 실측 매칭표와 IR 선형 모델을 사용해 EO 광학 배율을 구간 보간한다.</summary>
        public static double EstimateEoOpticalRatio(int position, double eoWide, double irWide, double maximum)
        {
            int p = Math.Max(0, Math.Min(1000, position));
            double scale = Math.Tan(eoWide * Math.PI / 360) / Math.Tan(irWide * Math.PI / 360);
            int previousPosition = 0;
            double previousRatio = 1;

            for (int i = 1; i < CalibratedEoPositions.Length; i++)
            {
                double ratio = Math.Max(previousRatio, Math.Min(maximum, scale * (1 + 8 * CalibratedIrPositions[i] / 1000.0)));
                int nextPosition = CalibratedEoPositions[i];

                if (p <= nextPosition)
                    return previousRatio + (ratio - previousRatio) * (p - previousPosition) / (nextPosition - previousPosition);

                previousPosition = nextPosition;
                previousRatio = ratio;
            }

            return previousRatio + (maximum - previousRatio) * (p - previousPosition) / (1000 - previousPosition);
        }

        private static readonly short[] CalibratedEoPositions =
        {
            0,
            399,
            443,
            492,
            536,
            588,
            636,
            693,
            765,
            834,
            940
        };

        private static readonly short[] CalibratedIrPositions =
        {
            0,
            99,
            200,
            302,
            397,
            501,
            599,
            700,
            801,
            900,
            1000
        };

        // XV-Z2090HC 렌즈: 초점거리 6~540mm, 수평 화각 65.24~0.82도.
        private const double EoMinFocalMm = 6.0;

        private const double EoMaxFocalMm = 540.0;

        private static readonly double EoSensorWidthMm = 2.0 * EoMinFocalMm * Math.Tan(65.24 * Math.PI / 360.0);

        // IR 렌즈 25~225mm, 검출기 640×512 및 픽셀 간격 17마이크로미터 기준.
        // 규격상 수평 화각 24.5~2.7도, 센서 폭은 640×0.017=10.88mm이다.
        private const double IrMinFocalMm = 25.0;

        private const double IrMaxFocalMm = 225.0;

        private const double IrSensorWidthMm = 10.88;

        /// <summary>0~10 레벨의 실측 위치와 양 채널 예상 화각을 묶어 반환한다.</summary>
        public ZoomFovTarget CreateTarget(int level)
        {
            int safeLevel = Math.Max(0, Math.Min(10, level));
            short irPosition = CalibratedIrPositions[safeLevel];
            short eoPosition = GetEoPositionForIr(irPosition);
            double irHfov = PositionToHfov(irPosition, IrMinFocalMm, IrMaxFocalMm, IrSensorWidthMm);
            double eoHfov = PositionToHfov(eoPosition, EoMinFocalMm, EoMaxFocalMm, EoSensorWidthMm);
            return new ZoomFovTarget(safeLevel, eoPosition, irPosition, eoHfov, irHfov);
        }

        /// <summary>EO 표준 위치를 기존 초점거리 모델의 수평 화각으로 환산한다.</summary>
        public double GetEoHfov(short normalizedPosition)
        {
            return PositionToHfov(normalizedPosition, EoMinFocalMm, EoMaxFocalMm, EoSensorWidthMm);
        }

        /// <summary>IR 표준 위치를 기존 초점거리 모델의 수평 화각으로 환산한다.</summary>
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

        /// <summary>두 번째 화각을 기준으로 상대 오차율을 계산하고 분모의 0을 방지한다.</summary>
        public static double GetErrorPercent(double firstHfov, double secondHfov)
        {
            double reference = Math.Max(0.0001, secondHfov);
            return Math.Abs(firstHfov - secondHfov) / reference * 100.0;
        }

        /// <summary>위치를 0~1000으로 제한해 초점거리와 센서 폭으로 수평 화각을 계산한다.</summary>
        private static double PositionToHfov(short position, double minFocal, double maxFocal, double sensorWidth)
        {
            double ratio = Math.Max(0.0, Math.Min(1.0, position / 1000.0));
            double focal = minFocal + ((maxFocal - minFocal) * ratio);
            return 2.0 * Math.Atan(sensorWidth / (2.0 * focal)) * 180.0 / Math.PI;
        }

        /// <summary>초점거리 모델의 화각을 표준 위치로 역산한다. 공개 API와 별개인 기존 보조 함수이다.</summary>
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
