using System.Linq.Expressions;
using AutoMapper;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Services;

/// <summary>
/// Service applicatif : orchestre les Use Cases liés aux produits.
/// Dépend uniquement des abstractions (IGenericRepository, IMapper)
/// jamais des implémentations concrètes (EF Core, SQL).
/// </summary>
public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _repository;
    private readonly IMapper _mapper;

    public ProductService(IGenericRepository<Product> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDTO>> GetAllAsync(int pageNumber, int pageSize, string? filter = null)
    {
        // Construction conditionnelle du filtre — pas de SQL ici, juste une Expression LINQ
        Expression<Func<Product, bool>>? filterExpression = null;

        if (!string.IsNullOrWhiteSpace(filter))
            filterExpression = p => p.Name.Contains(filter) || p.Description.Contains(filter);

        var products = await _repository.GetAllAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            filter: filterExpression,
            orderBy: q => q.OrderBy(p => p.Name));

        return _mapper.Map<IEnumerable<ProductDTO>>(products);
    }

    //public async Task<ProductDTO?> GetByIdAsync(Guid id)
    //{
    //    var product = await _repository.GetByIdAsync(id);
    //    return product == null ? null : _mapper.Map<ProductDTO>(product);
    //}
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

    public async Task<ProductDTO> CreateAsync(ProductDTO productDto)
    {
        // Utiliser la méthode factory de l'entité pour garantir les invariants
        var product = Product.Create(productDto.Name, productDto.Price, productDto.Description, productDto.Stock);
        await _repository.AddAsync(product);
        return _mapper.Map<ProductDTO>(product);
    }

    public async Task UpdateAsync(Guid id, ProductDTO productDto)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product with ID {id} not found.");

        // Utiliser la méthode de l'entité pour modifier — les invariants sont respectés
        existing.Update(productDto.Name, productDto.Price, productDto.Description);
        await _repository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product with ID {id} not found.");

        await _repository.DeleteAsync(product);
    }

    public async Task UpdateStockAsync(Guid id, int quantity)
    {
        var product = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product with ID {id} not found.");

        // La règle métier (stock suffisant) est dans l'entité, pas ici
        product.DecrementStock(quantity);
        await _repository.UpdateAsync(product);
    }
}