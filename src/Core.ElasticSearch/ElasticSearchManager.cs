using Elasticsearch.Net;
using NArchitecture.Core.ElasticSearch.Constants;
using NArchitecture.Core.ElasticSearch.Models;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;

namespace NArchitecture.Core.ElasticSearch;

public class ElasticSearchManager : IElasticSearch
{
    private readonly ConnectionSettings _connectionSettings;

    public ElasticSearchManager(ElasticSearchConfig configuration)
    {
        SingleNodeConnectionPool pool = new(new Uri(configuration.ConnectionString));
        _connectionSettings = new ConnectionSettings(
            pool,
            sourceSerializer: (builtInSerializer, connectionSettings) =>
                new JsonNetSerializer(
                    builtInSerializer,
                    connectionSettings,
                    jsonSerializerSettingsFactory: () =>
                        new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
                )
        );
    }

    public IReadOnlyDictionary<IndexName, IndexState> GetIndexList()
    {
        ElasticClient elasticClient = new(_connectionSettings);
        return elasticClient.Indices.Get(new GetIndexRequest(Indices.All)).Indices;
    }

    public async Task<IElasticSearchResult> InsertManyAsync(string indexName, object[] items)
    {
        ElasticClient elasticClient = getElasticClient(indexName);
        await elasticClient.BulkAsync(a => a.Index(indexName).IndexMany(items));

        return new ElasticSearchResult();
    }

    public async Task<IElasticSearchResult> CreateNewIndexAsync(IndexModel indexModel)
    {
        ElasticClient elasticClient = getElasticClient(indexModel.IndexName);
        if (elasticClient.Indices.Exists(indexModel.IndexName).Exists)
            return new ElasticSearchResult(success: false, message: ElasticSearchMessages.IndexAlreadyExists);

        CreateIndexResponse? response = await elasticClient.Indices.CreateAsync(
            indexModel.IndexName,
            selector: se =>
                se.Settings(a => a.NumberOfReplicas(indexModel.NumberOfReplicas).NumberOfShards(indexModel.NumberOfShards))
                    .Aliases(x => x.Alias(indexModel.AliasName))
        );

        return new ElasticSearchResult(
            response.IsValid,
            message: response.IsValid ? ElasticSearchMessages.Success : response.ServerError.Error.Reason
        );
    }

    public async Task<IElasticSearchResult> DeleteByElasticIdAsync(ElasticSearchModel model)
    {
        ElasticClient elasticClient = getElasticClient(model.IndexName);
        DeleteResponse? response = await elasticClient.DeleteAsync<object>(
            model.ElasticId,
            selector: x => x.Index(model.IndexName)
        );
        return new ElasticSearchResult(
            response.IsValid,
            message: response.IsValid ? ElasticSearchMessages.Success : response.ServerError.Error.Reason
        );
    }

    public async Task<List<ElasticSearchGetModel<T>>> GetAllSearch<T>(SearchParameters parameters)
        where T : class
    {
        ElasticClient elasticClient = getElasticClient(parameters.IndexName);
        ISearchResponse<T>? searchResponse = await elasticClient.SearchAsync<T>(s =>
            s.Index(Indices.Index(parameters.IndexName)).From(parameters.From).Size(parameters.Size)
        );

        var list = searchResponse.Hits.Select(x => new ElasticSearchGetModel<T> { ElasticId = x.Id, Item = x.Source }).ToList();

        return list;
    }

    public async Task<List<ElasticSearchGetModel<T>>> GetSearchByField<T>(SearchByFieldParameters fieldParameters)
        where T : class
    {
        ElasticClient elasticClient = getElasticClient(fieldParameters.IndexName);
        ISearchResponse<T>? searchResponse = await elasticClient.SearchAsync<T>(s =>
            s.Index(fieldParameters.IndexName).From(fieldParameters.From).Size(fieldParameters.Size)
        );

        var list = searchResponse.Hits.Select(x => new ElasticSearchGetModel<T> { ElasticId = x.Id, Item = x.Source }).ToList();
        return list;
    }

    public async Task<List<ElasticSearchGetModel<T>>> GetSearchBySimpleQueryString<T>(SearchByQueryParameters queryParameters)
        where T : class
    {
        const string analyzer = "standard",
            minimumShouldMatch = "30%";
        ElasticClient elasticClient = getElasticClient(queryParameters.IndexName);
        ISearchResponse<T>? searchResponse = await elasticClient.SearchAsync<T>(s =>
            s.Index(queryParameters.IndexName)
                .From(queryParameters.From)
                .Size(queryParameters.Size)
                .MatchAll()
                .Query(a =>
                    a.SimpleQueryString(c =>
                        c.Name(queryParameters.QueryName)
                            .Boost(1.1)
                            .Fields(queryParameters.Fields)
                            .Query(queryParameters.Query)
                            .Analyzer(analyzer)
                            .DefaultOperator(Operator.Or)
                            .Flags(SimpleQueryStringFlags.And | SimpleQueryStringFlags.Near)
                            .Lenient()
                            .AnalyzeWildcard(false)
                            .MinimumShouldMatch(minimumShouldMatch)
                            .FuzzyPrefixLength(0)
                            .FuzzyMaxExpansions(50)
                            .FuzzyTranspositions()
                            .AutoGenerateSynonymsPhraseQuery(false)
                    )
                )
        );

        var list = searchResponse.Hits.Select(x => new ElasticSearchGetModel<T> { ElasticId = x.Id, Item = x.Source }).ToList();
        return list;
    }

    public async Task<IElasticSearchResult> InsertAsync(ElasticSearchInsertUpdateModel model)
    {
        ElasticClient elasticClient = getElasticClient(model.IndexName);

        IndexResponse? response = await elasticClient.IndexAsync(
            model.Item,
            selector: i => i.Index(model.IndexName).Id(model.ElasticId).Refresh(Refresh.True)
        );

        return new ElasticSearchResult(
            response.IsValid,
            message: response.IsValid ? ElasticSearchMessages.Success : response.ServerError.Error.Reason
        );
    }

    public async Task<IElasticSearchResult> UpdateByElasticIdAsync(ElasticSearchInsertUpdateModel model)
    {
        ElasticClient elasticClient = getElasticClient(model.IndexName);
        UpdateResponse<object>? response = await elasticClient.UpdateAsync<object>(
            model.ElasticId,
            selector: u => u.Index(model.IndexName).Doc(model.Item)
        );
        return new ElasticSearchResult(
            response.IsValid,
            message: response.IsValid ? ElasticSearchMessages.Success : response.ServerError.Error.Reason
        );
    }

    private ElasticClient getElasticClient(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
            throw new ArgumentNullException(indexName, message: ElasticSearchMessages.IndexNameCannotBeNullOrEmpty);

        return new ElasticClient(_connectionSettings);
    }
}
