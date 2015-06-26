using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;

    public class BufferedMessageHandler
    {

        public async Task HandleRequest(IRequestContext context, Func<IRequestContext, Task> next)
        {
            if (context.Protocol != "binary")
            {
                await next(context);
                return;
            }

            var binaryContext = (BinaryRequestContext)context;
            var incoming = binaryContext.GetRequestStream();

            //buffer for robustness.
            using (var outgoingBuffer = new MemoryStream())
            {
                var newContext = new BinaryRequestContext(incoming, outgoingBuffer);

                await next(newContext);

                var bufferArray = outgoingBuffer.ToArray();

                if (Debugger.IsAttached)
                {
                    Debug.WriteLine("Write response (Length={0} bytes): {1}", bufferArray.Length, Binary.Utils.Tokenize(bufferArray));
                }

                await binaryContext.GetResponseStream().WriteAsync(bufferArray, 0, bufferArray.Length);
            }

        }
    }
}
