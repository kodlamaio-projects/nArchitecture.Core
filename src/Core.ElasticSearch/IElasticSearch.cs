using Elastic.Clients.Elasticsearch.IndexManagement;
using NArchitecture.Core.ElasticSearch.Models;

namespace NArchitecture.Core.ElasticSearch;

public interface IElasticSearch
{
    Task<IElasticSearchResult> CreateNewIndexAsync(IndexModel indexModel, CancellationToken cancellationToken = default);
    Task<IElasticSearchResult> InsertAsync(ElasticSearchInsertUpdateModel model, CancellationToken cancellationToken = default);
    Task<IElasticSearchResult> InsertManyAsync(string indexName, object[] items, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, IndexState>> GetIndexListAsync(CancellationToken cancellationToken = default);
    Task<List<ElasticSearchGetModel<T>>> GetAllSearch<T>(
        SearchParameters parameters,
        CancellationToken cancellationToken = default
    )
        where T : class;
    Task<List<ElasticSearchGetModel<T>>> GetSearchByField<T>(
        SearchByFieldParameters fieldParameters,
        CancellationToken cancellationToken = default
    )
        where T : class;
    Task<List<ElasticSearchGetModel<T>>> GetSearchBySimpleQueryString<T>(
        SearchByQueryParameters queryParameters,
        CancellationToken cancellationToken = default
    )
        where T : class;
    Task<IElasticSearchResult> UpdateByElasticIdAsync(
        ElasticSearchInsertUpdateModel model,
        CancellationToken cancellationToken = default
    );
    Task<IElasticSearchResult> DeleteByElasticIdAsync(ElasticSearchModel model, CancellationToken cancellationToken = default);
}
