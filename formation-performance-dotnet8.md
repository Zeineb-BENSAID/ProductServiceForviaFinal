# Formation : Optimisation des Performances des Applications .NET
## Guide de Formation Complet — 3 Jours — Niveau Senior

> **Public cible :** Développeurs seniors (2-5 ans+) ayant suivi la formation Clean Architecture .NET 8  
> **Prérequis :** Maîtrise de la Clean Architecture, .NET 8, EF Core  
> **Durée :** 3 jours  
> **Projet fil rouge :** Optimisation de ProductService (issu de la formation Clean Architecture précédente)

---

## Table des matières

- [Vue d'ensemble](#vue-densemble)
- [JOUR 1 — Fondamentaux et Profiling](#jour-1--fondamentaux-de-loptimisation-et-outils-de-profiling)
  - [Chapitre 1.1 — Comprendre les performances en .NET Core](#chapitre-11--comprendre-les-performances-en-net-core)
  - [Chapitre 1.2 — Outils de Profiling et Diagnostic](#chapitre-12--outils-de-profiling-et-diagnostic)
  - [Lab 1 — Benchmarking et Profiling du projet fil rouge](#lab-1--benchmarking-et-profiling-du-projet-fil-rouge)
- [JOUR 2 — Code et Mémoire](#jour-2--optimisations-au-niveau-du-code-et-de-la-mémoire)
  - [Chapitre 2.1 — Optimisations du Code et Patterns Performants](#chapitre-21--optimisations-du-code-et-patterns-performants)
  - [Chapitre 2.2 — Gestion de la Mémoire et Garbage Collector](#chapitre-22--gestion-de-la-mémoire-et-garbage-collector)
  - [Lab 2 — Refactoring Performance du ProductService](#lab-2--refactoring-performance-du-productservice)
- [JOUR 3 — Avancé, Scalabilité, Production](#jour-3--optimisations-avancées-scalabilité-et-intégration)
  - [Chapitre 3.1 — I/O Asynchrones et Caching](#chapitre-31--io-asynchrones-et-caching)
  - [Chapitre 3.2 — Scalabilité et Monitoring](#chapitre-32--scalabilité-et-monitoring)
  - [Lab 3 — Caching, Load Testing et Monitoring](#lab-3--caching-load-testing-et-monitoring)
- [Ressources et références](#ressources-et-références)

---

## Vue d'ensemble

### Objectifs de la formation

| # | Objectif |
|---|----------|
| 1 | Identifier et mesurer les problèmes de performance dans une application Clean Architecture |
| 2 | Appliquer des optimisations au niveau du code, de la mémoire, des I/O et des services externes |
| 3 | Intégrer des outils de profiling et de monitoring pour des scénarios réels |
| 4 | Optimiser sans compromettre les principes de la Clean Architecture |

### Le fil conducteur de cette formation

> **La question centrale des 3 jours :** *"Comment optimiser sans casser l'architecture qu'on vient de construire ?"*

C'est la tension permanente que nous allons explorer. La Clean Architecture introduit des abstractions (interfaces, DTOs, mapping) qui ont un coût en performance. Mais ce coût est presque toujours **négligeable** face aux gains en maintenabilité — sauf si on l'ignore complètement.

**Notre approche :** mesurer avant d'optimiser, comprendre où optimiser, et optimiser **dans** chaque couche sans violer la règle de dépendance.

---

# JOUR 1 — Fondamentaux de l'Optimisation et Outils de Profiling

---

## Chapitre 1.1 — Comprendre les performances en .NET Core

### 🎯 Objectif
Comprendre les métriques de performance, le coût réel des abstractions de la Clean Architecture, et savoir utiliser BenchmarkDotNet.

---

### Pourquoi parler de performance APRÈS la Clean Architecture ?

Il y a une croyance répandue : *"Clean Architecture = plus lent, car plus de couches, plus d'abstractions, plus de mapping."*

C'est **partiellement vrai**, mais voici la nuance essentielle :

```
Coût d'une abstraction = quelques microsecondes (interface, DI, mapping)
Coût d'un mauvais algorithme = peut être 1000x à 1 000 000x plus lent
```

**Exemple concret :**

| Opération | Temps approximatif |
|---|---|
| Appel via interface (`IProductService`) vs appel direct | ~0.000001 ms (négligeable) |
| `AutoMapper.Map()` vs mapping manuel | ~0.001 ms (négligeable) |
| Requête SQL avec `N+1 queries` au lieu d'un `JOIN` | Peut passer de 5ms à 5000ms |
| Boucle avec `string +=` au lieu de `StringBuilder` | Peut passer de O(n) à O(n²) |

**Conclusion :** Le problème n'est presque jamais l'architecture elle-même. Le problème est **où et comment** on écrit le code à l'intérieur de chaque couche.

> 💡 **Règle d'or de cette formation :** On garde la Clean Architecture intacte. On optimise *l'intérieur* de chaque couche, jamais en cassant les frontières entre elles.

---

### Les 4 métriques de performance essentielles

Avant d'optimiser quoi que ce soit, il faut savoir **mesurer**. Voici le vocabulaire indispensable :

#### 1. Latence (Latency)
**Définition :** Le temps écoulé entre l'envoi d'une requête et la réception de la réponse, pour **une seule requête**.

```
Client → [────── 250ms ──────] → Réponse
```

> Une latence de 250ms signifie qu'un utilisateur attend 250ms pour voir le résultat.

#### 2. Débit (Throughput)
**Définition :** Le nombre de requêtes traitées **par unité de temps** (souvent par seconde).

```
1000 requêtes / seconde = throughput de 1000 req/s
```

> ⚠️ **Piège classique :** Une latence faible ne garantit pas un bon throughput. Un serveur peut traiter une requête en 10ms mais ne supporter que 50 requêtes simultanées avant de saturer.

#### 3. Utilisation CPU et Mémoire
**Définition :** La quantité de ressources processeur et RAM consommées par l'application.

- **CPU élevé** → souvent causé par des calculs inefficaces, boucles, sérialisation excessive
- **Mémoire élevée** → souvent causé par des allocations excessives, fuites mémoire, objets non libérés

#### 4. Scalabilité
**Définition :** La capacité de l'application à maintenir ses performances quand la charge augmente.

```
Scalabilité verticale   : ajouter plus de CPU/RAM à un seul serveur
Scalabilité horizontale : ajouter plus d'instances (microservices, Kubernetes)
```

> La Clean Architecture facilite la scalabilité horizontale car chaque couche est stateless et remplaçable.

---

### Le tableau de bord mental d'un développeur senior

Quand vous regardez une application, posez-vous toujours ces 4 questions :

| Question | Métrique | Outil |
|---|---|---|
| "Est-ce rapide pour UN utilisateur ?" | Latence | BenchmarkDotNet, Stopwatch |
| "Combien d'utilisateurs simultanés supporte-t-on ?" | Throughput | Apache JMeter, k6 |
| "Où va le CPU et la RAM ?" | CPU/Mémoire | dotnet-trace, dotnet-counters |
| "Que se passe-t-il sous forte charge ?" | Scalabilité | Kubernetes HPA, load testing |

---

### BenchmarkDotNet : mesurer avec précision

**Pourquoi pas juste un `Stopwatch` ?**

```csharp
// ❌ Approche naïve avec Stopwatch
var sw = Stopwatch.StartNew();
DoSomething();
sw.Stop();
Console.WriteLine(sw.ElapsedMilliseconds);
```

**Problèmes de cette approche :**
- Le JIT (Just-In-Time compiler) n'a pas eu le temps de "warm up" (optimiser le code)
- Le Garbage Collector peut se déclencher pendant la mesure et fausser les résultats
- Une seule mesure n'est pas statistiquement fiable

**BenchmarkDotNet** résout tous ces problèmes automatiquement :
- Exécute le code des milliers de fois
- Gère le warm-up (échauffement JIT)
- Calcule moyenne, écart-type, et alloue la mémoire précisément

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser] // Affiche aussi les allocations mémoire
public class StringConcatenationBenchmark
{
    private readonly string[] _items = Enumerable.Range(1, 100)
        .Select(i => $"Item{i}")
        .ToArray();

    [Benchmark(Baseline = true)]
    public string StringConcat()
    {
        var result = string.Empty;
        foreach (var item in _items)
            result += item; // ❌ Crée une nouvelle string à chaque itération
        return result;
    }

    [Benchmark]
    public string StringBuilderConcat()
    {
        var sb = new StringBuilder();
        foreach (var item in _items)
            sb.Append(item); // ✅ Réutilise le buffer interne
        return sb.ToString();
    }
}

// Program.cs
BenchmarkRunner.Run<StringConcatenationBenchmark>();
```

**Résultat typique :**

| Méthode | Temps moyen | Mémoire allouée |
|---|---|---|
| `StringConcat` | 45.2 μs | 52,400 B |
| `StringBuilderConcat` | 2.1 μs | 2,048 B |

> **Interprétation :** `StringBuilder` est ~20x plus rapide et alloue ~25x moins de mémoire pour ce cas. Sur 100 itérations, l'effet est visible — imaginez sur 10 millions d'itérations.

---

### 🎯 Exercice théorique : Identifier les bottlenecks

Regardez ce Use Case extrait du ProductService (Clean Architecture). Identifiez les problèmes de performance potentiels :

```csharp
public async Task<IEnumerable<ProductDto>> GetAllWithCategoryNamesAsync()
{
    var products = await _repository.GetAllAsync(1, 1000); // récupère TOUT
    var result = new List<ProductDto>();

    foreach (var product in products)
    {
        // ❌ Requête dans une boucle = N+1 queries
        var category = await _categoryRepository.GetByIdAsync(product.CategoryId);

        result.Add(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            CategoryName = category?.Name ?? "Unknown"
        });
    }

    return result;
}
```

**Problèmes identifiés :**
1. **`GetAllAsync(1, 1000)`** : récupère jusqu'à 1000 produits même si l'utilisateur n'en a besoin que de 10 → gaspillage de mémoire et de bande passante
2. **N+1 queries** : si on a 1000 produits, on fait 1000 requêtes SQL séparées pour récupérer les catégories → devrait être un `JOIN` ou un `Include()`
3. **Pas de `AsNoTracking()`** : EF Core garde un suivi inutile de ces entités en lecture seule
4. **Construction manuelle du DTO dans une boucle** : peut être remplacé par `Select()` + AutoMapper en une seule passe

---

## Chapitre 1.2 — Outils de Profiling et Diagnostic

### 🎯 Objectif
Savoir utiliser les outils .NET pour diagnostiquer CPU, mémoire et comportements asynchrones en production.

---

### Qu'est-ce que le Profiling ?

**Le profiling**, c'est l'art d'observer une application **pendant son exécution** pour comprendre :
- Où le CPU passe son temps (quelle méthode consomme le plus ?)
- Où la mémoire est allouée (quels objets s'accumulent ?)
- Comment les tâches asynchrones s'enchaînent (y a-t-il des blocages ?)

> **Analogie :** Le profiling, c'est mettre un GPS sur chaque ligne de code pour savoir combien de temps elle prend et combien de "carburant" (mémoire) elle consomme.

---

### Les outils du SDK .NET

.NET fournit une suite d'outils en ligne de commande, installables via :

```bash
dotnet tool install --global dotnet-trace
dotnet tool install --global dotnet-dump
dotnet tool install --global dotnet-counters
```

#### `dotnet-counters` — Le tableau de bord en temps réel

**Rôle :** Affiche en direct les métriques CPU, mémoire, GC, threads d'une application qui tourne.

```bash
# Lister les process .NET en cours
dotnet-counters ps

# Surveiller un process en temps réel
dotnet-counters monitor --process-id <PID>
```

**Ce que vous voyez :**
```
[System.Runtime]
    CPU Usage (%)                    45
    GC Heap Size (MB)                128
    Gen 0 GC Count                   12
    Gen 1 GC Count                   3
    Gen 2 GC Count                   0
    ThreadPool Thread Count          8
```

> **Quand l'utiliser ?** En premier réflexe, pour avoir une vue d'ensemble avant de creuser plus profondément.

---

#### `dotnet-trace` — Le profiling CPU détaillé

**Rôle :** Enregistre une trace d'exécution complète (quelle méthode appelle quelle méthode, et combien de temps chacune prend).

```bash
# Démarrer une trace
dotnet-trace collect --process-id <PID>

# Arrêter avec Ctrl+C, génère un fichier .nettrace
# Ouvrir avec Visual Studio ou PerfView pour analyser
```

**Ce que vous obtenez :** Un "flame graph" (graphique en flammes) montrant visuellement quelle méthode consomme le plus de temps CPU.

```
Program.Main                          [████████████████████] 100%
└─ ProductController.GetAll           [████████████████░░░░]  82%
   └─ ProductService.GetAllAsync      [██████████████░░░░░░]  70%
      └─ Repository.GetAllAsync       [████████████░░░░░░░░]  60%  ← bottleneck ici
      └─ AutoMapper.Map               [██░░░░░░░░░░░░░░░░░░]  10%
```

> **Lecture :** 60% du temps total est passé dans `Repository.GetAllAsync`. C'est notre cible d'optimisation prioritaire.

---

#### `dotnet-dump` — L'analyse mémoire post-mortem

**Rôle :** Capture un "snapshot" complet de la mémoire d'un process (un "dump"), pour analyser après coup (utile pour les fuites mémoire en production).

```bash
# Capturer un dump
dotnet-dump collect --process-id <PID>

# Analyser le dump
dotnet-dump analyze core_20240115_103000

# Dans le shell interactif :
> dumpheap -stat        # statistiques des objets en mémoire
> gcroot <address>       # trouver pourquoi un objet n'est pas libéré
```

**Cas d'usage typique :** *"L'application consomme 2GB après 1h, alors qu'elle ne devrait en consommer que 200MB. Pourquoi ?"*

```
Statistics:
      MT    Count    TotalSize Class Name
7ff8a1b2  450,000   21,600,000 ProductDto
7ff8a1c3  450,000   14,400,000 System.String
```

> **Interprétation :** 450 000 instances de `ProductDto` en mémoire — ça ressemble à une fuite (des objets jamais libérés par le GC, souvent via un event handler ou une collection statique qui grossit indéfiniment).

---

### Application Insights : le monitoring en production

**Pourquoi pas juste les outils CLI ?**

Les outils `dotnet-*` sont parfaits en développement ou en debug ponctuel. Mais en **production**, vous avez besoin de :
- Voir les tendances sur plusieurs jours/semaines
- Être alerté automatiquement en cas de dégradation
- Corréler les performances avec les déploiements

**Application Insights** (Azure) capture automatiquement :

| Donnée capturée | Exemple |
|---|---|
| Temps de réponse par endpoint | `GET /api/products` → 120ms moyen |
| Dépendances externes | Appel SQL Server → 45ms |
| Exceptions | `NotEnoughStockException` → 12 occurrences/jour |
| Requêtes lentes | `GET /api/products?filter=x` → 2.3s (alerte) |

```csharp
// Program.cs — Installation minimale
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);
```

> Avec cette seule ligne, chaque requête HTTP, chaque appel SQL, et chaque exception sont automatiquement trackés et visualisables dans le portail Azure.

---

## Lab 1 — Benchmarking et Profiling du projet fil rouge

### 🎯 Objectif du lab
Mettre en place BenchmarkDotNet sur le ProductService, profiler une opération, et identifier un bottleneck réel.

---

### Étape 1 — Installer BenchmarkDotNet

```bash
# Créer un projet de benchmarks séparé
dotnet new console -n ProductService.Benchmarks
cd ProductService.Benchmarks

dotnet add package BenchmarkDotNet
dotnet add reference ../ProductService.Application/ProductService.Application.csproj
dotnet add reference ../ProductService.Domain/ProductService.Domain.csproj
```

> **Pourquoi un projet séparé ?** Les benchmarks doivent tourner en mode `Release` avec des optimisations activées, et ne doivent pas polluer le code de production.

---

### Étape 2 — Benchmarker le mapping AutoMapper vs manuel

**`ProductService.Benchmarks/MappingBenchmark.cs`**

```csharp
using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProductService.Application.DTOs;
using ProductService.Application.Profiles;
using ProductService.Domain.Entities;

namespace ProductService.Benchmarks;

[MemoryDiagnoser]
public class MappingBenchmark
{
    private readonly IMapper _mapper;
    private readonly Product _product;

    public MappingBenchmark()
    {
        // Configuration AutoMapper (comme dans Program.cs)
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _product = Product.Create("Test Product", 29.99m, "A sample description", 100);
    }

    [Benchmark(Baseline = true)]
    public ProductDto ManualMapping()
    {
        // Mapping manuel : chaque propriété assignée explicitement
        return new ProductDto
        {
            Id = _product.Id,
            Name = _product.Name,
            Price = _product.Price,
            Description = _product.Description,
            Stock = _product.Stock
        };
    }

    [Benchmark]
    public ProductDto AutoMapperMapping()
    {
        return _mapper.Map<ProductDto>(_product);
    }
}

// Program.cs du projet Benchmarks
class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<MappingBenchmark>();
    }
}
```

**Exécuter :**

```bash
dotnet run -c Release
```

**Résultat attendu (ordre de grandeur) :**

| Méthode | Temps moyen | Mémoire allouée | Ratio |
|---|---|---|---|
| `ManualMapping` | 8 ns | 56 B | 1.00x (baseline) |
| `AutoMapperMapping` | 95 ns | 56 B | ~12x plus lent |

> **Analyse :** AutoMapper est ~12x plus lent qu'un mapping manuel, mais en valeur absolue, **95 nanosecondes reste négligeable** face au temps d'une requête HTTP (généralement plusieurs millisecondes, soit 10 000x plus lent que la différence observée).
>
> **Conclusion pédagogique :** Ne remplacez PAS AutoMapper par du mapping manuel partout "pour la performance" — sauf si vous mappez des **millions d'objets** dans une boucle critique (ex : traitement batch).

---

### Étape 3 — Profiler une requête API avec dotnet-trace

```bash
# 1. Lancer l'API
cd ProductService.API
dotnet run

# 2. Dans un autre terminal, trouver le PID
dotnet-counters ps

# 3. Démarrer la trace
dotnet-trace collect --process-id <PID> --output trace.nettrace

# 4. Générer du trafic (dans un 3ème terminal)
1..100 | ForEach-Object {
    Invoke-RestMethod https://localhost:7066/api/v1/Product
}

# 5. Arrêter la trace avec Ctrl+C
```

**Ouvrir `trace.nettrace` dans Visual Studio :**
- `Fichier → Ouvrir → trace.nettrace`
- Onglet **CPU Usage** → voir le flame graph
- Identifier les méthodes qui prennent le plus de temps cumulé

---

### Étape 4 — Détecter une fuite mémoire (démo guidée)

**Scénario :** Un développeur a ajouté un cache "naïf" qui grossit indéfiniment :

```csharp
// ❌ Code avec fuite mémoire volontaire (pour la démo)
public class ProductService : IProductService
{
    // Cache statique qui n'est JAMAIS vidé
    private static readonly Dictionary<Guid, ProductDTO> _cache = new();

    public async Task<ProductDTO?> GetByIdAsync(Guid id)
    {
        if (_cache.TryGetValue(id, out var cached))
            return cached;

        var product = await _repository.GetByIdAsync(id);
        if (product == null) return null;

        var dto = _mapper.Map<ProductDTO>(product);
        _cache[id] = dto; // ❌ Ajouté mais jamais retiré → grossit indéfiniment
        return dto;
    }
}
```

**Détection avec dotnet-dump :**

```bash
dotnet-dump collect --process-id <PID>
dotnet-dump analyze <dump_file>

> dumpheap -stat -type ProductDTO
```

**Résultat après simulation de charge :**
```
MT       Count    TotalSize Class Name
7ff8a1b2 500,000  28,000,000 ProductService.Application.DTOs.ProductDTO
```

> 500 000 instances de `ProductDTO` jamais libérées = fuite mémoire confirmée. La solution sera abordée au Jour 2 avec les patterns de caching corrects (`IMemoryCache` avec expiration).

---

# JOUR 2 — Optimisations au Niveau du Code et de la Mémoire

---

## Chapitre 2.1 — Optimisations du Code et Patterns Performants

### Objectif
Maîtriser les techniques d'optimisation de boucles, LINQ, collections et async/await — sans casser la Clean Architecture.

---

### `Span<T>` : éviter les allocations inutiles

**Le problème avec les strings et tableaux classiques :**

Chaque fois que vous faites `string.Substring()` ou que vous découpez un tableau, .NET **alloue une nouvelle zone mémoire** et copie les données.

```csharp
// ❌ Chaque Substring alloue une nouvelle string en mémoire
string input = "PRODUCT-12345-ELECTRONICS";
string category = input.Substring(15);     // nouvelle allocation
string id = input.Substring(8, 5);          // nouvelle allocation
```

**`Span<T>`** est une "fenêtre" sur une mémoire existante, **sans copie** :

```csharp
// ✅ Span<T> : aucune allocation, juste une "vue" sur la mémoire existante
ReadOnlySpan<char> input = "PRODUCT-12345-ELECTRONICS";
ReadOnlySpan<char> category = input.Slice(15);     // pas d'allocation
ReadOnlySpan<char> id = input.Slice(8, 5);          // pas d'allocation
```

> **Analogie :** `Substring` photocopie une page d'un livre. `Span<T>` met juste un marque-page — le livre original reste intact, mais on peut lire la partie qui nous intéresse sans copier.

**Quand utiliser `Span<T>` ?**
- Parsing de chaînes (logs, CSV, protocoles réseau)
- Traitement de buffers (lecture de fichiers, sockets)
- Boucles critiques exécutées des millions de fois

**Quand NE PAS s'en préoccuper ?**
- Code applicatif classique (controllers, services) où le volume est faible
- Si le code devient illisible pour un gain marginal

---

### `ArrayPool<T>` : réutiliser au lieu d'allouer

**Le problème :** Allouer un tableau (`new byte[1024]`) à chaque appel de méthode crée de la pression sur le Garbage Collector.

**La solution :** `ArrayPool<T>` maintient un "pool" de tableaux réutilisables.

```csharp
// ❌ Alloue un nouveau tableau à chaque appel
public byte[] ProcessData(int size)
{
    var buffer = new byte[size]; // nouvelle allocation chaque fois
    // ... traitement
    return buffer;
}

// ✅ Réutilise un tableau du pool
public void ProcessData(int size)
{
    var buffer = ArrayPool<byte>.Shared.Rent(size); // emprunte un tableau existant
    try
    {
        // ... traitement
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer); // rend le tableau au pool
    }
}
```


> Performance maximale : ne pas nettoyer et réécrire les données utiles.
Sécurité : nettoyer le tableau (clearArray: true) avant de le rendre, ou l'effacer manuellement si le buffer a contenu des données sensibles.

**La règle d'or**

Lorsque tu utilises ArrayPool<T> :

considère le buffer comme ayant un contenu indéterminé au moment du Rent();
écris toutes les cases que tu vas utiliser avant de les lire ;
ne lis jamais au-delà de la quantité de données réellement écrites.

C'est cette discipline qui permet d'obtenir les gains de performance d'ArrayPool<T> tout en évitant les bugs liés aux données résiduelles.

> **Cas d'usage typique dans une Clean Architecture :** dans la couche **Infrastructure**, lors du traitement de fichiers volumineux ou de flux réseau (ex : export CSV, traitement d'images).

---

### LINQ : performant ou pas ?

LINQ est élégant, mais certaines méthodes ont des coûts cachés :

| Méthode LINQ | Coût caché | Alternative |
|---|---|---|
| `.Count()` sur `IEnumerable` non matérialisé | Peut re-énumérer toute la collection | `.Count` (propriété) si `List<T>` |
| `.Where().First()` | Évalue le `Where` en streaming — OK | — |
| `.ToList()` appelé plusieurs fois | Recrée la liste à chaque fois | Matérialiser une seule fois |
| `.OrderBy()` sur de gros volumes | Tri O(n log n) en mémoire | Trier en base de données (SQL `ORDER BY`) |

**Exemple concret — piège classique :**

```csharp
// ❌ products est un IQueryable EF Core — chaque accès = nouvelle requête SQL !
var products = _context.Products.Where(p => p.Price > 100);

var count = products.Count();        // requête SQL #1 : SELECT COUNT(*)
var list = products.ToList();        // requête SQL #2 : SELECT * (re-exécute le WHERE)
var first = products.FirstOrDefault(); // requête SQL #3 : SELECT TOP 1

// ✅ Matérialiser UNE SEULE FOIS
var productsList = await _context.Products.Where(p => p.Price > 100).ToListAsync(); // 1 requête SQL

var count = productsList.Count;       // en mémoire, pas de SQL
var first = productsList.FirstOrDefault(); // en mémoire, pas de SQL
```

> **Règle d'or :** Avec EF Core, matérialisez (`ToListAsync()`, `ToArrayAsync()`) **une seule fois**, le plus tard possible, après avoir appliqué tous les filtres/tris en `IQueryable`.

---

### Async/Await avancé : éviter les deadlocks et le context switching

#### Le piège du `.Result` et `.Wait()`

```csharp
// ❌ DANGER : peut causer un deadlock dans ASP.NET Core
public IActionResult GetProduct(Guid id)
{
    var product = _service.GetByIdAsync(id).Result; // ⚠️ bloque le thread
    return Ok(product);
}
```

**Pourquoi c'est dangereux ?**

Dans certains contextes (anciens frameworks UI, certains middlewares), `.Result` bloque le thread appelant **et** le thread sur lequel la tâche async doit continuer — créant un blocage circulaire (deadlock).

```csharp
// ✅ TOUJOURS async de bout en bout
public async Task<IActionResult> GetProduct(Guid id)
{
    var product = await _service.GetByIdAsync(id);
    return Ok(product);
}
```

> **Règle d'or :** *"async all the way"* — si une méthode appelle une méthode async, elle doit elle-même être async, jusqu'au point d'entrée (Controller).

---

#### `ConfigureAwait(false)` : utile ou pas en ASP.NET Core ?

```csharp
// Dans les anciennes applications (ASP.NET classique, WPF, WinForms)
await _repository.GetByIdAsync(id).ConfigureAwait(false);
```

**Ce que ça fait :** Indique que la continuation (le code après `await`) n'a pas besoin de revenir sur le thread/contexte d'origine.

> **Dans ASP.NET Core moderne (.NET 8) :** Il n'y a **plus de `SynchronizationContext`** par défaut, donc `ConfigureAwait(false)` n'apporte **aucun bénéfice** dans les Controllers, Services, Repositories de votre API. Vous pouvez l'omettre dans le code applicatif .NET 8.
>
> Il reste utile dans les **bibliothèques génériques** (NuGet packages) qui peuvent être utilisées dans des contextes variés (UI, etc.).

---

#### `ValueTask<T>` : quand l'utiliser ?

**`Task<T>`** est une classe (allocation sur le heap). **`ValueTask<T>`** est une struct (peut éviter l'allocation si le résultat est déjà disponible).

>Stack (pile) : mémoire très rapide, utilisée pour les types valeur (int, bool, struct, ValueTask<T>...). Les données sont automatiquement libérées à la fin de la méthode. Pas d'intervention du Garbage Collector (GC).
>Heap (tas) : mémoire utilisée pour les types référence (class, string, Task<T>, objets...). Les objets restent en mémoire tant qu'ils sont référencés, puis sont nettoyés par le Garbage Collector, ce qui entraîne un coût supplémentaire.

```csharp int x = 10;                 // Stocké sur le Stack
Task<int> task = Task.FromResult(42); // Objet Task stocké sur le Heap
ValueTask<int> valueTask = ValueTask.FromResult(42); // Struct stockée sur le Stack
```
>**Pourquoi ValueTask<T> peut être plus performant ?**
Task<T> est une classe → allocation sur le Heap → plus de travail pour le GC.
ValueTask<T> est une struct → stockée sur le Stack (si le résultat est immédiatement disponible) → peut éviter une allocation sur le Heap et réduire la pression sur le GC.

En résumé :

Stack = rapide, temporaire, pas de GC.
Heap = plus flexible, mais allocations et nettoyage par le GC.

```csharp
// Le cas typique : un cache qui retourne parfois immédiatement, parfois après un await
public async ValueTask<ProductDto?> GetByIdAsync(Guid id)
{
    if (_cache.TryGetValue(id, out var cached))
        return cached; // ✅ retour synchrone, ValueTask évite l'allocation Task

    var product = await _repository.GetByIdAsync(id); // cas asynchrone réel
    var dto = _mapper.Map<ProductDto>(product);
    _cache.Set(id, dto);
    return dto;
}
```

> **Règle d'or :** Utilisez `ValueTask<T>` uniquement quand la méthode retourne **fréquemment de manière synchrone** (cache hit, validation rapide). Sinon, `Task<T>` reste le standard — `ValueTask` mal utilisé peut introduire des bugs subtils (ne peut être attendu qu'une fois).

---

### Optimiser les Use Cases et Repositories sans casser la Clean Architecture

**La question piège :** *"Si je dois optimiser le Repository, est-ce que je viole la Clean Architecture ?"*

**Réponse : NON**, tant que :
1. L'**interface** (`IGenericRepository<T>`) dans le Domain ne change pas
2. L'**implémentation** dans Infrastructure peut être aussi optimisée que nécessaire
3. Les Use Cases (Application) continuent à dépendre de l'interface, pas de l'implémentation

```
┌─────────────────────────────────────────┐
│ Domain : IGenericRepository<T>           │  ← INCHANGÉ
│   Task<T?> GetByIdAsync(Guid id);        │
└─────────────────────────────────────────┘
              ▲
              │ implémente
┌─────────────────────────────────────────┐
│ Infrastructure : GenericRepository<T>    │  ← OPTIMISABLE LIBREMENT
│   - Compiled queries                     │
│   - AsNoTracking()                       │
│   - Caching interne                      │
│   - Span<T>, pooling...                  │
└─────────────────────────────────────────┘
```

> **C'est exactement le principe d'inversion de dépendances qui rend cette optimisation possible** : l'Application ne sait même pas QUE le Repository a été optimisé.

---

## Chapitre 2.2 — Gestion de la Mémoire et Garbage Collector

### Objectif
Comprendre le fonctionnement du Garbage Collector .NET, les générations, et les techniques pour réduire la pression mémoire.

---

### Comment fonctionne le Garbage Collector (GC) ?

**Le principe de base :** .NET alloue automatiquement la mémoire pour vos objets, et le GC la libère automatiquement quand ils ne sont plus utilisés. Vous n'appelez jamais `free()` comme en C.

**Mais "automatique" a un coût :** le GC doit régulièrement **mettre en pause** l'application pour scanner la mémoire et identifier les objets à libérer.

---

### Les générations du GC : Gen 0, Gen 1, Gen 2

.NET divise le tas mémoire (heap) en **générations**, basé sur une observation statistique : *"la plupart des objets ont une vie très courte"*.

```
┌──────────────────────────────────────────────────┐
│  Gen 0  │  Objets récents (variables locales,     │  ← collecté très souvent
│         │  objets temporaires)                    │     (rapide, peu coûteux)
├──────────────────────────────────────────────────┤
│  Gen 1  │  Objets ayant survécu à 1 collection    │  ← collecté moins souvent
│         │  Gen 0 (buffer entre Gen0 et Gen2)      │
├──────────────────────────────────────────────────┤
│  Gen 2  │  Objets de longue durée (singletons,    │  ← collecté rarement
│         │  caches, configuration)                 │     (lent, coûteux)
└──────────────────────────────────────────────────┘
```

**Exemple concret avec ProductService :**

| Objet | Génération typique | Pourquoi |
|---|---|---|
| `ProductDto` créé dans une requête HTTP | Gen 0 | Vit le temps de la requête, puis libéré |
| `string` temporaire dans une boucle | Gen 0 | Très courte durée de vie |
| `IMemoryCache` (singleton) | Gen 2 | Vit toute la durée de l'application |
| `DbContext` (scoped) | Gen 0 → Gen 1 | Vit le temps de la requête, parfois promu si la requête est longue |

> **Implication pratique :** Si votre code crée énormément de petits objets temporaires (Gen 0), le GC travaille plus souvent — mais ces collections sont rapides. Le vrai problème survient quand des objets **survivent trop longtemps** et finissent en Gen 2 inutilement (ex : la fuite mémoire du Lab 1).

---

### LOH (Large Object Heap) : le cas spécial des gros objets

**Règle :** Tout objet de **85 000 octets ou plus** est alloué directement dans le **Large Object Heap (LOH)**, qui est géré différemment (collecté avec Gen 2, et historiquement non compacté).

```csharp
// ❌ Ce tableau de bytes ira directement dans le LOH
byte[] buffer = new byte[100_000]; // > 85000 bytes → LOH

// Dans une Clean Architecture, attention aux DTOs avec de grosses collections
public class ProductExportDto
{
    public List<ProductDto> Products { get; set; } // si > ~3000 produits → LOH
}
```

> **Impact :** Le LOH peut causer de la **fragmentation mémoire** — des "trous" inutilisables entre les objets. Pour les exports/imports volumineux, privilégiez le **streaming** (traiter par lots) plutôt que de charger tout en mémoire.

---

### Structs vs Classes : quand préférer une struct ?

| Critère | `class` (référence) | `struct` (valeur) |
|---|---|---|
| Stockage | Heap (tas) | Stack (pile) si possible |
| Allocation GC | Oui | Non (si sur la pile) |
| Copie | Référence copiée (rapide) | Toute la donnée copiée |
| Taille recommandée | Toute taille | < 16 bytes idéalement |
| Mutabilité | Mutable ou immutable | **Toujours préférer immutable** |

**Exemple — un Value Object dans le Domain :**

```csharp
// ❌ Class : alloué sur le heap, surveillé par le GC
public class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}

// ✅ Struct : alloué sur la pile (si utilisé localement), pas de pression GC
public readonly struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new Money(a.Amount + b.Amount, a.Currency);
    }
}
```

> **Dans la Clean Architecture :** les **Value Objects** du Domain (Money, Address, Email...) sont d'excellents candidats pour des `readonly struct` — ils sont immutables par nature et souvent petits.

---

### Object Pooling : réutiliser au lieu de recréer

Nous avons vu `ArrayPool<T>` pour les tableaux. **`ObjectPool<T>`** généralise ce concept à n'importe quel objet coûteux à créer.

```csharp
// Installation : Microsoft.Extensions.ObjectPool

// Définir une politique de création/réinitialisation
public class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
{
    public override StringBuilder Create() => new StringBuilder(256);

    public override bool Return(StringBuilder obj)
    {
        obj.Clear(); // réinitialiser avant de remettre dans le pool
        return true;
    }
}

// Utilisation
var pool = new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());

var sb = pool.Get();
try
{
    sb.Append("Building a report...");
    // ... utilisation
}
finally
{
    pool.Return(sb);
}
```

> **Cas d'usage Infrastructure :** génération de rapports, sérialisation custom, construction de requêtes SQL dynamiques répétées.

---

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

# JOUR 3 — Optimisations Avancées, Scalabilité et Intégration

---

## Chapitre 3.1 — I/O Asynchrones et Caching

### Objectif
Optimiser les appels HTTP externes, comprendre le Distributed Caching avec Redis, et l'Output Caching ASP.NET Core.

---

### Le problème du HttpClient mal utilisé

**L'erreur classique :**

```csharp
// ❌ Crée une nouvelle connexion TCP à chaque appel — épuisement des sockets (Socket Exhaustion)
public async Task<InventoryDto> CheckInventoryAsync(Guid productId)
{
    using var client = new HttpClient(); // ❌ instancié à chaque appel
    var response = await client.GetAsync($"https://inventory-service/api/inventory/{productId}");
    return await response.Content.ReadFromJsonAsync<InventoryDto>();
}
```

**Pourquoi c'est un problème ?**

Chaque `new HttpClient()` ouvre potentiellement une nouvelle connexion TCP. Sous charge, le système d'exploitation peut **épuiser le nombre de sockets disponibles**, causant des erreurs `SocketException`.

---

### La solution : `IHttpClientFactory` avec pooling

```csharp
// Program.cs — Enregistrement avec pooling automatique
builder.Services.AddHttpClient("InventoryService", client =>
{
    client.BaseAddress = new Uri("https://inventory-service/");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5)); // recycle les connexions périodiquement
```

```csharp
// Dans l'Infrastructure : injection via IHttpClientFactory
public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;

    // ✅ HttpClient géré par le factory — connexions poolées et réutilisées
    public InventoryServiceClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("InventoryService");
    }

    public async Task<InventoryDto?> CheckInventoryAsync(Guid productId)
    {
        var response = await _httpClient.GetAsync($"api/inventory/{productId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryDto>();
    }
}
```

> **Dans la Clean Architecture :** `IInventoryServiceClient` est l'interface définie dans **Application** (ou Domain), `InventoryServiceClient` est l'implémentation dans **Infrastructure**. Le Use Case dépend uniquement de l'interface.

---

### gRPC : alternative performante à REST entre microservices

**Pourquoi gRPC pour la communication inter-services ?**

| Critère | REST/JSON | gRPC |
|---|---|---|
| Format | JSON texte (verbeux) | Protocol Buffers (binaire, compact) |
| Performance | Standard | ~5-10x plus rapide en sérialisation |
| Contrat | OpenAPI (optionnel) | `.proto` (obligatoire, fortement typé) |
| Streaming | Limité | Natif (bidirectionnel) |
| Cas d'usage | API publiques, navigateurs | Communication interne microservices |

```protobuf
// inventory.proto
syntax = "proto3";

service InventoryService {
  rpc CheckStock (StockRequest) returns (StockResponse);
}

message StockRequest {
  string product_id = 1;
}

message StockResponse {
  int32 quantity = 1;
  bool available = 2;
}
```

> **Recommandation :** Gardez **REST pour les APIs publiques** (versioning, documentation Swagger, compatibilité navigateurs) et envisagez **gRPC pour la communication interne** entre microservices à fort volume (ex : ProductService ↔ InventoryService).

---

### Distributed Caching avec Redis

**`IMemoryCache` vs Redis — quelle différence ?**

| | `IMemoryCache` | Redis (Distributed Cache) |
|---|---|---|
| Emplacement | RAM du process .NET | Serveur séparé (réseau) |
| Partagé entre instances ? | ❌ Non — chaque instance a son propre cache | ✅ Oui — toutes les instances partagent le même cache |
| Survit à un redémarrage ? | ❌ Non | ✅ Oui |
| Cas d'usage | Cache local, single instance | Microservices, plusieurs instances (Kubernetes) |

**Pourquoi c'est crucial en microservices :** Si vous avez 5 instances de `ProductService.API` derrière un load balancer (API Gateway YARP), un `IMemoryCache` créerait **5 caches différents et incohérents**. Redis résout ce problème avec un cache **partagé**.

```bash
dotnet add ProductService.Infrastructure package Microsoft.Extensions.Caching.StackExchangeRedis
```

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ProductService:";
});
```

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<T>(cached);

        var value = await factory();

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return value;
    }
}
```

> **Point Clean Architecture :** Grâce à l'interface `ICacheService` définie au Jour 2, on peut **switcher de `MemoryCacheService` à `RedisCacheService`** en changeant uniquement l'enregistrement DI dans `Program.cs` — **aucune ligne des Use Cases ne change**.

---

### Output Caching ASP.NET Core

**Différence avec le Data Caching (Redis/Memory) :**

- **Data Caching** : met en cache le *résultat d'une méthode* (ex : `ProductDto`)
- **Output Caching** : met en cache la *réponse HTTP entière* (headers + body), avant même d'exécuter le controller

```csharp
// Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("ProductsCache", builder =>
        builder.Expire(TimeSpan.FromMinutes(2))
               .Tag("products"));
});

