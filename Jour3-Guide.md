# JOUR 3 — Guidé
## Caching, Tests de Charge et Monitoring —


---

## Avant de commencer — Vérifier ce qu'on a déjà

On va utiliser 3 outils dans ce chapitre : **Docker**, **Redis**, et **JMeter**. 

### Étape 0.1 — Vérifier si Docker est installé

Ouvrez un terminal (PowerShell, CMD, ou Terminal Mac/Linux) et tapez :

```bash
docker --version
```

**Résultat attendu :**
```
Docker version 24.0.x, build xxxxxxx
```

➡️ **Si vous voyez une version :** Docker est prêt, passez à l'étape 0.2.

➡️ **Si vous voyez "command not found" ou une erreur :** Docker n'est pas installé.

---

### Étape 0.2 — Installer Docker (uniquement si nécessaire)

| Système | Lien de téléchargement |
|---|---|
| Windows | https://www.docker.com/products/docker-desktop/ |
| Mac | https://www.docker.com/products/docker-desktop/ |
| Linux | https://docs.docker.com/engine/install/ |

**Étapes :**
1. Téléchargez Docker Desktop
2. Installez-le (double-clic sur le fichier téléchargé, suivez l'assistant)
3. Redémarrez votre ordinateur si demandé
4. Lancez Docker Desktop (l'icône doit apparaître dans la barre des tâches)
5. Attendez que l'icône devienne **verte/stable** (ça veut dire que Docker tourne)

**Vérification finale :**
```bash
docker --version
docker ps
```

**Résultat attendu pour `docker ps` :**
```
CONTAINER ID   IMAGE   COMMAND   CREATED   STATUS   PORTS   NAMES
```
*(Un tableau vide est normal — ça veut juste dire qu'aucun container ne tourne encore)*

✅ **Si vous voyez ce tableau (même vide) → Docker fonctionne. On peut continuer.**

> 💡 **Si vous ne pouvez/voulez pas installer Docker**, ne vous inquiétez pas : chaque section ci-dessous a une **alternative sans Docker**.

---

# PARTIE 1 — Le Caching avec Redis

## 1.1 — C'est quoi le Caching ? 

Imaginez ce scénario : un utilisateur demande "donne-moi les infos du produit 123". Votre application va chercher dans la base de données. Ça prend, disons, **80 millisecondes**.

Si 100 personnes demandent le même produit dans la minute qui suit, vous refaites **100 fois** le même aller-retour vers la base de données, pour obtenir **exactement la même réponse**.

**Le caching, c'est simple :** la première fois, on garde la réponse "de côté" (en mémoire). Les fois suivantes, on donne directement cette réponse gardée, sans repasser par la base de données.

```
SANS CACHE :
Utilisateur 1 → [Base de données: 80ms] → Réponse
Utilisateur 2 → [Base de données: 80ms] → Réponse
Utilisateur 3 → [Base de données: 80ms] → Réponse

AVEC CACHE :
Utilisateur 1 → [Base de données: 80ms] → Réponse (on la garde de côté)
Utilisateur 2 → [Cache: 2ms] → Réponse (déjà en mémoire !)
Utilisateur 3 → [Cache: 2ms] → Réponse (déjà en mémoire !)
```

**Qu'est-ce que Redis dans tout ça ?**

Redis est juste un programme qui sert de "boîte de rangement" pour ces réponses gardées de côté. C'est très rapide et c'est l'outil standard pour ça.

> **Ce qu'on va faire dans cette partie :** installer Redis, le brancher à notre API, et vérifier que ça marche.

---

## 1.2 — Installation de Redis

### Option A — Avec Docker (recommandé, plus simple)

Dans votre terminal :

```bash
docker run -d --name redis-cache -p 6379:6379 redis:7-alpine
```

**Ce que fait cette commande, expliqué mot par mot :**
- `docker run` : démarre un nouveau programme dans Docker
- `-d` : le fait tourner en arrière-plan (vous gardez votre terminal libre)
- `--name redis-cache` : donne le nom "redis-cache" à ce programme, pour le retrouver facilement
- `-p 6379:6379` : ouvre la porte de communication (port) 6379, le port standard de Redis
- `redis:7-alpine` : le programme à télécharger et lancer (Redis version 7, en version légère "alpine")

**Test immédiat — vérifier que Redis tourne :**

```bash
docker ps
```

**Résultat attendu :**
```
CONTAINER ID   IMAGE             STATUS         PORTS                    NAMES
a1b2c3d4e5f6   redis:7-alpine    Up 10 seconds  0.0.0.0:6379->6379/tcp   redis-cache
```

✅ **Si vous voyez "redis-cache" dans la liste avec "Up X seconds" → Redis fonctionne !**

---

### Option B — Sans Docker (alternative)

Si vous ne pouvez pas utiliser Docker, Redis a une version native Windows via WSL, ou vous pouvez utiliser Memurai (équivalent Redis pour Windows) :

- **Windows :** https://www.memurai.com/ (téléchargez la version gratuite Developer)
- **Mac :** `brew install redis` puis `brew services start redis` (nécessite Homebrew : https://brew.sh)
- **Linux :** `sudo apt install redis-server` puis `sudo systemctl start redis`

**Test (toutes plateformes) :**
```bash
redis-cli ping
```

**Résultat attendu :**
```
PONG
```

✅ **Si vous voyez "PONG" → Redis fonctionne !**

> 🆘 **Si rien ne fonctionne :** pas de panique. On peut faire tout l'exercice de caching avec `IMemoryCache` (qui ne nécessite AUCUNE installation, déjà inclus dans .NET). Voir la section 1.5 "Alternative sans Redis" plus bas.

---

## 1.3 — Brancher Redis à notre API ProductService

### Étape 1 — Installer le package NuGet

```bash
dotnet add ProductService.Infrastructure package Microsoft.Extensions.Caching.StackExchangeRedis
```

**Résultat attendu dans le terminal :**
```
Determining projects to restore...
Writing C:\...\obj\ProductService.Infrastructure.csproj.nuget.g.props...
info : PackageReference for package 'Microsoft.Extensions.Caching.StackExchangeRedis' ... added
```

✅ **Si vous voyez "added" → le package est installé.**

---

### Étape 2 — Dire à l'application où trouver Redis

Ouvrez le fichier `ProductService.API/appsettings.json` et ajoutez cette ligne :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ProductDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "Redis": "localhost:6379"
  }
}
```

> 📝 **Explication :** `localhost:6379` veut dire "Redis tourne sur cet ordinateur (localhost), sur le port 6379". C'est exactement le port qu'on a ouvert avec Docker à l'étape 1.2.

---

### Étape 3 — Activer Redis dans le code

Ouvrez `ProductService.API/Program.cs` et ajoutez cette ligne **avant** `var app = builder.Build();` :

```csharp
// Connecter l'application à Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ProductService:";
});
```

> 📝 **Explication ligne par ligne :**
> - `AddStackExchangeRedisCache` : active la connexion Redis dans l'application
> - `options.Configuration = ...` : récupère l'adresse Redis qu'on a mise dans `appsettings.json`
> - `options.InstanceName = "ProductService:"` : préfixe toutes les clés de cache avec "ProductService:" pour éviter les mélanges si plusieurs applications utilisent le même Redis

---

### Étape 4 — Créer le service de cache (le code qui sait lire/écrire dans Redis)

Créez un nouveau fichier : `ProductService.Infrastructure/Caching/ICacheService.cs`

```csharp
namespace ProductService.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
}
```

> 📝 **Explication :** Cette interface décrit une seule action : *"Donne-moi la valeur associée à cette clé. Si elle n'existe pas encore, calcule-la avec `factory`, garde-la en cache, puis retourne-la."*

Créez maintenant : `ProductService.Infrastructure/Caching/RedisCacheService.cs`

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ProductService.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
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
}
```

