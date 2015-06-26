namespace HmLib.Abstractions
{
    public interface IRequestHandler
    {
        void HandleRequest(IRequestContext requestContext);
    }
}
