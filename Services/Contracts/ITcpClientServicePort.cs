using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface ITcpClientServicePort
    {
        bool IsConnected { get; }

        /// <summary>
        /// [Web Agent / LA] TCP Server 연결 연결 제한 시간을 적용하여 서버가 준비되지 않은 경우에도 장비 연결 UI가 장시간
        /// 대기하지 않도록 한다.
        /// </summary>
        Task<bool> ConnectAsync(string ip, int port);

        /// <summary>
        /// 2026-09-17: Stream/Socket 종료 후 기존 RX Worker가 실제로 빠져나온 것을 확인한다. Timeout은 UI 무한
        /// 대기를 방지하는 보호 장치다.
        /// </summary>
        Task DisconnectAsync();
    }
}
