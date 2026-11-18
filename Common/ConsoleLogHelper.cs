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
    /// Console과 Serilog에 실행 흐름 및 성공/실패 정보를 전달한다.
    /// 출력 실패가 장비 제어를 중단하지 않도록 기존 안전 출력 경로를 유지한다.
    /// </summary>
    public static class ConsoleLogHelper
    {
        private static readonly object ConsoleLock = new object();

        // 로그 경계 문자는 불변 테이블을 재사용해 로그마다 새 배열을 만들지 않는다.
        private static readonly char[] RtspAuthorityBoundaries =
        {
            '/',
            '?',
            '#'
        };

        // 2026-11-12: 로그마다 정규식·성능 항목 배열을 생성하지 않고 불변 자료를 재사용한다.
        private static readonly System.Text.RegularExpressions.Regex RtspAddressInLog =
            new System.Text.RegularExpressions.Regex(@"rtsp://[^\s""'<>]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly string[] PerformanceMetricKeys =
        {
            "RTSP_BITRATE_MBPS", "PACKET_RATE", "DECODE_FPS", "LAST_FRAME_MS"
        };

        private static readonly string[] PerformanceMetricLabels =
        {
            "비트레이트(Mbps)", "수신률", "디코딩FPS", "최근프레임지연(ms)"
        };

        private static readonly System.Text.RegularExpressions.Regex[] PerformanceMetricPatterns =
            PerformanceMetricKeys.Select(key => new System.Text.RegularExpressions.Regex(
                @"(?:^| / |\r?\n\s*)" + key + @"=([^/\r\n]+)")).ToArray();

        /// <summary>
        /// Console 출력 가능 여부.
        ///
        /// 프로그램 종료 또는 Console 창이 먼저 닫힌 경우
        /// 잘못된 출력 Handle에 반복 접근하지 않도록 사용한다.
        /// </summary>
        private static bool _isConsoleOutputAvailable = true;

        // 공통 Helper가 쓴 콘솔 줄을 기존 Console.WriteLine 경로로 다시 저장하지 않는다.
        [ThreadStatic]
        private static bool _writingStructuredConsole;

        private static long _totalWriteCount;

        public static long TotalWriteCount
        {
            get
            {
                return Interlocked.Read(ref _totalWriteCount);
            }
        }

        /// <summary>
        /// Console 로그 구분선.
        /// </summary>
        public const string LogLine = "=======================================================================================================================";

        static ConsoleLogHelper()
        {
            try
            {
                if (!(Console.Out is NormalizedConsoleTextWriter))
                {
                    Console.SetOut(new NormalizedConsoleTextWriter(Console.Out));
                }
            }
            catch (IOException)
            {
                _isConsoleOutputAvailable = false;
            }
        }

        /// <summary>
        /// 콘솔 장치를 새로 열어도 직접 출력과 공통 로그가 같은 정규화 경로를 사용한다.
        /// </summary>
        public static void AttachConsoleWriter(TextWriter writer)
        {
            lock (ConsoleLock)
            {
                Console.SetOut(new NormalizedConsoleTextWriter(writer ?? TextWriter.Null));

                _isConsoleOutputAvailable = true;
            }
        }

        /// <summary>
        /// 기존 코드와의 호환을 위한 구분선 출력.
        /// </summary>
        public static void PrintLine()
        {
            lock (ConsoleLock)
            {
                WriteConsoleSafe();
            }
        }

        /// <summary>
        /// 시간 / 레벨이 없는 일반 제목과 본문을 구분선 안에 출력한다.
        /// </summary>
        public static void PrintSection(string header, params string[] lines)
        {
            WriteSection(header ?? string.Empty, lines);
        }

        /// <summary>
        /// 일반 실행 흐름을 제목과 본문으로 분리하여 출력한다.
        /// </summary>
        public static void InfoSection(string category, params string[] lines)
        {
            WriteStructuredSection("INFO", category, lines);
        }

        /// <summary>
        /// 장비 명령 흐름을 제목과 본문으로 분리하여 출력한다.
        /// </summary>
        public static void CommandSection(string category, params string[] lines)
        {
            WriteStructuredSection("CMD ", category, lines);
        }

        /// <summary>
        /// 상태 변경을 제목과 본문으로 분리하여 출력한다.
        /// </summary>
        public static void StateSection(string category, params string[] lines)
        {
            WriteStructuredSection("STATE", category, lines);
        }

        /// <summary>
        /// RTSP 주소의 사용자 이름과 비밀번호를 모두 마스킹한다.
        /// </summary>
        public static string MaskRtspPassword(string rtspAddress)
        {
            if (string.IsNullOrWhiteSpace(rtspAddress))
            {
                return string.Empty;
            }

            rtspAddress = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(rtspAddress);

            int scheme = rtspAddress.IndexOf("://", StringComparison.Ordinal);

            if (scheme < 0)
            {
                return rtspAddress;
            }

            int authority = scheme + 3, end = rtspAddress.IndexOfAny(RtspAuthorityBoundaries, authority);

            if (end < 0)
            {
                end = rtspAddress.Length;
            }

            int at = rtspAddress.LastIndexOf('@', end - 1, end - authority);

            if (at < authority)
            {
                return rtspAddress;
            }
            else
            {
                return rtspAddress.Substring(0, authority) + "**:**" + rtspAddress.Substring(at);
            }
        }

        /// <summary>로그 본문에 포함된 RTSP 주소의 인증 정보를 마스킹한다.</summary>
        public static string MaskRtspInLog(string message)
        {
            string text = message ?? string.Empty;

            // 주소가 없는 일반 로그는 원문을 그대로 반환해 불필요한 정규식 검색을 생략한다.
            if (text.IndexOf("rtsp://", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return text;
            }

            return RtspAddressInLog.Replace(text, match => MaskRtspPassword(match.Value));
        }

        /// <summary>
        /// 일반 실행 흐름 로그.
        /// </summary>
        public static void Info(string category, string message)
        {
            Write("INFO", category, message);
        }

        /// <summary>
        /// 장비 명령 송신 및 사용자 제어 흐름 로그.
        /// </summary>
        public static void Command(string category, string message)
        {
            Write("CMD ", category, message);
        }

        /// <summary>
        /// 상태 변경 로그.
        /// </summary>
        public static void State(string category, string message)
        {
            Write("STATE", category, message);
        }

        /// <summary>
        /// 복구 가능한 경고 로그.
        /// </summary>
        public static void Warning(string category, string message)
        {
            Write("WARN", category, "[운영 경고] " + GetFriendlyFeatureName(category) + " | 내용=" + (message ?? string.Empty));
        }

        /// <summary>
        /// 예외 및 실패 로그.
        /// </summary>
        public static void Error(string category, string message, Exception exception = null)
        {
            string code = GetOperationalErrorCode(category);

            string feature = GetFriendlyFeatureName(category);

            string cause = GetFriendlyCause(message, exception);

            string action = GetRecommendedAction(category);

            string detail = $"[장애 요약] 코드={code} | 기능={feature} | 원인={cause} | 조치={action}" + Environment.NewLine + "    [기술 상세] " + (message ?? string.Empty) + (exception == null ? string.Empty : Environment.NewLine + "    [예외] " + exception);

            Write("ERROR", category, detail);
        }

        private static string GetOperationalErrorCode(string category)
        {
            string value = (category ?? string.Empty).ToUpperInvariant();

            if (value.Contains("RTSP") || value.Contains("VIDEO") || value.Contains("DISPLAY"))
            {
                return "E-VIDEO-001";
            }

            if (value.Contains("DEVICE") || value.Contains("TCP") || value.Contains("AGENT") || value.Contains("CONNECT"))
            {
                return "E-COMM-001";
            }

            if (value.Contains("ZOOM") || value.Contains("FOCUS") || value.Contains("PAN") || value.Contains("TILT") || value.Contains("SYNC") || value.Contains("MOVE"))
            {
                return "E-PTZF-001";
            }

            if (value.Contains("AI") || value.Contains("DETECTION") || value.Contains("FIRE") || value.Contains("SMOKE"))
            {
                return "E-DETECT-001";
            }

            if (value.Contains("PANORAMA"))
            {
                return "E-PANO-001";
            }

            if (value.Contains("CSV") || value.Contains("PRESET") || value.Contains("STORAGE"))
            {
                return "E-DATA-001";
            }

            return "E-SYSTEM-001";
        }

        private static string GetFriendlyFeatureName(string category)
        {
            string value = (category ?? string.Empty).ToUpperInvariant();

            if (value.Contains("RTSP") || value.Contains("VIDEO"))
            {
                return "실시간 영상";
            }

            if (value.Contains("DISPLAY"))
            {
                return "화면 표시";
            }

            if (value.Contains("DEVICE") || value.Contains("TCP") || value.Contains("AGENT") || value.Contains("CONNECT"))
            {
                return "장비 통신";
            }

            if (value.Contains("ZOOM") || value.Contains("FOCUS") || value.Contains("PAN") || value.Contains("TILT") || value.Contains("SYNC") || value.Contains("MOVE"))
            {
                return "카메라 PTZF 제어";
            }

            if (value.Contains("FIRE") || value.Contains("SMOKE") || value.Contains("DETECTION") || value.Contains("AI"))
            {
                return "화재·연기 감지";
            }

            if (value.Contains("PANORAMA"))
            {
                return "파노라마 생성";
            }

            if (value.Contains("CSV") || value.Contains("PRESET") || value.Contains("STORAGE"))
            {
                return "설정·데이터 저장";
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                return "프로그램";
            }
            else
            {
                return category;
            }
        }

        private static string GetFriendlyCause(string message, Exception exception)
        {
            // 파일 공유·바이트 범위 잠금 오류를 AI 또는 네트워크 장애로 분류하지 않는다.
            if (exception is IOException && ((exception.HResult & 0xFFFF) == 32 || (exception.HResult & 0xFFFF) == 33))
            {
                return "다른 작업 또는 프로그램이 파일을 잠그고 있습니다";
            }

            string value = ((message ?? string.Empty) + " " + (exception?.Message ?? string.Empty)).ToUpperInvariant();

            if (value.Contains("TIMEOUT"))
            {
                return "장비 응답 시간이 초과되었습니다";
            }

            if (value.Contains("DISCONNECT") || value.Contains("CONNECTION"))
            {
                return "장비 또는 영상 연결이 끊겼습니다";
            }

            if (value.Contains("CANCEL"))
            {
                return "작업이 취소되었습니다";
            }

            if (value.Contains("ACCESS") || value.Contains("UNAUTHORIZED"))
            {
                return "접근 권한 또는 인증 정보가 올바르지 않습니다";
            }

            if (value.Contains("FILE") || value.Contains("DIRECTORY") || value.Contains("CSV"))
            {
                return "파일을 읽거나 저장하지 못했습니다";
            }

            if (value.Contains("DECODE"))
            {
                return "영상 디코딩에 실패했습니다";
            }

            if (value.Contains("RENDER") || value.Contains("DISPLAY"))
            {
                return "영상 화면 갱신에 실패했습니다";
            }

            if (value.Contains("COMMAND") || value.Contains("SEND") || value.Contains("PACKET"))
            {
                return "제어 명령을 전송하거나 처리하지 못했습니다";
            }

            if (exception == null)
            {
                return "요청한 작업을 완료하지 못했습니다";
            }
            else
            {
                return exception.GetType().Name + ": " + exception.Message;
            }
        }

        private static string GetRecommendedAction(string category)
        {
            string value = (category ?? string.Empty).ToUpperInvariant();

            if (value == "CSV AUDIT")
            {
                return "CSV를 연 프로그램·중복 Viewer를 닫고 저장 경로·권한을 확인. 실패한 감사 행은 자동 복구되지 않으므로 화면 목록을 CSV로 저장";
            }

            if (value.Contains("RTSP") || value.Contains("VIDEO") || value.Contains("DISPLAY"))
            {
                return "카메라 전원·RTSP 주소·네트워크 상태를 확인 후 재연결";
            }

            if (value.Contains("DEVICE") || value.Contains("TCP") || value.Contains("AGENT") || value.Contains("CONNECT"))
            {
                return "Agent IP/Port와 장비 전원을 확인 후 CONNECT 재시도";
            }

            if (value.Contains("ZOOM") || value.Contains("FOCUS") || value.Contains("PAN") || value.Contains("TILT") || value.Contains("SYNC") || value.Contains("MOVE"))
            {
                return "장비 연결과 현재 상태값 수신 여부를 확인 후 STOP→재시도";
            }

            if (value.Contains("AI") || value.Contains("DETECTION") || value.Contains("FIRE") || value.Contains("SMOKE"))
            {
                return "AI 연결·입력 영상·모델 설정을 확인";
            }

            if (value.Contains("PANORAMA"))
            {
                return "EO 영상 연결과 저장 경로를 확인 후 다시 촬영";
            }

            if (value.Contains("CSV") || value.Contains("PRESET") || value.Contains("STORAGE"))
            {
                return "파일 경로·권한·남은 디스크 공간을 확인";
            }

            return "오류 코드와 기술 상세를 담당자에게 전달";
        }

        /// <summary>
        /// 작업 시작과 종료 시간을 한 묶음으로 출력한다.
        /// using 블록에서 사용하면 예외나 조기 return이 있어도 종료 로그가 남는다.
        /// </summary>
        public static IDisposable BeginScope(string category, string operation)
        {
            return new ConsoleLogScope(category, operation);
        }

        /// <summary>
        /// Write 저장 함수.
        /// </summary>
        private static void Write(string level, string category, string message)
        {
            string safeCategory = string.IsNullOrWhiteSpace(category) ? "GENERAL" : category.Trim();

            string safeMessage = MaskRtspInLog(message ?? string.Empty);

            int threadId = Thread.CurrentThread.ManagedThreadId;

            bool warningOrError = level == "WARN" || level == "ERROR";

            bool diagnosticOnly = !warningOrError && (IsDiagnosticLog(safeCategory, safeMessage) || (safeCategory == "EQUIPMENT STORAGE" && safeMessage.StartsWith("장비 ID 기준 저장 / ", StringComparison.Ordinal)));

            string summary = warningOrError ? null : "[" + OperationalLogText.Category(safeCategory) + "] " + OperationalLogText.Summary(safeCategory, safeMessage);

            lock (ConsoleLock)
            {
                // 반복 원시 상태·경로 확인은 파일에 보존하며 운용 콘솔에서는 출력하지 않는다.
                // 주기 성능 로그만 기존 핵심 수치를 짧게 표시한다.
                if (!diagnosticOnly || safeCategory.IndexOf("RTSP PERF", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string display = warningOrError ? "[T" + threadId.ToString("00") + "] [" + safeCategory + "] " + safeMessage : diagnosticOnly ? FormatDiagnosticSummary(safeCategory, safeMessage) : summary;

                    WriteConsoleSafe($"[{DateTime.Now:HH:mm:ss.fff}] [{level.Trim()}]");

                    WriteConsoleSafe(ReadableLogLayout.Body(display));

                    WriteConsoleSafe();
                }

                // 이벤트를 한 번만 기록한다. 요약은 속성으로, 원문은 메시지로 전달한다.
                WriteSerilog(level, $"[T{threadId:00}] [{safeCategory}] " + safeMessage, diagnosticOnly, summary);
            }
        }

        /// <summary>주기 성능·원시 상태 샘플은 운용 이력과 구분하되 진단 파일에는 원문을 보존한다.</summary>
        private static bool IsDiagnosticLog(string category, string message)
        {
            string name = (category ?? string.Empty).ToUpperInvariant();

            return name.Contains("RTSP PERF") || name.Contains("PAN/TILT POSITION") || name.Contains("/ FRAME") || name.Contains("/ RX") || name.Contains("/ TX") || (name == "IR ZOOM SYNC" && (message ?? string.Empty).Contains("STATUS_STEP="));
        }

        /// <summary>콘솔에서는 영상 성능의 핵심 수치만 보여주고 전체 샘플은 진단 파일로 안내한다.</summary>
        private static string FormatDiagnosticSummary(string category, string message)
        {
            var values = new System.Collections.Generic.List<string>();

            for (int index = 0; index < PerformanceMetricPatterns.Length; index++)
            {
                var match = PerformanceMetricPatterns[index].Match(message ?? string.Empty);

                if (match.Success)
                {
                    values.Add(PerformanceMetricLabels[index] + "=" + match.Groups[1].Value.Trim());
                }
            }

            return "[성능 진단] " + category + (values.Count == 0 ? " 상태 샘플 수신" : " / " + string.Join(" / ", values)) + " / 전체 수치: Logs의 rei-viewer-diagnostics 파일";
        }

        /// <summary>
        /// WriteStructuredSection 저장 함수.
        /// </summary>
        private static void WriteStructuredSection(string level, string category, params string[] lines)
        {
            string safeCategory = string.IsNullOrWhiteSpace(category) ? "GENERAL" : category.Trim();

            string header = $"[{DateTime.Now:HH:mm:ss.fff}] " + $"[{level}] " + $"[T{Thread.CurrentThread.ManagedThreadId:00}] " + $"[{safeCategory}]";

            WriteSection(header, lines, level, safeCategory);
        }

        /// <summary>
        /// WriteSection 저장 함수.
        /// </summary>
        private static void WriteSection(string header, params string[] lines)
        {
            WriteSection(header, lines, "INFO", null);
        }

        /// <summary>
        /// Console에는 사람이 읽기 좋은 Section Header를 출력하고,
        /// Serilog에는 중복 Timestamp/Level이 없는 정리된 Section을 기록한다.
        /// </summary>
        private static void WriteSection(string header, string[] lines, string level, string category)
        {
            // 긴 구분선과 반복 표제를 만들지 않고 공통 이벤트 경로를 사용한다.
            string body = lines == null ? string.Empty : string.Join(Environment.NewLine, lines);

            Write(level, string.IsNullOrWhiteSpace(category) ? header.Trim('[', ']') : category, body);
        }

        /// <summary>
        /// Console Handle 종료 상태를 고려한 안전 출력 함수.
        ///
        /// 영상 Capture, 통신 수신 등 Background Thread가
        /// 프로그램 종료 시점에 로그를 출력하더라도
        /// IOException이 작업 Thread 밖으로 전파되지 않도록 처리한다.
        /// </summary>
        private static void WriteConsoleSafe(string message = null)
        {
            if (!_isConsoleOutputAvailable)
            {
                return;
            }

            try
            {
                Interlocked.Increment(ref _totalWriteCount);

                _writingStructuredConsole = true;

                try
                {
                    Console.WriteLine(message ?? string.Empty);
                }
                finally
                {
                    _writingStructuredConsole = false;
                }
            }
            catch (IOException)
            {
                /*
                 * Console 창 또는 표준 출력 Handle이 먼저 종료된 경우이다.
                 * 영상 및 통신 동작과 무관한 로그 출력 오류이므로
                 * 이후 Console 출력을 중지한다.
                 */
                _isConsoleOutputAvailable = false;

                Debug.WriteLine(message ?? string.Empty);
            }
            catch (ObjectDisposedException)
            {
                _isConsoleOutputAvailable = false;

                Debug.WriteLine(message ?? string.Empty);
            }
        }

        /// <summary>
        /// Console 로그 레벨을 Serilog 파일 로그 레벨로 변환한다.
        ///
        /// 기존 ViewModel과 Service의 호출부는 변경하지 않고,
        /// 이 Helper를 통과하는 운용 로그를 날짜별 파일에도 함께 남긴다.
        /// </summary>
        private static void WriteSerilog(string level, string message, bool diagnosticOnly, string summary)
        {
            message = MaskRtspInLog(message);

            // 경고·오류는 진단 전용으로 숨기지 않고 기존 일반·오류 파일에 남긴다.
            var logger = Log.ForContext("ViewerDiagnosticOnly", diagnosticOnly && level != "WARN" && level != "ERROR").ForContext("ViewerSourceLevel", level.Trim());

            if (summary != null)
            {
                logger = logger.ForContext("ViewerOperationalMessage", MaskRtspInLog(summary));
            }

            switch (level)
            {
                case "WARN":
                    logger.Warning("{LogMessage}", message);
                    break;
                case "ERROR":
                    logger.Error("{LogMessage}", message);
                    break;
                case "CMD ":
                case "STATE":
                case "INFO":
                default:
                    logger.Information("{LogMessage}", message);
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
            public ConsoleLogScope(string category, string operation)
            {
                _category = category;
                _operation = operation;
                _startedAt = DateTime.Now;

                Info(_category, $"START / {_operation}");
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

                TimeSpan elapsed = DateTime.Now - _startedAt;

                Info(_category, $"END / {_operation} / ELAPSED={elapsed.TotalMilliseconds:F0}ms");
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

            private bool _innerUnavailable;

            /// <summary>
            /// NormalizedConsoleTextWriter 동작 수행 함수.
            /// </summary>
            public NormalizedConsoleTextWriter(TextWriter inner)
            {
                _inner = inner ?? TextWriter.Null;
            }

            public override Encoding Encoding
            {
                get { return _inner.Encoding; }
            }

            /// <summary>
            /// Write 저장 함수.
            /// </summary>
            public override void Write(char value)
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
            public override void Write(string value)
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
            public override void WriteLine(string value)
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
                    SafeFlush();
                }
            }

            /// <summary>
            /// FlushPendingLine 동작 수행 함수.
            /// </summary>
            private void FlushPendingLine()
            {
                string line = MaskRtspInLog(_pendingLine.ToString());

                _pendingLine.Clear();

                string trimmed = line.Trim();

                bool isSeparator = trimmed.Length >= 20 && trimmed.All(character => character == '=');

                if (isSeparator)
                {
                    if (_separatorSinceContent)
                    {
                        return;
                    }

                    SafeWriteLine(LogLine);

                    _separatorSinceContent = true;
                    _lastLineWasBlank = false;

                    return;
                }

                if (trimmed.Length == 0)
                {
                    if (!_separatorSinceContent && !_lastLineWasBlank)
                    {
                        SafeWriteLine();

                        _lastLineWasBlank = true;
                    }

                    return;
                }

                if (!_writingStructuredConsole)
                {
                    // 기존 TCP 송수신 등 직접 콘솔 기록도 시간·스레드와 함께 기술 파일에 보존한다.
                    // 공통 Helper를 거치지 않은 줄은 내용을 재해석하여 성공·실패를 만들어 내지 않는다.
                    string legacyLevel = line.StartsWith("[TCP ERROR]", StringComparison.Ordinal) || line.StartsWith("[ERROR]", StringComparison.Ordinal) ? "ERROR" : line.StartsWith("[WARN]", StringComparison.Ordinal) ? "WARN" : "INFO";

                    WriteSerilog(legacyLevel, $"[T{Thread.CurrentThread.ManagedThreadId:00}] [CONSOLE] " + line, true, null);
                }

                SafeWriteLine(line);

                _separatorSinceContent = false;
                _lastLineWasBlank = false;
            }

            /// <summary>
            /// 2026-10-06: 디버그 콘솔이 닫히거나 출력 핸들이 무효화되어도
            /// 로그 보조 기능의 IOException이 RTSP/제어 작업으로 전파되지 않게 한다.
            /// 최초 실패 후에는 해당 Writer 출력을 중단해 예외 반복과 성능 저하를 막는다.
            /// </summary>
            private void SafeWriteLine(string value = null)
            {
                if (_innerUnavailable)
                {
                    return;
                }

                try
                {
                    if (value == null)
                    {
                        _inner.WriteLine();
                    }
                    else
                    {
                        _inner.WriteLine(value);
                    }
                }
                catch (IOException ex)
                {
                    DisableInnerWriter(ex);
                }
                catch (ObjectDisposedException ex)
                {
                    DisableInnerWriter(ex);
                }
            }

            private void SafeFlush()
            {
                if (_innerUnavailable)
                {
                    return;
                }

                try
                {
                    _inner.Flush();
                }
                catch (IOException ex)
                {
                    DisableInnerWriter(ex);
                }
                catch (ObjectDisposedException ex)
                {
                    DisableInnerWriter(ex);
                }
            }

            private void DisableInnerWriter(Exception exception)
            {
                _innerUnavailable = true;

                try
                {
                    Debug.WriteLine("Console writer disabled: " + exception.Message);
                }
                catch
                {
                    // 진단 출력 실패는 애플리케이션 기능에 영향을 주지 않는다.
                }
            }
        }
    }
}
