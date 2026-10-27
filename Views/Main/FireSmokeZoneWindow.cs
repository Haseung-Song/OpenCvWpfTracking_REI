using OpenCvWpfTracking.Services.Position;
using OpenCvWpfTracking.ViewModels.Main;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Data;

namespace OpenCvWpfTracking
{
    public sealed class FireSmokeZoneWindow : Window
    {
        private readonly MainViewModel _vm;

        private readonly FireSmokeZoneFile _data;

        private int _calibrationGeneration;

        private readonly string _equipment, _eoSource, _irSource;

        private readonly ComboBox _camera = new ComboBox
        {
            ItemsSource = new[]
            {
                "EO",
                "IR"
            },
            SelectedIndex = 0
        };

        private readonly ComboBox _scenes = new ComboBox();

        private readonly ComboBox _presets = new ComboBox();

        private MainViewModel.ZonePresetOption _pendingPreset;

        private ZoneFrameContext _pendingFrame;

        private string _pendingCamera, _pendingSignature;

        private DateTime _pendingUntil;

        private string _pendingWaitReason;

        private readonly ListBox _zones = new ListBox
        {
            MinHeight = 42,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };

        private readonly TextBox _sceneName = new TextBox();

        private readonly TextBlock _zoneEmpty = new TextBlock
        {
            Text = "등록 구역 없음\n상단 ‘그리기’ → 영상에서 드래그\n이름은 장면 ‘상세’에서 수정",
            Foreground = Brush("#A9BDCA"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12),
            IsHitTestVisible = false
        };

        private readonly ComboBox _kind = new ComboBox
        {
            ItemsSource = new[]
            {
                "허용",
                "주의",
                "위험"
            },
            SelectedIndex = 1
        };

        private readonly ComboBox _target = new ComboBox
        {
            ItemsSource = new[]
            {
                "FIRE",
                "SMOKE",
                "BOTH"
            },
            SelectedIndex = 2
        };

        private readonly Canvas _canvas = new Canvas
        {
            Background = Brushes.Black,
            Width = 1280,
            Height = 720,
            ClipToBounds = true
        };

        private readonly TextBlock _state = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.Bold
        };

