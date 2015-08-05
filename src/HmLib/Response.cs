namespace HmLib
{
    using Abstractions;
    using Serialization;

    public class Response : Message, IResponseMessage
    {
        public Response() : base(MessageType.Response)
        {

        }

        public virtual object Content { get; set; }

        public override string ToString()
        {
            return string.Concat( "Response: ", Content);
        }

        public IMessageReader GetMessageReader()
        {
            return new MessageReader(this);
        }
    }
}
