using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.Mediator.Test
{
    public class ServiceFactoryTests
    {
        public interface IPong
        {
            string? Message { get; }
        }

        public interface IPing : IRequest<IPong>
        {
        }

        public class Ping : IPing
        {
        }

        public class Pong : IPong
        {
            public string? Message { get; set; }
        }

        [Fact]
        public async Task Should_throw_given_no_handler()
        {
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var mediator = new Mediator(serviceProvider);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => mediator.Send(new Ping()));
        }
    }
}