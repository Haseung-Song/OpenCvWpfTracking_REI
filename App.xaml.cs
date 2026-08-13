using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Microsoft.Win32.SafeHandles;
using FFmpeg.AutoGen;
using OpenCvWpfTracking.Common;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// [App.xaml]에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        #region [Windows API]

        /// <summary>
        /// [Windows] 콘솔 창 생성 함수
        /// 
        /// [C++] [AllocConsole()]과 동일하다.
        /// 
        /// [WPF]는 기본적으로 콘솔 프로그램이 아니므로,
        /// 별도로 콘솔 창을 생성해야 한다.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        /// <summary>
        /// 생성한 콘솔 창 해제 함수
        /// 
        /// 프로그램 종료 시 사용한다.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        /// <summary>
        /// [Windows API]
        /// 
        /// 콘솔 출력 장치([CONOUT$])를 여는 함수
        /// 
        /// [WPF]는 [Console.WriteLine()] 출력 대상이 없기 때문에,
        /// [CreateFile()]을 사용해서 실제 콘솔 출력 핸들을 가져온다.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        /// <summary>
        /// [Windows API]
        /// 
        /// 표준 출력 핸들을 새 콘솔 핸들로 변경하는 함수
        /// 
        /// 이 작업을 하지 않으면,
        /// [Console.WriteLine()]이 [Visual Studio] 출력창으로 갈 수 있다.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(
            int nStdHandle,
            SafeFileHandle handle);

        #endregion

        #region [Constants]

        /// <summary>
        /// 표준 출력([stdout]) 핸들 번호
        /// </summary>
        private const int STD_OUTPUT_HANDLE = -11;

        /// <summary>
        /// 표준 에러([stderr]) 핸들 번호
        /// </summary>
        private const int STD_ERROR_HANDLE = -12;

        /// <summary>
        /// 콘솔 쓰기 권한
        /// </summary>
        private const uint GENERIC_WRITE = 0x40000000;

        /// <summary>
        /// 콘솔 공유 쓰기 권한
        /// </summary>
        private const uint FILE_SHARE_WRITE = 0x00000002;

        /// <summary>
        /// 기존 콘솔 장치 열기
        /// </summary>
        private const uint OPEN_EXISTING = 3;

        #endregion

        #region [FFmpeg Initialize]

        /// <summary>
        /// [FFmpeg] [Native DLL] 경로 설정
        /// 
        /// [avcodec] / [avformat] / [avutil] / [swscale] [DLL]을 찾도록 지정한다.
        /// </summary>
        private void InitializeFFmpeg()
        {
            string ffmpegPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "FFmpeg");
            ffmpeg.RootPath = ffmpegPath;

            Console.WriteLine("[FFmpeg] RootPath : " + ffmpeg.RootPath);
        }

        #endregion

        #region [Application Startup]

        /// <summary>
        /// [WPF] 프로그램 시작 시 최초 실행되는 함수
        /// 
        /// 콘솔 창 생성 및 [Console.WriteLine()] 출력 연결을 수행한다.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            /// <summary>
            /// [OpenCV] [FFmpeg] 디버그 로그 출력 비활성화
            /// </summary>
            Environment.SetEnvironmentVariable("OPENCV_FFMPEG_DEBUG", "0");

            /// <summary>
            /// [OpenCV] 경고 및 정보 로그 숨김
            /// 
            /// [ERROR] 로그만 출력한다.
            /// </summary>
            Environment.SetEnvironmentVariable("OPENCV_LOG_LEVEL", "ERROR");

            base.OnStartup(e);

            /*
             * Debug / Release 환경과 관계없이 Serilog 파일 로그를 먼저 초기화한다.
             * 이후 초기화 과정에서 발생하는 FFmpeg 및 장비 연결 로그도 Logs 폴더에 남는다.
             */
            AppLogger.Initialize();

            InitializeFFmpeg();

#if DEBUG

            /// <summary>
            /// [Debug] 콘솔 창 생성
            /// </summary>
            AllocConsole();

            /// <summary>
            /// 콘솔 창 제목 설정
            /// </summary>
            Console.Title = "OpenCV WPF Debug Console";

            /// <summary>
            /// [BOM] 없는 [UTF8] 사용
            /// 
            /// 한글 출력 깨짐 방지 목적
            /// </summary>
            Console.OutputEncoding = new UTF8Encoding(false);

            /// <summary>
            /// 실제 콘솔 출력 장치([CONOUT$]) 열기
            /// 
            /// 이 핸들을 통해,
            /// [Console.WriteLine()] 출력 대상을
            /// [Visual Studio] 출력창에서 실제 콘솔창으로 변경한다.
            /// </summary>
            SafeFileHandle consoleHandle = CreateFile(
                "CONOUT$",
                GENERIC_WRITE,
                FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            /// <summary>
            /// 표준 출력([stdout]) 핸들을
            /// 새 콘솔 핸들로 변경한다.
            /// </summary>
            SetStdHandle(
                STD_OUTPUT_HANDLE,
                consoleHandle);

            /// <summary>
            /// 표준 에러([stderr]) 핸들도
            /// 같은 콘솔로 연결한다.
            /// </summary>
            SetStdHandle(
                STD_ERROR_HANDLE,
                consoleHandle);

            /// <summary>
            /// 콘솔 출력 핸들을 [C#] [Stream]으로 변환한다.
            /// </summary>
            var consoleStream = new FileStream(
                consoleHandle,
                FileAccess.Write);

            /// <summary>
            /// [UTF8] 기반 [StreamWriter] 생성
            /// 
            /// [AutoFlush] = true : [Console.WriteLine()] 호출 즉시 출력
            /// </summary>
            var consoleWriter = new StreamWriter(
                consoleStream,
                new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            /// <summary>
            /// [Console.WriteLine()] 출력 연결
            /// </summary>
            Console.SetOut(consoleWriter);

            /// <summary>
            /// [Console.Error] 출력 연결
            /// </summary>
            Console.SetError(consoleWriter);

            ConsoleLogHelper.PrintSection(
                "[CONSOLE]",
                "OpenCV WPF Debug Console Start");

#endif

        }

        #endregion

        #region [Application Exit]

        /// <summary>
        /// [WPF] 프로그램 종료 시 호출 함수
        /// 
        /// 콘솔 종료 및 리소스 정리를 수행한다.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            ConsoleLogHelper.PrintSection(
                "[CONSOLE]",
                "OpenCV WPF Debug Console End");

#if DEBUG

            /// <summary>
            /// [Debug] 콘솔 창 해제
            /// </summary>
            FreeConsole();
#endif

            AppLogger.Shutdown();

            base.OnExit(e);

        }
        #endregion
    }

}
