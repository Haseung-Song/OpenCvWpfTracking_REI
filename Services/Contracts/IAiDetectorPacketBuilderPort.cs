
namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IAiDetectorPacketBuilderPort
    {
        byte[] BuildAiDetectorInfoRequest();

        byte[] BuildRtspAddressSetRequest(string eoRtspAddress, string irRtspAddress);

        byte[] BuildRtspAddressRequest();

        byte[] BuildOnnxListRequest();

        byte[] BuildRtspOnnxMappingSetRequest(int rtsp0OnnxIndex, int rtsp1OnnxIndex, double confidence, double iou);

        byte[] BuildRtspOnnxMappingRequest();

        byte[] BuildRtspVideoSizeRequest(int rtspIndex);
    }
}
