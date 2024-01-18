#if NETSTANDARD2_0
using Dasync.Collections;
#endif
using Microsoft.Extensions.DependencyInjection;
using System;
#if !NETSTANDARD2_0
using System.Collections;
#endif
using System.Collections.Generic;
using System.Linq;
#if !NETSTANDARD2_0
using System.Runtime.CompilerServices;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    internal class StreamRequestHandlerWrapperImpl<TRequest, TResponse>
        : StreamRequestHandlerWrapper<TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
#if NETSTANDARD2_0
        private static IAsyncEnumerable<T> NextWrapper<T>(
            IAsyncEnumerable<T> items,
            CancellationToken cancellationToken)
#else
        private static async IAsyncEnumerable<T> NextWrapper<T>(
            IAsyncEnumerable<T> items,
            [EnumeratorCancellation] CancellationToken cancellationToken)
#endif
        {
            var cancellable = items
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);

#if NETSTANDARD2_0
            return new AsyncEnumerable<T>(
                async yield =>
                {
                    var enumerator = cancellable.GetAsyncEnumerator();

                    try
                    {
                        while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
                        {
                            await yield.ReturnAsync(enumerator.Current);
                        }
                    }
                    finally
                    {
                        await enumerator.DisposeAsync();
                    }
                });
#else
            await foreach (var item in cancellable)
            {
                yield return item;
            }
#endif
        }

#if NETSTANDARD2_0
        public override IAsyncEnumerable<object> Handle(
            object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
#else
        public override async IAsyncEnumerable<object> Handle(
            object request, IServiceProvider serviceProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
#endif
        {
            var enumerable = this.Handle((IStreamRequest<TResponse>)request, serviceProvider, cancellationToken);

#if NETSTANDARD2_0
            return new AsyncEnumerable<object>(
                async yield =>
                {
                    var enumerator = enumerable.GetAsyncEnumerator();

                    try
                    {
                        while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
                        {
                            await yield.ReturnAsync(enumerator.Current);
                        }
                    }
                    finally
                    {
                        await enumerator.DisposeAsync();
                    }
                });
#else
            await foreach (var item in enumerable)
            {
                yield return item;
            }
#endif
        }

#if NETSTANDARD2_0
        public override IAsyncEnumerable<TResponse> Handle(
            IStreamRequest<TResponse> request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
#else
        public override async IAsyncEnumerable<TResponse> Handle(
            IStreamRequest<TResponse> request,
            IServiceProvider serviceProvider,
            [EnumeratorCancellation] CancellationToken cancellationToken)
#endif
        {
            IAsyncEnumerable<TResponse> Handler() => serviceProvider
                .GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>()
                .Handle((TRequest)request, cancellationToken);

            var items = serviceProvider
                .GetServices<IStreamPipelineBehavior<TRequest, TResponse>>()
                .Reverse()
                .Aggregate(
                    (StreamHandlerDelegate<TResponse>)Handler,
                    (next, pipeline) => () => pipeline.Handle(
                        (TRequest)request,
                        () => NextWrapper(next(), cancellationToken),
                        cancellationToken))();

#if NETSTANDARD2_0
            var enumerable = items.WithCancellation(cancellationToken);

            return new AsyncEnumerable<TResponse>(
                async yield =>
                {
                    var enumerator = enumerable.GetAsyncEnumerator();

                    try
                    {
                        while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
                        {
                            await yield.ReturnAsync(enumerator.Current);
                        }
                    }
                    finally
                    {
                        await enumerator.DisposeAsync();
                    }
                });
#else
            await foreach (var item in items.WithCancellation(cancellationToken))
            {
                yield return item;
            }
#endif
        }
    }
}