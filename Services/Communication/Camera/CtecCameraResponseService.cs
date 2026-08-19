using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// [EO] 주간 카메라 [XV-Z4850HC] [CTEC] 응답 수신 서비스
    ///
    /// 카메라 웹 설정의 [Serial Port #1]에서
    /// [TCP Access Enable] 및 [Port 9000]을 활성화한 뒤 사용한다.
    ///
    /// 연결 구조:
    /// OpenCvWpfTracking
    /// -> [Camera IP : 9000] TCP Client 연결
    /// -> 카메라가 송신하는 [0x99 0x55 ... 0xFF] 응답 Packet 수신
    ///
    /// 주의:
    /// - CGI 명령 송신은 [CtecCameraCommandService]가 담당한다.
    /// - 본 서비스는 카메라 Protocol 응답 수신만 담당한다.
    /// - 카메라 웹 설정에서 Port가 변경된 경우 ResponsePort 값도 동일하게 변경해야 한다.
    /// </summary>
    public sealed class CtecCameraResponseService
    {
        #region [Constants]

        /// <summary>
        /// CTEC Camera Response Header
        /// </summary>
        private const byte ResponseHeader1 = 0x99;
        private const byte ResponseHeader2 = 0x55;

        /// <summary>
        /// CTEC Camera Response 종료 Byte
        /// </summary>
        private const byte ResponseEnd = 0xFF;

        /// <summary>
        /// CTEC Camera Response 고정 Packet 길이
        ///
        /// 구조:
        /// 99 55 CMD 00 DATA1 DATA2 FF
        ///
        /// DATA1 또는 DATA2에도 0xFF가 들어올 수 있으므로
        /// 종료 Byte 검색 방식이 아니라 고정 7 Byte 기준으로 분리한다.
        /// </summary>
        private const int ResponsePacketLength = 7;

        /// <summary>
        /// TCP 연결 제한시간
        /// </summary>
        private const int ConnectTimeoutMs = 3000;

        /// <summary>
        /// 자동 재연결 대기시간
        /// </summary>
        private const int ReconnectDelayMs = 3000;

        #endregion

        #region [Fields]

        /// <summary>
        /// 카메라 응답 수신 TCP Client
        /// </summary>
        private TcpClient _tcpClient;

        /// <summary>
        /// TCP 수신 Stream
        /// </summary>
        private NetworkStream _networkStream;

        /// <summary>
        /// 응답 수신 및 자동 재연결 Loop 종료 Token
        /// </summary>
        private CancellationTokenSource _receiveCts;

        /// <summary>
        /// 현재 연결 유지 요청 상태
        ///
        /// true이면 연결이 끊겨도 자동 재연결을 수행한다.
        /// Stop() 호출 시 false로 변경한다.
        /// </summary>
        private volatile bool _isConnectionRequested;

        /// <summary>
        /// 현재 연결 대상 카메라 IP
        /// </summary>
        private string _cameraIp;

        /// <summary>
        /// 현재 연결 대상 TCP 응답 Port
        /// </summary>
        private int _responsePort;

        /// <summary>
        /// 연결 / 해제 동시 호출 보호
        /// </summary>
        private readonly SemaphoreSlim _connectionLock =
            new SemaphoreSlim(1, 1);

        /// <summary>
        /// Zoom / Focus Position 응답 대기 작업 보호
        /// </summary>
        private readonly object _positionWaitLock =
            new object();

        /// <summary>
        /// 다음 Zoom Position 응답 대기 작업
        /// </summary>
        private TaskCompletionSource<int> _zoomPositionWaitSource;

        /// <summary>
        /// 다음 Focus Position 응답 대기 작업
        /// </summary>
        private TaskCompletionSource<int> _focusPositionWaitSource;

        #endregion

        #region [Events]

        /// <summary>
        /// 완성된 CTEC Camera Response Packet 수신 이벤트
        /// </summary>
        public event Action<byte[]> PacketReceived;

        /// <summary>
        /// TCP 연결 상태 변경 이벤트
        ///
        /// 전달 문자열:
        /// Disconnected / Connecting / Connected / Reconnecting
        /// </summary>
        public event Action<string> ConnectionStatusChanged;

        #endregion

        #region [Properties]

        /// <summary>
        /// 현재 CTEC Response TCP 연결 여부
        /// </summary>
        public bool IsConnected =>
            _tcpClient != null &&
            _tcpClient.Connected &&
            _networkStream != null;

        #endregion

        #region [Public Methods]

        /// <summary>
        /// 카메라 [TCP Response Port] 연결 및 수신 Loop 시작
        ///
        /// 동일 카메라에 이미 연결되어 있으면 중복 연결하지 않는다.
        /// 다른 카메라 또는 Port로 변경된 경우 기존 연결을 종료한 뒤 새로 시작한다.
        /// </summary>
        public async Task StartAsync(
            string cameraIp,
            int responsePort)
        {
            if (string.IsNullOrWhiteSpace(
                    cameraIp))
            {
                Console.WriteLine(
                    "[CTEC RESPONSE] Start Failed : Camera IP is empty");

                return;
            }

            if (responsePort < 1 ||
                responsePort > 65535)
            {
                Console.WriteLine(
                    $"[CTEC RESPONSE] Start Failed : Invalid Port {responsePort}");

                return;
            }

            await _connectionLock.WaitAsync();

            try
            {
                string normalizedIp =
                    cameraIp.Trim();

                if (_isConnectionRequested &&
                    string.Equals(
                        _cameraIp,
                        normalizedIp,
                        StringComparison.OrdinalIgnoreCase) &&
                    _responsePort == responsePort)
                {
                    return;
                }

                StopCore();

                _cameraIp =
                    normalizedIp;

                _responsePort =
                    responsePort;

                _isConnectionRequested =
                    true;

                _receiveCts =
                    new CancellationTokenSource();

                /*
                 * 장비 연결 버튼의 UI Thread를 점유하지 않도록
                 * TCP 연결 / 수신 / 재연결 Loop는 별도 Task에서 수행한다.
                 */
                _ = Task.Run(
                    () => RunConnectionLoopAsync(
                        _receiveCts.Token));
            }
            finally
            {
                _connectionLock.Release();
            }

        }

        /// <summary>
        /// 카메라 응답 TCP 연결 및 자동 재연결 종료
        /// </summary>
        public void Stop()
        {
            _connectionLock.Wait();

            try
            {
                StopCore();
            }
            finally
            {
                _connectionLock.Release();
            }

        }

        #endregion

        #region [Connection Loop]

        /// <summary>
        /// CTEC Response TCP 연결 / 수신 / 자동 재연결 Loop
        /// </summary>
        private async Task RunConnectionLoopAsync(
            CancellationToken cancellationToken)
        {
            bool isFirstAttempt =
                true;

            while (_isConnectionRequested &&
                   !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    RaiseConnectionStatus(
                        isFirstAttempt
                            ? "Connecting"
                            : "Reconnecting");

                    isFirstAttempt =
                        false;

                    bool connectResult =
                        await ConnectCoreAsync(
                            cancellationToken);

                    if (!connectResult)
                    {
                        await DelayReconnectAsync(
                            cancellationToken);

                        continue;
                    }

                    RaiseConnectionStatus(
                        "Connected");

                    await ReceiveLoopAsync(
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[CTEC RESPONSE] LOOP ERROR : {ex.Message}");

                    ConsoleLogHelper.PrintLine();
                }
                finally
                {
                    CloseSocket();
                }

                if (_isConnectionRequested &&
                    !cancellationToken.IsCancellationRequested)
                {
                    RaiseConnectionStatus(
                        "Reconnecting");

                    await DelayReconnectAsync(
                        cancellationToken);
                }

            }

            RaiseConnectionStatus(
                "Disconnected");
        }

        /// <summary>
        /// 카메라 IP의 TCP Response Port로 연결
        /// </summary>
        private async Task<bool> ConnectCoreAsync(
            CancellationToken cancellationToken)
        {
            CloseSocket();

            TcpClient tcpClient =
                new TcpClient();

            try
            {
                Task connectTask =
                    tcpClient.ConnectAsync(
                        _cameraIp,
                        _responsePort);

                Task completedTask =
                    await Task.WhenAny(
                        connectTask,
                        Task.Delay(
                            ConnectTimeoutMs,
                            cancellationToken));

                if (completedTask !=
                    connectTask)
                {
                    tcpClient.Close();

                    Console.WriteLine(
                        $"[CTEC RESPONSE] CONNECT TIMEOUT : " +
                        $"{_cameraIp}:{_responsePort}");

                    ConsoleLogHelper.PrintLine();

                    return false;
                }

                await connectTask;

                _tcpClient =
                    tcpClient;

                _networkStream =
                    tcpClient.GetStream();

                Console.WriteLine();
                Console.WriteLine(
                    $"[CTEC RESPONSE] CONNECTED : " +
                    $"{_cameraIp}:{_responsePort}");

                ConsoleLogHelper.PrintLine();

                return true;
            }
            catch (Exception ex)
            {
                tcpClient.Close();

                Console.WriteLine(
                    $"[CTEC RESPONSE] CONNECT FAILED : " +
                    $"{_cameraIp}:{_responsePort} / {ex.Message}");

                ConsoleLogHelper.PrintLine();

                return false;
            }

        }

        #endregion

        #region [Position Response Wait]

        /// <summary>
        /// [EO Zoom] 다음 TCP 9000 Position 응답을 기다린다.
        ///
        /// 반드시 CGI Inquiry 송신 전에 호출해야 한다.
        /// 그래야 빠르게 도착한 응답도 놓치지 않는다.
        /// </summary>
        public Task<int?> WaitForNextZoomPositionAsync(
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            return WaitForNextPositionAsync(
                true,
                timeoutMs,
                cancellationToken);
        }

        /// <summary>
        /// [EO Focus] 다음 TCP 9000 Position 응답을 기다린다.
        /// </summary>
        public Task<int?> WaitForNextFocusPositionAsync(
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            return WaitForNextPositionAsync(
                false,
                timeoutMs,
                cancellationToken);
        }

        /// <summary>
        /// Position 응답 대기 작업 생성 및 Timeout 처리
        ///
        /// 동일 종류의 이전 대기 작업이 남아 있으면 취소하고
        /// 항상 가장 최근 Inquiry 한 건만 대기한다.
        /// </summary>
        private async Task<int?> WaitForNextPositionAsync(
            bool isZoom,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            TaskCompletionSource<int> waitSource =
                new TaskCompletionSource<int>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_positionWaitLock)
            {
                TaskCompletionSource<int> previousSource =
                    isZoom
                        ? _zoomPositionWaitSource
                        : _focusPositionWaitSource;

                previousSource?.TrySetCanceled();

                if (isZoom)
                {
                    _zoomPositionWaitSource = waitSource;
                }
                else
                {
                    _focusPositionWaitSource = waitSource;
                }

            }

            using (CancellationTokenSource timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken))
            {
                timeoutCts.CancelAfter(
                    Math.Max(1, timeoutMs));

                using (timeoutCts.Token.Register(
                    () => waitSource.TrySetCanceled()))
                {
                    try
                    {
                        return await waitSource.Task;
                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }
                    finally
                    {
                        lock (_positionWaitLock)
                        {
                            if (isZoom &&
                                ReferenceEquals(
                                    _zoomPositionWaitSource,
                                    waitSource))
                            {
                                _zoomPositionWaitSource = null;
                            }
                            else if (!isZoom &&
                                     ReferenceEquals(
                                         _focusPositionWaitSource,
                                         waitSource))
                            {
                                _focusPositionWaitSource = null;
                            }

                        }

                    }

                }

            }

        }

        /// <summary>
        /// 수신된 Position을 현재 대기 중인 Inquiry에 전달한다.
        /// </summary>
        private void CompletePositionWait(
            byte commandCode,
            int position)
        {
            TaskCompletionSource<int> waitSource = null;

            lock (_positionWaitLock)
            {
                if (commandCode == 0x47)
                {
                    waitSource = _zoomPositionWaitSource;
                    _zoomPositionWaitSource = null;
                }
                else if (commandCode == 0x48)
                {
                    waitSource = _focusPositionWaitSource;
                    _focusPositionWaitSource = null;
                }

            }

            waitSource?.TrySetResult(
                position);
        }

        /// <summary>
        /// 연결 종료 시 남아 있는 Position 대기 작업을 취소한다.
        /// </summary>
        private void CancelPositionWaits()
        {
            TaskCompletionSource<int> zoomSource;
            TaskCompletionSource<int> focusSource;

            lock (_positionWaitLock)
            {
                zoomSource = _zoomPositionWaitSource;
                focusSource = _focusPositionWaitSource;

                _zoomPositionWaitSource = null;
                _focusPositionWaitSource = null;
            }

            zoomSource?.TrySetCanceled();
            focusSource?.TrySetCanceled();
        }

        #endregion

        #region [Receive / Packet Parsing]

        /// <summary>
        /// 카메라 CTEC Response 수신 Loop
        ///
        /// TCP는 Packet 경계를 보장하지 않으므로
        /// 수신 Byte를 누적한 뒤 [0x99 0x55] Header와
        /// [0xFF] 종료 Byte를 기준으로 완성 Packet을 분리한다.
        /// </summary>
        private async Task ReceiveLoopAsync(
            CancellationToken cancellationToken)
        {
            byte[] receiveBuffer =
                new byte[1024];

            List<byte> packetBuffer =
                new List<byte>();

            while (_isConnectionRequested &&
                   !cancellationToken.IsCancellationRequested &&
                   IsConnected)
            {
                int readLength =
                    await _networkStream.ReadAsync(
                        receiveBuffer,
                        0,
                        receiveBuffer.Length,
                        cancellationToken);

                if (readLength <= 0)
                {
                    Console.WriteLine(
                        "[CTEC RESPONSE] REMOTE CLOSED");

                    ConsoleLogHelper.PrintLine();

                    break;
                }

                for (int index = 0;
                     index < readLength;
                     index++)
                {
                    packetBuffer.Add(
                        receiveBuffer[index]);
                }

                ExtractPackets(
                    packetBuffer);
            }

        }

        /// <summary>
        /// 누적 Buffer에서 완성된 CTEC Response Packet 추출
        /// </summary>
        private void ExtractPackets(
            List<byte> packetBuffer)
        {
            while (packetBuffer.Count >= 2)
            {
                int headerIndex =
                    FindHeaderIndex(
                        packetBuffer);

                if (headerIndex < 0)
                {
                    bool keepLastHeaderByte =
                        packetBuffer[packetBuffer.Count - 1] ==
                        ResponseHeader1;

                    packetBuffer.Clear();

                    if (keepLastHeaderByte)
                    {
                        packetBuffer.Add(
                            ResponseHeader1);
                    }

                    return;
                }

                if (headerIndex > 0)
                {
                    packetBuffer.RemoveRange(
                        0,
                        headerIndex);
                }

                /*
                 * TCP 수신이 6 Byte + 1 Byte처럼 분할될 수 있으므로
                 * 정확히 7 Byte가 누적될 때까지 기다린다.
                 */
                if (packetBuffer.Count <
                    ResponsePacketLength)
                {
                    return;
                }

                /*
                 * 7번째 Byte가 종료값이 아니면 현재 Header를 잘못 잡은 것이다.
                 * 첫 Byte만 제거한 뒤 다음 Header를 다시 검색한다.
                 */
                if (packetBuffer[ResponsePacketLength - 1] !=
                    ResponseEnd)
                {
                    packetBuffer.RemoveAt(0);
                    continue;
                }

                byte[] packet =
                    packetBuffer.GetRange(
                        0,
                        ResponsePacketLength)
                    .ToArray();

                packetBuffer.RemoveRange(
                    0,
                    ResponsePacketLength);

                OnPacketReceived(
                    packet);
            }

        }

        /// <summary>
        /// CTEC Response Header 시작 위치 검색
        /// </summary>
        private int FindHeaderIndex(
            List<byte> packetBuffer)
        {
            for (int index = 0;
                 index < packetBuffer.Count - 1;
                 index++)
            {
                if (packetBuffer[index] ==
                        ResponseHeader1 &&
                    packetBuffer[index + 1] ==
                        ResponseHeader2)
                {
                    return index;
                }

            }
            return -1;
        }

        /// <summary>
        /// 완성 CTEC Response Packet 로그 및 이벤트 전달
        /// </summary>
        private void OnPacketReceived(
            byte[] packet)
        {
            string packetHex =
                BitConverter
                    .ToString(packet)
                    .Replace("-", " ");

            Console.WriteLine();
            Console.WriteLine(
                $"[CTEC RESPONSE] RX : {packetHex}");

            ConsoleLogHelper.PrintLine();

            if (packet.Length == ResponsePacketLength &&
                (packet[2] == 0x47 ||
                 packet[2] == 0x48))
            {
                int position =
                    (packet[4] << 8) |
                     packet[5];

                CompletePositionWait(
                    packet[2],
                    position);
            }

            PacketReceived?.Invoke(
                packet);
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// 자동 재연결 대기
        /// </summary>
        private async Task DelayReconnectAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(
                    ReconnectDelayMs,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

        }

        /// <summary>
        /// 연결 상태 이벤트 전달
        /// </summary>
        private void RaiseConnectionStatus(
            string status)
        {
            ConnectionStatusChanged?.Invoke(
                status);
        }

        /// <summary>
        /// 연결 요청 종료 및 Socket 정리
        /// </summary>
        private void StopCore()
        {
            CancelPositionWaits();

            _isConnectionRequested =
                false;

            try
            {
                _receiveCts?.Cancel();
            }
            catch
            {
            }

            _receiveCts?.Dispose();
            _receiveCts =
                null;

            CloseSocket();

            _cameraIp =
                null;

            _responsePort =
                0;

            RaiseConnectionStatus(
                "Disconnected");
        }

        /// <summary>
        /// TCP Client / NetworkStream 안전 해제
        /// </summary>
        private void CloseSocket()
        {
            try
            {
                _networkStream?.Close();
            }
            catch
            {
            }

            try
            {
                _tcpClient?.Close();
            }
            catch
            {
            }

            _networkStream =
                null;

            _tcpClient =
                null;
        }
        #endregion
    }

}
