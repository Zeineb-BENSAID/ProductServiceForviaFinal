using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Interfaces;

/// <summary>
/// Abstraction du caching — définie dans Infrastructure car c'est un détail technique,
/// mais pourrait être déplacée dans Application si l'Application doit en dépendre.
/// </summary>
public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
    void Remove(string key);
}
