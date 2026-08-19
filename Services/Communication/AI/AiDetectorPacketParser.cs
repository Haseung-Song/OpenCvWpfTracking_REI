using OpenCvWpfTracking.Models.AI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>
    /// [AI] [Detector Agent]에서 수신한 [Packet]을 해석하는 [Parser] 클래스
    ///
    /// 역할:
    /// 1. [TCP] 수신 [Buffer]에서 완성된 [AI] [Packet] 분리
    /// 2. [Packet]의 [STX] / [CMD] / [SIZE] / [CHECKSUM] / [ETX] 검증
    /// 3. [CMD 55] 탐지데이터 [Payload]를 [AiDetectionResult]로 변환
    ///
    /// [Packet] 구조:
    /// [0]      [STX]      : 0x02
    /// [1..2]   [CMD]      : [ASCII] 2자리
    /// [3..5]   [SIZE]     : [Payload] [UTF-8] [byte] 길이, [ASCII] 3자리
    /// [6..N]   [PAYLOAD]  : [UTF-8] 문자열
    /// [N+1]    [CHECKSUM] : [CMD] [ASCII] + [Payload] [bytes] 합산 하위 1[byte]
    /// [N+2]    [ETX]      : 0x03
    ///
    /// [Payload] 내부 파라미터 구분자:
    /// 
    /// 현재 [US] [Unit Separator] [0x1F] 기준으로 구분한다.
    /// </summary>
    public class AiDetectorPacketParser
    {
        #region [Constants]

        /// <summary>
        /// [Packet] 시작 문자 [STX]
        /// </summary>
        private const byte Stx = 0x02;

        /// <summary>
        /// [Packet] 종료 문자 [ETX]
        /// </summary>
        private const byte Etx = 0x03;

        /// <summary>
        /// [Payload] 내부 파라미터 구분자
        ///
        /// 기존 [Space] 구분자는
        /// [RTSP] 문자열 / 비밀번호 등에서 충돌 가능성이 있어
        /// [US] [Unit Separator] [0x1F] 기준으로 변경되었다.
        /// </summary>
        private const char PayloadSeparator = (char)0x1F;

        /// <summary>
        /// [Header] 크기
        /// [STX] 1[byte] + [CMD] 2[byte] + [SIZE] 3[byte] = 6[byte]
        /// </summary>
        private const int HeaderSize = 6;

        /// <summary>
        /// [Checksum] 크기
        /// </summary>
        private const int ChecksumSize = 1;

        /// <summary>
        /// [ETX] 크기
        /// </summary>
        private const int EtxSize = 1;

        /// <summary>
        /// 탐지데이터 응답 [CMD]
        /// </summary>
        private const string DetectCommand = "55";

        #endregion

        #region [Packet Extract]

        /// <summary>
        /// [TCP] 수신 [Buffer]에서 완성된 [AI] [Packet]들을 분리한다.
        ///
        /// [TCP]는 [Packet] 단위로 딱 맞게 들어오지 않는다.
        /// 그래서 [receiveBuffer]에 누적해두고,
        /// [SIZE] 값을 기준으로 완성된 [Packet]만 잘라낸다.
        ///
        /// 예:
        /// [receiveBuffer] = [패킷1][패킷2 앞부분]
        /// -> 패킷1만 반환
        /// -> 패킷2 앞부분은 [receiveBuffer]에 그대로 남겨둠
        /// </summary>
        public List<byte[]> ExtractPackets(List<byte> receiveBuffer)
        {
            List<byte[]> packets = new List<byte[]>();

            while (true)
            {
                // 1. [STX] 위치 찾기
                int stxIndex = receiveBuffer.IndexOf(Stx);

                // [STX]가 아예 없으면 쓰레기 데이터이므로 비움
                if (stxIndex < 0)
                {
                    receiveBuffer.Clear();
                    break;
                }

                // [STX] 앞에 쓰레기 [byte]가 있으면 제거
                if (stxIndex > 0)
                {
                    receiveBuffer.RemoveRange(0, stxIndex);
                }

                // [Header] + [Checksum] + [ETX]도 아직 안 들어왔으면 대기
                if (receiveBuffer.Count < HeaderSize + ChecksumSize + EtxSize)
                {
                    break;
                }

                // 2. [SIZE] 3자리 읽기
                string sizeText = Encoding.ASCII.GetString(
                    receiveBuffer.GetRange(3, 3).ToArray());

                int payloadSize;

                // [SIZE]가 숫자가 아니면 비정상 [Packet]으로 보고 [STX] 하나 제거 후 재탐색
                if (!int.TryParse(sizeText, out payloadSize))
                {
                    receiveBuffer.RemoveAt(0);
                    continue;
                }

                // 3. 전체 [Packet] 크기 계산
                int packetSize =
                    HeaderSize +
                    payloadSize +
                    ChecksumSize +
                    EtxSize;

                // 아직 [Packet] 전체가 다 안 들어왔으면 다음 수신까지 대기
                if (receiveBuffer.Count < packetSize)
                {
                    break;
                }

                // 4. 마지막 [byte]가 [ETX]인지 확인
                if (receiveBuffer[packetSize - 1] != Etx)
                {
                    // [ETX] 위치가 안 맞으면 현재 [STX]가 잘못된 시작점일 수 있음
                    receiveBuffer.RemoveAt(0);
                    continue;
                }

                // 5. 완성 [Packet] 복사
                byte[] packet = receiveBuffer.GetRange(0, packetSize).ToArray();

                // 6. [Buffer]에서 잘라낸 [Packet] 제거
                receiveBuffer.RemoveRange(0, packetSize);

                packets.Add(packet);
            }
            return packets;
        }

        #endregion

        #region [Detection Packet Parse]

        /// <summary>
        /// [CMD 55] 탐지데이터 [Packet]을 [AiDetectionResult]로 변환한다.
        ///
        /// [CMD 55]가 아니면 [false] 반환.
        /// [Checksum]이 틀려도 [false] 반환.
        /// </summary>
        public bool TryParseDetectionPacket(
            byte[] packet,
            out AiDetectionResult result)
        {
            result = null;

            string command;
            string payload;
            bool checksumValid;

            // 1. 공통 [Packet] 구조 파싱
            if (!TryParsePacket(
                packet,
                out command,
                out payload,
                out checksumValid))
            {
                return false;
            }

            // 2. [Checksum] 검증
            if (!checksumValid)
            {
                Console.WriteLine("[AI PARSER] Checksum Invalid.");
                return false;
            }

            // 3. [CMD 55] 탐지데이터만 처리
            if (command != DetectCommand)
            {
                return false;
            }

            // 4. [Payload] 문자열을 탐지 결과 [Model]로 변환
            return TryParseDetectionPayload(payload, out result);
        }

        #endregion

        #region [Response Payload Parse]

        /// <summary>
        /// [CMD 52] [RTSP] 주소 목록 [Payload] 파싱
        /// 
        /// [Payload] 예:
        /// rtsp://... [US] rtsp://...
        /// </summary>
        public List<AiRtspInfo> ParseRtspListPayload(string payload)
        {
            List<AiRtspInfo> rtspList = new List<AiRtspInfo>();

            if (string.IsNullOrWhiteSpace(payload))
            {
                return rtspList;
            }

            string[] tokens = payload.Split(
                new[] { PayloadSeparator },
                StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length; i++)
            {
                rtspList.Add(
                    new AiRtspInfo
                    {
                        Index = i,
                        Url = tokens[i]
                    });
            }
            return rtspList;
        }

        /// <summary>
        /// [CMD 53] [ONNX] 목록 [Payload] 파싱
        /// 
        /// [Payload] 예:
        /// best.onnx:person,ship,car,drone [US] best_uav.onnx:uav
        /// </summary>
        public List<AiOnnxInfo> ParseOnnxListPayload(string payload)
        {
            List<AiOnnxInfo> onnxList = new List<AiOnnxInfo>();

            if (string.IsNullOrWhiteSpace(payload))
            {
                return onnxList;
            }

            string[] tokens = payload.Split(
                new[] { PayloadSeparator },
                StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length; i++)
            {
                string[] parts = tokens[i].Split(':');

                AiOnnxInfo info = new AiOnnxInfo
                {
                    Index = i,
                    FileName = parts[0]
                };

                if (parts.Length > 1)
                {
                    string[] classes = parts[1].Split(',');

                    foreach (string className in classes)
                    {
                        info.Classes.Add(className);
                    }

                }
                onnxList.Add(info);
            }
            return onnxList;
        }

        /// <summary>
        /// [CMD 54 / 56] [RTSP] / [ONNX] [Mapping] [Payload] 파싱
        /// 
        /// [Payload] 예:
        /// 0:1^0.10^0.45[US]1:0^0.10^0.45,1^0.10^0.45
        /// </summary>
        public List<AiMappingInfo> ParseMappingPayload(string payload)
        {
            List<AiMappingInfo> mappingList = new List<AiMappingInfo>();

            if (string.IsNullOrWhiteSpace(payload))
            {
                return mappingList;
            }

            string[] rtspTokens = payload.Split(
                new[] { PayloadSeparator },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string rtspToken in rtspTokens)
            {
                string[] rtspParts = rtspToken.Split(':');

                if (rtspParts.Length != 2)
                {
                    continue;
                }

                int rtspIndex;

                if (!int.TryParse(rtspParts[0], out rtspIndex))
                {
                    continue;
                }

                string[] mappingTokens = rtspParts[1].Split(',');

                foreach (string mappingToken in mappingTokens)
                {
                    string[] values = mappingToken.Split('^');

                    if (values.Length != 3)
                    {
                        continue;
                    }

                    int onnxIndex;
                    double confidence;
                    double iou;

                    if (!int.TryParse(values[0], out onnxIndex))
                    {
                        continue;
                    }

                    if (!double.TryParse(
                        values[1],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out confidence))
                    {
                        continue;
                    }

                    if (!double.TryParse(
                        values[2],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out iou))
                    {
                        continue;
                    }

                    mappingList.Add(
                        new AiMappingInfo
                        {
                            RtspIndex = rtspIndex,
                            OnnxIndex = onnxIndex,
                            Confidence = confidence,
                            Iou = iou
                        });

                }

            }
            return mappingList;
        }

        #endregion

        #region [Common Packet Parse]

        /// <summary>
        /// [AI] [Packet] 공통 구조를 외부에서 파싱한다.
        /// 
        /// [CMD 55] 탐지데이터 외에도
        /// [CMD 51] / [CMD 52] / [CMD 53] / [CMD 54] / [CMD 56]
        /// 응답 [Packet]을 구분하기 위해 사용한다.
        /// 
        /// 내부적으로 [TryParsePacket()]을 호출하여
        /// [STX] / [CMD] / [SIZE] / [Payload] / [Checksum] / [ETX] 검증을 수행한다.
        /// </summary>
        /// <param name="packet">수신된 [AI] [Packet]</param>
        /// <param name="command">[Packet] [CMD]</param>
        /// <param name="payload">[Packet] [Payload]</param>
        /// <returns>정상 [Packet] 여부</returns>
        public bool TryParseCommonPacket(
            byte[] packet,
            out string command,
            out string payload)
        {
            bool checksumValid;

            if (!TryParsePacket(
                packet,
                out command,
                out payload,
                out checksumValid))
            {
                return false;
            }

            if (!checksumValid)
            {
                Console.WriteLine("[AI PARSER] Checksum Invalid.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// [AI] [Packet] 공통 구조를 파싱한다.
        ///
        /// 여기서는 [CMD], [Payload], [Checksum] 검증 결과만 꺼낸다.
        /// 실제 [CMD]별 [Payload] 해석은 별도 함수에서 처리한다.
        /// </summary>
        private bool TryParsePacket(
            byte[] packet,
            out string command,
            out string payload,
            out bool checksumValid)
        {
            command = string.Empty;
            payload = string.Empty;
            checksumValid = false;

            if (packet == null ||
                packet.Length < HeaderSize + ChecksumSize + EtxSize)
            {
                return false;
            }

            // [STX] 확인
            if (packet[0] != Stx)
            {
                return false;
            }

            // [ETX] 확인
            if (packet[packet.Length - 1] != Etx)
            {
                return false;
            }

            // [CMD] 추출
            command = Encoding.ASCII.GetString(packet, 1, 2);

            // [SIZE] 추출
            string sizeText = Encoding.ASCII.GetString(packet, 3, 3);

            int payloadSize;

            if (!int.TryParse(sizeText, out payloadSize))
            {
                return false;
            }

            // 문서 기준 전체 [Packet] 길이 재검증
            int expectedPacketSize =
                HeaderSize +
                payloadSize +
                ChecksumSize +
                EtxSize;

            if (packet.Length != expectedPacketSize)
            {
                return false;
            }

            // [Payload] [byte] 추출
            byte[] payloadBytes = new byte[payloadSize];

            Array.Copy(
                packet,
                HeaderSize,
                payloadBytes,
                0,
                payloadSize);

            payload = Encoding.UTF8.GetString(payloadBytes);

            // 수신 [Checksum] 위치:
            // [HeaderSize] + [PayloadSize]
            byte receivedChecksum =
                packet[HeaderSize + payloadSize];

            byte calculatedChecksum =
                CalculateChecksum(command, payloadBytes);

            checksumValid = receivedChecksum == calculatedChecksum;

            return true;
        }

        #endregion

        #region [Checksum]

        /// <summary>
        /// [Checksum] 계산
        ///
        /// 문서 기준:
        /// [Checksum] = [CMD] [ASCII] [byte] + [Payload] [byte] 전체 합산
        /// [byte] 자료형이므로 자동으로 하위 1[byte]만 유지된다.
        /// </summary>
        private byte CalculateChecksum(
            string command,
            byte[] payloadBytes)
        {
            byte sum = 0;

            byte[] commandBytes =
                Encoding.ASCII.GetBytes(command);

            for (int i = 0; i < commandBytes.Length; i++)
            {
                sum += commandBytes[i];
            }

            for (int i = 0; i < payloadBytes.Length; i++)
            {
                sum += payloadBytes[i];
            }
            return sum;
        }

        #endregion

        #region [Detection Payload Parse]

        /// <summary>
        /// [CMD 55] 탐지데이터 [Payload] 파싱
        ///
        /// [Payload] 구조:
        /// [FrameTime] [InferenceMs] [RtspIndex] [DetectionCount]
        /// [ObjectId] [ClassIndex] [Confidence] [Left] [Top] [Right] [Bottom] ...
        ///
        /// [Payload] 내부 파라미터는
        /// [US] [Unit Separator] [0x1F]
        /// 기준으로 구분된다.
        ///
        /// 객체가 1개면 7개 필드가 한 번,
        /// 객체가 2개면 7개 필드가 두 번 반복된다.
        /// </summary>
        private bool TryParseDetectionPayload(
            string payload,
            out AiDetectionResult result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            /// <summary>
            /// [Payload] 내부 파라미터 분리
            /// 
            /// [AI Detector Agent] [Payload]는
            /// [US] [Unit Separator] [0x1F] 기준으로 구분한다.
            /// </summary>
            string[] tokens = payload.Split(
                new[] { PayloadSeparator },
                StringSplitOptions.RemoveEmptyEntries);

            // 최소 [4]개는 있어야 한다.
            // [FrameTime] / [InferenceMs] / [RtspIndex] / [DetectionCount]
            if (tokens.Length < 4)
            {
                return false;
            }

            int index = 0;

            long frameTime;
            int inferenceMs;
            int rtspIndex;
            int detectionCount;

            if (!long.TryParse(tokens[index++], out frameTime))
            {
                return false;
            }

            if (!int.TryParse(tokens[index++], out inferenceMs))
            {
                return false;
            }

            if (!int.TryParse(tokens[index++], out rtspIndex))
            {
                return false;
            }

            if (!int.TryParse(tokens[index++], out detectionCount))
            {
                return false;
            }

            // 객체 [1]개당 [7]개 필드
            int expectedTokenCount = 4 + detectionCount * 7;

            if (tokens.Length < expectedTokenCount)
            {
                Console.WriteLine(
                    "[AI PARSER] Payload token count invalid. " +
                    $"Token:{tokens.Length}, Expected:{expectedTokenCount}");

                return false;
            }

            AiDetectionResult detectionResult =
                new AiDetectionResult
                {
                    FrameTime = frameTime,
                    InferenceMs = inferenceMs,
                    RtspIndex = rtspIndex,
                    DetectionCount = detectionCount
                };

            for (int i = 0; i < detectionCount; i++)
            {
                long objectId;
                int classIndex;
                double confidence;
                int left;
                int top;
                int right;
                int bottom;

                if (!long.TryParse(tokens[index++], out objectId))
                {
                    return false;
                }

                if (!int.TryParse(tokens[index++], out classIndex))
                {
                    return false;
                }

                if (!double.TryParse(
                    tokens[index++],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out confidence))
                {
                    return false;
                }

                if (!int.TryParse(tokens[index++], out left))
                {
                    return false;
                }

                if (!int.TryParse(tokens[index++], out top))
                {
                    return false;
                }

                if (!int.TryParse(tokens[index++], out right))
                {
                    return false;
                }

                if (!int.TryParse(tokens[index++], out bottom))
                {
                    return false;
                }

                detectionResult.Boxes.Add(
                    new AiDetectionBox
                    {
                        ObjectId = objectId,
                        ClassIndex = classIndex,
                        Confidence = confidence,
                        Left = left,
                        Top = top,
                        Right = right,
                        Bottom = bottom
                    });
            }
            result = detectionResult;
            return true;
        }
        #endregion
    }

}
