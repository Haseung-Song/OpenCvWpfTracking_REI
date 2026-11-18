using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace OpenCvWpfTracking.Common
{
    /// <summary>
    /// 2026-11-06: 운용 화면에는 실제 이벤트의 짧은 설명을, 진단 파일에는 원문을 남긴다.
    /// 수신·송신·도착 여부를 추정하지 않으며 알 수 없는 설명은 원문 첫 줄을 사용한다.
    /// </summary>
    internal static class OperationalLogText
    {
        // 2026-11-12: 번역 순서와 문구는 유지하고 고정 테이블을 재사용한다.
        private static readonly string[,] SummaryPhrases =
            {
            {
                "MainViewModel initialization started",
                "프로그램 초기화 시작"
            },
            {
                "MainViewModel initialization completed",
                "프로그램 초기화 완료"
            },
            {
                "MainViewModel initialized",
                "프로그램 초기화 완료"
            },
            {
                "OpenCV WPF Debug Console Start",
                "디버그 콘솔 시작"
            },
            {
                "Saved camera settings loaded",
                "저장된 카메라 설정 불러옴"
            },
            {
                "Profile default=",
                "기본 속도="
            },
            {
                "Connection requested",
                "연결 요청"
            },
            {
                "Connect Try...",
                "TCP 연결 시도"
            },
            {
                "Connect Success",
                "TCP 연결 완료"
            },
            {
                "Connect Failed",
                "TCP 연결 실패"
            },
            {
                "Connected to",
                "TCP 연결 완료:"
            },
            {
                "Disconnected",
                "연결 해제"
            },
            {
                "Connecting",
                "연결 시도 중"
            },
            {
                "Reconnecting",
                "재연결 중"
            },
            {
                "Timeout",
                "시간 초과"
            },
            {
                "TARGET_REACHED",
                "목표 도착"
            },
            {
                "ACTIVE_CAMERA=",
                "기준 카메라="
            }
            };

        internal static string Category(string category)
        {
            string name = (category ?? "").ToUpperInvariant();

            if (name == "MAIN" || name == "SYSTEM" || name == "CONSOLE")
            {
                return "시스템";
            }

            if (name.Contains("PANORAMA"))
            {
                return "파노라마";
            }

            if (name.Contains("PRESET"))
            {
                return "프리셋";
            }

            if (name.Contains("SCAN"))
            {
                return "자동 순회";
            }

            if (name.Contains("ZONE"))
            {
                return "구역";
            }

            if (name.Contains("POWER"))
            {
                return "전원";
            }

            if (name.Contains("RTSP") || name.Contains("VIDEO"))
            {
                return "영상";
            }

            if (name.Contains("STORAGE") || name.Contains("CONFIG"))
            {
                return "설정";
            }

            if (name.Contains("FOCUS") || name.Contains("ZOOM") || name.Contains("PAN") || name.Contains("TILT") || name.Contains("PTZ"))
            {
                return "PTZF";
            }

            if (name.Contains("FIRE") || name.Contains("SMOKE") || name.Contains("AI"))
            {
                return "감지";
            }

            if (name.Contains("AGENT") || name.Contains("TCP") || name.Contains("CONNECT"))
            {
                return "통신";
            }

            return category ?? "시스템";
        }

        internal static string Summary(string category, string message)
        {
            string text = (message ?? "").Trim();

            if (string.Equals(category, "PAN / TILT SPEED SOURCE", StringComparison.Ordinal))
            {
                text = "PT 속도 기준 변경 / " + text;
            }

            // 호출부의 결과 동사를 그대로 번역하여 송신과 실제 도착을 혼동하지 않는다.
            for (int index = 0; index < SummaryPhrases.GetLength(0); index++)
            {
                text = text.Replace(SummaryPhrases[index, 0], SummaryPhrases[index, 1]);
            }

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string first = lines.Length == 0 ? "" : lines[0].Trim();

            // 여러 줄 설정 목록은 장비와 개수만 보이며 전체 경로·주소·항목은 기술 파일에서 확인한다.
            if (lines.Length > 1)
            {
                var context = lines.Skip(1).Select(line => line.Trim()).Where(line => line.StartsWith("장비:", StringComparison.Ordinal) || line.StartsWith("목록:", StringComparison.Ordinal) || line.StartsWith("CONTROL_PROFILE=", StringComparison.Ordinal) || line.StartsWith("TARGET :", StringComparison.Ordinal) || line.StartsWith("REASON :", StringComparison.Ordinal)).Take(3);

                first += string.Concat(context.Select(line => " / " + line));
            }

            first = string.Join(" / ", first.Split(new[] { " / " }, StringSplitOptions.None).Where(part => !Regex.IsMatch(part.Trim(), @"^(?:HEX|PACKET|TX|RX|FUNCTION|CHECKSUM|SESSION|VALID|MCB|SCB)=", RegexOptions.IgnoreCase)));

            first = Regex.Replace(first, @"(?i)rtsp://[^\s]+", "RTSP 주소 확인");

            first = Regex.Replace(first, @"[A-Za-z]:[\\/].*", "경로는 기술 로그 참조");

            first = Regex.Replace(first, @"\s+", " ").Trim();

            if (first.Length <= 220)
            {
                return first;
            }
            else
            {
                return first.Substring(0, 217) + "…";
            }
        }
    }

    /// <summary>메모장·콘솔에서 이벤트와 기술 항목을 빈 줄·들여쓰기로 구분한다.</summary>
    internal static class ReadableLogLayout
    {
        internal static string Body(string message)
        {
            var result = new System.Text.StringBuilder();

            bool previousBlank = true;

            foreach (string original in (message ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = original.Trim();

                if (line.Length == 0)
                {
                    if (!previousBlank)
                    {
                        result.AppendLine();
                    }

                    previousBlank = true;

                    continue;
                }

                // 슬래시가 포함된 주소·경로·HEX는 보존한다. 명시적인 항목 구분자만 줄바꿈한다.
                string[] fields = line.Length > 100 ? line.Split(new[] { " / " }, StringSplitOptions.None) : new[]
                {
                    line
                };

                for (int index = 0; index < fields.Length; index++)
                {
                    result.Append("  ").AppendLine(fields[index]);
                }

                previousBlank = false;
            }

            return result.ToString().TrimEnd('\r', '\n');
        }
    }

    /// <summary>한 이벤트의 짧은 표시 속성을 읽어 Debug·일반 파일에 같은 운용 내용을 출력한다.</summary>
    internal sealed class OperationalLogFormatter : ITextFormatter
    {
        private readonly MessageTemplateTextFormatter _technical = new MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}", CultureInfo.InvariantCulture);

        public void Format(LogEvent logEvent, TextWriter output)
        {
            LogEventPropertyValue property;

            var summary = logEvent.Properties.TryGetValue("ViewerOperationalMessage", out property) ? property as ScalarValue : null;

            string message;

            if (summary?.Value is string)
            {
                message = (string)summary.Value;
            }
            else
            {
                // 오류·예외와 기존 직접 Serilog 기록은 상세 표현을 그대로 보존한다.
                using (var writer = new StringWriter(CultureInfo.InvariantCulture))
                {
                    _technical.Format(logEvent, writer);

                    message = writer.ToString();
                }
            }

            ReadableTechnicalLogFormatter.WriteBlock(logEvent, output, message);
        }
    }

    /// <summary>기술·오류 파일도 같은 블록 형식을 쓰며 원래 필드와 예외를 누락하지 않는다.</summary>
    internal sealed class ReadableTechnicalLogFormatter : ITextFormatter
    {
        private readonly MessageTemplateTextFormatter _message = new MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}", CultureInfo.InvariantCulture);

        public void Format(LogEvent logEvent, TextWriter output)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                _message.Format(logEvent, writer);

                WriteBlock(logEvent, output, writer.ToString());
            }
        }

        internal static void WriteBlock(LogEvent logEvent, TextWriter output, string message)
        {
            LogEventPropertyValue level;

            output.Write(logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

            output.Write(" [");

            output.Write(logEvent.Properties.TryGetValue("ViewerSourceLevel", out level) && level is ScalarValue ? ((ScalarValue)level).Value : logEvent.Level.ToString().ToUpperInvariant());

            output.Write("]");

            // 카테고리는 헤더에 한 번만 표시한다. 본문에는 사건 내용과 기술 값만 둔다.
            var category = Regex.Match(message ?? "", @"^(?:\[T\d+\]\s*)?\[[^\]\r\n]+\]\s*");

            if (category.Success)
            {
                output.Write(" ");

                output.Write(category.Value.Trim());

                message = message.Substring(category.Length);
            }

            output.WriteLine();

            output.WriteLine(ReadableLogLayout.Body(message));

            output.WriteLine();
        }
    }
}
