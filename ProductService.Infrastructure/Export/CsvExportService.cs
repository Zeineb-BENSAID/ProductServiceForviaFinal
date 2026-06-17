using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using ProductService.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