var app = builder.Build();
app.UseOutputCache();
```

```csharp
[HttpGet]
[OutputCache(PolicyName = "ProductsCache")]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(...)
{
    // Si la requête identique a été faite il y a < 2 min, ce code n'est même pas exécuté
    return Ok(await _service.GetAllAsync(...));
}
```

> **Quand l'utiliser ?** Pour des endpoints **GET** dont le résultat change peu (catalogues produits, configuration, listes de référence). **Jamais** pour des endpoints retournant des données spécifiques à un utilisateur authentifié sans varier le cache par utilisateur.

---

## Chapitre 3.2 — Scalabilité et Monitoring

### Objectif
Comprendre le scaling horizontal vs vertical, et mettre en place Prometheus/Grafana pour le monitoring.

---

### Scaling Vertical vs Horizontal

```
SCALING VERTICAL (Scale Up)
┌─────────────────┐        ┌─────────────────────┐
│  Serveur         │   →    │  Serveur PLUS GROS  │
│  4 CPU / 8GB RAM │        │  16 CPU / 64GB RAM  │
└─────────────────┘        └─────────────────────┘
Limite : il existe toujours une taille de serveur maximale (et un coût croissant)


SCALING HORIZONTAL (Scale Out)
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│ Instance │  │ Instance │  │ Instance │  │ Instance │
│    1     │  │    2     │  │    3     │  │    4     │
└──────────┘  └──────────┘  └──────────┘  └──────────┘
       └──────────────┬──────────────────────┘
                  Load Balancer (YARP / Kubernetes Service)
