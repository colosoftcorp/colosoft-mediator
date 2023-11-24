using System;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    public abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
    {
        public abstract Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }
}