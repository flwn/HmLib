namespace HmLib.Abstractions
{
    public interface IResponseMessage
    {
        IMessageReader GetMessageReader();

        bool IsErrorResponse { get; }
    }
}
