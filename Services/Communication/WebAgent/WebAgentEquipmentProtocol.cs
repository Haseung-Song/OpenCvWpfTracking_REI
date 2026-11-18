using System;

namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>2026-11-03 정비: 장비 온도 응답과 와이퍼 입력을 검증한다. 기준 규격은 프로토콜 2.93이다.</summary>
    public static class WebAgentEquipmentProtocol
    {
        /// <summary>시간 구동은 1~255초, 정지·연속 구동은 시간값 0만 허용한다.</summary>
        public static bool IsWiperRequestValid(int mode, int seconds)
        {
            if (mode == 1)
            {
                return seconds >= 1 && seconds <= 255;
            }
            else
            {
                return (mode == 2 || mode == 4) && seconds == 0;
            }
        }

        /// <summary>예약 비트·표본 시각·센서 유효성을 검증하며 실제 0도를 보존한다.</summary>
        public static bool TryTemperature(byte[] payload, out TemperatureSnapshot snapshot)
        {
            snapshot = null;

            if (payload == null || payload.Length != 22 || payload[0] != 1 || payload[1] != 5 || (payload[2] & 0xE0) != 0 || (payload[3] & 0xF0) != 0 || (payload[3] & ~payload[2]) != 0)
            {
                return false;
            }

            ulong sample = BitConverter.ToUInt64(payload, 14);

            if (sample > 253402300799999UL)
            {
                return false;
            }

            if (sample == 0)
            {
                for (int index = 2; index < payload.Length; index++)
                {
                    if (payload[index] != 0)
                    {
                        return false;
                    }
                }
            }

            var values = new double?[4];

            for (int index = 0; index < values.Length; index++)
            {
                if ((payload[3] & (1 << index)) != 0)
                {
                    values[index] = BitConverter.ToInt16(payload, 4 + index * 2) / 100.0;
                }
            }

            snapshot = new TemperatureSnapshot(values, sample, BitConverter.ToInt16(payload, 12));

            return true;
        }
    }

    /// <summary>수신 표본을 불변 상태로 보관하며 이전 센서값을 새 표본에 섞지 않는다.</summary>
    public sealed class TemperatureSnapshot
    {
        private readonly double?[] _values;

        public ulong SampleTime { get; }

        public short ReservedRaw { get; }

        internal TemperatureSnapshot(double?[] values, ulong sampleTime, short reservedRaw)
        {
            _values = (double?[])values.Clone();

            SampleTime = sampleTime;
            ReservedRaw = reservedRaw;
        }

        public double? Value(int index)
        {
            return _values[index];
        }
    }
}
