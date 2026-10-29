using OpenCvWpfTracking.Models.Main;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace OpenCvWpfTracking.Services.Video
{
    public sealed class PanoramaPresetLink
    {
        public int Number { get; set; }

        public string Fingerprint { get; set; }

        public double U { get; set; }

        public double V { get; set; }

        public double Pan { get; set; }

        public double Tilt { get; set; }

        public bool ReferenceValid { get; set; } = true;
    }

    /// <summary>영점 변경과 분리한 사진 위치와 연동 당시의 기준 방향을 보존한다.</summary>
    [DataContract]
    public sealed class PanoramaPresetAnchor
    {
        [DataMember]
        public int Number { get; set; }

        [DataMember]
        public string RegistrationKey { get; set; }

        [DataMember]
        public double BasePan { get; set; }

        [DataMember]
        public double BaseTilt { get; set; }

        [DataMember]
        public double U { get; set; }

        [DataMember]
        public double V { get; set; }
    }

    /// <summary>촬영 행별 실제 각도와 최종 합성 위치를 보관한다.</summary>
    [DataContract]
    public sealed class PanoramaCoordinateRow
    {
        [DataMember]
        public double TargetTilt { get; set; }

        [DataMember]
        public double ActualTilt { get; set; }

        [DataMember] public List<double> FramePans { get; set; } = new List<double>();

        [DataMember]
        public double FinalCenterY { get; set; }

        [DataMember]
        public int OutputTop { get; set; }

        [DataMember]
        public int VerticalOverlap { get; set; }

        [DataMember]
        public int VerticalOffset { get; set; }
    }

    /// <summary>촬영 정보와 실제 정합 배치를 JPG 옆 JSON에 함께 보관한다.</summary>
    [DataContract]
    public sealed class PanoramaCoordinateMetadata
    {
        [DataMember] public int FormatVersion { get; set; } = 1;

        [DataMember]
        public string PanoramaFileName { get; set; }

        [DataMember]
        public string ImageHash { get; set; }

        [DataMember]
        public string ContextHash { get; set; }

        [DataMember]
        public string CaptureDateTime { get; set; }

        [DataMember]
        public string GeometryMode { get; set; }

        [DataMember]
        public double ReferencePan { get; set; }

        [DataMember]
        public double StartPan { get; set; }

        [DataMember]
        public double ReferenceTilt { get; set; }

        [DataMember]
        public double CaptureRange { get; set; }

        [DataMember]
        public double VerticalFov { get; set; }

        [DataMember]
        public double OpticalCenterX { get; set; }

        [DataMember]
        public double OpticalCenterY { get; set; }

        [DataMember]
        public int FrameWidth { get; set; }

        [DataMember]
        public int FrameHeight { get; set; }

        [DataMember]
        public int ContributionWidth { get; set; }

        [DataMember]
        public int OutputWidth { get; set; }

        [DataMember]
        public int OutputHeight { get; set; }

        [DataMember] public List<PanoramaCoordinateRow> Rows { get; set; } = new List<PanoramaCoordinateRow>();

        [DataMember]
        public double CapturePanOffset { get; set; }

        [DataMember]
        public double CaptureTiltOffset { get; set; }

        [DataMember]
        public bool ReferenceUncertain { get; set; }

        [DataMember] public List<int> LinkedPresetNumbers { get; set; } = new List<int>();

        /// <summary>null은 기존 수동 연동, false는 명시적 숨김, true는 자동 연동을 뜻한다.</summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? AutoLinkEnabled { get; set; }

        [DataMember]
        public List<PanoramaPresetAnchor> PresetAnchors { get; set; } = new List<PanoramaPresetAnchor>();
    }

    /// <summary>영상 비교 없이 저장된 팬·틸트를 실제 행 정합 좌표계로 변환한다.</summary>
    public static class PanoramaPresetRegistration
    {
        public static double Wrap(double degrees) => (degrees % 360 + 360) % 360;

        /// <summary>인증정보 원문을 저장하지 않고 장비·영상 구성을 구분한다.</summary>
        public static string ContextIdentity(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "");
        }

        /// <summary>
        /// 순번·EO/IR 접미사·해시를 빼고 장비 이름으로 저장한다. 이름 중복 시에만 주소를 붙인다.
        /// </summary>
        public static string DeviceDirectory(string root, bool web, string deviceKey, string displayName)
        {
            string name = System.Text.RegularExpressions.Regex.Replace(displayName ?? "DEVICE", @"^\s*\d+\.\s*", "");
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+EO\s*/\s*IR\s*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string label = new string(name.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_').ToArray());
            label = System.Text.RegularExpressions.Regex.Replace(label, "_+", "_").Trim('_');
            if (label.Length == 0)
                label = "DEVICE";
            if (label.Length > 48)
                label = label.Substring(0, 48);

            string endpoint = (deviceKey ?? "DIRECT").Split('|').Last();
            string identity = ContextIdentity((web ? "WEB|" : "LA|") + endpoint);
            string stem = (web ? "WEB_" : "LA_") + label;
            string address = new string(endpoint.Select(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' ? c : '_').ToArray());
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string directory = Path.Combine(root, stem + (attempt == 0 ? "" : "_" + address));
                Directory.CreateDirectory(directory);
                string owner = Path.Combine(directory, ".device");
                if (File.Exists(owner))
                {
                    if (File.ReadAllText(owner) == identity)
                        return directory;
                    continue;
                }

                // 장비 정보가 없는 기존 사진 폴더를 다른 장비 소유로 임의 지정하지 않는다.
                if (Directory.EnumerateFiles(directory, "*.jpg").Any())
                    continue;
                try
                {
                    using (var stream = new FileStream(owner, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream))
                        writer.Write(identity);
                    return directory;
                }
                catch (IOException)
                {
                    if (File.Exists(owner) && File.ReadAllText(owner) == identity)
                        return directory;
                }
            }
            throw new IOException("장비별 파노라마 폴더 구분 정보를 확인하세요.");
        }

        /// <summary>기존 긴 이름 폴더는 이동·삭제하지 않고 사진 복원 시 호환 경로로 사용한다.</summary>
        public static string LegacyDeviceDirectory(string root, bool web, string deviceKey, string displayName)
        {
            string label = new string((displayName ?? "DEVICE").Select(c =>
                char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray()).Trim('_');
            if (label.Length == 0)
                label = "DEVICE";
            if (label.Length > 32)
                label = label.Substring(0, 32);
            string key = (web ? "WEB|" : "LA|") + deviceKey;
            return Path.Combine(root, (web ? "WEB_" : "LA_") + label + "_" + ContextIdentity(key).Substring(0, 12));
        }

        /// <summary>다른 사진이나 외부에서 변경된 JPG에 과거 좌표를 적용하지 않는다.</summary>
        public static string FileIdentity(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        public static string PresetIdentity(PresetPointOption point)
        {
            return ContextIdentity(point.Number + "|" + point.Pan.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                "|" + point.Tilt.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>자동 연동 중에만 현재 번호 목록을 반영하며 저장된 사진 앵커는 유지한다.</summary>
        public static bool SynchronizeLinkedPresetNumbers(PanoramaCoordinateMetadata data, IEnumerable<PresetPointOption> presets)
        {
            if (data?.AutoLinkEnabled != true)
                return false;

            var numbers = (presets ?? Enumerable.Empty<PresetPointOption>()).Select(point => point.Number)
                .Distinct().OrderBy(number => number).ToList();
            if (data.LinkedPresetNumbers != null && data.LinkedPresetNumbers.SequenceEqual(numbers))
                return false;

            // 삭제된 번호의 앵커도 보관한다. 재등록은 ResolveLink의 등록 키로 구분한다.
            data.LinkedPresetNumbers = numbers;
            return true;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> LegacyReferenceStamps =
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        /// <summary>이전 버전의 장비 식별자를 한 번만 읽어 기존 사진과 Config 키의 호환성을 유지한다.</summary>
        public static string ReferenceStamp(string context)
        {
            return LegacyReferenceStamps.GetOrAdd(context, key =>
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "PanoramaReferences",
                    ContextIdentity(key) + ".txt");
                return File.Exists(path) && new FileInfo(path).Length <= 128 ? File.ReadAllText(path) : "0";
            });
        }

        /// <summary>현재 합성기의 중앙 스트립과 행별 실제 평행 이동·중첩 결과를 기록한다.</summary>
        public static void SetGeometry(PanoramaCoordinateMetadata metadata, int width, int height,
            int contribution, IList<int> overlaps, IList<int> offsets)
        {
            if (metadata == null)
                return;

            metadata.GeometryMode = "SHARED_LONGITUDE_ROW_FIRST";
            metadata.FrameWidth = width;
            metadata.FrameHeight = height;
            metadata.ContributionWidth = contribution;
            metadata.OutputWidth = contribution * 36;
            int top = 0;

            for (int i = 0; i < metadata.Rows.Count; i++)
            {
                int overlap = i == 0 ? 0 : Math.Max(24, Math.Min(overlaps[i - 1], height - 1));
                int offset = i == 0 ? 0 : Math.Max(-height + 2, Math.Min(height - 2, offsets[i - 1]));
                if (i > 0)
                    top += height - overlap;
                var row = metadata.Rows[i];
                row.OutputTop = top;
                row.VerticalOverlap = overlap;
                row.VerticalOffset = offset;
                row.FinalCenterY = top + offset + height * metadata.OpticalCenterY;
            }

            metadata.OutputHeight = top + height;
            // X=0은 첫 프레임 광축보다 반 스트립 앞이다. 광학 중심 보정도 같은 픽셀 기준으로 적용한다.
            metadata.StartPan = Wrap(metadata.Rows[0].FramePans[0] -
                (contribution * .5 + (metadata.OpticalCenterX - .5) * width) / contribution * 10);
        }

        public static bool Valid(PanoramaCoordinateMetadata data)
        {
            return data != null && data.FormatVersion == 1 && data.GeometryMode == "SHARED_LONGITUDE_ROW_FIRST" &&
                data.Rows != null && data.Rows.Count >= 1 && data.Rows.Count <= 5 &&
                data.ContributionWidth > 0 && data.OutputWidth == data.ContributionWidth * 36 &&
                data.FrameHeight > 0 && data.OutputHeight > 0 && data.FrameWidth > 0 &&
                Finite(data.ReferenceTilt) && data.CaptureRange > 0 && data.CaptureRange <= 60 &&
                data.VerticalFov > 0 && data.VerticalFov < 180 &&
                data.OpticalCenterX >= 0 && data.OpticalCenterX <= 1 &&
                data.OpticalCenterY >= 0 && data.OpticalCenterY <= 1 &&
                data.Rows.All(row => row != null && row.FramePans != null && row.FramePans.Count == 36 &&
                    row.FramePans.All(Finite) && Finite(row.ActualTilt) && Finite(row.FinalCenterY) &&
                    row.FinalCenterY >= 0 && row.FinalCenterY < data.OutputHeight &&
                    row.FramePans.Zip(row.FramePans.Skip(1), (a, b) => Wrap(b - a) > 0 && Wrap(b - a) < 20).All(value => value)) &&
                data.Rows.Zip(data.Rows.Skip(1), (a, b) => a.ActualTilt > b.ActualTilt &&
                    a.FinalCenterY < b.FinalCenterY).All(value => value);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        /// <summary>프레임 35→0의 순환 경계를 포함해 실제 촬영 팬 각도를 공통 경도 픽셀로 보간한다.</summary>
        private static double MapPan(PanoramaCoordinateMetadata data, PanoramaCoordinateRow row, double pan)
        {
            double delta = Wrap(pan - row.FramePans[0]);
            for (int i = 0; i < 36; i++)
            {
                double left = i == 0 ? 0 : Wrap(row.FramePans[i] - row.FramePans[0]);
                double right = i == 35 ? 360 : Wrap(row.FramePans[i + 1] - row.FramePans[0]);
                if (right <= left || delta < left || delta > right)
                    continue;
                double x = (i + (delta - left) / (right - left) + .5) * data.ContributionWidth +
                    (data.OpticalCenterX - .5) * data.FrameWidth;
                return (x % data.OutputWidth + data.OutputWidth) % data.OutputWidth;
            }
            return double.NaN;
        }

        /// <summary>행 중심 사이에는 정합된 Y를 보간하고, 한 행은 기록한 수직 화각의 투영식을 사용한다.</summary>
        public static PanoramaPresetLink Project(PanoramaCoordinateMetadata data, PresetPointOption preset, double panOffset = 0, double tiltOffset = 0)
        {
            if (!Valid(data) || preset == null || !Finite(preset.Pan) || !Finite(preset.Tilt))
                return null;
            double pan = preset.Pan - panOffset + data.CapturePanOffset;
            double tilt = preset.Tilt - tiltOffset + data.CaptureTiltOffset;
            if (Math.Abs(tilt - data.ReferenceTilt) > data.CaptureRange / 2)
                return null;

            var rows = data.Rows;
            int index = 0;
            while (index < rows.Count - 1 && tilt < rows[index + 1].ActualTilt)
                index++;
            double y;
            double x = MapPan(data, rows[index], pan);
            if (index < rows.Count - 1 && tilt <= rows[index].ActualTilt)
            {
                double ratio = (rows[index].ActualTilt - tilt) /
                    (rows[index].ActualTilt - rows[index + 1].ActualTilt);
                y = rows[index].FinalCenterY + ratio * (rows[index + 1].FinalCenterY - rows[index].FinalCenterY);
                double nextX = MapPan(data, rows[index + 1], pan);
                double dx = (nextX - x + data.OutputWidth * 1.5) % data.OutputWidth - data.OutputWidth / 2.0;
                x = (x + dx * ratio + data.OutputWidth) % data.OutputWidth;
            }
            else
            {
                double focal = data.FrameHeight / (2 * Math.Tan(data.VerticalFov * Math.PI / 360));
                y = rows[index].FinalCenterY - focal * Math.Tan((tilt - rows[index].ActualTilt) * Math.PI / 180);
            }
            if (!Finite(x) || !Finite(y) || y < 0 || y >= data.OutputHeight)
                return null;
            return new PanoramaPresetLink
            {
                Number = preset.Number,
                Pan = preset.Pan,
                Tilt = preset.Tilt,
                Fingerprint = PresetIdentity(preset),
                U = x / data.OutputWidth,
                V = y / data.OutputHeight
            };
        }

        /// <summary>렌즈와 등록 시각으로 재등록을 구분하며 ZERO로 바뀌는 각도는 식별자에서 제외한다.</summary>
        private static string AnchorRegistrationKey(PresetPointOption preset)
        {
            return ContextIdentity(preset.Number + "|" + preset.PositionSnapshot?.CapturedAt.ToString("o") + "|" +
                preset.EoZoomText + "|" + preset.EoFocusText + "|" + preset.IrZoomText + "|" + preset.IrFocusText);
        }

        /// <summary>사진 위치는 고정하고 현재 이동 각도와 지문만 갱신한다. 기준 불일치는 감추지 않고 이동을 막는다.</summary>
        public static PanoramaPresetLink ResolveLink(PanoramaCoordinateMetadata data, PresetPointOption preset,
            double panOffset, double tiltOffset, bool allowCapture, out bool changed)
        {
            changed = false;
            if (data == null || preset == null || !Finite(panOffset) || !Finite(tiltOffset))
                return null;

            string key = AnchorRegistrationKey(preset);
            var anchor = data.PresetAnchors?.FirstOrDefault(item => item.Number == preset.Number);
            if (anchor != null && anchor.RegistrationKey == key && Finite(anchor.U) && Finite(anchor.V) &&
                anchor.U >= 0 && anchor.U < 1 && anchor.V >= 0 && anchor.V < 1)
            {
                double panError = Wrap(preset.Pan - panOffset - anchor.BasePan + 180) - 180;
                double tiltError = preset.Tilt - tiltOffset - anchor.BaseTilt;
                bool samePosition = Math.Abs(panError) <= .03 && Math.Abs(tiltError) <= .03;
                if (!allowCapture || samePosition || data.AutoLinkEnabled != true)
                {
                    return new PanoramaPresetLink
                    {
                        Number = preset.Number,
                        Fingerprint = PresetIdentity(preset),
                        Pan = preset.Pan,
                        Tilt = preset.Tilt,
                        U = anchor.U,
                        V = anchor.V,
                        ReferenceValid = allowCapture && samePosition
                    };
                }

                // 자동 연동 ON의 확인된 기준에서 실제 위치만 바뀌면 그 번호만 다시 투영한다.
                // 기존 수동 자료는 기준 불일치 표시와 이동 차단 규칙을 그대로 유지한다.
                // 정상 ZERO는 현재 각도에서 offset을 빼면 같은 위치이므로 위 앵커를 유지한다.
            }

            // 미확정 ZERO 중에는 과거 파일을 새 기준으로 변환하여 저장하지 않는다.
            if (!allowCapture)
                return null;
            var link = Project(data, preset, panOffset, tiltOffset);
            if (link == null)
                return null;
            if (data.PresetAnchors == null)
                data.PresetAnchors = new List<PanoramaPresetAnchor>();
            if (anchor != null)
                data.PresetAnchors.Remove(anchor);
            data.PresetAnchors.Add(new PanoramaPresetAnchor
            {
                Number = preset.Number,
                RegistrationKey = key,
                BasePan = Wrap(preset.Pan - panOffset),
                BaseTilt = preset.Tilt - tiltOffset,
                U = link.U,
                V = link.V
            });
            changed = true;
            return link;
        }

        public static string ConfigDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "PanoramaLinks");

        public static string ConfigPath(string context, string image) =>
            Path.Combine(ConfigDirectory, ContextIdentity(context + "|" + image) + ".json");

        /// <summary>임시 파일을 디스크에 반영한 뒤 교체하여 비정상 종료 중에도 이전 또는 새 기록을 복원한다.</summary>
        public static void WriteAtomic<T>(string path, T data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, data);
                stream.Flush(true);
            }
            if (File.Exists(path))
                File.Replace(temporary, path, path + ".bak");
            else
                File.Move(temporary, path);
        }

        /// <summary>주 저장 파일이 손상되어도 마지막 정상 백업을 사용하며 임시 파일은 읽지 않는다.</summary>
        public static T ReadAtomic<T>(string path) where T : class
        {
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate))
                    continue;
                try
                {
                    if (new FileInfo(candidate).Length > 8388608)
                        continue;
                    using (var stream = File.OpenRead(candidate))
                        return new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
                }
                catch (Exception ex) when (ex is IOException || ex is SerializationException ||
                    ex is ArgumentException || ex is UnauthorizedAccessException)
                {
                    // 손상 기록으로 정상 연동 상태를 덮어쓰지 않고 이전 백업을 확인한다.
                }
            }
            return null;
        }

        public static string LastImagePath(string context) => Path.Combine(ConfigDirectory, "last_" + ContextIdentity(context) + ".json");

        /// <summary>이미지 자체는 복제하지 않고 마지막 선택 경로만 Config에 즉시 보관한다.</summary>
        public static void RememberImage(string imagePath, string context)
        {
            if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
                WriteAtomic(LastImagePath(context), Path.GetFullPath(imagePath));
        }

        /// <summary>Config를 우선 저장소로 확정한 뒤 JPG 옆 JSON을 함께 갱신한다.</summary>
        public static void Save(string imagePath, PanoramaCoordinateMetadata data)
        {
            data.PanoramaFileName = Path.GetFileName(imagePath);
            data.ImageHash = FileIdentity(imagePath);
            WriteAtomic(ConfigPath(data.ContextHash, data.ImageHash), data);
            // Config 교체가 끝나면 저장은 확정된다. 보조 경로 실패를 토글 실패로 되돌리지 않는다.
            try
            {
                RememberImage(imagePath, data.ContextHash);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                OpenCvWpfTracking.Common.ConsoleLogHelper.Warning("PANORAMA STORAGE",
                    "Config 저장 완료 / 마지막 사진 경로 갱신 실패: " + ex.Message);
            }

            try
            {
                WriteAtomic(Path.ChangeExtension(imagePath, ".json"), data);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                OpenCvWpfTracking.Common.ConsoleLogHelper.Warning("PANORAMA STORAGE",
                    "Config 저장 완료 / 사진 옆 JSON 갱신 실패: " + ex.Message);
            }
        }

        /// <summary>재연결·재시작은 Config에서 먼저 복원하며 과거 Sidecar도 함께 지원한다.</summary>
        public static PanoramaCoordinateMetadata Load(string imagePath, string contextHash, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return null;
            string hash = FileIdentity(imagePath);
            foreach (string path in new[] { ConfigPath(contextHash, hash), Path.ChangeExtension(imagePath, ".json") })
            {
                var data = ReadAtomic<PanoramaCoordinateMetadata>(path);
                if (data == null || data.OutputWidth != width || data.OutputHeight != height || data.ImageHash != hash)
                    continue;
                if (data.ContextHash != contextHash)
                {
                    if (data.GeometryMode != "INVALIDATED_REFERENCE")
                        continue;
                    data.ContextHash = contextHash;
                    data.ReferenceUncertain = true;
                }
                // 이전 빌드에서 HOME으로 무효화된 기록도 지우지 않는다. 실제 ZERO 여부는 별도 확인한다.
                if (data.GeometryMode == "INVALIDATED_REFERENCE")
                {
                    data.GeometryMode = "SHARED_LONGITUDE_ROW_FIRST";
                    data.ReferenceUncertain = true;
                }
                if (Valid(data))
                {
                    WriteAtomic(ConfigPath(contextHash, hash), data);
                    return data;
                }
            }
            return null;
        }
    }
}
