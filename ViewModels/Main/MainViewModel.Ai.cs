using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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

                if (!await RequestAiDetectorRtspAddressSetAsync())
                {
                    AiSettingStatusText = "[AI] RTSP Apply Failed";
                    return;
                }

                await Task.Delay(300);

                if (!await RequestAiDetectorInfoAsync() ||
                    !await RequestAiDetectorRtspAddressAsync() ||
                    !await RequestAiDetectorOnnxListAsync() ||
                    !await RequestAiDetectorMappingSetAsync() ||
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

                ConsoleLogHelper.Error(
                    "AI DETECTOR",
                    "Connect / setting exception / " + ex.Message);
            }

        }

        /// <summary>
        /// [AI DISCONNECT] 버튼 기준 수동 연결 해제
        ///
        /// 자동 재연결 요청을 함께 중단하여 사용자가 다시
        /// [AI CONNECT]를 누르기 전에는 AI Agent에 재접속하지 않는다.
        /// </summary>
        private void DisconnectAiAgent()
        {
            _aiDetectorClientService.StopAutoReconnect();
            _aiDetectorClientService.Disconnect();

            AiPowerStatusText = "OFF";
            AiSettingStatusText = "[AI] Disconnected";

            ConsoleLogHelper.Command(
                "AI DETECTOR",
                "Manual disconnect completed");
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
            App.Current.Dispatcher.Invoke(() =>
            {
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
                                .Where(box => box.Confidence >= AiDisplayConfidenceThreshold)
                                .Select(box =>
                                    ConvertBoxForDisplay(
                                        box,
                                        EoVideoWidth,
                                        EoVideoHeight))
                                .ToList();

                        UpdateDetectionBoxes(
                            EoDetectionBoxes,
                            rtspIndex0DisplayBoxes);
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
                                .Where(box => box.Confidence >= AiDisplayConfidenceThreshold)
                                .Select(box =>
                                    ConvertBoxForDisplay(
                                        box,
                                        IrVideoWidth,
                                        IrVideoHeight))
                                .ToList();

                        UpdateDetectionBoxes(
                            IrDetectionBoxes,
                            rtspIndex1DisplayBoxes);
                        break;

                    default:
                        Console.WriteLine(
                            $"[AI DETECT] Unknown RTSP Index : {result.RtspIndex}");
                        break;
                }

            });

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
                        $"[Confidence] {box.Confidence * 100:F0}%, " +
                        $"[Box] {box.Left}, {box.Top}, {box.Right}, {box.Bottom}");
                }
                ConsoleLogHelper.PrintLine();
            }

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
        /// [ONNX] 파일 목록 조회 요청
        ///
        /// 요청 [CMD 04]
        /// 응답 [CMD 53]
        /// </summary>
        private async Task<bool> RequestAiDetectorOnnxListAsync()
        {
            byte[] packet =
                _aiPacketBuilder
                    .BuildOnnxListRequest();

            return await _aiDetectorClientService.SendAsync(packet);
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
        /// [AI Detector] 탐지 결과 [Bounding Box] 목록 갱신
        ///
        /// 기존 [Bounding Box] 목록을 초기화한 뒤,
        /// 새로 수신한 탐지 결과를 화면 표시용 [Collection]에 반영한다.
        /// </summary>
        private void UpdateDetectionBoxes(
            ObservableCollection<AiDetectionBox> targetBoxes,
            List<AiDetectionBox> sourceBoxes)
        {
            targetBoxes.Clear();

            foreach (AiDetectionBox box in sourceBoxes)
            {
                targetBoxes.Add(box);
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
            App.Current.Dispatcher.Invoke(() =>
            {
                AiRtspList.Clear();

                foreach (AiRtspInfo rtspInfo in rtspList)
                {
                    AiRtspList.Add(rtspInfo);
                }

            });

        }

        /// <summary>
        /// [AI Detector Agent] [ONNX] 조회 결과를 [UI Collection]에 반영한다.
        /// </summary>
        private void UpdateAiOnnxList(
            List<AiOnnxInfo> onnxList)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                AiOnnxList.Clear();

                foreach (AiOnnxInfo onnxInfo in onnxList)
                {
                    AiOnnxList.Add(onnxInfo);
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
            });

        }

        /// <summary>
        /// [AI Detector Agent] [RTSP] / [ONNX] Mapping 조회 결과를 [UI Collection]에 반영한다.
        /// </summary>
        private void UpdateAiMappingList(
            List<AiMappingInfo> mappingList)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                AiMappingList.Clear();

                foreach (AiMappingInfo mappingInfo in mappingList)
                {
                    AiMappingList.Add(mappingInfo);
                }

            });

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
