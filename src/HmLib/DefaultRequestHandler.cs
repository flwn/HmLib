﻿using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;
    using Serialization;

    public class DefaultRequestHandler : IRequestHandler
    {
        private static readonly IMessageConverter DefaultConverter = new MessageConverter();

        public Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
#if DEBUG
            var messageBuilder = new MessageBuilder();
            DefaultConverter.Convert(requestMessage, (IMessageBuilder)messageBuilder);

            var request = (Request)messageBuilder.Result;

            System.Diagnostics.Debug.WriteLine(messageBuilder.Debug);
            Console.WriteLine(request);
#else
            var request = DefaultConverter.Convert<Request>(requestMessage);
#endif
            var response = HandleRequest(request);

            return Task.FromResult<IResponseMessage>(response);
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
