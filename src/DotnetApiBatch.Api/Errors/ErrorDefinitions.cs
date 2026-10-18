namespace DotnetApiBatch.Api.Errors;

/// <summary>
/// エラーの種類ごとの HTTP ステータス・コード・既定のメッセージ。
/// </summary>
public record ErrorDefinition(int StatusCode, string Code, string Message);

/// <summary>
/// API が返すエラーの定義。エラーのレスポンスは、すべてここに定義したものから作る。
/// </summary>
public static class ErrorDefinitions
{
    public static readonly ErrorDefinition BadRequest =
        new(StatusCodes.Status400BadRequest, "BAD_REQUEST", "リクエストが正しくありません。");

    public static readonly ErrorDefinition Validation =
        new(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "入力内容に誤りがあります。");

    public static readonly ErrorDefinition NotFound =
        new(StatusCodes.Status404NotFound, "NOT_FOUND", "指定されたリソースが見つかりません。");

    public static readonly ErrorDefinition MethodNotAllowed =
        new(StatusCodes.Status405MethodNotAllowed, "METHOD_NOT_ALLOWED", "このメソッドは使用できません。");

    public static readonly ErrorDefinition Conflict =
        new(StatusCodes.Status409Conflict, "CONFLICT", "データが競合しています。");

    public static readonly ErrorDefinition PayloadTooLarge =
        new(StatusCodes.Status413PayloadTooLarge, "PAYLOAD_TOO_LARGE", "リクエストのサイズが大きすぎます。");

    public static readonly ErrorDefinition UnsupportedMediaType =
        new(StatusCodes.Status415UnsupportedMediaType, "UNSUPPORTED_MEDIA_TYPE", "Content-Type が正しくありません。");

    public static readonly ErrorDefinition InternalServerError =
        new(StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR", "サーバーでエラーが発生しました。");

    public static readonly ErrorDefinition ServiceUnavailable =
        new(StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE", "現在サービスを利用できません。時間をおいて再度お試しください。");

    private static readonly Dictionary<int, ErrorDefinition> ByStatusCode = new[]
    {
        BadRequest, NotFound, MethodNotAllowed, Conflict, PayloadTooLarge, UnsupportedMediaType,
        InternalServerError, ServiceUnavailable,
    }.ToDictionary(d => d.StatusCode);

    /// <summary>
    /// ステータスコードだけが分かっているとき（ルートに一致しない 404 など）に使う定義。
    /// </summary>
    public static ErrorDefinition FromStatusCode(int statusCode) =>
        ByStatusCode.GetValueOrDefault(statusCode)
        ?? (statusCode < 500
            ? new(statusCode, "CLIENT_ERROR", "リクエストを処理できませんでした。")
            : new(statusCode, "SERVER_ERROR", InternalServerError.Message));
}
