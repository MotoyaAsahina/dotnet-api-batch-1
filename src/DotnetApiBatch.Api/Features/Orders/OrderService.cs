using DotnetApiBatch.Api.Errors;
using DotnetApiBatch.Api.Features.Catalog;
using DotnetApiBatch.Api.Features.Customers;
using DotnetApiBatch.Data.Orders;

namespace DotnetApiBatch.Api.Features.Orders;

/// <remarks>
/// 業務上のエラーは <see cref="AppException"/> を投げ、<see cref="AppExceptionHandler"/> がレスポンスに変換する。
/// </remarks>
public class OrderService(
    CustomerRepository customers,
    ProductRepository products,
    OrderRepository orders,
    TimeProvider timeProvider)
{
    public async Task<OrderDetailResponse> GetDetailAsync(long id, CancellationToken cancellationToken) =>
        await orders.GetDetailAsync(id, cancellationToken)
        ?? throw new NotFoundException("注文", id);

    public async Task<IReadOnlyList<OrderSummaryResponse>> ListByCustomerAsync(
        long customerId, OrderStatus? status, CancellationToken cancellationToken)
    {
        if (!await customers.ExistsAsync(customerId, cancellationToken))
        {
            throw new NotFoundException("顧客", customerId);
        }

        return await orders.ListByCustomerAsync(customerId, status, cancellationToken);
    }

    public async Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await customers.ExistsAsync(request.CustomerId, cancellationToken))
        {
            throw new ValidationFailedException("customerId", $"顧客 {request.CustomerId} が存在しません。");
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToArray();
        var found = (await products.ListByIdsAsync(productIds, cancellationToken)).ToDictionary(p => p.Id);
        var missing = productIds.Where(id => !found.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
        {
            throw new ValidationFailedException("items", $"商品 {string.Join(", ", missing)} が存在しません。");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            OrderedAt = now,
            UpdatedAt = now,
            Items = request.Items
                .Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    // 注文時点の単価を明細に残す
                    UnitPrice = found[i.ProductId].Price,
                })
                .ToList(),
        };

        orders.Add(order);
        await orders.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse(order.Id);
    }
}
