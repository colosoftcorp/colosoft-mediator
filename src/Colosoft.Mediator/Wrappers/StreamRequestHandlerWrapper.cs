using System;
using System.Collections.Generic;
using System.Threading;

namespace Colosoft.Mediator.Wrappers
{
    internal abstract class StreamRequestHandlerBase
    {
        public abstract IAsyncEnumerable<object> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }
}