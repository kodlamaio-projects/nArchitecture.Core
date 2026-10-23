> [!WARNING]
> **NArchitecture.Core 2.0 is a breaking upgrade** — .NET 10, `NArchitecture.Core.Mediator` instead of MediatR, [Mapperly](https://mapperly.riok.app/) instead of AutoMapper. Before you update past a 1.x version, read the [migration guide](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.md) ([Türkçe](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.tr.md)).

# NArchitecture.Core.Mediator

A small, dependency-free mediator for .NET 10. It provides the request/response, streaming and pipeline-behavior surface that NArchitecture previously consumed from MediatR, with the same shape so existing handlers and behaviors migrate with a `using` swap.

## Install

```shell
dotnet add package NArchitecture.Core.Mediator
```

## Register

```csharp
using NArchitecture.Core.Mediator.DependencyInjection;

services.AddMediator(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    cfg.AddOpenBehavior(typeof(RequestValidationBehavior<,>));
});
```

Behaviors run in registration order. Open generic behaviors may constrain `TRequest` with marker interfaces; the container only applies them to requests that satisfy the constraints.

## Use

```csharp
public record GetUserQuery(Guid Id) : IRequest<UserDto>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken) { ... }
}

UserDto dto = await mediator.Send(new GetUserQuery(id), cancellationToken);
```

Streaming:

```csharp
public record ExportRows : IStreamRequest<Row>;

public class ExportRowsHandler : IStreamRequestHandler<ExportRows, Row>
{
    public async IAsyncEnumerable<Row> Handle(ExportRows request, [EnumeratorCancellation] CancellationToken ct) { ... }
}

await foreach (Row row in mediator.CreateStream(new ExportRows(), ct)) { ... }
```

## Migrating from MediatR

| MediatR | NArchitecture.Core.Mediator |
|---|---|
| `using MediatR;` | `using NArchitecture.Core.Mediator;` |
| `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` | `services.AddMediator(cfg => cfg.RegisterServicesFromAssembly(...))` |
| `cfg.AddOpenBehavior(typeof(X<,>))` | same |
| `IRequest<T>`, `IRequest`, `IRequestHandler<,>`, `IPipelineBehavior<,>`, `RequestHandlerDelegate<T>`, `Unit` | same names |
| `IStreamRequest<T>`, `IStreamRequestHandler<,>`, `IMediator.CreateStream` | same names |
| `INotification` / `Publish` | not provided |

`RequestHandlerDelegate<TResponse>` accepts an optional `CancellationToken`; `await next()` reuses the token the request was sent with.
