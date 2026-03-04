using System.ComponentModel.DataAnnotations;

namespace MentorApp.Infrastructure.Authentication.Providers.EntraId;

/// <summary>
/// Entra ID (旧 Azure AD) の設定オプション
/// </summary>
internal class EntraIdProviderOptions
{
    /// <summary>
    /// Entra ID テナントID
    /// </summary>
    [Required(ErrorMessage = "Authentication:Providers:EntraId:TenantId は必須です。")]
    public string TenantId { get; init; } = null!;

    /// <summary>
    /// アプリケーション（クライアント）ID
    /// </summary>
    [Required(ErrorMessage = "Authentication:Providers:EntraId:ClientId は必須です。")]
    public string ClientId { get; init; } = null!;

    /// <summary>
    /// クライアントシークレット
    /// </summary>
    [Required(ErrorMessage = "Authentication:Providers:EntraId:ClientSecret は必須です。")]
    public string ClientSecret { get; init; } = null!;

    /// <summary>
    /// Authority URL を取得
    /// </summary>
    public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";
}
