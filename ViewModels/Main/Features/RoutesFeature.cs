using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenCvWpfTracking.Models.Main;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>감시 경로 편집과 실행에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IRoutesFeatureContext
    {
        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        bool _isDeviceConnectionRequested { get; set; }

        CancellationTokenSource _laPresetSingleMoveCts { get; set; }

        Services.Communication.ITcpClientServicePort _laTcpService { get; }

        CancellationTokenSource _presetSingleMoveCts { get; set; }

        string ControlLockTitle { get; }

        double CurrentPanoramaPan { get; }

        double CurrentPanoramaTilt { get; }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// -&gt; Standard 0(Far) Raw 0(Near) -&gt; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrFocusStandardPosition();

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// -&gt; Standard 0(Wide) Raw 0(Tele) -&gt; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrZoomStandardPosition();

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardFocus();

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardZoom();

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다.
        /// </summary>
        bool HasFreshPresetLens(bool ir, bool zoom);

        bool IsControlInputLocked { get; }

        bool IsEventAlertControlEnabled { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsOperationLockOverlayVisible { get; }

        bool IsPresetScanControlLocked { get; }

        /// <summary>
        /// 저장된 PRESET 1 한 지점의 PTZF 값을 직접 복원한다.
        /// </summary>
        Task<bool> MoveLaPresetPointDirectAsync(PresetPointOption preset, CancellationToken cancellationToken, bool scanTiming = false, int? routeSpeed = null, int? routeDelay = null, bool? onlyIr = null);

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        /// <summary>
        /// 보정에 필요한 렌즈와 위치 응답의 신선도를 검사한다.
        /// </summary>
        bool OpticalFeedbackFresh(bool ir);

        string PanoramaDeviceKey { get; }

        bool PanoramaPoseFresh { get; }

        /// <summary>
        /// 지정 창/입력 소유자의 토큰만 일괄 해제한다.
        /// </summary>
        void ReleaseControlOwner(string owner);
    }

    /// <summary>감시 경로 편집과 실행 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class RoutesFeature
    {

        #region [협력 객체와 초기화]

        private readonly IRoutesFeatureContext _context;

        /// <summary>
        /// 자동 순회 경로 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal RoutesFeature(IRoutesFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 기능 객체가 사용하는 초기 상태·목록·타이머를 준비한다.
        /// </summary>
        internal void InitializeState()
        {
            RouteStatus = "경로 선택 후 현재 위치 추가";
        }

        #endregion

        #region [상태 필드]

        internal ScanRouteStore routeStore;

        internal bool routeIr;

        internal int routeIndex;

        internal CancellationTokenSource routeCts;

        internal string routeDeviceKey;

        internal ScanRoutePoint selectedRoutePoint;

        #endregion

        #region [화면 속성과 이벤트]

        internal ScanRouteStore RouteStore
        {
            get
            {
                return routeStore ?? (routeStore = new ScanRouteStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ScanRoutes.xml")));
            }
        }

        internal event Action RouteUiChanged;

        internal bool RouteInfrared
        {
            get { return routeIr; }

            set
            {
                if (IsRouteRunning)
                {
                    return;
                }

                routeIr = value;
                SelectedRoutePoint = null;

                RouteUiChanged?.Invoke();
            }
        }

        internal int RouteIndex
        {
            get { return routeIndex; }

            set
            {
                if (IsRouteRunning)
                {
                    return;
                }

                routeIndex = Math.Max(0, Math.Min(4, value));

                SelectedRoutePoint = null;

                RouteUiChanged?.Invoke();
            }
        }

        internal ScanRoute CurrentRoute
        {
            get
            {
                string key = _context.PanoramaDeviceKey;

                if (routeDeviceKey != key)
                {
                    routeDeviceKey = key;
                    selectedRoutePoint = null;
                }

                return RouteStore.Get(key, RouteInfrared)[RouteIndex];
            }
        }

        internal ScanRoutePoint SelectedRoutePoint
        {
            get { return selectedRoutePoint; }

            set
            {
                if (ReferenceEquals(selectedRoutePoint, value))
                {
                    return;
                }

                selectedRoutePoint = value;

                _context.OnPropertyChanged();

                RouteUiChanged?.Invoke();
            }
        }

        internal bool IsRouteRunning
        {
            get
            {
                return routeCts != null;
            }
        }

        internal string RouteStatus { get; set; }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 순회 진행 상황과 상태 안내를 화면에 반영한다.
        /// </summary>
        internal void RouteState(string text)
        {
            Action update = () =>
            {
                RouteStatus = text;

                _context.OnPropertyChanged(nameof(RouteStatus));

                _context.OnPropertyChanged(nameof(IsRouteRunning));

                foreach (string property in new[]
                {
                    nameof(_context.IsPresetScanControlLocked),
                    nameof(_context.IsControlInputLocked),
                    nameof(_context.IsOperationCommandEnabled),
                    nameof(_context.IsEventAlertControlEnabled),
                    nameof(_context.ControlLockTitle),
                    nameof(_context.IsOperationLockOverlayVisible)
                }

                )
                {
                    _context.OnPropertyChanged(property);
                }

                CommandManager.InvalidateRequerySuggested();

                RouteUiChanged?.Invoke();
            };

            var dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
            {
                update();
            }
            else if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
            {
                dispatcher.BeginInvoke(update);
            }
        }

        /// <summary>
        /// 현재 편집 중인 장비·채널별 감시 경로를 저장한다.
        /// </summary>
        internal void SaveCurrentRoute()
        {
            if (IsRouteRunning)
            {
                throw new InvalidOperationException("운용 중 경로 편집 불가");
            }

            CurrentRoute.Renumber();

            RouteStore.Save(_context.PanoramaDeviceKey, RouteInfrared);

            RouteState("경로 저장 완료 / " + CurrentRoute.Points.Count + "점");
        }

        /// <summary>
        /// 현재 위치를 선택 경로의 새 순회 지점으로 추가한다.
        /// </summary>
        internal void AddCurrentRoutePoint()
        {
            if (IsRouteRunning || _context.IsControlInputLocked || !_context.OpticalFeedbackFresh(RouteInfrared))
            {
                throw new InvalidOperationException("정지 상태의 최신 위치·렌즈 수신값이 필요합니다.");
            }

            if (CurrentRoute.Points.Count >= 1000)
            {
                throw new InvalidOperationException("경로당 최대 1000점");
            }

            var p = new ScanRoutePoint
            {
                Order = CurrentRoute.Points.Count + 1,
                Name = "지점 " + (CurrentRoute.Points.Count + 1),
                Pan = _context.CurrentPanoramaPan,
                Tilt = _context.CurrentPanoramaTilt,
                EoZoom = _context.GetCurrentPresetStandardZoom(),
                EoFocus = _context.GetCurrentPresetStandardFocus(),
                IrZoom = _context.GetCurrentIrZoomStandardPosition(),
                IrFocus = _context.GetCurrentIrFocusStandardPosition(),
                Speed = CurrentRoute.Speed,
                Delay = CurrentRoute.Delay
            };

            if (!p.Valid)
            {
                throw new InvalidOperationException("좌표/렌즈 값 확인 필요");
            }

            if (CurrentRoute.Near(p))
            {
                throw new InvalidOperationException("기존 지점과 0.01° 이내로 가까워 추가하지 않았습니다.");
            }

            CurrentRoute.Points.Add(p);

            SelectedRoutePoint = p;

            SaveCurrentRoute();
        }

        /// <summary>
        /// 선택한 경로 지점을 삭제하고 화면 목록을 갱신한다.
        /// </summary>
        internal void DeleteSelectedRoutePoint()
        {
            if (IsRouteRunning)
            {
                throw new InvalidOperationException("운용 중 삭제 불가");
            }

            if (SelectedRoutePoint == null)
            {
                return;
            }

            CurrentRoute.Points.Remove(SelectedRoutePoint);

            SelectedRoutePoint = null;

            SaveCurrentRoute();
        }

        /// <summary>
        /// 경로 실행으로 시작한 장비 이동을 정지한다.
        /// </summary>
        internal void StopRouteMotion()
        {
            foreach (Action stop in new Action[]
            {
                () => _context._controlCommandService.StopPanTiltPositionMove(),
                () => _context._controlCommandService.StopEoZoom(),
                () => _context._controlCommandService.StopIrZoom(),
                () => _context._controlCommandService.StopEoFocus(),
                () => _context._controlCommandService.StopIrFocus()
            }

            )
            {
                try
                {
                    stop();
                }
                catch (Exception error)
                {
                    Common.ConsoleLogHelper.Error("LOCAL ROUTE", "Axis stop failed; attempting remaining axes", error);
                }
            }
        }

        /// <summary>
        /// 경로 실행 취소를 요청하고 진행 중 이동을 정리한다.
        /// </summary>
        internal void StopRoute()
        {
            var cts = routeCts;

            if (cts == null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            StopRouteMotion();

            RouteState("경로 중지 요청");
        }

        /// <summary>
        /// 경로 지점을 순회하며 이동·렌즈 복원·관측 대기와 취소를 처리한다.
        /// </summary>
        internal async Task RunRouteAsync(bool single)
        {
            if (IsRouteRunning || !_context.IsOperationCommandEnabled || _context.IsControlInputLocked || !_context._isDeviceConnectionRequested || !_context._laTcpService.IsConnected || !_context.PanoramaPoseFresh)
            {
                throw new InvalidOperationException("장비 연결·수신 위치·운용 잠금 상태를 확인하세요.");
            }

            if (_context._laPresetSingleMoveCts != null || _context._presetSingleMoveCts != null)
            {
                throw new InvalidOperationException("기존 프리셋 이동이 끝난 후 실행하세요.");
            }

            if (!_context.OpticalFeedbackFresh(RouteInfrared) || !_context.HasFreshPresetLens(RouteInfrared, false))
            {
                throw new InvalidOperationException("현재 채널의 최신 줌/초점 피드백이 필요합니다.");
            }

            var currentRoute = CurrentRoute;

            if (single && (SelectedRoutePoint == null || !currentRoute.Points.Contains(SelectedRoutePoint)))
            {
                throw new InvalidOperationException("현재 장비·채널 경로의 지점을 다시 선택하세요.");
            }

            var queue = single ? new[]
            {
                SelectedRoutePoint.Copy()
            }

            : currentRoute.Queue();

            if (queue.Length == 0)
            {
                throw new InvalidOperationException("관측 지점을 선택/추가하세요.");
            }

            if (single && !CurrentRoute.IndividualSettings)
            {
                queue[0].Speed = CurrentRoute.Speed;
                queue[0].Delay = CurrentRoute.Delay;
            }

            if (!CurrentRoute.Valid)
            {
                throw new InvalidOperationException("경로 설정 범위 오류");
            }

            bool ir = RouteInfrared;

            string device = _context.PanoramaDeviceKey;

            var cts = new CancellationTokenSource();

            routeCts = cts;

            _context.ReleaseControlOwner("video");

            RouteState("경로 시작");

            try
            {
                do
                {
                    foreach (var point in queue)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        if (device != _context.PanoramaDeviceKey || !_context._laTcpService.IsConnected || !_context.PanoramaPoseFresh)
                        {
                            throw new InvalidOperationException("장비/통신/수신 위치 변경으로 중지");
                        }

                        SelectedRoutePoint = point;

                        RouteState("이동 중 · " + point.Name);

                        // 로컬 경로 감시 시간은 모든 이동이 끝난 뒤부터 계산한다.
                        // 기존 프리셋 대기 예산은 유지하고 경로 렌즈 복원에는 별도 30초 한도를 적용한다.
                        bool result = await _context.MoveLaPresetPointDirectAsync(point.AsPreset(), cts.Token, false, point.Speed, 30, ir);

                        if (!result)
                        {
                            throw new InvalidOperationException("도착/렌즈 확인 실패 · " + point.Name);
                        }

                        RouteState("정지 관측 중 · " + point.Name + " / " + point.Delay + "초");

                        await Task.Delay(TimeSpan.FromSeconds(point.Delay), cts.Token);

                        RouteState("관측 완료 · " + point.Name);
                    }
                }
                while (!single && !cts.IsCancellationRequested);

                RouteStatus = "지점 이동 완료";
            }
            catch (OperationCanceledException)
            {
                RouteStatus = "경로 중지 완료";
            }
            catch (Exception e)
            {
                RouteStatus = "경로 중지: " + e.Message;

                Common.ConsoleLogHelper.Warning("LOCAL ROUTE", e.Message);
            }
            finally
            {
                StopRouteMotion();

                routeCts = null;

                cts.Dispose();

                RouteState(RouteStatus);
            }
        }

        #endregion

    }
}
