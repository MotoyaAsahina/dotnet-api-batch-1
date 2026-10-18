namespace DotnetApiBatch.Data.Customers;

/// <remarks>
/// 注文（Orders）への参照は持たない。ドメインの依存は Orders → Customers の一方向にする。
/// 顧客の注文は Orders 側から CustomerId で絞り込む。
/// </remarks>
public class Customer
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
