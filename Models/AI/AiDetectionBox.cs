namespace OpenCvWpfTracking.Models.AI
{
    /// <summary>
    /// [AI Detector] 객체 1개 [Bounding Box] 정보
    /// </summary>
    public class AiDetectionBox
    {
        /// <summary>
        /// [AI Detector] 객체 고유 ID
        /// </summary>
        public long ObjectId { get; set; }

        /// <summary>
        /// [AI Detector] 클래스 인덱스
        /// 
        /// 0 = [Drone]
        /// 1 = [ONNX]
        /// 2 = [ClassIndex]
        /// </summary>
        public int ClassIndex { get; set; }

        /// <summary>
        /// [AI Detector] 객체 탐지 신뢰도
        /// 
        /// 범위: [0.0 ~ 1.0]
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// [Bounding Box] 좌측 X 좌표
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// [Bounding Box] 상단 Y 좌표
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// [Bounding Box] 우측 X 좌표
        /// </summary>
        public int Right { get; set; }

        /// <summary>
        /// [Bounding Box] 하단 Y 좌표
        /// </summary>
        public int Bottom { get; set; }

        /// <summary>
        /// [Bounding Box] 너비
        /// </summary>
        public int Width => Right - Left;

        /// <summary>
        /// [Bounding Box] 높이
        /// </summary>
        public int Height => Bottom - Top;

        /// <summary>
        /// [AI Detector] [Class Index] 기준 표시 이름
        ///
        /// 현재 기준:
        /// [ClassIndex 0] => Drone
        ///
        /// [ClassIndex 1]은 [Drone + best.onnx] 통합 탐지 결과로
        /// 실제 객체 종류(배, 차량 등)는 추가 매핑 확인이 필요하다.
        /// </summary>
        public string ClassName
        {
            get
            {
                switch (ClassIndex)
                {
                    case 0:
                        // [Drone] 탐지 클래스
                        return "Drone";

                    default:
                        // [Index] 탐지 클래스
                        return $"Class {ClassIndex}";
                }

            }

        }

        /// <summary>
        /// [AI Detector] 화면 표시용 탐지 정보 문자열
        /// 
        /// [Confidence]는 [0.0 ~ 1.0] 범위로 수신되므로,
        /// 화면에는 [%] 단위로 변환하여 표시한다.
        /// </summary>
        public string DisplayText
        {
            get
            {
                return $"{ClassName} {Confidence * 100:F0}%";
            }

        }

    }

}
