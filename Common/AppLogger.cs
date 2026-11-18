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

            var operationalFormatter = new OperationalLogFormatter();

            var technicalFormatter = new ReadableTechnicalLogFormatter();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Logger(operational => operational
                    .Filter.ByExcluding(IsDiagnosticOnly)
                    .WriteTo.Debug(formatter: operationalFormatter)
                    .WriteTo.File(
                        operationalFormatter,
                        logFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        shared: true))

                // 기술 파일은 요약 여부와 무관하게 원문 이벤트를 발생 순서대로 보존한다.
                .WriteTo.File(
                    technicalFormatter,
                    diagnosticLogFilePath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 10,
                    shared: true)

                .WriteTo.Logger(error => error
                    .MinimumLevel.Error()
                    .WriteTo.File(
                        technicalFormatter,
                        errorLogFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        shared: true))
                .CreateLogger();

            _isInitialized = true;

            Log.Information("[SYSTEM] 로그 시작 | 프로그램=TORUSS | 일반로그={NormalFile} | 오류로그={ErrorFile} | 진단로그={DiagnosticFile}\n\n일반·Debug: 짧은 운용 이력 / 오류: 코드·원인·조치 / 진단: 전체 기술 상세·반복 성능 샘플", logFilePath, errorLogFilePath, diagnosticLogFilePath);
        }

        /// <summary>속성으로 분리하여 메시지 문자열 검색·중복 렌더링 비용을 피한다.</summary>
        private static bool IsDiagnosticOnly(LogEvent logEvent)
        {
            LogEventPropertyValue value;

            return logEvent.Level < LogEventLevel.Warning && logEvent.Properties.TryGetValue("ViewerDiagnosticOnly", out value) && value is ScalarValue && Equals(((ScalarValue)value).Value, true);
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
