using MentorApp.Domain.Models.Shared;

namespace MentorApp.Domain.Models.Users;

/// <summary>
/// メールアドレス値オブジェクト
/// </summary>
/// <remarks>
/// IdP から取得するため最低限のチェックのみ実施。
/// </remarks>
public sealed record Email
{
    /// <summary>
    /// メールアドレスの最大長（RFC 5321準拠）
    /// </summary>
    public const int MaxLength = 254;

    public string Value { get; }

    public Email(string? value)
    {
        Validate(value).ToValidationErrors(nameof(Value)).ThrowIfInvalid();
        Value = value!.Trim();
    }

    public static IEnumerable<string> Validate(string? value)
    {
        return ValidationHelper.ValidateEmail(value, MaxLength, "メールアドレス");
    }

    public override string ToString() => Value;
}
