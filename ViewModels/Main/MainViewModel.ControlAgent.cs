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

        /// <summary>
        /// RTSP 주소 형식 확인 절대 URI이며 Scheme이 rtsp 또는 rtsps인 경우만 유효한 영상 주소로 처리한다.
        /// </summary>
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

        /// <summary>
        /// 2026-09-16: Web Agent 원시 Pan은 우회전 시 감소하므로 GUI/파노라마/프리셋의 우회전 증가 좌표계로 변환한다.
        /// </summary>
        internal static double ConvertWebAgentPanToGuiAngle(double webAgentAngle)
        {
            return ControlAgentFeature.ConvertWebAgentPanToGuiAngle(webAgentAngle);
        }

        /// <summary>
        /// 2026-09-15: Web Agent Tilt 상태는 전송 구간에서 0 ~ 360도를 사용하지만, GUI와 파노라마/프리셋 계산에서는 위쪽
        /// 양수, 아래쪽 음수(-90 ~ 90)를 사용한다. 물리 가동범위 밖의 값은 마지막 정상값을 유지하여 잘못된 상태가 제어 계산에 섞이지 않게
        /// 한다.
        /// </summary>
        internal static double NormalizeUnsignedWebAgentTilt(double angle, double fallbackTilt)
        {
            return ControlAgentFeature.NormalizeUnsignedWebAgentTilt(angle, fallbackTilt);
        }

    }
}
