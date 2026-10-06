using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// WebAgent(Local Agent) 수신 데이터를 Legacy 12Byte 또는
    /// 가변 길이 응답 Packet 단위로 분리하고 검증하는 Parser 클래스.
    ///
    /// TCP는 Message 단위 통신이 아니므로 다음 형태가 모두 발생할 수 있다.
    ///
    /// 1. 하나의 Packet이 여러 번으로 분할 수신
    /// 2. 여러 개의 Packet이 한 번에 합쳐져 수신
    /// 3. Packet 앞에 불필요한 byte가 포함되어 수신
    ///
    /// 따라서 ReadAsync에서 전달된 byte[] 하나를
    /// 완전한 Packet이라고 가정하지 않고 내부 Buffer에 누적한 뒤,
    /// Header(0xFF)와 Function을 확인하여 Legacy 12Byte 또는
    /// Function 0x23~0x2C의 Payload 길이 기반으로 분리한다.
    /// </summary>
    public class LAPacketParser
    {
        #region [Constants]

        /// <summary>
        /// [TORUSS] 응답 [Packet Header]
        /// </summary>
        private const byte Header =
            0xFF;

        /// <summary>
        /// [TORUSS] 응답 [Packet] 크기
        /// </summary>
        private const int LegacyPacketSize =
            12;

        // 2026-10-06: Function 0x31은 LEN 필드가 없는 고정 13Byte
        // PTZF + MCB/SCB 상태 응답이다.
        private const byte DevicePowerStatusFunction = 0x31;

        private const int DevicePowerStatusPacketSize = 13;

        private const byte VariableFunctionMinimum = 0x23;

        // 2026-10-02: GPS 자동 전송 주파수 설정/조회 응답(0x30)도
        // LEN 기반 가변 Packet으로 조립한다.
        private const byte VariableFunctionMaximum = 0x30;

        private const int VariableHeaderSize = 4;

        private const int MaximumPayloadLength = 4096;

        #endregion

        #region [Fields]

        /// <summary>
        /// [TCP] 분할 수신 Packet을 보관하는 누적 Buffer
        ///
        /// 예:
        /// 첫 번째 Receive  : FF 07 D6 03 E8
        /// 두 번째 Receive  : 03 00 00 00 00 00 CB
        ///
        /// 두 데이터를 누적한 뒤 12byte Packet으로 조립한다.
        /// </summary>
        private readonly List<byte> _receiveBuffer =
            new List<byte>();

        /// <summary>
        /// Parse / Reset 동시 호출 방지용 Lock
        /// </summary>
        private readonly object _bufferLock =
            new object();

        #endregion

        #region [Parse]

        /// <summary>
        /// 수신 byte[] 데이터를 내부 Buffer에 누적하고,
        /// 완성된 Legacy 또는 가변 길이 Packet만 반환한다.
        /// </summary>
        public List<LaResponsePacket> Parse(
            byte[] receivedData)
        {
            List<LaResponsePacket> packets =
                new List<LaResponsePacket>();

            if (receivedData == null ||
                receivedData.Length == 0)
            {
                return packets;
            }

            lock (_bufferLock)
            {
                _receiveBuffer.AddRange(
                    receivedData);

                while (true)
                {
                    int headerIndex =
                        FindHeaderIndex();

                    /// <summary>
                    /// Header가 없으면 현재 Buffer는 Packet으로 사용할 수 없다.
                    /// 다음 수신 데이터에 이전 쓰레기 byte를 연결하지 않도록 제거한다.
                    /// </summary>
                    if (headerIndex < 0)
                    {
                        _receiveBuffer.Clear();
                        break;
                    }

                    /// <summary>
                    /// Header 앞쪽에 불필요한 byte가 존재하면 제거한다.
                    /// </summary>
                    if (headerIndex > 0)
                    {
                        _receiveBuffer.RemoveRange(
                            0,
                            headerIndex);
                    }

                    /// <summary>
                    /// Header와 Function이 아직 모이지 않은 경우
                    /// 다음 TCP 수신까지 Buffer를 유지한다.
                    /// </summary>
                    if (_receiveBuffer.Count < 2)
                    {
                        break;
                    }

                    int packetSize = LegacyPacketSize;
                    byte function = _receiveBuffer[1];
                    if (function == DevicePowerStatusFunction)
                    {
                        packetSize = DevicePowerStatusPacketSize;
                    }
                    else if (function >= VariableFunctionMinimum &&
                        function <= VariableFunctionMaximum)
                    {
                        if (_receiveBuffer.Count < VariableHeaderSize)
                        {
                            break;
                        }

                        int payloadLength = _receiveBuffer[2] |
                                            _receiveBuffer[3] << 8;
                        if (payloadLength > MaximumPayloadLength)
                        {
                            _receiveBuffer.RemoveAt(0);
                            continue;
                        }

                        packetSize = VariableHeaderSize + payloadLength + 1;
                    }

                    if (_receiveBuffer.Count < packetSize)
                    {
                        break;
                    }

                    byte[] packet =
                        _receiveBuffer
                            .GetRange(
                                0,
                                packetSize)
                            .ToArray();

                    bool isValid =
                        ValidateChecksum(
                            packet);

                    if (isValid)
                    {
                        packets.Add(
                            new LaResponsePacket
                            {
                                RawData =
                                    packet,

                                IsValid =
                                    true
                            });

                        _receiveBuffer.RemoveRange(
                            0,
                            packetSize);

                        continue;
                    }

                    /// <summary>
                    /// Checksum이 맞지 않으면 현재 0xFF가 실제 Header가 아닐 수 있다.
                    ///
                    /// Buffer 전체를 버리지 않고 첫 byte만 제거한 뒤
                    /// 다음 0xFF 위치에서 다시 Packet 조립을 시도한다.
                    /// </summary>
                    packets.Add(
                        new LaResponsePacket
                        {
                            RawData =
                                packet,

                            IsValid =
                                false
                        });

                    _receiveBuffer.RemoveAt(
                        0);
                }

            }
            return packets;
        }

        /// <summary>
        /// 누적 Buffer에서 첫 번째 Header 위치 검색
        /// </summary>
        private int FindHeaderIndex()
        {
            for (int index = 0;
                 index < _receiveBuffer.Count;
                 index++)
            {
                if (_receiveBuffer[index] ==
                    Header)
                {
                    return index;
                }

            }
            return -1;
        }

        /// <summary>
        /// 연결 해제 또는 재연결 시
        /// 이전 연결에서 남은 분할 Packet 데이터를 제거한다.
        /// </summary>
        public void Reset()
        {
            lock (_bufferLock)
            {
                _receiveBuffer.Clear();
            }

        }

        #endregion

        #region [Checksum]

        /// <summary>
        /// [TORUSS] 응답 [Packet Checksum] 검증
        ///
        /// Checksum은 Function부터 마지막 Payload까지 합산한
        /// 하위 1Byte이며 Packet의 마지막 Byte와 비교한다.
        /// </summary>
        private bool ValidateChecksum(
            byte[] packet)
        {
            if (packet == null ||
                packet.Length < 3)
            {
                return false;
            }

            if (packet[0] !=
                Header)
            {
                return false;
            }

            byte sum =
                0;

            for (int index = 1;
                 index < packet.Length - 1;
                 index++)
            {
                unchecked
                {
                    sum +=
                        packet[index];
                }

            }

            return sum ==
                   packet[packet.Length - 1];
        }
        #endregion
    }

}
