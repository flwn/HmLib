using System;
using System.Threading.Tasks;

namespace HmLib.Abstractions
{
    public abstract class RequestHandler : IDisposable
    {
        protected RequestHandler() { }

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal protected abstract Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage);
    }
}
