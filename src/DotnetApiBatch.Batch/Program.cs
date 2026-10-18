using DotnetApiBatch.Batch.Hosting;
using DotnetApiBatch.Batch.Jobs;
using DotnetApiBatch.Batch.Repositories;
using DotnetApiBatch.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// 1つ目の引数でジョブを選び、残りは設定の上書きに使う（例: --Jobs:CancelExpiredOrders:ExpireAfterDays 7）
var jobName = args.FirstOrDefault() ?? string.Empty;
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args.Skip(1).ToArray(),
    // どこから実行しても appsettings.json を読めるよう、実行ファイルの場所を基準にする
    ContentRootPath = AppContext.BaseDirectory,
});

// 共通設定（DbContext）。バッチは大量データを扱うのでタイムアウトを長めにする
builder.Services.AddData(builder.Configuration, npgsql => npgsql.CommandTimeout(300));
// Batch のリポジトリ
builder.Services.AddScoped<OrderRepository>();

// ジョブ
builder.Services.Configure<CancelExpiredOrdersOptions>(
    builder.Configuration.GetSection(CancelExpiredOrdersOptions.SectionName));
builder.Services.AddBatchJob<CancelExpiredOrdersJob>(CancelExpiredOrdersJob.Name);
builder.Services.AddBatchJob<SalesReportJob>(SalesReportJob.Name);

// 指定されたジョブを実行する BackgroundService
builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<JobRunner>(sp, jobName));

using var host = builder.Build();
await host.RunAsync();

// 終了コードは JobRunner が Environment.ExitCode に設定する
