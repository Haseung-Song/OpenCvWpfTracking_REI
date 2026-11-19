using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface ICtecCameraResponseServicePort
    {
        bool IsConnected { get; }

        /// <summary>
        /// 카메라 [TCP Response Port] 연결 및 수신 Loop 시작 동일 카메라에 이미 연결되어 있으면 중복 연결하지 않는다. 다른
        /// 카메라 또는 Port로 변경된 경우 기존 연결을 종료한 뒤 새로 시작한다.
        /// </summary>
        Task StartAsync(string cameraIp, int responsePort);

        /// <summary>
        /// 카메라 응답 TCP 연결 및 자동 재연결 종료
        /// </summary>
        void Stop();

        /// <summary>
        /// [EO Zoom] 다음 TCP 9000 Position 응답을 기다린다. 반드시 CGI Inquiry 송신 전에 호출해야 한다. 그래야
        /// 빠르게 도착한 응답도 놓치지 않는다.
        /// </summary>
        Task<int?> WaitForNextZoomPositionAsync(int timeoutMs, CancellationToken cancellationToken);

        /// <summary>
        /// [EO Focus] 다음 TCP 9000 Position 응답을 기다린다.
        /// </summary>
        Task<int?> WaitForNextFocusPositionAsync(int timeoutMs, CancellationToken cancellationToken);
    }
}
