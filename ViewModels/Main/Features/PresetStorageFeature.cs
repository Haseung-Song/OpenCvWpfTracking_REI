using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Models.Position;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvWpfTracking.Services.Configuration;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>장비별 프리셋 저장에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IPresetStorageFeatureContext
    {
        bool _isLoadingPresetStorage { get; set; }

        bool _isLoadingRtspCommunicationSettings { get; set; }

        string _laPresetCommandStatusText { get; set; }

        int _laPresetScanDelay { get; set; }

        int _laPresetScanSpeed { get; set; }

        int _laPresetSlotNumber { get; set; }

        string _presetCommandStatusText { get; set; }

        int _presetScanDelay { get; set; }

        PresetScanOrderMode _presetScanOrderMode { get; set; }

        int _presetScanSpeed { get; set; }

        int _presetSlotNumber { get; set; }

        System.Collections.ObjectModel.ObservableCollection<ControlAgentProfileOption> ControlAgentProfiles { get; }

        string EoSourceAddress { get; set; }

        void InvalidatePanoramaReference();

        string IrSourceAddress { get; set; }

        bool IsPresetNearestOrderMode { get; set; }

        bool IsPresetPanOrderMode { get; set; }

        bool IsPresetPingPong { get; set; }

        bool IsPresetSavedOrderMode { get; set; }

        bool IsSelectedControlAgentWebAgent { get; }

        string LaPresetCommandStatusText { get; set; }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> LaPresetPoints { get; }

        int LaPresetScanDelay { get; set; }

        int LaPresetScanSpeed { get; set; }

        int LaPresetSlotNumber { get; set; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanoramaDeviceKey { get; }

        string PresetCommandStatusText { get; set; }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> PresetPoints { get; }

        int PresetScanDelay { get; set; }

        int PresetScanSpeed { get; set; }

        int PresetSlotNumber { get; set; }

        void RecoverPanoramaPresetReference();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        PresetPointOption SelectedLaPresetPoint { get; set; }

        PresetPointOption SelectedPresetPoint { get; set; }
    }

    /// <summary>장비별 프리셋 저장 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PresetStorageFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPresetStorageFeatureContext _context;

        internal PresetStorageFeature(IPresetStorageFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal string _presetEquipmentContext;

        internal string _presetEquipmentDirectory;

        internal bool _presetStorageReady;

        internal bool _applyingPresetProfile;

        internal bool _presetStorageLoadFailed;

        #endregion

        #region [화면 속성과 이벤트]

        internal string PresetEquipmentContext
        {
            get
            {
                if (HasRegisteredEquipmentIdentity)
                {
                    return RegisteredEquipmentIdentity;
                }
                else
                {
                    return LegacyPresetEquipmentContext;
                }
            }
        }

        internal bool HasRegisteredEquipmentIdentity
        {
            get
            {
                return _context.SelectedControlAgentProfile != null && !_context.SelectedControlAgentProfile.IsDirectInput && !string.IsNullOrWhiteSpace(_context.SelectedControlAgentProfile.DeviceId);
            }
        }

        internal string RegisteredEquipmentIdentity
        {
            get
            {
                return EquipmentStorage.Identity(_context.SelectedControlAgentProfile?.DeviceId, _context.IsSelectedControlAgentWebAgent);
            }
        }

        internal string LegacyPresetEquipmentContext
        {
            get
            {
                return (_context.SelectedControlAgentProfile?.AgentType.ToString() ?? "Unknown") + "|" + _context.PanoramaDeviceKey + "|" + PresetCameraIdentity(_context.EoSourceAddress) + "|" + PresetCameraIdentity(_context.IrSourceAddress);
            }
        }

        internal string PresetStoragePath
        {
            get
            {
                return Path.Combine(_presetEquipmentDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "EquipmentPresets", PresetRowHash(new[] { _presetEquipmentContext ?? PresetEquipmentContext })), "Presets.csv");
            }
        }

        #endregion

        #region [동작 처리]

        internal static string PresetCameraIdentity(string address)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri))
            {
                return address ?? string.Empty;
            }

            return uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.UriEscaped);
        }

        internal void InitializeEquipmentPresetFolders()
        {
            int prepared = 0, attempted = 0;

            foreach (var profile in _context.ControlAgentProfiles.Where(item => !item.IsDirectInput))
            {
                attempted++;

                try
                {
                    string key = DeviceCatalog.StorageDisplayName(profile.DisplayName) + "|" + profile.IpAddress + ":" + profile.Port;

                    var entry = DeviceCatalog.Get(profile.DeviceId);

                    string legacy = profile.AgentType + "|" + key + "|" + PresetCameraIdentity(entry.EoAddress) + "|" + PresetCameraIdentity(entry.IrAddress);

                    EquipmentStorage.ResolvePresets(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "EquipmentPresets"), profile.DeviceId, profile.AgentType == ControlAgentType.WebAgent, profile.DisplayName, PresetRowHash(new[] { legacy }), PresetRowHash(new[] { NormalizeLegacyPresetContext(legacy) }));

                    prepared++;
                }
                catch (IOException ex)
                {
                    ConsoleLogHelper.Warning("PRESET STORAGE", "장비명 폴더 준비 실패 / 원본 유지: " + ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    ConsoleLogHelper.Warning("PRESET STORAGE", "장비명 폴더 접근 확인 필요: " + ex.Message);
                }
            }

            ConsoleLogHelper.State("PRESET STORAGE", $"장비별 프리셋 경로 준비 / {prepared}/{attempted}개 확인");
        }

        internal string ResolveEquipmentPresetDirectory(string context)
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "EquipmentPresets");

            if (HasRegisteredEquipmentIdentity)
            {
                string legacy = LegacyPresetEquipmentContext;

                return EquipmentStorage.ResolvePresets(root, _context.SelectedControlAgentProfile.DeviceId, _context.IsSelectedControlAgentWebAgent, _context.SelectedControlAgentProfile.DisplayName, PresetRowHash(new[] { legacy }), PresetRowHash(new[] { NormalizeLegacyPresetContext(legacy) }));
            }

            string stem = OpenCvWpfTracking.Services.Video.PanoramaPresetRegistration.DeviceDirectory(root, _context.IsSelectedControlAgentWebAgent, _context.PanoramaDeviceKey, _context.SelectedControlAgentProfile?.DisplayName);

            string legacyIdentity = PresetRowHash(new[] { context });

            // Config 행 추가로 표시 순번이 바뀌어도 같은 장비의 폴더를 유지한다.
            string stableContext = NormalizeLegacyPresetContext(context);

            string identity = PresetRowHash(new[] { stableContext });

            for (int variant = 1; variant <= 100; variant++)
            {
                string directory = variant == 1 ? stem : stem + "_" + variant.ToString(CultureInfo.InvariantCulture);

                Directory.CreateDirectory(directory);

                string owner = Path.Combine(directory, ".preset-device");

                if (File.Exists(owner))
                {
                    string previousIdentity = File.ReadAllText(owner);

                    if (previousIdentity != identity)
                    {
                        if (previousIdentity != legacyIdentity)
                        {
                            continue;
                        }

                        string temporaryOwner = owner + ".tmp";

                        File.WriteAllText(temporaryOwner, identity, new UTF8Encoding(false));

                        File.Replace(temporaryOwner, owner, owner + ".bak");
                    }
                }
                else
                {
                    // 소유 정보가 없는 기존 프리셋을 다른 장비 자료로 임의 채택하지 않는다.
                    if (File.Exists(Path.Combine(directory, "Presets.csv")) || File.Exists(Path.Combine(directory, "Presets.tsv")))
                    {
                        continue;
                    }

                    try
                    {
                        using (var stream = new FileStream(owner, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                        {
                            writer.Write(identity);
                        }
                    }
                    catch (IOException)
                    {
                        if (!File.Exists(owner) || File.ReadAllText(owner) != identity)
                        {
                            continue;
                        }
                    }
                }

                RestoreLegacyEquipmentPresets(Path.Combine(root, legacyIdentity), directory);

                return directory;
            }

            throw new IOException("장비별 프리셋 폴더 소유 정보를 확인하세요.");
        }

        internal static string NormalizeLegacyPresetContext(string context)
        {
            return System.Text.RegularExpressions.Regex.Replace(context, @"^([^|]+\|)\s*\d+\.\s*", "$1");
        }

        internal static void RestoreLegacyEquipmentPresets(string legacy, string directory)
        {
            if (!Directory.Exists(legacy) || File.Exists(Path.Combine(directory, "Presets.csv")) || File.Exists(Path.Combine(directory, "Presets.tsv")))
            {
                return;
            }

            // 위치 메타데이터를 먼저 복사하고 CSV를 마지막에 확정한다.
            foreach (string name in new[]
            {
                "PresetPositions.xml",
                "PresetPositions.xml.bak",
                "Presets.csv.bak",
                "Presets.tsv.bak",
                "Presets.tsv",
                "Presets.csv"
            }

            )
            {
                if (name == "Presets.tsv" && File.Exists(Path.Combine(legacy, "Presets.csv")))
                {
                    continue;
                }

                string source = Path.Combine(legacy, name);

                if (!File.Exists(source))
                {
                    continue;
                }

                string destination = Path.Combine(directory, name);

                if (File.Exists(destination))
                {
                    if (!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))
                    {
                        throw new IOException("이전 프리셋 복사본 확인 필요 / 원본 유지");
                    }

                    continue;
                }

                string temporary = destination + ".importing";

                File.Copy(source, temporary, true);

                File.Move(temporary, destination);
            }

            ConsoleLogHelper.State("PRESET STORAGE", "기존 해시 폴더 자료 복원 / 장비명 폴더 사용 / 이전 원본 보존");
        }

        internal void SwitchEquipmentPresets()
        {
            if (!_presetStorageReady || _applyingPresetProfile || _context._isLoadingRtspCommunicationSettings)
            {
                return;
            }

            string context = PresetEquipmentContext;

            if (string.Equals(_presetEquipmentContext, context, StringComparison.Ordinal))
            {
                return;
            }

            // 저장 경로는 이전 장비 문맥을 유지한 채 저장한 뒤 전환한다.
            if (_presetEquipmentContext != null && !SavePresetStorage())
            {
                ConsoleLogHelper.State("PRESET EQUIPMENT", "이전 장비 저장 실패 / 원본 파일 확인 필요");
            }

            _context._isLoadingPresetStorage = true;

            try
            {
                _context.SelectedLaPresetPoint = null;
                _context.SelectedPresetPoint = null;

                _context.LaPresetPoints.Clear();

                _context.PresetPoints.Clear();

                _presetEquipmentContext = context;
                _presetEquipmentDirectory = null;
                _presetStorageLoadFailed = false;

                _context._laPresetSlotNumber = 1;
                _context._presetSlotNumber = 1;
                _context._laPresetCommandStatusText = "READY";
                _context._presetCommandStatusText = "READY";
                _context._presetScanOrderMode = PresetScanOrderMode.SavedOrder;
                _context._laPresetScanSpeed = 10;
                _context._presetScanSpeed = 10;
                _context._laPresetScanDelay = 1;
                _context._presetScanDelay = 1;
            }
            finally
            {
                _context._isLoadingPresetStorage = false;
            }

            try
            {
                _presetEquipmentDirectory = ResolveEquipmentPresetDirectory(context);

                LoadPresetStorage();

                _context.RecoverPanoramaPresetReference();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _presetStorageLoadFailed = true;

                ConsoleLogHelper.Error("PRESET STORAGE", "장비명 폴더 준비 실패 / 원본 유지 / 저장 보류", ex);
            }

            foreach (string name in new[]
            {
                nameof(_context.IsPresetSavedOrderMode),
                nameof(_context.IsPresetNearestOrderMode),
                nameof(_context.LaPresetScanSpeed),
                nameof(_context.PresetScanSpeed),
                nameof(_context.LaPresetScanDelay),
                nameof(_context.PresetScanDelay),
                nameof(_context.LaPresetSlotNumber),
                nameof(_context.PresetSlotNumber),
                nameof(_context.LaPresetCommandStatusText),
                nameof(_context.PresetCommandStatusText)
            }

            )
            {
                _context.OnPropertyChanged(name);
            }

            ConsoleLogHelper.StateSection("PRESET EQUIPMENT", "장비별 프리셋 전환", string.Empty, "장비: " + _context.SelectedControlAgentProfile?.DisplayName, "목록: LA " + _context.LaPresetPoints.Count + "개 / WEB " + _context.PresetPoints.Count + "개", "저장 위치: " + PresetStoragePath);

            _context.InvalidatePanoramaReference();
        }

        internal void LoadPresetStorage()
        {
            try
            {
                string legacyPath = Path.ChangeExtension(PresetStoragePath, "tsv");

                bool legacy = !File.Exists(PresetStoragePath);

                string loadPath = legacy ? legacyPath : PresetStoragePath;

                if (!File.Exists(loadPath))
                {
                    return;
                }

                _context._isLoadingPresetStorage = true;

                var rows = legacy ? File.ReadAllLines(loadPath).Select(line => line.Split('\t')).ToList() : DeviceCatalog.Parse(File.ReadAllText(loadPath, Encoding.UTF8)).Select(row => row.ToArray()).ToList();

                if (rows.Count == 0 || rows[0].Length == 0 || rows[0][0] != "TYPE")
                {
                    throw new InvalidDataException("프리셋 파일 헤더 확인 필요");
                }

                var positions = ReadPresetPositions();

                int coordinateColumn = rows.Count == 0 ? -1 : Array.IndexOf(rows[0], "IR_ZOOM_COORDINATES");

                foreach (string[] p in rows.Skip(1))
                {
                    if (p.Length >= 8 && p[0] == "SETTINGS")
                    {
                        if (Enum.TryParse(p[3], out PresetScanOrderMode mode))
                        {
                            _context._presetScanOrderMode = mode;
                        }

                        if (int.TryParse(p[4], out int laSpeed))
                        {
                            _context._laPresetScanSpeed = ClampPresetScanSetting(laSpeed);
                        }

                        if (int.TryParse(p[5], out int laDelay))
                        {
                            _context._laPresetScanDelay = ClampPresetScanSetting(laDelay);
                        }

                        if (int.TryParse(p[6], out int webSpeed))
                        {
                            _context._presetScanSpeed = ClampPresetScanSetting(webSpeed);
                        }

                        if (int.TryParse(p[7], out int webDelay))
                        {
                            _context._presetScanDelay = ClampPresetScanSetting(webDelay);
                        }

                        continue;
                    }

                    if (p.Length < 10 || (p[0] != "L" && p[0] != "W"))
                    {
                        continue;
                    }

                    if (!int.TryParse(p[1], out int order) || !int.TryParse(p[2], out int number) || !double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double pan) || !double.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double tilt))
                    {
                        continue;
                    }

                    if (p[0] == "W")
                    {
                        tilt = NormalizeUnsignedWebAgentTilt(tilt, 0.0);
                    }

                    PositionSnapshot.TryParseExportFields(p, 10, out PositionSnapshot positionSnapshot);

                    string[] savedPosition;

                    if (positionSnapshot == null && p.Length >= 12 && positions.TryGetValue(PresetRowHash(p), out savedPosition))
                    {
                        PositionSnapshot.TryParseExportFields(savedPosition, 0, out positionSnapshot);
                    }

                    PresetPointOption preset = new PresetPointOption(number, pan, tilt, p[6], p[7], p[8], p[9], p[3], order, positionSnapshot);

                    preset.UsesLegacyIrZoomCalibration = p[0] == "W" && (coordinateColumn < 0 || p.Length <= coordinateColumn || p[coordinateColumn] != "DEVICE_POSITION");

                    if (p[0] == "L")
                    {
                        _context.LaPresetPoints.Add(preset);
                    }
                    else
                    {
                        _context.PresetPoints.Add(preset);
                    }
                }

                ReorderPresetCollections();

                _context.SelectedLaPresetPoint = _context.LaPresetPoints.FirstOrDefault();

                _context.SelectedPresetPoint = _context.PresetPoints.FirstOrDefault();

                ConsoleLogHelper.State("PRESET STORAGE", "Loaded / PATH=" + loadPath);

                // 이전 파일을 덮어쓰거나 지우지 않으며 이후 시작부터 CSV를 우선한다.
                if (legacy || (coordinateColumn < 0 && rows.FirstOrDefault()?.Length > 12))
                {
                    _context._isLoadingPresetStorage = false;

                    SavePresetStorage();
                }
                else
                {
                    CsvExcelCompanion.Schedule(PresetStoragePath);
                }
            }
            catch (Exception ex)
            {
                _presetStorageLoadFailed = true;

                _context.LaPresetPoints.Clear();

                _context.PresetPoints.Clear();

                _context.SelectedLaPresetPoint = null;
                _context.SelectedPresetPoint = null;

                ConsoleLogHelper.Error("PRESET STORAGE", "읽기 실패 / 원본 보존 / 해당 장비 등록·복원 확인 필요", ex);
            }
            finally
            {
                _context._isLoadingPresetStorage = false;
            }
        }

        internal bool SavePresetStorage()
        {
            // 읽기 실패한 파일을 빈 목록으로 덮어쓰지 않는다.
            if (!_presetStorageReady || _presetEquipmentContext == null || _context._isLoadingPresetStorage || _applyingPresetProfile || _presetStorageLoadFailed)
            {
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(PresetStoragePath);

                Directory.CreateDirectory(directory);

                string temporaryPath = PresetStoragePath + ".tmp";

                List<string> lines = new List<string>
                {
                    "TYPE\tSAVED_ORDER\tNUMBER\tNAME_OR_MODE\tPAN_OR_L_SPEED\tTILT_OR_L_DELAY\tEO_ZOOM_OR_W_SPEED\tEO_FOCUS_OR_W_DELAY\tIR_ZOOM\tIR_FOCUS\t" + "POSITION_CAPTURED_AT\tPOSITION_SOURCE\tIR_ZOOM_COORDINATES",
                    string.Join("\t", new[] { "SETTINGS", "0", "0", _context._presetScanOrderMode.ToString(), _context._laPresetScanSpeed.ToString(), _context._laPresetScanDelay.ToString(), _context._presetScanSpeed.ToString(), _context._presetScanDelay.ToString(), "-", "-", "", "" })
                };

                var positions = new XElement("PresetPositions", new XAttribute("Version", 1));

                AppendPresetStorageLines(lines, "L", _context.LaPresetPoints, positions);

                AppendPresetStorageLines(lines, "W", _context.PresetPoints, positions);

                // 헤더·설정은 탭 구분이고 데이터 행은 따옴표를 지원하는 CSV이다.
                lines[0] = lines[0].Replace('\t', ',');

                lines[1] = lines[1].Replace('\t', ',');

                File.WriteAllLines(temporaryPath, lines, new UTF8Encoding(true));

                // 메타데이터를 먼저 저장해 두 번째 저장 실패 시 이전 CSV에 맞는 복구본을 남긴다.
                string positionPath = Path.Combine(directory, "PresetPositions.xml"), positionTemp = positionPath + ".tmp";

                new XDocument(positions).Save(positionTemp);

                if (File.Exists(positionPath))
                {
                    File.Replace(positionTemp, positionPath, positionPath + ".bak");
                }
                else
                {
                    File.Move(positionTemp, positionPath);
                }

                if (File.Exists(PresetStoragePath))
                {
                    File.Replace(temporaryPath, PresetStoragePath, PresetStoragePath + ".bak");
                }
                else
                {
                    File.Move(temporaryPath, PresetStoragePath);
                }

                ConsoleLogHelper.State("PRESET STORAGE", "Saved CSV / PATH=" + PresetStoragePath);

                CsvExcelCompanion.Create(PresetStoragePath);

                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("PRESET STORAGE", "Save failed", ex);

                return false;
            }
        }

        internal static void AppendPresetStorageLines(ICollection<string> lines, string type, IEnumerable<PresetPointOption> presets, XElement positions)
        {
            foreach (PresetPointOption preset in presets.OrderBy(p => p.SavedOrder))
            {
                string safeName = (preset.Name ?? $"P{preset.Number:00}").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");

                List<string> fields = new List<string>(new[] { type, preset.SavedOrder.ToString(CultureInfo.InvariantCulture), preset.Number.ToString(CultureInfo.InvariantCulture), safeName, preset.Pan.ToString("R", CultureInfo.InvariantCulture), preset.Tilt.ToString("R", CultureInfo.InvariantCulture), preset.EoZoomText, preset.EoFocusText, preset.IrZoomText, preset.IrFocusText });

                string[] positionFields = preset.PositionSnapshot?.ToExportFields() ?? Enumerable.Repeat(string.Empty, PositionSnapshot.ExportFieldCount).ToArray();

                fields.AddRange(positionFields.Take(2).Select(MainViewModel.SanitizePresetStorageField));

                fields.Add(preset.UsesLegacyIrZoomCalibration ? "LEGACY_SYNC" : "DEVICE_POSITION");

                if (preset.PositionSnapshot != null)
                {
                    positions.Add(new XElement("Preset", new XAttribute("Hash", PresetRowHash(fields)), positionFields.Select(v => new XElement("Field", v ?? ""))));
                }

                lines.Add(string.Join(",", fields.Select(value => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"")));
            }
        }

        internal static string PresetRowHash(IEnumerable<string> fields)
        {
            string canonical = string.Join(",", fields.Take(12).Select(v => "\"" + (v ?? "").Replace("\"", "\"\"") + "\""));

            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", "");
            }
        }

        internal Dictionary<string, string[]> ReadPresetPositions()
        {
            var result = new Dictionary<string, string[]>();

            string path = Path.Combine(Path.GetDirectoryName(PresetStoragePath), "PresetPositions.xml");

            foreach (string candidate in new[]
            {
                path,
                path + ".bak"
            }

            )
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                try
                {
                    if (new FileInfo(candidate).Length > 4000000)
                    {
                        throw new InvalidDataException("Preset metadata too large.");
                    }

                    using (var reader = XmlReader.Create(candidate, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    {
                        var data = XDocument.Load(reader);

                        if (data.Root?.Name != "PresetPositions" || (string)data.Root.Attribute("Version") != "1")
                        {
                            throw new InvalidDataException("Preset metadata version.");
                        }

                        foreach (var item in data.Root.Elements("Preset"))
                        {
                            string hash = (string)item.Attribute("Hash");

                            var values = item.Elements("Field").Select(e => e.Value).ToArray();

                            if (hash != null && hash.Length == 64 && values.Length == PositionSnapshot.ExportFieldCount && !result.ContainsKey(hash))
                            {
                                result.Add(hash, values);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.Error("PRESET POSITION", "위치 메타데이터 읽기 실패 / 해당 위치 연동 보류", ex);
                }
            }

            return result;
        }

        internal static string SanitizePresetStorageField(string value)
        {
            return (value ?? string.Empty).Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        internal void PreparePresetForUpsert(PresetPointOption newPreset, IEnumerable<PresetPointOption> presets, PresetPointOption existingPreset)
        {
            newPreset.SavedOrder = existingPreset != null ? existingPreset.SavedOrder : presets.Select(p => p.SavedOrder).DefaultIfEmpty(0).Max() + 1;
        }

        internal void ReorderPresetCollections()
        {
            ReplacePresetOrder(_context.LaPresetPoints, _context.LaPresetPoints.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray());

            ReplacePresetOrder(_context.PresetPoints, _context.PresetPoints.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray());
        }

        internal static void ReplacePresetOrder(System.Collections.ObjectModel.ObservableCollection<PresetPointOption> target, IEnumerable<PresetPointOption> ordered)
        {
            PresetPointOption[] snapshot = ordered.ToArray();

            target.Clear();

            foreach (PresetPointOption preset in snapshot)
            {
                target.Add(preset);
            }
        }

        internal PresetPointOption[] CreatePresetScanQueue(IEnumerable<PresetPointOption> source)
        {
            PresetPointOption[] registered = source.ToArray();

            // 2026-10-01: 본 GUI에서 NEAREST는 PAN/TILT 공간거리가 아니라
            // 등록된 PRESET 번호의 오름차순을 의미한다. SAVED ORDER는 사용자가
            // 저장한 순서를 그대로 보존하며 기본 모드로 유지한다.
            PresetPointOption[] result = _context._presetScanOrderMode == PresetScanOrderMode.SavedOrder ? registered.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray() : registered.OrderBy(p => p.Number).ThenBy(p => p.SavedOrder).ToArray();

            if (_context.IsPresetPanOrderMode)
            {
                result = result.OrderBy(p => NormalizeToSignedAngle(p.Pan)).ThenBy(p => p.Tilt).ToArray();
            }

            if (_context.IsPresetPingPong && result.Length > 2)
            {
                result = result.Concat(result.Reverse().Skip(1).Take(result.Length - 2)).ToArray();
            }

            ConsoleLogHelper.State("PRESET SCAN ORDER", "MODE=" + _context._presetScanOrderMode + " / REGISTERED=" + string.Join("->", registered.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).Select(p => p.Number)) + " / EXECUTION=" + string.Join("->", result.Select(p => p.Number)));

            return result;
        }

        internal static int ClampPresetScanSetting(int value)
        {
            return Math.Max(1, Math.Min(60, value));
        }

        #endregion

    }
}
