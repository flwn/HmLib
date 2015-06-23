namespace HmLib.Serialization
{
    public enum ReadState
    {
        Initial,
        Message,
        Headers,
        Body,
        EndOfFile,
    }

    public interface IMessageReader : IObjectReader
    {
        ReadState ReadState { get; }

        MessageType MessageType { get; }
    }
}
