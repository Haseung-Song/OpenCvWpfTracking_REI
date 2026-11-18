using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>AI 연결과 탐지 결과에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IAiFeatureContext
    {
        Dictionary<string, AiEventTrack> _activeAiEvents { get; }

        string _aiControlAgentIp { get; set; }

        Services.Communication.AI.IAiDetectorClientServicePort _aiDetectorClientService { get; }

        Services.Communication.AI.IAiDetectorPacketParserPort _aiDetectorPacketParser { get; }

        Services.Communication.AI.IAiDetectorPacketBuilderPort _aiPacketBuilder { get; }

        CancellationTokenSource _controlAgentReconnectCts { get; set; }

        CancellationTokenSource _cts { get; set; }

        bool _isEoFrameDisplayed { get; set; }

        bool _isIrFrameDisplayed { get; set; }

        ref global::System.DateTime _lastAiDetectorLogTime { get; }

        Models.Main.RtspSourceOption _selectedEoRtspSource { get; set; }

        Models.Main.RtspSourceOption _selectedIrRtspSource { get; set; }

        DispatcherTimer _testProgramEventTimer { get; set; }

        CancellationTokenSource _videoReconnectCts { get; set; }

        int ActiveAiCount { get; set; }

        int AiAgentPort { get; set; }

        string AiControlAgentIp { get; set; }

        double AiDisplayConfidenceThreshold { get; set; }

        double AiMappingConfidence { get; set; }

        double AiMappingIou { get; set; }

        ObservableCollection<AiMappingInfo> AiMappingList { get; }

        ObservableCollection<AiOnnxInfo> AiOnnxList { get; }

        string AiPowerStatusText { get; set; }

        string AiRtsp0Address { get; set; }

        int AiRtsp0OnnxIndex { get; set; }

        string AiRtsp1Address { get; set; }

        int AiRtsp1OnnxIndex { get; set; }

        ObservableCollection<AiRtspInfo> AiRtspList { get; }

        string AiSettingStatusText { get; set; }

        void ClearAiSmokeCandidateSnapshots();

        void ClearDisabledVisionEvents(bool fire, bool writeAudit = true);

        void Disconnect();

        ObservableCollection<AiDetectionBox> EoDetectionBoxes { get; }

        ObservableCollection<VisionDetectionBox> EoFireSmokeDetectionBoxes { get; }

        string EoSourceAddress { get; set; }

        int EoVideoHeight { get; set; }

        int EoVideoWidth { get; set; }

        ObservableCollection<AiDetectionBox> IrDetectionBoxes { get; }

        ObservableCollection<VisionDetectionBox> IrFireSmokeDetectionBoxes { get; }

        string IrSourceAddress { get; set; }

        int IrVideoHeight { get; set; }

        int IrVideoWidth { get; set; }

        bool IsAutoTrackingEnabled { get; set; }

        bool IsFireDiagnosticEnabled { get; set; }

        bool IsPanoramaMotionDetectionSuppressed();

        bool IsSmokeDetectionEnabled { get; set; }

        bool IsSmokeDiagnosticEnabled { get; set; }

        bool IsThermalFireDetectionEnabled { get; set; }

        void NotifyAiEventSummaryChanged();

        void NotifyFireSmokeWarningStateChanged(int rtspIndex);

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        void SaveRtspCommunicationSettings();

        Models.Main.RtspSourceOption SelectedAiEoRtspSource { get; set; }

        Models.Main.RtspSourceOption SelectedAiIrRtspSource { get; set; }

        void SetFireSmokeDetectorsForAiConnection(bool enabled, string reason);

        void UpdateAiDetectionEvent(AiDetectionResult result, DateTime receiveTime);

        void UpdateAiSmokeCandidateSnapshot(AiDetectionResult result, DateTime receiveTime);

        Action DrainPendingAiUiUpdatesCallback { get; }

        Action<byte[], DateTime> OnAiDetectorPacketReceivedCallback { get; }
    }

    /// <summary>AI 연결과 탐지 결과 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class AiFeature
    {

        #region [협력 객체와 초기화]

        private readonly IAiFeatureContext _context;

        internal AiFeature(IAiFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _lastEoAiDisplayDetectionTime = DateTime.MinValue;
            _lastIrAiDisplayDetectionTime = DateTime.MinValue;
        }

        #endregion

        #region [상태 필드]

        internal readonly object _aiUiUpdateSync = new object();

        internal readonly Dictionary<int, Action> _pendingAiUiUpdates = new Dictionary<int, Action>();

        internal readonly Dictionary<int, bool> _pendingAiUiUpdateHasDetection = new Dictionary<int, bool>();

        internal bool _isAiUiDrainScheduled;

        internal bool _isApplicationShutdownRequested;

        internal int _coalescedAiUiUpdateCount;

        internal int _aiDetectionGeneration;

        internal bool _hasAppliedDefaultAiModelMapping;

        internal readonly object _aiRtspVideoSizeSync = new object();

        internal readonly Dictionary<int, Tuple<int, int, DateTime>> _aiRtspVideoSizes = new Dictionary<int, Tuple<int, int, DateTime>>();

        internal bool _aiRtspVideoSizeErrorActive;

        internal DateTime _lastEoAiDisplayDetectionTime;

        internal DateTime _lastIrAiDisplayDetectionTime;

        internal bool _isEoAiDisplayHoldActive;

        internal bool _isIrAiDisplayHoldActive;

        internal int _aiManualConnectionGeneration;

        #endregion

        #region [동작 처리]

        internal async Task ConnectAiAgentFromSettingAsync()
        {
            int generation = Interlocked.Increment(ref _aiManualConnectionGeneration);

            _context.AiPowerStatusText = "OFF";
            _context.AiSettingStatusText = "[AI] Connecting...";

            _context.SetFireSmokeDetectorsForAiConnection(false, "AI connect initialization");

            // 2026-08-25: 재연결 시작 시 이전 연결의 BBox와 ACTIVE 상태를 제거한다.
            ClearAiDetectionState("Connect initialization");

            try
            {
                /*
                 * 과거 Auto Reconnect가 실행된 상태가 남아 있더라도
                 * 버튼 기반 수동 연결 정책과 충돌하지 않도록 먼저 종료한다.
                 */
                // 재연결 취소가 실패해도 소켓 정리는 계속 시도한다.
                try
                {
                    _context._aiDetectorClientService.StopAutoReconnect();
                }
                finally
                {
                    _context._aiDetectorClientService.Disconnect();
                }

                ResetAiRtspVideoSizes();

                bool connected = await _context._aiDetectorClientService.ConnectAsync(_context.AiControlAgentIp, _context.AiAgentPort);

                if (generation != Volatile.Read(ref _aiManualConnectionGeneration))
                {
                    return;
                }

                if (!connected)
                {
                    _context.AiSettingStatusText = "[AI] Connect Failed";

                    return;
                }

                _context.AiPowerStatusText = "ON";
                _context.AiSettingStatusText = "[AI] Connected";

                // 연결 버튼은 AI 통신만 변경하며 VP 기능은 자체 확인란으로 제어한다.
                _context.SetFireSmokeDetectorsForAiConnection(true, "AI connected");

                if (!await RequestAiDetectorRtspAddressSetAsync())
                {
                    if (generation != Volatile.Read(ref _aiManualConnectionGeneration))
                    {
                        return;
                    }

                    _context.AiSettingStatusText = "[AI] RTSP Apply Failed";

                    return;
                }

                await Task.Delay(300);

                if (generation != Volatile.Read(ref _aiManualConnectionGeneration))
                {
                    return;
                }

                if (!await RequestAiDetectorInfoAsync() || !await RequestAiDetectorRtspAddressAsync() || !await RequestAiDetectorOnnxListAsync())
                {
                    if (generation != Volatile.Read(ref _aiManualConnectionGeneration))
                    {
                        return;
                    }

                    _context.AiSettingStatusText = "[AI] Initial Setting Incomplete";

                    return;
                }

                // 2026-08-31: ONNX 목록 응답이 UI 기본 선택에 반영된 뒤 Mapping을
                // 전송해야 EO/IR 모두 Real-Time Smoke 모델로 실제 Agent에 적용된다.
                await WaitForInitialAiModelSelectionAsync();

                if (generation != Volatile.Read(ref _aiManualConnectionGeneration))
                {
                    return;
                }

                if (!await RequestAiDetectorMappingSetAsync() || !await RequestAiDetectorMappingAsync())
                {
                    if (generation != Volatile.Read(ref _aiManualConnectionGeneration))
                    {
                        return;
                    }

                    _context.AiSettingStatusText = "[AI] Initial Setting Incomplete";

                    return;
                }

                // v2.91: CMD56은 Mapping이 아니라 RTSP 원본 영상 크기 응답이다.
                // 자동 알림을 놓친 경우를 보완하기 위해 EO/IR을 1회 조회한다.
                await RequestAiDetectorRtspVideoSizeAsync(0);

                await RequestAiDetectorRtspVideoSizeAsync(1);

                if (generation == Volatile.Read(ref _aiManualConnectionGeneration))
                {
                    _context.AiSettingStatusText = "[AI] Connect / Setting Complete";
                }
            }
            catch (Exception ex)
            {
                if (generation != Volatile.Read(ref _aiManualConnectionGeneration))
                {
                    return;
                }

                _context.AiPowerStatusText = "OFF";
                _context.AiSettingStatusText = "[AI] Connect / Setting Incomplete";

                _context.SetFireSmokeDetectorsForAiConnection(false, "AI connect exception");

                ConsoleLogHelper.Error("AI DETECTOR", "Connect / setting exception / " + ex.Message);
            }
        }

        internal async Task WaitForInitialAiModelSelectionAsync()
        {
            const string DefaultModelName = "Real-Time-Smoke-Fire-Detection-YOLO11n-main.onnx";

            for (int attempt = 0; attempt < 20; attempt++)
            {
                if (_context.AiOnnxList.Any(model => string.Equals(model.FileName, DefaultModelName, StringComparison.OrdinalIgnoreCase)) && _context.AiRtsp0OnnxIndex == _context.AiRtsp1OnnxIndex)
                {
                    return;
                }

                await Task.Delay(75);
            }

            ConsoleLogHelper.Warning("AI DETECTOR", "Default smoke model selection timeout; current mapping will be used");
        }

        internal void DisconnectAiAgent()
        {
            Interlocked.Increment(ref _aiManualConnectionGeneration);

            // 전체 해제는 단순 통신 상태 변경이 아니라 사용자의 기능 해제 요청이다.
            // AI 소켓이 이미 끊겨 있어도 소켓 정리 전에 기능 해제를 적용한다.
            TryRunAiUiAction(() =>
            {
                _context.IsThermalFireDetectionEnabled = false;
                _context.IsSmokeDetectionEnabled = false;
                _context.IsFireDiagnosticEnabled = false;
                _context.IsSmokeDiagnosticEnabled = false;
                _context.IsAutoTrackingEnabled = false;

                _context.ClearDisabledVisionEvents(true);

                _context.ClearDisabledVisionEvents(false);
            }, "AI/VP manual disconnect all-off");

            _context.AiPowerStatusText = "OFF";
            _context.AiSettingStatusText = "[AI] Disconnecting...";

            try
            {
                _context._aiDetectorClientService.StopAutoReconnect();

                _context._aiDetectorClientService.Disconnect();

                ResetAiRtspVideoSizes();

                // 2026-08-25: 수동 연결 해제 즉시 EO/IR BBox와 AI ACTIVE 상태를 정리한다.
                ClearAiDetectionState("Manual disconnect");

                _context.SetFireSmokeDetectorsForAiConnection(false, "AI manual disconnect");

                _context.AiSettingStatusText = "[AI] Disconnected";

                ConsoleLogHelper.Command("AI DETECTOR", "Manual disconnect completed / Detection overlay cleared");
            }
            catch (Exception ex)
            {
                ClearAiDetectionState("Manual disconnect exception fallback");

                _context.SetFireSmokeDetectorsForAiConnection(false, "AI disconnect exception");

                _context.AiSettingStatusText = "[AI] Disconnect Incomplete";

                ConsoleLogHelper.Error("AI DETECTOR", "Manual disconnect exception", ex);
            }
        }

        internal void OnAiDetectorPacketReceived(byte[] packet, DateTime receiveTime)
        {
            // [AI Detector] 공통 [Packet] 구조 파싱
            //
            // [STX] / [CMD] / [SIZE] / [Payload] / [Checksum] / [ETX] 검증 후,
            // [CMD]와 [Payload]를 추출한다.
            //
            if (!_context._aiDetectorPacketParser.TryParseCommonPacket(packet, out string command, out string payload))
            {
                return;
            }

            // [CMD] 기준 응답 분기
            //
            // [CMD 51] : [AI Detector Info] 응답
            // [CMD 52] : [RTSP] 주소 조회 응답
            // [CMD 53] : [ONNX] 목록 조회 응답
            // [CMD 54] : [RTSP] / [ONNX] Mapping 조회 응답
            // [CMD 55] : 탐지데이터 응답
            // [CMD 56] : RTSP 원본 영상 크기 응답 또는 비동기 알림
            //
            switch (command)
            {
                case "50":

                    // [CMD 50] 설정 요청 결과 응답
                    //
                    // [CMD 02] RTSP 주소 설정,
                    // [CMD 05] RTSP / ONNX Mapping 설정 등
                    // 설정 계열 요청 이후 수신되는 결과 Packet.
                    //
                    // 현재 확인 기준:
                    // Payload "o" => 설정 성공
                    // 그 외 값       => Agent 응답 원문 출력
                    //
                    if (payload == "o")
                    {
                        Console.WriteLine();

                        Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 50] Setting Result : OK");

                        _context.AiSettingStatusText = "[AI] Setting Result : OK";

                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine();

                        Console.WriteLine($"[AI DETECTOR RESPONSE] [CMD 50] Setting Result : {payload}");

                        _context.AiSettingStatusText = $"[AI] Setting Result : {payload}";

                        Console.WriteLine();
                    }

                    break;
                case "51":
                    HandleAiDetectorInfoResponse(payload);
                    break;
                case "52":
                    HandleAiDetectorRtspResponse(payload);
                    break;
                case "53":
                    HandleAiDetectorOnnxResponse(payload);
                    break;
                case "54":
                    HandleAiDetectorMappingResponse(payload);
                    break;
                case "55":
                    HandleAiDetectorDetectionPacket(packet, receiveTime);
                    break;
                case "56":
                    HandleAiDetectorRtspVideoSizeResponse(payload, receiveTime);
                    break;
                case "99":
                    ConsoleLogHelper.Warning("AI DETECTOR RESPONSE", "CMD=99 / MESSAGE=" + payload);
                    break;
                default:
                    Console.WriteLine($"[AI DETECTOR] Unknown CMD : {command}, Payload : {payload}");
                    break;
            }
        }

        internal void HandleAiDetectorDetectionPacket(byte[] packet, DateTime receiveTime)
        {
            // 2026-08-25: Disconnect 이후 도착한 지연 CMD 55가 BBox를 다시 만들지 못하게 한다.
            if (_context.AiPowerStatusText != "ON")
            {
                return;
            }

            if (!_context._aiDetectorPacketParser.TryParseDetectionPacket(packet, out AiDetectionResult result))
            {
                return;
            }

            // 2026-10-01: 장비 전원 차단 뒤 지연 수신된 AI Packet이
            // 이미 Blackout된 화면 위에 BBox를 다시 생성하지 못하게 한다.
            bool isTargetFrameAvailable = result.RtspIndex == 0 ? _context._isEoFrameDisplayed : result.RtspIndex == 1 && _context._isIrFrameDisplayed;

            if (!isTargetFrameAvailable)
            {
                return;
            }

            HandleAiDetectionResult(result, receiveTime);
        }

        internal void ResetAiRtspVideoSizes()
        {
            lock (_aiRtspVideoSizeSync)
            {
                _aiRtspVideoSizes.Clear();

                _aiRtspVideoSizeErrorActive = false;
            }
        }

        internal void HandleAiDetectorRtspVideoSizeResponse(string payload, DateTime receiveTime)
        {
            if (!_context._aiDetectorPacketParser.TryParseRtspVideoSizePayload(payload, out int rtspIndex, out int width, out int height))
            {
                if (!_aiRtspVideoSizeErrorActive)
                {
                    _aiRtspVideoSizeErrorActive = true;

                    ConsoleLogHelper.Warning("AI RTSP VIDEO SIZE", "CMD=56 / RESULT=INVALID_PAYLOAD / PAYLOAD=" + payload);
                }

                return;
            }

            _aiRtspVideoSizeErrorActive = false;

            lock (_aiRtspVideoSizeSync)
            {
                _aiRtspVideoSizes[rtspIndex] = Tuple.Create(width, height, receiveTime);
            }

            bool available = width > 0 && height > 0;

            ConsoleLogHelper.State("AI RTSP VIDEO SIZE", $"CMD=56 / INDEX={rtspIndex} / WIDTH={width} / HEIGHT={height} / AVAILABLE={available}");

            int localWidth = rtspIndex == 0 ? _context.EoVideoWidth : rtspIndex == 1 ? _context.IrVideoWidth : 0;

            int localHeight = rtspIndex == 0 ? _context.EoVideoHeight : rtspIndex == 1 ? _context.IrVideoHeight : 0;

            if (available && localWidth > 0 && localHeight > 0 && (localWidth != width || localHeight != height))
            {
                ConsoleLogHelper.Warning("AI RTSP VIDEO SIZE", $"SOURCE_SIZE_MISMATCH / INDEX={rtspIndex} / AI={width}x{height} / VIEWER={localWidth}x{localHeight}");
            }
        }

        internal void HandleAiDetectionResult(AiDetectionResult result, DateTime receiveTime, bool forcePrintLog = false)
        {
            /*
             * 2026-08-26: Dispatcher.Invoke는 UI가 파노라마 촬영 처리 중일 때
             * AI 수신 Thread까지 대기시켰다. 비동기 최신값 병합 Queue를 사용하여
             * 촬영 중에도 TCP 수신을 계속하고 UI 복귀 직후 최신 BBox를 반영한다.
             */
            bool containsDisplayDetection = result.Boxes.Any(box => box.NormalizedConfidence >= _context.AiDisplayConfidenceThreshold);

            int generation = Volatile.Read(ref _aiDetectionGeneration);

            QueueAiDetectionUiUpdate(result.RtspIndex, () =>
            {
                // 연결 해제·재연결 후 대기 중이던 결과로 경보를 되살리지 않는다.
                if (generation != Volatile.Read(ref _aiDetectionGeneration) || _context.AiPowerStatusText != "ON" || !(result.RtspIndex == 0 ? _context._isEoFrameDisplayed : _context._isIrFrameDisplayed))
                {
                    return;
                }

                // 2026-08-31: PTZ 이동·잔진동 프레임은 AI Agent가 SMOKE로
                // 오인할 수 있으므로 파노라마 안정 구간에서만 신규 AI 결과를 반영한다.
                if (_context.IsPanoramaMotionDetectionSuppressed())
                {
                    _context.ClearAiSmokeCandidateSnapshots();

                    List<AiEventTrack> motionClearedEvents = _context._activeAiEvents.Where(item => item.Value.RtspIndex == result.RtspIndex).Select(item => item.Value).ToList();

                    foreach (AiEventTrack motionClearedTrack in motionClearedEvents)
                    {
                        FireEventRecord motionClearedEvent = motionClearedTrack.Event;

                        motionClearedEvent.MarkCleared(receiveTime);

                        AppendFireEventAudit(motionClearedEvent, "PANORAMA_MOVE_CLEARED");

                        ConsoleLogHelper.State("AI EVENT", "AI event cleared for panorama motion / EVENT_ID=" + motionClearedEvent.EventId + " / CAMERA=" + motionClearedEvent.Camera);
                    }

                    foreach (string motionKey in _context._activeAiEvents.Where(item => item.Value.RtspIndex == result.RtspIndex).Select(item => item.Key).ToList())
                    {
                        _context._activeAiEvents.Remove(motionKey);
                    }

                    _context.ActiveAiCount = _context._activeAiEvents.Count;

                    _context.NotifyAiEventSummaryChanged();

                    if (result.RtspIndex == 0)
                    {
                        _context.EoDetectionBoxes.Clear();
                    }
                    else if (result.RtspIndex == 1)
                    {
                        _context.IrDetectionBoxes.Clear();
                    }

                    _context.NotifyFireSmokeWarningStateChanged(result.RtspIndex);

                    return;
                }

                // 2026-08-31: 현재 RTSP Mapping과 모델 클래스 목록으로 Class Index를 실제 명칭으로 해석한다.
                ResolveAiDetectionClassNames(result);

                _context.UpdateAiSmokeCandidateSnapshot(result, receiveTime);

                switch (result.RtspIndex)
                {
                    case 0:
                        if (!_context._isEoFrameDisplayed)
                        {
                            return;
                        }

                        _context.UpdateAiDetectionEvent(result, receiveTime);

                        // [RTSP Index 0]
                        //
                        // 현재 [AI Detector Agent] 설정 기준:
                        // [RTSP Index 0] => [ONNX Index 1] [best_uav.onnx]
                        //
                        // [Drone] 전용 탐지 결과로 사용되며,
                        // [EO] 화면에 [Bounding Box]를 표시한다.
                        // [AI Detector] 표시 대상 [Bounding Box] 생성
                        //
                        // [Confidence] 기준 필터링 후
                        // [ConvertBoxForDisplay()]를 사용하여
                        // 현재 [EO] 영상 해상도 및 [Zoom] 상태가 반영된
                        // 화면 표시용 좌표로 변환한다.
                        //
                        List<AiDetectionBox> rtspIndex0DisplayBoxes = result.Boxes.Where(box => box.NormalizedConfidence >= _context.AiDisplayConfidenceThreshold).Select((box, index) =>
{
    // 2026-08-26: 화면에 남은 객체 기준으로 1부터 순번을 부여한다.
    box.DisplayOrder = index + 1;

    return ConvertBoxForDisplay(box, _context.EoVideoWidth, _context.EoVideoHeight);

}).ToList();

                        UpdateDetectionBoxes(_context.EoDetectionBoxes, rtspIndex0DisplayBoxes, 0, receiveTime);
                        break;
                    case 1:
                        if (!_context._isIrFrameDisplayed)
                        {
                            return;
                        }

                        _context.UpdateAiDetectionEvent(result, receiveTime);

                        // [RTSP Index 1]
                        //
                        // 현재 [AI Detector Agent] 설정 기준:
                        // [RTSP Index 1] => [ONNX Index 2] [best_yolov7.onnx]
                        //
                        // [YOLOv7] 탐지 결과로 사용되며,
                        // [IR] 화면에 [Bounding Box]를 표시한다.
                        // [AI Detector] 표시 대상 [Bounding Box] 생성
                        //
                        // [Confidence] 기준 필터링 후
                        // [ConvertBoxForDisplay()]를 사용하여
                        // 현재 [IR] 영상 해상도 및 [Zoom] 상태가 반영된
                        // 화면 표시용 좌표로 변환한다.
                        //
                        List<AiDetectionBox> rtspIndex1DisplayBoxes = result.Boxes.Where(box => box.NormalizedConfidence >= _context.AiDisplayConfidenceThreshold).Select((box, index) =>
{
    // 2026-08-26: EO와 동일한 AI BBox 순번 정책을 IR에도 적용한다.
    box.DisplayOrder = index + 1;

    return ConvertBoxForDisplay(box, _context.IrVideoWidth, _context.IrVideoHeight);

}).ToList();

                        UpdateDetectionBoxes(_context.IrDetectionBoxes, rtspIndex1DisplayBoxes, 1, receiveTime);
                        break;
                    default:
                        Console.WriteLine($"[AI DETECT] Unknown RTSP Index : {result.RtspIndex}");
                        break;
                }
            }, containsDisplayDetection);

            // 탐지 객체 존재 여부
            //
            // 객체가 없는 경우에는
            // Console 출력만 생략한다.
            //
            bool hasDetection = result.DetectionCount > 0 || result.Boxes.Count > 0;

            bool canPrintAiLog = hasDetection && (forcePrintLog || CanPrintAiDetectorLog());

            // [AI Detector] 탐지 [Packet]은 매우 빠르게 들어오므로,
            // 일정 시간 이내라면 [Console] 출력만 생략한다.
            //
            // 실제 수신 / 파싱 / 화면 반영은 계속 수행된다.
            //
            if (canPrintAiLog)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine("[AI DETECTOR PACKET] Detection Data");

                Console.WriteLine();

                Console.WriteLine($"[AI DETECT] [Frame Time]   : {result.FrameTime}");

                Console.WriteLine($"[AI DETECT] [Inference ms] : {result.InferenceMs}");

                Console.WriteLine($"[AI DETECT] [RTSP Index]   : {result.RtspIndex}");

                Console.WriteLine($"[AI DETECT] [Count]        : {result.DetectionCount}");

                Console.WriteLine($"[AI DETECT] [Box Count]    : {result.Boxes.Count}");

                for (int i = 0; i < result.Boxes.Count; i++)
                {
                    AiDetectionBox box = result.Boxes[i];

                    Console.WriteLine($"[AI BOX #{i + 1}] [ID] {box.ObjectId}, " + $"[Class] {box.ClassIndex}, " + $"[Confidence] {box.NormalizedConfidence * 100:F1}%, " + $"[Box] {box.Left}, {box.Top}, {box.Right}, {box.Bottom}");
                }

                ConsoleLogHelper.PrintLine();
            }
        }

        internal void QueueAiDetectionUiUpdate(int rtspIndex, Action updateAction, bool containsDetection)
        {
            bool shouldSchedule = false;

            lock (_aiUiUpdateSync)
            {
                if (_isApplicationShutdownRequested)
                {
                    return;
                }

                if (_pendingAiUiUpdates.ContainsKey(rtspIndex))
                {
                    _coalescedAiUiUpdateCount++;

                    bool pendingHasDetection = _pendingAiUiUpdateHasDetection.TryGetValue(rtspIndex, out bool pendingValue) && pendingValue;

                    // 2026-09-02: UI가 바쁜 순간 검출 Frame 직후의 빈 Frame이
                    // pending 작업을 덮어써 BBox가 한 번도 보이지 않는 현상을 막는다.
                    if (pendingHasDetection && !containsDetection)
                    {
                        return;
                    }
                }

                _pendingAiUiUpdates[rtspIndex] = updateAction;

                _pendingAiUiUpdateHasDetection[rtspIndex] = containsDetection;

                if (!_isAiUiDrainScheduled)
                {
                    _isAiUiDrainScheduled = true;
                    shouldSchedule = true;
                }
            }

            if (!shouldSchedule)
            {
                return;
            }

            Application application = Application.Current;

            Dispatcher dispatcher = application?.Dispatcher;

            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                ResetPendingAiUiUpdates();

                return;
            }

            try
            {
                dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(_context.DrainPendingAiUiUpdatesCallback));
            }
            catch (Exception ex)
            {
                ResetPendingAiUiUpdates();

                if (!_isApplicationShutdownRequested)
                {
                    ConsoleLogHelper.Error("AI UI / DISPATCH", "Detection UI update scheduling failed", ex);
                }
            }
        }

        internal void DrainPendingAiUiUpdates()
        {
            List<Action> pendingActions;

            int coalescedCount;

            lock (_aiUiUpdateSync)
            {
                if (_isApplicationShutdownRequested)
                {
                    _pendingAiUiUpdates.Clear();

                    _pendingAiUiUpdateHasDetection.Clear();

                    _isAiUiDrainScheduled = false;
                    _coalescedAiUiUpdateCount = 0;

                    return;
                }

                pendingActions = _pendingAiUiUpdates.OrderBy(item => item.Key).Select(item => item.Value).ToList();

                _pendingAiUiUpdates.Clear();

                _pendingAiUiUpdateHasDetection.Clear();

                _isAiUiDrainScheduled = false;
                coalescedCount = _coalescedAiUiUpdateCount;
                _coalescedAiUiUpdateCount = 0;
            }

            foreach (Action pendingAction in pendingActions)
            {
                try
                {
                    pendingAction();
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.Error("AI UI / DISPATCH", "Detection UI update failed", ex);
                }
            }

            // 2026-08-26: 최신 프레임 병합은 유지하되 큰 적체만 기록하여 UI/디스크 부하를 줄인다.
            if (coalescedCount >= 25)
            {
                ConsoleLogHelper.State("AI UI / DISPATCH", "Latest detection state restored after UI delay / CHANNELS=" + pendingActions.Count + " / COALESCED=" + coalescedCount);
            }
        }

        internal void ResetPendingAiUiUpdates()
        {
            lock (_aiUiUpdateSync)
            {
                _pendingAiUiUpdates.Clear();

                _pendingAiUiUpdateHasDetection.Clear();

                _isAiUiDrainScheduled = false;
                _coalescedAiUiUpdateCount = 0;
            }
        }

        internal bool TryRunAiUiAction(Action action, string operationName)
        {
            if (_isApplicationShutdownRequested)
            {
                return false;
            }

            Application application = Application.Current;

            Dispatcher dispatcher = application?.Dispatcher;

            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                return false;
            }

            try
            {
                if (dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    dispatcher.Invoke(action);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!_isApplicationShutdownRequested)
                {
                    ConsoleLogHelper.Error("AI UI / DISPATCH", operationName + " failed", ex);
                }

                return false;
            }
        }

        internal void ShutdownForApplicationExit()
        {
            lock (_aiUiUpdateSync)
            {
                if (_isApplicationShutdownRequested)
                {
                    return;
                }

                _isApplicationShutdownRequested = true;

                _pendingAiUiUpdates.Clear();

                _pendingAiUiUpdateHasDetection.Clear();

                _isAiUiDrainScheduled = false;
                _coalescedAiUiUpdateCount = 0;
            }

            ConsoleLogHelper.State("APPLICATION / SHUTDOWN", "ViewModel resource cleanup started");

            try
            {
                _context._testProgramEventTimer?.Stop();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("APPLICATION / SHUTDOWN", "Event timer stop failed", ex);
            }

            try
            {
                _context._aiDetectorClientService.PacketReceived -= _context.OnAiDetectorPacketReceivedCallback;

                _context._aiDetectorClientService.StopAutoReconnect();

                _context._aiDetectorClientService.Disconnect();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("APPLICATION / SHUTDOWN", "AI detector cleanup failed", ex);
            }

            try
            {
                _context.Disconnect();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("APPLICATION / SHUTDOWN", "Device/video cleanup failed", ex);

                _context._cts?.Cancel();

                _context._controlAgentReconnectCts?.Cancel();

                _context._videoReconnectCts?.Cancel();
            }

            ConsoleLogHelper.State("APPLICATION / SHUTDOWN", "ViewModel resource cleanup completed");
        }

        internal void HandleAiDetectorInfoResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 51] Detector Info");

            Console.WriteLine("[AI PAYLOAD] " + payload);

            ConsoleLogHelper.PrintLine();
        }

        internal void HandleAiDetectorRtspResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 52] RTSP List");

            Console.WriteLine();

            List<AiRtspInfo> rtspList = _context._aiDetectorPacketParser.ParseRtspListPayload(payload);

            foreach (AiRtspInfo rtsp in rtspList)
            {
                Console.WriteLine($"[RTSP] [Index] {rtsp.Index}, [URL] {ConsoleLogHelper.MaskRtspPassword(rtsp.Url)}");
            }

            // [AI Detector Agent][RTSP] 조회 결과를 [UI Collection]에 반영
            UpdateAiRtspList(rtspList);

            ConsoleLogHelper.PrintLine();
        }

        internal void HandleAiDetectorOnnxResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 53] ONNX List");

            List<AiOnnxInfo> onnxList = _context._aiDetectorPacketParser.ParseOnnxListPayload(payload);

            foreach (AiOnnxInfo onnx in onnxList)
            {
                Console.WriteLine($"[ONNX] [Index] {onnx.Index}, " + $"[File] {onnx.FileName}, " + $"[Classes] {string.Join(", ", onnx.Classes)}");
            }

            // [AI Detector Agent] [ONNX] 조회 결과를 [UI Collection]에 반영
            UpdateAiOnnxList(onnxList);

            ConsoleLogHelper.PrintLine();
        }

        internal void HandleAiDetectorMappingResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[AI DETECTOR RESPONSE] Mapping Info");

            Console.WriteLine();

            List<AiMappingInfo> mappingList = _context._aiDetectorPacketParser.ParseMappingPayload(payload);

            foreach (AiMappingInfo mapping in mappingList)
            {
                Console.WriteLine($"[MAPPING] [RTSP] {mapping.RtspIndex}, " + $"[ONNX] {mapping.OnnxIndex}, " + $"[Confidence] {mapping.Confidence:F2}, " + $"[IOU] {mapping.Iou:F2}");
            }

            // [AI Detector Agent] [RTSP] / [ONNX] Mapping 조회 결과를 [UI Collection]에 반영
            UpdateAiMappingList(mappingList);

            ConsoleLogHelper.PrintLine();
        }

        internal async Task<bool> RequestAiDetectorInfoAsync()
        {
            byte[] packet = _context._aiPacketBuilder.BuildAiDetectorInfoRequest();

            return await _context._aiDetectorClientService.SendAsync(packet);
        }

        internal async Task<bool> RequestAiDetectorRtspAddressSetAsync()
        {
            string eoAddress = _context.AiRtsp0Address?.Trim();

            string irAddress = _context.AiRtsp1Address?.Trim();

            if (!IsValidRtspAddress(eoAddress) || !IsValidRtspAddress(irAddress) || string.Equals(eoAddress, irAddress, StringComparison.OrdinalIgnoreCase))
            {
                _context.AiSettingStatusText = "RTSP CONFIG ERROR";

                ConsoleLogHelper.Warning("AI RTSP CONFIG ERROR", "Invalid or duplicated EO/IR RTSP URL");

                return false;
            }

            // [Viewer] 영상 연결 주소 갱신
            //
            // 이후 장비 연결 해제 후 다시 연결하면
            // 변경된 RTSP 주소로 [EO] / [IR] 영상 연결을 시도한다.
            //
            _context.EoSourceAddress = eoAddress;
            _context.IrSourceAddress = irAddress;
            _context._selectedEoRtspSource = _context.SelectedAiEoRtspSource;
            _context._selectedIrRtspSource = _context.SelectedAiIrRtspSource;

            _context.SaveRtspCommunicationSettings();

            _context.OnPropertyChanged(nameof(_context.EoSourceAddress));

            _context.OnPropertyChanged(nameof(_context.IrSourceAddress));

            byte[] packet = _context._aiPacketBuilder.BuildRtspAddressSetRequest(_context.AiRtsp0Address, _context.AiRtsp1Address);

            ResetAiRtspVideoSizes();

            return await _context._aiDetectorClientService.SendAsync(packet);
        }

        internal async Task<bool> RequestAiDetectorRtspAddressAsync()
        {
            byte[] packet = _context._aiPacketBuilder.BuildRtspAddressRequest();

            return await _context._aiDetectorClientService.SendAsync(packet);
        }

        internal async Task<bool> RequestAiDetectorOnnxListAsync()
        {
            if (await TryLoadAiDetectorModelListFromHttpAsync())
            {
                return true;
            }

            byte[] packet = _context._aiPacketBuilder.BuildOnnxListRequest();

            ConsoleLogHelper.Warning("AI MODEL / HTTP", "HTTP model list unavailable; TCP CMD 04 fallback requested");

            return await _context._aiDetectorClientService.SendAsync(packet);
        }

        internal async Task<bool> TryLoadAiDetectorModelListFromHttpAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_context._aiControlAgentIp))
                {
                    return false;
                }

                Uri configUri = new UriBuilder(Uri.UriSchemeHttp, _context._aiControlAgentIp.Trim(), 5101, "api/config").Uri;

                using (HttpClientHandler handler = new HttpClientHandler
                {
                    UseProxy = false
                }

                )
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(3);

                    string json = await client.GetStringAsync(configUri);

                    List<AiOnnxInfo> models = ParseAiModelsFromHttpConfig(json);

                    if (models.Count == 0)
                    {
                        ConsoleLogHelper.Warning("AI MODEL / HTTP", "Config response contained no ONNX/HEF model names");

                        return false;
                    }

                    UpdateAiOnnxList(models);

                    ConsoleLogHelper.State("AI MODEL / HTTP", "Model and class list loaded / HOST=" + _context._aiControlAgentIp + " / PORT=5101 / COUNT=" + models.Count + " / CLASS_COUNT=" + models.Sum(model => model.Classes.Count));

                    return true;
                }
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error("AI MODEL / HTTP", "Model list request failed; TCP fallback will be used", exception);

                return false;
            }
        }

        internal static List<AiOnnxInfo> ParseAiModelsFromHttpConfig(string json)
        {
            List<AiOnnxInfo> models = new List<AiOnnxInfo>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return models;
            }

            MatchCollection objectMatches = Regex.Matches(json, @"\{(?<body>[^{}]*)\}", RegexOptions.Singleline);

            foreach (Match objectMatch in objectMatches)
            {
                string body = objectMatch.Groups["body"].Value;

                Match fileMatch = Regex.Match(body, @"""filename""\s*:\s*""(?<name>(?:\\.|[^""\\])*?\.(?:onnx|hef))""", RegexOptions.IgnoreCase);

                if (!fileMatch.Success)
                {
                    continue;
                }

                string rawName = Regex.Unescape(fileMatch.Groups["name"].Value).Replace("\\/", "/");

                int separatorIndex = Math.Max(rawName.LastIndexOf('/'), rawName.LastIndexOf('\\'));

                string fileName = separatorIndex >= 0 ? rawName.Substring(separatorIndex + 1) : rawName;

                AiOnnxInfo model = new AiOnnxInfo
                {
                    Index = models.Count,
                    FileName = fileName
                };

                Match classesMatch = Regex.Match(body, @"""classes""\s*:\s*\[(?<items>.*?)\]", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (classesMatch.Success)
                {
                    MatchCollection classMatches = Regex.Matches(classesMatch.Groups["items"].Value, @"""(?<class>(?:\\.|[^""\\])*)""");

                    foreach (Match classMatch in classMatches)
                    {
                        string className = Regex.Unescape(classMatch.Groups["class"].Value).Trim();

                        if (!string.IsNullOrWhiteSpace(className))
                        {
                            model.Classes.Add(className);
                        }
                    }
                }

                models.Add(model);
            }

            return models;
        }

        internal async Task<bool> RequestAiDetectorMappingSetAsync()
        {
            byte[] packet = _context._aiPacketBuilder.BuildRtspOnnxMappingSetRequest(_context.AiRtsp0OnnxIndex, _context.AiRtsp1OnnxIndex, _context.AiMappingConfidence, _context.AiMappingIou);

            return await _context._aiDetectorClientService.SendAsync(packet);
        }

        internal async Task<bool> RequestAiDetectorMappingAsync()
        {
            byte[] packet = _context._aiPacketBuilder.BuildRtspOnnxMappingRequest();

            return await _context._aiDetectorClientService.SendAsync(packet);
        }

        internal async Task<bool> RequestAiDetectorRtspVideoSizeAsync(int rtspIndex)
        {
            byte[] packet = _context._aiPacketBuilder.BuildRtspVideoSizeRequest(rtspIndex);

            bool sent = await _context._aiDetectorClientService.SendAsync(packet);

            ConsoleLogHelper.Command("AI RTSP VIDEO SIZE", $"CMD=07 / INDEX={rtspIndex} / SENT={sent}");

            return sent;
        }

        internal void ResolveAiDetectionClassNames(AiDetectionResult result)
        {
            if (result == null || result.Boxes == null)
            {
                return;
            }

            int selectedOnnxIndex = result.RtspIndex == 0 ? _context.AiRtsp0OnnxIndex : result.RtspIndex == 1 ? _context.AiRtsp1OnnxIndex : -1;

            AiMappingInfo mapping = _context.AiMappingList.LastOrDefault(item => item.RtspIndex == result.RtspIndex);

            int mappedOnnxIndex = mapping?.OnnxIndex ?? selectedOnnxIndex;

            AiOnnxInfo model = _context.AiOnnxList.FirstOrDefault(item => item.Index == mappedOnnxIndex);

            foreach (AiDetectionBox box in result.Boxes)
            {
                box.ModelFileName = model?.FileName;

                box.ResolvedClassName = model != null && box.ClassIndex >= 0 && box.ClassIndex < model.Classes.Count ? model.Classes[box.ClassIndex] : null;
            }
        }

        internal void UpdateDetectionBoxes(ObservableCollection<AiDetectionBox> targetBoxes, List<AiDetectionBox> sourceBoxes, int rtspIndex, DateTime receiveTime)
        {
            bool hasDetection = sourceBoxes != null && sourceBoxes.Count > 0;

            DateTime lastDetectionTime = rtspIndex == 0 ? _lastEoAiDisplayDetectionTime : _lastIrAiDisplayDetectionTime;

            bool holdActive = !hasDetection && (receiveTime - lastDetectionTime).TotalMilliseconds >= 0 && (receiveTime - lastDetectionTime).TotalMilliseconds < AiDisplayHoldMilliseconds;

            SetAiDisplayHoldState(rtspIndex, holdActive);

            if (hasDetection)
            {
                if (rtspIndex == 0)
                {
                    _lastEoAiDisplayDetectionTime = receiveTime;
                }
                else
                {
                    _lastIrAiDisplayDetectionTime = receiveTime;
                }
            }

            // 2026-09-09: 객체별 Event Track을 단일 상태 원본으로 사용한다.
            // 이번 패킷에서 빠진 객체도 750ms Hold 동안 기존 BBox와 ACTIVE Event를 함께 유지한다.
            Dictionary<string, AiDetectionBox> merged = new Dictionary<string, AiDetectionBox>(StringComparer.Ordinal);

            foreach (AiDetectionBox box in sourceBoxes ?? new List<AiDetectionBox>())
            {
                merged[GetAiEventKey(rtspIndex, box)] = box;
            }

            foreach (AiDetectionBox previousBox in targetBoxes)
            {
                string key = GetAiEventKey(rtspIndex, previousBox);

                if (!merged.ContainsKey(key) && _context._activeAiEvents.ContainsKey(key))
                {
                    merged[key] = previousBox;
                }
            }

            targetBoxes.Clear();

            int displayOrder = 1;

            int frameWidth = Math.Max(1, rtspIndex == 0 ? _context.EoVideoWidth : _context.IrVideoWidth);

            int frameHeight = Math.Max(1, rtspIndex == 0 ? _context.EoVideoHeight : _context.IrVideoHeight);

            foreach (AiDetectionBox box in merged.Values.OrderBy(item => item.DetectionEventId))
            {
                box.DisplayOrder = displayOrder++;

                PrepareAiOverlayLayout(box, frameWidth, frameHeight);

                targetBoxes.Add(box);
            }

            // 2026-09-17: IR/EO 하이브리드 탐지에서 AI와 로컬 FIRE/SMOKE가
            // 같은 객체를 표시하면 AI BBox를 우선하고 로컬 중복 표시를 제거한다.
            SuppressLocalBoxesOverlappingAi(rtspIndex == 0 ? _context.EoFireSmokeDetectionBoxes : _context.IrFireSmokeDetectionBoxes, targetBoxes);

            // 2026-09-22: AI FIRE/FLAME/SMOKE BBox의 생성·유지·제거 상태를
            // 메인/분리 영상 상단 경고에 즉시 반영한다.
            _context.NotifyFireSmokeWarningStateChanged(rtspIndex);

#if DEBUG
            int channelEventCount = _context._activeAiEvents.Count(item => item.Value.RtspIndex == rtspIndex);

            if (targetBoxes.Count != channelEventCount)
            {
                ConsoleLogHelper.Warning("EVENT SYNC WARNING", "AI_BBOX_ACTIVE=" + targetBoxes.Count + " / AI_EVENT_ACTIVE=" + channelEventCount + " / CHANNEL=" + (rtspIndex == 0 ? "EO" : "IR"));
            }
#endif
        }

        internal static void PrepareAiOverlayLayout(AiDetectionBox box, int frameWidth, int frameHeight)
        {
            const int strokeInset = 2;

            const int labelHeight = 24;

            int left = Math.Max(strokeInset, Math.Min(frameWidth - strokeInset - 1, box.Left));

            int top = Math.Max(strokeInset, Math.Min(frameHeight - strokeInset - 1, box.Top));

            int right = Math.Max(left + 1, Math.Min(frameWidth - strokeInset, box.Right));

            int bottom = Math.Max(top + 1, Math.Min(frameHeight - strokeInset, box.Bottom));

            int labelWidth = Math.Max(40, Math.Min((int)Math.Ceiling(box.DisplayText.Length * 9.5 + 12.0), frameWidth - 4));

            int labelLeft = Math.Max(2, Math.Min(frameWidth - labelWidth - 2, left));

            int labelTop = top - labelHeight >= 2 ? top - labelHeight : Math.Max(2, Math.Min(frameHeight - labelHeight - 2, top + 2));

            int overlayLeft = Math.Min(left, labelLeft);

            int overlayTop = Math.Min(top, labelTop);

            box.OverlayLeft = overlayLeft;
            box.OverlayTop = overlayTop;

            box.OverlayWidth = Math.Max(1, Math.Max(right, labelLeft + labelWidth) - overlayLeft);

            box.OverlayHeight = Math.Max(1, Math.Max(bottom, labelTop + labelHeight) - overlayTop);

            box.BoxOffsetX = left - overlayLeft;

            box.BoxOffsetY = top - overlayTop;

            box.RenderWidth = right - left;

            box.RenderHeight = bottom - top;

            box.LabelOffsetX = labelLeft - overlayLeft;

            box.LabelOffsetY = labelTop - overlayTop;

            box.LabelWidth = labelWidth;
        }

        internal void SetAiDisplayHoldState(int rtspIndex, bool active)
        {
            bool previous = rtspIndex == 0 ? _isEoAiDisplayHoldActive : _isIrAiDisplayHoldActive;

            if (previous == active)
            {
                return;
            }

            if (rtspIndex == 0)
            {
                _isEoAiDisplayHoldActive = active;
            }
            else
            {
                _isIrAiDisplayHoldActive = active;
            }

            ConsoleLogHelper.State("AI BBOX HOLD", (active ? "Short detection gap retained" : "Hold ended") + " / CHANNEL=" + (rtspIndex == 0 ? "EO" : "IR") + " / HOLD_MS=" + AiDisplayHoldMilliseconds);
        }

        internal void ClearAiDetectionState(string reason, bool writeAudit = true)
        {
            Interlocked.Increment(ref _aiDetectionGeneration);

            lock (_aiUiUpdateSync)
            {
                _pendingAiUiUpdates.Clear();

                _pendingAiUiUpdateHasDetection.Clear();
            }

            int eoBoxCount = 0;

            int irBoxCount = 0;

            int activeEventCount = 0;

            _context.ClearAiSmokeCandidateSnapshots();

            Action clearAction = () =>
            {
                eoBoxCount = _context.EoDetectionBoxes.Count;
                irBoxCount = _context.IrDetectionBoxes.Count;
                activeEventCount = _context._activeAiEvents.Count;

                _context.EoDetectionBoxes.Clear();

                _context.IrDetectionBoxes.Clear();

                _context.NotifyFireSmokeWarningStateChanged(0);

                _context.NotifyFireSmokeWarningStateChanged(1);

                _lastEoAiDisplayDetectionTime = DateTime.MinValue;
                _lastIrAiDisplayDetectionTime = DateTime.MinValue;
                _isEoAiDisplayHoldActive = false;
                _isIrAiDisplayHoldActive = false;

                DateTime clearedTime = DateTime.Now;

                foreach (AiEventTrack activeTrack in _context._activeAiEvents.Values.ToList())
                {
                    FireEventRecord activeEvent = activeTrack.Event;

                    activeEvent.MarkCleared(clearedTime);

                    try
                    {
                        if (writeAudit)
                        {
                            AppendFireEventAudit(activeEvent, "CLEARED");
                        }
                    }
                    catch (Exception auditException)
                    {
                        ConsoleLogHelper.Error("AI EVENT", "Disconnect audit write failed / EVENT_ID=" + activeEvent.EventId, auditException);
                    }
                }

                _context._activeAiEvents.Clear();

                _context.ActiveAiCount = 0;

                _context.NotifyAiEventSummaryChanged();
            };

            try
            {
                if (!TryRunAiUiAction(clearAction, "Detection state clear"))
                {
                    return;
                }

                ConsoleLogHelper.State("AI DETECTOR", "Detection state cleared / REASON=" + reason + " / EO_BOXES=" + eoBoxCount + " / IR_BOXES=" + irBoxCount + " / ACTIVE_EVENTS=" + activeEventCount);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("AI DETECTOR", "Detection state clear failed / REASON=" + reason, ex);
            }
        }

        internal void UpdateAiRtspList(List<AiRtspInfo> rtspList)
        {
            TryRunAiUiAction(() =>
            {
                _context.AiRtspList.Clear();

                foreach (AiRtspInfo rtspInfo in rtspList)
                {
                    _context.AiRtspList.Add(rtspInfo);
                }
            }, "RTSP list update");
        }

        internal void UpdateAiOnnxList(List<AiOnnxInfo> onnxList)
        {
            TryRunAiUiAction(() =>
            {
                _context.AiOnnxList.Clear();

                foreach (AiOnnxInfo onnxInfo in onnxList)
                {
                    _context.AiOnnxList.Add(onnxInfo);
                }

                /*
                 * 2026-08-31: 최초 ONNX 목록 수신 시 EO와 IR 모두
                 * Real-Time Smoke/Fire 모델을 기본 Mapping으로 선택한다.
                 * 이후 사용자가 UI에서 변경한 값은 목록 새로 고침으로 덮어쓰지 않는다.
                 */
                if (!_hasAppliedDefaultAiModelMapping)
                {
                    AiOnnxInfo smokeDefault = _context.AiOnnxList.FirstOrDefault(model => string.Equals(model.FileName, "Real-Time-Smoke-Fire-Detection-YOLO11n-main.onnx", StringComparison.OrdinalIgnoreCase));

                    if (smokeDefault != null)
                    {
                        _context.AiRtsp0OnnxIndex = smokeDefault.Index;
                        _context.AiRtsp1OnnxIndex = smokeDefault.Index;
                    }

                    _hasAppliedDefaultAiModelMapping = true;

                    ConsoleLogHelper.State("AI DETECTOR", "Default model mapping selected / EO=" + (smokeDefault?.FileName ?? "CURRENT") + " / IR=" + (smokeDefault?.FileName ?? "CURRENT"));
                }

                // [ONNX] 목록 조회 후 선택값 보정
                //
                // 현재 선택된 [ONNX Index]가 목록에 없으면
                // 데모 기본 Mapping 기준으로 다시 설정한다.
                //
                if (!_context.AiOnnxList.Any(onnx => onnx.Index == _context.AiRtsp0OnnxIndex))
                {
                    _context.AiRtsp0OnnxIndex = _context.AiOnnxList.Any(onnx => onnx.Index == 1) ? 1 : _context.AiOnnxList.FirstOrDefault()?.Index ?? 0;
                }

                if (!_context.AiOnnxList.Any(onnx => onnx.Index == _context.AiRtsp1OnnxIndex))
                {
                    _context.AiRtsp1OnnxIndex = _context.AiOnnxList.Any(onnx => onnx.Index == 2) ? 2 : _context.AiOnnxList.FirstOrDefault()?.Index ?? 0;
                }

                _context.OnPropertyChanged(nameof(_context.AiRtsp0OnnxIndex));

                _context.OnPropertyChanged(nameof(_context.AiRtsp1OnnxIndex));
            }, "Model list update");
        }

        internal void UpdateAiMappingList(List<AiMappingInfo> mappingList)
        {
            TryRunAiUiAction(() =>
            {
                _context.AiMappingList.Clear();

                foreach (AiMappingInfo mappingInfo in mappingList)
                {
                    _context.AiMappingList.Add(mappingInfo);
                }
            }, "Mapping list update");
        }

        internal bool CanPrintAiDetectorLog()
        {
            if ((DateTime.Now - _context._lastAiDetectorLogTime).TotalSeconds < AiDetectorLogIntervalSeconds)
            {
                return false;
            }

            _context._lastAiDetectorLogTime = DateTime.Now;

            return true;
        }

        internal AiDetectionBox ConvertBoxForDisplay(AiDetectionBox sourceBox, int videoWidth, int videoHeight)
        {
            return sourceBox;
        }

        #endregion

    }
}
