using Colosoft.Mediator.NotificationPublishers;
using Colosoft.Mediator.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public class Mediator : IMediator
    {
        private static readonly ConcurrentDictionary<Type, RequestHandlerBase> RequestHandlers = new ConcurrentDictionary<Type, RequestHandlerBase>();
        private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new ConcurrentDictionary<Type, NotificationHandlerWrapper>();
        private static readonly ConcurrentDictionary<Type, StreamRequestHandlerBase> StreamRequestHandlers = new ConcurrentDictionary<Type, StreamRequestHandlerBase>();

        private readonly IServiceProvider serviceProvider;
        private readonly INotificationPublisher publisher;

        public Mediator(IServiceProvider serviceProvider)
            : this(serviceProvider, new ForeachAwaitPublisher())
        {
        }

        public Mediator(IServiceProvider serviceProvider, INotificationPublisher publisher)
        {
            this.serviceProvider = serviceProvider;
            this.publisher = publisher;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var handler = (RequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd(typeof(TRequest), requestType =>
            {
                var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
                return (RequestHandlerBase)wrapper;
            });

            return handler.Handle(request, this.serviceProvider, cancellationToken);
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var handler = (RequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd(request.GetType(), requestType =>
            {
                var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
                return (RequestHandlerBase)wrapper;
            });

            return handler.Handle(request, this.serviceProvider, cancellationToken);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var handler = (RequestHandlerWrapper)RequestHandlers.GetOrAdd(typeof(TRequest), requestType =>
            {
                var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
                return (RequestHandlerBase)wrapper;
            });

            return handler.Handle(request, this.serviceProvider, cancellationToken);
        }

        public Task<object> Send(object request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var handler = RequestHandlers.GetOrAdd(request.GetType(), requestType =>
            {
                Type wrapperType;

                var requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
                if (requestInterfaceType is null)
                {
                    requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(i => i == typeof(IRequest));
                    if (requestInterfaceType is null)
                    {
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one
                        throw new ArgumentException($"{requestType.Name} does not implement {nameof(IRequest)}", nameof(request));
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one
                    }

                    wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
                }
                else
                {
                    var responseType = requestInterfaceType.GetGenericArguments()[0];
                    wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
                }

                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}");
                return (RequestHandlerBase)wrapper;
            });

            return handler.Handle(request, this.serviceProvider, cancellationToken);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var notificationType = typeof(TNotification);

            if (notificationType == typeof(INotification))
            {
                notificationType = notification.GetType();
            }

            return this.PublishNotification(notification, notificationType, cancellationToken);
        }

        public async Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }
            else if (notification is INotification instance)
            {
                await this.PublishNotification(instance, notification.GetType(), cancellationToken);
            }
            else
            {
                throw new ArgumentException($"{nameof(notification)} does not implement ${nameof(INotification)}");
            }
        }

        protected virtual Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken) =>
            this.publisher.Publish(handlerExecutors, notification, cancellationToken);

        private Task PublishNotification(
            INotification notification,
            Type type,
            CancellationToken cancellationToken)
        {
            var handler = NotificationHandlers.GetOrAdd(type, notificationType =>
            {
                var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType);
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {notificationType}");
                return (NotificationHandlerWrapper)wrapper;
            });

            return handler.Handle(notification, this.serviceProvider, this.PublishCore, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IStreamRequest<TResponse>
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var streamHandler = (StreamRequestHandlerWrapper<TResponse>)StreamRequestHandlers.GetOrAdd(typeof(TRequest), requestType =>
            {
                var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}");
                return (StreamRequestHandlerBase)wrapper;
            });

            var items = streamHandler.Handle(request, this.serviceProvider, cancellationToken);

            return items;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var streamHandler = (StreamRequestHandlerWrapper<TResponse>)StreamRequestHandlers.GetOrAdd(request.GetType(), requestType =>
            {
                var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}");
                return (StreamRequestHandlerBase)wrapper;
            });

            var items = streamHandler.Handle(request, this.serviceProvider, cancellationToken);

            return items;
        }

        public IAsyncEnumerable<object> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var handler = StreamRequestHandlers.GetOrAdd(request.GetType(), requestType =>
            {
                var requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>));
                if (requestInterfaceType is null)
                {
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one
                    throw new ArgumentException($"{requestType.Name} does not implement IStreamRequest<TResponse>", nameof(request));
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one
                }

                var responseType = requestInterfaceType.GetGenericArguments()[0];
                var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}");
                return (StreamRequestHandlerBase)wrapper;
            });

            var items = handler.Handle(request, this.serviceProvider, cancellationToken);

            return items;
        }
    }
}
