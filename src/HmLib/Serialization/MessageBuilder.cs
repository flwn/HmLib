using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Serialization
{
    public class MessageBuilder : ObjectBuilder, IMessageBuilder
    {
        private int HeaderCount { get; set; }

        public void BeginMessage(MessageType messageType)
        {
            Result = Message.Create(messageType);
        }
        public void EndMessage() { }

        public void SetMethod(string method)
        {
            ((Request)Result).Method = method;
        }

        public void BeginHeaders(int headerCount)
        {
            HeaderCount = headerCount;
        }
        public void WriteHeader(string key, string value)
        {
            ((Request)Result).SetHeader(key, value);
        }
        public void EndHeaders()
        {

        }

        public void BeginContent()
        {
        }

        public void EndContent()
        {
            if (Result.Type == MessageType.Request)
            {
                var request = (Request)Result;
                request.Parameters = CollectionResult;
            }
            else if (Result.Type == MessageType.Error)
            {
                var errorResponse = (ErrorResponse)Result;
                errorResponse.Content = StructResult;
            }
            else if (Result.Type == MessageType.Response)
            {
                var response = (Response)Result;
                response.Content = StructResult ?? (object)CollectionResult ?? SimpleResult;
            }
        }

        public Message Result { get; private set; }
    }
}
