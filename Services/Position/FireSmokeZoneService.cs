using OpenCvWpfTracking.Models.Position;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace OpenCvWpfTracking.Services.Position
{
    // V29: reference-scene rectangles, not a geographic projection or a safety verdict.
    public sealed class FireSmokeZone
    {
        public string Name { get; set; } = "구역";
        public string Kind { get; set; } = "주의";
        public bool Fire { get; set; }
        public bool Smoke { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        [XmlIgnore] public List<System.Windows.Point> ProjectedPolygon { get; set; }
        [XmlIgnore] public System.Windows.Media.Imaging.BitmapSource Thumbnail { get; set; }
        [XmlIgnore] public string DisplayColor => "#C8A2FF";
        public override string ToString() => Name + " / " + Kind + " / " +
            (Fire ? "FIRE " : "") + (Smoke ? "SMOKE" : "");
    }

    public sealed class FireSmokeScene
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "기준 장면";
        public string Key { get; set; }
        public string EquipmentKey { get; set; }
        public string Camera { get; set; }
        public string RegisteredAt { get; set; }
        public double Pan { get; set; }
        public double Tilt { get; set; }
        public int Zoom { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public double HorizontalFov { get; set; }
        public double AngleTolerance { get; set; } = 0.3;
        public bool FixedInstallationConfirmed { get; set; }
        public bool Invalidated { get; set; }
        public string Revision { get; set; } = Guid.NewGuid().ToString("N");
        public string PresetKind { get; set; }
        public int PresetNumber { get; set; }
        public string PresetSignature { get; set; }
        public byte[] ReferencePng { get; set; }
        public List<FireSmokeZone> Zones { get; set; } = new List<FireSmokeZone>();
        public override string ToString() => Name + (PresetNumber > 0 ? " / " + PresetKind + " P" + PresetNumber.ToString("00") : "") + (Invalidated ? " / 재등록 필요" : "");
    }

    public sealed class FireSmokeZoneFile
    {
        public int Version { get; set; } = 1;
        public List<FireSmokeScene> Scenes { get; set; } = new List<FireSmokeScene>();
    }

    public sealed class ZoneAssessment
    {
        public string Level { get; set; } = "미확인";
        public string Zone { get; set; } = "";
        public string Reason { get; set; } = "구역 미등록 또는 프레임 위치 미확인";
        public string Revision { get; set; } = "";
        public string Context { get; set; } = "";
        public string Detail => Level + " / " + Zone + "\n" + Reason + "\n" + Context;
        public static ZoneAssessment Unknown(string reason) => new ZoneAssessment { Reason = reason };
    }

    public sealed class ZoneFrameContext
    {
        public bool RequireImageRegistration { get; set; }
        public IDictionary<string, FireSmokeScene> RegisteredScenes { get; set; }
        public DateTimeOffset At { get; set; }
        public PositionSnapshot Position { get; set; }
        public string Key { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Epoch { get; set; }
        public int ChannelEpoch { get; set; }
        public double HorizontalFov { get; set; }
        public double ReferenceEoFov(int zoom) => new OpenCvWpfTracking.Services.Control.FieldOfViewSyncService().GetEoHfov((short)zoom);
    }

    public static class FireSmokeZoneService
    {
        public static void Validate(FireSmokeZoneFile data)
        {
            if (data == null || data.Version != 1 || data.Scenes == null || data.Scenes.Count > 100)
                throw new InvalidDataException("구역 설정 버전 또는 장면 수가 올바르지 않습니다.");
            var ids = new HashSet<string>();
            foreach (var scene in data.Scenes)
            {
                if (scene == null || string.IsNullOrWhiteSpace(scene.Id) || !ids.Add(scene.Id) ||
                    string.IsNullOrWhiteSpace(scene.Key) || string.IsNullOrWhiteSpace(scene.EquipmentKey) ||
                    string.IsNullOrWhiteSpace(scene.Name) || (scene.Camera != "EO" && scene.Camera != "IR") ||
                    !Finite(scene.Pan) || !Finite(scene.Tilt) || !Finite(scene.AngleTolerance) ||
                    !Finite(scene.HorizontalFov) || scene.HorizontalFov < 0 || scene.HorizontalFov >= 170 ||
                    scene.AngleTolerance < 0.05 || scene.AngleTolerance > 2 ||
                    scene.FrameWidth < 1 || scene.FrameHeight < 1 || !scene.FixedInstallationConfirmed ||
                    scene.ReferencePng == null || scene.ReferencePng.Length > 16000000 ||
                    scene.Zones == null || scene.Zones.Count > 100 || scene.PresetNumber < 0 || scene.PresetNumber > 63 ||
                    (scene.PresetNumber > 0 && ((scene.PresetKind != "LA" && scene.PresetKind != "WEB") || string.IsNullOrWhiteSpace(scene.PresetSignature))))
                    throw new InvalidDataException("기준 장면 정보가 올바르지 않습니다.");
                foreach (var z in scene.Zones)
                {
                    if (z == null || string.IsNullOrWhiteSpace(z.Name) || (!z.Fire && !z.Smoke) ||
                        (z.Kind != "허용" && z.Kind != "주의" && z.Kind != "위험") ||
                        !Finite(z.X) || !Finite(z.Y) || !Finite(z.Width) || !Finite(z.Height) ||
                        z.X < 0 || z.Y < 0 || z.Width < 0.005 || z.Height < 0.005 ||
                        z.X + z.Width > 1.000001 || z.Y + z.Height > 1.000001)
                        throw new InvalidDataException("구역 좌표/종류/FIRE·SMOKE 적용 여부를 확인하세요.");
                }
            }
        }
        private static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        public static FireSmokeZoneFile Load(string path)
        {
            if (!File.Exists(path)) return new FireSmokeZoneFile();
            if (new FileInfo(path).Length > 80000000) throw new InvalidDataException("구역 설정 파일이 너무 큽니다.");
            using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                var data = (FireSmokeZoneFile)new XmlSerializer(typeof(FireSmokeZoneFile)).Deserialize(reader);
                Validate(data);
                return data;
            }
        }
        public static void Save(string path, FireSmokeZoneFile data)
        {
            Validate(data);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = File.Create(temp)) new XmlSerializer(typeof(FireSmokeZoneFile)).Serialize(stream, data);
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public static FireSmokeZoneFile Clone(FireSmokeZoneFile data)
        {
            var serializer = new XmlSerializer(typeof(FireSmokeZoneFile));
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, data); stream.Position = 0;
                return (FireSmokeZoneFile)serializer.Deserialize(stream);
            }
        }
        public static bool ValidPose(PositionSnapshot p, string camera) => p != null &&
            p.PtzStatus == PositionDataStatus.Valid && p.Pan.HasValue && p.Tilt.HasValue &&
            (camera == "IR" ? p.IrLensStatus == PositionDataStatus.Valid && p.IrZoom.HasValue :
                p.EoLensStatus == PositionDataStatus.Valid && p.EoZoom.HasValue);
        public static double PanDistance(double a, double b)
        {
            double d = Math.Abs(a - b) % 360;
            return Math.Min(d, 360 - d);
        }
        public static string MatchFailure(FireSmokeScene s, ZoneFrameContext f)
        {
            if (s == null || f == null) return "기준 장면 없음";
            if (s.Invalidated || !s.FixedInstallationConfirmed) return "재등록 필요";
            if (s.Key != f.Key) return "장비·영상 소스·원점 불일치";
            if (!ValidPose(f.Position, s.Camera)) return "현재 PTZ/ZOOM 미수신·지연";
            var projected = ForFrame(s, f);
            if (projected != null && !ReferenceEquals(projected, s))
                return projected.Zones.Count > 0 ? null : "등록 구역이 현재 화면 밖에 있음";
            int? zoom = s.Camera == "IR" ? f.Position.IrZoom : f.Position.EoZoom;
            string values = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "기준 {0:F2}° / {1:F2}° / Z{2} · 현재 {3:F2}° / {4:F2}° / Z{5}",
                s.Pan, s.Tilt, s.Zoom, f.Position.Pan, f.Position.Tilt, zoom);
            if (PanDistance(s.Pan, f.Position.Pan.Value) > s.AngleTolerance ||
                Math.Abs(s.Tilt - f.Position.Tilt.Value) > s.AngleTolerance)
                return "PAN/TILT 불일치 (허용 " + s.AngleTolerance.ToString("F2") + "°)\n" + values;
            if (s.Zoom != zoom) return "ZOOM 불일치\n" + values;
            if (f.Width <= 0 || f.Height <= 0 || Math.Abs(s.FrameWidth / (double)s.FrameHeight - f.Width / (double)f.Height) >= .005)
                return "영상 해상도·화면비 불일치";
            return null;
        }
        public static bool Matches(FireSmokeScene s, ZoneFrameContext f) => f != null &&
            !s.Invalidated && s.FixedInstallationConfirmed && s.Key == f.Key &&
            ValidPose(f.Position, s.Camera) && PanDistance(s.Pan, f.Position.Pan.Value) <= s.AngleTolerance &&
            Math.Abs(s.Tilt - f.Position.Tilt.Value) <= s.AngleTolerance &&
            s.Zoom == (s.Camera == "IR" ? f.Position.IrZoom : f.Position.EoZoom) &&
            f.Width > 0 && f.Height > 0 && Math.Abs(s.FrameWidth / (double)s.FrameHeight - f.Width / (double)f.Height) < 0.005;

        public static ZoneAssessment Assess(FireSmokeZoneFile data, ZoneFrameContext f,
            string type, double left, double top, double right, double bottom)
        {
            bool smoke = (type ?? "").IndexOf("SMOKE", StringComparison.OrdinalIgnoreCase) >= 0;
            bool fire = (type ?? "").IndexOf("FIRE", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!fire && !smoke) return ZoneAssessment.Unknown("FIRE/SMOKE 외 객체");
            if (f == null || f.Width <= 0 || f.Height <= 0) return ZoneAssessment.Unknown("원본 프레임 정보 부족");
            if (!Finite(left) || !Finite(top) || !Finite(right) || !Finite(bottom) ||
                left < 0 || top < 0 || right > f.Width || bottom > f.Height || right <= left || bottom <= top)
                return ZoneAssessment.Unknown("원본 BBox 좌표/해상도 불일치");
            var scenes = data.Scenes.Select(s => ForFrame(s, f)).Where(s => s != null).ToList();
            if (scenes.Count == 0) return ZoneAssessment.Unknown("등록 시점·Zoom 불일치, 재등록 필요 또는 구역 미등록");
            double x = left / f.Width, y = top / f.Height, r = right / f.Width, b = bottom / f.Height;
            var hits = scenes.SelectMany(s => s.Zones.Where(z => (smoke ? z.Smoke : z.Fire) &&
                ZoneViewProjection.Intersects(z, x, y, r, b))
                .Select(z => new { Scene = s, Zone = z })).ToList();
            if (hits.Count == 0) return ZoneAssessment.Unknown("지정 구역 밖 / 비허용으로 자동 단정하지 않음");
            var hit = hits.OrderByDescending(h => h.Zone.Kind == "위험" ? 3 : h.Zone.Kind == "주의" ? 2 : 1).First();
            var zone = hit.Zone;
            bool contained = x >= zone.X + 0.005 && y >= zone.Y + 0.005 &&
                r <= zone.X + zone.Width - 0.005 && b <= zone.Y + zone.Height - 0.005 &&
                ZoneViewProjection.Contains(zone, x, y, r, b);
            string level = zone.Kind == "위험" ? "이상" : zone.Kind == "주의" || !contained || smoke ? "주의" : "위치 적합";
            return new ZoneAssessment
            {
                Level = level, Zone = hit.Scene.Name + " / " + zone.Name, Revision = hit.Scene.Revision,
                Reason = zone.Kind == "위험" ? "비허용/위험 구역과 BBox 겹침" :
                    zone.Kind == "주의" ? "주의 구역과 BBox 겹침" : !contained ? "허용 구역 경계에 걸침" :
                    smoke ? "허용 구역 안 연기 / BBox만으로 실제 배출원 확정 불가" : "FIRE BBox가 허용 구역 내부 / 안전 판정 아님",
                Context = f.At.ToString("O") + " / " + f.Position.PositionSummary +
                    " / ZOOM " + hit.Scene.Zoom + " / BBOX " + left + "," + top + "," + right + "," + bottom
            };
        }
        public static FireSmokeScene ForFrame(FireSmokeScene scene, ZoneFrameContext frame)
        {
            if(frame?.RequireImageRegistration==true)
            {
                FireSmokeScene aligned;
                return scene!=null && !scene.Invalidated && scene.FixedInstallationConfirmed && scene.Key==frame.Key &&
                    frame.RegisteredScenes!=null && frame.RegisteredScenes.TryGetValue(scene.Id+scene.Revision,out aligned) ? aligned : null;
            }
            // Use projection even near the original pose, avoiding a discontinuity at angle tolerance.
            var projected = ZoneViewProjection.Project(scene, frame);
            return projected ?? (scene != null && Matches(scene, frame) ? scene : null);
        }
    }
}
