
namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IFieldOfViewSyncServicePort
    {
        ZoomFovTarget CreateTarget(int level);
    }
}
