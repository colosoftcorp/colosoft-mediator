using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public interface IPublisher
    {
        Task Publish(object notification, CancellationToken cancellationToken = default);

        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}