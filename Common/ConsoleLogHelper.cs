using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace OpenCvWpfTracking.Common
{
    /// <summary>
    /// Debug Console 공통 로그 Helper.
    ///
    /// 현재 단계에서는 Serilog 같은 외부 로깅 패키지에 의존하지 않고
    /// CMD 창에서 실행 순서, Thread, 성공/실패 원인을 빠르게 확인한다.
    /// 이후 Serilog를 적용할 때 이 클래스의 내부 구현만 교체하면
    /// ViewModel과 Service의 호출부는 그대로 유지할 수 있다.
    /// </summary>
    public static class ConsoleLogHelper
    {
        private static readonly object ConsoleLock =
            new object();

        /// <summary>
        /// Console 출력 가능 여부.
        ///
        /// 프로그램 종료 또는 Console 창이 먼저 닫힌 경우
        /// 잘못된 출력 Handle에 반복 접근하지 않도록 사용한다.
        /// </summary>
        private static bool _isConsoleOutputAvailable =
            true;

        /*
         * TODO(LOGGING-NEXT):
         * Serilog 적용 시 ViewModel 호출부를 직접 수정하지 말고
         * 이 Helper 내부를 ILogger Adapter로 교체한다.
         */
        /// <summary>
        /// Console 로그 구분선.
        /// </summary>
        public const string LogLine =
            "=======================================================================================================================";

        /// <summary>
        /// 기존 코드와의 호환을 위한 구분선 출력.
        /// </summary>
        public static void PrintLine()
        {
            lock (ConsoleLock)
            {
                WriteConsoleSafe(
                    LogLine);
            }

        }

        /// <summary>
        /// 시간 / 레벨이 없는 일반 제목과 본문을 구분선 안에 출력한다.
        /// </summary>
        public static void PrintSection(
            string header,
            params string[] lines)
        {
            WriteSection(
                header ?? string.Empty,
                lines);
        }

        /// <summary>
        /// 일반 실행 흐름을 제목과 본문으로 분리하여 출력한다.
        /// </summary>
        public static void InfoSection(
            string category,
            params string[] lines)
        {
            WriteStructuredSection(
                "INFO",
                category,
                lines);
        }

        /// <summary>
        /// 장비 명령 흐름을 제목과 본문으로 분리하여 출력한다.
        /// </summary>
        public static void CommandSection(
            string category,
            params string[] lines)
        {
            WriteStructuredSection(
                "CMD ",
                category,
                lines);
        }

        /// <summary>
        /// 상태 변경을 제목과 본문으로 분리하여 출력한다.
        /// </summary>
        public static void StateSection(
            string category,
            params string[] lines)
        {
            WriteStructuredSection(
                "STATE",
                category,
                lines);
        }

        /// <summary>
        /// RTSP 주소의 사용자 이름은 유지하고 비밀번호만 마스킹한다.
        /// </summary>
        public static string MaskRtspPassword(
            string rtspAddress)
        {
            if (string.IsNullOrWhiteSpace(rtspAddress))
            {
                return string.Empty;
            }

            int schemeEnd =
                rtspAddress.IndexOf(
                    "://",
                    StringComparison.Ordinal);

            int atIndex =
                rtspAddress.IndexOf(
                    '@',
                    schemeEnd + 3);

            int passwordSeparator =
                rtspAddress.IndexOf(
                    ':',
                    schemeEnd + 3);

            if (schemeEnd < 0 ||
                atIndex < 0 ||
                passwordSeparator < 0 ||
                passwordSeparator > atIndex)
            {
                return rtspAddress;
            }

            return rtspAddress.Substring(
                       0,
                       passwordSeparator + 1) +
                   "********" +
                   rtspAddress.Substring(
                       atIndex);
        }

        /// <summary>
        /// 일반 실행 흐름 로그.
        /// </summary>
        public static void Info(
            string category,
            string message)
        {
            Write(
                "INFO",
                category,
                message);
        }

        /// <summary>
        /// 장비 명령 송신 및 사용자 제어 흐름 로그.
        /// </summary>
        public static void Command(
            string category,
            string message)
        {
            Write(
                "CMD ",
                category,
                message);
        }

        /// <summary>
        /// 상태 변경 로그.
        /// </summary>
        public static void State(
            string category,
            string message)
        {
            Write(
                "STATE",
                category,
                message);
        }

        /// <summary>
        /// 복구 가능한 경고 로그.
        /// </summary>
        public static void Warning(
            string category,
            string message)
        {
            Write(
                "WARN",
                category,
                message);
        }

        /// <summary>
        /// 예외 및 실패 로그.
        /// </summary>
        public static void Error(
            string category,
            string message,
            Exception exception = null)
        {
            string detail =
                exception == null
                    ? message
                    : $"{message} / {exception.GetType().Name}: {exception.Message}";

            Write(
                "ERROR",
                category,
                detail);
        }

        /// <summary>
        /// 작업 시작과 종료 시간을 한 묶음으로 출력한다.
        /// using 블록에서 사용하면 예외나 조기 return이 있어도 종료 로그가 남는다.
        /// </summary>
        public static IDisposable BeginScope(
            string category,
            string operation)
        {
            return new ConsoleLogScope(
                category,
                operation);
        }

        private static void Write(
            string level,
            string category,
            string message)
        {
            string safeCategory =
                string.IsNullOrWhiteSpace(category)
                    ? "GENERAL"
                    : category.Trim();

            string safeMessage =
                message ??
                string.Empty;

            lock (ConsoleLock)
            {
                WriteConsoleSafe(
                    $"[{DateTime.Now:HH:mm:ss.fff}] " +
                    $"[{level}] " +
                    $"[T{Thread.CurrentThread.ManagedThreadId:00}] " +
                    $"[{safeCategory}] {safeMessage}");
            }

        }

        private static void WriteStructuredSection(
            string level,
            string category,
            params string[] lines)
        {
            string safeCategory =
                string.IsNullOrWhiteSpace(category)
                    ? "GENERAL"
                    : category.Trim();

            string header =
                $"[{DateTime.Now:HH:mm:ss.fff}] " +
                $"[{level}] " +
                $"[T{Thread.CurrentThread.ManagedThreadId:00}] " +
                $"[{safeCategory}]";

            WriteSection(
                header,
                lines);
        }

        private static void WriteSection(
            string header,
            params string[] lines)
        {
            lock (ConsoleLock)
            {
                WriteConsoleSafe(
                    LogLine);

                WriteConsoleSafe(
                    header);

                if (lines != null)
                {
                    foreach (string line in lines)
                    {
                        WriteConsoleSafe(
                            line ?? string.Empty);
                    }

                }

                WriteConsoleSafe(
                    LogLine);

                WriteConsoleSafe();
            }

        }

        /// <summary>
        /// Console Handle 종료 상태를 고려한 안전 출력 함수.
        ///
        /// 영상 Capture, 통신 수신 등 Background Thread가
        /// 프로그램 종료 시점에 로그를 출력하더라도
        /// IOException이 작업 Thread 밖으로 전파되지 않도록 처리한다.
        /// </summary>
        private static void WriteConsoleSafe(
            string message = null)
        {
            if (!_isConsoleOutputAvailable)
            {
                return;
            }

            try
            {
                Console.WriteLine(
                    message ??
                    string.Empty);
            }
            catch (IOException)
            {
                /*
                 * Console 창 또는 표준 출력 Handle이 먼저 종료된 경우이다.
                 * 영상 및 통신 동작과 무관한 로그 출력 오류이므로
                 * 이후 Console 출력을 중지한다.
                 */
                _isConsoleOutputAvailable =
                    false;

                Debug.WriteLine(
                    message ??
                    string.Empty);
            }
            catch (ObjectDisposedException)
            {
                _isConsoleOutputAvailable =
                    false;

                Debug.WriteLine(
                    message ??
                    string.Empty);
            }
        }

        private sealed class ConsoleLogScope : IDisposable
        {
            private readonly string _category;
            private readonly string _operation;
            private readonly DateTime _startedAt;
            private bool _isDisposed;

            public ConsoleLogScope(
                string category,
                string operation)
            {
                _category = category;
                _operation = operation;
                _startedAt = DateTime.Now;

                Info(
                    _category,
                    $"START / {_operation}");
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;

                TimeSpan elapsed =
                    DateTime.Now -
                    _startedAt;

                Info(
                    _category,
                    $"END / {_operation} / ELAPSED={elapsed.TotalMilliseconds:F0}ms");
            }

        }

    }

}
