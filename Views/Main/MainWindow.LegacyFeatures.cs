using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System;
using System.Linq;
using System.Windows.Media;
using System.Threading.Tasks;
using OpenCvWpfTracking.Models.Main;

namespace OpenCvWpfTracking
{
    public partial class MainWindow
    {
        private PanoramaLinkedView linkedPanorama;

        private void InitializeLegacyFeatures()
        {
            var host = PanoramaImage.Parent as Grid;

            if (host == null)
                return;

            linkedPanorama = new PanoramaLinkedView(vm, () => _currentPanoramaFilePath ?? "BUNDLED_DEFAULT");
            linkedPanorama.SetBinding(PanoramaLinkedView.SourceProperty, new Binding("Source") { Source = PanoramaImage });
            Panel.SetZIndex(linkedPanorama, 1);
            host.Children.Add(linkedPanorama);
            linkedPanorama.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                    PanoramaImage_MouseLeftButtonDown(PanoramaImage, e);
            };
            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(linkedPanorama, 1);
            // 기존 원본 바인딩을 유지하고 영상 행에는 연동된 보기만 표시한다.
            PanoramaImage.Visibility = Visibility.Collapsed;
            PanoramaImage.IsHitTestVisible = false;
            Grid.SetRow(PanoramaImage, 1);
            Grid.SetRow(PanoramaEmptyText, 1);
            Grid.SetRow(PanoramaLoadingOverlay, 1);
            var bar = new WrapPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(31, 41, 49)),
                Margin = new Thickness(4)
            };
            var toolbarScroll = new ScrollViewer
            {
                Name = "PanoramaRouteToolbar",
                Content = bar,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 12, 10),
                MaxHeight = 130
            };
            Panel.SetZIndex(toolbarScroll, 3);
            host.Children.Add(toolbarScroll);
            var channel = new ComboBox
            {
                ItemsSource = new[]
                {
                    "EO 경로",
                    "IR 경로"
                },
                SelectedIndex = 0,
                MinWidth = 80,
                Margin = new Thickness(3)
            };
            bar.Children.Add(channel);
            var route = new ComboBox
            {
                ItemsSource = new[]
                {
                    "경로 A",
                    "경로 B",
                    "경로 C",
                    "경로 D",
                    "경로 E"
                },
                SelectedIndex = 0,
                MinWidth = 86,
                Margin = new Thickness(3)
            };
            bar.Children.Add(route);
            var name = new TextBox
            {
                Width = 125,
                MaxLength = 80,
                Margin = new Thickness(3),
                Padding = new Thickness(4),
                ToolTip = "경로 이름 · 저장 버튼으로 적용"
            };
            bar.Children.Add(name);
            var message = new TextBlock
            {
                Foreground = Brushes.LightSteelBlue,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };
            var editor = new Expander
            {
                Header = "경로 설정·지점 목록",
                Foreground = Brushes.White,
                Margin = new Thickness(5)
            };
            bar.Children.Add(editor);
            var editPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(31, 41, 49))
            };
            var editScroll = new ScrollViewer
            {
                Content = editPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 200,
                Padding = new Thickness(5, 5, 14, 12)
            };
            editor.Content = editScroll;
            var options = new WrapPanel();
            editPanel.Children.Add(options);

            foreach (var pair in new[]
            {
                Tuple.Create("지점별 속도/대기", "IndividualSettings"),
                Tuple.Create("PAN 좌우 순서", "PanOrder"),
                Tuple.Create("왕복 (해제: 순환)", "PingPong")
            }

            )
            {
                var check = new CheckBox
                {
                    Content = pair.Item1,
                    Foreground = Brushes.White,
                    Margin = new Thickness(5)
                };
                check.SetBinding(CheckBox.IsCheckedProperty, new Binding(pair.Item2) { Mode = BindingMode.TwoWay });
                options.Children.Add(check);
            }

            options.Children.Add(new TextBlock { Text = "공통 속도 / 대기(초) 1~60", Foreground = Brushes.White, Margin = new Thickness(5) });

            foreach (string property in new[]
            {
                "Speed",
                "Delay"
            }

            )
            {
                var box = new TextBox
                {
                    Width = 40,
                    Margin = new Thickness(4)
                };
                box.SetBinding(TextBox.TextProperty, new Binding(property) { Mode = BindingMode.TwoWay, ValidatesOnExceptions = true });
                options.Children.Add(box);
            }

            var table = new DataGrid
            {
                Name = "PanoramaRoutePoints",
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                Height = 140,
                MinWidth = 0,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(52, 60, 66)),
                Foreground = Brushes.White,
                RowBackground = new SolidColorBrush(Color.FromRgb(63, 72, 79)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(52, 60, 66)),
                HeadersVisibility = DataGridHeadersVisibility.Column
            };
            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(55, 89, 105))));
            headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5, 3, 5, 3)));
            table.ColumnHeaderStyle = headerStyle;
            editPanel.Children.Add(table);
            string[] properties =
            {
                "Order",
                "Name",
                "Pan",
                "Tilt",
                "EoZoom",
                "EoFocus",
                "IrZoom",
                "IrFocus",
                "Speed",
                "Delay"
            };
            string[] headers =
            {
                "순서",
                "지점명",
                "PAN(°)",
                "TILT(°)",
                "EO 줌",
                "EO 초점",
                "IR 줌",
                "IR 초점",
                "속도",
                "관측(초)"
            };

            for (int i = 0; i < properties.Length; i++)
            {
                string property = properties[i];
                table.Columns.Add(new DataGridTextColumn { Header = headers[i], Binding = new Binding(property) { Mode = BindingMode.TwoWay, ValidatesOnExceptions = true }, IsReadOnly = property == "Order", Width = property == "Name" ? 100 : 65 });
            }

            bool refreshing = false;
            ScanRoute displayed = null;
            table.SelectionChanged += (s, e) =>
            {
                if (!refreshing)
                    vm.SelectedRoutePoint = table.SelectedItem as ScanRoutePoint;
            };
            Action refresh = () =>
            {
                if (refreshing)
                    return;

                refreshing = true;

                try
                {
                    var current = vm.CurrentRoute;

                    if (displayed != current || !name.IsKeyboardFocusWithin)
                        name.Text = current.Name;

                    displayed = current;
                    editPanel.DataContext = current;
                    table.ItemsSource = current.Points;
                    table.SelectedItem = vm.SelectedRoutePoint;
                    table.IsReadOnly = vm.IsRouteRunning;
                    channel.IsEnabled = route.IsEnabled = name.IsEnabled = options.IsEnabled = !vm.IsRouteRunning;
                    message.Text = vm.RouteStatus;
                    linkedPanorama.InvalidateVisual();
                }
                catch (Exception error)
                {
                    message.Text = error.Message;
                    Common.ConsoleLogHelper.Error("PANORAMA ROUTE UI", "Refresh failed", error);
                }
                finally
                {
                    refreshing = false;
                }
            };
            channel.SelectionChanged += (s, e) =>
            {
                if (!refreshing)
                {
                    vm.RouteInfrared = channel.SelectedIndex == 1;
                    refresh();
                }
            };
            route.SelectionChanged += (s, e) =>
            {
                if (!refreshing)
                {
                    vm.RouteIndex = route.SelectedIndex;
                    refresh();
                }
            };
            vm.RouteUiChanged += refresh;
            Closed += (s, e) => vm.RouteUiChanged -= refresh;
            System.ComponentModel.PropertyChangedEventHandler routeDeviceChanged = (s, e) =>
            {
                if (e.PropertyName == "SelectedControlAgentProfile" ||
                    e.PropertyName == "ControlAgentIp" ||
                    e.PropertyName == "ControlAgentPortText")
                    refresh();
            };
            vm.PropertyChanged += routeDeviceChanged;
            Closed += (s, e) => vm.PropertyChanged -= routeDeviceChanged;
            Action commit = () =>
            {
                if (!table.CommitEdit(DataGridEditingUnit.Cell, true) || !table.CommitEdit(DataGridEditingUnit.Row, true))
                    throw new InvalidOperationException("편집 중인 값의 형식을 확인하세요.");

                vm.CurrentRoute.Name = name.Text;
            };
            AddRouteButton(bar, "저장", () =>
            {
                commit();
                vm.SaveCurrentRoute();
            }, message);
            AddRouteButton(bar, "현재 위치 추가", () =>
            {
                commit();
                vm.AddCurrentRoutePoint();
            }, message);
            AddRouteButton(bar, "선택 삭제", () =>
            {
                if (MessageBox.Show(this, "선택한 지점을 삭제할까요?", "경로", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    vm.DeleteSelectedRoutePoint();
            }, message);
            AddRouteAsyncButton(bar, "선택 이동", async () =>
            {
                commit();
                await vm.RunRouteAsync(true);
            }, message);
            AddRouteAsyncButton(bar, "경로 실행", async () =>
            {
                commit();
                await vm.RunRouteAsync(false);
            }, message);
            AddRouteButton(bar, "정지", () => vm.StopRoute(), message);
            AddRouteButton(bar, "사진 기준 맞춤", () => linkedPanorama.BeginRegistration(), message);
            AddRouteButton(bar, "저장 기준 사용", () => linkedPanorama.LoadRegistration(), message);
            AddRouteAsyncButton(bar, "EO 이미지 저장", async () =>
            {
                await LegacyVideoIntegration.SaveSnapshotAsync(vm, false);
                message.Text = "EO 이미지 저장 완료";
            }, message);
            AddRouteAsyncButton(bar, "IR 이미지 저장", async () =>
            {
                await LegacyVideoIntegration.SaveSnapshotAsync(vm, true);
                message.Text = "IR 이미지 저장 완료";
            }, message);
            AddRouteButton(bar, "저장 폴더", () =>
            {
                string folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Snapshots");
                System.IO.Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true });
            }, message);
            var follow = new CheckBox
            {
                Content = "시선 따라가기",
                Foreground = Brushes.White,
                Margin = new Thickness(6)
            };
            follow.Checked += (s, e) => linkedPanorama.SetFollowing(true);
            follow.Unchecked += (s, e) => linkedPanorama.SetFollowing(false);
            bar.Children.Add(follow);
            bar.Children.Add(message);

            // 펼친 편집창 높이 때문에 옆의 선택 상자와 버튼까지 늘어나지 않게 한다.
            foreach (var child in bar.Children.OfType<FrameworkElement>())
                child.VerticalAlignment = VerticalAlignment.Top;

            host.SizeChanged += (s, e) =>
            {
                toolbarScroll.MaxHeight = Math.Max(40, host.ActualHeight * .55);
                editScroll.MaxHeight = Math.Max(50, Math.Min(200, host.ActualHeight * .4));
                editor.MaxWidth = Math.Max(100, host.ActualWidth - 40);
                editPanel.Width = Math.Max(80, Math.Min(760, host.ActualWidth - 72));
                table.MaxWidth = editPanel.Width;
            };
            refresh();
        }

        private static void AddRouteButton(Panel panel, string text, Action action, TextBlock status)
        {
            var button = new Button
            {
                Content = text,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(3),
                FontSize = 12
            };
            button.Click += (s, e) =>
            {
                try
                {
                    action();
                }
                catch (Exception error)
                {
                    status.Text = error.Message;
                }
            };
            panel.Children.Add(button);
        }

        private static void AddRouteAsyncButton(Panel panel, string text, Func<Task> action, TextBlock status)
        {
            var button = new Button
            {
                Content = text,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(3),
                FontSize = 12
            };
            button.Click += async (s, e) =>
            {
                button.IsEnabled = false;

                try
                {
                    await action();
                }
                catch (Exception error)
                {
                    status.Text = error.Message;
                }
                finally
                {
                    button.IsEnabled = true;
                }
            };
            panel.Children.Add(button);
        }
    }
}
