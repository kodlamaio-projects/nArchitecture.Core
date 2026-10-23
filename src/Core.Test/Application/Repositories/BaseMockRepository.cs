using Moq;
using NArchitecture.Core.Application.Rules;
using NArchitecture.Core.Localization.Resource.Yaml;
using NArchitecture.Core.Persistence.Repositories;
using NArchitecture.Core.Test.Application.FakeData;
using NArchitecture.Core.Test.Application.Helpers;

namespace NArchitecture.Core.Test.Application.Repositories;

/// <summary>
/// Builds a Moq-backed repository seeded with <typeparamref name="TFakeData"/> and the matching business rules.
/// Mapping is no longer part of this base: construct your handler with the Mapperly mapper (or any mapper) directly.
/// </summary>
public abstract class BaseMockRepository<TRepository, TEntity, TEntityId, TBusinessRules, TFakeData>
    where TEntity : Entity<TEntityId>, new()
    where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    where TBusinessRules : BaseBusinessRules
    where TFakeData : BaseFakeData<TEntity, TEntityId>, new()
{
    public Mock<TRepository> MockRepository;
    public TBusinessRules BusinessRules;

    protected BaseMockRepository(TFakeData fakeData)
    {
        MockRepository = MockRepositoryHelper.GetRepository<TRepository, TEntity, TEntityId>(fakeData.Data);
        BusinessRules =
            (TBusinessRules)
                Activator.CreateInstance(
                    type: typeof(TBusinessRules),
                    MockRepository.Object,
                    new ResourceLocalizationManager(resources: []) { AcceptLocales = new[] { "en" } }
                )!
            ?? throw new InvalidOperationException($"Cannot create an instance of {typeof(TBusinessRules).FullName}.");
    }
}
