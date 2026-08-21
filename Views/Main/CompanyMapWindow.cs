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

            root.Children.Add(
                header);

            root.Children.Add(
                _mapControl);

            Content =
                root;
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
            Log.Information(
                "[MAP] Expanded Map Window Close");
        }
    }
}
