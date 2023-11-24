using System;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public class NotificationHandlerExecutor
    {
        public NotificationHandlerExecutor(object handlerInstance, Func<INotification, CancellationToken, Task> handlerCallback)
        {
            this.HandlerInstance = handlerInstance;
            this.HandlerCallback = handlerCallback;
        }

        public object HandlerInstance { get; }

        public Func<INotification, CancellationToken, Task> HandlerCallback { get; }
    }
}