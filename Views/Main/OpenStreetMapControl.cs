using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Serilog;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// Lightweight OpenStreetMap tile viewer for the REI demo screen.
    ///
    /// - No external NuGet map control is required.
    /// - Downloads standard OSM raster tiles while online.
    /// - Keeps downloaded tiles in LocalApplicationData for reuse.
    /// - Uses the existing GlobalSystemsTacticalMap.png as a visual fallback.
    /// - Mouse wheel: zoom, drag: pan, double click: handled by the parent window.
    ///
    /// NOTE:
    /// This viewer is intentionally simple and intended for the on-screen
    /// company-location overview, not for high-volume GIS use.
    /// </summary>
    public sealed class OpenStreetMapControl : UserControl
    {
        private const int TileSize = 256;
        private const int MinimumZoom = 3;
        private const int MaximumZoom = 19;

        // GLOBAL SYSTEMS
        // 265, Techno 2-ro, Yuseong-gu, Daejeon
        public const double DefaultLatitude = 36.4186235;
        public const double DefaultLongitude = 127.4118881;
        public const int DefaultZoom = 16;

        private static readonly HttpClient HttpClient =
            CreateHttpClient();

        private readonly Grid _root;
        private readonly Image _fallbackImage;
        private readonly Canvas _tileCanvas;
        private readonly Canvas _markerLayer;
        private Grid _companyMarker;
        private readonly TextBlock _statusText;

        private readonly string _tileCacheRoot;

        private double _centerLatitude;
        private double _centerLongitude;
        private int _zoom;

        private bool _isDragging;
        private bool _hasDragged;
        private Point _dragStartPoint;
        private double _dragStartCenterWorldX;
        private double _dragStartCenterWorldY;
        private long _renderVersion;

        // OSM Tile 실패 로그가 Tile 개수만큼 반복되지 않도록
        // 현재 Offline/Fallback 상태에서 1회만 기록한다.
        private bool _isTileFallbackLogged;

        public OpenStreetMapControl()
        {
            _centerLatitude = DefaultLatitude;
            _centerLongitude = DefaultLongitude;
            _zoom = DefaultZoom;

            ClipToBounds = true;
            Background = Brushes.Black;
            Focusable = true;

            _tileCacheRoot = System.IO.Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "OpenCvWpfTracking",
                "MapTiles");

            _root = new Grid
            {
                Background = Brushes.Black
            };

            _fallbackImage = new Image
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 0.72
            };

            try
            {
                _fallbackImage.Source =
                    new BitmapImage(
                        new Uri(
                            "pack://application:,,,/Images/Demo/GlobalSystemsTacticalMap.png",
                            UriKind.Absolute));
            }
            catch
            {
                // Resource fallback is optional. Tile rendering still works.
            }

            _tileCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                ClipToBounds = true
            };

            _markerLayer = BuildMarkerLayer();

            TextBlock attribution = new TextBlock
            {
                Text = "© OpenStreetMap contributors",
                Foreground = Brushes.White,
                Background =
                    new SolidColorBrush(
                        Color.FromArgb(
                            175,
                            20,
                            24,
                            28)),
                FontSize = 10,
                Padding = new Thickness(5, 2, 5, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5)
            };

            _statusText = new TextBlock
            {
                Text = "OPENSTREETMAP",
                Foreground = Brushes.White,
                Background =
                    new SolidColorBrush(
                        Color.FromArgb(
                            185,
                            24,
                            35,
                            42)),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(6, 3, 6, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(6)
            };

            _root.Children.Add(_fallbackImage);
            _root.Children.Add(_tileCanvas);
            _root.Children.Add(_markerLayer);
            _root.Children.Add(attribution);
            _root.Children.Add(_statusText);

            Content = _root;

            Loaded += OpenStreetMapControl_Loaded;
            SizeChanged += OpenStreetMapControl_SizeChanged;
            MouseWheel += OpenStreetMapControl_MouseWheel;
            MouseLeftButtonDown += OpenStreetMapControl_MouseLeftButtonDown;
            MouseLeftButtonUp += OpenStreetMapControl_MouseLeftButtonUp;
            MouseMove += OpenStreetMapControl_MouseMove;
            MouseLeave += OpenStreetMapControl_MouseLeave;
        }

        public double CenterLatitude
        {
            get { return _centerLatitude; }
        }

        public double CenterLongitude
        {
            get { return _centerLongitude; }
        }

        public int Zoom
        {
            get { return _zoom; }
        }

        public void SetView(
            double latitude,
            double longitude,
            int zoom)
        {
            _centerLatitude =
                ClampLatitude(
                    latitude);

            _centerLongitude =
                NormalizeLongitude(
                    longitude);

            _zoom =
                Math.Max(
                    MinimumZoom,
                    Math.Min(
                        MaximumZoom,
                        zoom));

            RequestRender();
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client =
                new HttpClient();

            client.Timeout =
                TimeSpan.FromSeconds(
                    5);

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "OpenCvWpfTracking-REI/1.0");

            return client;
        }

        /// <summary>
        /// GLOBAL SYSTEMS 고정 좌표 Marker Layer 생성.
        ///
        /// 기존 구현은 Marker를 화면 중앙 Grid에 고정했기 때문에
        /// 지도를 Drag/Zoom해도 Marker가 지도와 함께 이동하지 않았다.
        ///
        /// 현재 구현은 Marker를 Canvas 위에 올리고,
        /// Render 시 회사 위/경도를 현재 화면 Pixel 좌표로 변환하여
        /// 실제 지도 좌표에 고정되도록 한다.
        /// </summary>
        private Canvas BuildMarkerLayer()
        {
            Canvas layer =
                new Canvas
                {
                    IsHitTestVisible = false,
                    ClipToBounds = true
                };

            _companyMarker =
                new Grid
                {
                    Width = 170,
                    Height = 65
                };

            Canvas crosshair =
                new Canvas
                {
                    Width = 44,
                    Height = 44,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top
                };

            Ellipse ring =
                new Ellipse
                {
                    Width = 28,
                    Height = 28,
                    Stroke = Brushes.Red,
                    StrokeThickness = 3
                };

            Canvas.SetLeft(
                ring,
                8);
            Canvas.SetTop(
                ring,
                8);

            Border vertical =
                new Border
                {
                    Width = 3,
                    Height = 44,
                    Background = Brushes.Red
                };

            Canvas.SetLeft(
                vertical,
                20.5);

            Border horizontal =
                new Border
                {
                    Width = 44,
                    Height = 3,
                    Background = Brushes.Red
                };

            Canvas.SetTop(
                horizontal,
                20.5);

            crosshair.Children.Add(
                ring);
            crosshair.Children.Add(
                vertical);
            crosshair.Children.Add(
                horizontal);

            Border labelBorder =
                new Border
                {
                    Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                32,
                                53,
                                61)),
                    BorderBrush = Brushes.White,
                    BorderThickness =
                        new Thickness(
                            1),
                    CornerRadius =
                        new CornerRadius(
                            3),
                    Padding =
                        new Thickness(
                            9,
                            4,
                            9,
                            4),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

            labelBorder.Child =
                new TextBlock
                {
                    Text = "GLOBAL SYSTEMS",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                };

            _companyMarker.Children.Add(
                crosshair);
            _companyMarker.Children.Add(
                labelBorder);

            layer.Children.Add(
                _companyMarker);

            return layer;
        }

        /// <summary>
        /// GLOBAL SYSTEMS 실제 위/경도를 현재 OpenStreetMap 화면 Pixel 위치로 변환한다.
        ///
        /// Marker의 Crosshair 중심점이 회사 좌표와 정확히 일치하도록
        /// Marker Grid의 Left/Top을 보정한다.
        /// </summary>
        private void UpdateCompanyMarkerPosition(
            double viewLeftWorld,
            double viewTopWorld)
        {
            if (_companyMarker == null)
            {
                return;
            }

            double markerWorldX;
            double markerWorldY;

            LatLonToWorldPixel(
                DefaultLatitude,
                DefaultLongitude,
                _zoom,
                out markerWorldX,
                out markerWorldY);

            double markerScreenX =
                markerWorldX -
                viewLeftWorld;

            double markerScreenY =
                markerWorldY -
                viewTopWorld;

            // [2026-08-24] 지도 축척에 맞춰 회사 Marker도 함께 확대/축소한다.
            double markerScale =
                Math.Max(
                    0.45,
                    Math.Min(
                        1.35,
                        1.0 + ((_zoom - DefaultZoom) * 0.10)));

            _companyMarker.RenderTransformOrigin =
                new System.Windows.Point(0, 0);
            _companyMarker.RenderTransform =
                new ScaleTransform(markerScale, markerScale);

            // Marker Grid 170x65:
            // Crosshair 44x44 is horizontally centered,
            // so its center is X=85, Y=22 relative to the Grid.
            Canvas.SetLeft(
                _companyMarker,
                markerScreenX -
                (85.0 * markerScale));

            Canvas.SetTop(
                _companyMarker,
                markerScreenY -
                (22.0 * markerScale));
        }

        private void OpenStreetMapControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Log.Information(
                "[MAP] OpenStreetMap Loaded / CENTER=({Latitude:F6}, {Longitude:F6}) / ZOOM={Zoom}",
                _centerLatitude,
                _centerLongitude,
                _zoom);

            RequestRender();
        }

        private void OpenStreetMapControl_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            RequestRender();
        }

        private void OpenStreetMapControl_MouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            int delta =
                e.Delta > 0
                    ? 1
                    : -1;

            int newZoom =
                Math.Max(
                    MinimumZoom,
                    Math.Min(
                        MaximumZoom,
                        _zoom + delta));

            if (newZoom == _zoom)
            {
                return;
            }

            int previousZoom =
                _zoom;

            _zoom =
                newZoom;

            Log.Information(
                "[MAP] Zoom Changed / {PreviousZoom} -> {CurrentZoom} / CENTER=({Latitude:F6}, {Longitude:F6})",
                previousZoom,
                _zoom,
                _centerLatitude,
                _centerLongitude);

            RequestRender();

            e.Handled = true;
        }

        private void OpenStreetMapControl_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                return;
            }

            Focus();

            _isDragging = true;
            _hasDragged = false;

            _dragStartPoint =
                e.GetPosition(
                    this);

            LatLonToWorldPixel(
                _centerLatitude,
                _centerLongitude,
                _zoom,
                out _dragStartCenterWorldX,
                out _dragStartCenterWorldY);

            CaptureMouse();
        }

        private void OpenStreetMapControl_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isDragging ||
                e.LeftButton !=
                MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPoint =
                e.GetPosition(
                    this);

            double deltaX =
                currentPoint.X -
                _dragStartPoint.X;

            double deltaY =
                currentPoint.Y -
                _dragStartPoint.Y;

            if (Math.Abs(deltaX) >= 2.0 ||
                Math.Abs(deltaY) >= 2.0)
            {
                _hasDragged = true;
            }

            double newCenterWorldX =
                _dragStartCenterWorldX -
                deltaX;

            double newCenterWorldY =
                _dragStartCenterWorldY -
                deltaY;

            WorldPixelToLatLon(
                newCenterWorldX,
                newCenterWorldY,
                _zoom,
                out _centerLatitude,
                out _centerLongitude);

            RequestRender();
        }

        private void OpenStreetMapControl_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            EndDrag();
        }

        private void OpenStreetMapControl_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (e.LeftButton !=
                MouseButtonState.Pressed)
            {
                EndDrag();
            }
        }

        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;

            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }

            if (_hasDragged)
            {
                Log.Information(
                    "[MAP] Drag End / CENTER=({Latitude:F6}, {Longitude:F6}) / ZOOM={Zoom}",
                    _centerLatitude,
                    _centerLongitude,
                    _zoom);
            }

            _hasDragged = false;
        }

        private void RequestRender()
        {
            if (!IsLoaded ||
                ActualWidth < 1 ||
                ActualHeight < 1)
            {
                return;
            }

            long version =
                ++_renderVersion;

            RenderTilesAsync(
                version);
        }

        private async void RenderTilesAsync(
            long version)
        {
            double centerWorldX;
            double centerWorldY;

            LatLonToWorldPixel(
                _centerLatitude,
                _centerLongitude,
                _zoom,
                out centerWorldX,
                out centerWorldY);

            double viewLeftWorld =
                centerWorldX -
                ActualWidth / 2.0;

            double viewTopWorld =
                centerWorldY -
                ActualHeight / 2.0;

            // 2026-08-21:
            // GLOBAL SYSTEMS Marker를 화면 중앙이 아니라
            // 실제 회사 위/경도 좌표의 현재 Screen Pixel 위치에 배치한다.
            UpdateCompanyMarkerPosition(
                viewLeftWorld,
                viewTopWorld);

            int firstTileX =
                (int)Math.Floor(
                    viewLeftWorld /
                    TileSize);

            int firstTileY =
                (int)Math.Floor(
                    viewTopWorld /
                    TileSize);

            int lastTileX =
                (int)Math.Floor(
                    (viewLeftWorld + ActualWidth) /
                    TileSize);

            int lastTileY =
                (int)Math.Floor(
                    (viewTopWorld + ActualHeight) /
                    TileSize);

            int tileCount =
                1 << _zoom;

            _tileCanvas.Children.Clear();

            List<Task> tileTasks =
                new List<Task>();

            for (int tileY = firstTileY;
                 tileY <= lastTileY;
                 tileY++)
            {
                if (tileY < 0 ||
                    tileY >= tileCount)
                {
                    continue;
                }

                for (int rawTileX = firstTileX;
                     rawTileX <= lastTileX;
                     rawTileX++)
                {
                    int tileX =
                        Mod(
                            rawTileX,
                            tileCount);

                    double left =
                        rawTileX *
                        TileSize -
                        viewLeftWorld;

                    double top =
                        tileY *
                        TileSize -
                        viewTopWorld;

                    Image tileImage =
                        new Image
                        {
                            Width = TileSize,
                            Height = TileSize,
                            Stretch = Stretch.Fill,
                            SnapsToDevicePixels = true
                        };

                    RenderOptions.SetBitmapScalingMode(
                        tileImage,
                        BitmapScalingMode.HighQuality);

                    Canvas.SetLeft(
                        tileImage,
                        Math.Floor(
                            left));

                    Canvas.SetTop(
                        tileImage,
                        Math.Floor(
                            top));

                    _tileCanvas.Children.Add(
                        tileImage);

                    tileTasks.Add(
                        LoadTileIntoImageAsync(
                            tileImage,
                            _zoom,
                            tileX,
                            tileY,
                            version));
                }
            }

            try
            {
                await Task.WhenAll(
                    tileTasks);
            }
            catch
            {
                // Individual tile failures are already handled.
            }

            if (version !=
                _renderVersion)
            {
                return;
            }

            _statusText.Text =
                string.Format(
                    CultureInfo.InvariantCulture,
                    "OPENSTREETMAP  Z{0}",
                    _zoom);
        }

        private async Task LoadTileIntoImageAsync(
            Image image,
            int zoom,
            int x,
            int y,
            long version)
        {
            try
            {
                byte[] bytes =
                    await GetTileBytesAsync(
                        zoom,
                        x,
                        y);

                if (bytes == null ||
                    bytes.Length == 0 ||
                    version !=
                    _renderVersion)
                {
                    return;
                }

                BitmapImage bitmap =
                    CreateBitmap(
                        bytes);

                if (version ==
                    _renderVersion)
                {
                    image.Source =
                        bitmap;

                    if (_isTileFallbackLogged)
                    {
                        _isTileFallbackLogged =
                            false;

                        Log.Information(
                            "[MAP] OpenStreetMap Tile Load Recovered");
                    }
                }
            }
            catch (Exception ex)
            {
                // Tile 하나마다 로그가 쌓이지 않도록 Offline/Fallback 진입 시 1회만 기록.
                if (!_isTileFallbackLogged)
                {
                    _isTileFallbackLogged =
                        true;

                    Log.Warning(
                        ex,
                        "[MAP] OpenStreetMap Tile Load Failed / Static Map Fallback");
                }

                // The fallback tactical map remains visible behind the tile layer.
            }
        }

        private async Task<byte[]> GetTileBytesAsync(
            int zoom,
            int x,
            int y)
        {
            string cachePath =
                System.IO.Path.Combine(
                    _tileCacheRoot,
                    zoom.ToString(
                        CultureInfo.InvariantCulture),
                    x.ToString(
                        CultureInfo.InvariantCulture),
                    y.ToString(
                        CultureInfo.InvariantCulture) +
                    ".png");

            if (File.Exists(
                    cachePath))
            {
                try
                {
                    return File.ReadAllBytes(
                        cachePath);
                }
                catch
                {
                    // Cache miss/failure: try network below.
                }
            }

            string tileUrl =
                string.Format(
                    CultureInfo.InvariantCulture,
                    "https://tile.openstreetmap.org/{0}/{1}/{2}.png",
                    zoom,
                    x,
                    y);

            byte[] bytes =
                await HttpClient.GetByteArrayAsync(
                    tileUrl);

            try
            {
                string directory =
                    System.IO.Path.GetDirectoryName(
                        cachePath);

                if (!Directory.Exists(
                        directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                File.WriteAllBytes(
                    cachePath,
                    bytes);
            }
            catch
            {
                // Cache write failure must not prevent map display.
            }

            return bytes;
        }

        private static BitmapImage CreateBitmap(
            byte[] bytes)
        {
            BitmapImage bitmap =
                new BitmapImage();

            using (MemoryStream stream =
                new MemoryStream(
                    bytes,
                    false))
            {
                bitmap.BeginInit();
                bitmap.CacheOption =
                    BitmapCacheOption.OnLoad;
                bitmap.StreamSource =
                    stream;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            return bitmap;
        }

        private static void LatLonToWorldPixel(
            double latitude,
            double longitude,
            int zoom,
            out double worldX,
            out double worldY)
        {
            double safeLatitude =
                ClampLatitude(
                    latitude);

            double normalizedLongitude =
                NormalizeLongitude(
                    longitude);

            double scale =
                TileSize *
                (1 << zoom);

            worldX =
                (normalizedLongitude + 180.0) /
                360.0 *
                scale;

            double sinLatitude =
                Math.Sin(
                    safeLatitude *
                    Math.PI /
                    180.0);

            double normalizedY =
                0.5 -
                Math.Log(
                    (1.0 + sinLatitude) /
                    (1.0 - sinLatitude)) /
                (4.0 * Math.PI);

            worldY =
                normalizedY *
                scale;
        }

        private static void WorldPixelToLatLon(
            double worldX,
            double worldY,
            int zoom,
            out double latitude,
            out double longitude)
        {
            double scale =
                TileSize *
                (1 << zoom);

            longitude =
                worldX /
                scale *
                360.0 -
                180.0;

            double normalizedY =
                0.5 -
                worldY /
                scale;

            latitude =
                90.0 -
                360.0 *
                Math.Atan(
                    Math.Exp(
                        -normalizedY *
                        2.0 *
                        Math.PI)) /
                Math.PI;

            latitude =
                ClampLatitude(
                    latitude);

            longitude =
                NormalizeLongitude(
                    longitude);
        }

        private static double ClampLatitude(
            double latitude)
        {
            const double maximumMercatorLatitude =
                85.05112878;

            return Math.Max(
                -maximumMercatorLatitude,
                Math.Min(
                    maximumMercatorLatitude,
                    latitude));
        }

        private static double NormalizeLongitude(
            double longitude)
        {
            double normalized =
                longitude;

            while (normalized < -180.0)
            {
                normalized += 360.0;
            }

            while (normalized >= 180.0)
            {
                normalized -= 360.0;
            }

            return normalized;
        }

        private static int Mod(
            int value,
            int modulus)
        {
            int result =
                value %
                modulus;

            return result < 0
                ? result + modulus
                : result;
        }
    }
}
