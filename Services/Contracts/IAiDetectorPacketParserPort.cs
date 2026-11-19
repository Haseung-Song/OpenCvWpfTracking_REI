using OpenCvWpfTracking.Models.AI;
using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IAiDetectorPacketParserPort
    {
        /// <summary>
        /// [CMD 55] 탐지데이터 [Packet]을 [AiDetectionResult]로 변환한다. [CMD 55]가 아니면 [false] 반환.
        /// [Checksum]이 틀려도 [false] 반환.
        /// </summary>
        bool TryParseDetectionPacket(byte[] packet, out AiDetectionResult result);

        /// <summary>
        /// [CMD 52] [RTSP] 주소 목록 [Payload] 파싱 [Payload] 예: rtsp://... [US] rtsp://...
        /// </summary>
        List<AiRtspInfo> ParseRtspListPayload(string payload);

        /// <summary>
        /// [CMD 53] [ONNX] 목록 [Payload] 파싱 [Payload] 예: best.onnx:person,ship,car,drone
        /// [US] best_uav.onnx:uav
        /// </summary>
        List<AiOnnxInfo> ParseOnnxListPayload(string payload);

        /// <summary>
        /// [CMD 54] [RTSP] / [ONNX] [Mapping] [Payload] 파싱 [Payload] 예:
        /// 0:1^0.10^0.45[US]1:0^0.10^0.45,1^0.10^0.45
        /// </summary>
        List<AiMappingInfo> ParseMappingPayload(string payload);

        /// <summary>
        /// [CMD 56] RTSP 원본 영상 크기 Payload를 파싱한다. 형식: index[US]width[US]height index는 0~2,
        /// 크기는 양수 쌍 또는 미가용을 나타내는 0/0만 허용한다.
        /// </summary>
        bool TryParseRtspVideoSizePayload(string payload, out int rtspIndex, out int width, out int height);

        /// <summary>
        /// [AI] [Packet] 공통 구조를 외부에서 파싱한다. [CMD 55] 탐지데이터 외에도 [CMD 51] / [CMD 52] / [CMD
        /// 53] / [CMD 54] / [CMD 55] / [CMD 56] / [CMD 99] 응답 [Packet]을 구분하기 위해 사용한다.
        /// 내부적으로 [TryParsePacket()]을 호출하여 [STX] / [CMD] / [SIZE] / [Payload] / [Checksum]
        /// / [ETX] 검증을 수행한다.
        /// </summary>
        bool TryParseCommonPacket(byte[] packet, out string command, out string payload);
    }
}
