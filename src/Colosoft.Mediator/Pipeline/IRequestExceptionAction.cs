using System;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Pipeline
{
    public interface IRequestExceptionAction<in TRequest, in TException>
        where TException : Exception
    {
        Task Execute(TRequest request, TException exception, CancellationToken cancellationToken);
    }
}