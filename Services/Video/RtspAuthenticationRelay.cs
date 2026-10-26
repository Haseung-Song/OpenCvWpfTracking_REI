using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.Services.Video
{
    // 단일 클라이언트의 로컬 루프백·TCP 혼합 전송만 지원하며 인증정보를 기록하지 않는다.
    // 직접 FFmpeg 연결이 인증 오류 401을 반환한 경우에만 사용한다.
    internal sealed class RtspAuthenticationRelay : IDisposable
    {
        private readonly Uri _target;

        private readonly TcpListener _listener;

        private TcpClient _client, _remote;

        private readonly CancellationTokenSource _stop = new CancellationTokenSource();

        private int _disposed;

        public int Md5Selections;

        /// <summary>401 재접속용 루프백 릴레이를 준비한다. 인증 정보는 기록하지 않는다.</summary>
        public RtspAuthenticationRelay(string address)
        {
            _target = new Uri(address);

            if (_target.Scheme != "rtsp")
                throw new ArgumentException("Only plain RTSP supported.");

            _listener = new TcpListener(IPAddress.Loopback, 0);
        }

        /// <summary>단일 클라이언트 수신을 시작하고 FFmpeg가 접속할 루프백 주소를 반환한다.</summary>
        public string Start()
        {
            _listener.Start(1);
            int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(Run);
            return new UriBuilder(_target)
            {
                Host = "127.0.0.1",
                Port = port
            }.Uri.AbsoluteUri;
        }

        /// <summary>요청과 응답을 양방향 중계하고 어느 쪽이든 종료되면 연결을 함께 정리한다.</summary>
        private async Task Run()
        {
            try
            {
                _client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _listener.Stop();

                if (_stop.IsCancellationRequested)
                    return;

                _client.NoDelay = true;
                _remote = new TcpClient
                {
                    NoDelay = true
                };
                var connect = _remote.ConnectAsync(_target.Host, _target.Port > 0 ? _target.Port : 554);

                if (await Task.WhenAny(connect, Task.Delay(5000, _stop.Token)).ConfigureAwait(false) != connect)
                    return;

                await connect.ConfigureAwait(false);

                if (_stop.IsCancellationRequested)
                    return;

                var local = _client.GetStream();
                var remote = _remote.GetStream();
                // 요청과 혼합 전송 RTCP 데이터는 변경 없이 전달한다.
                var outgoing = local.CopyToAsync(remote, 65536, _stop.Token);
                var incoming = ForwardResponses(remote, local);
                await Task.WhenAny(outgoing, incoming).ConfigureAwait(false);
                Dispose();

                // 연결 해제가 대기 입출력을 취소하며 종료 과정의 반복 로그를 억제한다.
                try
                {
                    await Task.WhenAll(outgoing, incoming).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>RTP/RTCP 바이트는 그대로 전달하고 RTSP 401 인증 헤더만 선별한다.</summary>
        private async Task ForwardResponses(NetworkStream input, NetworkStream output)
        {
            // 헤더 버퍼는 연결별로 재사용하고 동시에 실행되는 중계끼리 공유하지 않는다.
            // 매번 4바이트 전체를 갱신하고 쓰기가 끝난 뒤 다음 데이터를 읽는다.
            byte[] one = new byte[1], block = new byte[65536], header = new byte[4];
            while (!_stop.IsCancellationRequested)
            {
                if (await input.ReadAsync(one, 0, 1, _stop.Token).ConfigureAwait(false) == 0)
                    return;

                if (one[0] == (byte)'$')
                {
                    header[0] = one[0];
                    await ReadExact(input, header, 1, 3).ConfigureAwait(false);
                    await output.WriteAsync(header, 0, 4, _stop.Token).ConfigureAwait(false);
                    await CopyCount(input, output, (header[2] << 8) | header[3], block).ConfigureAwait(false);
                    continue;
                }

                var bytes = new List<byte>(1024)
                {
                    one[0]
                };
                while (true)
                {
                    if (bytes.Count >= 65536)
                        throw new IOException("RTSP header limit.");

                    await ReadExact(input, one, 0, 1).ConfigureAwait(false);
                    bytes.Add(one[0]);
                    int n = bytes.Count;

                    if (n >= 4 && bytes[n - 4] == 13 && bytes[n - 3] == 10 && bytes[n - 2] == 13 && bytes[n - 1] == 10)
                        break;
                }

                string original = Encoding.ASCII.GetString(bytes.ToArray());
                string filtered = SelectMd5Challenge(original);

                if (filtered != original)
                    Interlocked.Increment(ref Md5Selections);

                var headerBytes = Encoding.ASCII.GetBytes(filtered);
                await output.WriteAsync(headerBytes, 0, headerBytes.Length, _stop.Token).ConfigureAwait(false);
                var lengthMatch = Regex.Match(original, @"(?:^|\r\n)Content-Length:\s*(\d+)", RegexOptions.IgnoreCase);
                int length;

                if (lengthMatch.Success)
                {
                    if (!int.TryParse(lengthMatch.Groups[1].Value, out length) || length < 0 || length > 16 * 1024 * 1024)
                        throw new IOException("RTSP body limit.");

                    await CopyCount(input, output, length, block).ConfigureAwait(false);
                }
            }
        }

        /// <summary>401 응답에 호환 MD5 Digest가 있을 때만 지원하지 않는 인증 후보를 제외한다.</summary>
        internal static string SelectMd5Challenge(string header)
        {
            if (!header.StartsWith("RTSP/1.0 401", StringComparison.Ordinal))
                return header;

            var lines = header.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var auth = lines.Where(l => l.StartsWith("WWW-Authenticate:", StringComparison.OrdinalIgnoreCase)).ToList();
            Func<string, bool> supported = l =>
            {
                if (!Regex.IsMatch(l, @"^WWW-Authenticate:\s*Digest\s", RegexOptions.IgnoreCase))
                    return false;

                var algo = Regex.Match(l, @"algorithm\s*=\s*""?([^"",\s]+)", RegexOptions.IgnoreCase);
                return !algo.Success ||
                    algo.Groups[1].Value.Equals(
                    "MD5",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                    algo.Groups[1].Value.Equals(
                    "MD5-sess",
                    StringComparison.OrdinalIgnoreCase
                );
            };

            if (!auth.Any(supported) || !auth.Any(l => !supported(l)))
                return header;

            return string.Join(
                "\r\n",
                lines.Where(l => !l.StartsWith(
                "WWW-Authenticate:",
                StringComparison.OrdinalIgnoreCase
            ) ||
                supported(l))
            );
        }

        /// <summary>지정 바이트 수를 읽으며 조기 연결 종료는 상위 연결 정리 경로로 전달한다.</summary>
        private async Task ReadExact(NetworkStream input, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int read = await input.ReadAsync(buffer, offset, count, _stop.Token).ConfigureAwait(false);

                if (read == 0)
                    throw new EndOfStreamException();

                offset += read;
                count -= read;
            }
        }

        /// <summary>공유 작업 버퍼로 지정 길이의 본문/미디어를 순차 전송한다.</summary>
        private async Task CopyCount(NetworkStream input, NetworkStream output, int count, byte[] block)
        {
            while (count > 0)
            {
                int read = await input.ReadAsync(block, 0, Math.Min(count, block.Length), _stop.Token).ConfigureAwait(false);

                if (read == 0)
                    throw new EndOfStreamException();

                await output.WriteAsync(block, 0, read, _stop.Token).ConfigureAwait(false);
                count -= read;
            }
        }

        /// <summary>중복 호출을 허용하면서 취소 및 양쪽 소켓 종료를 한 번만 수행한다.</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _stop.Cancel();
            _listener.Stop();
            _client?.Close();
            _remote?.Close();
        }
    }
}
