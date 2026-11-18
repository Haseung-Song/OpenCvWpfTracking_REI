using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

// 2026-10-16: 원본 정규화 좌표 기반 십자선 중심 저장과 교점 계산.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.Services.Control
{
    // 창 크기와 DPI에 영향을 받지 않는 원본 영상 정규화 좌표를 사용한다.
    public sealed class OpticalCenterCalibration
    {

        #region [필드 및 상태 정보]
        private readonly List<Tuple<string, int, double, double>> anchors = new List<Tuple<string, int, double, double>>();

        private readonly string file;

        // 명시적으로 초기화한 장비를 과거 주소 키에서 다시 가져오지 않도록 파일에도 남긴다.
        private readonly HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);

        private readonly HashSet<string> resolvedIdentities = new HashSet<string>(StringComparer.Ordinal);

        #endregion

        #region [초기화]
        public OpticalCenterCalibration(string filePath)
        {
            file = filePath;

            if (!File.Exists(file))
            {
                return;
            }

            try
            {
                var root = XDocument.Load(file).Root;

                foreach (var identity in root.Elements("Identity"))
                {
                    identities.Add((string)identity.Attribute("Device") ?? "");
                }

                foreach (var e in root.Elements("Point"))
                {
                    string key = (string)e.Attribute("Device");

                    int zoom = (int)e.Attribute("Zoom");

                    double x = (double)e.Attribute("X"), y = (double)e.Attribute("Y");

                    if (Valid(x, y) && zoom >= 0 && zoom <= 1000)
                    {
                        anchors.Add(Tuple.Create(key, zoom, x, y));
                    }
                }
            }
            catch (Exception e)
            {
                Common.ConsoleLogHelper.Warning("OPTICAL CENTER", "Load failed: " + e.Message);
            }
        }

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 원본 영상의 정규화 좌표가 유한한 화면 내부 값인지 확인한다.
        /// </summary>
        public static bool Valid(double x, double y)
        {
            return !double.IsNaN(x) && !double.IsNaN(y) && !double.IsInfinity(x) && !double.IsInfinity(y) && x >= 0 && x <= 1 && y >= 0 && y <= 1;
        }

        /// <summary>
        /// 고정 ID에 보정값이 없을 때 같은 장비 이름·채널의 이전 키만 한 번 이전한다.
        /// 주소별 값이 서로 다르면 자동 병합하지 않고 원본을 보존한다.
        /// </summary>
        public string ResolveIdentity(string stableKey, string exactLegacyKey, string displayName, bool ir)
        {
            if (stableKey == exactLegacyKey || identities.Contains(stableKey) || anchors.Any(point => point.Item1 == stableKey) || resolvedIdentities.Contains(stableKey))
            {
                return stableKey;
            }

            var groups = anchors.Where(point => OpticalDeviceIdentity.MatchesLegacyName(point.Item1, displayName, ir)).GroupBy(point => point.Item1).ToArray();

            var selected = groups.FirstOrDefault(group => group.Key == exactLegacyKey);

            if (selected == null && groups.Length == 1)
            {
                selected = groups[0];
            }

            if (selected == null)
            {
                resolvedIdentities.Add(stableKey);

                if (groups.Length > 1)
                {
                    Common.ConsoleLogHelper.Warning("OPTICAL CENTER", "이전 주소별 보정값 중복 / DEVICE_KEY=" + stableKey + " / 원본 보존·현재 장비 보정 확인 필요");
                }

                return stableKey;
            }

            var next = anchors.ToList();

            next.AddRange(selected.Select(point => Tuple.Create(stableKey, point.Item2, point.Item3, point.Item4)));

            var nextIdentities = new HashSet<string>(identities)
            {
                stableKey
            };

            try
            {
                Persist(next, nextIdentities);

                anchors.Clear();

                anchors.AddRange(next);

                identities.Add(stableKey);

                resolvedIdentities.Add(stableKey);

                Common.ConsoleLogHelper.State("OPTICAL CENTER", "장비 ID 기준 보정 이전 완료 / DEVICE_KEY=" + stableKey + " / POINTS=" + selected.Count() + " / 이전 주소 보정 원본 유지");

                return stableKey;
            }
            catch (Exception error)
            {
                // 저장 실패 시 확인된 이전 값을 사용한다. 이전 파일과 메모리는 손상시키지 않는다.
                Common.ConsoleLogHelper.Warning("OPTICAL CENTER", "보정 이전 저장 실패 / 이전 보정 유지 / " + error.GetType().Name);

                return selected.Key;
            }
        }

        /// <summary>
        /// 같은 장비의 줌 구간별 보정값을 조회하고 필요한 구간은 보간한다.
        /// </summary>
        public Point Get(string key, int zoom)
        {
            var a = anchors.Where(p => p.Item1 == key).OrderBy(p => p.Item2).ToArray();

            if (a.Length == 0)
            {
                return new Point(.5, .5);
            }

            if (zoom <= a[0].Item2)
            {
                return new Point(a[0].Item3, a[0].Item4);
            }

            for (int i = 1; i < a.Length; i++)
            {
                if (zoom <= a[i].Item2)
                {
                    double t = (zoom - a[i - 1].Item2) / (double)(a[i].Item2 - a[i - 1].Item2);

                    return new Point(a[i - 1].Item3 + t * (a[i].Item3 - a[i - 1].Item3), a[i - 1].Item4 + t * (a[i].Item4 - a[i - 1].Item4));
                }
            }

            return new Point(a[a.Length - 1].Item3, a[a.Length - 1].Item4);
        }

        /// <summary>
        /// 조건 검사를 통과한 십자선 중심을 저장한다.
        /// </summary>
        public void Save(string key, int zoom, Point point)
        {
            if (!Valid(point.X, point.Y) || zoom < 0 || zoom > 1000)
            {
                throw new ArgumentException("Invalid optical center");
            }

            var next = anchors.Where(p => p.Item1 != key || p.Item2 != zoom).ToList();

            next.Add(Tuple.Create(key, zoom, point.X, point.Y));

            var nextIdentities = new HashSet<string>(identities)
            {
                key
            };

            Persist(next, nextIdentities);

            anchors.Clear();

            anchors.AddRange(next);

            identities.Add(key);
        }

        /// <summary>
        /// 지정 장비의 보정값을 초기화하여 기본 영상 중심으로 복원한다.
        /// </summary>
        public void Reset(string key)
        {
            var next = anchors.Where(p => p.Item1 != key).ToList();

            var nextIdentities = new HashSet<string>(identities)
            {
                key
            };

            Persist(next, nextIdentities);

            anchors.Clear();

            anchors.AddRange(next);

            identities.Add(key);
        }

        /// <summary>
        /// 임시 파일을 완성한 뒤 기존 보정 파일을 교체한다.
        /// </summary>
        private void Persist(List<Tuple<string, int, double, double>> next, IEnumerable<string> nextIdentities)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));

            var doc = new XDocument(new XElement("OpticalCenters", new XAttribute("Version", 2), nextIdentities.OrderBy(key => key).Select(key => new XElement("Identity", new XAttribute("Device", key))), next.Select(p => new XElement("Point", new XAttribute("Device", p.Item1), new XAttribute("Zoom", p.Item2), new XAttribute("X", p.Item3.ToString("R", CultureInfo.InvariantCulture)), new XAttribute("Y", p.Item4.ToString("R", CultureInfo.InvariantCulture))))));

            string tmp = file + ".tmp";

            doc.Save(tmp);

            if (File.Exists(file))
            {
                File.Replace(tmp, file, file + ".bak");
            }
            else
            {
                File.Move(tmp, file);
            }
        }

        // 기존 C++ 방식의 4점 광축 교점을 계산하며 수직선과 평행선을 검증한다.
        /// <summary>
        /// 두 기준물의 이동선이 유효하게 만나는 영상 내부 교점을 구한다.
        /// </summary>
        public static bool TryIntersection(Point aLow, Point bLow, Point aHigh, Point bHigh, out Point center)
        {
            center = new Point();

            Vector a = aHigh - aLow, b = bHigh - bLow;

            double determinant = a.X * b.Y - a.Y * b.X;

            if (a.Length < .01 || b.Length < .01 || Math.Abs(determinant) / (a.Length * b.Length) < .08)
            {
                return false;
            }

            Vector q = bLow - aLow;

            double t = (q.X * b.Y - q.Y * b.X) / determinant;

            center = aLow + t * a;

            return Valid(center.X, center.Y);
        }
        #endregion
    }
}
