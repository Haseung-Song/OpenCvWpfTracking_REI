using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenCvWpfTracking.Common;

namespace OpenCvWpfTracking.Services.Configuration
{
    /// <summary>접속 주소와 분리한 고정 장비 ID로 저장 폴더를 찾는다.</summary>
    public static class EquipmentStorage
    {
        private const string OwnerFile = ".equipment-id";

        private static readonly object Gate = new object();

        private static readonly Dictionary<string, string> Directories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] PresetFiles =
        {
            "Presets.csv",
            "Presets.tsv",
            "PresetPositions.xml",
            "Presets.xlsx",
            "Presets.csv.bak",
            "Presets.tsv.bak",
            "PresetPositions.xml.bak"
        };

        /// <summary>이전 장비 ID 표기도 같은 고정 ID로 정규화한다. 주소·암호는 포함하지 않는다.</summary>
        public static string Identity(string deviceId, bool web)
        {
            string id = DeviceCatalog.CanonicalDeviceId(deviceId).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(id) || id.Any(char.IsControl) || id.IndexOfAny(new[] { '/', '\\', '|' }) >= 0)
            {
                throw new IOException("장비 고정 ID를 확인하세요.");
            }

            return (web ? "WEB|" : "LA|") + id;
        }

        /// <summary>IP·카메라 주소가 달라져도 같은 장비의 폴더와 완전한 프리셋 묶음을 유지한다.</summary>
        public static string ResolvePresets(string root, string deviceId, bool web, string displayName, params string[] legacyHashes)
        {
            return Resolve(root, deviceId, web, displayName, true, legacyHashes);
        }

        /// <summary>기존 사진은 옮기거나 덮어쓰지 않고 마지막 촬영 폴더를 고정 장비에 연결한다.</summary>
        public static string ResolvePanoramas(string root, string deviceId, bool web, string displayName)
        {
            return Resolve(root, deviceId, web, displayName, false, new string[0]);
        }

        private static string Resolve(string root, string deviceId, bool web, string displayName, bool presets, string[] legacyHashes)
        {
            root = Path.GetFullPath(root);

            string identity = Identity(deviceId, web);

            string cacheKey = root + "|" + identity;

            lock (Gate)
            {
                string cached;

                if (Directories.TryGetValue(cacheKey, out cached) && Directory.Exists(cached))
                {
                    return cached;
                }

                Directory.CreateDirectory(root);

                // 다른 실행 인스턴스가 이전 자료를 가져오는 동안 같은 폴더를 동시에 변경하지 않는다.
                using (new FileStream(Path.Combine(root, ".equipment-storage.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    string[] folders = Directory.GetDirectories(root).Where(IsRegularDirectory).ToArray();

                    foreach (string folder in folders)
                    {
                        RecoverPresetImport(folder);
                    }

                    string[] owned = folders.Where(path => ReadOwner(path) == identity).ToArray();

                    if (owned.Length > 1)
                    {
                        throw new IOException("같은 장비 ID의 저장 폴더가 여러 개입니다. 원본을 보존하고 확인하세요.");
                    }

                    if (owned.Length == 1)
                    {
                        Directories[cacheKey] = owned[0];

                        return owned[0];
                    }

                    string stem = FolderName(displayName, web);

                    string previousStem = FolderName(DeviceCatalog.StorageDisplayName(displayName), web);

                    var candidates = folders.Where(path => string.IsNullOrEmpty(ReadOwner(path)) && (MatchesLegacyName(Path.GetFileName(path), stem) || MatchesLegacyName(Path.GetFileName(path), previousStem) || legacyHashes.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))).ToArray();

                    string directory = Path.Combine(root, stem);

                    if (Directory.Exists(directory) && !string.IsNullOrEmpty(ReadOwner(directory)))
                    {
                        directory += "_" + SafeLabel(DeviceCatalog.CanonicalDeviceId(deviceId));
                    }

                    if (Directory.Exists(directory) && !string.IsNullOrEmpty(ReadOwner(directory)))
                    {
                        throw new IOException("장비명 폴더가 다른 장비에 연결되어 있습니다.");
                    }

                    if (!presets)
                    {
                        // 기본 사진 0번만 있는 빈 폴더보다 실제 촬영 결과가 있는 기존 폴더를 우선한다.
                        string previous = candidates.OrderByDescending(LastPanoramaWrite).ThenBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();

                        if (previous != null && LastPanoramaWrite(previous) > DateTime.MinValue)
                        {
                            directory = previous;
                        }
                    }

                    Directory.CreateDirectory(directory);

                    if (presets)
                    {
                        string latest = candidates.Where(HasPresetFile).OrderByDescending(LastPresetWrite).ThenBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();

                        if (latest != null && !string.Equals(latest, directory, StringComparison.OrdinalIgnoreCase))
                        {
                            ImportPresetBundle(latest, directory);
                        }
                    }

                    // 자료 복원이 끝난 뒤 소유 정보를 확정한다. 실패한 이전 작업은 빈 목록으로 확정하지 않는다.
                    string marker = Path.Combine(directory, OwnerFile);

                    string temporary = marker + ".tmp";

                    File.WriteAllText(temporary, identity, new UTF8Encoding(false));

                    File.Move(temporary, marker);

                    Directories[cacheKey] = directory;

                    ConsoleLogHelper.State("EQUIPMENT STORAGE", "장비 ID 기준 저장 / " + identity + " / 폴더: " + directory);

                    return directory;
                }
            }
        }

        private static bool IsRegularDirectory(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
        }

        private static string ReadOwner(string path)
        {
            string marker = Path.Combine(path, OwnerFile);

            if (File.Exists(marker))
            {
                return File.ReadAllText(marker).Trim();
            }
            else
            {
                return string.Empty;
            }
        }

        private static string SafeLabel(string value)
        {
            string label = new string((value ?? "DEVICE").Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_').ToArray());

            label = Regex.Replace(label, "_+", "_").Trim('_');

            if (string.IsNullOrEmpty(label))
            {
                return "DEVICE";
            }
            else
            {
                return label;
            }
        }

        private static string FolderName(string displayName, bool web)
        {
            string name = Regex.Replace(displayName ?? "DEVICE", @"^\s*\d+\.\s*", "");

            name = Regex.Replace(name, @"\s+EO\s*/\s*IR\s*$", "", RegexOptions.IgnoreCase);

            return (web ? "WEB_" : "LA_") + SafeLabel(name);
        }

        private static bool MatchesLegacyName(string name, string stem)
        {
            return string.Equals(name, stem, StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(name, "^" + Regex.Escape(stem) + @"_(?:\d+|[0-9.]+_\d+|[0-9A-F]{12})(?:_\d+)?$", RegexOptions.IgnoreCase);
        }

        private static bool HasPresetFile(string directory)
        {
            return File.Exists(Path.Combine(directory, "Presets.csv")) || File.Exists(Path.Combine(directory, "Presets.tsv"));
        }

        private static DateTime LastPresetWrite(string directory)
        {
            return File.GetLastWriteTimeUtc(Path.Combine(directory, File.Exists(Path.Combine(directory, "Presets.csv")) ? "Presets.csv" : "Presets.tsv"));
        }

        private static DateTime LastPanoramaWrite(string directory)
        {
            return Directory.EnumerateFiles(directory, "Panorama *.jpg").Where(path => !string.Equals(Path.GetFileName(path), "Panorama 0.jpg", StringComparison.OrdinalIgnoreCase)).Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();
        }

        /// <summary>최신 CSV와 위치 메타데이터를 한 묶음으로 복원하며 기존 묶음은 별도로 보존한다.</summary>
        private static void ImportPresetBundle(string source, string destination)
        {
            var incoming = PresetFiles.Where(name => File.Exists(Path.Combine(source, name))).ToDictionary(name => name, name => File.ReadAllBytes(Path.Combine(source, name)));

            var previous = PresetFiles.Where(name => File.Exists(Path.Combine(destination, name))).ToDictionary(name => name, name => File.ReadAllBytes(Path.Combine(destination, name)));

            string listName = incoming.ContainsKey("Presets.csv") ? "Presets.csv" : "Presets.tsv";

            string header = Encoding.UTF8.GetString(incoming[listName]).TrimStart('\uFEFF');

            if (!header.StartsWith("TYPE", StringComparison.Ordinal) || !header.Contains("SAVED_ORDER"))
            {
                throw new IOException("이전 프리셋 파일 형식을 확인하세요. 원본은 보존됩니다.");
            }

            string archive = Path.Combine(destination, "이전자료", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(archive);

            foreach (var file in previous)
            {
                File.WriteAllBytes(Path.Combine(archive, file.Key), file.Value);
            }

            string staged = Path.Combine(archive, "가져올자료");

            Directory.CreateDirectory(staged);

            foreach (var file in incoming)
            {
                File.WriteAllBytes(Path.Combine(staged, file.Key), file.Value);
            }

            // 반영 도중 종료되어도 보관 위치와 가져올 원본을 확인할 수 있게 기록한다.
            File.WriteAllText(Path.Combine(archive, "자료이전.txt"), "기존 묶음 보관\r\n\r\n가져올 자료: " + source + "\r\n", new UTF8Encoding(true));

            ReplaceFile(Path.Combine(destination, ".preset-import"), Encoding.UTF8.GetBytes(Path.GetFileName(archive)));

            RecoverPresetImport(destination);

            ConsoleLogHelper.StateSection("PRESET STORAGE", "장비별 기존 자료 복원", string.Empty, "선택: 최근 저장한 프리셋 묶음", "원본: " + source, "사용: " + destination, "이전 자료: " + archive, "슬롯·좌표·줌·초점 값은 변환하지 않습니다.");
        }

        /// <summary>이전 중 종료된 경우 보관된 새 묶음을 다시 반영한 뒤에만 목록을 읽게 한다.</summary>
        private static void RecoverPresetImport(string directory)
        {
            string journal = Path.Combine(directory, ".preset-import");

            if (!File.Exists(journal))
            {
                return;
            }

            string leaf = File.ReadAllText(journal).Trim();

            if (!Regex.IsMatch(leaf, @"^\d{8}_\d{6}_\d{3}_[0-9a-f]{32}$"))
            {
                throw new IOException("프리셋 자료 이전 기록을 확인하세요. 원본은 보존됩니다.");
            }

            string staged = Path.Combine(directory, "이전자료", leaf, "가져올자료");

            var contents = PresetFiles.Where(name => File.Exists(Path.Combine(staged, name))).ToDictionary(name => name, name => File.ReadAllBytes(Path.Combine(staged, name)));

            string listName = contents.ContainsKey("Presets.csv") ? "Presets.csv" : "Presets.tsv";

            if (!contents.ContainsKey(listName))
            {
                throw new IOException("프리셋 자료 이전 묶음이 없습니다. 보존 자료를 확인하세요.");
            }

            foreach (string name in PresetFiles.Where(name => name != listName).Concat(new[] { listName }))
            {
                string target = Path.Combine(directory, name);

                if (contents.ContainsKey(name))
                {
                    ReplaceFile(target, contents[name]);
                }
                else if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }

            File.Delete(journal);
        }

        private static void ReplaceFile(string path, byte[] contents)
        {
            string temporary = path + ".importing";

            File.WriteAllBytes(temporary, contents);

            if (File.Exists(path))
            {
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }
    }
}
