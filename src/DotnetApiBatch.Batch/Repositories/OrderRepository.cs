using DotnetApiBatch.Data;
using DotnetApiBatch.Data.Orders;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiBatch.Batch.Repositories;

public record ProductSales(long ProductId, string ProductName, int Quantity, decimal Amount);

/// <summary>
/// Batch 固有の注文リポジトリ。エンティティを読み込まずに、まとめて更新・集計する。
/// </summary>
public class OrderRepository(AppDbContext db)
{
    public Task<int> CancelPendingOrderedBeforeAsync(
        DateTime threshold, DateTime now, CancellationToken cancellationToken) =>
        db.Orders.WithStatus(OrderStatus.Pending)
            .OrderedBefore(threshold)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatus.Cancelled)
                .SetProperty(o => o.UpdatedAt, now),
                cancellationToken);

    public async Task<IReadOnlyList<ProductSales>> SumShippedSalesByProductAsync(
        CancellationToken cancellationToken) =>
        await db.Orders.WithStatus(OrderStatus.Shipped)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.Product.Name })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Quantity * i.UnitPrice),
            })
            // record のコンストラクターで射影した後のプロパティでは並べ替えを SQL に変換できないので、先に並べ替える
            .OrderByDescending(s => s.Amount)
            .Select(s => new ProductSales(s.ProductId, s.Name, s.Quantity, s.Amount))
            .ToListAsync(cancellationToken);
}
