using Lamar;
using Shouldly;
using System.Runtime.CompilerServices;

namespace Colosoft.Mediator.Test
{
    public class CreateStreamTests
    {
        public interface IPong
        {
            string? Message { get; }
        }
        public interface IPing : IStreamRequest<IPong>
        {
            string? Message { get; }
        }

        private class Ping : IPing
        {
            public string? Message { get; set; }
        }

        private class Pong : IPong
        {
            public string? Message { get; set; }
        }

        public class PingStreamHandler : IStreamRequestHandler<IPing, IPong>
        {
            public async IAsyncEnumerable<IPong> Handle(IPing request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return await Task.Run(() => new Pong { Message = request.Message + " Pang" });
            }
        }

        [Fact]
        public async Task Should_resolve_main_handler()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(CreateStreamTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IStreamRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            var response = mediator.CreateStream(new Ping { Message = "Ping" });
            int i = 0;
            await foreach (var result in response)
            {
                if (i == 0)
                {
                    result.Message.ShouldBe("Ping Pang");
                }

                i++;
            }

            i.ShouldBe(1);
        }

        [Fact]
        public async Task Should_resolve_main_handler_via_dynamic_dispatch()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(CreateStreamTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IStreamRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            object request = new Ping { Message = "Ping" };
            var response = mediator.CreateStream(request);
            int i = 0;
            await foreach (Pong? result in response)
            {
                if (i == 0)
                {
                    result!.Message.ShouldBe("Ping Pang");
                }

                i++;
            }

            i.ShouldBe(1);
        }

        [Fact]
        public async Task Should_resolve_main_handler_by_specific_interface()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(CreateStreamTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IStreamRequestHandler<,>));
                });
                cfg.For<ISender>().Use<Mediator>();
            });

            var mediator = container.GetInstance<ISender>();
            var response = mediator.CreateStream(new Ping { Message = "Ping" });
            int i = 0;
            await foreach (var result in response)
            {
                if (i == 0)
                {
                    result.Message.ShouldBe("Ping Pang");
                }

                i++;
            }

            i.ShouldBe(1);
        }

        [Fact]
        public void Should_raise_execption_on_null_request()
        {
            var container = new Container(cfg =>
            {
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            Should.Throw<ArgumentNullException>(() => mediator.CreateStream((Ping)null!));
        }

        [Fact]
        public void Should_raise_execption_on_null_request_via_dynamic_dispatch()
        {
            var container = new Container(cfg =>
            {
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            Should.Throw<ArgumentNullException>(() => mediator.CreateStream((object)null!));
        }
    }
}