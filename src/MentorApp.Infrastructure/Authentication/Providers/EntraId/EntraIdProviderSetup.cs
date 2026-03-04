using System.Security.Claims;
using MentorApp.Infrastructure.Authentication.Providers.Shared;

namespace MentorApp.Infrastructure.Authentication.Providers.EntraId;

/// <summary>
/// Entra ID (旧 Azure AD) 認証プロバイダーの設定
/// </summary>
internal class EntraIdProviderSetup(EntraIdProviderOptions options)
    : OidcProviderSetupBase(options.Authority, options.ClientId, options.ClientSecret)
{
    public override IdentityClaimMappings ClaimMappings => new()
    {
        // 完全形を優先、短縮形をフォールバック
        ExternalIdClaims =
        [
            "http://schemas.microsoft.com/identity/claims/objectidentifier",
            "oid"
        ],
        DisplayNameClaims = ["name", ClaimTypes.Name],
        // email → preferred_username → ClaimTypes.Email の順
        EmailClaims = ["email", "preferred_username", ClaimTypes.Email]
    };
}
