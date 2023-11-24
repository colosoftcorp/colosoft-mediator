using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Pipeline
{
    public class RequestPreProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IEnumerable<IRequestPreProcessor<TRequest>> preProcessors;

        public RequestPreProcessorBehavior(IEnumerable<IRequestPreProcessor<TRequest>> preProcessors)
            => this.preProcessors = preProcessors;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            foreach (var processor in this.preProcessors)
            {
                await processor.Process(request, cancellationToken).ConfigureAwait(false);
            }

            return await next().ConfigureAwait(false);
        }
    }
}