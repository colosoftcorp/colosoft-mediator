using Lamar;
using Shouldly;

namespace Colosoft.Mediator.Test
{
    public class PipelineTests
    {
        public interface IPong
        {
            string? Message { get; }
        }

        public interface IPing : IRequest<IPong>
        {
            string? Message { get; }
        }

        public class Ping : IPing
        {
            public string? Message { get; set; }
        }

        public class Pong : IPong
        {
            public string? Message { get; set; }
        }

        public interface IVoidPing : IRequest
        {
            string? Message { get; }
        }

        public class VoidPing : IVoidPing
        {
            public string? Message { get; set; }
        }

        public interface IZong
        {
            string? Message { get; }
        }

        public interface IZing : IRequest<IZong>
        {
            string? Message { get; }
        }

        public class Zing : IZing
        {
            public string? Message { get; set; }
        }

        public class Zong : IZong
        {
            public string? Message { get; set; }
        }

        public class PingHandler : IRequestHandler<IPing, IPong>
        {
            private readonly Logger output;

            public PingHandler(Logger output)
            {
                this.output = output;
            }

            public Task<IPong> Handle(IPing request, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Handler");
                return Task.FromResult<IPong>(new Pong { Message = request.Message + " Pong" });
            }
        }

        public class VoidPingHandler : IRequestHandler<IVoidPing>
        {
            private readonly Logger output;

            public VoidPingHandler(Logger output)
            {
                this.output = output;
            }

            public Task Handle(IVoidPing request, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Handler");
                return Task.CompletedTask;
            }
        }

        public class ZingHandler : IRequestHandler<IZing, IZong>
        {
            private readonly Logger output;

            public ZingHandler(Logger output)
            {
                this.output = output;
            }

            public Task<IZong> Handle(IZing request, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Handler");
                return Task.FromResult<IZong>(new Zong { Message = request.Message + " Zong" });
            }
        }

        public class OuterBehavior : IPipelineBehavior<IPing, IPong>
        {
            private readonly Logger output;

            public OuterBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<IPong> Handle(IPing request, RequestHandlerDelegate<IPong> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Outer before");
                var response = await next();
                this.output.Messages.Add("Outer after");

                return response;
            }
        }

        public class OuterVoidBehavior : IPipelineBehavior<IVoidPing, Unit>
        {
            private readonly Logger output;

            public OuterVoidBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<Unit> Handle(IVoidPing request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Outer before");
                var response = await next();
                this.output.Messages.Add("Outer after");

                return response;
            }
        }

        public class InnerBehavior : IPipelineBehavior<IPing, IPong>
        {
            private readonly Logger output;

            public InnerBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<IPong> Handle(IPing request, RequestHandlerDelegate<IPong> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Inner before");
                var response = await next();
                this.output.Messages.Add("Inner after");

                return response;
            }
        }

        public class InnerVoidBehavior : IPipelineBehavior<IVoidPing, Unit>
        {
            private readonly Logger output;

            public InnerVoidBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<Unit> Handle(IVoidPing request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Inner before");
                var response = await next();
                this.output.Messages.Add("Inner after");

                return response;
            }
        }

        public class InnerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : notnull
        {
            private readonly Logger output;

            public InnerBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Inner generic before");
                var response = await next();
                this.output.Messages.Add("Inner generic after");

                return response;
            }
        }

        public class OuterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : notnull
        {
            private readonly Logger output;

            public OuterBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Outer generic before");
                var response = await next();
                this.output.Messages.Add("Outer generic after");

