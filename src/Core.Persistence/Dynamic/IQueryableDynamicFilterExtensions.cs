using System.Linq.Dynamic.Core;
using System.Text;

namespace NArchitecture.Core.Persistence.Dynamic;

public static class IQueryableDynamicFilterExtensions
{
    private static readonly string[] _orders = { "asc", "desc" };
    private static readonly string[] _logics = { "and", "or" };

    private static readonly IDictionary<string, string> _operators = new Dictionary<string, string>
    {
        { "eq", "=" },
        { "neq", "!=" },
        { "lt", "<" },
        { "lte", "<=" },
        { "gt", ">" },
        { "gte", ">=" },
        { "isnull", "== null" },
        { "isnotnull", "!= null" },
        { "startswith", "StartsWith" },
        { "endswith", "EndsWith" },
        { "contains", "Contains" },
        { "doesnotcontain", "Contains" },
        { "in", "In" },
        { "between", "Between" }
    };

    public static IQueryable<T> ToDynamic<T>(this IQueryable<T> query, DynamicQuery dynamicQuery)
    {
        // Test
        Console.WriteLine(dynamicQuery.Filter.Value);
        if (dynamicQuery.Filter is not null)
            query = Filter(query, dynamicQuery.Filter);
        if (dynamicQuery.Sort is not null && dynamicQuery.Sort.Any())
            query = Sort(query, dynamicQuery.Sort);
        return query;
    }

    private static IQueryable<T> Filter<T>(IQueryable<T> queryable, Filter filter)
    {
        IList<Filter> filters = GetAllFilters(filter);
        var values = new List<object>();
        foreach (var f in filters)
        {
            if (f.Operator == "in")
            {
                Console.WriteLine(f.Value);
                var inValues = f.Value.Split(',').Select(v => v.Trim());
                values.AddRange(inValues);
            }
            else if (f.Operator == "between")
            {
                var betweenValues = f.Value.Split(',');
                if (betweenValues.Length != 2)
                    throw new ArgumentException("Invalid Value for 'between' operator");

                values.AddRange(betweenValues.Select(v => v.Trim()));
            }
            else
            {
                values.Add(f.Value);
            }
        }

        string where = Transform(filter, filters);
        Console.WriteLine(where);
        if (!string.IsNullOrEmpty(where) && values != null)
            queryable = queryable.Where(where, values.ToArray());

        return queryable;
    }

    private static IQueryable<T> Sort<T>(IQueryable<T> queryable, IEnumerable<Sort> sort)
    {
        foreach (Sort item in sort)
        {
            if (string.IsNullOrEmpty(item.Field))
                throw new ArgumentException("Invalid Field");
            if (string.IsNullOrEmpty(item.Dir) || !_orders.Contains(item.Dir))
                throw new ArgumentException("Invalid Order Type");
        }

        if (sort.Any())
        {
            string ordering = string.Join(separator: ",", values: sort.Select(s => $"{s.Field} {s.Dir}"));
            return queryable.OrderBy(ordering);
        }

        return queryable;
    }

    public static IList<Filter> GetAllFilters(Filter filter)
    {
        List<Filter> filters = [];
        GetFilters(filter, filters);
        return filters;
    }

    private static void GetFilters(Filter filter, IList<Filter> filters)
    {
        filters.Add(filter);
        if (filter.Filters is not null && filter.Filters.Any())
            foreach (Filter item in filter.Filters)
                GetFilters(item, filters);
    }

    public static string Transform(Filter filter, IList<Filter> filters)
    {
        if (string.IsNullOrEmpty(filter.Field))
            throw new ArgumentException("Invalid Field");
        if (string.IsNullOrEmpty(filter.Operator) || !_operators.ContainsKey(filter.Operator))
            throw new ArgumentException("Invalid Operator");

        int index = filters.IndexOf(filter);
        string comparison = _operators[filter.Operator];
        StringBuilder where = new();

        if (!string.IsNullOrEmpty(filter.Value))
            if (filter.Operator == "doesnotcontain")
                where.Append($"(!np({filter.Field}).{comparison}(@{index.ToString()}))");
            else if (comparison is "StartsWith" or "EndsWith" or "Contains")
            {
                if (!filter.CaseSensitive)
                {
                    where.Append($"(np({filter.Field}).ToLower().{comparison}(@{index.ToString()}.ToLower()))");
                }
                else
                {
                    where.Append($"(np({filter.Field}).{comparison}(@{index.ToString()}))");
                }
            }
            else if (filter.Operator == "in")
            {
                var valueCount = filter.Value.Split(',').Length;
                var paramIndexes = Enumerable.Range(index, valueCount)
                                    .Select(i => $"@{i}")
                                    .ToArray();

                if (!filter.CaseSensitive)
                {
                    where.Append($"np({filter.Field}).ToLower() in ({string.Join(",", paramIndexes)})");
                }
                else
                {
                    where.Append($"np({filter.Field}) in ({string.Join(",", paramIndexes)})");
                }
            }
            else if (filter.Operator == "between")
            {
                var values = filter.Value.Split(',');
                if (values.Length != 2)
                    throw new ArgumentException("Invalid Value for 'between' operator");

                var lowerBound = $"@{index++}";
                var upperBound = $"@{index++}";

                where.Append($"(np({filter.Field}) >= {lowerBound} and np({filter.Field}) <= {upperBound})");
            }
            else
                where.Append($"np({filter.Field}) {comparison} @{index.ToString()}");
        else if (filter.Operator is "isnull" or "isnotnull")
            where.Append($"np({filter.Field}) {comparison}");

        if (filter.Logic is not null && filter.Filters is not null && filter.Filters.Any())
        {
            if (!_logics.Contains(filter.Logic))
                throw new ArgumentException("Invalid Logic");
            return $"{where} {filter.Logic} ({string.Join(separator: $" {filter.Logic} ", value: filter.Filters.Select(f => Transform(f, filters)).ToArray())})";
        }

        return where.ToString();
    }
}
