namespace Core.ElasticSearch.Models;

public class SearchByQueryParameters : SearchParameters
{
    public string QueryName { get; set; }
    public string Query { get; set; }
    public string[] Fields { get; set; }

    public SearchByQueryParameters()
    {
        QueryName = string.Empty;
        Query = string.Empty;
        Fields = Array.Empty<string>();
    }

    public SearchByQueryParameters(string queryName, string query, string[] fields)
    {
        QueryName = queryName;
        Query = query;
        Fields = fields;
    }
}
