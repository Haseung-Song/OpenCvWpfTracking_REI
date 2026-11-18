using System;
using System.Threading.Tasks;
using OpenCvWpfTracking.Models.Main;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        public event Action RouteUiChanged
        {
            add
            {
                _routesFeature.RouteUiChanged += value;
            }

            remove
            {
                _routesFeature.RouteUiChanged -= value;
            }
        }

        public bool RouteInfrared
        {
            get { return _routesFeature.RouteInfrared; }
            set { _routesFeature.RouteInfrared = value; }
        }

        public int RouteIndex
        {
            get { return _routesFeature.RouteIndex; }
            set { _routesFeature.RouteIndex = value; }
        }

        public ScanRoute CurrentRoute
        {
            get { return _routesFeature.CurrentRoute; }
        }

        public ScanRoutePoint SelectedRoutePoint
        {
            get { return _routesFeature.SelectedRoutePoint; }
            set { _routesFeature.SelectedRoutePoint = value; }
        }

        public bool IsRouteRunning
        {
            get { return _routesFeature.IsRouteRunning; }
        }

        public string RouteStatus
        {
            get { return _routesFeature.RouteStatus; }
            private set { _routesFeature.RouteStatus = value; }
        }

        /// <summary>
        /// 현재 편집 중인 장비·채널별 감시 경로를 저장한다.
        /// </summary>
        public void SaveCurrentRoute()
        {
            _routesFeature.SaveCurrentRoute();
        }

        /// <summary>
        /// 현재 위치를 선택 경로의 새 순회 지점으로 추가한다.
        /// </summary>
        public void AddCurrentRoutePoint()
        {
            _routesFeature.AddCurrentRoutePoint();
        }

        /// <summary>
        /// 선택한 경로 지점을 삭제하고 화면 목록을 갱신한다.
        /// </summary>
        public void DeleteSelectedRoutePoint()
        {
            _routesFeature.DeleteSelectedRoutePoint();
        }

        /// <summary>
        /// 경로 실행 취소를 요청하고 진행 중 이동을 정리한다.
        /// </summary>
        public void StopRoute()
        {
            _routesFeature.StopRoute();
        }

        /// <summary>
        /// 경로 지점을 순회하며 이동·렌즈 복원·관측 대기와 취소를 처리한다.
        /// </summary>
        public Task RunRouteAsync(bool single)
        {
            return _routesFeature.RunRouteAsync(single);
        }
    }
}
