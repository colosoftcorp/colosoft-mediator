using System;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    public abstract class RequestHandlerBase
    {
        public abstract Task<object> Handle(
            object request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }
}