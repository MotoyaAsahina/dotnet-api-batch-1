using Microsoft.Extensions.DependencyInjection;

namespace DotnetApiBatch.Batch.Hosting;

/// <summary>
/// 登録されたジョブの名前。使い方の表示でジョブの一覧を出すために使う。
/// </summary>
public record BatchJobName(string Value);

public static class BatchJobServiceCollectionExtensions
{
    /// <summary>
    /// ジョブを、名前をキーにした keyed service として登録する。
    /// </summary>
    public static IServiceCollection AddBatchJob<TJob>(this IServiceCollection services, string name)
        where TJob : class, IBatchJob
    {
        services.AddKeyedScoped<IBatchJob, TJob>(name);
        services.AddSingleton(new BatchJobName(name));
        return services;
    }
}
