using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using System.Globalization;
using OpenCvWpfTracking.Common;

// 2026-10-16: 실제 영상 영역의 단축키·배율·경고 배치.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking
{
    // 2026-10-12: 메인 EO/IR과 분리창의 단축키 안내 내용과 형식을 공유한다.
    internal static class VideoShortcutGuide
    {
        #region [기능 처리 함수]

        /// <summary>
        /// 영상 영역 크기에 맞는 단축키 안내 배율을 계산한다.
        /// </summary>
        internal static double Scale(double width, double height) => Math.Max(.4, Math.Min(1.15, Math.Min(width / 1280, height / 720)));

        // IR 서브 영상에서도 문구 가독성을 유지하고 도움말만 추가 축소를 허용한다.
        /// <summary>
        /// 작은 영상에서도 읽을 수 있는 표시 문구 배율을 계산한다.
        /// </summary>
        internal static double LabelScale(double width, double height) => Math.Max(
            1.0,
            Math.Min(
            1.3,
            Math.Min(
            width / 1280,
            height / 720
        )
        )
        );

        // 2026-10-15: 두 채널과 모든 창에서 같은 Uniform 원본 영상 표시 영역을 사용한다.
        /// <summary>
        /// 실제 영상 원본의 종횡비를 읽고 없으면 채널 기본값을 사용한다.
        /// </summary>
        internal static double SourceAspect(FrameworkElement video, double fallback)
        {
            ImageSource source = (video as Image)?.Source ?? ((video as System.Windows.Shapes.Shape)?.Fill as ImageBrush)?.ImageSource;
            return source != null && source.Width > 0 && source.Height > 0 ? source.Width / source.Height : fallback;
        }

        /// <summary>
        /// 검은 여백을 제외한 실제 원본 영상의 표시 사각형을 구한다.
        /// </summary>
        internal static Rect VideoContentRect(FrameworkElement host, FrameworkElement video, double aspect)
        {
            if (host == null || video == null || video.ActualWidth <= 0 || video.ActualHeight <= 0)
                return Rect.Empty;

            if (!ReferenceEquals(host, video) && !host.IsAncestorOf(video))
                return Rect.Empty;

            if (!IsFinite(aspect) || aspect <= 0)
                return Rect.Empty;

            double width = Math.Min(video.ActualWidth, video.ActualHeight * aspect), height = width / aspect;
            Point a = video.TranslatePoint(new Point((video.ActualWidth - width) / 2, (video.ActualHeight - height) / 2), host);
            Point b = video.TranslatePoint(new Point((video.ActualWidth + width) / 2, (video.ActualHeight + height) / 2), host);
            return new Rect(a, b);
        }

        /// <summary>
        /// 화면 부착과 분리 수명주기에 맞춰 오버레이 배치 갱신을 구독한다.
        /// </summary>
        internal static void WatchOverlayLayout(FrameworkElement host, Action update)
        {
            bool reported = false;
            EventHandler handler = (s, e) =>
            {
                try
                {
                    update();
                }
                catch (Exception error)
                {
                    if (!reported)
                    {
                        reported = true;
                        ConsoleLogHelper.Error("VIDEO UI / CONTENT RECT", "Overlay update skipped; video unchanged", error);
                    }
                }
            };
            host.Loaded += (s, e) =>
            {
                host.LayoutUpdated -= handler;
                host.LayoutUpdated += handler;
                handler(s, EventArgs.Empty);
            };
            host.Unloaded += (s, e) => host.LayoutUpdated -= handler;

            if (host.IsLoaded)
            {
                host.LayoutUpdated += handler;
                handler(host, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 실제 영상 우측 상단에 화재와 연기 경고를 겹치지 않게 배치한다.
        /// </summary>
        internal static void AttachVideoAlarmPositions(
            FrameworkElement host,
            StackPanel panel,
            FrameworkElement video,
            Func<double> aspect
        )
        {
            if (host == null || panel == null || video == null || aspect == null)
                return;

            Rect previous = Rect.Empty;
            double previousWidth = -1, previousHeight = -1;
            WatchOverlayLayout(host, () =>
            {
                Rect area = VideoContentRect(host, video, aspect());

                if (area.IsEmpty)
                    return;

                if (area == previous && host.ActualWidth == previousWidth && host.ActualHeight == previousHeight)
                    return;

                previous = area;
                previousWidth = host.ActualWidth;
                previousHeight = host.ActualHeight;
                double scale = Math.Max(.6, Math.Min(1, Math.Min(area.Width / 1000, area.Height / 600)));
                panel.HorizontalAlignment = HorizontalAlignment.Right;
                panel.VerticalAlignment = VerticalAlignment.Top;
                panel.Margin = new Thickness(0, area.Top + Math.Max(4, 10 * scale), Math.Max(0, host.ActualWidth - area.Right) + 8, 0);
                panel.MaxWidth = Math.Max(1, area.Width / 2 - 28);

                foreach (var child in panel.Children)
                    if (child is Border banner && banner.Child is TextBlock text)
                    {
                        text.FontSize = Math.Max(11, 18 * scale);
                        text.TextWrapping = TextWrapping.Wrap;
                        text.TextAlignment = TextAlignment.Center;
                        banner.Padding = new Thickness(10 * scale, 4 * scale, 10 * scale, 4 * scale);
                        banner.BorderThickness = new Thickness(scale < .8 ? 1 : 2);

                        if (text.Text.Contains("FIRE"))
                        {
                            text.Foreground = LegacyVideoIntegration.EventBrush("FIRE");
                            banner.BorderBrush = text.Foreground;
                        }
                    }
            });
        }

        /// <summary>
        /// 연결 상태와 실제 영상 영역에 따라 배율 및 연결 문구를 배치한다.
        /// </summary>
        internal static void AttachVideoLabelPositions(
            FrameworkElement host,
            FrameworkElement video,
            FrameworkElement ratio,
            TextBlock status,
            Func<double> aspect
        )
        {
            if (host == null || video == null || ratio == null || status == null || aspect == null)
                return;

            Thickness originalRatio = ratio.Margin, originalStatus = status.Margin;
            Rect previous = Rect.Empty;
            Size previousSize = new Size();
            bool? previousConnected = null;
            WatchOverlayLayout(host, () =>
            {
                bool connected = (status.Text ?? "").EndsWith("] Connected", StringComparison.Ordinal);
                Rect area = connected ? VideoContentRect(host, video, aspect()) : Rect.Empty;
                Size size = new Size(host.ActualWidth, host.ActualHeight);

                if (previous == area && previousSize == size && previousConnected == connected)
                    return;

                if (connected && area.IsEmpty)
                    return;

                Thickness r = originalRatio, s = originalStatus;

                if (connected)
                {
                    double bottom = Math.Max(0, host.ActualHeight - area.Bottom) + 1;
                    r = new Thickness(area.Left + 8, 0, 0, bottom);
                    s = new Thickness(0, 0, Math.Max(0, host.ActualWidth - area.Right) + 8, bottom);
                }

                if (ratio.Margin != r)
                    ratio.Margin = r;

                if (status.Margin != s)
                    status.Margin = s;

                previous = area;
                previousSize = size;
                previousConnected = connected;
            });
        }

        /// <summary>
        /// 값이 유효한 유한수인지 확인한다.
        /// </summary>
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        /// <summary>
        /// 연결 상태와 배율 표시 설정을 문구 표시 여부에 연결한다.
        /// </summary>
        internal static void BindRatioVisibility(TextBlock label, bool ir)
        {
            var binding = new MultiBinding
            {
                Converter = new ConnectedRatioVisibilityConverter()
            };
            binding.Bindings.Add(new Binding(ir ? "IrStatusText" : "EoStatusText"));
            binding.Bindings.Add(new Binding("IsZoomRatioOverlayVisible"));
            label.SetBinding(UIElement.VisibilityProperty, binding);
        }

        /// <summary>
        /// 영상 연결 상태를 배율 버튼 표시 여부에 연결한다.
        /// </summary>
        internal static void BindRatioToggleVisibility(FrameworkElement toggle, bool ir) =>
            toggle.SetBinding(
            UIElement.VisibilityProperty,
            new Binding(ir ? "IrStatusText" : "EoStatusText") { Converter = new ConnectedVideoVisibilityConverter() }
        );

        /// <summary>
        /// 연결과 클릭 이동 설정을 방향 안내 표시 여부에 연결한다.
        /// </summary>
        internal static void BindJogVisibility(FrameworkElement overlay, bool ir)
        {
            var binding = new MultiBinding
            {
                Converter = new ConnectedRatioVisibilityConverter()
            };
            binding.Bindings.Add(new Binding(ir ? "IrStatusText" : "EoStatusText"));
            binding.Bindings.Add(new Binding("IsVideoClickCenterEnabled"));
            overlay.SetBinding(UIElement.VisibilityProperty, binding);
        }

        /// <summary>
        /// 기존 채널색을 사용하는 배율 표시 토글 버튼을 만든다.
        /// </summary>
        internal static ToggleButton CreateRatioToggle(bool ir)
        {
            var toggle = new ToggleButton
            {
                Content = "배율",
                Width = 36,
                Height = 22,
                Foreground = LegacyVideoIntegration.ChannelBrush(ir),
                Background = new SolidColorBrush(Color.FromArgb(225, 27, 34, 41)),
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#53616D"),
                BorderThickness = new Thickness(1),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Template = RatioToggleTemplate(),
                ToolTip = "EO/IR 메인·서브·분리 영상의 배율 표시/숨기기"
            };
            toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsZoomRatioOverlayVisible") { Mode = BindingMode.TwoWay });
            BindRatioToggleVisibility(toggle, ir);
            return toggle;
        }

        /// <summary>
        /// 배율과 십자선 버튼에서 공유하는 작은 토글 형식을 반환한다.
        /// </summary>
        internal static ControlTemplate RatioToggleTemplate() =>
            (ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ToggleButton'>
<Border x:Name='Surface' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='2'>
<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border>
<ControlTemplate.Triggers>
<Trigger Property='IsChecked' Value='True'><Setter TargetName='Surface' Property='Background' Value='#EC26333F'/></Trigger>
<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Surface' Property='Background' Value='#F0354654'/></Trigger>
<Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Surface' Property='BorderBrush' Value='#C5D2DD'/></Trigger>
</ControlTemplate.Triggers></ControlTemplate>");

        // 2026-10-12: 분리창 안내와 같은 사각 화살표와 오버레이 여백 및 색상을 사용한다.
        /// <summary>
        /// 기존 화살표와 펼침 안내를 사용하는 단축키 UI 형식을 반환한다.
        /// </summary>
        internal static ControlTemplate MainGuideTemplate() =>
            (ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Expander'>
<Grid><Border x:Name='Info' Visibility='Collapsed' Padding='42,7,10,7' Background='#B21D232A' BorderBrush='{TemplateBinding Foreground}' BorderThickness='1' CornerRadius='3'><ContentPresenter Content='{TemplateBinding Content}'/></Border>
<ToggleButton x:Name='Arrow' Width='28' Height='28' Margin='4,4,0,0' Padding='0' HorizontalAlignment='Left' VerticalAlignment='Top' Content='▼' FontSize='15' FontWeight='Bold' Foreground='White' Background='#B21D232A' BorderBrush='{TemplateBinding Foreground}' BorderThickness='1' IsChecked='{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' ToolTip='단축키 안내 표시하기'/></Grid>
<ControlTemplate.Triggers><Trigger Property='IsExpanded' Value='True'><Setter TargetName='Info' Property='Visibility' Value='Visible'/><Setter TargetName='Arrow' Property='Content' Value='▲'/><Setter TargetName='Arrow' Property='ToolTip' Value='단축키 안내 숨기기'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");

        /// <summary>
        /// 해당 채널의 기존 단축키 안내 내용을 구성한다.
        /// </summary>
        internal static StackPanel CreateContent(bool ir)
        {
            var panel = new StackPanel();
            Action<string, string, double, bool> add = (
                text,
                color,
                size,
                bold
            ) => panel.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = (Brush)new BrushConverter().ConvertFromString(color),
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(
                0,
                panel.Children.Count == 0 ? 0 : 4,
                0,
                0
            )
            });
            add(
                (ir ? "IR" : "EO") + " CAMERA / SHORTCUT CONTROL",
                ir ? LegacyVideoIntegration.IrColor : LegacyVideoIntegration.EoColor,
                18,
                true
            );
            add("R : EO MAIN    T : IR MAIN", "#FFD166", 13, true);
            add("ARROW KEYS : PAN / TILT CONTROL", "#9FE870", 13, true);
            add("Z : PT SPEED −    C : PT SPEED +  (HOLD)", "#E5B7FF", 13, true);
            add("W : ZOOM IN    S : ZOOM OUT", "#8FD3FF", 13, true);
            add("A : FOCUS NEAR    D : FOCUS FAR", "#FFBC8A", 13, true);
            add("키를 떼면 해당 입력이 정지합니다.", "#D0D0D0", 12, false);
            var warning = new TextBlock
            {
                Text = "자동 운용 / 잠금 중에는 수동 제어가 제한됩니다.",
                Foreground = Brushes.Salmon,
                FontSize = 13,
                Margin = new Thickness(0, 7, 0, 0)
            };
            warning.SetBinding(
                UIElement.VisibilityProperty,
                new Binding("IsControlInputLocked") { Converter = new BooleanToVisibilityConverter() }
            );
            panel.Children.Add(warning);
            return panel;
        }

        #endregion
    }

    internal sealed class DeviceOnOffBrushConverter : IValueConverter
    {
        #region [필드 및 상태 정보]

        private static readonly Brush OnBrush = LegacyVideoIntegration.FrozenBrush("#55D187");

        private static readonly Brush OffBrush = LegacyVideoIntegration.FrozenBrush("#FF6B6B");

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 화면 바인딩 값을 해당 표시 형식으로 변환한다.
        /// </summary>
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) =>
            string.Equals(
            value as string,
            "ON",
            StringComparison.OrdinalIgnoreCase
        ) ? OnBrush : OffBrush;

        /// <summary>
        /// 역방향 바인딩에서 사용할 값을 반환한다.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

        #endregion
    }

    internal sealed class ConnectedVideoVisibilityConverter : IValueConverter
    {
        #region [기능 처리 함수]

        /// <summary>
        /// 화면 바인딩 값을 해당 표시 형식으로 변환한다.
        /// </summary>
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) =>
            value is string status &&
            status.EndsWith(
            "] Connected",
            StringComparison.Ordinal
        ) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 역방향 바인딩에서 사용할 값을 반환한다.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

        #endregion
    }

    internal sealed class ConnectedRatioVisibilityConverter : IMultiValueConverter
    {
        #region [기능 처리 함수]

        /// <summary>
        /// 화면 바인딩 값을 해당 표시 형식으로 변환한다.
        /// </summary>
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) =>
            values.Length == 2 &&
            values[0] is string status &&
            status.EndsWith(
            "] Connected",
            StringComparison.Ordinal
        ) &&
            values[1] is bool visible &&
            visible ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 역방향 바인딩에서 사용할 값을 반환한다.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => new object[]
        {
            Binding.DoNothing,
            Binding.DoNothing
        };

        #endregion
    }
}
