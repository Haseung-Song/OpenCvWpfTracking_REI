namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// [WEB AGENT](Local Agent) 응답 [Packet] 데이터 클래스
    /// 
    /// 역할:
    /// 1. [WEB AGENT]에서 수신한 원본 [12byte Packet] 데이터 저장
    /// 2. [Header] / [Function] / [Checksum] 정보 접근
    /// 3. [Packet] 유효성 상태 저장
    /// 
    /// [TORUSS] 응답 [Packet] 구조:
    /// 
    /// [0]  : [Header]      [0xFF]
    /// [1]  : [Function]
    /// [2]  : [Data1]
    /// [3]  : [Data2]
    /// [4]  : [Data3]
    /// [5]  : [Data4]
    /// [6]  : [Data5]
    /// [7]  : [Data6]
    /// [8]  : [Data7]
    /// [9]  : [Data8]
    /// [10] : [Data9]
    /// [11] : [Checksum]
    /// 
    /// [Checksum]:
    /// [packet[1] ~ packet[10] 합산값] 사용
    /// </summary>
    public class LaResponsePacket
    {
        #region [Fields / Properties]

        /// <summary>
        /// [WEB AGENT]에서 수신한 원본 [12byte Packet]
        /// 
        /// 예: FF 01 F8 FF 00 00 00 00 8B 00 C9 4C
        /// </summary>
        public byte[] RawData { get; set; }

        /// <summary>
        /// [Packet Header]
        /// 
        /// [TORUSS] 응답 [Packet] 시작 값
        /// 정상 [Packet] 기준 [0xFF] 사용
        /// 
        /// 위치: packet[0]
        /// </summary>
        public byte Header => RawData[0];

        /// <summary>
        /// [Function Number]
        /// 
        /// 현재 수신 [Packet] 종류를 구분하는 값
        /// 
        /// 위치: packet[1]
        /// 
        /// 주요 [Function]:
        /// 
        /// [0x01] [Pan] / [Tilt] / [Zoom] / [Focus] 상태 정보
        /// 
        /// [0x07] [IR Zoom] / [IR Focus] 위치 상태 [Packet]
        ///
        /// [0xA1] 미확인 확장 상태 [Packet]
        /// 
        /// [0x04] [LRF] 거리측정 응답 [Packet]
        /// </summary>
        public byte Function => RawData[1];

        /// <summary>
        /// [Checksum] 값
        /// 
        /// [TORUSS] 응답 [Packet] 마지막 [byte] 값
        /// 
        /// 위치: packet[11]
        /// </summary>
        public byte Checksum => RawData[11];

        /// <summary>
        /// [IR Camera Status Packet] 여부
        ///
        /// Function 0x07:
        /// 열영상 카메라 Zoom / Focus 위치 상태
        /// </summary>
        public bool IsIrCameraStatus =>
            Function ==
            0x07;

        /// <summary>
        /// [IR Zoom Position]
        ///
        /// Function 0x07 Packet:
        /// packet[2] = Low Byte
        /// packet[3] = High Byte
        ///
        /// 장비 수신 Packet은 Little Endian 형식으로 확인된다.
        ///
        /// 예:
        /// D6 03 → 0x03D6 → 982
        ///
        /// 정상 운용 범위:
        /// 0 ~ 1000
        /// </summary>
        public ushort IrZoomPosition =>
            IsIrCameraStatus &&
            RawData != null &&
            RawData.Length >= 6
                ? (ushort)(
                    RawData[2] |
                    RawData[3] << 8)
                : (ushort)0;

        /// <summary>
        /// [IR Focus Position]
        ///
        /// Function 0x07 Packet:
        /// packet[4] = Low Byte
        /// packet[5] = High Byte
        ///
        /// 예:
        /// E8 03 → 0x03E8 → 1000
        ///
        /// 정상 운용 범위:
        /// 0 ~ 1000
        /// </summary>
        public ushort IrFocusPosition =>
            IsIrCameraStatus &&
            RawData != null &&
            RawData.Length >= 6
                ? (ushort)(
                    RawData[4] |
                    RawData[5] << 8)
                : (ushort)0;

        /// <summary>
        /// [Packet] 유효성 여부
        /// 
        /// [LAPacketParser]에서
        /// [Header] / [Checksum] 검증 후 설정된다.
        /// 
        /// [true]:
        /// 정상 [Packet]
        /// 
        /// [false]:
        /// 손상 또는 비정상 [Packet]
        /// </summary>
        public bool IsValid { get; set; }

        #endregion
    }

}
