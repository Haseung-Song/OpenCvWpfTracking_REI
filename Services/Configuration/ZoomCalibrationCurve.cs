using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenCvWpfTracking.Common;

namespace OpenCvWpfTracking.Services.Configuration
{
    /// <summary>ER-WATCHER 1 실측표를 사용자 지정 장비에 공통 적용하는 불변 배율 곡선이다.</summary>
    public sealed class ZoomCalibrationCurve
    {
        private readonly int[] _positions;

        private readonly double[] _ratios;

        private readonly int _lastMeasuredPosition;

        private static readonly Lazy<Dictionary<string, ZoomCalibrationCurve>> Curves = new Lazy<Dictionary<string, ZoomCalibrationCurve>>(() =>
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ER-WATCHER1_ZoomCalibration.csv");

                var values = Load(path);

                ConsoleLogHelper.State("줌 실측표", "ER-WATCHER 1 기준표를 납품 장비 1~4·환경부·기동형·LA에 공통 적용 / EO 30배 초과는 카탈로그 끝점 연결 추정 / 개별 장비 실측 아님");

                return values;
            }
            catch (Exception error)
            {
                ConsoleLogHelper.Warning("줌 실측표", "설정 읽기 실패 / 기존 환산 유지 / " + error.GetType().Name);

                return new Dictionary<string, ZoomCalibrationCurve>();
            }
        });

        public double MinimumRatio
        {
            get
            {
                return _ratios[0];
            }
        }

        public double MaximumRatio
        {
            get
            {
                return _ratios[_ratios.Length - 1];
            }
        }

        private ZoomCalibrationCurve(int[] positions, double[] ratios, int lastMeasuredPosition)
        {
            _positions = positions;
            _ratios = ratios;
            _lastMeasuredPosition = lastMeasuredPosition;
        }

        /// <summary>사용자가 공통 적용을 지정한 현재 7개 장비만 허용하며 이전 식별자도 호환한다.</summary>
        public static bool SupportsDevice(string deviceId)
        {
            switch (DeviceCatalog.CanonicalDeviceId(deviceId))
            {
                case "ER_WATCHER_1":
                case "ER_WATCHER_2":
                case "ER_WATCHER_3":
                case "ER_WATCHER_4":
                case "MR300_TEST":
                case "MR500_MOE":
                case "LR1000_MOBILE":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>장비 1에서 측정한 표를 지정 장비에 공유한다. 각 장비의 개별 실측을 의미하지 않는다.</summary>
        public static ZoomCalibrationCurve Get(string deviceId, bool ir)
        {
            if (!SupportsDevice(deviceId))
            {
                return null;
            }

            ZoomCalibrationCurve curve;

            if (Curves.Value.TryGetValue(ir ? "IR" : "EO", out curve))
            {
                return curve;
            }
            else
            {
                return null;
            }
        }

        /// <summary>단조 증가·양 끝점·실측/추정 구분을 검증하고 측정 중단 기능 없이 표만 읽는다.</summary>
        public static Dictionary<string, ZoomCalibrationCurve> Load(string path)
        {
            var lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#")).ToArray();

            if (lines.Length < 5 || lines[0].TrimStart('\uFEFF') != "Channel,Raw,Ratio,Basis")
            {
                throw new FormatException("줌 실측표 헤더 오류");
            }

            var rows = lines.Skip(1).Select(line => line.Split(',')).ToArray();

            if (rows.Any(row => row.Length != 4 || (row[0] != "EO" && row[0] != "IR")))
            {
                throw new FormatException("줌 실측표 채널·열 오류");
            }

            var result = new Dictionary<string, ZoomCalibrationCurve>();

            foreach (string channel in new[]
            {
                "EO",
                "IR"
            }

            )
            {
                var entries = rows.Where(row => row[0] == channel).ToArray();

                var positions = new List<int>();

                var ratios = new List<double>();

                int measuredEnd = -1;

                bool estimated = false;

                foreach (var row in entries)
                {
                    int position = int.Parse(row[1], CultureInfo.InvariantCulture);

                    double ratio = double.Parse(row[2], CultureInfo.InvariantCulture);

                    bool measured = row[3] == "Measured";

                    if ((!measured && row[3] != "CatalogEstimate") || (measured && estimated) || position < 0 || position > 1000 || double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio < 1 || ratio > 1000 || (positions.Count > 0 && (position <= positions.Last() || ratio <= ratios.Last())))
                    {
                        throw new FormatException("줌 실측표 순서·범위 오류");
                    }

                    positions.Add(position);

                    ratios.Add(ratio);

                    if (measured)
                    {
                        measuredEnd = position;
                    }
                    else
                    {
                        estimated = true;
                    }
                }

                if (positions.Count < 2 || positions[0] != 0 || ratios[0] != 1 || positions.Last() != 1000 || measuredEnd < 0)
                {
                    throw new FormatException("줌 실측표 끝점 오류");
                }

                result[channel] = new ZoomCalibrationCurve(positions.ToArray(), ratios.ToArray(), measuredEnd);
            }

            return result;
        }

        /// <summary>실측점 사이를 보간한다. EO 30~90배는 카탈로그 끝점을 연결한 선형 추정이며 실측값은 아니다.</summary>
        public double Ratio(int position)
        {
            int value = Math.Max(0, Math.Min(1000, position));

            int upper = Array.BinarySearch(_positions, value);

            if (upper >= 0)
            {
                return _ratios[upper];
            }

            upper = ~upper;

            int lower = upper - 1;

            double progress = (value - _positions[lower]) / (double)(_positions[upper] - _positions[lower]);

            return _ratios[lower] + progress * (_ratios[upper] - _ratios[lower]);
        }

        public bool IsEstimated(int position)
        {
            return position > _lastMeasuredPosition;
        }
    }
}
