using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 키보드 및 버튼 기반 Pan/Tilt/Zoom/Focus 연속 제어와 공통 Stop을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {

        /// <summary>
        /// Keyboard 방향키 KeyDown 처리
        ///
        /// 방향키 눌림 상태를 저장한 뒤,
        /// 현재 눌린 전체 방향키 조합에 따라
        /// 단일 방향 또는 대각선 이동 명령을 송신한다.
        /// </summary>
        public void HandlePanTiltKeyDown(Key key)
        {
            _continuousControlFeature.HandlePanTiltKeyDown(key);
        }

        /// <summary>
        /// Keyboard 방향키 KeyUp 처리
        ///
        /// 해제된 방향키 상태를 제거한 뒤,
        /// 아직 누르고 있는 나머지 방향키 기준으로
        /// 이동 방향을 다시 계산한다.
        /// </summary>
        public void HandlePanTiltKeyUp(Key key)
        {
            _continuousControlFeature.HandlePanTiltKeyUp(key);
        }

        /// <summary>
        /// 장비 제어용 키보드 상태 전체 초기화
        ///
        /// HOME POSITION 시작/완료/실패/Timeout 시 공통으로 호출한다.
        ///
        /// 현재 구현:
        /// - 방향키 Pan/Tilt 상태 초기화
        /// - WASD Pan/Tilt 상태 초기화
        ///
        /// 방향키와 WASD는 동일한 Pan/Tilt 상태 필드를 사용하므로
        /// ResetKeyboardPanTiltState 호출 한 번으로 함께 초기화된다.
        ///
        /// Zoom/Focus 단축키는 현재 별도 눌림 상태를 보관하지 않고
        /// Window PreviewKeyDown/PreviewKeyUp 입구에서 HOME Lock 중 전체 차단한다.
        /// 추후 Zoom/Focus 키 상태 필드가 추가되면 반드시 이 함수에서 같이 초기화한다.
        /// </summary>
        public void ResetAllKeyboardControlState()
        {
            _continuousControlFeature.ResetAllKeyboardControlState();
        }

        /// <summary>
        /// Keyboard Pan / Tilt 상태 초기화
        ///
        /// Window Focus 이탈로 KeyUp 이벤트가 누락될 경우
        /// 모든 방향키 상태를 초기화하고
        /// 현재 키보드 Pan / Tilt 이동을 정지한다.
        /// </summary>
        public void ResetKeyboardPanTiltState()
        {
            _continuousControlFeature.ResetKeyboardPanTiltState();
        }

        /// <summary>
        /// 2026-10-01: 메인 화면의 활성 EO/IR 영상 변경을 PT 감쇄 기준에 반영한다.
        /// 신규 WebAgent 프로토콜 없이 기존 PT 이동 패킷의 속도값만 조정한다.
        /// </summary>
        public void SetActivePanTiltZoomSource(bool isInfrared)
        {
            _continuousControlFeature.SetActivePanTiltZoomSource(isInfrared);
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 좌측 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 좌측 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartPanLeftMove()
        {
            _continuousControlFeature.StartPanLeftMove();
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [PAN] 우측 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 우측 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartPanRightMove()
        {
            _continuousControlFeature.StartPanRightMove();
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 위쪽 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 위쪽 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartTiltUpMove()
        {
            _continuousControlFeature.StartTiltUpMove();
        }

        /// <summary>
        /// [EO/IR] 주간/열상 카메라 [TILT] 아래쪽 연속 이동 시작
        ///
        /// [PanTiltSpeedLevel] 값을 사용하여
        /// 아래 방향으로 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartTiltDownMove()
        {
            _continuousControlFeature.StartTiltDownMove();
        }

        /// <summary>
        /// [EO/IR] 좌측 상단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltUpMove()
        {
            _continuousControlFeature.StartPanLeftTiltUpMove();
        }

        /// <summary>
        /// [EO/IR] 우측 상단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltUpMove()
        {
            _continuousControlFeature.StartPanRightTiltUpMove();
        }

        /// <summary>
        /// [EO/IR] 좌측 하단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltDownMove()
        {
            _continuousControlFeature.StartPanLeftTiltDownMove();
        }

        /// <summary>
        /// [EO/IR] 우측 하단 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltDownMove()
        {
            _continuousControlFeature.StartPanRightTiltDownMove();
        }

        /// <summary>
        /// [EO] 주간 카메라 [ZOOM] [Tele] 연속 이동 시작
        ///
        /// 옥상 MR300 EO 카메라 선택 시:
        /// - XV-Z4850HC CTEC CGI 직접 제어
        ///
        /// 그 외 EO 카메라 선택 시:
        /// - 기존 Control Agent 제어 유지
        /// </summary>
        public void StartEoZoomInMove()
        {
            _continuousControlFeature.StartEoZoomInMove();
        }

        /// <summary>
        /// [EO] 주간 카메라 [ZOOM] [Wide] 연속 이동 시작
        ///
        /// 선택된 EO 프리셋에 따라
        /// CTEC CGI 직접 제어 또는 Control Agent 제어로 분기한다.
        /// </summary>
        public void StartEoZoomOutMove()
        {
            _continuousControlFeature.StartEoZoomOutMove();
        }

        /// <summary>
        /// [EO] 주간 카메라 Focus Near 연속 이동 시작
        ///
        /// 옥상 MR300 EO 선택 시:
        /// Focus Manual -> Focus Near 순서로 CTEC CGI 직접 송신한다.
        ///
        /// 그 외 EO 선택 시:
        /// 기존 Control Agent Focus Near 명령을 유지한다.
        /// </summary>
        public void StartEoFocusNearMove()
        {
            _continuousControlFeature.StartEoFocusNearMove();
        }

        /// <summary>
        /// [EO] 주간 카메라 Focus Far 연속 이동 시작
        ///
        /// 옥상 MR300 EO 선택 시:
        /// Focus Manual -> Focus Far 순서로 CTEC CGI 직접 송신한다.
        ///
        /// 그 외 EO 선택 시:
        /// 기존 Control Agent Focus Far 명령을 유지한다.
        /// </summary>
        public void StartEoFocusFarMove()
        {
            _continuousControlFeature.StartEoFocusFarMove();
        }

        /// <summary>
        /// [EO] 주간 카메라 [One Push Focus] 요청
        ///
        /// 옥상 MR300 EO 선택 시 CTEC CGI 직접 제어,
        /// 그 외 EO 선택 시 기존 Control Agent 명령을 사용한다.
        /// </summary>
        public void StartEoAutoFocusMove()
        {
            _continuousControlFeature.StartEoAutoFocusMove();
        }

        /// <summary>
        /// CTEC Response TCP 연결 상태 변경 처리
        ///
        /// Connected 상태가 되면 현재 Zoom / Focus Position 및
        /// Focus Mode Inquiry를 순차 송신하여 초기 상태값을 조회한다.
        /// </summary>
        private void OnCtecCameraResponseConnectionStatusChanged(string status)
        {
            _continuousControlFeature.OnCtecCameraResponseConnectionStatusChanged(status);
        }

        /// <summary>
        /// CTEC Camera Response Packet 수신 처리
        ///
        /// 공통 Header:
        /// 0x99 0x55
        ///
        /// 명령 코드:
        /// 0x47 = Zoom Position
        /// 0x48 = Focus Position
        /// 0x38 = Focus Mode
        /// </summary>
        private void OnCtecCameraResponsePacketReceived(byte[] packet)
        {
            _continuousControlFeature.OnCtecCameraResponsePacketReceived(packet);
        }

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Tele] 연속 이동 시작
        /// </summary>
        public void StartIrZoomInMove()
        {
            _continuousControlFeature.StartIrZoomInMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [ZOOM] [Wide] 연속 이동 시작
        /// </summary>
        public void StartIrZoomOutMove()
        {
            _continuousControlFeature.StartIrZoomOutMove();
        }

        /// <summary>
        /// [IR] [ZOOM] 연속 이동 정지
        ///
        /// IR Zoom 버튼 [MouseUp] 시에만 호출한다.
        /// </summary>
        public void StopIrZoomMove()
        {
            _continuousControlFeature.StopIrZoomMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Near] 연속 이동 시작
        /// </summary>
        public void StartIrFocusNearMove()
        {
            _continuousControlFeature.StartIrFocusNearMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] [Far] 연속 이동 시작
        /// </summary>
        public void StartIrFocusFarMove()
        {
            _continuousControlFeature.StartIrFocusFarMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [FOCUS] 연속 이동 정지
        /// </summary>
        public void StopIrFocusMove()
        {
            _continuousControlFeature.StopIrFocusMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Digital Zoom] 확대 시작
        /// </summary>
        public void StartIrDigitalZoomInMove()
        {
            _continuousControlFeature.StartIrDigitalZoomInMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Digital Zoom] 축소 시작
        /// </summary>
        public void StartIrDigitalZoomOutMove()
        {
            _continuousControlFeature.StartIrDigitalZoomOutMove();
        }

        /// <summary>
        /// [IR] 열상 카메라 [Auto Focus] 요청
        /// </summary>
        public void StartIrAutoFocusMove()
        {
            _continuousControlFeature.StartIrAutoFocusMove();
        }

        public void StopPanTiltContinuousMove()
        {
            _continuousControlFeature.StopPanTiltContinuousMove();
        }

        /// <summary>
        /// 2026-09-15: 화면 중앙 STOP 버튼 전용 PTZF 전체 정지.
        /// 현재 GUI 이동 상태와 관계없이 Pelco-D 전체 STOP
        /// FF 01 00 00 00 00 01을 반드시 1회 송신한다.
        /// 옥상 CTEC EO 렌즈가 직접 제어 중이면 해당 렌즈도 함께 정지한다.
        /// </summary>
        public void StopAllPtzfMove()
        {
            _continuousControlFeature.StopAllPtzfMove();
        }

        /// <summary>
        /// 연속 이동 정지
        ///
        /// 버튼 [MouseUp] 또는 [MouseLeave] 시 호출된다.
        ///
        /// 옥상 MR300 EO Zoom / Focus는 CTEC CGI 전용 Stop 명령을 송신하고,
        /// 그 외 제어는 기존 Control Agent Stop 명령을 유지한다.
        /// </summary>
        public void StopContinuousMove()
        {
            _continuousControlFeature.StopContinuousMove();
        }

    }
}
