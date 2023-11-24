using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Wrappers
{
    public class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
        where TRequest : IRequest
    {
        public override async Task<object> Handle(
            object request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken) =>
            await this.Handle((IRequest)request, serviceProvider, cancellationToken).ConfigureAwait(false);

        public override Task<Unit> Handle(
            IRequest request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            async Task<Unit> Handler()
            {
                await serviceProvider.GetRequiredService<IRequestHandler<TRequest>>()
                    .Handle((TRequest)request, cancellationToken);

                return Unit.Value;
            }

            return serviceProvider
                .GetServices<IPipelineBehavior<TRequest, Unit>>()
                .Reverse()
                .Aggregate(
                    (RequestHandlerDelegate<Unit>)Handler,
                    (next, pipeline) => () => pipeline.Handle((TRequest)request, next, cancellationToken))();
        }
    }
}