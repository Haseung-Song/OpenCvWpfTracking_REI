using OpenCvSharp;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// EO/IR RTSP 연결, 재연결, 프레임 수신과 화면 갱신 흐름을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        internal sealed class DetectionFrameSlot
        {
            public readonly object Sync = new object();

            public Mat LatestFrame;

            public OpenCvWpfTracking.Services.Position.ZoneFrameContext LatestZoneFrame;

            public OpenCvWpfTracking.Services.Position.ZoneFrameContext ResultZoneFrame;

            public Mat ReusableFrame;

            public int WorkerRunning;

            public long LastQueuedTicks;

            public ThermalFireDetectionResult ThermalResult;

            public SmokeDetectionResult SmokeResult;

            public long ResultVersion;

            public int SettingsGeneration;
        }

        internal sealed class DisplayFrameSlot
        {
            public readonly object Sync = new object();

            public Mat LatestFrame;

            public Mat ReusableFrame;

            public int WorkerRunning;

            public readonly Queue<BufferedDisplayFrame> BufferedFrames = new Queue<BufferedDisplayFrame>();

            public readonly Queue<Mat> BufferedReusableFrames = new Queue<Mat>();

            public int BufferedWorkerRunning;

            public Task BufferedWorkerTask = Task.CompletedTask;

            public WriteableBitmap Bitmap;

            public long LatestFrameDecodedTicks;

            public int OverlayDispatchPending;

            public long LastOverlayVersion;
        }

        internal sealed class BufferedDisplayFrame
        {
            public Mat Frame;

            public long DecodedTicks;
        }

        internal sealed class RtspChannelPerformance
        {
            public readonly object Sync = new object();

            public readonly Stopwatch Interval = Stopwatch.StartNew();

            public readonly Stopwatch LastFrame = Stopwatch.StartNew();

            public int Received;

            public int Displayed;

            public int DisplayDropped;

            public int DisplayQueueReplaced;

            public int DetectionCount;

            public long DetectionTicks;

            public int ConvertCount;

            public long ConvertTicks;

            public int DispatchCount;

            public long DispatchTicks;

            public long DispatchMaxTicks;

            public int FrameCopyCount;

            public long FrameCopyTicks;

            public long FrameCopyMaxTicks;

            public int FrameAgeCount;

            public long FrameAgeTicks;

            public long FrameAgeMaxTicks;

            public long DisplayIntervalTicks;

            public long DisplayIntervalMaxTicks;

            public long LastDisplayTicks;

            public int LastGen0 = GC.CollectionCount(0);

            public int LastGen1 = GC.CollectionCount(1);

            public int LastGen2 = GC.CollectionCount(2);

            public long LastFunction01Count;

            public long LastPtzfStatusCount;

            public long LastPropertyChangedCount;

            public long LastPanPropertyChangedCount;

            public long LastTiltPropertyChangedCount;

            public long LastZoomPropertyChangedCount;

            public long LastFocusPropertyChangedCount;

            public long LastLogWriteCount;

            public long LastPanStartTxCount;

            public long LastPanStopTxCount;

            public long LastTiltStartTxCount;

            public long LastTiltStopTxCount;

            public long LastZoomStartTxCount;

            public long LastZoomStopTxCount;

            public int JitterBufferCurrentDepth;

            public int JitterBufferMaximumDepth;

            public int JitterBufferUnderrun;

            public int JitterBufferDropped;

            public long LastDiagnosticArrivalTicks;

            public long DecodeIntervalTicks;

            public long DecodeIntervalMaxTicks;

            public int DecodeIntervalCount;

            public double LastTimestampMilliseconds;

            public bool HasLastTimestamp;

            public double TimestampDeltaMilliseconds;

            public double TimestampDeltaMaximumMilliseconds;

            public int TimestampDeltaCount;

            public int TimestampMissingCount;

            public int TimestampNonMonotonicCount;

            public readonly byte[] PreviousFrameSamples = new byte[64];

            public bool HasPreviousFrameSamples;

            public double SampleDifferenceTotal;

            public int SampleDifferenceCount;

            public int ExactDuplicateFrameCount;

            public int ConsecutiveDuplicateFrameCount;

            public int MaximumConsecutiveDuplicateFrameCount;

            public int NearStaticFrameCount;

            public int InputStall70MillisecondsCount;

            public int InputStall80MillisecondsCount;

            public int InputStall100MillisecondsCount;

            public int DisplayStall50MillisecondsCount;

            public int DisplayStall60MillisecondsCount;

            public int DisplayStall70MillisecondsCount;

            public long LastFramePts;

            public long LastFrameDts;
        }

        internal static readonly long DetectionMinimumIntervalTicks = Stopwatch.Frequency / 10;

        internal static readonly int EoJitterDisplayFramesPerSecond = ReadBoundedEnvironmentInteger("TORUSS_EO_JITTER_FPS", 30, 20, 60);

        internal static readonly int EoJitterBufferCapacity = ReadBoundedEnvironmentInteger("TORUSS_EO_JITTER_FRAMES", 4, 2, 6);

        internal static readonly long EoJitterDisplayIntervalTicks = Math.Max(1L, Stopwatch.Frequency / EoJitterDisplayFramesPerSecond);

        internal static readonly int EoDisplayMaximumWidth = ReadBoundedEnvironmentInteger("TORUSS_EO_DISPLAY_MAX_WIDTH", 1280, 640, 1920);

        internal static readonly bool UseLegacyImmediateRtspDisplay = string.Equals(Environment.GetEnvironmentVariable("TORUSS_RTSP_DISPLAY"), "LEGACY", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 장비 연결 버튼의 진입점이다.
        ///
        /// 중복 연결·해제 요청을 확인하고 연결 생명주기 잠금 안에서
        /// 제어 TCP와 EO/IR RTSP 연결을 시작한다.
        /// 지연될 수 있는 영상 열기는 내부 비동기 연결 경로에서 처리한다.
        /// </summary>
        public void Connect()
        {
            _videoFeature.Connect();
        }

        /// <summary>
        /// 영상 연결 해제 함수
        ///
        /// 1. [CaptureLoop] 종료 요청
        /// 2. [VD__VideoCapture] 해제
        /// 3. [FFmpeg] [EO / IR] [RTSP] Decoder 해제
        /// 4. [상태 문자열 갱신]
        /// </summary>
        public void Disconnect()
        {
            _videoFeature.Disconnect();
        }

        internal static int ReadBoundedEnvironmentInteger(string variableName, int defaultValue, int minimum, int maximum)
        {
            return VideoFeature.ReadBoundedEnvironmentInteger(variableName, defaultValue, minimum, maximum);
        }

        internal struct VideoConnectResult
        {
            public bool EoResult;

            public bool IrResult;
        }
    }
}