> 📝 **Explication simple :** Cette méthode fait exactement 3 choses, dans l'ordre :
> 1. **Regarder** si la réponse existe déjà dans Redis
> 2. Si non, **calculer** la vraie réponse (appel à la base de données)
> 3. **Sauvegarder** cette réponse dans Redis, pour que la prochaine demande soit rapide

---

### Étape 5 — Enregistrer le service dans Program.cs

Ajoutez cette ligne juste après la ligne `AddStackExchangeRedisCache` :

```csharp
builder.Services.AddScoped<ICacheService, RedisCacheService>();
```

---

### Étape 6 — Utiliser le cache dans le Service ProductService

Ouvrez `ProductService.Application/Services/ProductService.cs`.

**Ajoutez le `ICacheService` au constructeur :**

```csharp
public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache; // ← nouvelle ligne

    public ProductService(IGenericRepository<Product> repository, IMapper mapper, ICacheService cache)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache; // ← nouvelle ligne
    }
```

**Modifiez la méthode `GetByIdAsync` pour utiliser le cache :**

```csharp
public async Task<ProductDto?> GetByIdAsync(Guid id)
{
    return await _cache.GetOrCreateAsync(
        key: $"product:{id}",                   // l'étiquette unique pour ce produit
        factory: async () =>                     // ce qu'on fait si pas en cache
        {
            var product = await _repository.GetByIdAsync(id);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        },
        expiration: TimeSpan.FromMinutes(5));     // garder en cache pendant 5 minutes
}
```

