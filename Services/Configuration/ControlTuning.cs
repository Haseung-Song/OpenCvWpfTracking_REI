using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenCvWpfTracking.Common;

namespace OpenCvWpfTracking.Services.Configuration
{
    // 2026-10-12 FIX7: bounded linear display model; caller supplies monotonic elapsed time.
    public sealed class DigitalZoomEstimate
    {
        public double Factor { get; private set; } = 1;
        public bool Enabled { get; private set; }
        public int Direction { get; set; }
        private bool _previousAtMaximum;
        public void Enable(bool enabled) { if(!enabled || !Enabled){Factor=1;_previousAtMaximum=false;}Enabled=enabled;if(!enabled)Direction=0; }
        public void Advance(double seconds,bool fresh,bool atMaximum,double maximum,double traversalSeconds)
        {
            if(!Enabled){Factor=1;_previousAtMaximum=false;return;}
            if(fresh && !atMaximum)Factor=1;
            if(fresh && atMaximum && _previousAtMaximum && !double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds>=0 && seconds<=1 && maximum>1 && traversalSeconds>=1)
                Factor=Math.Max(1,Math.Min(maximum,Factor+Math.Sign(Direction)*(maximum-1)*seconds/traversalSeconds));
            _previousAtMaximum=fresh && atMaximum;
        }
    }
    // 2026-10-08 V30_0: provisional ratios, replaceable per-device calibration.
    public sealed class LensControlTuning
    {
        public double MinimumRatio = 1, MaximumRatio = 90, Exponent = 1;
        public int MinimumSpeed = 1;
        // 2026-10-12: provisional wide HFOV, configurable per device/channel for click centering.
        public double WideHorizontalFov = 65.24;
        // 2026-10-12 FIX7: optional display-only, command-time estimate; not camera telemetry.
        public double DigitalEstimateMaximum = 1, DigitalEstimateSeconds = 10;
        public object Clone() => MemberwiseClone();
        public double Ratio(int position) => MinimumRatio + Math.Max(0, Math.Min(1000, position)) / 1000.0 * (MaximumRatio - MinimumRatio);
        public int Position(double ratio) => (int)Math.Round(1000 * (Math.Max(MinimumRatio, Math.Min(MaximumRatio, ratio)) - MinimumRatio) / (MaximumRatio - MinimumRatio));
        public byte Speed(int basis, int position, bool enabled)
        {
            basis = Math.Max(1, Math.Min(60, basis));
            if (!enabled) return (byte)basis;
            // 2026-10-12: interpolate to the configured minimum over the full zoom range.
            // The old inverse-ratio power rounded to 1 already at EO 465/1000 (42.39x).
            double progress = Math.Max(0, Math.Min(1000, position)) / 1000.0;
            int minimum = Math.Min(basis, MinimumSpeed);
            int speed = (int)Math.Round(minimum + (basis - minimum) * Math.Pow(1 - progress, Exponent));
            return (byte)Math.Max(Math.Min(basis, MinimumSpeed), Math.Min(basis, speed));
        }
    }
    public static class ControlTuning
    {
        private static readonly Dictionary<string, LensControlTuning> Values = Load();
        public static LensControlTuning Get(string id, bool ir)
        {
            lock (Values)
            {
            LensControlTuning value;
            string key = (id ?? "Default") + (ir ? "/IR" : "/EO");
            if (Values.TryGetValue(key, out value)) return value;
            value = (LensControlTuning)Values["Default" + (ir ? "/IR" : "/EO")].Clone();
            Values[key] = value; return value;
            }
        }
        private static Dictionary<string, LensControlTuning> Load()
        {
            var result = new Dictionary<string, LensControlTuning>(StringComparer.OrdinalIgnoreCase)
            {
                ["Default/EO"] = new LensControlTuning(),
                ["Default/IR"] = new LensControlTuning { MaximumRatio = 9, WideHorizontalFov = 24.5 }
            };
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ControlTuning.xml");
                if (!File.Exists(path)) return result;
                var doc = XDocument.Load(path);
                foreach (var device in doc.Root.Elements("Device"))
                foreach (var lens in device.Elements("Lens"))
                {
                    var channel = (string)lens.Attribute("Channel");
                    if (channel != "EO" && channel != "IR") throw new FormatException("Invalid tuning channel.");
                    Func<string, double> read = name => double.Parse((string)lens.Attribute(name), CultureInfo.InvariantCulture);
                    var value = new LensControlTuning { MinimumRatio = read("MinRatio"), MaximumRatio = read("MaxRatio"),
                        Exponent = read("Exponent"), MinimumSpeed = (int)read("MinSpeed") };
                    value.WideHorizontalFov = lens.Attribute("WideHFov") == null ? (channel == "IR" ? 24.5 : 65.24) : read("WideHFov");
                    value.DigitalEstimateMaximum = lens.Attribute("DigitalEstimateMax") == null ? 1 : read("DigitalEstimateMax");
                    value.DigitalEstimateSeconds = lens.Attribute("DigitalEstimateSeconds") == null ? 10 : read("DigitalEstimateSeconds");
                    if(double.IsNaN(value.DigitalEstimateMaximum) || double.IsInfinity(value.DigitalEstimateMaximum) || value.DigitalEstimateMaximum<1 || value.DigitalEstimateMaximum>8 ||
                       double.IsNaN(value.DigitalEstimateSeconds) || double.IsInfinity(value.DigitalEstimateSeconds) || value.DigitalEstimateSeconds<1 || value.DigitalEstimateSeconds>120)
                        throw new FormatException("Invalid digital estimate limits.");
                    if (!new[] { value.MinimumRatio, value.MaximumRatio, value.Exponent }.All(x => !double.IsNaN(x) && !double.IsInfinity(x)) ||
                        value.MinimumRatio < 1 || value.MaximumRatio <= value.MinimumRatio || value.MaximumRatio > 1000 ||
                        value.Exponent < .1 || value.Exponent > 4 || value.MinimumSpeed < 1 || value.MinimumSpeed > 60 ||
                        double.IsNaN(value.WideHorizontalFov) || double.IsInfinity(value.WideHorizontalFov) || value.WideHorizontalFov <= 0 || value.WideHorizontalFov >= 170)
                        throw new FormatException("Invalid tuning limits.");
                    result[(string)device.Attribute("Id") + "/" + channel] = value;
                }
            }
            catch (Exception ex) { result.Clear(); result["Default/EO"] = new LensControlTuning(); result["Default/IR"] = new LensControlTuning { MaximumRatio = 9, WideHorizontalFov = 24.5 }; ConsoleLogHelper.Warning("CONTROL TUNING", "Config validation failed; safe defaults used / " + ex.GetType().Name); }
            return result;
        }
    }
}
