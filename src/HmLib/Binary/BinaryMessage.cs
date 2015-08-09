using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;

    public abstract class BinaryMessage : IFastCopyTo<BinaryMessage>
    {
        protected BinaryMessage() : this(new MemoryStream())
        {

        }

        protected BinaryMessage(Stream messageStream)
        {
            if (messageStream == null) throw new ArgumentNullException(nameof(messageStream));

            MessageStream = messageStream;
        }


        public static async Task<BinaryMessage> ReadFromStream(Stream stream)
        {
            var buffer = await Utils.ReadMessageStreamWithLengthVerification(stream);
            var messageType = buffer[3];

            var bufferedStream = new MemoryStream(buffer);
            switch (messageType)
            {
                default:
                    throw new ProtocolException($"Unsupported message type {messageType}.");

                case Packet.ErrorMessage:
                    {
                        return new BinaryResponse(bufferedStream) { IsErrorResponse = true };
                    }

                case Packet.ResponseMessage:
                case Packet.ResponseMessageWithHeaders:
                    {
                        return new BinaryResponse(bufferedStream);
                    }

                case Packet.RequestMessage:
                case Packet.RequestMessageWithHeaders:
                    {
                        return new BinaryRequest(bufferedStream);
                    }
            }

        }

        public static BinaryResponse CreateErrorResponse(int code, string message)
        {
            var buffer = new MemoryStream();
            var writer = new HmBinaryMessageWriter(buffer);
            writer.BeginMessage(MessageType.Error);
            writer.BeginContent();
            writer.BeginStruct(2);
            writer.BeginItem();
            writer.WritePropertyName("faultCode");
            writer.WriteInt32Value(code);
            writer.EndItem();
            writer.BeginItem();
            writer.WritePropertyName("faultMessage");
            writer.WriteStringValue(message);
            writer.EndItem();
            writer.EndStruct();
            writer.EndContent();
            writer.EndMessage();

            buffer.Position = 0L;
            return new BinaryResponse(buffer)
            {
                IsErrorResponse = true
            };
        }

        public Stream MessageStream { get; set; }

        public async Task CopyTo(BinaryMessage target, CancellationToken cancellation)
        {

            await MessageStream.CopyToAsync(target.MessageStream, 1024, cancellation);
        }

        public IMessageReader GetMessageReader() => new HmBinaryMessageReader(MessageStream);
    }

    public class BinaryResponse : BinaryMessage, IResponseMessage
    {
        public BinaryResponse() : base()
        {

        }
        public BinaryResponse(Stream responseBuffer) : base(responseBuffer)
        {

        }

        public virtual bool IsErrorResponse { get; internal set; }
    }

    public class BinaryRequest : BinaryMessage, IRequestMessage
    {
        public BinaryRequest()
        {

        }
        public BinaryRequest(Stream requestBuffer) : base(requestBuffer)
        {

        }
    }
}
