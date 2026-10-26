using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace OpenCvWpfTracking.Services.Position
{
    // 고정 광학 중심·직선 렌즈·회전각 0을 가정하고 기존 EO 렌즈 화각 모델을 쓴다.
    // 지리·깊이 투영이 아니며 미지원 렌즈나 오래된 위치에는 투영하지 않는다.
    public static class ZoneViewProjection
    {
        public static FireSmokeScene Project(FireSmokeScene s, ZoneFrameContext f)
        {
            if (s == null ||
                f == null ||
                s.Camera != "EO" ||
                s.Invalidated ||
                !s.FixedInstallationConfirmed ||
                s.Key != f.Key ||
                !FireSmokeZoneService.ValidPose(
                f.Position,
                "EO"
            ) ||
                f.HorizontalFov <= 0 ||
                f.HorizontalFov >= 170 ||
                double.IsNaN(f.HorizontalFov) ||
                double.IsInfinity(f.HorizontalFov) ||
                f.Width <= 0 ||
                f.Height <= 0 ||
                s.Zoom < 0 ||
                s.Zoom > 1000 ||
                s.FrameWidth <= 0 ||
                s.FrameHeight <= 0 ||
                Math.Abs(s.FrameWidth / (double)s.FrameHeight - f.Width / (double)f.Height) >= .005)
                return null;

            double referenceFov = s.HorizontalFov > 0 ? s.HorizontalFov : f.ReferenceEoFov(s.Zoom);

            if (referenceFov <= 0 || referenceFov >= 170 || double.IsNaN(referenceFov) || double.IsInfinity(referenceFov))
                return null;

            double a = Math.Tan(referenceFov * Math.PI / 360), b = a * s.FrameHeight / s.FrameWidth;
            double c = Math.Tan(f.HorizontalFov * Math.PI / 360), d = c * f.Height / f.Width;
            var result = new FireSmokeScene
            {
                Id = s.Id,
                Name = s.Name,
                Revision = s.Revision,
                Key = s.Key,
                Camera = s.Camera,
                Pan = f.Position.Pan.Value,
                Tilt = f.Position.Tilt.Value,
                Zoom = f.Position.EoZoom.Value,
                Zones = new List<FireSmokeZone>()
            };

            foreach (var z in s.Zones)
            {
                var points = new List<Point>();
                bool behind = false;

                foreach (var p in new[]
                {
                    new Point(z.X, z.Y),
                    new Point(z.X + z.Width, z.Y),
                    new Point(z.X + z.Width, z.Y + z.Height),
                    new Point(z.X, z.Y + z.Height)
                }

                )
                {
                    double x = (p.X - .5) * 2 * a, y = (.5 - p.Y) * 2 * b, depth = 1;
                    Rotate(ref y, ref depth, s.Tilt);
                    Rotate(ref x, ref depth, s.Pan);
                    Rotate(ref x, ref depth, -f.Position.Pan.Value);
                    Rotate(ref y, ref depth, -f.Position.Tilt.Value);

                    if (depth <= .02)
                    {
                        behind = true;
                        break;
                    }

                    points.Add(new Point(.5 + x / depth / (2 * c), .5 - y / depth / (2 * d)));
                }

                if (behind)
                    continue;

                points = Clip(points, 0, 0, 1, 1);

                if (points.Count < 3 || Area(points) < .000001)
                    continue;

                double x0 = points.Min(p => p.X), y0 = points.Min(p => p.Y), x1 = points.Max(p => p.X), y1 = points.Max(p => p.Y);
                result.Zones.Add(new FireSmokeZone { Name = z.Name, Kind = z.Kind, Fire = z.Fire, Smoke = z.Smoke, X = x0, Y = y0, Width = x1 - x0, Height = y1 - y0, ProjectedPolygon = points });
            }

            return result;
        }

        // 회전 부호는 팬 오른쪽·틸트 위쪽을 양수로 하며 화면 좌표는 오른쪽·위쪽 기준이다.
        private static void Rotate(ref double first, ref double depth, double degrees)
        {
            double t = degrees * Math.PI / 180, p = first;
            first = Math.Cos(t) * p + Math.Sin(t) * depth;
            depth = -Math.Sin(t) * p + Math.Cos(t) * depth;
        }

        public static bool Intersects(FireSmokeZone z, double x, double y, double r, double b)
        {
            if (z.ProjectedPolygon == null)
                return Math.Min(r, z.X + z.Width) > Math.Max(x, z.X) && Math.Min(b, z.Y + z.Height) > Math.Max(y, z.Y);

            return Area(Clip(z.ProjectedPolygon, x, y, r, b)) > .00000001;
        }

        public static bool Contains(FireSmokeZone z, double x, double y, double r, double b)
        {
            if (z.ProjectedPolygon == null)
                return true;

            double inside = Area(Clip(z.ProjectedPolygon, x, y, r, b)), box = (r - x) * (b - y);
            return inside >= box * .999999;
        }

        private static double Area(IList<Point> p)
        {
            double sum = 0;

            for (int i = 0; i < p.Count; i++)
            {
                var q = p[(i + 1) % p.Count];
                sum += p[i].X * q.Y - q.X * p[i].Y;
            }

            return Math.Abs(sum) * .5;
        }

        internal static List<Point> Clip(IList<Point> input, double left, double top, double right, double bottom)
        {
            var p = new List<Point>(input);

            for (int edge = 0; edge < 4 && p.Count > 0; edge++)
            {
                var output = new List<Point>();
                var prev = p[p.Count - 1];

                foreach (var cur in p)
                {
                    double v0 = Value(
                        prev,
                        edge
                    ), v1 = Value(
                        cur,
                        edge
                    ), bound = edge == 0 ? left : edge == 1 ? right : edge == 2 ? top : bottom;
                    bool in0 = edge == 0 ||
                        edge == 2 ? v0 >= bound : v0 <= bound, in1 = edge == 0 ||
                        edge == 2 ? v1 >= bound : v1 <= bound;

                    if (in0 != in1)
                    {
                        double t = (bound - v0) / (v1 - v0);
                        output.Add(new Point(prev.X + t * (cur.X - prev.X), prev.Y + t * (cur.Y - prev.Y)));
                    }

                    if (in1)
                        output.Add(cur);

                    prev = cur;
                }

                p = output;
            }

            return p;
        }

        private static double Value(Point p, int edge) => edge < 2 ? p.X : p.Y;
    }
}