```

> **La Clean Architecture facilite le scaling horizontal** car :
> - Pas d'état partagé en mémoire dans les Services (Scoped, stateless)
> - Le cache distribué (Redis) est partagé entre toutes les instances
> - Chaque microservice peut scaler **indépendamment** selon sa charge

---

### Kubernetes : Horizontal Pod Autoscaler (HPA)

**Principe :** Kubernetes peut **automatiquement** ajouter ou supprimer des instances (pods) de votre application selon des métriques (CPU, mémoire, requêtes/seconde).

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: productservice-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: productservice-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70  # scale up si CPU moyen > 70%
```

**Lecture de cette configuration :**
- Minimum **2 instances** toujours actives (haute disponibilité)
- Maximum **10 instances** en cas de forte charge
- Kubernetes ajoute des instances automatiquement si le CPU moyen dépasse 70%

---

### Prometheus et Grafana : le duo de monitoring open-source

**Prometheus** collecte des métriques numériques dans le temps. **Grafana** les visualise sous forme de dashboards.

```
ProductService.API ──[expose /metrics]──> Prometheus ──[query]──> Grafana
OrderService.API    ──[expose /metrics]──┘                          │
                                                                 Dashboards
```

**Exposer des métriques .NET avec `prometheus-net` :**

```bash
dotnet add ProductService.API package prometheus-net.AspNetCore
```

