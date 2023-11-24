using Lamar;
using Shouldly;
using System.Runtime.CompilerServices;

namespace Colosoft.Mediator.Test
{
    public class StreamPipelineTests
    {
        public interface IPong
        {
            string? Message { get; }
        }

        public interface IPing : IStreamRequest<IPong>
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

        public interface IZong
        {
            string? Message { get; }
        }

        public interface IZing : IStreamRequest<IZong>
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

        public class PingHandler : IStreamRequestHandler<IPing, IPong>
        {
            private readonly Logger output;

            public PingHandler(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<IPong> Handle(IPing request, [EnumeratorCancellation]CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Handler");
                yield return await Task.FromResult(new Pong { Message = request.Message + " Pong" });
            }
        }

        public class ZingHandler : IStreamRequestHandler<IZing, IZong>
        {
            private readonly Logger output;

            public ZingHandler(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<IZong> Handle(IZing request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Handler");
                yield return await Task.FromResult(new Zong { Message = request.Message + " Zong" });
            }
        }

        public class OuterBehavior : IStreamPipelineBehavior<IPing, IPong>
        {
            private readonly Logger output;

            public OuterBehavior(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<IPong> Handle(IPing request, StreamHandlerDelegate<IPong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Outer before");
                await foreach (var result in next())
                {
                    yield return result;
                }

                this.output.Messages.Add("Outer after");
            }
        }

        public class InnerBehavior : IStreamPipelineBehavior<IPing, IPong>
        {
            private readonly Logger output;

            public InnerBehavior(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<IPong> Handle(IPing request, StreamHandlerDelegate<IPong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Inner before");
                await foreach (var result in next())
                {
                    yield return result;
                }

                this.output.Messages.Add("Inner after");
            }
        }

        public class InnerBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
            where TRequest : IStreamRequest<TResponse>
        {
            private readonly Logger output;

            public InnerBehavior(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Inner generic before");
                await foreach (var result in next())
                {
                    yield return result;
                }

                this.output.Messages.Add("Inner generic after");
            }
        }

        public class OuterBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
            where TRequest : IStreamRequest<TResponse>
        {
            private readonly Logger output;

            public OuterBehavior(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Outer generic before");
                await foreach (var result in next())
                {
                    yield return result;
                }

                this.output.Messages.Add("Outer generic after");
            }
        }

        public class ConstrainedBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
            where TRequest : IPing, IStreamRequest<TResponse>
            where TResponse : IPong
        {
            private readonly Logger output;

            public ConstrainedBehavior(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Constrained before");
                await foreach (var result in next())
                {
                    yield return result;
                }

                this.output.Messages.Add("Constrained after");
            }
        }

        public class ConcreteBehavior : IStreamPipelineBehavior<Ping, Pong>
        {
            private readonly Logger output;

            public ConcreteBehavior(Logger output)
            {
                this.output = output;
            }

            public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                this.output.Messages.Add("Concrete before");
                await foreach (var result in next())
                {
                    yield return result;
                }

                this.output.Messages.Add("Concrete after");
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
                    scanner.AddAllTypesOf(typeof(IStreamRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);
                cfg.For<IStreamPipelineBehavior<IPing, IPong>>().Add<OuterBehavior>();
                cfg.For<IStreamPipelineBehavior<IPing, IPong>>().Add<InnerBehavior>();
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await foreach (var response in mediator.CreateStream(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

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
                    scanner.AddAllTypesOf(typeof(IStreamRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);

                cfg.For(typeof(IStreamPipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IStreamPipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));

                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await foreach (var response in mediator.CreateStream(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

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
                    scanner.AddAllTypesOf(typeof(IStreamRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);

                cfg.For(typeof(IStreamPipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IStreamPipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));
                cfg.For(typeof(IStreamPipelineBehavior<,>)).Add(typeof(ConstrainedBehavior<,>));

                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await foreach (var response in mediator.CreateStream(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

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

            await foreach (var response in mediator.CreateStream(new Zing { Message = "Zing" }))
            {
                response.Message.ShouldBe("Zing Zong");
            }

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
                    scanner.AddAllTypesOf(typeof(IStreamRequestHandler<,>));
                });
                cfg.For<Logger>().Use(output);

                cfg.For(typeof(IStreamPipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IStreamPipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));
                cfg.For(typeof(IStreamPipelineBehavior<IPing, IPong>)).Add(typeof(ConcreteBehavior));

                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            await foreach (var response in mediator.CreateStream(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

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

            await foreach (var response in mediator.CreateStream(new Zing { Message = "Zing" }))
            {
                response.Message.ShouldBe("Zing Zong");
            }

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