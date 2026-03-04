using System.ComponentModel.DataAnnotations;

namespace MentorApp.Infrastructure.Authentication.Providers.Google;

/// <summary>
/// Google 認証の設定オプション
/// </summary>
internal class GoogleProviderOptions
{
    /// <summary>
    /// Google OAuth クライアントID
    /// </summary>
    [Required(ErrorMessage = "Authentication:Providers:Google:ClientId は必須です。")]
    public string ClientId { get; init; } = null!;

    /// <summary>
    /// Google OAuth クライアントシークレット
    /// </summary>
    [Required(ErrorMessage = "Authentication:Providers:Google:ClientSecret は必須です。")]
    public string ClientSecret { get; init; } = null!;

    /// <summary>
    /// Authority URL（Google は固定値）
    /// </summary>
    /// <remarks>
    /// EntraId との構造的一貫性のためにプロパティとして公開。
    /// Google の OIDC Authority は固定のため、読み取り専用。
    /// </remarks>
    public string Authority => "https://accounts.google.com";
}
