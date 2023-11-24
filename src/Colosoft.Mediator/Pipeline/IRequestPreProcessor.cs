using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Pipeline
{
    public interface IRequestPreProcessor<in TRequest>
    {
        Task Process(TRequest request, CancellationToken cancellationToken);
    }
}