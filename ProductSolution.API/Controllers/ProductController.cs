using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;

namespace ProductService.API.Controllers;

/// <summary>
/// Controller API REST pour les produits.
/// Ne contient PAS de logique métier — délègue tout au service.
/// Versioning : v1.0 et v2.0 avec comportements différents sur GET all.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class ProductController : ControllerBase
{
    private readonly IProductService _service;

    public ProductController(IProductService service) => _service = service;

    // GET v1 : page size par défaut = 10
    [HttpGet, MapToApiVersion("1.0")]
    public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAllV1(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? filter = null)
    {
        return Ok(await _service.GetAllAsync(pageNumber, pageSize, filter));
    }

    // GET v2 : page size par défaut = 25 (nouvelle valeur)
    [HttpGet, MapToApiVersion("2.0")]
    public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAllV2(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? filter = null)
    {
        return Ok(await _service.GetAllAsync(pageNumber, pageSize, filter));
    }

    [HttpGet("{id:guid}"), MapToApiVersion("1.0"), MapToApiVersion("2.0")]
    public async Task<ActionResult<ProductDTO>> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDTO>> Create([FromBody] ProductDTO dto)
    {
        var created = await _service.CreateAsync(dto);
        var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return CreatedAtAction(nameof(GetById), new { version, id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductDTO dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // Endpoint spécifique : décrémenter le stock
    [HttpPost("{id:guid}/stock/decrement")]
    public async Task<IActionResult> DecrementStock(Guid id, [FromQuery] int quantity)
    {
        await _service.UpdateStockAsync(id, quantity);
        return NoContent();
    }

    // Endpoint de test pour le middleware d'erreur global
    [HttpGet("throw")]
    public IActionResult ThrowError()
        => throw new Exception("Test exception — global error handler active.");
}
