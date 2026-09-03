using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Serilog;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// Expanded OpenStreetMap window opened by double-clicking
    /// the company map panel on the main REI screen.
    /// </summary>
    public sealed class CompanyMapWindow : Window
    {
        private readonly OpenStreetMapControl _mapControl;
        private readonly Border _loadingOverlay;

        public CompanyMapWindow(
            double latitude,
            double longitude,
            int zoom)
        {
            Title =
                "GLOBAL SYSTEMS - OPENSTREETMAP";

            Width =
                1280;

            Height =
                820;

            MinWidth =
                720;

            MinHeight =
                480;

            WindowStartupLocation =
                WindowStartupLocation.CenterOwner;

            Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        28,
                        33,
                        39));

            Grid root =
                new Grid();

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        GridLength.Auto
                });

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            Border header =
                new Border
                {
                    Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                46,
                                56,
                                64)),
                    Padding =
                        new Thickness(
                            12,
                            8,
                            12,
                            8)
                };

            header.Child =
                new TextBlock
                {
                    Text =
                        "GLOBAL SYSTEMS  |  Mouse wheel: Zoom  |  Drag: Move  |  Double click: Company location",
                    Foreground =
                        Brushes.White,
                    FontWeight =
                        FontWeights.Bold
                };

            _mapControl =
                new OpenStreetMapControl();

            // 최초 타일 묶음이 준비되기 전 정적 Fallback 지도가 확대되어
            // 순간 노출되지 않도록 지도는 준비 완료 시점에 한 번에 표시한다.
            _mapControl.Opacity =
                0.0;

            _mapControl.IsHitTestVisible =
                false;

            _mapControl.InitialRenderCompleted +=
                MapControl_InitialRenderCompleted;

            _mapControl.SetView(
                latitude,
                longitude,
                zoom);

            _mapControl.MouseDoubleClick +=
                MapControl_MouseDoubleClick;

            Closed +=
                CompanyMapWindow_Closed;

            Grid.SetRow(
                header,
                0);

            Grid.SetRow(
                _mapControl,
                1);

            _loadingOverlay =
                new Border
                {
                    Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                28,
                                33,
                                39)),
                    Child =
                        new TextBlock
                        {
                            Text = "OPENSTREETMAP LOADING...",
                            Foreground = Brushes.White,
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                };

            Grid.SetRow(
                _loadingOverlay,
                1);

            root.Children.Add(
                header);

            root.Children.Add(
                _mapControl);

            root.Children.Add(
                _loadingOverlay);

            Content =
                root;
        }

        private void MapControl_InitialRenderCompleted(
            object sender,
            System.EventArgs e)
        {
            _mapControl.Opacity =
                1.0;

            _mapControl.IsHitTestVisible =
                true;

            _loadingOverlay.Visibility =
                Visibility.Collapsed;

            Log.Information(
                "[MAP] Expanded Map Initial View Displayed");
        }

        private void MapControl_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            _mapControl.SetView(
                OpenStreetMapControl.DefaultLatitude,
                OpenStreetMapControl.DefaultLongitude,
                System.Math.Max(
                    OpenStreetMapControl.DefaultZoom,
                    _mapControl.Zoom));

            Log.Information(
                "[MAP] Reset To GLOBAL SYSTEMS / CENTER=({Latitude:F6}, {Longitude:F6}) / ZOOM={Zoom}",
                OpenStreetMapControl.DefaultLatitude,
                OpenStreetMapControl.DefaultLongitude,
                _mapControl.Zoom);

            e.Handled =
                true;
        }

        private void CompanyMapWindow_Closed(
            object sender,
            System.EventArgs e)
        {
            _mapControl.InitialRenderCompleted -=
                MapControl_InitialRenderCompleted;

            Log.Information(
                "[MAP] Expanded Map Window Close");
        }

    }

}
