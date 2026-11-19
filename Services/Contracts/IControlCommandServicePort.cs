using System;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IControlCommandServicePort
    {
        bool UseUnsignedWebAgentPanCoordinates { get; set; }

        bool UseUnsignedWebAgentTiltCoordinates { get; set; }

        /// <summary>
        /// 정밀 이동은 속도와 위치를 같은 프레임으로 묶으며 기존 속도 설정을 변경하지 않는다.
        /// </summary>
        bool SendPrecisionAbsolute(bool tilt, double guiAngle, double speed, double maximum);

        /// <summary>
        /// 정밀 조그는 방향을 명령 첫째 바이트에 넣는다. 영 방향은 양 축 정지이다.
        /// </summary>
        bool SendPrecisionJog(byte direction, double panSpeed, double tiltSpeed, double maximum);

        /// <summary>
        /// 자동 초점 조회·설정·한 번 실행을 구분하여 전송한다.
        /// </summary>
        bool SendAutoFocusCommand(byte command, byte target, byte value);

        /// <summary>
        /// [TORUSS] 제어 [Packet] 생성 및 송신
        /// </summary>
        bool SendCommand(byte cmd1, byte cmd2, byte data1, byte data2);

        /// <summary>
        /// v2.9 연결별 분리 텔레메트리 전송률을 설정한다. Category 0=PT, 1=EO Lens, 2=IR Lens이며 0Hz는 자동 발신
        /// 중지다.
        /// </summary>
        bool SetWebAgentTelemetryRate(byte category, byte frequencyHz);

        /// <summary>
        /// v2.9 Category별 현재 GUI 전달 설정을 조회한다. Category 0=PT, 1=EO Lens, 2=IR Lens,
        /// 3=Power다.
        /// </summary>
        bool RequestWebAgentTelemetrySetting(byte category);

        /// <summary>
        /// v2.9 Category별 캐시를 단건 조회한다. 자동 발신이 0Hz인 경우에도 사용할 수 있다.
        /// </summary>
        bool RequestWebAgentTelemetrySnapshot(byte category);

        /// <summary>
        /// 2026-10-02: WebAgent에 최신 GPS 상태를 단건 조회한다. Packet: FF 01 00 A3 00 00 A4
        /// </summary>
        bool RequestGpsTelemetry();

        /// <summary>
        /// 2026-10-02: 모든 GUI 연결에 공통 적용되는 GPS 자동 전송률을 설정한다. 0Hz는 자동 전송 중지이며 단건 조회는 계속 사용할
        /// 수 있다.
        /// </summary>
        bool SetGpsAutoTxRate(byte frequencyHz);

        /// <summary>
        /// 2026-10-02: 현재 GPS 자동 전송률을 조회한다. Packet: FF 01 00 F9 00 00 FA
        /// </summary>
        bool RequestGpsAutoTxRate();

        /// <summary>
        /// 2026-10-06: WebAgent를 통해 EO/IR 장비 전원을 제어한다. 요청 성공만으로 화면 상태를 바꾸지 않으며, 실제 ON/OFF
        /// 표시는 Function 0x31의 MCB/SCB 보드 상태 비트를 기준으로 갱신한다.
        /// </summary>
        bool SetDevicePower(byte deviceCode, bool turnOn);

        /// <summary>
        /// 2026-10-01: WebAgent Build 86 이상에서 RPY 자동 전송 주파수를 설정한다. 0Hz는 자동 전송 중지이며 허용 범위는
        /// 0~30Hz이다.
        /// </summary>
        bool SetRpyAutoTxRate(byte frequencyHz);

        /// <summary>
        /// 2026-10-01: Roll/Pitch/Yaw 오프셋을 하나의 11Byte 요청으로 전송한다. Roll/Pitch는 signed
        /// int16, Yaw는 unsigned uint16이며 모두 0.01도 단위, Little Endian 순서로 배치한다.
        /// </summary>
        bool SetRpyOffsets(short rollHundredths, short pitchHundredths, ushort yawHundredths);

        /// <summary>
        /// 2026-10-01: WebAgent Build 93 이상에서 DB에 저장된 Roll/Pitch/Yaw Offset을 직접 조회한다.
        /// Packet: FF 01 00 F3 00 00 F4
        /// </summary>
        bool RequestStoredRpyOffsets();

        /// <summary>
        /// 2026-10-01: WebAgent Build 93 이상에서 DB에 저장된 RPY 자동 전송 주파수를 직접 조회한다. Packet: FF
        /// 01 00 F5 00 00 F6
        /// </summary>
        bool RequestStoredRpyAutoTxRate();

        /// <summary>
        /// 2026-10-01: WebAgent 현재 UTC 날짜·시간을 조회한다. Packet: FF 01 00 F1 00 00 F2
        /// </summary>
        bool RequestSystemDateTime();

        /// <summary>
        /// 2026-10-01: PC 현재 시간을 UTC로 변환하여 WebAgent OS 시간으로 설정한다. 시간대는 변경하지 않으며 초 단위까지만
        /// 전송한다.
        /// </summary>
        bool SetSystemDateTimeUtc(DateTime utcDateTime);

        /// <summary>
        /// WEB AGENT의 장비 식별·버전 정보를 조회한다.
        /// </summary>
        bool RequestWebAgentIdentity();

        /// <summary>
        /// 대상 렌즈의 지원 기능과 현재 상태 조회를 함께 요청한다.
        /// </summary>
        bool RequestLensCapabilityAndTelemetry(byte target);

        /// <summary>
        /// IMU 자세 상태를 조회하는 명령을 송신한다.
        /// </summary>
        bool RequestImuTelemetry();

        /// <summary>
        /// 대상 카메라의 특수 기능 지원 정보를 조회한다.
        /// </summary>
        bool RequestCameraFeatureCapability(byte target);

        /// <summary>
        /// 지정한 카메라 특수 기능의 현재 설정값을 조회한다.
        /// </summary>
        bool RequestCameraFeatureState(byte target, byte featureId);

        /// <summary>
        /// 대상 카메라 특수 기능에 지정한 값을 설정한다.
        /// </summary>
        bool SetCameraFeature(byte featureId, byte target, byte value);

        /// <summary>
        /// WebAgent에 현재 Encoder 위치를 0으로 재정의하도록 요청한다. Cmd2 0xB3은 2026-09-29 제안 규격이며
        /// WebAgent 구현 확정 전까지 송신 성공은 TCP 전달 성공만 의미한다. 실제 장비 ZERO 완료 여부는 향후 WebAgent 성공/실패
        /// 응답 규격으로 별도 판정해야 한다.
        /// </summary>
        bool RequestWebAgentPositionZero(byte axis);

        /// <summary>
        /// 2026-09-30: TILT Set Zero 이후 변경된 실제 동작 범위를 WebAgent에 조회한다. Packet: FF 01 00 B5
        /// 02 00 B8 Cmd2 : 0xB5 (TILT 제한값 조회) Data1 : 0x02 (TILT 축)
        /// </summary>
        bool RequestWebAgentTiltLimits();

        /// <summary>
        /// [Pan] 이동 시 원점 통과 모드 설정 TORUSS / Pelco-D 확장 명령: Command2 = 0x4D Data1 = 0x01
        /// Data2 = 0x00 이후 Pan 위치 이동 명령에서 장비가 원점 통과 경로를 선택한다.
        /// </summary>
        bool SetPanViaZeroMode();

        /// <summary>
        /// [Pan] 이동 시 최단거리 선택 모드 설정 TORUSS / Pelco-D 확장 명령: Command2 = 0x4D Data1 = 0x02
        /// Data2 = 0x00 이후 Pan 위치 이동 명령에서 장비가 최단 회전 경로를 선택한다.
        /// </summary>
        bool SetPanShortestPathMode();

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 ID를 설정한다. Command2 = 0x19 Data1/Data2 = ID (0 ~ 99, Big
        /// Endian)
        /// </summary>
        bool SetLaPresetId(ushort presetId);

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Pan 위치를 설정한다. Command2 = 0x91 Data1/Data2 = Degree * 100
        /// (signed short, Big Endian)
        /// </summary>
        bool SetLaPresetPan(double pan);

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Tilt 위치를 설정한다. Command2 = 0x93 Data1/Data2 = Degree * 100
        /// (signed short, Big Endian)
        /// </summary>
        bool SetLaPresetTilt(double tilt);

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Zoom 위치를 설정한다. Command2 = 0x95 Data1/Data2 = Position (0 ~
        /// 1000, Big Endian)
        /// </summary>
        bool SetLaPresetZoom(ushort position);

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Focus 위치를 설정하고, LA 내부 scan-&gt;AddPreset()을 완료한다. Command2 =
        /// 0x97 Data1/Data2 = Position (0 ~ 1000, Big Endian)
        /// </summary>
        bool SetLaPresetFocusAndCommit(ushort position);

        /// <summary>
        /// LA 실제 구현 기준 전체 스캔 프리셋 데이터를 초기화한다. Command2 = 0x9B Data1 = 0x00 Data2 = 0x01
        /// </summary>
        bool ClearAllLaPresets();

        /// <summary>
        /// 현재 PTZF 위치를 프리셋 슬롯에 추가 / 갱신한다. TORUSS 프리셋 실행 / 편집 명령: Command2 = 0x03 Data1 =
        /// 0x00 Data2 = Preset Number 유효 슬롯: 1 ~ 100
        /// </summary>
        bool AddPresetPoint(byte presetNumber);

        /// <summary>
        /// 프리셋 슬롯을 제거한다. TORUSS 프리셋 실행 / 편집 명령: Command2 = 0x05 Data1 = 0x00 Data2 =
        /// Preset Number
        /// </summary>
        bool RemovePresetPoint(byte presetNumber);

        /// <summary>
        /// 저장된 프리셋 슬롯으로 이동한다. TORUSS 프리셋 실행 / 편집 명령: Command2 = 0x07 Data1 = 0x00 Data2 =
        /// Preset Number
        /// </summary>
        bool MoveToPresetPoint(byte presetNumber);

        /// <summary>
        /// 프리셋 오토 스캔을 정지한다. TORUSS 프리셋 설정 / 오토 스캔 명령: Command2 = 0x9B Data1 = 0x00 Data2
        /// = 0x00 Data2 = 0x01은 프리셋 데이터 초기화이므로 일반 정지 함수에서는 사용하지 않는다.
        /// </summary>
        bool StopPresetScan();

        /// <summary>
        /// 사용자 정의 Pan 위치 이동 속도를 설정한다. Command2 = 0x49 Speed = deg/s * 100 (unsigned
        /// short, Big Endian)
        /// </summary>
        bool SetPanPositionSpeed(double speedDegreesPerSecond);

        /// <summary>
        /// 사용자 정의 Tilt 위치 이동 속도를 설정한다. Command2 = 0x4B Speed = deg/s * 100 (unsigned
        /// short, Big Endian)
        /// </summary>
        bool SetTiltPositionSpeed(double speedDegreesPerSecond);

        /// <summary>
        /// [Pan] 위치 제어 명령 입력 기준은 -180 ~ 180도이다. 범위를 벗어난 내부 계산값은 signed Pan 범위로 정규화한 뒤, 위치
        /// 값을 [각도 * 100]하여 [Data1 / Data2]에 [Big Endian] 방식으로 설정한다.
        /// </summary>
        bool PanGoPosition(double pan);

        /// <summary>
        /// [PAN] 우측 연속 이동 시작 [Command2 Bit0 = Pan Right] [Data1 = Pan Speed Level] [0 ~
        /// 63]
        /// </summary>
        bool StartPanRight(byte speed = 20);

        /// <summary>
        /// [PAN] 좌측 연속 이동 시작 [Command2 Bit1 = Pan Left] [Data1 = Pan Speed Level] [0 ~
        /// 63]
        /// </summary>
        bool StartPanLeft(byte speed = 20);

        /// <summary>
        /// [Tilt] 위치 제어 명령
        /// </summary>
        bool TiltGoPosition(double tilt);

        /// <summary>
        /// [TILT] 위쪽 연속 이동 시작 [Command2 Bit2 = Tilt Up] [Data2 = Tilt Speed Level] [0 ~
        /// 63]
        /// </summary>
        bool StartTiltUp(byte speed = 20);

        /// <summary>
        /// [TILT] 아래쪽 연속 이동 시작 [Command2 Bit3 = Tilt Down] [Data2 = Tilt Speed Level] [0
        /// ~ 63]
        /// </summary>
        bool StartTiltDown(byte speed = 20);

        /// <summary>
        /// [PAN LEFT + TILT UP] 좌측 상단 대각선 연속 이동 시작 [Command2] Pan Left : 0x04 Tilt Up :
        /// 0x08 결합값 : 0x0C [Data1] = Pan Speed [Data2] = Tilt Speed
        /// </summary>
        bool StartPanLeftTiltUp(byte panSpeed = 20, byte tiltSpeed = 20);

        /// <summary>
        /// [PAN RIGHT + TILT UP] 우측 상단 대각선 연속 이동 시작 [Command2] Pan Right : 0x02 Tilt Up :
        /// 0x08 결합값 : 0x0A [Data1] = Pan Speed [Data2] = Tilt Speed
        /// </summary>
        bool StartPanRightTiltUp(byte panSpeed = 20, byte tiltSpeed = 20);

        /// <summary>
        /// [PAN LEFT + TILT DOWN] 좌측 하단 대각선 연속 이동 시작 [Command2] Pan Left : 0x04 Tilt Down
        /// : 0x10 결합값 : 0x14 [Data1] = Pan Speed [Data2] = Tilt Speed
        /// </summary>
        bool StartPanLeftTiltDown(byte panSpeed = 20, byte tiltSpeed = 20);

        /// <summary>
        /// [PAN RIGHT + TILT DOWN] 우측 하단 대각선 연속 이동 시작 [Command2] Pan Right : 0x02 Tilt
        /// Down : 0x10 결합값 : 0x12 [Data1] = Pan Speed [Data2] = Tilt Speed
        /// </summary>
        bool StartPanRightTiltDown(byte panSpeed = 20, byte tiltSpeed = 20);

        /// <summary>
        /// Pan 위치 이동을 정지한다. TORUSS 위치 제어 명령: Command2 = 0x4F Data1 = 0x01 (Pan Position
        /// Stop) Data2 = 0x00
        /// </summary>
        bool StopPanPositionMove();

        /// <summary>
        /// Tilt 위치 이동을 정지한다. TORUSS 위치 제어 명령: Command2 = 0x4F Data1 = 0x02 (Tilt Position
        /// Stop) Data2 = 0x00
        /// </summary>
        bool StopTiltPositionMove();

        /// <summary>
        /// Pan과 Tilt 위치 이동 정지 명령을 순서대로 송신한다. PRESET GOTO 또는 ABSOLUTE 위치 이동 중지에 사용한다.
        /// </summary>
        bool StopPanTiltPositionMove();

        /// <summary>
        /// 전체 연속 속도제어 정지
        /// </summary>
        bool StopMove();

        /// <summary>
        /// PTZ(회전형) 카메라 [Zoom] 위치 제어 명령 범위: [0 ~ 1000]
        /// </summary>
        bool EoZoomGoPosition(short zoom);

        /// <summary>
        /// [EO] [ZOOM] [Tele] 연속제어 시작 [Command2 Bit5 = Zoom Tele]
        /// </summary>
        bool StartEoZoomTele();

        /// <summary>
        /// [EO] [ZOOM] [Wide] 연속제어 시작 [Command2 Bit6 = Zoom Wide]
        /// </summary>
        bool StartEoZoomWide();

        /// <summary>
        /// 2026-09-17: Web Agent EO Zoom 연속제어 전용 정지. v1.8 추가 프로토콜의 축별 STOP을 사용하여 EO Zoom
        /// 정지가 이후 PAN/TILT 제어 상태까지 초기화하지 않도록 한다. Packet: FF 01 00 51 01 00 53
        /// </summary>
        bool StopEoZoom();

        /// <summary>
        /// [EO] PTZ(회전형) 카메라 [Focus] 위치 제어 명령 범위: [0 ~ 1000]
        /// </summary>
        bool EoFocusGoPosition(short focus);

        /// <summary>
        /// [EO] [FOCUS] [Near] 연속제어 시작 [Command2 Bit0 = Focus Near]
        /// </summary>
        bool StartEoFocusNear();

        /// <summary>
        /// [EO] [FOCUS] [Far] 연속제어 시작 [Command1 Bit7 = Focus Far]
        /// </summary>
        bool StartEoFocusFar();

        /// <summary>
        /// 2026-09-17: Web Agent EO Focus 연속제어 전용 정지. v1.8 추가 프로토콜의 축별 STOP을 사용하여 EO
        /// Focus 정지가 이후 PAN/TILT 제어 상태까지 초기화하지 않도록 한다. Packet: FF 01 00 51 02 00 54
        /// </summary>
        bool StopEoFocus();

        /// <summary>
        /// [EO] 주간 카메라 [One Push Auto Focus] 요청 Toruss V1 EO/IR 연동규격서 rev15의 주간 카메라 제어 확장
        /// 명령을 동일한 제어 TCP 연결로 송신한다. [Command1 = 0x00] [Command2 = 0x35] [Data1 = 0xAF]
        /// [Data2 = 0x00] [Packet = FF 01 00 35 AF 00 E5]
        /// </summary>
        bool StartEoAutoFocus();

        /// <summary>
        /// LA 규격 2.6의 열영상 줌 위치 이동 명령이다. 저장된 제어 위치를 상위 바이트부터 송신한다. 반전된 수신 좌표를 송신값으로 재사용하지
        /// 않는다. WEB 0xB7과 구분하고 실제 응답으로 도착을 확인한다.
        /// </summary>
        bool LaIrZoomGoPosition(int standardPosition);

        /// <summary>
        /// 2026-10-16: WebAgent IR 줌 절대 위치를 CMD2=0xB7로 요청한다. 위치 0~1000을 상위/하위 바이트 순서로
        /// 송신하며 화각×100 값이 아니다. 범위를 벗어난 값은 보정해 송신하지 않고 거절한다. 송신 성공은 장비 도달 확인과 다르며 기존 렌즈
        /// 피드백으로 확인한다.
        /// </summary>
        bool IrZoomGoPosition(short zoom);

        /// <summary>
        /// [IR] 열영상 카메라 [ZOOM] [Tele] 연속제어 시작 [Command2 = 0x31] [Data1 = 0x00] : Zoom In
        /// Start [Data2 = 0x00]
        /// </summary>
        bool StartIrZoomTele();

        /// <summary>
        /// [IR] 열영상 카메라 [ZOOM] [Wide] 연속제어 시작 [Command2 = 0x31] [Data1 = 0x01] : Zoom Out
        /// Start [Data2 = 0x00]
        /// </summary>
        bool StartIrZoomWide();

        /// <summary>
        /// [IR] 열영상 카메라 [ZOOM] 연속제어 정지 [Command2 = 0x31] [Data1 = 0xFF] : Zoom Stop
        /// [Data2 = 0x00]
        /// </summary>
        bool StopIrZoom();

        /// <summary>LA 수신 Raw 좌표를 그대로 사용하는 0x2B 절대 포커스 명령.</summary>
        bool LaIrFocusGoPosition(int rawPosition);

        /// <summary>
        /// v2.91 [IR] 열영상 카메라 [Focus] 절대 위치 제어 명령. 범위 0(Far)~1000(Near)을 Big Endian으로
        /// 송신한다. 장비가 직접 절대이동을 지원하지 않으면 상위 이동 로직이 기존 0x31 Near/Far/Stop 폐루프로 자동 전환한다.
        /// </summary>
        bool IrFocusGoPosition(short focus);

        /// <summary>
        /// [IR] 열영상 카메라 [FOCUS] [Near] 연속제어 시작 [Command2 = 0x31] [Data1 = 0x03] : Focus
        /// Near Start [Data2 = 0x00]
        /// </summary>
        bool StartIrFocusNear();

        /// <summary>
        /// [IR] 열영상 카메라 [FOCUS] [Far] 연속제어 시작 [Command2 = 0x31] [Data1 = 0x04] : Focus
        /// Far Start [Data2 = 0x00]
        /// </summary>
        bool StartIrFocusFar();

        /// <summary>
        /// [IR] 열영상 카메라 [FOCUS] 연속제어 정지 [Command2 = 0x31] [Data1 = 0x05] : Focus Stop
        /// [Data2 = 0x00]
        /// </summary>
        bool StopIrFocus();

        /// <summary>
        /// [IR] 열영상 카메라 [Digital Zoom] 확대 시작 [Command2 = 0x31] [Data1 = 0x07] : Digital
        /// Zoom In Start [Data2 = 0x00]
        /// </summary>
        bool StartIrDigitalZoomIn();

        /// <summary>
        /// [IR] 열영상 카메라 [Digital Zoom] 축소 시작 [Command2 = 0x31] [Data1 = 0x08] : Digital
        /// Zoom Out Start [Data2 = 0x00]
        /// </summary>
        bool StartIrDigitalZoomOut();

        /// <summary>
        /// [IR] 열영상 카메라 [Digital Zoom] 정지 [Command2 = 0x31] [Data1 = 0x06] : Digital Zoom
        /// Stop [Data2 = 0x00]
        /// </summary>
        bool StopIrDigitalZoom();

        /// <summary>
        /// [IR] 열영상 카메라 [Auto Focus] 요청 [Command2 = 0x31] [Data1 = 0x02] : Auto Focus
        /// [Data2 = 0x00]
        /// </summary>
        bool StartIrAutoFocus();

        /// <summary>
        /// IR 팔레트를 다음 항목으로 전환한다.
        /// </summary>
        bool SelectNextIrPalette();

        /// <summary>
        /// IR 팔레트를 이전 항목으로 전환한다.
        /// </summary>
        bool SelectPreviousIrPalette();

        /// <summary>
        /// IR 영상의 팔레트를 Black Hot으로 설정한다.
        /// </summary>
        bool SelectIrBlackHot();

        /// <summary>
        /// IR 영상의 팔레트를 White Hot으로 설정한다.
        /// </summary>
        bool SelectIrWhiteHot();

        /// <summary>
        /// IR 영상의 팔레트를 Rainbow로 설정한다.
        /// </summary>
        bool SelectIrRainbow();

        /// <summary>
        /// 열영상 NUC 잔열 보정을 요청한다.
        /// </summary>
        bool RequestIrNuc();
    }
}
