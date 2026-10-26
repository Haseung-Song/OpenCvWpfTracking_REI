using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.Views.Video
{
    /// <summary>
    /// 전체 영상의 여백과 확대 비율을 유지하며 번호와 순서선만 그린다.
    /// 번호 이외 영역의 입력은 기존 파노라마 확대 동작으로 전달한다.
    /// </summary>
    public sealed class PanoramaPresetOverlay : FrameworkElement
    {
        private readonly List<PanoramaPresetLink> _links = new List<PanoramaPresetLink>();

        private int[] _order = new int[0];

        private BitmapSource _source;

        public event Action<int> PresetClicked;

        public PanoramaPresetOverlay()
        {
            Cursor = Cursors.Hand;
            MouseMove += (sender, args) =>
            {
                int? number = HitNumber(args.GetPosition(this));
                var point = _links.FirstOrDefault(link => link.Number == number);
                ToolTip = point == null ? "번호 클릭: 기존 PRESET 이동" :
                    "PRESET " + point.Number + " / PAN " + point.Pan.ToString("F2") + "° / TILT " + point.Tilt.ToString("F2") + "°";
            };
            ToolTip = "번호 클릭: 저장된 PRESET PTZF로 이동";
            SizeChanged += (sender, args) => InvalidateVisual();
        }

        /// <summary>
        /// 이미지·장비가 바뀔 때 기존 번호를 한꺼번에 비우거나 교체한다.
        /// </summary>
        public void Update(BitmapSource source, IEnumerable<PanoramaPresetLink> links, int[] order)
        {
            _source = source;
            _links.Clear();
            _links.AddRange(links ?? Enumerable.Empty<PanoramaPresetLink>());
            _order = order ?? new int[0];
            InvalidateVisual();
        }

        /// <summary>
        /// 전체 영상 비율을 유지하는 실제 표시 사각형을 계산한다.
        /// </summary>
        public static Rect ImageRectangle(double width, double height, int pixelsWide, int pixelsHigh)
        {
            if (width <= 0 || height <= 0 || pixelsWide <= 0 || pixelsHigh <= 0)
                return Rect.Empty;

            double scale = Math.Min(width / pixelsWide, height / pixelsHigh);
            return new Rect((width - pixelsWide * scale) / 2,
                (height - pixelsHigh * scale) / 2, pixelsWide * scale, pixelsHigh * scale);
        }

        private Rect ImageBounds => _source == null ? Rect.Empty :
            ImageRectangle(ActualWidth, ActualHeight, _source.PixelWidth, _source.PixelHeight);

        /// <summary>
        /// 정규화 좌표를 검은 여백을 제외한 영상 좌표로 변환한다.
        /// </summary>
        private static Point Position(PanoramaPresetLink link, Rect bounds)
        {
            return new Point(bounds.X + link.U * bounds.Width, bounds.Y + link.V * bounds.Height);
        }

        /// <summary>
        /// 같은 팬·틸트에 서로 다른 줌으로 저장한 번호도 각각 누를 수 있도록 라벨을 분산한다.
        /// 실제 영상 위치는 작은 점과 라벨 연결선으로 그대로 유지한다.
        /// </summary>
        private Dictionary<int, Point> LabelPositions(Rect bounds)
        {
            var labels = new Dictionary<int, Point>();

            foreach (var link in _links.OrderBy(point => point.Number))
            {
                Point anchor = Position(link, bounds);
                Point label = anchor;

                for (int attempt = 0; attempt < 81; attempt++)
                {
                    int ring = (attempt + 7) / 8;
                    double angle = ((attempt - 1) % 8) * Math.PI / 4 - Math.PI / 2;
                    label = attempt == 0 ? anchor : new Point(anchor.X + ring * 34 * Math.Cos(angle),
                        anchor.Y + ring * 34 * Math.Sin(angle));
                    label.X = Math.Max(bounds.Left + 15, Math.Min(bounds.Right - 15, label.X));
                    label.Y = Math.Max(bounds.Top + 15, Math.Min(bounds.Bottom - 15, label.Y));

                    if (labels.Values.All(previous => (previous - label).Length >= 31))
                        break;
                }

                labels[link.Number] = label;
            }

            return labels;
        }

        /// <summary>
        /// 정합된 원본 위치의 번호와 기존 프리셋 번호 순서선을 표시한다.
        /// </summary>
        protected override void OnRender(DrawingContext drawing)
        {
            base.OnRender(drawing);
            Rect bounds = ImageBounds;

            if (bounds.IsEmpty)
                return;

            var pen = new Pen(Brushes.Gold, 2);

            for (int i = 1; i < _order.Length; i++)
            {
                var previous = _links.FirstOrDefault(link => link.Number == _order[i - 1]);
                var next = _links.FirstOrDefault(link => link.Number == _order[i]);

                // 미연동 번호를 건너뛰어 실제로 없는 연결선을 만들지 않는다.
                if (previous == null || next == null)
                    continue;

                Point a = Position(previous, bounds);
                Point b = Position(next, bounds);
                drawing.DrawLine(new Pen(Brushes.Black, 4), a, b);
                drawing.DrawLine(pen, a, b);
            }

            var labels = LabelPositions(bounds);

            foreach (var link in _links)
            {
                Point anchor = Position(link, bounds);
                Point position = labels[link.Number];

                if ((anchor - position).Length > 2)
                {
                    drawing.DrawLine(new Pen(Brushes.Gold, 1), anchor, position);
                    drawing.DrawEllipse(Brushes.Gold, new Pen(Brushes.Black, 1), anchor, 3, 3);
                }

                drawing.DrawEllipse(new SolidColorBrush(Color.FromArgb(235, 28, 51, 67)),
                    new Pen(Brushes.Gold, 2), position, 14, 14);
                var text = new FormattedText(link.Number.ToString(CultureInfo.InvariantCulture),
                    CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface("Malgun Gothic"), 13, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                drawing.DrawText(text, new Point(position.X - text.Width / 2, position.Y - text.Height / 2));
            }
        }

        /// <summary>
        /// 번호 안의 클릭만 판별하고 여백이나 선 클릭은 제외한다.
        /// </summary>
        public int? HitNumber(Point point)
        {
            Rect bounds = ImageBounds;

            if (bounds.IsEmpty)
                return null;

            var nearest = LabelPositions(bounds).OrderBy(pair => (pair.Value - point).Length).FirstOrDefault();
            return _links.Count > 0 && (nearest.Value - point).Length <= 15 ? nearest.Key : (int?)null;
        }

        /// <summary>
        /// 번호 밖의 입력을 원래 파노라마 이미지로 전달한다.
        /// </summary>
        protected override HitTestResult HitTestCore(PointHitTestParameters parameters)
        {
            return HitNumber(parameters.HitPoint).HasValue ? new PointHitTestResult(this, parameters.HitPoint) : null;
        }

        /// <summary>
        /// 단일 번호 클릭만 이동 요청으로 전달하여 중복 실행을 막는다.
        /// </summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            int? number = HitNumber(e.GetPosition(this));

            if (!number.HasValue)
                return;

            e.Handled = true;

            // 두 번째 클릭은 확대나 추가 이동 명령으로 처리하지 않는다.
            if (e.ClickCount == 1)
                PresetClicked?.Invoke(number.Value);
        }
    }
}
