using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface ICtecCameraCommandServicePort
    {
        Task<bool> StartZoomTeleAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        Task<bool> StartZoomWideAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        Task<bool> StopZoomAsync(string cameraIp, string userName, string password, bool useHttps);

        Task<bool> MoveZoomPositionAsync(string cameraIp, string userName, string password, bool useHttps, ushort position);

        Task<bool> MoveFocusPositionAsync(string cameraIp, string userName, string password, bool useHttps, ushort position);

        Task<bool> StartFocusNearAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        Task<bool> StartFocusFarAsync(string cameraIp, string userName, string password, bool useHttps, byte speed = 7);

        Task<bool> StopFocusAsync(string cameraIp, string userName, string password, bool useHttps);

        Task<bool> OnePushFocusAsync(string cameraIp, string userName, string password, bool useHttps);

        Task<bool> RequestZoomPositionAsync(string cameraIp, string userName, string password, bool useHttps);

        Task<bool> RequestFocusPositionAsync(string cameraIp, string userName, string password, bool useHttps);

        Task<bool> RequestFocusModeAsync(string cameraIp, string userName, string password, bool useHttps);
    }
}
