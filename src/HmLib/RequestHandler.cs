using System;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;

    public abstract class RequestHandler : IRequestHandler, IDisposable
    {
        protected RequestHandler() { }

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage);
    }
}
