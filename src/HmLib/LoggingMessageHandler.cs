using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;

    public class LoggingMessageHandler : DelegatingRequestHandler
    {
        public LoggingMessageHandler(RequestHandler next) : base(next)
        {
        }

        internal protected override async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            try
            {
                var result = await base.HandleRequest(requestMessage);

                return result;
            }
            catch (ProtocolException protocolException)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("Protocol Error: {0}", protocolException);
                Console.ResetColor();
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error handling request. {0}", ex);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Error: {0}", ex);
                Console.ResetColor();
                throw;
            }
        }
    }
}
