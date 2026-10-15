using System;
using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>2026-10-14 V31_5: v1.8 공통 Feature ID 정의. 제조사 원시 명령은 WebAgent가 변환한다.</summary>
    public sealed class CameraFeatureDefinition
    {
        public byte Id { get; private set; }
        public string Name { get; private set; }
        public byte ValueType { get; private set; }
        public int Minimum { get; private set; }
        public int Maximum { get; private set; }
        public string[] Options { get; private set; }
        public int Targets { get; private set; }
        private CameraFeatureDefinition(byte id, string name, byte type, int min, int max, int targets, params string[] options)
        { Id=id;Name=name;ValueType=type;Minimum=min;Maximum=max;Targets=targets;Options=options; }

        // Targets: bit0=EO, bit1=IR. 코드표의 대상과 값 범위를 그대로 사용한다.
        public static readonly CameraFeatureDefinition[] All = {
            new CameraFeatureDefinition(0x01,"Defog",0,0,1,1,"OFF","ON"),
            new CameraFeatureDefinition(0x02,"영상 세부 보정",2,0,100,3),
            new CameraFeatureDefinition(0x03,"영상 안정화",1,0,3,3,"OFF","EIS / 기본","OIS","EIS + OIS"),
            new CameraFeatureDefinition(0x04,"NUC / FFC",3,0,4,2,"기본","Internal","External","Blurred","Combo"),
            new CameraFeatureDefinition(0x05,"팔레트",1,0,5,2,"White Hot","Black Hot","Iron","Rainbow","Rainbow HC","Fusion"),
            new CameraFeatureDefinition(0x06,"극성",1,0,1,2,"White Hot","Black Hot"),
            new CameraFeatureDefinition(0x07,"영상 정지",0,0,1,3,"Live","Freeze"),
            new CameraFeatureDefinition(0x08,"좌우 반전",0,0,1,3,"OFF","ON"),
            new CameraFeatureDefinition(0x09,"상하 반전",0,0,1,3,"OFF","ON"),
            new CameraFeatureDefinition(0x0A,"노이즈 제거",2,0,100,3),
            new CameraFeatureDefinition(0x0B,"AGC 모드",1,0,4,2,"Auto","Linear","Manual","Wide","Histogram"),
            new CameraFeatureDefinition(0x0C,"밝기",2,0,100,3),
            new CameraFeatureDefinition(0x0D,"대비",2,0,100,3),
            new CameraFeatureDefinition(0x0E,"선명도",2,0,100,3),
            new CameraFeatureDefinition(0x0F,"Defog 단계",1,0,2,1,"Low","Middle","High"),
            new CameraFeatureDefinition(0x10,"역광 보정",1,0,3,1,"OFF","BLC","HLC","WDR"),
            new CameraFeatureDefinition(0x11,"역광 단계",2,0,100,1),
            new CameraFeatureDefinition(0x12,"주야간 모드",1,0,2,1,"Auto","Day","Night"),
            new CameraFeatureDefinition(0x13,"화이트 밸런스",1,0,5,1,"Auto","ATW","Indoor","Outdoor","Manual","One Push"),
            new CameraFeatureDefinition(0x14,"One Push WB",3,1,1,1),
            new CameraFeatureDefinition(0x15,"Heat Haze 저감",0,0,1,3,"OFF","ON"),
            new CameraFeatureDefinition(0x16,"난류 저감 강도",2,0,100,2),
            new CameraFeatureDefinition(0x17,"노출 모드",1,0,1,1,"Auto","Manual"),
            new CameraFeatureDefinition(0x18,"셔터",2,0,100,1),
            new CameraFeatureDefinition(0x19,"조리개",2,0,100,1),
            new CameraFeatureDefinition(0x1A,"채도",2,0,100,1),
            new CameraFeatureDefinition(0x1B,"색조",2,0,100,1),
            new CameraFeatureDefinition(0x1C,"감마",2,0,100,1),
            new CameraFeatureDefinition(0x1D,"디지털 줌",0,0,1,3,"OFF","ON"),
            new CameraFeatureDefinition(0x1E,"자동 영상 조정",3,1,1,2)
        };
    }

    public sealed class CameraFeatureCapability
    {
        public byte Id, Flags, ValueType;
        public short Minimum, Maximum;
        public uint OptionMask;
        /// <summary>정의 범위·장비 범위·열거형 지원 마스크의 교집합만 송신 가능하다.</summary>
        public bool Allows(CameraFeatureDefinition definition, int value)
        {
            if ((Flags & 5)!=5 || ValueType!=definition.ValueType || Minimum>Maximum ||
                value<Minimum || value>Maximum || value<definition.Minimum || value>definition.Maximum || value<0 || value>255) return false;
            if (ValueType==1 || definition.Id==0x04)
                return value<32 && (OptionMask & (1u<<value))!=0;
            return true;
        }
    }

    /// <summary>수신 payload의 스키마·길이·예약 필드·중복 ID를 검증하는 부작용 없는 파서.</summary>
    public static class CameraFeatureProtocol
    {
        public static bool TryCapability(byte[] p, out byte target, out List<CameraFeatureCapability> entries)
        {
            target=0;entries=null;
            if(p==null || p.Length<13 || p[0]!=2 || p[1]>1 || p.Length<13+12*p[12])return false;
            var result=new List<CameraFeatureCapability>();var ids=new HashSet<byte>();
            for(int o=13;o<13+12*p[12];o+=12)
            {
                if(!ids.Add(p[o]))return false;
                // 미래 확장/알 수 없는 타입은 지원으로 추정하지 않는다.
                result.Add(new CameraFeatureCapability {Id=p[o],Flags=(byte)(p[o+3]==0 && (p[o+1]&0xE0)==0?p[o+1]:0),
                    ValueType=p[o+2],Minimum=BitConverter.ToInt16(p,o+4),Maximum=BitConverter.ToInt16(p,o+6),OptionMask=BitConverter.ToUInt32(p,o+8)});
            }
            target=p[1];entries=result;return true;
        }
        public static bool TryState(byte[] p, out byte target, out byte id, out bool supported, out bool valid, out short value)
        {
            target=id=0;supported=valid=false;value=0;
            if(p==null || p.Length<18 || p[0]!=1 || p[1]>1 || (p[3]&0xFC)!=0)return false;
            target=p[1];id=p[2];supported=(p[3]&1)!=0;
            value=BitConverter.ToInt16(p,4);int min=BitConverter.ToInt16(p,6),max=BitConverter.ToInt16(p,8);
            valid=supported && (p[3]&2)!=0 && min<=max && value>=min && value<=max;
            return true;
        }
    }
}
