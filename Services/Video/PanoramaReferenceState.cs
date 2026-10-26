using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OpenCvWpfTracking.Services.Video
{
    /// <summary>영점 변경과 프리셋 저장 사이에 종료되어도 좌표 보정을 다시 적용할 수 있는 기록이다.</summary>
    [DataContract]
    public sealed class PanoramaPresetRebase
    {
        [DataMember]
        public int Number { get; set; }

        [DataMember]
        public string BeforeKey { get; set; }

        [DataMember]
        public string AfterKey { get; set; }

        [DataMember]
        public double Pan { get; set; }

        [DataMember]
        public double Tilt { get; set; }
    }

    /// <summary>물리 방향과 현재 팬·틸트 좌표 사이의 누적 변환을 장비별로 보존한다.</summary>
    [DataContract]
    public sealed class PanoramaReferenceState
    {
        [DataMember]
        public int Version { get; set; } = 1;

        [DataMember]
        public double PanOffset { get; set; }

        [DataMember]
        public double TiltOffset { get; set; }

        [DataMember]
        public bool Pending { get; set; }

        [DataMember]
        public List<PanoramaPresetRebase> Journal { get; set; } = new List<PanoramaPresetRebase>();
    }
}
