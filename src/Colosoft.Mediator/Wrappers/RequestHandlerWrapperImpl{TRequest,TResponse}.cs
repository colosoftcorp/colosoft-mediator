using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    public class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override async Task<object> Handle(
            object request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken) =>
            await this.Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);

        public override Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            Task<TResponse> Handler() => serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                .Handle((TRequest)request, cancellationToken);

            return serviceProvider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse()
                .Aggregate(
                    (RequestHandlerDelegate<TResponse>)Handler,
                    (next, pipeline) => () => pipeline.Handle((TRequest)request, next, cancellationToken))();
        }
    }
}