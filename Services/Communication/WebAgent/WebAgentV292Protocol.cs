using System;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>정밀 이동과 자동 초점의 프레임 생성·검증만 담당한다. 장비에는 직접 송신하지 않는다.</summary>
    public static class WebAgentV292Protocol
    {
        /// <summary>
        /// 정밀 속도를 검증한 뒤 0.001도/초 단위의 정수로 변환한다.
        /// </summary>
        public static uint EncodeSpeed(double speed, double maximum)
        {
            if (double.IsNaN(speed) || double.IsInfinity(speed) ||
                double.IsNaN(maximum) || double.IsInfinity(maximum) ||
                maximum <= 0 || maximum > 90 || speed < 0.001 || speed > maximum)
                throw new ArgumentOutOfRangeException(nameof(speed), "속도는 장비 상한 이내의 0.001도/초 이상이어야 합니다.");

            double raw = Math.Round(speed * 1000, MidpointRounding.AwayFromZero);
            if (raw < 1 || raw > Math.Floor(maximum * 1000))
                throw new ArgumentOutOfRangeException(nameof(speed));

            return checked((uint)raw);
        }

        /// <summary>
        /// 위치와 정밀 속도를 한 프레임에 담는 절대 이동 패킷을 생성한다.
        /// </summary>
        public static byte[] Absolute(byte address, bool tilt, double guiAngle, double speed, double maximum)
        {
            if (double.IsNaN(guiAngle) || double.IsInfinity(guiAngle) || guiAngle < -180 || guiAngle > 180)
                throw new ArgumentOutOfRangeException(nameof(guiAngle));

            // 기존 화면의 팬 부호는 송신 경계에서 한 번만 반전한다. 틸트는 부호를 유지한다.
            double unsigned = ((tilt ? guiAngle : -guiAngle) % 360 + 360) % 360;
            int angle = (int)Math.Round(unsigned * 100, MidpointRounding.AwayFromZero) % 36000;
            var packet = new byte[11];
            packet[0] = 0xFF;
            packet[1] = address;
            packet[3] = tilt ? (byte)0xC3 : (byte)0xC1;
            packet[4] = (byte)(angle >> 8);
            packet[5] = (byte)angle;
            PutUInt32(packet, 6, EncodeSpeed(speed, maximum));
            Seal(packet);
            return packet;
        }

        /// <summary>
        /// 축별 정밀 속도와 방향 비트를 검증해 조그 패킷을 생성한다.
        /// </summary>
        public static byte[] Jog(byte address, byte direction, double panSpeed, double tiltSpeed, double maximum)
        {
            if ((direction & 0xE1) != 0 || (direction & 6) == 6 || (direction & 24) == 24)
                throw new ArgumentOutOfRangeException(nameof(direction));

            bool pan = (direction & 6) != 0;
            bool tilt = (direction & 24) != 0;
            if ((!pan && panSpeed != 0) || (!tilt && tiltSpeed != 0))
                throw new ArgumentOutOfRangeException(nameof(direction), "정지 축에는 속도를 지정할 수 없습니다.");

            var packet = new byte[13];
            packet[0] = 0xFF;
            packet[1] = address;
            packet[2] = direction;
            packet[3] = 0xC5;
            PutUInt32(packet, 4, pan ? EncodeSpeed(panSpeed, maximum) : 0);
            PutUInt32(packet, 8, tilt ? EncodeSpeed(tiltSpeed, maximum) : 0);
            Seal(packet);
            return packet;
        }

        /// <summary>
        /// 대상과 동작 값을 검증해 AF 조회·설정·단발 실행 패킷을 생성한다.
        /// </summary>
        public static byte[] AutoFocus(byte address, byte command, byte target, byte value)
        {
            if (target > 1 || (command != 0xC7 && command != 0xC9 && command != 0xCB) ||
                (command == 0xC7 ? value > 1 : value != 0))
                throw new ArgumentOutOfRangeException(nameof(command));

            var packet = new byte[] { 0xFF, address, 0, command, target, value, 0 };
            Seal(packet);
            return packet;
        }

        /// <summary>
        /// AF 응답 길이·버전·필드 범위를 확인하고 유효한 상태 객체로 변환한다.
        /// </summary>
        public static bool TryReadAutoFocus(byte[] payload, out AutoFocusSnapshot snapshot)
        {
            snapshot = null;
            if (payload == null || payload.Length != 8 || payload[0] != 1 || payload[1] > 1 ||
                payload[2] > 3 || payload[3] > 4 || payload[3] == 3 || (payload[4] & 0xC0) != 0 ||
                payload[5] > 2 || payload[6] > 3 || payload[7] > 5)
                return false;

            snapshot = new AutoFocusSnapshot(payload);
            return true;
        }

        /// <summary>
        /// 32비트 값을 프로토콜의 상위 바이트 우선 순서로 기록한다.
        /// </summary>
        private static void PutUInt32(byte[] packet, int offset, uint value)
        {
            for (int i = 3; i >= 0; i--)
            {
                packet[offset + i] = (byte)value;
                value >>= 8;
            }
        }

        /// <summary>
        /// 주소부터 데이터 끝까지의 합으로 패킷 체크섬을 채운다.
        /// </summary>
        private static void Seal(byte[] packet)
        {
            int sum = 0;
            for (int i = 1; i < packet.Length - 1; i++)
                sum += packet[i];
            packet[packet.Length - 1] = (byte)sum;
        }
    }

    public sealed class AutoFocusSnapshot
    {
        public byte Target { get; }

        public byte Operation { get; }

        public byte Result { get; }

        public byte Flags { get; }

        public byte Owner { get; }

        public byte Transport { get; }

        public byte State { get; }

        public bool OnePushSupported => Result == 0 && (Flags & 1) != 0;

        public bool ContinuousSupported => Result == 0 && (Flags & 2) != 0;

        public bool Running => Result == 0 && (State == 1 || (Flags & 8) != 0);

        public bool ConfirmedComplete => Result == 0 && (State == 3 || (Flags & 16) != 0);

        internal AutoFocusSnapshot(byte[] payload)
        {
            Target = payload[1];
            Operation = payload[2];
            Result = payload[3];
            Flags = payload[4];
            Owner = payload[5];
            Transport = payload[6];
            State = payload[7];
        }
    }
}
