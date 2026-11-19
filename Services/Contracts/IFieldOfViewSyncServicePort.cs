
namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IFieldOfViewSyncServicePort
    {
        /// <summary>
        /// 0~10 레벨의 실측 위치와 양 채널 예상 화각을 묶어 반환한다.
        /// </summary>
        ZoomFovTarget CreateTarget(int level);
    }
}
