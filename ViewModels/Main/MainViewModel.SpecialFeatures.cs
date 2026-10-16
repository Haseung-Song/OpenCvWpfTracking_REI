using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Communication.WebAgent;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private ObservableCollection<CameraSpecialFeatureRow> _eoSpecialFeatures,_irSpecialFeatures;
        private readonly Queue<CameraSpecialFeatureRow> _specialReadQueue=new Queue<CameraSpecialFeatureRow>();
        private ICommand _refreshSpecialFeaturesCommand;
        private int _specialFeatureEpoch;
        private bool _specialWasReady;
        private string _specialTargetKey;
        private long _lastSpecialCapabilityQuery;
        private byte _specialCapabilityReceived;
        private long _specialCapabilityDeadline;
        private string _specialFeatureSummary;
        public ObservableCollection<CameraSpecialFeatureRow> EoSpecialFeatures {get{EnsureSpecialFeatures();return _eoSpecialFeatures;}}
        public ObservableCollection<CameraSpecialFeatureRow> IrSpecialFeatures {get{EnsureSpecialFeatures();return _irSpecialFeatures;}}
        // 2026-10-16 V32_0: LA/WEB share camera feature packets; RTSP alone is not a control connection.
        private bool SpecialFeatureReady => CameraFeatureTransportReady && IsSelectedConnectionTarget;
        private string SpecialFeatureAgentName => IsSelectedControlAgentWebAgent ? "WEB AGENT" : "LA AGENT";
        private string SpecialFeatureTargetKey => _connectionSessionId+"|"+SelectedControlAgentProfile.DisplayName+"|"+ControlAgentIp+"|"+ControlAgentPortText;
        public bool CanRefreshSpecialFeatures => SpecialFeatureReady && DateTime.UtcNow.Ticks-_lastSpecialCapabilityQuery>=TimeSpan.FromSeconds(2).Ticks;
        public string SpecialFeatureSummary => SpecialFeatureAgentName+" · "+(!SpecialFeatureReady?"선택 장비 연결 후 지원 조회가 가능합니다.":_specialFeatureSummary??"지원 미조회 · 조회 후 사용하세요.");
        public ICommand RefreshSpecialFeaturesCommand => _refreshSpecialFeaturesCommand??(_refreshSpecialFeaturesCommand=new RelayCommand(QuerySpecialFeatureSupport,()=>CanRefreshSpecialFeatures));
        private IEnumerable<CameraSpecialFeatureRow> SpecialFeatureRows => _eoSpecialFeatures.Concat(_irSpecialFeatures);

        /// <summary>2026-10-14 V31_5: 규격 대상별 행을 한 번 생성한다. 기존 LENS 제어는 이동하거나 제거하지 않는다.</summary>
        private void EnsureSpecialFeatures()
        {
            if(_eoSpecialFeatures!=null)return;
            _eoSpecialFeatures=new ObservableCollection<CameraSpecialFeatureRow>();_irSpecialFeatures=new ObservableCollection<CameraSpecialFeatureRow>();
            foreach(var definition in CameraFeatureDefinition.All)
                for(byte target=0;target<2;target++)
                    if((definition.Targets&(1<<target))!=0)
                    {
                        var row=new CameraSpecialFeatureRow(definition,target,()=>SpecialFeatureReady,
                            ()=>IsOperationCommandEnabled && !IsControlInputLocked,SendSpecialFeature,QuerySpecialFeatureState,
                            ()=>SpecialFeatureAgentName+" 연결 필요");
                        (target==0?_eoSpecialFeatures:_irSpecialFeatures).Add(row);
                    }
        }
        /// <summary>연결/모드 변경 시 이전 장비 capability와 지연된 UI 응답의 적용을 무효화한다.</summary>
        private void ResetSpecialFeatureSession()
        {
            ++_specialFeatureEpoch;_specialReadQueue.Clear();_lastSpecialCapabilityQuery=0;_specialFeatureSummary=null;_specialCapabilityReceived=0;_specialCapabilityDeadline=0;
            if(_eoSpecialFeatures!=null)foreach(var row in SpecialFeatureRows)row.Reset();
            OnPropertyChanged(nameof(SpecialFeatureSummary));OnPropertyChanged(nameof(CanRefreshSpecialFeatures));
        }
        public void QuerySpecialFeatureSupport()
        {
            EnsureSpecialFeatures();
            if(!CanRefreshSpecialFeatures)return;
            _lastSpecialCapabilityQuery=DateTime.UtcNow.Ticks;
            _specialCapabilityReceived=0;_specialCapabilityDeadline=DateTime.UtcNow.AddSeconds(3).Ticks;
            bool eo=_controlCommandService.RequestCameraFeatureCapability(0),ir=_controlCommandService.RequestCameraFeatureCapability(1);
            _specialFeatureSummary=eo && ir?"EO / IR 지원 조회 전송 · 응답 대기":"지원 조회 전송 실패 · 연결 확인";
            OnPropertyChanged(nameof(SpecialFeatureSummary));
            ConsoleLogHelper.Command("SPECIAL FEATURE","CAPABILITY / EO_TX="+eo+" / IR_TX="+ir);
        }
        /// <summary>UI 큐에 넣기 전에 세션을 캡처하고 연결 변경 후 도착한 callback을 버린다.</summary>
        private void DispatchSpecialFeaturePacket(byte[] p,Action<byte[]> apply)
        {
            int epoch=_specialFeatureEpoch;string session=_connectionSessionId,target=SpecialFeatureTargetKey;var copy=(byte[])p.Clone();
            Application.Current?.Dispatcher.BeginInvoke(new Action(()=>
            {if(epoch==_specialFeatureEpoch && session==_connectionSessionId && target==SpecialFeatureTargetKey && SpecialFeatureReady)apply(copy);}));
        }
        private void ApplySpecialFeatureCapability(byte[] p)
        {
            byte target;List<CameraFeatureCapability> entries;
            if(!CameraFeatureProtocol.TryCapability(p,out target,out entries))return;
            EnsureSpecialFeatures();
            _specialCapabilityReceived|=(byte)(1<<target);
            foreach(var row in SpecialFeatureRows.Where(r=>r.Target==target))
            {
                row.SetCapability(entries.FirstOrDefault(c=>c.Id==row.Id));
                if(row.Capability!=null && (row.Capability.Flags&3)==3 && !row.IsTrigger && row.Id!=0x1D && !_specialReadQueue.Contains(row))_specialReadQueue.Enqueue(row);
            }
            _specialFeatureSummary="지원 EO "+((_specialCapabilityReceived&1)!=0?_eoSpecialFeatures.Count(r=>r.Capability!=null && (r.Capability.Flags&1)!=0).ToString():"미응답")+
                " / IR "+((_specialCapabilityReceived&2)!=0?_irSpecialFeatures.Count(r=>r.Capability!=null && (r.Capability.Flags&1)!=0).ToString():"미응답")+" · 값은 장비 응답 기준";
            OnPropertyChanged(nameof(SpecialFeatureSummary));CommandManager.InvalidateRequerySuggested();
        }
        private void ApplySpecialFeatureState(byte[] p)
        {
            byte target,id;bool supported,valid;short value;
            if(!CameraFeatureProtocol.TryState(p,out target,out id,out supported,out valid,out value))return;
            EnsureSpecialFeatures();var row=SpecialFeatureRows.FirstOrDefault(r=>r.Target==target && r.Id==id);
            row?.SetState(supported,valid,value,BitConverter.ToUInt64(p,10));
            CommandManager.InvalidateRequerySuggested();
        }
        private void ApplySpecialFeatureUnavailable(byte[] p)
        {
            if(p.Length<6 || p[0]!=1 || p[1]>1 || p[4]>1 || (p[3]!=0xE9 && p[3]!=0xE7))return;
            EnsureSpecialFeatures();byte id=p[3]==0xE9?p[2]:p[5];
            var row=SpecialFeatureRows.FirstOrDefault(r=>r.Target==p[4] && r.Id==id);
            row?.Reject(p[1]==0);
            CommandManager.InvalidateRequerySuggested();
        }
        /// <summary>supported/settable·지원값·운용 잠금을 재검증한 단발 E9만 전송한다.</summary>
        private void SendSpecialFeature(CameraSpecialFeatureRow row,int value)
        {
            if(!SpecialFeatureReady || !IsOperationCommandEnabled || IsControlInputLocked || !row.CanEdit || !row.Allows(value))return;
            // 지원 장비의 Defog/WDR 상호 배타 조건을 임의의 자동 OFF 명령으로 해결하지 않는다.
            var peers=row.Target==0?_eoSpecialFeatures:_irSpecialFeatures;
            if((row.Id==0x01 && value==1 && peers.Any(r=>r.Id==0x10 && r.HasFreshValue(3))) ||
               (row.Id==0x10 && value==3 && peers.Any(r=>r.Id==0x01 && r.HasFreshValue(1))))
            {row.Warn("Defog / WDR 동시 사용 불가 · 기존 기능 OFF 후 적용");return;}
            bool sent=row.Id==0x1D?SetDigitalZoom(row.Target==1,value==1):_controlCommandService.SetCameraFeature(row.Id,row.Target,(byte)value);
            row.MarkSent(value,sent);
            if(sent && !row.IsTrigger && row.Capability!=null && (row.Capability.Flags&2)!=0)
            {
                var rest=_specialReadQueue.Where(r=>r!=row).ToArray();_specialReadQueue.Clear();
                _specialReadQueue.Enqueue(row);foreach(var queued in rest)_specialReadQueue.Enqueue(queued);
            }
            ConsoleLogHelper.Command("SPECIAL FEATURE","TARGET="+row.Target+" / ID=0x"+row.Id.ToString("X2")+" / VALUE="+value+" / TX="+sent+" / not device ACK");
            CommandManager.InvalidateRequerySuggested();
        }
        private void QuerySpecialFeatureState(CameraSpecialFeatureRow row)
        {
            if(!row.CanQuery)return;
            row.QuerySent(_controlCommandService.RequestCameraFeatureState(row.Target,row.Id));
        }
        /// <summary>기존 250ms UI 타이머를 사용한다. 일괄 조회도 한 틱당 한 건으로 제한한다.</summary>
        private void TickSpecialFeatures()
        {
            bool ready=SpecialFeatureReady;
            string targetKey=SpecialFeatureTargetKey;
            if(ready!=_specialWasReady || targetKey!=_specialTargetKey)
            {_specialWasReady=ready;_specialTargetKey=targetKey;ResetSpecialFeatureSession();if(ready)QuerySpecialFeatureSupport();}
            if(_eoSpecialFeatures==null)return;
            if(_specialCapabilityDeadline!=0 && DateTime.UtcNow.Ticks>=_specialCapabilityDeadline)
            {
                _specialCapabilityDeadline=0;
                if(_specialCapabilityReceived!=3)
                {
                    _specialFeatureSummary="지원 응답 없음: "+((_specialCapabilityReceived&1)==0?"EO ":"")+((_specialCapabilityReceived&2)==0?"IR":"")+" · 지원 조회로 재시도";
                    foreach(var row in SpecialFeatureRows.Where(r=>(_specialCapabilityReceived&(1<<r.Target))==0 && !r.CapabilityKnown))row.Warn("지원 응답 없음 · 제어 잠금");
                    OnPropertyChanged(nameof(SpecialFeatureSummary));
                }
            }
            bool changed=false;
            foreach(var row in SpecialFeatureRows)changed|=row.Refresh();
            if(ready && _specialReadQueue.Count>0)
            {
                var row=_specialReadQueue.Peek();
                if(row.CanQuery){_specialReadQueue.Dequeue();QuerySpecialFeatureState(row);}
                else if(row.Capability==null || (row.Capability.Flags&3)!=3)_specialReadQueue.Dequeue();
            }
            OnPropertyChanged(nameof(CanRefreshSpecialFeatures));
            if(changed)CommandManager.InvalidateRequerySuggested();
        }
    }
}
