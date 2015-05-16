using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib
{
    using Binary;
    

    public class HmRpcClient : IDisposable
    {
        private readonly IPEndPoint _endpoint;

        private readonly string _host;
        private readonly int _port;


        private readonly TcpClient _tcpClient = new TcpClient();

        private readonly IProtocol _protocol = new HmBinaryProtocol();

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
            if (_endpoint != null)
            {
                await _tcpClient.ConnectAsync(_endpoint.Address, _endpoint.Port);
            }
            else
            {
                await _tcpClient.ConnectAsync(host ?? _host, port ?? _port);
            }
        }

        public async Task<Response> ExecuteRequest(Request request)
        {
            if (!_tcpClient.Connected)
            {
                throw new InvalidOperationException("Connect first.");
            }

            var networkStream = _tcpClient.GetStream();

            _protocol.WriteRequest(networkStream, request);

            Thread.Sleep(100);

            var response = _protocol.ReadResponse(networkStream);

            return response;
        }

        public void Dispose()
        {
            _tcpClient.Close();
        }

    }
}