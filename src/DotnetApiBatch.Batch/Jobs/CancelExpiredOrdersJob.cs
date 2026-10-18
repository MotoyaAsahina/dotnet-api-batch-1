using DotnetApiBatch.Batch.Hosting;
using DotnetApiBatch.Batch.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetApiBatch.Batch.Jobs;

public class CancelExpiredOrdersOptions
{
    public const string SectionName = "Jobs:CancelExpiredOrders";

    /// <summary>
    /// 注文からこの日数を過ぎても pending のままの注文をキャンセルする。
    /// </summary>
    public int ExpireAfterDays { get; set; } = 3;
}

/// <summary>
/// 一定期間 pending のままの注文をキャンセルする。
/// </summary>
public class CancelExpiredOrdersJob(
    OrderRepository orders,
    IOptions<CancelExpiredOrdersOptions> options,
    TimeProvider timeProvider,
    ILogger<CancelExpiredOrdersJob> logger) : IBatchJob
{
    public const string Name = "cancel-expired-orders";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var threshold = now.AddDays(-options.Value.ExpireAfterDays);

        var count = await orders.CancelPendingOrderedBeforeAsync(threshold, now, cancellationToken);

        logger.LogInformation(
            "{Threshold:u} より前の pending の注文を {Count} 件キャンセルしました", threshold, count);
    }
}
