using System.Threading.Tasks;

namespace HmLib.Abstractions
{
    public interface IRequestHandler
    {
        Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage);
    }
}
