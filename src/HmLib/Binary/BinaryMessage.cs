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


        public static async Task<BinaryMessage> ReadFromStream(Stream networkStream)
        {
            var readResult = await Utils.ReadMessageIntoBuffer(networkStream);
            var messageType = readResult.Item1[3];

            var bufferedStream = new MemoryStream(readResult.Item1, 0, readResult.Item2);
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

        public Stream MessageStream { get; set; }

        public async Task CopyTo(BinaryMessage target, CancellationToken cancellation)
        {

            await MessageStream.CopyToAsync(target.MessageStream, 1024, cancellation);
        }

        public IMessageReader GetMessageReader()
        {
            return new HmBinaryMessageReader(MessageStream);
        }
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
