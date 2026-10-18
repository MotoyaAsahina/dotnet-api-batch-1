namespace DotnetApiBatch.Data.Catalog;

public class Product
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
