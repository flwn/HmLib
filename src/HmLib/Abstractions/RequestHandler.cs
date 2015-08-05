using System;
using System.Threading.Tasks;

namespace HmLib.Abstractions
{
    public abstract class RequestHandler<TResponse, TRequest> : IRequestHandler
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        private IMessageConverter _messageConverter;

        protected RequestHandler(IMessageConverter messageConverter)
        {
            if (messageConverter == null) throw new ArgumentNullException(nameof(messageConverter));

            _messageConverter = messageConverter;
        }

        public virtual Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            TRequest request;
            if (false == (requestMessage is TRequest))
            {
                request = _messageConverter.Convert<TRequest>(requestMessage);
            }

            var response = HandleRequest(requestMessage);

            return response;
        }

        protected abstract Task<TResponse> HandleRequest(TRequest request);
    }
}
