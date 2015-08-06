using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;
    using Binary;

    public class TcpClientRequestHandler : DelegatingRequestHandler
    {
        private readonly IPEndPoint _endpoint;
        private readonly string _host;
        private readonly int _port;

        private readonly TcpClient _tcpClient = new TcpClient();

        private bool _isDisposed;

        public TcpClientRequestHandler(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public TcpClientRequestHandler(DnsEndPoint endpoint)
        {
            _host = endpoint.Host;
            _port = endpoint.Port;
        }

        public TcpClientRequestHandler(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
        }


        public async Task ConnectAsync(string host = null, int? port = null)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TcpClientRequestHandler));
            }

            if (_tcpClient.Connected)
            {
                return;
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

            InnerHandler = new StreamWritingRequestHandler(_tcpClient.GetStream());

        }

        internal protected override async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            await ConnectAsync();

            var response = await base.HandleRequest(requestMessage);

            return response;
        }

        protected override void Dispose(bool disposing)
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

    }
}
