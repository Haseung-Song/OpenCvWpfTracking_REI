using FFmpeg.AutoGen;
using OpenCvSharp;
using OpenCvWpfTracking.Common;
using System;

namespace OpenCvWpfTracking.Services.Video
{
    /// <summary>
    /// [FFmpeg.AutoGen] 기반 [RTSP] [Decoder Service]
    ///
    /// 역할:
    /// 1. [OpenCvSharp] [VideoCapture]로 열리지 않는 [RTSP] [Stream] 직접 연결
    /// 2. [FFmpeg] [API] 기반 [Packet] 수신 / [Decode] 수행
    /// 3. [Decode]된 [AVFrame] => [OpenCV] [Mat](BGR24)으로 변환
    /// 4. [ViewModel]의 [FFmpegCaptureLoop]에서 [WPF] [Image] 출력용 [Frame] 제공
    ///
    /// 주의:
    /// - [unsafe] 코드 허용 필요
    /// - [FFmpeg] [Native DLL] 경로 설정 필요
    /// - 반환되는 [Mat]은 호출부에서 [Dispose] 필요
    /// </summary>
    public unsafe class FFmpegDecoderService : IDisposable
    {
        #region [Fields]

        /// <summary>
        /// [RTSP] / 영상 파일 입력 [Format Context]
        /// 
        /// [avformat_open_input()] 성공 시 생성되는 입력 컨텍스트
        /// </summary>
        private AVFormatContext* _formatContext;

        /// <summary>
        /// 영상 [Decode]용 [Codec Context]
        /// 
        /// [H.264] / [H.265] 등 실제 영상 [Codec] [Decode] 담당
        /// </summary>
        private AVCodecContext* _codecContext;

        /// <summary>
        /// [FFmpeg] [Packet]
        /// 
        /// [RTSP] [Stream]에서 읽어온 압축 데이터 저장용
        /// </summary>
        private AVPacket* _packet;

        /// <summary>
        /// [FFmpeg] [Frame]
        /// 
        /// [Packet] [Decode] 후 실제 영상 [Frame] 저장용
        /// </summary>
        private AVFrame* _frame;

        /// <summary>
        /// [Pixel Format] 변환 [Context]
        /// 
        /// [AVFrame]을 [WPF] 출력에 사용 가능한 [BGR24] [Mat]으로 변환할 때 사용
        /// </summary>
        private SwsContext* _swsContext;

        /// <summary>
        /// 현재 입력 [Stream] 중 [Video Stream Index]
        /// </summary>
        private int _videoStreamIndex = -1;

        /// <summary>
        /// [FFmpeg] 리소스 접근 동기화 객체
        ///
        /// [ReadFrame()]과 [Close()]가 동시에
        /// [FFmpeg] 포인터를 접근하지 못하도록 제어한다.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// 로그 출력용 영상 구분 이름
        /// 
        /// Ex.) [EO] / [IR]
        /// </summary>
        private readonly string _streamName;

        #endregion

        #region [Properties]

        /// <summary>
        /// [RTSP] 연결 및 [Decoder] 초기화 완료 여부
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        /// [RTSP] 원본 영상 너비
        /// </summary>
        public int VideoWidth { get; private set; }

