
namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IAiDetectorPacketBuilderPort
    {
        /// <summary>
        /// [AI Detector Info] 조회 요청 [Packet] 생성 요청 [CMD 01] 응답 [CMD 51]
        /// </summary>
        byte[] BuildAiDetectorInfoRequest();

        /// <summary>
        /// [RTSP] 주소 설정 요청 [Packet] 생성 요청 [CMD 02] [RTSP] 주소 목록은 [US] [Unit Separator]
        /// [0x1F] 기준으로 구분한다.
        /// </summary>
        byte[] BuildRtspAddressSetRequest(string eoRtspAddress, string irRtspAddress);

        /// <summary>
        /// [RTSP] 주소 조회 요청 [Packet] 생성 요청 [CMD 03] 응답 [CMD 52]
        /// </summary>
        byte[] BuildRtspAddressRequest();

        /// <summary>
        /// [ONNX] 파일 목록 조회 요청 [Packet] 생성 요청 [CMD 04] 응답 [CMD 53]
        /// </summary>
        byte[] BuildOnnxListRequest();

        /// <summary>
        /// [RTSP] / [ONNX] [Mapping] 설정 요청 [Packet] 생성 요청 [CMD 05] [UI]에서 입력한 [RTSP 0] /
        /// [RTSP 1]별 [ONNX Index], [Confidence], [IOU] 값을 기준으로 [Mapping] 설정 [Packet]을
        /// 생성한다. 생성 [Payload] 예: 0:1^0.10^0.45[US]1:2^0.10^0.45
        /// </summary>
        byte[] BuildRtspOnnxMappingSetRequest(int rtsp0OnnxIndex, int rtsp1OnnxIndex, double confidence, double iou);

        /// <summary>
        /// [RTSP] / [ONNX] [Mapping] 조회 요청 [Packet] 생성 요청 [CMD 06] 응답 [CMD 54]
        /// </summary>
        byte[] BuildRtspOnnxMappingRequest();

        /// <summary>
        /// [RTSP] 원본 영상 크기 조회 요청 [Packet] 생성 요청 [CMD 07] 응답 [CMD 56] Payload는 0부터 시작하는
        /// RTSP Index의 ASCII 10진 문자열이다.
        /// </summary>
        byte[] BuildRtspVideoSizeRequest(int rtspIndex);
    }
}