                return response;
            }
        }

        public class ConstrainedBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IPing
            where TResponse : IPong
        {
            private readonly Logger output;

            public ConstrainedBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Constrained before");
                var response = await next();
                this.output.Messages.Add("Constrained after");

                return response;
            }
        }

        public class ConcreteBehavior : IPipelineBehavior<IPing, IPong>
        {
            private readonly Logger output;

            public ConcreteBehavior(Logger output)
            {
                this.output = output;
            }

            public async Task<IPong> Handle(IPing request, RequestHandlerDelegate<IPong> next, CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Concrete before");
                var response = await next();
                this.output.Messages.Add("Concrete after");

                return response;
            }
        }

        public class Logger
        {
            public IList<string> Messages { get; } = new List<string>();
        }

        [Fact]
        public async Task Should_wrap_with_behavior()
        {
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(PublishTests));
                    scanner.IncludeNamespaceContainingType<IPing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);
                cfg.For<IPipelineBehavior<IPing, IPong>>().Add<OuterBehavior>();
                cfg.For<IPipelineBehavior<IPing, IPong>>().Add<InnerBehavior>();
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            var response = await mediator.Send(new Ping { Message = "Ping" });

            response.Message.ShouldBe("Ping Pong");

            output.Messages.ShouldBe(new[]
            {
                "Outer before",
                "Inner before",
                "Handler",
                "Inner after",
                "Outer after",
            });
        }

        [Fact]
        public async Task Should_wrap_void_with_behavior()
        {
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(PublishTests));
                    scanner.IncludeNamespaceContainingType<IPing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                });
                cfg.For<Logger>().Use(output);
                cfg.For<IPipelineBehavior<IVoidPing, Unit>>().Add<OuterVoidBehavior>();
                cfg.For<IPipelineBehavior<IVoidPing, Unit>>().Add<InnerVoidBehavior>();
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await mediator.Send(new VoidPing { Message = "Ping" });

            output.Messages.ShouldBe(new[]
            {
                "Outer before",
                "Inner before",
                "Handler",
                "Inner after",
                "Outer after",
            });
        }

        [Fact]
        public async Task Should_wrap_generics_with_behavior()
        {
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(PublishTests));
                    scanner.IncludeNamespaceContainingType<IPing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);

                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));

                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            var response = await mediator.Send(new Ping { Message = "Ping" });

            response.Message.ShouldBe("Ping Pong");

            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Handler",
                "Inner generic after",
                "Outer generic after",
            });
        }

        [Fact]
        public async Task Should_wrap_void_generics_with_behavior()
        {
            var output = new Logger();
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
                cfg.For<Logger>().Use(output);

                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));

                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await mediator.Send(new VoidPing { Message = "Ping" });

            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Handler",
                "Inner generic after",
                "Outer generic after",
            });
        }

        [Fact]
        public async Task Should_handle_constrained_generics()
        {
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(PublishTests));
                    scanner.IncludeNamespaceContainingType<IPing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);

                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));
                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(ConstrainedBehavior<,>));

                cfg.For<IMediator>().Use<Mediator>();
            });

            container.GetAllInstances<IPipelineBehavior<IPing, IPong>>();

            var mediator = container.GetInstance<IMediator>();

            var response = await mediator.Send(new Ping { Message = "Ping" });

            response.Message.ShouldBe("Ping Pong");

            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Constrained before",
                "Handler",
                "Constrained after",
                "Inner generic after",
                "Outer generic after",
            });

            output.Messages.Clear();

            var zingResponse = await mediator.Send(new Zing { Message = "Zing" });

            zingResponse.Message.ShouldBe("Zing Zong");

            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Handler",
                "Inner generic after",
                "Outer generic after",
            });
        }

        [Fact(Skip = "Lamar does not mix concrete and open generics. Use constraints instead.")]
        public async Task Should_handle_concrete_and_open_generics()
        {
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(PublishTests));
                    scanner.IncludeNamespaceContainingType<IPing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);

                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));
                cfg.For(typeof(IPipelineBehavior<IPing, IPong>)).Add(typeof(ConcreteBehavior));

                cfg.For<IMediator>().Use<Mediator>();
            });

            container.GetAllInstances<IPipelineBehavior<IPing, IPong>>();

            var mediator = container.GetInstance<IMediator>();

            var response = await mediator.Send(new Ping { Message = "Ping" });

            response.Message.ShouldBe("Ping Pong");

            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Concrete before",
                "Handler",
                "Concrete after",
                "Inner generic after",
                "Outer generic after",
            });

            output.Messages.Clear();

            var zingResponse = await mediator.Send(new Zing { Message = "Zing" });

            zingResponse.Message.ShouldBe("Zing Zong");

            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Handler",
                "Inner generic after",
                "Outer generic after",
            });
        }
    }
}