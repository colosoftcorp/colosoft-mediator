using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    public class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
        where TNotification : INotification
    {
        public override Task Handle(
            INotification notification,
            IServiceProvider serviceFactory,
            Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
            CancellationToken cancellationToken)
        {
            var handlers = serviceFactory
                .GetServices<INotificationHandler<TNotification>>()
                .Select(x => new NotificationHandlerExecutor(x, (theNotification, theToken) => x.Handle((TNotification)theNotification, theToken)));

            return publish(handlers, notification, cancellationToken);
        }
    }
}