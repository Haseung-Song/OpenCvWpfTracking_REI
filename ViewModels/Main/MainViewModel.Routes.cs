using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenCvWpfTracking.Models.Main;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private ScanRouteStore routeStore;
        private ScanRouteStore RouteStore=>routeStore??(routeStore=new ScanRouteStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Config","ScanRoutes.xml")));
        private bool routeIr;private int routeIndex;private CancellationTokenSource routeCts;
        public event Action RouteUiChanged;
        public bool RouteInfrared{get=>routeIr;set{if(IsRouteRunning)return;routeIr=value;SelectedRoutePoint=null;RouteUiChanged?.Invoke();}}
        public int RouteIndex{get=>routeIndex;set{if(IsRouteRunning)return;routeIndex=Math.Max(0,Math.Min(4,value));SelectedRoutePoint=null;RouteUiChanged?.Invoke();}}
        private string routeDeviceKey;
        public ScanRoute CurrentRoute{get{
            string key=PanoramaDeviceKey;
            if(routeDeviceKey!=key){routeDeviceKey=key;selectedRoutePoint=null;}
            return RouteStore.Get(key,RouteInfrared)[RouteIndex];
        }}
        private ScanRoutePoint selectedRoutePoint;
        public ScanRoutePoint SelectedRoutePoint{get=>selectedRoutePoint;set{if(ReferenceEquals(selectedRoutePoint,value))return;selectedRoutePoint=value;OnPropertyChanged();RouteUiChanged?.Invoke();}}
        public bool IsRouteRunning=>routeCts!=null;
        public string RouteStatus{get;private set;}="경로 선택 후 현재 위치 추가";
        // 2026-10-15 V32_0: async completion must never mutate WPF controls from a worker thread.
        private void RouteState(string text)
        {
            Action update=()=>{RouteStatus=text;OnPropertyChanged(nameof(RouteStatus));OnPropertyChanged(nameof(IsRouteRunning));foreach(string property in new[]{nameof(IsPresetScanControlLocked),nameof(IsControlInputLocked),nameof(IsOperationCommandEnabled),nameof(IsEventAlertControlEnabled),nameof(ControlLockTitle),nameof(IsOperationLockOverlayVisible)})OnPropertyChanged(property);CommandManager.InvalidateRequerySuggested();RouteUiChanged?.Invoke();};
            var dispatcher=System.Windows.Application.Current?.Dispatcher;
            if(dispatcher==null||dispatcher.CheckAccess())update();
            else if(!dispatcher.HasShutdownStarted&&!dispatcher.HasShutdownFinished)dispatcher.BeginInvoke(update);
        }
        public void SaveCurrentRoute(){if(IsRouteRunning)throw new InvalidOperationException("운용 중 경로 편집 불가");CurrentRoute.Renumber();RouteStore.Save(PanoramaDeviceKey,RouteInfrared);RouteState("경로 저장 완료 / "+CurrentRoute.Points.Count+"점");}
        public void AddCurrentRoutePoint(){if(IsRouteRunning||IsControlInputLocked||!OpticalFeedbackFresh(RouteInfrared))throw new InvalidOperationException("정지 상태의 최신 위치·렌즈 수신값이 필요합니다.");
            if(CurrentRoute.Points.Count>=1000)throw new InvalidOperationException("경로당 최대 1000점");
            var p=new ScanRoutePoint{Order=CurrentRoute.Points.Count+1,Name="지점 "+(CurrentRoute.Points.Count+1),Pan=CurrentPanoramaPan,Tilt=CurrentPanoramaTilt,EoZoom=GetCurrentPresetStandardZoom(),EoFocus=GetCurrentPresetStandardFocus(),IrZoom=GetCurrentIrZoomStandardPosition(),IrFocus=GetCurrentIrFocusStandardPosition(),Speed=CurrentRoute.Speed,Delay=CurrentRoute.Delay};
            if(!p.Valid)throw new InvalidOperationException("좌표/렌즈 값 확인 필요");if(CurrentRoute.Near(p))throw new InvalidOperationException("기존 지점과 0.01° 이내로 가까워 추가하지 않았습니다.");CurrentRoute.Points.Add(p);SelectedRoutePoint=p;SaveCurrentRoute();}
        public void DeleteSelectedRoutePoint(){if(IsRouteRunning)throw new InvalidOperationException("운용 중 삭제 불가");if(SelectedRoutePoint==null)return;CurrentRoute.Points.Remove(SelectedRoutePoint);SelectedRoutePoint=null;SaveCurrentRoute();}
        private void StopRouteMotion()
        {
            foreach(Action stop in new Action[]{()=>_controlCommandService.StopPanTiltPositionMove(),()=>_controlCommandService.StopEoZoom(),()=>_controlCommandService.StopIrZoom(),()=>_controlCommandService.StopEoFocus(),()=>_controlCommandService.StopIrFocus()})
                try{stop();}catch(Exception error){Common.ConsoleLogHelper.Error("LOCAL ROUTE","Axis stop failed; attempting remaining axes",error);}
        }
        public void StopRoute(){var cts=routeCts;if(cts==null)return;try{cts.Cancel();}catch(ObjectDisposedException){return;}StopRouteMotion();RouteState("경로 중지 요청");}
        public async Task RunRouteAsync(bool single)
        {
            if(IsRouteRunning||!IsOperationCommandEnabled||IsControlInputLocked||!_isDeviceConnectionRequested||!_laTcpService.IsConnected||!PanoramaPoseFresh)throw new InvalidOperationException("장비 연결·수신 위치·운용 잠금 상태를 확인하세요.");
            if(_laPresetSingleMoveCts!=null||_presetSingleMoveCts!=null)throw new InvalidOperationException("기존 프리셋 이동이 끝난 후 실행하세요.");
            if(!OpticalFeedbackFresh(RouteInfrared)||!HasFreshPresetLens(RouteInfrared,false))throw new InvalidOperationException("현재 채널의 최신 줌/초점 피드백이 필요합니다.");
            var currentRoute=CurrentRoute;
            if(single&&(SelectedRoutePoint==null||!currentRoute.Points.Contains(SelectedRoutePoint)))throw new InvalidOperationException("현재 장비·채널 경로의 지점을 다시 선택하세요.");
            var queue=single?new[]{SelectedRoutePoint.Copy()}:currentRoute.Queue();if(queue.Length==0)throw new InvalidOperationException("관측 지점을 선택/추가하세요.");
            if(single&&!CurrentRoute.IndividualSettings){queue[0].Speed=CurrentRoute.Speed;queue[0].Delay=CurrentRoute.Delay;}
            if(!CurrentRoute.Valid)throw new InvalidOperationException("경로 설정 범위 오류");bool ir=RouteInfrared;string device=PanoramaDeviceKey;var cts=new CancellationTokenSource();routeCts=cts;ReleaseControlOwner("video");RouteState("경로 시작");
            try{do{foreach(var point in queue){cts.Token.ThrowIfCancellationRequested();if(device!=PanoramaDeviceKey||!_laTcpService.IsConnected||!PanoramaPoseFresh)throw new InvalidOperationException("장비/통신/수신 위치 변경으로 중지");
                        SelectedRoutePoint=point;RouteState("이동 중 · "+point.Name);
                        // 2026-10-16 V32: local route observation starts after all motion settles.
                        // Legacy PRESET retains its shared Delay budget; route lens restoration has a separate 30s cap.
                        bool result=await MoveLaPresetPointDirectAsync(point.AsPreset(),cts.Token,false,point.Speed,30,ir);
                        if(!result)throw new InvalidOperationException("도착/렌즈 확인 실패 · "+point.Name);
                        RouteState("정지 관측 중 · "+point.Name+" / "+point.Delay+"초");
                        await Task.Delay(TimeSpan.FromSeconds(point.Delay),cts.Token);
                        RouteState("관측 완료 · "+point.Name);
                    }}while(!single&&!cts.IsCancellationRequested);
                RouteStatus="지점 이동 완료";
            }catch(OperationCanceledException){RouteStatus="경로 중지 완료";}
            catch(Exception e){RouteStatus="경로 중지: "+e.Message;Common.ConsoleLogHelper.Warning("LOCAL ROUTE",e.Message);}
            finally{StopRouteMotion();routeCts=null;cts.Dispose();RouteState(RouteStatus);}
        }
    }
}
