using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Benchmarks;

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
