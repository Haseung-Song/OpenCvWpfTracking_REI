using System;
using OpenCvWpfTracking.Services.Configuration;

namespace OpenCvWpfTracking.Services.Control
{
    /// <summary>2026-11-06: 광학 보정값을 IP 대신 고정 장비 ID와 통신 방식·채널로 구분한다.</summary>
    public static class OpticalDeviceIdentity
    {
        /// <summary>등록 장비의 고정 키를 반환하고 직접 입력 장비는 기존 키를 사용한다.</summary>
        public static string Key(string deviceId, string agentType, bool ir, string directKey)
        {
            // 직접 입력은 같은 실장비임을 확인할 ID가 없으므로 기존 주소 구분을 유지한다.
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return directKey;
            }

            return "DEVICE|" + (agentType ?? "").ToUpperInvariant() + "|" + DeviceCatalog.CanonicalDeviceId(deviceId).ToUpperInvariant() + "|" + (ir ? "IR" : "EO");
        }

        /// <summary>순번·설치 장소 접미사가 달라져도 같은 이름·채널의 과거 보정만 찾는다.</summary>
        public static bool MatchesLegacyName(string key, string displayName, bool ir)
        {
            string[] fields = (key ?? "").Split('|');

            return fields.Length == 4 && fields[3] == (ir ? "IR" : "EO") && string.Equals(DeviceCatalog.DisplayIdentity(fields[0]), DeviceCatalog.DisplayIdentity(displayName), StringComparison.OrdinalIgnoreCase);
        }
    }
}
