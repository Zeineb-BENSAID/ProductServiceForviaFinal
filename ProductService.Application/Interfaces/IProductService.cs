using ProductService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDTO>> GetAllAsync(int pageNumber, int pageSize, string? filter = null);
    Task<ProductDTO?> GetByIdAsync(Guid id);
    Task<ProductDTO> CreateAsync(ProductDTO productDto);
    Task UpdateAsync(Guid id, ProductDTO productDto);
    Task DeleteAsync(Guid id);
    Task UpdateStockAsync(Guid id, int quantity);
}
