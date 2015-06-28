using System.Threading.Tasks;

namespace HmLib.Abstractions
{
    public interface IRequestHandler
    {
        Task HandleRequest(IRequestContext requestContext);
    }
}
