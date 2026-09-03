using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Communication;
using OpenCvWpfTracking.Services.Communication.AI;
using OpenCvWpfTracking.Services.Communication.WebAgent;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 메인 화면 전체 상태와 서비스 생명주기를 조정하는 중심 ViewModel.
    ///
    /// 세부 동작은 같은 폴더의 MainViewModel.*.cs partial 파일로 분리한다.
    /// 이 파일에는 공유 필드, Command 생성, 기본 초기화와 공통 알림만 둔다.
    /// </summary>
    public partial class MainViewModel : INotifyPropertyChanged
    {
        #region [Enum Type]

        /// <summary>
        /// 현재 진행 중인 [연속 제어] 종류
        /// </summary>
        private enum ContinuousMoveType
        {
            None,
            PanTilt,
            EoZoom,
            EoFocus,
            IrZoom,
            IrFocus,
            IrDigitalZoom
        }

        /// <summary>
        /// 현재 키보드 방향키 조합으로 수행 중인
        /// Pan / Tilt 이동 방향
        /// </summary>
        private enum KeyboardPanTiltDirection
        {
            None,

            PanLeft,
            PanRight,

            TiltUp,
            TiltDown,

            PanLeftTiltUp,
            PanRightTiltUp,

            PanLeftTiltDown,
            PanRightTiltDown
        }

        #endregion

        #region [Fields]

        #region [Video State Fields]

        /// <summary>
        /// [EO] 주간 카메라 [RTSP] 영상 처리 객체
        ///
        /// [OpenCvSharp] [VideoCapture] [RTSP] 연결 실패로 인해
        /// 실제 [RTSP] 출력은 [FFmpegRtspDecoderService]를 사용한다.
        /// </summary>
        private readonly FFmpegDecoderService _eoDecoder;

        /// <summary>
        /// [IR] 열상 카메라 [RTSP] 영상 처리 객체
        ///
        /// [OpenCvSharp] [VideoCapture] [RTSP] 연결 실패로 인해
        /// 실제 [RTSP] 출력은 [FFmpegRtspDecoderService]를 사용한다.
        /// </summary>
        private readonly FFmpegDecoderService _irDecoder;

        #endregion

        #region [RTSP Source Preset Fields]

        /*
         * TODO(CONFIG-NEXT):
         * 장비 주소와 인증정보가 최종 확정되면 App.config 또는 별도 설정 파일로 이동한다.
         * 현재 단계에서는 기존 운용값과 XAML 선택 흐름을 바꾸지 않기 위해 상수로 유지한다.
         * 설정 파일화할 때는 암호를 Console 로그에 출력하지 않도록 반드시 마스킹한다.
         */

        /// <summary>
        /// [3] 1층 생산팀 [ADS] 주간(EO) 카메라 RTSP 주소
        /// </summary>
        private const string AdsEoRtspAddress =
            "rtsp://service:Xhddlf1!@192.168.0.100:554/rtsp_tunnel";

        /// <summary>
        /// [3] 1층 생산팀 [ADS] 열상(IR) 카메라 RTSP 주소
        /// </summary>
        private const string AdsIrRtspAddress =
            "rtsp://admin:admin@192.168.0.101:554/hdmi";

        /// <summary>
        /// [4] 옥상 [GOP] 주간(EO) 카메라 RTSP 주소
        /// </summary>
        private const string GopEoRtspAddress =
            "rtsp://root:rmffhqjf1!@192.168.1.2:554/AVStream1_1";

        /// <summary>
        /// [4] 옥상 [GOP] 주간(EO) 카메라 CTEC CGI 직접 제어 정보
        ///
        /// RTSP 영상 수신 주소와 별도로
        /// Zoom / Focus 명령을 카메라 CGI로 직접 송신할 때 사용한다.
        /// </summary>
        private const string GopEoControlIp =
            "192.168.1.2";

        private const string GopEoControlUserName =
            "root";

        private const string GopEoControlPassword =
            "rmffhqjf1!";

        /// <summary>
        /// [옥상 GOP EO] 카메라 CGI 제어 HTTPS 사용 여부
        ///
        /// 실제 카메라 웹 설정의 [Connection Mode]가 [HTTPS]이므로
        /// HTTP 요청 시 Viewer Page Redirection HTML이 반환된다.
        /// 
        /// 따라서 CTEC CGI 명령은 HTTPS Port 443으로 직접 송신한다.
        /// </summary>
        private const bool GopEoControlUseHttps =
            true;

        /// <summary>
        /// 옥상 GOP EO 카메라 Zoom / Focus 연속 제어 속도
        ///
        /// XV-Z4850HC 문서 기준 유효 범위는 [1 ~ 7]이다.
        /// </summary>
        private const byte GopEoCtecControlSpeed =
            7;

        /// <summary>
        /// 옥상 GOP EO 카메라 CTEC 응답 수신 TCP Port
        ///
        /// 카메라 웹 설정:
        /// [Services] -> [Port] -> [Serial Port #1]
        /// -> [TCP Access Enable] -> [Port 9000]
        ///
        /// 카메라 웹 설정의 Port를 변경한 경우
        /// 이 값도 동일하게 변경해야 한다.
        /// </summary>
        private const int GopEoCtecResponsePort =
            9000;

        /// <summary>
        /// [4] 옥상 [GOP] 열상(IR) 카메라 RTSP 주소
        /// </summary>
        private const string GopIrRtspAddress =
            "rtsp://root:rmffhqjf1!@192.168.0.121:554/cam0_0";

        /// <summary>
        /// [5] 4층 개발팀 환경부 주간(EO) PTZ 카메라 RTSP 주소
        /// </summary>
        private const string MoeEoRtspAddress =
            "rtsp://root:rmffhqjf1!@192.168.0.100:554/AVStream1_1";

        /// <summary>
        /// [5] 4층 개발팀 환경부 열상(IR) PTZ 카메라 RTSP 주소
        /// </summary>
        private const string MoeIrRtspAddress =
            "rtsp://root:rmffhqjf1!@10.20.30.40:554/cam0_0";

        #endregion

        #region [LA Communication Fields]

        /// <summary>
        /// [Control Agent] 제어 [TCP] 통신 서비스 객체
        ///
        /// 기존 고흥 건의 LA 통신 구조를 유지하며,
        /// 운용 환경에 따라 Web Agent 또는 LA Agent 구현체와 연결한다.
        /// UI와 공통 코드 명칭은 Control Agent로 사용한다.
        /// </summary>
        private readonly TcpClientService _laTcpService;

        /// <summary>
        /// [TORUSS] 제어 명령 서비스
        ///
        /// [TORUSS] 제어 [Protocol] 기준 [7byte Packet] 생성 / 송신 담당
        /// </summary>
        private readonly ControlCommandService _controlCommandService;

        /// <summary>
        /// MCB Pan / Tilt Zero 및 Home fallback 직접 명령 서비스
        /// </summary>
        private readonly McbMaintenanceCommandService _mcbMaintenanceCommandService;

        /// <summary>
        /// 2026-08-26: 옥상 GOP MCB 유지보수 기본 Port.
        /// MCB는 4001, SCB는 4002를 사용하므로 서로 혼용하지 않는다.
        /// </summary>
        private const int McbMaintenancePort =
            4001;

        /// <summary>
        /// 환경장비 Web Agent 기준 EO / IR Zoom 동기화 Adapter
        /// </summary>
        private readonly WebAgentZoomControlService _webAgentZoomControlService;

        private readonly WebAgentThermalPaletteService _webAgentThermalPaletteService;

        /// <summary>
        /// [옥상 GOP EO] [XV-Z4850HC] CTEC CGI 직접 제어 서비스
        ///
        /// 선택된 EO 프리셋의 제어 방식이 CtecCgi인 경우에만 사용하며,
        /// 그 외 EO / Pan / Tilt / IR 제어는 기존 Control Agent 경로를 유지한다.
        /// </summary>
        private readonly CtecCameraCommandService _ctecCameraCommandService;

        /// <summary>
        /// [옥상 GOP EO] [XV-Z4850HC] CTEC 응답 수신 서비스
        ///
        /// 카메라 IP의 TCP Port 9000에 Client로 연결하여
        /// CGI Inquiry 명령에 대한 [0x99 0x55 ... 0xFF] 응답을 수신한다.
        /// </summary>
        private readonly CtecCameraResponseService _ctecCameraResponseService;

        /// <summary>
        /// [EO] 영상 첫 Frame 화면 표시 여부
        ///
        /// true : [EO] 영상 표시 중
        /// false: 검은 화면 또는 미연결 상태
        /// </summary>
        private bool _isEoFrameDisplayed;

        /// <summary>
        /// [IR] 영상 첫 [Frame] 화면 표시 여부
        ///
        /// true : [IR] 영상 표시 중
        /// false: 검은 화면 또는 미연결 상태
        /// </summary>
        private bool _isIrFrameDisplayed;

        /// <summary>
        /// [EO] Frame UI 반영 예약 상태
        ///
        /// 0 : Dispatcher 등록 없음
        /// 1 : 이전 EO Frame이 Dispatcher에서 처리 대기/처리 중
        ///
        /// EO는 1920 x 1080 고해상도이므로
        /// UI Queue에 Frame이 누적되지 않도록 별도로 관리한다.
        /// </summary>
        private int _isEoFrameDispatchPending;

        /// <summary>
        /// [IR] Frame UI 반영 예약 상태
        ///
        /// 0 : Dispatcher 등록 없음
        /// 1 : 이전 IR Frame이 Dispatcher에서 처리 대기/처리 중
        /// </summary>
        private int _isIrFrameDispatchPending;

        #endregion

        #region [AI Detector Communication Fields]

        /// <summary>
        /// [AI] [Detector Agent] [TCP] 통신 서비스
        ///
        /// [AI] [Detector Agent]와 [TCP] 연결 후,
        /// 수신된 [AI Packet]을 [MainViewModel]로 전달한다.
        /// </summary>
        private readonly AiDetectorClientService _aiDetectorClientService;

        /// <summary>
        /// [AI] [Detector] [Packet Parser]
        ///
        /// [AI] [Detector Agent]에서 수신한 [Packet]을
        /// [CMD] / [SIZE] / [Payload] / [Checksum] 기준으로 해석한다.
        /// </summary>
        private readonly AiDetectorPacketParser _aiDetectorPacketParser;

        /// <summary>
        /// [AI Detector Agent] 요청 [Packet] 생성 객체
        ///
        /// 향후 [ONNX] 목록 조회,
        /// [RTSP] 정보 조회 등의 요청 [Packet] 생성에 사용한다.
        /// </summary>
        private readonly AiDetectorPacketBuilder _aiPacketBuilder;

        #endregion

        #region [Control State Fields]

        /// <summary>
        /// [PAN / TILT] 버튼 1회 클릭 시 이동할 각도 값
        ///
        /// 기존 [CONTROL AGENT] 프로그램의 [PT] 버튼 동작처럼
        /// 한 번 클릭할 때마다 [1.0]도 단위로 이동하도록 설정한다.
        /// </summary>
        private const double PanTiltMoveStep = 1.0;

        /// <summary>
        /// 현재 [PAN] 각도 값(현재 위치 저장용)
        ///
        /// [LA Status Packet] 수신 시 갱신되고,
        /// 버튼 클릭 시 상대 이동 계산 기준값으로 사용한다.
        /// </summary>
        private double _currentPan;

        /// <summary>
        /// 마지막으로 송신한 Pan Absolute 목표값
        ///
        /// -180도와 +180도는 물리적으로 같은 위치이므로
        /// LA 상태 Packet이 두 값을 모두 +180도로 반환할 수 있다.
        ///
        /// 사용자가 -180을 명령한 경우에는 Current Status도 -180으로,
        /// +180을 명령한 경우에는 +180으로 표시하기 위해
        /// 마지막 목표 부호를 보관한다.
        /// </summary>
        private double? _lastPanAbsoluteTarget;

        /// <summary>
        /// 현재 진행 중인 Pan / Tilt Absolute 이동 목표값
        ///
        /// 이동 중 속도 Slider가 변경되면 위치 속도 명령을 적용한 뒤
        /// 같은 목표 좌표를 다시 송신하기 위해 보관한다.
        /// 목표 도착, STOP, 연속 이동 시작 또는 연결 해제 시 초기화한다.
        /// </summary>
        private double? _activePanAbsoluteTarget;

        private double? _activeTiltAbsoluteTarget;

        /// <summary>
        /// 현재 [TILT] 각도 값(현재 위치 저장용)
        ///
        /// [LA Status Packet] 수신 시 갱신되고,
        /// 버튼 클릭 시 상대 이동 계산 기준값으로 사용한다.
        /// </summary>
        private double _currentTilt;

        /// <summary>
        /// 최신 Pan / Tilt 상태 Packet 수신 순번.
        ///
        /// 프리셋 이동 완료 판정에서 명령 이전의 오래된 상태값을
        /// 완료 상태로 오판하지 않도록 사용한다.
        /// </summary>
        private long _panTiltStatusVersion;

        /// <summary>
        /// 최신 IR Zoom / Focus 상태 Packet 수신 순번.
        ///
        /// 2026-08-27: 마우스 해제 후 STOP 명령보다 늦게 도착한
        /// 실제 장비 상태값인지 판별하는 데 사용한다.
        /// </summary>
        private long _irLensStatusVersion;

        /// <summary>
        /// 프로그램 시작 이후 고정밀 경과시간 측정용
        /// </summary>
        private readonly Stopwatch _focusLogStopwatch =
            Stopwatch.StartNew();

        /// <summary>
        /// 마지막 Focus 명령 송신 시각
        /// </summary>
        private long _lastEoFocusCommandElapsedMs;

        /// <summary>
        /// 마지막 Focus 명령 종류
        /// </summary>
        private string _lastEoFocusCommandName =
            "NONE";

        /// <summary>
        /// Focus 명령 송신 순번
        /// </summary>
        private int _eoFocusCommandSequence;

        /// <summary>
        /// Focus 상태 수신 순번
        /// </summary>
        private int _eoFocusReceiveSequence;

        /// <summary>
        /// 현재 어떤 [연속 제어]가 동작 중인지
        /// </summary>
        private ContinuousMoveType _currentMoveType = ContinuousMoveType.None;

        /// <summary>
        /// 현재 EO 연속 제어를 시작한 CTEC CGI 직접 제어 프리셋
        ///
        /// Zoom / Focus 시작 이후 사용자가 ComboBox 선택값을 변경하더라도
        /// Stop 명령이 반드시 시작 명령을 보낸 동일 카메라로 전송되도록 저장한다.
        ///
        /// null이면 현재 EO 연속 제어는 기존 Control Agent 경로이다.
        /// </summary>
        private RtspSourceOption _activeEoCtecSource;

        /// <summary>
        /// 현재 장비 연결 시점에 확정된 EO CTEC 직접 제어 프리셋
        ///
        /// ComboBox 선택값은 다음 장비 연결 시 적용되므로,
        /// 연결 중 선택값이 변경되어도 현재 TCP Response 연결 대상과
        /// 명령 조회 대상이 변경되지 않도록 별도로 저장한다.
        /// </summary>
        private RtspSourceOption _connectedEoCtecSource;

        /// <summary>
        /// [XV-Z4850HC] EO Optical Zoom Position 최대 원시값
        ///
        /// CTEC 응답의 Zoom Position은 0x0000 ~ 0x4000 범위이며,
        /// 십진수 기준 0 ~ 16384로 사용한다.
        /// </summary>
        private const int CtecEoZoomPositionMax =
            0x4000;

        /// <summary>
        /// [XV-Z4850HC] EO Focus Position 최대 원시값
        ///
        /// 실제 장비에서 확인한 Focus Position은 0x0000 ~ 0x8000 범위이며,
        /// 십진수 기준 0 ~ 32768로 사용한다.
        /// </summary>
        private const int CtecEoFocusPositionMax =
            0x8000;

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position Inquiry 주기
        ///
        /// 연속 이동 명령은 최초 1회만 송신하고,
        /// 버튼을 누르고 있는 동안 현재 Position 조회만 반복한다.
        ///
        /// 200ms = 초당 약 5회 상태 갱신
        /// </summary>
        private const int CtecEoPositionPollingIntervalMs =
            75;

        /// <summary>
        /// [CTEC Position Inquiry] TCP 9000 응답 제한시간
        ///
        /// CGI 200 OK는 Inquiry 명령 전달 성공일 뿐이며
        /// 실제 Position 값은 TCP 9000 응답을 받아야 확정된다.
        /// </summary>
        private const int CtecEoPositionResponseTimeoutMs =
            1000;

        /// <summary>
        /// Stop 이후 최종 Position 안정화 최대 조회 횟수
        /// </summary>
        private const int CtecEoPositionSettleMaximumCount =
            6;

        /// <summary>
        /// 연속 두 Position 차이가 이 값 이하이면 동일 위치로 판단한다.
        /// </summary>
        private const int CtecEoPositionStableTolerance =
            5;

        /// <summary>
        /// [CTEC EO Zoom / Focus] 실시간 Position Inquiry 종료 Token
        ///
        /// Zoom 또는 Focus 버튼을 누르면 생성하고,
        /// MouseUp / MouseLeave / Disconnect 시 Cancel한다.
        /// </summary>
        private CancellationTokenSource _ctecEoPositionPollingCts;

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신부터 TCP 응답 수신까지 한 묶음으로 직렬화한다.
        ///
        /// Inquiry CGI 자체의 Lock만으로는 TCP 응답 대기 구간이 보호되지 않으므로,
        /// Zoom / Focus Polling과 Stop 후 최종 조회가 동시에 대기 작업을 만들지 않게 한다.
        /// </summary>
        private readonly SemaphoreSlim _ctecEoPositionQueryLock =
            new SemaphoreSlim(1, 1);

        /// <summary>
        /// CTEC Zoom / Focus 이동 및 안정화 작업 세대 번호
        ///
        /// 새 이동이 시작되면 증가하며,
        /// 이전 Stop 안정화 작업은 세대가 달라진 즉시 종료한다.
        /// </summary>
        private long _ctecEoPositionOperationGeneration;

        /// <summary>
        /// [CURRENT STATUS] 현재 표시 중인 장비 구성
        ///
        /// Rooftop    : 한국씨텍 EO 직접 제어 상태
        /// Environment: Web Agent 기준 0 ~ 1000 상태
        /// </summary>
        private EquipmentStatusMode _selectedEquipmentStatusMode =
            EquipmentStatusMode.Rooftop;

        /// <summary>
        /// [EO / IR Zoom Synchronization] 현재 선택된 10단계 Zoom Level
        /// </summary>
        private ZoomSyncLevelOption _selectedZoomSyncLevel;

        /// <summary>
        /// [EO / IR Zoom Synchronization] 동작 상태 표시 문자열
        /// </summary>
        private string _zoomSyncStatusText =
            "READY";

        /// <summary>
        /// [옥상장비 Zoom Sync] 한국씨텍 EO 목표 위치 이동 종료 Token
        /// </summary>
        private CancellationTokenSource _rooftopZoomSyncCts;

        /// <summary>
        /// [옥상장비 Zoom Sync] EO Direct Position 도착 허용 오차
        ///
        /// Direct Position 명령은 목표값을 카메라에 직접 전달하므로
        /// 연속 이동 방식처럼 큰 제동 구간이나 방향 보정이 필요하지 않다.
        /// 실제 장비 Position 응답의 미세 편차만 허용한다.
        /// </summary>
        private const int RooftopZoomSyncTolerance =
            120;

        /// <summary>
        /// [옥상장비 Zoom Sync] Direct Position 도착 확인 주기
        /// </summary>
        private const int RooftopZoomSyncInquiryIntervalMs =
            100;

        /// <summary>
        /// [옥상장비 Zoom Sync] Direct Position 이동 완료 대기시간
        /// </summary>
        private const int RooftopZoomSyncTimeoutMs =
            15000;

        /// <summary>
        /// [EO / IR Focus Synchronization] 현재 선택된 10단계 Focus Level
        /// </summary>
        private ZoomSyncLevelOption _selectedFocusSyncLevel;

        /// <summary>
        /// [EO / IR Focus Synchronization] 동작 상태 표시 문자열
        /// </summary>
        private string _focusSyncStatusText =
            "READY";

        /// <summary>
        /// [옥상장비 Focus Sync] 한국씨텍 EO 목표 위치 이동 종료 Token
        /// </summary>
        private CancellationTokenSource _rooftopFocusSyncCts;

        /// <summary>
        /// [옥상장비 Focus Sync] EO Direct Position 도착 허용 오차
        ///
        /// CTEC EO Focus 전체 범위는 0 ~ 32768이며,
        /// 실제 응답의 미세 편차를 고려하여 ±240을 허용한다.
        /// </summary>
        private const int RooftopFocusSyncTolerance =
            240;

        /// <summary>
        /// [옥상장비 Focus Sync] Direct Position 도착 확인 주기
        /// </summary>
        private const int RooftopFocusSyncInquiryIntervalMs =
            100;

        /// <summary>
        /// [옥상장비 Focus Sync] Direct Position 이동 완료 대기시간
        /// </summary>
        private const int RooftopFocusSyncTimeoutMs =
            15000;

        /// <summary>
        /// [IR Focus Sync] 최종 완료 허용 오차
        ///
        /// IR Focus는 Near / Far 연속 구동 후 Stop 방식이므로
        /// 상태 수신 지연과 정지 지연을 고려해 최종 ±12 이내를 정상으로 판단한다.
        /// </summary>
        private const int IrFocusSyncTolerance =
            12;

        /// <summary>
        /// [IR Focus Sync] 이동 상태 확인 주기
        /// </summary>
        private const int IrFocusSyncPollingIntervalMs =
            20;

        /// <summary>
        /// [IR Focus Sync] Stop 명령 이후 위치 안정화 확인 주기
        /// </summary>
        private const int IrFocusSyncSettlePollingIntervalMs =
            40;

        /// <summary>
        /// [IR Focus Sync] Stop 이후 최대 안정화 대기시간
        /// </summary>
        private const int IrFocusSyncSettleTimeoutMs =
            700;

        /// <summary>
        /// [IR Focus Sync] 위치가 연속으로 동일 범위에 들어와야 하는 횟수
        /// </summary>
        private const int IrFocusSyncStableSampleCount =
            3;

        /// <summary>
        /// [IR Focus Sync] 1차 이동 시 기본 선행 정지 거리
        /// </summary>
        private const int IrFocusSyncInitialStopLead =
            38;

        /// <summary>
        /// [IR Focus Sync] 보정 이동 시 최소 선행 정지 거리
        /// </summary>
        private const int IrFocusSyncCorrectionStopLead =
            10;

        /// <summary>
        /// [IR Focus Sync] 최대 보정 횟수
        /// </summary>
        private const int IrFocusSyncMaxMoveAttempts =
            3;

        /// <summary>
        /// [IR Focus Sync] 최대 이동 대기시간
        /// </summary>
        private const int IrFocusSyncTimeoutMs =
            12000;

        /// <summary>
        /// CTEC Port 9000 응답으로 수신한 EO Optical Zoom Position
        ///
        /// 원시값 범위: 0x0000 ~ 0x4000 (0 ~ 16384)
        /// </summary>
        private ushort _currentCtecEoZoomPosition;

        /// <summary>
        /// CTEC Port 9000 응답으로 수신한 EO Focus Position
        ///
        /// 원시값 범위: 0x0000 ~ 0x8000 (0 ~ 32768)
        /// </summary>
        private ushort _currentCtecEoFocusPosition;

        /// <summary>
        /// CTEC Port 9000 응답으로 수신한 EO Focus Mode
        ///
        /// 0x02 = Auto
        /// 0x03 = Manual
        /// </summary>
        private byte _currentCtecEoFocusMode;

        /// <summary>
        /// Keyboard Pan Left 입력 상태
        /// </summary>
        private bool _isKeyboardPanLeftPressed;

        /// <summary>
        /// Keyboard Pan Right 입력 상태
        /// </summary>
        private bool _isKeyboardPanRightPressed;

        /// <summary>
        /// Keyboard Tilt Up 입력 상태
        /// </summary>
        private bool _isKeyboardTiltUpPressed;

        /// <summary>
        /// Keyboard Tilt Down 입력 상태
        /// </summary>
        private bool _isKeyboardTiltDownPressed;

        /// <summary>
        /// 현재 키보드 입력으로 실행 중인
        /// Pan / Tilt 이동 방향
        ///
        /// KeyDown 자동 반복으로 동일 패킷이 계속 송신되는 것을
        /// 방지하기 위해 마지막 적용 방향을 저장한다.
        /// </summary>
        private KeyboardPanTiltDirection
            _currentKeyboardPanTiltDirection =
                KeyboardPanTiltDirection.None;

        /// <summary>
        /// 마우스 또는 키보드로 현재 실행 중인 Pelco-D Pan / Tilt 방향.
        /// Slider 변경 시 같은 방향 명령을 새 속도로 즉시 재송신한다.
        /// </summary>
        private KeyboardPanTiltDirection
            _activePanTiltMoveDirection =
                KeyboardPanTiltDirection.None;

        #endregion

        #region [Control Properties]

        /// <summary>
        /// [EO] 주간 카메라 RTSP 주소 입력값
        ///
        /// 통신 설정 탭에서 직접 수정하며,
        /// 장비 연결 시 현재 입력값을 사용한다.
        /// </summary>
        private string _eoSourceAddress;

        /// <summary>
        /// [IR] 열상 카메라 RTSP 주소 입력값
        ///
        /// 통신 설정 탭에서 직접 수정하며,
        /// 장비 연결 시 현재 입력값을 사용한다.
        /// </summary>
        private string _irSourceAddress;

        /// <summary>
        /// Control Agent 제어 TCP 연결 IP 입력값
        /// </summary>
        private string _controlControlAgentIp;

        /// <summary>
        /// 옥상 MCB 유지보수 직접 연결 IP 입력값
        ///
        /// Control Agent(Local LA) 주소와 MCB 장비 주소는 서로 다르므로
        /// 별도 값으로 관리한다.
        /// </summary>
        private string _mcbMaintenanceIpAddress;

        /// <summary>
        /// Control Agent 제어 TCP 연결 Port 입력 문자열
        ///
        /// TextBox에 문자 또는 빈값이 입력되더라도
        /// 바인딩 변환 예외가 발생하지 않도록 string으로 관리한다.
        /// </summary>
        private string _controlControlAgentPortText;

        /// <summary>
        /// Control Agent 연결 중 상태 최소 표시시간
        ///
        /// TCP 연결이 매우 빠르게 완료되더라도
        /// Connecting 상태가 UI에 최소한 표시되도록 사용한다.
        /// </summary>
        private const int ControlAgentConnectingMinimumDisplayMs =
            300;

        /// <summary>
        /// [PAN / TILT] 속도제어 현재 속도 [Level]
        ///
        /// UI 속도 Level [0 ~ 50]을 사용한다.
        /// 실제 Pelco-D 송신 시 [1 ~ 63]으로 환산하며 UI 0은 STOP으로 처리한다.
        /// 현재 기본값은 [30]으로 설정한다.
        ///
        /// 이후 [Slider] 또는 [ComboBox] 등 [UI] 조작으로 값이 변경될 수 있으며,
        /// 실제 연속 이동 제어 시 해당 값을 사용한다.
        /// </summary>
        private byte _panTiltSpeedLevel = 30;


        /// <summary>
        /// [ZOOM] 버튼 1회 클릭 시 이동할 값
        ///
        /// 문서 기준 Zoom 값은 [열상 화각 × 100] 형태로 송신한다.
        /// 따라서 [10] 단위 이동은 화각 기준 약 [0.1] 단위 조정으로 사용한다.
        /// </summary>
        private const short ZoomMoveStep = 10;

        /// <summary>
        /// [FOCUS] 버튼 1회 클릭 시 이동할 값
        ///
        /// 문서 기준 Focus 위치값은
        /// [0 = Focus Far] ~ [1000 = Focus Near] 범위를 사용한다.
        /// </summary>
        private const short FocusMoveStep = 5;

        /// <summary>
        /// [LA Status Packet]에서 수신한 [EO] [Zoom] 현재 값
        ///
        /// 일반 상태 [Packet]의 [Zoom] 값은
        ///
        /// [IR]이 아닌 [EO] 기준 값으로 확인되어
        /// [EO Zoom] 상태값으로 관리한다.
        /// </summary>
        private short _currentEoZoom;

        /// <summary>
        /// [LA Status Packet]에서 수신한 [EO] [Focus] 현재 값
        ///
        /// 일반 상태 [Packet]의 [Focus] 값은
        ///
        /// [IR]이 아닌 [EO] 기준 값으로 확인되어
        /// [EO Focus] 상태값으로 관리한다.
        /// </summary>
        private short _currentEoFocus;

        /// <summary>
        /// 마지막 EO Focus Command 실행 시간
        ///
        /// 일정 시간 이상 입력이 없으면
        /// 다음 입력 시 실제 상태값으로 다시 동기화한다.
        /// </summary>
        private DateTime _lastEoFocusCommandTime =
            DateTime.MinValue;

        /// <summary>
        /// [LRF] 최근 거리측정 값 표시 문자열
        /// </summary>
        private string _lrfDistanceText = "DISTANCE : - m";

        /// <summary>
        /// [LA Status Packet]에서 수신한 장비 전원 상태값
        /// </summary>
        private byte _currentPowerStatus;

        /// <summary>
        /// [IR] 상태 Packet에서 수신한 Zoom 현재 값
        ///
        /// 실제 필드 의미가 확정되기 전까지
        /// 수신 Raw 값을 기준으로 관리한다.
        /// </summary>
        private ushort _currentIrZoom;

        /// <summary>
        /// [IR] 상태 Packet에서 수신한 Focus 현재 값
        ///
        /// 실제 필드 의미가 확정되기 전까지
        /// 수신 Raw 값을 기준으로 관리한다.
        /// </summary>
        private ushort _currentIrFocus;

        #region [Move Control Constants / Fields]

        /// <summary>
        /// 이동 제어 화면에서 사용하는 공통 Lens Position 최소값
        /// </summary>
        private const int MoveControlPositionMinimum =
            0;

        /// <summary>
        /// 이동 제어 화면에서 사용하는 공통 Lens Position 최대값
        /// </summary>
        private const int MoveControlPositionMaximum =
            1000;

        /// <summary>
        /// EO / IR 광학 Zoom 최소 배율
        /// </summary>
        private const double MoveControlMinimumZoomRatio =
            1.0;

        /// <summary>
        /// 옥상 EO 카메라 XV-Z2050HC 최대 광학 배율
        ///
        /// 6 ~ 300mm:
        /// 광학 50배
        /// </summary>
        private const double MoveControlEoMaximumZoomRatio =
            50.0;

        /// <summary>
        /// IR 카메라 Infra-LWZ-30-150-AF 최대 광학 배율
        ///
        /// 30 ~ 150mm:
        /// 광학 5배
        /// </summary>
        private const double MoveControlIrMaximumZoomRatio =
            5.0;

        /// <summary>
        /// LA / Pelco-D 기준 Pan 입력 허용 범위
        ///
        /// Pan Absolute는 -180 ~ 180을 사용한다.
        /// 범위를 벗어난 입력은 각각 -180 또는 180으로 제한한다.
        /// </summary>
        private const double MoveControlPanMinimum =
            -180.0;

        private const double MoveControlPanMaximum =
            180.0;

        /// <summary>
        /// Tilt Absolute 입력 허용 범위
        /// </summary>
        private const double MoveControlTiltMinimum =
            -90.0;

        private const double MoveControlTiltMaximum =
            90.0;

        /// <summary>
        /// 현재 Pan 선회 모드
        /// </summary>
        private PanTurnMode _panTurnMode =
            PanTurnMode.Short;

        /// <summary>
        /// Pan Absolute 입력값
        /// </summary>
        private double? _panAbsoluteValue =
            0.0;

        /// <summary>
        /// Tilt Absolute 입력값
        /// </summary>
        private double? _tiltAbsoluteValue =
            0.0;

        /// <summary>
        /// EO / IR Zoom 공통 Position 입력값
        /// </summary>
        private int? _zoomPositionValue =
            0;

        /// <summary>
        /// EO / IR Zoom 공통 배율 입력값
        /// </summary>
        private double? _zoomRatioValue =
            1.0;

        /// <summary>
        /// EO / IR Focus 공통 Position 입력값
        /// </summary>
        private int? _focusPositionValue =
            0;

        /// <summary>
        /// Home / Pan Zero / Tilt Zero 최근 실행 상태
        /// </summary>
        private string _homeZeroStatusText =
            "READY";

        /// <summary>
        /// HOME / PAN ZERO / TILT ZERO 공통 잠금 화면 제목.
        /// 현재 실행 중인 작업에 따라 XAML Overlay 문구를 구분한다.
        /// </summary>
        private string _homeZeroLockTitle =
            "HOME / ZERO OPERATION";

        /// <summary>
        /// HOME / ZERO 공통 잠금 화면 상세 문구.
        /// </summary>
        private string _homeZeroLockMessage =
            "PROCESSING...";

        /// <summary>
        /// HOME POSITION 이동 진행 여부.
        ///
        /// true인 동안에는 우측의 모든 버튼/탭과
        /// 키보드 제어를 차단한다.
        /// HOME / ZERO 기능은 LA AGENT(ROOFTOP) 전용이며,
        /// WEB AGENT(ENVIRONMENT) 선택 상태에서는 실행하지 않는다.
        /// </summary>
        private bool _isHomePositionMoving;

        /// <summary>
        /// HOME / PAN ZERO / TILT ZERO 작업을 직렬화한다.
        ///
        /// Zero 명령의 마지막 Motor On 안정화 대기까지 동일 Lock 범위에 포함하여
        /// HOME 또는 다른 Zero 명령이 중간에 겹치지 않도록 한다.
        /// </summary>
        private readonly SemaphoreSlim _homeZeroOperationLock =
            new SemaphoreSlim(1, 1);

        /// <summary>
        /// HOME POSITION 완료 판정 최대 대기시간.
        /// </summary>
        private const int HomePositionTimeoutMs =
            30000;

        /// <summary>
        /// HOME POSITION 상태 확인 주기.
        /// </summary>
        private const int HomePositionPollingIntervalMs =
            100;

        /// <summary>
        /// HOME POSITION 목표 위치 허용 오차.
        /// </summary>
        private const double HomePositionTargetTolerance =
            0.50;

        /// <summary>
        /// HOME POSITION 정지 상태 판정용 연속 안정 샘플 수.
        /// </summary>
        private const int HomePositionStableSampleCount =
            5;

        /// <summary>
        /// HOME POSITION 연속 상태값 변화 허용 오차.
        /// </summary>
        private const double HomePositionStableTolerance =
            0.05;

        /// <summary>
        /// 프리셋 반복 이동 시 목표각과 현재각이 같은 0.01° 값인지 판단하는 허용 오차.
        ///
        /// 상태 및 명령 Protocol의 분해능이 0.01°이므로 반올림 경계와
        /// 부동 소수점 오차를 포함한 0.015° 이내를 동일 위치로 판단한다.
        /// </summary>
        private const double PresetPanTiltTargetTolerance =
            0.015;

        /// <summary>
        /// 목표 범위에 들어온 최신 상태 Packet 한 건으로 도착을 확정한다.
        /// 별도의 정착 대기를 추가하지 않기 위한 값이다.
        /// </summary>
        private const int PresetPanTiltStableSampleCount =
            1;

        /// <summary>
        /// 목표 밖에서 장비 위치가 멈췄다고 판단할 연속 상태 Packet 수.
        /// </summary>
        private const int PresetPanTiltStationarySampleCount =
            3;

        /// <summary>
        /// 통신 이상으로 상태 Packet이 오지 않는 경우를 위한 안전 여유시간.
        /// 정상 도착 시에는 이 시간까지 기다리지 않고 즉시 반환한다.
        /// </summary>
        private const int PresetPanTiltSafetyTimeoutMarginMs =
            1500;

        private const int PresetPanTiltSettleTimeoutMinimumMs =
            2000;

        private const int PresetPanTiltSettleTimeoutMaximumMs =
            60000;

        /// <summary>
        /// 목표 밖에서 이동이 멈춘 경우 허용하는 즉시 재보정 횟수.
        /// </summary>
        private const int PresetPanTiltCorrectionRetryCount =
            1;

        /// <summary>
        /// PRESET 1 (LA TEST) 선택 ID
        /// LA 실제 구현 기준 0 ~ 99
        /// </summary>
        private int _laPresetSlotNumber =
            1;

        private PresetPointOption _selectedLaPresetPoint;

        private int _laPresetScanSpeed =
            10;

        private int _laPresetScanDelay =
            1;

        private string _laPresetCommandStatusText =
            "READY";

        private bool _isLaPresetScanRunning;

        /// <summary>
        /// 프리셋 추가 / 제거 대상 슬롯 번호
        ///
        /// TORUSS 프리셋 실행 / 편집 명령 기준:
        /// 1 ~ 63
        /// </summary>
        private int _presetSlotNumber =
            1;

        /// <summary>
        /// PRESET ComboBox에서 현재 선택된 프리셋
        /// </summary>
        private PresetPointOption _selectedPresetPoint;

        /// <summary>
        /// 오토 스캔 이동 속도
        ///
        /// TORUSS 문서 기준:
        /// 1 ~ 60
        /// </summary>
        private int _presetScanSpeed =
            10;

        /// <summary>
        /// 오토 스캔 프리셋 정지시간
        ///
        /// TORUSS 문서 기준:
        /// 1 ~ 60초
        /// </summary>
        private int _presetScanDelay =
            1;

        /// <summary>
        /// 마지막 프리셋 / 스캔 명령 상태 표시
        ///
        /// 별도 ACK / 프리셋 상태 응답이 없으므로
        /// 실제 장비 상태가 아니라 TCP 송신 결과를 표시한다.
        /// </summary>
        private string _presetCommandStatusText =
            "READY";

        /// <summary>
        /// 현재 프로그램에서 스캔 시작 명령을 송신한 상태
        ///
        /// 장비 응답 기반 상태가 아닌 UI 표시용 로컬 상태이다.
        /// </summary>
        private bool _isPresetScanRunning;

        /// <summary>
        /// VIA 0 Pan 연속 이동 취소 Token
        /// </summary>
        private CancellationTokenSource _moveControlPanCts;

        /// <summary>
        /// PRESET 1 WPF 직접 오토 스캔 작업 취소 토큰
        /// </summary>
        private CancellationTokenSource _laPresetDirectScanCts;

        /// <summary>
        /// PRESET 2 WEB AGENT Pelco-D 슬롯 순회 취소 토큰
        /// </summary>
        private CancellationTokenSource _presetDirectScanCts;

        /// <summary>
        /// PRESET L 단일 MOVE TO PRESET 작업 취소 토큰
        /// </summary>
        private CancellationTokenSource _laPresetSingleMoveCts;

        /// <summary>
        /// PRESET W 단일 MOVE TO PRESET 작업 취소 토큰
        /// </summary>
        private CancellationTokenSource _presetSingleMoveCts;

        /// <summary>
        /// 우측 상위 탭 선택 인덱스.
        /// 0: 통신 설정, 1: 운용 제어
        /// </summary>
        private int _selectedRightPanelTabIndex;

        /// <summary>
        /// 운용 제어 하위 탭 선택 인덱스.
        /// 0: PTZF
        /// </summary>
        private int _selectedOperationControlTabIndex;

        #endregion

        #endregion

        #region [LA Packet Fields]

        /// <summary>
        /// [CONTROL AGENT] 수신 [Packet Parser]
        ///
        /// [TcpClientService]에서 받은 byte[] 데이터를
        /// [12byte] 단위의 [CONTROL AGENT] 응답 [Packet]으로 분리 / 검증하는 역할
        /// </summary>
        private readonly LAPacketParser _laPacketParser;

        /// <summary>
        /// 마지막 [CONTROL AGENT] 상태 로그 출력 시간
        ///
        /// [Pan] / [Tilt] / [EO Zoom] / [EO Focus]
        /// 상태 [Packet]은 약 [10Hz] 주기로 수신되므로,
        /// [Console] 도배 방지 목적으로 사용한다.
        /// </summary>
        private DateTime _lastLaStatusLogTime = DateTime.MinValue;

        /// <summary>
        /// 마지막 [CONTROL AGENT] [Extended Status] 로그 출력 시간
        ///
        /// [IR] 확장 상태 [Packet]은
        /// 지속적으로 수신되므로,
        /// [Console] 도배 방지 목적으로 사용한다.
        /// </summary>
        private DateTime _lastLaExtendedStatusLogTime = DateTime.MinValue;

        /// <summary>
        /// [CONTROL AGENT] 상태 로그 출력 간격
        ///
        /// [0x01] 기본 상태 Packet
        /// [0xA1] 확장 상태 Packet
        /// 로그 출력 주기 계산에 사용한다.
        /// </summary>
        private const int LaLogIntervalSeconds = 1;

        #endregion

        #region [AI Detector Packet Fields]

        /// <summary>
        /// 마지막 [AI Detector] 탐지 로그 출력 시간
        ///
        /// [AI Detector] 탐지 [Packet]은 매우 빠르게 들어오므로,
        /// [Console] 도배 방지 목적으로 사용한다.
        /// </summary>
        private DateTime _lastAiDetectorLogTime = DateTime.MinValue;

        /// <summary>
        /// [AI Detector] 탐지 로그 출력 간격
        /// </summary>
        private const int AiDetectorLogIntervalSeconds = 3;

        #endregion

        #region [AI Detector Setting Fields]

        /// <summary>
        /// [AI Detector Agent] 연결 [IP]
        /// </summary>
        // 2026-08-25: AI Detector 운영 서버 기본 주소를 192.168.20.165로 변경한다. (Port 5055 유지)
        private string _aiControlAgentIp = "192.168.20.165";

        /// <summary>
        /// [AI Detector Agent] 연결 [Port]
        /// </summary>
        private int _aiAgentPort = 5055;

        /// <summary>
        /// [AI Detector Agent] 분석 대상 [RTSP Index 0] 주소
        ///
        /// 기본값은 [EO] 영상 주소를 사용한다.
        /// </summary>
        private string _aiRtsp0Address;

        /// <summary>
        /// [AI Detector Agent] 분석 대상 [RTSP Index 1] 주소
        ///
        /// 기본값은 [IR] 영상 주소를 사용한다.
        /// </summary>
        private string _aiRtsp1Address;

        /// <summary>
        /// [RTSP Index 0]에 연결할 [ONNX Index]
        /// </summary>
        private int _aiRtsp0OnnxIndex;

        /// <summary>
        /// [RTSP Index 1]에 연결할 [ONNX Index]
        /// </summary>
        private int _aiRtsp1OnnxIndex;

        /// <summary>
        /// [AI Detector] [Mapping Confidence] 기준값
        /// </summary>
        private double _aiMappingConfidence;

        /// <summary>
        /// [AI Detector] [Mapping IOU] 기준값
        /// </summary>
        private double _aiMappingIou;

        /// <summary>
        /// 화면에 표시할 [Bounding Box] 최소 [Confidence] 기준값
        /// </summary>
        private double _aiDisplayConfidenceThreshold;

        /// <summary>
        /// [AI Detector Setting] 상태 표시 문자열
        /// </summary>
        private string _aiSettingStatusText = "AI Setting Ready";

        /// <summary>
        /// [AI Detector Agent] 연결 상태 화면 표시 문자열
        ///
        /// CONNECTION STATUS 영역의 AI 상태를
        /// CONTROL / EO / IR 상태와 동일한 형식으로 표시한다.
        /// </summary>
        private string _aiPowerStatusText = "OFF";

        /// <summary>
        /// [AI Tracking] 자동 추적 사용 여부
        /// </summary>
        // AUTO TRACKING은 후속 구현 전에도 기본 선택 상태로 표시한다.
        private bool _isAutoTrackingEnabled = true;

        /// <summary>
        /// [EO / IR] 영상 중앙 십자선 표시 여부
        ///
        /// true:
        /// EO / IR 영상 화면 중앙에 십자선을 표시한다.
        ///
        /// false:
        /// EO / IR 영상 화면의 십자선을 숨긴다.
        ///
        /// 십자선은 RTSP 원본 Frame에 직접 그리지 않고
        /// WPF Overlay로 표시하므로 AI Bounding Box 좌표와
        /// 영상 Decoder 처리에는 영향을 주지 않는다.
        /// </summary>
        private bool _isCrosshairVisible =
            false;

        /// <summary>
        /// 중앙 십자선의 현재 표시 색상.
        /// 최초 색상은 기존 적색이며, 사용자가 십자선을 다시 켤 때마다
        /// 적색 -> 주황 -> 황색 -> 녹색 -> 청록 -> 청색 -> 보라 순으로 변경한다.
        /// </summary>
        private Brush _crosshairBrush =
            new SolidColorBrush(
                Color.FromArgb(
                    0xFF,
                    0xFF,
                    0x3B,
                    0x30));

        private int _crosshairColorIndex;

        private bool _hasCrosshairBeenDisplayed;

        #endregion

        #region [AI Overlay Size Binding Fields]

        /// <summary>
        /// [EO] [RTSP] 원본 영상 너비
        ///
        /// [FFmpegDecoderService]에서 읽은
        /// 실제 [RTSP] 원본 해상도 저장용.
        /// </summary>
        private int _eoVideoWidth;

        /// <summary>
        /// [EO] [RTSP] 원본 영상 높이
        ///
        /// [FFmpegDecoderService]에서 읽은
        /// 실제 [RTSP] 원본 해상도 저장용.
        /// </summary>
        private int _eoVideoHeight;

        /// <summary>
        /// [IR] [RTSP] 원본 영상 너비
        ///
        /// [FFmpegDecoderService]에서 읽은
        /// 실제 [RTSP] 원본 해상도 저장용.
        /// </summary>
        private int _irVideoWidth;

        /// <summary>
        /// [IR] [RTSP] 원본 영상 높이
        ///
        /// [FFmpegDecoderService]에서 읽은
        /// 실제 [RTSP] 원본 해상도 저장용.
        /// </summary>
        private int _irVideoHeight;

        #endregion

        #region [Video Runtime Fields]

        /// <summary>
        /// 영상 루프를 중지하기 위한 [CancellationTokenSource]
        ///
        /// [Connect] 시 새로 생성하고,
        /// [Disconnect] 시 [Cancel / Dispose] 처리한다.
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// [Control Agent] 제어 TCP 자동 재연결 Loop 종료 Token
        /// </summary>
        private CancellationTokenSource _controlAgentReconnectCts;

        /// <summary>
        /// [EO / IR] RTSP 자동 재연결 Loop 종료 Token
        /// </summary>
        private CancellationTokenSource _videoReconnectCts;

        /// <summary>
        /// 사용자가 장비 연결 상태를 유지하도록 요청한 상태
        ///
        /// 서버 또는 RTSP가 아직 준비되지 않았더라도
        /// 연결 해제 버튼을 누르기 전까지 자동 재연결을 유지한다.
        /// </summary>
        private bool _isDeviceConnectionRequested;

        #endregion

        #region [Image Binding Fields]

        /// <summary>
        /// 왼쪽 상하단 [EO] 주간 영상 출력용 [Image]
        /// </summary>
        private BitmapSource _eoCameraImage;

        /// <summary>
        /// 오른쪽 상단 [IR] 열상 영상 출력용 [Image]
        /// </summary>
        private BitmapSource _irCameraImage;

        #endregion

        #region [Status Binding Fields]

        /// <summary>
        /// [EO] 영상 상태 표시
        /// </summary>
        private string _eoStatusText = "[EO] Disconnected";

        /// <summary>
        /// [IR] 영상 상태 표시
        /// </summary>
        private string _irStatusText = "[IR] Disconnected";

        /// <summary>
        /// Control Agent TCP 연결 상태 문자열
        ///
        /// Disconnected
        /// Connecting
        /// Connected
        /// Reconnecting
        /// </summary>
        private string _controlAgentConnectionStatusText =
            "Disconnected";

        /// <summary>
        /// Control Agent TCP 연결 상태 표시 색상
        ///
        /// Disconnected : Red
        /// Connecting   : Yellow
        /// Connected    : Green
        /// Reconnecting : Yellow
        /// </summary>
        private string _controlAgentConnectionStatusColor =
            "#FF6B6B";

        /// <summary>
        /// 현재 영상 [Connect] 진행 중 여부
        ///
        /// true  : [Connect] 수행 중
        /// false : 연결 완료 또는 종료 상태
        /// </summary>
        private bool _isVideoConnecting;

        #endregion

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MainViewModel] 생성자 (초기화 역할)
        /// </summary>
        public MainViewModel()
        {
            ConsoleLogHelper.InfoSection(
                "MAIN",
                "MainViewModel initialization started");

            #region [Command Initialize]

            #region [Display Overlay Command Binding]

            /// <summary>
            /// [EO / IR] 중앙 십자선 표시 상태 전환
            ///
            /// 버튼을 눌러 숨긴 뒤 다시 표시할 때마다
            /// EO / IR 영상의 십자선 색상을 7가지 색상으로 순환한다.
            /// </summary>
            ToggleCrosshairCommand =
                new RelayCommand(() =>
                {
                    if (!IsCrosshairVisible &&
                        _hasCrosshairBeenDisplayed)
                    {
                        AdvanceCrosshairColor();
                    }

                    IsCrosshairVisible =
                        !IsCrosshairVisible;
                });


            /// <summary>
            /// [Environment Equipment / Zoom Synchronization]
            ///
            /// Web Agent 기준 EO / IR Zoom Position을
            /// 0부터 1000까지 100 단위로 생성한다.
            ///
            /// LEVEL과 Position을 동일한 기준으로 사용한다.
            ///
            /// LEVEL 0  = 0
            /// LEVEL 1  = 100
            /// LEVEL 2  = 200
            /// ...
            /// LEVEL 10 = 1000
            ///
            /// Enumerable.Range(0, 11):
            /// 0부터 10까지 총 11개의 항목을 생성한다.
            /// </summary>
            ZoomSyncLevelOptions =
                new ObservableCollection<ZoomSyncLevelOption>(
                    Enumerable.Range(
                            0,
                            11)
                        .Select(level =>
                            new ZoomSyncLevelOption(
                                level,
                                (short)(
                                    level *
                                    100))));

            SelectedZoomSyncLevel =
                ZoomSyncLevelOptions[0];

            ShowRooftopStatusCommand =
                new RelayCommand(() =>
                    SelectedEquipmentStatusMode =
                        EquipmentStatusMode.Rooftop);

            ShowEnvironmentStatusCommand =
                new RelayCommand(() =>
                    SelectedEquipmentStatusMode =
                        EquipmentStatusMode.Environment);

            PreviousZoomSyncLevelCommand =
                new RelayCommand(SelectPreviousZoomSyncLevel);

            NextZoomSyncLevelCommand =
                new RelayCommand(SelectNextZoomSyncLevel);

            ApplyZoomSyncCommand =
                new AsyncRelayCommand(ApplySelectedZoomSyncLevelAsync);

            StopZoomSyncCommand =
                new AsyncRelayCommand(StopZoomSyncAsync);

            /// <summary>
            /// [EO / IR Focus Synchronization]
            ///
            /// Focus 역시 Web Agent 표준 범위 0 ~ 1000을 사용하며,
            /// Zoom Sync와 동일하게 LEVEL 0 ~ LEVEL 10으로 구성한다.
            /// </summary>
            FocusSyncLevelOptions =
                new ObservableCollection<ZoomSyncLevelOption>(
                    Enumerable.Range(
                            0,
                            11)
                        .Select(level =>
                            new ZoomSyncLevelOption(
                                level,
                                (short)(
                                    level *
                                    100))));

            SelectedFocusSyncLevel =
                FocusSyncLevelOptions[0];

            PreviousFocusSyncLevelCommand =
                new RelayCommand(
                    SelectPreviousFocusSyncLevel);

            NextFocusSyncLevelCommand =
                new RelayCommand(
                    SelectNextFocusSyncLevel);

            ApplyFocusSyncCommand =
                new AsyncRelayCommand(
                    ApplySelectedFocusSyncLevelAsync);

            StopFocusSyncCommand =
                new AsyncRelayCommand(
                    StopFocusSyncAsync);

            #endregion

            #region [Connect / Disconnect Command Binding]

            /// <summary>
            /// [Connect] 버튼 클릭 시 호출
            ///
            /// 영상 스트림 및 [CONTROL AGENT] [TCP] 통신 연결을 시작한다.
            /// </summary>
            ConnectCommand = new RelayCommand(Connect);

            /// <summary>
            /// [Disconnect] 버튼 클릭 시 호출
            ///
            /// 영상 스트림 및 [CONTROL AGENT] [TCP] 통신 연결을 종료한다.
            /// </summary>
            DisconnectCommand = new RelayCommand(Disconnect);

            #endregion

            #region [AI Detector Setting Command Binding]

            /// <summary>
            /// [AI Detector Agent] 수동 연결
            ///
            /// 기존 [AI Detector Agent] 연결 및 자동 재연결 루프를 정리한 뒤,
            /// UI에 입력된 [IP] / [Port] 기준으로 수동 1회 연결을 수행한다.
            /// </summary>
            ConnectAiAgentCommand =
                new AsyncRelayCommand(
                    ConnectAiAgentFromSettingAsync);

            DisconnectAiAgentCommand =
                new RelayCommand(
                    DisconnectAiAgent);

            /// <summary>
            /// [AI Detector Agent] [RTSP] 주소 적용
            ///
            /// UI에 입력한 [RTSP 0] / [RTSP 1] 주소를
            /// [CMD 02] 요청 Packet으로 송신한다.
            /// </summary>
            ApplyAiRtspCommand =
                new AsyncRelayCommand(
                    async () =>
                    {
                        AiSettingStatusText = "[AI] Apply RTSP...";

                        await RequestAiDetectorRtspAddressSetAsync();

                        AiSettingStatusText = "[AI] RTSP Apply Complete";
                    });

            /// <summary>
            /// [AI Detector Agent] Mapping 설정 적용
            ///
            /// UI에 입력한 [ONNX Index] / [Confidence] / [IOU] 값을
            /// [CMD 05] 요청 Packet으로 송신한다.
            /// </summary>
            ApplyAiMappingCommand =
                new AsyncRelayCommand(
                    async () =>
                    {
                        AiSettingStatusText = "[AI] Apply Mapping...";
                        await RequestAiDetectorMappingSetAsync();
                        AiSettingStatusText = "[AI] Mapping Apply Complete";
                    });

            /// <summary>
            /// [AI Detector Agent] 현재 설정 조회
            ///
            /// [Detector Info] / [RTSP List] / [ONNX List] / [Mapping Info]를 순차 조회한다.
            /// </summary>
            RefreshAiSettingCommand =
                new AsyncRelayCommand(
                    async () =>
                    {
                        if (!_aiDetectorClientService.IsConnected)
                        {
                            AiSettingStatusText =
                                "[AI] Not Connected";

                            ConsoleLogHelper.PrintLine();

                            Console.WriteLine(
                                "[AI TCP] Refresh Failed : Not Connected");

                            ConsoleLogHelper.PrintLine();

                            return;
                        }

                        AiSettingStatusText = "[AI] Refresh Setting...";

                        await RequestAiDetectorInfoAsync();
                        await Task.Delay(200);

                        await RequestAiDetectorRtspAddressAsync();
                        await Task.Delay(200);

                        await RequestAiDetectorOnnxListAsync();
                        await Task.Delay(200);

                        await RequestAiDetectorMappingAsync();

                        AiSettingStatusText = "[AI] Refresh Complete";
                    });

            #endregion

            #region [Pan / Tilt Command Binding]

            /// <summary>
            /// [PAN] 왼쪽 상대 이동 테스트
            ///
            /// 현재 [PAN] 값에서 [1.0]도 감소한 값을 목표 각도로 송신한다.
            /// </summary>
            PanLeftCommand = new RelayCommand(() =>
            {
                double targetPan = _currentPan - PanTiltMoveStep;

                Console.WriteLine();
                Console.WriteLine($"[CONTROL] PAN -{PanTiltMoveStep} => Target : {targetPan:F2}");
                ConsoleLogHelper.PrintLine();

                _controlCommandService.PanGoPosition(targetPan);
            });

            /// <summary>
            /// [PAN] 오른쪽 상대 이동 테스트
            ///
            /// 현재 [PAN] 값에서 [1.0]도 증가한 값을 목표 각도로 송신한다.
            /// </summary>
            PanRightCommand = new RelayCommand(() =>
            {
                double targetPan = _currentPan + PanTiltMoveStep;

                Console.WriteLine();
                Console.WriteLine($"[CONTROL] PAN +{PanTiltMoveStep} => Target : {targetPan:F2}");
                ConsoleLogHelper.PrintLine();

                _controlCommandService.PanGoPosition(targetPan);
            });

            /// <summary>
            /// [TILT] 위쪽 상대 이동 테스트
            ///
            /// 현재 [TILT] 값에서 [1.0]도 증가한 값을 목표 각도로 송신한다.
            /// </summary>
            TiltUpCommand = new RelayCommand(() =>
            {
                double targetTilt = _currentTilt + PanTiltMoveStep;

                Console.WriteLine();
                Console.WriteLine($"[CONTROL] TILT +{PanTiltMoveStep} => Target : {targetTilt:F2}");
                ConsoleLogHelper.PrintLine();

                _controlCommandService.TiltGoPosition(targetTilt);
            });

            /// <summary>
            /// [TILT] 아래쪽 상대 이동 테스트
            ///
            /// 현재 [TILT] 값에서 [1.0]도 감소한 값을 목표 각도로 송신한다.
            /// </summary>
            TiltDownCommand = new RelayCommand(() =>
            {
                double targetTilt = _currentTilt - PanTiltMoveStep;

                Console.WriteLine();
                Console.WriteLine($"[CONTROL] TILT -{PanTiltMoveStep} => Target : {targetTilt:F2}");
                ConsoleLogHelper.PrintLine();

                _controlCommandService.TiltGoPosition(targetTilt);
            });

            #endregion

            #region [Zoom / Focus Command Binding]

            /// <summary>
            /// [ZOOM] 확대 상대 이동 테스트
            ///
            /// 현재 [ZOOM] 값에서 [1] 증가한 값을 목표 위치로 송신한다.
            /// </summary>
            ZoomInCommand = new RelayCommand(() =>
            {
                short targetZoom = (short)(_currentEoZoom + ZoomMoveStep);

                Console.WriteLine();
                Console.WriteLine($"[CONTROL] ZOOM +{ZoomMoveStep} => Target : {targetZoom}");
                ConsoleLogHelper.PrintLine();

                _controlCommandService.EoZoomGoPosition(targetZoom);
            });

            /// <summary>
            /// [ZOOM] 축소 상대 이동 테스트
            ///
            /// 현재 [ZOOM] 값에서 [1] 감소한 값을 목표 위치로 송신한다.
            /// </summary>
            ZoomOutCommand = new RelayCommand(() =>
            {
                short targetZoom = (short)(_currentEoZoom - ZoomMoveStep);

                Console.WriteLine();
                Console.WriteLine($"[CONTROL] ZOOM -{ZoomMoveStep} => Target : {targetZoom}");
                ConsoleLogHelper.PrintLine();

                _controlCommandService.EoZoomGoPosition(targetZoom);
            });

            FocusFarCommand = new RelayCommand(() =>
            {
                int targetFocus =
                    Math.Max(
                        0,
                        _currentEoFocus -
                        FocusMoveStep);

                Console.WriteLine();
                Console.WriteLine(
                    $"[CONTROL] EO FOCUS FAR : " +
                    $"{_currentEoFocus} -> {targetFocus}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService
                    .EoFocusGoPosition(
                        (short)targetFocus);
            });

            FocusNearCommand = new RelayCommand(() =>
            {
                int targetFocus =
                    Math.Min(
                        1000,
                        _currentEoFocus +
                        FocusMoveStep);

                Console.WriteLine();
                Console.WriteLine(
                    $"[CONTROL] EO FOCUS NEAR : " +
                    $"{_currentEoFocus} -> {targetFocus}");

                ConsoleLogHelper.PrintLine();

                _controlCommandService
                    .EoFocusGoPosition(
                        (short)targetFocus);
            });

            #endregion

            #region [Move Control Command Binding]

            /// <summary>
            /// VertiportNexus 이동 제어 기능을 현재 프로젝트 구조에 맞춰 연결한다.
            ///
            /// 포함 기능:
            /// - Home Position : 현재 좌표계 기준 Pan 0° / Tilt 0° 절대 이동
            /// - Pan Zero / Tilt Zero : MCB 직접 Set0 명령
            /// </summary>
            MoveHomePositionCommand =
                new AsyncRelayCommand(
                    MoveHomePositionAsync);

            SetPanZeroCommand =
                new AsyncRelayCommand(
                    SetPanZeroAsync);

            SetTiltZeroCommand =
                new AsyncRelayCommand(
                    SetTiltZeroAsync);

            MovePanAbsoluteCommand =
                new AsyncRelayCommand(
                    MovePanAbsoluteFromInputAsync);

            MoveTiltAbsoluteCommand =
                new RelayCommand(
                    MoveTiltAbsoluteFromInput);

            StopAbsoluteMoveCommand =
                new RelayCommand(
                    StopAbsoluteMove);

            SetZoomPositionCommand =
                new AsyncRelayCommand(
                    SetZoomPositionFromInputAsync);

            SetZoomRatioCommand =
                new AsyncRelayCommand(
                    SetZoomRatioFromInputAsync);

            SetFocusPositionCommand =
                new AsyncRelayCommand(
                    SetFocusPositionFromInputAsync);

            ResetPositionInputCommand =
                new RelayCommand(
                    ResetMoveControlInput);


            AddOrUpdateLaPresetCommand =
                new RelayCommand(
                    AddOrUpdateLaPresetPoint);

            ClearAllLaPresetsCommand =
                new RelayCommand(
                    ClearAllLaPresetPoints);

            DeleteSelectedLaPresetCommand =
                new RelayCommand(
                    DeleteSelectedLaPresetPoint);

            MoveToLaPresetCommand =
                new AsyncRelayCommand(
                    MoveToSelectedLaPresetPointAsync);

            StartLaPresetScanCommand =
                new AsyncRelayCommand(
                    StartLaPresetScanAsync);

            UpdateLaPresetScanCommand =
                new RelayCommand(
                    UpdateLaPresetScan);

            StopLaPresetScanCommand =
                new RelayCommand(
                    StopLaPresetScan);

            StopLaPresetMoveCommand =
                new RelayCommand(
                    StopLaPresetMove);

            AddOrUpdatePresetCommand =
                new RelayCommand(
                    AddOrUpdatePresetPoint);

            DeletePresetCommand =
                new RelayCommand(
                    DeletePresetPoint);

            ClearAllPresetsCommand =
                new RelayCommand(
                    ClearAllPresetPoints);

            MoveToPresetCommand =
                new RelayCommand(
                    MoveToSelectedPresetPoint);

            StartPresetScanCommand =
                new RelayCommand(
                    StartPresetScan);

            UpdatePresetScanCommand =
                new RelayCommand(
                    UpdatePresetScan);

            StopPresetScanCommand =
                new RelayCommand(
                    StopPresetScan);

            StopPresetMoveCommand =
                new RelayCommand(
                    StopPresetMove);

            StopActivePresetScanCommand =
                new RelayCommand(
                    StopActivePresetScan);

            #endregion

            #region [LRF Command Binding]

            /// <summary>
            /// [LRF] 거리측정 [1회] 요청
            ///
            /// 버튼 클릭 시
            /// 거리측정기 [1회 측정] [Packet]을 송신한다.
            /// </summary>
            LrfMeasureCommand = new RelayCommand(() =>
            {
                Console.WriteLine();
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[CONTROL] LRF MEASURE REQUEST");
                ConsoleLogHelper.PrintLine();

                _controlCommandService.ReadOnceLrfValue();
            });

            #endregion

            #region [STOP Command Binding]

            /// <summary>
            /// [PT] 연속 이동 정지
            ///
            /// 현재 진행 중인
            /// [PAN] / [TILT] / [Zoom] / [Focus]
            /// 연속 이동을 정지한다.
            /// </summary>
            StopMoveCommand = new RelayCommand(() =>
            {
                Console.WriteLine();
                Console.WriteLine("[CONTROL] STOP MOVE");
                ConsoleLogHelper.PrintLine();

                StopContinuousMove();
            });

            #endregion

            #endregion

            #region [Service Initialize]

            /// <summary>
            /// EO / IR FFmpeg RTSP Decoder 생성
            /// </summary>
            _eoDecoder = new FFmpegDecoderService("EO");
            _irDecoder = new FFmpegDecoderService("IR");

            /// <summary>
            /// [CONTROL AGENT] 통신 서비스 생성
            /// </summary>
            _laTcpService = new TcpClientService();

            /// <summary>
            /// [TORUSS] 제어 명령 서비스 생성
            /// </summary>
            _controlCommandService = new ControlCommandService(_laTcpService);


            /// <summary>
            /// MCB 유지보수 직접 명령 서비스 생성
            /// </summary>
            _mcbMaintenanceCommandService =
                new McbMaintenanceCommandService();

            /// <summary>
            /// 환경장비 Web Agent Zoom Adapter 생성
            /// </summary>
            _webAgentZoomControlService =
                new WebAgentZoomControlService(
                    _controlCommandService);

            _webAgentThermalPaletteService =
                new WebAgentThermalPaletteService(
                    _controlCommandService);

            /// <summary>
            /// [옥상 GOP EO] CTEC CGI 직접 제어 서비스 생성
            /// </summary>
            _ctecCameraCommandService =
                new CtecCameraCommandService();

            /// <summary>
            /// [옥상 GOP EO] CTEC Port 9000 응답 수신 서비스 생성
            /// </summary>
            _ctecCameraResponseService =
                new CtecCameraResponseService();

            /// <summary>
            /// CTEC Camera Response Packet 수신 이벤트 연결
            /// </summary>
            _ctecCameraResponseService.PacketReceived +=
                OnCtecCameraResponsePacketReceived;

            /// <summary>
            /// CTEC Response TCP 연결 상태 이벤트 연결
            /// </summary>
            _ctecCameraResponseService.ConnectionStatusChanged +=
                OnCtecCameraResponseConnectionStatusChanged;

            /// <summary>
            /// [CONTROL AGENT] 수신 [Packet Parser] 생성
            /// </summary>
            _laPacketParser = new LAPacketParser();

            /// <summary>
            /// [CONTROL AGENT] [TCP] 수신 이벤트 연결
            ///
            /// [TcpClientService]의 [ReceiveLoop]에서 데이터 수신 시
            /// [OnLaMessageReceived] 함수가 호출된다.
            /// </summary>
            _laTcpService.MessageReceived += OnLaMessageReceived;

            /// <summary>
            /// [Control Agent] 서버가 연결을 종료한 경우
            /// 장비 연결 요청 상태가 유지되어 있으면 자동 재연결을 시작한다.
            /// </summary>
            _laTcpService.ConnectionClosed += OnControlAgentConnectionClosed;

            /// <summary>
            /// [AI Detector] 통신 서비스 생성
            /// </summary>
            _aiDetectorClientService = new AiDetectorClientService();

            /// <summary>
            /// [AI Detector] [Packet Parser] 생성
            /// </summary>
            _aiDetectorPacketParser = new AiDetectorPacketParser();

            /// <summary>
            /// [AI Detector Agent] 요청 [Packet] 생성
            /// </summary>
            _aiPacketBuilder = new AiDetectorPacketBuilder();

            /// <summary>
            /// [AI Detector] 수신 이벤트 연결
            ///
            /// [AiDetectorClientService]에서 완성 [Packet] 수신 시
            /// [OnAiDetectorPacketReceived] 함수가 호출된다.
            /// </summary>
            _aiDetectorClientService.PacketReceived += OnAiDetectorPacketReceived;

            #endregion

            #region [Default Source Initialize]

            /// <summary>
            /// 기본 영상 주소 초기화
            /// </summary>
            InitializeDefaultSourceAddress();

            /// <summary>
            /// Control Agent 통신 설정 기본값 초기화
            /// </summary>
            InitializeControlAgentSetting();

            /// <summary>
            /// AI Detector 설정 기본값 초기화
            /// </summary>
            InitializeAiDetectorSetting();
            InitializeThermalFeatures();
            InitializeSmokeFeatures();
            InitializeFireEventFeatures();

            ConsoleLogHelper.PrintSection(
                "[CONTROL AGENT]",
                "Service Initialize Complete");

            ConsoleLogHelper.StateSection(
                "MAIN",
                "Initialization complete",
                string.Empty,
                $"EO      : {ConsoleLogHelper.MaskRtspPassword(EoSourceAddress)}",
                $"IR      : {ConsoleLogHelper.MaskRtspPassword(IrSourceAddress)}",
                $"CONTROL : {ControlAgentIp}:{ControlAgentPortText}");

            #endregion

        }

        #endregion

        #region [Initialize]

        /// <summary>
        /// Control Agent 통신 설정 기본값 초기화
        ///
        /// 통신 설정 탭의 IP / Port 입력창에
        /// 프로그램 시작 시 표시할 기본값을 설정한다.
        /// </summary>
        private void InitializeControlAgentSetting()
        {
            // 1-1. 환경부 실장비 Control Agent(Web Agent) IP
            //ControlAgentIp =
            //    "192.168.20.161";

            // 1-2. 환경부 실장비 Control Agent(Web Agent) Port
            //ControlAgentPortText =
            //    "5005";

            // 2-1. 옥상 GOP 장비 Local Control Agent(LA) IP
            ControlAgentIp =
                "127.0.0.1";

            // 2-2. 옥상 GOP 장비 Control Agent(LA) Port
            ControlAgentPortText =
                "5001";

            // 2026-08-26: 옥상 GOP MCB 실장비 IP를 192.168.0.112로 변경한다.
            McbMaintenanceIpAddress =
                "192.168.0.112";

            ControlAgentConnectionStatusText =
                "Disconnected";

            ControlAgentConnectionStatusColor =
                "#FF6B6B";
        }

        /// <summary>
        /// 기본 EO / IR RTSP 선택값 초기화
        ///
        /// 통신 설정 탭에서 제공하는 카메라 프리셋:
        ///
        /// [3] 1층 생산팀 ADS 카메라
        /// - EO: 주간 카메라
        /// - IR: 열상 카메라
        ///
        /// [4] 옥상 GOP 카메라
        /// - EO: 주간 카메라
        /// - IR: 열상 카메라
        ///
        /// [5] 4층 환경부 PTZ 카메라
        /// - EO: 주간 PTZ 카메라
        /// - IR: 열상 PTZ 카메라
        ///
        /// 프로그램 시작 시에는 현재 개발에 사용하는
        /// [5] 환경부 EO / IR 카메라를 기본 선택한다.
        /// </summary>
        private void InitializeDefaultSourceAddress()
        {
            /*
             * 프로그램 시작 기본 선택값
             *
             * 기존 하드코딩 주소 중 현재 테스트에 사용 중인
             * 1. 주간(EO): 옥상 GOP 주간(EO) 카메라를 기본값으로 설정한다.
             * 2. 열상(IR): 옥상 GOP 열상(IR) 카메라를 기본값으로 설정한다.
             *
             * 이후에는 소스코드 주석을 변경하지 않고
             * 통신 설정 탭의 EO / IR ComboBox에서 개별 선택한다.
             */
            EoSourceAddress =
                GopEoRtspAddress;

            IrSourceAddress =
                GopIrRtspAddress;
        }

        /// <summary>
        /// [AI Detector] 설정 기본값 초기화
        ///
        /// Viewer에서 사용하는 [EO] / [IR] 주소를
        /// [AI Detector Agent]의 [RTSP 0] / [RTSP 1] 기본값으로 복사한다.
        /// </summary>
        private void InitializeAiDetectorSetting()
        {
            AiRtsp0Address = EoSourceAddress;
            AiRtsp1Address = IrSourceAddress;

            AiRtsp0OnnxIndex = 1;
            AiRtsp1OnnxIndex = 1;

            // 2026-09-02: Agent 추론 하한은 0.15로 유지해 16% 수준의 실제 송신
            // 후보를 보존하고, NMS IOU는 0.50으로 완화해 인접 플룸을 과도하게
            // 제거하지 않는다. Viewer 표시 하한은 0.12로 두어 Agent와 UI 차이를 줄인다.
            AiMappingConfidence = 0.15;
            AiMappingIou = 0.50;

            AiDisplayConfidenceThreshold = 0.12;
        }

        #endregion

        #region [INotifyPropertyChanged]

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

}
