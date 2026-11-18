using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// Control Agent TCP 연결, LA Packet 수신/파싱과 장비 상태 반영을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        internal const int StatusUiCoalesceMilliseconds = 100;

        internal static bool IsValidRtspAddress(string address)
        {
            return ControlAgentFeature.IsValidRtspAddress(address);
        }

        /// <summary>
        /// [Control Agent] 비정상 연결 종료 처리
        ///
        /// 현재 통신 설정 탭에 입력된 IP / Port를 사용하여
        /// 자동 재연결을 시작한다.
        /// </summary>
        private void OnControlAgentConnectionClosed()
        {
            _controlAgentFeature.OnControlAgentConnectionClosed();
        }

        /// <summary>
        /// [CONTROL AGENT] [TCP] 수신 데이터 처리 함수
        ///
        /// [TcpClientService]에서 byte[] 원본 데이터를 받으면,
        /// [LaPacketParser]를 통해 12byte [Packet] 단위로 분리한다.
        /// </summary>
        private void OnLaMessageReceived(byte[] data, DateTime receiveTime)
        {
            _controlAgentFeature.OnLaMessageReceived(data, receiveTime);
        }

        internal static double ConvertWebAgentPanToGuiAngle(double webAgentAngle)
        {
            return ControlAgentFeature.ConvertWebAgentPanToGuiAngle(webAgentAngle);
        }

        internal static double NormalizeUnsignedWebAgentTilt(double angle, double fallbackTilt)
        {
            return ControlAgentFeature.NormalizeUnsignedWebAgentTilt(angle, fallbackTilt);
        }

    }
}
