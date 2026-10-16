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

        public double UnitsPerSecond => _unitsPerSecond;
        public int ReversalCount { get; private set; }

        public int StopLead(int tolerance)
        {
            return (int)Math.Ceiling(Math.Max(tolerance,
                _stopTravel + _unitsPerSecond * (_feedbackMs + 120.0) / 1000.0));
        }

        public bool UsePulse(int error, int tolerance)
        {
            return error <= Math.Max(48, StopLead(tolerance) * 2);
        }

        public int PulseMilliseconds(int error)
        {
            double rate = Math.Max(_unitsPerSecond, _pulseUnitsPerSecond);
            double scale = ReversalCount > 0 ? 0.30 : 0.50;
            return Math.Max(_minimumPulseMs, Math.Min(180,
                (int)Math.Floor(Math.Max(1, error - _stopTravel) * 1000.0 / rate * scale)));
        }

        public int CoarseMilliseconds(int error, int tolerance)
        {
            return Math.Max(60, Math.Min(12000,
                (int)Math.Floor(Math.Max(1, error - StopLead(tolerance)) * 1000.0 / _unitsPerSecond)));
        }

        public void ObserveFeedback(int delta, long elapsedMs)
        {
            if (elapsedMs < 20 || elapsedMs > 3000) return;
            _sampleMs += elapsedMs;
            _sampleDelta += delta;
            if (_sampleMs < 250) return;
            _feedbackMs = _feedbackMs * 0.5 + _sampleMs * 0.5;
            double measured = Math.Abs(_sampleDelta) * 1000.0 / _sampleMs;
            _sampleMs = 0;
            _sampleDelta = 0;
            if (measured >= 1 && measured <= 2000)
                _unitsPerSecond = _unitsPerSecond * 0.6 + measured * 0.4;
        }

        public void ObserveSettled(int start, int stopped, int settled, long onMs, bool pulse)
        {
            _stopTravel = _stopTravel * 0.5 + Math.Abs(settled - stopped) * 0.5;
            if (pulse && onMs >= 20 && settled != start)
            {
                double measured = Math.Abs(settled - start) * 1000.0 / onMs;
                _pulseUnitsPerSecond = Math.Max(1, Math.Min(4000,
                    Math.Max(measured, _pulseUnitsPerSecond * 0.5 + measured * 0.5)));
            }
        }

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
