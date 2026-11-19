using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Services.Control;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>추적 판단에서 화면 상태·장비 통신 구현을 분리하는 협력 계약이다.</summary>
    internal interface IAutoTrackingFeatureContext
    {
        string SessionKey { get; }

        double ZoomRatio { get; }

        double Confidence { get; }

        bool CanIssueMove { get; }

        /// <summary>현재 연결·영상·렌즈·다른 제어 작업의 차단 사유를 반환한다.</summary>
        string GetBlockReason();

        /// <summary>AI가 보고한 EO 원본 크기와 Viewer 크기의 일치를 확인한다.</summary>
        bool TryGetFrameSize(out int width, out int height);

        /// <summary>현재 RTSP 매핑의 실제 클래스명을 UI 스레드에서 해석한다.</summary>
        void ResolveClassNames(AiDetectionResult result);

        /// <summary>기존 영상 투영과 장비 명령을 사용하여 제한된 추적 보정을 요청한다.</summary>
        Task<bool> MoveAsync(TrackingTarget target, TrackingMotionLease lease);

        /// <summary>추적이 소유한 PT 이동만 취소하고 기존 도달 추적값을 정리한다.</summary>
        void StopOwnedMove();

        /// <summary>체크 상태와 운용 상태 변경을 기존 화면 알림 경로에 전달한다.</summary>
        void Publish(bool enabled, string status);
    }

    /// <summary>
    /// EO 단일 표적 추적의 시작·주기 제어·중단을 담당한다.
    /// 최신 프레임만 보관하며 추적 중 사용자 제어를 잠그고 명시적 STOP 뒤에만 수동 조작을 허용한다.
    /// </summary>
    internal sealed class AutoTrackingFeature
    {
        private readonly IAutoTrackingFeatureContext _context;
        private readonly AutoTrackingController _controller = new AutoTrackingController();
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherTimer _timer;
        private readonly object _pendingSync = new object();
        private Tuple<AiDetectionResult, DateTime, int> _pendingResult;
        private TrackingMotionLease _lease;
        private string _sessionKey;
        private DateTime _startedUtc;
        private DateTime _lastCommandUtc;
        private bool _inFlight;
        private int _generation;

        internal bool IsEnabled { get; private set; }

        internal string StatusText { get; private set; } = "AUTO TRACKING OFF / EO 단일 Drone·UAV 표적";

        /// <summary>UI 스레드의 저우선순위 감시 타이머와 협력 계약을 준비한다. 시작 전에는 타이머를 실행하지 않는다.</summary>
        internal AutoTrackingFeature(IAutoTrackingFeatureContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dispatcher = Dispatcher.CurrentDispatcher;
            _timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += OnTick;
        }

        /// <summary>체크를 켜면 현재 세션만 추적하고, 끄면 소유 이동과 표적·필터를 즉시 정리한다.</summary>
        internal void SetEnabled(bool enabled)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(() => SetEnabled(enabled)));
                return;
            }

            if (!enabled)
            {
                Stop("사용자 해제");
                return;
            }

            if (IsEnabled)
            {
                return;
            }

            string blocked = _context.GetBlockReason();

            if (blocked != null || !_context.TryGetFrameSize(out int width, out int height) || !_context.CanIssueMove)
            {
                SetStatus(false, "시작 차단: " + (blocked ?? "AI 원본 크기 미확인/영상 크기 불일치 또는 PT 이동 중"));
                ConsoleLogHelper.Warning("AUTO TRACKING", StatusText);
                return;
            }

            _controller.Reset();
            _sessionKey = _context.SessionKey;
            _startedUtc = DateTime.UtcNow;
            _lastCommandUtc = DateTime.MinValue;
            Interlocked.Increment(ref _generation);
            SetStatus(true, "EO Drone/UAV 표적 대기 / 3프레임 확인");
            _timer.Start();
            ConsoleLogHelper.State("AUTO TRACKING", "START / EO / latest target only / no auto zoom-focus");
        }

        /// <summary>표시용 Hold 처리 전의 원본 결과를 전달받아 최신 표적만 교체한다.</summary>
        internal void Update(AiDetectionResult result, DateTime receiveTime)
        {
            if (!IsEnabled || result?.RtspIndex != 0)
            {
                return;
            }

            if (!_context.TryGetFrameSize(out int width, out int height))
            {
                Stop("AI 원본 크기 변경 또는 미확인");
                return;
            }

            bool hadLock = _controller.HasLock;
            _controller.Update(result, width, height, receiveTime.ToUniversalTime(), DateTime.UtcNow, _context.Confidence);

            if (!hadLock && _controller.HasLock)
            {
                SetStatus(true, "EO 표적 확인 중");
                ConsoleLogHelper.State("AUTO TRACKING", "TARGET ACQUIRED / EO / confirmation pending");
            }

            if (_controller.GetTarget(DateTime.UtcNow) == null)
            {
                StopOwnedMotion();
            }
        }

        /// <summary>
        /// 수신 스레드에서는 최신 EO 프레임 한 개만 교체한다. 빈 프레임도 교체하며
        /// 표시용 BBox 유지 정책이나 Dispatcher 작업 수에 추적 입력을 종속시키지 않는다.
        /// </summary>
        internal void Enqueue(AiDetectionResult result, DateTime receiveTime)
        {
            int generation = Volatile.Read(ref _generation);

            if (result?.RtspIndex != 0)
            {
                return;
            }

            lock (_pendingSync)
            {
                if (IsEnabled && generation == Volatile.Read(ref _generation))
                {
                    _pendingResult = Tuple.Create(result, receiveTime, generation);
                }
            }
        }

        /// <summary>UI 스레드에서 최신 프레임을 한 번만 소비하고 현재 매핑으로 클래스명을 확인한다.</summary>
        private void DrainLatest()
        {
            Tuple<AiDetectionResult, DateTime, int> pending;

            lock (_pendingSync)
            {
                pending = _pendingResult;
                _pendingResult = null;
            }

            if (pending != null && IsEnabled && pending.Item3 == Volatile.Read(ref _generation))
            {
                _context.ResolveClassNames(pending.Item1);
                Update(pending.Item1, pending.Item2);
            }
        }

        /// <summary>
        /// 명시적 STOP·연결 종료·종료 정리 시 추적을 끈다. 잠금 중 거부된 사용자 입력은 이 함수를 호출하지 않는다.
        /// 다른 스레드에서는 먼저 송신 세대를 무효화하고 UI에서 자원을 정리한다.
        /// </summary>
        internal void Stop(string reason)
        {
            if (!_dispatcher.CheckAccess())
            {
                Interlocked.Increment(ref _generation);
                _dispatcher.BeginInvoke(new Action(() => Stop(reason)));
                return;
            }

            bool active = IsEnabled || _lease != null;
            _timer.Stop();
            Interlocked.Increment(ref _generation);
            StopOwnedMotion();
            _controller.Reset();

            lock (_pendingSync)
            {
                _pendingResult = null;
            }

            SetStatus(false, "AUTO TRACKING OFF / " + reason);

            if (active)
            {
                ConsoleLogHelper.State("AUTO TRACKING", "STOP / " + reason);
            }
        }

        /// <summary>수신 유효 시간과 제어 소유권을 감시하고 장비 응답을 기다리며 한 번에 하나의 이동만 요청한다.</summary>
        private async void OnTick(object sender, EventArgs e)
        {
            try
            {
                if (!IsEnabled)
                {
                    return;
                }

                string blocked = _context.GetBlockReason();

                if (blocked != null || _sessionKey != _context.SessionKey)
                {
                    Stop(blocked ?? "장비/영상/AI 매핑 변경");
                    return;
                }

                DrainLatest();

                if (!IsEnabled)
                {
                    return;
                }

                DateTime now = DateTime.UtcNow;
                TrackingTarget target = _controller.GetTarget(now);

                if (target == null)
                {
                    StopOwnedMotion();
                    DateTime since = _controller.HasLock ? _controller.LastSeenUtc : _startedUtc;
                    int timeout = _controller.HasLock ? AutoTrackingController.FreshnessMilliseconds : 5000;

                    if ((now - since).TotalMilliseconds > timeout)
                    {
                        Stop(_controller.HasLock ? "표적 소실/탐지 수신 지연" : "추적 가능 Drone/UAV 표적 없음");
                    }

                    return;
                }

                if (_inFlight || !_context.CanIssueMove || (now - _lastCommandUtc).TotalMilliseconds < AutoTrackingController.CommandInterval(_context.ZoomRatio))
                {
                    return;
                }

                int generation = Volatile.Read(ref _generation);
                var lease = new TrackingMotionLease
                {
                    Filter = _controller.FilterCorrection,
                    CanContinue = () =>
                    {
                        DrainLatest();

                        return IsEnabled && generation == Volatile.Read(ref _generation) &&
                            _sessionKey == _context.SessionKey && _context.GetBlockReason() == null && _controller.GetTarget(DateTime.UtcNow) != null;
                    }
                };
                _lease = lease;
                _inFlight = true;
                _lastCommandUtc = now;
                SetStatus(true, "EO 단일 표적 추적 중 / ID=" + target.Box.ObjectId);

                try
                {
                    bool sent = await _context.MoveAsync(target, lease);

                    if (!sent && lease.CanContinue())
                    {
                        Stop("PT 이동 요청 거절/송신 실패");
                    }
                }
                finally
                {
                    _inFlight = false;
                }
            }
            catch (Exception ex)
            {
                Stop("추적 처리 오류");
                ConsoleLogHelper.Error("AUTO TRACKING", "Tracking cycle failed", ex);
            }
        }

        /// <summary>보류 중인 송신을 무효화하며 실제 송신한 추적 이동에만 PT 정지를 전송한다.</summary>
        private void StopOwnedMotion()
        {
            if (_lease == null)
            {
                return;
            }

            Interlocked.Increment(ref _generation);
            bool sent = _lease.MotionSent;
            _lease = null;

            if (sent)
            {
                try
                {
                    _context.StopOwnedMove();
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.Error("AUTO TRACKING", "Owned PT stop failed; tracking remains disabled/cancelled", ex);
                }
            }
        }

        /// <summary>상태가 바뀔 때만 화면 알림을 발행하여 반복 로그·바인딩 갱신을 줄인다.</summary>
        private void SetStatus(bool enabled, string status)
        {
            if (IsEnabled == enabled && StatusText == status)
            {
                return;
            }

            IsEnabled = enabled;
            StatusText = status;
            _context.Publish(enabled, status);
        }
    }
}
