using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Infrastructure.Data;

/// <summary>
/// Implémentation concrète du repository avec EF Core.
/// Vit dans l'Infrastructure — c'est ici que EF Core est connu.
/// Implémente l'interface définie dans le Domain.
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly ProductDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(ProductDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        IQueryable<T> query = _dbSet.AsNoTracking(); // AsNoTracking = meilleure perf pour les lectures

        if (filter != null) query = query.Where(filter);
        if (orderBy != null) query = orderBy(query);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    //public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    // ✅ Compiled Query : la traduction LINQ → SQL est faite UNE FOIS, réutilisée ensuite
    private static readonly Func<ProductDbContext, Guid, Task<Product?>> GetProductByIdCompiled =
        EF.CompileAsyncQuery((ProductDbContext context, Guid id) =>
            context.Products.AsNoTracking().FirstOrDefault(p => p.Id == id));


    public async Task<T?> GetByIdAsync(Guid id)
    {
        if (typeof(T) == typeof(Product) && _context is ProductDbContext ctx)
        {
            return await GetProductByIdCompiled(ctx, id) as T;
        }

        return await _dbSet.FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
