using Lamar;
using Lamar.IoC;
using Shouldly;

namespace Colosoft.Mediator.Test
{
    public class ExceptionTests
    {
        private readonly IMediator mediator;

        public interface IPong
        {
        }

        public interface IPing : IRequest<IPong>
        {
        }

        public class Ping : IPing
        {
        }

        public class Pong : IPong
        {
        }

        public interface IVoidPing : IRequest
        {
        }

        public class VoidPing : IVoidPing
        {
        }

        public interface IPinged : INotification
        {
        }

        public class Pinged : IPinged
        {
        }

        public interface IAsyncPing : IRequest<IPong>
        {
        }

        public class AsyncPing : IAsyncPing
        {
        }

        public interface IAsyncVoidPing : IRequest
        {
        }

        public class AsyncVoidPing : IAsyncVoidPing
        {
        }

        public interface IAsyncPinged : INotification
        {
        }

        public class AsyncPinged : IAsyncPinged
        {
        }

        public interface INullPing : IRequest<IPong>
        {
        }

        public class NullPing : INullPing
        {
        }

        public interface IVoidNullPing : IRequest
        {
        }

        public class VoidNullPing : IVoidNullPing
        {
        }

        public interface INullPinged : INotification
        {
        }

        public class NullPinged : INullPinged
        {
        }

        public class NullPingHandler : IRequestHandler<INullPing, IPong>
        {
            public Task<IPong> Handle(INullPing request, CancellationToken cancellationToken)
            {
                return Task.FromResult<IPong>(new Pong());
            }
        }

        public class VoidNullPingHandler : IRequestHandler<IVoidNullPing>
        {
            public Task Handle(IVoidNullPing request, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        public ExceptionTests()
        {
            var container = new Container(cfg =>
            {
                cfg.For<IMediator>().Use<Mediator>();
            });

            this.mediator = container.GetInstance<IMediator>();
        }

        [Fact]
        public async Task Should_throw_for_send()
        {
            await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await this.mediator.Send(new Ping()));
        }

        [Fact]
        public async Task Should_throw_for_void_send()
        {
            await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await this.mediator.Send(new VoidPing()));
        }

        [Fact]
        public async Task Should_not_throw_for_publish()
        {
            Exception ex = null!;
            try
            {
                await this.mediator.Publish<IPinged>(new Pinged());
            }
            catch (Exception e)
            {
                ex = e;
            }

            ex.ShouldBeNull();
        }

        [Fact]
        public async Task Should_throw_for_async_send()
        {
            await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await this.mediator.Send(new AsyncPing()));
        }

        [Fact]
        public async Task Should_throw_for_async_void_send()
        {
            await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await this.mediator.Send(new AsyncVoidPing()));
        }

        [Fact]
        public async Task Should_not_throw_for_async_publish()
        {
            Exception ex = null!;
            try
            {
                await this.mediator.Publish<IAsyncPinged>(new AsyncPinged());
            }
            catch (Exception e)
            {
                ex = e;
            }

            ex.ShouldBeNull();
        }

        [Fact]
        public async Task Should_throw_argument_exception_for_send_when_request_is_null()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(NullPing));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator1 = container.GetInstance<IMediator>();

            NullPing request = null!;

            await Should.ThrowAsync<ArgumentNullException>(async () => await mediator1.Send(request));
        }

        [Fact]
        public async Task Should_throw_argument_exception_for_void_send_when_request_is_null()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(VoidNullPing));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator1 = container.GetInstance<IMediator>();

            VoidNullPing request = null!;

            await Should.ThrowAsync<ArgumentNullException>(async () => await mediator1.Send(request));
        }

        [Fact]
        public async Task Should_throw_argument_exception_for_publish_when_request_is_null()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(NullPinged));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator1 = container.GetInstance<IMediator>();

            NullPinged notification = null!;

            await Should.ThrowAsync<ArgumentNullException>(async () => await mediator1.Publish<INullPinged>(notification));
        }

        [Fact]
        public async Task Should_throw_argument_exception_for_publish_when_request_is_null_object()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(NullPinged));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator1 = container.GetInstance<IMediator>();

            object notification = null!;

            await Should.ThrowAsync<ArgumentNullException>(async () => await mediator1.Publish(notification));
        }

        [Fact]
        public async Task Should_throw_argument_exception_for_publish_when_request_is_not_notification()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(NullPinged));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator1 = container.GetInstance<IMediator>();

            object notification = "totally not notification";

            await Should.ThrowAsync<ArgumentException>(async () => await mediator1.Publish(notification));
        }

        public interface IPingException : IRequest
        {
        }

        public class PingException : IPingException
        {
        }

        public class PingExceptionHandler : IRequestHandler<IPingException>
        {
            public Task Handle(IPingException request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        public class PingException2Handler : IRequestHandler<PingException>
        {
            public Task Handle(PingException request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task Should_throw_exception_for_non_request_send()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(NullPinged));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator1 = container.GetInstance<IMediator>();

            object nonRequest = new NonRequest();

            var argumentException = await Should.ThrowAsync<ArgumentException>(async () => await mediator1.Send(nonRequest));
            Assert.StartsWith("NonRequest does not implement IRequest", argumentException.Message);
        }

#pragma warning disable S2094 // Classes should not be empty
        public class NonRequest
#pragma warning restore S2094 // Classes should not be empty
        {
        }

        [Fact]
        public async Task Should_throw_exception_for_generic_send_when_exception_occurs()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(NullPinged));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                });
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator1 = container.GetInstance<IMediator>();

            IPingException pingException = new PingException();

            await Should.ThrowAsync<NotImplementedException>(async () => await mediator1.Send(pingException));
        }
    }
}