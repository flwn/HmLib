using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HmLib
{
    public class HmRpcServer : IDisposable
    {
        private readonly Func<Request, Response> _requestHandler;
        private readonly TcpListener _listener;

        private Task _listenerTask;

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
                var protocol = new RequestResponseProtocol();

                var alreadyWrittenToResponse = false;
                try
                {
                    var messageBuilder = new Serialization.MessageBuilder();
                    var messageReader = new Binary.HmBinaryMessageReader(stream);
                    protocol.ReadRequest(messageReader, messageBuilder);

                    var request = (Request)messageBuilder.Result;
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(messageBuilder.Debug);
                    Console.WriteLine(request);
#endif
                    var response = (object)_requestHandler(request);


                    switch (request.Method)
                    {
                        case "newDevices":
                            response = string.Empty;
                            break;
                        case "listDevices":
                            response = new List<object>(0);
                            break;
                        case "system.listMethods":
                            response = new List<object> { "system.multicall" };
                            break;
                        case "system.multicall":
                            var parameters = (ICollection<object>)request.Parameters.First();
                            response = new List<object>(parameters.Select(x => ""));
                            response = string.Empty;
                            break;
                        default:
                            response = null;
                            break;
                    }

                    //buffer for robustness.
                    using (var buffer = new MemoryStream())
                    {
                        protocol.WriteResponse(new Binary.HmBinaryMessageWriter(buffer), response);
                        alreadyWrittenToResponse = true;
                        var bufferArray = buffer.ToArray();

                        if (Debugger.IsAttached)
                        {
                            Debug.WriteLine("Write response (Length={0} bytes): {1}", bufferArray.Length, Binary.Utils.Tokenize(bufferArray));
                        }

                        await stream.WriteAsync(bufferArray, 0, bufferArray.Length);
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
