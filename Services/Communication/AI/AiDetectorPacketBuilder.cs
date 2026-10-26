using System;
using System.Text;

namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>
    /// [AI] [Detector Agent] 요청 [Packet] 생성 클래스
    /// 
    /// [AI] [Detector Agent]로 송신할 요청 [Packet]을 생성한다.
    /// 
    /// [Packet] 구조:
    /// [0]      [STX]      : 0x02
    /// [1..2]   [CMD]      : [ASCII] 2자리
    /// [3..5]   [SIZE]     : [Payload] [UTF-8] [byte] 길이, [ASCII] 3자리
    /// [6..N]   [PAYLOAD]  : [UTF-8] 문자열
    /// [N+1]    [CHECKSUM] : [CMD] [ASCII] + [Payload] [byte] 합산 하위 1[byte]
    /// [N+2]    [ETX]      : 0x03
    /// 
    /// 현재 사용 목적:
    /// 1. [CMD 01] [AI Detector Info] 조회
    /// 2. [CMD 02] [RTSP] 주소 설정
    /// 3. [CMD 03] [RTSP] 주소 조회
    /// 4. [CMD 04] [ONNX] 파일 목록 조회
    /// 5. [CMD 05] [RTSP] / [ONNX] [Mapping] 설정
    /// 6. [CMD 06] [RTSP] / [ONNX] [Mapping] 조회
    /// 7. [CMD 07] [RTSP] 원본 영상 크기 조회
    /// </summary>
    public class AiDetectorPacketBuilder
    {
        #region [상수]

        /// <summary>
        /// [Packet] 시작 문자 [STX]
        /// </summary>
        private const byte Stx = 0x02;

        /// <summary>
        /// [Packet] 종료 문자 [ETX]
        /// </summary>
        private const byte Etx = 0x03;

        #endregion

        #region [요청 패킷 생성]

        /// <summary>
        /// [AI Detector Info] 조회 요청 [Packet] 생성
        /// 
        /// 요청 [CMD 01]
        /// 응답 [CMD 51]
        /// </summary>
        public byte[] BuildAiDetectorInfoRequest()
        {
            return BuildRequestPacket("01", string.Empty);
        }

        /// <summary>
        /// [RTSP] 주소 설정 요청 [Packet] 생성
        /// 
        /// 요청 [CMD 02]
        /// 
        /// [RTSP] 주소 목록은 [US] [Unit Separator] [0x1F] 기준으로 구분한다.
        /// </summary>
        public byte[] BuildRtspAddressSetRequest(string eoRtspAddress, string irRtspAddress)
        {
            string payload = string.Join(((char)0x1F).ToString(), eoRtspAddress, irRtspAddress);
            return BuildRequestPacket("02", payload);
        }

        /// <summary>
        /// [RTSP] 주소 조회 요청 [Packet] 생성
        /// 
        /// 요청 [CMD 03]
        /// 응답 [CMD 52]
        /// </summary>
        public byte[] BuildRtspAddressRequest()
        {
            return BuildRequestPacket("03", string.Empty);
        }

        /// <summary>
        /// [ONNX] 파일 목록 조회 요청 [Packet] 생성
        /// 
        /// 요청 [CMD 04]
        /// 응답 [CMD 53]
        /// </summary>
        public byte[] BuildOnnxListRequest()
        {
            return BuildRequestPacket("04", string.Empty);
        }

        /// <summary>
        /// [RTSP] / [ONNX] [Mapping] 설정 요청 [Packet] 생성
        /// 
        /// 요청 [CMD 05]
        /// 
        /// [UI]에서 입력한 [RTSP 0] / [RTSP 1]별 [ONNX Index],
        /// [Confidence], [IOU] 값을 기준으로 [Mapping] 설정 [Packet]을 생성한다.
        /// 
        /// 생성 [Payload] 예:
        /// 0:1^0.10^0.45[US]1:2^0.10^0.45
        /// </summary>
        public byte[] BuildRtspOnnxMappingSetRequest(int rtsp0OnnxIndex, int rtsp1OnnxIndex, double confidence, double iou)
        {
            string confidenceText = confidence.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            string iouText = iou.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            string payload = $"0:{rtsp0OnnxIndex}^{confidenceText}^{iouText}" + ((char)0x1F).ToString() + $"1:{rtsp1OnnxIndex}^{confidenceText}^{iouText}";
            return BuildRequestPacket("05", payload);
        }

        /// <summary>
        /// [RTSP] / [ONNX] [Mapping] 조회 요청 [Packet] 생성
        /// 
        /// 요청 [CMD 06]
        /// 응답 [CMD 54]
        /// </summary>
        public byte[] BuildRtspOnnxMappingRequest()
        {
            return BuildRequestPacket("06", string.Empty);
        }

        /// <summary>
        /// [RTSP] 원본 영상 크기 조회 요청 [Packet] 생성
        ///
        /// 요청 [CMD 07]
        /// 응답 [CMD 56]
        /// Payload는 0부터 시작하는 RTSP Index의 ASCII 10진 문자열이다.
        /// </summary>
        public byte[] BuildRtspVideoSizeRequest(int rtspIndex)
        {
            if (rtspIndex < 0 || rtspIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(rtspIndex), rtspIndex, "RTSP Index는 0~2 범위여야 합니다.");
            }

            return BuildRequestPacket("07", rtspIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        #endregion

        #region [Common 패킷 생성]

        /// <summary>
        /// 공통 요청 [Packet] 생성
        /// 
        /// [CMD]와 [Payload]를 기반으로
        /// [AI] [Detector Agent] 송신용 완성 [Packet]을 생성한다.
        /// 
        /// 조회 요청의 경우 [Payload]가 비어 있을 수 있으므로
        /// [SIZE]는 [000]으로 생성된다.
        /// </summary>
        private byte[] BuildRequestPacket(string command, string payload)
        {
            if (string.IsNullOrWhiteSpace(command) || command.Length != 2)
            {
                throw new ArgumentException("[AI PACKET BUILDER] Command must be 2 ASCII characters.");
            }

            if (payload == null)
            {
                payload = string.Empty;
            }

            byte[] commandBytes = Encoding.ASCII.GetBytes(command);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            if (payloadBytes.Length > 999)
            {
                throw new ArgumentOutOfRangeException(nameof(payload), "AI Payload의 UTF-8 바이트 길이는 999 이하여야 합니다.");
            }

            string sizeText = payloadBytes.Length.ToString("D3");
            byte[] sizeBytes = Encoding.ASCII.GetBytes(sizeText);
            byte checksum = CalculateChecksum(commandBytes, payloadBytes);
            int packetSize = 1 + commandBytes.Length + sizeBytes.Length + payloadBytes.Length + 1 + 1;
            byte[] packet = new byte[packetSize];
            int index = 0;
            packet[index++] = Stx;
            Array.Copy(commandBytes, 0, packet, index, commandBytes.Length);
            index += commandBytes.Length;
            Array.Copy(sizeBytes, 0, packet, index, sizeBytes.Length);
            index += sizeBytes.Length;
            Array.Copy(payloadBytes, 0, packet, index, payloadBytes.Length);
            index += payloadBytes.Length;
            packet[index++] = checksum;
            packet[index] = Etx;
            return packet;
        }

        /// <summary>
        /// [Checksum] 계산
        /// 
        /// 문서 기준:
        /// [Checksum] = [CMD] [ASCII] [byte] + [Payload] [byte] 전체 합산
        /// 
        /// [byte] 자료형이므로 자동으로 하위 1[byte]만 유지된다.
        /// </summary>
        private byte CalculateChecksum(byte[] commandBytes, byte[] payloadBytes)
        {
            byte checksum = 0;

            for (int i = 0; i < commandBytes.Length; i++)
            {
                checksum += commandBytes[i];
            }

            for (int i = 0; i < payloadBytes.Length; i++)
            {
                checksum += payloadBytes[i];
            }

            return checksum;
        }

        #endregion
    }
}
