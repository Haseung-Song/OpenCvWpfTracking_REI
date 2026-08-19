using System.Collections.Generic;

namespace OpenCvWpfTracking.Models.AI
{
    /// <summary>
    /// [AI Detector] 탐지 결과 [1 Frame] 정보
    /// 
    /// [AI Detector Agent]에서 수신한
    /// [CMD 55] 탐지데이터를 저장한다.
    /// </summary>
    public class AiDetectionResult
    {
        /// <summary>
        /// [RTSP] 프레임 수신 시간
        /// 
        /// [AI Detector Agent]에서 전달한
        /// 원본 프레임 기준 시간값
        /// </summary>
        public long FrameTime { get; set; }

        /// <summary>
        /// [AI] 추론 처리 시간 [ms]
        /// </summary>
        public int InferenceMs { get; set; }

        /// <summary>
        /// [RTSP] 채널 인덱스
        /// 
        /// [AI Detector Agent] 설정 기준
        /// [EO] / [IR] 구분값으로 사용한다.
        /// </summary>
        public int RtspIndex { get; set; }

        /// <summary>
        /// 현재 프레임 탐지 객체 수
        /// </summary>
        public int DetectionCount { get; set; }

        /// <summary>
        /// 탐지 객체 [Bounding Box] 목록
        /// </summary>
        public List<AiDetectionBox> Boxes { get; set; }
            = new List<AiDetectionBox>();
    }

}
