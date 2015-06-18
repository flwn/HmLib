namespace HmLib.Serialization
{
    public interface IMessageBuilder : IObjectBuilder
    {

        void BeginMessage(MessageType messageType);
        void EndMessage();

        void SetMethod(string method);

        void BeginHeaders(int headerCount);
        void WriteHeader(string key, string value);
        void EndHeaders();

        void BeginContent();
        void EndContent();
    }
}
