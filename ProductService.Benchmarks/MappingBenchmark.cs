using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.StackSources;
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
    public ProductDTO ManualMapping()
    {
        // Mapping manuel : chaque propriété assignée explicitement
        return new ProductDTO
        {
            Id = _product.Id,
            Name = _product.Name,
            Price = _product.Price,
            Description = _product.Description,
            Stock = _product.Stock
        };
    }

    [Benchmark]
    public ProductDTO AutoMapperMapping()
    {
        return _mapper.Map<ProductDTO>(_product);
    }
}
