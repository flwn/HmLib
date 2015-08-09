using System.Threading.Tasks;

namespace HmLib.Server
{
    using Abstractions;

    public class RequestDispatcher
    {
        private RequestHandler _requestHandler;

        public RequestDispatcher(RequestHandler requestHandler)
        {
            _requestHandler = new LoggingMessageHandler(requestHandler);
        }


        public async Task<IResponseMessage> Dispatch(IRequestMessage requestMessage)
        {
            var response = await _requestHandler.HandleRequest(requestMessage);

            return response;
        }
    }
}
