using DotnetApiBatch.Batch.Hosting;
using DotnetApiBatch.Batch.Repositories;
using Microsoft.Extensions.Logging;

namespace DotnetApiBatch.Batch.Jobs;

/// <summary>
/// 出荷済みの注文から、商品ごとの売上を集計して出力する。
/// </summary>
public class SalesReportJob(OrderRepository orders, ILogger<SalesReportJob> logger) : IBatchJob
{
    public const string Name = "sales-report";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var sales = await orders.SumShippedSalesByProductAsync(cancellationToken);

        foreach (var s in sales)
        {
            logger.LogInformation(
                "{ProductId} {ProductName}: {Quantity} 個 / {Amount:N0} 円",
                s.ProductId, s.ProductName, s.Quantity, s.Amount);
        }

        logger.LogInformation("合計: {Total:N0} 円", sales.Sum(s => s.Amount));
    }
}