        /// <summary>
        /// [RTSP] 원본 영상 높이
        /// </summary>
        public int VideoHeight { get; private set; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [FFmpeg Decoder Service]
        /// </summary>
        /// <param name="streamName">
        /// 로그 출력용 영상 이름
        /// </param>
        public FFmpegDecoderService(string streamName)
        {
            _streamName = streamName;
        }

        #endregion

        #region [Open]

        /// <summary>
        /// [RTSP] 연결 및 [FFmpeg] [Decoder] 초기화
        ///
        /// 처리 순서:
        /// 
        /// 1. 기존 연결 정리
        /// 2. [RTSP] [TCP] / [Timeout] 옵션 생성
        /// 3. [avformat_open_input()]으로 [RTSP] 연결
        /// 4. [Stream] 정보 조회
        /// 5. [Video Stream] 탐색
        /// 6. [Codec Context] 생성 및 [Decoder] [Open]
        /// 7. [Packet] / [Frame] 버퍼 생성
        /// </summary>
        /// <param name="rtspUrl">[RTSP] 주소</param>
        /// <returns>연결 및 [Decoder] 초기화 성공 여부</returns>
        public bool Open(string rtspUrl)
        {
            Close();

            /// <summary>
            /// [RTSP] 연결 시도 로그
            /// </summary>
            Console.WriteLine(
                $"[{_streamName}] [FFmpeg RTSP] Open Try...");

            /// <summary>
            /// [RTSP] 연결 대상 주소 로그
            /// </summary>
            Console.WriteLine(
                $"[{_streamName}] [FFmpeg RTSP] Source : " +
                $"{ConsoleLogHelper.MaskRtspPassword(rtspUrl)}");

            ConsoleLogHelper.PrintLine();

            ffmpeg.avformat_network_init();

            AVFormatContext* formatContext = null;

            AVDictionary* options = CreateRtspOptions();

            int result =
                ffmpeg.avformat_open_input(
                    &formatContext,
                    rtspUrl,
                    null,
                    &options);

            Console.WriteLine(
                $"[{_streamName}] [FFmpeg RTSP] avformat_open_input Result : {result}");

            ConsoleLogHelper.PrintLine();

            ffmpeg.av_dict_free(&options);

            if (result < 0)
            {
                Console.WriteLine(
                    $"[{_streamName}] [FFmpeg RTSP] avformat_open_input Failed");

                Console.WriteLine();

                return false;
            }

            _formatContext = formatContext;

            if (!LoadStreamInfo())
                return false;

            if (!FindVideoStream())
                return false;

            if (!OpenCodec())
                return false;

            AllocateDecodeBuffer();

            IsOpened = true;

            Console.WriteLine(
                $"[{_streamName}] [FFmpeg RTSP] Open Success.");

            Console.WriteLine();

            return true;
        }

        /// <summary>
        /// [RTSP] 연결 및 저지연 영상 출력을 위한
        /// [FFmpeg] 입력 옵션 생성
        ///
        /// 주요 설정:
        ///
        /// 1. [rtsp_transport = tcp]
        ///    RTSP 영상 전송 방식을 [TCP]로 고정한다.
        ///    UDP 대비 Packet 손실에 강하며,
        ///    장비 네트워크 환경에서 안정적인 영상 수신을 목적으로 사용한다.
        ///
        /// 2. [timeout / stimeout / rw_timeout]
        ///    RTSP 연결 및 데이터 읽기 제한 시간을 설정한다.
        ///    단위는 [microsecond]이며,
        ///    현재 [5000000 = 5초]로 설정한다.
        ///
        ///    해당 값은 항상 5초 동안 대기하는 시간이 아니라,
        ///    연결 또는 데이터 수신이 지연될 경우
        ///    최대 5초까지만 기다리도록 제한하는 값이다.
        ///
        ///    장비가 정상 응답하면 제한 시간과 관계없이
        ///    즉시 다음 연결 절차를 진행한다.
        ///
        /// 3. [max_delay]
        ///    RTSP Packet 수신 시 허용할 최대 지연 시간을 설정한다.
        ///    현재 [500000 = 0.5초]로 설정한다.
        ///
        ///    값을 지나치게 줄이면 네트워크 상태에 따라
        ///    Frame 손실이나 영상 끊김이 발생할 수 있다.
        ///
        /// 4. [analyzeduration]
        ///    FFmpeg가 입력 Stream 정보를 분석하는 최대 시간을 설정한다.
        ///    현재 [1000000 = 1초]로 설정한다.
        ///
        ///    Stream 분석 시간을 제한하여 초기 연결 시간을 줄이되,
        ///    Video Stream 및 Codec 정보를 탐색할 시간을 확보한다.
        ///
        /// 5. [probesize]
        ///    Stream 정보 탐색에 사용할 최대 데이터 크기를 설정한다.
        ///    현재 [65536 Byte]로 설정한다.
        ///
        ///    값을 지나치게 줄이면 Video Stream 또는 Codec 정보를
        ///    정상적으로 찾지 못할 수 있다.
        ///
        /// 6. [fflags = nobuffer]
        ///    FFmpeg 내부 입력 Buffer 사용을 최소화하여
        ///    실시간 영상 출력 지연을 줄인다.
        ///
        /// 7. [flags = low_delay]
        ///    Decoder를 낮은 지연 방식으로 동작하도록 설정한다.
        ///
        /// 주의:
        /// [timeout]은 영상 표시를 의도적으로 늦추는 설정이 아니다.
        /// 정상 RTSP Server가 즉시 응답하면 영상 연결도 바로 진행된다.
        ///
        /// 연결 전에 일정 시간 동안 [Connecting] 상태를 표시하려면
        /// [MainViewModel]에서 [OpenVideoSourcesAsync()] 호출 전에
        /// 별도의 [Task.Delay()]를 적용해야 한다.
        ///
        /// Timeout / Analyze Duration / Probe Size 값을 지나치게 작게 설정하면
        /// 장비 상태 또는 네트워크 환경에 따라
        /// [avformat_open_input] 또는 [avformat_find_stream_info]가
        /// 실패할 수 있으므로 실장비 시험 결과에 따라 조정한다.
        /// </summary>
        /// <returns>
        /// [FFmpeg] RTSP 입력 연결에 사용할 옵션 Dictionary
        /// </returns>
        private AVDictionary* CreateRtspOptions()
        {
            AVDictionary* options =
                null;

            /// <summary>
            /// [RTSP] 전송 방식을 [TCP]로 고정
            ///
            /// UDP Packet 손실보다
            /// 영상 연결 안정성을 우선한다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "rtsp_transport",
                "tcp",
                0);

            /// <summary>
            /// [RTSP] 일반 입출력 Timeout
            ///
            /// 단위:
            /// microsecond
            ///
            /// 5000000 = 5초
            ///
            /// 입력 또는 출력 처리가 지연되는 경우
            /// 최대 5초까지만 대기한다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "timeout",
                "5000000",
                0);

