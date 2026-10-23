using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using NArchitecture.Core.ElasticSearch.Constants;
using NArchitecture.Core.ElasticSearch.Models;

namespace NArchitecture.Core.ElasticSearch;

public class ElasticSearchManager : IElasticSearch
{
    private readonly ElasticsearchClient _client;

    public ElasticSearchManager(ElasticSearchConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        SingleNodePool pool = new(new Uri(configuration.ConnectionString));
        ElasticsearchClientSettings settings = new(
            pool,
            sourceSerializer: (_, clientSettings) =>
                new DefaultSourceSerializer(clientSettings, options => options.ReferenceHandler = ReferenceHandler.IgnoreCycles)
        );
        if (!string.IsNullOrEmpty(configuration.UserName))
            settings.Authentication(new BasicAuthentication(configuration.UserName, configuration.Password));

        _client = new ElasticsearchClient(settings);
    }

    public async Task<IReadOnlyDictionary<string, IndexState>> GetIndexListAsync(CancellationToken cancellationToken = default)
    {
        GetIndexResponse response = await _client.Indices.GetAsync(Indices.All, cancellationToken);
        return response.Indices;
    }

    public async Task<IElasticSearchResult> InsertManyAsync(
        string indexName,
        object[] items,
        CancellationToken cancellationToken = default
    )
    {
        ensureIndexName(indexName);

        BulkResponse response = await _client.BulkAsync(b => b.Index(indexName).IndexMany(items), cancellationToken);
        return toResult(response);
    }

    public async Task<IElasticSearchResult> CreateNewIndexAsync(
        IndexModel indexModel,
        CancellationToken cancellationToken = default
    )
    {
        ensureIndexName(indexModel.IndexName);

        Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse existsResponse = await _client.Indices.ExistsAsync(
            indexModel.IndexName,
            cancellationToken
        );
        if (existsResponse.Exists)
            return new ElasticSearchResult(success: false, message: ElasticSearchMessages.IndexAlreadyExists);

        CreateIndexRequest request = new(indexModel.IndexName)
        {
            Settings = new IndexSettings
            {
                NumberOfReplicas = indexModel.NumberOfReplicas,
                NumberOfShards = indexModel.NumberOfShards,
            },
            Aliases = new Dictionary<Name, Alias> { [indexModel.AliasName] = new Alias() },
        };
        CreateIndexResponse response = await _client.Indices.CreateAsync(request, cancellationToken);
        return toResult(response);
    }

    public async Task<IElasticSearchResult> DeleteByElasticIdAsync(
        ElasticSearchModel model,
        CancellationToken cancellationToken = default
    )
    {
        ensureIndexName(model.IndexName);

        DeleteResponse response = await _client.DeleteAsync(model.IndexName, model.ElasticId, cancellationToken);
        return toResult(response);
    }

    public async Task<List<ElasticSearchGetModel<T>>> GetAllSearch<T>(
        SearchParameters parameters,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ensureIndexName(parameters.IndexName);

        SearchResponse<T> response = await _client.SearchAsync<T>(
            s => s.Indices(parameters.IndexName).From(parameters.From).Size(parameters.Size),
            cancellationToken
        );
        return toGetModels(response);
    }

    public async Task<List<ElasticSearchGetModel<T>>> GetSearchByField<T>(
        SearchByFieldParameters fieldParameters,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ensureIndexName(fieldParameters.IndexName);

        SearchResponse<T> response = await _client.SearchAsync<T>(
            s =>
                s.Indices(fieldParameters.IndexName)
                    .From(fieldParameters.From)
                    .Size(fieldParameters.Size)
                    .Query(q => q.Match(m => m.Field(fieldParameters.FieldName!).Query(fieldParameters.Value))),
            cancellationToken
        );
        return toGetModels(response);
    }

    public async Task<List<ElasticSearchGetModel<T>>> GetSearchBySimpleQueryString<T>(
        SearchByQueryParameters queryParameters,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ensureIndexName(queryParameters.IndexName);

        const string analyzer = "standard",
            minimumShouldMatch = "30%";
        SearchResponse<T> response = await _client.SearchAsync<T>(
            s =>
                s.Indices(queryParameters.IndexName)
                    .From(queryParameters.From)
                    .Size(queryParameters.Size)
                    .Query(q =>
                        q.SimpleQueryString(c =>
                            c.QueryName(queryParameters.QueryName)
                                .Boost(1.1f)
                                .Fields(queryParameters.Fields)
                                .Query(queryParameters.Query)
                                .Analyzer(analyzer)
                                .DefaultOperator(Operator.Or)
                                .Flags(SimpleQueryStringFlags.And | SimpleQueryStringFlags.Near)
                                .Lenient(true)
                                .AnalyzeWildcard(false)
                                .MinimumShouldMatch(minimumShouldMatch)
                                .FuzzyPrefixLength(0)
                                .FuzzyMaxExpansions(50)
                                .FuzzyTranspositions(true)
                                .AutoGenerateSynonymsPhraseQuery(false)
                        )
                    ),
            cancellationToken
        );
        return toGetModels(response);
    }

    public async Task<IElasticSearchResult> InsertAsync(
        ElasticSearchInsertUpdateModel model,
        CancellationToken cancellationToken = default
    )
    {
        ensureIndexName(model.IndexName);

        IndexResponse response = await _client.IndexAsync(
            model.Item,
            i => i.Index(model.IndexName).Id(model.ElasticId).Refresh(Refresh.True),
            cancellationToken
        );
        return toResult(response);
    }

    public async Task<IElasticSearchResult> UpdateByElasticIdAsync(
        ElasticSearchInsertUpdateModel model,
        CancellationToken cancellationToken = default
    )
    {
        ensureIndexName(model.IndexName);

        UpdateResponse<object> response = await _client.UpdateAsync<object, object>(
            model.IndexName,
            model.ElasticId,
            u => u.Doc(model.Item),
            cancellationToken
        );
        return toResult(response);
    }

    private static void ensureIndexName(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
            throw new ArgumentException(ElasticSearchMessages.IndexNameCannotBeNullOrEmpty, nameof(indexName));
    }

    private static ElasticSearchResult toResult(ElasticsearchResponse response) =>
        new(
            response.IsValidResponse,
            message: response.IsValidResponse
                ? ElasticSearchMessages.Success
                : response.ElasticsearchServerError?.Error?.Reason ?? response.DebugInformation
        );

    private static List<ElasticSearchGetModel<T>> toGetModels<T>(SearchResponse<T> response)
        where T : class =>
        response
            .Hits.Select(hit => new ElasticSearchGetModel<T> { ElasticId = hit.Id ?? string.Empty, Item = hit.Source! })
            .ToList();
}
