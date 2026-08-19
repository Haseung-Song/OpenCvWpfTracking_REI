using OpenCvWpfTracking.Common;
using System;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// [TORUSS] 감시장비 제어 명령 [Packet] 생성 / 송신 서비스
    /// 
    /// 제어 [Packet] 형식:
    /// [0] Sync Code  : 0xFF
    /// [1] Unit ID    : 0x01
    /// [2] Command 1
    /// [3] Command 2
    /// [4] Data 1
    /// [5] Data 2
    /// [6] CheckSum   : byte[1] ~ byte[5] 합
    /// </summary>
    public class ControlCommandService
    {
        /// <summary>
        /// [WEB AGENT](Local Agent) [TCP] 통신 서비스
        /// </summary>
        private readonly TcpClientService _tcpClientService;

        /// <summary>
        /// [Unit ID]
        /// 
        /// [TORUSS] 문서 기준 기본 [0x01] 고정 사용.
        /// [Packet] 생성 이후 변경되지 않으므로
        /// [readonly]로 선언한다.
        /// </summary>
        private readonly byte _unitId = 0x01;

        private readonly object _irPaletteSync =
            new object();

        private const int IrPaletteCount = 10;

        private int _currentIrPaletteIndex;

        private bool _isIrPaletteSynchronized;

        /// <summary>
        /// ControlCommandService 동작 수행 함수.
        /// </summary>
        public ControlCommandService(TcpClientService tcpClientService)
        {
            _tcpClientService = tcpClientService;
        }

        /// <summary>
        /// [TORUSS] 제어 [Packet] 생성 및 송신
        /// </summary>
        public bool SendCommand(byte cmd1, byte cmd2, byte data1, byte data2)
        {
            byte[] packet =
            {
                0xFF,
                _unitId,
                cmd1,
                cmd2,
                data1,
                data2,
                0x00
            };
            packet[6] = CheckSum(packet, 1, 5);

            return _tcpClientService.Send(packet);
        }

        /// <summary>
        /// [CheckSum] 계산 함수
        /// 지정 범위의 [byte] 합산값 반환
        /// </summary>
        private byte CheckSum(byte[] data, int startIndex, int length)
        {
            byte sum = 0;

            for (int i = startIndex; i < startIndex + length; i++)
            {
                sum += data[i];
            }
            return sum;
        }

        /// <summary>
        /// LA 실제 구현 기준 Pan / Tilt Home Position 실행.
        ///
        /// UICommandParser:
        /// Command2 = 0xB1
        /// pm-&gt;StartHoming();
        /// tm-&gt;StartHoming();
        /// </summary>
        public bool MoveHomePosition()
        {
            return SendCommand(
                0x00,
                0xB1,
                0x00,
                0x00);
        }

        /// <summary>
        /// [Pan] 이동 시 원점 통과 모드 설정
        ///
        /// TORUSS / Pelco-D 확장 명령:
        /// Command2 = 0x4D
        /// Data1    = 0x01
        /// Data2    = 0x00
        ///
        /// 이후 Pan 위치 이동 명령에서 장비가 원점 통과 경로를 선택한다.
        /// </summary>
        public bool SetPanViaZeroMode()
        {
            return SendCommand(
                0x00,
                0x4D,
                0x01,
                0x00);
        }

        /// <summary>
        /// [Pan] 이동 시 최단거리 선택 모드 설정
        ///
        /// TORUSS / Pelco-D 확장 명령:
        /// Command2 = 0x4D
        /// Data1    = 0x02
        /// Data2    = 0x00
        ///
        /// 이후 Pan 위치 이동 명령에서 장비가 최단 회전 경로를 선택한다.
        /// </summary>
        public bool SetPanShortestPathMode()
        {
            return SendCommand(
                0x00,
                0x4D,
                0x02,
                0x00);
        }


        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 ID를 설정한다.
        ///
        /// Command2 = 0x19
        /// Data1/Data2 = ID (0 ~ 99, Big Endian)
        /// </summary>
        public bool SetLaPresetId(
            ushort presetId)
        {
            presetId =
                NormalizeLaPresetId(
                    presetId);

            return SendUnsignedShortCommand(
                0x19,
                presetId);
        }

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Pan 위치를 설정한다.
        ///
        /// Command2 = 0x91
        /// Data1/Data2 = Degree * 100 (signed short, Big Endian)
        /// </summary>
        public bool SetLaPresetPan(
            double pan)
        {
            return SendSignedDegreeCommand(
                0x91,
                pan);
        }

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Tilt 위치를 설정한다.
        ///
        /// Command2 = 0x93
        /// Data1/Data2 = Degree * 100 (signed short, Big Endian)
        /// </summary>
        public bool SetLaPresetTilt(
            double tilt)
        {
            return SendSignedDegreeCommand(
                0x93,
                tilt);
        }

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Zoom 위치를 설정한다.
        ///
        /// Command2 = 0x95
        /// Data1/Data2 = Position (0 ~ 1000, Big Endian)
        /// </summary>
        public bool SetLaPresetZoom(
            ushort position)
        {
            position =
                NormalizePresetPosition(
                    position);

            return SendUnsignedShortCommand(
                0x95,
                position);
        }

        /// <summary>
        /// LA 실제 구현 기준 스캔 프리셋 Focus 위치를 설정하고,
        /// LA 내부 scan-&gt;AddPreset()을 완료한다.
        ///
        /// Command2 = 0x97
        /// Data1/Data2 = Position (0 ~ 1000, Big Endian)
        /// </summary>
        public bool SetLaPresetFocusAndCommit(
            ushort position)
        {
            position =
                NormalizePresetPosition(
                    position);

            return SendUnsignedShortCommand(
                0x97,
                position);
        }

        /// <summary>
        /// LA 실제 구현 기준 프리셋 위치로 이동한다.
        ///
        /// 실제 LA UICommandParser 구현:
        /// case 0x05:
        ///     scan-&gt;GotoPreset(GetInteger(data1, data2));
        ///
        /// Command2 = 0x05
        /// Data1/Data2 = ID (0 ~ 99, Big Endian)
        /// </summary>
        public bool MoveToLaPreset(
            ushort presetId)
        {
            presetId =
                NormalizeLaPresetId(
                    presetId);

            return SendUnsignedShortCommand(
                0x05,
                presetId);
        }

        /// <summary>
        /// LA 실제 구현 기준 스캔 이동 모드를 순환(CYCLE)으로 설정한다.
        ///
        /// UICommandParser:
        /// Command2 = 0x1B
        /// Data1 = 0x01 : CYCLE
        /// Data2 = 0x00
        ///
        /// P01 -> P02 -> ... -> P01 반복 순회를 보장하기 위해
        /// AUTO SCAN START 직전에 송신한다.
        /// </summary>
        public bool SetLaPresetScanCycleMode()
        {
            return SendCommand(
                0x00,
                0x1B,
                0x01,
                0x00);
        }

        /// <summary>
        /// LA 실제 구현 기준 전체 스캔 프리셋 데이터를 초기화한다.
        ///
        /// Command2 = 0x9B
        /// Data1 = 0x00
        /// Data2 = 0x01
        /// </summary>
        public bool ClearAllLaPresets()
        {
            return SendCommand(
                0x00,
                0x9B,
                0x00,
                0x01);
        }

        /// <summary>
        /// signed Degree 값을 Degree * 100으로 변환하여
        /// Big Endian 2byte 명령으로 송신한다.
        /// </summary>
        private bool SendSignedDegreeCommand(
            byte command2,
            double degree)
        {
            double safeDegree =
                Math.Max(
                    -180.0,
                    Math.Min(
                        180.0,
                        degree));

            short value =
                safeDegree < 0.0
                    ? (short)((safeDegree - 0.005) * 100.0)
                    : (short)((safeDegree + 0.005) * 100.0);

            byte data1 =
                (byte)((value >> 8) & 0xFF);

            byte data2 =
                (byte)(value & 0xFF);

            return SendCommand(
                0x00,
                command2,
                data1,
                data2);
        }

        /// <summary>
        /// unsigned 16bit 값을 Big Endian 2byte 명령으로 송신한다.
        /// </summary>
        private bool SendUnsignedShortCommand(
            byte command2,
            ushort value)
        {
            byte data1 =
                (byte)((value >> 8) & 0xFF);

            byte data2 =
                (byte)(value & 0xFF);

            return SendCommand(
                0x00,
                command2,
                data1,
                data2);
        }

        /// <summary>
        /// NormalizeLaPresetId 동작 수행 함수.
        /// </summary>
        private static ushort NormalizeLaPresetId(
            ushort presetId)
        {
            return presetId > 98
                ? (ushort)98
                : presetId;
        }

        /// <summary>
        /// NormalizePresetPosition 동작 수행 함수.
        /// </summary>
        private static ushort NormalizePresetPosition(
            ushort position)
        {
            return position > 1000
                ? (ushort)1000
                : position;
        }

        /// <summary>
        /// 현재 PTZF 위치를 프리셋 슬롯에 추가 / 갱신한다.
        ///
        /// TORUSS 프리셋 실행 / 편집 명령:
        /// Command2 = 0x03
        /// Data1    = 0x00
        /// Data2    = Preset Number
        ///
        /// 유효 슬롯:
        /// 1 ~ 63
        /// </summary>
        public bool AddPresetPoint(
            byte presetNumber)
        {
            presetNumber =
                NormalizePresetNumber(
                    presetNumber);

            return SendCommand(
                0x00,
                0x03,
                0x00,
                presetNumber);
        }

        /// <summary>
        /// 프리셋 슬롯을 제거한다.
        ///
        /// TORUSS 프리셋 실행 / 편집 명령:
        /// Command2 = 0x05
        /// Data1    = 0x00
        /// Data2    = Preset Number
        /// </summary>
        public bool RemovePresetPoint(
            byte presetNumber)
        {
            presetNumber =
                NormalizePresetNumber(
                    presetNumber);

            return SendCommand(
                0x00,
                0x05,
                0x00,
                presetNumber);
        }

        /// <summary>
        /// 저장된 프리셋 슬롯으로 이동한다.
        ///
        /// TORUSS 프리셋 실행 / 편집 명령:
        /// Command2 = 0x07
        /// Data1    = 0x00
        /// Data2    = Preset Number
        /// </summary>
        public bool MoveToPresetPoint(
            byte presetNumber)
        {
            presetNumber =
                NormalizePresetNumber(
                    presetNumber);

            return SendCommand(
                0x00,
                0x07,
                0x00,
                presetNumber);
        }

        /// <summary>
        /// 프리셋 오토 스캔을 시작한다.
        ///
        /// TORUSS 프리셋 설정 / 오토 스캔 명령:
        /// Command2 = 0x99
        /// Data1    = Speed (1 ~ 60)
        /// Data2    = Delay (1 ~ 60)
        /// </summary>
        public bool StartPresetScan(
            byte speed,
            byte delay)
        {
            speed =
                NormalizePresetScanValue(
                    speed);

            delay =
                NormalizePresetScanValue(
                    delay);

            return SendCommand(
                0x00,
                0x99,
                speed,
                delay);
        }

        /// <summary>
        /// 프리셋 오토 스캔을 정지한다.
        ///
        /// TORUSS 프리셋 설정 / 오토 스캔 명령:
        /// Command2 = 0x9B
        /// Data1    = 0x00
        /// Data2    = 0x00
        ///
        /// Data2 = 0x01은 프리셋 데이터 초기화이므로
        /// 일반 정지 함수에서는 사용하지 않는다.
        /// </summary>
        public bool StopPresetScan()
        {
            return SendCommand(
                0x00,
                0x9B,
                0x00,
                0x00);
        }

        /// <summary>
        /// 실행 중인 프리셋 스캔의 속도 / 정지시간을 변경한다.
        ///
        /// TORUSS 프리셋 설정 / 오토 스캔 명령:
        /// Command2 = 0x9D
        /// Data1    = Speed (1 ~ 60)
        /// Data2    = Delay (1 ~ 60)
        /// </summary>
        public bool UpdatePresetScan(
            byte speed,
            byte delay)
        {
            speed =
                NormalizePresetScanValue(
                    speed);

            delay =
                NormalizePresetScanValue(
                    delay);

            return SendCommand(
                0x00,
                0x9D,
                speed,
                delay);
        }

        /// <summary>
        /// 프리셋 번호를 문서 허용 범위 1 ~ 63으로 제한한다.
        /// </summary>
        private static byte NormalizePresetNumber(
            byte presetNumber)
        {
            if (presetNumber < 1)
            {
                return 1;
            }

            if (presetNumber > 63)
            {
                return 63;
            }

            return presetNumber;
        }

        /// <summary>
        /// 스캔 Speed / Delay를 문서 허용 범위 1 ~ 60으로 제한한다.
        /// </summary>
        private static byte NormalizePresetScanValue(
            byte value)
        {
            if (value < 1)
            {
                return 1;
            }

            if (value > 60)
            {
                return 60;
            }

            return value;
        }

        /// <summary>
        /// 사용자 정의 Pan 위치 이동 속도를 설정한다.
        ///
        /// Command2 = 0x49
        /// Speed    = deg/s * 100 (unsigned short, Big Endian)
        /// </summary>
        public bool SetPanPositionSpeed(
            double speedDegreesPerSecond)
        {
            ushort speedValue =
                ConvertPositionSpeed(
                    speedDegreesPerSecond);

            return SendCommand(
                0x00,
                0x49,
                (byte)((speedValue >> 8) & 0xFF),
                (byte)(speedValue & 0xFF));
        }

        /// <summary>
        /// 사용자 정의 Tilt 위치 이동 속도를 설정한다.
        ///
        /// Command2 = 0x4B
        /// Speed    = deg/s * 100 (unsigned short, Big Endian)
        /// </summary>
        public bool SetTiltPositionSpeed(
            double speedDegreesPerSecond)
        {
            ushort speedValue =
                ConvertPositionSpeed(
                    speedDegreesPerSecond);

            return SendCommand(
                0x00,
                0x4B,
                (byte)((speedValue >> 8) & 0xFF),
                (byte)(speedValue & 0xFF));
        }

        /// <summary>
        /// ConvertPositionSpeed 생성 및 변환 함수.
        /// </summary>
        private static ushort ConvertPositionSpeed(
            double speedDegreesPerSecond)
        {
            double safeSpeed =
                Math.Max(
                    0.0,
                    Math.Min(
                        655.35,
                        speedDegreesPerSecond));

            return (ushort)Math.Round(
                safeSpeed * 100.0,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// [Pan] 위치 제어 명령
        /// 
        /// 입력 기준은 -180 ~ 180도이다.
        ///
        /// 범위를 벗어난 내부 계산값은 signed Pan 범위로 정규화한 뒤,
        /// 위치 값을 [각도 * 100]하여
        /// [Data1 / Data2]에 [Big Endian] 방식으로 설정한다.
        /// </summary>
        public bool PanGoPosition(double pan)
        {
            while (pan > 180.0)
                pan -= 360.0;

            while (pan < -180.0)
                pan += 360.0;

            short value =
                pan < 0
                    ? (short)((pan - 0.005) * 100)
                    : (short)((pan + 0.005) * 100);

            byte data1 = (byte)((value >> 8) & 0xFF);
            byte data2 = (byte)(value & 0xFF);

            return SendCommand(
                0x00,
                0x45,
                data1,
                data2);
        }

        /// <summary>
        /// Pelco-D Pan / Tilt 속도를 문서 허용 범위 [0 ~ 63]으로 제한한다.
        ///
        /// UI의 0=STOP 정책과 1~50 환산은 ViewModel에서 처리하며,
        /// Service는 최종 Packet 값이 허용 범위를 벗어나지 않도록 방어한다.
        /// </summary>
        private static byte NormalizePanTiltProtocolSpeed(
            byte speed)
        {
            return speed > 0x3F
                ? (byte)0x3F
                : speed;
        }

        /// <summary>
        /// [PAN] 우측 연속 이동 시작
        /// 
        /// [Command2 Bit0 = Pan Right]
        /// [Data1 = Pan Speed Level] [0 ~ 63]
        /// </summary>
        public bool StartPanRight(byte speed = 20)
        {
            byte protocolSpeed =
                NormalizePanTiltProtocolSpeed(
                    speed);

            return SendCommand(
                0x00,
                0x02,
                protocolSpeed,
                0x00);
        }

        /// <summary>
        /// [PAN] 좌측 연속 이동 시작
        /// 
        /// [Command2 Bit1 = Pan Left]
        /// [Data1 = Pan Speed Level] [0 ~ 63]
        /// </summary>
        public bool StartPanLeft(byte speed = 20)
        {
            byte protocolSpeed =
                NormalizePanTiltProtocolSpeed(
                    speed);

            return SendCommand(
                0x00,
                0x04,
                protocolSpeed,
                0x00);
        }

        /// <summary>
        /// [Tilt] 위치 제어 명령
        /// </summary>
        public bool TiltGoPosition(double tilt)
        {
            while (tilt > 180.0)
                tilt -= 360.0;

            while (tilt < -180.0)
                tilt += 360.0;

            short value =
                tilt < 0
                    ? (short)((tilt - 0.005) * 100)
                    : (short)((tilt + 0.005) * 100);

            byte data1 = (byte)((value >> 8) & 0xFF);
            byte data2 = (byte)(value & 0xFF);

            return SendCommand(
                0x00,
                0x47,
                data1,
                data2);
        }

        /// <summary>
        /// [TILT] 위쪽 연속 이동 시작
        /// 
        /// [Command2 Bit2 = Tilt Up]
        /// [Data2 = Tilt Speed Level] [0 ~ 63]
        /// </summary>
        public bool StartTiltUp(byte speed = 20)
        {
            byte protocolSpeed =
                NormalizePanTiltProtocolSpeed(
                    speed);

            return SendCommand(
                0x00,
                0x08,
                0x00,
                protocolSpeed);
        }

        /// <summary>
        /// [TILT] 아래쪽 연속 이동 시작
        /// 
        /// [Command2 Bit3 = Tilt Down]
        /// [Data2 = Tilt Speed Level] [0 ~ 63]
        /// </summary>
        public bool StartTiltDown(byte speed = 20)
        {
            byte protocolSpeed =
                NormalizePanTiltProtocolSpeed(
                    speed);

            return SendCommand(
                0x00,
                0x10,
                0x00,
                protocolSpeed);
        }

        /// <summary>
        /// [PAN LEFT + TILT UP] 좌측 상단 대각선 연속 이동 시작
        ///
        /// [Command2]
        /// Pan Left : 0x04
        /// Tilt Up  : 0x08
        /// 결합값   : 0x0C
        ///
        /// [Data1] = Pan Speed
        /// [Data2] = Tilt Speed
        /// </summary>
        public bool StartPanLeftTiltUp(
            byte panSpeed = 20,
            byte tiltSpeed = 20)
        {
            byte protocolPanSpeed =
                NormalizePanTiltProtocolSpeed(
                    panSpeed);

            byte protocolTiltSpeed =
                NormalizePanTiltProtocolSpeed(
                    tiltSpeed);

            return SendCommand(
                0x00,
                0x0C,
                protocolPanSpeed,
                protocolTiltSpeed);
        }

        /// <summary>
        /// [PAN RIGHT + TILT UP] 우측 상단 대각선 연속 이동 시작
        ///
        /// [Command2]
        /// Pan Right : 0x02
        /// Tilt Up   : 0x08
        /// 결합값    : 0x0A
        ///
        /// [Data1] = Pan Speed
        /// [Data2] = Tilt Speed
        /// </summary>
        public bool StartPanRightTiltUp(
            byte panSpeed = 20,
            byte tiltSpeed = 20)
        {
            byte protocolPanSpeed =
                NormalizePanTiltProtocolSpeed(
                    panSpeed);

            byte protocolTiltSpeed =
                NormalizePanTiltProtocolSpeed(
                    tiltSpeed);

            return SendCommand(
                0x00,
                0x0A,
                protocolPanSpeed,
                protocolTiltSpeed);
        }

        /// <summary>
        /// [PAN LEFT + TILT DOWN] 좌측 하단 대각선 연속 이동 시작
        ///
        /// [Command2]
        /// Pan Left  : 0x04
        /// Tilt Down : 0x10
        /// 결합값    : 0x14
        ///
        /// [Data1] = Pan Speed
        /// [Data2] = Tilt Speed
        /// </summary>
        public bool StartPanLeftTiltDown(
            byte panSpeed = 20,
            byte tiltSpeed = 20)
        {
            byte protocolPanSpeed =
                NormalizePanTiltProtocolSpeed(
                    panSpeed);

            byte protocolTiltSpeed =
                NormalizePanTiltProtocolSpeed(
                    tiltSpeed);

            return SendCommand(
                0x00,
                0x14,
                protocolPanSpeed,
                protocolTiltSpeed);
        }

        /// <summary>
        /// [PAN RIGHT + TILT DOWN] 우측 하단 대각선 연속 이동 시작
        ///
        /// [Command2]
        /// Pan Right : 0x02
        /// Tilt Down : 0x10
        /// 결합값    : 0x12
        ///
        /// [Data1] = Pan Speed
        /// [Data2] = Tilt Speed
        /// </summary>
        public bool StartPanRightTiltDown(
            byte panSpeed = 20,
            byte tiltSpeed = 20)
        {
            byte protocolPanSpeed =
                NormalizePanTiltProtocolSpeed(
                    panSpeed);

            byte protocolTiltSpeed =
                NormalizePanTiltProtocolSpeed(
                    tiltSpeed);

            return SendCommand(
                0x00,
                0x12,
                protocolPanSpeed,
                protocolTiltSpeed);
        }

        /// <summary>
        /// Pan 위치 이동을 정지한다.
        ///
        /// TORUSS 위치 제어 명령:
        /// Command2 = 0x4F
        /// Data1    = 0x01 (Pan Position Stop)
        /// Data2    = 0x00
        /// </summary>
        public bool StopPanPositionMove()
        {
            return SendCommand(
                0x00,
                0x4F,
                0x01,
                0x00);
        }

        /// <summary>
        /// Tilt 위치 이동을 정지한다.
        ///
        /// TORUSS 위치 제어 명령:
        /// Command2 = 0x4F
        /// Data1    = 0x02 (Tilt Position Stop)
        /// Data2    = 0x00
        /// </summary>
        public bool StopTiltPositionMove()
        {
            return SendCommand(
                0x00,
                0x4F,
                0x02,
                0x00);
        }

        /// <summary>
        /// Pan과 Tilt 위치 이동 정지 명령을 순서대로 송신한다.
        /// PRESET GOTO 또는 ABSOLUTE 위치 이동 중지에 사용한다.
        /// </summary>
        public bool StopPanTiltPositionMove()
        {
            bool panResult =
                StopPanPositionMove();

            bool tiltResult =
                StopTiltPositionMove();

            ConsoleLogHelper.Command(
                "POSITION STOP",
                $"PAN={panResult} / TILT={tiltResult} / CMD2=0x4F");

            return panResult &&
                   tiltResult;
        }

        /// <summary>
        /// 전체 연속 속도제어 정지
        /// </summary>
        public bool StopMove()
        {
            Console.WriteLine();
            Console.WriteLine(
                "[CONTROL] STOP MOVE");

            Console.WriteLine(
                "[CONTROL] STOP COMMAND PARAMETER : " +
                "CMD1=0x00, CMD2=0x00, DATA1=0x00, DATA2=0x00");

            bool result =
                SendCommand(
                    0x00,
                    0x00,
                    0x00,
                    0x00);

            Console.WriteLine(
                $"[CONTROL] STOP SEND RESULT : {result}");

            ConsoleLogHelper.PrintLine();

            return result;
        }

        /// <summary>
        /// PTZ(회전형) 카메라 [Zoom] 위치 제어 명령
        /// 범위: [0 ~ 1000]
        /// </summary>
        public bool EoZoomGoPosition(short zoom)
        {
            if (zoom > 1000)
                zoom = 1000;
            else if (zoom < 0)
                zoom = 0;

            byte data1 = (byte)((zoom >> 8) & 0xFF);
            byte data2 = (byte)(zoom & 0xFF);

            return SendCommand(
                0x00,
                0x37,
                data1,
                data2);
        }

        /// <summary>
        /// [EO] [ZOOM] [Tele] 연속제어 시작
        /// 
        /// [Command2 Bit5 = Zoom Tele]
        /// </summary>
        public bool StartEoZoomTele()
        {
            return SendCommand(
                0x00,
                0x20,
                0x00,
                0x00);
        }

        /// <summary>
        /// [EO] [ZOOM] [Wide] 연속제어 시작
        /// 
        /// [Command2 Bit6 = Zoom Wide]
        /// </summary>
        public bool StartEoZoomWide()
        {
            return SendCommand(
                0x00,
                0x40,
                0x00,
                0x00);
        }

        /// <summary>
        /// [EO] PTZ(회전형) 카메라 [Focus] 위치 제어 명령
        /// 범위: [0 ~ 1000]
        /// </summary>
        public bool EoFocusGoPosition(
            short focus)
        {
            if (focus > 1000)
            {
                focus = 1000;
            }
            else if (focus < 0)
            {
                focus = 0;
            }

            byte data1 =
                (byte)((focus >> 8) & 0xFF);

            byte data2 =
                (byte)(focus & 0xFF);

            return SendCommand(
                0x00,
                0x39,
                data1,
                data2);
        }

        /// <summary>
        /// [EO] 주간 카메라 Focus 연속 제어 속도 설정
        ///
        /// Command2 = 0x27
        /// Data2    = Speed [0 ~ 3]
        ///
        /// 우선 최소 속도 Level 0을 사용한다.
        /// 장비 반응 확인 후 1 ~ 3 범위에서 조정한다.
        /// </summary>
        public bool SetEoFocusSpeed(
            byte speed)
        {
            if (speed > 3)
            {
                speed = 3;
            }

            Console.WriteLine();
            Console.WriteLine(
                $"[CONTROL] EO FOCUS SPEED SET : {speed}");

            return SendCommand(
                0x00,
                0x27,
                0x00,
                speed);
        }

        /// <summary>
        /// [EO] [FOCUS] [Near] 연속제어 시작
        /// 
        /// [Command2 Bit0 = Focus Near]
        /// </summary>
        public bool StartEoFocusNear()
        {
            return SendCommand(
                0x01,
                0x00,
                0x00,
                0x00);
        }

        /// <summary>
        /// [EO] [FOCUS] [Far] 연속제어 시작
        /// 
        /// [Command1 Bit7 = Focus Far]
        /// </summary>
        public bool StartEoFocusFar()
        {
            return SendCommand(
                0x00,
                0x80,
                0x00,
                0x00);
        }

        /// <summary>
        /// [EO] 주간 카메라 [One Push Auto Focus] 요청
        ///
        /// 기존 EO 제어 구현에서 사용하던
        /// [Pelco-D] 확장 명령을 동일한 제어 TCP 연결로 송신한다.
        ///
        /// [Command1 = 0x00]
        /// [Command2 = 0x2B]
        /// [Data1    = 0x00]
        /// [Data2    = 0x00]
        /// </summary>
        public bool StartEoAutoFocus()
        {
            return SendCommand(
                0x00,
                0x2B,
                0x00,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [Zoom] 위치 제어 명령
        /// 
        /// 위치 값은 [화각 * 100] 후
        /// [Data1 / Data2]에 [Big Endian] 방식으로 설정
        /// </summary>
        public bool IrZoomGoPosition(short zoom)
        {
            byte data1 = (byte)((zoom >> 8) & 0xFF);
            byte data2 = (byte)(zoom & 0xFF);

            return SendCommand(
                0x00,
                0x29,
                data1,
                data2);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [ZOOM] [Tele] 연속제어 시작
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x00] : Zoom In Start
        /// [Data2 = 0x00]
        /// </summary>
        public bool StartIrZoomTele()
        {
            return SendCommand(
                0x00,
                0x31,
                0x00,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [ZOOM] [Wide] 연속제어 시작
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x01] : Zoom Out Start
        /// [Data2 = 0x00]
        /// </summary>
        public bool StartIrZoomWide()
        {
            return SendCommand(
                0x00,
                0x31,
                0x01,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [ZOOM] 연속제어 정지
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0xFF] : Zoom Stop
        /// [Data2 = 0x00]
        /// </summary>
        public bool StopIrZoom()
        {
            return SendCommand(
                0x00,
                0x31,
                0xFF,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [Focus] 위치 제어 명령
        /// 
        /// 범위: [0 ~ 1000]
        /// </summary>
        public bool IrFocusGoPosition(short focus)
        {
            if (focus > 1000)
                focus = 1000;
            else if (focus < 0)
                focus = 0;

            byte data1 = (byte)((focus >> 8) & 0xFF);
            byte data2 = (byte)(focus & 0xFF);

            return SendCommand(
                0x00,
                0x28,
                data1,
                data2);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [FOCUS] [Near] 연속제어 시작
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x03] : Focus Near Start
        /// [Data2 = 0x00]
        /// </summary>
        public bool StartIrFocusNear()
        {
            return SendCommand(
                0x00,
                0x31,
                0x03,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [FOCUS] [Far] 연속제어 시작
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x04] : Focus Far Start
        /// [Data2 = 0x00]
        /// </summary>
        public bool StartIrFocusFar()
        {
            return SendCommand(
                0x00,
                0x31,
                0x04,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [FOCUS] 연속제어 정지
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x05] : Focus Stop
        /// [Data2 = 0x00]
        /// </summary>
        public bool StopIrFocus()
        {
            return SendCommand(
                0x00,
                0x31,
                0x05,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [Digital Zoom] 확대 시작
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x07] : Digital Zoom In Start
        /// [Data2 = 0x00]
        /// </summary>
        public bool StartIrDigitalZoomIn()
        {
            return SendCommand(
                0x00,
                0x31,
                0x07,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [Digital Zoom] 축소 시작
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x08] : Digital Zoom Out Start
        /// [Data2 = 0x00]
        /// </summary>
        public bool StartIrDigitalZoomOut()
        {
            return SendCommand(
                0x00,
                0x31,
                0x08,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [Digital Zoom] 정지
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x06] : Digital Zoom Stop
        /// [Data2 = 0x00]
        /// </summary>
        public bool StopIrDigitalZoom()
        {
            return SendCommand(
                0x00,
                0x31,
                0x06,
                0x00);
        }

        /// <summary>
        /// [IR] 열영상 카메라 [Auto Focus] 요청
        /// 
        /// [Command2 = 0x31]
        /// [Data1 = 0x02] : Auto Focus
        /// [Data2 = 0x00]
        /// </summary>
        public bool StartIrAutoFocus()
        {
            return SendCommand(
                0x00,
                0x31,
                0x02,
                0x00);
        }

        #region [IR Thermal Image Control]

        /// <summary>
        /// SendIrImageControlCommand 송신 함수.
        /// </summary>
        private bool SendIrImageControlCommand(byte operation)
        {
            return SendCommand(0x00, 0x31, operation, 0x00);
        }

        /// <summary>
        /// 실제 장비 Palette를 BLACK HOT으로 초기화하고
        /// 프로그램의 현재 Palette Index를 동기화한다.
        /// </summary>
        public bool InitializeIrPaletteToBlackHot()
        {
            lock (_irPaletteSync)
            {
                // 2026-08-14: BLACK HOT is the inverse grayscale command (0xF4).
                if (!SendIrImageControlCommand(0xF4))
                {
                    _isIrPaletteSynchronized = false;
                    return false;
                }

                _currentIrPaletteIndex = 0;
                _isIrPaletteSynchronized = true;

                return true;
            }

        }

        /// <summary>
        /// 동기화된 현재 Palette에서 선택한 Palette까지
        /// 필요한 NEXT 또는 PREV 명령만 송신한다.
        /// 이미 선택된 Palette를 다시 적용하면 명령을 송신하지 않는다.
        /// </summary>
        public bool SelectIrPalette(byte paletteIndex)
        {
            if (paletteIndex >= IrPaletteCount)
            {
                return false;
            }

            lock (_irPaletteSync)
            {
                if (!_isIrPaletteSynchronized)
                {
                    return false;
                }

                int targetIndex = paletteIndex;

                if (targetIndex == _currentIrPaletteIndex)
                {
                    return true;
                }

                int nextCount =
                    (targetIndex - _currentIrPaletteIndex +
                     IrPaletteCount) % IrPaletteCount;

                int previousCount =
                    (_currentIrPaletteIndex - targetIndex +
                     IrPaletteCount) % IrPaletteCount;

                byte operation;
                int moveCount;

                if (nextCount <= previousCount)
                {
                    operation = 0x0D;
                    moveCount = nextCount;
                }
                else
                {
                    operation = 0x0E;
                    moveCount = previousCount;
                }

                for (int index = 0; index < moveCount; index++)
                {
                    if (!SendIrImageControlCommand(operation))
                    {
                        // 일부 명령만 적용됐을 수 있으므로 추적값을 폐기한다.
                        _isIrPaletteSynchronized = false;
                        return false;
                    }

                    if (index + 1 < moveCount)
                    {
                        System.Threading.Thread.Sleep(75);
                    }

                }

                _currentIrPaletteIndex = targetIndex;
                return true;
            }

        }

        /// <summary>
        /// SelectNextIrPalette 동작 수행 함수.
        /// </summary>
        public bool SelectNextIrPalette()
        {
            return SendEnvironmentIrPaletteCommand(0x0D);
        }

        /// <summary>
        /// SelectPreviousIrPalette 동작 수행 함수.
        /// </summary>
        public bool SelectPreviousIrPalette()
        {
            return SendEnvironmentIrPaletteCommand(0x0E);
        }

        /// <summary>
        /// SelectIrBlackHot 동작 수행 함수.
        /// </summary>
        public bool SelectIrBlackHot()
        {
            // 2026-08-14: BLACK HOT = inverse grayscale palette (0xF4).
            return SendEnvironmentIrPaletteCommand(0xF4);
        }

        /// <summary>
        /// SelectIrWhiteHot 동작 수행 함수.
        /// </summary>
        public bool SelectIrWhiteHot()
        {
            // 2026-08-14: WHITE HOT = normal grayscale palette (0xF3).
            return SendEnvironmentIrPaletteCommand(0xF3);
        }

        /// <summary>
        /// SelectIrRainbow 동작 수행 함수.
        /// </summary>
        public bool SelectIrRainbow()
        {
            // 2026-08-14: RAINBOW direct selection = 0xF5.
            return SendEnvironmentIrPaletteCommand(0xF5);
        }

        /// <summary>
        /// SendEnvironmentIrPaletteCommand 송신 함수.
        /// </summary>
        private bool SendEnvironmentIrPaletteCommand(byte operation)
        {
            lock (_irPaletteSync)
            {
                bool result = SendIrImageControlCommand(operation);

                // ENVIRONMENT 상대/직접 명령은 ROOFTOP의 10단계 추적값과
                // 독립적이므로 다음 ROOFTOP 진입 시 반드시 재동기화한다.
                _isIrPaletteSynchronized = false;

                return result;
            }

        }

        /// <summary>
        /// 열영상 NUC 잔열 보정을 요청한다.
        /// </summary>
        public bool RequestIrNuc()
        {
            // 2026-08-14: 규격서 2.7 기준 NUC = Command2 0x31, Data1 0x0F.
            return SendCommand(0x00, 0x31, 0x0F, 0x00);
        }

        #endregion

        /// <summary>
        /// 거리측정기 [1회] 측정 요청
        /// 
        /// [Command2 = 0x57]
        /// </summary>
        public bool ReadOnceLrfValue()
        {
            return SendCommand(
                0x00,
                0x57,
                0x00,
                0x00);
        }

    }

}
