using DotnetApiBatch.Data;
using DotnetApiBatch.Data.Orders;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiBatch.Api.Features.Orders;

/// <summary>
/// API 固有の注文リポジトリ。画面表示用に、レスポンスの形へ直接射影して取得する。
/// </summary>
public class OrderRepository(AppDbContext db)
{
    public Task<OrderDetailResponse?> GetDetailAsync(long id, CancellationToken cancellationToken) =>
        db.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderDetailResponse(
                o.Id,
                o.CustomerId,
                o.Customer.Name,
                o.Status,
                o.OrderedAt,
                o.UpdatedAt,
                o.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new OrderItemResponse(i.ProductId, i.Product.Name, i.Quantity, i.UnitPrice))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderSummaryResponse>> ListByCustomerAsync(
        long customerId, OrderStatus? status, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.CustomerId == customerId);
        if (status is { } s)
        {
            query = query.WithStatus(s);
        }

        return await query
            .OrderByDescending(o => o.OrderedAt)
            .Select(o => new OrderSummaryResponse(
                o.Id,
                o.Status,
                o.OrderedAt,
                o.Items.Sum(i => i.Quantity * i.UnitPrice)))
            .ToListAsync(cancellationToken);
    }

    public void Add(Order order) => db.Orders.Add(order);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
