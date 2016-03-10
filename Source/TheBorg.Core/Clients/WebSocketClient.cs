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
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Core.Extensions;

namespace TheBorg.Core.Clients
{
    public class WebSocketClient : IWebSocketClient
    {
        private readonly ILogger _logger;
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Subject<string> _messages = new Subject<string>();

        public IObservable<string> Messages => _messages; 

        public WebSocketClient(
            ILogger logger)
        {
            _logger = logger;
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _webSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            Listen();
        }

        public Task SendAsync(string message, CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException($"Web socket isn't open, its '{_webSocket.State}'");
            }

            var encoded = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);

            return _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
        }

        private async void Listen()
        {
            var buffer = new byte[1024];

            try
            {
                while (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseSent)
                {
                    var stringBuilder = new StringBuilder();
                    WebSocketReceiveResult webSocketReceiveResult;

                    do
                    {
                        webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token).ConfigureAwait(false);
                        if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.Verbose("Web socket close message received");
                            await _webSocket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                string.Empty,
                                CancellationToken.None)
                                .ConfigureAwait(false);
                            _messages.OnCompleted();
                        }
                        else
                        {
                            stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, webSocketReceiveResult.Count));
                        }
                    } while (!webSocketReceiveResult.EndOfMessage);

                    var message = stringBuilder.ToString();
                    if (!string.IsNullOrEmpty(message) && _webSocket.State == WebSocketState.Open)
                    {
                        _messages.OnNext(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unexpected exception");
                _messages.OnError(e);
            }
            finally
            {
                _webSocket.DisposeExceptionSafe(_logger);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _messages.DisposeExceptionSafe(_logger);
            _webSocket.DisposeExceptionSafe(_logger);
        }
    }
}