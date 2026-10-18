using DotnetApiBatch.Api.Features.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApiBatch.Api.Controllers;

[ApiController]
[Route("products")]
public class ProductsController(ProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await productService.ListAsync(cancellationToken));
}
