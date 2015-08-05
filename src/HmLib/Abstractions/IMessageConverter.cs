namespace HmLib.Abstractions
{
    public interface IMessageConverter
    {

        TResponse Convert<TResponse>(IResponseMessage source, IHasResult<TResponse> usingBuilder);

        TResult Convert<TResult>(IRequestMessage source, IHasResult<TResult> usingBuilder);

        TResult Convert<TResult>(IRequestMessage source)
            where TResult : IRequestMessage;
        TResponse Convert<TResponse>(IResponseMessage source)
            where TResponse : IResponseMessage;


    }
}
