# Détecter les failles de performance : Développement vs Production

## Question 

> *"Comment faire pour détecter les failles de performance en production, sur une application déjà déployée et utilisée par le client final ? Est-ce que ce qu'on a vu en formation permet de faire ça en dev, en prod, ou les deux ?"*

---

## Réponse 

**Les deux** — mais pas avec les mêmes outils, ni avec la même logique d'utilisation. C'est une distinction essentielle à bien comprendre.

> **En développement, on cherche.** On a un accès total à la machine, on peut profiler en profondeur, lancer des benchmarks, simuler de la charge artificielle.
>
> **En production, on observe.** On n'a généralement pas d'accès direct au serveur (surtout chez un client), donc on dépend d'outils qui **collectent et exposent les métriques automatiquement**, consultables à distance, sans rien installer de nouveau au moment du problème.

---

## Tableau de référence — quel outil, pour quel environnement

| Outil vu en formation | Dev | Prod | Pourquoi |
|---|---|---|---|
| **BenchmarkDotNet** | ✅ Oui — usage principal | ❌ Non | Nécessite de lancer manuellement un projet de benchmark isolé. Jamais exécuté sur un serveur client |
| **dotnet-trace** | ✅ Oui | ⚠️ Possible, mais rare | Peut s'attacher à un process en prod, mais demande un accès serveur direct (SSH/RDP) et influence légèrement les performances pendant la capture |
| **dotnet-dump** | ✅ Oui | ⚠️ Possible, mais rare | Utile en prod uniquement pour un incident ponctuel ("le serveur consomme 4 Go, pourquoi ?"), jamais en continu |
| **dotnet-counters** | ✅ Oui | ⚠️ Possible, mais rare | Même contrainte — nécessite un accès direct au serveur |
| **Apache JMeter** | ✅ Oui | ❌ Jamais sur le serveur du client | La charge se simule **avant** la mise en production, sur un environnement de test/staging |
| **Application Insights** | ✅ Oui (utile aussi en dev) | ✅ **Oui — conçu pour ça** | Tourne en continu, sans accès serveur nécessaire |
| **Prometheus + Grafana** | ✅ Oui | ✅ **Oui — conçu pour ça** | Conçus pour le monitoring continu en production |
| **Serilog (logs structurés)** | ✅ Oui | ✅ **Oui — conçu pour ça** | Complémentaire au monitoring, vu dans la formation Clean Architecture |

---

## La méthode concrète en 4 étapes

### Étape 1 — Avoir déjà un outil de surveillance actif

Application Insights ou Prometheus/Grafana doivent être installés et configurés **avant** qu'un problème survienne — pas après. C'est une démarche préventive, pas réactive.

### Étape 2 — Repérer le signal dans le dashboard

On surveille en continu des indicateurs comme :
- la latence P95 / P99 qui grimpe anormalement
- le taux d'erreur qui augmente
- le CPU ou la mémoire qui dérive progressivement
(P95/P99 indiquent le temps de réponse maximal vécu par 95% ou 99% des utilisateurs, révélant les cas extrêmes que la moyenne dissimule.)
### Étape 3 — Creuser la cause si un accès exceptionnel est possible

Si l'on obtient un accès ponctuel au serveur (avec l'accord du client), on peut alors utiliser `dotnet-trace` ou `dotnet-dump` pour identifier précisément la méthode ou l'objet responsable du ralentissement.

### Étape 4 — Reproduire et valider en dev avant de redéployer

Une fois la cause identifiée, on reproduit le problème en environnement de développement ou de staging avec **BenchmarkDotNet** (pour mesurer précisément) et **JMeter** (pour simuler la charge), afin de valider que le correctif fonctionne avant de le déployer en production.

---

## Application Insights — explication complémentaire

C'est l'outil qui répond le plus directement à la question initiale : il permet de surveiller une application en production, à distance, sans avoir besoin d'un accès serveur.

**Ce que c'est :** un service de monitoring fourni par Microsoft Azure qui observe automatiquement l'application et affiche les résultats dans un tableau de bord web (le portail Azure).

**Analogie :** une boîte noire d'avion — elle enregistre automatiquement tout ce qui se passe pendant le vol, pour qu'on puisse comprendre après coup ce qui s'est produit. Application Insights fait la même chose pour une API : chaque requête, chaque erreur, chaque appel à la base de données est enregistré sans code supplémentaire à écrire.

**Ce qu'il capture automatiquement :**

| Donnée capturée | Exemple concret |
|---|---|
| Chaque requête HTTP | `GET /api/products` → 120ms → 200 OK |
| Chaque appel à la base de données | Requête SQL → 45ms |
| Chaque exception | `NotEnoughStockException` → 12 occurrences aujourd'hui |
| Les requêtes anormalement lentes | `GET /api/products?filter=x` → 2.3 secondes (alerte automatique) |

**Activation — une seule ligne de code :**

```bash
dotnet add ProductService.API package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);
```

Avec cette ligne, dès que l'application tourne en production, on peut ouvrir le portail Azure et voir en temps réel : le nombre d'utilisateurs, les requêtes lentes, la localisation des erreurs — sans jamais se connecter au serveur du client.

---

## Comparaison Application Insights vs Prometheus/Grafana

| | Application Insights | Prometheus + Grafana |
|---|---|---|
| Fournisseur | Microsoft Azure (gratuit jusqu'à un certain quota, payant au-delà) | Open-source, gratuit |
| Installation | Une ligne de code + un compte Azure | Installation et configuration de deux outils distincts |
| Hébergement | Microsoft héberge tout ; on consulte un site web | Hébergement à la charge de l'équipe (ou version cloud) |
| Adapté pour | Les équipes déjà dans l'écosystème Azure, qui veulent une mise en place rapide | Les équipes qui veulent un contrôle total ou qui n'utilisent pas Azure |

---

## Ce qu'il faut retenir

> Détecter une faille de performance en production n'est jamais une question d'outil unique, mais d'une **chaîne d'outils complémentaires** : une surveillance continue (Application Insights ou Prometheus/Grafana) pour repérer le problème, un diagnostic ciblé (dotnet-trace, dotnet-dump) pour comprendre la cause exacte, puis une validation en environnement de développement (BenchmarkDotNet, JMeter) avant tout nouveau déploiement.

---

*Document de restitution — Formation Optimisation des Performances .NET*
