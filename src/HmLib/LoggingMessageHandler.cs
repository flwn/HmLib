using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;
    using System.Diagnostics;

    public class LoggingMessageHandler : IRequestHandler
    {
        private IRequestHandler _next;
        public LoggingMessageHandler(IRequestHandler next)
        {
            _next = next;
        }

        public async Task HandleRequest(IRequestContext requestContext)
        {
            try
            {
                await _next.HandleRequest(requestContext);
            }
            catch (ProtocolException protocolException)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("Protocol Error: {0}", protocolException);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error handling request. {0}", ex);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Error: {0}", ex);
                Console.ResetColor();
            }
        }
    }
}
