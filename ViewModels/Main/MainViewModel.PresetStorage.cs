using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
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

                    PresetPointOption preset = new PresetPointOption(
                        number, pan, tilt, p[6], p[7], p[8], p[9], p[3], order);
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
                    "TYPE\tSAVED_ORDER\tNUMBER\tNAME_OR_MODE\tPAN_OR_L_SPEED\tTILT_OR_L_DELAY\tEO_ZOOM_OR_W_SPEED\tEO_FOCUS_OR_W_DELAY\tIR_ZOOM\tIR_FOCUS",
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
                lines.Add(string.Join("\t", new[]
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
                }));
            }

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
            List<PresetPointOption> remaining = source
                .OrderBy(p => p.SavedOrder)
                .ThenBy(p => p.Number)
                .ToList();
            if (_presetScanOrderMode == PresetScanOrderMode.SavedOrder)
            {
                return remaining.ToArray();
            }

            List<PresetPointOption> result = new List<PresetPointOption>();
            double pan = _currentPan;
            double tilt = _currentTilt;
            while (remaining.Count > 0)
            {
                PresetPointOption nearest = remaining
                    .OrderBy(p => PresetDistance(pan, tilt, p))
                    .ThenBy(p => p.SavedOrder)
                    .First();
                result.Add(nearest);
                remaining.Remove(nearest);
                pan = nearest.Pan;
                tilt = nearest.Tilt;
            }
            return result.ToArray();
        }

        private double PresetDistance(double currentPan, double currentTilt, PresetPointOption preset)
        {
            double panDifference = Math.Abs(currentPan - preset.Pan) % 360.0;
            panDifference = Math.Min(panDifference, 360.0 - panDifference);
            double tiltDifference = GetTiltDifference(currentTilt, preset.Tilt);
            return Math.Sqrt(panDifference * panDifference + tiltDifference * tiltDifference);
        }

        private static int ClampPresetScanSetting(int value) => Math.Max(1, Math.Min(60, value));
    }

}
