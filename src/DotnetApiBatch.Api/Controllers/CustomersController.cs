using DotnetApiBatch.Api.Features.Orders;
using DotnetApiBatch.Data.Orders;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApiBatch.Api.Controllers;

[ApiController]
[Route("customers")]
public class CustomersController(OrderService orderService) : ControllerBase
{
    /// <param name="status">pending / shipped / cancelled（大文字小文字は区別しない）</param>
    [HttpGet("{id:long}/orders")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> ListOrders(
        long id, [FromQuery] OrderStatus? status, CancellationToken cancellationToken) =>
        Ok(await orderService.ListByCustomerAsync(id, status, cancellationToken));
}
