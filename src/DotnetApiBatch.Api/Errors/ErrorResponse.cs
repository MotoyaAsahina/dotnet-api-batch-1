using System.Diagnostics;
using System.Text.Json.Serialization;

namespace DotnetApiBatch.Api.Errors;

/// <summary>
/// API のエラーレスポンス。本番・非本番とも同じ形で返し、スタックトレースなどの内部情報は含めない。
/// </summary>
/// <param name="TraceId">ログと突き合わせるための ID。</param>
public record ErrorResponse(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ErrorDetail>? Details,
    string TraceId)
{
    public static ErrorResponse Create(
        HttpContext httpContext,
        ErrorDefinition definition,
        string? message = null,
        IReadOnlyList<ErrorDetail>? details = null) =>
        new(
            definition.Code,
            message ?? definition.Message,
            details,
            Activity.Current?.Id ?? httpContext.TraceIdentifier);
}

/// <summary>
/// 項目ごとのエラー。Field は JSON の名前（items[0].quantity など）。
/// リクエストの本文全体に対するエラー（JSON が壊れているなど）では Field を省略する。
/// </summary>
public record ErrorDetail(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Field,
    string Message);
