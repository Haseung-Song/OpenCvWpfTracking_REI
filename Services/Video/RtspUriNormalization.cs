using System;

namespace OpenCvWpfTracking.Services.Video
{
    /// <summary>RTSP 인증정보만 정규화하며 호스트·경로·쿼리의 이스케이프는 그대로 유지한다.</summary>
    public static class RtspUriNormalization
    {
        /// <summary>
        /// RTSP 주소의 사용자 이름·비밀번호 부분을 인코딩하거나 표시용으로 복원한다.
        /// </summary>
        private static string Transform(string input, bool encode)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            int scheme = input.IndexOf("://", StringComparison.Ordinal);

            if (scheme < 0 || !(input.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) || input.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase)))
            {
                return input;
            }

            int start = scheme + 3;

            // 원문 비밀번호의 #, ?, /, @를 일반 URI 파서보다 먼저 분리한다.
            int pathStart = input.IndexOf('/', start);

            int authorityEnd = pathStart < 0 ? input.Length : pathStart;

            int at = input.LastIndexOf('@', authorityEnd - 1, authorityEnd - start);

            if (at < start)
            {
                return input;
            }

            string userInfo = input.Substring(start, at - start);

            int colon = userInfo.IndexOf(':');

            string user = colon < 0 ? userInfo : userInfo.Substring(0, colon);

            string password = colon < 0 ? null : userInfo.Substring(colon + 1);

            Func<string, string> component = value =>
            {
                string decoded = Uri.UnescapeDataString(value);

                if (encode)
                {
                    return Uri.EscapeDataString(decoded).Replace("!", "%21");
                }
                else
                {
                    return decoded;
                }
            };

            return input.Substring(0, start) + component(user) + (password == null ? "" : ":" + component(password)) + input.Substring(at);
        }

        /// <summary>
        /// RTSP 인증 정보의 인코딩 문자를 입력·표시용 형태로 복원한다.
        /// </summary>
        public static string Display(string input)
        {
            return Transform(input, false);
        }

        /// <summary>
        /// RTSP 사용자 이름·비밀번호를 연결용으로 인코딩한 주소를 반환한다.
        /// </summary>
        public static string Connect(string input)
        {
            return Transform(input, true);
        }
    }
}
