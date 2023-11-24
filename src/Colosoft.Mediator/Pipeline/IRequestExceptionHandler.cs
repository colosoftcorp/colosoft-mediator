using System;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Pipeline
{
    public interface IRequestExceptionHandler<in TRequest, TResponse, in TException>
        where TException : Exception
    {
        Task Handle(TRequest request, TException exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken);
    }
}
