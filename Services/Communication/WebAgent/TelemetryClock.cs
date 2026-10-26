using System;
using System.Diagnostics;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// PC와 장비 시각이 같다고 가정하지 않고 측정 시각을 로컬 유효 시각에 대응시킨다.
    /// 정상적으로 빠르게 수신된 F1 조회 응답만 장비 시각 기준으로 사용한다.
    /// </summary>
    internal sealed class TelemetryClock
    {
        private readonly object _gate = new object();

        private bool _ready;

        private double _remoteAtAnchor;

        private double _monotonicAtAnchor;

        private int _generation;

        internal static double MonotonicMilliseconds => Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency);

        /// <summary>
        /// 연결 세션의 장비 시각 기준을 무효화하고 기준 세대를 변경한다.
        /// </summary>
        internal void Reset()
        {
            lock (_gate)
            {
                _ready = false;
                _generation++;
            }
        }

        /// <summary>
        /// 왕복 지연이 허용 범위인 장비 시각 응답으로 로컬 단조 시계와의 대응 기준을 갱신한다.
        /// </summary>
        internal bool Observe(long remoteUnixMilliseconds, double sentAt, double receivedAt, out bool changed)
        {
            changed = false;
            double roundTrip = receivedAt - sentAt;

            if (remoteUnixMilliseconds <= 0 ||
                remoteUnixMilliseconds > 253402300799999L ||
                sentAt <= 0 ||
                roundTrip < 0 ||
                roundTrip > 2000)
                return false;

            // F1 응답은 초 단위이므로 해당 초의 중간 시점을 기준으로 삼는다.
            // 허용 범위 내 왕복 지연의 절반을 전송 시간 추정에 반영한다.
            double remoteNow = remoteUnixMilliseconds + 500 + roundTrip / 2;
            lock (_gate)
            {
                changed = !_ready || Math.Abs(remoteNow - (_remoteAtAnchor + receivedAt - _monotonicAtAnchor)) > 2000;

                if (changed)
                    _generation++;

                _remoteAtAnchor = remoteNow;
                _monotonicAtAnchor = receivedAt;
                _ready = true;
            }

            return true;
        }

        /// <summary>
        /// 장비 측정 시각을 로컬 취득 시각으로 환산하고 미래값·오래된 상태를 거부한다.
        /// </summary>
        internal bool TryGetLocalSampleTime(
            ulong sample,
            double monotonicNow,
            DateTimeOffset localNow,
            out DateTimeOffset localSample,
            out int generation,
            bool requireFresh = true
        )
        {
            localSample = default(DateTimeOffset);
            lock (_gate)
            {
                generation = _generation;

                if (!_ready || sample == 0 || sample > 253402300799999UL)
                    return false;

                double age = _remoteAtAnchor + monotonicNow - _monotonicAtAnchor - sample;

                // 3초 유효성 제한을 유지하며 반복 수신된 캐시를 새 측정으로 취급하지 않는다.
                // 동일한 캐시 응답의 재수신만으로 장비 측정 시각을 갱신하지 않는다.
                if ((requireFresh && age > 3000) || age < -2000)
                    return false;

                try
                {
                    localSample = localNow.AddMilliseconds(-Math.Max(0, age));
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
