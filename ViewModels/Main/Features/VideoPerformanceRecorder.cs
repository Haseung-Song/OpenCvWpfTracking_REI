using OpenCvSharp;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Diagnostics;
using System.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>영상 계측에 필요한 누적 카운터와 시나리오만 읽는다. 연결·프레임 출력·장비 제어는 요청할 수 없다.</summary>
    internal interface IVideoPerformanceContext
    {
        ref long _focusPropertyChangedCount { get; }

        ref long _function01ReceiveCount { get; }

        ref long _panPropertyChangedCount { get; }

        ref long _panStartTxCount { get; }

        ref long _panStopTxCount { get; }

        ref long _propertyChangedCount { get; }

        ref long _ptzfStatusUpdateCount { get; }

        string _ptzPerformanceScenario { get; set; }

        ref long _tiltPropertyChangedCount { get; }

        ref long _tiltStartTxCount { get; }

        ref long _tiltStopTxCount { get; }

        ref long _zoomPropertyChangedCount { get; }

        ref long _zoomStartTxCount { get; }

        ref long _zoomStopTxCount { get; }
    }

    /// <summary>채널별 수신·출력 계측 상태와 진단 로그 집계를 보관한다. 기존 주기·계산식·로그 항목을 유지한다.</summary>
    internal sealed class VideoPerformanceRecorder
    {
        private readonly IVideoPerformanceContext _context;

        internal VideoPerformanceRecorder(IVideoPerformanceContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal readonly RtspChannelPerformance _eoRtspPerformance = new RtspChannelPerformance();

        internal readonly RtspChannelPerformance _irRtspPerformance = new RtspChannelPerformance();

        /// <summary>채널 이름에 해당하는 기존 계측 상태를 돌려준다.</summary>
        internal RtspChannelPerformance GetRtspPerformance(string streamName)
        {
            if (streamName == "EO")
            {
                return _eoRtspPerformance;
            }
            else
            {
                return _irRtspPerformance;
            }
        }

        /// <summary>표시 간격과 지연 횟수를 기존 기준으로 누적한다.</summary>
        internal static void RecordDisplayInterval(RtspChannelPerformance performance, long intervalTicks)
        {
            performance.DisplayIntervalTicks += intervalTicks;

            performance.DisplayIntervalMaxTicks = Math.Max(performance.DisplayIntervalMaxTicks, intervalTicks);

            double intervalMilliseconds = intervalTicks * 1000.0 / Stopwatch.Frequency;

            if (intervalMilliseconds >= 50.0)
            {
                performance.DisplayStall50MillisecondsCount++;
            }

            if (intervalMilliseconds >= 60.0)
            {
                performance.DisplayStall60MillisecondsCount++;
            }

            if (intervalMilliseconds >= 70.0)
            {
                performance.DisplayStall70MillisecondsCount++;
            }
        }

        /// <summary>수신 프레임의 시간 정보와 중복·정지 진단 항목을 누적한다.</summary>
        internal static void UpdateFrameTimingDiagnostics(RtspChannelPerformance performance, FFmpegDecoderService.FrameTimingSnapshot timing, Mat frame)
        {
            if (performance.LastDiagnosticArrivalTicks > 0 && timing.DecodedTicks > performance.LastDiagnosticArrivalTicks)
            {
                long intervalTicks = timing.DecodedTicks - performance.LastDiagnosticArrivalTicks;

                performance.DecodeIntervalTicks += intervalTicks;

                performance.DecodeIntervalCount++;

                performance.DecodeIntervalMaxTicks = Math.Max(performance.DecodeIntervalMaxTicks, intervalTicks);

                double intervalMilliseconds = intervalTicks * 1000.0 / Stopwatch.Frequency;

                if (intervalMilliseconds >= 70.0)
                {
                    performance.InputStall70MillisecondsCount++;
                }

                if (intervalMilliseconds >= 80.0)
                {
                    performance.InputStall80MillisecondsCount++;
                }

                if (intervalMilliseconds >= 100.0)
                {
                    performance.InputStall100MillisecondsCount++;
                }
            }

            performance.LastDiagnosticArrivalTicks = timing.DecodedTicks;
            performance.LastFramePts = timing.Pts;
            performance.LastFrameDts = timing.Dts;

            if (timing.HasTimestamp)
            {
                if (performance.HasLastTimestamp)
                {
                    double deltaMilliseconds = timing.TimestampMilliseconds - performance.LastTimestampMilliseconds;

                    if (deltaMilliseconds <= 0.0)
                    {
                        performance.TimestampNonMonotonicCount++;
                    }
                    else
                    {
                        performance.TimestampDeltaMilliseconds += deltaMilliseconds;

                        performance.TimestampDeltaCount++;

                        performance.TimestampDeltaMaximumMilliseconds = Math.Max(performance.TimestampDeltaMaximumMilliseconds, deltaMilliseconds);
                    }
                }

                performance.LastTimestampMilliseconds = timing.TimestampMilliseconds;
                performance.HasLastTimestamp = true;
            }
            else
            {
                performance.TimestampMissingCount++;
            }

            if (frame == null || frame.Empty() || frame.Channels() < 3)
            {
                return;
            }

            const int sampleGridSize = 8;

            int sampleIndex = 0;

            int differenceTotal = 0;

            for (int row = 0; row < sampleGridSize; row++)
            {
                int y = Math.Min(frame.Rows - 1, ((row * 2 + 1) * frame.Rows) / (sampleGridSize * 2));

                for (int column = 0; column < sampleGridSize; column++)
                {
                    int x = Math.Min(frame.Cols - 1, ((column * 2 + 1) * frame.Cols) / (sampleGridSize * 2));

                    Vec3b pixel = frame.At<Vec3b>(y, x);

                    byte luminance = (byte)((pixel.Item0 + pixel.Item1 + pixel.Item2) / 3);

                    if (performance.HasPreviousFrameSamples)
                    {
                        differenceTotal += Math.Abs(luminance - performance.PreviousFrameSamples[sampleIndex]);
                    }

                    performance.PreviousFrameSamples[sampleIndex] = luminance;

                    sampleIndex++;
                }
            }

            if (performance.HasPreviousFrameSamples)
            {
                double averageDifference = differenceTotal / (double)performance.PreviousFrameSamples.Length;

                performance.SampleDifferenceTotal += averageDifference;

                performance.SampleDifferenceCount++;

                if (differenceTotal == 0)
                {
                    performance.ExactDuplicateFrameCount++;

                    performance.ConsecutiveDuplicateFrameCount++;

                    performance.MaximumConsecutiveDuplicateFrameCount = Math.Max(performance.MaximumConsecutiveDuplicateFrameCount, performance.ConsecutiveDuplicateFrameCount);
                }
                else
                {
                    performance.ConsecutiveDuplicateFrameCount = 0;
                }

                if (averageDifference <= 1.0)
                {
                    performance.NearStaticFrameCount++;
                }
            }

            performance.HasPreviousFrameSamples = true;
        }

        /// <summary>기존 1초 기준으로 수신·표시·제어 계측을 로그에 요약한다.</summary>
        internal void WriteRtspPerformanceSummary(string streamName, IFFmpegDecoderServicePort decoder)
        {
            RtspChannelPerformance performance = GetRtspPerformance(streamName);

            lock (performance.Sync)
            {
                if (performance.Interval.ElapsedMilliseconds < 1000)
                {
                    return;
                }

                double seconds = Math.Max(0.001, performance.Interval.Elapsed.TotalSeconds);

                double detectionMs = performance.DetectionCount > 0 ? performance.DetectionTicks * 1000.0 / Stopwatch.Frequency / performance.DetectionCount : 0.0;

                double convertMs = performance.ConvertCount > 0 ? performance.ConvertTicks * 1000.0 / Stopwatch.Frequency / performance.ConvertCount : 0.0;

                double dispatchMs = performance.DispatchCount > 0 ? performance.DispatchTicks * 1000.0 / Stopwatch.Frequency / performance.DispatchCount : 0.0;

                double dispatchMaxMs = performance.DispatchMaxTicks * 1000.0 / Stopwatch.Frequency;

                double frameCopyMs = performance.FrameCopyCount > 0 ? performance.FrameCopyTicks * 1000.0 / Stopwatch.Frequency / performance.FrameCopyCount : 0.0;

                double frameCopyMaxMs = performance.FrameCopyMaxTicks * 1000.0 / Stopwatch.Frequency;

                double frameAgeMs = performance.FrameAgeCount > 0 ? performance.FrameAgeTicks * 1000.0 / Stopwatch.Frequency / performance.FrameAgeCount : 0.0;

                double frameAgeMaxMs = performance.FrameAgeMaxTicks * 1000.0 / Stopwatch.Frequency;

                double intervalAverageMs = performance.Displayed > 1 ? performance.DisplayIntervalTicks * 1000.0 / Stopwatch.Frequency / (performance.Displayed - 1) : 0.0;

                double intervalMaximumMs = performance.DisplayIntervalMaxTicks * 1000.0 / Stopwatch.Frequency;

                double decodeIntervalAverageMs = performance.DecodeIntervalCount > 0 ? performance.DecodeIntervalTicks * 1000.0 / Stopwatch.Frequency / performance.DecodeIntervalCount : 0.0;

                double decodeIntervalMaximumMs = performance.DecodeIntervalMaxTicks * 1000.0 / Stopwatch.Frequency;

                double timestampDeltaAverageMs = performance.TimestampDeltaCount > 0 ? performance.TimestampDeltaMilliseconds / performance.TimestampDeltaCount : 0.0;

                double sampleDifferenceAverage = performance.SampleDifferenceCount > 0 ? performance.SampleDifferenceTotal / performance.SampleDifferenceCount : 0.0;

                int gen0 = GC.CollectionCount(0);

                int gen1 = GC.CollectionCount(1);

                int gen2 = GC.CollectionCount(2);

                FFmpegDecoderService.DecoderPerformanceSnapshot d = decoder.CaptureAndResetPerformance();

                double readAvgMs = d.ReadCount > 0 ? d.ReadTicks * 1000.0 / Stopwatch.Frequency / d.ReadCount : 0.0;

                double readMaxMs = d.ReadMaxTicks * 1000.0 / Stopwatch.Frequency;

                double decodeAvgMs = d.DecodeCount > 0 ? d.DecodeTicks * 1000.0 / Stopwatch.Frequency / d.DecodeCount : 0.0;

                double decodeMaxMs = d.DecodeMaxTicks * 1000.0 / Stopwatch.Frequency;

                double scaleAvgMs = d.ScaleCount > 0 ? d.ScaleTicks * 1000.0 / Stopwatch.Frequency / d.ScaleCount : 0.0;

                double scaleMaxMs = d.ScaleMaxTicks * 1000.0 / Stopwatch.Frequency;

                long function01 = Interlocked.Read(ref _context._function01ReceiveCount);

                long ptzfStatus = Interlocked.Read(ref _context._ptzfStatusUpdateCount);

                long propertyChanged = Interlocked.Read(ref _context._propertyChangedCount);

                long panChanged = Interlocked.Read(ref _context._panPropertyChangedCount);

                long tiltChanged = Interlocked.Read(ref _context._tiltPropertyChangedCount);

                long zoomChanged = Interlocked.Read(ref _context._zoomPropertyChangedCount);

                long focusChanged = Interlocked.Read(ref _context._focusPropertyChangedCount);

                long logWrites = ConsoleLogHelper.TotalWriteCount;

                long panStartTx = Interlocked.Read(ref _context._panStartTxCount);

                long panStopTx = Interlocked.Read(ref _context._panStopTxCount);

                long tiltStartTx = Interlocked.Read(ref _context._tiltStartTxCount);

                long tiltStopTx = Interlocked.Read(ref _context._tiltStopTxCount);

                long zoomStartTx = Interlocked.Read(ref _context._zoomStartTxCount);

                long zoomStopTx = Interlocked.Read(ref _context._zoomStopTxCount);

                // 2026-09-18: Capture Thread에서 동기 파일 로그를 쓰면 다음 Frame
                // 표시 예약이 늦어진다. 문자열을 확정한 뒤 실제 I/O만 ThreadPool로 넘긴다.
                string performanceCategory = "RTSP PERF " + streamName;

                string performanceMessage = $"SCENARIO={_context._ptzPerformanceScenario} / RTSP_BITRATE_MBPS={d.PacketBytes * 8.0 / seconds / 1000000.0:F2} / PACKET_RATE={d.PacketCount / seconds:F1} / " + $"READ_FRAME_AVG_MS={readAvgMs:F1} / READ_FRAME_MAX_MS={readMaxMs:F1} / RX_FPS={performance.Received / seconds:F1} / DECODE_FPS={performance.Received / seconds:F1} / " + $"DECODE_AVG_MS={decodeAvgMs:F1} / DECODE_MAX_MS={decodeMaxMs:F1} / SWS_SCALE_AVG_MS={scaleAvgMs:F1} / SWS_SCALE_MAX_MS={scaleMaxMs:F1} / " + $"DISPLAY_FPS={performance.Displayed / seconds:F1} / DISPLAY_DROP={performance.DisplayDropped} / " + $"DISPLAY_QUEUE_REPLACE={performance.DisplayQueueReplaced} / UI_DISPATCH_WAIT_AVG_MS={dispatchMs:F1} / UI_DISPATCH_WAIT_MAX_MS={dispatchMaxMs:F1} / " + $"JITTER_BUFFER_DEPTH={performance.JitterBufferCurrentDepth} / JITTER_BUFFER_MAX={performance.JitterBufferMaximumDepth} / " + $"JITTER_BUFFER_UNDERRUN={performance.JitterBufferUnderrun} / JITTER_BUFFER_DROP={performance.JitterBufferDropped} / " + $"FRAME_COPY_AVG_MS={frameCopyMs:F1} / FRAME_COPY_MAX_MS={frameCopyMaxMs:F1} / DISPLAY_INTERVAL_AVG_MS={intervalAverageMs:F1} / " + $"DISPLAY_INTERVAL_MAX_MS={intervalMaximumMs:F1} / FRAME_AGE_AVG_MS={frameAgeMs:F1} / FRAME_AGE_MAX_MS={frameAgeMaxMs:F1} / " + $"DECODE_INTERVAL_AVG_MS={decodeIntervalAverageMs:F1} / DECODE_INTERVAL_MAX_MS={decodeIntervalMaximumMs:F1} / " + $"INPUT_STALL_70MS_COUNT={performance.InputStall70MillisecondsCount} / INPUT_STALL_80MS_COUNT={performance.InputStall80MillisecondsCount} / " + $"INPUT_STALL_100MS_COUNT={performance.InputStall100MillisecondsCount} / DISPLAY_STALL_50MS_COUNT={performance.DisplayStall50MillisecondsCount} / " + $"DISPLAY_STALL_60MS_COUNT={performance.DisplayStall60MillisecondsCount} / DISPLAY_STALL_70MS_COUNT={performance.DisplayStall70MillisecondsCount} / " + $"SOURCE_PTS={performance.LastFramePts} / SOURCE_DTS={performance.LastFrameDts} / PTS_DELTA_AVG_MS={timestampDeltaAverageMs:F2} / " + $"PTS_DELTA_MAX_MS={performance.TimestampDeltaMaximumMilliseconds:F2} / PTS_MISSING={performance.TimestampMissingCount} / " + $"PTS_NON_MONOTONIC={performance.TimestampNonMonotonicCount} / SAMPLE_DIFF_AVG={sampleDifferenceAverage:F2} / " + $"EXACT_DUPLICATE_FRAMES={performance.ExactDuplicateFrameCount} / MAX_CONSECUTIVE_DUPLICATE_FRAMES={performance.MaximumConsecutiveDuplicateFrameCount} / " + $"NEAR_STATIC_FRAMES={performance.NearStaticFrameCount} / " + $"FUNCTION_01_RX_COUNT={function01 - performance.LastFunction01Count} / PTZF_STATUS_UPDATE_COUNT={ptzfStatus - performance.LastPtzfStatusCount} / " + $"PROPERTY_CHANGED_COUNT={propertyChanged - performance.LastPropertyChangedCount} / PAN_PROPERTY_CHANGED_COUNT={panChanged - performance.LastPanPropertyChangedCount} / " + $"TILT_PROPERTY_CHANGED_COUNT={tiltChanged - performance.LastTiltPropertyChangedCount} / ZOOM_PROPERTY_CHANGED_COUNT={zoomChanged - performance.LastZoomPropertyChangedCount} / " + $"FOCUS_PROPERTY_CHANGED_COUNT={focusChanged - performance.LastFocusPropertyChangedCount} / LOG_WRITE_COUNT={logWrites - performance.LastLogWriteCount} / " + $"PAN_START_TX_COUNT={panStartTx - performance.LastPanStartTxCount} / PAN_STOP_TX_COUNT={panStopTx - performance.LastPanStopTxCount} / " + $"TILT_START_TX_COUNT={tiltStartTx - performance.LastTiltStartTxCount} / TILT_STOP_TX_COUNT={tiltStopTx - performance.LastTiltStopTxCount} / " + $"ZOOM_START_TX_COUNT={zoomStartTx - performance.LastZoomStartTxCount} / ZOOM_STOP_TX_COUNT={zoomStopTx - performance.LastZoomStopTxCount} / " + $"DETECT_MS={detectionMs:F1} / CONVERT_MS={convertMs:F1} / " + $"LAST_FRAME_MS={performance.LastFrame.ElapsedMilliseconds} / " + $"GC={gen0 - performance.LastGen0},{gen1 - performance.LastGen1},{gen2 - performance.LastGen2}";

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        ConsoleLogHelper.State(performanceCategory, performanceMessage);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[RTSP PERF LOG ERROR] " + ex.Message);
                    }
                });

                performance.Received = 0;
                performance.Displayed = 0;
                performance.DisplayDropped = 0;
                performance.DisplayQueueReplaced = 0;
                performance.JitterBufferMaximumDepth = performance.JitterBufferCurrentDepth;
                performance.JitterBufferUnderrun = 0;
                performance.JitterBufferDropped = 0;
                performance.DetectionCount = 0;
                performance.DetectionTicks = 0;
                performance.ConvertCount = 0;
                performance.ConvertTicks = 0;
                performance.DispatchCount = 0;
                performance.DispatchTicks = 0;
                performance.DispatchMaxTicks = 0;
                performance.FrameCopyCount = 0;
                performance.FrameCopyTicks = 0;
                performance.FrameCopyMaxTicks = 0;
                performance.FrameAgeCount = 0;
                performance.FrameAgeTicks = 0;
                performance.FrameAgeMaxTicks = 0;
                performance.DisplayIntervalTicks = 0;
                performance.DisplayIntervalMaxTicks = 0;
                performance.DecodeIntervalTicks = 0;
                performance.DecodeIntervalMaxTicks = 0;
                performance.DecodeIntervalCount = 0;
                performance.InputStall70MillisecondsCount = 0;
                performance.InputStall80MillisecondsCount = 0;
                performance.InputStall100MillisecondsCount = 0;
                performance.DisplayStall50MillisecondsCount = 0;
                performance.DisplayStall60MillisecondsCount = 0;
                performance.DisplayStall70MillisecondsCount = 0;
                performance.TimestampDeltaMilliseconds = 0.0;
                performance.TimestampDeltaMaximumMilliseconds = 0.0;
                performance.TimestampDeltaCount = 0;
                performance.TimestampMissingCount = 0;
                performance.TimestampNonMonotonicCount = 0;
                performance.SampleDifferenceTotal = 0.0;
                performance.SampleDifferenceCount = 0;
                performance.ExactDuplicateFrameCount = 0;
                performance.MaximumConsecutiveDuplicateFrameCount = performance.ConsecutiveDuplicateFrameCount;
                performance.NearStaticFrameCount = 0;
                performance.LastGen0 = gen0;
                performance.LastGen1 = gen1;
                performance.LastGen2 = gen2;
                performance.LastFunction01Count = function01;
                performance.LastPtzfStatusCount = ptzfStatus;
                performance.LastPropertyChangedCount = propertyChanged;
                performance.LastPanPropertyChangedCount = panChanged;
                performance.LastTiltPropertyChangedCount = tiltChanged;
                performance.LastZoomPropertyChangedCount = zoomChanged;
                performance.LastFocusPropertyChangedCount = focusChanged;
                performance.LastLogWriteCount = logWrites;
                performance.LastPanStartTxCount = panStartTx;
                performance.LastPanStopTxCount = panStopTx;
                performance.LastTiltStartTxCount = tiltStartTx;
                performance.LastTiltStopTxCount = tiltStopTx;
                performance.LastZoomStartTxCount = zoomStartTx;
                performance.LastZoomStopTxCount = zoomStopTx;

                performance.Interval.Restart();
            }
        }
    }
}
