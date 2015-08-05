using System.IO;

namespace HmLib.SimpleJson
{
    using System;
    using Abstractions;

    public class JsonMessageBuilder : JsonObjectBuilder, IMessageBuilder, IHasResult<string>
    {
        public JsonMessageBuilder(TextWriter writer = null) : base(writer)
        {
        }


        public void BeginMessage(MessageType messageType) { }
        public void EndMessage()
        {
        }

        private int HeaderCount { get; set; }

        public string Result => base.ToString();

        public void SetMethod(string method)
        {
        }
        public void BeginHeaders(int headerCount)
        {
            HeaderCount = headerCount;
        }
        public void WriteHeader(string key, string value)
        {

        }
        public void EndHeaders()
        {

        }

        public void BeginContent()
        {
        }
        public void EndContent()
        {
        }
    }
}
