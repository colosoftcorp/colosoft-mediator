using Shouldly;
using System.Text;

namespace Colosoft.Mediator.Test
{
    public class NotificationHandlerTests
    {
        public interface IPing : INotification
        {
            string? Message { get; }
        }

        public class Ping : IPing
        {
            public string? Message { get; set; }
        }

        public class PongChildHandler : NotificationHandler<IPing>
        {
            private readonly TextWriter writer;

            public PongChildHandler(TextWriter writer)
            {
                this.writer = writer;
            }

            protected override void Handle(IPing notification)
            {
                this.writer.WriteLine(notification.Message + " Pong");
            }
        }

        [Fact]
        public async Task Should_call_abstract_handle_method()
        {
            var builder = new StringBuilder();
            var writer = new StringWriter(builder);

            INotificationHandler<Ping> handler = new PongChildHandler(writer);

            await handler.Handle(
                new Ping() { Message = "Ping" },
                default
            );

            var result = builder.ToString();
            result.ShouldContain("Ping Pong");
        }
    }
}