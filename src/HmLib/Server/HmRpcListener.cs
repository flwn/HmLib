using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib.Server
{
    public class HmRpcListener : IDisposable
    {
        private readonly RequestDispatcher _requestDispatcher;
        private readonly TcpListener _tcpListener;

        private Task _listenerTask;
        private CancellationTokenSource _cancellation = new CancellationTokenSource();

        public event Action<ClientConnectionInfo> OnClientConnected = _ => { };
        public event Action<ClientConnectionInfo> OnClientDisconnected = _ => { };

        public HmRpcListener(RequestDispatcher requestDispatcher, int port)
        {
            _requestDispatcher = requestDispatcher;

            _tcpListener = new TcpListener(IPAddress.Any, 6300);
            _tcpListener.Server.ReceiveTimeout = 3000 * 10;
        }


        public void Start()
        {
            if (_listenerTask != null)
            {
                return;
            }

            _tcpListener.Start();
            _listenerTask = Task.Run(async () =>
            {
                while (!_cancellation.Token.IsCancellationRequested)
                {

                    var connection = await _tcpListener.AcceptTcpClientAsync();

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

            var connectionInfo = new ClientConnectionInfo
            {
                LocalEndPoint = local,
                RemoteEndPoint = remote,
                Started = DateTimeOffset.UtcNow
            };

            OnClientConnected(connectionInfo);

            using (var stream = tcpClient.GetStream())
            {
                using (var connection = new Connection(connectionInfo, stream, _requestDispatcher))
                {
                    await connection.Handle();
                }
            }

            connectionInfo.Finished = DateTimeOffset.UtcNow;

            OnClientDisconnected(connectionInfo);
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
                    _tcpListener.Stop();
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
