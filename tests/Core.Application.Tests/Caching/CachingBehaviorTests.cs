using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.Mediator;

namespace Core.Application.Tests.Caching;

public class CachingBehaviorTests
{
    private sealed record CachedQuery(string Key, bool Bypass = false, string? Group = null) : IRequest<string>, ICachableRequest
    {
        public bool BypassCache => Bypass;
        public string CacheKey => Key;
        public string? CacheGroupKey => Group;
        public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
    }

    private static readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["CacheSettings:SlidingExpiration"] = "2" })
        .Build();

    private static IDistributedCache newCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static CachingBehavior<CachedQuery, string> newBehavior(IDistributedCache cache) =>
        new(cache, NullLogger<CachingBehavior<CachedQuery, string>>.Instance, _configuration);

    [Fact]
    public async Task Miss_calls_handler_and_stores_utf8_json()
    {
        IDistributedCache cache = newCache();
        int calls = 0;

        string response = await newBehavior(cache)
            .Handle(new CachedQuery("k"), _ => Task.FromResult($"value-{++calls}"), TestContext.Current.CancellationToken);

        Assert.Equal("value-1", response);
        Assert.Equal(1, calls);
        byte[]? stored = await cache.GetAsync("k", TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal("value-1", JsonSerializer.Deserialize<string>(stored));
    }

    [Fact]
    public async Task Hit_returns_cached_value_without_calling_handler()
    {
        IDistributedCache cache = newCache();
        await cache.SetAsync("k", JsonSerializer.SerializeToUtf8Bytes("cached"), TestContext.Current.CancellationToken);
        int calls = 0;

        string response = await newBehavior(cache)
            .Handle(new CachedQuery("k"), _ => Task.FromResult($"value-{++calls}"), TestContext.Current.CancellationToken);

        Assert.Equal("cached", response);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Bypass_skips_cache_entirely()
    {
        IDistributedCache cache = newCache();
        int calls = 0;

        string response = await newBehavior(cache)
            .Handle(
                new CachedQuery("k", Bypass: true),
                _ => Task.FromResult($"value-{++calls}"),
                TestContext.Current.CancellationToken
            );

        Assert.Equal("value-1", response);
        Assert.Null(await cache.GetAsync("k", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Group_key_tracks_member_keys_and_sliding_expiration()
    {
        IDistributedCache cache = newCache();
        CachingBehavior<CachedQuery, string> behavior = newBehavior(cache);

        await behavior.Handle(
            new CachedQuery("k1", Group: "G"),
            _ => Task.FromResult("a"),
            TestContext.Current.CancellationToken
        );
        await behavior.Handle(
            new CachedQuery("k2", Group: "G"),
            _ => Task.FromResult("b"),
            TestContext.Current.CancellationToken
        );

        byte[]? group = await cache.GetAsync("G", TestContext.Current.CancellationToken);
        Assert.NotNull(group);
        HashSet<string> keys = JsonSerializer.Deserialize<HashSet<string>>(group)!;
        Assert.Equal(["k1", "k2"], keys.Order());

        byte[]? sliding = await cache.GetAsync("GSlidingExpiration", TestContext.Current.CancellationToken);
        Assert.NotNull(sliding);
        Assert.Equal((int)TimeSpan.FromMinutes(5).TotalSeconds, JsonSerializer.Deserialize<int>(sliding));
    }

    [Fact]
    public void Missing_CacheSettings_section_throws_descriptive_error()
    {
        IConfiguration empty = new ConfigurationBuilder().Build();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new CachingBehavior<CachedQuery, string>(newCache(), NullLogger<CachingBehavior<CachedQuery, string>>.Instance, empty)
        );

        Assert.Contains("CacheSettings", ex.Message);
    }
}
