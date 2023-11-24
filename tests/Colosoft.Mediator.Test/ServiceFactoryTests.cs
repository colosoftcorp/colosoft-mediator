using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.Mediator.Test
{
    public class ServiceFactoryTests
    {
        public class Ping : IRequest<Pong>
        {
        }

        public class Pong
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
                () => mediator.Send(new Ping())
            );
        }
    }
}