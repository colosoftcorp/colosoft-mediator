# Colosoft Mediator

Simple mediator implementation in .NET

In-process messaging with no dependencies.

Supports request/response, commands, queries, notifications and events, synchronous and async with intelligent dispatching via C# generic variance.

### Registering with `IServiceCollection`

Supports `Microsoft.Extensions.DependencyInjection.Abstractions` directly. To register various services and handlers:

```
services.AddMediator(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
```

or with an assembly:

```
services.AddMediator(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));
```

This registers:

- `IMediator` as transient
- `ISender` as transient
- `IPublisher` as transient
- `IRequestHandler<,>` concrete implementations as transient
- `IRequestHandler<>` concrete implementations as transient
- `INotificationHandler<>` concrete implementations as transient
- `IStreamRequestHandler<>` concrete implementations as transient
- `IRequestExceptionHandler<,,>` concrete implementations as transient
- `IRequestExceptionAction<,>)` concrete implementations as transient

This also registers open generic implementations for:

- `INotificationHandler<>`
- `IRequestExceptionHandler<,,>`
- `IRequestExceptionAction<,>`

To register behaviors, stream behaviors, pre/post processors:

```csharp
services.AddMediator(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly);
    cfg.AddBehavior<PingPongBehavior>();
    cfg.AddStreamBehavior<PingPongStreamBehavior>();
    cfg.AddRequestPreProcessor<PingPreProcessor>();
    cfg.AddRequestPostProcessor<PingPongPostProcessor>();
    cfg.AddOpenBehavior(typeof(GenericBehavior<,>));
    });
```

With additional methods for open generics and overloads for explicit service types.