using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// [EO] 주간 카메라 [XV-Z4850HC] [CTEC CGI] 직접 제어 서비스
    ///
    /// 기존 [Control Agent TCP]는 [Pan / Tilt] 및 [IR] 제어에 계속 사용한다.
    /// [옥상 GOP EO]의 [Zoom / Focus]만 카메라 [HTTP / HTTPS CGI]로 직접 송신한다.
    ///
    /// CGI 형식:
    /// http://[Camera IP]/api/ptz.cgi
    /// ?PTZNumber=1
    /// &Transparent=[CTEC Packet Hex]
    /// &SerialPort=1
    ///
    /// 주의:
    /// - HTTP 응답 성공은 CGI 요청 전달 성공 여부만 의미한다.
    /// - 카메라 제어 응답 Packet은 문서 기준 별도 [TCP/IP Port 9000]으로 수신한다.
    /// - 현재 단계에서는 Zoom / Focus 명령 송신 기능만 구현한다.
    /// </summary>
    public sealed class CtecCameraCommandService
    {
        #region [Constants]

        /// <summary>
        /// [CTEC] 고정 Header / Camera Code
        /// </summary>
        private const byte Header1 = 0xFF;
        private const byte Header2 = 0x01;
        private const byte CameraCode1 = 0x44;
        private const byte CameraCode2 = 0x77;

        /// <summary>
        /// [Zoom / Focus] 연속 제어 속도 범위
        /// </summary>
        private const byte MinimumControlSpeed = 1;
        private const byte MaximumControlSpeed = 7;

        /// <summary>
        /// [XV-Z4850HC] EO Optical Zoom Direct Position 최대값
        ///
        /// VISCA Zoom Direct 명령 범위:
        /// 0x0000 ~ 0x4000
        /// </summary>
        private const ushort MaximumZoomDirectPosition =
            0x4000;

        /// <summary>
        /// [XV-Z4850HC] EO Focus Direct Position 최대값
        ///
        /// VISCA Focus Direct 명령 범위:
        /// 0x0000 ~ 0x8000
        /// </summary>
        private const ushort MaximumFocusDirectPosition =
            0x8000;

        /// <summary>
        /// [Focus Mode Manual] 적용 후
        /// 실제 Near / Far 명령 송신 전 대기시간
        /// </summary>
        private const int FocusManualApplyDelayMs = 100;

        /// <summary>
        /// 카메라 CGI 요청 제한시간
        /// </summary>
        private const int RequestTimeoutSeconds = 3;

        /// <summary>
        /// Console에 출력할 CGI 응답 본문 최대 길이
        /// </summary>
        private const int MaximumResponseLogLength = 500;

        #endregion

        #region [Fields]

        /// <summary>
        /// CTEC 명령 순차 송신 보호
        ///
        /// MouseDown 직후 MouseUp이 빠르게 발생하더라도
        /// Start 명령보다 Stop 명령이 먼저 전송되지 않도록
        /// Zoom / Focus CGI 요청을 한 번에 하나씩 처리한다.
        /// </summary>
        private readonly SemaphoreSlim _controlSendLock =
            new SemaphoreSlim(1, 1);

        /// <summary>
        /// [CTEC Inquiry] 상태 조회 명령 순차 송신 보호
        ///
        /// Zoom / Focus Start 및 Stop과 분리된 Lock을 사용한다.
        /// 따라서 Position Inquiry가 진행 중이어도
        /// MouseUp에서 발생한 Stop 명령은 조회 완료를 기다리지 않고 송신할 수 있다.
        /// </summary>
        private readonly SemaphoreSlim _inquirySendLock =
            new SemaphoreSlim(1, 1);

        /// <summary>
        /// [Digest Authentication] Nonce Count
        ///
        /// 동일 Nonce에 대한 요청 순번을 8자리 Hex 문자열로 생성할 때 사용한다.
        /// 카메라 CGI 요청마다 새로운 Challenge를 먼저 받으므로,
        /// 실제 인증 요청의 nc 값은 일반적으로 00000001부터 시작한다.
        /// </summary>
        private int _digestNonceCount;

        #endregion

        #region [Zoom Continuous Control]

        /// <summary>
        /// [EO] 주간 카메라 [Zoom Tele] 연속 제어 시작
        ///
        /// [P = 2] : Tele
        /// [Q = 1 ~ 7] : Speed
        /// </summary>
        public Task<bool> StartZoomTeleAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            byte speed = MaximumControlSpeed)
        {
            speed = ClampControlSpeed(
                speed);

            return ExecuteControlLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO ZOOM TELE",
                    0x81,
                    0x01,
                    0x04,
                    0x07,
                    (byte)(0x20 | speed),
                    0xFF));
        }

        /// <summary>
        /// [EO] 주간 카메라 [Zoom Wide] 연속 제어 시작
        /// </summary>
        public Task<bool> StartZoomWideAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            byte speed = MaximumControlSpeed)
        {
            speed = ClampControlSpeed(
                speed);

            return ExecuteControlLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO ZOOM WIDE",
                    0x81,
                    0x01,
                    0x04,
                    0x07,
                    (byte)(0x30 | speed),
                    0xFF));
        }

        /// <summary>
        /// [EO] 주간 카메라 [Zoom] 연속 제어 정지
        /// </summary>
        public Task<bool> StopZoomAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps)
        {
            return ExecuteControlLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO ZOOM STOP",
                    0x81,
                    0x01,
                    0x04,
                    0x07,
                    0x00,
                    0xFF));
        }

        #endregion

        #region [Zoom Direct Position Control]

        /// <summary>
        /// [EO] 주간 카메라 Optical Zoom 목표 위치 직접 이동
        ///
        /// 연속 TELE / WIDE 이동 후 Stop 시점을 추정하는 방식이 아니라,
        /// 카메라에 최종 Zoom Raw Position을 한 번에 지정한다.
        ///
        /// VISCA:
        /// 81 01 04 47 0P 0Q 0R 0S FF
        ///
        /// Position 범위:
        /// 0x0000 = Wide
        /// 0x4000 = Tele
        ///
        /// 전달되는 Position은 4개의 4bit Nibble로 분리한다.
        /// </summary>
        public Task<bool> MoveZoomPositionAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            ushort position)
        {
            ushort safePosition =
                (ushort)Math.Min(
                    MaximumZoomDirectPosition,
                    position);

            byte positionNibble1 =
                (byte)((safePosition >> 12) & 0x0F);

            byte positionNibble2 =
                (byte)((safePosition >> 8) & 0x0F);

            byte positionNibble3 =
                (byte)((safePosition >> 4) & 0x0F);

            byte positionNibble4 =
                (byte)(safePosition & 0x0F);

            return ExecuteControlLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    $"EO ZOOM DIRECT / {safePosition}",
                    0x81,
                    0x01,
                    0x04,
                    0x47,
                    positionNibble1,
                    positionNibble2,
                    positionNibble3,
                    positionNibble4,
                    0xFF));
        }

        #endregion

        #region [Focus Direct Position Control]

        /// <summary>
        /// [EO] 주간 카메라 Focus 목표 위치 직접 이동
        ///
        /// Focus Mode를 Manual로 적용한 뒤
        /// 최종 Focus Raw Position을 한 번에 지정한다.
        ///
        /// VISCA:
        /// 81 01 04 48 0P 0Q 0R 0S FF
        ///
        /// Position 범위:
        /// 0x0000 = Far
        /// 0x8000 = Near
        ///
        /// 전달되는 Position은 4개의 4bit Nibble로 분리한다.
        /// </summary>
        public Task<bool> MoveFocusPositionAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            ushort position)
        {
            ushort safePosition =
                (ushort)Math.Min(
                    MaximumFocusDirectPosition,
                    position);

            byte positionNibble1 =
                (byte)((safePosition >> 12) & 0x0F);

            byte positionNibble2 =
                (byte)((safePosition >> 8) & 0x0F);

            byte positionNibble3 =
                (byte)((safePosition >> 4) & 0x0F);

            byte positionNibble4 =
                (byte)(safePosition & 0x0F);

            return ExecuteControlLockedAsync(
                async () =>
                {
                    bool manualResult =
                        await SendFocusManualCoreAsync(
                            cameraIp,
                            userName,
                            password,
                            useHttps);

                    if (!manualResult)
                    {
                        return false;
                    }

                    await Task.Delay(
                        FocusManualApplyDelayMs);

                    return await SendCameraCommandCoreAsync(
                        cameraIp,
                        userName,
                        password,
                        useHttps,
                        $"EO FOCUS DIRECT / {safePosition}",
                        0x81,
                        0x01,
                        0x04,
                        0x48,
                        positionNibble1,
                        positionNibble2,
                        positionNibble3,
                        positionNibble4,
                        0xFF);
                });
        }

        #endregion

        #region [Focus Continuous Control]

        /// <summary>
        /// [EO] 주간 카메라 [Focus Near] 연속 제어 시작
        ///
        /// 문서 기준 Focus 제어 전
        /// Focus Mode Manual 명령을 먼저 송신한다.
        /// </summary>
        public Task<bool> StartFocusNearAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            byte speed = MaximumControlSpeed)
        {
            speed = ClampControlSpeed(
                speed);

            return ExecuteControlLockedAsync(
                async () =>
                {
                    bool manualResult =
                        await SendFocusManualCoreAsync(
                            cameraIp,
                            userName,
                            password,
                            useHttps);

                    if (!manualResult)
                    {
                        return false;
                    }

                    await Task.Delay(
                        FocusManualApplyDelayMs);

                    return await SendCameraCommandCoreAsync(
                        cameraIp,
                        userName,
                        password,
                        useHttps,
                        "EO FOCUS NEAR",
                        0x81,
                        0x01,
                        0x04,
                        0x08,
                        (byte)(0x30 | speed),
                        0xFF);
                });
        }

        /// <summary>
        /// [EO] 주간 카메라 [Focus Far] 연속 제어 시작
        ///
        /// 문서 기준 Focus 제어 전
        /// Focus Mode Manual 명령을 먼저 송신한다.
        /// </summary>
        public Task<bool> StartFocusFarAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            byte speed = MaximumControlSpeed)
        {
            speed = ClampControlSpeed(
                speed);

            return ExecuteControlLockedAsync(
                async () =>
                {
                    bool manualResult =
                        await SendFocusManualCoreAsync(
                            cameraIp,
                            userName,
                            password,
                            useHttps);

                    if (!manualResult)
                    {
                        return false;
                    }

                    await Task.Delay(
                        FocusManualApplyDelayMs);

                    return await SendCameraCommandCoreAsync(
                        cameraIp,
                        userName,
                        password,
                        useHttps,
                        "EO FOCUS FAR",
                        0x81,
                        0x01,
                        0x04,
                        0x08,
                        (byte)(0x20 | speed),
                        0xFF);
                });
        }

        /// <summary>
        /// [EO] 주간 카메라 [Focus] 연속 제어 정지
        /// </summary>
        public Task<bool> StopFocusAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps)
        {
            return ExecuteControlLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO FOCUS STOP",
                    0x81,
                    0x01,
                    0x04,
                    0x08,
                    0x00,
                    0xFF));
        }

        /// <summary>
        /// [EO] 주간 카메라 [One Push Focus] 요청
        /// </summary>
        public Task<bool> OnePushFocusAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps)
        {
            return ExecuteControlLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO ONE PUSH FOCUS",
                    0x81,
                    0x01,
                    0x04,
                    0x18,
                    0x01,
                    0xFF));
        }

        /// <summary>
        /// [EO] 주간 카메라 [Focus Mode]를 [Manual]로 설정
        /// </summary>
        private Task<bool> SendFocusManualCoreAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps)
        {
            return SendCameraCommandCoreAsync(
                cameraIp,
                userName,
                password,
                useHttps,
                "EO FOCUS MODE MANUAL",
                0x81,
                0x01,
                0x04,
                0x38,
                0x03,
                0xFF);
        }

        #endregion

        #region [Camera Inquiry Commands]

        /// <summary>
        /// [EO] 주간 카메라 현재 [Optical Zoom Position] 조회
        ///
        /// CGI로 Inquiry Packet을 송신한 뒤,
        /// 실제 응답은 [CtecCameraResponseService]가
        /// 카메라 [TCP Port 9000]에서 수신한다.
        ///
        /// Response:
        /// 0x99 0x55 0x47 0x00 P1 P2 0xFF
        /// </summary>
        public Task<bool> RequestZoomPositionAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps)
        {
            return ExecuteInquiryLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO ZOOM POSITION INQUIRY",
                    0x81,
                    0x09,
                    0x04,
                    0x47,
                    0xFF));
        }

        /// <summary>
        /// [EO] 주간 카메라 현재 [Focus Position] 조회
        ///
        /// Response:
        /// 0x99 0x55 0x48 0x00 P1 P2 0xFF
        /// </summary>
        public Task<bool> RequestFocusPositionAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps)
        {
            return ExecuteInquiryLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO FOCUS POSITION INQUIRY",
                    0x81,
                    0x09,
                    0x04,
                    0x48,
                    0xFF));
        }

        /// <summary>
        /// [EO] 주간 카메라 현재 [Focus Mode] 조회
        ///
        /// Response:
        /// 0x99 0x55 0x38 0x00 0x00 P1 0xFF
        ///
        /// P1:
        /// 0x02 = Auto
        /// 0x03 = Manual
        /// </summary>
        public Task<bool> RequestFocusModeAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps)
        {
            return ExecuteInquiryLockedAsync(
                () => SendCameraCommandCoreAsync(
                    cameraIp,
                    userName,
                    password,
                    useHttps,
                    "EO FOCUS MODE INQUIRY",
                    0x81,
                    0x09,
                    0x04,
                    0x38,
                    0xFF));
        }

        #endregion

        #region [CGI / Packet Methods]

        /// <summary>
        /// CTEC 명령 송신 순서 보호 실행
        /// </summary>
        private async Task<bool> ExecuteControlLockedAsync(
            Func<Task<bool>> action)
        {
            await _controlSendLock.WaitAsync();

            try
            {
                return await action();
            }
            finally
            {
                _controlSendLock.Release();
            }

        }

        /// <summary>
        /// [CTEC Inquiry] 상태 조회 명령 순서 보호 실행
        ///
        /// 제어 명령 Lock과 분리되어 있으므로
        /// 실행 중인 Inquiry가 Stop 명령 송신을 막지 않는다.
        /// </summary>
        private async Task<bool> ExecuteInquiryLockedAsync(
            Func<Task<bool>> action)
        {
            await _inquirySendLock.WaitAsync();

            try
            {
                return await action();
            }
            finally
            {
                _inquirySendLock.Release();
            }

        }

        /// <summary>
        /// [CTEC] 외부 Frame에 [VISCA Protocol]을 결합한 뒤
        /// 카메라 [HTTP / HTTPS CGI]로 직접 송신한다.
        /// </summary>
        private async Task<bool> SendCameraCommandCoreAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            string commandName,
            params byte[] viscaProtocol)
        {
            if (string.IsNullOrWhiteSpace(
                    cameraIp))
            {
                Console.WriteLine(
                    "[CTEC CGI] Send Failed : Camera IP is empty");

                return false;
            }

            if (viscaProtocol == null ||
                viscaProtocol.Length == 0 ||
                viscaProtocol.Length > byte.MaxValue)
            {
                Console.WriteLine(
                    "[CTEC CGI] Send Failed : Invalid VISCA Protocol");

                return false;
            }

            byte[] packet =
                BuildPacket(
                    viscaProtocol);

            return await SendCgiRequestAsync(
                cameraIp.Trim(),
                userName,
                password,
                useHttps,
                commandName,
                packet);
        }

        /// <summary>
        /// [CTEC] 최종 Packet을 카메라 [CGI] 주소의
        /// [Transparent] Parameter로 전달한다.
        ///
        /// HTTP / HTTPS 선택은 RTSP 프리셋의 UseHttps 값으로 결정한다.
        ///
        /// 옥상 GOP 카메라는 실제 웹 설정이 HTTPS이므로
        /// HTTP Redirect에 의존하지 않고 HTTPS 주소로 직접 요청한다.
        /// </summary>
        private async Task<bool> SendCgiRequestAsync(
            string cameraIp,
            string userName,
            string password,
            bool useHttps,
            string commandName,
            byte[] packet)
        {
            string packetHex =
                BitConverter
                    .ToString(packet)
                    .Replace(
                        "-",
                        string.Empty);

            string scheme =
                useHttps
                    ? "https"
                    : "http";

            /*
             * Digest HA2 계산에는 전체 URL이 아니라
             * Path + Query 형태의 Request-URI가 사용된다.
             *
             * curl --digest에서 실제 동작이 확인된 URI와
             * 동일한 문자열을 생성해야 한다.
             */
            string requestPath =
                "/api/ptz.cgi" +
                "?PTZNumber=1" +
                "&Transparent=" +
                packetHex +
                "&SerialPort=1";

            string requestUrl =
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}://{1}{2}",
                    scheme,
                    cameraIp,
                    requestPath);

            Uri requestUri =
                new Uri(
                    requestUrl,
                    UriKind.Absolute);

            Console.WriteLine();
            Console.WriteLine(
                $"[CTEC CGI] {commandName}");

            Console.WriteLine(
                $"[CTEC CGI] TARGET : {scheme}://{cameraIp}");

            Console.WriteLine(
                $"[CTEC CGI] REQUEST URI : {requestUri.AbsoluteUri}");

            Console.WriteLine(
                "[CTEC CGI] AUTH : Manual Digest MD5");

            Console.WriteLine(
                $"[CTEC CGI] PACKET : {packetHex}");

            ConsoleLogHelper.PrintLine();

            try
            {
                using (HttpClientHandler handler =
                    CreateHttpClientHandler())
                using (HttpClient client =
                    new HttpClient(
                        handler,
                        true))
                {
                    client.Timeout =
                        TimeSpan.FromSeconds(
                            RequestTimeoutSeconds);

                    /*
                     * 1차 요청:
                     * Authorization Header 없이 요청하여
                     * 카메라의 401 Digest Challenge를 수신한다.
                     */
                    using (HttpRequestMessage challengeRequest =
                        CreateRequestMessage(
                            requestUri))
                    using (HttpResponseMessage challengeResponse =
                        await client
                            .SendAsync(
                                challengeRequest,
                                HttpCompletionOption.ResponseContentRead)
                            .ConfigureAwait(false))
                    {
                        if (challengeResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine(
                                $"[CTEC CGI] RESULT : " +
                                $"{(int)challengeResponse.StatusCode} " +
                                $"{challengeResponse.StatusCode}");

                            Console.WriteLine(
                                "[CTEC CGI] SEND RESULT : True");

                            ConsoleLogHelper.PrintLine();

                            return true;
                        }

                        if (challengeResponse.StatusCode !=
                            HttpStatusCode.Unauthorized)
                        {
                            string challengeErrorText =
                                challengeResponse.Content == null
                                    ? string.Empty
                                    : await challengeResponse.Content
                                        .ReadAsStringAsync()
                                        .ConfigureAwait(false);

                            Console.WriteLine(
                                $"[CTEC CGI] CHALLENGE RESULT : " +
                                $"{(int)challengeResponse.StatusCode} " +
                                $"{challengeResponse.StatusCode}");

                            LogResponseSummary(
                                challengeErrorText);

                            Console.WriteLine(
                                "[CTEC CGI] SEND RESULT : False");

                            ConsoleLogHelper.PrintLine();

                            return false;
                        }

                        string digestChallenge =
                            GetMd5DigestChallenge(
                                challengeResponse);

                        if (string.IsNullOrWhiteSpace(
                                digestChallenge))
                        {
                            Console.WriteLine(
                                "[CTEC CGI] FAILED : " +
                                "MD5 Digest Challenge Not Found");

                            Console.WriteLine(
                                "[CTEC CGI] CHECK : " +
                                "WWW-Authenticate Header");

                            ConsoleLogHelper.PrintLine();

                            return false;
                        }

                        string authorizationValue =
                            CreateDigestAuthorization(
                                "GET",
                                requestPath,
                                userName,
                                password,
                                digestChallenge);

                        if (string.IsNullOrWhiteSpace(
                                authorizationValue))
                        {
                            Console.WriteLine(
                                "[CTEC CGI] FAILED : " +
                                "Digest Authorization Build Failed");

                            ConsoleLogHelper.PrintLine();

                            return false;
                        }

                        /*
                         * 2차 요청:
                         * MD5 Digest 값을 직접 계산하여
                         * Authorization Header에 포함한 뒤 실제 명령을 송신한다.
                         */
                        using (HttpRequestMessage authenticatedRequest =
                            CreateRequestMessage(
                                requestUri))
                        {
                            authenticatedRequest.Headers
                                .TryAddWithoutValidation(
                                    "Authorization",
                                    authorizationValue);

                            using (HttpResponseMessage authenticatedResponse =
                                await client
                                    .SendAsync(
                                        authenticatedRequest,
                                        HttpCompletionOption.ResponseContentRead)
                                    .ConfigureAwait(false))
                            {
                                string responseText =
                                    authenticatedResponse.Content == null
                                        ? string.Empty
                                        : await authenticatedResponse.Content
                                            .ReadAsStringAsync()
                                            .ConfigureAwait(false);

                                string contentType =
                                    authenticatedResponse.Content?.Headers
                                        ?.ContentType?.MediaType ??
                                    string.Empty;

                                Console.WriteLine(
                                    $"[CTEC CGI] RESULT : " +
                                    $"{(int)authenticatedResponse.StatusCode} " +
                                    $"{authenticatedResponse.StatusCode}");

                                if (!string.IsNullOrWhiteSpace(
                                        contentType))
                                {
                                    Console.WriteLine(
                                        $"[CTEC CGI] CONTENT TYPE : {contentType}");
                                }

                                LogResponseSummary(
                                    responseText);

                                if (authenticatedResponse.StatusCode ==
                                    HttpStatusCode.Unauthorized)
                                {
                                    Console.WriteLine(
                                        "[CTEC CGI] FAILED : " +
                                        "Digest Authentication Failed");

                                    Console.WriteLine(
                                        "[CTEC CGI] CHECK : " +
                                        "Camera User Name / Password / CGI Permission");

                                    Console.WriteLine(
                                        "[CTEC CGI] SEND RESULT : False");

                                    ConsoleLogHelper.PrintLine();

                                    return false;
                                }

                                bool isCameraLoginPage =
                                    IsCameraLoginPage(
                                        responseText);

                                if (isCameraLoginPage)
                                {
                                    Console.WriteLine(
                                        "[CTEC CGI] FAILED : " +
                                        "Camera Login / Redirect Page Response");

                                    Console.WriteLine(
                                        "[CTEC CGI] SEND RESULT : False");

                                    ConsoleLogHelper.PrintLine();

                                    return false;
                                }

                                if (!authenticatedResponse.IsSuccessStatusCode)
                                {
                                    Console.WriteLine(
                                        "[CTEC CGI] FAILED : HTTP Request Error");

                                    Console.WriteLine(
                                        "[CTEC CGI] SEND RESULT : False");

                                    ConsoleLogHelper.PrintLine();

                                    return false;
                                }

                                Console.WriteLine(
                                    "[CTEC CGI] NOTE : " +
                                    "Camera Protocol ACK is received through TCP Port 9000");

                                Console.WriteLine(
                                    "[CTEC CGI] SEND RESULT : True");

                                ConsoleLogHelper.PrintLine();

                                return true;
                            }

                        }

                    }

                }

            }
            catch (TaskCanceledException)
            {
                Console.WriteLine(
                    "[CTEC CGI] ERROR : Request Timeout");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(
                    $"[CTEC CGI] HTTP ERROR : {ex.Message}");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[CTEC CGI] ERROR : {ex.Message}");

                ConsoleLogHelper.PrintLine();

                return false;
            }

        }

        /// <summary>
        /// 카메라 CGI 요청용 HttpClientHandler 생성
        ///
        /// Digest 인증은 수동으로 처리하므로 Credentials를 설정하지 않는다.
        /// 폐쇄망 실장비의 자체 서명 인증서는 현장 시험을 위해 허용한다.
        /// </summary>
        private HttpClientHandler CreateHttpClientHandler()
        {
            HttpClientHandler handler =
                new HttpClientHandler
                {
                    AllowAutoRedirect =
                        false,

                    UseDefaultCredentials =
                        false
                };

            handler.ServerCertificateCustomValidationCallback =
                (request, certificate, chain, sslPolicyErrors) =>
                    true;

            return handler;
        }

        /// <summary>
        /// 카메라 CGI GET 요청 Message 생성
        /// </summary>
        private HttpRequestMessage CreateRequestMessage(
            Uri requestUri)
        {
            HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUri);

            request.Headers
                .TryAddWithoutValidation(
                    "User-Agent",
                    "OpenCvWpfTracking/1.0");

            request.Headers
                .TryAddWithoutValidation(
                    "Accept",
                    "*/*");

            return request;
        }

        /// <summary>
        /// 카메라가 반환한 Digest Challenge 중
        /// [algorithm=MD5] 항목을 선택한다.
        ///
        /// XV-Z4850HC는 MD5와 SHA-256 Challenge를 동시에 반환하므로,
        /// curl --digest로 검증된 MD5 항목을 우선 사용한다.
        /// </summary>
        private string GetMd5DigestChallenge(
            HttpResponseMessage response)
        {
            IEnumerable<string> values;

            if (!response.Headers.TryGetValues(
                    "WWW-Authenticate",
                    out values))
            {
                return null;
            }

            string[] challenges =
                values
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value))
                    .ToArray();

            string md5Challenge =
                challenges.FirstOrDefault(
                    value =>
                        value.IndexOf(
                            "Digest",
                            StringComparison.OrdinalIgnoreCase) >= 0 &&
                        Regex.IsMatch(
                            value,
                            @"algorithm\s*=\s*""?MD5""?(?:,|$)",
                            RegexOptions.IgnoreCase));

            if (!string.IsNullOrWhiteSpace(
                    md5Challenge))
            {
                return md5Challenge;
            }

            return challenges.FirstOrDefault(
                value =>
                    value.IndexOf(
                        "Digest",
                        StringComparison.OrdinalIgnoreCase) >= 0 &&
                    value.IndexOf(
                        "SHA-256",
                        StringComparison.OrdinalIgnoreCase) < 0);
        }

        /// <summary>
        /// HTTP Digest MD5 Authorization Header 생성
        ///
        /// qop=auth 계산식:
        /// HA1 = MD5(username:realm:password)
        /// HA2 = MD5(method:uri)
        /// response = MD5(HA1:nonce:nc:cnonce:qop:HA2)
        /// </summary>
        private string CreateDigestAuthorization(
            string method,
            string requestPath,
            string userName,
            string password,
            string challenge)
        {
            string realm =
                GetDigestValue(
                    challenge,
                    "realm");

            string nonce =
                GetDigestValue(
                    challenge,
                    "nonce");

            string qop =
                GetDigestValue(
                    challenge,
                    "qop");

            string opaque =
                GetDigestValue(
                    challenge,
                    "opaque");

            if (string.IsNullOrWhiteSpace(
                    realm) ||
                string.IsNullOrWhiteSpace(
                    nonce))
            {
                return null;
            }

            /*
             * qop 값이 "auth,auth-int" 형태로 오는 경우에도
             * 현재 GET 요청에 맞는 auth 항목을 선택한다.
             */
            qop =
                SelectDigestQop(
                    qop);

            Interlocked.Exchange(
                ref _digestNonceCount,
                0);

            int nonceCount =
                Interlocked.Increment(
                    ref _digestNonceCount);

            string nc =
                nonceCount.ToString(
                    "x8",
                    CultureInfo.InvariantCulture);

            string cnonce =
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(
                        0,
                        16);

            string normalizedUserName =
                userName ??
                string.Empty;

            string normalizedPassword =
                password ??
                string.Empty;

            string ha1 =
                ComputeMd5(
                    normalizedUserName +
                    ":" +
                    realm +
                    ":" +
                    normalizedPassword);

            string ha2 =
                ComputeMd5(
                    method +
                    ":" +
                    requestPath);

            string digestResponse;

            if (!string.IsNullOrWhiteSpace(
                    qop))
            {
                digestResponse =
                    ComputeMd5(
                        ha1 +
                        ":" +
                        nonce +
                        ":" +
                        nc +
                        ":" +
                        cnonce +
                        ":" +
                        qop +
                        ":" +
                        ha2);
            }
            else
            {
                digestResponse =
                    ComputeMd5(
                        ha1 +
                        ":" +
                        nonce +
                        ":" +
                        ha2);
            }

            StringBuilder authorization =
                new StringBuilder();

            authorization.Append(
                "Digest ");

            authorization.Append(
                "username=\"");
            authorization.Append(
                EscapeDigestQuotedValue(
                    normalizedUserName));
            authorization.Append(
                "\", ");

            authorization.Append(
                "realm=\"");
            authorization.Append(
                EscapeDigestQuotedValue(
                    realm));
            authorization.Append(
                "\", ");

            authorization.Append(
                "nonce=\"");
            authorization.Append(
                EscapeDigestQuotedValue(
                    nonce));
            authorization.Append(
                "\", ");

            authorization.Append(
                "uri=\"");
            authorization.Append(
                EscapeDigestQuotedValue(
                    requestPath));
            authorization.Append(
                "\", ");

            authorization.Append(
                "algorithm=MD5, ");

            authorization.Append(
                "response=\"");
            authorization.Append(
                digestResponse);
            authorization.Append(
                "\"");

            if (!string.IsNullOrWhiteSpace(
                    qop))
            {
                authorization.Append(
                    ", qop=");
                authorization.Append(
                    qop);

                authorization.Append(
                    ", nc=");
                authorization.Append(
                    nc);

                authorization.Append(
                    ", cnonce=\"");
                authorization.Append(
                    cnonce);
                authorization.Append(
                    "\"");
            }

            if (!string.IsNullOrWhiteSpace(
                    opaque))
            {
                authorization.Append(
                    ", opaque=\"");
                authorization.Append(
                    EscapeDigestQuotedValue(
                        opaque));
                authorization.Append(
                    "\"");
            }

            return authorization.ToString();
        }

        /// <summary>
        /// Digest Challenge 문자열에서 지정 Parameter 값 추출
        /// </summary>
        private string GetDigestValue(
            string challenge,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    challenge) ||
                string.IsNullOrWhiteSpace(
                    parameterName))
            {
                return null;
            }

            Match quotedMatch =
                Regex.Match(
                    challenge,
                    @"(?:^|[,\s])" +
                    Regex.Escape(parameterName) +
                    @"\s*=\s*""(?<value>[^""]*)""",
                    RegexOptions.IgnoreCase);

            if (quotedMatch.Success)
            {
                return quotedMatch
                    .Groups["value"]
                    .Value;
            }

            Match tokenMatch =
                Regex.Match(
                    challenge,
                    @"(?:^|[,\s])" +
                    Regex.Escape(parameterName) +
                    @"\s*=\s*(?<value>[^,\s]+)",
                    RegexOptions.IgnoreCase);

            return tokenMatch.Success
                ? tokenMatch.Groups["value"].Value
                : null;
        }

        /// <summary>
        /// Digest qop 목록에서 auth 항목 선택
        /// </summary>
        private string SelectDigestQop(
            string qopValue)
        {
            if (string.IsNullOrWhiteSpace(
                    qopValue))
            {
                return null;
            }

            string[] qopItems =
                qopValue
                    .Split(',')
                    .Select(item =>
                        item.Trim())
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(item))
                    .ToArray();

            string authQop =
                qopItems.FirstOrDefault(
                    item =>
                        string.Equals(
                            item,
                            "auth",
                            StringComparison.OrdinalIgnoreCase));

            return authQop ??
                   qopItems.FirstOrDefault();
        }

        /// <summary>
        /// Digest 인증용 MD5 Hash 계산
        /// </summary>
        private string ComputeMd5(
            string value)
        {
            using (MD5 md5 =
                MD5.Create())
            {
                byte[] inputBytes =
                    Encoding.UTF8.GetBytes(
                        value ??
                        string.Empty);

                byte[] hashBytes =
                    md5.ComputeHash(
                        inputBytes);

                StringBuilder builder =
                    new StringBuilder(
                        hashBytes.Length * 2);

                foreach (byte hashByte in
                    hashBytes)
                {
                    builder.Append(
                        hashByte.ToString(
                            "x2",
                            CultureInfo.InvariantCulture));
                }
                return builder.ToString();
            }

        }

        /// <summary>
        /// Digest Header의 따옴표 문자열 Escape 처리
        /// </summary>
        private string EscapeDigestQuotedValue(
            string value)
        {
            return (value ?? string.Empty)
                .Replace(
                    "\\",
                    "\\\\")
                .Replace(
                    "\"",
                    "\\\"");
        }

        /// <summary>
        /// Console 과다 출력을 방지하기 위해
        /// CGI 응답 본문의 앞부분만 출력한다.
        /// </summary>
        private void LogResponseSummary(
            string responseText)
        {
            if (string.IsNullOrWhiteSpace(
                    responseText))
            {
                return;
            }

            string normalizedResponse =
                responseText
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();

            if (normalizedResponse.Length >
                MaximumResponseLogLength)
            {
                normalizedResponse =
                    normalizedResponse.Substring(
                        0,
                        MaximumResponseLogLength) +
                    "...";
            }

            Console.WriteLine(
                $"[CTEC CGI] RESPONSE : {normalizedResponse}");
        }

        /// <summary>
        /// CGI 응답이 실제 제어 결과가 아니라
        /// 카메라 로그인 / 초기 설정 페이지인지 확인한다.
        /// </summary>
        private bool IsCameraLoginPage(
            string responseText)
        {
            if (string.IsNullOrWhiteSpace(
                    responseText))
            {
                return false;
            }

            return responseText.IndexOf(
                       "Page Redirection",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   responseText.IndexOf(
                       "admin_password_status",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   responseText.IndexOf(
                       "/viewer/viewer.html",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// [CTEC] 최종 송신 [Packet] 생성
        ///
        /// Packet 구성:
        /// [0] Header 1
        /// [1] Header 2
        /// [2] Camera Code 1
        /// [3] Camera Code 2
        /// [4] VISCA Data Length
        /// [5 ~ n] VISCA Protocol
        /// [n + 1] Checksum
        /// </summary>
        private byte[] BuildPacket(
            byte[] viscaProtocol)
        {
            byte[] packet =
                new byte[viscaProtocol.Length + 6];

            packet[0] = Header1;
            packet[1] = Header2;
            packet[2] = CameraCode1;
            packet[3] = CameraCode2;
            packet[4] =
                (byte)viscaProtocol.Length;

            Buffer.BlockCopy(
                viscaProtocol,
                0,
                packet,
                5,
                viscaProtocol.Length);

            packet[packet.Length - 1] =
                CalculateChecksum(
                    packet,
                    1,
                    packet.Length - 2);

            return packet;
        }

        /// <summary>
        /// [CTEC] Checksum 계산
        ///
        /// 문서 기준 Byte 2부터
        /// 마지막 VISCA Data까지 모두 더한 뒤 0xFF Mask 처리한다.
        /// </summary>
        private byte CalculateChecksum(
            byte[] data,
            int startIndex,
            int length)
        {
            int sum = 0;

            for (int i = startIndex;
                 i < startIndex + length;
                 i++)
            {
                sum += data[i];
            }

            return (byte)(sum & 0xFF);
        }

        #endregion

        #region [Value Helpers]

        /// <summary>
        /// Zoom / Focus 속도를 문서 허용 범위 [1 ~ 7]로 보정한다.
        /// </summary>
        private byte ClampControlSpeed(
            byte speed)
        {
            if (speed < MinimumControlSpeed)
            {
                return MinimumControlSpeed;
            }

            if (speed > MaximumControlSpeed)
            {
                return MaximumControlSpeed;
            }

            return speed;
        }
        #endregion
    }

}
