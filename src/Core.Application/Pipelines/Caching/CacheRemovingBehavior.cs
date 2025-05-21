using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

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

        if (request.CacheGroupKey?.Any() == true)
            foreach (var groupKey in request.CacheGroupKey)
            {
                byte[]? cachedGroup = await _cache.GetAsync(groupKey, cancellationToken);
                if (cachedGroup != null)
                {
                    var keysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.UTF8.GetString(cachedGroup));
                    if (keysInGroup != null)
                    {
                        foreach (var key in keysInGroup)
                        {
                            await _cache.RemoveAsync(key, cancellationToken);
                            _logger.LogInformation("Removed Cache -> {Key}", key);
                        }
                    }

                    await _cache.RemoveAsync(groupKey, cancellationToken);
                    _logger.LogInformation("Removed Cache Group -> {GroupKey}", groupKey);

                    await _cache.RemoveAsync($"{groupKey}SlidingExpiration", cancellationToken);
                    _logger.LogInformation("Removed Cache -> {Key}", $"{groupKey}SlidingExpiration");
                }
            }

        if (!string.IsNullOrWhiteSpace(request.CacheKey))
        {
            await _cache.RemoveAsync(request.CacheKey, cancellationToken);
            _logger.LogInformation("Removed Cache -> {Key}", request.CacheKey);
        }

        return response;
    }
}
