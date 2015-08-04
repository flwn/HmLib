using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;
    using Binary;

    public class HmRpcServer : IDisposable
    {
        private readonly IRequestHandler _requestHandler;
        private readonly TcpListener _listener;

        private Task _listenerTask;
        private CancellationTokenSource _cancellation = new CancellationTokenSource();

        public event Action<ClientConnectionInfo> OnClientConnected = _ => { };
        public event Action<ClientConnectionInfo> OnClientDisconnected = _ => { };

        public HmRpcServer(IRequestHandler requestHandler)
        {
            _requestHandler = new LoggingMessageHandler(new BufferedMessageHandler(requestHandler));

            _listener = new TcpListener(IPAddress.Any, 6300);
            _listener.Server.ReceiveTimeout = 3000 * 10;
        }


        public void Start()
        {
            if (_listenerTask != null)
            {
                return;
            }

            _listener.Start();
            _listenerTask = Task.Run(async () =>
            {
                while (!_cancellation.Token.IsCancellationRequested)
                {

                    var connection = await _listener.AcceptTcpClientAsync();

                    var task = StartHandleConnectionAsync(connection);
                    // if already faulted, re-throw any error on the calling context
                    if (task.IsFaulted)
                        task.Wait();
                }
            });
        }
        // Handle new connection
        private async Task HandleConnectionAsync(TcpClient tcpClient)
        {
            await Task.Yield();
            // continue asynchronously on another threads

            var local = (IPEndPoint)tcpClient.Client.LocalEndPoint;
            var remote = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            OnClientConnected(new ClientConnectionInfo
            {
                LocalEndPoint = local,
                RemoteEndPoint = remote,
                Timestamp = DateTime.Now
            });

            using (var stream = tcpClient.GetStream())
            {
                try
                {
                    var message = new BinaryMessage(stream);

                    await _requestHandler.HandleRequest(message);

                }
                finally
                {
                    await stream.FlushAsync();
                }
            }

            OnClientDisconnected(new ClientConnectionInfo
            {
                LocalEndPoint = local,
                RemoteEndPoint = remote,
                Timestamp = DateTime.Now
            });
        }
        private async Task StartHandleConnectionAsync(TcpClient tcpConnection)
        {

            // start the new connection task
            var connectionTask = HandleConnectionAsync(tcpConnection);

            // catch all errors of HandleConnectionAsync
            try
            {
                await connectionTask;
                // we may be on another thread after "await"
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine("Error handling connection: " + ex.ToString());
            }
        }

        private bool isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    _listener.Stop();
                    _cancellation.Cancel();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                isDisposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

    }
}
