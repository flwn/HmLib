using System;

namespace HmLib.Server
{
    using Abstractions;

    public class RpcServer : IDisposable
    {
        public HmRpcListener Listener { get; }

        protected RpcServer(HmRpcListener listener)
        {
            Listener = listener;
        }

        public static RpcServer Create(int port = 6300, RequestHandler defaultRequestHandler = null)
        {

            var requestHandler = defaultRequestHandler ?? new DefaultRequestHandler();
            var requestDispatcher = new RequestDispatcher(requestHandler);
            var listener = new HmRpcListener(requestDispatcher, port);

            return new RpcServer(listener);
        }


        public void Start() => Listener.Start();


        public void Dispose()
        {
            Listener.Dispose();
        }
    }
}