        private readonly TextBlock _health = new TextBlock
        {
            Foreground = Brush("#8FD3FF"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        private readonly TextBlock _message = new TextBlock
        {
            Text = "고정 설치 · ZERO 기준 자동 보정 · 장비 설치 위치·방향 변경 시 재등록",
            Foreground = Brush("#FFD166"),
            TextWrapping = TextWrapping.Wrap
        };

        private readonly TextBlock _previewTitle = new TextBlock
        {
            Foreground = Brush("#FFAB91"),
            FontWeight = FontWeights.Bold
        };

        private readonly TextBlock[] _values = Enumerable.Range(
            0,
            6
        ).Select(i => new TextBlock { Foreground = Brush("#A9E2F3"), TextTrimming = TextTrimming.CharacterEllipsis }).ToArray();

        private readonly DispatcherTimer _timer;

        private Button _eoTab, _irTab;

        private Viewbox _view;

        private FireSmokeScene _scene;

        private FireSmokeZone _selectedZone;

        private FireSmokeZone _redrawZone;

        private bool _refreshingZones;

        private bool _refreshingScenes;

        private bool _zoneEditorClosed;

        private Point? _start;

        private Rectangle _draft;

        private bool _drawing;

        private bool _identityChanged;

        private double _displayScale = 1;

        public FireSmokeZoneWindow(MainViewModel vm)
        {
            _vm = vm;
            _data = vm.GetZoneEditorData();
            _calibrationGeneration = vm.ZoneCalibrationGeneration;
            _equipment = vm.ZoneEquipmentKey;
            _eoSource = vm.EoSourceAddress;
            _irSource = vm.IrSourceAddress;
            Title = "FIRE / SMOKE ZONE SETTING";
            Width = 1240;
            Height = 820;
            MinWidth = 1000;
            MinHeight = 720;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brush("#20272D");
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Malgun Gothic");
            FontSize = 12;

            foreach (var type in new[]
            {
                typeof(TextBox),
                typeof(ComboBox)
            }

            )
                Resources[type] = new Style(type)
                {
                    Setters =
                    {
                        new Setter(Control.PaddingProperty, new Thickness(5, 3, 5, 3)),
                        new Setter(Control.MarginProperty, new Thickness(0, 2, 0, 4)),
                        new Setter(Control.MinHeightProperty, 27.0)
                    }
                };

            Resources[typeof(ListBox)] = new Style(typeof(ListBox))
            {
                Setters =
                {
                    new Setter(Control.BackgroundProperty, Brush("#252F37")),
                    new Setter(Control.ForegroundProperty, Brushes.White),
                    new Setter(Control.BorderBrushProperty, Brush("#657E8C")),
                    new Setter(Control.PaddingProperty, new Thickness(4)),
                    new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled)
                }
            };
            Resources[typeof(ListBoxItem)] = new Style(typeof(ListBoxItem))
            {
                Setters =
                {
                    new Setter(Control.PaddingProperty, new Thickness(4)),
                    new Setter(Control.ForegroundProperty, Brushes.White),
                    new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
                }
            };
            var item = new FrameworkElementFactory(typeof(StackPanel));
            var itemText = new FrameworkElementFactory(typeof(TextBlock), "ZoneItemLabel");
            itemText.SetBinding(TextBlock.TextProperty, new Binding());
            itemText.SetBinding(TextBlock.ForegroundProperty, new Binding("DisplayColor"));
            itemText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            item.AppendChild(itemText);
            var thumbnail = new FrameworkElementFactory(typeof(Image));
            thumbnail.SetBinding(Image.SourceProperty, new Binding("Thumbnail"));
            var previewHeight = new MultiBinding
            {
                Converter = new ZoneThumbnailHeightConverter()
            };
            previewHeight.Bindings.Add(new Binding("ActualHeight")
            {
                RelativeSource = new RelativeSource(
                RelativeSourceMode.FindAncestor,
                typeof(ListBox),
                1
            )
            });
            previewHeight.Bindings.Add(new Binding("Items.Count")
            {
                RelativeSource = new RelativeSource(
                RelativeSourceMode.FindAncestor,
                typeof(ListBox),
                1
            )
            });
            previewHeight.Bindings.Add(new Binding("ActualHeight") { ElementName = "ZoneItemLabel" });
            thumbnail.SetBinding(FrameworkElement.HeightProperty, previewHeight);
            thumbnail.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 6, 0, 4));
            thumbnail.SetValue(Image.StretchProperty, Stretch.Uniform);
            item.AppendChild(thumbnail);
            _zones.ItemTemplate = new DataTemplate
            {
                VisualTree = item
            };
            // 최초 선택과 드래그 중인 구역에도 저장 후와 같은 색상을 즉시 적용한다.
            ConfigureColorChoices(_kind, FireSmokeZone.ColorForKind);
            ConfigureColorChoices(_target, TargetColor);
            _target.SelectionChanged += (s, e) => _target.Foreground = Brush(TargetColor((string)_target.SelectedItem));
            _target.Foreground = Brush(TargetColor((string)_target.SelectedItem));
            _kind.SelectionChanged += (s, e) => UpdateZoneKindColor();
            UpdateZoneKindColor();
            var root = new Grid
            {
                Margin = new Thickness(16)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var head = new StackPanel();
            Heading(head, "FIRE / SMOKE ZONE SETTING", "#FFAB91", 19);
            var actions = new WrapPanel
            {
                Margin = new Thickness(0, 4, 0, 2)
            };
            _eoTab = MakeButton("EO", () => _camera.SelectedIndex = 0);
            _irTab = MakeButton("IR", () => _camera.SelectedIndex = 1);
            actions.Children.Add(_eoTab);
            actions.Children.Add(_irTab);
            actions.Children.Add(MakeButton("그리기", () =>
            {
                if (_pendingPreset != null)
                    throw new InvalidOperationException("자동 캡처 완료 후 그리세요.");

                if (_scene == null)
                    throw new InvalidOperationException("프리셋 선택 후 이동 후 캡처를 실행하세요.");

                CancelDrawing();
                _drawing = true;
                Notice("영상 위에서 드래그하세요.");
            }));
            actions.Children.Add(MakeButton("다시 그리기", BeginRedraw));
            foreach (Button button in actions.Children)
            {
                button.MinWidth = 88;
                button.MinHeight = 36;
                button.Padding = new Thickness(14, 6, 14, 6);
                button.Margin = new Thickness(0, 0, 12, 6);
            }

            head.Children.Add(actions);
            root.Children.Add(Card(head, new Thickness(0, 0, 0, 10)));
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition());
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350) });
            var preview = new Grid();
            preview.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            preview.RowDefinitions.Add(new RowDefinition());
            preview.Children.Add(_previewTitle);
            var view = new Viewbox
            {
                Stretch = Stretch.Uniform,
                Child = _canvas,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(view, 1);
            preview.Children.Add(view);
            view.SizeChanged += (s, e) =>
            {
                _displayScale = Math.Max(.01, Math.Min(view.ActualWidth / _canvas.Width, view.ActualHeight / _canvas.Height));
                UpdateStrokes();
            };
            _view = view;
            var imageCard = Card(preview, new Thickness(0, 0, 12, 0));
            imageCard.Background = Brushes.Black;
            body.Children.Add(imageCard);
            var right = new Grid();
            right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            right.RowDefinitions.Add(new RowDefinition());
            var status = new StackPanel();
            Heading(status, "CAPTURE STATUS", "#A9E2F3");
            var table = new Grid();
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            table.ColumnDefinitions.Add(new ColumnDefinition());
            var names = new[]
            {
                "CHANNEL",
                "MODE",
                "PAN / TILT",
                "ZOOM",
                "CAPTURE",
                "DEVICE"
            };

            for (int i = 0; i < 6; i++)
            {
                table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddCell(table, new TextBlock { Text = names[i], FontWeight = FontWeights.Bold }, i, 0);
                AddCell(table, new TextBlock { Text = ":" }, i, 1);
                AddCell(table, _values[i], i, 2);
            }

            status.Children.Add(table);
            status.Children.Add(_health);
            _state.Margin = new Thickness(0, 6, 0, 0);
            status.Children.Add(_state);
            right.Children.Add(Card(status, new Thickness(0, 0, 0, 8)));
            var presetRow = new Grid
            {
                Margin = new Thickness(0, 10, 0, 4)
            };
            presetRow.ColumnDefinitions.Add(new ColumnDefinition());
            presetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _presets.Margin = new Thickness(0, 2, 10, 2);
            presetRow.Children.Add(_presets);
            var moveCapture = MakeButton("이동 후 캡처", BeginPresetCapture);
            moveCapture.Margin = new Thickness(0, 2, 0, 2);
            Grid.SetColumn(moveCapture, 1);
            presetRow.Children.Add(moveCapture);
            status.Children.Add(presetRow);
            _presets.ToolTip = "프리셋 도착 및 영상 안정화 후 자동 캡처";
            _scenes.Margin = new Thickness(0, 10, 0, 8);
            status.Children.Add(_scenes);
            var tools = new WrapPanel
            {
                Margin = new Thickness(0, 2, 0, 0)
            };
            tools.Children.Add(MakeButton("장면 삭제", () =>
            {
                if (_scene != null)
                {
                    _data.Scenes.Remove(_scene);
                    _scene = null;
                    RefreshScenes();
                }
            }));
            tools.Children.Add(MakeButton("장면 상세", Advanced));
            status.Children.Add(tools);
            var zonePanel = new Grid();
            zonePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            zonePanel.RowDefinitions.Add(new RowDefinition());
            zonePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            zonePanel.Children.Add(new TextBlock
            {
                Text = "ZONE EDIT",
                FontWeight = FontWeights.Bold,
                Foreground = Brush("#FFD166"),
                Margin = new Thickness(
                0,
                0,
                0,
                6
            )
            });
            var zoneList = new Grid();
            zoneList.Children.Add(_zones);
            zoneList.Children.Add(_zoneEmpty);
            Grid.SetRow(zoneList, 1);
            zonePanel.Children.Add(zoneList);
            var edit = new StackPanel
            {
                Margin = new Thickness(0, 6, 0, 0)
            };
            var pair = new Grid();
            pair.ColumnDefinitions.Add(new ColumnDefinition());
            pair.ColumnDefinitions.Add(new ColumnDefinition());
            _kind.Margin = new Thickness(0, 2, 6, 4);
            pair.Children.Add(_kind);
            Grid.SetColumn(_target, 1);
            pair.Children.Add(_target);
            edit.Children.Add(pair);
            _kind.ToolTip = "구역 종류";
            _target.ToolTip = "검출 대상";
            var edits = new WrapPanel
            {
                Margin = new Thickness(0, 6, 0, 0)
            };
            edits.Children.Add(MakeButton("수정", EditZone));
            edits.Children.Add(MakeButton("삭제", DeleteZone));
            edit.Children.Add(edits);
            Grid.SetRow(edit, 2);
            zonePanel.Children.Add(edit);
            var zc = Card(zonePanel, new Thickness(0));
            Grid.SetRow(zc, 1);
            right.Children.Add(zc);
            Grid.SetColumn(right, 1);
            body.Children.Add(right);
            Grid.SetRow(body, 1);
            root.Children.Add(body);
            var bottom = new Grid
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            bottom.ColumnDefinitions.Add(new ColumnDefinition());
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bottom.Children.Add(_message);
            var buttons = new WrapPanel();
            buttons.Children.Add(MakeButton("저장", Save));
            buttons.Children.Add(MakeButton("닫기", Close));
            Grid.SetColumn(buttons, 1);
            bottom.Children.Add(buttons);
            Grid.SetRow(bottom, 2);
            root.Children.Add(bottom);
            Content = root;
            _scenes.SelectionChanged += (s, e) => SelectScene();
            _camera.SelectionChanged += (s, e) =>
            {
                _pendingPreset = null;
                CancelDrawing();
                _scene = null;
                RefreshScenes();
            };
            _sceneName.TextChanged += (s, e) =>
            {
                if (_scene != null)
                    _scene.Name = _sceneName.Text.Trim();
            };
            _zones.SelectionChanged += (s, e) => SelectZone();
            _canvas.MouseLeftButtonDown += StartDrawing;
            _canvas.MouseMove += MoveDrawing;
            _canvas.MouseLeftButtonUp += EndDrawing;
            _canvas.LostMouseCapture += (s, e) =>
            {
                if (_start.HasValue)
                    CancelDrawing();
            };
            RefreshScenes();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += (s, e) => RefreshStatus();
            Loaded += (s, e) => _timer.Start();
            _vm.ZoneReferenceChanged += HandleZoneReferenceChanged;
            Closed += (s, e) =>
            {
                _zoneEditorClosed = true;
                _timer.Stop();
                _vm.ZoneReferenceChanged -= HandleZoneReferenceChanged;
                CancelDrawing();
            };
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key != Key.Escape || (_pendingPreset == null && !_drawing && !_start.HasValue))
                    return;
                _pendingPreset = null;
                CancelDrawing();
                Notice("그리기·자동 캡처 대기 취소");
                e.Handled = true;
            };
            RefreshStatus();
        }

        /// <summary>기존 FIRE·SMOKE 색상과 두 색의 중간색으로 대상 선택을 구분한다.</summary>
        private static string TargetColor(string target)
        {
            switch (target)
            {
                case "FIRE":
                    return LegacyVideoIntegration.FireColor;
                case "SMOKE":
                    return LegacyVideoIntegration.SmokeColor;
                default:
                    return "#EC866E";
            }
        }

        /// <summary>콤보박스 기본 배경은 유지하고 선택값과 펼친 목록의 글자에만 등급·대상 색상을 적용한다.</summary>
        private static void ConfigureColorChoices(ComboBox selector, Func<string, string> palette)
        {
            selector.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.PaddingProperty, new Thickness(6, 2, 6, 2));
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new Binding("."));
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            foreach (string choice in selector.Items)
            {
                var trigger = new DataTrigger { Binding = new Binding("."), Value = choice };
                trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brush(palette(choice))));
                style.Triggers.Add(trigger);
            }
            text.SetValue(FrameworkElement.StyleProperty, style);
            border.AppendChild(text);
            selector.ItemTemplate = new DataTemplate { VisualTree = border };

        }

        private static SolidColorBrush Brush(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex);

        private static Border Card(UIElement child, Thickness margin) => new Border
        {
            Child = child,
            Background = Brush("#343E47"),
            BorderBrush = Brush("#8197A5"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8),
            Margin = margin
        };

        private static void Heading(
            Panel p,
            string text,
            string color,
            double size = 12
        ) =>
            p.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = Brush(color),
                FontWeight = FontWeights.Bold,
                FontSize = size,
                Margin = new Thickness(
            0,
            0,
            0,
            5
        )
            });

        private static void AddCell(Grid g, UIElement e, int row, int col)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            g.Children.Add(e);
        }

        private Button MakeButton(string text, Action action)
        {
            var b = new Button
            {
                Content = text,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 2, 6, 2),
                Background = Brush("#34596D"),
                BorderBrush = Brush("#729EB8"),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            b.Click += (s, e) =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Notice(ex.Message, true);
                    OpenCvWpfTracking.Common.ConsoleLogHelper.Warning("ZONE EDIT", "ACTION=" + text + " / " + ex.Message);
                }
            };
            return b;
        }

        private void Notice(string text, bool error = false)
        {
            _message.Text = text;
            _message.Foreground = Brush(error ? "#FF8A80" : text.Contains("완료") ? "#9FE870" : "#FFD166");
        }

        /// <summary>열린 편집본의 미저장 구역도 같은 ZERO 변환을 적용하며 현재 선택은 보존한다.</summary>
        private void HandleZoneReferenceChanged(OpenCvWpfTracking.Services.Video.PanoramaZoneRebase operation)
        {
            if (_zoneEditorClosed || Dispatcher.HasShutdownStarted)
                return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => HandleZoneReferenceChanged(operation)));
                return;
            }
            if (_equipment != _vm.ZoneEquipmentKey || (operation != null && operation.EquipmentKey != _equipment))
                return;
            _pendingPreset = null;
            CancelDrawing();
            if (operation != null)
                FireSmokeZoneService.RebaseZero(_data, operation);
            _calibrationGeneration = _vm.ZoneCalibrationGeneration;
            Draw();
            Notice(operation == null ? "ZERO 기준 확인 완료 / 구역 보존" : "ZERO 좌표 보정 완료 / 구역·선택 유지");
        }

        private bool IdentityValid
        {
            get
            {
                if (_equipment != _vm.ZoneEquipmentKey ||
                    _eoSource != _vm.EoSourceAddress ||
                    _irSource != _vm.IrSourceAddress)
                    _identityChanged = true;

                return !_identityChanged && !_vm.IsZoneReferencePending &&
                    _calibrationGeneration == _vm.ZoneCalibrationGeneration;
            }
        }

        private void RefreshPresets()
        {
            var old = _presets.SelectedItem as MainViewModel.ZonePresetOption;
            var list = _vm.GetZonePresets();
            string before = string.Join(
                "|",
                _presets.Items.Cast<MainViewModel.ZonePresetOption>().Select(p => p.Kind + MainViewModel.ZonePresetSignature(p.Point))
            );
            string after = string.Join("|", list.Select(p => p.Kind + MainViewModel.ZonePresetSignature(p.Point)));

            if (_presets.ItemsSource == null || before != after)
            {
                _presets.ItemsSource = list;
                _presets.SelectedItem = list.FirstOrDefault(p => old != null &&
                    p.Kind == old.Kind &&
                    p.Point.Number == old.Point.Number);
            }
        }

        private void BeginPresetCapture()
        {
            if (!IdentityValid)
                throw new InvalidOperationException("장비 변경 / 창을 다시 여세요.");

            _pendingPreset = null;
            CancelDrawing();
            var preset = _presets.SelectedItem as MainViewModel.ZonePresetOption;
            _vm.MoveZonePreset(preset);
            _pendingPreset = preset;
            _pendingSignature = MainViewModel.ZonePresetSignature(preset.Point);
            _pendingCamera = (string)_camera.SelectedItem;
            _pendingFrame = _vm.CaptureZoneFrame(_pendingCamera);
            _pendingWaitReason = "도착·안정화 확인 중";
            _pendingUntil = DateTime.UtcNow.AddSeconds(60);
            Notice("프리셋 이동 / 도착·안정화 후 자동 캡처");
        }

        private void TryAutoCapture()
        {
            if (_pendingPreset == null)
                return;

            var frame = _vm.CaptureZoneFrame(_pendingCamera);
            string cancel = !IdentityValid ||
                _pendingCamera != (string)_camera.SelectedItem ||
                frame.Epoch != _pendingFrame.Epoch ||
                frame.ChannelEpoch != _pendingFrame.ChannelEpoch ? "연결·장비·채널 변경" : DateTime.UtcNow >= _pendingUntil ? "도착 확인 시간 초과 / " + _pendingWaitReason : !_vm.GetZonePresets().Any(p => p.Kind == _pendingPreset.Kind &&
                ReferenceEquals(
                p.Point,
                _pendingPreset.Point
            )) ||
                _pendingSignature != MainViewModel.ZonePresetSignature(_pendingPreset.Point) ? "프리셋 변경" : null;

            if (cancel != null)
            {
                _pendingPreset = null;
                Notice("자동 캡처 취소 / " + cancel, true);
                return;
            }

            _pendingWaitReason = _vm.ZonePresetArrivalReason(_pendingCamera, _pendingPreset) ?? _vm.ZoneCaptureState(_pendingCamera);

            if (_pendingWaitReason != null)
                return;

            var pending = _pendingPreset;
            _pendingPreset = null;

            try
            {
                CaptureScene(pending);
            }
            catch (Exception ex)
            {
                Notice("자동 캡처 취소 / " + ex.Message, true);
            }
        }

        private void RefreshStatus()
        {
            RefreshPresets();

            if (IdentityValid && (string)_camera.SelectedItem == "EO")
                _vm.RefreshZoneLensState();

            TryAutoCapture();
            _health.Text = _vm.PositionStatusText;
            string reason = _vm.IsZoneReferencePending ? "ZERO 좌표 기준 확인 대기 / 구역 보존" :
                !IdentityValid ? "장비·소스 변경 / 창을 다시 여세요" : _vm.ZoneCaptureState((string)_camera.SelectedItem);
            string mismatch = _scene == null ? null : FireSmokeZoneService.MatchFailure(_scene, _vm.CaptureZoneFrame(_scene.Camera));
            string state = reason != null ? "미확인 / " + reason : _scene == null ? "캡처 가능" : !_vm.ZonePresetCurrent(_scene) ||
                _scene.Invalidated ? "재등록 필요" : mismatch == null ? "적용 가능 / 시점 일치" : mismatch;

            if (_pendingPreset != null)
                state = "자동 캡처 대기\n" + _pendingWaitReason;

            _state.Text = state;
            _state.ToolTip = state + "\n지원 EO의 PTZ/ZOOM 시점 연동은 화각 모델 기반이며 장비 이동/ZERO 후에는 재등록해야 합니다.";
            _state.Foreground = Brush(state.StartsWith("적용 가능") ||
                state == "캡처 가능" ? "#9FE870" : state == "재등록 필요" ||
                !IdentityValid ? "#FF8A80" : "#FFD166");
            _values[0].Text = (string)_camera.SelectedItem;
            _values[1].Text = _scene == null ||
                _scene.PresetNumber == 0 ? "DIRECT" : _scene.PresetKind + " P" + _scene.PresetNumber.ToString("00");
            _values[2].Text = _scene == null ? "—" : _scene.Pan.ToString("F2") + "° / " + _scene.Tilt.ToString("F2") + "°";
            _values[3].Text = _scene == null ? "—" : _scene.Zoom + " / 1000";
            DateTimeOffset date;
            _values[4].Text = _scene != null &&
                DateTimeOffset.TryParse(
                _scene.RegisteredAt,
                out date
            ) ? date.ToString("yyyy-MM-dd HH:mm:ss") : "—";
            _values[5].Text = _vm.SelectedControlAgentProfile.DisplayName;

            foreach (var v in _values)
                v.ToolTip = v.Text;
        }

        private void RefreshScenes()
        {
            var wanted = _scene;
            string camera = (string)_camera.SelectedItem;
            _previewTitle.Text = camera + " / REFERENCE SNAPSHOT";
            _eoTab.Background = Brush(camera == "EO" ? "#286B8A" : "#34596D");
            _irTab.Background = Brush(camera == "IR" ? "#286B8A" : "#34596D");
            _refreshingScenes = true;
            try
            {
                _scenes.ItemsSource = _data.Scenes.Where(s => s.EquipmentKey == _equipment && s.Camera == camera).ToList();
                _scenes.SelectedItem = wanted != null && _scenes.Items.Contains(wanted)
                    ? wanted : _scenes.Items.Cast<FireSmokeScene>().FirstOrDefault();
            }
            finally
            {
                _refreshingScenes = false;
            }
            SelectScene();

            if (_scenes.SelectedItem == null)
            {
                _scene = null;
                _sceneName.Text = "";
                _zones.ItemsSource = null;
                _zoneEmpty.Visibility = Visibility.Visible;
                _canvas.Children.Clear();
                _canvas.Children.Add(new TextBlock { Text = "CAPTURE REQUIRED", FontSize = 28, Foreground = Brush("#91A4AF"), Margin = new Thickness(32) });
            }
        }

        private void SelectScene()
        {
            if (_refreshingScenes)
                return;

            var scene = _scenes.SelectedItem as FireSmokeScene;
            CancelDrawing();
            if (!ReferenceEquals(_scene, scene))
                _selectedZone = null;
            _scene = scene;

            if (_scene == null)
                return;

            _sceneName.Text = _scene.Name;
            _canvas.Width = _scene.FrameWidth;
            _canvas.Height = _scene.FrameHeight;
            Draw();
        }

        private static string DefaultSceneName(string camera, int number) =>
            "[" + camera + "] 탐지 구역 " + Math.Max(
            1,
            number
        ).ToString(CultureInfo.InvariantCulture);

        private void CaptureScene(MainViewModel.ZonePresetOption preset)
        {
            if (!IdentityValid)
                throw new InvalidOperationException("장비·소스·원점 변경 / 창을 다시 여세요.");

            CommitScene();
            var s = _vm.CaptureZoneScene((string)_camera.SelectedItem, out BitmapSource image);
            _vm.LinkZonePreset(s, preset);
            OpenCvWpfTracking.Common.ConsoleLogHelper.State("ZONE CAPTURE",
                "재캡처 연결 확정 / CAMERA=" + s.Camera + " / PRESET=" + s.PresetKind +
                " P" + s.PresetNumber.ToString("00"));
            int number = preset?.Point.Number ?? 1;

            if (preset == null)
                while (_data.Scenes.Any(existing => existing.Camera == s.Camera && existing.Name == DefaultSceneName(s.Camera, number)))
                    number++;

            s.Name = DefaultSceneName(s.Camera, number);
            s.FixedInstallationConfirmed = true;
            _data.Scenes.Add(s);
            _scene = s;
            RefreshScenes();
            _target.SelectedIndex = 2;
            Notice("캡처 완료 / 구역을 그린 뒤 저장하세요.");
        }

        private void CommitScene()
        {
            if (_scene == null)
                return;

            if (string.IsNullOrWhiteSpace(_sceneName.Text))
                throw new InvalidOperationException("장면 이름을 입력하세요.");

            _scene.Name = _sceneName.Text.Trim();
        }

        private void Advanced()
        {
            if (_scene == null)
                throw new InvalidOperationException("장면을 선택하세요.");

            var name = new TextBox
            {
                Text = _scene.Name,
                Margin = new Thickness(0, 8, 0, 8)
            };
            var input = new TextBox
            {
                Text = _scene.AngleTolerance.ToString(CultureInfo.InvariantCulture),
                Margin = new Thickness(0, 8, 0, 8)
            };
            var panel = new StackPanel
            {
                Margin = new Thickness(16)
            };
            panel.Children.Add(new TextBlock { Text = "장면 이름", Foreground = Brushes.White });
            panel.Children.Add(name);
            panel.Children.Add(new TextBlock { Text = "각도 오차 0.05~2° / ZOOM 동일값", Foreground = Brushes.White });
            panel.Children.Add(input);
            var editedScene = _scene;
            var w = new Window
            {
                Title = "장면 상세",
                Width = 360,
                Height = 265,
                ResizeMode = ResizeMode.NoResize,
                Owner = this,
                Background = Brush("#343E47"),
                Content = panel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            panel.Children.Add(MakeButton(
                "적용",
                () =>
            {
                if (!IdentityValid || !_data.Scenes.Contains(editedScene))
                    throw new InvalidOperationException("장면 변경 / 다시 여세요.");

                double n;

                if (string.IsNullOrWhiteSpace(name.Text) ||
                    !double.TryParse(
                    input.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out n
                ) ||
                    n < .05 ||
                    n > 2)
                    throw new InvalidOperationException("이름과 0.05~2° 범위를 확인하세요.");

                RenameScene(editedScene, name.Text.Trim());
                editedScene.AngleTolerance = n;
                RefreshScenes();
                w.Close();
            }
            ));
            w.Show();
        }

        private void RenameScene(FireSmokeScene scene, string name)
        {
            scene.Name = name;

            foreach (var zone in scene.Zones)
                zone.Name = name;

            scene.Revision = Guid.NewGuid().ToString("N");

            if (_scene == scene)
                _sceneName.Text = name;
        }

        private void Draw()
        {
            _canvas.Children.Clear();

            if (_scene == null)
                return;

            BitmapImage b;
            using (var stream = new MemoryStream(_scene.ReferencePng))
            {
                b = new BitmapImage();
                b.BeginInit();
                b.CacheOption = BitmapCacheOption.OnLoad;
                b.StreamSource = stream;
                b.EndInit();
                b.Freeze();
                _canvas.Children.Add(new Image { Source = b, Width = _canvas.Width, Height = _canvas.Height, Stretch = Stretch.Fill, IsHitTestVisible = false });
            }

            foreach (var z in _scene.Zones)
                z.Thumbnail = CreateThumbnail(b, z);

            _zoneEmpty.Visibility = _scene.Zones.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var z in _scene.Zones)
            {
                var color = Brush(z.DisplayColor);
                var r = new Rectangle
                {
                    Width = z.Width * _canvas.Width,
                    Height = z.Height * _canvas.Height,
                    Stroke = color,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(r, z.X * _canvas.Width);
                Canvas.SetTop(r, z.Y * _canvas.Height);
                _canvas.Children.Add(r);
                var label = new TextBlock
                {
                    Text = "ZONE " + z,
                    Tag = r,
                    Foreground = color,
                    Background = Brush("#BB000000"),
                    TextWrapping = TextWrapping.Wrap,
                    IsHitTestVisible = false
                };
                _canvas.Children.Add(label);
            }

            // 목록 갱신 중의 임시 선택 해제는 사용자 선택 변경으로 처리하지 않는다.
            var selected = _selectedZone ?? _zones.SelectedItem as FireSmokeZone;
            _refreshingZones = true;
            try
            {
                _zones.ItemsSource = null;
                _zones.ItemsSource = _scene.Zones;
                _zones.SelectedItem = selected != null && _scene.Zones.Contains(selected) ? selected : null;
            }
            finally
            {
                _refreshingZones = false;
            }
            SelectZone();
            UpdateStrokes();
        }

        private void UpdateStrokes()
        {
            if (_view != null && _view.ActualWidth > 0 && _view.ActualHeight > 0)
                _displayScale = Math.Max(.01, Math.Min(_view.ActualWidth / _canvas.Width, _view.ActualHeight / _canvas.Height));

            foreach (var r in _canvas.Children.OfType<Rectangle>())
                r.StrokeThickness = 2 / _displayScale;

            foreach (var t in _canvas.Children.OfType<TextBlock>().Where(t => t.Tag is Rectangle))
            {
                var box = (Rectangle)t.Tag;
                t.FontSize = 12 / _displayScale;
                t.Padding = new Thickness(3 / _displayScale);
                t.MaxWidth = _canvas.Width;
                t.Measure(new Size(_canvas.Width, double.PositiveInfinity));
                var placement = ZoneOverlayView.LabelBounds(
                    new Rect(
                    Canvas.GetLeft(box) * _displayScale,
                    Canvas.GetTop(box) * _displayScale,
                    box.Width * _displayScale,
                    box.Height * _displayScale
                ),
                    new Size(
                    t.DesiredSize.Width * _displayScale,
                    t.DesiredSize.Height * _displayScale
                ),
                    new Rect(
                    0,
                    0,
                    _canvas.Width * _displayScale,
                    _canvas.Height * _displayScale
                )
                );
                Canvas.SetLeft(t, placement.X / _displayScale);
                Canvas.SetTop(t, placement.Y / _displayScale);
            }
        }

        private static BitmapSource CreateThumbnail(BitmapSource source, FireSmokeZone zone)
        {
            int left = Math.Max(0, (int)(zone.X * source.PixelWidth) - 16), top = Math.Max(0, (int)(zone.Y * source.PixelHeight) - 16);
            int right = Math.Min(
                source.PixelWidth,
                (int)Math.Ceiling((zone.X + zone.Width) * source.PixelWidth) + 16
            ), bottom = Math.Min(
                source.PixelHeight,
                (int)Math.Ceiling((zone.Y + zone.Height) * source.PixelHeight) + 16
            );

            if (right <= left || bottom <= top)
                return null;

            var crop = new CroppedBitmap(source, new Int32Rect(left, top, right - left, bottom - top));
            // 테두리 크기의 미리보기 품질은 유지하되 비트맵 할당량을 제한한다.
            var visual = new DrawingVisual();
            double scale = Math.Min(1, Math.Min(512.0 / crop.PixelWidth, 288.0 / crop.PixelHeight));
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(crop, new Rect(0, 0, crop.PixelWidth * scale, crop.PixelHeight * scale));
                var rect = new Rect(
                    (zone.X * source.PixelWidth - left) * scale,
                    (zone.Y * source.PixelHeight - top) * scale,
                    zone.Width * source.PixelWidth * scale,
                    zone.Height * source.PixelHeight * scale
                );
                dc.DrawRectangle(null, new Pen(Brushes.Black, 4), rect);
                dc.DrawRectangle(null, new Pen(Brush(zone.DisplayColor), 2), rect);
            }

            var image = new RenderTargetBitmap(
                Math.Max(
                1,
                (int)Math.Ceiling(crop.PixelWidth * scale)
            ),
                Math.Max(
                1,
                (int)Math.Ceiling(crop.PixelHeight * scale)
            ),
                96,
                96,
                PixelFormats.Pbgra32
            );
            image.Render(visual);
            image.Freeze();
            return image;
        }

        private void SelectZone()
        {
            if (_refreshingZones)
                return;

            var z = _zones.SelectedItem as FireSmokeZone;
            if (!ReferenceEquals(_selectedZone, z))
                CancelDrawing();
            _selectedZone = z;
            if (z == null)
                return;

            _kind.SelectedItem = z.Kind;
            _target.SelectedItem = z.Fire && z.Smoke ? "BOTH" : z.Fire ? "FIRE" : "SMOKE";
        }

        private void SetZone(FireSmokeZone z)
        {
            z.Name = _scene.Name;
            z.Kind = (string)_kind.SelectedItem;
            z.Fire = (string)_target.SelectedItem != "SMOKE";
            z.Smoke = (string)_target.SelectedItem != "FIRE";
        }

        private void EditZone()
        {
            if (!(_zones.SelectedItem is FireSmokeZone z))
                throw new InvalidOperationException("구역을 선택하세요.");

            SetZone(z);
            _selectedZone = z;
            _scene.Revision = Guid.NewGuid().ToString("N");
            Draw();
            Notice("구역 수정 완료 / 저장 필요");
        }

        private void DeleteZone()
        {
            if (_scene == null || !(_zones.SelectedItem is FireSmokeZone z))
                return;

            _scene.Zones.Remove(z);
            _scene.Revision = Guid.NewGuid().ToString("N");
            Draw();
        }

        /// <summary>장면·다른 구역은 보존하고 선택 구역의 사각형만 새 드래그로 교체한다.</summary>
        private void BeginRedraw()
        {
            if (_pendingPreset != null)
                throw new InvalidOperationException("자동 캡처 완료 후 다시 그리세요.");
            var zone = _zones.SelectedItem as FireSmokeZone;
            if (_scene == null || zone == null || !_scene.Zones.Contains(zone))
                throw new InvalidOperationException("다시 그릴 구역을 선택하세요.");
            CancelDrawing();
            _selectedZone = zone;
            _redrawZone = zone;
            _drawing = true;
            Notice("선택 구역을 다시 드래그하세요 / ESC: 기존 구역 유지");
        }

        /// <summary>유효한 사각형이 완성된 뒤에만 원본을 교체하므로 취소·작은 드래그는 기존 구역을 보존한다.</summary>
        private void ApplyDrawnRectangle(Rect rectangle)
        {
            double width = rectangle.Width / _canvas.Width;
            double height = rectangle.Height / _canvas.Height;
            if (_scene == null || double.IsNaN(width) || double.IsNaN(height) || width < .005 || height < .005)
                throw new InvalidOperationException("구역이 너무 작습니다.");
            var zone = _redrawZone;
            bool redraw = zone != null;
            if (redraw && !_scene.Zones.Contains(zone))
                throw new InvalidOperationException("다시 그릴 구역이 변경되었습니다.");
            if (!redraw)
            {
                zone = new FireSmokeZone();
                SetZone(zone);
                _scene.Zones.Add(zone);
            }
            zone.X = rectangle.X / _canvas.Width;
            zone.Y = rectangle.Y / _canvas.Height;
            zone.Width = width;
            zone.Height = height;
            _selectedZone = zone;
            _scene.Revision = Guid.NewGuid().ToString("N");
            Notice(redraw ? "다시 그리기 완료 / 저장 필요" : "구역 추가 완료 / 저장 필요");
            OpenCvWpfTracking.Common.ConsoleLogHelper.State("ZONE EDIT",
                (redraw ? "선택 구역 사각형 교체" : "구역 추가") + " / CAMERA=" + _scene.Camera + " / COUNT=" + _scene.Zones.Count);
        }

        private Point Clamp(Point p) => new Point(
            Math.Max(
            0,
            Math.Min(
            _canvas.Width,
            p.X
        )
        ),
            Math.Max(
            0,
            Math.Min(
            _canvas.Height,
            p.Y
        )
        )
        );

        private void UpdateZoneKindColor()
        {
            var color = Brush(FireSmokeZone.ColorForKind((string)_kind.SelectedItem));
            _kind.Foreground = color;
            if (_draft != null)
                _draft.Stroke = color;
        }

        private void StartDrawing(object s, MouseButtonEventArgs e)
        {
            if (!_drawing || _scene == null)
                return;

            _start = Clamp(e.GetPosition(_canvas));
            _draft = new Rectangle
            {
                Stroke = Brush(FireSmokeZone.ColorForKind((string)_kind.SelectedItem)),
                StrokeThickness = 3 / _displayScale,
                IsHitTestVisible = false
            };
            _canvas.Children.Add(_draft);
            _canvas.CaptureMouse();
            e.Handled = true;
        }

        private void MoveDrawing(object s, MouseEventArgs e)
        {
            if (!_start.HasValue)
                return;

            var p = Clamp(e.GetPosition(_canvas));
            Canvas.SetLeft(_draft, Math.Min(_start.Value.X, p.X));
            Canvas.SetTop(_draft, Math.Min(_start.Value.Y, p.Y));
            _draft.Width = Math.Abs(p.X - _start.Value.X);
            _draft.Height = Math.Abs(p.Y - _start.Value.Y);
            e.Handled = true;
        }

        private void EndDrawing(object s, MouseButtonEventArgs e)
        {
            if (!_start.HasValue)
                return;

            MoveDrawing(s, e);
            _start = null;
            _canvas.ReleaseMouseCapture();
            _drawing = false;

            try
            {
                ApplyDrawnRectangle(new Rect(Canvas.GetLeft(_draft), Canvas.GetTop(_draft), _draft.Width, _draft.Height));
            }
            catch (Exception ex)
            {
                Notice(ex.Message, true);
            }

            _draft = null;
            _redrawZone = null;
            Draw();
        }

        private void CancelDrawing()
        {
            _start = null;
            _drawing = false;
            _redrawZone = null;

            if (_draft != null)
                _canvas.Children.Remove(_draft);

            _draft = null;
            _canvas.ReleaseMouseCapture();
        }

        private sealed class ZoneThumbnailHeightConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                double height = values.Length > 0 && values[0] is double ? (double)values[0] : 0;
                int count = values.Length > 1 && values[1] is int ? (int)values[1] : 0;
                double label = values.Length > 2 && values[2] is double ? (double)values[2] : 24;
                // 구역 하나는 가용 테두리 영역에 맞추고 여러 구역은 스크롤 목록으로 유지한다.
                return count == 1 ? Math.Max(20, height - label - 28) : 90;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>선택 장면 외의 오래된 연결도 이름과 원인을 알려 불필요한 재캡처 반복을 막는다.</summary>
        private void EnsurePresetReferencesCurrent()
        {
            var stale = _data.Scenes.Where(scene => scene.EquipmentKey == _equipment && !scene.Invalidated &&
                !_vm.ZonePresetCurrent(scene)).ToList();
            if (stale.Count == 0)
                return;
            foreach (var scene in stale)
                OpenCvWpfTracking.Common.ConsoleLogHelper.Warning("ZONE PRESET VALIDATION",
                    "SCENE=" + scene.Name + " / CAMERA=" + scene.Camera + " / PRESET=" + scene.PresetKind +
                    " P" + scene.PresetNumber.ToString("00") + " / REASON=" + _vm.ZonePresetMismatchReason(scene));
            var first = stale[0];
            throw new InvalidOperationException("재캡처 필요: " + first.Camera + " " + first.PresetKind +
                " P" + first.PresetNumber.ToString("00") + " / " + first.Name + (stale.Count > 1 ? " 외 " + (stale.Count - 1) + "건" : ""));
        }

        private void Save()
        {
            if (_pendingPreset != null)
                throw new InvalidOperationException("자동 캡처 완료 후 저장하세요.");

            if (!IdentityValid)
                throw new InvalidOperationException("장비·소스·원점 변경 / 저장하지 않습니다. 창을 다시 여세요.");

            CommitScene();

            EnsurePresetReferencesCurrent();

            foreach (var s in _data.Scenes.Where(s => s.EquipmentKey == _equipment))
            {
                s.FixedInstallationConfirmed = true;
                s.Revision = Guid.NewGuid().ToString("N");
            }

            _vm.SaveZoneEditorData(_data, _calibrationGeneration);
            Notice("저장되었습니다.");
            MessageBox.Show(this, "저장되었습니다.", "구역 저장", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
