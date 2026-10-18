using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace DotnetApiBatch.Api.Errors;

public static class ErrorHandlingExtensions
{
    /// <summary>
    /// エラーレスポンスを <see cref="ErrorResponse"/> の形にそろえるための設定を登録する。
    /// </summary>
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<AppExceptionHandler>();

        services.Configure<MvcOptions>(options =>
        {
            // 検証エラーの項目名を C# のプロパティ名（Items）ではなく JSON の名前（items）にする
            options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());

            // null 非許容の引数・プロパティに暗黙で付く必須チェックを外す。
            // JSON が壊れているときに "The request field is required." が重ねて出るのを防ぐ。
            // 本文が空のリクエストは、引数が null 非許容なので引き続き 400 になる
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

            // モデルバインディングのエラーメッセージ（既定は英語）
            var messages = options.ModelBindingMessageProvider;
            messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"'{value}' は {field} の値として正しくありません。");
            messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"'{value}' は正しい値ではありません。");
            messages.SetValueIsInvalidAccessor(value => $"'{value}' は正しい値ではありません。");
            messages.SetUnknownValueIsInvalidAccessor(field => $"{field} の値が正しくありません。");
            messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "値が正しくありません。");
            messages.SetValueMustBeANumberAccessor(field => $"{field} には数値を指定してください。");
            messages.SetNonPropertyValueMustBeANumberAccessor(() => "数値を指定してください。");
            messages.SetValueMustNotBeNullAccessor(value => $"'{value}' は指定できません。");
            messages.SetMissingBindRequiredValueAccessor(field => $"{field} を指定してください。");
            messages.SetMissingKeyOrValueAccessor(() => "値を指定してください。");
            messages.SetMissingRequestBodyRequiredValueAccessor(() => "リクエストの本文を指定してください。");
        });

        // JSON の読み取りエラーで、.NET の型名などを含む例外メッセージを返さない
        services.Configure<JsonOptions>(options => options.AllowInputFormatterExceptionMessages = false);

        services.Configure<ApiBehaviorOptions>(options =>
        {
            // NotFound() などを ProblemDetails に変換しない（UseStatusCodePages で ErrorResponse にする）
            options.SuppressMapClientErrors = true;

            // モデル検証エラー（[ApiController] の自動 400）
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .SelectMany(entry => entry.Value!.Errors.Select(error => new ErrorDetail(
                        ToFieldName(entry.Key),
                        string.IsNullOrEmpty(error.ErrorMessage) ? "値の形式が正しくありません。" : error.ErrorMessage)))
                    .ToList();

                return new BadRequestObjectResult(
                    ErrorResponse.Create(context.HttpContext, ErrorDefinitions.Validation, details: details));
            };
        });

        return services;
    }

    /// <summary>
    /// 例外と、本文のないエラー（ルートに一致しない 404、405 など）を <see cref="ErrorResponse"/> で返す。
    /// 本番・非本番とも同じ動きにするため、開発者向けの例外ページより先に例外を処理する。
    /// </summary>
    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        // 例外は AppExceptionHandler がすべて処理するので、ここでの追加処理はない
        app.UseExceptionHandler(_ => { });

        app.UseStatusCodePages(async context =>
        {
            var httpContext = context.HttpContext;
            var definition = ErrorDefinitions.FromStatusCode(httpContext.Response.StatusCode);
            await httpContext.Response.WriteAsJsonAsync(ErrorResponse.Create(httpContext, definition));
        });

        return app;
    }

    /// <summary>
    /// ModelState のキーを、レスポンスの項目名にそろえる。
    /// JSON の読み取りエラーは "$.customerId" のような JSON Path になるので先頭の "$." を外し、
    /// 本文全体のエラー（"$" や ""）は項目名なしにする。
    /// </summary>
    private static string? ToFieldName(string key) => key switch
    {
        "" or "$" => null,
        _ when key.StartsWith("$.") => key[2..],
        _ => key,
    };
}
