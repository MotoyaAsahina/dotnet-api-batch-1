namespace DotnetApiBatch.Api.Features.Catalog;

public class ProductService(IProductRepository products)
{
    public Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken) =>
        products.ListAsync(cancellationToken);
}
