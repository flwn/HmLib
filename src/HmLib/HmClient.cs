using System;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;
    using Serialization;

    public class HmClient
    {
        private Func<RequestHandler> _requestHandlerFunc;

        private static readonly IMessageConverter DefaultConverter = new MessageConverter();
        private RequestHandler _requestHandler;
        private bool _disposeAfterUse;

        public HmClient(RequestHandler requestHandler)
        {
            if (requestHandler == null) throw new ArgumentNullException(nameof(requestHandler));
            _requestHandler = requestHandler;
        }

        public HmClient(Func<RequestHandler> requestHandlerFunc)
        {
            if (requestHandlerFunc == null) throw new ArgumentNullException(nameof(requestHandlerFunc));
            _requestHandlerFunc = requestHandlerFunc;
            _disposeAfterUse = true;
        }


        public async Task<IResponseMessage> ExecuteRequest(IRequestMessage request)
        {

            var client = _requestHandler ?? _requestHandlerFunc();

            try
            {
                var response = await client.HandleRequest(request);

                return response;
            }
            finally
            {
                if(_disposeAfterUse)
                {
                    (client as IDisposable)?.Dispose();
                }
            }
        }

        public async Task<TResponse> ExecuteRequest<TRequest, TResponse>(TRequest request)
            where TRequest : IRequestMessage
            where TResponse : IResponseMessage
        {
            var response = await ExecuteRequest(request);

            if (response is TResponse)
            {
                return (TResponse)response;
            }

            var convertedResult = DefaultConverter.Convert<TResponse>(response);


            return convertedResult;
        }

    }
}
