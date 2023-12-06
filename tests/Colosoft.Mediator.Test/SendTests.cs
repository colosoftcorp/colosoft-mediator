using Lamar;
using Shouldly;

namespace Colosoft.Mediator.Test
{
    public class SendTests
    {
        public interface IPong
        {
            string? Message { get; }
        }

        public interface IPing : IRequest<IPong>
        {
            string? Message { get; }
        }

        public interface IVoidPing : IRequest
        {
        }

        public class Ping : IPing
        {
            public string? Message { get; set; }
        }

        public class VoidPing : IVoidPing
        {
        }

        public class Pong : IPong
        {
            public string? Message { get; set; }
        }

        public class PingHandler : IRequestHandler<IPing, IPong>
        {
            public Task<IPong> Handle(IPing request, CancellationToken cancellationToken)
            {
                return Task.FromResult<IPong>(new Pong { Message = request.Message + " Pong" });
            }
        }

        public class Dependency
        {
            public bool Called { get; set; }
        }

        public class VoidPingHandler : IRequestHandler<VoidPing>
        {
            private readonly Dependency dependency;

            public VoidPingHandler(Dependency dependency) => this.dependency = dependency;

            public Task Handle(VoidPing request, CancellationToken cancellationToken)
            {
                this.dependency.Called = true;

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Should_resolve_main_handler()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(SendTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            var response = await mediator.Send(new Ping { Message = "Ping" });

            response.Message.ShouldBe("Ping Pong");
        }

        [Fact]
        public async Task Should_resolve_main_void_handler()
        {
            var dependency = new Dependency();

            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(SendTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.ForSingletonOf<Dependency>().Use(dependency);
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await mediator.Send(new VoidPing());

            dependency.Called.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_resolve_main_handler_via_dynamic_dispatch()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(SendTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            object request = new Ping { Message = "Ping" };
            var response = await mediator.Send(request);

            var pong = response.ShouldBeOfType<Pong>();
            pong.Message.ShouldBe("Ping Pong");
        }

        [Fact]
        public async Task Should_resolve_main_void_handler_via_dynamic_dispatch()
        {
            var dependency = new Dependency();

            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(SendTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.ForSingletonOf<Dependency>().Use(dependency);
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            object request = new VoidPing();
            var response = await mediator.Send(request);

            response.ShouldBeOfType<Unit>();

            dependency.Called.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_resolve_main_handler_by_specific_interface()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(SendTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<ISender>().Use<Mediator>();
            });

            var mediator = container.GetInstance<ISender>();

            var response = await mediator.Send(new Ping { Message = "Ping" });

            response.Message.ShouldBe("Ping Pong");
        }

        [Fact]
        public async Task Should_resolve_main_handler_by_given_interface()
        {
            var dependency = new Dependency();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(SendTests));
                    scanner.IncludeNamespaceContainingType<VoidPing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                });
                cfg.ForSingletonOf<Dependency>().Use(dependency);
                cfg.For<ISender>().Use<Mediator>();
            });

            var mediator = container.GetInstance<ISender>();

            var requests = new IRequest[] { new VoidPing() };
            await mediator.Send(requests[0]);

            dependency.Called.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_raise_execption_on_null_request()
        {
            var container = new Container(cfg =>
            {
                cfg.For<ISender>().Use<Mediator>();
            });

            var mediator = container.GetInstance<ISender>();

            await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Send(default!));
        }
    }
}