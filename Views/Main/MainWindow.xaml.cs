using OpenCvWpfTracking.Common;
using Microsoft.Win32;
using OpenCvWpfTracking.ViewModels.Main;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Serilog;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// [MainWindow.xaml]에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        #region [Fields]

        /// <summary>
        /// [Main] 화면 -> [ViewModel]
        ///
        /// XAML Binding 및 화면 입력 이벤트를
        /// MainViewModel로 전달한다.
        /// </summary>
        private readonly MainViewModel vm =
            new MainViewModel();

        /// <summary>
        /// EO 영상 분리 창.
        ///
        /// 동일 카메라 창이 중복으로 생성되지 않도록
        /// 현재 열린 창 참조를 보관한다.
        /// </summary>
        private VideoPopoutWindow _eoVideoPopoutWindow;

        /// <summary>
        /// IR 영상 분리 창.
        /// </summary>
        private VideoPopoutWindow _irVideoPopoutWindow;

        /// <summary>
        /// 현재 메인 화면의 큰 영상으로 선택된 카메라.
        /// </summary>
        private VideoPopoutCameraType _primaryVideoType =
            VideoPopoutCameraType.Eo;

        // TEST PROGRAM is a separate executable. Keep its process so it can
        // be stopped when the viewer closes.
        private Process _fireDetectorTestProgram;

        /// <summary>
        /// 2026-08-18: EO 360도 촬영 프레임 합성 서비스와 취소 상태.
        /// </summary>
        private readonly EoPanoramaStitchingService _eoPanoramaStitchingService =
            new EoPanoramaStitchingService();

        private CancellationTokenSource _panoramaCaptureCts;

        private string _currentPanoramaFilePath;

        private PanoramaPreviewWindow _panoramaPreviewWindow;

        /// <summary>
        /// GLOBAL SYSTEMS OpenStreetMap 확대 창.
        /// 중복 생성되지 않도록 현재 열린 Window 참조를 보관한다.
        /// </summary>
        private CompanyMapWindow _companyMapWindow;

        private bool _isWindowDragPending;
        private Point _windowDragStartPoint;

        // 2026-08-18: 분리 창을 열지 않아도 마우스가 올라간 EO/IR 화면을
        // 대상으로 W/S/A/D 줌·포커스 연속 제어를 수행한다.
        private Key? _activeHoverLensKey;
        private VideoPopoutCameraType? _activeHoverLensCameraType;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main] 화면 생성자
        ///
        /// ViewModel 생성 및 DataContext 연결
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            DataContext =
                vm;

            LoadLatestGeneratedPanoramaOrKeepDefault();
        }

        /// <summary>
        /// FireDetectorTestProgram_Click 이벤트 처리 함수.
        /// </summary>
        private void FireDetectorTestProgram_Click(
            object sender,
            RoutedEventArgs e)
        {
            string testProgramPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "FireCandidateValidator.exe");

            if (!File.Exists(testProgramPath))
            {
                MessageBox.Show(
                    "Fire detector test program was not found. Rebuild the solution and run it from the output folder.",
                    "TEST PROGRAM",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_fireDetectorTestProgram != null &&
                !_fireDetectorTestProgram.HasExited)
            {
                _fireDetectorTestProgram.Refresh();
                return;
            }

            try
            {
                _fireDetectorTestProgram = Process.Start(new ProcessStartInfo
                {
                    FileName = testProgramPath,
                    WorkingDirectory = Path.GetDirectoryName(testProgramPath),
                    UseShellExecute = false
                });

                _fireDetectorTestProgram.EnableRaisingEvents = true;
                _fireDetectorTestProgram.Exited +=
                    (s, args) => Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            _fireDetectorTestProgram = null;
                        }));
            }
            catch (Exception ex)
            {
                _fireDetectorTestProgram = null;
                MessageBox.Show(
                    "Could not start the fire detector test program.\n" + ex.Message,
                    "TEST PROGRAM",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// OnClosed 상태 및 이벤트 처리 함수.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            /*
             * 2026-08-26: Application.Current가 해제되기 전에 ViewModel의
             * AI 수신, 이벤트 Timer와 영상 연결을 먼저 종료한다.
             */
            try
            {
                _panoramaCaptureCts?.Cancel();
                vm.ShutdownForApplicationExit();

                if (_fireDetectorTestProgram != null &&
                    !_fireDetectorTestProgram.HasExited)
                {
                    _fireDetectorTestProgram.Kill();
                }

            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "APPLICATION / SHUTDOWN",
                    "Main window cleanup failed",
                    ex);
            }

            base.OnClosed(e);
        }

        #endregion

        #region [Company OpenStreetMap Events]

        /// <summary>
        /// 메인 화면 GLOBAL SYSTEMS 지도 더블클릭 처리.
        ///
        /// 메인 화면의 현재 지도 중심/Zoom을 그대로 넘겨
        /// 별도 확대 Window에서 OpenStreetMap을 계속 탐색할 수 있게 한다.
        /// </summary>
        private void CompanyMap_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            OpenStreetMapControl mapControl =
                sender as OpenStreetMapControl;

            if (mapControl == null)
            {
                return;
            }

            if (_companyMapWindow != null)
            {
                if (_companyMapWindow.WindowState ==
                    WindowState.Minimized)
                {
                    _companyMapWindow.WindowState =
                        WindowState.Normal;
                }

                _companyMapWindow.Activate();

                Log.Information(
                    "[MAP] Expanded Map Window Activate");

                e.Handled =
                    true;

                return;
            }

            _companyMapWindow =
                new CompanyMapWindow(
                    mapControl.CenterLatitude,
                    mapControl.CenterLongitude,
                    Math.Max(
                        mapControl.Zoom,
                        16));

            _companyMapWindow.Owner =
                this;

            _companyMapWindow.Closed +=
                (closedSender, closedArgs) =>
                {
                    _companyMapWindow =
                        null;
                };

            _companyMapWindow.Show();

            Log.Information(
                "[MAP] Expanded Map Window Open / CENTER=({Latitude:F6}, {Longitude:F6}) / ZOOM={Zoom}",
                mapControl.CenterLatitude,
                mapControl.CenterLongitude,
                Math.Max(
                    mapControl.Zoom,
                    16));

            e.Handled =
                true;
        }

        #endregion

        #region [Video Popout Window Events]

        /// <summary>
        /// EO 영상 영역 더블클릭 처리.
        ///
        /// 기존 RTSP 연결을 새로 생성하지 않고,
        /// MainViewModel의 EOCameraImage Binding을 공유하는
        /// EO 전용 분리 창을 연다.
        /// </summary>
        private void EoVideoBorder_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
            {
                return;
            }

            ShowVideoPopoutWindow(
                VideoPopoutCameraType.Eo);

            e.Handled =
                true;
        }

        /// <summary>
        /// IR 영상 영역 더블클릭 처리.
        /// </summary>
        private void IrVideoBorder_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
            {
                return;
            }

            ShowVideoPopoutWindow(
                VideoPopoutCameraType.Ir);

            e.Handled =
                true;
        }

        /// <summary>
        /// EO 또는 IR 영상 분리 창을 표시한다.
        ///
        /// 이미 열려 있는 경우 새 창을 만들지 않고
        /// 기존 창을 복원한 뒤 앞으로 가져온다.
        /// </summary>
        private void ShowVideoPopoutWindow(
            VideoPopoutCameraType cameraType)
        {
            VideoPopoutWindow currentWindow =
                cameraType == VideoPopoutCameraType.Eo
                    ? _eoVideoPopoutWindow
                    : _irVideoPopoutWindow;

            if (currentWindow != null)
            {
                if (currentWindow.WindowState ==
                    WindowState.Minimized)
                {
                    currentWindow.WindowState =
                        WindowState.Normal;
                }

                currentWindow.Activate();
                currentWindow.Focus();

                return;
            }

            VideoPopoutWindow popoutWindow =
                new VideoPopoutWindow(
                    vm,
                    cameraType,
                    GetViewportAspectRatio(
                        EoCameraImageView,
                        940.0 / 650.0),
                    GetViewportAspectRatio(
                        IrCameraImageView,
                        440.0 / 365.0),
                    EoRenderedVideoSurface,
                    IrRenderedVideoSurface)
                {
                    Owner = this
                };

            popoutWindow.Closed +=
                (sender, args) =>
                {
                    if (cameraType ==
                        VideoPopoutCameraType.Eo)
                    {
                        _eoVideoPopoutWindow =
                            null;
                    }
                    else
                    {
                        _irVideoPopoutWindow =
                            null;
                    }

                };

            if (cameraType ==
                VideoPopoutCameraType.Eo)
            {
                _eoVideoPopoutWindow =
                    popoutWindow;
            }
            else
            {
                _irVideoPopoutWindow =
                    popoutWindow;
            }

            popoutWindow.Show();
            popoutWindow.Activate();
        }

        /// <summary>
        /// 2026-08-25: 현재 프레임의 실제 픽셀 종횡비를 분리 창에 전달한다.
        /// 프레임 정보를 아직 받지 못한 경우에만 화면 영역 또는 기본값을 사용한다.
        /// </summary>
        private static double GetViewportAspectRatio(
            FrameworkElement viewport,
            double fallbackAspectRatio)
        {
            Image image =
                viewport as Image;

            BitmapSource frame =
                image?.Source as BitmapSource;

            if (frame != null &&
                frame.PixelWidth > 0 &&
                frame.PixelHeight > 0)
            {
                return (double)frame.PixelWidth /
                       frame.PixelHeight;
            }

            if (viewport == null ||
                viewport.ActualWidth <= 0.0 ||
                viewport.ActualHeight <= 0.0)
            {
                return fallbackAspectRatio;
            }

            return viewport.ActualWidth /
                   viewport.ActualHeight;
        }

        /// <summary>
        /// EO 또는 IR 영상을 메인 화면의 큰 영상으로 선택한다.
        ///
        /// 영상 Decoder, RTSP 연결, BitmapSource는 변경하지 않는다.
        ///
        /// EO / IR 영상 Container의 위치, 높이와 정렬을 함께 교환하여
        /// 선택한 영상이 큰 주화면 영역 전체에 배치되도록 한다.
        ///
        /// R:
        /// EO  -> Grid.Column 0
        /// IR  -> Grid.Column 1
        ///
        /// T:
        /// IR  -> Grid.Column 0
        /// EO  -> Grid.Column 1
        /// </summary>
        internal void SelectPrimaryVideo(
            VideoPopoutCameraType cameraType)
        {
            if (_primaryVideoType ==
                cameraType)
            {
                return;
            }

            _primaryVideoType =
                cameraType;

            bool isEoPrimary =
                cameraType ==
                VideoPopoutCameraType.Eo;

            ApplyPrimaryVideoLayout(
                EoVideoBorder,
                isEoPrimary);

            ApplyPrimaryVideoLayout(
                IrVideoContainer,
                !isEoPrimary);

            Console.WriteLine();

            Console.WriteLine(
                "[VIDEO SWITCH] PRIMARY VIDEO : " +
                (isEoPrimary
                    ? "EO"
                    : "IR"));

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// EO / IR 영상 Container를 주화면 또는 보조화면 규격으로 배치한다.
        ///
        /// 주화면:
        /// - Grid.Column 0
        /// - 영상 행의 전체 높이 사용
        /// - 중앙 십자선과 AI Overlay가 주화면 중앙에 배치됨
        ///
        /// 보조화면:
        /// - Grid.Column 1
        /// - 기존 IR 보조화면 높이 365 유지
        /// - 상단 정렬
        ///
        /// R / T 전환 시 위치만 바꾸면 IR의 고정 높이가 주화면에도 남아
        /// 영상 아래에 빈 공간이 생기고 십자선 기준이 어긋나므로,
        /// 높이와 정렬까지 동일한 시점에 함께 갱신한다.
        /// </summary>
        private static void ApplyPrimaryVideoLayout(
            FrameworkElement videoContainer,
            bool isPrimary)
        {
            Grid.SetColumn(
                videoContainer,
                isPrimary
                    ? 0
                    : 1);

            // 2026-08-26: R/T 왕복 시 행의 Stretch 값이 남아 파노라마/지도와 겹치지 않도록
            // 각 슬롯의 검증된 높이를 매 전환마다 명시적으로 복원한다.
            videoContainer.Height =
                isPrimary
                    ? 522
                    : 345;

            videoContainer.HorizontalAlignment =
                HorizontalAlignment.Stretch;

            videoContainer.VerticalAlignment = VerticalAlignment.Top;

            videoContainer.Margin =
                isPrimary
                    ? new Thickness(
                        5,
                        5,
                        10,
                        0)
                    : new Thickness(
                        0,
                        5,
                        10,
                        0);
        }

        #endregion

        #region [Window Title Bar Events]

        /// <summary>
        /// WindowFrame_PreviewMouseLeftButtonDown 동작 수행 함수.
        /// </summary>
        private void WindowFrame_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount != 1 ||
                IsInteractiveWindowElement(e.OriginalSource as DependencyObject))
            {
                _isWindowDragPending = false;
                return;
            }

            _windowDragStartPoint = e.GetPosition(this);
            _isWindowDragPending = true;
        }

        /// <summary>
        /// WindowFrame_PreviewMouseMove 동작 수행 함수.
        /// </summary>
        private void WindowFrame_PreviewMouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isWindowDragPending ||
                e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point current = e.GetPosition(this);

            if (Math.Abs(current.X - _windowDragStartPoint.X) <
                    SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - _windowDragStartPoint.Y) <
                    SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            _isWindowDragPending = false;

            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // Mouse button state can change between the preview event and DragMove.
            }

        }

        /// <summary>
        /// WindowFrame_PreviewMouseLeftButtonUp 동작 수행 함수.
        /// </summary>
        private void WindowFrame_PreviewMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            _isWindowDragPending = false;
        }

        /// <summary>
        /// IsInteractiveWindowElement 상태 확인 함수.
        /// </summary>
        private static bool IsInteractiveWindowElement(
            DependencyObject source)
        {
            DependencyObject current = source;

            while (current != null)
            {
                if (current is ButtonBase ||
                    current is TextBoxBase ||
                    current is Selector ||
                    current is RangeBase ||
                    current is ScrollViewer ||
                    current is PasswordBox)
                {
                    return true;
                }

                current = current is Visual
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        /// <summary>
        /// Window_StateChanged 동작 수행 함수.
        /// </summary>
        private void Window_StateChanged(
            object sender,
            EventArgs e)
        {
            UpdateWindowChromeState();
        }

        /// <summary>
        /// UpdateWindowChromeState 갱신 함수.
        /// </summary>
        private void UpdateWindowChromeState()
        {
            bool isMaximized =
                WindowState == WindowState.Maximized;

            MaximizeWindowGlyph.Visibility =
                isMaximized ? Visibility.Collapsed : Visibility.Visible;
            RestoreWindowGlyph.Visibility =
                isMaximized ? Visibility.Visible : Visibility.Collapsed;
            MaximizeRestoreWindowButton.ToolTip =
                isMaximized ? "이전 크기로 복원" : "최대화";
            WindowFrameBorder.CornerRadius =
                isMaximized ? new CornerRadius(0) : new CornerRadius(8);
        }

        /// <summary>
        /// 사용자 정의 Title Bar 마우스 입력 처리.
        ///
        /// 한 번 누른 상태로 이동하면 창을 이동하고,
        /// 두 번 누르면 최대화 / 이전 크기를 전환한다.
        /// </summary>
        private void WindowTitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.Source is Button)
            {
                return;
            }

            if (e.ClickCount ==
                2)
            {
                ToggleWindowState();
                return;
            }

            if (e.LeftButton ==
                MouseButtonState.Pressed)
            {
                DragMove();
            }

        }

        /// <summary>
        /// 프로그램 창 최소화.
        /// </summary>
        private void MinimizeWindowButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState =
                WindowState.Minimized;
        }

        /// <summary>
        /// 2026-08-24: 상위 이벤트 알림 탭 선택 시 최신 AI/FIRE 이벤트 하위 탭을 즉시 연다.
        /// 내부 TabControl의 SelectionChanged 버블링은 처리하지 않는다.
        /// </summary>
        private void RightPanelTabControl_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            TabControl tabControl =
                sender as TabControl;

            // 2026-08-24: OriginalSource는 클릭한 TabItem이 될 수 있으므로
            // 실제 SelectionChanged 발생원(Source)으로 상위 TabControl을 판별한다.
            if (!ReferenceEquals(e.Source, sender) ||
                tabControl == null ||
                tabControl.SelectedIndex != 2)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(() => EventAlertPanel?.SelectLatestEventTab()),
                DispatcherPriority.DataBind);
        }

        /// <summary>
        /// 2026-08-24: 이미 선택된 이벤트 알림 상위 탭을 다시 눌러도 최신 이벤트 종류를 연다.
        /// </summary>
        private void EventAlertTabItem_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(() => EventAlertPanel?.SelectLatestEventTab()),
                DispatcherPriority.DataBind);
        }

        /// <summary>
        /// 프로그램 창 최대화 / 이전 크기 전환.
        /// </summary>
        private void MaximizeRestoreWindowButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        /// <summary>
        /// 프로그램 종료 확인.
        ///
        /// 사용자가 확인을 선택한 경우에만 MainWindow를 종료한다.
        /// </summary>
        private void CloseWindowButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    this,
                    "프로그램을 종료하시겠습니까?",
                    "프로그램 종료",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

            if (result ==
                MessageBoxResult.OK)
            {
                Close();
            }

        }

        /// <summary>
        /// 현재 창 상태에 따라 최대화와 이전 크기를 전환한다.
        /// </summary>
        private void ToggleWindowState()
        {
            WindowState =
                WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
        }

        #endregion

        #region [Window Keyboard Events]

        /// <summary>
        /// [MainWindow] Loaded 처리
        ///
        /// 방향키 입력을 Window에서 받을 수 있도록
        /// 초기 Keyboard Focus를 MainWindow에 설정한다.
        /// </summary>
        private void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateWindowChromeState();

            Keyboard.Focus(
                this);
        }

        /// <summary>
        /// 2026-08-18: ROOFTOP EO 카메라를 360도 자동 회전시키고,
        /// 정지 위치별 프레임을 OpenCV Panorama Stitcher로 합성한다.
        /// 촬영 중 다시 누르면 안전 취소를 요청한다.
        /// </summary>
        private async void NewPanoramaButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_panoramaCaptureCts != null)
            {
                ConsoleLogHelper.Warning(
                    "EO PANORAMA / UI",
                    "Operator requested capture cancellation");
                _panoramaCaptureCts.Cancel();
                vm.SetPanoramaCancellationState(
                    true,
                    false);
                PanoramaFileNameText.Text =
                    "360° PANORAMA / 촬영 취소 중...";
                PanoramaLoadingText.Text =
                    "촬영 취소 중...";

                return;
            }

            string blockReason =
                vm.GetPanoramaCaptureBlockReason();

            if (blockReason != null)
            {
                ConsoleLogHelper.Warning(
                    "EO PANORAMA / UI",
                    "Start rejected / REASON=" + blockReason);
                MessageBox.Show(
                    this,
                    blockReason,
                    "360° 파노라마 촬영",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBoxResult confirmation =
                MessageBox.Show(
                    this,
                    "EO 카메라가 자동으로 360° 회전합니다.\n\n" +
                    "- EO Zoom: 광각 0~100 / 1000 권장\n" +
                    "- 촬영 중 다른 Pan/Tilt/Zoom 조작 금지\n" +
                    "- 10° 간격, 총 36개 위치 촬영 후 자동 합성\n\n" +
                    "촬영을 시작하시겠습니까?",
                    "360° 파노라마 촬영",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                ConsoleLogHelper.Info(
                    "EO PANORAMA / UI",
                    "Operator declined capture confirmation");
                return;
            }

            ConsoleLogHelper.Info(
                "EO PANORAMA / UI",
                "Operator confirmed 360-degree panorama capture");

            ClosePanoramaPreview();

            _panoramaCaptureCts =
                new CancellationTokenSource();
            vm.SetPanoramaCancellationState(
                false,
                false);
            vm.SetPanoramaCompletionState(
                false);

            // 2026-08-18: 촬영부터 정합/블렌딩/저장 완료까지 제어 잠금 유지.
            vm.SetPanoramaProcessingRunning(
                true);

            NewPanoramaButton.Content =
                "촬영 중지";

            LoadPanoramaImageButton.IsEnabled =
                false;
            PanoramaLoadingText.Text = "촬영 중...";
            PanoramaLoadingOverlay.Visibility = Visibility.Visible;

            Progress<string> progress =
                new Progress<string>(message =>
                {
                    PanoramaFileNameText.Text = message;

                    if (message.Contains("시작 위치 복귀"))
                    {
                        PanoramaLoadingText.Text =
                            _panoramaCaptureCts != null &&
                            _panoramaCaptureCts.IsCancellationRequested
                                ? "촬영 취소 중... / 시작 위치 복귀 중..."
                                : "시작 위치 복귀 중...";
                    }

                });

            try
            {
                IList<IList<BitmapSource>> frameRows =
                    await vm.CaptureEoPanoramaFramesAsync(
                        progress,
                        _panoramaCaptureCts.Token);

                ConsoleLogHelper.State(
                    "EO PANORAMA / UI",
                    "Capture phase completed / ROWS=" + frameRows.Count +
                    " / FRAMES=" + frameRows.Sum(row => row.Count));

                PanoramaFileNameText.Text =
                    "360° PANORAMA / 특징점 정합 및 블렌딩 중...";
                PanoramaLoadingText.Text =
                    "특징점 정합 및 블렌딩 중...";

                NewPanoramaButton.Content =
                    "합성 중...";

                NewPanoramaButton.IsEnabled =
                    false;

                string outputPath =
                    GetNextPanoramaOutputPath();

                ConsoleLogHelper.Info(
                    "EO PANORAMA / UI",
                    "Stitch phase dispatched / OUTPUT=" + outputPath);

                BitmapSource panorama =
                    await Task.Run(() =>
                        _eoPanoramaStitchingService.StitchRowsAndSave(
                            frameRows,
                            outputPath));

                PanoramaImage.Source =
                    panorama;

                _currentPanoramaFilePath =
                    outputPath;

                PanoramaEmptyText.Visibility =
                    Visibility.Collapsed;

                PanoramaFileNameText.Text =
                    "ROOFTOP PANORAMA / " +
                    Path.GetFileName(outputPath);

                ConsoleLogHelper.State(
                    "EO PANORAMA / UI",
                    "Panorama completed and displayed / OUTPUT=" + outputPath +
                    " / SIZE=" + panorama.PixelWidth + "x" + panorama.PixelHeight);

                // 2026-08-21: 완료 알림 확인 전까지 간결한 완료 상태를 함께 표시한다.
                PanoramaLoadingText.Text =
                    "파노라마 생성 완료";

                // 2026-08-24: 정상 완료 알림 전 우측 상단 작업 상태도 완료 문구로 갱신한다.
                vm.SetPanoramaCompletionState(
                    true);
                await Dispatcher.Yield(
                    DispatcherPriority.Render);

                ConsoleLogHelper.State(
                    "EO PANORAMA / UI",
                    "Completion status displayed before confirmation dialog");

                MessageBox.Show(
                    this,
                    "360° EO 파노라마 생성이 완료되었습니다.\n\n" +
                    outputPath,
                    "파노라마 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                PanoramaLoadingOverlay.Visibility =
                    Visibility.Collapsed;
            }
            catch (OperationCanceledException)
            {
                vm.SetPanoramaCancellationState(
                    true,
                    true);
                ConsoleLogHelper.Warning(
                    "EO PANORAMA / UI",
                    "Panorama operation canceled");
                PanoramaFileNameText.Text =
                    "ROOFTOP PANORAMA / 촬영 취소 완료";
                PanoramaLoadingText.Text =
                    "촬영 취소 완료 / 시작 위치 복귀 완료";

                ConsoleLogHelper.State(
                    "EO PANORAMA / UI",
                    "Capture cancellation completed and start position restored");

                MessageBox.Show(
                    this,
                    "파노라마 촬영을 취소했습니다.\n시작 위치로 복귀했습니다.",
                    "촬영 취소",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "EO PANORAMA / UI",
                    "Panorama operation failed",
                    ex);
                PanoramaFileNameText.Text =
                    "ROOFTOP PANORAMA / 생성 실패";

                MessageBox.Show(
                    this,
                    "360° 파노라마를 생성할 수 없습니다.\n\n" +
                    ex.Message,
                    "파노라마 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                vm.SetPanoramaCancellationState(
                    false,
                    false);
                vm.SetPanoramaCompletionState(
                    false);
                vm.SetPanoramaProcessingRunning(
                    false);

                _panoramaCaptureCts.Dispose();
                _panoramaCaptureCts = null;

                NewPanoramaButton.Content =
                    "360° 파노라마 촬영";

                NewPanoramaButton.IsEnabled =
                    true;

                LoadPanoramaImageButton.IsEnabled =
                    true;
                PanoramaLoadingOverlay.Visibility = Visibility.Collapsed;

                ConsoleLogHelper.Info(
                    "EO PANORAMA / UI",
                    "Panorama UI and control lock restored");
            }

        }

        /// <summary>
        /// 2026-08-18: 기존 파노라마 이미지 불러오기
        /// </summary>
        private void LoadPanoramaImageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SelectAndLoadPanoramaImage();
        }

        /// <summary>
        /// 정적 파노라마 파일을 선택하고 화면에 표시한다.
        ///
        /// BitmapCacheOption.OnLoad를 사용하여 파일 전체를 메모리에 읽은 뒤
        /// 원본 파일 잠금을 해제한다. 따라서 이미지를 표시한 상태에서도
        /// 외부 편집 프로그램에서 동일 파일을 수정하거나 교체할 수 있다.
        /// </summary>
        private void SelectAndLoadPanoramaImage()
        {
            string panoramaDirectory =
                GetPanoramaDirectory();

            /*
             * 2026-08-18: 촬영 결과가 저장되는 동일한 Panoramas 폴더에서
             * 파일 선택 창을 시작한다. 폴더가 아직 없으면 먼저 생성한다.
             */
            Directory.CreateDirectory(
                panoramaDirectory);

            OpenFileDialog dialog =
                new OpenFileDialog
                {
                    Title =
                        "파노라마 이미지 선택",
                    Filter =
                        "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All Files (*.*)|*.*",
                    CheckFileExists =
                        true,
                    Multiselect =
                        false,
                    InitialDirectory =
                        panoramaDirectory,
                    RestoreDirectory =
                        true
                };

            if (dialog.ShowDialog(this) !=
                true)
            {
                return;
            }

            try
            {
                DisplayPanoramaFile(
                    dialog.FileName,
                    "Manual file selected");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "파노라마 이미지를 불러올 수 없습니다.\n\n" +
                    ex.Message,
                    "파노라마 이미지 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

        }

        /// <summary>
        /// Panoramas 폴더의 Panorama N.jpg 중 가장 큰 N을 시작 이미지로
        /// 표시한다. 파일이 없거나 읽기에 실패하면 XAML의 기존 내장 기본
        /// 이미지를 그대로 유지한다.
        /// </summary>
        private void LoadLatestGeneratedPanoramaOrKeepDefault()
        {
            try
            {
                string directory =
                    GetPanoramaDirectory();

                if (!Directory.Exists(directory))
                {
                    ConsoleLogHelper.Info(
                        "EO PANORAMA / DEFAULT",
                        "Generated panorama directory not found; bundled default retained / DIRECTORY=" +
                        directory);
                    return;
                }

                string latestPath =
                    Directory
                        .EnumerateFiles(
                            directory,
                            "Panorama *.jpg",
                            SearchOption.TopDirectoryOnly)
                        .Select(path => new
                        {
                            Path = path,
                            Sequence = GetPanoramaSequence(path)
                        })
                        .Where(item => item.Sequence > 0)
                        .OrderByDescending(item => item.Sequence)
                        .Select(item => item.Path)
                        .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(latestPath))
                {
                    ConsoleLogHelper.Info(
                        "EO PANORAMA / DEFAULT",
                        "No generated Panorama N.jpg found; bundled default retained / DIRECTORY=" +
                        directory);
                    return;
                }

                DisplayPanoramaFile(
                    latestPath,
                    "Latest generated panorama loaded at startup");
            }
            catch (Exception ex)
            {
                _currentPanoramaFilePath =
                    null;

                ConsoleLogHelper.Warning(
                    "EO PANORAMA / DEFAULT",
                    "Latest panorama load failed; bundled default retained / " + ex.Message);
            }

        }

        /// <summary>
        /// DisplayPanoramaFile 화면 표시 함수.
        /// </summary>
        private void DisplayPanoramaFile(
            string filePath,
            string logOperation)
        {
            BitmapImage bitmap =
                new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption =
                BitmapCacheOption.OnLoad;
            bitmap.CreateOptions =
                BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource =
                new Uri(
                    filePath,
                    UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            PanoramaImage.Source =
                bitmap;

            _currentPanoramaFilePath =
                filePath;

            PanoramaEmptyText.Visibility =
                Visibility.Collapsed;

            PanoramaFileNameText.Text =
                "ROOFTOP PANORAMA / " +
                Path.GetFileName(filePath);

            ConsoleLogHelper.State(
                "EO PANORAMA / DEFAULT",
                logOperation + " / FILE=" + filePath +
                " / SIZE=" + bitmap.PixelWidth + "x" + bitmap.PixelHeight);
        }

        /// <summary>
        /// GetPanoramaSequence 조회 함수.
        /// </summary>
        private static int GetPanoramaSequence(
            string filePath)
        {
            string fileName =
                Path.GetFileNameWithoutExtension(filePath);

            const string Prefix =
                "Panorama ";

            if (string.IsNullOrWhiteSpace(fileName) ||
                !fileName.StartsWith(
                    Prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            return int.TryParse(
                       fileName.Substring(Prefix.Length),
                       out int sequence)
                ? sequence
                : -1;
        }

        /// <summary>
        /// PanoramaImage_MouseLeftButtonDown 동작 수행 함수.
        /// </summary>
        private void PanoramaImage_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2 ||
                PanoramaImage.Source == null)
            {
                return;
            }

            if (vm.IsPanoramaCaptureRunning ||
                vm.IsPanoramaProcessingRunning)
            {
                e.Handled = true;

                ConsoleLogHelper.Warning(
                    "EO PANORAMA / PREVIEW",
                    "Preview blocked while capture, stitching, blending or saving is running");
                return;
            }

            if (_panoramaPreviewWindow != null)
            {
                if (_panoramaPreviewWindow.WindowState == WindowState.Minimized)
                {
                    _panoramaPreviewWindow.WindowState = WindowState.Normal;
                }

                _panoramaPreviewWindow.Activate();
                e.Handled = true;
                return;
            }

            e.Handled = true;

            try
            {
                PanoramaPreviewWindow previewWindow =
                    new PanoramaPreviewWindow(
                        PanoramaImage.Source,
                        _currentPanoramaFilePath,
                        string.IsNullOrWhiteSpace(_currentPanoramaFilePath)
                            ? "기본 파노라마"
                            : Path.GetFileName(_currentPanoramaFilePath))
                    {
                        Owner = this
                    };

                _panoramaPreviewWindow = previewWindow;
                previewWindow.Closed +=
                    (closedSender, closedArgs) =>
                    {
                        if (ReferenceEquals(
                                _panoramaPreviewWindow,
                                previewWindow))
                        {
                            _panoramaPreviewWindow = null;
                        }

                    };

                previewWindow.Show();

                ConsoleLogHelper.Info(
                    "EO PANORAMA / PREVIEW",
                    "Panorama preview opened / FILE=" +
                    (_currentPanoramaFilePath ?? "BUNDLED_DEFAULT"));
            }
            catch (Exception ex)
            {
                _panoramaPreviewWindow = null;

                ConsoleLogHelper.Error(
                    "EO PANORAMA / PREVIEW",
                    "Panorama preview open failed",
                    ex);

                MessageBox.Show(
                    this,
                    "파노라마 확대 창을 열 수 없습니다.\n\n" + ex.Message,
                    "파노라마 확대 보기",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

        }

        /// <summary>
        /// ClosePanoramaPreview 종료 및 자원 해제 함수.
        /// </summary>
        private void ClosePanoramaPreview()
        {
            PanoramaPreviewWindow previewWindow =
                _panoramaPreviewWindow;

            _panoramaPreviewWindow = null;

            if (previewWindow == null)
            {
                return;
            }

            try
            {
                previewWindow.Close();

                ConsoleLogHelper.Info(
                    "EO PANORAMA / PREVIEW",
                    "Open preview closed before panorama processing");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "EO PANORAMA / PREVIEW",
                    "Failed to close preview before panorama processing",
                    ex);
            }

        }

        /// <summary>
        /// 2026-08-18: 생성과 불러오기가 항상 동일한 저장 폴더를 사용한다.
        /// Visual Studio 실행 시에는 bin\x64\Debug\Panoramas 아래가 된다.
        /// </summary>
        private static string GetPanoramaDirectory()
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Panoramas");
        }

        /// <summary>
        /// 2026-08-18: 기존 결과를 덮어쓰지 않고 현재 가장 큰 번호 + 1로
        /// 저장한다. 중간 번호가 삭제되어도 새 결과가 항상 최신 번호가 된다.
        /// </summary>
        private static string GetNextPanoramaOutputPath()
        {
            string directory =
                GetPanoramaDirectory();

            Directory.CreateDirectory(
                directory);

            int sequence =
                Directory
                    .EnumerateFiles(
                        directory,
                        "Panorama *.jpg",
                        SearchOption.TopDirectoryOnly)
                    .Select(GetPanoramaSequence)
                    .Where(value => value > 0)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

            while (true)
            {
                string candidate =
                    Path.Combine(
                        directory,
                        "Panorama " + sequence + ".jpg");

                if (!File.Exists(candidate))
                {
                    return candidate;
                }

                sequence++;
            }

        }

        /// <summary>
        /// [Window] Keyboard KeyDown 처리
        ///
        /// R / T:
        /// EO 또는 IR 영상을 큰 주화면으로 선택한다.
        ///
        /// 방향키:
        /// Pan / Tilt 이동 상태를 ViewModel로 전달한다.
        ///
        /// TextBox 입력 중에는 단축키를 처리하지 않는다.
        /// </summary>
        private void Window_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            // HOME / ZERO 또는 AUTO SCAN 중에는 장비 제어 키 입력 전체를 차단한다.
            //
            // 차단 범위:
            // - 방향키
            // - WASD
            // - Zoom In / Zoom Out 단축키
            // - Focus Near / Focus Far 단축키
            // - R / T 영상 전환
            // - 이후 추가되는 기타 Window 장비 제어 단축키
            //
            // 특정 키만 선별하지 않고 PreviewKeyDown 입구에서 먼저 반환하므로
            // 잠금 진행 중 어떠한 키보드 제어도 장비 명령 함수까지 도달하지 않는다.
            // 작업 완료/중지 후 IsControlInputLocked=false가 되면
            // 별도 사용자 조작 없이 자동으로 정상 입력 상태로 복귀한다.
            if (IsControlInputKeyboardLocked())
            {
                StopActiveHoverLensMove();
                e.Handled =
                    true;

                return;
            }

            if (IsTextBoxKeyboardFocus())
            {
                return;
            }

            /// <summary>
            /// R / T 영상 주화면 전환 처리
            ///
            /// KeyDown 자동 반복으로 동일 전환이 반복되지 않도록
            /// 최초 KeyDown에서만 처리한다.
            /// </summary>
            if (IsVideoSwitchKey(
                    e.Key))
            {
                if (!e.IsRepeat)
                {
                    VideoPopoutCameraType targetCameraType =
                        e.Key == Key.R
                            ? VideoPopoutCameraType.Eo
                            : VideoPopoutCameraType.Ir;

                    SelectPrimaryVideo(
                        targetCameraType);
                }

                e.Handled =
                    true;

                return;
            }

            // 2026-08-18: 메인 화면의 EO/IR 영상 위에 마우스를 둔 상태에서는
            // W/S=Zoom In/Out, A/D=Focus Near/Far로 해당 카메라를 제어한다.
            if (IsLensShortcutKey(e.Key))
            {
                VideoPopoutCameraType? hoveredCamera =
                    GetHoveredCameraType();

                if (hoveredCamera.HasValue)
                {
                    if (!_activeHoverLensKey.HasValue && !e.IsRepeat)
                    {
                        _activeHoverLensKey = e.Key;
                        _activeHoverLensCameraType = hoveredCamera.Value;
                        StartHoverLensMove(hoveredCamera.Value, e.Key);
                    }

                    e.Handled = true;
                    return;
                }

            }

            if (!IsPanTiltDirectionKey(
                    e.Key))
            {
                return;
            }

            vm?.HandlePanTiltKeyDown(
                e.Key);

            e.Handled =
                true;
        }

        /// <summary>
        /// [Window] 방향키 KeyUp 처리
        ///
        /// 해제된 방향키 상태를 ViewModel로 전달하고,
        /// 남아 있는 방향키 조합에 맞춰 이동 방향을 갱신한다.
        ///
        /// 예:
        /// Left + Up 상태에서 Up만 해제
        /// -> 대각선 이동에서 Pan Left 이동으로 전환
        /// </summary>
        private void Window_PreviewKeyUp(
            object sender,
            KeyEventArgs e)
        {
            // HOME / ZERO 또는 AUTO SCAN 진행 중 KeyUp도 함께 소비한다.
            //
            // HOME 시작 전에 눌려 있던 방향키/WASD/Zoom/Focus 키가
            // HOME 완료 뒤 늦게 해제되면서 Stop 또는 방향 전환 명령을
            // 발생시키는 것을 방지한다.
            if (IsControlInputKeyboardLocked())
            {
                StopActiveHoverLensMove();
                e.Handled =
                    true;

                return;
            }

            if (_activeHoverLensKey.HasValue &&
                _activeHoverLensKey.Value == e.Key)
            {
                StopActiveHoverLensMove();
                e.Handled = true;
                return;
            }

            if (!IsPanTiltDirectionKey(
                    e.Key))
            {
                return;
            }

            vm?.HandlePanTiltKeyUp(
                e.Key);

            e.Handled =
                true;
        }

        /// <summary>
        /// [Window] Focus 이탈 처리
        ///
        /// 방향키를 누른 상태에서 다른 프로그램으로 전환되면
        /// KeyUp 이벤트가 들어오지 않을 수 있으므로,
        /// 키보드 Pan / Tilt 상태를 강제로 초기화하고 정지한다.
        /// </summary>
        private void Window_Deactivated(
            object sender,
            EventArgs e)
        {
            StopActiveHoverLensMove();
            vm?.ResetAllKeyboardControlState();
        }

        /// <summary>
        /// 2026-08-18: 제어 중 마우스가 EO/IR 영상 밖으로 나가면 KeyUp 유실과
        /// 관계없이 줌·포커스 연속 명령을 즉시 정지한다.
        /// </summary>
        private void VideoBorder_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            StopActiveHoverLensMove();
        }

        /// <summary>
        /// GetHoveredCameraType 조회 함수.
        /// </summary>
        private VideoPopoutCameraType? GetHoveredCameraType()
        {
            if (EoVideoBorder != null && EoVideoBorder.IsMouseOver)
            {
                return VideoPopoutCameraType.Eo;
            }

            if (IrVideoBorder != null && IrVideoBorder.IsMouseOver)
            {
                return VideoPopoutCameraType.Ir;
            }

            return null;
        }

        /// <summary>
        /// IsLensShortcutKey 상태 확인 함수.
        /// </summary>
        private static bool IsLensShortcutKey(Key key)
        {
            return key == Key.W || key == Key.S ||
                   key == Key.A || key == Key.D;
        }

        /// <summary>
        /// StartHoverLensMove 시작 함수.
        /// </summary>
        private void StartHoverLensMove(
            VideoPopoutCameraType cameraType,
            Key key)
        {
            if (cameraType == VideoPopoutCameraType.Eo)
            {
                switch (key)
                {
                    case Key.W: vm?.StartEoZoomInMove(); break;
                    case Key.S: vm?.StartEoZoomOutMove(); break;
                    case Key.A: vm?.StartEoFocusNearMove(); break;
                    case Key.D: vm?.StartEoFocusFarMove(); break;
                }

                return;
            }

            switch (key)
            {
                case Key.W: vm?.StartIrZoomInMove(); break;
                case Key.S: vm?.StartIrZoomOutMove(); break;
                case Key.A: vm?.StartIrFocusNearMove(); break;
                case Key.D: vm?.StartIrFocusFarMove(); break;
            }

        }

        /// <summary>
        /// StopActiveHoverLensMove 중지 함수.
        /// </summary>
        private void StopActiveHoverLensMove()
        {
            if (!_activeHoverLensKey.HasValue ||
                !_activeHoverLensCameraType.HasValue)
            {
                return;
            }

            Key key = _activeHoverLensKey.Value;
            VideoPopoutCameraType cameraType =
                _activeHoverLensCameraType.Value;

            _activeHoverLensKey = null;
            _activeHoverLensCameraType = null;

            if (cameraType == VideoPopoutCameraType.Eo)
            {
                vm?.StopContinuousMove();
                return;
            }

            if (key == Key.W || key == Key.S)
            {
                vm?.StopIrZoomMove();
            }
            else
            {
                vm?.StopIrFocusMove();
            }

        }

        /// <summary>
        /// HOME / ZERO 또는 AUTO SCAN 실행 중 키보드 전체 Lock 여부 확인
        ///
        /// PreviewKeyDown / PreviewKeyUp이 동일한 조건을 사용하도록
        /// 공통 함수로 관리한다.
        /// </summary>
        private bool IsControlInputKeyboardLocked()
        {
            return vm?.IsControlInputLocked == true;
        }

        /// <summary>
        /// Pan / Tilt 제어용 방향키 여부 확인
        /// </summary>
        private bool IsPanTiltDirectionKey(
            Key key)
        {
            return key == Key.Left ||
                   key == Key.Right ||
                   key == Key.Up ||
                   key == Key.Down;
        }

        /// <summary>
        /// EO / IR 주화면 선택 단축키 여부 확인
        ///
        /// R:
        /// EO 고배율 영상을 주화면으로 선택
        ///
        /// T:
        /// IR 열영상을 주화면으로 선택
        /// </summary>
        private bool IsVideoSwitchKey(
            Key key)
        {
            return key == Key.R ||
                   key == Key.T;
        }

        /// <summary>
        /// 현재 TextBox가 Keyboard Focus를 갖고 있는지 확인
        ///
        /// AI IP / Port / RTSP 주소 입력 중에는
        /// 방향키를 장비 제어로 사용하지 않는다.
        /// </summary>
        private bool IsTextBoxKeyboardFocus()
        {
            return Keyboard.FocusedElement
                is TextBox;
        }

        #endregion

        #region [PAN / TILT Mouse Events]

        /// <summary>
        /// [PAN] 좌측 버튼 MouseDown
        /// </summary>
        private void PanLeft_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartPanLeftMove();
        }

        /// <summary>
        /// [PAN] 우측 버튼 MouseDown
        /// </summary>
        private void PanRight_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartPanRightMove();
        }

        /// <summary>
        /// [TILT] 위쪽 버튼 MouseDown
        /// </summary>
        private void TiltUp_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartTiltUpMove();
        }

        /// <summary>
        /// [TILT] 아래쪽 버튼 MouseDown
        /// </summary>
        private void TiltDown_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartTiltDownMove();
        }

        /// <summary>
        /// [PAN LEFT + TILT UP]
        /// 좌측 상단 대각선 버튼 MouseDown
        /// </summary>
        private void PanLeftTiltUp_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartPanLeftTiltUpMove();
        }

        /// <summary>
        /// [PAN RIGHT + TILT UP]
        /// 우측 상단 대각선 버튼 MouseDown
        /// </summary>
        private void PanRightTiltUp_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartPanRightTiltUpMove();
        }

        /// <summary>
        /// [PAN LEFT + TILT DOWN]
        /// 좌측 하단 대각선 버튼 MouseDown
        /// </summary>
        private void PanLeftTiltDown_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartPanLeftTiltDownMove();
        }

        /// <summary>
        /// [PAN RIGHT + TILT DOWN]
        /// 우측 하단 대각선 버튼 MouseDown
        /// </summary>
        private void PanRightTiltDown_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartPanRightTiltDownMove();
        }

        #endregion

        #region [EO Zoom / Focus Mouse Events]

        /// <summary>
        /// [EO] Zoom 확대 버튼 MouseDown
        /// </summary>
        private void EoZoomIn_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartEoZoomInMove();
        }

        /// <summary>
        /// [EO] Zoom 축소 버튼 MouseDown
        /// </summary>
        private void EoZoomOut_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartEoZoomOutMove();
        }

        /// <summary>
        /// EO Focus Near 연속 이동 시작
        /// </summary>
        private void EoFocusNear_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartEoFocusNearMove();
        }

        /// <summary>
        /// EO Focus Far 연속 이동 시작
        /// </summary>
        private void EoFocusFar_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartEoFocusFarMove();
        }

        /// <summary>
        /// [EO] One Push Focus 버튼 MouseDown
        /// </summary>
        private void EoAutoFocus_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartEoAutoFocusMove();
        }

        #endregion

        #region [IR Zoom / Focus Mouse Events]

        /// <summary>
        /// [IR] Zoom 확대 버튼 MouseDown
        /// </summary>
        private void IrZoomIn_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled =
                true;

            vm?.StartIrZoomInMove();
        }

        /// <summary>
        /// [IR] Zoom 축소 버튼 MouseDown
        /// </summary>
        private void IrZoomOut_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled =
                true;

            vm?.StartIrZoomOutMove();
        }

        /// <summary>
        /// [IR] Focus Near 버튼 MouseDown
        /// </summary>
        private void IrFocusNear_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartIrFocusNearMove();
        }

        /// <summary>
        /// [IR] Focus Far 버튼 MouseDown
        /// </summary>
        private void IrFocusFar_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartIrFocusFarMove();
        }

        /// <summary>
        /// [IR] Digital Zoom 확대 버튼 MouseDown
        /// </summary>
        private void IrDigitalZoomIn_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartIrDigitalZoomInMove();
        }

        /// <summary>
        /// [IR] Digital Zoom 축소 버튼 MouseDown
        /// </summary>
        private void IrDigitalZoomOut_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartIrDigitalZoomOutMove();
        }

        /// <summary>
        /// [IR] Auto Focus 버튼 MouseDown
        /// </summary>
        private void IrAutoFocus_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            vm?.StartIrAutoFocusMove();
        }

        #endregion

        #region [Continuous Move Stop Events]

        /// <summary>
        /// MouseUp 공통 연속 이동 정지
        /// </summary>
        private void MoveStop_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            vm?.StopContinuousMove();
        }

        /// <summary>
        /// MouseLeave 연속 이동 정지
        ///
        /// 버튼을 누른 상태로 영역 밖으로 이동한 경우에만
        /// 정지 명령을 송신한다.
        /// </summary>
        private void MoveStop_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (e.LeftButton !=
                MouseButtonState.Pressed)
            {
                return;
            }

            vm?.StopContinuousMove();
        }

        /// <summary>
        /// 운용 제어 하위 탭을 선택할 때 이전 스크롤 위치를 남기지 않고
        /// 첫 제목과 버튼이 완전히 보이는 위치로 이동한다.
        /// </summary>
        private void OperationScrollViewer_IsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is ScrollViewer scrollViewer) ||
                !(e.NewValue is bool isVisible) ||
                !isVisible)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(scrollViewer.ScrollToTop));
        }
        #endregion
    }

}
