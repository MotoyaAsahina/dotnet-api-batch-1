using System.ComponentModel.DataAnnotations;
using DotnetApiBatch.Data.Orders;

namespace DotnetApiBatch.Api.Features.Orders;

public record OrderSummaryResponse(long Id, OrderStatus Status, DateTime OrderedAt, decimal Total);

public record OrderDetailResponse(
    long Id,
    long CustomerId,
    string CustomerName,
    OrderStatus Status,
    DateTime OrderedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public record OrderItemResponse(long ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderRequest
{
    public long CustomerId { get; init; }

    [MinLength(1, ErrorMessage = "注文明細を1件以上指定してください。")]
    public IReadOnlyList<CreateOrderItemRequest> Items { get; init; } = [];
}

public record CreateOrderItemRequest
{
    public long ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "数量は1以上を指定してください。")]
    public int Quantity { get; init; }
}

public record CreateOrderResponse(long Id);
