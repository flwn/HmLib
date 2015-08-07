namespace HmLib.Abstractions
{
    public enum ReadState
    {
        Initial,
        Message,
        Headers,
        Body,
        EndOfFile,
        Error,
    }

    public interface IMessageReader : IObjectReader
    {
        ReadState ReadState { get; }

        MessageType MessageType { get; }
    }
}
