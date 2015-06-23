using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib
{
    using Binary;
    using Serialization;
    using System.IO;

    public class HmRpcClient : IDisposable
    {
        private readonly IPEndPoint _endpoint;

        private readonly string _host;
        private readonly int _port;

        private readonly TcpClient _tcpClient = new TcpClient();

        private readonly IProtocol _protocol = new RequestResponseProtocol();

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

        public async Task<Response> ExecuteRequest(Request request)
        {
            if (!_tcpClient.Connected)
            {
                throw new InvalidOperationException("Connect first.");
            }


            var requestBuffer = new MemoryStream();
            var streamWriter = new HmBinaryMessageWriter(requestBuffer);
            _protocol.WriteRequest(streamWriter, request);

            var responseBuilder = new MessageBuilder();
            using (var networkStream = _tcpClient.GetStream())
            {
                requestBuffer.Position = 0;
                await requestBuffer.CopyToAsync(networkStream);

                Thread.Sleep(100);

                //todo: implement buffered reader
                var streamReader = new HmBinaryMessageReader(networkStream);

                _protocol.ReadResponse(streamReader, responseBuilder);
            }

            var response = (Response)responseBuilder.Result;

            return response;
        }

        public void Dispose()
        {
            _tcpClient.Close();
        }

    }
}