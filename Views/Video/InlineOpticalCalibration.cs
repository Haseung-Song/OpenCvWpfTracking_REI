using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;
using OpenCvWpfTracking.Services.Control;
using OpenCvWpfTracking.ViewModels.Main;

// 2026-10-16: 채널별 4점 십자선 보정과 기준점 시각화.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking
{
    public sealed class OpticalCalibrationSession
    {
        #region [필드 및 상태 정보]

        private readonly List<Point> points = new List<Point>();

        private readonly List<int> zooms = new List<int>();

        #endregion

        #region [속성 및 상태 표시]

        public bool Active { get; private set; }

        public bool FourPoint { get; private set; }

        public Point? Candidate { get; private set; }

        public Point Pose { get; private set; }

        public int CandidateZoom { get; private set; }

        public string Device { get; private set; }

        public string Status { get; private set; } = "수동 계산(4점)을 선택하세요.";

        public IReadOnlyList<Point> SelectedPoints => points;

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 현재 위치와 장비 기준을 고정하여 십자선 보정 점 선택을 시작한다.
        /// </summary>
        public void Begin(bool four, Point pose, string device)
        {
            Cancel();
            Active = true;
            FourPoint = four;
            Pose = pose;
            Device = device;
            Status = four ? "1/4 · 낮은 줌에서 기준물 A 클릭 (PAN/TILT 고정)" : "십자선 중심으로 사용할 위치를 클릭하세요.";
        }

        /// <summary>
        /// 진행 중인 후보와 선택점을 버리고 기존 저장값을 유지한다.
        /// </summary>
        public void Cancel()
        {
            Active = false;
            Candidate = null;
            points.Clear();
            zooms.Clear();
            Status = "보정 종료 · 저장값 유지";
        }

        /// <summary>
        /// PAN 순환 경계를 고려하여 두 장비 위치의 최대 차이를 구한다.
        /// </summary>
        public static double PoseError(Point a, Point b)
        {
            double x = Math.Abs(a.X - b.X) % 360;
            return Math.Max(Math.Min(x, 360 - x), Math.Abs(a.Y - b.Y));
        }

        /// <summary>
        /// 선택점과 줌 및 장비 위치를 검증하고 네 점의 교점을 계산한다.
        /// </summary>
        public void Select(Point point, int zoom, Point pose, string device, bool fresh)
        {
            if (!Active)
                return;

            if (device != Device)
            {
                Cancel();
                Status = "장비/채널 변경 · 다시 시작하세요.";
                return;
            }

            if (!fresh)
            {
                Status = "최신 위치·줌 수신값을 기다리세요.";
                return;
            }

            if (!OpticalCenterCalibration.Valid(point.X, point.Y))
                return;

            if (PoseError(Pose, pose) > .005)
            {
                Cancel();
                Status = "PAN/TILT 변경 감지 · 다시 시작하세요.";
                return;
            }

            if (!FourPoint)
            {
                Candidate = point;
                CandidateZoom = zoom;
                Status = "미리보기 · 위치 확인 후 저장 (장비는 이동하지 않음)";
                return;
            }

            if (Candidate.HasValue)
            {
                Status = "계산 완료 · 저장 또는 취소하세요.";
                return;
            }

            points.Add(point);
            zooms.Add(zoom);

            if (points.Count < 4)
            {
                Status = points.Count == 1 ? "2/4 · 같은 낮은 줌에서 기준물 B 클릭" : points.Count == 2 ? "3/4 · 줌만 확대 후 같은 기준물 A 클릭" : "4/4 · 같은 높은 줌에서 기준물 B 클릭";
                return;
            }

            Point center;

            if (Math.Abs(zooms[0] - zooms[1]) > 12 ||
                Math.Abs(zooms[2] - zooms[3]) > 12 ||
                zooms[2] - zooms[0] < 50 ||
                !OpticalCenterCalibration.TryIntersection(
                points[0],
                points[1],
                points[2],
                points[3],
                out center
            ))
            {
                Cancel();
                Status = "교점 계산 불가 · 줌 차이/대응점 확인 후 다시 시작";
                return;
            }

            Candidate = center;
            CandidateZoom = zoom;
            Status = "교점 계산 완료 · 십자선 미리보기 확인 후 저장";
        }

        /// <summary>
        /// 후보와 현재 장비 및 줌 상태가 저장 조건을 만족하는지 확인한다.
        /// </summary>
        public bool CanSave(
            int zoom,
            Point pose,
            string device,
            bool fresh
        ) =>
            Active &&
            Candidate.HasValue &&
            fresh &&
            zoom == CandidateZoom &&
            device == Device &&
            PoseError(
            Pose,
            pose
        ) <= .005;

        #endregion
    }

    internal sealed class InlineOpticalCalibration
    {
        #region [필드 및 상태 정보]

        private static readonly ConditionalWeakTable<FrameworkElement, InlineOpticalCalibration> controllers = new ConditionalWeakTable<FrameworkElement, InlineOpticalCalibration>();

        private readonly FrameworkElement image;

        private readonly MainViewModel vm;

        private readonly Button fourPointButton;

        private readonly Func<bool> ir;

        private readonly OpticalCalibrationSession session = new OpticalCalibrationSession();

        private readonly TextBlock status;

        private readonly ToggleButton settingsToggle;

        private readonly Border settingsBorder;

        private readonly TextBlock pickingHint;

        private readonly Canvas calibrationOverlay;

        private readonly List<Point> drawnPoints = new List<Point>();

        private Point? drawnCenter;

        private Rect markerArea = Rect.Empty;

        private int markerVersion, paintedVersion = -1;

        private string overlayDeviceKey;

        private bool hidingForSelection;

        #endregion

        #region [속성 및 상태 표시]

        internal FrameworkElement SettingsPanel { get; private set; }

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 영상에 연결된 십자선 설정 요소를 반환한다.
        /// </summary>
        internal static FrameworkElement SettingsFor(FrameworkElement image)
        {
            InlineOpticalCalibration c;
            return image != null && controllers.TryGetValue(image, out c) ? c.SettingsPanel : null;
        }

        /// <summary>
        /// 입력 위치가 보정 설정 UI에 속하는지 확인하여 영상 이동 입력과 분리한다.
        /// </summary>
        internal static bool IsSettingsHit(FrameworkElement image, DependencyObject hit)
        {
            InlineOpticalCalibration c;

            if (image == null || hit == null || !controllers.TryGetValue(image, out c))
                return false;

            return hit == c.settingsToggle ||
                hit == c.settingsBorder ||
                c.settingsToggle.IsAncestorOf(hit) ||
                c.settingsBorder.IsAncestorOf(hit);
        }

        #endregion

        #region [속성 및 상태 표시]

        internal Point? Preview
        {
            get
            {
                if (session.Active && session.Device != vm.OpticalDeviceKey(ir()))
                {
                    session.Cancel();
                    Refresh();
                }

                return session.Active ? session.Candidate : null;
            }
        }

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 해당 영상에서 보정 점 선택을 진행 중인지 확인한다.
        /// </summary>
        internal static bool IsActive(FrameworkElement image)
        {
            InlineOpticalCalibration c;
            return image != null && controllers.TryGetValue(image, out c) && c.session.Active;
        }

        #endregion

        #region [초기화]

        internal InlineOpticalCalibration(Grid host, FrameworkElement viewport, MainViewModel model, Func<bool> infrared)
        {
            image = viewport;
            vm = model;
            ir = infrared;
            controllers.Add(image, this);
            var tools = new StackPanel();
            // 2026-10-16: 작은 영상 설정을 사용하고 점 선택 중에는 패널을 접어 얇은 기준 도형을 표시한다.
            SettingsPanel = tools;
            var header = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            var visible = new CheckBox
            {
                Content = header,
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(3, 2, 3, 2),
                ToolTip = "십자선 표시/숨김은 EO / IR 공통 적용"
            };
            visible.SetBinding(
                ToggleButton.IsCheckedProperty,
                new Binding("IsCrosshairVisible") { Source = vm, Mode = BindingMode.TwoWay }
            );
            tools.Children.Add(visible);
            var buttons = new Grid();
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tools.Children.Add(buttons);
            fourPointButton = Add(buttons, 0, "수동 계산(4점)", () => Begin(true));
            Add(buttons, 1, "기본 중심", () =>
            {
                try
                {
                    session.Cancel();
                    ClearGeometry();
                    vm.ResetOpticalCenter(ir());
                    Notice("기본 중심 복원 완료");
                }
                catch (Exception e)
                {
                    OpenCvWpfTracking.Common.ConsoleLogHelper.Error("십자선 보정", "기본 중심 복원 실패 · 기존 저장값 유지", e);
                    Notice(e.Message);
                }
            });
            status = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(3, 2, 3, 2),
                Visibility = Visibility.Collapsed
            };
            tools.Children.Add(status);
            settingsToggle = new ToggleButton
            {
                Content = "십자선 ⚙",
                Width = 76,
                Height = 22,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = LegacyVideoIntegration.ChannelBrush(ir()),
                Background = LegacyVideoIntegration.FrozenBrush("#E61B2229"),
                BorderBrush = LegacyVideoIntegration.FrozenBrush("#53616D"),
                Template = VideoShortcutGuide.RatioToggleTemplate(),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                ToolTip = "이 영상의 십자선 표시 및 중심 보정 설정"
            };
            settingsBorder = new Border
            {
                Child = new ScrollViewer
                {
                    Content = tools,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                },
                Background = LegacyVideoIntegration.FrozenBrush("#F21B2229"),
                BorderBrush = settingsToggle.Foreground,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Visibility = Visibility.Collapsed
            };
            pickingHint = new TextBlock
            {
                FontSize = 11,
                Foreground = Brushes.White,
                Background = LegacyVideoIntegration.FrozenBrush("#E61B2229"),
                Padding = new Thickness(3),
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            calibrationOverlay = new Canvas
            {
                IsHitTestVisible = false
            };
            Panel.SetZIndex(calibrationOverlay, 85);
            host.Children.Add(calibrationOverlay);
            Panel.SetZIndex(settingsToggle, 90);
            Panel.SetZIndex(settingsBorder, 91);
            host.Children.Add(settingsToggle);
            host.Children.Add(settingsBorder);
            Panel.SetZIndex(pickingHint, 90);
            host.Children.Add(pickingHint);
            settingsToggle.Checked += (s, e) =>
            {
                Refresh();
                settingsBorder.Visibility = Visibility.Visible;
            };
            settingsToggle.Unchecked += (s, e) =>
            {
                if (!hidingForSelection)
                {
                    session.Cancel();
                    ClearGeometry();
                }

                Refresh();
                settingsBorder.Visibility = Visibility.Collapsed;
            };
            bool previousIr = ir();
            VideoShortcutGuide.WatchOverlayLayout(
                host,
                () =>
            {
                bool channel = ir();

                if (channel != previousIr || overlayDeviceKey != null && overlayDeviceKey != vm.OpticalDeviceKey(channel))
                {
                    session.Cancel();
                    ClearGeometry();
                    settingsToggle.IsChecked = false;
                    Refresh();
                    previousIr = channel;
                }

                header.Text = (channel ? "IR" : "EO") + " 십자선 표시";
                RefreshDigitalZoomAvailability();
                settingsToggle.Foreground = LegacyVideoIntegration.ChannelBrush(channel);
                settingsBorder.BorderBrush = settingsToggle.Foreground;
                bool connected = (channel ? vm.IrStatusText : vm.EoStatusText).EndsWith("] Connected", StringComparison.Ordinal);
                Rect area = VideoShortcutGuide.VideoContentRect(
                    host,
                    image,
                    VideoShortcutGuide.SourceAspect(
                    image,
                    channel ? 1.25 : 16.0 / 9
                )
                );

                if (!connected || area.IsEmpty || area.Width < 100 || area.Height < 80)
                {
                    if (session.Active)
                    {
                        session.Cancel();
                        Refresh();
                    }

                    ClearGeometry();
                    settingsToggle.IsChecked = false;
                    settingsToggle.Visibility = Visibility.Collapsed;
                    return;
                }

                settingsToggle.Visibility = Visibility.Visible;
                double bottom = Math.Max(0, host.ActualHeight - area.Bottom);
                settingsToggle.Margin = new Thickness(area.Left + 8, 0, 0, bottom + 33);
                settingsBorder.Margin = new Thickness(area.Left + 8, 0, 0, bottom + 59);
                // 보더 폭은 고정 EO 크기가 아닌 실제 원본 영상 표시 영역을 따른다.
                double width = Math.Min(238, Math.Max(174, area.Width * .36));
                settingsBorder.Width = Math.Min(width, area.Width - 16);
                settingsBorder.MaxHeight = Math.Max(1, Math.Min(area.Height * .36, area.Height - 67));
                double font = area.Width < 400 ? 10 : 11;
                header.FontSize = font;

                foreach (var button in buttons.Children.OfType<Button>())
                    button.FontSize = font;

                pickingHint.Margin = new Thickness(area.Left + 90, 0, 0, bottom + 33);
                pickingHint.MaxWidth = Math.Max(1, area.Width - 98);
                DrawGeometry(area);
            }
            );
            host.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler((s, e) =>
            {
                if (IsSettingsHit(image, e.OriginalSource as DependencyObject))
                    return;

                if (!session.Active)
                    return;

                for (DependencyObject hit = e.OriginalSource as DependencyObject; hit != null; hit = hit is Visual ? VisualTreeHelper.GetParent(hit) : null)
                    if (hit is System.Windows.Controls.Primitives.ButtonBase || hit is Expander)
                        return;

                var bitmap = ir() ? vm.IRCameraImage : vm.EOCameraImage;

                if (bitmap == null)
                    return;

                double scale = Math.Min(image.ActualWidth / bitmap.PixelWidth, image.ActualHeight / bitmap.PixelHeight);

                if (scale <= 0)
                    return;

                Point p = e.GetPosition(image);
                p = new Point(
                    (p.X - (image.ActualWidth - bitmap.PixelWidth * scale) / 2) / (bitmap.PixelWidth * scale),
                    (p.Y - (image.ActualHeight - bitmap.PixelHeight * scale) / 2) / (bitmap.PixelHeight * scale)
                );
                SelectPoint(p);
                e.Handled = true;
            }),
                true
            );
            image.Unloaded += (s, e) =>
            {
                session.Cancel();
                ClearGeometry();
                settingsToggle.IsChecked = false;
                Refresh();
            };
        }

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 현재 위치와 장비 기준을 고정하여 십자선 보정 점 선택을 시작한다.
        /// </summary>
        private void Begin(bool four)
        {
            RefreshDigitalZoomAvailability();

            if (vm.IsOpticalCalibrationBlockedByDigitalZoom(ir()))
                return;

            if (vm.IsControlInputLocked || !vm.OpticalFeedbackFresh(ir()))
            {
                Notice("장비 작업 종료·최신 위치/줌 수신 후 시작하세요.");
                return;
            }

            ClearGeometry();
            overlayDeviceKey = vm.OpticalDeviceKey(ir());
            status.Visibility = Visibility.Collapsed;
            vm.ReleaseControlOwner("video");
            session.Begin(four, vm.OpticalPose, overlayDeviceKey);
            // 작은 IR 패널이 영상 중심을 가리지 않도록 점 선택 시 패널을 접는다.
            hidingForSelection = true;

            try
            {
                settingsToggle.IsChecked = false;
            }
            finally
            {
                hidingForSelection = false;
            }

            Refresh();
        }

        /// <summary>
        /// 현재 상태와 표시 영역에 맞춰 화면을 갱신한다.
        /// </summary>
        private void Refresh()
        {
            RefreshDigitalZoomAvailability();
            pickingHint.Text = session.Status;
            pickingHint.Visibility = session.Active && settingsToggle.IsChecked != true ? Visibility.Visible : Visibility.Collapsed;
            settingsToggle.ToolTip = session.Active ? session.Status : "이 영상의 십자선 표시 및 중심 보정 설정";
        }

        /// <summary>
        /// 보정 결과 또는 오류 안내를 작은 설정 패널에 표시한다.
        /// </summary>
        private void Notice(string text)
        {
            status.Text = text;
            status.ToolTip = text;
            status.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 선택점을 시각화하고 유효한 네 점이 완성되면 보정값을 적용한다.
        /// </summary>
        private void SelectPoint(Point point)
        {
            RefreshDigitalZoomAvailability();

            if (vm.IsOpticalCalibrationBlockedByDigitalZoom(ir()))
                return;

            session.Select(point, vm.OpticalZoom(ir()), vm.OpticalPose, vm.OpticalDeviceKey(ir()), vm.OpticalFeedbackFresh(ir()));
            drawnPoints.Clear();
            drawnPoints.AddRange(session.SelectedPoints);
            drawnCenter = session.Candidate;
            markerVersion++;
            Refresh();

            if (session.Candidate.HasValue)
            {
                Save();
                settingsToggle.IsChecked = true;
            }
            else if (!session.Active)
            {
                Notice(session.Status);
                settingsToggle.IsChecked = true;
            }
        }

        /// <summary>
        /// 조건 검사를 통과한 십자선 중심을 저장한다.
        /// </summary>
        private void Save()
        {
            RefreshDigitalZoomAvailability();

            if (vm.IsOpticalCalibrationBlockedByDigitalZoom(ir()))
                return;

            try
            {
                if (vm.IsControlInputLocked)
                    throw new InvalidOperationException("장비 작업 중 · 다시 계산하세요.");

                if (!session.CanSave(vm.OpticalZoom(ir()), vm.OpticalPose, vm.OpticalDeviceKey(ir()), vm.OpticalFeedbackFresh(ir())))
                    throw new InvalidOperationException("후보/현재 위치·줌 수신값 확인 후 다시 선택하세요.");

                vm.SaveOpticalCenter(ir(), session.Candidate.Value);
                session.Cancel();
                Refresh();
                Notice("보정 적용 완료");
            }
            catch (Exception e)
            {
                OpenCvWpfTracking.Common.ConsoleLogHelper.Error("십자선 보정", "보정 적용 실패 · 기존 저장값 유지", e);
                Notice(e.Message);
            }
        }

        /// <summary>
        /// 현재 영상의 임시 기준점과 연결선을 제거한다.
        /// </summary>
        private void ClearGeometry()
        {
            if (drawnPoints.Count == 0 && !drawnCenter.HasValue && overlayDeviceKey == null)
                return;

            drawnPoints.Clear();
            drawnCenter = null;
            overlayDeviceKey = null;
            markerVersion++;

            if (calibrationOverlay != null && calibrationOverlay.Children.Count > 0)
                calibrationOverlay.Children.Clear();
        }

        /// <summary>
        /// 원본 영상 표시 영역을 기준으로 기준점과 교점 연결선을 그린다.
        /// </summary>
        private void DrawGeometry(Rect area)
        {
            if (area == markerArea && paintedVersion == markerVersion)
                return;

            markerArea = area;
            paintedVersion = markerVersion;
            calibrationOverlay.Children.Clear();
            calibrationOverlay.Clip = new RectangleGeometry(area);

            if (area.IsEmpty || drawnPoints.Count == 0)
                return;

            var points = drawnPoints.Select(p => new Point(area.Left + p.X * area.Width, area.Top + p.Y * area.Height)).ToArray();
            Action<int, int, Brush, bool> line = (a, b, color, dashed) =>
            {
                if (points.Length <= b)
                    return;

                Point start = points[a], end = points[b];

                if (!dashed && drawnCenter.HasValue)
                {
                    var c = drawnCenter.Value;
                    end = new Point(area.Left + c.X * area.Width, area.Top + c.Y * area.Height);
                    start = (points[a] - end).LengthSquared > (points[b] - end).LengthSquared ? points[a] : points[b];
                }

                var shape = new Line
                {
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = end.X,
                    Y2 = end.Y,
                    Stroke = color,
                    StrokeThickness = dashed ? 1 : 1.3,
                    Opacity = .85
                };

                if (dashed)
                    shape.StrokeDashArray = new DoubleCollection
                    {
                        3,
                        3
                    };

                calibrationOverlay.Children.Add(shape);
            };
            line(0, 1, Brushes.Cyan, true);
            line(2, 3, Brushes.Gold, true);
            line(0, 2, Brushes.Cyan, false);
            line(1, 3, Brushes.Gold, false);

            for (int i = 0; i < points.Length; i++)
            {
                var dot = new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = i < 2 ? Brushes.Cyan : Brushes.Gold,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(dot, points[i].X - 3.5);
                Canvas.SetTop(dot, points[i].Y - 3.5);
                calibrationOverlay.Children.Add(dot);
                var label = new TextBlock
                {
                    Text = (i % 2 == 0 ? "A" : "B") + (i < 2 ? "1" : "2"),
                    FontSize = 10,
                    Foreground = dot.Fill,
                    Background = LegacyVideoIntegration.FrozenBrush("#BB101820"),
                    Padding = new Thickness(2, 0, 2, 0)
                };
                Canvas.SetLeft(label, Math.Max(area.Left, Math.Min(area.Right - 24, points[i].X + 5)));
                Canvas.SetTop(label, Math.Max(area.Top, Math.Min(area.Bottom - 16, points[i].Y - 17)));
                calibrationOverlay.Children.Add(label);
            }

            if (drawnCenter.HasValue)
            {
                var p = drawnCenter.Value;
                var ring = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Stroke = Brushes.OrangeRed,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(ring, area.Left + p.X * area.Width - 6);
                Canvas.SetTop(ring, area.Top + p.Y * area.Height - 6);
                calibrationOverlay.Children.Add(ring);
            }
        }

        /// <summary>
        /// 지정 열에 동일한 형식의 작은 보정 버튼을 추가한다.
        /// </summary>
        private static Button Add(Grid panel, int column, string text, Action action)
        {
            var button = new Button
            {
                Content = text,
                FontSize = 11,
                Padding = new Thickness(3, 2, 3, 2),
                Margin = new Thickness(2)
            };
            button.Click += (s, e) => action();
            Grid.SetColumn(button, column);
            panel.Children.Add(button);
            return button;
        }

        /// <summary>
        /// 체크 확인 없이 채널의 D-ZOOM 요청 이력으로 계산 버튼만 갱신한다.
        /// 계산 중 ON 전환 시 임시 점만 버리고 기존 저장 중심을 유지한다.
        /// </summary>
        private void RefreshDigitalZoomAvailability()
        {
            bool blocked = vm.IsOpticalCalibrationBlockedByDigitalZoom(ir());
            fourPointButton.IsEnabled = !blocked;

            if (!blocked || !session.Active)
                return;

            session.Cancel();
            ClearGeometry();
            pickingHint.Visibility = Visibility.Collapsed;
            OpenCvWpfTracking.Common.ConsoleLogHelper.State(
                "OPTICAL CENTER",
                (ir() ? "IR" : "EO") + " / CALIBRATION_CANCELED / DIGITAL_ZOOM_ON"
            );
        }

        #endregion
    }
}
