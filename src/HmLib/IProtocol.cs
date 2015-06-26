namespace HmLib
{
    using Abstractions;

    public interface IProtocol
    {
        void ReadResponse(IMessageReader input, IMessageBuilder output);

        void WriteRequest(IMessageBuilder output, Request request);
        void WriteResponse(IMessageBuilder output, object response);
        void WriteErrorResponse(IMessageBuilder output, string errorMessage);
    }
}