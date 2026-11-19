using System;

namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>
    /// 2026-10-16: 실제 Focus 이동량과 정지 잔류량으로 선행 정지·미세 펄스를 계산한다.
    /// 작업마다 새 인스턴스를 사용하여 다른 장비의 학습값을 재사용하지 않는다.
    /// </summary>
    internal sealed class IrFocusMotionPlanner
    {
        private double _unitsPerSecond = 30.0;

        private double _pulseUnitsPerSecond = 30.0;

        private double _stopTravel;

        private double _feedbackMs = 500.0;

        private long _sampleMs;

        private int _sampleDelta;

        private int _minimumPulseMs = 30;

        public double UnitsPerSecond
        {
            get { return _unitsPerSecond; }
        }

        public int ReversalCount { get; private set; }

        /// <summary>
        /// 최근 응답과 정지 후 이동량을 이용해 목표 도달 전 선행 정지 보정값을 구한다.
        /// </summary>
        public int StopLead(int tolerance)
        {
            return (int)Math.Ceiling(Math.Max(tolerance, _stopTravel + _unitsPerSecond * (_feedbackMs + 120.0) / 1000.0));
        }

        /// <summary>
        /// 목표 오차와 허용 범위에 따라 짧은 펄스 제어를 사용할지 판단한다.
        /// </summary>
        public bool UsePulse(int error, int tolerance)
        {
            return error <= Math.Max(48, StopLead(tolerance) * 2);
        }

        /// <summary>
        /// 미세 초점 조정에 사용할 짧은 명령 유지 시간을 계산한다.
        /// </summary>
        public int PulseMilliseconds(int error)
        {
            double rate = Math.Max(_unitsPerSecond, _pulseUnitsPerSecond);

            double scale = ReversalCount > 0 ? 0.30 : 0.50;

            return Math.Max(_minimumPulseMs, Math.Min(180, (int)Math.Floor(Math.Max(1, error - _stopTravel) * 1000.0 / rate * scale)));
        }

        /// <summary>
        /// 목표와의 차이에 맞는 큰 단계 초점 이동 시간을 계산한다.
        /// </summary>
        public int CoarseMilliseconds(int error, int tolerance)
        {
            return Math.Max(60, Math.Min(12000, (int)Math.Floor(Math.Max(1, error - StopLead(tolerance)) * 1000.0 / _unitsPerSecond)));
        }

        /// <summary>
        /// 새 위치 응답의 이동량과 시간을 제어 계획의 관측값에 반영한다.
        /// </summary>
        public void ObserveFeedback(int delta, long elapsedMs)
        {
            if (elapsedMs < 20 || elapsedMs > 3000)
            {
                return;
            }

            _sampleMs += elapsedMs;

            _sampleDelta += delta;

            if (_sampleMs < 250)
            {
                return;
            }

            _feedbackMs = _feedbackMs * 0.5 + _sampleMs * 0.5;

            double measured = Math.Abs(_sampleDelta) * 1000.0 / _sampleMs;

            _sampleMs = 0;
            _sampleDelta = 0;

            if (measured >= 1 && measured <= 2000)
            {
                _unitsPerSecond = _unitsPerSecond * 0.6 + measured * 0.4;
            }
        }

        /// <summary>
        /// 명령 종료 후 실제 정착 위치를 기록해 다음 정지 보정을 조정한다.
        /// </summary>
        public void ObserveSettled(int start, int stopped, int settled, long onMs, bool pulse)
        {
            _stopTravel = _stopTravel * 0.5 + Math.Abs(settled - stopped) * 0.5;

            if (pulse && onMs >= 20 && settled != start)
            {
                double measured = Math.Abs(settled - start) * 1000.0 / onMs;

                _pulseUnitsPerSecond = Math.Max(1, Math.Min(4000, Math.Max(measured, _pulseUnitsPerSecond * 0.5 + measured * 0.5)));
            }
        }

        /// <summary>
        /// 허용된 횟수 안에서만 초점 이동 방향 반전을 승인한다.
        /// </summary>
        public bool TryReverse()
        {
            ReversalCount++;

            return ReversalCount <= 2;
        }

        /// <summary>
        /// 2026-10-16: 짧은 펄스가 장비 기동 지연에 묻히면 제한적으로 구동 시간을 늘린다.
        /// 목표 오프셋을 더하거나 수신값을 바꾸지 않는다.
        /// </summary>
        public void ObserveNoMovement()
        {
            _minimumPulseMs = Math.Min(300, Math.Max(150, _minimumPulseMs + 75));
        }
    }
}