```csharp
// Program.cs
var app = builder.Build();

app.UseHttpMetrics(); // métriques HTTP automatiques (latence, status codes, etc.)
app.MapMetrics();     // expose l'endpoint /metrics pour Prometheus

app.Run();
```

**Métriques custom pour une règle métier :**

```csharp
using Prometheus;

public class ProductService : IProductService
{
    private static readonly Counter ProductsCreated = Metrics
        .CreateCounter("products_created_total", "Nombre total de produits créés");

    private static readonly Histogram StockUpdateDuration = Metrics
        .CreateHistogram("stock_update_duration_seconds", "Durée des mises à jour de stock");

    public async Task<ProductDto> CreateAsync(ProductDto dto)
    {
        var product = Product.Create(dto.Name, dto.Price, dto.Description, dto.Stock);
        await _repository.AddAsync(product);

        ProductsCreated.Inc(); // ✅ incrémente le compteur

        return _mapper.Map<ProductDto>(product);
    }

    public async Task UpdateStockAsync(Guid id, int quantity)
    {
        using (StockUpdateDuration.NewTimer()) // ✅ mesure automatiquement la durée
        {
            var product = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException();
            product.DecrementStock(quantity);
            await _repository.UpdateAsync(product);
        }
    }
}
```

**Dashboard Grafana typique :**

