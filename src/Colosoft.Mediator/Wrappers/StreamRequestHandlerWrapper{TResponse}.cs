using System;
using System.Collections.Generic;
using System.Threading;

namespace Colosoft.Mediator.Wrappers
{
    internal abstract class StreamRequestHandlerWrapper<TResponse> : StreamRequestHandlerBase
    {
        public abstract IAsyncEnumerable<TResponse> Handle(
            IStreamRequest<TResponse> request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }
}