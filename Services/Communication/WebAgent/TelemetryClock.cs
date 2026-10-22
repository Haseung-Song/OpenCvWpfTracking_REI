using System;
using System.Diagnostics;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// Maps SBC sample times to local freshness without assuming synchronized clocks.
    /// Only successful, promptly received F1 queries may establish the remote clock.
    /// </summary>
    internal sealed class TelemetryClock
    {
        private readonly object _gate = new object();
        private bool _ready;
        private double _remoteAtAnchor;
        private double _monotonicAtAnchor;
        private int _generation;

        internal static double MonotonicMilliseconds =>
            Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency);

        internal void Reset()
        {
            lock (_gate) { _ready = false; _generation++; }
        }

        internal bool Observe(long remoteUnixMilliseconds, double sentAt, double receivedAt,
            out bool changed)
        {
            changed = false;
            double roundTrip = receivedAt - sentAt;
            if (remoteUnixMilliseconds <= 0 || remoteUnixMilliseconds > 253402300799999L ||
                sentAt <= 0 || roundTrip < 0 || roundTrip > 2000) return false;
            // F1 has whole-second precision. Use the middle of that second,
            // with half of the bounded RTT as the transport estimate.
            double remoteNow = remoteUnixMilliseconds + 500 + roundTrip / 2;
            lock (_gate)
            {
                changed = !_ready || Math.Abs(remoteNow -
                    (_remoteAtAnchor + receivedAt - _monotonicAtAnchor)) > 2000;
                if (changed) _generation++;
                _remoteAtAnchor = remoteNow;
                _monotonicAtAnchor = receivedAt;
                _ready = true;
            }
            return true;
        }

        internal bool TryGetLocalSampleTime(ulong sample, double monotonicNow,
            DateTimeOffset localNow, out DateTimeOffset localSample, out int generation,
            bool requireFresh = true)
        {
            localSample = default(DateTimeOffset);
            lock (_gate)
            {
                generation = _generation;
                if (!_ready || sample == 0 || sample > 253402300799999UL) return false;
                double age = _remoteAtAnchor + monotonicNow - _monotonicAtAnchor - sample;
                // Retain the 3-second stale-cache guard; do not treat receipt
                // of repeated cached packets as a new device measurement.
                if ((requireFresh && age > 3000) || age < -2000) return false;
                try { localSample = localNow.AddMilliseconds(-Math.Max(0, age)); }
                catch (ArgumentOutOfRangeException) { return false; }
                return true;
            }
        }
    }
}
