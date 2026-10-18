namespace DotnetApiBatch.Api.Features.Catalog;

public class ProductService(ProductRepository products)
{
    public Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken) =>
        products.ListAsync(cancellationToken);
}
