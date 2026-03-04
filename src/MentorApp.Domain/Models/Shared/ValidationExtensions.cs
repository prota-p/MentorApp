namespace MentorApp.Domain.Models.Shared;

public static class ValidationExtensions
{
    /// <summary>
    /// エラーメッセージをValidationErrorに変換
    /// </summary>
    public static IEnumerable<ValidationError> ToValidationErrors(
        this IEnumerable<string> messages, string propertyName)
        => messages.Select(m => new ValidationError(propertyName, m));

    /// <summary>
    /// 複数フィールドのバリデーション結果を統合
    /// </summary>
    public static IEnumerable<ValidationError> CombineValidations(
        params (string propertyName, IEnumerable<string> messages)[] validations)
        => validations.SelectMany(v => v.messages.ToValidationErrors(v.propertyName));

    /// <summary>
    /// 検証エラーがある場合に例外をスロー
    /// </summary>
    public static void ThrowIfInvalid(this IEnumerable<ValidationError> errors)
    {
        var errorList = errors.ToList();
        if (errorList.Count > 0)
        {
            var messages = errorList.Select(e => $"{e.Property}: {e.Message}");
            throw new ArgumentException(string.Join(Environment.NewLine, messages));
        }
    }
}
