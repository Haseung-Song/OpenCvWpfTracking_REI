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

        /// <summary>
        /// Initialize 초기화 함수.
        /// </summary>
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

            string errorLogFilePath = Path.Combine(
                logDirectoryPath,
                "rei-viewer-errors-.log");

            Log.Logger =
                new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override(
                        "Microsoft",
                        LogEventLevel.Warning)
                    .WriteTo.Debug(
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}")
                    .WriteTo.File(
                        logFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        shared: true,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}")
                    // 2026-09-18: 운용자가 장애만 즉시 찾을 수 있는 별도 오류 요약 파일.
                    .WriteTo.Logger(errorLogger => errorLogger
                        .MinimumLevel.Error()
                        .WriteTo.File(
                            errorLogFilePath,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            shared: true,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}"))
                    .CreateLogger();

            _isInitialized =
                true;

            Log.Information(
                "[SYSTEM] 로그 시작 | APP=REI | ERROR_FILE={ErrorFile} | 오류 형식=코드/기능/원인/조치",
                errorLogFilePath);
        }

        /// <summary>
        /// Shutdown 동작 수행 함수.
        /// </summary>
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
