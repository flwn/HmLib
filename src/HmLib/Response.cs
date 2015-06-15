namespace HmLib
{
    public class Response
    {
        public bool IsError { get; set; }

        public string Content { get; set; }

        public override string ToString()
        {
            return string.Concat(IsError ? "Error: " : "Success: ", Content);
        }
    }
}
