using System.Security.Claims;
using MentorApp.Infrastructure.Authentication.Providers.Shared;

namespace MentorApp.Infrastructure.Authentication.Providers.Google;

/// <summary>
/// Google 認証プロバイダーの設定
/// </summary>
internal class GoogleProviderSetup(GoogleProviderOptions options)
    : OidcProviderSetupBase(options.Authority, options.ClientId, options.ClientSecret)
{
    public override IdentityClaimMappings ClaimMappings => new()
    {
        // sub クレームを優先、ClaimTypes.NameIdentifier をフォールバック
        ExternalIdClaims = ["sub", ClaimTypes.NameIdentifier],
        DisplayNameClaims = ["name", ClaimTypes.Name],
        EmailClaims = ["email", ClaimTypes.Email]
    };
}
