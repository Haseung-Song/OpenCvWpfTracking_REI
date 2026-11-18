using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;

namespace OpenCvWpfTracking.Services.Configuration
{
    public sealed class DeviceEntry
    {
        public string Id;

        public string Name;

        public string AgentIp;

        public string AgentPort;

        public string EoUrl;

        public string EoUser;

        public string EoPassword;

        public string IrUrl;

        public string IrUser;

        public string IrPassword;

        public string ControlIp;

        public string ControlUser;

        public string ControlPassword;

        public ControlAgentType AgentType;

        public CameraControlType EoControlType;

        public bool UseHttps;

        public bool PositionSensors;

        public int Order;

        public string Label
        {
            get
            {
                return Order.ToString(CultureInfo.InvariantCulture) + ". " + Name;
            }
        }

        public string EoLabel
        {
            get
            {
                return Label + " - EO";
            }
        }

        public string IrLabel
        {
            get
            {
                return Label + " - IR";
            }
        }

        public string EoAddress
        {
            get
            {
                return DeviceCatalog.Address(EoUrl, EoUser, EoPassword);
            }
        }

        public string IrAddress
        {
            get
            {
                return DeviceCatalog.Address(IrUrl, IrUser, IrPassword);
            }
        }
    }

    // UTF-8 CSV는 민감한 설정 파일이며 암호화된 인증정보 저장소가 아니다.
    // 잘못된 설정은 조용히 덮어쓰지 않고 행 내용도 로그에 기록하지 않는다.
    public static class DeviceCatalog
    {
        public const string Header = "DeviceId,Name,AgentType,AgentIp,AgentPort,EoUrl,EoId,EoPw,IrUrl,IrId,IrPw,EoControlType,EoControlIp,EoControlId,EoControlPw,EoHttps,PositionSensors";

        private static readonly Lazy<List<DeviceEntry>> Cache = new Lazy<List<DeviceEntry>>(() =>
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Devices.csv");

                var entries = Load(path);

                CsvExcelCompanion.Schedule(path);

                return entries;
            }
            catch (Exception)
            {
                ConsoleLogHelper.State("DEVICE CSV", "장비 설정 읽기 실패 / Config/Devices.csv 확인 필요 (원본 유지)");

                return new List<DeviceEntry>();
            }
        });

        public static IReadOnlyList<DeviceEntry> Entries
        {
            get { return Cache.Value; }
        }

        /// <summary>설치 장소 표시는 UI에 유지하되 기존 장비 저장 키에서는 장소 접미사만 제외한다.</summary>
        public static string StorageDisplayName(string displayName)
        {
            return System.Text.RegularExpressions.Regex.Replace(displayName ?? "", @"(ER-WATCHER\s+[1-4])\s*\([^)]*\)", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>장비 번호와 EO/IR 구분을 유지하여 과거 이름을 현재 선택 항목에 대응한다.</summary>
        public static string DisplayIdentity(string displayName)
        {
            return System.Text.RegularExpressions.Regex.Replace(StorageDisplayName(displayName), @"^\s*\d+\.\s*", "").Trim();
        }

        /// <summary>
        /// 장비 식별자로 CSV 항목을 찾으며 항목이 없으면 빈 설정 객체를 반환한다.
        /// </summary>
        public static string CanonicalDeviceId(string id)
        {
            if (string.Equals(id, "MR300_ERWATCHER", StringComparison.OrdinalIgnoreCase))
            {
                return "ER_WATCHER_1";
            }

            return System.Text.RegularExpressions.Regex.Replace(id ?? "", @"^ER_?WATCHER_([1-4])$", "ER_WATCHER_$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>이전 장비 ID로 조회해도 통일한 현재 ID의 설정을 사용한다.</summary>
        public static DeviceEntry Get(string id)
        {
            return Entries.FirstOrDefault(e => CanonicalDeviceId(e.Id) == CanonicalDeviceId(id)) ?? new DeviceEntry
            {
                Name = "",
                EoUrl = "",
                IrUrl = ""
            };
        }

        /// <summary>
        /// 기존 인증정보와 URL 이스케이프를 보존하며 RTSP 접속 주소를 구성한다.
        /// </summary>
        public static string Address(string url, string user, string password)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }

            if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password))
            {
                return url;
            }

            // 완성된 URI는 다시 디코딩하거나 이스케이프하지 않는다.
            var uri = new Uri(url, UriKind.Absolute);

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                return url;
            }

            int authority = url.IndexOf("://", StringComparison.Ordinal) + 3;

            return url.Insert(authority, Uri.EscapeDataString(user ?? "") + ":" + Uri.EscapeDataString(password ?? "") + "@");
        }

        /// <summary>
        /// 장비 CSV의 헤더·필드·주소·중복 식별자를 검증하고 장비 목록을 만든다.
        /// </summary>
        public static List<DeviceEntry> Load(string path)
        {
            var rows = Parse(File.ReadAllText(path, Encoding.UTF8));

            if (rows.Count < 2 || string.Join(",", rows[0]) != Header)
            {
                throw new FormatException("Invalid device CSV header.");
            }

            var result = new List<DeviceEntry>();

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows.Skip(1))
            {
                if (row.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (row.Count != 17 || result.Count >= 256)
                {
                    throw new FormatException("Invalid device CSV field count.");
                }

                ControlAgentType agent;

                CameraControlType control;

                int port;

                bool https, sensors;

                if (!ids.Add(row[0]) || string.IsNullOrWhiteSpace(row[0]) || string.IsNullOrWhiteSpace(row[1]) || row[1].Any(char.IsControl) || row[1].Contains("://") || !Enum.TryParse(row[2], true, out agent) || !Enum.IsDefined(typeof(ControlAgentType), agent) || string.IsNullOrWhiteSpace(row[3]) || !int.TryParse(row[4], out port) || port < 1 || port > 65535 || !Enum.TryParse(row[11], true, out control) || !Enum.IsDefined(typeof(CameraControlType), control) || !bool.TryParse(row[15], out https) || !bool.TryParse(row[16], out sensors))
                {
                    throw new FormatException("Invalid device CSV values.");
                }

                foreach (var index in new[]
                {
                    5,
                    8
                }

                )
                {
                    Uri uri;

                    if (!Uri.TryCreate(row[index], UriKind.Absolute, out uri) || uri.Scheme != "rtsp" || string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.Fragment))
                    {
                        throw new FormatException("Invalid RTSP URI. Escape credential # as %23.");
                    }
                }

                if (control == CameraControlType.CtecCgi && string.IsNullOrWhiteSpace(row[12]))
                {
                    throw new FormatException("Missing camera control host.");
                }

                result.Add(new DeviceEntry { Id = row[0], Name = row[1], AgentType = agent, AgentIp = row[3], AgentPort = row[4], EoUrl = row[5], EoUser = row[6], EoPassword = row[7], IrUrl = row[8], IrUser = row[9], IrPassword = row[10], EoControlType = control, ControlIp = row[12], ControlUser = row[13], ControlPassword = row[14], UseHttps = https, PositionSensors = sensors, Order = result.Count + 1 });
            }

            if (result.Count == 0)
            {
                throw new FormatException("Empty device CSV.");
            }

            return result;
        }

        // 쉼표·따옴표 중복·따옴표 안 줄바꿈을 포함한 표준 CSV를 처리한다.
        internal static List<List<string>> Parse(string text)
        {
            return CsvExcelCompanion.Parse(text);
        }
    }
}
