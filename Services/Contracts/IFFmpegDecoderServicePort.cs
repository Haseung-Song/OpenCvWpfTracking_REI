using OpenCvSharp;
using System.Threading;

namespace OpenCvWpfTracking.Services.Video
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IFFmpegDecoderServicePort
    {
        bool IsOpened { get; }

        int VideoWidth { get; }

        int VideoHeight { get; }

        bool IsAuthenticationFailure { get; }

        /// <summary>
        /// 누적 영상 처리 성능 수치를 가져온 뒤 다음 측정 구간을 위해 초기화한다.
        /// </summary>
        FFmpegDecoderService.DecoderPerformanceSnapshot CaptureAndResetPerformance();

        /// <summary>
        /// 마지막 영상 프레임의 수신·디코딩 시간 정보를 반환한다.
        /// </summary>
        FFmpegDecoderService.FrameTimingSnapshot GetLastFrameTiming();

        /// <summary>
        /// [RTSP] 연결 및 [FFmpeg] [Decoder] 초기화 처리 순서: 1. 기존 연결 정리 2. [RTSP] [TCP] /
        /// [Timeout] 옵션 생성 3. [avformat_open_input()]으로 [RTSP] 연결 4. [Stream] 정보 조회 5.
        /// [Video Stream] 탐색 6. [Codec Context] 생성 및 [Decoder] [Open] 7. [Packet] /
        /// [Frame] 버퍼 생성
        /// </summary>
        bool Open(string rtspUrl, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// [RTSP]에서 다음 영상 [Frame]을 읽어 [OpenCV] [Mat]으로 반환 처리 순서: 1. [av_read_frame()]으로
        /// [Packet] 수신 2. [Video Stream] [Packet]만 [Decode] 대상으로 사용 3.
        /// [avcodec_send_packet()] 4. [avcodec_receive_frame()] 5. [AVFrame]을 [BGR24]
        /// [Mat]으로 변환 반환 [Mat]은 호출부에서 [using] / [Dispose] 처리 필요
        /// </summary>
        Mat ReadFrame();

        /// <summary>
        /// [RTSP] 연결 해제 및 [FFmpeg] 리소스 정리 해제 순서: 1. [Packet] 2. [Frame] 3. [Codec
        /// Context] 4. [Format Context] 5. [Sws Context]
        /// </summary>
        void Close();
    }
}
