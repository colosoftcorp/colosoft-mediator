using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
    public interface ISender
    {
        Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>;

        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest;

        Task<object> Send(object request, CancellationToken cancellationToken = default);

        IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IStreamRequest<TResponse>;

        IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);

        IAsyncEnumerable<object> CreateStream(object request, CancellationToken cancellationToken = default);
    }
}