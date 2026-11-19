using System;
using System.Linq;
using OpenCvWpfTracking.Models.AI;

namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>
    /// 한 프레임에서 선택한 EO 표적의 원본 좌표와 수신 시각을 보관한다.
    /// 화면에 남겨 둔 표시 박스나 프레임별 DisplayOrder는 제어에 사용하지 않는다.
    /// </summary>
    internal sealed class TrackingTarget
    {
        internal AiDetectionBox Box { get; set; }

        internal int Width { get; set; }

        internal int Height { get; set; }

        internal DateTime ReceivedUtc { get; set; }

        internal double NormalizedX => (Box.Left + (double)Box.Right) / Width - 1;

        internal double NormalizedY => (Box.Top + (double)Box.Bottom) / Height - 1;
    }

    /// <summary>
    /// MJ의 최신 표적·중복 프레임 관리와 기존 Vertiport의 필터·이동 제한을 결합한다.
    /// UI 스레드 한 곳에서 호출하며, 통신과 화면 객체를 소유하지 않는다.
    /// </summary>
    internal sealed class AutoTrackingController
    {
        internal const int FreshnessMilliseconds = 700;

        private TrackingTarget _current;
        private TrackingTarget _locked;
        private long _lastFrameTime;
        private int _confirmations;
        private DateTime _lastSeenUtc;
        private double _panFiltered;
        private double _tiltFiltered;
        private double _panPending;
        private double _tiltPending;

        internal bool HasLock => _locked != null;

        internal DateTime LastSeenUtc => _lastSeenUtc;

        /// <summary>
        /// 최근 EO 탐지 한 프레임으로 표적을 갱신한다. 기존 ID가 빠지면 다른 ID로 갈아타지 않는다.
        /// ID가 없을 때만 같은 클래스의 겹치는 박스로 연속성을 보완한다.
        /// </summary>
        internal void Update(AiDetectionResult result, int width, int height, DateTime receivedUtc, DateTime nowUtc, double confidence)
        {
            if (result == null || result.RtspIndex != 0 || result.Boxes == null || width <= 0 || height <= 0 ||
                !IsFresh(receivedUtc, nowUtc) || (result.FrameTime > 0 && _lastFrameTime > 0 && result.FrameTime <= _lastFrameTime))
            {
                return;
            }

            if (result.FrameTime > 0)
            {
                _lastFrameTime = result.FrameTime;
            }

            var candidates = result.Boxes.Where(box => IsCandidate(box, width, height, confidence)).ToList();

            AiDetectionBox selected = null;

            if (_locked == null)
            {
                selected = candidates.OrderByDescending(box => box.NormalizedConfidence)
                    .ThenByDescending(box => (double)(box.Right - box.Left) * (box.Bottom - box.Top)).FirstOrDefault();
            }
            else if (_locked.Box.ObjectId > 0)
            {
                selected = candidates.FirstOrDefault(box => box.ObjectId == _locked.Box.ObjectId && box.ClassIndex == _locked.Box.ClassIndex);
            }
            else
            {
                selected = candidates.Where(box => box.ClassIndex == _locked.Box.ClassIndex && box.ObjectId <= 0)
                    .OrderByDescending(box => IntersectionOverUnion(box, _locked.Box))
                    .FirstOrDefault(box => IntersectionOverUnion(box, _locked.Box) >= .3);
            }

            if (selected == null)
            {
                _current = null;
                _confirmations = 0;
                ResetFilter();
                return;
            }

            // 표시 코드가 같은 AiDetectionBox를 변경해도 추적 좌표는 바뀌지 않게 복사한다.
            _current = new TrackingTarget
            {
                Box = new AiDetectionBox
                {
                    ObjectId = selected.ObjectId,
                    ClassIndex = selected.ClassIndex,
                    Left = selected.Left,
                    Top = selected.Top,
                    Right = selected.Right,
                    Bottom = selected.Bottom
                },
                Width = width,
                Height = height,
                ReceivedUtc = receivedUtc
            };

            _locked = _current;
            _lastSeenUtc = receivedUtc;
            _confirmations++;
        }

        /// <summary>
        /// 최신 표적이 연속 세 프레임 확인되고 수신 유효 시간 안에 있을 때만 제어 입력을 반환한다.
        /// </summary>
        internal TrackingTarget GetTarget(DateTime nowUtc)
        {
            return _confirmations >= 3 && _current != null && IsFresh(_current.ReceivedUtc, nowUtc) ? _current : null;
        }

        /// <summary>
        /// 줌에 따라 명령 간격을 늘려 고배율에서 과도한 보정 명령이 누적되는 것을 막는다.
        /// </summary>
        internal static int CommandInterval(double zoomRatio)
        {
            return zoomRatio >= 20 ? 900 : zoomRatio >= 10 ? 700 : 500;
        }

        /// <summary>
        /// REI에서 계산한 각도 오차에 데드존·저역 필터·작은 각도 누적·최대 스텝을 적용한다.
        /// 장비 전용 ADS1000 보정 계수는 사용하지 않는다.
        /// </summary>
        internal Tuple<double, double> FilterCorrection(double pan, double tilt, double zoomRatio)
        {
            if (!Finite(pan) || !Finite(tilt) || !Finite(zoomRatio) || zoomRatio < 1)
            {
                ResetFilter();
                return null;
            }

            double deadZone = Math.Max(.003, .03 / zoomRatio);
            double maximum = Math.Max(.025, .2 / Math.Sqrt(zoomRatio));
            double alpha = Math.Max(.08, .25 / Math.Sqrt(zoomRatio));

            return Tuple.Create(FilterAxis(pan, deadZone, maximum, alpha, ref _panFiltered, ref _panPending),
                FilterAxis(tilt, deadZone, maximum, alpha, ref _tiltFiltered, ref _tiltPending));
        }

        /// <summary>
        /// 중앙 도달·표적 변경 시 이전 방향의 잔류 보정값을 버린다.
        /// </summary>
        internal void ResetFilter()
        {
            _panFiltered = _tiltFiltered = _panPending = _tiltPending = 0;
        }

        /// <summary>
        /// 정지·연결 세대 변경 시 ID·프레임 시각·보류값을 모두 초기화한다.
        /// </summary>
        internal void Reset()
        {
            _current = _locked = null;
            _lastFrameTime = 0;
            _lastSeenUtc = DateTime.MinValue;
            _confirmations = 0;
            ResetFilter();
        }

        /// <summary>
        /// 해석된 Drone/UAV 클래스와 유효한 원본 영상 내부 박스만 추적 후보로 허용한다.
        /// </summary>
        private static bool IsCandidate(AiDetectionBox box, int width, int height, double confidence)
        {
            if (box == null || !Finite(box.NormalizedConfidence) || box.NormalizedConfidence < Math.Max(.5, confidence) ||
                box.NormalizedConfidence > 1 || box.Left < 0 || box.Top < 0 || box.Right > width || box.Bottom > height ||
                box.Right <= box.Left || box.Bottom <= box.Top)
            {
                return false;
            }

            string name = box.ResolvedClassName?.Trim();

            return string.Equals(name, "Drone", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "UAV", StringComparison.OrdinalIgnoreCase) || name == "드론";
        }

        /// <summary>
        /// 수신 시각의 미래값과 유효 시간이 지난 표적을 차단한다.
        /// </summary>
        private static bool IsFresh(DateTime receivedUtc, DateTime nowUtc)
        {
            double elapsed = (nowUtc - receivedUtc).TotalMilliseconds;

            return elapsed >= 0 && elapsed <= FreshnessMilliseconds;
        }

        /// <summary>
        /// 한 축의 부호 반전과 중앙 도달 시 잔류값을 없애고 작은 보정값을 제한하여 반환한다.
        /// </summary>
        private static double FilterAxis(double error, double deadZone, double maximum, double alpha, ref double filtered, ref double pending)
        {
            if (Math.Abs(error) <= deadZone)
            {
                filtered = pending = 0;
                return 0;
            }

            if (Math.Sign(error) != Math.Sign(filtered))
            {
                filtered = pending = 0;
            }

            filtered += alpha * (error - filtered);
            pending += filtered;

            if (Math.Abs(pending) < deadZone)
            {
                return 0;
            }

            double step = Math.Sign(pending) * Math.Min(Math.Abs(pending), Math.Min(maximum, Math.Abs(error)));
            pending = 0;

            return step;
        }

        /// <summary>
        /// 객체 ID가 없는 출력에서 이전 박스와 후보 박스의 겹침 비율을 계산한다.
        /// </summary>
        private static double IntersectionOverUnion(AiDetectionBox a, AiDetectionBox b)
        {
            double area = Math.Max(0, Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left)) *
                (double)Math.Max(0, Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top));
            double union = (double)(a.Right - a.Left) * (a.Bottom - a.Top) + (double)(b.Right - b.Left) * (b.Bottom - b.Top) - area;

            return union > 0 ? area / union : 0;
        }

        /// <summary>
        /// 계산 결과가 유한한 수인지 확인한다.
        /// </summary>
        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    /// <summary>
    /// 비동기 PT 송신이 끝날 때까지 추적 세대·표적·제어 소유권을 재검증하는 이동 권한이다.
    /// </summary>
    internal sealed class TrackingMotionLease
    {
        internal Func<bool> CanContinue { get; set; }

        internal Func<double, double, double, Tuple<double, double>> Filter { get; set; }

        internal bool MotionSent { get; set; }
    }
}
