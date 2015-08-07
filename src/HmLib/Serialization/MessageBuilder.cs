namespace HmLib.Serialization
{
    using Abstractions;

    public class MessageBuilder : ObjectBuilder, IMessageBuilder, IHasResult<Request>, IHasResult<Response>
    {
        public MessageBuilder()
        {

        }

        public MessageBuilder(Response response)
        {
            Result = response;
        }

        public MessageBuilder(Request request)
        {
            Result = request;
        }

        private int HeaderCount { get; set; }

        public void BeginMessage(MessageType messageType)
        {
            if (Result == null)
            {
                Result = Message.Create(messageType);
            }

#if DEBUG
            var count = 2;
            if (messageType == MessageType.Request) count++;
            Debug.BeginStruct(count);
            Debug.BeginItem();
            Debug.WritePropertyName("messageType");
            Debug.WriteStringValue(messageType.ToString());
            Debug.EndItem();
#endif
        }
        public void EndMessage()
        {
#if DEBUG
            Debug.EndStruct();
#endif
        }

        public void SetMethod(string method)
        {
            ((Request)Result).Method = method;
#if DEBUG
            Debug.WriteStringValue(method);
            Debug.EndItem();
            Debug.BeginItem();
            Debug.WritePropertyName("params");
#endif
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
#if DEBUG
            Debug.BeginItem();
            var propName = Result.Type == MessageType.Request ? "method" : "content";
            Debug.WritePropertyName(propName);
#endif
        }

        public void EndContent()
        {
#if DEBUG
            Debug.EndItem();
#endif
            if (Result.Type == MessageType.Request)
            {
                Result.Content = StructResult;
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

        Request IHasResult<Request>.Result => (Request)Result;
        Response IHasResult<Response>.Result => (Response)Result;
    }
}
