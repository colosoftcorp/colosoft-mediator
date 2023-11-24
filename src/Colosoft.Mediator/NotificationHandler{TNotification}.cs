using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public abstract class NotificationHandler<TNotification> : INotificationHandler<TNotification>
        where TNotification : INotification
    {
        Task INotificationHandler<TNotification>.Handle(TNotification notification, CancellationToken cancellationToken)
        {
            this.Handle(notification);
            return Task.CompletedTask;
        }

        protected abstract void Handle(TNotification notification);
    }
}