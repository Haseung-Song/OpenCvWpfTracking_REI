using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace OpenCvWpfTracking.Common
{
    /// <summary>
    /// 프로그램 운용 이력을 날짜별 파일로 저장하는 Serilog 공통 Logger.
    ///
    /// 장비 연결, RTSP, AI, PTZF, 열상 제어 및 영상처리 상태를
    /// 프로그램 실행 폴더의 Logs 디렉터리에 최대 30일간 보관한다.
    /// </summary>
    internal static class AppLogger
    {
        private static bool _isInitialized;

        internal static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            string logDirectoryPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Logs");

            Directory.CreateDirectory(
                logDirectoryPath);

            string logFilePath =
                Path.Combine(
                    logDirectoryPath,
                    "rei-viewer-.log");

            Log.Logger =
                new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override(
                        "Microsoft",
                        LogEventLevel.Warning)
                    .WriteTo.Debug(
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        logFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        shared: true,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

            _isInitialized =
                true;

            Log.Information(
                "[SYSTEM] Logger Initialize Complete");
        }

        internal static void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            Log.Information(
                "[SYSTEM] Logger Shutdown");

            Log.CloseAndFlush();

            _isInitialized =
                false;
        }
    }
}
