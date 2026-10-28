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

            string logDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectoryPath);
            string logFilePath = Path.Combine(logDirectoryPath, "rei-viewer-.log");
            string errorLogFilePath = Path.Combine(logDirectoryPath, "rei-viewer-errors-.log");
            string diagnosticLogFilePath = Path.Combine(logDirectoryPath, "rei-viewer-diagnostics-.log");
            const string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Debug(outputTemplate: template)
                .WriteTo.Logger(operational => operational
                    .Filter.ByExcluding(IsDiagnosticOnly)
                    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30, shared: true, outputTemplate: template))
                // 반복 성능 샘플은 삭제하지 않고 크기가 제한된 별도 진단 파일에 보존한다.
                .WriteTo.Logger(diagnostic => diagnostic
                    .Filter.ByIncludingOnly(IsDiagnosticOnly)
                    .WriteTo.File(diagnosticLogFilePath, rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 10 * 1024 * 1024, rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 10, shared: true, outputTemplate: template))
                .WriteTo.Logger(error => error.MinimumLevel.Error()
                    .WriteTo.File(errorLogFilePath, rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30, shared: true, outputTemplate: template))
                .CreateLogger();
            _isInitialized = true;
            Log.Information("[SYSTEM] 로그 시작 | 프로그램=TORUSS | 일반로그={NormalFile} | 오류로그={ErrorFile} | 진단로그={DiagnosticFile}\n\n일반: 한국어 운용 요약 다음 기술 상세 / 오류: 기존 코드·원인·조치 / 진단: 반복 성능 샘플", logFilePath, errorLogFilePath, diagnosticLogFilePath);
        }

        /// <summary>속성으로 분리하여 메시지 문자열 검색·중복 렌더링 비용을 피한다.</summary>
        private static bool IsDiagnosticOnly(LogEvent logEvent)
        {
            LogEventPropertyValue value;
            return logEvent.Level < LogEventLevel.Warning &&
                logEvent.Properties.TryGetValue("ViewerDiagnosticOnly", out value) &&
                value is ScalarValue && Equals(((ScalarValue)value).Value, true);
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

            Log.Information("[SYSTEM] Logger Shutdown");
            Log.CloseAndFlush();
            _isInitialized = false;
        }
    }
}
