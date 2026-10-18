using DotnetApiBatch.Data.Customers;

namespace DotnetApiBatch.Data.Orders;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public DateTime OrderedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
