> [!WARNING]
> **NArchitecture.Core 2.0 is a breaking upgrade** — .NET 10, `NArchitecture.Core.Mediator` instead of MediatR, [Mapperly](https://mapperly.riok.app/) instead of AutoMapper. Before you update past a 1.x version, read the [migration guide](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.md) ([Türkçe](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.tr.md)).

# NArchitecture.Core.Application

This library contains application layer components utilized in kodlama.io projects. The `NArchitecture.Core.Application` package offers commonly used features and structures in the application layer, aimed at streamlining the software development process. It includes core classes for both clean and union architectures, providing support for CQRS, mediator pipelines (authorization, caching, logging, validation, transactions, performance), request models, response models, and essential security features.

## Installation

You can add the package to your project using NuGet package manager or .NET CLI:

```bash
dotnet add package NArchitecture.Core.Application
```

## Registration

```csharp
services.AddMediator(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddNArchitecturePipelines(); // Authorization, Caching, CacheRemoving, Logging, RequestValidation, TransactionScope
});
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

Pipelines are opt-in per request through marker interfaces: `ISecuredRequest`, `ICachableRequest`, `ICacheRemoverRequest`, `ILoggableRequest`, `ITransactionalRequest`, `IIntervalRequest`.

## Migrating to 2.0

- MediatR was removed. `NArchitecture.Core.Application` now depends on `NArchitecture.Core.Mediator`, which exposes the same `IRequest<T>`, `IRequestHandler<,>`, `IPipelineBehavior<,>` and `RequestHandlerDelegate<T>` names. Replace `using MediatR;` with `using NArchitecture.Core.Mediator;`.
- Replace `services.AddMediatR(cfg => ...)` with `services.AddMediator(cfg => ...)` (`using NArchitecture.Core.Mediator.DependencyInjection;`). `AddOpenBehavior` keeps the same name; `AddNArchitecturePipelines()` registers the standard set.
- The package targets .NET 10 and references the `Microsoft.AspNetCore.App` shared framework instead of the legacy `Microsoft.AspNetCore.Http.Abstractions` 2.2 package.
- Caching behaviors read and write UTF-8 consistently; cache group removal now removes the correct `{group}SlidingExpiration` key.

## Contribution

If you would like to contribute, please visit the GitHub repository and submit a pull request: [NArchitecture.Core.Application GitHub Repository](https://github.com/kodlamaio-projects/nArchitecture.Core)
