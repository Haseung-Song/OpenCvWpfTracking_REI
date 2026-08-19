using System.Collections.Generic;

namespace OpenCvWpfTracking.Models.AI
{
    /// <summary>
    /// [AI Detector Agent] [ONNX] 모델 정보
    /// </summary>
    public class AiOnnxInfo
    {
        /// <summary>
        /// [ONNX] 인덱스
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// [ONNX] 파일명
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 탐지 클래스 목록
        /// </summary>
        public List<string> Classes { get; set; }
            = new List<string>();
    }

}
