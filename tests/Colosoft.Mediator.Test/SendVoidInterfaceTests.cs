using Lamar;
using Shouldly;
using System.Text;

namespace Colosoft.Mediator.Test
{
    public class SendVoidInterfaceTests
    {
        public interface IPing : IRequest
        {
            string? Message { get; }
        }

        public class Ping : IPing
        {
            public string? Message { get; set; }
        }

        public class PingHandler : IRequestHandler<IPing>
        {
            private readonly TextWriter writer;

            public PingHandler(TextWriter writer) => this.writer = writer;

            public Task Handle(IPing request, CancellationToken cancellationToken)
                => this.writer.WriteAsync(request.Message + " Pong");
        }

        [Fact]
        public async Task Should_resolve_main_void_handler()
        {
            var builder = new StringBuilder();
            var writer = new StringWriter(builder);

            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(PublishTests));
                    scanner.IncludeNamespaceContainingType<IPing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                });
                cfg.For<TextWriter>().Use(writer);
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await mediator.Send(new Ping { Message = "Ping" });

            builder.ToString().ShouldBe("Ping Pong");
        }
    }
}