| Panel | Requête PromQL | Signification |
|---|---|---|
| Requêtes par seconde | `rate(http_requests_total[1m])` | Throughput en temps réel |
| Latence P95 | `histogram_quantile(0.95, http_request_duration_seconds)` | 95% des requêtes sont plus rapides que X |
| Produits créés/heure | `rate(products_created_total[1h])` | Métrique métier |
| CPU par pod | `container_cpu_usage_seconds_total` | Santé infrastructure |

---

### Migration Legacy vers Optimisé sans casser la Clean Architecture

**Scénario réel :** Un Use Case existant est lent. Comment l'optimiser progressivement, sans tout casser ?

**Démarche en 4 étapes :**

```
1. MESURER     → Benchmark + Profiling (Jour 1) : identifier le VRAI bottleneck
2. ISOLER      → L'optimisation reste dans UNE couche (souvent Infrastructure)
3. TESTER      → Les tests existants (xUnit) doivent toujours passer
4. COMPARER    → Benchmark avant/après pour valider le gain réel
```

**Exemple concret — avant/après sur `GetAllAsync` :**

```csharp
// ❌ AVANT (Lab 1, Chapitre 1.1) — N+1 queries, pas de pagination réelle
public async Task<IEnumerable<ProductDto>> GetAllWithCategoryNamesAsync()
{
    var products = await _repository.GetAllAsync(1, 1000);
    var result = new List<ProductDto>();
    foreach (var product in products)
    {
        var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
        result.Add(new ProductDto { /* ... */ CategoryName = category?.Name });
    }
    return result;
}

// ✅ APRÈS — JOIN unique, pagination réelle, AsNoTracking, projection directe
public async Task<IEnumerable<ProductDto>> GetAllWithCategoryNamesAsync(int pageNumber, int pageSize)
{
    return await _context.Products
        .AsNoTracking()
        .Join(_context.Categories,
            p => p.CategoryId,
            c => c.Id,
            (p, c) => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CategoryName = c.Name
            })
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}
```

