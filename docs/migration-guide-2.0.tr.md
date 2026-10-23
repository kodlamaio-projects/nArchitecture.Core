# nArchitecture 1.x projesini 2.0'a taşıma

> English version: [migration-guide-2.0.md](./migration-guide-2.0.md)

Bu rehber `nArchGen new` 1.x (starter `v1.x`) ile oluşturulup `nArchGen generate crud|command|query` ile büyütülmüş projeler içindir.
Aynı kalıpları izleyen el yazımı kod da aynı şekilde taşınır.

İşin büyük kısmı mekaniktir ve `nArchGen upgrade` (Gen 2.0.0) bunu sizin için yapar; bkz. [Otomatik yükseltme](#otomatik-yükseltme).
Manuel adımları yine de okuyun: aracın neyi değiştirdiğini ve neyi size bıraktığını anlatırlar.

## 2.0'da ne değişti

| Alan | 1.x | 2.0 |
|---|---|---|
| Hedef framework | `net8.0` | `net10.0` (`global.json` SDK `10.0.100`, `rollForward: latestFeature`) |
| Mediator | `MediatR` 12 | `NArchitecture.Core.Mediator` (aynı request/handler/behavior şekilleri, `INotification` yok) |
| Mapping | `AutoMapper` 13 (`Profile`, `IMapper`) | [Mapperly](https://mapperly.riok.app/) source generator (`static partial class XMapper`) |
| `NArchitecture.Core.*` | 1.0.x – 1.3.x | hepsi `2.0.0` |
| `NArchitecture.Core.Test` | `BaseMockRepository<TRepo, TEntity, TId, TMappingProfile, TRules, TFakeData>` ve `Mapper` alanı | `BaseMockRepository<TRepo, TEntity, TId, TRules, TFakeData>`, mapper yok |
| Swagger | Swashbuckle 6.5, `Microsoft.OpenApi.Models` | Swashbuckle 10.3, `Microsoft.OpenApi` (OpenAPI 2.x nesne modeli) |
| EF Core / ASP.NET paketleri | 8.x | 10.x (Core içindeki `Microsoft.AspNetCore.*` paketleri framework reference'a döndü) |
| Elasticsearch | NEST 7 | `Elastic.Clients.Elasticsearch` 9 (`GetIndexList()` → `GetIndexListAsync()`, id'ler `string`) |
| Testler | xunit 2 + VSTest | xunit.v3 + Microsoft Testing Platform (opsiyonel, `nArchGen upgrade` için varsayılan) |
| Araçlar | csharpier 0.x (`dotnet csharpier .`), roslynator 0.5, dotnet-ef 8 | csharpier 1.3 (`dotnet csharpier format .`), roslynator 1.0, dotnet-ef 10, `narchitecture.gen` 2.0 |

Kendiliğinden gelen davranış düzeltmeleri: cache grubu temizliği doğru `{group}SlidingExpiration` anahtarını siler, cache uçtan uca UTF-8'dir,
`Paginate.Pages` doğru hesaplanır, pipeline behavior'lar `HttpContext` yokken çökmez, `UseDbMigrationApplier` bir service scope içinde çalışır
ve `UserForLoginDto` / `UserForRegisterDto` şifreyi JSON body'den yeniden okur.

## Önkoşullar

- .NET 10 SDK.
- nuget.org'da `NArchitecture.Core.*` 2.0.0 ve `NArchitecture.Gen` 2.0.0 (ön sürümleri denerken yerel bir feed).
- Yeni bir dalda temiz bir git çalışma ağacı. Yükseltme dosyaların çoğuna dokunur; okunabilir bir diff isteyeceksiniz.

## Adım 1 – Proje dosyaları

1. Solution'ın yanına `global.json` ekleyin:
   ```json
   { "sdk": { "version": "10.0.100", "rollForward": "latestFeature" }, "test": { "runner": "Microsoft.Testing.Platform" } }
   ```
   xunit 2'de kalacaksanız `test` bölümünü çıkarın.
2. Her `.csproj`'da: `<TargetFramework>net8.0</TargetFramework>` → `net10.0`.
3. Paket referansları:

   | İşlem | Paket | Not |
   |---|---|---|
   | Kaldır | `MediatR`, `AutoMapper`, `AutoMapper.Extensions.Microsoft.DependencyInjection` | |
   | Ekle | `NArchitecture.Core.Mediator` 2.0.0 | Application projesi |
   | Ekle | `Riok.Mapperly` 4.3.1, `PrivateAssets="all" ExcludeAssets="runtime"` ile | Application projesi (source generator) |
   | Güncelle | tüm `NArchitecture.Core.*` | `2.0.0` |
   | Güncelle | `Microsoft.EntityFrameworkCore.*`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `Microsoft.Extensions.Configuration.Abstractions` | `10.0.13` |
   | Güncelle | `Swashbuckle.AspNetCore` | `10.3.1` |
   | Güncelle | `System.IdentityModel.Tokens.Jwt` | `8.23.0` |
   | Kaldır | WebAPI projesindeki `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging` | web SDK zaten içeriyor (NU1510) |

   İsteğe bağlı: sürümleri 2.0 starter'ındaki gibi `Directory.Packages.props`'a taşıyın (merkezi paket yönetimi).
4. Araç manifesti `.config/dotnet-tools.json`: `dotnet-ef` 10.0.13, `csharpier` 1.3.0 (komut artık `dotnet-csharpier` değil `csharpier`), `roslynator.dotnet.cli` 1.0.1, `narchitecture.gen` 2.0.0.
5. CI: `actions/setup-dotnet@v4` + `global-json-file: global.json`; `dotnet csharpier --check .` → `dotnet csharpier check .`.

## Adım 2 – Mediator

API yüzeyi aynı; yalnız namespace ve kayıt metodu değişir.

```csharp
// önce
using MediatR;
services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    // ...
});

// sonra
using NArchitecture.Core.Mediator;                      // her handler / request / controller base'de
using NArchitecture.Core.Mediator.DependencyInjection;  // AddMediator'ın çağrıldığı yerde
services.AddMediator(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    configuration.AddNArchitecturePipelines(); // Authorization, Caching, CacheRemoving, Logging, RequestValidation, TransactionScope
});
```

`IRequest<T>`, `IRequest`, `IRequestHandler<,>`, `IPipelineBehavior<,>`, `RequestHandlerDelegate<T>`, `IStreamRequest<T>`, `IMediator`,
`ISender` adlarını korur. Behavior'lar eskisi gibi kayıt sırasıyla çalışır.

Sağlanmayanlar: `INotification`, `INotificationHandler<>`, `IPublisher`, `IMediator.Publish`. Kullanıyorsanız küçük bir ev yapımı
publisher yazın (`IEnumerable<INotificationHandler<T>>` döngüsü) ya da MediatR'ı yalnız notification için yan yana tutun;
`nArchGen upgrade` her `Publish` çağrısını sizin için listeler.

## Adım 3 – AutoMapper → Mapperly

Mapperly mapping kodunu derleme zamanında `static partial` metotlardan üretir. Çalışma zamanı konfigürasyonu, DI kaydı ve `IMapper` yoktur.

### Profiller mapper olur

```csharp
// önce: Features/Brands/Profiles/MappingProfiles.cs
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

// sonra: Features/Brands/Mappers/BrandMapper.cs
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

Pratik kurallar:

- Yalnız gerçekten çağırdığınız yönler için metot gerekir. `ReverseMap()` iki yönü de üretiyordu; ters yönlerin çoğu hiç kullanılmıyordu.
- `RequiredMappingStrategy.Target`, bir *hedef* üye eşlenmediğinde Mapperly'nin uyarmasını sağlar (`RMG020`); response'lar için istediğiniz budur.
  Entity hedeflerinde denetim alanlarını ve navigation property'leri `[MapperIgnoreTarget]` ile yok sayın.
- `IPaginate<T>` → `GetListResponse<TDto>` doğrudan çalışır: `Items` koleksiyonu aynı sınıftaki `To…ItemDto` metoduyla eleman eleman eşlenir.

### Üye konfigürasyonu

| AutoMapper | Mapperly |
|---|---|
| `.ForMember(d => d.FullName, o => o.MapFrom(s => s.Name))` | `[MapProperty(nameof(Source.Name), nameof(Target.FullName))]` |
| `.ForMember(d => d.City, o => o.MapFrom(s => s.Address.City))` | `[MapProperty("Address.City", nameof(Target.City))]` |
| `.ForMember(d => d.Secret, o => o.Ignore())` | `[MapperIgnoreTarget(nameof(Target.Secret))]` |
| `.ForSourceMember(s => s.Internal, o => o.DoNotValidate())` | `[MapperIgnoreSource(nameof(Source.Internal))]` |
| `.ForMember(d => d.X, o => o.MapFrom(s => s.A + s.B))` (ifade) | mapper'a normal bir metot yazın ve çağırın; `Use = nameof(...)` yalnız 1:1 dönüşümler için, dolayısıyla genelde: `public static Target ToTarget(Source s) { var t = ToTargetCore(s); t.X = s.A + s.B; return t; }` |
| `.ConstructUsing(...)`, `.ConvertUsing(...)` | el yazımı metot (Mapperly parametre adları eşleşen constructor'lara otomatik eşler) |
| `.BeforeMap` / `.AfterMap` | el yazımı sarmalayıcı metot |
| `.IncludeBase<,>()` | `[MapDerivedType<TBase, TDerived>]` veya açık metotlar |
| `.ForAllMembers(o => o.Condition((s, d, v) => v != null))` | `[Mapper]` üzerinde `AllowNullPropertyAssignment = false` |
| `queryable.ProjectTo<Dto>(_mapper.ConfigurationProvider)` | `public static partial IQueryable<Dto> ProjectToDto(this IQueryable<Entity> query);` |

Bilmeye değer Mapperly tanıları: `RMG020` eşlenmeyen hedef üye, `RMG012` eşlenmeyen kaynak üye (yalnız `Source` stratejisinde),
`RMG037` eşlenmeyen `required` üye. Bunlar uyarıdır; kontrol listesi gibi kullanın.

### Handler'lar ve servisler

```csharp
// önce
private readonly IMapper _mapper;
public CreateBrandCommandHandler(IMapper mapper, IBrandRepository brandRepository, BrandBusinessRules rules) { _mapper = mapper; ... }
Brand brand = _mapper.Map<Brand>(request);
CreatedBrandResponse response = _mapper.Map<CreatedBrandResponse>(brand);

// sonra
public CreateBrandCommandHandler(IBrandRepository brandRepository, BrandBusinessRules rules) { ... }
Brand brand = BrandMapper.ToEntity(request);
CreatedBrandResponse response = BrandMapper.ToCreatedResponse(brand);

// güncelleme: önce
Brand mappedBrand = _mapper.Map(request, destination: brand!);
await _brandRepository.UpdateAsync(mappedBrand);
// güncelleme: sonra
BrandMapper.UpdateEntity(request, brand!);
await _brandRepository.UpdateAsync(brand!);
```

Sonra `services.AddAutoMapper(...)` satırını, `Profiles` klasörlerini ve tüm `using AutoMapper;` satırlarını silin.

## Adım 4 – Core'daki kırıcı değişiklikler

- **Swagger**: `using Microsoft.OpenApi.Models;` → `using Microsoft.OpenApi;`. `BearerSecurityRequirementOperationFilter` artık yalnız
  requirement ekler; şema `AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "Bearer", ... })`
  ile tanımlanmalıdır (starter zaten böyle).
- **`AddDbMigrationApplier`** `Func<IServiceProvider, TDbContext>` alır. Alışıldık `sp => sp.GetRequiredService<BaseDbContext>()` lambda'sı değişmeden derlenir.
- **Elasticsearch**: `IElasticSearch.GetIndexList()` → `await GetIndexListAsync()`; `ElasticSearchGetModel<T>.ElasticId` artık `string`.
- **`BaseMockRepository`**: bkz. adım 5.
- **Pipeline'lar**: log detayındaki `MethodName` artık derleyicinin lambda adı değil request tipinin adıdır.

## Adım 5 – Test projesi

```csharp
// önce
public class BrandMockRepository : BaseMockRepository<IBrandRepository, Brand, Guid, MappingProfiles, BrandBusinessRules, BrandFakeData>
_handler = new CreateBrandCommandHandler(Mapper, MockRepository.Object, BusinessRules);

// sonra
public class BrandMockRepository : BaseMockRepository<IBrandRepository, Brand, Guid, BrandBusinessRules, BrandFakeData>
_handler = new CreateBrandCommandHandler(MockRepository.Object, BusinessRules);
```

Core.Test 2.0'daki `MockRepositoryHelper` `withDeleted`'i uygular, `orderBy`'ı çalıştırır, `index`/`size` ile sayfalar ve `Count`/`Pages` doldurur;
eski "her şeyi döndür" davranışına güvenen liste doğrulamaları yeterince büyük bir `PageSize` isteyebilir.

İsteğe bağlı (`nArchGen upgrade`'de varsayılan): xunit v3 + Microsoft Testing Platform'a geçiş.

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
  <!-- xunit, Microsoft.NET.Test.Sdk, coverlet.collector kaldırılır -->
</ItemGroup>
```

`Startup` tabanlı constructor injection `Xunit.DependencyInjection` 12 ile çalışmaya devam eder.

## Adım 6 – Doğrulama

```bash
dotnet restore
dotnet build -c Release          # 0 hata; RMG uyarılarını gözden geçirin
dotnet test -c Release
grep -rn "MediatR\|AutoMapper" src tests   # boş olmalı
dotnet run --project src/<project>/WebAPI  # Swagger'da Bearer kilidi görünür, login/register JSON body ile çalışır
```

Davranış kontrolleri: bir entity'ye `PUT`, `Get<Plural>` cache grubunu boşaltır (sonraki `GET` listesi değişikliği yansıtır) ve sayfalı
yanıtlar doğru `pages` değerini verir.

## Sorun giderme

- Başlangıçta **`Cannot resolve scoped service 'BaseDbContext' from root provider`**: `NArchitecture.Core.Persistence.WebApi` < 2.0.0 kullanıyorsunuz; 2.0'daki `UseDbMigrationApplier` scope açar.
- **Üretilen mapper'larda `RMG020` uyarıları**: hedefte kaynakta olmayan üyeler var. Ya veriyi kaynağa ekleyin ya da üyeyi açıkça yok sayın.
- **Response DTO'larında `CS8618`**: Gen 1.x çıktısından gelen nullable uyarıları; string property'lere varsayılan değer verin veya `required` yapın.
- **`dotnet tool restore` `narchitecture.gen`'de hata veriyor**: manifest 2.0.0'ı sabitler; yayınlandığından emin olun (veya yerel feed ekleyin).
- **Login şifre için doğrulama hatası döndürüyor**: hâlâ Core.Application < 2.0.0 çalışıyor; eski DTO şifreyi girişte yok sayıyordu.

## Otomatik yükseltme

```bash
cd <solution kökü>
# 1.x projelerin çoğunda eski Gen sürümünü sabitleyen yerel bir araç manifesti (.config/dotnet-tools.json) vardır.
# `dotnet nArchGen ...` önce o manifesti çözer; yalnız global araçı güncellemek `upgrade`'i ortaya çıkarmaz.
if [ -f .config/dotnet-tools.json ]; then
  dotnet tool update NArchitecture.Gen --version 2.0.0     # yerel manifesti günceller
else
  dotnet tool update -g NArchitecture.Gen --version 2.0.0  # global aracı günceller/kurar
fi

nArchGen upgrade --dry-run        # yalnız analiz; neyin değişeceğini ve neyin manuel kalacağını yazar
nArchGen upgrade                  # 1–5. adımları uygular, sonra build alır ve MIGRATION-REPORT.md yazar
```

Seçenekler: `src/` altında birden çok proje varsa `--project <ad>`, xunit 2'de kalmak için `--keep-test-framework`, `--skip <adım,...>`
(`project-files`, `mediator`, `automapper`, `tests`, `web-api`, `format`, `verify`), `--no-build`, kirli çalışma ağacında çalışmak için `--force`.
Dosyaları yazdıktan sonra araç mümkünse solution'ın kendi `csharpier`'ını (`.config/dotnet-tools.json`) çalıştırır, ardından
`dotnet restore` + `dotnet build` yapar ve solution'ın yanına `MIGRATION-REPORT.md` bırakır.

Aracın yapmadığı ve rapora yazdığı şeyler: `INotification`/`Publish`, AutoMapper `ConstructUsing`/`ConvertUsing`/`BeforeMap`/`AfterMap`/`Condition`/`IncludeBase`/`ForAllMembers`,
üye yolu olmayan ifadeli `MapFrom`, kaynak tipi çıkarılamayan `IMapper` kullanımları ve testlerdeki `MapperConfiguration` örnekleri.
Her biri `MIGRATION-REPORT.md` içinde `dosya:satır – neden` biçiminde, bu rehberin ilgili bölümüne linkle listelenir.
