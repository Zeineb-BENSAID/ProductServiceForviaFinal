using Microsoft.Extensions.Caching.Memory;
using ProductService.Domain.Interfaces;

namespace ProductService.Infrastructure.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached;

        var value = await factory();

        // ✅ Expiration automatique — pas de fuite mémoire
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Size = 1 // requis si SizeLimit configuré
        });

        return value;
    }

    public void Remove(string key) => _cache.Remove(key);
}