using System.IO;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;

    public class BufferedMessageHandler : DelegatingRequestHandler
    {
        public BufferedMessageHandler(RequestHandler next) : base(next)
        {
        }

        public override async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            try
            {
                var bufferedRequest = await requestMessage.ReadAsBinary();

                var innerResponse = await base.HandleRequest(bufferedRequest);

                var outerResponse = await innerResponse.ReadAsBinary();

                return outerResponse;
            }
            catch (ProtocolException protocolException)
            {
                var errorBuffer = new MemoryStream();
                var response = new BinaryResponse();
                var writer = new HmBinaryMessageWriter(response);
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

                return response;
            }
        }

    }
}
