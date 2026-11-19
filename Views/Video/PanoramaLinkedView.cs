using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using OpenCvWpfTracking.ViewModels.Main;

namespace OpenCvWpfTracking
{
    // 저장된 파노라마는 수신 위치로 탐색하며 실시간 합성 영상이 아니다.
    public sealed class PanoramaLinkedView : FrameworkElement
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ImageSource), typeof(PanoramaLinkedView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, e) => ((PanoramaLinkedView)d).InvalidateReference()));

        public ImageSource Source
        {
            get
            {
                return (ImageSource)GetValue(SourceProperty);
            }

            set
            {
                SetValue(SourceProperty, value);
            }
        }

        private readonly MainViewModel vm;

        private readonly Func<string> sourceId;

        private readonly DispatcherTimer timer;

        private bool registered;

        private bool registerNext;

        private bool follow;

        private double anchorU = .5;

        private double anchorV = .5;

        private double anchorPan;

        private double anchorTilt;

        private double verticalSpan = 24;

        private string key;

        private string lastKey;

        private Rect area;

        private double centerU;

        private double centerV;

        private double worldWidth;

        private double worldHeight;

        private double imageX;

        private double imageY;

        private readonly string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "PanoramaRegistration.xml");

        /// <summary>
        /// PanoramaLinkedView 화면의 컨트롤·이벤트와 전달받은 데이터 문맥을 초기화한다.
        /// </summary>
        public PanoramaLinkedView(MainViewModel model, Func<string> identity)
        {
            vm = model;
            sourceId = identity;
            ClipToBounds = true;
            Cursor = Cursors.Hand;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };

            timer.Tick += (s, e) => InvalidateVisual();

            Loaded += (s, e) =>
            {
                vm.PositionReferenceChanged -= InvalidatePositionReference;

                vm.PositionReferenceChanged += InvalidatePositionReference;

                timer.Start();
            };

            Unloaded += (s, e) =>
            {
                timer.Stop();

                vm.PositionReferenceChanged -= InvalidatePositionReference;
            };

            ToolTip = "상단 GUI에서 경로 선택·기준 맞춤·시선 따라가기 / 지점 클릭: 선택";

            MouseLeftButtonDown += Click;
        }

        /// <summary>
        /// 현재 파노라마·장면 참조를 더 이상 유효하지 않은 상태로 만든다.
        /// </summary>
        private void InvalidateReference()
        {
            if (!Dispatcher.CheckAccess())
            {
                if (!Dispatcher.HasShutdownStarted)
                {
                    Dispatcher.BeginInvoke(new Action(InvalidateReference));
                }

                return;
            }

            registered = false;
            follow = false;
            registerNext = false;

            InvalidateVisual();
        }

        /// <summary>
        /// 연결 영상이 현재 장비 시야를 따라갈지 설정한다.
        /// </summary>
        public void SetFollowing(bool enabled)
        {
            follow = enabled && registered;

            InvalidateVisual();
        }

        private bool referenceWriteFailed;

        /// <summary>
        /// 기준 위치가 바뀌었음을 반영해 영상 위치 참조를 무효화한다.
        /// </summary>
        private void InvalidatePositionReference()
        {
            if (!Dispatcher.CheckAccess())
            {
                if (!Dispatcher.HasShutdownStarted)
                {
                    Dispatcher.BeginInvoke(new Action(InvalidatePositionReference));
                }

                return;
            }

            InvalidateReference();

            // 영점 변경은 저장된 위치 기준도 무효화하며 다시 불러와도 복구하지 않는다.
            try
            {
                if (File.Exists(file))
                {
                    var doc = XDocument.Load(file);

                    foreach (var entry in doc.Root.Elements("Image").Where(p => ((string)p.Attribute("Key") ?? "").StartsWith(vm.PanoramaDeviceKey + "|", StringComparison.Ordinal)))
                    {
                        entry.SetAttributeValue("Invalid", true);
                    }

                    WriteRegistration(doc);
                }
            }
            catch (Exception error)
            {
                referenceWriteFailed = true;

                Common.ConsoleLogHelper.Error("PANORAMA REFERENCE", "Reference invalidated; saved-reference loading blocked", error);
            }
        }

        /// <summary>
        /// 기준 영상과 현재 영상의 대응점을 등록하는 절차를 시작한다.
        /// </summary>
        public void BeginRegistration()
        {
            if (!vm.PanoramaPoseFresh)
            {
                throw new InvalidOperationException("최신 PAN/TILT 수신값이 필요합니다.");
            }

            follow = false;
            registerNext = true;

            InvalidateVisual();
        }

        /// <summary>
        /// 저장된 영상 정합·기준 위치 정보를 읽는다.
        /// </summary>
        public void LoadRegistration()
        {
            var p = ReadEntry();

            if (p == null)
            {
                throw new InvalidOperationException("저장된 기준이 없습니다. '사진 기준 맞춤'을 선택하세요.");
            }

            anchorU = (double)p.Attribute("U");

            anchorV = (double)p.Attribute("V");

            anchorPan = (double)p.Attribute("Pan");

            anchorTilt = (double)p.Attribute("Tilt");

            verticalSpan = (double)p.Attribute("Span");

            registered = Services.Control.OpticalCenterCalibration.Valid(anchorU, anchorV) && verticalSpan > 0 && verticalSpan <= 180 && !double.IsNaN(anchorPan) && !double.IsInfinity(anchorPan) && !double.IsNaN(anchorTilt) && !double.IsInfinity(anchorTilt);

            if (!registered)
            {
                throw new InvalidOperationException("저장된 기준 데이터 오류");
            }

            InvalidateVisual();
        }

        /// <summary>
        /// 압축 저장 자료에서 지정 항목의 내용을 읽는다.
        /// </summary>
        private XElement ReadEntry()
        {
            if (!referenceWriteFailed && File.Exists(file))
            {
                return XDocument.Load(file).Root.Elements("Image").FirstOrDefault(p => (string)p.Attribute("Key") == key && (bool?)p.Attribute("Invalid") != true);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 파노라마 영상 키별 연결 기준점과 장비 자세 정보를 XML에 저장한다.
        /// </summary>
        private void Save()
        {
            var doc = File.Exists(file) ? XDocument.Load(file) : new XDocument(new XElement("Panoramas"));

            doc.Root.Elements("Image").Where(p => (string)p.Attribute("Key") == key).Remove();

            doc.Root.Add(new XElement("Image", new XAttribute("Key", key), new XAttribute("U", anchorU), new XAttribute("V", anchorV), new XAttribute("Pan", anchorPan), new XAttribute("Tilt", anchorTilt), new XAttribute("Span", verticalSpan)));

            WriteRegistration(doc);

            referenceWriteFailed = false;
        }

        /// <summary>
        /// 영상 정합 정보와 참조 데이터를 저장한다.
        /// </summary>
        private void WriteRegistration(XDocument doc)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));

            string temp = file + ".tmp";

            doc.Save(temp);

            if (File.Exists(file))
            {
                File.Replace(temp, file, file + ".bak");
            }
            else
            {
                File.Move(temp, file);
            }
        }

        /// <summary>
        /// 각도 또는 순환 인덱스를 지정한 범위 안으로 감싼다.
        /// </summary>
        public static double Wrap(double value)
        {
            return value - Math.Floor(value);
        }

        /// <summary>
        /// 파노라마 배경·현재 시야·연결 표시를 화면 비율과 따라가기 상태에 맞춰 그린다.
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(16, 22, 28)), null, new Rect(RenderSize));

            var image = Source;

            if (image == null || image.Width <= 0 || ActualWidth < 1 || ActualHeight < 1 || !vm.IsContextPanelsVisible)
            {
                return;
            }

            key = vm.PanoramaDeviceKey + "|" + sourceId() + "|" + image.Width + "x" + image.Height;

            if (lastKey != key)
            {
                registered = false;
                follow = false;
                registerNext = false;
                lastKey = key;
            }

            double fit = Math.Min(ActualWidth / image.Width, ActualHeight / image.Height);

            area = new Rect((ActualWidth - image.Width * fit) / 2, (ActualHeight - image.Height * fit) / 2, image.Width * fit, image.Height * fit);

            centerU = Wrap(anchorU + (vm.CurrentPanoramaPan - anchorPan) / 360.0);

            centerV = anchorV - (vm.CurrentPanoramaTilt - anchorTilt) / verticalSpan;

            if (follow && registered)
            {
                worldWidth = ActualWidth * 4;

                worldHeight = worldWidth * image.Height / image.Width;

                imageX = ActualWidth / 2 - centerU * worldWidth;

                imageY = ActualHeight / 2 - centerV * worldHeight;

                area = new Rect(0, 0, ActualWidth, ActualHeight);
            }
            else
            {
                worldWidth = area.Width;
                worldHeight = area.Height;
                imageX = area.X;
                imageY = area.Y;
            }

            dc.PushClip(new RectangleGeometry(area));

            for (int i = follow ? -1 : 0; i <= (follow ? 1 : 0); i++)
            {
                dc.DrawImage(image, new Rect(imageX + i * worldWidth, imageY, worldWidth, worldHeight));
            }

            if (registered && vm.PanoramaPoseFresh)
            {
                Point? previous = null;

                var route = vm.CurrentRoute;

                var points = route.PanOrder ? route.Points.OrderBy(p => p.Pan).ThenBy(p => p.Tilt) : route.Points.OrderBy(p => p.Order);

                foreach (var p in points)
                {
                    Point pos = Project(p.Pan, p.Tilt);

                    var color = LegacyVideoIntegration.ChannelBrush(vm.RouteInfrared);

                    if (previous.HasValue)
                    {
                        Point end = pos;

                        double difference = pos.X - previous.Value.X;

                        if (Math.Abs(difference) > worldWidth / 2)
                        {
                            end.X += difference > 0 ? -worldWidth : worldWidth;
                        }

                        for (int copy = -1; copy <= 1; copy++)
                        {
                            dc.DrawLine(new Pen(color, 1.5), previous.Value + new Vector(copy * worldWidth, 0), end + new Vector(copy * worldWidth, 0));
                        }
                    }

                    for (int copy = -1; copy <= 1; copy++)
                    {
                        Point shown = pos + new Vector(copy * worldWidth, 0);

                        bool active = vm.SelectedRoutePoint != null && vm.SelectedRoutePoint.Order == p.Order;

                        dc.DrawEllipse(active ? Brushes.White : color, new Pen(Brushes.Black, 1), shown, active ? 6 : 4, active ? 6 : 4);

                        if (route.Points.Count < 120 || active)
                        {
                            DrawText(dc, p.Order.ToString(), shown + new Vector(6, -14), color);
                        }
                    }

                    previous = pos;
                }

                bool ir = vm.IsPanoramaIrSource;

                var tuning = vm.ControlTuningForPanorama(ir);

                double hfov = tuning.Item1, vfov = 2 * Math.Atan(Math.Tan(hfov * Math.PI / 360) / tuning.Item2) * 180 / Math.PI;

                Point c = Project(vm.CurrentPanoramaPan, vm.CurrentPanoramaTilt);

                double w = Math.Max(3, hfov / 360 * worldWidth), h = Math.Max(3, vfov / verticalSpan * worldHeight);

                for (int copy = -1; copy <= 1; copy++)
                {
                    dc.DrawRectangle(null, new Pen(LegacyVideoIntegration.ChannelBrush(ir), 2), new Rect(c.X + copy * worldWidth - w / 2, c.Y - h / 2, w, h));
                }
            }

            dc.Pop();

            DrawText(dc, registerNext ? "현재 영상과 같은 기준물을 사진에서 클릭" : !registered ? "상단 '사진 기준 맞춤'으로 현재 시선 연결" : !vm.PanoramaPoseFresh ? "위치 수신 대기 · 표시 중지" : (follow ? "시선 따라가기" : "전체 보기") + " · " + vm.CurrentRoute.Name + " / " + vm.CurrentRoute.Points.Count + "점", new Point(8, 5), Brushes.LightSteelBlue);
        }

        /// <summary>
        /// 파노라마 기준점과 Pan·Tilt 차이로 화면상의 영상 좌표를 계산한다.
        /// </summary>
        private Point Project(double pan, double tilt)
        {
            double u = Wrap(anchorU + (pan - anchorPan) / 360.0), v = anchorV - (tilt - anchorTilt) / verticalSpan;

            double x = imageX + u * worldWidth;

            if (follow)
            {
                while (x < -worldWidth / 2)
                {
                    x += worldWidth;
                }

                while (x > ActualWidth + worldWidth / 2)
                {
                    x -= worldWidth;
                }
            }

            return new Point(x, imageY + v * worldHeight);
        }

        /// <summary>
        /// 오버레이 문자열을 지정 위치와 표시 형식으로 그린다.
        /// </summary>
        private static void DrawText(DrawingContext dc, string text, Point at, Brush color)
        {
            dc.DrawText(new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Malgun Gothic"), 11, color, 1), at);
        }

        /// <summary>
        /// 영상 클릭 위치를 참조 장면의 보정·연결 작업에 전달한다.
        /// </summary>
        private void Click(object sender, MouseButtonEventArgs e)
        {
            Point mouse = e.GetPosition(this);

            if (registerNext && area.Contains(mouse) && vm.PanoramaPoseFresh)
            {
                try
                {
                    anchorU = (mouse.X - area.X) / area.Width;

                    anchorV = (mouse.Y - area.Y) / area.Height;

                    anchorPan = vm.CurrentPanoramaPan;
                    anchorTilt = vm.CurrentPanoramaTilt;
                    verticalSpan = vm.SelectedPanoramaTiltRangeDegrees;

                    Save();

                    registered = true;
                    registerNext = false;

                    InvalidateVisual();

                    e.Handled = true;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }

                return;
            }

            if (!registered || !vm.PanoramaPoseFresh || !area.Contains(mouse))
            {
                return;
            }

            // 360도 이음매 양쪽의 투영 사본도 선택 대상으로 포함한다.
            var hit = vm.CurrentRoute.Points.Select(p => new { Point = p, Distance = Enumerable.Range(-1, 3).Min(copy => (Project(p.Pan, p.Tilt) + new Vector(copy * worldWidth, 0) - mouse).Length) }).OrderBy(p => p.Distance).FirstOrDefault();

            if (hit != null && hit.Distance < 14)
            {
                vm.SelectedRoutePoint = hit.Point;

                InvalidateVisual();

                e.Handled = true;
            }
        }
    }
}
