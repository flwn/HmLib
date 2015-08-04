using System;
using System.IO;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;
    using Serialization;

    public class BufferedMessageHandler : IRequestHandler
    {
        private readonly IMessageConverter _messageConverter;


        private IRequestHandler _next;
        public BufferedMessageHandler(IRequestHandler next, IMessageConverter messageConverter = null)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            _next = next;
            _messageConverter = messageConverter ?? new MessageConverter();
        }

        public async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {

            try
            {
                var requestBuffer = new MemoryStream();
                var bufferedRequest = new BinaryMessage(requestBuffer);
                var bufferedMessageWriter = new HmBinaryMessageWriter(bufferedRequest);

                _messageConverter.Convert(requestMessage, (IMessageBuilder)bufferedMessageWriter);

                //rewind memory stream for reading
                requestBuffer.Position = 0L;

                var innerResponse = await _next.HandleRequest(bufferedRequest);

                var outerResponse = _messageConverter.Convert<BinaryMessage>(innerResponse);

                return outerResponse;
            }
            catch (ProtocolException protocolException)
            {
                var errorBuffer = new MemoryStream();

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

                return new BinaryMessage(errorBuffer);
            }
        }

    }
}
