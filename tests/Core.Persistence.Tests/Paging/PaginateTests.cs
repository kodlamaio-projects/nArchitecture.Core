using NArchitecture.Core.Persistence.Paging;

namespace Core.Persistence.Tests.Paging;

public class PaginateTests
{
    [Fact]
    public void Enumerable_source_computes_count_pages_and_navigation()
    {
        Paginate<int> page = new(Enumerable.Range(0, 25), index: 1, size: 10, from: 0);

        Assert.Equal(25, page.Count);
        Assert.Equal(3, page.Pages);
        Assert.Equal(Enumerable.Range(10, 10), page.Items);
        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public void Queryable_source_computes_pages_from_count()
    {
        IQueryable<int> source = Enumerable.Range(0, 21).AsQueryable();

        Paginate<int> page = new(source, index: 2, size: 10, from: 0);

        Assert.Equal(21, page.Count);
        Assert.Equal(3, page.Pages);
        Assert.Single(page.Items);
        Assert.False(page.HasNext);
    }

    [Fact]
    public void Converting_paginate_keeps_paging_metadata()
    {
        Paginate<int, string> page = new(
            Enumerable.Range(0, 5),
            items => items.Select(i => i.ToString()),
            index: 0,
            size: 2,
            from: 0
        );

        Assert.Equal(5, page.Count);
        Assert.Equal(3, page.Pages);
        Assert.Equal(["0", "1"], page.Items);
    }

    [Fact]
    public void Paginate_From_maps_items_and_copies_metadata()
    {
        IPaginate<int> source = new Paginate<int>(Enumerable.Range(0, 5), index: 1, size: 2, from: 0);

        IPaginate<string> mapped = Paginate.From(source, items => items.Select(i => $"#{i}"));

        Assert.Equal(["#2", "#3"], mapped.Items);
        Assert.Equal(source.Pages, mapped.Pages);
        Assert.Equal(source.Count, mapped.Count);
        Assert.Equal(source.Index, mapped.Index);
    }

    [Fact]
    public void From_greater_than_index_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Paginate<int>([], index: 0, size: 10, from: 1));
    }
}
