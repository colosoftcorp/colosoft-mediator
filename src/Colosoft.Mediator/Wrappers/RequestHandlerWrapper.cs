using System;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    public abstract class RequestHandlerWrapper : RequestHandlerBase
    {
        public abstract Task<Unit> Handle(
            IRequest request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }
}