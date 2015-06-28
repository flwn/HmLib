using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;

    public class BufferedMessageHandler : IRequestHandler
    {
        private IRequestHandler _next;
        public BufferedMessageHandler(IRequestHandler next)
        {
            _next = next;
        }

        public async Task HandleRequest(IRequestContext context)
        {
            if (context.Protocol != "binary")
            {
                await _next.HandleRequest(context);
                return;
            }

            var binaryContext = (BinaryRequestContext)context;
            var incoming = binaryContext.GetRequestStream();

            //buffer for robustness.
            using (var outgoingBuffer = new MemoryStream())
            {
                var newContext = new BinaryRequestContext(incoming, outgoingBuffer);
                try
                {
                    await _next.HandleRequest(newContext);

                    var bufferArray = outgoingBuffer.ToArray();

                    if (Debugger.IsAttached)
                    {
                        Debug.WriteLine("Write response (Length={0} bytes): {1}", bufferArray.Length, Binary.Utils.Tokenize(bufferArray));
                    }

                    var responseStream = binaryContext.GetResponseStream();

                    await responseStream.WriteAsync(bufferArray, 0, bufferArray.Length);

                }
                catch (ProtocolException protocolException)
                {
                    var responseStream = binaryContext.GetResponseStream();
                    using (var errorBuffer = new MemoryStream())
                    {
                        var writer = new HmBinaryMessageWriter(errorBuffer);
                        writer.BeginMessage(MessageType.Error);
                        writer.BeginContent();
                        writer.BeginStruct(2);
                        writer.BeginItem();
                        writer.WritePropertyName("faultCode");
                        writer.WriteInt32Value(int.MinValue);
                        writer.EndItem();
                        writer.BeginItem();
                        writer.WritePropertyName("faultMessage");
                        writer.WriteStringValue(protocolException.Message);
                        writer.EndItem();
                        writer.EndStruct();
                        writer.EndContent();
                        writer.EndMessage();

#if DEBUG
                        errorBuffer.Seek(0, SeekOrigin.Begin);
                        var intBuf = new byte[4];
                        errorBuffer.Read(intBuf, 4, 4);
                        var length = HmBitConverter.ToInt32(intBuf);
                        if (errorBuffer.Length != length + 4)
                        {
                            throw new ProtocolException("Error message not correct.");
                        }
#endif

                        var errorBufferArray = errorBuffer.ToArray();
                        await responseStream.WriteAsync(errorBufferArray, 0, errorBufferArray.Length);
                    }
                }
            }

        }
    }
}
