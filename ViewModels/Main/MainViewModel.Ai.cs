using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// AI Detector 연결, Packet 처리, 설정 조회와 Bounding Box 반영을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        /*
         * 2026-08-26: 파노라마 촬영 중 UI Dispatcher가 일시적으로 바빠져도
         * AI TCP 수신 Thread가 멈추지 않도록 채널별 최신 UI 갱신만 보관한다.
         */
        private readonly object _aiUiUpdateSync = new object();
        private readonly Dictionary<int, Action> _pendingAiUiUpdates =
            new Dictionary<int, Action>();
        private readonly Dictionary<int, bool> _pendingAiUiUpdateHasDetection =
            new Dictionary<int, bool>();
        private bool _isAiUiDrainScheduled;
        private bool _isApplicationShutdownRequested;
        private int _coalescedAiUiUpdateCount;
        private bool _hasAppliedDefaultAiModelMapping;
        private const int AiDisplayHoldMilliseconds = 750;
        private DateTime _lastEoAiDisplayDetectionTime = DateTime.MinValue;
        private DateTime _lastIrAiDisplayDetectionTime = DateTime.MinValue;
        private bool _isEoAiDisplayHoldActive;
        private bool _isIrAiDisplayHoldActive;

        #region [AI Detector Communication]

        #region [AI Detector Connect]

        /// <summary>
        /// [AI CONNECT] 버튼 기준 수동 연결 및 초기 설정 적용
        ///
        /// 프로그램 시작 또는 장비 연결 시에는 AI Agent에 자동 연결하지 않는다.
        /// 사용자가 [AI CONNECT]를 누른 경우에만 UI의 IP / Port로 1회 연결하고,
        /// 연결 성공 뒤 RTSP / 모델 / Mapping 정보를 순서대로 적용 및 조회한다.
        /// </summary>
        private async Task ConnectAiAgentFromSettingAsync()
        {
            AiPowerStatusText = "OFF";
            AiSettingStatusText = "[AI] Connecting...";
            SetFireSmokeDetectorsForAiConnection(false, "AI connect initialization");
            // 2026-08-25: 재연결 시작 시 이전 연결의 BBox와 ACTIVE 상태를 제거한다.
            ClearAiDetectionState("Connect initialization");

            try
            {
                /*
                 * 과거 Auto Reconnect가 실행된 상태가 남아 있더라도
                 * 버튼 기반 수동 연결 정책과 충돌하지 않도록 먼저 종료한다.
                 */
                _aiDetectorClientService.StopAutoReconnect();
                _aiDetectorClientService.Disconnect();

                bool connected =
                    await _aiDetectorClientService.ConnectAsync(
                        AiControlAgentIp,
                        AiAgentPort);

                if (!connected)
                {
                    AiSettingStatusText = "[AI] Connect Failed";
                    return;
                }

                AiPowerStatusText = "ON";
                AiSettingStatusText = "[AI] Connected";
                // 2026-08-31: 실제 TCP 연결 성공 시 FIRE/SMOKE 분석을 함께 활성화한다.
                SetFireSmokeDetectorsForAiConnection(true, "AI connected");

                if (!await RequestAiDetectorRtspAddressSetAsync())
                {
                    AiSettingStatusText = "[AI] RTSP Apply Failed";
                    return;
                }

                await Task.Delay(300);

                if (!await RequestAiDetectorInfoAsync() ||
                    !await RequestAiDetectorRtspAddressAsync() ||
                    !await RequestAiDetectorOnnxListAsync())
                {
                    AiSettingStatusText = "[AI] Initial Setting Incomplete";
                    return;
                }

                // 2026-08-31: ONNX 목록 응답이 UI 기본 선택에 반영된 뒤 Mapping을
                // 전송해야 EO/IR 모두 Real-Time Smoke 모델로 실제 Agent에 적용된다.
                await WaitForInitialAiModelSelectionAsync();

                if (!await RequestAiDetectorMappingSetAsync() ||
                    !await RequestAiDetectorMappingAsync())
                {
                    AiSettingStatusText = "[AI] Initial Setting Incomplete";
                    return;
                }

                AiSettingStatusText = "[AI] Connect / Setting Complete";
            }
            catch (Exception ex)
            {
                AiPowerStatusText = "OFF";
                AiSettingStatusText = "[AI] Connect / Setting Incomplete";
                SetFireSmokeDetectorsForAiConnection(false, "AI connect exception");

                ConsoleLogHelper.Error(
                    "AI DETECTOR",
                    "Connect / setting exception / " + ex.Message);
            }

        }

        /// <summary>
        /// 2026-08-31: 최초 연결의 ONNX 목록 응답과 기본 모델 선택 완료를 제한 시간 동안 기다린다.
        /// 응답이 늦거나 모델이 없는 경우에는 기존 선택값으로 계속 연결한다.
        /// </summary>
        private async Task WaitForInitialAiModelSelectionAsync()
        {
            const string DefaultModelName =
                "Real-Time-Smoke-Fire-Detection-YOLO11n-main.onnx";

            for (int attempt = 0; attempt < 20; attempt++)
            {
                if (AiOnnxList.Any(model =>
                        string.Equals(
                            model.FileName,
                            DefaultModelName,
                            StringComparison.OrdinalIgnoreCase)) &&
                    AiRtsp0OnnxIndex == AiRtsp1OnnxIndex)
                {
                    return;
                }

                await Task.Delay(75);
            }

            ConsoleLogHelper.Warning(
                "AI DETECTOR",
                "Default smoke model selection timeout; current mapping will be used");
        }

        /// <summary>
        /// [AI DISCONNECT] 버튼 기준 수동 연결 해제
        ///
        /// 자동 재연결 요청을 함께 중단하여 사용자가 다시
        /// [AI CONNECT]를 누르기 전에는 AI Agent에 재접속하지 않는다.
        /// </summary>
        private void DisconnectAiAgent()
        {
            AiPowerStatusText = "OFF";
            AiSettingStatusText = "[AI] Disconnecting...";

            try
            {
                _aiDetectorClientService.StopAutoReconnect();
                _aiDetectorClientService.Disconnect();

                // 2026-08-25: 수동 연결 해제 즉시 EO/IR BBox와 AI ACTIVE 상태를 정리한다.
                ClearAiDetectionState("Manual disconnect");
                SetFireSmokeDetectorsForAiConnection(false, "AI manual disconnect");
                AiSettingStatusText = "[AI] Disconnected";

                ConsoleLogHelper.Command(
                    "AI DETECTOR",
                    "Manual disconnect completed / Detection overlay cleared");
            }
            catch (Exception ex)
            {
                ClearAiDetectionState("Manual disconnect exception fallback");
                SetFireSmokeDetectorsForAiConnection(false, "AI disconnect exception");
                AiSettingStatusText = "[AI] Disconnect Incomplete";
                ConsoleLogHelper.Error(
                    "AI DETECTOR",
                    "Manual disconnect exception",
                    ex);
            }

        }

        #endregion

        #region [AI Detector Receive]

        /// <summary>
        /// [AI Detector Agent] [TCP] 수신 [Packet] 처리 함수
        ///
        /// 공통 [Packet] 구조를 먼저 검증한 뒤,
        /// [CMD] 값에 따라 응답 처리 함수를 분기한다.
        /// </summary>
        private void OnAiDetectorPacketReceived(
            byte[] packet,
            DateTime receiveTime)
        {

            /// <summary>
            /// [AI Detector] 공통 [Packet] 구조 파싱
            ///
            /// [STX] / [CMD] / [SIZE] / [Payload] / [Checksum] / [ETX] 검증 후,
            /// [CMD]와 [Payload]를 추출한다.
            /// </summary>
            if (!_aiDetectorPacketParser.TryParseCommonPacket(
                packet,
                out string command,
                out string payload))
            {
                return;
            }

            /// <summary>
            /// [CMD] 기준 응답 분기
            ///
            /// [CMD 51] : [AI Detector Info] 응답
            /// [CMD 52] : [RTSP] 주소 조회 응답
            /// [CMD 53] : [ONNX] 목록 조회 응답
            /// [CMD 54] : [RTSP] / [ONNX] Mapping 조회 응답
            /// [CMD 55] : 탐지데이터 응답
            /// [CMD 56] : Mapping 설정 응답 또는 확장 Mapping 응답
            /// </summary>
            switch (command)
            {
                case "50":
                    /// <summary>
                    /// [CMD 50] 설정 요청 결과 응답
                    ///
                    /// [CMD 02] RTSP 주소 설정,
                    /// [CMD 05] RTSP / ONNX Mapping 설정 등
                    /// 설정 계열 요청 이후 수신되는 결과 Packet.
                    ///
                    /// 현재 확인 기준:
                    /// Payload "o" => 설정 성공
                    /// 그 외 값       => Agent 응답 원문 출력
                    /// </summary>
                    if (payload == "o")
                    {
                        Console.WriteLine();
                        Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 50] Setting Result : OK");
                        AiSettingStatusText = "[AI] Setting Result : OK";
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            $"[AI DETECTOR RESPONSE] [CMD 50] Setting Result : {payload}");

                        AiSettingStatusText =
                            $"[AI] Setting Result : {payload}";
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
                    HandleAiDetectorDetectionPacket(
                        packet,
                        receiveTime);
                    break;

                case "56":
                    HandleAiDetectorMappingResponse(payload);
                    break;

                default:
                    Console.WriteLine(
                        $"[AI DETECTOR] Unknown CMD : {command}, Payload : {payload}");
                    break;
            }

        }

        /// <summary>
        /// [CMD 55] 탐지데이터 [Packet] 처리
        ///
        /// [AiDetectorPacketParser]에서 [AiDetectionResult]로 변환한 뒤,
        /// 화면 [Bounding Box] 반영 및 로그 출력을 수행한다.
        /// </summary>
        private void HandleAiDetectorDetectionPacket(
            byte[] packet,
            DateTime receiveTime)
        {
            // 2026-08-25: Disconnect 이후 도착한 지연 CMD 55가 BBox를 다시 만들지 못하게 한다.
            if (AiPowerStatusText != "ON")
            {
                return;
            }

            if (!_aiDetectorPacketParser.TryParseDetectionPacket(
                packet,
                out AiDetectionResult result))
            {
                return;
            }

            HandleAiDetectionResult(
                result,
                receiveTime);
        }

        #endregion

        #region [AI Detector Packet Handling]

        /// <summary>
        /// [AI Detector] 탐지 결과 처리 함수
        ///
        /// [AI Detector Agent]에서 파싱된 탐지 결과를
        /// [RTSP Index] 기준으로 화면 [Bounding Box] 컬렉션에 반영한다.
        ///
        /// 현재 기준:
        /// [RTSP Index 0] => [EO] 화면 표시
        /// [RTSP Index 1] => 수신은 하지만 [IR] 화면에는 표시하지 않음
        ///
        /// 현재 [AI Detector Agent]에서 [RTSP Index 0] / [1] 데이터가 모두 수신되므로,
        /// 데모 화면 기준상 [EO]에만 [Bounding Box]를 표시하고
        /// [IR] [Bounding Box]는 항상 제거한다.
        /// </summary>
        private void HandleAiDetectionResult(
            AiDetectionResult result,
            DateTime receiveTime,
            bool forcePrintLog = false)
        {
            /*
             * 2026-08-26: Dispatcher.Invoke는 UI가 파노라마 촬영 처리 중일 때
             * AI 수신 Thread까지 대기시켰다. 비동기 최신값 병합 Queue를 사용하여
             * 촬영 중에도 TCP 수신을 계속하고 UI 복귀 직후 최신 BBox를 반영한다.
             */
            bool containsDisplayDetection =
                result.Boxes.Any(box =>
                    box.NormalizedConfidence >= AiDisplayConfidenceThreshold);
            QueueAiDetectionUiUpdate(result.RtspIndex, () =>
            {
                // 2026-08-31: PTZ 이동·잔진동 프레임은 AI Agent가 SMOKE로
                // 오인할 수 있으므로 파노라마 안정 구간에서만 신규 AI 결과를 반영한다.
                if (IsPanoramaMotionDetectionSuppressed())
                {
                    ClearAiSmokeCandidateSnapshots();
                    if (_activeAiEvents.TryGetValue(
                            result.RtspIndex,
                            out FireEventRecord motionClearedEvent))
                    {
                        motionClearedEvent.MarkCleared(receiveTime);
                        _activeAiEvents.Remove(result.RtspIndex);
                        ActiveAiCount = _activeAiEvents.Count;
                        AppendFireEventAudit(motionClearedEvent, "PANORAMA_MOVE_CLEARED");
                        NotifyAiEventSummaryChanged();
                        ConsoleLogHelper.State(
                            "AI EVENT",
                            "AI event cleared for panorama motion / EVENT_ID=" +
                            motionClearedEvent.EventId +
                            " / CAMERA=" + motionClearedEvent.Camera);
                    }

                    if (result.RtspIndex == 0)
                    {
                        EoDetectionBoxes.Clear();
                    }
                    else if (result.RtspIndex == 1)
                    {
                        IrDetectionBoxes.Clear();
                    }

                    return;
                }

                // 2026-08-31: 현재 RTSP Mapping과 모델 클래스 목록으로 Class Index를 실제 명칭으로 해석한다.
                ResolveAiDetectionClassNames(result);
                UpdateAiSmokeCandidateSnapshot(result, receiveTime);
                UpdateAiDetectionEvent(result, receiveTime);
                int detectionEventId =
                    _activeAiEvents.TryGetValue(result.RtspIndex, out FireEventRecord activeEvent)
                        ? activeEvent.EventId
                        : 0;

                switch (result.RtspIndex)
                {
                    case 0:
                        if (!_isEoFrameDisplayed)
                        {
                            return;
                        }

                        /// <summary>
                        /// [RTSP Index 0]
                        ///
                        /// 현재 [AI Detector Agent] 설정 기준:
                        /// [RTSP Index 0] => [ONNX Index 1] [best_uav.onnx]
                        ///
                        /// [Drone] 전용 탐지 결과로 사용되며,
                        /// [EO] 화면에 [Bounding Box]를 표시한다.
                        /// </summary>

                        /// <summary>
                        /// [AI Detector] 표시 대상 [Bounding Box] 생성
                        ///
                        /// [Confidence] 기준 필터링 후
                        /// [ConvertBoxForDisplay()]를 사용하여
                        /// 현재 [EO] 영상 해상도 및 [Zoom] 상태가 반영된
                        /// 화면 표시용 좌표로 변환한다.
                        /// </summary>
                        List<AiDetectionBox> rtspIndex0DisplayBoxes =
                            result.Boxes
                                .Where(box => box.NormalizedConfidence >= AiDisplayConfidenceThreshold)
                                .Select((box, index) =>
                                {
                                    // 2026-08-26: 화면에 남은 객체 기준으로 1부터 순번을 부여한다.
                                    box.DisplayOrder = index + 1;
                                    box.DetectionEventId = detectionEventId;
                                    return ConvertBoxForDisplay(
                                        box,
                                        EoVideoWidth,
                                        EoVideoHeight);
                                })
                                .ToList();

                        UpdateDetectionBoxes(
                            EoDetectionBoxes,
                            rtspIndex0DisplayBoxes,
                            0,
                            receiveTime);
                        break;

                    case 1:
                        if (!_isIrFrameDisplayed)
                        {
                            return;
                        }

                        /// <summary>
                        /// [RTSP Index 1]
                        ///
                        /// 현재 [AI Detector Agent] 설정 기준:
                        /// [RTSP Index 1] => [ONNX Index 2] [best_yolov7.onnx]
                        ///
                        /// [YOLOv7] 탐지 결과로 사용되며,
                        /// [IR] 화면에 [Bounding Box]를 표시한다.
                        /// </summary>

                        /// <summary>
                        /// [AI Detector] 표시 대상 [Bounding Box] 생성
                        ///
                        /// [Confidence] 기준 필터링 후
                        /// [ConvertBoxForDisplay()]를 사용하여
                        /// 현재 [IR] 영상 해상도 및 [Zoom] 상태가 반영된
                        /// 화면 표시용 좌표로 변환한다.
                        /// </summary>
                        List<AiDetectionBox> rtspIndex1DisplayBoxes =
                            result.Boxes
                                .Where(box => box.NormalizedConfidence >= AiDisplayConfidenceThreshold)
                                .Select((box, index) =>
                                {
                                    // 2026-08-26: EO와 동일한 AI BBox 순번 정책을 IR에도 적용한다.
                                    box.DisplayOrder = index + 1;
                                    box.DetectionEventId = detectionEventId;
                                    return ConvertBoxForDisplay(
                                        box,
                                        IrVideoWidth,
                                        IrVideoHeight);
                                })
                                .ToList();

                        UpdateDetectionBoxes(
                            IrDetectionBoxes,
                            rtspIndex1DisplayBoxes,
                            1,
                            receiveTime);
                        break;

                    default:
                        Console.WriteLine(
                            $"[AI DETECT] Unknown RTSP Index : {result.RtspIndex}");
                        break;
                }

            }, containsDisplayDetection);

            /// <summary>
            /// 탐지 객체 존재 여부
            ///
            /// 객체가 없는 경우에는
            /// Console 출력만 생략한다.
            /// </summary>
            bool hasDetection =
                result.DetectionCount > 0 ||
                result.Boxes.Count > 0;

            bool canPrintAiLog = hasDetection && (forcePrintLog || CanPrintAiDetectorLog());

            /// <summary>
            /// [AI Detector] 탐지 [Packet]은 매우 빠르게 들어오므로,
            /// 일정 시간 이내라면 [Console] 출력만 생략한다.
            ///
            /// 실제 수신 / 파싱 / 화면 반영은 계속 수행된다.
            /// </summary>
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

                    Console.WriteLine(
                        $"[AI BOX #{i + 1}] [ID] {box.ObjectId}, " +
                        $"[Class] {box.ClassIndex}, " +
                        $"[Confidence] {box.NormalizedConfidence * 100:F1}%, " +
                        $"[Box] {box.Left}, {box.Top}, {box.Right}, {box.Bottom}");
                }
                ConsoleLogHelper.PrintLine();
            }

        }

        /// <summary>
        /// 2026-08-26: RTSP 채널별 최신 탐지 UI 작업을 비동기로 예약한다.
        /// UI가 지연되는 동안 같은 채널의 중간 Frame은 병합하여 Dispatcher 적체를 방지한다.
        /// </summary>
        private void QueueAiDetectionUiUpdate(
            int rtspIndex,
            Action updateAction,
            bool containsDetection)
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
                    bool pendingHasDetection =
                        _pendingAiUiUpdateHasDetection.TryGetValue(
                            rtspIndex,
                            out bool pendingValue) && pendingValue;
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

            if (dispatcher == null ||
                dispatcher.HasShutdownStarted ||
                dispatcher.HasShutdownFinished)
            {
                ResetPendingAiUiUpdates();
                return;
            }

            try
            {
                dispatcher.BeginInvoke(
                    DispatcherPriority.DataBind,
                    new Action(DrainPendingAiUiUpdates));
            }
            catch (Exception ex)
            {
                ResetPendingAiUiUpdates();

                if (!_isApplicationShutdownRequested)
                {
                    ConsoleLogHelper.Error(
                        "AI UI / DISPATCH",
                        "Detection UI update scheduling failed",
                        ex);
                }

            }

        }

        /// <summary>
        /// 2026-08-26: UI가 다시 응답하면 채널별 최신 BBox와 이벤트 상태를 한 번에 반영한다.
        /// </summary>
        private void DrainPendingAiUiUpdates()
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

                pendingActions = _pendingAiUiUpdates
                    .OrderBy(item => item.Key)
                    .Select(item => item.Value)
                    .ToList();
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
                    ConsoleLogHelper.Error(
                        "AI UI / DISPATCH",
                        "Detection UI update failed",
                        ex);
                }

            }

            // 2026-08-26: 최신 프레임 병합은 유지하되 큰 적체만 기록하여 UI/디스크 부하를 줄인다.
            if (coalescedCount >= 25)
            {
                ConsoleLogHelper.State(
                    "AI UI / DISPATCH",
                    "Latest detection state restored after UI delay / CHANNELS=" +
                    pendingActions.Count +
                    " / COALESCED=" +
                    coalescedCount);
            }

        }

        private void ResetPendingAiUiUpdates()
        {
            lock (_aiUiUpdateSync)
            {
                _pendingAiUiUpdates.Clear();
                _pendingAiUiUpdateHasDetection.Clear();
                _isAiUiDrainScheduled = false;
                _coalescedAiUiUpdateCount = 0;
            }

        }

        /// <summary>
        /// 2026-08-26: Application/Dispatcher 종료 상태를 확인한 뒤 UI 작업을 수행한다.
        /// 종료 이후 지연 Callback이 Application.Current를 참조하여 발생하던 예외를 방지한다.
        /// </summary>
        private bool TryRunAiUiAction(
            Action action,
            string operationName)
        {
            if (_isApplicationShutdownRequested)
            {
                return false;
            }

            Application application = Application.Current;
            Dispatcher dispatcher = application?.Dispatcher;

            if (dispatcher == null ||
                dispatcher.HasShutdownStarted ||
                dispatcher.HasShutdownFinished)
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
                    ConsoleLogHelper.Error(
                        "AI UI / DISPATCH",
                        operationName + " failed",
                        ex);
                }

                return false;
            }

        }

        /// <summary>
        /// 2026-08-26: 메인 창 종료 전에 AI 수신, 이벤트 Timer와 장비 연결을 정리한다.
        /// 종료 뒤 Dispatcher Callback 및 Application.Current null 참조를 차단한다.
        /// </summary>
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

            ConsoleLogHelper.State(
                "APPLICATION / SHUTDOWN",
                "ViewModel resource cleanup started");

            try
            {
                _testProgramEventTimer?.Stop();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "APPLICATION / SHUTDOWN",
                    "Event timer stop failed",
                    ex);
            }

            try
            {
                _aiDetectorClientService.PacketReceived -=
                    OnAiDetectorPacketReceived;
                _aiDetectorClientService.StopAutoReconnect();
                _aiDetectorClientService.Disconnect();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "APPLICATION / SHUTDOWN",
                    "AI detector cleanup failed",
                    ex);
            }

            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "APPLICATION / SHUTDOWN",
                    "Device/video cleanup failed",
                    ex);

                _cts?.Cancel();
                _controlAgentReconnectCts?.Cancel();
                _videoReconnectCts?.Cancel();
            }

            ConsoleLogHelper.State(
                "APPLICATION / SHUTDOWN",
                "ViewModel resource cleanup completed");
        }

        #endregion

        #region [AI Detector Response Handling]

        /// <summary>
        /// [CMD 51] [AI Detector Info] 응답 처리
        ///
        /// 현재는 응답 [Payload] 구조 확인 단계이므로
        /// [Raw Payload]를 [Console]에 출력한다.
        /// </summary>
        private void HandleAiDetectorInfoResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 51] Detector Info");
            Console.WriteLine("[AI PAYLOAD] " + payload);

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CMD 52] [RTSP] 주소 조회 응답 처리
        ///
        /// 현재는 응답 [Payload] 구조 확인 단계이므로
        /// [Raw Payload]를 [Console]에 출력한다.
        /// </summary>
        private void HandleAiDetectorRtspResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 52] RTSP List");
            Console.WriteLine();

            List<AiRtspInfo> rtspList =
                _aiDetectorPacketParser.ParseRtspListPayload(payload);

            foreach (AiRtspInfo rtsp in rtspList)
            {
                Console.WriteLine(
                    $"[RTSP] [Index] {rtsp.Index}, [URL] {rtsp.Url}");
            }

            // [AI Detector Agent][RTSP] 조회 결과를 [UI Collection]에 반영
            UpdateAiRtspList(rtspList);

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CMD 53] [ONNX] 목록 조회 응답 처리
        ///
        /// 현재는 응답 [Payload] 구조 확인 단계이므로
        /// [Raw Payload]를 [Console]에 출력한다.
        /// </summary>
        private void HandleAiDetectorOnnxResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[AI DETECTOR RESPONSE] [CMD 53] ONNX List");

            List<AiOnnxInfo> onnxList =
                _aiDetectorPacketParser.ParseOnnxListPayload(payload);

            foreach (AiOnnxInfo onnx in onnxList)
            {
                Console.WriteLine(
                    $"[ONNX] [Index] {onnx.Index}, " +
                    $"[File] {onnx.FileName}, " +
                    $"[Classes] {string.Join(", ", onnx.Classes)}");
            }

            // [AI Detector Agent] [ONNX] 조회 결과를 [UI Collection]에 반영
            UpdateAiOnnxList(onnxList);

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CMD 54] / [CMD 56] [RTSP] / [ONNX] Mapping 응답 처리
        ///
        /// 현재는 응답 [Payload] 구조 확인 단계이므로
        /// [Raw Payload]를 [Console]에 출력한다.
        /// </summary>
        private void HandleAiDetectorMappingResponse(string payload)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[AI DETECTOR RESPONSE] Mapping Info");
            Console.WriteLine();

            List<AiMappingInfo> mappingList =
                _aiDetectorPacketParser.ParseMappingPayload(payload);

            foreach (AiMappingInfo mapping in mappingList)
            {
                Console.WriteLine(
                    $"[MAPPING] [RTSP] {mapping.RtspIndex}, " +
                    $"[ONNX] {mapping.OnnxIndex}, " +
                    $"[Confidence] {mapping.Confidence:F2}, " +
                    $"[IOU] {mapping.Iou:F2}");
            }

            // [AI Detector Agent] [RTSP] / [ONNX] Mapping 조회 결과를 [UI Collection]에 반영
            UpdateAiMappingList(mappingList);

            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [AI Detector Testing Helpers]

        /// <summary>
        /// [AI Detector] 다중 객체 [Bounding Box] 표시 테스트
        ///
        /// 실제 [AI Detector Agent] 수신 없이
        /// 여러 개의 탐지 객체가 들어온 상황을 가정하여
        /// [Bounding Box] 표시 상태를 확인한다.
        ///
        /// 테스트 목적:
        /// 1. [DetectionCount] 기준 다중 객체 표시 확인
        /// 2. 객체별 [ObjectId] / [ClassIndex] / [Confidence] 표시 확인
        /// 3. [Canvas Overlay]에서 여러 [Bounding Box]가 겹치지 않고 표시되는지 확인
        /// 4. [RtspIndex] 기준 [EO] / [IR] 분기 동작 확인
        /// </summary>
        private void TestDummyAiDetectionResult()
        {
            AiDetectionResult result =
                new AiDetectionResult
                {
                    FrameTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    InferenceMs = 30,
                    RtspIndex = 0,
                    DetectionCount = 3
                };

            result.Boxes.Add(
                new AiDetectionBox
                {
                    ObjectId = 101,
                    ClassIndex = 0,
                    Confidence = 0.55,
                    Left = 1074,
                    Top = 519,
                    Right = 1233,
                    Bottom = 645
                });

            result.Boxes.Add(
                new AiDetectionBox
                {
                    ObjectId = 102,
                    ClassIndex = 0,
                    Confidence = 0.48,
                    Left = 600,
                    Top = 300,
                    Right = 800,
                    Bottom = 500
                });

            result.Boxes.Add(
                new AiDetectionBox
                {
                    ObjectId = 103,
                    ClassIndex = 0,
                    Confidence = 0.72,
                    Left = 300,
                    Top = 200,
                    Right = 450,
                    Bottom = 360
                });

            HandleAiDetectionResult(
                result,
                DateTime.Now,
                true);
        }

        #endregion

        #region [AI Detector Request]

        /// <summary>
        /// [AI Detector Info] 조회 요청
        ///
        /// 요청 [CMD 01]
        /// 응답 [CMD 51]
        /// </summary>
        private async Task<bool> RequestAiDetectorInfoAsync()
        {
            byte[] packet =
                _aiPacketBuilder.BuildAiDetectorInfoRequest();

            return await _aiDetectorClientService.SendAsync(packet);
        }

        /// <summary>
        /// [AI Detector Agent] [RTSP] 주소 설정 요청
        ///
        /// UI에서 입력한 [RTSP 0] / [RTSP 1] 주소를
        /// [AI Detector Agent]에 전달한다.
        /// </summary>
        private async Task<bool> RequestAiDetectorRtspAddressSetAsync()
        {
            /// <summary>
            /// [Viewer] 영상 연결 주소 갱신
            ///
            /// 이후 장비 연결 해제 후 다시 연결하면
            /// 변경된 RTSP 주소로 [EO] / [IR] 영상 연결을 시도한다.
            /// </summary>
            EoSourceAddress = AiRtsp0Address;
            IrSourceAddress = AiRtsp1Address;

            OnPropertyChanged(nameof(EoSourceAddress));
            OnPropertyChanged(nameof(IrSourceAddress));

            byte[] packet =
                _aiPacketBuilder
                    .BuildRtspAddressSetRequest(
                        AiRtsp0Address,
                        AiRtsp1Address);

            return await _aiDetectorClientService.SendAsync(packet);
        }

        /// <summary>
        /// [RTSP] 주소 조회 요청
        ///
        /// 요청 [CMD 03]
        /// 응답 [CMD 52]
        /// </summary>
        private async Task<bool> RequestAiDetectorRtspAddressAsync()
        {
            byte[] packet =
                _aiPacketBuilder
                    .BuildRtspAddressRequest();

            return await _aiDetectorClientService.SendAsync(packet);
        }

        /// <summary>
        /// [ONNX/HEF] 파일 목록 조회 요청
        ///
        /// 2026-08-25: 웹 설정 API(5101/api/config)를 우선 사용한다.
        /// HTTP 조회가 실패하거나 모델이 없을 때만 기존 TCP CMD 04/53으로 복귀한다.
        /// </summary>
        private async Task<bool> RequestAiDetectorOnnxListAsync()
        {
            if (await TryLoadAiDetectorModelListFromHttpAsync())
            {
                return true;
            }

            byte[] packet =
                _aiPacketBuilder
                    .BuildOnnxListRequest();

            ConsoleLogHelper.Warning(
                "AI MODEL / HTTP",
                "HTTP model list unavailable; TCP CMD 04 fallback requested");

            return await _aiDetectorClientService.SendAsync(packet);
        }

        /// <summary>
        /// 2026-08-25: AI 웹 뷰어 설정 API에서 ONNX/HEF 파일명을 읽어
        /// 기존 AiOnnxList에 반영한다. 모델 파일 자체는 다운로드하지 않는다.
        /// </summary>
        private async Task<bool> TryLoadAiDetectorModelListFromHttpAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_aiControlAgentIp))
                {
                    return false;
                }

                Uri configUri =
                    new UriBuilder(
                        Uri.UriSchemeHttp,
                        _aiControlAgentIp.Trim(),
                        5101,
                        "api/config")
                    .Uri;

                using (HttpClientHandler handler = new HttpClientHandler { UseProxy = false })
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(3);

                    string json =
                        await client.GetStringAsync(configUri);

                    List<AiOnnxInfo> models =
                        ParseAiModelsFromHttpConfig(json);

                    if (models.Count == 0)
                    {
                        ConsoleLogHelper.Warning(
                            "AI MODEL / HTTP",
                            "Config response contained no ONNX/HEF model names");
                        return false;
                    }

                    UpdateAiOnnxList(models);

                    ConsoleLogHelper.State(
                        "AI MODEL / HTTP",
                        "Model and class list loaded / HOST=" + _aiControlAgentIp +
                        " / PORT=5101 / COUNT=" + models.Count +
                        " / CLASS_COUNT=" + models.Sum(model => model.Classes.Count));

                    return true;
                }

            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error(
                    "AI MODEL / HTTP",
                    "Model list request failed; TCP fallback will be used",
                    exception);
                return false;
            }

        }

        /// <summary>
        /// 2026-08-31: AI 설정 API의 models 배열에서 파일명과 클래스 배열을 함께 읽는다.
        /// 배열 순서를 ONNX Index로 유지하여 CMD 54/56 Mapping과 정확히 대응시킨다.
        /// </summary>
        private static List<AiOnnxInfo> ParseAiModelsFromHttpConfig(string json)
        {
            List<AiOnnxInfo> models = new List<AiOnnxInfo>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return models;
            }

            MatchCollection objectMatches =
                Regex.Matches(
                    json,
                    @"\{(?<body>[^{}]*)\}",
                    RegexOptions.Singleline);

            foreach (Match objectMatch in objectMatches)
            {
                string body = objectMatch.Groups["body"].Value;
                Match fileMatch =
                    Regex.Match(
                        body,
                        @"""filename""\s*:\s*""(?<name>(?:\\.|[^""\\])*?\.(?:onnx|hef))""",
                        RegexOptions.IgnoreCase);

                if (!fileMatch.Success)
                {
                    continue;
                }

                string rawName =
                    Regex.Unescape(fileMatch.Groups["name"].Value)
                         .Replace("\\/", "/");
                int separatorIndex =
                    Math.Max(rawName.LastIndexOf('/'), rawName.LastIndexOf('\\'));
                string fileName =
                    separatorIndex >= 0
                        ? rawName.Substring(separatorIndex + 1)
                        : rawName;

                AiOnnxInfo model =
                    new AiOnnxInfo
                    {
                        Index = models.Count,
                        FileName = fileName
                    };

                Match classesMatch =
                    Regex.Match(
                        body,
                        @"""classes""\s*:\s*\[(?<items>.*?)\]",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (classesMatch.Success)
                {
                    MatchCollection classMatches =
                        Regex.Matches(
                            classesMatch.Groups["items"].Value,
                            @"""(?<class>(?:\\.|[^""\\])*)""");

                    foreach (Match classMatch in classMatches)
                    {
                        string className =
                            Regex.Unescape(classMatch.Groups["class"].Value).Trim();

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

        /// <summary>
        /// [AI Detector Agent] [RTSP] / [ONNX] Mapping 설정 요청
        ///
        /// UI에서 입력한 [RTSP 0] / [RTSP 1]별 [ONNX Index],
        /// [Confidence], [IOU] 값을 기준으로 [CMD 05] Packet을 송신한다.
        /// </summary>
        private async Task<bool> RequestAiDetectorMappingSetAsync()
        {
            byte[] packet =
                _aiPacketBuilder
                    .BuildRtspOnnxMappingSetRequest(
                        AiRtsp0OnnxIndex,
                        AiRtsp1OnnxIndex,
                        AiMappingConfidence,
                        AiMappingIou);

            return await _aiDetectorClientService.SendAsync(packet);
        }

        /// <summary>
        /// [RTSP] / [ONNX] Mapping 조회 요청
        ///
        /// 요청 [CMD 06]
        /// 응답 [CMD 54]
        /// </summary>
        private async Task<bool> RequestAiDetectorMappingAsync()
        {
            byte[] packet =
                _aiPacketBuilder
                    .BuildRtspOnnxMappingRequest();

            return await _aiDetectorClientService.SendAsync(packet);
        }

        #endregion

        #region [AI Detector Display Helpers]

        /// <summary>
        /// 2026-08-31: CMD 55의 RTSP Index와 Class Index를 현재 Mapping 및
        /// 모델별 클래스 목록에 연결한다. 조회 실패나 범위 초과 시 기존 Class n 표시를 유지한다.
        /// </summary>
        private void ResolveAiDetectionClassNames(AiDetectionResult result)
        {
            if (result == null || result.Boxes == null)
            {
                return;
            }

            int selectedOnnxIndex =
                result.RtspIndex == 0
                    ? AiRtsp0OnnxIndex
                    : result.RtspIndex == 1
                        ? AiRtsp1OnnxIndex
                        : -1;
            AiMappingInfo mapping =
                AiMappingList.LastOrDefault(item => item.RtspIndex == result.RtspIndex);
            int mappedOnnxIndex = mapping?.OnnxIndex ?? selectedOnnxIndex;
            AiOnnxInfo model =
                AiOnnxList.FirstOrDefault(item => item.Index == mappedOnnxIndex);

            foreach (AiDetectionBox box in result.Boxes)
            {
                box.ModelFileName = model?.FileName;
                box.ResolvedClassName =
                    model != null &&
                    box.ClassIndex >= 0 &&
                    box.ClassIndex < model.Classes.Count
                        ? model.Classes[box.ClassIndex]
                        : null;
            }
        }

        /// <summary>
        /// [AI Detector] 탐지 결과 [Bounding Box] 목록 갱신
        ///
        /// 기존 [Bounding Box] 목록을 초기화한 뒤,
        /// 새로 수신한 탐지 결과를 화면 표시용 [Collection]에 반영한다.
        /// </summary>
        private void UpdateDetectionBoxes(
            ObservableCollection<AiDetectionBox> targetBoxes,
            List<AiDetectionBox> sourceBoxes,
            int rtspIndex,
            DateTime receiveTime)
        {
            bool hasDetection = sourceBoxes != null && sourceBoxes.Count > 0;
            DateTime lastDetectionTime = rtspIndex == 0
                ? _lastEoAiDisplayDetectionTime
                : _lastIrAiDisplayDetectionTime;
            bool holdActive = !hasDetection &&
                (receiveTime - lastDetectionTime).TotalMilliseconds >= 0 &&
                (receiveTime - lastDetectionTime).TotalMilliseconds <
                    AiDisplayHoldMilliseconds;

            if (holdActive)
            {
                SetAiDisplayHoldState(rtspIndex, true);
                return;
            }

            SetAiDisplayHoldState(rtspIndex, false);
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

            targetBoxes.Clear();

            foreach (AiDetectionBox box in sourceBoxes ?? new List<AiDetectionBox>())
            {
                targetBoxes.Add(box);
            }

        }

        private void SetAiDisplayHoldState(int rtspIndex, bool active)
        {
            bool previous = rtspIndex == 0
                ? _isEoAiDisplayHoldActive
                : _isIrAiDisplayHoldActive;
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

            ConsoleLogHelper.State(
                "AI BBOX HOLD",
                (active ? "Short detection gap retained" : "Hold ended") +
                " / CHANNEL=" + (rtspIndex == 0 ? "EO" : "IR") +
                " / HOLD_MS=" + AiDisplayHoldMilliseconds);
        }

        /// <summary>
        /// 2026-08-25: AI 연결 전환 또는 해제 시 화면 BBox와 ACTIVE 이벤트 상태를
        /// UI Dispatcher에서 함께 정리하여 연결이 끊긴 뒤 이전 탐지 결과가 남지 않게 한다.
        /// </summary>
        private void ClearAiDetectionState(string reason)
        {
            int eoBoxCount = 0;
            int irBoxCount = 0;
            int activeEventCount = 0;
            ClearAiSmokeCandidateSnapshots();

            Action clearAction = () =>
            {
                eoBoxCount = EoDetectionBoxes.Count;
                irBoxCount = IrDetectionBoxes.Count;
                activeEventCount = _activeAiEvents.Count;

                EoDetectionBoxes.Clear();
                IrDetectionBoxes.Clear();
                _lastEoAiDisplayDetectionTime = DateTime.MinValue;
                _lastIrAiDisplayDetectionTime = DateTime.MinValue;
                _isEoAiDisplayHoldActive = false;
                _isIrAiDisplayHoldActive = false;

                DateTime clearedTime = DateTime.Now;
                foreach (FireEventRecord activeEvent in _activeAiEvents.Values.ToList())
                {
                    activeEvent.MarkCleared(clearedTime);

                    try
                    {
                        AppendFireEventAudit(activeEvent, "CLEARED");
                    }
                    catch (Exception auditException)
                    {
                        ConsoleLogHelper.Error(
                            "AI EVENT",
                            "Disconnect audit write failed / EVENT_ID=" +
                            activeEvent.EventId,
                            auditException);
                    }

                }

                _activeAiEvents.Clear();
                ActiveAiCount = 0;
                NotifyAiEventSummaryChanged();
            };

            try
            {
                if (!TryRunAiUiAction(
                        clearAction,
                        "Detection state clear"))
                {
                    return;
                }

                ConsoleLogHelper.State(
                    "AI DETECTOR",
                    "Detection state cleared / REASON=" + reason +
                    " / EO_BOXES=" + eoBoxCount +
                    " / IR_BOXES=" + irBoxCount +
                    " / ACTIVE_EVENTS=" + activeEventCount);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "AI DETECTOR",
                    "Detection state clear failed / REASON=" + reason,
                    ex);
            }

        }

        #endregion

        #region [AI Detector UI Update Helpers]

        /// <summary>
        /// [AI Detector Agent] [RTSP] 조회 결과를 [UI Collection]에 반영한다.
        /// </summary>
        private void UpdateAiRtspList(
            List<AiRtspInfo> rtspList)
        {
            TryRunAiUiAction(() =>
            {
                AiRtspList.Clear();

                foreach (AiRtspInfo rtspInfo in rtspList)
                {
                    AiRtspList.Add(rtspInfo);
                }

            }, "RTSP list update");

        }

        /// <summary>
        /// [AI Detector Agent] [ONNX] 조회 결과를 [UI Collection]에 반영한다.
        /// </summary>
        private void UpdateAiOnnxList(
            List<AiOnnxInfo> onnxList)
        {
            TryRunAiUiAction(() =>
            {
                AiOnnxList.Clear();

                foreach (AiOnnxInfo onnxInfo in onnxList)
                {
                    AiOnnxList.Add(onnxInfo);
                }

                /*
                 * 2026-08-31: 최초 ONNX 목록 수신 시 EO와 IR 모두
                 * Real-Time Smoke/Fire 모델을 기본 Mapping으로 선택한다.
                 * 이후 사용자가 UI에서 변경한 값은 목록 새로 고침으로 덮어쓰지 않는다.
                 */
                if (!_hasAppliedDefaultAiModelMapping)
                {
                    AiOnnxInfo smokeDefault =
                        AiOnnxList.FirstOrDefault(model =>
                            string.Equals(
                                model.FileName,
                                "Real-Time-Smoke-Fire-Detection-YOLO11n-main.onnx",
                                StringComparison.OrdinalIgnoreCase));

                    if (smokeDefault != null)
                    {
                        AiRtsp0OnnxIndex = smokeDefault.Index;
                        AiRtsp1OnnxIndex = smokeDefault.Index;
                    }

                    _hasAppliedDefaultAiModelMapping = true;
                    ConsoleLogHelper.State(
                        "AI DETECTOR",
                        "Default model mapping selected / EO=" +
                        (smokeDefault?.FileName ?? "CURRENT") +
                        " / IR=" + (smokeDefault?.FileName ?? "CURRENT"));
                }

                /// <summary>
                /// [ONNX] 목록 조회 후 선택값 보정
                ///
                /// 현재 선택된 [ONNX Index]가 목록에 없으면
                /// 데모 기본 Mapping 기준으로 다시 설정한다.
                /// </summary>
                if (!AiOnnxList.Any(onnx => onnx.Index == AiRtsp0OnnxIndex))
                {
                    AiRtsp0OnnxIndex = AiOnnxList.Any(onnx => onnx.Index == 1)
                        ? 1
                        : AiOnnxList.FirstOrDefault()?.Index ?? 0;
                }

                if (!AiOnnxList.Any(onnx => onnx.Index == AiRtsp1OnnxIndex))
                {
                    AiRtsp1OnnxIndex = AiOnnxList.Any(onnx => onnx.Index == 2)
                        ? 2
                        : AiOnnxList.FirstOrDefault()?.Index ?? 0;
                }
                OnPropertyChanged(nameof(AiRtsp0OnnxIndex));
                OnPropertyChanged(nameof(AiRtsp1OnnxIndex));
            }, "Model list update");

        }

        /// <summary>
        /// [AI Detector Agent] [RTSP] / [ONNX] Mapping 조회 결과를 [UI Collection]에 반영한다.
        /// </summary>
        private void UpdateAiMappingList(
            List<AiMappingInfo> mappingList)
        {
            TryRunAiUiAction(() =>
            {
                AiMappingList.Clear();

                foreach (AiMappingInfo mappingInfo in mappingList)
                {
                    AiMappingList.Add(mappingInfo);
                }

            }, "Mapping list update");

        }

        #endregion

        #region [AI Detector Log Helpers]

        /// <summary>
        /// [AI Detector] 탐지 로그 출력 여부 확인
        ///
        /// 현재 시간과 마지막 출력 시간을 비교하여
        /// 일정 시간 이내면 => [Console] 출력 생략
        /// </summary>
        private bool CanPrintAiDetectorLog()
        {
            if ((DateTime.Now -
                 _lastAiDetectorLogTime)
                .TotalSeconds
                < AiDetectorLogIntervalSeconds)
            {
                return false;
            }
            _lastAiDetectorLogTime = DateTime.Now;

            return true;
        }

        #endregion

        #region [AI Bounding Box Display Helpers]

        /// <summary>
        /// [AI Detector] [Bounding Box] 표시 좌표 보정
        ///
        /// 현재 [AI Agent] 좌표와 [Viewer] 표시 좌표가
        /// [Zoom] 상태에 따라 어긋나는 경우를 보정하기 위한 함수이다.
        ///
        /// 기본 기준:
        /// - [Zoom] 기준값 : 5
        /// - 현재 [EO Zoom] 값이 커질수록 중앙 기준으로 [Box] 확대
        /// - 현재 [EO Zoom] 값이 작아질수록 중앙 기준으로 [Box] 축소
        /// </summary>
        private AiDetectionBox ConvertBoxForDisplay(
            AiDetectionBox sourceBox,
            int videoWidth,
            int videoHeight)
        {
            return sourceBox;
        }

        #endregion

        #endregion
    }

}
