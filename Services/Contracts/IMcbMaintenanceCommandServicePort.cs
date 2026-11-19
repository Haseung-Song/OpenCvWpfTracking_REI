using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IMcbMaintenanceCommandServicePort
    {
        /// <summary>
        /// Pan 현재 Encoder 위치를 0으로 설정한다.
        /// </summary>
        Task<bool> SetPanZeroAsync(string ipAddress, int port);

        /// <summary>
        /// Tilt 현재 Encoder 위치를 0으로 설정한다.
        /// </summary>
        Task<bool> SetTiltZeroAsync(string ipAddress, int port);
    }
}
