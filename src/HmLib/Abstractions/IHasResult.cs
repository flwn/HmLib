namespace HmLib.Abstractions
{
    public interface IHasResult<T> : IMessageBuilder
    {
        T Result
        {
            get;
        }
    }
}
