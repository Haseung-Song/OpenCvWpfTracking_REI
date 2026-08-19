using System;

namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>
    /// HOME POSITION 완료 판정에 필요한 상태값 비교만 담당한다.
    ///
    /// 장비 통신, Delay, UI 상태는 알지 않으며
    /// 전달받은 Pan/Tilt Sample이 목표 근처에서 연속 안정 상태인지 계산한다.
    ///
    /// MainViewModel에서 완료 판정 계산을 분리하여
    /// 허용 오차 또는 안정 Sample 수를 변경할 때 이 클래스만 확인하면 된다.
    /// </summary>
    internal sealed class HomePositionStabilityTracker
    {
        private readonly double _targetTolerance;
        private readonly double _stableTolerance;
        private readonly int _requiredStableSamples;

        private double _previousPan;
        private double _previousTilt;

        public int StableCount { get; private set; }

        public bool IsNearTarget { get; private set; }

        public bool IsStableSample { get; private set; }

        /// <summary>
        /// HomePositionStabilityTracker 동작 수행 함수.
        /// </summary>
        public HomePositionStabilityTracker(
            double initialPan,
            double initialTilt,
            double targetTolerance,
            double stableTolerance,
            int requiredStableSamples)
        {
            if (targetTolerance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetTolerance));
            }

            if (stableTolerance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stableTolerance));
            }

            if (requiredStableSamples <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredStableSamples));
            }

            _previousPan = initialPan;
            _previousTilt = initialTilt;
            _targetTolerance = targetTolerance;
            _stableTolerance = stableTolerance;
            _requiredStableSamples = requiredStableSamples;
        }

        /// <summary>
        /// 현재 Sample을 반영하고 HOME 완료 여부를 반환한다.
        /// </summary>
        public bool Update(
            double currentPan,
            double currentTilt)
        {
            IsNearTarget =
                Math.Abs(currentPan) <= _targetTolerance &&
                Math.Abs(currentTilt) <= _targetTolerance;

            IsStableSample =
                Math.Abs(currentPan - _previousPan) <= _stableTolerance &&
                Math.Abs(currentTilt - _previousTilt) <= _stableTolerance;

            if (IsNearTarget &&
                IsStableSample)
            {
                StableCount++;
            }
            else
            {
                StableCount = 0;
            }

            _previousPan = currentPan;
            _previousTilt = currentTilt;

            return StableCount >=
                _requiredStableSamples;
        }

    }

}
