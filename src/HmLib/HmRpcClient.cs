using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;
    using Binary;
    using Serialization;

    public class HmRpcClient : IDisposable, IRequestHandler
    {
        private readonly IPEndPoint _endpoint;

        private readonly string _host;
        private readonly int _port;

        private readonly TcpClient _tcpClient = new TcpClient();

        private bool _isDisposed;

        public HmRpcClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public HmRpcClient(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
        }


        public async Task ConnectAsync(string host = null, int? port = null)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(HmRpcClient));
            }

            _tcpClient.ReceiveTimeout = 3000 * 10;

            if (_endpoint != null)
            {
                await _tcpClient.ConnectAsync(_endpoint.Address, _endpoint.Port);
            }
            else
            {
                await _tcpClient.ConnectAsync(host ?? _host, port ?? _port);
            }
        }

        public async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(HmRpcClient));
            }

            if (false == _tcpClient.Connected)
            {
                await ConnectAsync();
            }

            var networkStream = _tcpClient.GetStream();

            IRequestHandler handler = new InnerHandler(networkStream);

            var binaryRequest = requestMessage as BinaryMessage;
            if (binaryRequest == null)
            {
                //BufferedMessageHandler returns binary message
                handler = new BufferedMessageHandler(handler);
            }

            var response = await handler.HandleRequest(requestMessage);

            return response;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
#if DNX451
                _tcpClient.Close();
#else
                _tcpClient.Dispose();
#endif
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }


        private class InnerHandler : IRequestHandler
        {
            private Stream _innerStream;

            public InnerHandler(Stream innerStream)
            {
                _innerStream = innerStream;
            }

            public async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
            {
                return await HandleRequest((BinaryMessage)requestMessage);
            }

            private async Task<BinaryMessage> HandleRequest(BinaryMessage requestMessage)
            {
                await requestMessage.MessageStream.CopyToAsync(_innerStream);

                await Task.Delay(100);

                return new BinaryMessage(_innerStream);

            }
        }
    }
}
