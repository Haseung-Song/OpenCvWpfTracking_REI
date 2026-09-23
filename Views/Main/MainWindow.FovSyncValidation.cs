using OpenCvWpfTracking.Services.Control;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IOPath = System.IO.Path;

namespace OpenCvWpfTracking
{
    public partial class MainWindow
    {
        private readonly FovSyncValidationService _fovSyncValidationService =
            new FovSyncValidationService();

        private readonly List<double> _eoFovBoundaryX = new List<double>();
        private readonly List<double> _irFovBoundaryX = new List<double>();
        private bool _isFovValidationActive;

        private int _fovValidationLevel;

        /// <summary>
        /// 2026-09-21: 현재 SYNC 선택 단계의 수동 화각 검증을 시작한다.
        /// </summary>
        private void StartFovValidation_Click(object sender, RoutedEventArgs e)
        {
            if (vm.SelectedZoomSyncLevel == null ||
                vm.SelectedZoomSyncLevel.Level < 1)
            {
                FovValidationStatusText.Text = "SELECT LEVEL 1 ~ 10 FIRST";
                return;
            }

            if (!(EoCameraImageView.Source is BitmapSource) ||
                !(IrCameraImageView.Source is BitmapSource))
            {
                FovValidationStatusText.Text = "EO / IR VIDEO NOT READY";
                return;
            }

            ResetFovValidationMeasurement();
            _fovValidationLevel = vm.SelectedZoomSyncLevel.Level;
            _isFovValidationActive = true;
            FovValidationStatusText.Text =
                $"LEVEL {_fovValidationLevel} / CLICK EO LEFT BOUNDARY";

            Log.Information(
                "[FOV VALIDATION] 측정 시작 | LEVEL={Level} | 순서=EO 좌/우, IR 좌/우",
                _fovValidationLevel);
        }

        private void ResetFovValidation_Click(object sender, RoutedEventArgs e)
        {
            ResetFovValidationMeasurement();
            FovValidationStatusText.Text = "READY / SELECT LEVEL AND START";
        }

        private void ResetFovValidationMeasurement()
        {
            _isFovValidationActive = false;
            _fovValidationLevel = 0;
            _eoFovBoundaryX.Clear();
            _irFovBoundaryX.Clear();
            EoFovValidationOverlay.Children.Clear();
            IrFovValidationOverlay.Children.Clear();
        }

        /// <summary>
        /// 영상 더블클릭 분리창과 충돌하지 않도록 검증 활성 상태의 단일 클릭만 처리한다.
        /// </summary>
        private bool TryHandleFovValidationClick(
            bool infrared,
            MouseButtonEventArgs e)
        {
            if (!_isFovValidationActive)
            {
                return false;
            }

            e.Handled = true;

            if (e.ClickCount != 1)
            {
                return true;
            }

            Image image = infrared ? IrCameraImageView : EoCameraImageView;
            Canvas overlay = infrared ? IrFovValidationOverlay : EoFovValidationOverlay;
            List<double> boundaries = infrared ? _irFovBoundaryX : _eoFovBoundaryX;

            if ((!infrared && _eoFovBoundaryX.Count >= 2) ||
                (infrared && _eoFovBoundaryX.Count < 2))
            {
                FovValidationStatusText.Text = _eoFovBoundaryX.Count < 2
                    ? "CLICK EO LEFT / RIGHT FIRST"
                    : "CLICK IR LEFT / RIGHT";
                return true;
            }

            Point viewPoint = e.GetPosition(image);
            Point sourcePoint;

            if (!TryGetFovSourcePixel(image, viewPoint, out sourcePoint))
            {
                FovValidationStatusText.Text = "CLICK INSIDE THE ACTUAL VIDEO IMAGE";
                return true;
            }

            boundaries.Add(sourcePoint.X);
            AddFovBoundaryLine(overlay, viewPoint.X, boundaries.Count);

            if (_eoFovBoundaryX.Count < 2)
            {
                FovValidationStatusText.Text = "CLICK EO RIGHT BOUNDARY";
            }
            else if (_irFovBoundaryX.Count == 0)
            {
                FovValidationStatusText.Text = "CLICK IR LEFT BOUNDARY";
            }
            else if (_irFovBoundaryX.Count == 1)
            {
                FovValidationStatusText.Text = "CLICK IR RIGHT BOUNDARY";
            }
            else
            {
                CompleteFovValidation();
            }

            return true;
        }

