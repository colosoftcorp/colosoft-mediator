using System.Collections.Generic;
using System.Threading;

namespace Colosoft.Mediator
{
    public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();

    public interface IStreamPipelineBehavior<in TRequest, TResponse>
    {
        IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }
}