using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication.WebAgent;

namespace OpenCvWpfTracking.Models.Main
{
    public sealed class CameraFeatureOption
    {
        public int Value { get; private set; }
        public string Label { get; private set; }
        public CameraFeatureOption(int value,string label){Value=value;Label=label;}
    }

    /// <summary>2026-10-14 V31_5: 입력 후보와 장비 상태를 분리하는 특수기능 한 행. UI 스레드에서만 갱신한다.</summary>
    public sealed class CameraSpecialFeatureRow : INotifyPropertyChanged
    {
        private readonly Func<bool> _ready;
        private readonly Func<bool> _unlocked;
        private readonly Action<CameraSpecialFeatureRow,int> _send;
        private readonly Action<CameraSpecialFeatureRow> _query;
        private string _input="50",_message,_lastSignature;
        private CameraFeatureOption _selected;
        private bool _dirty;
        private long _validTicks,_pendingTicks,_nextActionTicks;
        private long _queryTicks;
        private ulong _lastDeviceTimestamp;
        private int? _reported,_requested;
        public CameraFeatureDefinition Definition { get; private set; }
        public byte Target { get; private set; }
        public CameraFeatureCapability Capability { get; private set; }
        public bool CapabilityKnown { get; private set; }
        public byte Id => Definition.Id;
        public string Name => Definition.Name;
        public string ProtocolLabel => (Target==0?"EO":"IR")+" / Feature 0x"+Id.ToString("X2");
        public bool IsBoolean => Definition.ValueType==0;
        public bool IsNumeric => Definition.ValueType==2;
        public bool HasOptions => Definition.Options.Length>0 && !IsBoolean;
        public bool IsTrigger => Definition.ValueType==3;
        public string ApplyLabel => IsTrigger?"실행":"적용";
        public ObservableCollection<CameraFeatureOption> Options { get; }=new ObservableCollection<CameraFeatureOption>();
        public string InputText {get=>_input;set{if(_input==value)return;_input=value;_dirty=true;Changed(nameof(InputText));}}
        public CameraFeatureOption SelectedOption {get=>_selected;set{if(_selected==value)return;_selected=value;_dirty=true;Changed(nameof(SelectedOption));}}
        public ICommand ApplyCommand {get;private set;}
        public ICommand OffCommand {get;private set;}
        public ICommand OnCommand {get;private set;}
        public ICommand QueryCommand {get;private set;}
        public bool CanEdit => _ready() && _unlocked() && CapabilityKnown && Capability!=null && (Capability.Flags&5)==5 &&
            Capability.ValueType==Definition.ValueType && Enumerable.Range(Definition.Minimum,Definition.Maximum-Definition.Minimum+1).Any(v=>Capability.Allows(Definition,v)) && _pendingTicks==0 && DateTime.UtcNow.Ticks>=_nextActionTicks;
        public bool CanOff => CanEdit && Allows(0);
        public bool CanOn => CanEdit && Allows(1);
        public bool CanQuery => !IsTrigger && _ready() && CapabilityKnown && Capability!=null && (Capability.Flags&3)==3 && DateTime.UtcNow.Ticks>=_nextActionTicks;
        public string RangeText => CapabilityKnown && Capability!=null && (Capability.Flags&1)!=0 ?
            (HasOptions?"지원 선택값만 표시":IsBoolean?Definition.Options[0]+" / "+Definition.Options[1]:IsTrigger?"단발 실행":"입력 "+Math.Max(Definition.Minimum,Capability.Minimum)+"~"+Math.Min(Definition.Maximum,Capability.Maximum)) :
            IsNumeric?"입력 0~100 (지원 조회 필요)":"지원 조회 필요";
        public string StatusText
        {
            get
            {
                if(!_ready())return "WEB AGENT 연결 필요";
                if(!CapabilityKnown)return _message??"지원 미조회";
                if(Capability==null || (Capability.Flags&1)==0)return _message??"장비 미지원";
                if(Capability.ValueType!=Definition.ValueType)return "값 형식 불일치 · 제어 차단";
                if(_pendingTicks!=0)return "요청 전송 · 응답 대기";
                if(_message!=null)return _message;
                if(_reported.HasValue && DateTime.UtcNow.Ticks-_validTicks<=TimeSpan.FromSeconds(10).Ticks)return "장비 현재: "+Format(_reported.Value);
                if(_reported.HasValue)return "장비 현재값 오래됨 · 조회 필요";
                return (Capability.Flags&2)==0?"상태 조회 미지원 · 적용 미확인":"장비 상태 미수신";
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public CameraSpecialFeatureRow(CameraFeatureDefinition definition,byte target,Func<bool> ready,Func<bool> unlocked,
            Action<CameraSpecialFeatureRow,int> send,Action<CameraSpecialFeatureRow> query)
        {
            Definition=definition;Target=target;_ready=ready;_unlocked=unlocked;_send=send;_query=query;
            ApplyCommand=new RelayCommand(ApplyInput,()=>CanEdit);
            OffCommand=new RelayCommand(()=>Apply(0),()=>CanOff);
            OnCommand=new RelayCommand(()=>Apply(1),()=>CanOn);
            QueryCommand=new RelayCommand(()=>{if(CanQuery)_query(this);},()=>CanQuery);
        }
        public bool Allows(int value)=>Capability!=null && Capability.Allows(Definition,value);
        public string Format(int value)=>value>=0 && value<Definition.Options.Length?Definition.Options[value]:value.ToString(CultureInfo.InvariantCulture);
        /// <summary>각 세션에서 이전 장비 지원·값·진행 요청을 폐기한다.</summary>
        public void Reset()
        {
            Capability=null;CapabilityKnown=false;_reported=_requested=null;_validTicks=_pendingTicks=_nextActionTicks=0;
            _queryTicks=0;_lastDeviceTimestamp=0;_message=null;_dirty=false;Options.Clear();_selected=null;Refresh();
        }
        public void SetCapability(CameraFeatureCapability capability)
        {
            Capability=capability;CapabilityKnown=true;_message=null;_reported=null;_validTicks=0;Options.Clear();
            int? previous=_selected?.Value;
            for(int i=0;i<Definition.Options.Length;i++)if(Allows(i))Options.Add(new CameraFeatureOption(i,Definition.Options[i]));
            _selected=Options.FirstOrDefault(o=>o.Value==previous)??Options.FirstOrDefault();
            Changed(nameof(SelectedOption));Refresh();
        }
        public void QuerySent(bool sent)
        {_nextActionTicks=DateTime.UtcNow.AddMilliseconds(500).Ticks;_queryTicks=sent?DateTime.UtcNow.Ticks:0;if(!sent)_message="조회 전송 실패";else if(_pendingTicks==0)_message="상태 조회 전송 · 응답 대기";Refresh();}
        public void MarkSent(int value,bool sent)
        {
            _nextActionTicks=DateTime.UtcNow.AddSeconds(1).Ticks;_reported=null;_validTicks=0;_requested=value;
            if(!sent){_pendingTicks=0;_message="요청 전송 실패";}
            else if(IsTrigger){_pendingTicks=0;_message="실행 요청 전송 · 완료 응답 없음";}
            else if(Capability!=null && (Capability.Flags&2)!=0){_pendingTicks=DateTime.UtcNow.Ticks;_message=null;}
            else{_pendingTicks=0;_message="요청 전송 · 상태 조회 미지원 / 적용 미확인";}
            Refresh();
        }
        /// <summary>supported/valid/범위 검증을 통과한 응답만 현재값과 요청 일치 판단에 사용한다.</summary>
        public void SetState(bool supported,bool valid,int value,ulong timestamp)
        {
            _queryTicks=0;
            if(!supported){Reject(true);return;}
            if(IsTrigger)return; // 트리거 상태값은 실행 완료 ACK가 아니다.
            if(!valid || !CapabilityKnown || !AllowsState(value))
            {_reported=null;_message="장비 현재값 무효 · 적용 미확인";Refresh();return;}
            // 같은 캐시 응답/역순 응답으로 현재값 신선도나 적용 확인을 갱신하지 않는다.
            if(timestamp==0 || timestamp<=_lastDeviceTimestamp)
            {if(_pendingTicks==0)_message="이전 상태 응답 · 새 장비 상태 미확인";Refresh();return;}
            _lastDeviceTimestamp=timestamp;_reported=value;_validTicks=DateTime.UtcNow.Ticks;
            _message=_pendingTicks!=0 && _requested!=value?"장비 현재: "+Format(value)+" · 요청 불일치":null;
            _pendingTicks=0;
            if(!_dirty){_input=value.ToString(CultureInfo.InvariantCulture);_selected=Options.FirstOrDefault(o=>o.Value==value);Changed(nameof(InputText));Changed(nameof(SelectedOption));}
            Refresh();
        }
        private bool AllowsState(int value)
        {
            if(Capability==null || Capability.ValueType!=Definition.ValueType || value<Definition.Minimum || value>Definition.Maximum || value<Capability.Minimum || value>Capability.Maximum)return false;
            return Definition.ValueType!=1 || (value<32 && (Capability.OptionMask&(1u<<value))!=0);
        }
        public bool HasFreshValue(int value)=>_reported==value && DateTime.UtcNow.Ticks-_validTicks<=TimeSpan.FromSeconds(10).Ticks;
        public void Reject(bool unavailable)
        {_pendingTicks=0;_reported=null;_requested=null;_message=unavailable?"장비 미지원 · 제어 차단":"실행 불가 · 조건 확인";if(unavailable && Capability!=null)Capability.Flags=0;Refresh();}
        public void Warn(string text){_message=text;Refresh();}
        public void Apply(int value)
        {
            if(!CanEdit || !Allows(value)){Warn("지원·연결·잠금·입력값 확인 필요");return;}
            _send(this,value);
        }
        private void ApplyInput()
        {
            int value;
            if(IsNumeric){if(!int.TryParse(InputText,NumberStyles.Integer,CultureInfo.InvariantCulture,out value)){Warn("정수 입력 필요");return;}}
            else if(HasOptions){if(SelectedOption==null){Warn("지원 선택값 필요");return;}value=SelectedOption.Value;}
            else value=Definition.Minimum;
            Apply(value);
        }
        /// <summary>응답 대기 만료 및 연결/운용 잠금 변화만 알리고 불필요한 UI 갱신은 억제한다.</summary>
        public bool Refresh()
        {
            if(_pendingTicks!=0 && DateTime.UtcNow.Ticks-_pendingTicks>=TimeSpan.FromSeconds(3).Ticks)
            {_pendingTicks=0;_message="요청 전송 · 응답 없음 / 적용 미확인";}
            if(_queryTicks!=0 && DateTime.UtcNow.Ticks-_queryTicks>=TimeSpan.FromSeconds(3).Ticks)
            {_queryTicks=0;if(_pendingTicks==0)_message="상태 응답 없음 · 적용 미확인";}
            string signature=CanEdit+"/"+CanQuery+"/"+StatusText+"/"+RangeText+"/"+CanOff+"/"+CanOn;
            if(signature==_lastSignature)return false;_lastSignature=signature;
            foreach(string name in new[]{nameof(CanEdit),nameof(CanQuery),nameof(CanOff),nameof(CanOn),nameof(StatusText),nameof(RangeText)})Changed(name);
            return true;
        }
        private void Changed(string name){PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));}
    }
}
