using DotnetApiBatch.Data;
using DotnetApiBatch.Data.Catalog;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiBatch.Api.Features.Catalog;

public class ProductRepository(AppDbContext db)
{
    public async Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProductResponse(p.Id, p.Name, p.Price))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// 注文作成時に単価を参照するため、指定した ID の商品を取得する。
    /// </summary>
    public async Task<IReadOnlyList<Product>> ListByIdsAsync(
        IReadOnlyCollection<long> ids, CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
}
