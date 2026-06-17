
## Lab 2 — Refactoring Performance du ProductService

### Étape 1 — Optimiser le Use Case avec ValueTask et caching correct

**Problème du Lab 1 :** le cache statique `Dictionary` qui grossit indéfiniment.

**Solution : `IMemoryCache` avec expiration**

```bash
dotnet add ProductService.Infrastructure package Microsoft.Extensions.Caching.Memory
```

**`ProductService.Infrastructure/Caching/ICacheService.cs`** *(nouvelle interface — Infrastructure)*

```csharp
namespace ProductService.Infrastructure.Caching;

/// <summary>
/// Abstraction du caching — définie dans Infrastructure car c'est un détail technique,
/// mais pourrait être déplacée dans Application si l'Application doit en dépendre.
/// </summary>
public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
    void Remove(string key);
}
```

**`ProductService.Infrastructure/Caching/MemoryCacheService.cs`**

```csharp
using Microsoft.Extensions.Caching.Memory;

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
```

**Utilisation dans le Service Application (avec `ValueTask`) :**

```csharp
public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public ProductService(IGenericRepository<Product> repository, IMapper mapper, ICacheService cache)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        // ✅ Cache avec expiration de 5 minutes — pas de fuite mémoire
        return await _cache.GetOrCreateAsync(
            key: $"product:{id}",
            factory: async () =>
            {
                var product = await _repository.GetByIdAsync(id);
                return product == null ? null : _mapper.Map<ProductDto>(product);
            },
            expiration: TimeSpan.FromMinutes(5));
    }
}
```

**Enregistrement dans `Program.cs` :**

```csharp
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // limite le nombre d'entrées — évite la croissance infinie
});
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
```

---

### Étape 2 — Optimiser le Repository avec Compiled Queries

**Qu'est-ce qu'une Compiled Query ?**

Normalement, EF Core doit **traduire** votre LINQ en SQL **à chaque appel**. Une "compiled query" mémorise cette traduction pour la réutiliser, évitant ce coût répété.

```csharp
using Microsoft.EntityFrameworkCore;

namespace ProductService.Infrastructure.Data;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly ProductDbContext _context;

    // ✅ Compiled Query : la traduction LINQ → SQL est faite UNE FOIS, réutilisée ensuite
    private static readonly Func<ProductDbContext, Guid, Task<Product?>> GetProductByIdCompiled =
        EF.CompileAsyncQuery((ProductDbContext context, Guid id) =>
            context.Products.AsNoTracking().FirstOrDefault(p => p.Id == id));

    public GenericRepository(ProductDbContext context) => _context = context;

    public async Task<T?> GetByIdAsync(Guid id)
    {
        if (typeof(T) == typeof(Product) && _context is ProductDbContext ctx)
        {
            return await GetProductByIdCompiled(ctx, id) as T;
        }

        return await _context.Set<T>().FindAsync(id);
    }
}
```

> **Quand utiliser les Compiled Queries ?** Pour les requêtes **exécutées très fréquemment** (ex : `GetById` appelé des milliers de fois/seconde). Pour les requêtes occasionnelles, le gain est négligeable et la complexité du code augmente.

---

### Étape 3 — Object Pooling dans l'Infrastructure (export CSV)

**Scénario :** Un endpoint génère un export CSV de tous les produits — opération exécutée régulièrement.

```csharp
using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace ProductService.Infrastructure.Export;

public class CsvExportService
{
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;
    private readonly ProductDbContext _context;

    public CsvExportService(ProductDbContext context)
    {
        _context = context;
        var provider = new DefaultObjectPoolProvider();
        _stringBuilderPool = provider.CreateStringBuilderPool();
    }

    public async Task<string> ExportProductsToCsvAsync()
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.AppendLine("Id,Name,Price,Stock");

            // ✅ AsNoTracking + streaming avec AsAsyncEnumerable pour gros volumes
            await foreach (var product in _context.Products.AsNoTracking().AsAsyncEnumerable())
            {
                sb.AppendLine($"{product.Id},{product.Name},{product.Price},{product.Stock}");
            }

            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }
}
```

> **Pourquoi `AsAsyncEnumerable()` ?** Au lieu de charger TOUS les produits en mémoire avec `ToListAsync()` (qui pourrait représenter des centaines de MB pour 1 million de produits), on **streame** ligne par ligne — la mémoire reste constante peu importe le volume.

---

### Étape 4 — Benchmark comparatif avant/après

```csharp
[MemoryDiagnoser]
public class RepositoryBenchmark
{
    private ProductDbContext _context;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase("BenchmarkDb")
            .Options;
        _context = new ProductDbContext(options);

        // Seed 10 000 produits
        for (int i = 0; i < 10_000; i++)
            _context.Products.Add(Product.Create($"Product {i}", i * 1.5m, "Desc"));
        _context.SaveChanges();
    }

    [Benchmark(Baseline = true)]
    public async Task<List<Product>> WithTracking()
    {
        return await _context.Products.Where(p => p.Price > 100).ToListAsync();
    }

    [Benchmark]
    public async Task<List<Product>> WithoutTracking()
    {
        return await _context.Products.AsNoTracking().Where(p => p.Price > 100).ToListAsync();
    }
}
```

**Résultat typique :**

| Méthode | Temps moyen | Mémoire allouée |
|---|---|---|
| `WithTracking` | 4.2 ms | 2.1 MB |
| `WithoutTracking` | 2.8 ms | 1.4 MB |

> **Conclusion :** `AsNoTracking()` réduit le temps de ~33% et la mémoire de ~33% sur les requêtes en lecture. **Règle d'or : tout `IQueryable` utilisé uniquement pour de la lecture doit utiliser `AsNoTracking()`.**

---