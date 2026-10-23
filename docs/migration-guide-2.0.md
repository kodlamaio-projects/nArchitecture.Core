# Migrating a nArchitecture 1.x project to 2.0

> Türkçe sürüm: [migration-guide-2.0.tr.md](./migration-guide-2.0.tr.md)

This guide is for projects that were created with `nArchGen new` 1.x (starter `v1.x`) and extended with
`nArchGen generate crud|command|query`. Hand-written code that follows the same conventions migrates the same way.

Most of the work is mechanical and `nArchGen upgrade` (Gen 2.0.0) does it for you; see [Automated upgrade](#automated-upgrade).
Read the manual steps anyway: they explain what the tool changes and what it leaves to you.

## What changed in 2.0

| Area | 1.x | 2.0 |
|---|---|---|
| Target framework | `net8.0` | `net10.0` (`global.json` pins SDK `10.0.100`, `rollForward: latestFeature`) |
| Mediator | `MediatR` 12 | `NArchitecture.Core.Mediator` (same request/handler/behavior shapes, no `INotification`) |
| Mapping | `AutoMapper` 13 (`Profile`, `IMapper`) | [Mapperly](https://mapperly.riok.app/) source generator (`static partial class XMapper`) |
| `NArchitecture.Core.*` | 1.0.x – 1.3.x | all `2.0.0` |
| `NArchitecture.Core.Test` | `BaseMockRepository<TRepo, TEntity, TId, TMappingProfile, TRules, TFakeData>` with a `Mapper` field | `BaseMockRepository<TRepo, TEntity, TId, TRules, TFakeData>`, no mapper |
| Swagger | Swashbuckle 6.5, `Microsoft.OpenApi.Models` | Swashbuckle 10.3, `Microsoft.OpenApi` (OpenAPI 2.x object model) |
| EF Core / ASP.NET packages | 8.x | 10.x (`Microsoft.AspNetCore.*` packages replaced by the framework reference inside Core) |
| Elasticsearch | NEST 7 | `Elastic.Clients.Elasticsearch` 9 (`GetIndexList()` → `GetIndexListAsync()`, ids are `string`) |
| Tests | xunit 2 + VSTest | xunit.v3 + Microsoft Testing Platform (optional but default for `nArchGen upgrade`) |
| Tooling | csharpier 0.x (`dotnet csharpier .`), roslynator 0.5, dotnet-ef 8 | csharpier 1.3 (`dotnet csharpier format .`), roslynator 1.0, dotnet-ef 10, `narchitecture.gen` 2.0 |

Behavior fixes you get for free: cache group invalidation removes the right `{group}SlidingExpiration` key, caches are UTF-8 end to end,
`Paginate.Pages` is correct, pipeline behaviors tolerate a missing `HttpContext`, `UseDbMigrationApplier` runs inside a service scope,
and `UserForLoginDto` / `UserForRegisterDto` accept the password from a JSON body again.

## Prerequisites

- .NET 10 SDK.
- `NArchitecture.Core.*` 2.0.0 and `NArchitecture.Gen` 2.0.0 on nuget.org (or a local feed while you test pre-release packages).
- A clean git working tree on a new branch. The upgrade touches most files; you want a readable diff.

## Step 1 – Project files

1. Add a `global.json` next to the solution:
   ```json
   { "sdk": { "version": "10.0.100", "rollForward": "latestFeature" }, "test": { "runner": "Microsoft.Testing.Platform" } }
   ```
   Drop the `test` section if you keep xunit 2.
2. In every `.csproj`: `<TargetFramework>net8.0</TargetFramework>` → `net10.0`.
3. Package references:

   | Action | Package | Note |
   |---|---|---|
   | Remove | `MediatR`, `AutoMapper`, `AutoMapper.Extensions.Microsoft.DependencyInjection` | |
   | Add | `NArchitecture.Core.Mediator` 2.0.0 | Application project |
   | Add | `Riok.Mapperly` 4.3.1 with `PrivateAssets="all" ExcludeAssets="runtime"` | Application project (source generator) |
   | Update | every `NArchitecture.Core.*` | `2.0.0` |
   | Update | `Microsoft.EntityFrameworkCore.*`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `Microsoft.Extensions.Configuration.Abstractions` | `10.0.13` |
   | Update | `Swashbuckle.AspNetCore` | `10.3.1` |
   | Update | `System.IdentityModel.Tokens.Jwt` | `8.23.0` |
   | Remove | `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging` from the WebAPI project | already part of the web SDK (NU1510) |

   Optional: move versions to `Directory.Packages.props` (central package management) as the 2.0 starter does.
4. Tool manifest `.config/dotnet-tools.json`: `dotnet-ef` 10.0.13, `csharpier` 1.3.0 (command is now `csharpier`, not `dotnet-csharpier`), `roslynator.dotnet.cli` 1.0.1, `narchitecture.gen` 2.0.0.
5. CI: `actions/setup-dotnet@v4` with `global-json-file: global.json`; `dotnet csharpier --check .` → `dotnet csharpier check .`.

## Step 2 – Mediator

The API surface is the same; only the namespace and the registration method change.

```csharp
// before
using MediatR;
services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    // ...
});

// after
using NArchitecture.Core.Mediator;                      // in every handler / request / controller base
using NArchitecture.Core.Mediator.DependencyInjection;  // where AddMediator is called
services.AddMediator(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    configuration.AddNArchitecturePipelines(); // Authorization, Caching, CacheRemoving, Logging, RequestValidation, TransactionScope
});
```

`IRequest<T>`, `IRequest`, `IRequestHandler<,>`, `IPipelineBehavior<,>`, `RequestHandlerDelegate<T>`, `IStreamRequest<T>`, `IMediator`,
`ISender` keep their names. Behaviors run in registration order, exactly as before.

Not provided: `INotification`, `INotificationHandler<>`, `IPublisher`, `IMediator.Publish`. If you use them, keep a small in-house
publisher (an `IEnumerable<INotificationHandler<T>>` loop) or keep MediatR side by side for notifications only; `nArchGen upgrade`
lists every `Publish` call site for you.

## Step 3 – AutoMapper → Mapperly

Mapperly generates the mapping code at compile time from `static partial` methods. There is no runtime configuration, no DI
registration and no `IMapper`.

### Profiles become mappers

```csharp
// before: Features/Brands/Profiles/MappingProfiles.cs
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Brand, CreateBrandCommand>().ReverseMap();
        CreateMap<Brand, CreatedBrandResponse>().ReverseMap();
        CreateMap<Brand, UpdateBrandCommand>().ReverseMap();
        CreateMap<Brand, UpdatedBrandResponse>().ReverseMap();
        CreateMap<Brand, DeletedBrandResponse>().ReverseMap();
        CreateMap<Brand, GetByIdBrandResponse>().ReverseMap();
        CreateMap<Brand, GetListBrandListItemDto>().ReverseMap();
        CreateMap<IPaginate<Brand>, GetListResponse<GetListBrandListItemDto>>().ReverseMap();
    }
}

// after: Features/Brands/Mappers/BrandMapper.cs
using Riok.Mapperly.Abstractions;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class BrandMapper
{
    [MapperIgnoreTarget(nameof(Brand.Id))]
    [MapperIgnoreTarget(nameof(Brand.CreatedDate))]
    [MapperIgnoreTarget(nameof(Brand.UpdatedDate))]
    [MapperIgnoreTarget(nameof(Brand.DeletedDate))]
    public static partial Brand ToEntity(CreateBrandCommand command);

    public static partial CreatedBrandResponse ToCreatedResponse(Brand entity);

    [MapperIgnoreTarget(nameof(Brand.Id))]
    [MapperIgnoreTarget(nameof(Brand.CreatedDate))]
    [MapperIgnoreTarget(nameof(Brand.UpdatedDate))]
    [MapperIgnoreTarget(nameof(Brand.DeletedDate))]
    public static partial void UpdateEntity(UpdateBrandCommand command, Brand entity);

    public static partial UpdatedBrandResponse ToUpdatedResponse(Brand entity);
    public static partial DeletedBrandResponse ToDeletedResponse(Brand entity);
    public static partial GetByIdBrandResponse ToGetByIdResponse(Brand entity);
    public static partial GetListBrandListItemDto ToGetListItemDto(Brand entity);
    public static partial GetListResponse<GetListBrandListItemDto> ToGetListResponse(IPaginate<Brand> paginate);
}
```

Rules of thumb:

- Only the directions you actually call need a method. `ReverseMap()` produced both directions; most reverse maps were never used.
- `RequiredMappingStrategy.Target` makes Mapperly warn (`RMG020`) when a *target* member is not mapped, which is what you want for
  responses. Ignore audit fields and navigation properties on entity targets with `[MapperIgnoreTarget]`.
- `IPaginate<T>` → `GetListResponse<TDto>` works out of the box: the `Items` collection is mapped element by element with the
  `To…ItemDto` method in the same class.

### Member configuration

| AutoMapper | Mapperly |
|---|---|
| `.ForMember(d => d.FullName, o => o.MapFrom(s => s.Name))` | `[MapProperty(nameof(Source.Name), nameof(Target.FullName))]` |
| `.ForMember(d => d.City, o => o.MapFrom(s => s.Address.City))` | `[MapProperty("Address.City", nameof(Target.City))]` |
| `.ForMember(d => d.Secret, o => o.Ignore())` | `[MapperIgnoreTarget(nameof(Target.Secret))]` |
| `.ForSourceMember(s => s.Internal, o => o.DoNotValidate())` | `[MapperIgnoreSource(nameof(Source.Internal))]` |
| `.ForMember(d => d.X, o => o.MapFrom(s => s.A + s.B))` (expression) | write a normal method in the mapper and call it: `[MapProperty(nameof(Source.A), nameof(Target.X), Use = nameof(Combine))]` is only for 1:1 conversions, so usually: `public static Target ToTarget(Source s) { var t = ToTargetCore(s); t.X = s.A + s.B; return t; }` |
| `.ConstructUsing(...)`, `.ConvertUsing(...)` | hand-written method (Mapperly can map to constructors with matching parameter names automatically) |
| `.BeforeMap` / `.AfterMap` | hand-written wrapper method |
| `.IncludeBase<,>()` | `[MapDerivedType<TBase, TDerived>]` or explicit methods |
| `.ForAllMembers(o => o.Condition((s, d, v) => v != null))` | `[MapperIgnoreObsoleteMembers]`/`AllowNullPropertyAssignment = false` on `[Mapper]` |
| `queryable.ProjectTo<Dto>(_mapper.ConfigurationProvider)` | `public static partial IQueryable<Dto> ProjectToDto(this IQueryable<Entity> query);` |

Mapperly diagnostics worth knowing: `RMG020` unmapped target member, `RMG012` unmapped source member (only with the `Source`
strategy), `RMG037` required member not mapped. They are warnings; treat them as a checklist.

### Handlers and services

```csharp
// before
private readonly IMapper _mapper;
public CreateBrandCommandHandler(IMapper mapper, IBrandRepository brandRepository, BrandBusinessRules rules) { _mapper = mapper; ... }
Brand brand = _mapper.Map<Brand>(request);
CreatedBrandResponse response = _mapper.Map<CreatedBrandResponse>(brand);

// after
public CreateBrandCommandHandler(IBrandRepository brandRepository, BrandBusinessRules rules) { ... }
Brand brand = BrandMapper.ToEntity(request);
CreatedBrandResponse response = BrandMapper.ToCreatedResponse(brand);

// update: before
Brand mappedBrand = _mapper.Map(request, destination: brand!);
await _brandRepository.UpdateAsync(mappedBrand);
// update: after
BrandMapper.UpdateEntity(request, brand!);
await _brandRepository.UpdateAsync(brand!);
```

Then delete `services.AddAutoMapper(...)`, the `Profiles` folders and every `using AutoMapper;`.

## Step 4 – Core breaking changes

- **Swagger**: `using Microsoft.OpenApi.Models;` → `using Microsoft.OpenApi;`. `BearerSecurityRequirementOperationFilter` now only
  adds the requirement; the scheme must be declared with `AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "Bearer", ... })`
  (the starter already does this).
- **`AddDbMigrationApplier`** takes `Func<IServiceProvider, TDbContext>`. The usual lambda
  `sp => sp.GetRequiredService<BaseDbContext>()` compiles unchanged.
- **Elasticsearch**: `IElasticSearch.GetIndexList()` → `await GetIndexListAsync()`; `ElasticSearchGetModel<T>.ElasticId` is a `string`.
- **`BaseMockRepository`**: see step 5.
- **Pipelines**: `MethodName` in the log detail is now the request type name instead of the compiler-generated lambda name.

## Step 5 – Test project

```csharp
// before
public class BrandMockRepository : BaseMockRepository<IBrandRepository, Brand, Guid, MappingProfiles, BrandBusinessRules, BrandFakeData>
_handler = new CreateBrandCommandHandler(Mapper, MockRepository.Object, BusinessRules);

// after
public class BrandMockRepository : BaseMockRepository<IBrandRepository, Brand, Guid, BrandBusinessRules, BrandFakeData>
_handler = new CreateBrandCommandHandler(MockRepository.Object, BusinessRules);
```

`MockRepositoryHelper` in Core.Test 2.0 honors `withDeleted`, applies `orderBy`, pages with `index`/`size` and fills `Count`/`Pages`,
so list assertions that relied on the old "return everything" behavior may need a `PageSize` large enough.

Optional (default in `nArchGen upgrade`): move to xunit v3 + Microsoft Testing Platform.

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <IsTestProject>true</IsTestProject>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="xunit.v3" Version="4.0.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="4.0.1" />
  <PackageReference Include="Xunit.DependencyInjection" Version="12.0.1" />
  <!-- remove xunit, Microsoft.NET.Test.Sdk, coverlet.collector -->
</ItemGroup>
```

`Startup`-based constructor injection keeps working with `Xunit.DependencyInjection` 12.

## Step 6 – Verify

```bash
dotnet restore
dotnet build -c Release          # 0 errors; review RMG warnings
dotnet test -c Release
grep -rn "MediatR\|AutoMapper" src tests   # must be empty
dotnet run --project src/<project>/WebAPI  # Swagger shows the Bearer lock, login/register work with a JSON body
```

Behavioral spot checks: `PUT` on an entity evicts the `Get<Plural>` cache group (the next `GET` list reflects the change) and paged
responses report the right `pages`.

## Troubleshooting

- **`Cannot resolve scoped service 'BaseDbContext' from root provider`** at startup: you are on `NArchitecture.Core.Persistence.WebApi` < 2.0.0; `UseDbMigrationApplier` 2.0 creates a scope.
- **`RMG020` warnings in generated mappers**: the target has members the source does not have. Either add the data to the source or ignore the member explicitly.
- **`CS8618` in response DTOs**: pre-existing nullable warnings in Gen 1.x output; give string properties a default or mark them `required`.
- **`dotnet tool restore` fails on `narchitecture.gen`**: the manifest pins 2.0.0; make sure it is published (or add a local feed).
- **Login returns a validation error for the password**: you still run Core.Application < 2.0.0 where the DTO ignored the password on input.

## Automated upgrade

```bash
cd <solution root>
# Most 1.x projects have a local tool manifest (.config/dotnet-tools.json) pinning the old Gen version.
# `dotnet nArchGen ...` resolves that manifest first, so updating only the global tool will not expose `upgrade`.
if [ -f .config/dotnet-tools.json ]; then
  dotnet tool update NArchitecture.Gen --version 2.0.0     # updates the local manifest
else
  dotnet tool update -g NArchitecture.Gen --version 2.0.0  # updates/installs the global tool
fi

nArchGen upgrade --dry-run        # analysis only, prints what would change and what needs manual work
nArchGen upgrade                  # applies steps 1–5, then builds and writes MIGRATION-REPORT.md
```

Options: `--project <name>` when `src/` holds several projects, `--keep-test-framework` to stay on xunit 2, `--skip <step,...>`
(`project-files`, `mediator`, `automapper`, `tests`, `web-api`, `format`, `verify`), `--no-build`, `--force` to run on a dirty working tree.
After writing the files the tool runs the solution's own `csharpier` (from `.config/dotnet-tools.json`) when it can, then
`dotnet restore` + `dotnet build`, and ends with `MIGRATION-REPORT.md` next to the solution.

What the tool does not do, and reports instead: `INotification`/`Publish`, AutoMapper `ConstructUsing`/`ConvertUsing`/`BeforeMap`/`AfterMap`/`Condition`/`IncludeBase`/`ForAllMembers`, `MapFrom` with an expression that is not a member path, `IMapper` usages whose source type it cannot infer, and `MapperConfiguration` instances in tests. Each one is listed as `file:line – reason` in `MIGRATION-REPORT.md` together with the matching section of this guide.
