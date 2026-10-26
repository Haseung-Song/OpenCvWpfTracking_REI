using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Input;

// 2026-10-16: 장비 제어 옵션과 영상 오버레이 UI 연결.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking
{
    public partial class MainWindow
    {
        #region [필드 및 상태 정보]

        private GlobalControlKeyboard _globalKeyboard;

        private bool _v30UiInitialized;

        private readonly List<Grid> _videoJogOverlays = new List<Grid>();

        #endregion

        #region [영상 배치 보정]

        /// <summary>
        /// IR이 보조 화면일 때 원본 전체가 여백 없이 들어가는 높이를 적용한다.
        /// EO/IR 주화면 전환과 디코더·BBox 좌표·클릭 이동 계산은 변경하지 않는다.
        /// </summary>
        private void UpdateAuxiliaryIrAspectLayout()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(UpdateAuxiliaryIrAspectLayout));
                return;
            }

            if (IrVideoContainer == null || CompanyMapPanel == null)
                return;

            if (Grid.GetColumn(IrVideoContainer) != 1)
            {
                if (Math.Abs(CompanyMapPanel.Height - 295) > .1)
                    CompanyMapPanel.Height = 295;

                return;
            }

            var bitmap = IrCameraImageView.Source as System.Windows.Media.Imaging.BitmapSource;
            // 미연결 로고의 비율은 카메라의 원본 비율로 사용하지 않는다.
            bool hasVideo = vm.IrStatusText != null && vm.IrStatusText.EndsWith("] Connected", StringComparison.Ordinal);
            double aspect = hasVideo &&
                bitmap != null &&
                bitmap.PixelWidth > 0 &&
                bitmap.PixelHeight > 0 ? bitmap.PixelWidth / (double)bitmap.PixelHeight : 4.0 / 3.0;
            double width = IrVideoContainer.ActualWidth;

            if (width <= 4 || aspect <= 0)
                return;

            double height = Math.Max(120, Math.Min(522, (width - 4) / aspect + 4));

            if (Math.Abs(IrVideoContainer.Height - height) > .1)
                IrVideoContainer.Height = height;

            double mapHeight = 295 + 345 - height;

            if (Math.Abs(CompanyMapPanel.Height - mapHeight) > .1)
                CompanyMapPanel.Height = mapHeight;
        }

        #endregion

        #region [기능 처리 함수]

        // 2026-10-08: 장비 제어 화면은 기존 보더 색상과 글자 형식을 유지한다.
        /// <summary>
        /// 기존 영상/장비 제어 UI에 옵션과 상태 표시를 한 번만 연결한다.
        /// </summary>
        private void InitializeV30ControlUi()
        {
            // 2026-10-12: 화면 재부착 시 Loaded가 반복되어도 제어 요소와 이벤트를 중복 등록하지 않는다.
            if (_v30UiInitialized)
                return;

            _v30UiInitialized = true;
            vm.InitializeV30Status();
            vm.PropertyChanged += V30PropertyChanged;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.IrStatusText))
                    UpdateAuxiliaryIrAspectLayout();
            };
            // IR 보조 슬롯은 실제 영상 종횡비로 맞추고 남는 높이를 지도에 배분한다.
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                Image.SourceProperty,
                typeof(Image)
            ).AddValueChanged(
                IrCameraImageView,
                (s, e) => UpdateAuxiliaryIrAspectLayout()
            );
            IrVideoContainer.SizeChanged += (s, e) => UpdateAuxiliaryIrAspectLayout();
            Loaded += (s, e) => UpdateAuxiliaryIrAspectLayout();
            AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(InputConfirmKey), true);
            AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(ReturnControlFocus), true);
            var speed = Descendants<Slider>(Content as DependencyObject).FirstOrDefault(s => BindingOperations.GetBinding(
                s,
                Slider.ValueProperty
            )?.Path?.Path == "PanTiltSpeedLevel");

            if (speed != null)
            {
                speed.Minimum = 1;
                speed.Ticks = new DoubleCollection(Enumerable.Range(1, 12).Select(x => x * 5.0));
                speed.TickFrequency = 5;
                speed.LargeChange = 5;
                var outer = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(speed)) as Grid;

                if (outer != null)
                {
                    outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var panel = new StackPanel
                    {
                        Margin = new Thickness(7, 3, 7, 2)
                    };
                    var options = new WrapPanel();
                    var clickMove = new CheckBox
                    {
                        Content = "클릭 이동",
                        Foreground = Brushes.LightSkyBlue,
                        Margin = new Thickness(0, 0, 10, 0),
                        ToolTip = "영상 단일 클릭 중심 이동과 방향 안내 표시를 함께 켜거나 끕니다."
                    };
                    clickMove.SetBinding(
                        ToggleButton.IsCheckedProperty,
                        new Binding("IsVideoClickCenterEnabled") { Mode = BindingMode.TwoWay }
                    );
                    options.Children.Add(clickMove);
                    var fov = new CheckBox
                    {
                        Content = "화각 비례 감속 (배율 근사)",
                        Foreground = Brushes.Turquoise,
                        ToolTip = "합의 배율 기반 근사 감속. 정확한 실측 화각은 아닙니다."
                    };
                    fov.Content = "화각 비례 감속";
                    fov.Margin = new Thickness(0, 0, 10, 0);
                    fov.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsFovSpeedEnabled") { Mode = BindingMode.TwoWay });
                    options.Children.Add(fov);
                    options.Children.Add(CreateFovTuningEditor());
                    // 십자선 도구는 오른쪽 장비 제어가 아닌 각 영상의 배율 버튼 위에 배치한다.
                    panel.Children.Add(new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(
                        68,
                        79,
                        88
                    )),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(
                        126,
                        148,
                        161
                    )),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(
                        5,
                        3,
                        5,
                        3
                    ),
                        Child = options
                    });
                    Grid.SetRow(panel, outer.RowDefinitions.Count - 1);
                    outer.Children.Add(panel);
                }
            }

            AddVideoControls(EoVideoBorder, false);
            AddVideoControls(IrVideoBorder, true);
            // 내장 영상에 마우스를 올리면 해당 PTZF 채널을 선택한다.
            // 두 영상 모두 벗어나면 마지막 렌즈가 아닌 큰 주 영상의 채널로 복귀한다.
            EoVideoBorder.MouseEnter += (s, e) => UpdateVideoControlRoute();
            IrVideoBorder.MouseEnter += (s, e) => UpdateVideoControlRoute();
            EoVideoBorder.MouseLeave += (s, e) => Dispatcher.BeginInvoke(new Action(UpdateVideoControlRoute));
            IrVideoBorder.MouseLeave += (s, e) => Dispatcher.BeginInvoke(new Action(UpdateVideoControlRoute));
            SetVideoJogVisible(vm.IsVideoClickCenterEnabled);
            AddDigitalZoomControls();
            FitLensCards();
            CompactOperationScrollContents();
            PadCommunicationScrollContents();
            ArrangeAlarmBanners(EoVideoBorder);
            ArrangeAlarmBanners(IrVideoBorder);
            AlignConnectionStatusRows();
            CompactCommunicationCard();
        }

        /// <summary>

        /// CTRL/RTSP 카드의 과도한 여백만 줄여 기존 연결/전원 항목이 함께 보이게 한다.

        /// </summary>
        private void CompactCommunicationCard()
        {
            // 2026-10-08: TCP와 RTSP 및 두 카메라 전원 행을 함께 표시한다.
            var tab = CommunicationSettingsTabControl.Items.OfType<TabItem>().FirstOrDefault(t => Convert.ToString(t.Header) == "CTRL / RTSP");

            if (!(tab?.Content is ScrollViewer scroll))
                return;

            scroll.Padding = new Thickness(0, 0, 6, 0);

            foreach (var item in LogicalDescendants<FrameworkElement>(scroll))
            {
                Thickness margin = item.Margin;

                if (margin.Top > 6 || margin.Bottom > 6)
                    item.Margin = new Thickness(margin.Left, Math.Min(6, margin.Top), margin.Right, Math.Min(6, margin.Bottom));

                if (item is Border separator && separator.Height == 1)
                    separator.Margin = new Thickness(margin.Left, 10, margin.Right, 8);
            }
        }

        #endregion

        #region [필드 및 상태 정보]

        private bool? _lastVideoControlRoute;

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 활성 창의 영상 영역에 따라 PTZF 채널을 선택하고 채널 전환 시 누름 입력을 정지한다.
        /// </summary>
        private void UpdateVideoControlRoute()
        {
            if (!IsActive)
                return;

            bool ir = GetHoveredCameraType() == VideoPopoutCameraType.Ir;

            // V32_4: 원격 제어 또는 서로 다른 DPI/해상도 환경에서는 마우스 포인터가
            // EO/IR 경계를 순간적으로 넘으며 MouseLeave/MouseEnter가 연속 발생할 수 있다.
            // 클릭 중심 이동 중 hover만으로 ReleaseAllControlInputs를 호출하면 절대 위치
            // 이동 정지(0x4F)가 송신되므로, 목표 도착 전까지 클릭한 채널을 유지한다.
            // 실제 버튼/키보드 입력은 PressControlInput에서 기존대로 클릭 이동을 중단한다.
            if (vm.IsVideoCenterMoveActive)
            {
                if (_lastVideoControlRoute.HasValue && _lastVideoControlRoute.Value != ir)
                {
                    ConsoleLogHelper.State(
                        "VIDEO CONTROL ROUTE",
                        "Hover channel change ignored during video-center move / " + "REQUESTED_CAMERA=" + (ir ? "IR" : "EO")
                    );
                }

                return;
            }

            if (_lastVideoControlRoute.HasValue && _lastVideoControlRoute.Value != ir)
                vm.ReleaseAllControlInputs("Video focus channel changed");

            _lastVideoControlRoute = ir;
            vm.SetActivePanTiltZoomSource(ir);
        }

        /// <summary>

        /// 기존 보더 높이 안에서 채널별 감속 입력과 오른쪽 적용 버튼을 배치한다.

        /// </summary>
        private static Expander CreateFovTuningEditor()
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 4)
            };

            foreach (var channel in new[]
            {
                "Eo",
                "Ir"
            }

            )
            {
                row.Children.Add(new TextBlock { Text = channel.ToUpper() + " 강도", FontSize = 10, Foreground = channel == "Eo" ? Brushes.Cyan : Brushes.Plum, VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(TuningEditor(channel + "SpeedExponent", 32));
                row.Children.Add(new TextBlock { Text = "최소", FontSize = 10, Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(TuningEditor(channel + "MinimumSpeed", 28));
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(new Viewbox { Child = row, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, HorizontalAlignment = HorizontalAlignment.Left });
            var apply = new Button
            {
                Content = "적용",
                Width = 42,
                Height = 24,
                FontSize = 10,
                Margin = new Thickness(4, 0, 0, 0),
                ToolTip = "EO/IR 입력값 적용. 실행 중 설정이며 재실행 기본값은 ControlTuning.xml입니다."
            };
            apply.Click += (s, e) =>
            {
                foreach (var box in row.Children.OfType<TextBox>())
                    box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                Keyboard.Focus(apply);
            };
            Grid.SetColumn(apply, 1);
            grid.Children.Add(apply);
            return new Expander
            {
                Header = "감속 설정 · 강도 1~4 / 최소 1~60",
                Content = grid,
                Foreground = Brushes.LightSkyBlue,
                Margin = new Thickness(8, 0, 0, 0),
                ToolTip = "강도가 클수록 중간 줌부터 더 감속합니다. 최대 줌은 최소 속도에 도달하며 1 미만 전송은 지원하지 않습니다."
            };
        }

        /// <summary>
        /// 범위 검사를 사용하는 감속 조정 입력란을 만든다.
        /// </summary>
        private static TextBox TuningEditor(string path, double width)
        {
            var box = new TextBox
            {
                Width = width,
                FontSize = 10,
                Margin = new Thickness(3, 0, 6, 0),
                Padding = new Thickness(2, 0, 2, 0),
                Background = new SolidColorBrush(Color.FromRgb(47, 61, 73)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = Brushes.LightSlateGray,
                ToolTip = path.EndsWith("Exponent") ? "강도 1~4 · 클수록 감속. 최대 줌은 최소 속도 적용." : "최소 속도 1~60 · 낮을수록 느림. 실행 중 조정."
            };
            box.SetBinding(
                TextBox.TextProperty,
                new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus, ValidatesOnExceptions = true }
            );
            return box;
        }

        /// <summary>

        /// 클릭 이동 표시 및 전역 입력 옵션 변경을 UI와 키보드 훅에 반영한다.

        /// </summary>
        private void V30PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsVideoClickCenterEnabled")
            {
                _videoCenterClickGeneration++;
                SetVideoJogVisible(vm.IsVideoClickCenterEnabled);
                return;
            }

            if (e.PropertyName != "IsGlobalControlEnabled")
                return;

            if (!vm.IsGlobalControlEnabled)
            {
                _globalKeyboard?.Dispose();
                _globalKeyboard = null;
                return;
            }

            try
            {
                _globalKeyboard = new GlobalControlKeyboard(Dispatcher);
                _globalKeyboard.KeyChanged += (key, down) =>
                {
                    if (!vm.IsGlobalControlEnabled)
                        return;

                    if (!down)
                    {
                        vm.HandleControlKey("global", key, false, _primaryVideoType == VideoPopoutCameraType.Ir);
                        return;
                    }

                    // 2026-10-12: 활성 REI 창은 해당 창의 영상 채널을 사용한다.
                    // 백그라운드 입력만 주 영상의 전역 경로를 사용하며 명령을 중복 전달하지 않는다.
                    if (Application.Current.Windows.OfType<Window>().Any(w => w.IsActive))
                        return;

                    if (key == Key.R || key == Key.T)
                    {
                        vm.ReleaseAllControlInputs("Channel switch");
                        SelectPrimaryVideo(key == Key.R ? VideoPopoutCameraType.Eo : VideoPopoutCameraType.Ir);
                        return;
                    }

                    vm.HandleControlKey("global", key, true, _primaryVideoType == VideoPopoutCameraType.Ir);
                };
                ConsoleLogHelper.State("GLOBAL INPUT", "Enabled / non-consuming hook / elevated windows not guaranteed");
            }
            catch (Exception ex)
            {
                vm.IsGlobalControlEnabled = false;
                ConsoleLogHelper.Warning("GLOBAL INPUT", "Hook failed / " + ex.GetType().Name);
            }
        }

        /// <summary>

        /// 텍스트 입력의 Enter/Escape에서 값을 반영하고 제어 창으로 키보드 포커스를 돌린다.

        /// </summary>
        private void InputConfirmKey(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox box && (e.Key == Key.Enter || e.Key == Key.Escape))
            {
                box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                Keyboard.Focus(this);
                e.Handled = true;
            }
        }

        /// <summary>

        /// 입력/선택 컨트롤 외부를 클릭했을 때만 텍스트 편집을 마치고 제어 포커스를 복원한다.

        /// </summary>
        private void ReturnControlFocus(object sender, MouseButtonEventArgs e)
        {
            // 2026-10-08: 드롭다운 항목은 별도 시각 트리에 존재한다.
            // 드롭다운 선택 중 포커스를 바꾸지 않고 텍스트 편집 영역을 벗어날 때만 복원한다.
            if (!(Keyboard.FocusedElement is TextBoxBase) && !(Keyboard.FocusedElement is PasswordBox))
                return;

            DependencyObject item = e.OriginalSource as DependencyObject;
            while (item != null)
            {
                if (item is TextBoxBase ||
                    item is PasswordBox ||
                    item is Selector ||
                    item is ComboBoxItem ||
                    item is MenuItem ||
                    item is RangeBase ||
                    item is ButtonBase ||
                    item is Expander)
                    return;

                if (item is Window)
                    break;

                item = item is Visual ? VisualTreeHelper.GetParent(item) : LogicalTreeHelper.GetParent(item);
            }

            if (Keyboard.FocusedElement is TextBoxBase box)
                (box as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            Keyboard.Focus(this);
        }

        /// <summary>

        /// 클릭 이동 방향 안내를 함께 표시/숨기고 해제 시 영상 소유 입력을 정지한다.

        /// </summary>
        private void SetVideoJogVisible(bool visible)
        {
            foreach (var overlay in _videoJogOverlays)
            {
                VideoShortcutGuide.BindJogVisibility(overlay, (bool)overlay.Tag);
            }

            if (!visible)
                vm.ReleaseControlOwner("video");
        }

        /// <summary>

        /// 채널별 배율·단축키 도움말·방향 입력 안내를 기존 영상 보더에 추가한다.

        /// </summary>
        private void AddVideoControls(Border border, bool ir)
        {
            if (!(border?.Child is Grid grid))
                return;

            var ratioHost = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ratioHost.Children.Add(VideoShortcutGuide.CreateRatioToggle(ir));
            var ratio = new TextBlock
            {
                Margin = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(5, 1, 5, 1),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = LegacyVideoIntegration.ChannelBrush(ir),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                IsHitTestVisible = false,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 1,
                    ShadowDepth = 1,
                    Opacity = 1
                }
            };
            ratio.SetBinding(TextBlock.TextProperty, new Binding(ir ? "IrZoomRatioOverlayText" : "EoZoomRatioOverlayText"));
            ratioHost.Children.Add(ratio);
            VideoShortcutGuide.BindRatioVisibility(ratio, ir);
            var ratioScale = new ScaleTransform();
            ratioHost.LayoutTransform = ratioScale;
            Action updateRatioScale = () =>
            {
                double scale = VideoShortcutGuide.LabelScale(border.ActualWidth, border.ActualHeight);
                ratioScale.ScaleX = scale;
                ratioScale.ScaleY = scale;
            };
            border.SizeChanged += (s, e) => updateRatioScale();
            ratioHost.Loaded += (s, e) => updateRatioScale();
            SizeChanged += (s, e) => updateRatioScale();
            updateRatioScale();
            var connection = grid.Children.OfType<TextBlock>().FirstOrDefault(t => BindingOperations.GetBinding(
                t,
                TextBlock.TextProperty
            )?.Path?.Path == (ir ? "IrStatusText" : "EoStatusText"));

            if (connection != null)
            {
                connection.Foreground = LegacyVideoIntegration.ChannelBrush(ir);
                connection.FontSize = 14;
                connection.Background = null;
                connection.Padding = new Thickness(4, 1, 4, 1);
                connection.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 1,
                    ShadowDepth = 1,
                    Opacity = 1
                };
                var statusScale = new ScaleTransform();
                connection.LayoutTransform = statusScale;
                Action updateStatus = () =>
                {
                    double scale = VideoShortcutGuide.LabelScale(border.ActualWidth, border.ActualHeight);
                    statusScale.ScaleX = scale;
                    statusScale.ScaleY = scale;
                };
                border.SizeChanged += (s, e) => updateStatus();
                connection.Loaded += (s, e) => updateStatus();
                updateStatus();
            }

            Panel.SetZIndex(ratioHost, 60);
            grid.Children.Add(ratioHost);
            var video = ir ? IrCameraImageView : EoCameraImageView;
            VideoShortcutGuide.AttachVideoLabelPositions(
                grid,
                video,
                ratioHost,
                connection,
                () => VideoShortcutGuide.SourceAspect(
                video,
                ir ? 1.25 : 16.0 / 9
            )
            );
            var marker = LogicalDescendants<Control>(grid).FirstOrDefault(c => c.Template == FindResource("CenterCrosshairTemplate"));

            if (marker != null)
                LegacyVideoIntegration.Attach(marker, ir ? IrCameraImageView : EoCameraImageView, vm, () => ir);

            var help = new Expander
            {
                Name = ir ? "IrVideoShortcutHelp" : "EoVideoShortcutHelp",
                IsExpanded = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = LegacyVideoIntegration.ChannelBrush(ir),
                Margin = new Thickness(14),
                Template = VideoShortcutGuide.MainGuideTemplate(),
                Content = VideoShortcutGuide.CreateContent(ir)
            };
            var helpScale = new ScaleTransform();
            help.LayoutTransform = helpScale;
            Action updateHelpScale = () =>
            {
                double scale = VideoShortcutGuide.Scale(border.ActualWidth, border.ActualHeight);
                helpScale.ScaleX = scale;
                helpScale.ScaleY = scale;
            };
            border.SizeChanged += (s, e) => updateHelpScale();
            help.Loaded += (s, e) => updateHelpScale();
            help.Expanded += (s, e) => updateHelpScale();
            SizeChanged += (s, e) => updateHelpScale();
            updateHelpScale();
            Panel.SetZIndex(help, 65);
            grid.Children.Add(help);
            var overlay = new Grid
            {
                Tag = ir,
                Background = null
            };
            Panel.SetZIndex(overlay, 70);
            VideoShortcutGuide.BindJogVisibility(overlay, ir);
            string[] symbols =
            {
                "◀",
                "▶",
                "▲",
                "▼"
            };
            int[] xs =
            {
                -1,
                1,
                0,
                0
            }, ys =
            {
                0,
                0,
                1,
                -1
            };

            for (int i = 0; i < 4; i++)
            {
                int x = xs[i], y = ys[i];
                var button = new TextBlock
                {
                    Name = (ir ? "Ir" : "Eo") + "VideoDirection" + i,
                    Text = symbols[i],
                    Width = 28,
                    Height = 28,
                    Opacity = .55,
                    IsHitTestVisible = false,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 20,
                    Foreground = Brushes.LightSkyBlue,
                    HorizontalAlignment = x < 0 ? HorizontalAlignment.Left : x > 0 ? HorizontalAlignment.Right : HorizontalAlignment.Center,
                    VerticalAlignment = y > 0 ? VerticalAlignment.Top : y < 0 ? VerticalAlignment.Bottom : VerticalAlignment.Center,
                    Margin = new Thickness(8)
                };
                overlay.Children.Add(button);
            }

            grid.Children.Add(overlay);
            _videoJogOverlays.Add(overlay);
        }

        /// <summary>
        /// 기존 EO와 IR 렌즈 카드에 디지털 줌 요청 및 상태 표시를 연결한다.
        /// </summary>
        private void AddDigitalZoomControls()
        {
            foreach (bool ir in new[]
            {
                false,
                true
            }

            )
            {
                var title = LogicalDescendants<TextBlock>(Content as DependencyObject).FirstOrDefault(t => t.Text == (ir ? "IR" : "EO") + " CAM ZOOM / FOCUS CONTROL");

                if (!(title?.Parent is Grid grid))
                    continue;

                foreach (var spacer in grid.Children.OfType<Border>().Where(b => Grid.GetRow(b) == 3).ToArray())
                    grid.Children.Remove(spacer);

                var panel = new StackPanel
                {
                    Margin = new Thickness(0, 6, 0, 6)
                };
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                foreach (bool on in new[]
                {
                    true,
                    false
                }

                )
                {
                    var button = new Button
                    {
                        Content = on ? "D-ZOOM ON" : "D-ZOOM OFF",
                        Width = 85,
                        Height = 32,
                        Margin = new Thickness(5),
                        Focusable = false,
                        Style = (Style)FindResource(ir ? "IrControlButtonStyle" : "EoControlButtonStyle"),
                        ToolTip = "디지털 줌 ON/OFF 요청. 전송 성공과 장비 상태 확인은 아래 문구로 구분합니다."
                    };

                    // 정상 운용 중에는 기존 EO/IR 제어 활성 상태를 유지한다. 에이전트의 지원과 상태는
                    // 진단 증거로 유지하며 이 정보만으로 기존 버튼을 비활성화하지 않는다.
                    // 2026-10-13: IR 디지털 줌은 설정 지원이 확인된 경우에만 사용한다.
                    // 다른 IR 렌즈 버튼과 EO 설정 전용 요청 흐름은 바꾸지 않는다.
                    if (ir)
                        button.SetBinding(IsEnabledProperty, new Binding("CanSetIrDigitalZoom"));

                    button.Click += (s, e) => vm.SetDigitalZoom(ir, on);
                    row.Children.Add(button);
                }

                panel.Children.Add(row);
                var status = new TextBlock
                {
                    FontSize = 9,
                    Foreground = Brushes.LightSteelBlue,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                status.SetBinding(TextBlock.TextProperty, new Binding(ir ? "IrDigitalZoomStatusText" : "EoDigitalZoomStatusText"));
                panel.Children.Add(status);
                // 요청 근거 문구를 간결하게 표시하며 전송 성공을 장비 적용 확인으로 취급하지 않는다.
                Grid.SetRow(panel, 3);
                grid.Children.Add(panel);
            }
        }

        /// <summary>
        /// 기존 버튼 크기를 보존하면서 렌즈 카드 높이를 배분한다.
        /// </summary>
        private void FitLensCards()
        {
            // 2026-10-12: 기존 버튼 크기를 보존하고 렌즈 영역 높이를 활용한다.
            // 이전 Viewbox는 두 카드를 축소하여 스크롤바를 없앴지만
            // 아래에 큰 공백이 남았다. 같은 비율의 행으로 제어 크기를 유지하면서 공간을 배분한다.
            var tab = LogicalDescendants<TabItem>(Content as DependencyObject).FirstOrDefault(t => Convert.ToString(t.Header) == "LENS");

            if (!(tab?.Content is ScrollViewer scroll) || !(scroll.Content is StackPanel cards))
                return;

            var lensCards = cards.Children.OfType<Border>().ToArray();

            if (lensCards.Length == 0)
                return;

            var host = new Grid
            {
                Name = "LensCardsGrid",
                Margin = new Thickness(4, 3, 4, 4)
            };
            var enabledBinding = BindingOperations.GetBinding(cards, IsEnabledProperty);

            if (enabledBinding != null)
                host.SetBinding(IsEnabledProperty, enabledBinding);
            else
                host.IsEnabled = cards.IsEnabled;

            foreach (var unused in lensCards)
                host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            scroll.Content = null;

            for (int index = 0; index < lensCards.Length; index++)
            {
                var card = lensCards[index];
                cards.Children.Remove(card);
                card.Margin = new Thickness(0, index == 0 ? 2 : 4, 0, index == lensCards.Length - 1 ? 2 : 4);
                card.Padding = new Thickness(6);
                card.VerticalAlignment = VerticalAlignment.Stretch;
                Grid.SetRow(card, index);
                host.Children.Add(card);

                if (card.Child is Grid controls)
                {
                    controls.VerticalAlignment = VerticalAlignment.Stretch;

                    for (int row = 0; row < controls.RowDefinitions.Count; row++)
                        controls.RowDefinitions[row].Height = row == 0 ? GridLength.Auto : new GridLength(
                            row == 3 ? 1.35 : 1,
                            GridUnitType.Star
                        );

                    foreach (var element in controls.Children.OfType<FrameworkElement>().Where(x => Grid.GetRow(x) > 0))
                    {
                        element.VerticalAlignment = VerticalAlignment.Center;

                        if (element is Panel)
                            element.Margin = new Thickness(0);
                    }

                    foreach (var button in LogicalDescendants<Button>(controls))
                    {
                        button.Width = Convert.ToString(button.Content) == "AUTO FOCUS" ? 180 : 85;
                        button.Height = 32;
                    }
                }
            }

            tab.Content = host;
        }

        /// <summary>
        /// 통신 설정 화면의 마지막 항목이 잘리지 않도록 내부 여백을 확보한다.
        /// </summary>
        private void PadCommunicationScrollContents()
        {
            // 2026-10-12: 위아래 내부 여백을 확보하고 버튼 전체 높이가 스크롤 영역에 포함되게 한다.
            foreach (var scroll in LogicalDescendants<ScrollViewer>(CommunicationSettingsTabControl))
                if (scroll.Content is FrameworkElement content)
                {
                    var m = content.Margin;
                    content.Margin = new Thickness(
                        Math.Max(
                        3,
                        m.Left
                    ),
                        Math.Max(
                        8,
                        m.Top
                    ),
                        Math.Max(
                        5,
                        m.Right
                    ),
                        Math.Max(
                        14,
                        m.Bottom
                    )
                    );
                    scroll.Padding = new Thickness(0, 4, 8, 6);
                }

            var aiTab = CommunicationSettingsTabControl.Items.OfType<TabItem>().FirstOrDefault(t => Convert.ToString(t.Header) == "AI / VP SETTING");

            if (aiTab?.Content is ScrollViewer aiScroll)
            {
                foreach (var element in LogicalDescendants<FrameworkElement>(aiScroll))
                {
                    var m = element.Margin;
                    element.Margin = new Thickness(
                        m.Left,
                        Math.Max(
                        0,
                        Math.Min(
                        5,
                        m.Top
                    )
                    ),
                        m.Right,
                        Math.Max(
                        0,
                        Math.Min(
                        5,
                        m.Bottom
                    )
                    )
                    );

                    // 제어 요소의 높이는 유지하고 버튼 내용을 자르지 않으며 간격에서 공간을 확보한다.
                    if (element is Button button)
                        button.MinHeight = Math.Max(30, button.MinHeight);
                }

                if (aiScroll.Content is StackPanel root)
                {
                    root.Margin = new Thickness(5, 8, 5, 16);
                    AlignAiScrollBottom(aiTab, aiScroll, root);
                }
            }
        }

        /// <summary>
        /// 실제 구역 상태 행 수에 맞춰 하단 여백을 계산한다.
        /// 최하단에서 이전 진단 문구가 상단 경계에 반쯤 남지 않게 다음 제목을 맞춘다.
        /// </summary>
        private void AlignAiScrollBottom(TabItem tab, ScrollViewer scroll, StackPanel root)
        {
            var tail = new Border
            {
                Height = 0,
                IsHitTestVisible = false
            };
            root.Children.Add(tail);
            bool adjusting = false;
            root.LayoutUpdated += (s, e) =>
            {
                if (adjusting || !tab.IsSelected || scroll.ViewportHeight <= 0)
                    return;

                double baseOffset = scroll.ScrollableHeight - tail.Height;

                if (baseOffset <= 0)
                    return;

                var smoke = LogicalDescendants<CheckBox>(root).FirstOrDefault(c => Convert.ToString(c.Content) == "SMOKE DETECTOR");

                if (smoke == null || smoke.ActualHeight <= 0)
                    return;

                double top = smoke.TranslatePoint(new Point(), scroll).Y + scroll.VerticalOffset - scroll.Padding.Top;
                // 가까운 카드 제목만 정렬하며 큰 빈 페이지를 추가하지 않는다.
                double desired = top >= baseOffset && top <= baseOffset + 120 ? Math.Max(0, top - baseOffset - 6) : 0;

                if (Math.Abs(tail.Height - desired) < 1)
                    return;

                bool atBottom = scroll.VerticalOffset >= scroll.ScrollableHeight - 1;
                adjusting = true;
                tail.Height = desired;
                adjusting = false;

                if (atBottom)
                    Dispatcher.BeginInvoke(new Action(scroll.ScrollToEnd));
            };
        }

        /// <summary>
        /// 운용 제어 화면의 불필요한 간격을 줄이고 마지막 항목을 보존한다.
        /// </summary>
        private void CompactOperationScrollContents()
        {
            // 마지막 상태와 도움말 행을 스크롤 영역 안에 유지하고
            // 기본 상단 위치에서도 PTZF/프리셋 카드가 잘리지 않도록 세로 공간을 확보한다.
            foreach (string header in new[]
            {
                "PTZF",
                "PRESET"
            }

            )
            {
                var tab = LogicalDescendants<TabItem>(Content as DependencyObject).FirstOrDefault(t => Convert.ToString(t.Header) == header);

                if (!(tab?.Content is ScrollViewer scroll) || !(scroll.Content is StackPanel root))
                    continue;

                scroll.Padding = new Thickness(6, 4, 8, 4);
                root.Margin = new Thickness(2, 4, 2, 4);

                foreach (var card in root.Children.OfType<Border>())
                {
                    var margin = card.Margin;
                    card.Margin = new Thickness(margin.Left, Math.Min(4, margin.Top), margin.Right, Math.Min(4, margin.Bottom));
                    var padding = card.Padding;
                    card.Padding = new Thickness(padding.Left, Math.Min(10, padding.Top), padding.Right, Math.Min(10, padding.Bottom));
                }

                foreach (var text in LogicalDescendants<TextBlock>(root))
                {
                    var margin = text.Margin;
                    text.Margin = new Thickness(margin.Left, Math.Min(6, margin.Top), margin.Right, Math.Min(6, margin.Bottom));
                }

                foreach (var nested in LogicalDescendants<Border>(root).Where(b => !ReferenceEquals(b.Parent, root)))
                {
                    var margin = nested.Margin;
                    nested.Margin = new Thickness(margin.Left, Math.Min(6, margin.Top), margin.Right, Math.Min(6, margin.Bottom));
                    var padding = nested.Padding;
                    nested.Padding = new Thickness(padding.Left, Math.Min(6, padding.Top), padding.Right, Math.Min(6, padding.Bottom));
                }

                var presetScanCard = header == "PRESET" ? root.Children.OfType<Border>().FirstOrDefault(card => LogicalDescendants<TextBlock>(card).Any(text => text.Text == "AUTO SCAN / LA SCAN OBJECT")) : null;
                var presetScanTitle = presetScanCard == null ? null : LogicalDescendants<TextBlock>(presetScanCard).First(text => text.Text == "AUTO SCAN / LA SCAN OBJECT");
                Thickness presetScanMargin = presetScanCard?.Margin ?? new Thickness();
                // 맨 아래 위치에서는 인접한 제목 또는 프리셋 상태가
                // 첫 행 중간에서 잘리지 않고 완전하게 표시되도록 맞춘다. 이 여백은 외부 패딩이 아닌
                // 실제 스크롤 내용이므로 이동 범위 계산에 포함된다.
                var tail = new Border
                {
                    Height = 0,
                    IsHitTestVisible = false
                };
                root.Children.Add(tail);
                bool adjusting = false;
                root.LayoutUpdated += (s, e) =>
                {
                    if (adjusting || !tab.IsSelected || scroll.ViewportHeight <= 0)
                        return;

                    // 첫 화면의 아래 경계에 오토 스캔 제목이 걸리면 카드 전체를
                    // 경계 아래로 옮긴다. 원래 위치를 기준으로 계산하여 여백 누적을 막는다.
                    // 카드 내용과 스크롤 이동 범위는 그대로 유지한다.
                    if (presetScanCard != null && presetScanTitle.ActualHeight > 0)
                    {
                        double addedMargin = presetScanCard.Margin.Top - presetScanMargin.Top;
                        double cardTop = presetScanCard.TranslatePoint(
                            new Point(),
                            scroll
                        ).Y + scroll.VerticalOffset - scroll.Padding.Top - addedMargin;
                        double titleBottom = presetScanTitle.TranslatePoint(
                            new Point(
                            0,
                            presetScanTitle.ActualHeight
                        ),
                            scroll
                        ).Y + scroll.VerticalOffset - scroll.Padding.Top - addedMargin;
                        double desiredMargin = cardTop < scroll.ViewportHeight &&
                            titleBottom + 4 > scroll.ViewportHeight ? scroll.ViewportHeight - cardTop + 8 : 0;

                        if (Math.Abs(addedMargin - desiredMargin) >= 1)
                        {
                            adjusting = true;
                            presetScanCard.Margin = new Thickness(
                                presetScanMargin.Left,
                                presetScanMargin.Top + desiredMargin,
                                presetScanMargin.Right,
                                presetScanMargin.Bottom
                            );
                            adjusting = false;
                            return;
                        }
                    }

                    double baseOffset = scroll.ScrollableHeight - tail.Height;

                    if (baseOffset <= 0)
                        return;

                    double anchor = double.PositiveInfinity;

                    foreach (var text in LogicalDescendants<TextBlock>(root))
                    {
                        string path = BindingOperations.GetBinding(text, TextBlock.TextProperty)?.Path?.Path;

                        if (text.ActualHeight <= 0 ||
                            text.Visibility != Visibility.Visible ||
                            !(text.FontWeight == FontWeights.Bold ||
                            path == "SelectedLaPresetDetailText" ||
                            path == "SelectedPresetDetailText"))
                            continue;

                        bool hidden = false;

                        for (DependencyObject parent = text.Parent; parent != null &&
                            parent != root; parent = LogicalTreeHelper.GetParent(parent))
                            if (parent is UIElement ui && ui.Visibility != Visibility.Visible)
                            {
                                hidden = true;
                                break;
                            }

                        if (hidden)
                            continue;

                        double top = text.TranslatePoint(new Point(), scroll).Y + scroll.VerticalOffset - scroll.Padding.Top;

                        if (top >= baseOffset && top <= baseOffset + 100)
                            anchor = Math.Min(anchor, top);
                    }

                    double desired = double.IsPositiveInfinity(anchor) ? 0 : Math.Max(0, anchor - baseOffset);

                    if (Math.Abs(tail.Height - desired) < 1)
                        return;

                    adjusting = true;
                    tail.Height = desired;
                    adjusting = false;
                };
            }
        }

        /// <summary>
        /// 현재 요소에서 지정 형식의 상위 요소를 찾는다.
        /// </summary>
        private static T Ancestor<T>(DependencyObject item)
            where T : DependencyObject
        {
            while (item != null)
            {
                if (item is T found)
                    return found;

                item = item is Visual ? VisualTreeHelper.GetParent(item) : LogicalTreeHelper.GetParent(item);
            }

            return null;
        }

        /// <summary>
        /// 시각 트리에서 지정 형식의 하위 요소를 순회한다.
        /// </summary>
        internal static IEnumerable<T> Descendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is T item)
                    yield return item;

                foreach (var descendant in Descendants<T>(child))
                    yield return descendant;
            }
        }

        /// <summary>
        /// 논리 트리에서 지정 형식의 하위 요소를 순회한다.
        /// </summary>
        private static IEnumerable<T> LogicalDescendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
                yield break;

            foreach (object item in LogicalTreeHelper.GetChildren(root))
                if (item is DependencyObject child)
                {
                    if (child is T match)
                        yield return match;

                    foreach (var descendant in LogicalDescendants<T>(child))
                        yield return descendant;
                }
        }

        /// <summary>
        /// 영상 경고 보더를 실제 영상 영역에 맞춰 배치한다.
        /// </summary>
        private void ArrangeAlarmBanners(Border border)
        {
            if (!(border?.Child is Grid grid))
                return;

            var banners = grid.Children.OfType<Border>().Where(b => Descendants<TextBlock>(b).Any(t => t.Text.Contains("DETECTED /"))).ToArray();

            if (banners.Length == 0)
                return;

            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 12, 0, 0),
                IsHitTestVisible = false
            };

            foreach (var banner in banners)
            {
                grid.Children.Remove(banner);
                banner.Margin = new Thickness(0, 0, 0, 5);
                panel.Children.Add(banner);
            }

            Panel.SetZIndex(panel, 40);
            grid.Children.Add(panel);
            var video = ReferenceEquals(border, IrVideoBorder) ? IrCameraImageView : EoCameraImageView;
            VideoShortcutGuide.AttachVideoAlarmPositions(
                grid,
                panel,
                video,
                () => VideoShortcutGuide.SourceAspect(
                video,
                ReferenceEquals(
                border,
                IrVideoBorder
            ) ? 1.25 : 16.0 / 9
            )
            );
        }

        /// <summary>

        /// 연결 상태 요약의 첫 데이터 열을 맞추고 기존 상태/색상 바인딩을 보존한다.

        /// </summary>
        private void AlignConnectionStatusRows()
        {
            var summary = LogicalDescendants<TextBlock>(Content as DependencyObject).FirstOrDefault(t => t.Text == "DEVICE CONNECTION STATUS");

            if (summary != null)
            {
                summary.HorizontalAlignment = HorizontalAlignment.Center;

                if (summary.Parent is StackPanel summaryPanel && summaryPanel.Parent is Border summaryCard)
                {
                    summaryCard.Background = (Brush)new BrushConverter().ConvertFromString("#495057");
                    summaryCard.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#95A5B0");
                }
            }

            foreach (var label in Descendants<TextBlock>(Content as DependencyObject).Where(t => t.Text == "CONTROL" ||
                t.Text == "CAMERA" ||
                t.Text == "DETECT").ToArray())
            {
                var grid = label.Parent as Grid;

                if (grid == null || grid.ColumnDefinitions.Count != 4)
                    continue;

                grid.ColumnDefinitions.Clear();

                for (int i = 0; i < 4; i++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 0 ? new GridLength(60) : GridLength.Auto });

                label.Margin = new Thickness(0, 0, 12, 0);

                foreach (var text in grid.Children.OfType<TextBlock>().Where(t => t != label).ToArray())
                {
                    var runs = text.Inlines.OfType<System.Windows.Documents.Run>().ToArray();

                    if (runs.Length != 2)
                        continue;

                    int column = Grid.GetColumn(text);
                    var cell = new Grid
                    {
                        Margin = column == 1 ? new Thickness(0) : new Thickness(10, 0, 0, 0)
                    };
                    Grid.SetColumn(cell, column);
                    cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    foreach (var pair in runs.Select((r, i) => new { Run = r, Index = i }))
                    {
                        var item = new TextBlock
                        {
                            Text = pair.Run.Text.Trim(),
                            Foreground = pair.Run.Foreground,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = pair.Index == 1 ? new Thickness(4, 0, 0, 0) : new Thickness(0)
                        };
                        var binding = BindingOperations.GetBinding(pair.Run, System.Windows.Documents.Run.TextProperty);

                        if (binding != null)
                            item.SetBinding(TextBlock.TextProperty, binding);

                        var foregroundBinding = BindingOperations.GetBinding(
                            pair.Run,
                            System.Windows.Documents.TextElement.ForegroundProperty
                        );

                        if (foregroundBinding != null)
                            item.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);

                        if (pair.Index == 1 && binding != null && new[]
                        {
                            "CurrentEoPowerText",
                            "CurrentIrPowerText",
                            "AiPowerStatusText",
                            "FirePowerStatusText",
                            "SmokePowerStatusText"
                        }.Contains(binding.Path?.Path))
                            item.SetBinding(
                                TextBlock.ForegroundProperty,
                                new Binding(binding.Path.Path) { Mode = BindingMode.OneWay, Converter = new DeviceOnOffBrushConverter() }
                            );

                        Grid.SetColumn(item, pair.Index);
                        cell.Children.Add(item);
                    }

                    cell.Style = null;
                    var visibility = BindingOperations.GetBinding(text, VisibilityProperty);

                    if (visibility != null)
                        cell.SetBinding(VisibilityProperty, visibility);

                    // 기동형 전용 항목의 기존 지원 여부 표시 조건은 유지한다.
                    cell.SetBinding(
                        VisibilityProperty,
                        new Binding("IsGpsTabVisible") { Converter = new BooleanToVisibilityConverter() }
                    );

                    if (text.Style == null)
                        cell.ClearValue(VisibilityProperty);

                    grid.Children.Remove(text);
                    grid.Children.Add(cell);
                }
            }
        }

        /// <summary>

        /// 전역 훅과 속성 구독을 정리하고 상태 타이머 및 제어 입력을 종료한다.

        /// </summary>
        private void ShutdownV30Ui()
        {
            _globalKeyboard?.Dispose();
            _globalKeyboard = null;
            vm.PropertyChanged -= V30PropertyChanged;
            vm.ShutdownV30Control();
        }

        /// <summary>
        /// 마우스 입력 요소별 제어 소유 토큰을 반환한다.
        /// </summary>
        private string MouseControlToken(object sender) => "mouse/" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(sender);

        #endregion

        #region [필드 및 상태 정보]

        private readonly HashSet<UIElement> _captureOwners = new HashSet<UIElement>();

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 마우스 캡처와 해제 보호를 연결한 뒤 소유자별 PTZF 누름 입력을 등록한다.
        /// </summary>
        private void BeginMouseControl(object sender, string axis, int x, int y, bool? ir = null)
        {
            var element = sender as UIElement;

            if (element != null)
            {
                if (_captureOwners.Add(element))
                    element.LostMouseCapture += (s, e) => vm.ReleaseControlInput(MouseControlToken(s));

                element.CaptureMouse();
            }

            vm.PressControlInput(MouseControlToken(sender), axis, x, y, ir ?? (_primaryVideoType == VideoPopoutCameraType.Ir));
        }

        /// <summary>

        /// 해당 마우스 입력 토큰을 해제하고 마우스 캡처를 반환한다.

        /// </summary>
        private void EndMouseControl(object sender)
        {
            vm.ReleaseControlInput(MouseControlToken(sender));
            (sender as UIElement)?.ReleaseMouseCapture();
        }

        #endregion
    }
}
