namespace Colosoft.Mediator.Pipeline
{
    public class RequestExceptionHandlerState<TResponse>
    {
        public bool Handled { get; private set; }

        public TResponse Response { get; private set; }

        public void SetHandled(TResponse response)
        {
            this.Handled = true;
            this.Response = response;
        }
    }
}