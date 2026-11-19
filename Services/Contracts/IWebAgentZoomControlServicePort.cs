
namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IWebAgentZoomControlServicePort
    {

        /// <summary>
        /// 환경장비 EO Zoom Position 이동
        /// </summary>
        bool SetEoZoomPosition(short position);

        /// <summary>
        /// 환경장비 IR Zoom Position 이동
        /// </summary>
        bool SetIrZoomPosition(short position);
    }
}