> **Ce qui n'a PAS changé :** l'interface `IProductService.GetAllWithCategoryNamesAsync` reste compatible (signature étendue avec des paramètres optionnels). Le Controller et les autres couches ne sont pas impactés.
>
> **Ce qui a changé :** uniquement l'implémentation dans Infrastructure — passage de N+1 requêtes à 1 seule requête SQL avec `JOIN`.

---

## Lab 3 — Caching, Load Testing et Monitoring

### Étape 1 — Démarrer Redis localement (Docker)

```bash
docker run -d --name redis-cache -p 6379:6379 redis:7-alpine
```

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

---

### Étape 2 — Ajouter le caching à un Use Case

Reprendre le `GetByIdAsync` du Jour 2 et remplacer `MemoryCacheService` par `RedisCacheService` :

```csharp
// Program.cs — un seul changement de ligne pour passer en distribué
// builder.Services.AddScoped<ICacheService, MemoryCacheService>();   // avant
builder.Services.AddScoped<ICacheService, RedisCacheService>();        // après
```

> **C'est tout.** Aucune ligne dans `ProductService.Application` n'a changé — c'est la preuve concrète que l'inversion de dépendances fonctionne.

---

### Étape 3 — Load Testing avec Apache JMeter

**Installation :** Télécharger depuis [jmeter.apache.org](https://jmeter.apache.org/)

**Configuration d'un test simple :**

1. Créer un **Thread Group** : 100 utilisateurs simultanés, montée en charge sur 10 secondes
2. Ajouter une **HTTP Request** : `GET https://localhost:5001/api/v1/Product/{id}`
3. Ajouter un **Listener** : "View Results in Table" + "Summary Report"

**Scénario de test :**

```
Thread Group:
  - Number of Threads (users): 100
  - Ramp-up period: 10 seconds
  - Loop Count: 50

HTTP Request:
  - GET /api/v1/Product/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Comparer AVANT/APRÈS caching :**

| Scénario | Latence moyenne | Latence P95 | Erreurs |
|---|---|---|---|
| Sans cache (DB à chaque requête) | 85 ms | 210 ms | 0 |
| Avec Redis cache (5 min TTL) | 8 ms | 15 ms | 0 |

> **Interprétation :** Le cache réduit la latence moyenne de ~90% pour les lectures répétées. C'est l'optimisation à plus fort impact pour un coût d'implémentation minimal.

---

### Étape 4 — Dashboard Grafana minimal

**docker-compose.yml pour Prometheus + Grafana :**

```yaml
version: '3.8'
services:
  prometheus:
    image: prom/prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    depends_on:
      - prometheus
```

**`prometheus.yml` :**

```yaml
global:
  scrape_interval: 5s

scrape_configs:
  - job_name: 'productservice'
    static_configs:
      - targets: ['host.docker.internal:5001']
```

```bash
docker-compose up -d
```

- Prometheus : `http://localhost:9090`
- Grafana : `http://localhost:3000` (login: admin/admin)

**Dans Grafana :**
1. Ajouter Prometheus comme datasource (`http://prometheus:9090`)
2. Importer le dashboard ASP.NET Core officiel (ID `19924` sur grafana.com)
3. Observer en temps réel : requêtes/sec, latence P50/P95/P99, erreurs

---

### Exercice final — Optimiser et simuler une charge

**Objectif :** Prendre le `GetAllWithCategoryNamesAsync` (avant : N+1 queries) et :

1. Appliquer la version optimisée (JOIN + AsNoTracking + pagination)
2. Ajouter l'Output Caching avec une politique de 2 minutes
3. Lancer un test JMeter avec 200 utilisateurs simultanés
4. Comparer les métriques Grafana avant/après

**Grille d'évaluation :**

| Critère | Points |
|---|---|
| Réduction du nombre de requêtes SQL (N+1 → 1) | 25% |
| `AsNoTracking()` appliqué correctement | 15% |
| Output Caching configuré sans casser l'authentification | 20% |
| Tests xUnit existants toujours verts | 20% |
| Amélioration mesurée (benchmark avant/après) | 20% |

---

# Ressources et références

### Livres recommandés

| Titre | Auteur | Niveau |
|---|---|---|
| *Pro .NET Performance* | Sasha Goldshtein et al. | Avancé |
| *Writing High-Performance .NET Code* | Ben Watson | Avancé |
| *Pro .NET Memory Management* | Konrad Kokosa | Expert |
| *CLR via C#* | Jeffrey Richter | Référence |

### Outils et liens

| Outil | URL |
|---|---|
| BenchmarkDotNet | https://benchmarkdotnet.org/ |
| dotnet-trace / dotnet-dump / dotnet-counters | https://learn.microsoft.com/dotnet/core/diagnostics/ |
| Apache JMeter | https://jmeter.apache.org/ |
| Prometheus | https://prometheus.io/ |
| Grafana | https://grafana.com/ |
| prometheus-net | https://github.com/prometheus-net/prometheus-net |

### Packages NuGet de référence

```bash
# Benchmarking
dotnet add package BenchmarkDotNet

# Caching
dotnet add package Microsoft.Extensions.Caching.Memory
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis

# Object Pooling
dotnet add package Microsoft.Extensions.ObjectPool

# Monitoring
dotnet add package prometheus-net.AspNetCore
dotnet add package Microsoft.ApplicationInsights.AspNetCore

# Diagnostic tools (CLI)
dotnet tool install --global dotnet-trace
dotnet tool install --global dotnet-dump
dotnet tool install --global dotnet-counters
```

---

*Formation Optimisation des Performances .NET — Niveau Senior — Suite directe de la formation Clean Architecture .NET 8*
