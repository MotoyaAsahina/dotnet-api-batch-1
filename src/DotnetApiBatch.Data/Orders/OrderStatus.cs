namespace DotnetApiBatch.Data.Orders;

/// <summary>
/// DB には小文字の文字列（pending / shipped / cancelled）で保存する。
/// </summary>
public enum OrderStatus
{
    Pending,
    Shipped,
    Cancelled,
}
