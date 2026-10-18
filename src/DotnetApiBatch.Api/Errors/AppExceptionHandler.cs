using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace DotnetApiBatch.Api.Errors;

/// <summary>
/// 処理されなかった例外を、例外の種類に応じたエラーレスポンスに変換する。
/// どの例外もここで処理するので、スタックトレースはレスポンスに含まれず、ログにだけ出る。
/// </summary>
public class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // クライアントが接続を切った場合は、返す相手がいないのでレスポンスを書かない
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("クライアントがリクエストを中断しました");
            httpContext.Response.StatusCode = 499;
            return true;
        }

        var (definition, message, details) = Map(exception);

        switch (definition.StatusCode)
        {
            case >= 500 when definition == ErrorDefinitions.ServiceUnavailable:
                logger.LogWarning(exception, "DB に接続できないため、処理を中断しました");
                break;
            case >= 500:
                logger.LogError(exception, "処理中に予期しないエラーが発生しました");
                break;
            default:
                logger.LogInformation("{Code}: {Message}", definition.Code, message ?? definition.Message);
                break;
        }

        httpContext.Response.StatusCode = definition.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            ErrorResponse.Create(httpContext, definition, message, details), cancellationToken);
        return true;
    }

    /// <summary>
    /// 例外とエラーの定義の対応。特定の例外で返すエラーを変えたいときはここに追加する。
    /// </summary>
    private static (ErrorDefinition Definition, string? Message, IReadOnlyList<ErrorDetail>? Details) Map(
        Exception exception) => exception switch
    {
        // アプリで定義した例外
        AppException e => (e.Definition, e.Message, e.Details),

        // リクエストの読み取りに失敗した（本文が大きすぎるなど）
        BadHttpRequestException e => (ErrorDefinitions.FromStatusCode(e.StatusCode), null, null),

        // 一意制約の違反
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            (ErrorDefinitions.Conflict, "既に登録されています。", null),

        // 楽観的同時実行制御で、他の更新と競合した
        DbUpdateConcurrencyException => (ErrorDefinitions.Conflict, null, null),

        // DB に接続できない・応答がない（リトライしても回復しなかった）
        RetryLimitExceededException or NpgsqlException { IsTransient: true } or TimeoutException =>
            (ErrorDefinitions.ServiceUnavailable, null, null),

        // それ以外は、詳細を返さずに 500 にする
        _ => (ErrorDefinitions.InternalServerError, null, null),
    };
}
