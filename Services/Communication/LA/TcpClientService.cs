using OpenCvWpfTracking.Common;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>
    /// [Web Agent / LA] 프로그램과 [TCP Client] 방식으로 통신하는 서비스
    ///
    /// 역할:
    /// 1. 제어 서버에 TCP 연결
    /// 2. Pelco-D 7Byte Packet 송신
    /// 3. 서버 응답 Packet 수신
    /// 4. 연결 종료 감지 및 상위 계층 재연결 요청
    /// 5. Disconnect 시 Socket / Stream / Token 리소스 정리
    /// </summary>
    public class TcpClientService
    {
        #region [Constants]

        /// <summary>
        /// [TCP] 연결 제한 시간 [ms]
        ///
        /// 장비 미기동 상태에서 UI가 오래 멈추지 않도록
        /// 기존 기본 Socket 대기 시간보다 짧게 제한한다.
        /// </summary>
        private const int CONNECT_TIMEOUT_MS =
            1500;

        /// <summary>
        /// [TCP] 송신 제한 시간 [ms]
        /// </summary>
        private const int SEND_TIMEOUT_MS =
            1500;

        #endregion

        #region [Fields]

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private CancellationTokenSource _cts;

        /// <summary>
        /// [TCP] 연결 / 송신 / 해제 동기화 객체
        /// </summary>
        private readonly object _socketLock =
            new object();

        /// <summary>
        /// 마지막 수신 Log 출력 시간
        /// </summary>
        private DateTime _lastRecvLogTime =
            DateTime.MinValue;

        /// <summary>
        /// 사용자가 직접 연결 해제를 요청했는지 여부
        ///
        /// 수신 Loop 종료 시 자동 재연결 이벤트가
        /// 잘못 발생하지 않도록 구분한다.
        /// </summary>
        private bool _isManualDisconnect;

        #endregion

        #region [Events]

        public event Action<byte[], DateTime> MessageReceived;

        /// <summary>
        /// 서버 연결이 비정상적으로 종료된 경우 발생
        ///
        /// MainViewModel에서 일정 간격으로 재연결을 시도할 때 사용한다.
        /// </summary>
        public event Action ConnectionClosed;

        #endregion

        #region [Properties]

        public bool IsConnected
        {
            get
            {
                lock (_socketLock)
                {
                    return _tcpClient != null &&
                           _tcpClient.Connected &&
                           _networkStream != null;
                }

            }

        }

        #endregion

        #region [Connect]

        /// <summary>
        /// [Web Agent / LA] TCP Server 연결
        ///
        /// 연결 제한 시간을 적용하여 서버가 준비되지 않은 경우에도
        /// 장비 연결 UI가 장시간 대기하지 않도록 한다.
        /// </summary>
        public async Task<bool> ConnectAsync(
            string ip,
            int port)
        {
            if (IsConnected)
            {
                Console.WriteLine("[TCP] Already Connected.");
                return true;
            }

            ConsoleLogHelper.InfoSection(
                "TCP",
                "Connect Try...",
                string.Empty,
                $"TARGET : {ip}:{port}");

            TcpClient newClient =
                new TcpClient
                {
                    NoDelay =
                    true,

                    SendTimeout =
                    SEND_TIMEOUT_MS
                };

            try
            {
                Task connectTask =
                    newClient.ConnectAsync(
                        ip,
                        port);

                Task completedTask =
                    await Task.WhenAny(
                        connectTask,
                        Task.Delay(CONNECT_TIMEOUT_MS));

                if (completedTask != connectTask)
                {
                    ConsoleLogHelper.StateSection(
                        "TCP",
                        "Connect Failed",
                        string.Empty,
                        "REASON : Timeout",
                        $"TARGET : {ip}:{port}");

                    newClient.Close();
                    newClient.Dispose();

                    return false;
                }

                await connectTask;

                lock (_socketLock)
                {
                    CleanupSocketInternal();

                    _tcpClient =
                        newClient;

                    _networkStream =
                        _tcpClient.GetStream();

                    _cts =
                        new CancellationTokenSource();

                    _isManualDisconnect =
                        false;
                }

                _ = Task.Run(() =>
                    ReceiveLoopAsync(
                        _cts.Token));

                ConsoleLogHelper.StateSection(
                    "TCP",
                    "Connect Success",
                    string.Empty,
                    $"TARGET : {ip}:{port}");

                return true;
            }
            catch (Exception ex)
            {
                newClient.Close();
                newClient.Dispose();

                ConsoleLogHelper.StateSection(
                    "TCP",
                    "Connect Failed",
                    string.Empty,
                    $"REASON : {ex.Message}",
                    $"TARGET : {ip}:{port}");

                return false;
            }

        }

        #endregion

        #region [Send]

        /// <summary>
        /// Send 송신 함수.
        /// </summary>
        public bool Send(
            byte[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                Console.WriteLine(
                    "[TCP SEND] Invalid Packet.");

                return false;
            }

            lock (_socketLock)
            {
                try
                {
                    if (!CanSend())
                    {
                        Console.WriteLine(
                            "[TCP SEND] Not Connected.");

                        return false;
                    }

                    _networkStream.Write(
                        data,
                        0,
                        data.Length);

                    _networkStream.Flush();

                    PrintHexData(
                        "[TCP SEND]",
                        data);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "[TCP ERROR] Send Failed : " +
                        ex.Message);

                    return false;
                }

            }

        }

        /// <summary>
        /// CanSend 상태 확인 함수.
        /// </summary>
        private bool CanSend()
        {
            return _tcpClient != null &&
                   _tcpClient.Connected &&
                   _networkStream != null &&
                   _networkStream.CanWrite;
        }

        #endregion

        #region [Receive]

        /// <summary>
        /// ReceiveLoopAsync 수신 함수.
        /// </summary>
        private async Task ReceiveLoopAsync(
            CancellationToken token)
        {
            byte[] buffer =
                new byte[2048];

            bool shouldNotifyConnectionClosed =
                false;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    NetworkStream stream;

                    lock (_socketLock)
                    {
                        stream =
                            _networkStream;
                    }

                    if (stream == null)
                    {
                        break;
                    }

                    int readSize =
                        await stream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length,
                            token);

                    if (readSize <= 0)
                    {
                        Console.WriteLine(
                            "[TCP] Server Disconnected.");

                        shouldNotifyConnectionClosed =
                            true;

                        break;
                    }

                    byte[] receivedData =
                        CopyReceivedData(
                            buffer,
                            readSize);

                    PrintReceiveLogIfNeeded(
                        receivedData);

                    RaiseMessageReceived(
                        receivedData);
                }

            }
            catch (OperationCanceledException)
            {
                // 사용자가 연결 해제를 요청한 정상 종료 흐름
            }
            catch (ObjectDisposedException)
            {
                // Disconnect 중 Stream 종료로 발생할 수 있는 정상 흐름
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[TCP ERROR] Receive Failed : " +
                    ex.Message);

                shouldNotifyConnectionClosed =
                    true;
            }
            finally
            {
                bool isManualDisconnect;

                lock (_socketLock)
                {
                    isManualDisconnect =
                        _isManualDisconnect;

                    CleanupSocketInternal();
                }

                if (shouldNotifyConnectionClosed &&
                    !isManualDisconnect)
                {
                    ConnectionClosed?.Invoke();
                }

            }

        }

        /// <summary>
        /// CopyReceivedData 동작 수행 함수.
        /// </summary>
        private byte[] CopyReceivedData(
            byte[] buffer,
            int readSize)
        {
            byte[] receivedData =
                new byte[readSize];

            Array.Copy(
                buffer,
                receivedData,
                readSize);

            return receivedData;
        }

        /// <summary>
        /// PrintReceiveLogIfNeeded 동작 수행 함수.
        /// </summary>
        private void PrintReceiveLogIfNeeded(
            byte[] receivedData)
        {
            if ((DateTime.Now - _lastRecvLogTime).TotalSeconds < 1)
            {
                return;
            }

            PrintReceivePackets(
                receivedData);

            _lastRecvLogTime =
                DateTime.Now;
        }

        /// <summary>
        /// PrintReceivePackets 동작 수행 함수.
        /// </summary>
        private void PrintReceivePackets(
            byte[] receivedData)
        {
            const int responsePacketSize =
                12;

            for (int i = 0;
                 i + responsePacketSize - 1 < receivedData.Length;
                 i += responsePacketSize)
            {
                string packet =
                    string.Empty;

                for (int j = 0;
                     j < responsePacketSize;
                     j++)
                {
                    packet +=
                        $"{receivedData[i + j]:X2} ";
                }

                Console.WriteLine(
                    $"[TCP RECV PACKET] {packet}");
            }

        }

        /// <summary>
        /// RaiseMessageReceived 동작 수행 함수.
        /// </summary>
        private void RaiseMessageReceived(
            byte[] receivedData)
        {
            MessageReceived?.Invoke(
                receivedData,
                DateTime.Now);
        }

        #endregion

        #region [Log]

        /// <summary>
        /// PrintHexData 동작 수행 함수.
        /// </summary>
        private void PrintHexData(
            string prefix,
            byte[] data)
        {
            Console.Write(
                prefix + " ");

            foreach (byte value in data)
            {
                Console.Write(
                    $"{value:X2} ");
            }
            Console.WriteLine();
        }

        #endregion

        #region [Disconnect]

        /// <summary>
        /// [TCP] 수동 연결 해제
        ///
        /// 수동 해제 시에는 자동 재연결 이벤트를 발생시키지 않는다.
        /// </summary>
        public void Disconnect()
        {
            lock (_socketLock)
            {
                _isManualDisconnect =
                    true;

                CleanupSocketInternal();
            }

            Console.WriteLine(
                "[TCP] Disconnected.");

            Console.WriteLine();
        }

        /// <summary>
        /// Socket / Stream / Token 내부 리소스 정리
        ///
        /// 호출 위치에서 [_socketLock]을 확보한 상태로 사용한다.
        /// </summary>
        private void CleanupSocketInternal()
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            try
            {
                _networkStream?.Close();
                _networkStream?.Dispose();
            }
            catch
            {
            }

            try
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch
            {
            }

            _cts?.Dispose();

            _cts =
                null;

            _networkStream =
                null;

            _tcpClient =
                null;
        }
        #endregion
    }

}
