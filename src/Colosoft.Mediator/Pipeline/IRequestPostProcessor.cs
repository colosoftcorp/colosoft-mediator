using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Pipeline
{
    public interface IRequestPostProcessor<in TRequest, in TResponse>
    {
        Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
    }
}