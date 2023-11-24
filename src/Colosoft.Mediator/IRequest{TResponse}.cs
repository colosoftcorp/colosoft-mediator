namespace Colosoft.Mediator
{
#pragma warning disable S2326 // Unused type parameters should be removed
    public interface IRequest<out TResponse> : IBaseRequest
#pragma warning restore S2326 // Unused type parameters should be removed
    {
    }
}