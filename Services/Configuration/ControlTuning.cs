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
        /// <summary>시간 기반 디지털 추정을 켜거나 끄며 새 활성화 및 OFF 시 배율을 초기화한다.</summary>
        public void Enable(bool enabled) { if(!enabled || !Enabled){Factor=1;_previousAtMaximum=false;}Enabled=enabled;if(!enabled)Direction=0; }
        /// <summary>연속된 광학 최대 위치 증거가 유효할 때만 경과 시간으로 디지털 배율을 증감한다.</summary>
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
        // V31: command-time estimate used by display AND click geometry; not camera telemetry.
        public double DigitalEstimateMaximum = 1, DigitalEstimateSeconds = 10;
        public bool UseMeasuredSyncAnchors;
        /// <summary>새 장비 키에 사용할 독립적인 기본 조정값 복사본을 만든다.</summary>
        public object Clone() => MemberwiseClone();
        /// <summary>표준 위치를 실측 매칭표 또는 기존 선형 모델로 광학 배율에 환산한다.</summary>
        public double Ratio(int position) => UseMeasuredSyncAnchors
            ? Services.Control.FieldOfViewSyncService.EstimateEoOpticalRatio(position, WideHorizontalFov, 24.5, MaximumRatio)
            : MinimumRatio + Math.Max(0, Math.Min(1000, position)) / 1000.0 * (MaximumRatio - MinimumRatio);
        /// <summary>광학 배율을 역산한다. 실측 모델은 이진 탐색 후 가장 가까운 위치를 선택한다.</summary>
        public int Position(double ratio)
        {
            ratio=Math.Max(MinimumRatio,Math.Min(MaximumRatio,ratio));
            if(!UseMeasuredSyncAnchors)return (int)Math.Round(1000*(ratio-MinimumRatio)/(MaximumRatio-MinimumRatio));
            int low=0,high=1000;
            while(low<high){int mid=(low+high)/2;if(Ratio(mid)<ratio)low=mid+1;else high=mid;}
            return low>0 && Math.Abs(Ratio(low-1)-ratio)<Math.Abs(Ratio(low)-ratio)?low-1:low;
        }
        /// <summary>원시 줌 진행률과 감속 지수로 PT 속도를 환산하며 디지털 추정값은 사용하지 않는다.</summary>
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
        /// <summary>장비/채널 조정값을 조회하고 없는 키는 기본값 복사본을 잠금 안에서 생성한다.</summary>
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
        /// <summary>설정 XML의 범위를 검증해 읽으며 실패하면 기존 정책대로 안전 기본값 전체를 사용한다.</summary>
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
                    value.UseMeasuredSyncAnchors = channel == "EO" && (bool?)lens.Attribute("UseMeasuredSyncAnchors") == true;
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
            catch (Exception ex) { result.Clear(); result["Default/EO"] = new LensControlTuning(); result["Default/IR"] = new LensControlTuning { MaximumRatio = 9, WideHorizontalFov = 24.5 }; ConsoleLogHelper.Warning("CONTROL TUNING", "Config validation failed; safe defaults used / " + ex.GetType().Name + " / HRESULT=0x" + ex.HResult.ToString("X8")); }
            return result;
        }
    }
}
