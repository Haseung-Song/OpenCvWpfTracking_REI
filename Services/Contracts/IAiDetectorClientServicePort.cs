using System;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IAiDetectorClientServicePort
    {
        event Action<byte[], DateTime> PacketReceived;

        bool IsConnected { get; }

        /// <summary>
        /// [AI] [Detector Agent]에 [TCP] 연결 [기본] : [IP] : [192.168.20.165] [PORT] : [5055]
        /// </summary>
        Task<bool> ConnectAsync(string ip, int port);

        /// <summary>
        /// [AI Detector Agent] 자동 재연결 중지 요청 기존 [Auto Reconnect] 루프를 종료시키기 위해 사용한다.
        /// </summary>
        void StopAutoReconnect();

        /// <summary>
        /// [AI Detector Agent] 요청 [Packet] 송신 연결 상태 확인 후 [AI Detector Agent]로 [Packet]을
        /// 전송한다. [CMD 51] [CMD 52] [CMD 53] [CMD 54] 조회 요청에 사용한다.
        /// </summary>
        Task<bool> SendAsync(byte[] packet);

        /// <summary>
        /// [AI] [Detector] [TCP] 연결 종료 및 리소스 정리
        /// </summary>
        void Disconnect();
    }
}
