using DotnetApiBatch.Api.Features.Orders;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApiBatch.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDetailResponse>> Get(long id, CancellationToken cancellationToken) =>
        Ok(await orderService.GetDetailAsync(id, cancellationToken));

    /// <remarks>
    /// リクエストの形式（明細の件数・数量）は DataAnnotations で検証され、不正なら [ApiController] が自動で 400 を返す。
    /// 顧客・商品が存在するかは OrderService で検証する。
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> Create(
        CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var created = await orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}
