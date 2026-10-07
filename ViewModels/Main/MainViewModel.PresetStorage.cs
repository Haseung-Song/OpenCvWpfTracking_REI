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
        private string PresetStoragePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Presets.csv");

        private void LoadPresetStorage()
        {
            try
            {
                string legacyPath=Path.ChangeExtension(PresetStoragePath,"tsv");
                bool legacy=!File.Exists(PresetStoragePath);
                string loadPath=legacy?legacyPath:PresetStoragePath;
                if (!File.Exists(loadPath))
                {
                    return;
                }

                _isLoadingPresetStorage = true;
                var rows=legacy ? File.ReadAllLines(loadPath).Select(line=>line.Split('\t')).ToList() :
                    DeviceCatalog.Parse(File.ReadAllText(loadPath,Encoding.UTF8)).Select(row=>row.ToArray()).ToList();
                var positions=ReadPresetPositions();
                foreach (string[] p in rows.Skip(1))
                {
                    if (p.Length >= 8 && p[0] == "SETTINGS")
                    {
                        if (Enum.TryParse(p[3], out PresetScanOrderMode mode)) _presetScanOrderMode = mode;
                        if (int.TryParse(p[4], out int laSpeed)) _laPresetScanSpeed = ClampPresetScanSetting(laSpeed);
                        if (int.TryParse(p[5], out int laDelay)) _laPresetScanDelay = ClampPresetScanSetting(laDelay);
                        if (int.TryParse(p[6], out int webSpeed)) _presetScanSpeed = ClampPresetScanSetting(webSpeed);
                        if (int.TryParse(p[7], out int webDelay)) _presetScanDelay = ClampPresetScanSetting(webDelay);
                        continue;
                    }
                    if (p.Length < 10 || (p[0] != "L" && p[0] != "W"))
                    {
                        continue;
                    }

                    if (!int.TryParse(p[1], out int order) ||
                        !int.TryParse(p[2], out int number) ||
                        !double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double pan) ||
                        !double.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double tilt))
                    {
                        continue;
                    }

                    if (p[0] == "W")
                    {
                        tilt = NormalizeUnsignedWebAgentTilt(tilt, 0.0);
                    }

                    PositionSnapshot.TryParseExportFields(
                        p,
                        10,
                        out PositionSnapshot positionSnapshot);
                    string[] savedPosition;
                    if(positionSnapshot==null && p.Length==12 && positions.TryGetValue(PresetRowHash(p),out savedPosition))
                        PositionSnapshot.TryParseExportFields(savedPosition,0,out positionSnapshot);

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
                        positionSnapshot);
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
                // Never overwrite or delete the legacy file. CSV wins on subsequent starts.
                if(legacy || rows.FirstOrDefault()?.Length>12) { _isLoadingPresetStorage=false; SavePresetStorage(); }
                else CsvExcelCompanion.Schedule(PresetStoragePath);
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

        private void SavePresetStorage()
        {
            if (_isLoadingPresetStorage)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(PresetStoragePath);
                Directory.CreateDirectory(directory);
                string temporaryPath = PresetStoragePath + ".tmp";
                List<string> lines = new List<string>
                {
                    "TYPE\tSAVED_ORDER\tNUMBER\tNAME_OR_MODE\tPAN_OR_L_SPEED\tTILT_OR_L_DELAY\tEO_ZOOM_OR_W_SPEED\tEO_FOCUS_OR_W_DELAY\tIR_ZOOM\tIR_FOCUS\t" +
                    "POSITION_CAPTURED_AT\tPOSITION_SOURCE",
                    string.Join("\t", new[] { "SETTINGS", "0", "0", _presetScanOrderMode.ToString(), _laPresetScanSpeed.ToString(), _laPresetScanDelay.ToString(), _presetScanSpeed.ToString(), _presetScanDelay.ToString(), "-", "-", "", "" })
                };
                var positions=new XElement("PresetPositions",new XAttribute("Version",1));
                AppendPresetStorageLines(lines, "L", LaPresetPoints,positions);
                AppendPresetStorageLines(lines, "W", PresetPoints,positions);
                // Header and settings were constructed with tab delimiters; data rows are quoted CSV.
                lines[0]=lines[0].Replace('\t',','); lines[1]=lines[1].Replace('\t',',');
                File.WriteAllLines(temporaryPath, lines,new UTF8Encoding(true));
                // Commit metadata first; its backup matches the old CSV if the second commit fails.
                string positionPath=Path.Combine(directory,"PresetPositions.xml"),positionTemp=positionPath+".tmp";
                new XDocument(positions).Save(positionTemp);
                if(File.Exists(positionPath))File.Replace(positionTemp,positionPath,positionPath+".bak");else File.Move(positionTemp,positionPath);
                if (File.Exists(PresetStoragePath))
                {
                    File.Replace(temporaryPath, PresetStoragePath, PresetStoragePath+".bak");
                }
                else
                {
                    File.Move(temporaryPath, PresetStoragePath);
                }
                ConsoleLogHelper.State("PRESET STORAGE", "Saved CSV / PATH="+PresetStoragePath);
                CsvExcelCompanion.Create(PresetStoragePath);

            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("PRESET STORAGE", "Save failed", ex);
            }

        }

        private static void AppendPresetStorageLines(
            ICollection<string> lines,
            string type,
            IEnumerable<PresetPointOption> presets, XElement positions)
        {
            foreach (PresetPointOption preset in presets.OrderBy(p => p.SavedOrder))
            {
                string safeName = (preset.Name ?? $"P{preset.Number:00}")
                    .Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
                List<string> fields = new List<string>(new[]
                {
                    type,
                    preset.SavedOrder.ToString(CultureInfo.InvariantCulture),
                    preset.Number.ToString(CultureInfo.InvariantCulture),
                    safeName,
                    preset.Pan.ToString("R", CultureInfo.InvariantCulture),
                    preset.Tilt.ToString("R", CultureInfo.InvariantCulture),
                    preset.EoZoomText,
                    preset.EoFocusText,
                    preset.IrZoomText,
                    preset.IrFocusText
                });

                string[] positionFields = preset.PositionSnapshot?.ToExportFields() ??
                                          Enumerable.Repeat(string.Empty, PositionSnapshot.ExportFieldCount).ToArray();
                fields.AddRange(positionFields.Take(2).Select(SanitizePresetStorageField));
                if(preset.PositionSnapshot!=null)positions.Add(new XElement("Preset",new XAttribute("Hash",PresetRowHash(fields)),positionFields.Select(v=>new XElement("Field",v??""))));
                lines.Add(string.Join(",", fields.Select(value=>"\""+(value??string.Empty).Replace("\"","\"\"")+"\"")));
            }

        }
        private static string PresetRowHash(IEnumerable<string> fields)
        {
            string canonical=string.Join(",",fields.Take(12).Select(v=>"\""+(v??"").Replace("\"","\"\"")+"\""));
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-","");
        }
        private Dictionary<string,string[]> ReadPresetPositions()
        {
            var result=new Dictionary<string,string[]>();string path=Path.Combine(Path.GetDirectoryName(PresetStoragePath),"PresetPositions.xml");
            foreach(string candidate in new[]{path,path+".bak"})
            {
                if(!File.Exists(candidate))continue;
                try
                {
                    if(new FileInfo(candidate).Length>4000000)throw new InvalidDataException("Preset metadata too large.");
                    using(var reader=XmlReader.Create(candidate,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))
                    {
                        var data=XDocument.Load(reader);
                        if(data.Root?.Name!="PresetPositions" || (string)data.Root.Attribute("Version")!="1")throw new InvalidDataException("Preset metadata version.");
                        foreach(var item in data.Root.Elements("Preset"))
                        {
                            string hash=(string)item.Attribute("Hash");var values=item.Elements("Field").Select(e=>e.Value).ToArray();
                            if(hash!=null && hash.Length==64 && values.Length==PositionSnapshot.ExportFieldCount && !result.ContainsKey(hash))result.Add(hash,values);
                        }
                    }
                }
                catch(Exception ex){ConsoleLogHelper.Error("PRESET POSITION","위치 메타데이터 읽기 실패 / 해당 위치 연동 보류",ex);}
            }
            return result;
        }

        private static string SanitizePresetStorageField(string value)
        {
            return (value ?? string.Empty)
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private void PreparePresetForUpsert(
            PresetPointOption newPreset,
            IEnumerable<PresetPointOption> presets,
            PresetPointOption existingPreset)
        {
            newPreset.SavedOrder = existingPreset != null
                ? existingPreset.SavedOrder
                : presets.Select(p => p.SavedOrder).DefaultIfEmpty(0).Max() + 1;
        }

        private void ReorderPresetCollections()
        {
            ReplacePresetOrder(LaPresetPoints, LaPresetPoints.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray());
            ReplacePresetOrder(PresetPoints, PresetPoints.OrderBy(p => p.SavedOrder).ThenBy(p => p.Number).ToArray());
        }

        private static void ReplacePresetOrder(
            System.Collections.ObjectModel.ObservableCollection<PresetPointOption> target,
            IEnumerable<PresetPointOption> ordered)
        {
            PresetPointOption[] snapshot = ordered.ToArray();
            target.Clear();
            foreach (PresetPointOption preset in snapshot)
            {
                target.Add(preset);
            }

        }

        private PresetPointOption[] CreatePresetScanQueue(IEnumerable<PresetPointOption> source)
        {
            PresetPointOption[] registered = source.ToArray();

            // 2026-10-01: 본 GUI에서 NEAREST는 PAN/TILT 공간거리가 아니라
            // 등록된 PRESET 번호의 오름차순을 의미한다. SAVED ORDER는 사용자가
            // 저장한 순서를 그대로 보존하며 기본 모드로 유지한다.
            PresetPointOption[] result = _presetScanOrderMode == PresetScanOrderMode.SavedOrder
                ? registered
                    .OrderBy(p => p.SavedOrder)
                    .ThenBy(p => p.Number)
                    .ToArray()
                : registered
                    .OrderBy(p => p.Number)
                    .ThenBy(p => p.SavedOrder)
                    .ToArray();

            ConsoleLogHelper.State(
                "PRESET SCAN ORDER",
                "MODE=" + _presetScanOrderMode +
                " / REGISTERED=" + string.Join("->", registered
                    .OrderBy(p => p.SavedOrder)
                    .ThenBy(p => p.Number)
                    .Select(p => p.Number)) +
                " / EXECUTION=" + string.Join("->", result.Select(p => p.Number)));

            return result;
        }

        private static int ClampPresetScanSetting(int value) => Math.Max(1, Math.Min(60, value));
    }

}
