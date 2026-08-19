using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Communication.AI
{
    /// <summary>
    /// [AI] [Detector Agent]와 [TCP] 통신을 담당하는 [Client] [Service]
    ///
    /// 역할:
    /// 1. [AI] [Detector Agent]에 [TCP] [Client]로 연결
    /// 2. [Agent]에서 송신하는 [byte[]] 데이터 수신
    /// 3. 수신 데이터를 [Buffer]에 누적
    /// 4. 완성된 [AI] [Packet]만 분리하여 [ViewModel]로 전달
    ///
    /// 주의:
    /// - 이 클래스는 [Packet] 내용을 해석하지 않는다.
    /// - [Packet] 해석은 [AiDetectorPacketParser]가 담당한다.
    /// </summary>
    public class AiDetectorClientService
    {
        #region [Fields]

        /// <summary>
        /// 실제 [TCP] [Client] 객체
        /// </summary>
        private TcpClient _tcpClient;

        /// <summary>
        /// [TCP] 송수신 [Stream]
        /// </summary>
        private NetworkStream _networkStream;

        /// <summary>
        /// [ReceiveLoop] 종료 제어용 [CancellationToken]
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// [TCP] 수신 데이터 누적 [Buffer]
        ///
        /// [TCP]는 [Packet] 단위로 들어오지 않으므로,
        /// 수신된 [byte[]]를 여기에 계속 누적한다.
        /// </summary>
        private readonly List<byte> _receiveBuffer = new List<byte>();

        /// <summary>
        /// 누적 [Buffer]에서 완성 [Packet]을 분리하기 위한 [Parser]
        /// </summary>
        private readonly AiDetectorPacketParser _packetParser = new AiDetectorPacketParser();

        /// <summary>
        /// [AI Detector] 자동 재연결 루프 실행 여부
        /// </summary>
        private bool _isReconnectRunning;

        /// <summary>
        /// [AI Detector] 자동 재연결 중복 실행 방지 여부
        /// </summary>
        private bool _isReconnectLoopStarted;

        #endregion

        #region [Events]

        /// <summary>
        /// 완성된 [AI] [Packet]이 수신되었을 때 발생하는 [Event]
        ///
        /// [MainViewModel]에서 이 [Event]를 구독해서
        /// [CMD 55] 탐지데이터를 파싱하면 된다.
        /// </summary>
        public event Action<byte[], DateTime> PacketReceived;

        #endregion

        #region [Properties]

        /// <summary>
        /// 현재 [TCP] 연결 상태
        /// </summary>
        public bool IsConnected =>
            _tcpClient != null &&
            _tcpClient.Connected;

        #endregion

        #region [Connect]

        /// <summary>
        /// [AI] [Detector Agent]에 [TCP] 연결
        ///
        /// [기본] :
        /// [IP]   : [192.168.20.160]
        /// [PORT] : [5055]
        /// </summary>
        public async Task<bool> ConnectAsync(string ip, int port)
        {
            TcpClient tcpClient = null;

            try
            {
                if (IsConnected)
                {
                    Console.WriteLine("[AI TCP] Already Connected.");
                    return true;
                }

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[AI TCP] Connect Try...");
                Console.WriteLine($"[AI TCP] Target : {ip}:{port}");
                Console.WriteLine();

                tcpClient = new TcpClient();

                await tcpClient.ConnectAsync(ip, port);

                _tcpClient = tcpClient;
                _networkStream = _tcpClient.GetStream();

                _cts = new CancellationTokenSource();

                // 수신 [Loop]는 [UI] 멈춤 방지를 위해 [Background Task]로 실행
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                Console.WriteLine("[AI TCP] Connect Success.");
                ConsoleLogHelper.PrintLine();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AI TCP ERROR] Connect Failed : " + ex.Message);
                ConsoleLogHelper.PrintLine();

                try
                {
                    tcpClient?.Close();
                }
                catch
                {

                }
                return false;
            }

        }

        #endregion

        #region [Auto Reconnect]

        /// <summary>
        /// [AI Detector Agent] 자동 재연결 시작
        /// 
        /// [AI Detector Agent] 프로그램이 늦게 켜지거나,
        /// 중간에 종료되었다가 다시 실행되는 경우를 대비하여
        /// 일정 주기마다 [TCP] 연결을 재시도한다.
        /// </summary>
        public async Task StartAutoReconnectAsync(
            string ip,
            int port,
            int retryIntervalMs = 3000)
        {
            if (_isReconnectLoopStarted)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[AI TCP] Auto Reconnect Already Running.");
                return;
            }

            _isReconnectLoopStarted = true;
            _isReconnectRunning = true;

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[AI TCP] Auto Reconnect Start.");

            while (_isReconnectRunning)
            {
                /// <summary>
                /// 현재 연결이 끊긴 상태일 때만
                /// [AI Detector Agent] 재연결을 시도한다.
                /// 
                /// 연결 성공 후, [IsConnected]가 true이므로
                /// 추가 [Connect] 시도 없이 상태만 유지한다.
                /// </summary>
                if (!IsConnected)
                {
                    bool connected = await ConnectAsync(ip, port);

                    if (connected)
                    {
                        Console.WriteLine("[AI TCP] Auto Reconnect Success.");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"[AI TCP] Reconnect Retry After {retryIntervalMs} ms");
                    }
                    ConsoleLogHelper.PrintLine();
                }
                await Task.Delay(retryIntervalMs);
            }
            _isReconnectLoopStarted = false;

            Console.WriteLine("[AI TCP] Auto Reconnect Stop.");
        }

        /// <summary>
        /// [AI Detector Agent] 자동 재연결 재시작
        /// 
        /// 기존 연결 및 자동 재연결 루프를 정리한 뒤,
        /// 새 [IP] / [Port] 기준으로 자동 재연결을 다시 시작한다.
        /// </summary>
        public async Task RestartAutoReconnectAsync(
            string ip,
            int port,
            int retryIntervalMs = 3000)
        {
            // [AI] [Detector] [TCP] 연결 종료 및 리소스 정리
            Disconnect();

            /// <summary>
            /// 기존 [Auto Reconnect] 루프가 종료될 시간을 짧게 확보한다.
            /// </summary>
            await Task.Delay(300);

            _isReconnectLoopStarted = false;

            await StartAutoReconnectAsync(
                ip,
                port,
                retryIntervalMs);
        }

        /// <summary>
        /// [AI Detector Agent] 자동 재연결 중지 요청
        /// 
        /// 기존 [Auto Reconnect] 루프를 종료시키기 위해 사용한다.
        /// </summary>
        public void StopAutoReconnect()
        {
            _isReconnectRunning = false;

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[AI TCP] Auto Reconnect Stop Request.");
        }

        #endregion

        #region [Receive]

        /// <summary>
        /// [AI] [Detector Agent]로부터 데이터를 계속 수신하는 [Loop]
        ///
        /// 수신 흐름:
        /// 1. [NetworkStream.ReadAsync()]로 [byte[]] 수신
        /// 2. [_receiveBuffer]에 누적
        /// 3. [Parser]로 완성 [Packet] 분리
        /// 4. [PacketReceived] [Event] 발생
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (!token.IsCancellationRequested &&
                       IsConnected &&
                       _networkStream != null)
                {
                    int readSize =
                        await _networkStream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length);

                    // [readSize]가 [0]이면 상대방이 연결 종료한 상태
                    if (readSize <= 0)
                    {
                        Console.WriteLine("[AI TCP] Server Disconnected.");
                        break;
                    }

                    // 실제 수신된 [byte]만 누적 [Buffer]에 추가
                    AppendReceiveBuffer(buffer, readSize);

                    // 누적 [Buffer]에서 완성 [Packet] 분리
                    List<byte[]> packets = _packetParser.ExtractPackets(_receiveBuffer);

                    foreach (byte[] packet in packets)
                    {
                        // [AI Detector] 수신 [Packet]은 매우 빠르게 들어오므로
                        // [Raw HEX Log]는 [Console] 도배 방지를 위해 기본 출력하지 않는다.
                        // 필요 시 디버깅할 때만 주석 해제한다.

                        // PrintHex("[AI TCP RECV]", packet);

                        PacketReceived?.Invoke(packet, DateTime.Now);
                    }

                }

            }
            catch (ObjectDisposedException)
            {
                // [Disconnect] 중 [Stream]이 닫히면서 발생 가능하므로,
                // 정상 종료 흐름으로 처리한다.
                Console.WriteLine("[AI TCP] Receive Loop Closed.");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[AI TCP ERROR] Receive Failed : " + ex.Message);
            }
            Disconnect();
        }

        /// <summary>
        /// 수신된 [byte[]] 중 실제 [readSize]만큼만 누적 [Buffer]에 추가
        /// </summary>
        private void AppendReceiveBuffer(byte[] buffer, int readSize)
        {
            for (int i = 0; i < readSize; i++)
            {
                _receiveBuffer.Add(buffer[i]);
            }

        }

        #endregion

        #region [Packet Send]

        /// <summary>
        /// [AI Detector Agent] 요청 [Packet] 송신
        ///
        /// 연결 상태 확인 후
        /// [AI Detector Agent]로 [Packet]을 전송한다.
        ///
        /// [CMD 51]
        /// [CMD 52]
        /// [CMD 53]
        /// [CMD 54]
        /// 
        /// 조회 요청에 사용한다.
        /// </summary>
        public async Task<bool> SendAsync(
            byte[] packet)
        {
            if (_tcpClient == null ||
                !_tcpClient.Connected)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[AI TCP] Send Failed : Not Connected");

                ConsoleLogHelper.PrintLine();

                return false;
            }

            try
            {
                NetworkStream stream = _tcpClient.GetStream();

                await stream.WriteAsync(
                    packet,
                    0,
                    packet.Length);

                Console.WriteLine(
                    "[AI TCP SEND] " +
                    BitConverter.ToString(packet));

                ConsoleLogHelper.PrintLine();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[AI TCP ERROR] Send Failed : " +
                    ex.Message);

                ConsoleLogHelper.PrintLine();

                return false;
            }

        }

        #endregion

        #region [Disconnect]

        /// <summary>
        /// [AI] [Detector] [TCP] 연결 종료 및 리소스 정리
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _cts?.Cancel();

                NetworkStream stream = _networkStream;
                TcpClient client = _tcpClient;

                _networkStream = null;
                _tcpClient = null;

                stream?.Close();
                client?.Close();
            }
            catch
            {
                // 종료 과정에서 발생하는 예외는 무시한다.
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;

                _receiveBuffer.Clear();

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[AI TCP] Disconnected.");
            }

        }

        #endregion

        #region [Log]

        /// <summary>
        /// 수신 [Packet] [HEX] 로그 출력
        /// </summary>
        private void PrintHex(
            string title,
            byte[] data)
        {
            Console.WriteLine(title + " " + BitConverter.ToString(data));
        }
        #endregion
    }

}
