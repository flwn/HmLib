using System;
using System.IO;

namespace HmLib.Binary
{
    using Abstractions;

    public class BinaryMessage : IRequestMessage, IResponseMessage, IFastCopyTo<BinaryMessage>
    {
        public BinaryMessage() : this (new MemoryStream())
        {

        }
        public BinaryMessage(Stream messageStream )
        {
            if (messageStream == null) throw new ArgumentNullException(nameof(messageStream));

            MessageStream = messageStream;
        }


        public Stream MessageStream { get; set; }

        public IMessageReader CreateResponseReader()
        {
            return new HmBinaryMessageReader(MessageStream);
        }
        public IMessageBuilder CreateResponseWriter()
        {
            return new HmBinaryMessageWriter(MessageStream);
        }
        public IMessageReader CreateRequestReader()
        {
            return new HmBinaryMessageReader(MessageStream);
        }
        public IMessageBuilder CreateRequestWriter()
        {
            return new HmBinaryMessageWriter(MessageStream);
        }

        public void CopyTo(BinaryMessage target)
        {
            MessageStream.CopyTo(target.MessageStream);
        }
    }
}
