using Microsoft.Extensions.Caching.Distributed;
using ProductService.Domain.Interfaces;
using System.Text.Json;

namespace ProductService.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetOrCreateAsyncOLD<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        // Étape A : on regarde si la donnée existe déjà dans Redis
        var cachedValue = await _cache.GetStringAsync(key);

        if (cachedValue != null)
        {
            // Trouvé dans le cache : on la renvoie directement, sans toucher la base de données
            return JsonSerializer.Deserialize<T>(cachedValue);
        }

        // Étape B : pas trouvé — on calcule la vraie valeur (ex : requête base de données)
        var freshValue = await factory();

        // Étape C : on sauvegarde cette valeur dans Redis pour la prochaine fois
        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(freshValue),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });

        return freshValue;
    }
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        Console.WriteLine($"[CACHE] Recherche de la clé : {key}"); // ← ajout temporaire

        var cachedValue = await _cache.GetStringAsync(key);

        if (cachedValue != null)
        {
            Console.WriteLine($"[CACHE] TROUVÉ en cache : {key}"); // ← ajout temporaire
            return JsonSerializer.Deserialize<T>(cachedValue);
        }

        Console.WriteLine($"[CACHE] PAS trouvé, calcul de la valeur pour : {key}"); // ← ajout temporaire

        var freshValue = await factory();

        Console.WriteLine($"[CACHE] Sauvegarde dans Redis : {key}"); // ← ajout temporaire

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(freshValue),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });

        Console.WriteLine($"[CACHE] Sauvegarde TERMINÉE pour : {key}"); // ← ajout temporaire

        return freshValue;
    }
}