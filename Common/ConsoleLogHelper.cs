using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Serilog;

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

        static ConsoleLogHelper()
        {
            try
            {
                if (!(Console.Out is NormalizedConsoleTextWriter))
                {
                    Console.SetOut(
                        new NormalizedConsoleTextWriter(
                            Console.Out));
                }

            }
            catch (IOException)
            {
                _isConsoleOutputAvailable = false;
            }

        }

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

        /// <summary>
        /// Write 저장 함수.
        /// </summary>
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
                string formattedMessage =
                    $"[{DateTime.Now:HH:mm:ss.fff}] " +
                    $"[{level}] " +
                    $"[T{Thread.CurrentThread.ManagedThreadId:00}] " +
                    $"[{safeCategory}] {safeMessage}";

                WriteConsoleSafe(
                    formattedMessage);

                WriteSerilog(
                    level,
                    $"[T{Thread.CurrentThread.ManagedThreadId:00}] " +
                    $"[{safeCategory}] {safeMessage}");
            }

        }

        /// <summary>
        /// WriteStructuredSection 저장 함수.
        /// </summary>
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

        /// <summary>
        /// WriteSection 저장 함수.
        /// </summary>
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

                string sectionMessage =
                    header +
                    (lines == null || lines.Length == 0
                        ? string.Empty
                        : Environment.NewLine + string.Join(
                            Environment.NewLine,
                            lines));

                Log.Information(
                    "{SectionMessage}",
                    sectionMessage);
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

        /// <summary>
        /// Console 로그 레벨을 Serilog 파일 로그 레벨로 변환한다.
        ///
        /// 기존 ViewModel과 Service의 호출부는 변경하지 않고,
        /// 이 Helper를 통과하는 운용 로그를 날짜별 파일에도 함께 남긴다.
        /// </summary>
        private static void WriteSerilog(
            string level,
            string message)
        {
            switch (level)
            {
                case "WARN":
                    Log.Warning(
                        "{LogMessage}",
                        message);
                    break;

                case "ERROR":
                    Log.Error(
                        "{LogMessage}",
                        message);
                    break;

                case "CMD ":
                case "STATE":
                case "INFO":
                default:
                    Log.Information(
                        "{LogMessage}",
                        message);
                    break;
            }

        }

        private sealed class ConsoleLogScope : IDisposable
        {
            private readonly string _category;
            private readonly string _operation;
            private readonly DateTime _startedAt;
            private bool _isDisposed;

            /// <summary>
            /// ConsoleLogScope 동작 수행 함수.
            /// </summary>
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

            /// <summary>
            /// Dispose 종료 및 자원 해제 함수.
            /// </summary>
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

        /// <summary>
        /// 기존 모듈들이 각자 블록의 끝과 다음 블록의 시작에서 PrintLine을
        /// 호출해 구분선이 두 번 연속 출력되는 경우를 한 줄로 정규화한다.
        /// 직접 Console.Write/WriteLine을 사용하는 기존 로그도 함께 처리한다.
        /// </summary>
        private sealed class NormalizedConsoleTextWriter : TextWriter
        {
            private readonly TextWriter _inner;
            private readonly object _writeLock = new object();
            private readonly StringBuilder _pendingLine = new StringBuilder();
            private bool _separatorSinceContent;
            private bool _lastLineWasBlank;

            /// <summary>
            /// NormalizedConsoleTextWriter 동작 수행 함수.
            /// </summary>
            public NormalizedConsoleTextWriter(
                TextWriter inner)
            {
                _inner = inner ?? TextWriter.Null;
            }

            public override Encoding Encoding =>
                _inner.Encoding;

            /// <summary>
            /// Write 저장 함수.
            /// </summary>
            public override void Write(
                char value)
            {
                lock (_writeLock)
                {
                    if (value == '\r')
                    {
                        return;
                    }

                    if (value == '\n')
                    {
                        FlushPendingLine();
                        return;
                    }

                    _pendingLine.Append(value);
                }

            }

            /// <summary>
            /// Write 저장 함수.
            /// </summary>
            public override void Write(
                string value)
            {
                if (value == null)
                {
                    return;
                }

                lock (_writeLock)
                {
                    foreach (char character in value)
                    {
                        if (character == '\r')
                        {
                            continue;
                        }

                        if (character == '\n')
                        {
                            FlushPendingLine();
                        }
                        else
                        {
                            _pendingLine.Append(character);
                        }

                    }

                }

            }

            /// <summary>
            /// WriteLine 저장 함수.
            /// </summary>
            public override void WriteLine(
                string value)
            {
                lock (_writeLock)
                {
                    Write(value);
                    FlushPendingLine();
                }

            }

            /// <summary>
            /// WriteLine 저장 함수.
            /// </summary>
            public override void WriteLine()
            {
                lock (_writeLock)
                {
                    FlushPendingLine();
                }

            }

            /// <summary>
            /// Flush 동작 수행 함수.
            /// </summary>
            public override void Flush()
            {
                lock (_writeLock)
                {
                    _inner.Flush();
                }

            }

            /// <summary>
            /// FlushPendingLine 동작 수행 함수.
            /// </summary>
            private void FlushPendingLine()
            {
                string line = _pendingLine.ToString();
                _pendingLine.Clear();

                string trimmed = line.Trim();
                bool isSeparator =
                    trimmed.Length >= 20 &&
                    trimmed.All(character => character == '=');

                if (isSeparator)
                {
                    if (_separatorSinceContent)
                    {
                        return;
                    }

                    _inner.WriteLine(LogLine);
                    _separatorSinceContent = true;
                    _lastLineWasBlank = false;
                    return;
                }

                if (trimmed.Length == 0)
                {
                    if (!_separatorSinceContent &&
                        !_lastLineWasBlank)
                    {
                        _inner.WriteLine();
                        _lastLineWasBlank = true;
                    }

                    return;
                }

                _inner.WriteLine(line);
                _separatorSinceContent = false;
                _lastLineWasBlank = false;
            }

        }

    }

}
