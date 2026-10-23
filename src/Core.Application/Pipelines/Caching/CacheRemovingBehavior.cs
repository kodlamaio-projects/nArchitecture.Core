using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;

namespace NArchitecture.Core.Application.Pipelines.Caching;

public class CacheRemovingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheRemoverRequest
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheRemovingBehavior<TRequest, TResponse>> _logger;

    public CacheRemovingBehavior(IDistributedCache cache, ILogger<CacheRemovingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request.BypassCache)
            return await next();

        TResponse response = await next();

        if (request.CacheGroupKey != null)
            foreach (string cacheGroupKey in request.CacheGroupKey)
            {
                byte[]? cachedGroup = await _cache.GetAsync(cacheGroupKey, cancellationToken);
                if (cachedGroup == null)
                    continue;

                HashSet<string> keysInGroup = JsonSerializer.Deserialize<HashSet<string>>(cachedGroup)!;
                foreach (string key in keysInGroup)
                {
                    await _cache.RemoveAsync(key, cancellationToken);
                    _logger.LogInformation("Removed Cache -> {CacheKey}", key);
                }

                await _cache.RemoveAsync(cacheGroupKey, cancellationToken);
                _logger.LogInformation("Removed Cache -> {CacheGroupKey}", cacheGroupKey);

                string cacheGroupSlidingExpirationKey = $"{cacheGroupKey}SlidingExpiration";
                await _cache.RemoveAsync(cacheGroupSlidingExpirationKey, cancellationToken);
                _logger.LogInformation("Removed Cache -> {CacheGroupSlidingExpirationKey}", cacheGroupSlidingExpirationKey);
            }

        if (request.CacheKey != null)
        {
            await _cache.RemoveAsync(request.CacheKey, cancellationToken);
            _logger.LogInformation("Removed Cache -> {CacheKey}", request.CacheKey);
        }

        return response;
    }
}