> ⚠️ **Important :** `ProductService.Infrastructure` doit référencer le namespace `ProductService.Infrastructure.Caching`. Ajoutez en haut du fichier :
> ```csharp
> using ProductService.Infrastructure.Caching;
> ```

---

## 1.4 — TEST : Vérifier que le cache fonctionne vraiment

### Étape 1 — Lancer l'application

```bash
cd ProductService.API
dotnet run
```

**Résultat attendu :**
```
Now listening on: https://localhost:5001
Application started. Press Ctrl+C to shut down.
```

### Étape 2 — Premier appel (sans cache — doit être plus lent)

Ouvrez Swagger : `https://localhost:5001/swagger`

Testez `GET /api/v1/Product/{id}` avec un ID de produit existant. Notez le temps de réponse affiché par Swagger (en bas à droite généralement, ou utilisez l'onglet Network du navigateur).

**Résultat attendu :** quelque chose comme **40-100ms** (premier appel, va chercher en base de données)

### Étape 3 — Deuxième appel (avec cache — doit être beaucoup plus rapide)

Refaites **exactement la même requête** (même ID).

**Résultat attendu :** quelque chose comme **2-10ms** (vient directement de Redis, pas de la base de données)

✅ **Si le 2ème appel est nettement plus rapide que le 1er → le cache fonctionne parfaitement !**

### Étape 4 — Vérifier directement dans Redis (optionnel mais rassurant)

```bash
docker exec -it redis-cache redis-cli
```

Puis dans l'invite Redis qui s'ouvre :

```
KEYS *
```

**Résultat attendu :**
```
1) "ProductService:product:3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

✅ **Si vous voyez une clé qui commence par "ProductService:product:" → la donnée est bien stockée dans Redis !**

Tapez `exit` pour quitter Redis CLI.

---

## 1.5 — Alternative SANS Redis (si vous n'avez pas pu l'installer)

Pas de souci ! `IMemoryCache` fait la même chose mais garde les données directement dans la mémoire de votre application .NET (pas besoin d'un programme externe).

**Différence à connaître :** `IMemoryCache` ne fonctionne bien que si vous avez **une seule instance** de votre application. Pour cette formation, c'est largement suffisant.

### Installation — Aucune ! C'est déjà inclus dans .NET.

### Code — Remplacez `RedisCacheService` par cette version :

Créez `ProductService.Infrastructure/Caching/MemoryCacheService.cs` :

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace ProductService.Infrastructure.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            return cachedValue; // trouvé en mémoire, on renvoie directement
        }

        var freshValue = await factory(); // pas trouvé, on calcule
        _cache.Set(key, freshValue, expiration); // on garde en mémoire pour la prochaine fois
        return freshValue;
    }
}
```

Dans `Program.cs`, remplacez :
```csharp
// builder.Services.AddStackExchangeRedisCache(...);  // on n'utilise pas Redis
builder.Services.AddMemoryCache();                      // version simple, sans installation
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
```

**Le test est exactement le même que dans la section 1.4** — premier appel lent, deuxième appel rapide. Le résultat attendu est identique.

---

# PARTIE 2 — Tester sous charge avec Apache JMeter

## 2.1 — C'est quoi un test de charge ? 
Jusqu'ici, on a testé avec **une seule personne** (vous) qui clique sur Swagger. Mais en vrai, une application doit parfois répondre à **100 ou 1000 personnes en même temps**.

**JMeter**, c'est un outil qui simule plein d'utilisateurs qui appellent votre API en même temps, pour voir comment elle réagit sous la pression.

> **Ce qu'on va faire :** installer JMeter, lui dire d'envoyer 50 requêtes simultanées vers notre API, et regarder les résultats.

---

## 2.2 — Installation de JMeter

### Étape 1 — Télécharger

Allez sur : https://jmeter.apache.org/download_jmeter.cgi

Téléchargez le fichier **Binaries → apache-jmeter-5.6.3.zip** (ou la dernière version disponible)

> 💡 **Prérequis :** JMeter nécessite Java. Si vous avez déjà installé Java pour Keycloak (formation précédente), c'est déjà bon. Sinon, téléchargez Java 21 ici : https://adoptium.net/temurin/releases/?version=21

### Étape 2 — Installer

1. Décompressez le fichier ZIP téléchargé dans un dossier simple (ex : `C:\jmeter` ou `~/jmeter`)
2. Aucune autre installation nécessaire — JMeter ne s'installe pas, il se lance directement

### Étape 3 — Lancer JMeter

**Windows :**
```bash
cd C:\jmeter\apache-jmeter-5.6.3\bin
jmeter.bat
```

**Mac/Linux :**
```bash
cd ~/jmeter/apache-jmeter-5.6.3/bin
./jmeter.sh
```

**Résultat attendu :** une fenêtre JMeter s'ouvre avec une interface graphique.

✅ **Si la fenêtre JMeter apparaît → l'installation est réussie !**

---

## 2.3 — Créer votre premier test de charge (pas à pas avec captures conceptuelles)

### Étape 1 — Créer un "Thread Group" (= groupe d'utilisateurs simulés)

1. Dans JMeter, clic droit sur **"Test Plan"** (en haut à gauche)
2. Choisir **Add → Threads (Users) → Thread Group**
3. Une nouvelle section apparaît au centre. Remplissez :

| Champ | Valeur | Explication |
|---|---|---|
| Number of Threads (users) | `50` | Simule 50 utilisateurs différents |
| Ramp-up period (seconds) | `10` | Les 50 utilisateurs arrivent progressivement sur 10 secondes (pas tous d'un coup) |
| Loop Count | `5` | Chaque utilisateur répète sa requête 5 fois |

> 📝 **Résultat avec ces chiffres :** 50 utilisateurs × 5 requêtes = **250 requêtes au total**, envoyées progressivement sur 10 secondes.

---

### Étape 2 — Ajouter la requête HTTP à tester

1. Clic droit sur **"Thread Group"** (que vous venez de créer)
2. Choisir **Add → Sampler → HTTP Request**
3. Remplissez :

| Champ | Valeur |
|---|---|
| Server Name or IP | `localhost` |
| Port Number | `5001` |
| Method | `GET` |
| Path | `/api/v1/Product/3fa85f64-5717-4562-b3fc-2c963f66afa6` *(remplacez par un vrai ID de produit)* |

> ⚠️ **Important :** Si votre API utilise HTTPS, ouvrez l'onglet **"Advanced"** dans la même fenêtre et cochez la case pour autoriser les certificats auto-signés (sinon JMeter refuse de se connecter).

---

### Étape 3 — Ajouter un moyen de voir les résultats

1. Clic droit sur **"Thread Group"**
2. Choisir **Add → Listener → Summary Report**

C'est un tableau qui va afficher les statistiques de votre test (temps moyen, erreurs, etc.)

---

### Étape 4 — Lancer le test

1. Cliquez sur le bouton **vert ▶️ "Start"** en haut de la fenêtre (ou `Ctrl+R`)
2. Cliquez sur **"Summary Report"** dans la liste à gauche pour voir les résultats en direct

**Résultat attendu — tableau qui se remplit progressivement :**

| Label | # Samples | Average | Min | Max | Error % |
|---|---|---|---|---|---|
| HTTP Request | 250 | 45 ms | 8 ms | 120 ms | 0.00% |

> 📝 **Comment lire ce tableau :**
> - **# Samples** : nombre total de requêtes envoyées (doit être 250 si tout s'est bien passé)
> - **Average** : temps de réponse moyen en millisecondes
> - **Min / Max** : la requête la plus rapide / la plus lente
> - **Error %** : doit être **0.00%** — s'il y a des erreurs, vérifiez que votre API tourne bien

✅ **Si vous voyez "Error % = 0.00%" et un tableau rempli → le test a réussi !**

---

## 2.4 — Comparer AVANT et APRÈS le cache (l'exercice le plus important)

### Étape 1 — Test SANS cache

Si vous avez suivi la Partie 1, le cache est déjà actif. Pour tester "sans cache", changez temporairement le temps d'expiration à une valeur très courte, ou testez avec un ID de produit **différent à chaque fois** (jamais en cache).

**Astuce simple :** dans JMeter, ajoutez un second test avec un ID de produit aléatoire pour forcer un passage systématique en base de données.

**Résultat attendu (exemple) :**

| Label | Average | Min | Max |
|---|---|---|---|
| Sans cache | 85 ms | 60 ms | 210 ms |

### Étape 2 — Test AVEC cache (même produit demandé plusieurs fois)

Remettez le même ID de produit pour toutes les requêtes (pour bénéficier du cache après le premier appel).

**Résultat attendu (exemple) :**

| Label | Average | Min | Max |
|---|---|---|---|
| Avec cache | 8 ms | 2 ms | 15 ms |

✅ **Conclusion attendue :** le temps moyen avec cache doit être **largement inférieur** (souvent 5 à 10 fois plus rapide) au temps sans cache.

> 🎉 **Félicitations — vous venez de prouver, avec des vrais chiffres, que votre optimisation fonctionne !**

---

# PARTIE 3 — Voir les performances en direct avec un Dashboard (Monitoring simple)

## 3.1 — C'est quoi le Monitoring ? 

Jusqu'ici, on a mesuré "à la main" avec JMeter. Mais en production, on veut un **écran qui affiche en permanence** la santé de l'application : combien de requêtes par seconde, est-ce que c'est lent, y a-t-il des erreurs...

**Prometheus** collecte ces chiffres. **Grafana** les affiche sous forme de joli graphique.

>  **Ce qu'on va faire :** afficher un dashboard simple qui montre le nombre de requêtes et leur vitesse, en temps réel.

> 💡 **Cette partie est optionnelle et plus avancée.** .

---

## 3.2 — Installation avec Docker (méthode recommandée)

### Étape 1 — Créer un fichier de configuration pour Prometheus

Créez un nouveau fichier nommé `prometheus.yml` dans un dossier de votre choix (ex : `C:\monitoring\prometheus.yml`) :

```yaml
global:
  scrape_interval: 5s

scrape_configs:
  - job_name: 'productservice'
    static_configs:
      - targets: ['host.docker.internal:5001']
```

> 📝 **Explication :** Ce fichier dit à Prometheus *"va regarder toutes les 5 secondes ce qui se passe sur l'application qui tourne sur le port 5001"*.

### Étape 2 — Créer le fichier docker-compose.yml

Dans le **même dossier**, créez `docker-compose.yml` :

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
```

### Étape 3 — Démarrer les deux outils en une seule commande

Dans votre terminal, allez dans le dossier où sont les 2 fichiers, puis :

```bash
docker-compose up -d
```

**Résultat attendu :**
```
[+] Running 2/2
 ✔ Container monitoring-prometheus-1   Started
 ✔ Container monitoring-grafana-1      Started
```

✅ **Si vous voyez "Started" deux fois → les deux outils tournent !**

---

## 3.3 — Préparer votre API pour être surveillée

### Étape 1 — Installer le package

```bash
dotnet add ProductService.API package prometheus-net.AspNetCore
```

### Étape 2 — Activer dans Program.cs

Ajoutez ces deux lignes **juste avant** `app.Run();` :

```csharp
app.UseHttpMetrics();  // collecte automatiquement les statistiques de chaque requête
app.MapMetrics();      // crée une page spéciale /metrics que Prometheus va lire
```

### Étape 3 — Relancer l'application

```bash
dotnet run
```

### Étape 4 — Vérifier que ça fonctionne

Ouvrez votre navigateur sur : `https://localhost:5001/metrics`

**Résultat attendu :** une longue page de texte qui ressemble à ça :
```
# HELP http_requests_received_total ...
http_requests_received_total{code="200",method="GET"} 12
```

✅ **Si vous voyez ce genre de texte → votre application expose bien ses statistiques !**

---

## 3.4 — Voir le dashboard dans Grafana

### Étape 1 — Ouvrir Grafana

Allez sur : `http://localhost:3000`

**Identifiants par défaut :**
- Utilisateur : `admin`
- Mot de passe : `admin`

*(Grafana demandera peut-être de changer le mot de passe — vous pouvez cliquer "Skip" pour passer cette étape pendant la formation)*

### Étape 2 — Connecter Grafana à Prometheus

1. Dans le menu de gauche, allez dans **Connections → Data sources**
2. Cliquez **Add data source**
3. Choisissez **Prometheus**
4. Dans le champ URL, mettez : `http://prometheus:9090`
5. Cliquez **Save & Test** en bas

**Résultat attendu :** un message vert *"Data source is working"*

✅ **Si vous voyez ce message vert → Grafana est connecté à Prometheus !**

### Étape 3 — Créer un graphique simple

1. Menu de gauche → **Dashboards → New → New Dashboard**
2. Cliquez **Add visualization**
3. Choisissez la source **Prometheus**
4. Dans le champ de requête, tapez simplement :
   ```
   rate(http_requests_received_total[1m])
   ```
5. Cliquez **Run query** (ou attendez le rafraîchissement automatique)

**Résultat attendu :** une ligne sur le graphique qui représente le nombre de requêtes par seconde de votre API.

### Étape 4 — Générer du trafic pour voir le graphique bouger

Relancez votre test JMeter de la Partie 2 pendant que vous regardez le graphique Grafana.

**Résultat attendu :** la ligne sur le graphique **monte** pendant que JMeter envoie des requêtes, puis **redescend** quand le test se termine.

🎉 **Si vous voyez la ligne bouger en fonction du trafic → vous avez un monitoring fonctionnel !**

---

## 3.5 — Alternative SANS Docker (Application Insights, plus simple à activer)

Si Docker n'est pas une option, vous pouvez utiliser **Application Insights** (nécessite un compte Azure gratuit) :

```bash
dotnet add ProductService.API package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);
```

Le dashboard est alors automatiquement disponible dans le portail Azure (portal.azure.com), sans installation locale de Prometheus/Grafana. C'est une bonne alternative si vous êtes déjà dans l'écosystème Azure.

---

# Récapitulatif — Ce qu'on a accompli aujourd'hui

| Étape | Ce qu'on a fait | Comment on sait que ça marche |
|---|---|---|
| 1. Caching | Branché Redis (ou IMemoryCache) à `GetByIdAsync` | 2ème appel beaucoup plus rapide que le 1er |
| 2. Test de charge | Simulé 50 utilisateurs avec JMeter | Tableau de résultats avec 0% d'erreur |
| 3. Comparaison | Mesuré avant/après cache | Temps moyen "avec cache" 5-10x plus rapide |
| 4. Monitoring *(optionnel)* | Dashboard Grafana en temps réel | Graphique qui bouge selon le trafic |

---

## Nettoyage — Arrêter les outils après la formation

Quand vous avez terminé, pour libérer les ressources de votre ordinateur :

```bash
# Arrêter Redis
docker stop redis-cache
docker rm redis-cache

# Arrêter Prometheus + Grafana
docker-compose down
```

---

*Formation Optimisation des Performances .NET — Jour 3 Simplifié — Guide pas à pas*
