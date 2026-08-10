using OpenCvWpfTracking.Common;
using Microsoft.Win32;
using OpenCvWpfTracking.ViewModels.Main;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
                    cameraType)
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

            videoContainer.Height =
                isPrimary
                    ? double.NaN
                    : 365;

            videoContainer.HorizontalAlignment =
                HorizontalAlignment.Stretch;

            videoContainer.VerticalAlignment =
                isPrimary
                    ? VerticalAlignment.Stretch
                    : VerticalAlignment.Top;

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
            Keyboard.Focus(
                this);
        }

        /// <summary>
        /// [Demo Panorama] 새 파노라마 이미지 선택
        ///
        /// 현 단계에서는 실시간 Stitching을 수행하지 않고,
        /// 새 파노라마로 사용할 정적 JPG / PNG 파일을 선택하여 표시한다.
        /// 추후 다중 영상 합성 기능을 추가할 때 이 진입점을 그대로 확장한다.
        /// </summary>
        private void NewPanoramaButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SelectAndLoadPanoramaImage();
        }

        /// <summary>
        /// [Demo Panorama] 기존 파노라마 이미지 불러오기
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
                        false
                };

            if (dialog.ShowDialog(this) !=
                true)
            {
                return;
            }

            try
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
                        dialog.FileName,
                        UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                PanoramaImage.Source =
                    bitmap;

                PanoramaEmptyText.Visibility =
                    Visibility.Collapsed;

                PanoramaFileNameText.Text =
                    "ROOFTOP PANORAMA / " +
                    Path.GetFileName(
                        dialog.FileName);
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
                e.Handled =
                    true;

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
            vm?.ResetAllKeyboardControlState();
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
        #endregion
    }

}
