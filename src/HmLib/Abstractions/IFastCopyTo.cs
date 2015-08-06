
using System.Threading;
using System.Threading.Tasks;

namespace HmLib.Abstractions
{
    internal interface IFastCopyTo<T>
    {
        Task CopyTo(T target, CancellationToken cancellation);
    }
}