            /// <summary>
            /// [RTSP] Socket 연결 및 수신 Timeout
            ///
            /// 5000000 = 5초
            ///
            /// RTSP Server가 응답하지 않을 경우
            /// 최대 대기시간을 제한한다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "stimeout",
                "5000000",
                0);

            /// <summary>
            /// [RTSP] 데이터 읽기 / 쓰기 Timeout
            ///
            /// 5000000 = 5초
            ///
            /// 연결 이후 영상 데이터가 일정 시간 동안 수신되지 않으면
            /// 현재 읽기 동작을 실패 처리할 수 있도록 제한한다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "rw_timeout",
                "5000000",
                0);

            /// <summary>
            /// [RTSP] Packet 최대 지연 허용 시간
            ///
            /// 500000 = 0.5초
            ///
            /// Packet 지연 누적을 제한하여
            /// 실시간 영상 출력 지연을 줄인다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "max_delay",
                "500000",
                0);

            /// <summary>
            /// [FFmpeg] 입력 Stream 분석 최대 시간
            ///
            /// 1000000 = 1초
            ///
            /// Video Stream 및 Codec 정보를 탐색할 시간을 확보하면서
            /// 초기 연결이 과도하게 지연되지 않도록 제한한다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "analyzeduration",
                "1000000",
                0);

            /// <summary>
            /// [FFmpeg] Stream 탐색 최대 데이터 크기
            ///
            /// 65536 Byte
            ///
            /// 초기 Stream 정보 탐색에 사용할 데이터 크기를 제한한다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "probesize",
                "65536",
                0);

            /// <summary>
            /// [FFmpeg] 내부 입력 Buffer 최소화
            ///
            /// Buffer에 Frame이 과도하게 누적되어
            /// 실시간 화면이 늦게 표시되는 현상을 줄인다.
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "fflags",
                "nobuffer",
                0);

            /// <summary>
            /// [FFmpeg] 낮은 지연 Decode 모드 적용
            /// </summary>
            ffmpeg.av_dict_set(
                &options,
                "flags",
                "low_delay",
                0);

            return options;
        }

        /// <summary>
        /// 입력 [Stream] 정보 조회
        /// 
        /// [RTSP] 연결 이후 영상 / 음성 [Stream] 정보 확인 단계
        /// </summary>
        private bool LoadStreamInfo()
        {
            int result =
                ffmpeg.avformat_find_stream_info(
                    _formatContext,
                    null);

            if (result < 0)
            {
                Console.WriteLine(
                    $"[{_streamName}] [FFmpeg RTSP] avformat_find_stream_info Failed");

                Close();

                return false;
            }
            return true;
        }

        /// <summary>
        /// 입력 [Stream] 목록에서 첫 번째 [Video Stream] 탐색
        /// </summary>
        private bool FindVideoStream()
        {
            _videoStreamIndex = -1;

            for (int i = 0; i < _formatContext->nb_streams; i++)
            {
                if (_formatContext->streams[i]->codecpar->codec_type ==
                    AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    _videoStreamIndex = i;

                    break;
                }

            }

            if (_videoStreamIndex < 0)
            {
                Console.WriteLine(
                    $"[{_streamName}] [FFmpeg RTSP] Video Stream Not Found");

                Close();

                return false;
            }
            return true;
        }

        /// <summary>
        /// [Video Stream]의 [Codec] 정보를 기반으로 [Decoder] [Open]
        /// </summary>
        private bool OpenCodec()
        {
            AVCodecParameters* codecParameters =
                _formatContext->streams[_videoStreamIndex]->codecpar;

            AVCodec* codec =
                ffmpeg.avcodec_find_decoder(
                    codecParameters->codec_id);

            if (codec == null)
            {
                Console.WriteLine(
                    $"[{_streamName}] [FFmpeg RTSP] Decoder Not Found");

                Close();

                return false;
            }

            _codecContext =
                ffmpeg.avcodec_alloc_context3(codec);

            ffmpeg.avcodec_parameters_to_context(
                _codecContext,
                codecParameters);

            /// <summary>
            /// [RTSP] 원본 영상 해상도 저장
            /// 
            /// [AI Detector] [Bounding Box] 좌표 기준과
            /// [Canvas Overlay] 좌표 기준을 맞추기 위해 사용한다.
            /// </summary>
            VideoWidth = _codecContext->width;
            VideoHeight = _codecContext->height;

            /// <summary>
            /// [RTSP] 원본 영상 해상도 로그
            /// 
            /// [FFmpeg]에서 읽은 실제 영상 해상도를 출력하며,
            /// [AI Detector] [Bounding Box] 좌표 기준과
            /// [Overlay Canvas] 크기 설정 확인에 사용한다.
            /// </summary>
            ConsoleLogHelper.PrintLine();
            Console.WriteLine(
                $"[{_streamName}] [FFmpeg RTSP SIZE] {VideoWidth} x {VideoHeight}");
            Console.WriteLine();

            int result =
                ffmpeg.avcodec_open2(
                    _codecContext,
                    codec,
                    null);

            if (result < 0)
            {
                Console.WriteLine(
                    $"[{_streamName}] [FFmpeg RTSP] avcodec_open2 Failed");

                Close();

                return false;
            }

            Console.WriteLine(
                $"[{_streamName}] [FFmpeg RTSP] Codec : " +
                ffmpeg.avcodec_get_name(codecParameters->codec_id));

            return true;
        }

        /// <summary>
        /// [Decode]에 사용할 [Packet] / [Frame] 버퍼 생성
        /// </summary>
        private void AllocateDecodeBuffer()
        {
            _packet = ffmpeg.av_packet_alloc();
            _frame = ffmpeg.av_frame_alloc();
        }

        #endregion

        #region [Read Frame]

        /// <summary>
        /// [RTSP]에서 다음 영상 [Frame]을 읽어 [OpenCV] [Mat]으로 반환
        ///
        /// 처리 순서:
        /// 1. [av_read_frame()]으로 [Packet] 수신
        /// 2. [Video Stream] [Packet]만 [Decode] 대상으로 사용
        /// 3. [avcodec_send_packet()]
        /// 4. [avcodec_receive_frame()]
        /// 5. [AVFrame]을 [BGR24] [Mat]으로 변환
        ///
        /// 반환 [Mat]은 호출부에서 [using] / [Dispose] 처리 필요
        /// </summary>
        public Mat ReadFrame()
        {
            // [ReadFrame()] 중에는 [Close()] 못 들어오게
            lock (_syncLock)
            {
                if (!IsOpened ||
                    _formatContext == null ||
                    _codecContext == null ||
                    _packet == null)
                {
                    return null;
                }

                while (true)
                {
                    int result =
                        ffmpeg.av_read_frame(
                            _formatContext,
                            _packet);

                    if (result < 0)
                        return null;

                    try
                    {
                        if (_packet->stream_index != _videoStreamIndex)
                            continue;

                        result =
                            ffmpeg.avcodec_send_packet(
                                _codecContext,
                                _packet);

                        if (result < 0)
                            return null;

                        result =
                            ffmpeg.avcodec_receive_frame(
                                _codecContext,
                                _frame);

                        if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                            continue;

                        if (result < 0)
                            return null;

                        return ConvertFrameToMat(_frame);
                    }
                    finally
                    {
                        if (_packet != null)
                        {
                            ffmpeg.av_packet_unref(_packet);
                        }

                    }

                }

            }

        }

        /// <summary>
        /// [FFmpeg] [AVFrame]을 [OpenCV] [Mat]([BGR24])으로 변환
        ///
        /// 기존 [WPF] 출력 구조는 => [MatToBitmapSourceConverter]를 사용하므로,
        /// 여기서는 [WPF]가 바로 처리하기 쉬운 [BGR24] [Mat] 형태로 맞춘다.
        /// </summary>
        private Mat ConvertFrameToMat(AVFrame* sourceFrame)
        {
            int width = sourceFrame->width;
            int height = sourceFrame->height;

            Mat mat =
                new Mat(
                    height,
                    width,
                    MatType.CV_8UC3);

            _swsContext =
                ffmpeg.sws_getCachedContext(
                    _swsContext,
                    width,
                    height,
                    (AVPixelFormat)sourceFrame->format,
                    width,
                    height,
                    AVPixelFormat.AV_PIX_FMT_BGR24,
                    // [SWS_BILINEAR]
                    2,
                    null,
                    null,
                    null);

            byte_ptrArray4 dstData = default;

            int_array4 dstLineSize = default;

            dstData[0] = (byte*)mat.Data;

            dstLineSize[0] = (int)mat.Step();

            ffmpeg.sws_scale(
                _swsContext,
                sourceFrame->data,
                sourceFrame->linesize,
                0,
                height,
                dstData,
                dstLineSize);

            return mat;
        }

        #endregion

        #region [Close / Dispose]

        /// <summary>
        /// [RTSP] 연결 해제 및 [FFmpeg] 리소스 정리
        ///
        /// 해제 순서:
        /// 1. [Packet]
        /// 2. [Frame]
        /// 3. [Codec Context]
        /// 4. [Format Context]
        /// 5. [Sws Context]
        /// </summary>
        public void Close()
        {
            // [Close()] 중에는 [ReadFrame()] 못 들어오게
            lock (_syncLock)
            {
                IsOpened = false;

                FreePacket();
                FreeFrame();
                FreeCodecContext();
                FreeFormatContext();
                FreeSwsContext();

                _videoStreamIndex = -1;

                VideoWidth = 0;
                VideoHeight = 0;
            }

        }

        /// <summary>
        /// [Packet] 리소스 해제
        /// </summary>
        private void FreePacket()
        {
            if (_packet == null)
                return;

            AVPacket* packet = _packet;

            ffmpeg.av_packet_free(&packet);

            _packet = null;
        }

        /// <summary>
        /// [Frame] 리소스 해제
        /// </summary>
        private void FreeFrame()
        {
            if (_frame == null)
                return;

            AVFrame* frame = _frame;

            ffmpeg.av_frame_free(&frame);

            _frame = null;
        }

        /// <summary>
        /// [Codec Context] 리소스 해제
        /// </summary>
        private void FreeCodecContext()
        {
            if (_codecContext == null)
                return;

            AVCodecContext* codecContext = _codecContext;

            ffmpeg.avcodec_free_context(&codecContext);

            _codecContext = null;
        }

        /// <summary>
        /// [Format Context] 리소스 해제
        /// </summary>
        private void FreeFormatContext()
        {
            if (_formatContext == null)
                return;

            AVFormatContext* formatContext = _formatContext;

            ffmpeg.avformat_close_input(&formatContext);

            _formatContext = null;
        }

        /// <summary>
        /// [Pixel Format] 변환 [Context] 해제
        /// </summary>
        private void FreeSwsContext()
        {
            if (_swsContext == null)
                return;

            ffmpeg.sws_freeContext(_swsContext);

            _swsContext = null;
        }

        /// <summary>
        /// 외부 [using] / [Dispose] 호출 시 내부 [FFmpeg] 리소스 정리
        /// </summary>
        public void Dispose()
        {
            Close();
        }
        #endregion
    }

}
