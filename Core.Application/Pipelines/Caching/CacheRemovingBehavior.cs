using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Core.Application.Pipelines.Caching;

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

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
            return await next();

        TResponse response = await next();

        if (request.CacheGroupKey != null)
        {
            byte[]? cachedGroup = await _cache.GetAsync(request.CacheGroupKey, cancellationToken);
            if (cachedGroup != null)
            {
                HashSet<string> keysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(cachedGroup))!;
                foreach (string key in keysInGroup)
                {
                    await _cache.RemoveAsync(key, cancellationToken);
                    _logger.LogInformation($"Removed Cache -> {key}");
                }

                await _cache.RemoveAsync(request.CacheGroupKey, cancellationToken);
                _logger.LogInformation($"Removed Cache -> {request.CacheGroupKey}");
                await _cache.RemoveAsync(key: $"{request.CacheGroupKey}SlidingExpiration", cancellationToken);
                _logger.LogInformation($"Removed Cache -> {request.CacheGroupKey}SlidingExpiration");
            }
        }

        if (request.CacheKey != null)
        {
            await _cache.RemoveAsync(request.CacheKey, cancellationToken);
            _logger.LogInformation($"Removed Cache -> {request.CacheKey}");
        }

        return response;
    }
}
