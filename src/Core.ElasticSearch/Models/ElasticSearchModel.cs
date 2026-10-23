namespace NArchitecture.Core.ElasticSearch.Models;

public class ElasticSearchModel
{
    public string ElasticId { get; set; }
    public string IndexName { get; set; }

    public ElasticSearchModel()
    {
        ElasticId = string.Empty;
        IndexName = string.Empty;
    }

    public ElasticSearchModel(string elasticId, string indexName)
    {
        ElasticId = elasticId;
        IndexName = indexName;
    }
}
