namespace MentorApp.Domain.Models.Shared;

/// <summary>
/// 共通バリデーションロジック
/// </summary>
internal static class ValidationHelper
{
    public static IEnumerable<string> ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [$"{fieldName}は必須です。"];
        return [];
    }

    public static IEnumerable<string> ValidateMaxLength(string? value, int maxLength, string fieldName)
    {
        if (value is not null && value.Length > maxLength)
            return [$"{fieldName}は{maxLength}文字以内で入力してください。"];
        return [];
    }

    public static IEnumerable<string> ValidateRequiredMaxLength(
        string? value, int maxLength, string fieldName)
    {
        var requiredErrors = ValidateRequired(value, fieldName).ToList();
        if (requiredErrors.Count > 0) return requiredErrors;
        return ValidateMaxLength(value, maxLength, fieldName);
    }

    /// <summary>
    /// メールアドレスチェック（簡易版）
    /// </summary>
    /// <remarks>
    /// IdP から取得するため最低限のチェックのみ実施。
    /// </remarks>
    public static IEnumerable<string> ValidateEmail(string? value, int maxLength, string fieldName)
    {
        var requiredErrors = ValidateRequired(value, fieldName).ToList();
        if (requiredErrors.Count > 0)
        {
            foreach (var error in requiredErrors)
                yield return error;
            yield break;
        }

        var trimmed = value!.Trim();

        if (!trimmed.Contains('@'))
            yield return $"{fieldName}には @ が必要です。";

        if (trimmed.Length < 5)
            yield return $"{fieldName}が短すぎます。";

        if (trimmed.Length > maxLength)
            yield return $"{fieldName}は{maxLength}文字以内で入力してください。";
    }
}
