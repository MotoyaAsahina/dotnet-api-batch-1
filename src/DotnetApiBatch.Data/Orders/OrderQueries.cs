namespace DotnetApiBatch.Data.Orders;

/// <summary>
/// API / Batch のリポジトリで共通して使う、注文の絞り込み条件。
/// </summary>
public static class OrderQueries
{
    public static IQueryable<Order> WithStatus(this IQueryable<Order> query, OrderStatus status) =>
        query.Where(o => o.Status == status);

    public static IQueryable<Order> OrderedBefore(this IQueryable<Order> query, DateTime threshold) =>
        query.Where(o => o.OrderedAt < threshold);
}
