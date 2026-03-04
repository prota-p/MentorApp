using System.ComponentModel.DataAnnotations;
using MentorApp.Infrastructure.Authentication.Providers.EntraId;
using MentorApp.Infrastructure.Authentication.Providers.Google;

namespace MentorApp.Infrastructure.Authentication;

internal class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    [Required(ErrorMessage = "認証プロバイダーは必須です。")]
    [EnumDataType(typeof(AuthProviderType), ErrorMessage = "無効な認証プロバイダーが指定されています。")]
    public string Provider { get; init; } = null!;

    public InitialAdminOptions InitialAdmin { get; init; } = new();
    public AuthProvidersOptions Providers { get; init; } = new();
}

internal enum AuthProviderType
{
    Mock,
    EntraId,
    Google
}

internal class AuthProvidersOptions
{
    public EntraIdProviderOptions EntraId { get; init; } = new();
    public GoogleProviderOptions Google { get; init; } = new();
}

internal class InitialAdminOptions
{
    public string? ExternalId { get; init; }
    public string DisplayName { get; init; } = "初期管理者";

    [EmailAddress(ErrorMessage = "有効なメールアドレスを指定してください。")]
    public string? Email { get; init; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ExternalId);
}
