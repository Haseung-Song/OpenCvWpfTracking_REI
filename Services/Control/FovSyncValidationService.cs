using System;

namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>
    /// 2026-09-21: EO/IR 원본 프레임에서 같은 기준물의 가로 폭을 비교한다.
    /// 카메라 설치 위치에 따른 중심/TILT 차이는 제외하고 화각 배율만 검증한다.
    /// </summary>
    public sealed class FovSyncValidationService
    {
        public const double PassThresholdPercent = 10.0;

        public FovSyncValidationResult Calculate(
            int level,
            double eoLeftX,
            double eoRightX,
            int eoFrameWidth,
            double irLeftX,
            double irRightX,
            int irFrameWidth)
        {
            if (level < 1 || level > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "LEVEL must be between 1 and 10.");
            }

            if (eoFrameWidth <= 0 || irFrameWidth <= 0)
            {
                throw new ArgumentException("EO/IR frame width must be greater than zero.");
            }

            double eoPixelWidth = Math.Abs(eoRightX - eoLeftX);
            double irPixelWidth = Math.Abs(irRightX - irLeftX);

            if (eoPixelWidth < 1.0 || irPixelWidth < 1.0)
            {
                throw new ArgumentException("Select two different horizontal boundaries for each camera.");
            }

            double eoNormalizedWidth = eoPixelWidth / eoFrameWidth;
            double irNormalizedWidth = irPixelWidth / irFrameWidth;
            double errorPercent = Math.Abs(eoNormalizedWidth - irNormalizedWidth) /
                                  eoNormalizedWidth * 100.0;

            return new FovSyncValidationResult(
                level,
                eoPixelWidth,
                irPixelWidth,
                eoNormalizedWidth,
                irNormalizedWidth,
                errorPercent,
                errorPercent <= PassThresholdPercent);
        }
    }

    public sealed class FovSyncValidationResult
    {
        public int Level { get; }
        public double EoPixelWidth { get; }
        public double IrPixelWidth { get; }
        public double EoNormalizedWidth { get; }
        public double IrNormalizedWidth { get; }
        public double ErrorPercent { get; }
        public bool Passed { get; }

        public FovSyncValidationResult(
            int level,
            double eoPixelWidth,
            double irPixelWidth,
            double eoNormalizedWidth,
            double irNormalizedWidth,
            double errorPercent,
            bool passed)
        {
            Level = level;
            EoPixelWidth = eoPixelWidth;
            IrPixelWidth = irPixelWidth;
            EoNormalizedWidth = eoNormalizedWidth;
            IrNormalizedWidth = irNormalizedWidth;
            ErrorPercent = errorPercent;
            Passed = passed;
        }
    }
}
