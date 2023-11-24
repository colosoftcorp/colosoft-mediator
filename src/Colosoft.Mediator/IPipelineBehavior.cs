using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    public interface IPipelineBehavior<in TRequest, TResponse>
    {
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }
}