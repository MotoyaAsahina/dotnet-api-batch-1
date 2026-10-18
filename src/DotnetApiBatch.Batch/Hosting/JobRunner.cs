using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotnetApiBatch.Batch.Hosting;

/// <summary>
/// 指定されたジョブを1回実行し、終わったらアプリを終了させる。
/// 結果は終了コード（成功 0、失敗・キャンセル 1）で返す。
/// </summary>
public class JobRunner(
    string jobName,
    IEnumerable<BatchJobName> jobNames,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<JobRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // ジョブは DbContext などのスコープ付きサービスを使うので、実行ごとにスコープを作る
            await using var scope = scopeFactory.CreateAsyncScope();

            var job = scope.ServiceProvider.GetKeyedService<IBatchJob>(jobName);
            if (job is null)
            {
                Console.Error.WriteLine($"""
                    使い方: DotnetApiBatch.Batch <ジョブ名> [設定の上書き...]
                    ジョブ名: {string.Join(", ", jobNames.Select(n => n.Value))}
                    """);
                Environment.ExitCode = 1;
                return;
            }

            logger.LogInformation("ジョブ {JobName} を開始します", jobName);
            // Ctrl+C や SIGTERM を受けると stoppingToken がキャンセルされる
            await job.RunAsync(stoppingToken);
            logger.LogInformation("ジョブ {JobName} が完了しました", jobName);
            Environment.ExitCode = 0;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("ジョブ {JobName} はキャンセルされました", jobName);
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ジョブ {JobName} が失敗しました", jobName);
            Environment.ExitCode = 1;
        }
        finally
        {
            // 常駐させずに、ジョブが終わったらアプリを終了させる
            lifetime.StopApplication();
        }
    }
}
