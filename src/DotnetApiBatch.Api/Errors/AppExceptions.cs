namespace DotnetApiBatch.Api.Errors;

/// <summary>
/// アプリで定義した例外の基底クラス。<see cref="AppExceptionHandler"/> が、
/// <see cref="Definition"/> に従ってエラーレスポンスに変換する。
/// </summary>
public abstract class AppException(
    ErrorDefinition definition,
    string? message = null,
    IReadOnlyList<ErrorDetail>? details = null)
    : Exception(message ?? definition.Message)
{
    public ErrorDefinition Definition { get; } = definition;
    public IReadOnlyList<ErrorDetail>? Details { get; } = details;
}

/// <summary>
/// 指定されたリソースが存在しない（404）。
/// </summary>
public class NotFoundException(string resourceName, object id)
    : AppException(ErrorDefinitions.NotFound, $"{resourceName} {id} が見つかりません。");

/// <summary>
/// 業務上の検証エラー（400）。リクエストの形式チェックは DataAnnotations で行い、
/// 存在チェックなど DB を見ないと分からないものに使う。
/// </summary>
public class ValidationFailedException(string field, string message)
    : AppException(ErrorDefinitions.Validation, details: [new ErrorDetail(field, message)]);
