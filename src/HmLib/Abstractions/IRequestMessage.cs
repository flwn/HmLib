namespace HmLib.Abstractions
{
    public interface IRequestMessage
    {
        IMessageReader GetMessageReader();
    }

}
