> [!WARNING]
> **NArchitecture.Core 2.0 is a breaking upgrade** — .NET 10, `NArchitecture.Core.Mediator` instead of MediatR, [Mapperly](https://mapperly.riok.app/) instead of AutoMapper. Before you update past a 1.x version, read the [migration guide](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.md) ([Türkçe](https://github.com/kodlamaio-projects/nArchitecture.Core/blob/main/docs/migration-guide-2.0.tr.md)).

# NArchitecture.Core.Test

This library provides utilities for testing in kodlama.io projects, including tools for generating fake data and working with fake repositories. The `NArchitecture.Core.Test` package offers support for writing unit tests and integration tests more efficiently. It includes core classes and extensions for implementing unit tests and integration tests for the application layer in both clean and union architectures, supporting the writing of test cases, mock objects, and other essential utilities for testing components and functionalities in applications.

## Installation

You can add the package to your test project using NuGet package manager or .NET CLI:

```bash
dotnet add package NArchitecture.Core.Test
```

## Usage

```csharp
public class BrandMockRepository : BaseMockRepository<IBrandRepository, Brand, Guid, BrandBusinessRules, BrandFakeData>
{
    public BrandMockRepository() : base(new BrandFakeData()) { }
}

// in a test
BrandMockRepository mock = new();
CreateBrandCommandHandler handler = new(mock.MockRepository.Object, mock.BusinessRules);
```

## Migrating to 2.0

- AutoMapper was removed. `BaseMockRepository` lost its `TMappingProfile` type parameter and the `Mapper` field. Handlers that map with [Mapperly](https://mapperly.riok.app/) static mappers need nothing from the test base; pass any other mapper to the handler yourself.
- `MockRepositoryHelper` now honors `withDeleted`, applies `orderBy`, pages with `index`/`size`, and fills `Count`/`Pages` on the returned `Paginate<T>`; `AnyAsync` and `UpdateAsync` setups were fixed.

## Contribution

If you would like to contribute, please visit the GitHub repository and submit a pull request: [NArchitecture.Core.Test GitHub Repository](https://github.com/kodlamaio-projects/nArchitecture.Core)
