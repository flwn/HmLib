using System;
using System.IO;

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


        public Stream MessageStream { get; set; }

        public void CopyTo(BinaryMessage target)
        {
            MessageStream.CopyTo(target.MessageStream);
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
