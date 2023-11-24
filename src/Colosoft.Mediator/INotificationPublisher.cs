using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public interface INotificationPublisher
    {
        Task Publish(
            IEnumerable<NotificationHandlerExecutor> handlerExecutors,
            INotification notification,
            CancellationToken cancellationToken);
    }
}