using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    public abstract class NotificationHandlerWrapper
    {
        public abstract Task Handle(
            INotification notification,
            IServiceProvider serviceFactory,
            Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
            CancellationToken cancellationToken);
    }
}