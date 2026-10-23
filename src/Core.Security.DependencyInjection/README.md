> [!WARNING]
> **NArchitecture.Core 2.0 is a breaking upgrade** — .NET 10, `NArchitecture.Core.Mediator` instead of MediatR, [Mapperly](https://mapperly.riok.app/) instead of AutoMapper. Before you update past a 1.x version, read the [migration guide](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.md) ([Türkçe](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.tr.md)).

# NArchitecture.Core.Security.DependencyInjection

This library provides dependency injection extensions specifically tailored for the security utilities in kodlama.io projects. The `NArchitecture.Core.Security.DependencyInjection` package offers utilities to configure and use security services within the dependency injection framework effectively. It includes core classes and extensions for integrating security services into the dependency injection container in both clean and union architectures, supporting the registration of JWT authentication services, 2FA services, OTP services, email handling services, hashing services, encryption services, and other essential utilities for security dependency injection.

## Installation

You can add the package to your project using NuGet package manager or .NET CLI:

```bash
dotnet add package NArchitecture.Core.Security.DependencyInjection
```

## Contribution

If you would like to contribute, please visit the GitHub repository and submit a pull request: [NArchitecture.Core.Security.DependencyInjection GitHub Repository](https://github.com/kodlamaio-projects/nArchitecture.Core)
