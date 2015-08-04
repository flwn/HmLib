using System.Threading.Tasks;

namespace HmLib.Abstractions
{
    public interface IRequestHandler
    {
        Task<IResponseMessage> HandleRequest(IRequestMessage requestContext);
    }

    public interface IRequestMessage
    {

    }

    public interface IResponseMessage
    {

    }
}
