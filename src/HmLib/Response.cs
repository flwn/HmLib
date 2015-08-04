namespace HmLib
{
    using Abstractions;

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
    }
}
