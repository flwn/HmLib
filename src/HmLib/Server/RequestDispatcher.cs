using System.Threading.Tasks;

namespace HmLib.Server
{
    using Abstractions;
    using Binary;

    public class RequestDispatcher
    {
        private RequestHandler _requestHandler;

        public RequestDispatcher(RequestHandler requestHandler)
        {
            _requestHandler = new LoggingMessageHandler(new BufferedMessageHandler(requestHandler));
        }


        public async Task<IResponseMessage> Dispatch(IRequestMessage requestMessage)
        {
            var response = await _requestHandler.HandleRequest(requestMessage);

            return response;
        }
    }
}
