namespace NArchitecture.Core.ElasticSearch.Models;

public class ElasticSearchInsertUpdateModel : ElasticSearchModel
{
    public object Item { get; set; }

    public ElasticSearchInsertUpdateModel(object item)
    {
        Item = item;
    }

    public ElasticSearchInsertUpdateModel(string elasticId, string indexName, object item)
        : base(elasticId, indexName)
    {
        Item = item;
    }
}
