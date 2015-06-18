﻿namespace HmLib
{
    public class Response : Message
    {
        public Response() : base(Serialization.MessageType.Response)
        {

        }

        public object Content { get; set; }

        public override string ToString()
        {
            return string.Concat( "Response: ", Content);
        }
    }
}
