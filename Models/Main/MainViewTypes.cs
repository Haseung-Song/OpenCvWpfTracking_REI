namespace OpenCvWpfTracking.Models.Main
{
    /// <summary>
    /// 카메라 제어 명령 송신 경로
    /// </summary>
    public enum CameraControlType
    {
        /// <summary>
        /// 기존 Control Agent TCP를 통한 장비 제어
        /// </summary>
        ControlAgent,

        /// <summary>
        /// XV-Z4850HC CTEC CGI를 통한 카메라 직접 제어
        /// </summary>
        CtecCgi
    }

    /// <summary>
    /// CURRENT STATUS 화면에 표시할 장비 구성
    /// </summary>
    public enum EquipmentStatusMode
    {
        /// <summary>
        /// 옥상 시험장비
        /// 한국씨텍 EO 직접 제어 + IR 제어 구성
        /// </summary>
        Rooftop,

        /// <summary>
        /// 환경장비
        /// Web Agent 기준 EO / IR Zoom 0 ~ 1000 구성
        /// </summary>
        Environment
    }

    /// <summary>
    /// [Pan Absolute] 이동 시 사용할 선회 방향 계산 모드
    ///
    /// VertiportNexus의 PAN TURN MODE 동작 기준을
    /// 현재 Control Agent 제어 구조에 맞춰 적용한다.
    /// </summary>
    public enum PanTurnMode
    {
        /// <summary>
        /// 현재 위치에서 목표 위치까지
        /// 0도 방향을 경유하는 정방향 회전을 수행한다.
        /// </summary>
        ViaZero,

        /// <summary>
        /// 현재 위치에서 목표 위치까지
        /// 가장 짧은 회전 방향을 계산한다.
        /// </summary>
        Short
    }

    /// <summary>
    /// [EO / IR Zoom Synchronization]
    ///
    /// 환경장비 Web Agent 기준 Zoom Position
    /// 0 ~ 1000 범위를 100 단위로 구분한 선택 항목
    ///
    /// 구성:
    /// LEVEL 0  = 0
    /// LEVEL 1  = 100
    /// LEVEL 2  = 200
    /// ...
    /// LEVEL 10 = 1000
    ///
    /// 총 11개 Position을 사용한다.
    /// </summary>
    public sealed class ZoomSyncLevelOption
    {
        public int Level { get; }

        public short Position { get; }

        public string DisplayText =>
            $"LEVEL {Level}  /  {Position}";

        public ZoomSyncLevelOption(
            int level,
            short position)
        {
            Level = level;
            Position = position;
        }

    }

    /// <summary>
    /// PRESET 탭에서 표시하는 프리셋 슬롯 정보
    ///
    /// TORUSS 규격에는 프리셋 목록 조회 응답이 별도로 없으므로,
    /// 이 객체는 현재 프로그램에서 ADD 명령을 송신한 시점의
    /// PTZF 상태를 화면 확인용으로 보관한다.
    ///
    /// 실제 프리셋 저장 / 이동 주체는 Control Agent(Local Agent)이다.
    /// </summary>
    public sealed class PresetPointOption
    {
        /// <summary>
        /// TORUSS 프리셋 슬롯 번호
        ///
        /// 프리셋 추가 / 제거 / 이동 명령 기준:
        /// 1 ~ 63
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// 등록 명령 송신 시점의 Pan 상태값
        /// </summary>
        public double Pan { get; }

        /// <summary>
        /// 등록 명령 송신 시점의 Tilt 상태값
        /// </summary>
        public double Tilt { get; }

        /// <summary>
        /// 등록 명령 송신 시점의 EO Zoom 표시값
        /// </summary>
        public string EoZoomText { get; }

        /// <summary>
        /// 등록 명령 송신 시점의 EO Focus 표시값
        /// </summary>
        public string EoFocusText { get; }

        /// <summary>
        /// 등록 명령 송신 시점의 IR Zoom 표시값
        /// </summary>
        public string IrZoomText { get; }

        /// <summary>
        /// 등록 명령 송신 시점의 IR Focus 표시값
        /// </summary>
        public string IrFocusText { get; }

        /// <summary>
        /// ComboBox 한 줄 표시 문자열
        /// </summary>
        public string DisplayText =>
            $"P{Number:00}  |  PAN {Pan:F2}°  |  TILT {Tilt:F2}°";

        /// <summary>
        /// 선택된 프리셋 상세 표시 문자열
        /// </summary>
        public string DetailText =>
            $"PAN      : {Pan:F2}°\n" +
            $"TILT     : {Tilt:F2}°\n" +
            $"EO ZOOM  : {EoZoomText} / 1000\n" +
            $"EO FOCUS : {EoFocusText} / 1000\n" +
            $"IR ZOOM  : {IrZoomText} / 1000\n" +
            $"IR FOCUS : {IrFocusText} / 1000";

        public PresetPointOption(
            int number,
            double pan,
            double tilt,
            string eoZoomText,
            string eoFocusText,
            string irZoomText,
            string irFocusText)
        {
            Number =
                number;

            Pan =
                pan;

            Tilt =
                tilt;

            EoZoomText =
                eoZoomText ?? "-";

            EoFocusText =
                eoFocusText ?? "-";

            IrZoomText =
                irZoomText ?? "-";

            IrFocusText =
                irFocusText ?? "-";
        }

    }

    /// <summary>
    /// 통신 설정 화면의 RTSP 선택 ComboBox 항목
    ///
    /// DisplayName       : UI에 표시할 카메라 구분명
    /// Address           : 실제 FFmpeg 연결에 사용할 RTSP 주소
    /// ControlType       : Zoom / Focus 명령 송신 경로
    /// ControlIp         : CTEC CGI 직접 제어 대상 IP
    /// ControlUserName   : 카메라 CGI 인증 계정
    /// ControlPassword   : 카메라 CGI 인증 암호
    /// UseHttps          : CGI HTTPS 사용 여부
    /// </summary>
    public sealed class RtspSourceOption
    {
        /// <summary>
        /// RTSP 카메라 선택 항목 표시명
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// RTSP 카메라 실제 연결 주소
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// 카메라 Zoom / Focus 제어 명령 송신 경로
        /// </summary>
        public CameraControlType ControlType { get; }

        /// <summary>
        /// CTEC CGI 직접 제어 대상 카메라 IP
        /// </summary>
        public string ControlIp { get; }

        /// <summary>
        /// CTEC CGI 인증 계정
        /// </summary>
        public string ControlUserName { get; }

        /// <summary>
        /// CTEC CGI 인증 암호
        /// </summary>
        public string ControlPassword { get; }

        /// <summary>
        /// CTEC CGI HTTPS 사용 여부
        /// </summary>
        public bool UseHttps { get; }

        /// <summary>
        /// 사전 등록되지 않은 RTSP 주소를 사용자가 직접 입력하는 항목인지 여부
        /// </summary>
        public bool IsDirectInput { get; }

        /// <summary>
        /// RTSP 선택 항목 생성
        ///
        /// 별도 직접 제어 정보가 없으면
        /// 기존 Control Agent 제어 방식으로 처리한다.
        /// </summary>
        public RtspSourceOption(
            string displayName,
            string address,
            CameraControlType controlType =
                CameraControlType.ControlAgent,
            string controlIp = null,
            string controlUserName = null,
            string controlPassword = null,
            bool useHttps = false,
            bool isDirectInput = false)
        {
            DisplayName =
                displayName;

            Address =
                address;

            ControlType =
                controlType;

            ControlIp =
                controlIp;

            ControlUserName =
                controlUserName;

            ControlPassword =
                controlPassword;

            UseHttps =
                useHttps;

            IsDirectInput =
                isDirectInput;
        }

    }

}
