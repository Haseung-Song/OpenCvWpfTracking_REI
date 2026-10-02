using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Models.Position;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private string PresetStoragePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Presets.tsv");

        private void LoadPresetStorage()
        {
            try
            {
                if (!File.Exists(PresetStoragePath))
                {
                    return;
                }

                _isLoadingPresetStorage = true;
                foreach (string line in File.ReadAllLines(PresetStoragePath).Skip(1))
                {
                    string[] p = line.Split('\t');
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
                ConsoleLogHelper.State("PRESET STORAGE", "Loaded / PATH=" + PresetStoragePath);
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
                    "POSITION_CAPTURED_AT\tPOSITION_SOURCE\tPOSITION_PRESET_ID\tPOSITION_PAN\tPOSITION_TILT\tPOSITION_EO_ZOOM\tPOSITION_EO_FOCUS\tPOSITION_IR_ZOOM\tPOSITION_IR_FOCUS\t" +
                    "POSITION_LATITUDE\tPOSITION_LONGITUDE\tPOSITION_ALTITUDE\tPOSITION_ROLL\tPOSITION_PITCH\tPOSITION_YAW\t" +
                    "POSITION_PTZ_STATUS\tPOSITION_EO_LENS_STATUS\tPOSITION_IR_LENS_STATUS\tPOSITION_GPS_STATUS\tPOSITION_IMU_STATUS\t" +
                    "POSITION_GPS_SPEED\tPOSITION_GPS_COURSE\tPOSITION_GPS_HDOP\tPOSITION_GPS_SATELLITES\tPOSITION_GPS_FIX",
                    string.Join("\t", new[] { "SETTINGS", "0", "0", _presetScanOrderMode.ToString(), _laPresetScanSpeed.ToString(), _laPresetScanDelay.ToString(), _presetScanSpeed.ToString(), _presetScanDelay.ToString(), "-", "-" })
                };
                AppendPresetStorageLines(lines, "L", LaPresetPoints);
                AppendPresetStorageLines(lines, "W", PresetPoints);
                File.WriteAllLines(temporaryPath, lines);
                if (File.Exists(PresetStoragePath))
                {
                    File.Replace(temporaryPath, PresetStoragePath, null);
                }
                else
                {
                    File.Move(temporaryPath, PresetStoragePath);
                }

            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("PRESET STORAGE", "Save failed", ex);
            }

        }

        private static void AppendPresetStorageLines(
            ICollection<string> lines,
            string type,
            IEnumerable<PresetPointOption> presets)
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
                fields.AddRange(positionFields.Select(SanitizePresetStorageField));
                lines.Add(string.Join("\t", fields));
            }

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
