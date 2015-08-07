namespace HmLib
{
    using Abstractions;
    using Serialization;

    public class Response : Message, IResponseMessage
    {
        public Response() : base(MessageType.Response)
        {

        }


        public override string ToString() => string.Concat("Response: ", Content);

        public IMessageReader GetMessageReader() => new MessageReader(this);

        public virtual bool IsErrorResponse => false;
    }
}
