using MentorApp.Domain.Models.Shared;

namespace MentorApp.Domain.Models.Users;

/// <summary>
/// ユーザーエンティティ（集約根）
/// </summary>
/// <remarks>
/// Mentorship や Topic から参照されるが、それらのライフサイクルとは独立。
/// </remarks>
public class User
{
    public const int ExternalIdMaxLength = 255;
    public const int DisplayNameMaxLength = 100;

    public Guid Id { get; private set; }

    /// <summary>
    /// IdP の sub クレーム。IdP 切り替え時もこの値で紐付けを維持する。
    /// </summary>
    public string ExternalId { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public Email Email { get; private set; } = null!;

    public Role Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core 用
    private User() { }

    public User(string? externalId, string? displayName, DateTimeOffset createdAt, string? email, Role role = Role.Mentee)
    {
        Validate(externalId, displayName, email).ThrowIfInvalid();

        Id = Guid.NewGuid();
        ExternalId = externalId!;
        DisplayName = displayName!;
        Email = new Email(email);
        Role = role;
        CreatedAt = createdAt;
    }

    public static IEnumerable<ValidationError> Validate(
        string? externalId, string? displayName, string? email)
    {
        return ValidateExternalId(externalId).ToValidationErrors(nameof(ExternalId))
            .Concat(ValidateDisplayName(displayName).ToValidationErrors(nameof(DisplayName)))
            .Concat(Email.Validate(email).ToValidationErrors(nameof(Email)));
    }

    public static IEnumerable<string> ValidateExternalId(string? externalId)
        => ValidationHelper.ValidateRequiredMaxLength(externalId, ExternalIdMaxLength, "ExternalId");

    public static IEnumerable<string> ValidateDisplayName(string? displayName)
        => ValidationHelper.ValidateRequiredMaxLength(displayName, DisplayNameMaxLength, "表示名");

    public void UpdateDisplayName(string? displayName)
    {
        ValidateDisplayName(displayName).ToValidationErrors(nameof(DisplayName)).ThrowIfInvalid();
        DisplayName = displayName!;
    }

    public void UpdateEmail(string email) => Email = new Email(email);

    /// <remarks>認可チェックはアプリケーション層（UserService）で行うため、このメソッド自体は認可を持たない。</remarks>
    public void ChangeRole(Role newRole) => Role = newRole;
}
