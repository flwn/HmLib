namespace HmLib
{
    public class ErrorResponse : Response
    {
        public ErrorResponse()
        {
            Type = MessageType.Error;
        }


        public override string ToString()
        {
            return string.Concat("Error: ", Content);
        }
    }
}
