namespace HmLib
{
    public class Response : Message
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
