namespace Core.ElasticSearch.Models;

public class SearchByFieldParameters : SearchParameters
{
    public string FieldName { get; set; }
    public string Value { get; set; }

    public SearchByFieldParameters()
    {
        FieldName = string.Empty;
        Value = string.Empty;
    }

    public SearchByFieldParameters(string fieldName, string value)
    {
        FieldName = fieldName;
        Value = value;
    }
}
