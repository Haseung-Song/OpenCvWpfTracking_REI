using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface ICtecCameraCommandServicePort
    {
        /// <summary>
        /// [EO] 주간 카메라 [Zoom Tele] 연속 제어 시작 [P = 2] : Tele [Q = 1 ~ 7] : Speed
        /// </summary>
        Task<bool> StartZoomTeleAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        /// <summary>
        /// [EO] 주간 카메라 [Zoom Wide] 연속 제어 시작
        /// </summary>
        Task<bool> StartZoomWideAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        /// <summary>
        /// [EO] 주간 카메라 [Zoom] 연속 제어 정지
        /// </summary>
        Task<bool> StopZoomAsync(string cameraIp, string userName, string password, bool useHttps);

        /// <summary>
        /// [EO] 주간 카메라 Optical Zoom 목표 위치 직접 이동 연속 TELE / WIDE 이동 후 Stop 시점을 추정하는 방식이
        /// 아니라, 카메라에 최종 Zoom Raw Position을 한 번에 지정한다. VISCA: 81 01 04 47 0P 0Q 0R 0S FF
        /// Position 범위: 0x0000 = Wide 0x4000 = Tele 전달되는 Position은 4개의 4bit Nibble로 분리한다.
        /// </summary>
        Task<bool> MoveZoomPositionAsync(string cameraIp, string userName, string password, bool useHttps, ushort position);

        /// <summary>
        /// [EO] 주간 카메라 Focus 목표 위치 직접 이동 Focus Mode를 Manual로 적용한 뒤 최종 Focus Raw Position을
        /// 한 번에 지정한다. VISCA: 81 01 04 48 0P 0Q 0R 0S FF Position 범위: 0x0000 = Far 0x8000
        /// = Near 전달되는 Position은 4개의 4bit Nibble로 분리한다.
        /// </summary>
        Task<bool> MoveFocusPositionAsync(string cameraIp, string userName, string password, bool useHttps, ushort position);

        /// <summary>
        /// [EO] 주간 카메라 [Focus Near] 연속 제어 시작 문서 기준 Focus 제어 전 Focus Mode Manual 명령을 먼저
        /// 송신한다.
        /// </summary>
        Task<bool> StartFocusNearAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        /// <summary>
        /// [EO] 주간 카메라 [Focus Far] 연속 제어 시작 문서 기준 Focus 제어 전 Focus Mode Manual 명령을 먼저
        /// 송신한다.
        /// </summary>
        Task<bool> StartFocusFarAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        /// <summary>
        /// [EO] 주간 카메라 [Focus] 연속 제어 정지
        /// </summary>
        Task<bool> StopFocusAsync(string cameraIp, string userName, string password, bool useHttps);

        /// <summary>
        /// [EO] 주간 카메라 [One Push Focus] 요청
        /// </summary>
        Task<bool> OnePushFocusAsync(string cameraIp, string userName, string password, bool useHttps);

        /// <summary>
        /// [EO] 주간 카메라 현재 [Optical Zoom Position] 조회 CGI로 Inquiry Packet을 송신한 뒤, 실제 응답은
        /// [CtecCameraResponseService]가 카메라 [TCP Port 9000]에서 수신한다. Response: 0x99 0x55
        /// 0x47 0x00 P1 P2 0xFF
        /// </summary>
        Task<bool> RequestZoomPositionAsync(string cameraIp, string userName, string password, bool useHttps);

        /// <summary>
        /// [EO] 주간 카메라 현재 [Focus Position] 조회 Response: 0x99 0x55 0x48 0x00 P1 P2 0xFF
        /// </summary>
        Task<bool> RequestFocusPositionAsync(string cameraIp, string userName, string password, bool useHttps);

        /// <summary>
        /// [EO] 주간 카메라 현재 [Focus Mode] 조회 Response: 0x99 0x55 0x38 0x00 0x00 P1 0xFF P1:
        /// 0x02 = Auto 0x03 = Manual
        /// </summary>
        Task<bool> RequestFocusModeAsync(string cameraIp, string userName, string password, bool useHttps);
    }
}
