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

namespace OpenCvWpfTracking
{
    public partial class MainWindow
    {
        private GlobalControlKeyboard _globalKeyboard;
        private bool _showVideoJog, _v30UiInitialized;
        private readonly List<Grid> _videoJogOverlays = new List<Grid>();
        // 2026-10-08 V30_0: UI additions use existing border colors and typography.
        private void InitializeV30ControlUi()
        {
            // 2026-10-12: Loaded can run again when a visual is reattached. Never duplicate controls/handlers.
            if (_v30UiInitialized) return;
            _v30UiInitialized = true;
            vm.InitializeV30Status();
            vm.PropertyChanged += V30PropertyChanged;
            AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(InputConfirmKey), true);
            AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(ReturnControlFocus), true);
            var speed = Descendants<Slider>(Content as DependencyObject).FirstOrDefault(s => BindingOperations.GetBinding(s, Slider.ValueProperty)?.Path?.Path == "PanTiltSpeedLevel");
            if (speed != null)
            {
                speed.Minimum = 1; speed.Ticks = new DoubleCollection(Enumerable.Range(1, 12).Select(x => x * 5.0));
                speed.TickFrequency = 5; speed.LargeChange = 5;
                var outer = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(speed)) as Grid;
                if (outer != null)
                {
                    outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var panel = new StackPanel { Margin = new Thickness(7, 3, 7, 2) };
                    var options = new WrapPanel();
                    var clickMove = new CheckBox { Content = "클릭 이동", Foreground = Brushes.LightSkyBlue, Margin = new Thickness(0, 0, 10, 0), ToolTip = "영상 단일 클릭 중심 이동과 방향 안내 표시를 함께 켜거나 끕니다." };
                    clickMove.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsVideoClickCenterEnabled") { Mode = BindingMode.TwoWay }); options.Children.Add(clickMove);
                    var fov = new CheckBox { Content = "화각 비례 감속 (배율 근사)", Foreground = Brushes.Turquoise, ToolTip = "합의 배율 기반 근사 감속. 정확한 실측 화각은 아닙니다." };
                    fov.Content = "화각 비례 감속";
                    fov.Margin = new Thickness(0, 0, 10, 0);
                    fov.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsFovSpeedEnabled") { Mode = BindingMode.TwoWay }); options.Children.Add(fov);
                    var row = new WrapPanel { Margin = new Thickness(0, 4, 0, 4) };
                    foreach (var channel in new[] { "Eo", "Ir" })
                    {
                        row.Children.Add(new TextBlock { Text = channel.ToUpper() + " 강도", Foreground = channel == "Eo" ? Brushes.Cyan : Brushes.Plum, VerticalAlignment = VerticalAlignment.Center });
                        row.Children.Add(TuningEditor(channel + "SpeedExponent", 42));
                        row.Children.Add(new TextBlock { Text = "최소", Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center });
                        row.Children.Add(TuningEditor(channel + "MinimumSpeed", 32));
                    }
                    // 2026-10-12: user requested only attenuation controls here. No global/direction checkbox.
                    // 2026-10-12: user explicitly withdrew click-to-move. No checkbox or click movement handler.
                    // 2026-10-08 V30_0 hotfix: keep everyday controls horizontal; tuning expands only on demand.
                    var advanced = new Expander { Header = "감속 설정", IsExpanded = false, Content = row, Foreground = Brushes.LightSkyBlue, Margin = new Thickness(8, 0, 0, 0) };
                    options.Children.Add(advanced);
                    panel.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(68, 79, 88)), BorderBrush = new SolidColorBrush(Color.FromRgb(126, 148, 161)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 3, 5, 3), Child = options });
                    Grid.SetRow(panel, outer.RowDefinitions.Count - 1); outer.Children.Add(panel);
                }
            }
            AddVideoControls(EoVideoBorder, false); AddVideoControls(IrVideoBorder, true);
            SetVideoJogVisible(vm.IsVideoClickCenterEnabled);
            AddDigitalZoomControls();
            FitLensCards();
            CompactOperationScrollContents();
            PadCommunicationScrollContents();
            ArrangeAlarmBanners(EoVideoBorder); ArrangeAlarmBanners(IrVideoBorder);
            AlignConnectionStatusRows();
            CompactCommunicationCard();
        }
        private void CompactCommunicationCard()
        {
            // 2026-10-08 V30_0 hotfix: show TCP, RTSP and both power rows together.
            var tab = CommunicationSettingsTabControl.Items.OfType<TabItem>().FirstOrDefault(t => Convert.ToString(t.Header) == "CTRL / RTSP");
            if (!(tab?.Content is ScrollViewer scroll)) return;
            scroll.Padding = new Thickness(0, 0, 6, 0);
            foreach (var item in LogicalDescendants<FrameworkElement>(scroll))
            {
                Thickness margin = item.Margin;
                if (margin.Top > 6 || margin.Bottom > 6)
                    item.Margin = new Thickness(margin.Left, Math.Min(6, margin.Top), margin.Right, Math.Min(6, margin.Bottom));
                if(item is Border separator && separator.Height==1)
                    separator.Margin=new Thickness(margin.Left,10,margin.Right,8);
            }
        }
        private TextBox TuningEditor(string path, double width)
        {
            var box = new TextBox { Width = width, Margin = new Thickness(4, 0, 8, 0), Padding = new Thickness(2, 0, 2, 0), Background = new SolidColorBrush(Color.FromRgb(47, 61, 73)), Foreground = Brushes.White, CaretBrush = Brushes.White, BorderBrush = Brushes.LightSlateGray, ToolTip = "Config/ControlTuning.xml에서 장비별 영구 설정 가능. 이 입력은 실행 중 조정." };
            box.SetBinding(TextBox.TextProperty, new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus, ValidatesOnExceptions = true });
            return box;
        }
        private void V30PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsVideoClickCenterEnabled")
            {
                _videoCenterClickGeneration++;
                SetVideoJogVisible(vm.IsVideoClickCenterEnabled);
                return;
            }
            if (e.PropertyName != "IsGlobalControlEnabled") return;
            if (!vm.IsGlobalControlEnabled) { _globalKeyboard?.Dispose(); _globalKeyboard = null; return; }
            try
            {
                _globalKeyboard = new GlobalControlKeyboard(Dispatcher);
                _globalKeyboard.KeyChanged += (key, down) =>
                {
                    if (!vm.IsGlobalControlEnabled) return;
                    if (!down) { vm.HandleControlKey("global", key, false, _primaryVideoType == VideoPopoutCameraType.Ir); return; }
                    // 2026-10-12: active REI windows use their own local channel routing.
                    // Only background input uses the main-channel global route; never dispatch twice.
                    if (Application.Current.Windows.OfType<Window>().Any(w => w.IsActive)) return;
                    if (key == Key.R || key == Key.T) { vm.ReleaseAllControlInputs("Channel switch"); SelectPrimaryVideo(key == Key.R ? VideoPopoutCameraType.Eo : VideoPopoutCameraType.Ir); return; }
                    vm.HandleControlKey("global", key, true, _primaryVideoType == VideoPopoutCameraType.Ir);
                };
                ConsoleLogHelper.State("GLOBAL INPUT", "Enabled / non-consuming hook / elevated windows not guaranteed");
            }
            catch (Exception ex) { vm.IsGlobalControlEnabled = false; ConsoleLogHelper.Warning("GLOBAL INPUT", "Hook failed / " + ex.GetType().Name); }
        }
        private void InputConfirmKey(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox box && (e.Key == Key.Enter || e.Key == Key.Escape))
            {
                box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource(); Keyboard.Focus(this); e.Handled = true;
            }
        }
        private void ReturnControlFocus(object sender, MouseButtonEventArgs e)
        {
            // 2026-10-08 V30_0 hotfix: popup items live in a separate visual tree.
            // Never move focus during dropdown selection; restore only when leaving a text editor.
            if (!(Keyboard.FocusedElement is TextBoxBase) && !(Keyboard.FocusedElement is PasswordBox)) return;
            DependencyObject item = e.OriginalSource as DependencyObject;
            while (item != null)
            {
                if (item is TextBoxBase || item is PasswordBox || item is Selector || item is ComboBoxItem || item is MenuItem || item is RangeBase || item is ButtonBase || item is Expander) return;
                if (item is Window) break;
                item = item is Visual ? VisualTreeHelper.GetParent(item) : LogicalTreeHelper.GetParent(item);
            }
            if (Keyboard.FocusedElement is TextBoxBase box) (box as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            Keyboard.Focus(this);
        }
        private void SetVideoJogVisible(bool visible)
        {
            _showVideoJog = visible;
            foreach (var overlay in _videoJogOverlays)
            {
                var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                overlay.Visibility = visibility;
                foreach (var hint in overlay.Children.OfType<TextBlock>()) hint.Visibility = visibility;
            }
            if (!visible) vm.ReleaseControlOwner("video");
        }
        private void AddVideoControls(Border border, bool ir)
        {
            if (!(border?.Child is Grid grid)) return;
            var ratio = new TextBlock { Margin = new Thickness(10, 0, 0, 8), Padding = new Thickness(5, 2, 5, 2),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom,
                Foreground = ir ? Brushes.YellowGreen : Brushes.Cyan, Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)), FontSize = 13, IsHitTestVisible = false };
            ratio.SetBinding(TextBlock.TextProperty, new Binding(ir ? "IrZoomRatioOverlayText" : "EoZoomRatioOverlayText")); Panel.SetZIndex(ratio, 60); grid.Children.Add(ratio);
            VideoShortcutGuide.BindRatioVisibility(ratio,ir);
            var help = new Expander { Name = ir ? "IrVideoShortcutHelp" : "EoVideoShortcutHelp", IsExpanded = false, HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top, Foreground = (Brush)new BrushConverter().ConvertFromString(ir?"#C6E77B":"#5EE7F7"), Margin = new Thickness(14), Template=VideoShortcutGuide.MainGuideTemplate(),
                Content = VideoShortcutGuide.CreateContent(ir) };
            var helpScale=new ScaleTransform();help.LayoutTransform=helpScale;
            Action updateHelpScale=()=>{double scale=VideoShortcutGuide.Scale(border.ActualWidth,border.ActualHeight);helpScale.ScaleX=scale;helpScale.ScaleY=scale;};
            border.SizeChanged+=(s,e)=>updateHelpScale();
            help.Loaded+=(s,e)=>updateHelpScale();help.Expanded+=(s,e)=>updateHelpScale();
            SizeChanged+=(s,e)=>updateHelpScale();updateHelpScale();
            Panel.SetZIndex(help, 65); grid.Children.Add(help);
            var overlay = new Grid { Visibility = Visibility.Collapsed, Background = null }; Panel.SetZIndex(overlay, 70);
            string[] symbols = { "◀", "▶", "▲", "▼" }; int[] xs = { -1, 1, 0, 0 }, ys = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int x = xs[i], y = ys[i];
                var button = new TextBlock { Name = (ir ? "Ir" : "Eo") + "VideoDirection" + i, Text = symbols[i], Width = 28, Height = 28, Opacity = .55, IsHitTestVisible = false,
                    TextAlignment=TextAlignment.Center,FontSize=20,Foreground = Brushes.LightSkyBlue,
                    HorizontalAlignment = x < 0 ? HorizontalAlignment.Left : x > 0 ? HorizontalAlignment.Right : HorizontalAlignment.Center,
                    VerticalAlignment = y > 0 ? VerticalAlignment.Top : y < 0 ? VerticalAlignment.Bottom : VerticalAlignment.Center,
                    Margin = new Thickness(8) };
                overlay.Children.Add(button);
            }
            grid.Children.Add(overlay); _videoJogOverlays.Add(overlay);
        }
        private void AddDigitalZoomControls()
        {
            foreach(bool ir in new[]{false,true})
            {
                var title=LogicalDescendants<TextBlock>(Content as DependencyObject).FirstOrDefault(t=>t.Text==(ir?"IR":"EO")+" CAM ZOOM / FOCUS CONTROL");
                if(!(title?.Parent is Grid grid))continue;
                foreach(var spacer in grid.Children.OfType<Border>().Where(b=>Grid.GetRow(b)==3).ToArray())grid.Children.Remove(spacer);
                var panel=new StackPanel{Margin=new Thickness(0,6,0,6)};
                var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
                foreach(bool on in new[]{true,false})
                {
                    var button=new Button{Content=on?"D-ZOOM ON":"D-ZOOM OFF",Width=85,Height=32,Margin=new Thickness(5),Focusable=false,Style=(Style)FindResource(ir?"IrControlButtonStyle":"EoControlButtonStyle"),ToolTip="디지털 줌 ON/OFF 요청. 전송 성공과 장비 상태 확인은 아래 문구로 구분합니다."};
                    // FIX7: keep EO/IR controls enabled during normal operation. Agent capability/status
                    // remains diagnostic evidence and no longer greys out these buttons.
                    button.Click+=(s,e)=>vm.SetDigitalZoom(ir,on);
                    row.Children.Add(button);
                }
                panel.Children.Add(row);
                var status=new TextBlock{FontSize=9,Foreground=Brushes.LightSteelBlue,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,2,0,0)};
                status.SetBinding(TextBlock.TextProperty,new Binding(ir?"IrDigitalZoomStatusText":"EoDigitalZoomStatusText"));
                panel.Children.Add(status);
                // FIX9: compact evidence caption; a transmitted request is never device confirmation.
                Grid.SetRow(panel,3);grid.Children.Add(panel);
            }
        }
        private void FitLensCards()
        {
            // 2026-10-12: preserve the FIX4 button sizes and use the full LENS height.
            // The former Viewbox removed the scrollbar by shrinking both cards into a compact block,
            // leaving a large empty area below. Equal star rows remove scrolling without scaling controls.
            var tab=LogicalDescendants<TabItem>(Content as DependencyObject).FirstOrDefault(t=>Convert.ToString(t.Header)=="LENS");
            if(!(tab?.Content is ScrollViewer scroll) || !(scroll.Content is StackPanel cards))return;
            var lensCards=cards.Children.OfType<Border>().ToArray();
            if(lensCards.Length==0)return;
            var host=new Grid{Name="LensCardsGrid",Margin=new Thickness(4,3,4,4)};
            var enabledBinding=BindingOperations.GetBinding(cards,IsEnabledProperty);
            if(enabledBinding!=null)host.SetBinding(IsEnabledProperty,enabledBinding);else host.IsEnabled=cards.IsEnabled;
            foreach(var unused in lensCards)host.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            scroll.Content=null;
            for(int index=0;index<lensCards.Length;index++)
            {
                var card=lensCards[index];cards.Children.Remove(card);
                card.Margin=new Thickness(0,index==0?2:4,0,index==lensCards.Length-1?2:4);
                card.Padding=new Thickness(6);card.VerticalAlignment=VerticalAlignment.Stretch;
                Grid.SetRow(card,index);host.Children.Add(card);
                if(card.Child is Grid controls)
                {
                    controls.VerticalAlignment=VerticalAlignment.Stretch;
                    for(int row=0;row<controls.RowDefinitions.Count;row++)
                        controls.RowDefinitions[row].Height=row==0?GridLength.Auto:new GridLength(row==3?1.35:1,GridUnitType.Star);
                    foreach(var element in controls.Children.OfType<FrameworkElement>().Where(x=>Grid.GetRow(x)>0))
                    {
                        element.VerticalAlignment=VerticalAlignment.Center;
                        if(element is Panel)element.Margin=new Thickness(0);
                    }
                    foreach(var button in LogicalDescendants<Button>(controls))
                    {
                        button.Width=Convert.ToString(button.Content)=="AUTO FOCUS"?180:85;
                        button.Height=32;
                    }
                }
            }
            tab.Content=host;
        }
        private void PadCommunicationScrollContents()
        {
            // 2026-10-12: positive inner top/bottom clearance; keep full button height inside scroll extent.
            foreach(var scroll in LogicalDescendants<ScrollViewer>(CommunicationSettingsTabControl))
                if(scroll.Content is FrameworkElement content)
                {var m=content.Margin;content.Margin=new Thickness(Math.Max(3,m.Left),Math.Max(8,m.Top),Math.Max(5,m.Right),Math.Max(14,m.Bottom));scroll.Padding=new Thickness(0,4,8,6);}
            var aiTab=CommunicationSettingsTabControl.Items.OfType<TabItem>().FirstOrDefault(t=>Convert.ToString(t.Header)=="AI / VP SETTING");
            if(aiTab?.Content is ScrollViewer aiScroll)
            {
                foreach(var element in LogicalDescendants<FrameworkElement>(aiScroll))
                {
                    var m=element.Margin;
                    element.Margin=new Thickness(m.Left,Math.Max(0,Math.Min(5,m.Top)),m.Right,Math.Max(0,Math.Min(5,m.Bottom)));
                    // Keep controls full height; recover space from gaps, not by clipping button content.
                    if(element is Button button)button.MinHeight=Math.Max(30,button.MinHeight);
                }
                if(aiScroll.Content is FrameworkElement root)root.Margin=new Thickness(5,8,5,16);
            }
        }
        private void CompactOperationScrollContents()
        {
            // FIX8: keep the last status/help line inside the scroll extent and recover enough
            // vertical space that PTZF/PRESET cards are not clipped at the default top position.
            foreach(string header in new[]{"PTZF","PRESET"})
            {
                var tab=LogicalDescendants<TabItem>(Content as DependencyObject).FirstOrDefault(t=>Convert.ToString(t.Header)==header);
                if(!(tab?.Content is ScrollViewer scroll) || !(scroll.Content is StackPanel root))continue;
                scroll.Padding=new Thickness(6,4,8,4);
                root.Margin=new Thickness(2,4,2,4);
                foreach(var card in root.Children.OfType<Border>())
                {
                    var margin=card.Margin;
                    card.Margin=new Thickness(margin.Left,Math.Min(4,margin.Top),margin.Right,Math.Min(4,margin.Bottom));
                    var padding=card.Padding;
                    card.Padding=new Thickness(padding.Left,Math.Min(10,padding.Top),padding.Right,Math.Min(10,padding.Bottom));
                }
                foreach(var text in LogicalDescendants<TextBlock>(root))
                {
                    var margin=text.Margin;
                    text.Margin=new Thickness(margin.Left,Math.Min(6,margin.Top),margin.Right,Math.Min(6,margin.Bottom));
                }
                foreach(var nested in LogicalDescendants<Border>(root).Where(b=>!ReferenceEquals(b.Parent,root)))
                {
                    var margin=nested.Margin;
                    nested.Margin=new Thickness(margin.Left,Math.Min(6,margin.Top),margin.Right,Math.Min(6,margin.Bottom));
                    var padding=nested.Padding;
                    nested.Padding=new Thickness(padding.Left,Math.Min(6,padding.Top),padding.Right,Math.Min(6,padding.Bottom));
                }
                // At the bottom endpoint, align the viewport with a complete nearby heading or
                // preset snapshot, instead of cutting its first line in half. This is real scroll
                // content (not exterior padding), so it participates in the scroll extent.
                var tail=new Border{Height=0,IsHitTestVisible=false};root.Children.Add(tail);
                bool adjusting=false;
                root.LayoutUpdated+=(s,e)=>
                {
                    if(adjusting || !tab.IsSelected || scroll.ViewportHeight<=0)return;
                    double baseOffset=scroll.ScrollableHeight-tail.Height;
                    if(baseOffset<=0)return;
                    double anchor=double.PositiveInfinity;
                    foreach(var text in LogicalDescendants<TextBlock>(root))
                    {
                        string path=BindingOperations.GetBinding(text,TextBlock.TextProperty)?.Path?.Path;
                        if(text.ActualHeight<=0 || text.Visibility!=Visibility.Visible || !(text.FontWeight==FontWeights.Bold || path=="SelectedLaPresetDetailText" || path=="SelectedPresetDetailText"))continue;
                        bool hidden=false;
                        for(DependencyObject parent=text.Parent;parent!=null && parent!=root;parent=LogicalTreeHelper.GetParent(parent))
                            if(parent is UIElement ui && ui.Visibility!=Visibility.Visible){hidden=true;break;}
                        if(hidden)continue;
                        double top=text.TranslatePoint(new Point(),scroll).Y+scroll.VerticalOffset-scroll.Padding.Top;
                        if(top>=baseOffset && top<=baseOffset+100)anchor=Math.Min(anchor,top);
                    }
                    double desired=double.IsPositiveInfinity(anchor)?0:Math.Max(0,anchor-baseOffset);
                    if(Math.Abs(tail.Height-desired)<1)return;
                    adjusting=true;tail.Height=desired;adjusting=false;
                };
            }
        }
        private static T Ancestor<T>(DependencyObject item) where T : DependencyObject
        {
            while (item != null) { if (item is T found) return found; item = item is Visual ? VisualTreeHelper.GetParent(item) : LogicalTreeHelper.GetParent(item); } return null;
        }
        internal static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            { var child = VisualTreeHelper.GetChild(root, i); if (child is T item) yield return item; foreach (var descendant in Descendants<T>(child)) yield return descendant; }
        }
        private static IEnumerable<T> LogicalDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            if(root==null)yield break;
            foreach(object item in LogicalTreeHelper.GetChildren(root)) if(item is DependencyObject child)
            {if(child is T match)yield return match;foreach(var descendant in LogicalDescendants<T>(child))yield return descendant;}
        }
        private void ArrangeAlarmBanners(Border border)
        {
            if (!(border?.Child is Grid grid)) return;
            var banners = grid.Children.OfType<Border>().Where(b => Descendants<TextBlock>(b).Any(t => t.Text.Contains("DETECTED /"))).ToArray();
            if (banners.Length == 0) return;
            var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 12, 0, 0), IsHitTestVisible = false };
            foreach (var banner in banners) { grid.Children.Remove(banner); banner.Margin = new Thickness(0, 0, 0, 5); panel.Children.Add(banner); }
            Panel.SetZIndex(panel, 40); grid.Children.Add(panel);
        }
        private void AlignConnectionStatusRows()
        {
            var summary=LogicalDescendants<TextBlock>(Content as DependencyObject).FirstOrDefault(t=>t.Text=="4. DEVICE CONNECTION STATUS");
            if(summary!=null)
            {
                summary.HorizontalAlignment=HorizontalAlignment.Left;
                if(summary.Parent is StackPanel summaryPanel && summaryPanel.Parent is Border summaryCard)
                {
                    summaryCard.Background=(Brush)new BrushConverter().ConvertFromString("#495057");
                    summaryCard.BorderBrush=(Brush)new BrushConverter().ConvertFromString("#95A5B0");
                }
            }
            foreach (var label in Descendants<TextBlock>(Content as DependencyObject).Where(t => new[] { "CONTROL", "CAMERA", "DETECT" }.Contains(t.Text)).ToArray())
            {
                var grid = label.Parent as Grid; if (grid == null || grid.ColumnDefinitions.Count != 4) continue;
                grid.ColumnDefinitions.Clear();
                for(int i=0;i<4;i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = i==0?new GridLength(60):GridLength.Auto });
                label.Margin=new Thickness(0,0,12,0);
                foreach (var text in grid.Children.OfType<TextBlock>().Where(t => t != label).ToArray())
                {
                    var runs = text.Inlines.OfType<System.Windows.Documents.Run>().ToArray(); if (runs.Length != 2) continue;
                    int column=Grid.GetColumn(text);
                    var cell = new Grid { Margin = column==1?new Thickness(0):new Thickness(10,0,0,0) }; Grid.SetColumn(cell,column);
                    cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    foreach (var pair in runs.Select((r, i) => new { Run = r, Index = i }))
                    {
                        var item = new TextBlock { Text = pair.Run.Text.Trim(), Foreground = pair.Run.Foreground, FontSize = 10, FontWeight = FontWeights.Bold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = pair.Index == 1 ? new Thickness(4, 0, 0, 0) : new Thickness(0) };
                        var binding = BindingOperations.GetBinding(pair.Run, System.Windows.Documents.Run.TextProperty); if (binding != null) item.SetBinding(TextBlock.TextProperty, binding);
                        var foregroundBinding=BindingOperations.GetBinding(pair.Run,System.Windows.Documents.TextElement.ForegroundProperty);
                        if(foregroundBinding!=null)item.SetBinding(TextBlock.ForegroundProperty,foregroundBinding);
                        Grid.SetColumn(item, pair.Index); cell.Children.Add(item);
                    }
                    cell.Style = null;
                    var visibility = BindingOperations.GetBinding(text, VisibilityProperty);
                    if (visibility != null) cell.SetBinding(VisibilityProperty, visibility);
                    // Keep capability visibility triggers for mobile-only fields.
                    cell.SetBinding(VisibilityProperty, new Binding("IsGpsTabVisible") { Converter = new BooleanToVisibilityConverter() });
                    if (text.Style == null) cell.ClearValue(VisibilityProperty);
                    grid.Children.Remove(text); grid.Children.Add(cell);
                }
            }
        }
        private void AddShortcutHelp()
        {
            foreach (var title in Descendants<TextBlock>(Content as DependencyObject).Where(t => t.Text.Contains("W : ZOOM") || t.Text.Contains("W/S")).ToArray())
                if (!title.Text.Contains("Z :")) title.Text += "\nZ : PT SPEED −    C : PT SPEED +";
        }
        private void ShutdownV30Ui()
        {
            _globalKeyboard?.Dispose(); _globalKeyboard = null; vm.PropertyChanged -= V30PropertyChanged; vm.ShutdownV30Control();
        }
        private string MouseControlToken(object sender) => "mouse/" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(sender);
        private readonly HashSet<UIElement> _captureOwners = new HashSet<UIElement>();
        private void BeginMouseControl(object sender, string axis, int x, int y, bool? ir = null)
        {
            var element = sender as UIElement;
            if (element != null)
            {
                if (_captureOwners.Add(element)) element.LostMouseCapture += (s, e) => vm.ReleaseControlInput(MouseControlToken(s));
                element.CaptureMouse();
            }
            vm.PressControlInput(MouseControlToken(sender), axis, x, y, ir ?? (_primaryVideoType == VideoPopoutCameraType.Ir));
        }
        private void EndMouseControl(object sender)
        {
            vm.ReleaseControlInput(MouseControlToken(sender));
            (sender as UIElement)?.ReleaseMouseCapture();
        }
    }
}
