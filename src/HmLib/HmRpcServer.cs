using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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

        public HmRpcServer(IRequestHandler requestHandler)
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
                var protocol = new RequestResponseProtocol();

                var alreadyWrittenToResponse = false;
                try
                {
                    var context = new BinaryRequestContext(stream);
                    var bufferedHandler = new BufferedMessageHandler();

                    try {
                        await bufferedHandler.HandleRequest(context, (innerCtxt) =>
                        {
                            _requestHandler.HandleRequest(innerCtxt);
                            return Task.FromResult(0);
                        });

                        //alreadyWrittenToResponse = true;

                    }
                    catch(AggregateException aggrEx )
                    {
                        throw aggrEx.InnerException;
                    }
                }
                catch (ProtocolException protocolException) when (!alreadyWrittenToResponse)
                {
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine("Protocol Error: {0}", protocolException);
                    Console.ResetColor();
                    //do not write error if already written to stream...
                    protocol.WriteErrorResponse(new Binary.HmBinaryMessageWriter(stream), protocolException.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error handling request. {0}", ex);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Error: {0}", ex);
                    Console.ResetColor();
                }
                finally
                {
                    await stream.FlushAsync();
                    stream.Close();
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
            catch (AggregateException aggEx)
            {
                var ex = aggEx.InnerException;
                Debug.Fail(ex.Message, ex.ToString());

                Console.WriteLine("Error handling connection: " + ex.ToString());
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine("Error handling connection: " + ex.ToString());
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
