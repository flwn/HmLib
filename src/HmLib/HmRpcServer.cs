using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib
{
    public class HmRpcServer : IDisposable
    {
        private readonly Func<Request, Response> _requestHandler;
        private readonly TcpListener _listener;

        private ManualResetEventSlim _acceptWaiter = new ManualResetEventSlim();

        private Task _listenerTask;
        private readonly List<TcpClient> _connections = new List<TcpClient>();

        public HmRpcServer(Func<Request, Response> requestHandler)
        {
            _requestHandler = requestHandler;

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
                while (true)
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
            var local = (IPEndPoint)tcpClient.Client.LocalEndPoint;
            var remote = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            Console.Write("Incoming! (Local={0}, Remote=", local);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(remote);
            Console.ResetColor();
            Console.WriteLine(")");
            await Task.Yield();
            // continue asynchronously on another threads

            using (var stream = tcpClient.GetStream())
            {
                var protocol = new HmLib.Binary.HmBinaryProtocol();

                var alreadyWrittenToResponse = false;
                try
                {
                    var request = protocol.ReadRequest(stream);
                    var response = (object)_requestHandler(request);

                    Console.WriteLine(request);

                    switch (request.Method)
                    {
                        case "system.listMethods":
                            response = new List<object> { "system.multicall" };
                            break;
                        case "system.multicall":
                        default:
                            response = null;
                            break;
                    }

                    //buffer for robustness.
                    using (var buffer = new MemoryStream())
                    {
                        protocol.WriteResponse(buffer, response);
                        alreadyWrittenToResponse = true;
                        await buffer.CopyToAsync(stream);
                    }
                }
                catch (ProtocolException protocolException)
                {
                    if (!alreadyWrittenToResponse)
                    {
                        //do not write error if already written to stream...
                        protocol.WriteResponse(stream, protocolException);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: ", ex);
                }
            }
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
                Console.WriteLine(ex.ToString());
            }
        }

        #region IDisposable Support
        private bool isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    _listener.Stop();


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
        #endregion



    }
}
