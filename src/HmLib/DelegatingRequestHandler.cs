using System;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;

    /// <summary>
    /// From: https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DelegatingHandler.cs
    /// </summary>
    public abstract class DelegatingRequestHandler : RequestHandler
    {
        private volatile bool _operationStarted = false;
        private volatile bool _disposed = false;

        private RequestHandler _innerHandler;

        protected DelegatingRequestHandler()
        {

        }

        protected DelegatingRequestHandler(RequestHandler innerHandler)
        {
            if (innerHandler == null) throw new ArgumentNullException(nameof(innerHandler));
            _innerHandler = innerHandler;
        }

        public RequestHandler InnerHandler
        {
            get
            {
                return _innerHandler;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                CheckDisposed();

                if (_operationStarted)
                {
                    throw new InvalidOperationException("Operation already started");
                }

                _innerHandler = value;
            }
        }

        public override Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            if (requestMessage == null) throw new ArgumentNullException(nameof(requestMessage));

            CheckDisposed();

            if (InnerHandler == null) throw new InvalidOperationException("No inner handler");

            _operationStarted = true;

            return InnerHandler.HandleRequest(requestMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (InnerHandler != null)
                {
                    InnerHandler.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

    }
}
