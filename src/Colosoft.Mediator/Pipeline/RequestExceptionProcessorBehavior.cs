using Colosoft.Mediator.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Pipeline
{
    public class RequestExceptionProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IServiceProvider serviceProvider;

        public RequestExceptionProcessorBehavior(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        private static IEnumerable<Type> GetExceptionTypes(Type exceptionType)
        {
            while (exceptionType != null && exceptionType != typeof(object))
            {
                yield return exceptionType;
                exceptionType = exceptionType.BaseType;
            }
        }

        private static MethodInfo GetMethodInfoForHandler(Type exceptionType)
        {
            var exceptionHandlerInterfaceType = typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), exceptionType);

            var handleMethodInfo = exceptionHandlerInterfaceType.GetMethod(nameof(IRequestExceptionHandler<TRequest, TResponse, Exception>.Handle))
                               ?? throw new InvalidOperationException($"Could not find method {nameof(IRequestExceptionHandler<TRequest, TResponse, Exception>.Handle)} on type {exceptionHandlerInterfaceType}");

            return handleMethodInfo;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var state = new RequestExceptionHandlerState<TResponse>();

                var exceptionTypes = GetExceptionTypes(exception.GetType());

                var handlersForException = exceptionTypes
                    .SelectMany(exceptionType => this.GetHandlersForException(exceptionType, request))
                    .GroupBy(handlerForException => handlerForException.Handler.GetType())
                    .Select(handlerForException => handlerForException.First())
                    .Select(handlerForException => (MethodInfo: GetMethodInfoForHandler(handlerForException.ExceptionType), handlerForException.Handler))
                    .ToList();

                foreach (var handlerForException in handlersForException)
                {
                    try
                    {
                        await ((Task)(handlerForException.MethodInfo.Invoke(handlerForException.Handler, new object[] { request, exception, state, cancellationToken })
                                       ?? throw new InvalidOperationException("Did not return a Task from the exception handler."))).ConfigureAwait(false);
                    }
                    catch (TargetInvocationException invocationException) when (invocationException.InnerException != null)
                    {
                        ExceptionDispatchInfo.Capture(invocationException.InnerException).Throw();
                    }

                    if (state.Handled)
                    {
                        break;
                    }
                }

                if (!state.Handled)
                {
                    throw;
                }

                if (state.Response == null)
                {
                    throw;
                }

                return state.Response;
            }
        }

        private IEnumerable<(Type ExceptionType, object Handler)> GetHandlersForException(Type exceptionType, TRequest request)
        {
            var exceptionHandlerInterfaceType = typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), exceptionType);
            var enumerableExceptionHandlerInterfaceType = typeof(IEnumerable<>).MakeGenericType(exceptionHandlerInterfaceType);

            var exceptionHandlers = (IEnumerable<object>)this.serviceProvider.GetRequiredService(enumerableExceptionHandlerInterfaceType);

            return HandlersOrderer.Prioritize(exceptionHandlers.ToList(), request)
                .Select(handler => (exceptionType, action: handler));
        }
    }
}