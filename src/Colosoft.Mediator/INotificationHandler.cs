using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}