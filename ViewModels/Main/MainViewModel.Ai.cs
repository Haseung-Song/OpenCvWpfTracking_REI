using System;

using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// AI 화면 명령과 수신 이벤트를 AiFeature에 연결한다. 실제 연결·응답·탐지 처리는 AiFeature가 담당한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {

        internal const int AiDisplayHoldMilliseconds = 750;

        // 수동 연결 요청의 세대 번호는 AiFeature와 동일한 저장 위치를 사용한다.
        internal ref int _aiManualConnectionGeneration
        {
            get { return ref _aiFeature._aiManualConnectionGeneration; }
        }

        /// <summary>
        /// [AI CONNECT] 버튼 기준 수동 연결 및 초기 설정 적용
        ///
        /// 프로그램 시작 또는 장비 연결 시에는 AI Agent에 자동 연결하지 않는다.
        /// 사용자가 [AI CONNECT]를 누른 경우에만 UI의 IP / Port로 1회 연결하고,
        /// 연결 성공 뒤 RTSP / 모델 / Mapping 정보를 순서대로 적용 및 조회한다.
        /// </summary>
        private Task ConnectAiAgentFromSettingAsync()
        {
            return _aiFeature.ConnectAiAgentFromSettingAsync();
        }

        /// <summary>
        /// [AI DISCONNECT] 버튼 기준 수동 연결 해제
        ///
        /// 자동 재연결 요청을 함께 중단하여 사용자가 다시
        /// [AI CONNECT]를 누르기 전에는 AI Agent에 재접속하지 않는다.
        /// </summary>
        private void DisconnectAiAgent()
        {
            _aiFeature.DisconnectAiAgent();
        }

        /// <summary>
        /// [AI Detector Agent] [TCP] 수신 [Packet] 처리 함수
        ///
        /// 공통 [Packet] 구조를 먼저 검증한 뒤,
        /// [CMD] 값에 따라 응답 처리 함수를 분기한다.
        /// </summary>
        private void OnAiDetectorPacketReceived(byte[] packet, DateTime receiveTime)
        {
            _aiFeature.OnAiDetectorPacketReceived(packet, receiveTime);
        }

        /// <summary>
        /// 2026-08-26: UI가 다시 응답하면 채널별 최신 BBox와 이벤트 상태를 한 번에 반영한다.
        /// </summary>
        private void DrainPendingAiUiUpdates()
        {
            _aiFeature.DrainPendingAiUiUpdates();
        }

        /// <summary>
        /// 2026-08-26: 메인 창 종료 전에 AI 수신, 이벤트 Timer와 장비 연결을 정리한다.
        /// 종료 뒤 Dispatcher Callback 및 Application.Current null 참조를 차단한다.
        /// </summary>
        internal void ShutdownForApplicationExit()
        {
            _aiFeature.ShutdownForApplicationExit();
        }

        /// <summary>
        /// [AI Detector Info] 조회 요청
        ///
        /// 요청 [CMD 01]
        /// 응답 [CMD 51]
        /// </summary>
        private Task<bool> RequestAiDetectorInfoAsync()
        {
            return _aiFeature.RequestAiDetectorInfoAsync();
        }

        /// <summary>
        /// [AI Detector Agent] [RTSP] 주소 설정 요청
        ///
        /// UI에서 입력한 [RTSP 0] / [RTSP 1] 주소를
        /// [AI Detector Agent]에 전달한다.
        /// </summary>
        private Task<bool> RequestAiDetectorRtspAddressSetAsync()
        {
            return _aiFeature.RequestAiDetectorRtspAddressSetAsync();
        }

        /// <summary>
        /// [RTSP] 주소 조회 요청
        ///
        /// 요청 [CMD 03]
        /// 응답 [CMD 52]
        /// </summary>
        private Task<bool> RequestAiDetectorRtspAddressAsync()
        {
            return _aiFeature.RequestAiDetectorRtspAddressAsync();
        }

        /// <summary>
        /// [ONNX/HEF] 파일 목록 조회 요청
        ///
        /// 2026-08-25: 웹 설정 API(5101/api/config)를 우선 사용한다.
        /// HTTP 조회가 실패하거나 모델이 없을 때만 기존 TCP CMD 04/53으로 복귀한다.
        /// </summary>
        private Task<bool> RequestAiDetectorOnnxListAsync()
        {
            return _aiFeature.RequestAiDetectorOnnxListAsync();
        }

        /// <summary>
        /// [AI Detector Agent] [RTSP] / [ONNX] Mapping 설정 요청
        ///
        /// UI에서 입력한 [RTSP 0] / [RTSP 1]별 [ONNX Index],
        /// [Confidence], [IOU] 값을 기준으로 [CMD 05] Packet을 송신한다.
        /// </summary>
        private Task<bool> RequestAiDetectorMappingSetAsync()
        {
            return _aiFeature.RequestAiDetectorMappingSetAsync();
        }

        /// <summary>
        /// [RTSP] / [ONNX] Mapping 조회 요청
        ///
        /// 요청 [CMD 06]
        /// 응답 [CMD 54]
        /// </summary>
        private Task<bool> RequestAiDetectorMappingAsync()
        {
            return _aiFeature.RequestAiDetectorMappingAsync();
        }

        /// <summary>
        /// v2.91 CMD07로 지정 RTSP Index의 원본 영상 크기를 조회한다.
        /// </summary>
        private Task<bool> RequestAiDetectorRtspVideoSizeAsync(int rtspIndex)
        {
            return _aiFeature.RequestAiDetectorRtspVideoSizeAsync(rtspIndex);
        }

    }
}
