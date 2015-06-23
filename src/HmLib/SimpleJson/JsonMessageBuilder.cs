using System.IO;

namespace HmLib.SimpleJson
{
    using Serialization;

    public class JsonMessageBuilder : JsonObjectBuilder, IMessageBuilder
    {
        public JsonMessageBuilder(TextWriter writer = null) : base(writer)
        {
        }


        public void BeginMessage(MessageType messageType) { }
        public void EndMessage()
        {
        }

        private int HeaderCount { get; set; }

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
