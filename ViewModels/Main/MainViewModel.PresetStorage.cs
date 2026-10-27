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

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private string PresetStoragePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Presets.csv");

        /// <summary>
        /// 기존 프리셋 파일과 CSV 저장 정보를 읽어 현재 프리셋 목록을 복원한다.
        /// </summary>
        private void LoadPresetStorage()
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

                _isLoadingPresetStorage = true;
                var rows = legacy ? File.ReadAllLines(loadPath).Select(line => line.Split('\t')).ToList() : DeviceCatalog.Parse(File.ReadAllText(
                    loadPath,
                    Encoding.UTF8
                )).Select(row => row.ToArray()).ToList();
                var positions = ReadPresetPositions();
                int coordinateColumn = rows.Count == 0 ? -1 : Array.IndexOf(rows[0], "IR_ZOOM_COORDINATES");

                foreach (string[] p in rows.Skip(1))
                {
                    if (p.Length >= 8 && p[0] == "SETTINGS")
                    {
                        if (Enum.TryParse(p[3], out PresetScanOrderMode mode))
                            _presetScanOrderMode = mode;

                        if (int.TryParse(p[4], out int laSpeed))
                            _laPresetScanSpeed = ClampPresetScanSetting(laSpeed);

                        if (int.TryParse(p[5], out int laDelay))
                            _laPresetScanDelay = ClampPresetScanSetting(laDelay);

                        if (int.TryParse(p[6], out int webSpeed))
                            _presetScanSpeed = ClampPresetScanSetting(webSpeed);

                        if (int.TryParse(p[7], out int webDelay))
                            _presetScanDelay = ClampPresetScanSetting(webDelay);

                        continue;
                    }

                    if (p.Length < 10 || (p[0] != "L" && p[0] != "W"))
                    {
                        continue;
                    }

                    if (!int.TryParse(
                        p[1],
                        out int order
                    ) ||
                        !int.TryParse(
                        p[2],
                        out int number
                    ) ||
                        !double.TryParse(
                        p[4],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double pan
                    ) ||
                        !double.TryParse(
                        p[5],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double tilt
                    ))
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
                        PositionSnapshot.TryParseExportFields(savedPosition, 0, out positionSnapshot);

                    PresetPointOption preset = new PresetPointOption(
                        number,
                        pan,
                        tilt,
                        p[6],
                        p[7],
                        p[8],
                        p[9],
                        p[3],
                        order,
                        positionSnapshot
                    );

                    preset.UsesLegacyIrZoomCalibration = p[0] == "W" &&
                        (coordinateColumn < 0 || p.Length <= coordinateColumn || p[coordinateColumn] != "DEVICE_POSITION");

                    if (p[0] == "L")
                    {
                        LaPresetPoints.Add(preset);
                    }
                    else
                    {
                        PresetPoints.Add(preset);
                    }
                }

                ReorderPresetCollections();
                SelectedLaPresetPoint = LaPresetPoints.FirstOrDefault();
                SelectedPresetPoint = PresetPoints.FirstOrDefault();
                ConsoleLogHelper.State("PRESET STORAGE", "Loaded / PATH=" + loadPath);

                // 이전 파일을 덮어쓰거나 지우지 않으며 이후 시작부터 CSV를 우선한다.
                if (legacy || (coordinateColumn < 0 && rows.FirstOrDefault()?.Length > 12))
                {
                    _isLoadingPresetStorage = false;
                    SavePresetStorage();
                }
                else
                    CsvExcelCompanion.Schedule(PresetStoragePath);
            }
            catch (Exception ex)
            {
                LaPresetPoints.Clear();
                PresetPoints.Clear();
                ConsoleLogHelper.Error("PRESET STORAGE", "Load failed; empty lists retained", ex);
            }
            finally
            {
                _isLoadingPresetStorage = false;
            }
        }

        /// <summary>
        /// 현재 프리셋과 순서 정보를 저장하며 기존 파일 보존 규칙을 적용한다.
        /// </summary>
        private bool SavePresetStorage()
        {
            if (_isLoadingPresetStorage)
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
                    string.Join(
                        "\t",
                        new[] { "SETTINGS", "0", "0", _presetScanOrderMode.ToString(), _laPresetScanSpeed.ToString(), _laPresetScanDelay.ToString(), _presetScanSpeed.ToString(), _presetScanDelay.ToString(), "-", "-", "", "" }
                    )
                };
                var positions = new XElement("PresetPositions", new XAttribute("Version", 1));
                AppendPresetStorageLines(lines, "L", LaPresetPoints, positions);
                AppendPresetStorageLines(lines, "W", PresetPoints, positions);
                // 헤더·설정은 탭 구분이고 데이터 행은 따옴표를 지원하는 CSV이다.
                lines[0] = lines[0].Replace('\t', ',');
                lines[1] = lines[1].Replace('\t', ',');
                File.WriteAllLines(temporaryPath, lines, new UTF8Encoding(true));
                // 메타데이터를 먼저 저장해 두 번째 저장 실패 시 이전 CSV에 맞는 복구본을 남긴다.
                string positionPath = Path.Combine(directory, "PresetPositions.xml"), positionTemp = positionPath + ".tmp";
                new XDocument(positions).Save(positionTemp);

                if (File.Exists(positionPath))
                    File.Replace(positionTemp, positionPath, positionPath + ".bak");
                else
                    File.Move(positionTemp, positionPath);

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

        private static void AppendPresetStorageLines(
            ICollection<string> lines,
            string type,
            IEnumerable<PresetPointOption> presets,
            XElement positions
        )
        {
            foreach (PresetPointOption preset in presets.OrderBy(p => p.SavedOrder))
            {
                string safeName = (preset.Name ?? $"P{preset.Number:00}").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
                List<string> fields = new List<string>(new[] { type, preset.SavedOrder.ToString(CultureInfo.InvariantCulture), preset.Number.ToString(CultureInfo.InvariantCulture), safeName, preset.Pan.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                ), preset.Tilt.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                ), preset.EoZoomText, preset.EoFocusText, preset.IrZoomText, preset.IrFocusText });
                string[] positionFields = preset.PositionSnapshot?.ToExportFields() ?? Enumerable.Repeat(
                    string.Empty,
                    PositionSnapshot.ExportFieldCount
                ).ToArray();
                fields.AddRange(positionFields.Take(2).Select(SanitizePresetStorageField));
                fields.Add(preset.UsesLegacyIrZoomCalibration ? "LEGACY_SYNC" : "DEVICE_POSITION");

                if (preset.PositionSnapshot != null)
                    positions.Add(new XElement(
                        "Preset",
                        new XAttribute(
                        "Hash",
                        PresetRowHash(fields)
                    ),
                        positionFields.Select(v => new XElement(
                        "Field",
                        v ?? ""
                    ))
                    ));

                lines.Add(string.Join(",", fields.Select(value => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"")));
            }
        }

        private static string PresetRowHash(IEnumerable<string> fields)
        {
            string canonical = string.Join(",", fields.Take(12).Select(v => "\"" + (v ?? "").Replace("\"", "\"\"") + "\""));
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", "");
        }

        /// <summary>
        /// 저장된 프리셋 위치 행을 해석해 프리셋 객체로 변환한다.
        /// </summary>
        private Dictionary<string, string[]> ReadPresetPositions()
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
                    continue;

                try
                {
                    if (new FileInfo(candidate).Length > 4000000)
                        throw new InvalidDataException("Preset metadata too large.");

                    using (var reader = XmlReader.Create(
                        candidate,
                        new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
                    ))
                    {
                        var data = XDocument.Load(reader);

                        if (data.Root?.Name != "PresetPositions" || (string)data.Root.Attribute("Version") != "1")
                            throw new InvalidDataException("Preset metadata version.");

                        foreach (var item in data.Root.Elements("Preset"))
                        {
                            string hash = (string)item.Attribute("Hash");
                            var values = item.Elements("Field").Select(e => e.Value).ToArray();

                            if (hash != null &&
                                hash.Length == 64 &&
                                values.Length == PositionSnapshot.ExportFieldCount &&
                                !result.ContainsKey(hash))
                                result.Add(hash, values);
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

        /// <summary>
        /// 프리셋 저장 필드에 허용하지 않는 구분·제어 문자를 정리한다.
        /// </summary>
        private static string SanitizePresetStorageField(string value)
        {
            return (value ?? string.Empty).Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        /// <summary>
        /// 프리셋 추가·갱신 전에 저장에 필요한 항목을 준비한다.
        /// </summary>
        private void PreparePresetForUpsert(
            PresetPointOption newPreset,
            IEnumerable<PresetPointOption> presets,
            PresetPointOption existingPreset
        )
        {
            newPreset.SavedOrder = existingPreset != null ? existingPreset.SavedOrder : presets.Select(p => p.SavedOrder).DefaultIfEmpty(0).Max() + 1;
        }

        /// <summary>
        /// 저장된 순서에 맞춰 화면 프리셋 목록을 정렬한다.
        /// </summary>
        private void ReorderPresetCollections()
        {
            ReplacePresetOrder(LaPresetPoints, LaPresetPoints.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray());
            ReplacePresetOrder(PresetPoints, PresetPoints.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray());
        }

        /// <summary>
        /// 프리셋 순서 정보를 새 목록으로 교체한다.
        /// </summary>
        private static void ReplacePresetOrder(System.Collections.ObjectModel.ObservableCollection<PresetPointOption> target, IEnumerable<PresetPointOption> ordered)
        {
            PresetPointOption[] snapshot = ordered.ToArray();
            target.Clear();

            foreach (PresetPointOption preset in snapshot)
            {
                target.Add(preset);
            }
        }

        /// <summary>
        /// 현재 프리셋과 설정된 순회 순서로 자동 감시 대기열을 만든다.
        /// </summary>
        private PresetPointOption[] CreatePresetScanQueue(IEnumerable<PresetPointOption> source)
        {
            PresetPointOption[] registered = source.ToArray();
            // 2026-10-01: 본 GUI에서 NEAREST는 PAN/TILT 공간거리가 아니라
            // 등록된 PRESET 번호의 오름차순을 의미한다. SAVED ORDER는 사용자가
            // 저장한 순서를 그대로 보존하며 기본 모드로 유지한다.
            PresetPointOption[] result = _presetScanOrderMode == PresetScanOrderMode.SavedOrder ? registered.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray() : registered.OrderBy(p => p.Number).ThenBy(p => p.SavedOrder).ToArray();

            if (IsPresetPanOrderMode)
                result = result.OrderBy(p => NormalizeToSignedAngle(p.Pan)).ThenBy(p => p.Tilt).ToArray();

            if (IsPresetPingPong && result.Length > 2)
                result = result.Concat(result.Reverse().Skip(1).Take(result.Length - 2)).ToArray();

            ConsoleLogHelper.State(
                "PRESET SCAN ORDER",
                "MODE=" + _presetScanOrderMode + " / REGISTERED=" + string.Join(
                "->",
                registered.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).Select(p => p.Number)
            ) + " / EXECUTION=" + string.Join(
                "->",
                result.Select(p => p.Number)
            )
            );
            return result;
        }

        private static int ClampPresetScanSetting(int value) => Math.Max(1, Math.Min(60, value));
    }
}
