using OpenCvSharp;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Converters;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// EO/IR RTSP 연결, 재연결, 프레임 수신과 화면 갱신 흐름을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Video Connect / Disconnect]

        #region [Connect]

        /// <summary>
        /// 영상 연결 함수
        ///
        /// [VD] / [EO RTSP] / [IR RTSP] 연결을 시도하고,
        /// 연결 성공한 영상만 각각의 [CaptureLoop]로 출력한다.
        ///
        /// [FFmpeg RTSP Open]은 지연될 수 있으므로
        /// 백그라운드 [Task]에서 연결을 시도한다.
        /// </summary>
        public async void Connect()
        {
            /*
             * TODO(COMMAND-NEXT):
             * 현재 XAML Command가 RelayCommand(Action)에 연결되어 있어 async void를 유지한다.
             * 향후 ConnectCommand를 AsyncRelayCommand로 통일할 때 Task 반환형 ConnectAsync로 변경한다.
             * 그 전까지 예외는 App.xaml.cs의 전역 예외 로그에서 확인한다.
             */
            ConsoleLogHelper.CommandSection(
                "DEVICE CONNECT",
                "Connect requested",
                string.Empty,
                $"CONTROL : {ControlAgentIp}:{ControlAgentPortText}",
                $"EO      : {ConsoleLogHelper.MaskRtspPassword(EoSourceAddress)}",
                $"IR      : {ConsoleLogHelper.MaskRtspPassword(IrSourceAddress)}");

            /// <summary>
            /// 현재 [Connect] 시도 중이면
            /// 중복 [Connect] 입력 무시
            /// </summary>
            if (_isVideoConnecting)
            {
                Console.WriteLine();
                Console.WriteLine("[VIDEO] Connecting...");

                ConsoleLogHelper.PrintLine();
                return;
            }

            /*
             * 통신 설정 탭에서 선택된 EO / IR RTSP 주소를
             * 실제 연결 시작 전에 검증한다.
             *
             * 빈값 또는 RTSP 형식 오류가 있으면
             * FFmpeg Open을 수행하지 않고 즉시 종료하여
             * 불필요한 연결 지연과 예외 로그를 방지한다.
             */
            if (!TryGetRtspEndpoints(
                    out string eoRtspAddress,
                    out string irRtspAddress))
            {
                return;
            }

            /*
             * Trim 처리된 검증 완료 주소를
             * 실제 영상 연결 주소로 다시 반영한다.
             */
            EoSourceAddress =
                eoRtspAddress;

            IrSourceAddress =
                irRtspAddress;

            if (IsAllVideoConnected())
            {
                // 2026-08-14: 이미 재생 중인 RTSP 프레임과 BBox를 절대 초기화하지 않는다.
                EoStatusText =
                    "Already Connected...";

                IrStatusText =
                    "Already Connected...";

                Console.WriteLine(
                    "[VIDEO] EO / IR Already Connected.");

                ConsoleLogHelper.PrintLine();
                return;
            }

            // 2026-08-18: 한 채널만 연결된 상태에서 다시 연결해도
            // 정상 채널의 프레임과 CaptureLoop는 유지하고 실패 채널만 초기화한다.
            bool isEoStreamActive =
                _eoDecoder.IsOpened || _isEoFrameDisplayed;

            bool isIrStreamActive =
                _irDecoder.IsOpened || _isIrFrameDisplayed;

            if (!isEoStreamActive)
            {
                _isEoFrameDisplayed =
                    false;
            }

            if (!isIrStreamActive)
            {
                _isIrFrameDisplayed =
                    false;
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                if (!isEoStreamActive)
                {
                    EoDetectionBoxes.Clear();
                }

                if (!isIrStreamActive)
                {
                    IrDetectionBoxes.Clear();
                }
            });

            _isVideoConnecting = true; // 연결 시도 중 상태 설정

            /*
             * [VD] 로컬 테스트 영상 연결 기능 임시 비활성화
             *
             * 현재 실제 [EO / IR] RTSP 영상만 사용하므로
             * VD 연결 상태는 갱신하지 않는다.
             */
            if (!isEoStreamActive)
            {
                EoStatusText =
                    "[EO] Connecting...";
            }

            /*
             * 최초 RTSP 연결은 EO를 우선 처리한다.
             *
             * EO 연결 결과가 확정되고 영상 수신 Loop가 시작된 뒤
             * IR 연결을 시작하므로, 초기 상태 표시 순서가
             * EO -> IR 순서로 명확하게 유지된다.
             */
            if (!isIrStreamActive)
            {
                IrStatusText =
                    "[IR] Waiting for EO...";
            }

            try
            {
                // 2026-08-18: 살아 있는 채널의 CaptureLoop가 사용하는 Token은
                // 취소하지 않는다. 최초 연결 또는 명시적 연결 해제 후에만 새로 만든다.
                if (_cts == null ||
                    _cts.IsCancellationRequested)
                {
                    ResetCancellationToken();
                }

                _isDeviceConnectionRequested =
                    true;

                /// <summary>
                /// [Control Agent] 최초 연결을 바로 시도한다.
                ///
                /// 최초 연결에 실패하더라도 내부 Auto Reconnect Loop가
                /// 일정 간격으로 연결을 다시 시도한다.
                /// </summary>
                bool isControlAgentConnected =
                    await ConnectLaAsync();

                if (isControlAgentConnected)
                {
                    InitializeThermalBlackHotAfterDeviceConnected();
                }

                /// <summary>
                /// 선택된 EO 카메라가 [옥상 GOP CTEC] 직접 제어 장비이면
                /// 카메라 IP의 [TCP Port 9000] 응답 수신 연결을 시작한다.
                ///
                /// CGI 명령 송신과 TCP 응답 수신은 서로 다른 통로이며,
                /// Port 9000 연결은 명령 처리 결과 및 위치 조회 응답 수신에 사용한다.
                /// </summary>
                await StartSelectedEoCtecResponseAsync();

                /*
                 * AI Detector 연결은 하단 AI CONNECT 버튼의 독립 흐름에서 수행한다.
                 * 장비 연결 버튼은 Control Agent와 EO/IR RTSP 연결만 담당한다.
                 */

                /*
                 * 로컬 VD/OpenCV 테스트 경로는 현재 제품 흐름에서 사용하지 않아 제거했다.
                 * 영상 연결은 EO -> IR 순서의 FFmpeg RTSP 경로만 사용한다.
                 */

                VideoConnectResult result =
                    await OpenVideoSourcesAsync();

                if (!_isDeviceConnectionRequested ||
                    _cts == null ||
                    _cts.IsCancellationRequested)
                {
                    _eoDecoder.Close();
                    _irDecoder.Close();
                    return;
                }

                // EO / IR 개별 상태 Console 출력
                WriteVideoConnectLog(result);

                /*
                 * [LA AGENT] 최초 장비 연결 완료 후 HOME POSITION 자동 실행
                */

                /*
                 * [LA AGENT] 최초 장비 연결 완료 후 HOME POSITION 자동 실행
                 *
                 * Vertiport 운용 흐름과 동일하게 다음 순서를 보장한다.
                 *
                 * 1. Control Agent TCP 연결
                 * 2. EO/IR 두 채널이 모두 연결된 경우에만 HOME POSITION 실행
                 *
                 * 한 채널만 연결된 상태에서는 자동 HOME을 실행하지 않는다.
                 * 필요한 경우 운용자가 연결 복구 후 HOME 버튼을 직접 누른다.
                 *
                 * ENVIRONMENT / WEB AGENT에서는 HOME / ZERO가 지원 대상이 아니므로
                 * 자동 HOME을 실행하지 않는다.
                 *
                 * HOME 진행 중에는 수동 HOME과 동일하게
                 * 전체 우측 UI, 탭, 버튼, 방향키, WASD, Zoom/Focus 단축키가 잠기며,
                 * 정상 완료/송신 실패/예외/30초 Timeout 후 반드시 자동 해제된다.
                 */
                bool areBothCamerasConnected =
                    result.EoResult &&
                    result.IrResult;

                if (isControlAgentConnected &&
                    IsRooftopStatusSelected &&
                    areBothCamerasConnected)
                {
                    ConsoleLogHelper.Info(
                        "CONNECT / AUTO HOME",
                        "Eligibility passed / CONTROL AGENT + EO and IR connected / MODE=LA AGENT");

                    Console.WriteLine();
                    Console.WriteLine(
                        "[CONNECT] LA AGENT + EO/IR VIDEO AVAILABLE " +
                        "/ AUTO HOME POSITION START");
                    ConsoleLogHelper.PrintLine();

                    await MoveHomePositionAsync();

                    ConsoleLogHelper.State(
                        "CONNECT / AUTO HOME",
                        $"Completed / STATUS={HomeZeroStatusText}");

                    Console.WriteLine();
                    Console.WriteLine(
                        "[CONNECT] LA AGENT AUTO HOME POSITION END " +
                        $"/ STATUS={HomeZeroStatusText}");
                    ConsoleLogHelper.PrintLine();
                }
                else
                {
                    string autoHomeSkipReason =
                        $"CONTROL_AGENT_CONNECTED={isControlAgentConnected} " +
                        $"/ EO_CONNECTED={result.EoResult} " +
                        $"/ IR_CONNECTED={result.IrResult} " +
                        $"/ MODE={(IsRooftopStatusSelected ? "LA AGENT" : "WEB AGENT")}";

                    ConsoleLogHelper.Info(
                        "CONNECT / AUTO HOME",
                        "Skipped / " + autoHomeSkipReason);

                    Console.WriteLine();
                    Console.WriteLine(
                        "[CONNECT] AUTO HOME POSITION SKIPPED / " +
                        autoHomeSkipReason);
                    ConsoleLogHelper.PrintLine();
                }

                /// <summary>
                /// [EO / IR] 영상 연결 성공 시 중앙 십자선 자동 활성화
                ///
                /// 프로그램 최초 실행 상태에서는 십자선을 숨기고,
                /// EO 또는 IR RTSP 영상이 하나라도 정상 연결된 시점에
                /// 운용자가 중심 기준점을 바로 확인할 수 있도록 자동 표시한다.
                ///
                /// 자동 활성화는 연결 성공 시 한 번만 수행하며,
                /// 이후에는 [DISPLAY OVERLAY] 버튼을 통한 수동 조작값을 유지한다.
                /// </summary>
                if (result.EoResult ||
                    result.IrResult)
                {
                    IsCrosshairVisible =
                        true;
                }

                /*
                 * 최초 연결에 실패한 영상은 자동 재연결 시작
                 */
                StartVideoReconnectLoops(result);

                /*
                 * 둘 다 최초 연결에 실패했더라도
                 * 자동 재연결은 계속 수행한다.
                 */
                if (!result.EoResult &&
                    !result.IrResult)
                {
                    Console.WriteLine(
                        "[VIDEO] EO / IR All Connect Failed. " +
                        "Reconnect Loop Started.");

                    ConsoleLogHelper.PrintLine();
                }

                /// <summary>
                /// [AI Detector] 다중 객체 [Bounding Box] 표시 테스트
                ///
                /// 실제 [AI Detector Agent] 연결 전,
                /// 더미 탐지 결과를 이용하여 [Overlay] 표시 상태를 확인한다.
                /// 테스트 완료 후 주석 처리한다.
                /// </summary>
                /// 
                //TestDummyAiDetectionResult();
            }
            finally
            {
                _isVideoConnecting = false;
            }

        }

        #endregion

        #region [Disconnect]

        /// <summary>
        /// 영상 연결 해제 함수
        ///
        /// 1. [CaptureLoop] 종료 요청
        /// 2. [VD__VideoCapture] 해제
        /// 3. [FFmpeg] [EO / IR] [RTSP] Decoder 해제
        /// 4. [상태 문자열 갱신]
        /// </summary>
        public void Disconnect()
        {
            ConsoleLogHelper.Command(
                "DEVICE CONNECT",
                "Disconnect requested");

            /*
             * 2026-08-21: 상위 장비 연결 해제는 현재 연결된 AI Agent도 함께 종료한다.
             * AI 연결 시작은 기존 정책대로 AI CONNECT 버튼에서만 수행한다.
             */
            DisconnectAiAgent();

            Console.WriteLine("[VIDEO] Disconnect Try...");

            Console.WriteLine();

            /// <summary>
            /// 연결 시도 / 자동 재연결 진행 중에도
            /// 사용자가 즉시 연결 해제할 수 있도록 모든 Token을 먼저 종료한다.
            /// </summary>
            _isDeviceConnectionRequested =
                false;

            _controlAgentReconnectCts?.Cancel();
            _videoReconnectCts?.Cancel();

            /*
             * CTEC EO Zoom / Focus 버튼을 누르고 있던 상태에서
             * Disconnect하더라도 CGI Inquiry가 계속 송신되지 않도록 종료한다.
             */
            StopCtecEoPositionPolling();

            /// <summary>
            /// 진행 중인 이동 제어 VIA 0 Pan 작업 종료
            /// </summary>
            CancelMoveControlPanOperation();

            /// <summary>
            /// 진행 중인 EO / IR Zoom Sync 작업 종료
            /// </summary>
            _ = StopZoomSyncAsync();

            /// <summary>
            /// 진행 중인 EO / IR Focus Sync 작업 종료
            /// </summary>
            _ = StopFocusSyncAsync();

            // 1. 먼저 [Loop] 종료 요청
            _cts?.Cancel();

            Interlocked.Exchange(
                ref _isEoFrameDispatchPending,
                0);

            Interlocked.Exchange(
                ref _isIrFrameDispatchPending,
                0);

            /// <summary>
            /// 2-1. [EO] 영상 표시 상태 초기화
            /// </summary>
            _isEoFrameDisplayed = false;

            /// <summary>
            /// 2-2. [IR] 영상 표시 상태 초기화
            /// </summary>
            _isIrFrameDisplayed = false;

            /*
             * [VD] 로컬 테스트 영상 연결 기능 비활성화
             *
             * 현재 VD Decoder를 Open하지 않으므로
             * Release 처리도 함께 비활성화한다.
             */
            _eoDecoder.Close();
            _irDecoder.Close();

            /// <summary>
            /// [Control Agent] 제어 TCP 연결 해제
            /// </summary>
            _laTcpService.Disconnect();

            /// <summary>
            /// [옥상 GOP EO] CTEC Response TCP Port 9000 연결 해제
            /// </summary>
            _ctecCameraResponseService.Stop();

            _connectedEoCtecSource =
                null;

            _activeEoCtecSource =
                null;

            _currentCtecEoZoomPosition =
                0;

            _currentCtecEoFocusPosition =
                0;

            _currentCtecEoFocusMode =
                0;

            SetControlAgentConnectionStatus(
                "Disconnected",
                "#FF6B6B");

            /// <summary>
            /// 장비 연결 해제 시 중앙 십자선 비활성화
            ///
            /// 다음 연결 전까지 검은 화면에 십자선이 남지 않도록
            /// 기본 상태인 [DISABLED]로 초기화한다.
            /// </summary>
            IsCrosshairVisible =
                false;

            /// <summary>
            /// [CURRENT STATUS] 상태값 초기화
            ///
            /// Control Agent 연결 해제 후에는
            /// 마지막 수신 상태값이 화면에 남지 않도록 초기화한다.
            /// </summary>
            _currentPan =
                0.0;

            _lastPanAbsoluteTarget =
                null;

            ClearActivePanTiltAbsoluteMove();

            IsPresetScanRunning =
                false;

            PresetCommandStatusText =
                "CONTROL AGENT DISCONNECTED";

            _currentTilt =
                0.0;

            _currentEoZoom =
                0;

            _currentEoFocus =
                0;

            _currentIrZoom =
                0;

            _currentIrFocus =
                0;

            /// <summary>
            /// CONTROL AGENT TCP 연결에서 남아 있을 수 있는
            /// 분할 수신 Packet Buffer 초기화
            /// </summary>
            _laPacketParser.Reset();

            _currentPowerStatus =
                0x00;

            _currentMoveType =
                ContinuousMoveType.None;

            ClearKeyboardPanTiltPressedState();

            _currentKeyboardPanTiltDirection =
                KeyboardPanTiltDirection.None;

            /// <summary>
            /// CURRENT STATUS UI Binding 갱신
            /// </summary>
            NotifyEoCurrentStatusChanged();
            NotifyIrCurrentStatusChanged();

            // 4. [UI] [Thread]에서 마지막으로 검은 화면 덮어쓰기
            App.Current.Dispatcher.Invoke(() =>
            {
                ClearVideoView(); // [VD] / [EO] / [IR] Viewer 화면을 검은 화면으로 초기화

                /// <summary>
                /// [EO / IR] [AI Detector] 탐지 결과 초기화
                ///
                /// 영상 연결 해제 상태에서는
                /// 검은 화면 위에 [Bounding Box]가 표시되지 않도록 한다.
                /// </summary>
                EoDetectionBoxes.Clear();
                IrDetectionBoxes.Clear();
                EoStatusText = "Disconnected";
                IrStatusText = "Disconnected";
            });

            // 5. [VIDEO] 연결 해제 완료 [Log] 출력
            Console.WriteLine("[VIDEO] Disconnect Complete.");

            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Video View Clear]

        /// <summary>
        /// 지정한 크기의 검은색 [BitmapSource] 생성
        ///
        /// [Disconnect] 시 기존 마지막 프레임이 남지 않도록
        /// [Viewer] 화면을 검은 화면으로 초기화할 때 사용
        /// </summary>
        private BitmapSource CreateBlackBitmap(
            int width,
            int height)
        {
            /// <summary>
            /// [BGR24] 기준 1픽셀당 [3byte]
            /// 전체 [byte] 배열을 0으로 유지하면 검은색 화면이 된다.
            /// </summary>
            int stride = width * 3;

            byte[] pixels =
                new byte[height * stride];

            BitmapSource bitmap =
                BitmapSource.Create(
                    width,
                    height,
                    96,
                    96,
                    System.Windows.Media.PixelFormats.Bgr24,
                    null,
                    pixels,
                    stride);

            bitmap.Freeze();

            return bitmap;
        }

        /// <summary>
        /// [VD] / [EO] / [IR] [Viewer] 화면 초기화
        ///
        /// [C++]에서 [Disconnect] 시 [View]를 검은 화면으로 [Clear] 하던 것과 동일한 목적
        /// </summary>
        private void ClearVideoView()
        {
            /// <summary>
            /// 현재 [Viewer] 크기와 유사한 기본 검은 화면 생성
            /// 실제 출력은 [Image Stretch="Uniform"] 설정에 따라 자동 맞춤
            /// </summary>
            BitmapSource blackBitmap =
                CreateBlackBitmap(
                    1280,
                    720);

            /// <summary>
            /// [UI Thread]에서 [Image Source] 초기화
            /// </summary>
            App.Current.Dispatcher.Invoke(() =>
            {
                EOCameraImage =
                    blackBitmap;

                IRCameraImage =
                    blackBitmap;
            });

        }

        #endregion

        #region [Video State Helpers]

        /// <summary>
        /// [EO / IR] 전체 영상 연결 여부 확인
        ///
        /// [VD] 로컬 테스트 영상은 현재 사용하지 않으므로
        /// 연결 상태 판단 대상에서 제외한다.
        /// </summary>
        private bool IsAllVideoConnected()
        {
            // 2026-08-14: A decoder can briefly report a transitional state while
            // its capture loop is still displaying frames. Treat two displayed
            // streams as an active session so a duplicate click cannot cancel it.
            return (_eoDecoder.IsOpened && _irDecoder.IsOpened) ||
                   (_isEoFrameDisplayed && _isIrFrameDisplayed);
        }

        /// <summary>
        /// 기존 [CancellationTokenSource] 정리 후
        /// 새 영상 루프 종료 토큰을 생성한다.
        /// </summary>
        private void ResetCancellationToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
        }


        #endregion

        #region [Video Open Helpers]

        /// <summary>
        /// [EO / IR] 영상 연결 시도
        ///
        /// 이 함수는 [Task.Run] 함수 내부에서 호출되어,
        /// [RTSP Open]으로 인한 [UI] 프리징을 방지한다.
        /// </summary>
        private async Task<VideoConnectResult> OpenVideoSourcesAsync()
        {
            ConsoleLogHelper.Info(
                "RTSP",
                "Sequential open started / ORDER=EO -> IR");

            bool eoResult =
                false;

            bool irResult =
                false;

            CancellationToken captureToken =
                _cts?.Token ?? CancellationToken.None;

            /*
             * [1] EO RTSP 우선 연결
             *
             * VertiportNexus의 MCB -> SCB 순차 연결 방식과 동일하게
             * 첫 번째 장비의 Connect 결과가 확정되기 전에는
             * 다음 장비 연결을 시작하지 않는다.
             */
            bool wasEoAlreadyOpen =
                _eoDecoder.IsOpened;

            eoResult =
                wasEoAlreadyOpen ||
                await Task.Run(() =>
                    _eoDecoder.Open(
                        EoSourceAddress));

            if (!_isDeviceConnectionRequested ||
                captureToken.IsCancellationRequested)
            {
                return new VideoConnectResult
                {
                    EoResult = false,
                    IrResult = false
                };

            }

            if (eoResult)
            {
                EoVideoWidth =
                    _eoDecoder.VideoWidth;

                EoVideoHeight =
                    _eoDecoder.VideoHeight;

                EoStatusText =
                    "[EO] Connected";

                /*
                 * EO 연결 완료 직후 Capture Loop를 먼저 시작한다.
                 *
                 * 기존처럼 EO / IR Open이 모두 끝날 때까지 기다리지 않으므로
                 * EO Connected 상태와 영상이 IR보다 먼저 화면에 반영된다.
                 */
                if (!wasEoAlreadyOpen)
                {
                    _ = Task.Run(() =>
                        FFmpegCaptureLoop(
                            _eoDecoder,
                            "EO",
                            bitmap =>
                            {
                                EOCameraImage =
                                    bitmap;

                                _isEoFrameDisplayed =
                                    true;
                            },
                            captureToken));
                }

            }
            else
            {
                EoStatusText =
                    "[EO] Connect Failed";
            }

            /*
             * EO 상태 변경 및 첫 Frame이 UI에 먼저 반영될 시간을 확보한다.
             *
             * 연결 순서 자체는 위 await가 보장하며, 이 대기시간은
             * VertiportNexus의 MCB -> SCB 연결 간격과 동일한 표시 목적이다.
             */
            try
            {
                await Task.Delay(
                    500,
                    captureToken);
            }
            catch (OperationCanceledException)
            {
                return new VideoConnectResult
                {
                    EoResult = eoResult,
                    IrResult = false
                };

            }

            if (!_isDeviceConnectionRequested ||
                captureToken.IsCancellationRequested)
            {
                return new VideoConnectResult
                {
                    EoResult = eoResult,
                    IrResult = false
                };

            }

            /*
             * [2] EO 연결 처리 완료 후 IR RTSP 연결
             */
            IrStatusText =
                "[IR] Connecting...";

            bool wasIrAlreadyOpen =
                _irDecoder.IsOpened;

            irResult =
                wasIrAlreadyOpen ||
                await Task.Run(() =>
                    _irDecoder.Open(
                        IrSourceAddress));

            if (!_isDeviceConnectionRequested ||
                captureToken.IsCancellationRequested)
            {
                return new VideoConnectResult
                {
                    EoResult = eoResult,
                    IrResult = false
                };

            }

            if (irResult)
            {
                IrVideoWidth =
                    _irDecoder.VideoWidth;

                IrVideoHeight =
                    _irDecoder.VideoHeight;

                IrStatusText =
                    "[IR] Connected";

                if (!wasIrAlreadyOpen)
                {
                    _ = Task.Run(() =>
                        FFmpegCaptureLoop(
                            _irDecoder,
                            "IR",
                            bitmap =>
                            {
                                IRCameraImage =
                                    bitmap;

                                _isIrFrameDisplayed =
                                    true;
                            },
                            captureToken));
                }

            }
            else
            {
                IrStatusText =
                    "[IR] Connect Failed";
            }

            return new VideoConnectResult
            {
                EoResult = eoResult,
                IrResult = irResult
            };

        }

        /// <summary>
        /// [EO / IR] 최초 연결 실패 Stream 자동 재연결 시작
        ///
        /// VertiportNexus의 RTSP Reconnect 흐름과 동일하게,
        /// 장비 전원 인가 직후 Camera가 아직 Ready 상태가 아닌 경우에도
        /// 연결 해제 요청 전까지 일정 간격으로 재시도한다.
        /// </summary>
        private void StartVideoReconnectLoops(
            VideoConnectResult result)
        {
            _videoReconnectCts?.Cancel();
            _videoReconnectCts?.Dispose();

            _videoReconnectCts =
                new CancellationTokenSource();

            CancellationToken token =
                _videoReconnectCts.Token;

            if (!result.EoResult)
            {
                _ = ReconnectVideoAsync(
                    _eoDecoder,
                    EoSourceAddress,
                    "EO",
                    bitmap =>
                    {
                        EOCameraImage = bitmap;
                        _isEoFrameDisplayed = true;
                    },
                    token);
            }

            if (!result.IrResult)
            {
                _ = ReconnectVideoAsync(
                    _irDecoder,
                    IrSourceAddress,
                    "IR",
                    bitmap =>
                    {
                        IRCameraImage = bitmap;
                        _isIrFrameDisplayed = true;
                    },
                    token);
            }

        }

        /// <summary>
        /// 개별 RTSP Stream 재연결 Loop
        ///
        /// 2026-08-18: EO / IR 중 전원이 꺼진 채널이 무한 재연결되지 않도록
        /// 채널별 최대 재연결 시간을 5분으로 제한한다. 제한 시간이 지나면
        /// 해당 채널만 Disconnected로 전환하며 다른 영상과 장비 제어는 유지한다.
        /// </summary>
        private async Task ReconnectVideoAsync(
            FFmpegDecoderService decoder,
            string sourceAddress,
            string streamName,
            Action<BitmapSource> setImageAction,
            CancellationToken token)
        {
            const int reconnectDelayMs =
                5000;

            TimeSpan maximumReconnectDuration =
                TimeSpan.FromMinutes(5);

            Stopwatch reconnectStopwatch =
                Stopwatch.StartNew();

            int retryCount =
                0;

            while (_isDeviceConnectionRequested &&
                   !token.IsCancellationRequested &&
                   !decoder.IsOpened &&
                   reconnectStopwatch.Elapsed < maximumReconnectDuration)
            {
                retryCount++;

                App.Current.Dispatcher.Invoke(() =>
                {
                    if (streamName == "EO")
                    {
                        EoStatusText =
                            $"[EO] Reconnecting... ({retryCount})";
                    }
                    else
                    {
                        IrStatusText =
                            $"[IR] Reconnecting... ({retryCount})";
                    }

                });

                Console.WriteLine(
                    $"[{streamName}] RTSP Reconnect Try : {retryCount}");

                bool connected =
                    await Task.Run(() =>
                        decoder.Open(
                            sourceAddress));

                if (connected)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (streamName == "EO")
                        {
                            EoVideoWidth = decoder.VideoWidth;
                            EoVideoHeight = decoder.VideoHeight;
                            EoStatusText = "[EO] Connected";
                        }
                        else
                        {
                            IrVideoWidth = decoder.VideoWidth;
                            IrVideoHeight = decoder.VideoHeight;
                            IrStatusText = "[IR] Connected";
                        }

                        /// <summary>
                        /// 최초 연결에는 실패했지만 Auto Reconnect로
                        /// EO 또는 IR 영상이 정상 연결된 경우에도
                        /// 중앙 십자선을 자동 활성화한다.
                        /// </summary>
                        IsCrosshairVisible =
                            true;

                    });

                    if (_cts != null &&
                        !token.IsCancellationRequested)
                    {
                        CancellationToken captureToken =
                            _cts.Token;

                        _ = Task.Run(() =>
                            FFmpegCaptureLoop(
                                decoder,
                                streamName,
                                setImageAction,
                                captureToken));
                    }

                    Console.WriteLine(
                        $"[{streamName}] RTSP Reconnect Success");

                    return;
                }

                try
                {
                    await Task.Delay(
                        reconnectDelayMs,
                        token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

            }

            reconnectStopwatch.Stop();

            if (!_isDeviceConnectionRequested ||
                token.IsCancellationRequested ||
                decoder.IsOpened)
            {
                return;
            }

            // 2026-08-18: 5분 재연결 제한에 도달한 채널만 최종 해제한다.
            decoder.Close();

            App.Current.Dispatcher.Invoke(() =>
            {
                if (streamName == "EO")
                {
                    EoStatusText =
                        "[EO] Disconnected";

                    _isEoFrameDisplayed =
                        false;
                }
                else
                {
                    IrStatusText =
                        "[IR] Disconnected";

                    _isIrFrameDisplayed =
                        false;
                }
            });

            Console.WriteLine(
                $"[{streamName}] RTSP Reconnect Timeout / " +
                $"ELAPSED={reconnectStopwatch.Elapsed.TotalSeconds:F1}s / " +
                $"COUNT={retryCount}");

        }

        #endregion

        #region [Video Result Helpers]

        /// <summary>
        /// 영상 연결 결과 [Console Log] 출력
        /// </summary>
        private void WriteVideoConnectLog(VideoConnectResult result)
        {
            Console.WriteLine(
                "[EO] "
                + (result.EoResult ? "Connect Success" : "Connect Failure"));

            Console.WriteLine(
                "[IR] "
                + (result.IrResult ? "Connect Success" : "Connect Failure"));

            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #endregion

        #region [Video Capture Loop]

        #region [FFmpeg Capture Loop]

        /// <summary>
        /// 해당 Stream의 Frame을 Dispatcher에 등록할 수 있는지 확인한다.
        ///
        /// 이미 이전 Frame이 UI 처리 대기 중이면 false를 반환하여
        /// 현재 Frame을 버린다.
        ///
        /// 이를 통해 Dispatcher Queue에 과거 Frame이 누적되어
        /// 영상이 늦게 따라오는 현상을 방지한다.
        /// </summary>
        private bool TryReserveFrameDispatch(
            string streamName)
        {
            if (streamName == "EO")
            {
                return Interlocked.CompareExchange(
                    ref _isEoFrameDispatchPending,
                    1,
                    0) == 0;
            }

            if (streamName == "IR")
            {
                return Interlocked.CompareExchange(
                    ref _isIrFrameDispatchPending,
                    1,
                    0) == 0;
            }

            return false;
        }

        /// <summary>
        /// 해당 Stream의 Dispatcher 예약 상태를 해제한다.
        /// </summary>
        private void ReleaseFrameDispatch(
            string streamName)
        {
            if (streamName == "EO")
            {
                Interlocked.Exchange(
                    ref _isEoFrameDispatchPending,
                    0);

                return;
            }

            if (streamName == "IR")
            {
                Interlocked.Exchange(
                    ref _isIrFrameDispatchPending,
                    0);
            }

        }

        /// <summary>
        /// Stream별 UI 반영 우선순위를 반환한다.
        ///
        /// EO 영상은 1920 x 1080이며 메인 화면에서 크게 표시되므로
        /// IR보다 높은 Render 우선순위를 적용한다.
        /// </summary>
        private DispatcherPriority GetFrameDispatcherPriority(
            string streamName)
        {
            if (streamName == "EO")
            {
                return DispatcherPriority.Render;
            }

            return DispatcherPriority.Background;
        }

        /// <summary>
        /// [FFmpeg] 기반 [RTSP] Frame 수신 Loop
        ///
        /// 처리 순서:
        /// 1. Decoder에서 Frame 획득
        /// 2. 해당 Stream의 UI Frame 등록 가능 여부 확인
        /// 3. Mat을 BitmapSource로 변환
        /// 4. BitmapSource Freeze
        /// 5. Dispatcher.BeginInvoke로 UI 반영 예약
        ///
        /// 기존 Dispatcher.Invoke는 UI 반영이 끝날 때까지
        /// Decode Thread를 정지시켰다.
        ///
        /// 현재 구조는 BeginInvoke를 사용하고,
        /// 이전 Frame이 UI 처리 중이면 중간 Frame을 버려
        /// 실시간성과 화면 부드러움을 우선한다.
        /// </summary>
        /// <param name="decoder">
        /// EO 또는 IR FFmpeg Decoder
        /// </param>
        /// <param name="streamName">
        /// EO / IR Stream 구분
        /// </param>
        /// <param name="setImageAction">
        /// EOCameraImage 또는 IRCameraImage 설정 함수
        /// </param>
        /// <param name="cancellationToken">
        /// 영상 수신 중지 Token
        /// </param>
        private void FFmpegCaptureLoop(
            FFmpegDecoderService decoder,
            string streamName,
            Action<BitmapSource> setImageAction,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Mat frame = null;

                try
                {
                    /// <summary>
                    /// FFmpeg Decoder에서 다음 Frame 획득
                    /// </summary>
                    frame =
                        decoder.ReadFrame();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    /// <summary>
                    /// Frame 수신 실패 시 짧게 대기 후 재시도
                    /// </summary>
                    if (frame == null ||
                        frame.Empty())
                    {
                        Thread.Sleep(5);

                        continue;
                    }

                    /*
                     * 이전 Frame이 아직 UI Dispatcher에서 처리 중이면
                     * 현재 Frame은 변환조차 하지 않고 버린다.
                     *
                     * 특히 EO 1920 x 1080 Frame의 Bitmap 변환 비용이 크므로,
                     * 불필요한 변환 작업을 줄이는 효과도 있다.
                     */
                    if (!TryReserveFrameDispatch(
                            streamName))
                    {
                        continue;
                    }

                    bool dispatchQueued =
                        false;

                    try
                    {
                        ThermalFireDetectionResult thermalResult =
                            default(ThermalFireDetectionResult);

                        if (streamName == "IR")
                        {
                            thermalResult =
                                _thermalFireDetectionService.Process(
                                    frame,
                                    IsThermalFireDetectionEnabled,
                                    ThermalHotThresholdRatio,
                                    ThermalMinimumAreaRatio,
                                    ThermalFireBoxGroupingMode);
                        }

                        /// <summary>
                        /// OpenCV Mat → WPF BitmapSource 변환
                        /// </summary>
                        BitmapSource bitmap =
                            MatToBitmapSourceConverter
                                .Convert(frame);

                        if (bitmap == null)
                        {
                            continue;
                        }

                        /*
                         * BitmapSource는 Worker Thread에서 생성된다.
                         *
                         * Freeze 처리하면 변경 불가능한 객체가 되어
                         * UI Thread에서 안전하게 참조할 수 있다.
                         */
                        if (bitmap.CanFreeze &&
                            !bitmap.IsFrozen)
                        {
                            bitmap.Freeze();
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        Dispatcher dispatcher =
                            App.Current?.Dispatcher;

                        if (dispatcher == null ||
                            dispatcher.HasShutdownStarted ||
                            dispatcher.HasShutdownFinished)
                        {
                            break;
                        }

                        /*
                         * EO:
                         * DispatcherPriority.Render
                         *
                         * IR:
                         * DispatcherPriority.Background
                         *
                         * 메인 고해상도 EO 영상의 화면 반영을
                         * 우선적으로 처리한다.
                         */
                        DispatcherPriority priority =
                            GetFrameDispatcherPriority(
                                streamName);

                        dispatcher.BeginInvoke(
                            priority,
                            new Action(() =>
                            {
                                try
                                {
                                    if (cancellationToken
                                        .IsCancellationRequested)
                                    {
                                        return;
                                    }

                                    if (dispatcher
                                            .HasShutdownStarted ||
                                        dispatcher
                                            .HasShutdownFinished)
                                    {
                                        return;
                                    }

                                    /// <summary>
                                    /// Binding 대상 영상 Property 갱신
                                    /// </summary>
                                    setImageAction(
                                        bitmap);

                                    if (streamName == "IR" &&
                                        thermalResult.StateChanged)
                                    {
                                        UpdateThermalFireCandidateState(
                                            thermalResult);
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(
                                        $"[{streamName}] " +
                                        $"[UI FRAME UPDATE ERROR] " +
                                        ex.Message);
                                }
                                finally
                                {
                                    /*
                                     * UI 반영이 완료되었으므로
                                     * 다음 Frame의 Dispatcher 등록을 허용한다.
                                     */
                                    ReleaseFrameDispatch(
                                        streamName);
                                }

                            }));

                        dispatchQueued =
                            true;
                    }
                    finally
                    {
                        /*
                         * BeginInvoke 등록 전에 예외, 취소 또는 종료가 발생하면
                         * Dispatcher Callback이 실행되지 않으므로
                         * 여기에서 예약 상태를 직접 해제한다.
                         */
                        if (!dispatchQueued)
                        {
                            ReleaseFrameDispatch(
                                streamName);
                        }

                    }

                }
                catch (Exception ex)
                {
                    if (!cancellationToken
                        .IsCancellationRequested)
                    {
                        ConsoleLogHelper.Error(
                            $"{streamName} VIDEO",
                            "FFmpeg Capture Error",
                            ex);
                    }

                    break;
                }
                finally
                {
                    frame?.Dispose();
                }

            }

            /*
             * Loop 종료 시 예약값이 남아 있지 않도록 초기화한다.
             */
            ReleaseFrameDispatch(
                streamName);

            ConsoleLogHelper.Info(
                $"{streamName} VIDEO",
                "FFmpeg Capture Loop End");
        }


        #region [Video Result Type]

        /// <summary>
        /// 영상 연결 결과 저장 구조체.
        ///
        /// EO/IR 연결 결과를 하나로 묶어 상태 표시, 자동 HOME 조건,
        /// 재연결 Loop 시작 여부 판단에 사용한다.
        /// </summary>
        private struct VideoConnectResult
        {
            public bool EoResult;
            public bool IrResult;
        }

        #endregion

        #endregion

        #endregion
    }

}
