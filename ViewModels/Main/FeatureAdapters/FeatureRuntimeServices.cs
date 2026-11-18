using System.Diagnostics;
using System.Threading;
using OpenCvWpfTracking.Services.Communication;
using OpenCvWpfTracking.Services.Communication.WebAgent;
using OpenCvWpfTracking.Services.Video;
using OpenCvWpfTracking.Services.Control;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 구성 단계에서 생성한 서비스와 동기화 자원을 연결한다.
    /// 각 기능에는 전체 자원 목록을 전달하지 않고 해당 기능의 계약을 통해 필요한 항목만 제공한다.
    /// </summary>
    internal sealed class FeatureRuntimeServices
    {
        internal Services.Communication.AI.IAiDetectorClientServicePort _aiDetectorClientService;
        internal Services.Communication.AI.IAiDetectorPacketParserPort _aiDetectorPacketParser;
        internal Services.Communication.AI.IAiDetectorPacketBuilderPort _aiPacketBuilder;
        internal IControlCommandServicePort _controlCommandService;
        internal ITcpClientServicePort _laTcpService;
        internal Services.Communication.ICtecCameraCommandServicePort _ctecCameraCommandService;
        internal Services.Communication.ICtecCameraResponseServicePort _ctecCameraResponseService;
        internal SemaphoreSlim _ctecEoPositionQueryLock;
        internal Stopwatch _focusLogStopwatch;
        internal Services.Communication.ILAPacketParserPort _laPacketParser;
        internal SemaphoreSlim _homeZeroOperationLock;
        internal Services.Communication.IMcbMaintenanceCommandServicePort _mcbMaintenanceCommandService;
        internal Services.Communication.WebAgent.IWebAgentZoomControlServicePort _webAgentZoomControlService;
        internal IFFmpegDecoderServicePort _eoDecoder;
        internal IFFmpegDecoderServicePort _irDecoder;
        internal IWebAgentThermalPaletteServicePort _webAgentThermalPaletteService;
        internal IFieldOfViewSyncServicePort _fieldOfViewSyncService;
        internal SemaphoreSlim _lensSyncOperationLock;
    }
}
