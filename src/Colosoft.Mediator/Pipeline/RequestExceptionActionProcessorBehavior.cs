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
    public class RequestExceptionActionProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IServiceProvider serviceProvider;

        public RequestExceptionActionProcessorBehavior(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        private static IEnumerable<Type> GetExceptionTypes(Type exceptionType)
        {
            while (exceptionType != null && exceptionType != typeof(object))
            {
                yield return exceptionType;
                exceptionType = exceptionType.BaseType;
            }
        }

        private static MethodInfo GetMethodInfoForAction(Type exceptionType)
        {
            var exceptionActionInterfaceType = typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), exceptionType);

            var actionMethodInfo =
                exceptionActionInterfaceType.GetMethod(nameof(IRequestExceptionAction<TRequest, Exception>.Execute))
                ?? throw new InvalidOperationException(
                    $"Could not find method {nameof(IRequestExceptionAction<TRequest, Exception>.Execute)} on type {exceptionActionInterfaceType}");

            return actionMethodInfo;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var exceptionTypes = GetExceptionTypes(exception.GetType());

                var actionsForException = exceptionTypes
                    .SelectMany(exceptionType => this.GetActionsForException(exceptionType, request))
                    .GroupBy(actionForException => actionForException.Action.GetType())
                    .Select(actionForException => actionForException.First())
                    .Select(actionForException => (MethodInfo: GetMethodInfoForAction(actionForException.ExceptionType), actionForException.Action))
                    .ToList();

                foreach (var actionForException in actionsForException)
                {
                    try
                    {
                        await ((Task)(actionForException.MethodInfo.Invoke(actionForException.Action, new object[] { request, exception, cancellationToken })
                                      ?? throw new InvalidOperationException($"Could not create task for action method {actionForException.MethodInfo}."))).ConfigureAwait(false);
                    }
                    catch (TargetInvocationException invocationException) when (invocationException.InnerException != null)
                    {
                        ExceptionDispatchInfo.Capture(invocationException.InnerException).Throw();
                    }
                }

                throw;
            }
        }

        private IEnumerable<(Type ExceptionType, object Action)> GetActionsForException(Type exceptionType, TRequest request)
        {
            var exceptionActionInterfaceType = typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), exceptionType);
            var enumerableExceptionActionInterfaceType = typeof(IEnumerable<>).MakeGenericType(exceptionActionInterfaceType);

            var actionsForException = (IEnumerable<object>)this.serviceProvider.GetRequiredService(enumerableExceptionActionInterfaceType);

            return HandlersOrderer.Prioritize(actionsForException.ToList(), request)
                .Select(action => (exceptionType, action));
        }
    }
}
