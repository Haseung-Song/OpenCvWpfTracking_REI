using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface ILAPacketParserPort
    {
        /// <summary>
        /// 수신 byte[] 데이터를 내부 Buffer에 누적하고, 완성된 Legacy 또는 가변 길이 Packet만 반환한다.
        /// </summary>
        List<LaResponsePacket> Parse(byte[] receivedData);

        /// <summary>
        /// 연결 해제 또는 재연결 시 이전 연결에서 남은 분할 Packet 데이터를 제거한다.
        /// </summary>
        void Reset();
    }
}
