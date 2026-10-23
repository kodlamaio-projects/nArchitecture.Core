using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.Mediator;

namespace Core.Application.Tests.Caching;

public class CacheRemovingBehaviorTests
{
    private sealed record RemoveCommand(string? Key, string[]? Groups, bool Bypass = false)
        : IRequest<string>,
            ICacheRemoverRequest
    {
        public bool BypassCache => Bypass;
        public string? CacheKey => Key;
        public string[]? CacheGroupKey => Groups;
    }

    private static IDistributedCache newCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static CacheRemovingBehavior<RemoveCommand, string> newBehavior(IDistributedCache cache) =>
        new(cache, NullLogger<CacheRemovingBehavior<RemoveCommand, string>>.Instance);

    private static async Task seedGroup(IDistributedCache cache, string group, params string[] keys)
    {
        foreach (string key in keys)
            await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes($"value-{key}"), TestContext.Current.CancellationToken);
        await cache.SetAsync(
            group,
            JsonSerializer.SerializeToUtf8Bytes(new HashSet<string>(keys)),
            TestContext.Current.CancellationToken
        );
        await cache.SetAsync(
            $"{group}SlidingExpiration",
            JsonSerializer.SerializeToUtf8Bytes(300),
            TestContext.Current.CancellationToken
        );
    }

    [Fact]
    public async Task Removes_every_key_in_each_group_plus_group_bookkeeping_keys()
    {
        IDistributedCache cache = newCache();
        await seedGroup(cache, "G1", "a", "b");
        await seedGroup(cache, "G2", "c");

        string response = await newBehavior(cache)
            .Handle(
                new RemoveCommand(Key: null, Groups: ["G1", "G2"]),
                _ => Task.FromResult("ok"),
                TestContext.Current.CancellationToken
            );

        Assert.Equal("ok", response);
        foreach (string key in new[] { "a", "b", "c", "G1", "G2", "G1SlidingExpiration", "G2SlidingExpiration" })
            Assert.Null(await cache.GetAsync(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Removes_single_cache_key()
    {
        IDistributedCache cache = newCache();
        await cache.SetAsync("single", JsonSerializer.SerializeToUtf8Bytes("x"), TestContext.Current.CancellationToken);

        await newBehavior(cache)
            .Handle(new RemoveCommand("single", Groups: null), _ => Task.FromResult("ok"), TestContext.Current.CancellationToken);

        Assert.Null(await cache.GetAsync("single", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bypass_leaves_cache_untouched()
    {
        IDistributedCache cache = newCache();
        await seedGroup(cache, "G", "a");

        await newBehavior(cache)
            .Handle(
                new RemoveCommand("a", Groups: ["G"], Bypass: true),
                _ => Task.FromResult("ok"),
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(await cache.GetAsync("a", TestContext.Current.CancellationToken));
        Assert.NotNull(await cache.GetAsync("G", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handler_runs_before_removal_so_stale_entries_do_not_survive()
    {
        IDistributedCache cache = newCache();
        await seedGroup(cache, "G", "a");

        await newBehavior(cache)
            .Handle(
                new RemoveCommand(Key: null, Groups: ["G"]),
                async _ =>
                {
                    Assert.NotNull(await cache.GetAsync("a", TestContext.Current.CancellationToken));
                    return "ok";
                },
                TestContext.Current.CancellationToken
            );

        Assert.Null(await cache.GetAsync("a", TestContext.Current.CancellationToken));
    }
}
