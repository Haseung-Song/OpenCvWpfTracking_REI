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

        FFmpegDecoderService.DecoderPerformanceSnapshot CaptureAndResetPerformance();

        FFmpegDecoderService.FrameTimingSnapshot GetLastFrameTiming();

        bool Open(string rtspUrl, CancellationToken cancellationToken = default(CancellationToken));

        Mat ReadFrame();

        void Close();
    }
}
