using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public interface IRequestHandler<in TRequest>
        where TRequest : IRequest
    {
        Task Handle(TRequest request, CancellationToken cancellationToken);
    }
}
