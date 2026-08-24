using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.ViewModels.Main;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// 영상 분리 창에서 제어할 카메라 종류
    /// </summary>
    public enum VideoPopoutCameraType
    {
        Eo,
        Ir
    }

    /// <summary>
    /// EO 또는 IR 영상 전용 분리 창.
    ///
    /// MainViewModel의 기존 영상 프레임을 공유하며,
    /// 현재 분리된 카메라에 대한 W / S / A / D 렌즈 제어와
    /// 메인 화면과 동일한 방향키 Pan / Tilt 제어를 처리한다.
    /// </summary>
    public partial class VideoPopoutWindow : Window
    {
        #region [Fields]

        private readonly MainViewModel _viewModel;

        /// <summary>
        /// 현재 분리 창에 표시되고,
        /// W / S / A / D 제어 대상으로 선택된 카메라.
        ///
        /// R:
        /// EO 고배율 영상 선택
        ///
        /// T:
        /// IR 열영상 선택
        /// </summary>
        private VideoPopoutCameraType _cameraType;

        /// <summary>
        /// 현재 눌린 렌즈 제어 키.
        ///
        /// WPF KeyDown 자동 반복으로 동일 시작 명령이
        /// 여러 번 송신되는 것을 방지하기 위해 한 개만 유지한다.
        /// </summary>
        private Key? _activeLensKey;

        #endregion

        #region [Constructor]

        /// <summary>
        /// VideoPopoutWindow 동작 수행 함수.
        /// </summary>
        public VideoPopoutWindow(
            MainViewModel viewModel,
            VideoPopoutCameraType cameraType)
        {
            _viewModel =
                viewModel ??
                throw new ArgumentNullException(
                    nameof(viewModel));

            _cameraType =
                cameraType;

            InitializeComponent();

            DataContext =
                _viewModel;

            ConfigureCameraBinding();
        }

        #endregion

        #region [Window Events]

        /// <summary>
        /// Window_Loaded 이벤트 처리 함수.
        /// </summary>
        private void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Keyboard.Focus(
                this);
        }

        /// <summary>
        /// 분리 영상 창 Keyboard KeyDown 처리
        ///
        /// R:
        /// EO 고배율 영상을 주화면으로 선택한다.
        ///
        /// T:
        /// IR 열영상을 주화면으로 선택한다.
        ///
        /// W / S:
        /// 현재 선택 영상의 Zoom In / Out
        ///
        /// A / D:
        /// 현재 선택 영상의 Focus Near / Far
        /// </summary>
        private void Window_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (_viewModel.IsControlInputLocked)
            {
                e.Handled =
                    true;

                return;
            }

            if (IsTextInputFocused())
            {
                return;
            }

            /// <summary>
            /// EO / IR 주화면 전환
            ///
            /// 렌즈가 움직이는 중 카메라를 전환하면
            /// 이전 카메라의 연속 명령이 남을 수 있으므로
            /// 전환 전에 반드시 현재 렌즈 이동을 정지한다.
            /// </summary>
            if (IsVideoSwitchShortcutKey(
                    e.Key))
            {
                if (!e.IsRepeat)
                {
                    VideoPopoutCameraType targetCameraType =
                        e.Key == Key.R
                            ? VideoPopoutCameraType.Eo
                            : VideoPopoutCameraType.Ir;

                    SwitchCamera(
                        targetCameraType);

                    /// <summary>
                    /// 분리 창의 선택 상태와 메인 화면의
                    /// 큰 영상 위치를 동일하게 맞춘다.
                    /// </summary>
                    MainWindow mainWindow =
                        Owner as MainWindow;

                    mainWindow?
                        .SelectPrimaryVideo(
                            targetCameraType);
                }

                e.Handled =
                    true;

                return;
            }

            /// <summary>
            /// 분리 창에서도 메인 화면과 동일하게
            /// 방향키 Pan / Tilt 제어를 사용할 수 있도록 한다.
            ///
            /// MainViewModel이 기존 방향키 눌림 상태와
            /// 대각선 조합을 모두 관리하므로,
            /// 분리 창에서는 KeyDown / KeyUp만 그대로 전달한다.
            /// </summary>
            if (IsPanTiltShortcutKey(
                    e.Key))
            {
                _viewModel
                    .HandlePanTiltKeyDown(
                        e.Key);

                e.Handled =
                    true;

                return;
            }

            if (!IsLensShortcutKey(
                    e.Key))
            {
                return;
            }

            /*
             * 동시에 여러 렌즈 명령을 보내지 않는다.
             * 현재 키를 뗀 뒤 다음 키를 눌러야 새 동작을 시작한다.
             */
            if (_activeLensKey.HasValue)
            {
                e.Handled =
                    true;

                return;
            }

            _activeLensKey =
                e.Key;

            StartLensMove(
                e.Key);

            e.Handled =
                true;
        }

        /// <summary>
        /// Window_PreviewKeyUp 동작 수행 함수.
        /// </summary>
        private void Window_PreviewKeyUp(
            object sender,
            KeyEventArgs e)
        {
            if (_viewModel.IsControlInputLocked)
            {
                _activeLensKey =
                    null;

                e.Handled =
                    true;

                return;
            }

            /// <summary>
            /// 분리 창 방향키 해제 처리
            ///
            /// Left + Up 대각선 이동 중 Up만 해제하면
            /// MainViewModel이 남아 있는 Left 상태를 기준으로
            /// Pan Left 단독 이동으로 자동 전환한다.
            /// </summary>
            if (IsPanTiltShortcutKey(
                    e.Key))
            {
                _viewModel
                    .HandlePanTiltKeyUp(
                        e.Key);

                e.Handled =
                    true;

                return;
            }

            if (!_activeLensKey.HasValue ||
                _activeLensKey.Value != e.Key)
            {
                return;
            }

            StopActiveLensMove();

            e.Handled =
                true;
        }

        /// <summary>
        /// Alt+Tab, 다른 창 클릭 등으로 분리 창이 비활성화되면
        /// KeyUp 누락에 대비해 진행 중인 렌즈 이동을 강제 정지한다.
        /// </summary>
        private void Window_Deactivated(
            object sender,
            EventArgs e)
        {
            /// <summary>
            /// Alt + Tab 또는 다른 창 클릭으로 KeyUp이 누락될 수 있으므로
            /// 렌즈 이동과 Keyboard Pan / Tilt 상태를 모두 정리한다.
            /// </summary>
            StopActiveLensMove();

            _viewModel
                .ResetKeyboardPanTiltState();
        }

        /// <summary>
        /// Window_Closing 이벤트 처리 함수.
        /// </summary>
        private void Window_Closing(
            object sender,
            CancelEventArgs e)
        {
            /// <summary>
            /// 분리 창 종료 시 장비에 연속 이동 명령이 남지 않도록
            /// 렌즈와 Pan / Tilt를 모두 강제 정지한다.
            /// </summary>
            StopActiveLensMove();

            _viewModel
                .ResetKeyboardPanTiltState();
        }

        #endregion

        #region [Camera Binding]

        /// <summary>
        /// 분리 창에서 표시하고 제어할 카메라를 전환한다.
        ///
        /// 기존 분리 창을 닫거나 새 창을 생성하지 않는다.
        ///
        /// MainViewModel이 보유한 EO / IR BitmapSource 중
        /// 선택한 카메라의 Binding으로 교체한다.
        ///
        /// 전환 이후 W / S / A / D 명령도
        /// 새로 선택된 카메라 기준으로 처리된다.
        /// </summary>
        private void SwitchCamera(
            VideoPopoutCameraType cameraType)
        {
            if (_cameraType ==
                cameraType)
            {
                return;
            }

            /// <summary>
            /// 이전 카메라에 연속 렌즈 명령이 남지 않도록
            /// 영상 전환 전에 진행 중인 동작을 정지한다.
            /// </summary>
            StopActiveLensMove();

            _cameraType =
                cameraType;

            ConfigureCameraBinding();

            Keyboard.Focus(
                this);

            Console.WriteLine();

            Console.WriteLine(
                "[VIDEO POPOUT SWITCH] CAMERA : " +
                (_cameraType ==
                    VideoPopoutCameraType.Eo
                        ? "EO"
                        : "IR"));

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 현재 분리 영상 창의 카메라 종류에 맞춰
        /// 영상, 연결 상태, 제목 및 상태 표시 색상을 설정한다.
        ///
        /// EO 카메라:
        /// - EOCameraImage 영상 Binding
        /// - EoStatusText 연결 상태 Binding
        /// - 고대비 전술 청록색 상태 표시
        ///
        /// IR 카메라:
        /// - IRCameraImage 영상 Binding
        /// - IrStatusText 연결 상태 Binding
        /// - 고대비 열상 황색 상태 표시
        ///
        /// R / T 단축키로 분리 창의 카메라가 변경될 때도
        /// 이 함수를 다시 호출하여 모든 Binding과 표시 정보를 갱신한다.
        ///
        /// 메인 화면과 동일한 상태 색상:
        /// 2026-08-24: 영상 위에서도 식별되도록 방산 UI 고대비 색상으로 통일한다.
        /// - EO: #5EE7F7
        /// - IR: #FFB347
        /// </summary>
        private void ConfigureCameraBinding()
        {
            string imagePropertyName;
            string statusPropertyName;
            string cameraTitle;
            Color statusColor;

            if (_cameraType ==
                VideoPopoutCameraType.Eo)
            {
                imagePropertyName =
                    "EOCameraImage";

                statusPropertyName =
                    "EoStatusText";

                cameraTitle =
                    "EO CAMERA / SHORTCUT CONTROL";

                statusColor =
                    Color.FromRgb(
                        0x5E,
                        0xE7,
                        0xF7);

                // EO 영상은 16:9 비율의 넓은 분리 창을 사용한다.
                Width =
                    1280;

                Height =
                    720;

                CameraImage.Stretch =
                    Stretch.UniformToFill;

                Title =
                    "[REI] EO CAMERA VIEW";
            }
            else
            {
                imagePropertyName =
                    "IRCameraImage";

                statusPropertyName =
                    "IrStatusText";

                cameraTitle =
                    "IR CAMERA / SHORTCUT CONTROL";

                statusColor =
                    Color.FromRgb(
                        0xFF,
                        0xB3,
                        0x47);

                //
                // IR 영상은 EO보다 좁은 센서 종횡비를 사용한다.
                // 분리 창의 가로 크기를 줄이고 원본 비율을 유지하여
                // 화면이 좌우로 늘어나 보이지 않도록 한다.
                //
                Width =
                    960;

                Height =
                    800;

                CameraImage.Stretch =
                    Stretch.Uniform;

                Title =
                    "[REI] IR CAMERA VIEW";
            }

            BindingOperations.SetBinding(
                CameraImage,
                Image.SourceProperty,
                new Binding(imagePropertyName)
                {
                    Mode = BindingMode.OneWay
                });

            BindingOperations.SetBinding(
                CameraStatusText,
                TextBlock.TextProperty,
                new Binding(statusPropertyName)
                {
                    Mode = BindingMode.OneWay
                });

            CameraStatusText.Foreground =
                new SolidColorBrush(
                    statusColor);

            CameraTitleText.Text =
                cameraTitle;

            CameraTitleText.Foreground =
                new SolidColorBrush(
                    statusColor);

            CameraInfoBorder.BorderBrush =
                new SolidColorBrush(
                    statusColor);
        }

        #endregion

        #region [Shortcut Control]

        /// <summary>
        /// 분리 영상 창의 EO / IR 전환 단축키 여부 확인
        /// </summary>
        private static bool IsVideoSwitchShortcutKey(
            Key key)
        {
            return key == Key.R ||
                   key == Key.T;
        }

        /// <summary>
        /// 분리 영상 창의 Pan / Tilt 방향키 여부 확인
        ///
        /// 메인 화면과 동일하게 단일 방향과
        /// 두 키 조합 대각선 이동을 모두 지원한다.
        /// </summary>
        private static bool IsPanTiltShortcutKey(
            Key key)
        {
            return key == Key.Left ||
                   key == Key.Right ||
                   key == Key.Up ||
                   key == Key.Down;
        }

        /// <summary>
        /// IsLensShortcutKey 상태 확인 함수.
        /// </summary>
        private static bool IsLensShortcutKey(
            Key key)
        {
            return key == Key.W ||
                   key == Key.S ||
                   key == Key.A ||
                   key == Key.D;
        }

        /// <summary>
        /// IsTextInputFocused 상태 확인 함수.
        /// </summary>
        private static bool IsTextInputFocused()
        {
            return Keyboard.FocusedElement
                is TextBox;
        }

        /// <summary>
        /// StartLensMove 시작 함수.
        /// </summary>
        private void StartLensMove(
            Key key)
        {
            if (_cameraType ==
                VideoPopoutCameraType.Eo)
            {
                switch (key)
                {
                    case Key.W:

                        _viewModel
                            .StartEoZoomInMove();

                        break;

                    case Key.S:

                        _viewModel
                            .StartEoZoomOutMove();

                        break;

                    case Key.A:

                        _viewModel
                            .StartEoFocusNearMove();

                        break;

                    case Key.D:

                        _viewModel
                            .StartEoFocusFarMove();

                        break;
                }

                return;
            }

            switch (key)
            {
                case Key.W:

                    _viewModel
                        .StartIrZoomInMove();

                    break;

                case Key.S:

                    _viewModel
                        .StartIrZoomOutMove();

                    break;

                case Key.A:

                    _viewModel
                        .StartIrFocusNearMove();

                    break;

                case Key.D:

                    _viewModel
                        .StartIrFocusFarMove();

                    break;
            }

        }

        /// <summary>
        /// StopActiveLensMove 중지 함수.
        /// </summary>
        private void StopActiveLensMove()
        {
            if (!_activeLensKey.HasValue)
            {
                return;
            }

            Key activeKey =
                _activeLensKey.Value;

            _activeLensKey =
                null;

            if (_viewModel.IsControlInputLocked)
            {
                return;
            }

            if (_cameraType ==
                VideoPopoutCameraType.Eo)
            {
                _viewModel
                    .StopContinuousMove();

                return;
            }

            if (activeKey == Key.W ||
                activeKey == Key.S)
            {
                _viewModel
                    .StopIrZoomMove();
            }
            else
            {
                _viewModel
                    .StopIrFocusMove();
            }

        }
        #endregion
    }

}
