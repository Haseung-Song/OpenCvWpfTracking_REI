using OpenCvWpfTracking.Models.AI;
using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IAiDetectorPacketParserPort
    {
        bool TryParseDetectionPacket(byte[] packet, out AiDetectionResult result);

        List<AiRtspInfo> ParseRtspListPayload(string payload);

        List<AiOnnxInfo> ParseOnnxListPayload(string payload);

        List<AiMappingInfo> ParseMappingPayload(string payload);

        bool TryParseRtspVideoSizePayload(string payload, out int rtspIndex, out int width, out int height);

        bool TryParseCommonPacket(byte[] packet, out string command, out string payload);
    }
}
