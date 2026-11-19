using OpenCvSharp;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Converters;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>영상 연결과 프레임 처리에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IVideoFeatureContext
: IVideoPerformanceContext
    {
        Models.Main.RtspSourceOption ActiveEoCtecSource { get; set; }

        Models.Main.RtspSourceOption ConnectedEoCtecSource { get; set; }

        CancellationTokenSource ControlAgentReconnectCts { get; set; }

        Services.Communication.ICtecCameraResponseServicePort CtecCameraResponseService { get; }

        CancellationTokenSource Cts { get; set; }

        byte CurrentCtecEoFocusMode { get; set; }

        ushort CurrentCtecEoFocusPosition { get; set; }

        ushort CurrentCtecEoZoomPosition { get; set; }

        short CurrentEoFocus { get; set; }

        short CurrentEoZoom { get; set; }

        ushort CurrentIrFocus { get; set; }

        ushort CurrentIrZoom { get; set; }

        KeyboardPanTiltDirection CurrentKeyboardPanTiltDirection { get; set; }

        ContinuousMoveType CurrentMoveType { get; set; }

        double CurrentPan { get; set; }

        byte CurrentPowerStatus { get; set; }

        double CurrentTilt { get; set; }

        ref int DeviceConnectRequested { get; }

        IFFmpegDecoderServicePort EoDecoder { get; }

        ThermalFireDetectionService EoFireDetectionService { get; }

        ref long EoLensReceivedTicks { get; }

        IFFmpegDecoderServicePort IrDecoder { get; }

        ThermalFireDetectionService IrFireDetectionService { get; }

        ref long IrLensReceivedTicks { get; }

        bool IsDeviceConnectionRequested { get; set; }

        ref int IsEoFrameDispatchPending { get; }

        bool IsEoFrameDisplayed { get; set; }

        bool IsEoRtspAddressValid { get; set; }

        ref int IsIrFrameDispatchPending { get; }

        bool IsIrFrameDisplayed { get; set; }

        bool IsIrRtspAddressValid { get; set; }

        bool IsVideoConnecting { get; set; }

        Services.Communication.ILAPacketParserPort LaPacketParser { get; }

        ref double? LastPanAbsoluteTarget { get; }

        Services.Communication.ITcpClientServicePort LaTcpService { get; }

        string RequestedControlIp { get; set; }

        string RequestedControlPort { get; set; }

        ControlAgentProfileOption RequestedControlProfile { get; set; }

        string RequestedEoAddress { get; set; }

        string RequestedIrAddress { get; set; }

        long[] VideoCenterFrameTicks { get; }

        CancellationTokenSource VideoReconnectCts { get; set; }

        ref int VisionSettingsGeneration { get; }

        string AiPowerStatusText { get; set; }

        /// <summary>
        /// 2026-09-15: REI 통합본에서는 Web Agent 프로필에만 unsigned Pan 좌표를 적용한다.
        /// </summary>
        void ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile);

        /// <summary>
        /// 기존 VIA 0 Pan 이동 작업 취소
        /// </summary>
        void CancelMoveControlPanOperation();

        /// <summary>
        /// 구역 참조에 사용할 채널 영상 프레임을 안전하게 복사한다.
        /// </summary>
        Services.Position.ZoneFrameContext CaptureZoneFrame(string camera);

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다.
        /// </summary>
        void ClearActivePanTiltAbsoluteMove();

        /// <summary>
        /// Keyboard 방향키 입력 상태 초기화
        /// </summary>
        void ClearKeyboardPanTiltPressedState();

        /// <summary>
        /// [Control Agent] 제어 TCP 연결 기존 고흥 제어 구조는 유지하며, 운용 환경에 따라 연결 대상 IP / Port만 변경하여
        /// 사용한다.
        /// </summary>
        Task<bool> ConnectLaAsync();

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        /// <summary>
        /// [AI DISCONNECT] 버튼 기준 수동 연결 해제 자동 재연결 요청을 함께 중단하여 사용자가 다시 [AI CONNECT]를 누르기
        /// 전에는 AI Agent에 재접속하지 않는다.
        /// </summary>
        void DisconnectAiAgent();

        BitmapSource EOCameraImage { get; set; }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> EoDetectionBoxes { get; }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> EoFireSmokeDetectionBoxes { get; }

        string EoSourceAddress { get; set; }

        string EoStatusText { get; set; }

        int EoVideoHeight { get; set; }

        int EoVideoWidth { get; set; }

        /// <summary>
        /// 2026-09-02: AI FIRE/FLAME BBox를 영상처리 FIRE 중복 억제에 사용한다. Snapshot은 오래된 좌표가 남지
        /// 않도록 2초까지만 유효하다.
        /// </summary>
        IList<Rect> GetRecentAiFireCandidates(bool isInfrared);

        bool HasActiveDeviceSession { get; }

        string HomeZeroStatusText { get; set; }

        /// <summary>
        /// 장비 연결 후 IR 기본 팔레트 Black Hot 적용을 준비한다.
        /// </summary>
        void InitializeThermalBlackHotAfterDeviceConnected();

        /// <summary>
        /// 새 WEB AGENT 연결의 조회·상태 관리 세션을 초기화한다.
        /// </summary>
        void InitializeWebAgentSession();

        BitmapSource IRCameraImage { get; set; }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> IrDetectionBoxes { get; }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> IrFireSmokeDetectionBoxes { get; }

        string IrSourceAddress { get; set; }

        string IrStatusText { get; set; }

        int IrVideoHeight { get; set; }

        int IrVideoWidth { get; set; }

        bool IsCrosshairVisible { get; set; }

        bool IsDeviceSettingsLocked { get; }

        /// <summary>
        /// 2026-08-28: AUTO SCAN과 파노라마 촬영 중에도 FIRE/SMOKE 분석 및 이벤트 갱신을 유지하고, 일반 수동 이동 중에만
        /// 분석을 보류한다.
        /// </summary>
        bool IsFireSmokeFrameAnalysisAllowed();

        /// <summary>
        /// 지정한 영상 채널과 탐지 종류가 현재 사용 대상인지 확인한다.
        /// </summary>
        bool IsFireSourceEnabled(bool isInfrared);

        bool IsPanoramaCaptureRunning { get; set; }

        bool IsPresetScanRunning { get; set; }

        bool IsRooftopStatusSelected { get; }

        bool IsSmokeDetectionEnabled { get; set; }

        bool IsThermalFireDetectionEnabled { get; set; }

        /// <summary>
        /// 현재 설정된 Pan / Tilt 좌표계의 0°, 0° 위치로 절대 이동한다. 기존 LA 0xB1 명령은 기계식
        /// Homing(StartHoming) 명령이므로 카메라가 홈 센서 방향으로 이동한 뒤 해당 위치를 원점으로 잡는다. UI의 HOME
        /// POSITION은 사용자가 설정한 좌표 원점으로 복귀하는 기능이므로 Pan 0°(0x45), Tilt 0°(0x47) 절대 이동 명령을
        /// 사용한다.
        /// </summary>
        Task MoveHomePositionAsync();

        /// <summary>
        /// EO 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void NotifyEoCurrentStatusChanged(bool panChanged = true, bool tiltChanged = true, bool zoomChanged = true, bool focusChanged = true, bool powerChanged = true);

        /// <summary>
        /// IR 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void NotifyIrCurrentStatusChanged();

        string PresetCommandStatusText { get; set; }

        /// <summary>
        /// 현재 영상 프레임을 연기 탐지기로 분석하고 표시·이벤트 갱신에 전달한다.
        /// </summary>
        SmokeDetectionResult ProcessSmokeFrame(Mat frame, string streamName, Rect fireCandidateRect);

        /// <summary>
        /// 연결 세션 변경 시 이전 전원 근거와 진행 중 명령 상태를 초기화한다.
        /// </summary>
        void ResetDevicePowerStatus();

        /// <summary>
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다.
        /// </summary>
        void ResetDigitalZoomSession();

        /// <summary>
        /// 2026-08-27: 카메라 이동 또는 영상 조건 변경 뒤 EO/IR 기준 프레임과 시간축 후보를 함께 초기화하여 화면 전체 변화가 이벤트가
        /// 되지 않게 한다.
        /// </summary>
        void ResetFireSmokeFrameAnalysis(string reason);

        /// <summary>
        /// 현재 선택값을 다음 연결용 설정으로 저장하며 로그의 인증정보는 가린다.
        /// </summary>
        void SaveRtspCommunicationSettings();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        string SessionEoAddress { get; }

        string SessionIrAddress { get; }

        /// <summary>
        /// Control Agent TCP 연결 상태 UI 갱신
        /// </summary>
        void SetControlAgentConnectionStatus(string statusText, string statusColor, CancellationToken token = default(CancellationToken));

        /// <summary>
        /// 로컬 화재·연기 탐지의 사용 여부를 관련 상태에 반영한다.
        /// </summary>
        void SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke);

        /// <summary>
        /// 2026-08-31: 탐지 Overlay 적용 전 EO 프레임을 파노라마 전용 버퍼에 보관한다. Frozen Bitmap만 공유하여 수신
        /// Thread와 촬영 Thread 간 접근을 안전하게 한다.
        /// </summary>
        void SetRawEoPanoramaFrame(BitmapSource frame);

        /// <summary>
        /// 활성 세션을 변경하지 않고 중복 연결 안내만 갱신한다.
        /// </summary>
        void ShowAlreadyConnected();

        /// <summary>
        /// 현재 선택된 EO 프리셋 기준으로 CTEC Response TCP Port 9000 연결 시작 옥상 MR300 EO CTEC 직접 제어
        /// 프리셋이 아니면 기존 Response 연결을 종료하고 별도 TCP 연결을 생성하지 않는다.
        /// </summary>
        Task StartSelectedEoCtecResponseAsync();

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position 조회 종료
        /// </summary>
        void StopCtecEoPositionPolling();

        /// <summary>
        /// 진행 중인 Focus Sync 확인 작업과 장비 이동을 정지한다.
        /// </summary>
        Task StopFocusSyncAsync(bool stopEo = true, bool stopIr = true);

        /// <summary>
        /// 경로 실행 취소를 요청하고 진행 중 이동을 정리한다.
        /// </summary>
        void StopRoute();

        /// <summary>
        /// 진행 중인 줌 동기화를 취소하고 관련 렌즈 이동을 중지한다.
        /// </summary>
        Task StopZoomSyncAsync();

        int ThermalFireBoxGroupingMode { get; }

        double ThermalHotThresholdRatio { get; set; }

        double ThermalMinimumAreaRatio { get; set; }

        /// <summary>
        /// 통신 설정 탭에서 선택된 EO / IR RTSP 주소 검증 선택값이 없거나 rtsp / rtsps 형식이 아닌 주소는 실제 FFmpeg 연결
        /// 전에 차단한다.
        /// </summary>
        bool TryGetRtspEndpoints(out string eoRtspAddress, out string irRtspAddress);

        /// <summary>
        /// 2026-09-17: 로컬 FIRE/SMOKE 분석 결과를 영상 오버레이 컬렉션에 반영한다. 이벤트 목록만 갱신되고 영상 BBox가 누락되던
        /// 경로를 복구하며, AI Agent BBox 컬렉션은 변경하지 않는다.
        /// </summary>
        void UpdateFireSmokeDetectionOverlay(string camera, ThermalFireDetectionResult thermalResult, SmokeDetectionResult smokeResult);

        /// <summary>
        /// 열상 화재 후보의 위치·확정 상태를 표시와 이벤트 판정에 반영한다.
        /// </summary>
        void UpdateThermalFireCandidateState(string camera, ThermalFireDetectionResult result, Services.Position.ZoneFrameContext zoneFrame = null);

        /// <summary>
        /// 로컬 영상 후보와 기존 추적 이벤트를 대응시켜 생성·유지·해제를 반영한다.
        /// </summary>
        void UpdateVisionBBoxEvents(string camera, string detectionType, IList<Rect> candidates, IList<double> candidateScores, string detectionSource, Services.Position.ZoneFrameContext zoneFrame = null);
    }

    /// <summary>영상 연결과 프레임 처리 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class VideoFeature
    {

        #region [협력 객체와 초기화]

        private readonly IVideoFeatureContext _context;

        /// <summary>
        /// 영상 연결·프레임 처리 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal VideoFeature(IVideoFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));

            _performanceRecorder = new VideoPerformanceRecorder(context);
        }

        /// <summary>
        /// 기능 객체가 사용하는 초기 상태·목록·타이머를 준비한다.
        /// </summary>
        internal void InitializeState()
        {
            _eoCaptureTask = Task.CompletedTask;
            _irCaptureTask = Task.CompletedTask;
        }

        #endregion

        #region [상태 필드]

        internal readonly DetectionFrameSlot _eoDetectionSlot = new DetectionFrameSlot();

        internal readonly DetectionFrameSlot _irDetectionSlot = new DetectionFrameSlot();

        internal readonly DisplayFrameSlot _eoDisplaySlot = new DisplayFrameSlot();

        internal readonly DisplayFrameSlot _irDisplaySlot = new DisplayFrameSlot();

        internal RtspChannelPerformance EoRtspPerformance { get { return _performanceRecorder._eoRtspPerformance; } }

        internal RtspChannelPerformance IrRtspPerformance { get { return _performanceRecorder._irRtspPerformance; } }

        internal readonly SemaphoreSlim _deviceConnectionLifecycleLock = new SemaphoreSlim(1, 1);

        internal long _deviceSessionGeneration;

        internal int _deviceDisconnectRequested;

        internal Task _eoCaptureTask;

        internal Task _irCaptureTask;

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 장비 연결 버튼의 진입점이다. 중복 연결·해제 요청을 확인하고 연결 생명주기 잠금 안에서 제어 TCP와 EO/IR RTSP 연결을 시작한다.
        /// 지연될 수 있는 영상 열기는 내부 비동기 연결 경로에서 처리한다.
        /// </summary>
        internal async void Connect()
        {
            if (Volatile.Read(ref _deviceDisconnectRequested) != 0 || _context.IsDeviceSettingsLocked || Interlocked.CompareExchange(ref _context.DeviceConnectRequested, 1, 0) != 0)
            {
                _context.ShowAlreadyConnected();

                return;
            }

            await _deviceConnectionLifecycleLock.WaitAsync();

            long session = Interlocked.Increment(ref _deviceSessionGeneration);

            try
            {
                ConsoleLogHelper.State("DEVICE CONNECT", $"SESSION={session} / STATE=Connecting");

                await ConnectCoreAsync(session);
            }
            catch (OperationCanceledException)
            {
                ConsoleLogHelper.State("DEVICE CONNECT", $"SESSION={session} / RESULT=CANCELED");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("DEVICE CONNECT", $"SESSION={session} / RESULT=FAULTED", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _context.DeviceConnectRequested, 0);

                _deviceConnectionLifecycleLock.Release();
            }
        }

        /// <summary>
        /// 고정한 연결 대상과 세션 번호로 제어 TCP·EO/IR 영상을 연결하고 초기 동작을 시작한다.
        /// </summary>
        internal async Task ConnectCoreAsync(long session)
        {
            if (_context.HasActiveDeviceSession)
            {
                _context.ShowAlreadyConnected();

                return;
            }

            /*
             * TODO(COMMAND-NEXT):
             * 현재 XAML Command가 RelayCommand(Action)에 연결되어 있어 async void를 유지한다.
             * 향후 ConnectCommand를 AsyncRelayCommand로 통일할 때 Task 반환형 ConnectAsync로 변경한다.
             * 그 전까지 예외는 App.xaml.cs의 전역 예외 로그에서 확인한다.
             */
            ConsoleLogHelper.CommandSection("DEVICE CONNECT", "Connect requested", string.Empty, $"CONTROL : {_context.ControlAgentIp}:{_context.ControlAgentPortText}", $"EO      : {ConsoleLogHelper.MaskRtspPassword(_context.EoSourceAddress)}", $"IR      : {ConsoleLogHelper.MaskRtspPassword(_context.IrSourceAddress)}");

            // 현재 [Connect] 시도 중이면
            // 중복 [Connect] 입력 무시
            //
            if (_context.IsVideoConnecting)
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
            if (!_context.TryGetRtspEndpoints(out string eoRtspAddress, out string irRtspAddress))
            {
                return;
            }

            /*
             * Trim 처리된 검증 완료 주소를
             * 실제 영상 연결 주소로 다시 반영한다.
             */
            _context.EoSourceAddress = eoRtspAddress;
            _context.IrSourceAddress = irRtspAddress;

            // 2026-09-08: 검증을 통과한 직접 입력/프리셋 주소를 다음 실행에 복원한다.
            _context.SaveRtspCommunicationSettings();

            // 재시도 연결 주소를 고정하고 선택 상자 변경은 다음 명시적 연결에 적용한다.
            _context.RequestedControlProfile = _context.SelectedControlAgentProfile;
            _context.RequestedControlIp = _context.ControlAgentIp;
            _context.RequestedControlPort = _context.ControlAgentPortText;
            _context.RequestedEoAddress = eoRtspAddress;
            _context.RequestedIrAddress = irRtspAddress;

            // 이전 연결 중 선택한 다음 장비는 아직 이전 좌표계를 사용했을 수 있다.
            // 새 TCP 수신과 자동 HOME 전에 송신·수신 좌표계를 새 요청에 맞춘다.
            _context.ApplyControlAgentPanCoordinateMode(_context.RequestedControlProfile);

            // 2026-08-18: 한 채널만 연결된 상태에서 다시 연결해도
            // 정상 채널의 프레임과 CaptureLoop는 유지하고 실패 채널만 초기화한다.
            bool isEoStreamActive = _context.EoDecoder.IsOpened || _context.IsEoFrameDisplayed;

            bool isIrStreamActive = _context.IrDecoder.IsOpened || _context.IsIrFrameDisplayed;

            if (_context.IsEoRtspAddressValid && !isEoStreamActive)
            {
                _context.IsEoFrameDisplayed = false;
            }

            if (_context.IsIrRtspAddressValid && !isIrStreamActive)
            {
                _context.IsIrFrameDisplayed = false;
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                if (!isEoStreamActive)
                {
                    _context.EoDetectionBoxes.Clear();

                    _context.EoFireSmokeDetectionBoxes.Clear();

                    _context.SetLocalFireSmokeDetectionState(0, false, false);
                }

                if (!isIrStreamActive)
                {
                    _context.IrDetectionBoxes.Clear();

                    _context.IrFireSmokeDetectionBoxes.Clear();

                    _context.SetLocalFireSmokeDetectionState(1, false, false);
                }
            });

            _context.IsVideoConnecting = true; // 연결 시도 중 상태 설정

            /*
             * [VD] 로컬 테스트 영상 연결 기능 임시 비활성화
             *
             * 현재 실제 [EO / IR] RTSP 영상만 사용하므로
             * VD 연결 상태는 갱신하지 않는다.
             */
            if (_context.IsEoRtspAddressValid && !isEoStreamActive)
            {
                _context.EoStatusText = "[EO] Connecting...";
            }

            /*
             * 최초 RTSP 연결은 EO를 우선 처리한다.
             *
             * EO 연결 결과가 확정되고 영상 수신 Loop가 시작된 뒤
             * IR 연결을 시작하므로, 초기 상태 표시 순서가
             * EO -> IR 순서로 명확하게 유지된다.
             */
            if (_context.IsIrRtspAddressValid && !isIrStreamActive)
            {
                _context.IrStatusText = "[IR] Waiting for EO...";
            }

            try
            {
                // 2026-08-18: 살아 있는 채널의 CaptureLoop가 사용하는 Token은
                // 취소하지 않는다. 최초 연결 또는 명시적 연결 해제 후에만 새로 만든다.
                if (_context.Cts == null || _context.Cts.IsCancellationRequested)
                {
                    ResetCancellationToken();
                }

                _context.IsDeviceConnectionRequested = true;

                // [Control Agent] 최초 연결을 바로 시도한다.
                //
                // 최초 연결에 실패하더라도 내부 Auto Reconnect Loop가
                // 일정 간격으로 연결을 다시 시도한다.
                //
                bool isControlAgentConnected = await _context.ConnectLaAsync();

                if (isControlAgentConnected)
                {
                    _context.InitializeWebAgentSession();

                    _context.InitializeThermalBlackHotAfterDeviceConnected();
                }

                // 선택된 EO 카메라가 [옥상 MR300 CTEC] 직접 제어 장비이면
                // 카메라 IP의 [TCP Port 9000] 응답 수신 연결을 시작한다.
                //
                // CGI 명령 송신과 TCP 응답 수신은 서로 다른 통로이며,
                // Port 9000 연결은 명령 처리 결과 및 위치 조회 응답 수신에 사용한다.
                //
                await _context.StartSelectedEoCtecResponseAsync();

                /*
                 * AI Detector 연결은 하단 AI CONNECT 버튼의 독립 흐름에서 수행한다.
                 * 장비 연결 버튼은 Control Agent와 EO/IR RTSP 연결만 담당한다.
                 */
                /*
                 * 로컬 VD/OpenCV 테스트 경로는 현재 제품 흐름에서 사용하지 않아 제거했다.
                 * 영상 연결은 EO -> IR 순서의 FFmpeg RTSP 경로만 사용한다.
                 */
                VideoConnectResult result = await OpenVideoSourcesAsync();

                if (!_context.IsDeviceConnectionRequested || _context.Cts == null || _context.Cts.IsCancellationRequested)
                {
                    await Task.WhenAll(Task.Run(() => _context.EoDecoder.Close()), Task.Run(() => _context.IrDecoder.Close()));

                    return;
                }

                // EO / IR 개별 상태 Console 출력
                WriteVideoConnectLog(result);

                /*
                 * LA/WEB AGENT 최초 장비 연결 완료 후 HOME POSITION 자동 실행
                 *
                 * Vertiport 운용 흐름과 동일하게 다음 순서를 보장한다.
                 *
                 * 1. Control Agent TCP 연결
                 * 2. EO/IR 두 채널이 모두 연결된 경우에만 HOME POSITION 실행
                 *
                 * 한 채널만 연결된 상태에서는 자동 HOME을 실행하지 않는다.
                 * 필요한 경우 운용자가 연결 복구 후 HOME 버튼을 직접 누른다.
                 *
                 * LA/WEB 모두 수동 HOME과 같은 Pan 0° / Tilt 0° 절대 이동을 사용한다.
                 * 기계식 원점 탐색이나 ZERO 설정은 실행하지 않는다.
                 * 자동 영상 재연결 경로에서는 이 블록을 호출하지 않는다.
                 *
                 * HOME 진행 중에는 수동 HOME과 동일하게
                 * 전체 우측 UI, 탭, 버튼, 방향키, WASD, Zoom/Focus 단축키가 잠기며,
                 * 정상 완료/송신 실패/예외/30초 Timeout 후 반드시 자동 해제된다.
                 */
                bool areBothCamerasConnected = result.EoResult && result.IrResult;

                if (isControlAgentConnected && _context.LaTcpService.IsConnected && session == Interlocked.Read(ref _deviceSessionGeneration) && areBothCamerasConnected)
                {
                    ConsoleLogHelper.Info("CONNECT / AUTO HOME", $"Eligibility passed / CONTROL AGENT + EO and IR connected / MODE={(_context.IsRooftopStatusSelected ? "LA AGENT" : "WEB AGENT")}");

                    Console.WriteLine();

                    Console.WriteLine($"[CONNECT] {(_context.IsRooftopStatusSelected ? "LA AGENT" : "WEB AGENT")} + EO/IR VIDEO AVAILABLE " + "/ AUTO HOME POSITION START");

                    ConsoleLogHelper.PrintLine();

                    await _context.MoveHomePositionAsync();

                    ConsoleLogHelper.State("CONNECT / AUTO HOME", $"Completed / STATUS={_context.HomeZeroStatusText}");

                    Console.WriteLine();

                    Console.WriteLine($"[CONNECT] {(_context.IsRooftopStatusSelected ? "LA AGENT" : "WEB AGENT")} AUTO HOME POSITION END " + $"/ STATUS={_context.HomeZeroStatusText}");

                    ConsoleLogHelper.PrintLine();
                }
                else
                {
                    string autoHomeSkipReason = $"CONTROL_AGENT_CONNECTED={isControlAgentConnected} " + $"/ EO_CONNECTED={result.EoResult} " + $"/ IR_CONNECTED={result.IrResult} " + $"/ MODE={(_context.IsRooftopStatusSelected ? "LA AGENT" : "WEB AGENT")}";

                    ConsoleLogHelper.Info("CONNECT / AUTO HOME", "Skipped / " + autoHomeSkipReason);

                    Console.WriteLine();

                    Console.WriteLine("[CONNECT] AUTO HOME POSITION SKIPPED / " + autoHomeSkipReason);

                    ConsoleLogHelper.PrintLine();
                }

                // [EO / IR] 영상 연결 성공 시 중앙 십자선 자동 활성화
                //
                // 프로그램 최초 실행 상태에서는 십자선을 숨기고,
                // EO 또는 IR RTSP 영상이 하나라도 정상 연결된 시점에
                // 운용자가 중심 기준점을 바로 확인할 수 있도록 자동 표시한다.
                //
                // 자동 활성화는 연결 성공 시 한 번만 수행하며,
                // 이후에는 [DISPLAY OVERLAY] 버튼을 통한 수동 조작값을 유지한다.
                //
                if (result.EoResult || result.IrResult)
                {
                    _context.IsCrosshairVisible = true;
                }

                /*
                 * 최초 연결에 실패한 영상은 자동 재연결 시작
                 */
                StartVideoReconnectLoops(result);

                /*
                 * 둘 다 최초 연결에 실패했더라도
                 * 자동 재연결은 계속 수행한다.
                 */
                if (!result.EoResult && !result.IrResult)
                {
                    Console.WriteLine("[VIDEO] EO / IR All Connect Failed. " + "Reconnect Loop Started.");

                    ConsoleLogHelper.PrintLine();
                }

                // [AI Detector] 다중 객체 [Bounding Box] 표시 테스트
                //
                // 실제 [AI Detector Agent] 연결 전,
                // 더미 탐지 결과를 이용하여 [Overlay] 표시 상태를 확인한다.
                // 테스트 완료 후 주석 처리한다.
                //
                //
            }
            finally
            {
                _context.IsVideoConnecting = false;
            }
        }

        /// <summary>
        /// 영상 연결 해제 함수 1. [CaptureLoop] 종료 요청 2. [VD__VideoCapture] 해제 3. [FFmpeg] [EO /
        /// IR] [RTSP] Decoder 해제 4. [상태 문자열 갱신]
        /// </summary>
        internal async void Disconnect()
        {
            // 중복 종료 요청을 무시하고 이전 비동기 콜백은 즉시 무효화한다.
            if (Interlocked.CompareExchange(ref _deviceDisconnectRequested, 1, 0) != 0)
            {
                return;
            }

            Interlocked.Increment(ref _deviceSessionGeneration);

            _context.IsDeviceConnectionRequested = false;

            _context.ResetDigitalZoomSession();

            _context.ControlAgentReconnectCts?.Cancel();

            _context.VideoReconnectCts?.Cancel();

            // 2026-10-01: native RTSP 자원 정리가 끝날 때까지 기존 Connected/Waiting
            // 문구가 남지 않도록 해제 요청을 UI에 즉시 표시한다.
            _context.EoStatusText = "[EO] Disconnecting...";
            _context.IrStatusText = "[IR] Disconnecting...";

            // 2026-11-03 정비: 명시적 연결 해제는 전원 명령 대기도 즉시 취소한다.
            // 네이티브 자원 정리 동안 WAIT 잠금이 남지 않도록 상태를 먼저 초기화한다.
            _context.ResetDevicePowerStatus();

            _context.SetControlAgentConnectionStatus("Disconnecting...", "#FFD166");

            _context.Cts?.Cancel();

            await _deviceConnectionLifecycleLock.WaitAsync();

            long session = Interlocked.Increment(ref _deviceSessionGeneration);

            try
            {
                ConsoleLogHelper.State("DEVICE DISCONNECT", $"SESSION={session} / STATE=Disconnecting");

                await DisconnectCoreAsync(session);

                ConsoleLogHelper.State("DEVICE DISCONNECT", $"SESSION={session} / CONTROL_RX_STOPPED=True / TCP_DISPOSED=True / " + "EO_RTSP_DISPOSED=True / IR_RTSP_DISPOSED=True / CTS_DISPOSED=True / " + "FRAME_STATE_RESET=True / RESULT=COMPLETE");
            }
            catch (OperationCanceledException)
            {
                ConsoleLogHelper.State("DEVICE DISCONNECT", $"SESSION={session} / RESULT=CANCELED");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("DEVICE DISCONNECT", $"SESSION={session} / RESULT=FAULTED", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _deviceDisconnectRequested, 0);

                _deviceConnectionLifecycleLock.Release();
            }
        }

        /// <summary>
        /// 현재 세션의 입력·수신·표시 작업을 정리하고 장비 연결을 해제한다.
        /// </summary>
        internal async Task DisconnectCoreAsync(long session)
        {
            _context.StopRoute();

            ConsoleLogHelper.Command("DEVICE CONNECT", "Disconnect requested");

            /*
             * 2026-08-21: 상위 장비 연결 해제는 현재 연결된 AI Agent도 함께 종료한다.
             * AI 연결 시작은 기존 정책대로 AI CONNECT 버튼에서만 수행한다.
             */
            _context.DisconnectAiAgent();

            Console.WriteLine("[VIDEO] Disconnect Try...");

            Console.WriteLine();

            // 연결 시도 / 자동 재연결 진행 중에도
            // 사용자가 즉시 연결 해제할 수 있도록 모든 Token을 먼저 종료한다.
            //
            _context.IsDeviceConnectionRequested = false;

            _context.ControlAgentReconnectCts?.Cancel();

            _context.VideoReconnectCts?.Cancel();

            /*
             * CTEC EO Zoom / Focus 버튼을 누르고 있던 상태에서
             * Disconnect하더라도 CGI Inquiry가 계속 송신되지 않도록 종료한다.
             */
            _context.StopCtecEoPositionPolling();

            // 진행 중인 이동 제어 VIA 0 Pan 작업 종료
            //
            _context.CancelMoveControlPanOperation();

            // 진행 중인 EO / IR Zoom Sync 작업 종료
            //
            await _context.StopZoomSyncAsync();

            // 진행 중인 EO / IR Focus Sync 작업 종료
            //
            await _context.StopFocusSyncAsync();

            // 1. 먼저 [Loop] 종료 요청
            _context.Cts?.Cancel();

            Interlocked.Exchange(ref _context.IsEoFrameDispatchPending, 0);

            Interlocked.Exchange(ref _context.IsIrFrameDispatchPending, 0);

            // 2-1. [EO] 영상 표시 상태 초기화
            //
            _context.IsEoFrameDisplayed = false;

            // 2-2. [IR] 영상 표시 상태 초기화
            //
            _context.IsIrFrameDisplayed = false;

            /*
             * [VD] 로컬 테스트 영상 연결 기능 비활성화
             *
             * 현재 VD Decoder를 Open하지 않으므로
             * Release 처리도 함께 비활성화한다.
             */
            await Task.WhenAll(Task.Run(() => _context.EoDecoder.Close()), Task.Run(() => _context.IrDecoder.Close()));

            await WaitForCaptureWorkersAsync();

            // [Control Agent] 제어 TCP 연결 해제
            //
            await _context.LaTcpService.DisconnectAsync();

            // [옥상 MR300 EO] CTEC Response TCP Port 9000 연결 해제
            //
            _context.CtecCameraResponseService.Stop();

            _context.ConnectedEoCtecSource = null;
            _context.RequestedControlProfile = null;

            _context.RequestedControlIp = _context.RequestedControlPort = _context.RequestedEoAddress = _context.RequestedIrAddress = null;

            _context.ActiveEoCtecSource = null;
            _context.CurrentCtecEoZoomPosition = 0;
            _context.CurrentCtecEoFocusPosition = 0;
            _context.CurrentCtecEoFocusMode = 0;

            _context.SetControlAgentConnectionStatus("Disconnected", "#FF6B6B");

            // 장비 연결 해제 시 중앙 십자선 비활성화
            //
            // 다음 연결 전까지 검은 화면에 십자선이 남지 않도록
            // 기본 상태인 [DISABLED]로 초기화한다.
            //
            _context.IsCrosshairVisible = false;

            // [CURRENT STATUS] 상태값 초기화
            //
            // Control Agent 연결 해제 후에는
            // 마지막 수신 상태값이 화면에 남지 않도록 초기화한다.
            //
            _context.CurrentPan = 0.0;
            _context.LastPanAbsoluteTarget = null;

            _context.ClearActivePanTiltAbsoluteMove();

            _context.IsPresetScanRunning = false;
            _context.PresetCommandStatusText = "CONTROL AGENT DISCONNECTED";
            _context.CurrentTilt = 0.0;
            _context.CurrentEoZoom = 0;
            _context.CurrentEoFocus = 0;
            _context.CurrentIrZoom = 0;
            _context.CurrentIrFocus = 0;

            System.Threading.Interlocked.Exchange(ref _context.EoLensReceivedTicks, 0);

            System.Threading.Interlocked.Exchange(ref _context.IrLensReceivedTicks, 0);

            // CONTROL AGENT TCP 연결에서 남아 있을 수 있는
            // 분할 수신 Packet Buffer 초기화
            //
            _context.LaPacketParser.Reset();

            _context.CurrentPowerStatus = 0x00;

            // 2026-10-06: 재연결 전에는 0x31 상태를 OFF로 오인하지 않는다.
            _context.ResetDevicePowerStatus();

            _context.CurrentMoveType = ContinuousMoveType.None;

            _context.ClearKeyboardPanTiltPressedState();

            _context.CurrentKeyboardPanTiltDirection = KeyboardPanTiltDirection.None;

            // CURRENT STATUS UI Binding 갱신
            //
            _context.NotifyEoCurrentStatusChanged();

            _context.NotifyIrCurrentStatusChanged();

            // 4. [UI] [Thread]에서 마지막으로 검은 화면 덮어쓰기
            App.Current.Dispatcher.Invoke(() =>
            {
                ClearVideoView(); // [VD] / [EO] / [IR] Viewer 화면을 검은 화면으로 초기화

                // [EO / IR] [AI Detector] 탐지 결과 초기화
                //
                // 영상 연결 해제 상태에서는
                // 검은 화면 위에 [Bounding Box]가 표시되지 않도록 한다.
                //
                _context.EoDetectionBoxes.Clear();

                _context.IrDetectionBoxes.Clear();

                _context.EoFireSmokeDetectionBoxes.Clear();

                _context.IrFireSmokeDetectionBoxes.Clear();

                _context.SetLocalFireSmokeDetectionState(0, false, false);

                _context.SetLocalFireSmokeDetectionState(1, false, false);

                _context.EoStatusText = "Disconnected";
                _context.IrStatusText = "Disconnected";
            });

            ResetLatestVideoFrameState();

            _context.Cts?.Dispose();

            _context.Cts = null;

            // 5. [VIDEO] 연결 해제 완료 [Log] 출력
            Console.WriteLine("[VIDEO] Disconnect Complete.");

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 최근 표시·검출 프레임 상태를 비워 이전 세션 영상이 새 연결에 남지 않게 한다.
        /// </summary>
        internal void ResetLatestVideoFrameState()
        {
            ResetDisplayFrameSlot(_eoDisplaySlot);

            ResetDisplayFrameSlot(_irDisplaySlot);

            ResetDetectionFrameSlot(_eoDetectionSlot);

            ResetDetectionFrameSlot(_irDetectionSlot);
        }

        /// <summary>
        /// 영상 취득 작업의 종료를 기다려 디코더 자원 해제와 읽기 작업의 충돌을 피한다.
        /// </summary>
        internal async Task WaitForCaptureWorkersAsync()
        {
            Task eoDisplayWorker;

            Task irDisplayWorker;

            lock (_eoDisplaySlot.Sync)
            {
                eoDisplayWorker = _eoDisplaySlot.BufferedWorkerTask ?? Task.CompletedTask;
            }

            lock (_irDisplaySlot.Sync)
            {
                irDisplayWorker = _irDisplaySlot.BufferedWorkerTask ?? Task.CompletedTask;
            }

            Task workers = Task.WhenAll(_eoCaptureTask ?? Task.CompletedTask, _irCaptureTask ?? Task.CompletedTask, eoDisplayWorker, irDisplayWorker);

            Task completed = await Task.WhenAny(workers, Task.Delay(2000));

            ConsoleLogHelper.State("DEVICE DISCONNECT", $"EO_CAPTURE_STOPPED={_eoCaptureTask == null || _eoCaptureTask.IsCompleted} / " + $"IR_CAPTURE_STOPPED={_irCaptureTask == null || _irCaptureTask.IsCompleted} / " + $"EO_DISPLAY_STOPPED={eoDisplayWorker.IsCompleted} / " + $"IR_DISPLAY_STOPPED={irDisplayWorker.IsCompleted} / " + $"WAIT_TIMEOUT={completed != workers}");
        }

        /// <summary>
        /// 표시용 최신 프레임 보관 슬롯을 초기화하고 소유한 버퍼를 정리한다.
        /// </summary>
        internal static void ResetDisplayFrameSlot(DisplayFrameSlot slot)
        {
            lock (slot.Sync)
            {
                slot.LatestFrame?.Dispose();

                slot.LatestFrame = null;

                slot.ReusableFrame?.Dispose();

                slot.ReusableFrame = null;

                while (slot.BufferedFrames.Count > 0)
                {
                    slot.BufferedFrames.Dequeue().Frame?.Dispose();
                }

                while (slot.BufferedReusableFrames.Count > 0)
                {
                    slot.BufferedReusableFrames.Dequeue()?.Dispose();
                }

                slot.BufferedWorkerTask = Task.CompletedTask;
                slot.Bitmap = null;
                slot.LatestFrameDecodedTicks = 0;
                slot.LastOverlayVersion = 0;

                Interlocked.Exchange(ref slot.OverlayDispatchPending, 0);

                Interlocked.Exchange(ref slot.WorkerRunning, 0);

                Interlocked.Exchange(ref slot.BufferedWorkerRunning, 0);
            }
        }

        /// <summary>
        /// 검출용 최신 프레임 보관 슬롯을 초기화하고 이전 결과를 정리한다.
        /// </summary>
        internal static void ResetDetectionFrameSlot(DetectionFrameSlot slot)
        {
            lock (slot.Sync)
            {
                slot.LatestFrame?.Dispose();

                slot.LatestFrame = null;

                slot.ReusableFrame?.Dispose();

                slot.ReusableFrame = null;

                slot.ThermalResult = default(ThermalFireDetectionResult);

                slot.LatestZoneFrame = null;
                slot.ResultZoneFrame = null;

                slot.SmokeResult = default(SmokeDetectionResult);

                slot.ResultVersion = 0;

                Interlocked.Exchange(ref slot.WorkerRunning, 0);
            }
        }

        /// <summary>
        /// 지정한 크기의 검은색 [BitmapSource] 생성 [Disconnect] 시 기존 마지막 프레임이 남지 않도록 [Viewer] 화면을
        /// 검은 화면으로 초기화할 때 사용
        /// </summary>
        internal BitmapSource CreateBlackBitmap(int width, int height)
        {
            // [BGR24] 기준 1픽셀당 [3byte]
            // 전체 [byte] 배열을 0으로 유지하면 검은색 화면이 된다.
            //
            int stride = width * 3;

            byte[] pixels = new byte[height * stride];

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null, pixels, stride);

            bitmap.Freeze();

            return bitmap;
        }

        /// <summary>
        /// [VD] / [EO] / [IR] [Viewer] 화면 초기화 [C++]에서 [Disconnect] 시 [View]를 검은 화면으로
        /// [Clear] 하던 것과 동일한 목적
        /// </summary>
        internal void ClearVideoView()
        {
            // 현재 [Viewer] 크기와 유사한 기본 검은 화면 생성
            // 실제 출력은 [Image Stretch="Uniform"] 설정에 따라 자동 맞춤
            //
            BitmapSource blackBitmap = CreateBlackBitmap(1280, 720);

            // [UI Thread]에서 [Image Source] 초기화
            //
            App.Current.Dispatcher.Invoke(() =>
{
    _context.EOCameraImage = blackBitmap;
    _context.IRCameraImage = blackBitmap;
});
        }

        /// <summary>
        /// 기존 [CancellationTokenSource] 정리 후 새 영상 루프 종료 토큰을 생성한다.
        /// </summary>
        internal void ResetCancellationToken()
        {
            _context.Cts?.Cancel();

            _context.Cts?.Dispose();

            _context.Cts = new CancellationTokenSource();
        }

        /// <summary>
        /// [EO / IR] 영상 연결 시도 이 함수는 [Task.Run] 함수 내부에서 호출되어, [RTSP Open]으로 인한 [UI] 프리징을
        /// 방지한다.
        /// </summary>
        internal async Task<VideoConnectResult> OpenVideoSourcesAsync()
        {
            ConsoleLogHelper.Info("RTSP", "Sequential open started / ORDER=EO -> IR");

            bool eoResult = false;

            bool irResult = false;

            CancellationToken captureToken = _context.Cts?.Token ?? CancellationToken.None;

            /*
             * [1] EO RTSP 우선 연결
             *
             * VertiportNexus의 MCB -> SCB 순차 연결 방식과 동일하게
             * 첫 번째 장비의 Connect 결과가 확정되기 전에는
             * 다음 장비 연결을 시작하지 않는다.
             */
            bool wasEoAlreadyOpen = _context.EoDecoder.IsOpened;

            eoResult = _context.IsEoRtspAddressValid && (wasEoAlreadyOpen || await Task.Run(() => _context.EoDecoder.Open(_context.SessionEoAddress, captureToken)));

            if (!_context.IsDeviceConnectionRequested || captureToken.IsCancellationRequested)
            {
                return new VideoConnectResult
                {
                    EoResult = false,
                    IrResult = false
                };
            }

            if (eoResult)
            {
                _context.EoVideoWidth = _context.EoDecoder.VideoWidth;
                _context.EoVideoHeight = _context.EoDecoder.VideoHeight;
                _context.EoStatusText = "[EO] Waiting Frame...";

                /*
                 * EO 연결 완료 직후 Capture Loop를 먼저 시작한다.
                 *
                 * 기존처럼 EO / IR Open이 모두 끝날 때까지 기다리지 않으므로
                 * EO Connected 상태와 영상이 IR보다 먼저 화면에 반영된다.
                 */
                if (!wasEoAlreadyOpen)
                {
                    _eoCaptureTask = Task.Run(() => FFmpegCaptureLoop(_context.EoDecoder, _context.SessionEoAddress, "EO", bitmap =>
                    {
                        _context.EOCameraImage = bitmap;
                        _context.IsEoFrameDisplayed = true;
                    }, captureToken));
                }
            }
            else if (_context.IsEoRtspAddressValid)
            {
                _context.EoStatusText = _context.EoDecoder.IsAuthenticationFailure ? "[EO] RTSP 인증 실패 (401)" : "[EO] Connect Failed";
            }

            /*
             * EO 상태 변경 및 첫 Frame이 UI에 먼저 반영될 시간을 확보한다.
             *
             * 연결 순서 자체는 위 await가 보장하며, 이 대기시간은
             * VertiportNexus의 MCB -> SCB 연결 간격과 동일한 표시 목적이다.
             */
            try
            {
                await Task.Delay(500, captureToken);
            }
            catch (OperationCanceledException)
            {
                return new VideoConnectResult
                {
                    EoResult = eoResult,
                    IrResult = false
                };
            }

            if (!_context.IsDeviceConnectionRequested || captureToken.IsCancellationRequested)
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
            if (_context.IsIrRtspAddressValid)
            {
                _context.IrStatusText = "[IR] Connecting...";
            }

            bool wasIrAlreadyOpen = _context.IrDecoder.IsOpened;

            irResult = _context.IsIrRtspAddressValid && (wasIrAlreadyOpen || await Task.Run(() => _context.IrDecoder.Open(_context.SessionIrAddress, captureToken)));

            if (!_context.IsDeviceConnectionRequested || captureToken.IsCancellationRequested)
            {
                return new VideoConnectResult
                {
                    EoResult = eoResult,
                    IrResult = false
                };
            }

            if (irResult)
            {
                _context.IrVideoWidth = _context.IrDecoder.VideoWidth;
                _context.IrVideoHeight = _context.IrDecoder.VideoHeight;
                _context.IrStatusText = "[IR] Waiting Frame...";

                if (!wasIrAlreadyOpen)
                {
                    _irCaptureTask = Task.Run(() => FFmpegCaptureLoop(_context.IrDecoder, _context.SessionIrAddress, "IR", bitmap =>
                    {
                        _context.IRCameraImage = bitmap;
                        _context.IsIrFrameDisplayed = true;
                    }, captureToken));
                }
            }
            else if (_context.IsIrRtspAddressValid)
            {
                _context.IrStatusText = _context.IrDecoder.IsAuthenticationFailure ? "[IR] RTSP 인증 실패 (401)" : "[IR] Connect Failed";
            }

            return new VideoConnectResult
            {
                EoResult = eoResult,
                IrResult = irResult
            };
        }

        /// <summary>
        /// [EO / IR] 최초 연결 실패 Stream 자동 재연결 시작 VertiportNexus의 RTSP Reconnect 흐름과 동일하게,
        /// 장비 전원 인가 직후 Camera가 아직 Ready 상태가 아닌 경우에도 연결 해제 요청 전까지 일정 간격으로 재시도한다.
        /// </summary>
        internal void StartVideoReconnectLoops(VideoConnectResult result)
        {
            _context.VideoReconnectCts?.Cancel();

            _context.VideoReconnectCts?.Dispose();

            _context.VideoReconnectCts = new CancellationTokenSource();

            CancellationToken token = _context.VideoReconnectCts.Token;

            if (_context.IsEoRtspAddressValid && !result.EoResult)
            {
                _ = ReconnectVideoAsync(_context.EoDecoder, _context.SessionEoAddress, "EO", bitmap =>
                {
                    _context.EOCameraImage = bitmap;
                    _context.IsEoFrameDisplayed = true;
                }, token);
            }

            if (_context.IsIrRtspAddressValid && !result.IrResult)
            {
                _ = ReconnectVideoAsync(_context.IrDecoder, _context.SessionIrAddress, "IR", bitmap =>
                {
                    _context.IRCameraImage = bitmap;
                    _context.IsIrFrameDisplayed = true;
                }, token);
            }
        }

        /// <summary>
        /// 개별 RTSP Stream 재연결 Loop 2026-08-18: EO / IR 중 전원이 꺼진 채널이 무한 재연결되지 않도록 채널별 최대
        /// 재연결 시간을 5분으로 제한한다. 제한 시간이 지나면 해당 채널만 Disconnected로 전환하며 다른 영상과 장비 제어는 유지한다.
        /// </summary>
        internal async Task ReconnectVideoAsync(IFFmpegDecoderServicePort decoder, string sourceAddress, string streamName, Action<BitmapSource> setImageAction, CancellationToken token)
        {
            long session = Interlocked.Read(ref _deviceSessionGeneration);

            const int reconnectDelayMs = 5000;

            TimeSpan maximumReconnectDuration = TimeSpan.FromMinutes(5);

            Stopwatch reconnectStopwatch = Stopwatch.StartNew();

            int retryCount = 0;

            while (_context.IsDeviceConnectionRequested && session == Interlocked.Read(ref _deviceSessionGeneration) && !token.IsCancellationRequested && !decoder.IsOpened && reconnectStopwatch.Elapsed < maximumReconnectDuration)
            {
                retryCount++;

                App.Current.Dispatcher.Invoke(() =>
                {
                    if (token.IsCancellationRequested || session != Interlocked.Read(ref _deviceSessionGeneration))
                    {
                        return;
                    }

                    if (streamName == "EO")
                    {
                        _context.EoStatusText = $"[EO] Reconnecting... ({retryCount})";
                    }
                    else
                    {
                        _context.IrStatusText = $"[IR] Reconnecting... ({retryCount})";
                    }
                });

                Console.WriteLine($"[{streamName}] RTSP Reconnect Try : {retryCount}");

                bool connected = await Task.Run(() => decoder.Open(sourceAddress, token));

                if (token.IsCancellationRequested || !_context.IsDeviceConnectionRequested || session != Interlocked.Read(ref _deviceSessionGeneration))
                {
                    return;
                }

                if (connected)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested || session != Interlocked.Read(ref _deviceSessionGeneration))
                        {
                            return;
                        }

                        if (streamName == "EO")
                        {
                            _context.EoVideoWidth = decoder.VideoWidth;
                            _context.EoVideoHeight = decoder.VideoHeight;
                            _context.EoStatusText = "[EO] Waiting Frame...";
                        }
                        else
                        {
                            _context.IrVideoWidth = decoder.VideoWidth;
                            _context.IrVideoHeight = decoder.VideoHeight;
                            _context.IrStatusText = "[IR] Waiting Frame...";
                        }

                        // 최초 연결에는 실패했지만 Auto Reconnect로
                        // EO 또는 IR 영상이 정상 연결된 경우에도
                        // 중앙 십자선을 자동 활성화한다.
                        //
                        _context.IsCrosshairVisible = true;
                    });

                    if (_context.Cts != null && !token.IsCancellationRequested)
                    {
                        CancellationToken captureToken = _context.Cts.Token;

                        Task captureTask = Task.Run(() => FFmpegCaptureLoop(decoder, sourceAddress, streamName, setImageAction, captureToken));

                        if (streamName == "EO")
                        {
                            _eoCaptureTask = captureTask;
                        }
                        else
                        {
                            _irCaptureTask = captureTask;
                        }
                    }

                    Console.WriteLine($"[{streamName}] RTSP Reconnect Success");

                    return;
                }

                if (decoder.IsAuthenticationFailure)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested || session != Interlocked.Read(ref _deviceSessionGeneration))
                        {
                            return;
                        }

                        if (streamName == "EO")
                        {
                            _context.EoStatusText = "[EO] RTSP 인증 실패 (401)";
                        }
                        else
                        {
                            _context.IrStatusText = "[IR] RTSP 인증 실패 (401)";
                        }
                    });
                }

                try
                {
                    await Task.Delay(reconnectDelayMs, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            reconnectStopwatch.Stop();

            if (!_context.IsDeviceConnectionRequested || session != Interlocked.Read(ref _deviceSessionGeneration) || token.IsCancellationRequested || decoder.IsOpened)
            {
                return;
            }

            // 2026-08-18: 5분 재연결 제한에 도달한 채널만 최종 해제한다.
            decoder.Close();

            App.Current.Dispatcher.Invoke(() =>
            {
                if (token.IsCancellationRequested || session != Interlocked.Read(ref _deviceSessionGeneration))
                {
                    return;
                }

                if (streamName == "EO")
                {
                    _context.EoStatusText = "[EO] Disconnected";
                    _context.IsEoFrameDisplayed = false;
                }
                else
                {
                    _context.IrStatusText = "[IR] Disconnected";
                    _context.IsIrFrameDisplayed = false;
                }
            });

            Console.WriteLine($"[{streamName}] RTSP Reconnect Timeout / " + $"ELAPSED={reconnectStopwatch.Elapsed.TotalSeconds:F1}s / " + $"COUNT={retryCount}");
        }

        /// <summary>
        /// 영상 연결 결과 [Console Log] 출력
        /// </summary>
        internal void WriteVideoConnectLog(VideoConnectResult result)
        {
            Console.WriteLine("[EO] " + (result.EoResult ? "Connect Success" : "Connect Failure"));

            Console.WriteLine("[IR] " + (result.IrResult ? "Connect Success" : "Connect Failure"));

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 해당 Stream의 Frame을 Dispatcher에 등록할 수 있는지 확인한다. 이미 이전 Frame이 UI 처리 대기 중이면 false를
        /// 반환하여 현재 Frame을 버린다. 이를 통해 Dispatcher Queue에 과거 Frame이 누적되어 영상이 늦게 따라오는 현상을
        /// 방지한다.
        /// </summary>
        internal bool TryReserveFrameDispatch(string streamName)
        {
            if (streamName == "EO")
            {
                return Interlocked.CompareExchange(ref _context.IsEoFrameDispatchPending, 1, 0) == 0;
            }

            if (streamName == "IR")
            {
                return Interlocked.CompareExchange(ref _context.IsIrFrameDispatchPending, 1, 0) == 0;
            }

            return false;
        }

        /// <summary>
        /// 해당 Stream의 Dispatcher 예약 상태를 해제한다.
        /// </summary>
        internal void ReleaseFrameDispatch(string streamName)
        {
            if (streamName == "EO")
            {
                Interlocked.Exchange(ref _context.IsEoFrameDispatchPending, 0);

                return;
            }

            if (streamName == "IR")
            {
                Interlocked.Exchange(ref _context.IsIrFrameDispatchPending, 0);
            }
        }

        /// <summary>
        /// Stream별 UI 반영 우선순위를 반환한다. EO 영상은 1920 x 1080이며 메인 화면에서 크게 표시되므로 IR보다 높은 Render
        /// 우선순위를 적용한다.
        /// </summary>
        internal DispatcherPriority GetFrameDispatcherPriority(string streamName)
        {
            if (streamName == "EO")
            {
                return DispatcherPriority.Render;
            }

            return DispatcherPriority.Background;
        }

        /// <summary>
        /// EO/IR 채널에 해당하는 검출 프레임 보관 슬롯을 선택한다.
        /// </summary>
        internal DetectionFrameSlot GetDetectionSlot(string streamName)
        {
            if (streamName == "EO")
            {
                return _eoDetectionSlot;
            }
            else
            {
                return _irDetectionSlot;
            }
        }

        /// <summary>
        /// EO/IR 채널에 해당하는 표시 프레임 보관 슬롯을 선택한다.
        /// </summary>
        internal DisplayFrameSlot GetDisplaySlot(string streamName)
        {
            if (streamName == "EO")
            {
                return _eoDisplaySlot;
            }
            else
            {
                return _irDisplaySlot;
            }
        }

        /// <summary>
        /// EO/IR 채널별 수신·표시 성능 측정 상태를 선택한다.
        /// </summary>
        internal RtspChannelPerformance GetRtspPerformance(string streamName)
        { return _performanceRecorder.GetRtspPerformance(streamName); }

        /// <summary>
        /// 연속 표시 프레임 사이의 시간 간격을 성능 통계에 기록한다.
        /// </summary>
        internal static void RecordDisplayInterval(RtspChannelPerformance performance, long intervalTicks)
        { VideoPerformanceRecorder.RecordDisplayInterval(performance, intervalTicks); }

        /// <summary>
        /// 환경변수의 정수 값을 읽고 허용 범위 밖이면 안전한 기본값 또는 제한값을 사용한다.
        /// </summary>
        internal static int ReadBoundedEnvironmentInteger(string variableName, int defaultValue, int minimum, int maximum)
        {
            string value = Environment.GetEnvironmentVariable(variableName);

            if (!int.TryParse(value, out int parsed))
            {
                return defaultValue;
            }

            return Math.Max(minimum, Math.Min(maximum, parsed));
        }

        /// <summary>
        /// 표시 프레임에 필요한 크기의 버퍼를 풀에서 확보한다.
        /// </summary>
        internal static Mat RentDisplayFrameBuffer(DisplayFrameSlot slot)
        {
            lock (slot.Sync)
            {
                Mat buffer = slot.ReusableFrame;

                slot.ReusableFrame = null;

                return buffer ?? new Mat();
            }
        }

        /// <summary>
        /// 표시가 끝난 프레임 버퍼를 풀로 돌려 재사용한다.
        /// </summary>
        internal static void ReturnDisplayFrameBuffer(DisplayFrameSlot slot, Mat frame)
        {
            if (frame == null)
            {
                return;
            }

            lock (slot.Sync)
            {
                if (slot.ReusableFrame == null)
                {
                    slot.ReusableFrame = frame;
                }
                else
                {
                    frame.Dispose();
                }
            }
        }

        /// <summary>
        /// 검출 프레임에 필요한 버퍼를 풀에서 확보한다.
        /// </summary>
        internal static Mat RentDetectionFrameBuffer(DetectionFrameSlot slot)
        {
            lock (slot.Sync)
            {
                Mat buffer = slot.ReusableFrame;

                slot.ReusableFrame = null;

                return buffer ?? new Mat();
            }
        }

        /// <summary>
        /// 검출이 끝난 프레임 버퍼를 풀로 돌려 재사용한다.
        /// </summary>
        internal static void ReturnDetectionFrameBuffer(DetectionFrameSlot slot, Mat frame)
        {
            if (frame == null)
            {
                return;
            }

            lock (slot.Sync)
            {
                if (slot.ReusableFrame == null)
                {
                    slot.ReusableFrame = frame;
                }
                else
                {
                    frame.Dispose();
                }
            }
        }

        /// <summary>
        /// 디코더 프레임을 표시용 버퍼에 복사해 화면 작업과 원본 버퍼 수명을 분리한다.
        /// </summary>
        internal static void CopyDisplayFrame(string streamName, Mat sourceFrame, Mat destinationFrame)
        {
            if (streamName == "EO" && EoDisplayMaximumWidth > 0 && sourceFrame.Width > EoDisplayMaximumWidth)
            {
                int targetHeight = Math.Max(1, (int)Math.Round(sourceFrame.Height * (EoDisplayMaximumWidth / (double)sourceFrame.Width)));

                Cv2.Resize(sourceFrame, destinationFrame, new OpenCvSharp.Size(EoDisplayMaximumWidth, targetHeight), 0, 0, InterpolationFlags.Linear);

                return;
            }

            sourceFrame.CopyTo(destinationFrame);
        }

        /// <summary>
        /// 지연 표시 대기열에서 사용할 프레임 버퍼를 확보한다.
        /// </summary>
        internal static Mat RentBufferedDisplayFrameBuffer(DisplayFrameSlot slot)
        {
            lock (slot.Sync)
            {
                if (slot.BufferedReusableFrames.Count > 0)
                {
                    return slot.BufferedReusableFrames.Dequeue();
                }
                else
                {
                    return new Mat();
                }
            }
        }

        /// <summary>
        /// 지연 표시 대기열에서 사용한 프레임 버퍼를 반환한다.
        /// </summary>
        internal static void ReturnBufferedDisplayFrameBuffer(DisplayFrameSlot slot, Mat frame)
        {
            if (frame == null)
            {
                return;
            }

            lock (slot.Sync)
            {
                if (slot.BufferedReusableFrames.Count < EoJitterBufferCapacity + 1)
                {
                    slot.BufferedReusableFrames.Enqueue(frame);
                }
                else
                {
                    frame.Dispose();
                }
            }
        }

        /// <summary>
        /// 옥상 EO 표시 전용 제한형 지터 버퍼에 Frame을 넣는다. 최대 개수를 넘으면 가장 오래된 Frame부터 폐기하므로 지연이 계속
        /// 누적되거나 수 초 뒤의 영상이 표시되지 않는다.
        /// </summary>
        internal void QueueEoBufferedDisplayFrame(string streamName, Mat sourceFrame, Action<BitmapSource> setImageAction, CancellationToken cancellationToken)
        {
            DisplayFrameSlot slot = GetDisplaySlot(streamName);

            RtspChannelPerformance performance = GetRtspPerformance(streamName);

            // 프레임 통계는 시간값만 보관해 매 프레임 Stopwatch 객체 할당을 피한다.
            long copyTimerStartedAt = Stopwatch.GetTimestamp();

            Mat displayFrame = RentBufferedDisplayFrameBuffer(slot);

            try
            {
                CopyDisplayFrame(streamName, sourceFrame, displayFrame);
            }
            catch
            {
                ReturnBufferedDisplayFrameBuffer(slot, displayFrame);

                throw;
            }

            long copyTimerElapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - copyTimerStartedAt);

            BufferedDisplayFrame droppedFrame = null;

            int bufferDepth;

            lock (slot.Sync)
            {
                if (slot.BufferedFrames.Count >= EoJitterBufferCapacity)
                {
                    droppedFrame = slot.BufferedFrames.Dequeue();
                }

                slot.BufferedFrames.Enqueue(new BufferedDisplayFrame { Frame = displayFrame, DecodedTicks = Stopwatch.GetTimestamp() });

                bufferDepth = slot.BufferedFrames.Count;
            }

            if (droppedFrame != null)
            {
                ReturnBufferedDisplayFrameBuffer(slot, droppedFrame.Frame);
            }

            lock (performance.Sync)
            {
                performance.FrameCopyCount++;

                performance.FrameCopyTicks += copyTimerElapsedTicks;

                performance.FrameCopyMaxTicks = Math.Max(performance.FrameCopyMaxTicks, copyTimerElapsedTicks);

                performance.JitterBufferCurrentDepth = bufferDepth;

                performance.JitterBufferMaximumDepth = Math.Max(performance.JitterBufferMaximumDepth, bufferDepth);

                if (droppedFrame != null)
                {
                    performance.DisplayDropped++;

                    performance.JitterBufferDropped++;
                }
            }

            if (Interlocked.CompareExchange(ref slot.BufferedWorkerRunning, 1, 0) == 0)
            {
                Task workerTask = Task.Run(() => RunEoBufferedDisplayLoopAsync(streamName, slot, setImageAction, cancellationToken));

                lock (slot.Sync)
                {
                    slot.BufferedWorkerTask = workerTask;
                }
            }
        }

        /// <summary>
        /// 현재 지연 표시 대기열에 쌓인 프레임 수를 확인한다.
        /// </summary>
        internal static int GetBufferedFrameCount(DisplayFrameSlot slot)
        {
            lock (slot.Sync)
            {
                return slot.BufferedFrames.Count;
            }
        }

        /// <summary>
        /// EO 표시 시작에 필요한 초기 프레임을 모으되 세션 취소를 확인한다.
        /// </summary>
        internal async Task WaitForInitialEoDisplayBufferAsync(DisplayFrameSlot slot, CancellationToken cancellationToken)
        {
            Stopwatch wait = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested && GetBufferedFrameCount(slot) < EoJitterBufferCapacity && wait.ElapsedMilliseconds < 200)
            {
                await Task.Delay(2, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 목표 표시 시각까지 비동기로 기다려 표시 주기를 맞춘다.
        /// </summary>
        internal static async Task DelayUntilDisplayTickAsync(long targetTicks, CancellationToken cancellationToken)
        {
            while (true)
            {
                long remainingTicks = targetTicks - Stopwatch.GetTimestamp();

                if (remainingTicks <= 0)
                {
                    return;
                }

                int delayMilliseconds = Math.Max(1, (int)Math.Floor(remainingTicks * 1000.0 / Stopwatch.Frequency));

                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// EO 프레임 대기열을 순서대로 표시하며 취소 시 남은 버퍼를 정리한다.
        /// </summary>
        internal async Task RunEoBufferedDisplayLoopAsync(string streamName, DisplayFrameSlot slot, Action<BitmapSource> setImageAction, CancellationToken cancellationToken)
        {
            try
            {
                await WaitForInitialEoDisplayBufferAsync(slot, cancellationToken).ConfigureAwait(false);

                long nextDisplayTicks = Stopwatch.GetTimestamp();

                while (!cancellationToken.IsCancellationRequested)
                {
                    BufferedDisplayFrame bufferedFrame = null;

                    int bufferDepth;

                    lock (slot.Sync)
                    {
                        if (slot.BufferedFrames.Count > 0)
                        {
                            bufferedFrame = slot.BufferedFrames.Dequeue();
                        }

                        bufferDepth = slot.BufferedFrames.Count;
                    }

                    RtspChannelPerformance performance = GetRtspPerformance(streamName);

                    lock (performance.Sync)
                    {
                        performance.JitterBufferCurrentDepth = bufferDepth;

                        if (bufferedFrame == null)
                        {
                            performance.JitterBufferUnderrun++;
                        }
                    }

                    if (bufferedFrame != null)
                    {
                        try
                        {
                            await RenderBufferedDisplayFrameAsync(streamName, slot, bufferedFrame, setImageAction, cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            ReturnBufferedDisplayFrameBuffer(slot, bufferedFrame.Frame);
                        }
                    }

                    nextDisplayTicks += EoJitterDisplayIntervalTicks;

                    long nowTicks = Stopwatch.GetTimestamp();

                    if (nowTicks > nextDisplayTicks + EoJitterDisplayIntervalTicks)
                    {
                        // UI가 한 주기 이상 멈춘 경우 밀린 Tick을 연속 실행하지 않는다.
                        nextDisplayTicks = nowTicks;
                    }

                    await DelayUntilDisplayTickAsync(nextDisplayTicks, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 연결 해제 시 정상 종료 경로이다.
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("RTSP DISPLAY " + streamName, "Bounded jitter buffer worker failed", ex);
            }
            finally
            {
                while (true)
                {
                    BufferedDisplayFrame remainingFrame;

                    lock (slot.Sync)
                    {
                        remainingFrame = slot.BufferedFrames.Count > 0 ? slot.BufferedFrames.Dequeue() : null;
                    }

                    if (remainingFrame == null)
                    {
                        break;
                    }

                    ReturnBufferedDisplayFrameBuffer(slot, remainingFrame.Frame);
                }

                Interlocked.Exchange(ref slot.BufferedWorkerRunning, 0);
            }
        }

        /// <summary>
        /// 실제로 표시한 프레임 시각을 기록해 정지 영상과 최신 영상을 구분한다.
        /// </summary>
        internal void ConfirmDisplayedVideoFrame(string streamName)
        {
            bool isEo = string.Equals(streamName, "EO", StringComparison.OrdinalIgnoreCase);

            // 오래되거나 멈춘 표시 프레임으로 클릭 이동을 수행하지 않는다.
            Interlocked.Exchange(ref _context.VideoCenterFrameTicks[isEo ? 0 : 1], DateTime.UtcNow.Ticks);

            string connected = isEo ? "[EO] Connected" : "[IR] Connected";

            if ((isEo ? _context.EoStatusText : _context.IrStatusText) == connected)
            {
                return;
            }

            if (isEo)
            {
                _context.EoStatusText = connected;
            }
            else
            {
                _context.IrStatusText = connected;
            }

            ConsoleLogHelper.State("DEVICE CONNECT", $"SESSION={Interlocked.Read(ref _deviceSessionGeneration)} / {streamName}_DISPLAY_FRAME=True / {streamName}_STREAMING=True");
        }

        /// <summary>
        /// 대기열 프레임을 화면 스레드에서 표시하고 사용한 버퍼를 반환한다.
        /// </summary>
        internal async Task RenderBufferedDisplayFrameAsync(string streamName, DisplayFrameSlot slot, BufferedDisplayFrame bufferedFrame, Action<BitmapSource> setImageAction, CancellationToken cancellationToken)
        {
            Dispatcher dispatcher = App.Current?.Dispatcher;

            if (cancellationToken.IsCancellationRequested || dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                return;
            }

            long dispatchQueuedTicks = Stopwatch.GetTimestamp();

            DispatcherOperation operation = dispatcher.InvokeAsync(new Action(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                long displayTicks = Stopwatch.GetTimestamp();

                // 프레임 통계는 시간값만 보관해 매 프레임 Stopwatch 객체 할당을 피한다.
                long bitmapTimerStartedAt = Stopwatch.GetTimestamp();

                slot.Bitmap = MatToBitmapSourceConverter.UpdateOrCreate(bufferedFrame.Frame, slot.Bitmap);

                long bitmapTimerElapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - bitmapTimerStartedAt);

                // 재연결 시 비워진 Image Source에 재사용 Bitmap을 다시 연결한다.
                setImageAction(slot.Bitmap);

                ConfirmDisplayedVideoFrame(streamName);

                RtspChannelPerformance performance = GetRtspPerformance(streamName);

                lock (performance.Sync)
                {
                    performance.Displayed++;

                    performance.ConvertCount++;

                    performance.ConvertTicks += bitmapTimerElapsedTicks;

                    performance.DispatchCount++;

                    performance.DispatchTicks += displayTicks - dispatchQueuedTicks;

                    performance.DispatchMaxTicks = Math.Max(performance.DispatchMaxTicks, displayTicks - dispatchQueuedTicks);

                    performance.FrameAgeCount++;

                    performance.FrameAgeTicks += Math.Max(0, displayTicks - bufferedFrame.DecodedTicks);

                    performance.FrameAgeMaxTicks = Math.Max(performance.FrameAgeMaxTicks, Math.Max(0, displayTicks - bufferedFrame.DecodedTicks));

                    if (performance.LastDisplayTicks > 0)
                    {
                        long intervalTicks = displayTicks - performance.LastDisplayTicks;

                        RecordDisplayInterval(performance, intervalTicks);
                    }

                    performance.LastDisplayTicks = displayTicks;
                }

                QueueLatestOverlayUpdate(streamName, slot, cancellationToken);

            }), GetFrameDispatcherPriority(streamName));

            await operation.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// 2026-09-23: 디코더가 만든 최신 프레임 한 장과 UI Render 예약 한 건만 유지한다. 별도 시간 지연은 두지 않고 프레임 도착
        /// 즉시 표시하며, UI가 늦어질 때만 대기 중인 과거 프레임을 최신 프레임으로 교체한다. 따라서 Dispatcher Queue나 영상 지연은
        /// 누적되지 않는다.
        /// </summary>
        internal void QueueLatestDisplayFrame(string streamName, Mat sourceFrame, Action<BitmapSource> setImageAction, CancellationToken cancellationToken)
        {
            DisplayFrameSlot slot = GetDisplaySlot(streamName);

            RtspChannelPerformance performance = GetRtspPerformance(streamName);

            long decodedTicks = Stopwatch.GetTimestamp();

            // 프레임 통계는 시간값만 보관해 매 프레임 Stopwatch 객체 할당을 피한다.
            long copyTimerStartedAt = Stopwatch.GetTimestamp();

            Mat displayFrame = RentDisplayFrameBuffer(slot);

            try
            {
                CopyDisplayFrame(streamName, sourceFrame, displayFrame);
            }
            catch
            {
                ReturnDisplayFrameBuffer(slot, displayFrame);

                throw;
            }

            long copyTimerElapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - copyTimerStartedAt);

            lock (performance.Sync)
            {
                performance.FrameCopyCount++;

                performance.FrameCopyTicks += copyTimerElapsedTicks;

                performance.FrameCopyMaxTicks = Math.Max(performance.FrameCopyMaxTicks, copyTimerElapsedTicks);
            }

            Mat replacedFrame;

            lock (slot.Sync)
            {
                replacedFrame = slot.LatestFrame;

                slot.LatestFrame = displayFrame;
                slot.LatestFrameDecodedTicks = decodedTicks;
            }

            if (replacedFrame != null)
            {
                ReturnDisplayFrameBuffer(slot, replacedFrame);

                lock (performance.Sync)
                {
                    performance.DisplayDropped++;

                    performance.DisplayQueueReplaced++;
                }
            }

            if (Interlocked.CompareExchange(ref slot.WorkerRunning, 1, 0) == 0)
            {
                ScheduleLatestFrameRender(streamName, slot, setImageAction, cancellationToken);
            }
        }

        /// <summary>
        /// 화면 갱신이 누적되지 않도록 최신 프레임 표시 작업을 예약한다.
        /// </summary>
        internal void ScheduleLatestFrameRender(string streamName, DisplayFrameSlot slot, Action<BitmapSource> setImageAction, CancellationToken cancellationToken)
        {
            Dispatcher dispatcher = App.Current?.Dispatcher;

            if (cancellationToken.IsCancellationRequested || dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                AbortLatestFrameRender(slot);

                return;
            }

            long dispatchQueuedTicks = Stopwatch.GetTimestamp();

            try
            {
                dispatcher.BeginInvoke(GetFrameDispatcherPriority(streamName), new Action(() =>
                {
                    Mat frame = null;

                    long decodedTicks = 0;

                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        lock (slot.Sync)
                        {
                            frame = slot.LatestFrame;
                            decodedTicks = slot.LatestFrameDecodedTicks;

                            slot.LatestFrame = null;
                            slot.LatestFrameDecodedTicks = 0;
                        }

                        if (frame == null)
                        {
                            return;
                        }

                        long displayTicks = Stopwatch.GetTimestamp();

                        // 프레임 통계는 시간값만 보관해 매 프레임 Stopwatch 객체 할당을 피한다.
                        long bitmapTimerStartedAt = Stopwatch.GetTimestamp();

                        slot.Bitmap = MatToBitmapSourceConverter.UpdateOrCreate(frame, slot.Bitmap);

                        long bitmapTimerElapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - bitmapTimerStartedAt);

                        setImageAction(slot.Bitmap);

                        ConfirmDisplayedVideoFrame(streamName);

                        RtspChannelPerformance perf = GetRtspPerformance(streamName);

                        lock (perf.Sync)
                        {
                            perf.Displayed++;

                            perf.ConvertCount++;

                            perf.ConvertTicks += bitmapTimerElapsedTicks;

                            perf.DispatchCount++;

                            perf.DispatchTicks += displayTicks - dispatchQueuedTicks;

                            perf.DispatchMaxTicks = Math.Max(perf.DispatchMaxTicks, displayTicks - dispatchQueuedTicks);

                            perf.FrameAgeCount++;

                            perf.FrameAgeTicks += Math.Max(0, displayTicks - decodedTicks);

                            perf.FrameAgeMaxTicks = Math.Max(perf.FrameAgeMaxTicks, Math.Max(0, displayTicks - decodedTicks));

                            if (perf.LastDisplayTicks > 0)
                            {
                                long intervalTicks = displayTicks - perf.LastDisplayTicks;

                                RecordDisplayInterval(perf, intervalTicks);
                            }

                            perf.LastDisplayTicks = displayTicks;
                        }

                        QueueLatestOverlayUpdate(streamName, slot, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        ConsoleLogHelper.Error("RTSP DISPLAY " + streamName, "Event-driven render failed", ex);
                    }
                    finally
                    {
                        ReturnDisplayFrameBuffer(slot, frame);

                        CompleteLatestFrameRender(streamName, slot, setImageAction, cancellationToken);
                    }
                }));
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("RTSP DISPLAY " + streamName, "Render scheduling failed", ex);

                AbortLatestFrameRender(slot);
            }
        }

        /// <summary>
        /// 예약된 프레임 표시 작업을 취소하고 사용 자원을 정리한다.
        /// </summary>
        internal static void AbortLatestFrameRender(DisplayFrameSlot slot)
        {
            lock (slot.Sync)
            {
                slot.LatestFrame?.Dispose();

                slot.LatestFrame = null;

                slot.ReusableFrame?.Dispose();

                slot.ReusableFrame = null;
                slot.LatestFrameDecodedTicks = 0;
            }

            Interlocked.Exchange(ref slot.WorkerRunning, 0);
        }

        /// <summary>
        /// 프레임 표시 완료 후 예약 상태와 버퍼 소유권을 정리한다.
        /// </summary>
        internal void CompleteLatestFrameRender(string streamName, DisplayFrameSlot slot, Action<BitmapSource> setImageAction, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AbortLatestFrameRender(slot);

                return;
            }

            Interlocked.Exchange(ref slot.WorkerRunning, 0);

            bool hasLatest;

            lock (slot.Sync)
            {
                hasLatest = slot.LatestFrame != null;
            }

            if (hasLatest && Interlocked.CompareExchange(ref slot.WorkerRunning, 1, 0) == 0)
            {
                ScheduleLatestFrameRender(streamName, slot, setImageAction, cancellationToken);
            }
        }

        /// <summary>
        /// 화면 스레드에 최신 검출 오버레이 갱신을 예약한다.
        /// </summary>
        internal void QueueLatestOverlayUpdate(string streamName, DisplayFrameSlot displaySlot, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            DetectionFrameSlot detectionSlot = GetDetectionSlot(streamName);

            long version;

            lock (detectionSlot.Sync)
            {
                version = detectionSlot.ResultVersion;
            }

            if (version <= displaySlot.LastOverlayVersion || Interlocked.CompareExchange(ref displaySlot.OverlayDispatchPending, 1, 0) != 0)
            {
                return;
            }

            Dispatcher dispatcher = App.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.HasShutdownStarted)
            {
                Interlocked.Exchange(ref displaySlot.OverlayDispatchPending, 0);

                return;
            }

            dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    GetLatestDetectionResult(streamName, out ThermalFireDetectionResult thermalResult, out SmokeDetectionResult smokeResult, out long latestVersion, out OpenCvWpfTracking.Services.Position.ZoneFrameContext zoneFrame);

                    // 2026-10-06: 화재 후보 상태/경고는 IR뿐 아니라 원본 EO 영상에도 동일하게 반영한다.
                    _context.UpdateThermalFireCandidateState(streamName, thermalResult, zoneFrame);

                    _context.UpdateVisionBBoxEvents(streamName, smokeResult.IsInfraredSupport ? "IR SMOKE" : "SMOKE", smokeResult.CandidateRects, smokeResult.CandidateScores, smokeResult.IsInfraredSupport ? "IR SMOKE CANDIDATE" : "IMAGE PROCESSING", zoneFrame);

                    _context.UpdateFireSmokeDetectionOverlay(streamName, thermalResult, smokeResult);

                    displaySlot.LastOverlayVersion = latestVersion;
                }
                finally
                {
                    Interlocked.Exchange(ref displaySlot.OverlayDispatchPending, 0);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        QueueLatestOverlayUpdate(streamName, displaySlot, cancellationToken);
                    }
                }
            }));
        }

        /// <summary>
        /// 2026-09-16: 분석은 최대 10fps의 최신 프레임만 사용한다. 밀린 과거 프레임은 즉시 폐기하여 RTSP 수신과 화면 표시를
        /// 보호한다.
        /// </summary>
        internal void QueueLatestDetectionFrame(string streamName, Mat sourceFrame)
        {
            DetectionFrameSlot slot = GetDetectionSlot(streamName);

            long now = Stopwatch.GetTimestamp();

            long previous = Interlocked.Read(ref slot.LastQueuedTicks);

            if (now - previous < DetectionMinimumIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(ref slot.LastQueuedTicks, now);

            Mat detectionFrame = RentDetectionFrameBuffer(slot);

            try
            {
                sourceFrame.CopyTo(detectionFrame);
            }
            catch
            {
                ReturnDetectionFrameBuffer(slot, detectionFrame);

                throw;
            }

            Mat replacedFrame;

            var zoneFrame = _context.CaptureZoneFrame(streamName);

            zoneFrame.Width = sourceFrame.Width;
            zoneFrame.Height = sourceFrame.Height;

            lock (slot.Sync)
            {
                replacedFrame = slot.LatestFrame;

                slot.LatestFrame = detectionFrame;
                slot.LatestZoneFrame = zoneFrame;
            }

            ReturnDetectionFrameBuffer(slot, replacedFrame);

            if (Interlocked.CompareExchange(ref slot.WorkerRunning, 1, 0) == 0)
            {
                Task.Run(() => ProcessLatestDetectionFrames(streamName, slot));
            }
        }

        /// <summary>
        /// 채널별 최신 프레임만 검출에 사용해 처리 지연과 이전 결과 누적을 줄인다.
        /// </summary>
        internal void ProcessLatestDetectionFrames(string streamName, DetectionFrameSlot slot)
        {
            try
            {
                while (true)
                {
                    Mat frame;

                    OpenCvWpfTracking.Services.Position.ZoneFrameContext zoneFrame;

                    lock (slot.Sync)
                    {
                        frame = slot.LatestFrame;
                        zoneFrame = slot.LatestZoneFrame;

                        slot.LatestFrame = null;
                    }

                    if (frame == null)
                    {
                        return;
                    }

                    int settingsGeneration = Volatile.Read(ref _context.VisionSettingsGeneration);

                    // 프레임 통계는 시간값만 보관해 매 프레임 Stopwatch 객체 할당을 피한다.
                    long stopwatchStartedAt = Stopwatch.GetTimestamp();

                    try
                    {
                        bool isInfraredFire = string.Equals(streamName, "IR", StringComparison.OrdinalIgnoreCase);

                        ThermalFireDetectionService fireService = isInfraredFire ? _context.IrFireDetectionService : _context.EoFireDetectionService;

                        ThermalFireDetectionResult thermalResult = fireService.Process(frame, _context.IsThermalFireDetectionEnabled && _context.IsFireSourceEnabled(isInfraredFire) && _context.IsFireSmokeFrameAnalysisAllowed(), _context.ThermalHotThresholdRatio, _context.ThermalMinimumAreaRatio, _context.ThermalFireBoxGroupingMode, _context.GetRecentAiFireCandidates(isInfraredFire));

                        SmokeDetectionResult smokeResult = _context.ProcessSmokeFrame(frame, streamName, thermalResult.CandidateRect);

                        lock (slot.Sync)
                        {
                            if (settingsGeneration != Volatile.Read(ref _context.VisionSettingsGeneration))
                            {
                                continue;
                            }

                            slot.SettingsGeneration = settingsGeneration;
                            slot.ThermalResult = thermalResult;
                            slot.SmokeResult = smokeResult;
                            slot.ResultZoneFrame = zoneFrame;

                            slot.ResultVersion++;
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleLogHelper.Error(streamName + " DETECTION", "Latest-frame analysis failed", ex);
                    }
                    finally
                    {
                        ReturnDetectionFrameBuffer(slot, frame);

                        long stopwatchElapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - stopwatchStartedAt);

                        RtspChannelPerformance performance = GetRtspPerformance(streamName);

                        lock (performance.Sync)
                        {
                            performance.DetectionCount++;

                            performance.DetectionTicks += stopwatchElapsedTicks;
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref slot.WorkerRunning, 0);

                lock (slot.Sync)
                {
                    if (slot.LatestFrame != null && Interlocked.CompareExchange(ref slot.WorkerRunning, 1, 0) == 0)
                    {
                        Task.Run(() => ProcessLatestDetectionFrames(streamName, slot));
                    }
                }
            }
        }

        /// <summary>
        /// 해당 채널에 보관된 최신 검출 결과를 가져온다.
        /// </summary>
        internal void GetLatestDetectionResult(string streamName, out ThermalFireDetectionResult thermalResult, out SmokeDetectionResult smokeResult, out long resultVersion, out OpenCvWpfTracking.Services.Position.ZoneFrameContext zoneFrame)
        {
            DetectionFrameSlot slot = GetDetectionSlot(streamName);

            lock (slot.Sync)
            {
                thermalResult = slot.ThermalResult;
                smokeResult = slot.SmokeResult;

                if (slot.SettingsGeneration != Volatile.Read(ref _context.VisionSettingsGeneration))
                {
                    thermalResult = default(ThermalFireDetectionResult);

                    smokeResult = default(SmokeDetectionResult);
                }

                resultVersion = slot.ResultVersion;
                zoneFrame = slot.ResultZoneFrame;
            }
        }

        /// <summary>
        /// 원본 PTS/DTS, 실제 Decode 도착 간격과 8x8 표본 영상 변화를 기록한다. 전체 Frame Hash나 Resize를 수행하지
        /// 않아 실시간 영상처리 부하를 늘리지 않는다.
        /// </summary>
        internal static void UpdateFrameTimingDiagnostics(RtspChannelPerformance performance, FFmpegDecoderService.FrameTimingSnapshot timing, Mat frame)
        { VideoPerformanceRecorder.UpdateFrameTimingDiagnostics(performance, timing, frame); }

        /// <summary>
        /// 채널별 수신·표시 성능 요약을 진단 로그에 남긴다.
        /// </summary>
        internal void WriteRtspPerformanceSummary(string streamName, IFFmpegDecoderServicePort decoder)
        { _performanceRecorder.WriteRtspPerformanceSummary(streamName, decoder); }

        /// <summary>
        /// 취소 또는 연결 종료 전까지 FFmpeg 프레임을 수신해 표시·탐지 처리에 전달한다.
        /// </summary>
        internal void FFmpegCaptureLoop(IFFmpegDecoderServicePort decoder, string sourceAddress, string streamName, Action<BitmapSource> setImageAction, CancellationToken cancellationToken)
        {
            string displayMode = streamName == "EO" ? $"BOUNDED_JITTER_BUFFER / CAPACITY={EoJitterBufferCapacity} / " + $"TARGET_FPS={EoJitterDisplayFramesPerSecond}" : "DIRECT_LATEST_FRAME / NO_TIMER_DELAY";

            double frameIntervalMilliseconds = streamName == "EO" ? 1000.0 / EoJitterDisplayFramesPerSecond : 0.0;

            ConsoleLogHelper.Info("RTSP DISPLAY " + streamName, displayMode + " / SINGLE_PENDING_RENDER / " + "WRITEABLE_BITMAP_REUSE / MAT_BUFFER_REUSE / OVERLAY_DECOUPLED / " + $"BUFFER_CAPACITY={(streamName == "EO" ? EoJitterBufferCapacity : 1)} / " + $"FRAME_INTERVAL_MS={frameIntervalMilliseconds:F1} / " + $"PACING_MODE={(streamName == "EO" ? "ABSOLUTE_MONOTONIC" : "ARRIVAL_DRIVEN")} / " + "LATEST_FRAME_PRIORITY=True / STALE_FRAME_DROP_ENABLED=True / " + $"MAX_BUFFER_LIMIT={(streamName == "EO" ? EoJitterBufferCapacity : 1)} / " + $"EO_DISPLAY_MAX_WIDTH={EoDisplayMaximumWidth}");

            RtspChannelPerformance watchdogPerformance = GetRtspPerformance(streamName);

            lock (watchdogPerformance.Sync)
            {
                watchdogPerformance.LastFrame.Restart();

                watchdogPerformance.LastDiagnosticArrivalTicks = 0;
                watchdogPerformance.HasLastTimestamp = false;
                watchdogPerformance.HasPreviousFrameSamples = false;
            }

            // 2026-10-01: 운용 중 Frame 정지 시 기존 Capture Loop를 먼저 끝낸 뒤
            // 동일 채널만 재연결하기 위한 Watchdog 요청 Flag이다.
            int watchdogReconnectRequested = 0;

            // 2026-09-17: Decoder Open 상태와 실제 영상 수신을 분리 판정한다.
            // 전원 차단/RTSP 정지로 5초간 프레임이 없으면 백그라운드에서
            // Decoder를 닫아 ReadFrame을 해제하고 UI를 Disconnected로 전환한다.
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested && decoder.IsOpened)
                {
                    await Task.Delay(500).ConfigureAwait(false);

                    long noFrameMs;

                    lock (watchdogPerformance.Sync)
                    {
                        noFrameMs = watchdogPerformance.LastFrame.ElapsedMilliseconds;
                    }

                    if (noFrameMs < 5000)
                    {
                        continue;
                    }

                    Interlocked.Exchange(ref watchdogReconnectRequested, 1);

                    ConsoleLogHelper.Warning(streamName + " VIDEO", $"Frame watchdog timeout / LAST_FRAME_MS={noFrameMs} / AUTO_RECONNECT=True");

                    try
                    {
                        decoder.Close();
                    }
                    catch (Exception closeException)
                    {
                        ConsoleLogHelper.Error(streamName + " VIDEO", "Watchdog decoder close failed", closeException);
                    }

                    Dispatcher dispatcher = App.Current?.Dispatcher;

                    if (dispatcher != null && !dispatcher.HasShutdownStarted)
                    {
                        _ = dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            HandleRtspFrameLossOnUiThread(streamName);
                        }));
                    }

                    break;
                }
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Volatile.Read(ref watchdogReconnectRequested) != 0)
                {
                    break;
                }

                Mat frame = null;

                try
                {
                    // FFmpeg Decoder에서 다음 Frame 획득
                    //
                    frame = decoder.ReadFrame();

                    if (Volatile.Read(ref watchdogReconnectRequested) != 0)
                    {
                        break;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Frame 수신 실패 시 짧게 대기 후 재시도
                    //
                    if (frame == null || frame.Empty())
                    {
                        Thread.Sleep(5);

                        continue;
                    }

                    RtspChannelPerformance channelPerformance = GetRtspPerformance(streamName);

                    lock (channelPerformance.Sync)
                    {
                        channelPerformance.Received++;

                        channelPerformance.LastFrame.Restart();

                        UpdateFrameTimingDiagnostics(channelPerformance, decoder.GetLastFrameTiming(), frame);
                    }

                    QueueLatestDetectionFrame(streamName, frame);

                    WriteRtspPerformanceSummary(streamName, decoder);

                    // 2026-09-23: EO 표시만 3 Frame 제한 버퍼로 RTSP 도착 지터를
                    // 흡수한다. IR 및 분석/알람/파노라마 입력 경로는 기존 방식을 유지한다.
                    if (!UseLegacyImmediateRtspDisplay)
                    {
                        if (streamName == "EO" && _context.IsPanoramaCaptureRunning)
                        {
                            BitmapSource rawPanoramaFrame = MatToBitmapSourceConverter.Convert(frame);

                            if (rawPanoramaFrame != null)
                            {
                                if (rawPanoramaFrame.CanFreeze && !rawPanoramaFrame.IsFrozen)
                                {
                                    rawPanoramaFrame.Freeze();
                                }

                                _context.SetRawEoPanoramaFrame(rawPanoramaFrame);
                            }
                        }

                        if (streamName == "EO")
                        {
                            QueueEoBufferedDisplayFrame(streamName, frame, setImageAction, cancellationToken);
                        }
                        else
                        {
                            QueueLatestDisplayFrame(streamName, frame, setImageAction, cancellationToken);
                        }

                        continue;
                    }

                    /*
                     * 이전 Frame이 아직 UI Dispatcher에서 처리 중이면
                     * 현재 Frame은 변환조차 하지 않고 버린다.
                     *
                     * 특히 EO 1920 x 1080 Frame의 Bitmap 변환 비용이 크므로,
                     * 불필요한 변환 작업을 줄이는 효과도 있다.
                     */
                    if (!TryReserveFrameDispatch(streamName))
                    {
                        lock (channelPerformance.Sync)
                        {
                            channelPerformance.DisplayDropped++;
                        }

                        continue;
                    }

                    bool dispatchQueued = false;

                    try
                    {
                        /*
                         * 2026-08-31: 파노라마 합성 입력은 AI/FIRE/SMOKE Overlay가
                         * 그려지기 전 EO 원본이어야 한다. 촬영 중에만 별도 Bitmap을
                         * 생성해 일반 운용의 프레임 변환 비용은 증가시키지 않는다.
                         */
                        if (streamName == "EO" && _context.IsPanoramaCaptureRunning)
                        {
                            BitmapSource rawPanoramaFrame = MatToBitmapSourceConverter.Convert(frame);

                            if (rawPanoramaFrame != null)
                            {
                                if (rawPanoramaFrame.CanFreeze && !rawPanoramaFrame.IsFrozen)
                                {
                                    rawPanoramaFrame.Freeze();
                                }

                                _context.SetRawEoPanoramaFrame(rawPanoramaFrame);
                            }
                        }

                        // 2026-09-16: 탐지 결과는 별도 최신 프레임 Worker가 갱신한다.
                        // 표시 경로는 탐지 완료를 기다리지 않는다.
                        int overlaySettingsGeneration = Volatile.Read(ref _context.VisionSettingsGeneration);

                        GetLatestDetectionResult(streamName, out ThermalFireDetectionResult thermalResult, out SmokeDetectionResult smokeResult, out long _, out OpenCvWpfTracking.Services.Position.ZoneFrameContext zoneFrame);

                        // OpenCV Mat → WPF BitmapSource 변환
                        //
                        // 프레임 통계는 시간값만 보관해 매 프레임 Stopwatch 객체 할당을 피한다.
                        long convertStopwatchStartedAt = Stopwatch.GetTimestamp();

                        BitmapSource bitmap = MatToBitmapSourceConverter.Convert(frame);

                        long convertStopwatchElapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - convertStopwatchStartedAt);

                        lock (channelPerformance.Sync)
                        {
                            channelPerformance.ConvertCount++;

                            channelPerformance.ConvertTicks += convertStopwatchElapsedTicks;
                        }

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
                        if (bitmap.CanFreeze && !bitmap.IsFrozen)
                        {
                            bitmap.Freeze();
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        Dispatcher dispatcher = App.Current?.Dispatcher;

                        if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
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
                        DispatcherPriority priority = GetFrameDispatcherPriority(streamName);

                        long dispatchQueuedTicks = Stopwatch.GetTimestamp();

                        dispatcher.BeginInvoke(priority, new Action(() =>
                        {
                            try
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }

                                if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                                {
                                    return;
                                }

                                // Binding 대상 영상 Property 갱신
                                //
                                setImageAction(bitmap);

                                ConfirmDisplayedVideoFrame(streamName);

                                long dispatchElapsedTicks = Stopwatch.GetTimestamp() - dispatchQueuedTicks;

                                lock (channelPerformance.Sync)
                                {
                                    channelPerformance.Displayed++;

                                    channelPerformance.DispatchCount++;

                                    channelPerformance.DispatchTicks += dispatchElapsedTicks;

                                    channelPerformance.DispatchMaxTicks = Math.Max(channelPerformance.DispatchMaxTicks, dispatchElapsedTicks);
                                }

                                // 2026-10-06: 라이브 EO 원본 프레임에서도 화재 BBOX/경고 상태를 갱신한다.
                                if (overlaySettingsGeneration != Volatile.Read(ref _context.VisionSettingsGeneration))
                                {
                                    return;
                                }

                                _context.UpdateThermalFireCandidateState(streamName, thermalResult, zoneFrame);

                                _context.UpdateVisionBBoxEvents(streamName, // 2026-09-08: 이벤트 표에는 의미를 유지한 간결한 TYPE을 표시한다.
 smokeResult.IsInfraredSupport ? "IR SMOKE" : "SMOKE", smokeResult.CandidateRects, smokeResult.CandidateScores, smokeResult.IsInfraredSupport ? "IR SMOKE CANDIDATE" : "IMAGE PROCESSING", zoneFrame);

                                _context.UpdateFireSmokeDetectionOverlay(streamName, thermalResult, smokeResult);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[{streamName}] " + $"[UI FRAME UPDATE ERROR] " + ex.Message);
                            }
                            finally
                            {
                                /*
                                     * UI 반영이 완료되었으므로
                                     * 다음 Frame의 Dispatcher 등록을 허용한다.
                                     */
                                ReleaseFrameDispatch(streamName);
                            }
                        }));

                        dispatchQueued = true;
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
                            ReleaseFrameDispatch(streamName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        ConsoleLogHelper.Error($"{streamName} VIDEO", "FFmpeg Capture Error", ex);
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
            ReleaseFrameDispatch(streamName);

            ConsoleLogHelper.Info($"{streamName} VIDEO", "FFmpeg Capture Loop End");

            // 2026-10-01: Watchdog가 종료시킨 채널만 새 Decoder/Capture Loop로 복구한다.
            // 기존 Loop가 완전히 끝난 뒤 시작하므로 하나의 RTSP를 두 Loop가 동시에
            // 소비하는 경쟁 상태를 방지한다.
            if (Volatile.Read(ref watchdogReconnectRequested) != 0 && _context.IsDeviceConnectionRequested && !cancellationToken.IsCancellationRequested && _context.VideoReconnectCts != null && !_context.VideoReconnectCts.IsCancellationRequested)
            {
                ConsoleLogHelper.Info(streamName + " VIDEO", "Runtime frame stall recovery started");

                CancellationToken reconnectToken;

                try
                {
                    reconnectToken = _context.VideoReconnectCts.Token;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (NullReferenceException)
                {
                    return;
                }

                _ = ReconnectVideoAsync(decoder, sourceAddress, streamName, setImageAction, reconnectToken);
            }
        }

        /// <summary>
        /// 2026-10-01: 장비 전원 차단 또는 RTSP Frame 정지 시 영상, 연결 표시, AI BBox 및 FIRE/SMOKE 상태가 서로
        /// 다른 시점에 남지 않도록 한 번에 정리한다. Connected 복귀는 CaptureLoop에서 실제 첫 Frame을 받은 뒤에만 수행한다.
        /// </summary>
        internal void HandleRtspFrameLossOnUiThread(string streamName)
        {
            bool isEo = string.Equals(streamName, "EO", StringComparison.OrdinalIgnoreCase);

            if (isEo)
            {
                _context.IsEoFrameDisplayed = false;
                _context.EOCameraImage = null;
                _context.EoStatusText = "[EO] Reconnecting...";

                _context.EoDetectionBoxes.Clear();

                _context.EoFireSmokeDetectionBoxes.Clear();

                _context.SetLocalFireSmokeDetectionState(0, false, false);
            }
            else
            {
                _context.IsIrFrameDisplayed = false;
                _context.IRCameraImage = null;
                _context.IrStatusText = "[IR] Reconnecting...";

                _context.IrDetectionBoxes.Clear();

                _context.IrFireSmokeDetectionBoxes.Clear();

                _context.SetLocalFireSmokeDetectionState(1, false, false);
            }

            _context.ResetFireSmokeFrameAnalysis(streamName + " RTSP frame loss");

            bool allVideoFramesLost = !_context.IsEoFrameDisplayed && !_context.IsIrFrameDisplayed;

            if (allVideoFramesLost)
            {
                _context.IsCrosshairVisible = false;

                if (_context.AiPowerStatusText == "ON" || _context.IsThermalFireDetectionEnabled || _context.IsSmokeDetectionEnabled)
                {
                    _context.DisconnectAiAgent();
                }

                ConsoleLogHelper.State("DEVICE STATE SYNC", "EO_FRAME=False / IR_FRAME=False / AI=OFF / FIRE=OFF / SMOKE=OFF");
            }

            ConsoleLogHelper.State(streamName + " VIDEO", "Frame loss UI synchronized / STATUS=Reconnecting / " + "IMAGE_CLEARED=True / DETECTION_CLEARED=True");
        }

        private readonly VideoPerformanceRecorder _performanceRecorder;
        #endregion

    }
}
