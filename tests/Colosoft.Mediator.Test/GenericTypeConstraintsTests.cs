using Lamar;
using Shouldly;
using System.Reflection;

namespace Colosoft.Mediator.Test
{
    public class GenericTypeConstraintsTests
    {
        public interface IGenericTypeRequestHandlerTestClass<TRequest>
            where TRequest : IBaseRequest
        {
            Type[] Handle(TRequest request);
        }

        public abstract class GenericTypeRequestHandlerTestClass<TRequest> : IGenericTypeRequestHandlerTestClass<TRequest>
            where TRequest : IBaseRequest
        {
            public bool IsIRequest { get; }

            public bool IsIRequestT { get; }

            public bool IsIBaseRequest { get; }

            protected GenericTypeRequestHandlerTestClass()
            {
                this.IsIRequest = typeof(IRequest).IsAssignableFrom(typeof(TRequest));
                this.IsIRequestT = typeof(TRequest).GetInterfaces()
                    .Any(x => x.GetTypeInfo().IsGenericType &&
                              x.GetGenericTypeDefinition() == typeof(IRequest<>));

                this.IsIBaseRequest = typeof(IBaseRequest).IsAssignableFrom(typeof(TRequest));
            }

            public Type[] Handle(TRequest request)
            {
                return typeof(TRequest).GetInterfaces();
            }
        }

        public class GenericTypeConstraintPing : GenericTypeRequestHandlerTestClass<IPing>
        {
        }

        public class GenericTypeConstraintJing : GenericTypeRequestHandlerTestClass<Jing>
        {
        }

        public class Jing : IRequest
        {
            public string? Message { get; set; }
        }

        public class JingHandler : IRequestHandler<Jing>
        {
            public Task Handle(Jing request, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

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

        public class PingHandler : IRequestHandler<IPing, IPong>
        {
            public Task<IPong> Handle(IPing request, CancellationToken cancellationToken)
            {
                return Task.FromResult<IPong>(new Pong { Message = request.Message + " Pong" });
            }
        }

        private readonly IMediator mediator;

        public GenericTypeConstraintsTests()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(GenericTypeConstraintsTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.IncludeNamespaceContainingType<Jing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            this.mediator = container.GetInstance<IMediator>();
        }

        [Fact]
        public async Task Should_Resolve_Void_Return_Request()
        {
            var jing = new Jing { Message = "Jing" };

            await this.mediator.Send(jing);

            var genericTypeConstraintsVoidReturn = new GenericTypeConstraintJing();

            Assert.True(genericTypeConstraintsVoidReturn.IsIRequest);
            //Assert.False(genericTypeConstraintsVoidReturn.IsIRequestT);
            Assert.True(genericTypeConstraintsVoidReturn.IsIBaseRequest);

            var results = genericTypeConstraintsVoidReturn.Handle(jing);

            Assert.Equal(2, results.Length);

            results.ShouldNotContain(typeof(IRequest<Unit>));
            results.ShouldContain(typeof(IBaseRequest));
            results.ShouldContain(typeof(IRequest));
        }

        [Fact]
        public async Task Should_Resolve_Response_Return_Request()
        {
            var ping = new Ping { Message = "Ping" };

            var pingResponse = await this.mediator.Send(ping);
            pingResponse.Message.ShouldBe("Ping Pong");

            var genericTypeConstraintsResponseReturn = new GenericTypeConstraintPing();

            Assert.False(genericTypeConstraintsResponseReturn.IsIRequest);
            Assert.True(genericTypeConstraintsResponseReturn.IsIRequestT);
            Assert.True(genericTypeConstraintsResponseReturn.IsIBaseRequest);

            var results = genericTypeConstraintsResponseReturn.Handle(ping);

            Assert.Equal(2, results.Length);

            results.ShouldContain(typeof(IRequest<IPong>));
            results.ShouldContain(typeof(IBaseRequest));
            results.ShouldNotContain(typeof(IRequest));
        }
    }
}