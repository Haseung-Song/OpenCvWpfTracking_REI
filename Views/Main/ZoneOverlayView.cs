using OpenCvWpfTracking.ViewModels.Main;
using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace OpenCvWpfTracking
{
    // Draw in screen DIPs, rather than scaling line/text widths with source pixels.
    public sealed class ZoneOverlayView : FrameworkElement
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(IEnumerable), typeof(ZoneOverlayView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, Changed));
        public static readonly DependencyProperty SourceWidthProperty = DependencyProperty.Register("SourceWidth", typeof(double), typeof(ZoneOverlayView), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SourceHeightProperty = DependencyProperty.Register("SourceHeight", typeof(double), typeof(ZoneOverlayView), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public IEnumerable Items { get => (IEnumerable)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
        public double SourceWidth { get => (double)GetValue(SourceWidthProperty); set => SetValue(SourceWidthProperty, value); }
        public double SourceHeight { get => (double)GetValue(SourceHeightProperty); set => SetValue(SourceHeightProperty, value); }
        private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) { var v = (ZoneOverlayView)d; if (e.OldValue is INotifyCollectionChanged old) old.CollectionChanged -= v.ItemsChanged; if (e.NewValue is INotifyCollectionChanged next) next.CollectionChanged += v.ItemsChanged; }
        private void ItemsChanged(object s, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc); if (Items == null || SourceWidth <= 0 || SourceHeight <= 0 || ActualWidth < 1 || ActualHeight < 1) return;
            double scale = Math.Min(ActualWidth / SourceWidth, ActualHeight / SourceHeight), ox = (ActualWidth - SourceWidth * scale) / 2, oy = (ActualHeight - SourceHeight * scale) / 2;
            dc.PushClip(new RectangleGeometry(new Rect(ox, oy, SourceWidth * scale, SourceHeight * scale)));
            foreach (var item in Items)
            {
                var z = item as MainViewModel.ZoneOverlayBox;
                bool registeredZone = z != null;
                if (item is AiDetectionBox ai) z = new MainViewModel.ZoneOverlayBox { Left=ai.Left, Top=ai.Top, Width=ai.Width, Height=ai.Height, Name=ai.DisplayText, Color="#39FF14" };
                else if (item is VisionDetectionBox vp) z = new MainViewModel.ZoneOverlayBox { Left=vp.Left, Top=vp.Top, Width=vp.Width, Height=vp.Height, Name=vp.DisplayText, Color=vp.DetectionType=="SMOKE"?"#FFA000":"#FF3B30" };
                if (z == null) continue;
                var color = (Brush)new BrushConverter().ConvertFromString(registeredZone ? "#C8A2FF" : z.Color);
                var viewport = new Rect(ox, oy, SourceWidth * scale, SourceHeight * scale);
                var r = new Rect(ox + z.Left * scale, oy + z.Top * scale, Math.Max(0, z.Width * scale), Math.Max(0, z.Height * scale)); r.Intersect(viewport); if(r.IsEmpty)continue;
                if(z.Polygon!=null && z.Polygon.Count>=3)
                {
                    var shape=new StreamGeometry();
                    using(var path=shape.Open())
                    {
                        path.BeginFigure(new Point(ox+z.Polygon[0].X*scale,oy+z.Polygon[0].Y*scale),false,true);
                        for(int i=1;i<z.Polygon.Count;i++) path.LineTo(new Point(ox+z.Polygon[i].X*scale,oy+z.Polygon[i].Y*scale),true,false);
                    }
                    shape.Freeze(); dc.DrawGeometry(null,new Pen(Brushes.Black,5),shape);dc.DrawGeometry(null,new Pen(color,3),shape);
                }
                else { dc.DrawRectangle(null, new Pen(Brushes.Black, 4), r); dc.DrawRectangle(null, new Pen(color, 2), r); }
                // Screen-DIP text and strokes avoid oversized labels when low-resolution IR is enlarged.
                var text = new FormattedText(z.Name ?? "", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Malgun Gothic"), Math.Max(10,Math.Min(12,viewport.Width/45)), color, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                // V29_11: labels stay directly above their own BBOX, even when they overlap.
                text.MaxTextWidth = Math.Max(1, viewport.Width - 8);
                text.MaxLineCount = 1; text.Trimming = TextTrimming.CharacterEllipsis;
                var label = LabelBounds(r, new Size(Math.Min(text.Width + 8, viewport.Width), text.Height + 4), viewport);
                if (label.IsEmpty) continue;
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(190, 0, 0, 0)), null, label); dc.DrawText(text, new Point(label.X + 4, label.Y + 2));
            }
            dc.Pop();
        }
        internal static Rect PlaceLabel(Rect box, Size size, Rect viewport, IList<Rect> occupied)
        {
            return LabelBounds(box, size, viewport);
        }
        internal static Rect LabelBounds(Rect box, Size size, Rect viewport)
        {
            double w=Math.Min(size.Width,viewport.Width),h=Math.Min(size.Height,viewport.Height);
            double x=Math.Max(viewport.Left,Math.Min(box.Left,viewport.Right-w));
            // Only clamp to the frame edge; never move to separate collision-avoidance lanes.
            return new Rect(x,Math.Max(viewport.Top,Math.Min(box.Top-h-3,viewport.Bottom-h)),w,h);
        }
    }
}
