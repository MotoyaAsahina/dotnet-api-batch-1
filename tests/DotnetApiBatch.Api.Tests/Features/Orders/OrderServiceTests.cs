using DotnetApiBatch.Api.Errors;
using DotnetApiBatch.Api.Features.Catalog;
using DotnetApiBatch.Api.Features.Customers;
using DotnetApiBatch.Api.Features.Orders;
using DotnetApiBatch.Api.Persistence;
using DotnetApiBatch.Data.Catalog;
using DotnetApiBatch.Data.Orders;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace DotnetApiBatch.Api.Tests.Features.Orders;

/// <summary>
/// OrderService の単体テスト。リポジトリはモックに差し替え、DB を使わずに業務ロジックだけを確認する。
/// </summary>
public class OrderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 30, 0, TimeSpan.Zero);

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_customers, _products, _orders, _unitOfWork, new FakeTimeProvider(Now));
    }

    // --- GetDetailAsync ---

    [Fact]
    public async Task GetDetailAsync_returns_order_from_repository()
    {
        var detail = new OrderDetailResponse(1, 10, "山田 太郎", OrderStatus.Pending, Now.UtcDateTime, Now.UtcDateTime, []);
        _orders.GetDetailAsync(1, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await _sut.GetDetailAsync(1, CancellationToken.None);

        Assert.Same(detail, result);
    }

    [Fact]
    public async Task GetDetailAsync_throws_not_found_when_order_does_not_exist()
    {
        _orders.GetDetailAsync(1, Arg.Any<CancellationToken>()).Returns((OrderDetailResponse?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetDetailAsync(1, CancellationToken.None));

        Assert.Equal("注文 1 が見つかりません。", ex.Message);
    }

    // --- ListByCustomerAsync ---

    [Fact]
    public async Task ListByCustomerAsync_passes_status_to_repository()
    {
        IReadOnlyList<OrderSummaryResponse> summaries =
            [new OrderSummaryResponse(1, OrderStatus.Shipped, Now.UtcDateTime, 1200m)];
        _customers.ExistsAsync(10, Arg.Any<CancellationToken>()).Returns(true);
        _orders.ListByCustomerAsync(10, OrderStatus.Shipped, Arg.Any<CancellationToken>()).Returns(summaries);

        var result = await _sut.ListByCustomerAsync(10, OrderStatus.Shipped, CancellationToken.None);

        Assert.Same(summaries, result);
    }

    [Fact]
    public async Task ListByCustomerAsync_throws_not_found_when_customer_does_not_exist()
    {
        _customers.ExistsAsync(10, Arg.Any<CancellationToken>()).Returns(false);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.ListByCustomerAsync(10, null, CancellationToken.None));

        Assert.Equal("顧客 10 が見つかりません。", ex.Message);
        await _orders.DidNotReceiveWithAnyArgs().ListByCustomerAsync(default, default, default);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_saves_pending_order_with_current_unit_prices()
    {
        _customers.ExistsAsync(10, Arg.Any<CancellationToken>()).Returns(true);
        GivenProducts(new Product { Id = 100, Name = "ノート", Price = 300m }, new Product { Id = 200, Name = "ペン", Price = 150m });
        // DB が採番した ID を、SaveChangesAsync の時点でエンティティに設定する
        Order? added = null;
        _orders.When(o => o.Add(Arg.Any<Order>())).Do(call => added = call.Arg<Order>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            added!.Id = 999;
            return 1;
        });

        var result = await _sut.CreateAsync(
            Request(customerId: 10, (100, 2), (200, 1)), CancellationToken.None);

        Assert.Equal(999, result.Id);
        Assert.NotNull(added);
        Assert.Equal(10, added.CustomerId);
        Assert.Equal(OrderStatus.Pending, added.Status);
        Assert.Equal(Now.UtcDateTime, added.OrderedAt);
        Assert.Equal(Now.UtcDateTime, added.UpdatedAt);
        Assert.Collection(
            added.Items,
            i => Assert.Equal((100L, 2, 300m), (i.ProductId, i.Quantity, i.UnitPrice)),
            i => Assert.Equal((200L, 1, 150m), (i.ProductId, i.Quantity, i.UnitPrice)));
        Received.InOrder(() =>
        {
            _orders.Add(Arg.Any<Order>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task CreateAsync_looks_up_each_product_once_even_if_ordered_twice()
    {
        _customers.ExistsAsync(10, Arg.Any<CancellationToken>()).Returns(true);
        GivenProducts(new Product { Id = 100, Name = "ノート", Price = 300m });
        Order? added = null;
        _orders.When(o => o.Add(Arg.Any<Order>())).Do(call => added = call.Arg<Order>());

        await _sut.CreateAsync(Request(customerId: 10, (100, 1), (100, 3)), CancellationToken.None);

        await _products.Received(1).ListByIdsAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new long[] { 100 })),
            Arg.Any<CancellationToken>());
        Assert.NotNull(added);
        Assert.All(added.Items, i => Assert.Equal(300m, i.UnitPrice));
    }

    [Fact]
    public async Task CreateAsync_throws_validation_error_when_customer_does_not_exist()
    {
        _customers.ExistsAsync(10, Arg.Any<CancellationToken>()).Returns(false);

        var ex = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _sut.CreateAsync(Request(customerId: 10, (100, 1)), CancellationToken.None));

        var detail = Assert.Single(ex.Details!);
        Assert.Equal(new ErrorDetail("customerId", "顧客 10 が存在しません。"), detail);
        _orders.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task CreateAsync_throws_validation_error_listing_missing_products()
    {
        _customers.ExistsAsync(10, Arg.Any<CancellationToken>()).Returns(true);
        GivenProducts(new Product { Id = 100, Name = "ノート", Price = 300m });

        var ex = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _sut.CreateAsync(Request(customerId: 10, (100, 1), (200, 1), (300, 1)), CancellationToken.None));

        var detail = Assert.Single(ex.Details!);
        Assert.Equal(new ErrorDetail("items", "商品 200, 300 が存在しません。"), detail);
        _orders.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private void GivenProducts(params Product[] products) =>
        _products.ListByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(products);

    private static CreateOrderRequest Request(long customerId, params (long ProductId, int Quantity)[] items) =>
        new()
        {
            CustomerId = customerId,
            Items = items.Select(i => new CreateOrderItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList(),
        };
}
