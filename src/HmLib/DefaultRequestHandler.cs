using System.Collections.Generic;

namespace HmLib
{
    using Abstractions;
    using Serialization;
    using System;

    public class DefaultRequestHandler : IRequestHandler
    {
        public void HandleRequest(IRequestContext requestContext)
        {
            //var prot = new RequestResponseProtocol();

            var converter = new MessageConverter();
            var messageBuilder = new MessageBuilder();
            converter.Convert(requestContext.Request, messageBuilder);

            var request = (Request)messageBuilder.Result;
            var response = HandleRequest(request);


#if DEBUG
            System.Diagnostics.Debug.WriteLine(messageBuilder.Debug);
            Console.WriteLine(request);
#endif
            var responseReader = new MessageReader(response);
            converter.Convert(responseReader, requestContext.Response);
        }

        protected virtual Response HandleRequest(Request request)
        {
            object responseContent;

            switch (request.Method)
            {
                case "newDevices":
                    responseContent = string.Empty;
                    break;
                case "listDevices":
                    responseContent = new List<object>(0);
                    break;
                case "system.listMethods":
                    responseContent = new List<object> { "system.multicall" };
                    break;
                case "system.multicall":
                default:
                    responseContent = string.Empty;
                    break;
            }
            return new Response { Content = responseContent };
        }
    }
}
