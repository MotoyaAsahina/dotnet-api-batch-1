using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace DotnetApiBatch.Data;

public static class ServiceCollectionExtensions
{
    public const string ConnectionStringName = "AppDb";

    /// <summary>
    /// API / Batch 共通の設定（DbContext、TimeProvider）を登録する。
    /// </summary>
    /// <param name="configureNpgsql">
    /// アプリごとに変えたい Npgsql の設定（コマンドタイムアウトなど）。
    /// </param>
    public static IServiceCollection AddData(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"接続文字列 '{ConnectionStringName}' が設定されていません。");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure();
                configureNpgsql?.Invoke(npgsql);
            })
            .UseSnakeCaseNamingConvention());

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
