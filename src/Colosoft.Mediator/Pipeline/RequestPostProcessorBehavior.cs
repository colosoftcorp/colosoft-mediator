using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Pipeline
{
    public class RequestPostProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors;

        public RequestPostProcessorBehavior(IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors)
            => this.postProcessors = postProcessors;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next().ConfigureAwait(false);

            foreach (var processor in this.postProcessors)
            {
                await processor.Process(request, response, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
    }
}