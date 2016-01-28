// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/rasmus/TheBorg
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Data.SqlClient;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace TheBorg.Clients
{
    public class WebSocketClient : IWebSocketClient, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly Thread _receiverThread;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Subject<string> _messages = new Subject<string>();

        public IObservable<string> Messages => _messages; 

        public WebSocketClient(
            ILogger logger)
        {
            _logger = logger;
            _receiverThread = new Thread(Listen);
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _webSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            _receiverThread.Start();
        }

        public Task SendAsync(string message, CancellationToken cancellationToken)
        {
            var encoded = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            return _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
        }

        private void Listen()
        {
            try
            {
                ListenAsync().Wait();
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                //_messages.OnError(e);
                _logger.Error(e, "Web socket failed");
            }

            _messages.OnCompleted();

            _logger.Information("Web socket closed");
        }

        private async Task ListenAsync()
        {
            var buffer = new byte[1024];
            while (true)
            {
                var arraySegment = new ArraySegment<byte>(buffer);
                var receiveResult = await _webSocket.ReceiveAsync(arraySegment, _cancellationTokenSource.Token).ConfigureAwait(false);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.InvalidMessageType,
                        "I don't do binary",
                        _cancellationTokenSource.Token)
                        .ConfigureAwait(false);
                    return;
                }

                var count = receiveResult.Count;
                while (!receiveResult.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.InvalidPayloadData,
                            "That's too long",
                            _cancellationTokenSource.Token)
                            .ConfigureAwait(false);
                        return;
                    }

                    arraySegment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                    receiveResult = await _webSocket.ReceiveAsync(
                        arraySegment,
                        _cancellationTokenSource.Token)
                        .ConfigureAwait(false);
                    count += receiveResult.Count;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, count);
                _messages.OnNext(message);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}