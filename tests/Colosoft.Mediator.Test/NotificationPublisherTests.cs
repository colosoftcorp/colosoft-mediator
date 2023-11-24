using Colosoft.Mediator.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Colosoft.Mediator.Test
{
    public class NotificationPublisherTests
    {
        private readonly ITestOutputHelper output;

        public NotificationPublisherTests(ITestOutputHelper output) => this.output = output;

        public class Notification : INotification
        {
        }

        public class FirstHandler : INotificationHandler<Notification>
        {
            public async Task Handle(Notification notification, CancellationToken cancellationToken)
                => await Task.Delay(500, cancellationToken);
        }
        public class SecondHandler : INotificationHandler<Notification>
        {
            public async Task Handle(Notification notification, CancellationToken cancellationToken)
                => await Task.Delay(250, cancellationToken);
        }

        [Fact]
        public async Task Should_handle_sequentially_by_default()
        {
            var services = new ServiceCollection();
            services.AddMediator(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<Notification>();
            });
            var serviceProvider = services.BuildServiceProvider();

            var mediator = serviceProvider.GetRequiredService<IMediator>();

            var timer = new Stopwatch();
            timer.Start();

            await mediator.Publish(new Notification());

            timer.Stop();

            var sequentialElapsed = timer.ElapsedMilliseconds;

            services = new ServiceCollection();
            services.AddMediator(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<Notification>();
                cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);
            });
            serviceProvider = services.BuildServiceProvider();

            mediator = serviceProvider.GetRequiredService<IMediator>();

            timer.Restart();

            await mediator.Publish(new Notification());

            timer.Stop();

            var parallelElapsed = timer.ElapsedMilliseconds;

            sequentialElapsed.ShouldBeGreaterThan(parallelElapsed);
        }
    }
}