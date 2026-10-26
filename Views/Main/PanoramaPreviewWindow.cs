using OpenCvWpfTracking.Services.Video;
using OpenCvWpfTracking.Views.Video;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// 현재 파노라마를 별도 창에서 크게 표시한다.
    /// Wheel로 확대하고 우클릭으로 배율을 초기화한다.
    /// 더블 클릭은 최대화 전환, ESC는 닫기다.
    /// </summary>
    public sealed class PanoramaPreviewWindow : Window
    {
        private readonly ScaleTransform _zoomTransform = new ScaleTransform(1.0, 1.0);

        private readonly TranslateTransform _panTransform = new TranslateTransform();

        private readonly Image _previewImage;

        private readonly Grid _root;

        private readonly PanoramaPresetOverlay _presetOverlay = new PanoramaPresetOverlay();

        private Action<int> _movePreset;

        /// <summary>
        /// 확대 영상과 같은 변환을 적용한 번호 레이어를 갱신한다.
        /// </summary>
        public void UpdatePresetLinks(IEnumerable<PanoramaPresetLink> links, int[] order, Action<int> move)
        {
            _movePreset = move;
            _presetOverlay.Update(_previewImage.Source as BitmapSource, links, order);
        }

        private Point _dragStartPoint;

        private double _dragStartX;

        private double _dragStartY;

        private bool _isDragging;

        /// <summary>
        /// PanoramaPreviewWindow 동작 수행 함수.
        /// </summary>
        public PanoramaPreviewWindow(ImageSource source, string filePath, string panoramaName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Title = "파노라마 확대 보기 / " + (panoramaName ?? string.Empty);
            Width = 1400;
            Height = 850;
            MinWidth = 640;
            MinHeight = 360;
            Background = Brushes.Black;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            TransformGroup imageTransform = new TransformGroup();
            imageTransform.Children.Add(_zoomTransform);
            imageTransform.Children.Add(_panTransform);
            _previewImage = new Image
            {
                Source = CreatePreviewSource(source, filePath),
                Stretch = Stretch.Uniform,
                RenderTransform = imageTransform,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            _root = new Grid
            {
                Background = Brushes.Black,
                ClipToBounds = true
            };
            _root.Children.Add(_previewImage);
            _presetOverlay.RenderTransform = imageTransform;
            _presetOverlay.RenderTransformOrigin = new Point(0.5, 0.5);
            _root.Children.Add(_presetOverlay);
            Content = _root;
            PreviewMouseWheel += OnPreviewMouseWheel;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            PreviewMouseMove += OnPreviewMouseMove;
            LostMouseCapture += OnLostMouseCapture;
            MouseRightButtonDown += OnMouseRightButtonDown;
            MouseDoubleClick += OnMouseDoubleClick;
            PreviewKeyDown += OnPreviewKeyDown;
            SourceInitialized += OnSourceInitialized;
            SizeChanged += OnSizeChanged;
        }

        /// <summary>
        /// CreatePreviewSource 생성 및 변환 함수.
        /// </summary>
        private static ImageSource CreatePreviewSource(ImageSource source, string filePath)
        {
            const int MaximumPreviewPixelWidth = 8192;
            var bitmap = source as BitmapSource;

            // 파일이 외부에서 바뀌어도 메인 화면에서 검증한 동일 이미지로 번호를 표시한다.
            if (bitmap == null || bitmap.PixelWidth <= MaximumPreviewPixelWidth)
                return source;

            double scale = MaximumPreviewPixelWidth / (double)bitmap.PixelWidth;
            var reduced = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
            reduced.Freeze();
            return reduced;
        }

        /// <summary>
        /// OnSourceInitialized 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnSourceInitialized(object sender, EventArgs e)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource?.CompositionTarget != null)
            {
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }

        /// <summary>
        /// OnPreviewMouseWheel 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double previousScale = _zoomTransform.ScaleX;
            double nextScale = e.Delta > 0 ? previousScale * 1.15 : previousScale / 1.15;
            nextScale = Math.Max(1.0, Math.Min(8.0, nextScale));
            Point mouse = e.GetPosition(_root);
            double scaleRatio = nextScale / previousScale;
            _panTransform.X -= (scaleRatio - 1.0) * (mouse.X - (_root.ActualWidth / 2.0) - _panTransform.X);
            _panTransform.Y -= (scaleRatio - 1.0) * (mouse.Y - (_root.ActualHeight / 2.0) - _panTransform.Y);
            _zoomTransform.ScaleX = nextScale;
            _zoomTransform.ScaleY = nextScale;
            ClampPanTranslation();
            e.Handled = true;
        }

        /// <summary>
        /// OnPreviewMouseLeftButtonDown 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int? number = _presetOverlay.HitNumber(e.GetPosition(_presetOverlay));

            if (number.HasValue)
            {
                e.Handled = true;

                if (e.ClickCount == 1)
                    _movePreset?.Invoke(number.Value);

                return;
            }

            if (e.ClickCount != 1 || _zoomTransform.ScaleX <= 1.0)
            {
                return;
            }

            _dragStartPoint = e.GetPosition(_root);
            _dragStartX = _panTransform.X;
            _dragStartY = _panTransform.Y;
            _isDragging = true;
            Cursor = Cursors.SizeAll;
            Mouse.Capture(this);
            e.Handled = true;
        }

        /// <summary>
        /// OnPreviewMouseMove 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point current = e.GetPosition(_root);
            _panTransform.X = _dragStartX + current.X - _dragStartPoint.X;
            _panTransform.Y = _dragStartY + current.Y - _dragStartPoint.Y;
            ClampPanTranslation();
            e.Handled = true;
        }

        /// <summary>
        /// OnPreviewMouseLeftButtonUp 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndDrag();
        }

        /// <summary>
        /// OnLostMouseCapture 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// EndDrag 중지 함수.
        /// </summary>
        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            Cursor = Cursors.Arrow;

            if (Mouse.Captured == this)
            {
                Mouse.Capture(null);
            }
        }

        /// <summary>
        /// ClampPanTranslation 동작 수행 함수.
        /// </summary>
        private void ClampPanTranslation()
        {
            if (_zoomTransform.ScaleX <= 1.0 || _root.ActualWidth <= 0.0 || _root.ActualHeight <= 0.0)
            {
                _panTransform.X = 0.0;
                _panTransform.Y = 0.0;
                return;
            }

            BitmapSource source = _previewImage.Source as BitmapSource;
            double contentWidth = _root.ActualWidth;
            double contentHeight = _root.ActualHeight;

            if (source != null && source.PixelWidth > 0 && source.PixelHeight > 0)
            {
                double imageRatio = source.PixelWidth / (double)source.PixelHeight;
                double viewportRatio = _root.ActualWidth / _root.ActualHeight;

                if (imageRatio >= viewportRatio)
                {
                    contentHeight = _root.ActualWidth / imageRatio;
                }
                else
                {
                    contentWidth = _root.ActualHeight * imageRatio;
                }
            }

            double maximumX = Math.Max(0.0, contentWidth * (_zoomTransform.ScaleX - 1.0) / 2.0);
            double maximumY = Math.Max(0.0, contentHeight * (_zoomTransform.ScaleY - 1.0) / 2.0);
            _panTransform.X = Math.Max(-maximumX, Math.Min(maximumX, _panTransform.X));
            _panTransform.Y = Math.Max(-maximumY, Math.Min(maximumY, _panTransform.Y));
        }

        /// <summary>
        /// OnSizeChanged 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClampPanTranslation();
        }

        /// <summary>
        /// OnMouseRightButtonDown 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _zoomTransform.ScaleX = 1.0;
            _zoomTransform.ScaleY = 1.0;
            _panTransform.X = 0.0;
            _panTransform.Y = 0.0;
            e.Handled = true;
        }

        /// <summary>
        /// OnMouseDoubleClick 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }

        /// <summary>
        /// OnPreviewKeyDown 상태 및 이벤트 처리 함수.
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            Close();
            e.Handled = true;
        }
    }
}
