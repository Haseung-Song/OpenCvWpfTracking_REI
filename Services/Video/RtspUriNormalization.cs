using System;

namespace OpenCvWpfTracking.Services.Video
{
    /// <summary>RTSP 인증정보만 정규화하며 호스트·경로·쿼리의 이스케이프는 그대로 유지한다.</summary>
    public static class RtspUriNormalization
    {
        private static string Transform(string input, bool encode)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            int scheme = input.IndexOf("://", StringComparison.Ordinal);
            if (scheme < 0 || !(input.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase)))
                return input;
            int start = scheme + 3;
            // 원문 비밀번호의 #, ?, /, @를 일반 URI 파서보다 먼저 분리한다.
            int pathStart = input.IndexOf('/', start);
            int authorityEnd = pathStart < 0 ? input.Length : pathStart;
            int at = input.LastIndexOf('@', authorityEnd - 1, authorityEnd - start);
            if (at < start)
                return input;
            string userInfo = input.Substring(start, at - start);
            int colon = userInfo.IndexOf(':');
            string user = colon < 0 ? userInfo : userInfo.Substring(0, colon);
            string password = colon < 0 ? null : userInfo.Substring(colon + 1);
            Func<string, string> component = value =>
            {
                string decoded = Uri.UnescapeDataString(value);
                return encode ? Uri.EscapeDataString(decoded).Replace("!", "%21") : decoded;
            };
            return input.Substring(0, start) + component(user) +
                (password == null ? "" : ":" + component(password)) + input.Substring(at);
        }

        public static string Display(string input) => Transform(input, false);

        public static string Connect(string input) => Transform(input, true);
    }
}
