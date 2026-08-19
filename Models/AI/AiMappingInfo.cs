namespace OpenCvWpfTracking.Models.AI
{
    /// <summary>
    /// [RTSP] / [ONNX] Mapping 정보
    /// </summary>
    public class AiMappingInfo
    {
        /// <summary>
        /// [RTSP] 인덱스
        /// </summary>
        public int RtspIndex { get; set; }

        /// <summary>
        /// [ONNX] 인덱스
        /// </summary>
        public int OnnxIndex { get; set; }

        /// <summary>
        /// 탐지 정확도 기준값
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// [IOU] 기준값
        /// </summary>
        public double Iou { get; set; }
    }

}
