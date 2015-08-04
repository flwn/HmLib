namespace HmLib.Abstractions
{
    public interface IMessageConverter
    {
        void Convert(IResponseMessage source, IMessageBuilder usingBuilder);
        void Convert(IRequestMessage source, IMessageBuilder usingBuilder);

        void Convert(IMessageReader input, IMessageBuilder output);

        TResult Convert<TResult>(IRequestMessage source)
            where TResult : IRequestMessage;
        TResponse Convert<TResponse>(IResponseMessage source)
            where TResponse : IResponseMessage;

        void Convert<TResult>(IRequestMessage source, TResult targetMessage)
            where TResult : IRequestMessage;

    }
}
