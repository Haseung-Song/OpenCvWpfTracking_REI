using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace OpenCvWpfTracking.Models.Main
{
    public sealed class ScanRoutePoint
    {
        public int Order { get; set; }

        public string Name { get; set; }

        public double Pan { get; set; }

        public double Tilt { get; set; }

        public int EoZoom { get; set; }

        public int EoFocus { get; set; }

        public int IrZoom { get; set; }

        public int IrFocus { get; set; }

        public int Speed { get; set; } = 15;

        public int Delay { get; set; } = 3;

        public ScanRoutePoint Copy() => (ScanRoutePoint)MemberwiseClone();

        public bool Valid =>
            !double.IsNaN(Pan) &&
            !double.IsInfinity(Pan) &&
            Pan >= -180 &&
            Pan <= 180 &&
            !double.IsNaN(Tilt) &&
            !double.IsInfinity(Tilt) &&
            Tilt >= -90 &&
            Tilt <= 90 &&
            Speed >= 1 &&
            Speed <= 60 &&
            Delay >= 1 &&
            Delay <= 60 &&
            new[]
        {
            EoZoom,
            EoFocus,
            IrZoom,
            IrFocus
        }.All(v => v >= 0 && v <= 1000);

        public PresetPointOption AsPreset() =>
            new PresetPointOption(
            Order,
            Pan,
            Tilt,
            EoZoom.ToString(CultureInfo.InvariantCulture),
            EoFocus.ToString(CultureInfo.InvariantCulture),
            IrZoom.ToString(CultureInfo.InvariantCulture),
            IrFocus.ToString(CultureInfo.InvariantCulture),
            Name,
            Order
        );
    }

    public sealed class ScanRoute
    {
        public string Name { get; set; }

        public bool IndividualSettings { get; set; }

        public bool PanOrder { get; set; }

        public bool PingPong { get; set; }

        public int Speed { get; set; } = 15;

        public int Delay { get; set; } = 3;

        public ObservableCollection<ScanRoutePoint> Points { get; } = new ObservableCollection<ScanRoutePoint>();

        public void Renumber()
        {
            for (int i = 0; i < Points.Count; i++)
                Points[i].Order = i + 1;
        }

        public bool Valid => Points.Count <= 1000 && Speed >= 1 && Speed <= 60 && Delay >= 1 && Delay <= 60 && Points.All(p => p.Valid);

        public ScanRoutePoint[] Queue()
        {
            if (!Valid)
                throw new InvalidOperationException("좌표·렌즈 0~1000·속도/대기 1~60 범위를 확인하세요.");

            IEnumerable<ScanRoutePoint> order = PanOrder ? Points.OrderBy(p => p.Pan).ThenBy(p => p.Tilt) : Points.OrderBy(p => p.Order);
            var result = order.Select(p =>
            {
                var clone = p.Copy();

                if (!IndividualSettings)
                {
                    clone.Speed = Speed;
                    clone.Delay = Delay;
                }

                return clone;
            }).ToArray();
            return PingPong && result.Length > 2 ? result.Concat(result.Reverse().Skip(1).Take(result.Length - 2)).ToArray() : result;
        }

        public bool Near(ScanRoutePoint point) => Points.Any(p =>
        {
            double dx = Math.Abs(p.Pan - point.Pan) % 360;
            return Math.Min(dx, 360 - dx) < .01 && Math.Abs(p.Tilt - point.Tilt) < .01;
        });
    }

    public sealed class ScanRouteStore
    {
        private readonly string file;

        private Exception loadFailure;

        private readonly Dictionary<string, ScanRoute[]> banks = new Dictionary<string, ScanRoute[]>();

        public ScanRouteStore(string path)
        {
            file = path;
        }

        private static ScanRoute[] Empty() => Enumerable.Range(
            0,
            5
        ).Select(i => new ScanRoute { Name = "경로 " + (char)('A' + i) }).ToArray();

        public ScanRoute[] Get(string device, bool ir)
        {
            string key = device + "|" + (ir ? "IR" : "EO");
            ScanRoute[] routes;

            if (banks.TryGetValue(key, out routes))
                return routes;

            routes = Empty();

            try
            {
                if (File.Exists(file))
                {
                    var bank = XDocument.Load(file).Root.Elements("Bank").FirstOrDefault(e => (string)e.Attribute("Key") == key);

                    if (bank != null)
                        foreach (var row in bank.Elements("Route"))
                        {
                            int index = (int)row.Attribute("Index");

                            if (index < 0 || index >= 5)
                                throw new InvalidDataException("Invalid route index");

                            var route = routes[index];
                            route.Name = (string)row.Attribute("Name");
                            route.Speed = (int)row.Attribute("Speed");
                            route.Delay = (int)row.Attribute("Delay");
                            route.IndividualSettings = (bool)row.Attribute("Individual");
                            route.PanOrder = (bool)row.Attribute("PanOrder");
                            route.PingPong = (bool)row.Attribute("PingPong");

                            if (row.Elements("Point").Take(1001).Count() > 1000)
                                throw new InvalidDataException("Route point limit exceeded");

                            foreach (var p in row.Elements("Point"))
                                route.Points.Add(new ScanRoutePoint { Order = (int)p.Attribute("Order"), Name = (string)p.Attribute("Name"), Pan = (double)p.Attribute("Pan"), Tilt = (double)p.Attribute("Tilt"), EoZoom = (int)p.Attribute("EZ"), EoFocus = (int)p.Attribute("EF"), IrZoom = (int)p.Attribute("IZ"), IrFocus = (int)p.Attribute("IF"), Speed = (int)p.Attribute("Speed"), Delay = (int)p.Attribute("Delay") });

                            if (!route.Valid)
                                throw new InvalidDataException("Invalid route values");

                            route.Renumber();
                        }
                }
            }
            catch (Exception error)
            {
                // 손상된 원본은 보존하고 이후 화면 저장이 기본값으로 덮어쓰지 못하게 한다.
                routes = Empty();
                loadFailure = error;
                Common.ConsoleLogHelper.Error(
                    "LOCAL ROUTE",
                    "Stored routes invalid; editing persistence blocked, original retained",
                    error
                );
            }

            banks.Add(key, routes);
            return routes;
        }

        public void Save(string device, bool ir)
        {
            var routes = Get(device, ir);

            if (loadFailure != null)
                throw new InvalidOperationException("경로 파일 오류: 원본 보존 중입니다. ScanRoutes.xml과 로그를 확인하세요.", loadFailure);

            if (routes.Any(r => !r.Valid))
                throw new InvalidOperationException("경로 설정 범위를 확인하세요.");

            string key = device + "|" + (ir ? "IR" : "EO");
            var doc = File.Exists(file) ? XDocument.Load(file) : new XDocument(new XElement(
                "ScanRoutes",
                new XAttribute(
                "Version",
                1
            )
            ));
            doc.Root.Elements("Bank").Where(e => (string)e.Attribute("Key") == key).Remove();
            doc.Root.Add(new XElement(
                "Bank",
                new XAttribute(
                "Key",
                key
            ),
                routes.Select((r, i) => new XElement(
                "Route",
                new XAttribute(
                "Index",
                i
            ),
                new XAttribute(
                "Name",
                r.Name ?? ("경로 " + (i + 1))
            ),
                new XAttribute(
                "Speed",
                r.Speed
            ),
                new XAttribute(
                "Delay",
                r.Delay
            ),
                new XAttribute(
                "Individual",
                r.IndividualSettings
            ),
                new XAttribute(
                "PanOrder",
                r.PanOrder
            ),
                new XAttribute(
                "PingPong",
                r.PingPong
            ),
                r.Points.Select(p => new XElement(
                "Point",
                new XAttribute(
                "Order",
                p.Order
            ),
                new XAttribute(
                "Name",
                p.Name ?? ""
            ),
                new XAttribute(
                "Pan",
                p.Pan
            ),
                new XAttribute(
                "Tilt",
                p.Tilt
            ),
                new XAttribute(
                "EZ",
                p.EoZoom
            ),
                new XAttribute(
                "EF",
                p.EoFocus
            ),
                new XAttribute(
                "IZ",
                p.IrZoom
            ),
                new XAttribute(
                "IF",
                p.IrFocus
            ),
                new XAttribute(
                "Speed",
                p.Speed
            ),
                new XAttribute(
                "Delay",
                p.Delay
            )
            ))
            ))
            ));
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            string tmp = file + ".tmp";
            doc.Save(tmp);

            if (File.Exists(file))
                File.Replace(tmp, file, file + ".bak");
            else
                File.Move(tmp, file);
        }
    }
}
