using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using NArchitecture.Core.Persistence.Paging;
using NArchitecture.Core.Persistence.Repositories;

namespace NArchitecture.Core.Test.Application.Helpers;

public static class MockRepositoryHelper
{
    public static Mock<TRepository> GetRepository<TRepository, TEntity, TEntityId>(List<TEntity> list)
        where TEntity : Entity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        var mockRepo = new Mock<TRepository>();

        Build<TRepository, TEntity, TEntityId>(mockRepo, list);
        return mockRepo;
    }

    private static void Build<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        SetupGetListAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupGetAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupAddAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupUpdateAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupDeleteAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupAnyAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
    }

    private static void SetupGetListAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        mockRepo
            .Setup(s =>
                s.GetListAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>> expression,
                    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
                    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include,
                    int index,
                    int size,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    IEnumerable<TEntity> source = withDeleted ? entityList : entityList.Where(e => !e.DeletedDate.HasValue);
                    if (expression != null)
                        source = source.Where(expression.Compile());
                    if (orderBy != null)
                        source = orderBy(source.AsQueryable());

                    List<TEntity> filtered = source.ToList();
                    int count = filtered.Count;
                    List<TEntity> page = size > 0 ? filtered.Skip(index * size).Take(size).ToList() : filtered;

                    Paginate<TEntity> paginateList = new()
                    {
                        Items = page,
                        Index = index,
                        Size = size,
                        Count = count,
                        Pages = size > 0 ? (int)Math.Ceiling(count / (double)size) : 0,
                    };
                    return paginateList;
                }
            );
    }

    private static void SetupGetAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        mockRepo
            .Setup(s =>
                s.GetAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>> expression,
                    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    IEnumerable<TEntity> source = withDeleted ? entityList : entityList.Where(e => !e.DeletedDate.HasValue);
                    TEntity? result = source.FirstOrDefault(predicate: expression.Compile());
                    return result;
                }
            );
    }

    private static void SetupAddAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        mockRepo
            .Setup(r => r.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (TEntity entity, CancellationToken cancellationToken) =>
                {
                    entityList.Add(entity);
                    return entity;
                }
            );
    }

    private static void SetupUpdateAsync<TRepository, TEntity, TEntityId2>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TEntityId2>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId2>, IRepository<TEntity, TEntityId2>
    {
        mockRepo
            .Setup(r => r.UpdateAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))!
            .ReturnsAsync(
                (TEntity entity, CancellationToken cancellationToken) =>
                {
                    int index = entityList.FindIndex(x => x.Id!.Equals(entity.Id));
                    if (index >= 0)
                        entityList[index] = entity;
                    return entity;
                }
            );
    }

    private static void SetupDeleteAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        mockRepo
            .Setup(r => r.DeleteAsync(It.IsAny<TEntity>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (TEntity entity, bool permanent, CancellationToken cancellationToken) =>
                {
                    if (!permanent)
                        entity.DeletedDate = DateTime.UtcNow;
                    else
                        entityList.Remove(entity);
                    return entity;
                }
            );
    }

    public static void SetupAnyAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        mockRepo
            .Setup(s =>
                s.AnyAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>>? expression,
                    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include,
                    bool withDeleted,
                    CancellationToken cancellationToken
                ) =>
                {
                    IEnumerable<TEntity> source = withDeleted ? entityList : entityList.Where(e => !e.DeletedDate.HasValue);
                    return expression == null ? source.Any() : source.Any(expression.Compile());
                }
            );
    }
}