        private void CompleteFovValidation()
        {
            try
            {
                BitmapSource eoSource = EoCameraImageView.Source as BitmapSource;
                BitmapSource irSource = IrCameraImageView.Source as BitmapSource;

                if (eoSource == null || irSource == null)
                {
                    throw new InvalidOperationException("EO/IR frame is unavailable.");
                }

                FovSyncValidationResult result = _fovSyncValidationService.Calculate(
                    _fovValidationLevel,
                    _eoFovBoundaryX[0],
                    _eoFovBoundaryX[1],
                    eoSource.PixelWidth,
                    _irFovBoundaryX[0],
                    _irFovBoundaryX[1],
                    irSource.PixelWidth);

                _isFovValidationActive = false;
                FovValidationStatusText.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "LEVEL {0} / FOV ERROR {1:F2}% / {2}",
                    result.Level,
                    result.ErrorPercent,
                    result.Passed ? "PASS" : "FAIL");

                SaveFovValidationCsv(result, eoSource.PixelWidth, irSource.PixelWidth);

                Log.Information(
                    "[FOV VALIDATION] 측정 완료 | LEVEL={Level} | EO_NORMALIZED={EoNormalized:F6} | IR_NORMALIZED={IrNormalized:F6} | ERROR={Error:F2}% | RESULT={Result}",
                    result.Level,
                    result.EoNormalizedWidth,
                    result.IrNormalizedWidth,
                    result.ErrorPercent,
                    result.Passed ? "PASS" : "FAIL");
            }
            catch (Exception ex)
            {
                _isFovValidationActive = false;
                FovValidationStatusText.Text = "MEASUREMENT ERROR / " + ex.Message;
                Log.Error(ex, "[FOV VALIDATION] 측정 실패 | 조치=RESET 후 동일 기준물 좌/우를 다시 선택");
            }
        }

        private void SaveFovValidationCsv(
            FovSyncValidationResult result,
            int eoFrameWidth,
            int irFrameWidth)
        {
            string logDirectory = IOPath.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Logs");
            Directory.CreateDirectory(logDirectory);

            string path = IOPath.Combine(
                logDirectory,
                "fov-sync-validation-" + DateTime.Now.ToString("yyyyMMdd") + ".csv");

            if (!File.Exists(path))
            {
                File.AppendAllText(
                    path,
                    "Timestamp,Level,EoZoom,IrZoom,EoFrameWidth,IrFrameWidth,EoTargetPixelWidth,IrTargetPixelWidth,EoNormalizedWidth,IrNormalizedWidth,FovErrorPercent,Result" + Environment.NewLine,
                    new UTF8Encoding(true));
            }

            string row = string.Join(",",
                DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                result.Level.ToString(CultureInfo.InvariantCulture),
                vm.CurrentEoZoomText,
                vm.CurrentIrZoomText,
                eoFrameWidth.ToString(CultureInfo.InvariantCulture),
                irFrameWidth.ToString(CultureInfo.InvariantCulture),
                result.EoPixelWidth.ToString("F2", CultureInfo.InvariantCulture),
                result.IrPixelWidth.ToString("F2", CultureInfo.InvariantCulture),
                result.EoNormalizedWidth.ToString("F6", CultureInfo.InvariantCulture),
                result.IrNormalizedWidth.ToString("F6", CultureInfo.InvariantCulture),
                result.ErrorPercent.ToString("F2", CultureInfo.InvariantCulture),
                result.Passed ? "PASS" : "FAIL");

            File.AppendAllText(path, row + Environment.NewLine, new UTF8Encoding(true));
        }

        private static bool TryGetFovSourcePixel(
            Image image,
            Point viewPoint,
            out Point sourcePoint)
        {
            sourcePoint = new Point();
            BitmapSource source = image.Source as BitmapSource;

            if (source == null || source.PixelWidth <= 0 || source.PixelHeight <= 0 ||
                image.ActualWidth <= 0 || image.ActualHeight <= 0)
            {
                return false;
            }

            double scale = Math.Min(
                image.ActualWidth / source.PixelWidth,
                image.ActualHeight / source.PixelHeight);
            double renderWidth = source.PixelWidth * scale;
            double renderHeight = source.PixelHeight * scale;
            double offsetX = (image.ActualWidth - renderWidth) / 2.0;
            double offsetY = (image.ActualHeight - renderHeight) / 2.0;

            if (viewPoint.X < offsetX || viewPoint.X > offsetX + renderWidth ||
                viewPoint.Y < offsetY || viewPoint.Y > offsetY + renderHeight)
            {
                return false;
            }

            sourcePoint = new Point(
                (viewPoint.X - offsetX) / scale,
                (viewPoint.Y - offsetY) / scale);
            return true;
        }

        private static void AddFovBoundaryLine(Canvas overlay, double x, int index)
        {
            Line line = new Line
            {
                X1 = x,
                X2 = x,
                Y1 = 0,
                Y2 = Math.Max(1.0, overlay.ActualHeight),
                Stroke = index == 1 ? Brushes.Lime : Brushes.Yellow,
                StrokeThickness = 3.0
            };
            overlay.Children.Add(line);
        }
    }
}
