namespace HmLib.Serialization
{
    public enum HmMessagePart
    {
        None,
        Message,
        Headers,
        Body,
        EndOfFile,
    }
    public interface IMessageReader
    {

        bool Read();

        HmMessagePart MessagePart { get; }

        MessageType MessageType { get; }

        int CollectionCount { get; }

        string PropertyName { get; }

        string StringValue { get; }

        int IntValue { get; }

        double DoubleValue { get;  }

        bool BooleanValue { get; }

        ContentType? ValueType { get; }

    }
}
