using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// [WEB AGENT](Local Agent) 수신 데이터를
    /// [TORUSS] [12byte] 응답 [Packet] 단위로 분리 / 검증하는 [Parser] 클래스
    ///
    /// TCP는 Message 단위 통신이 아니므로 다음 형태가 모두 발생할 수 있다.
    ///
    /// 1. 하나의 12byte Packet이 여러 번으로 분할 수신
    /// 2. 여러 개의 12byte Packet이 한 번에 합쳐져 수신
    /// 3. Packet 앞에 불필요한 byte가 포함되어 수신
    ///
    /// 따라서 ReadAsync에서 전달된 byte[] 하나를
    /// 완전한 Packet이라고 가정하지 않고 내부 Buffer에 누적한 뒤,
    /// Header(0xFF) 기준으로 정확히 12byte씩 분리한다.
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
        private const int PacketSize =
            12;

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
        /// 완성된 12byte Packet만 반환한다.
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
                    /// Header부터 12byte가 아직 모이지 않은 경우
                    /// 다음 TCP 수신까지 Buffer를 유지한다.
                    /// </summary>
                    if (_receiveBuffer.Count <
                        PacketSize)
                    {
                        break;
                    }

                    byte[] packet =
                        _receiveBuffer
                            .GetRange(
                                0,
                                PacketSize)
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
                            PacketSize);

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
        /// 문서 기준:
        /// [Checksum] = packet[1] ~ packet[10] byte 합산값
        /// [packet[11] = Checksum]
        /// </summary>
        private bool ValidateChecksum(
            byte[] packet)
        {
            if (packet == null ||
                packet.Length != PacketSize)
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
                 index <= 10;
                 index++)
            {
                unchecked
                {
                    sum +=
                        packet[index];
                }

            }

            return sum ==
                   packet[11];
        }
        #endregion
    }

}
