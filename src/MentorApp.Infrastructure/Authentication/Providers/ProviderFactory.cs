using MentorApp.Infrastructure.Authentication.Providers.EntraId;
using MentorApp.Infrastructure.Authentication.Providers.Google;
using MentorApp.Infrastructure.Authentication.Providers.Mock;
using MentorApp.Infrastructure.Authentication.Providers.Shared;

namespace MentorApp.Infrastructure.Authentication.Providers;

/// <summary>
/// 認証プロバイダー設定のファクトリ
/// </summary>
/// <remarks>
/// AuthProviderType に応じた IAuthenticationProviderSetup 実装を生成する。
/// プロバイダ種別による分岐をこのファクトリに集約し、他の箇所での分岐を排除する。
/// </remarks>
internal static class ProviderFactory
{
    public static IProviderSetup Create(
        AuthProviderType providerType,
        AuthProvidersOptions providers)
    {
        return providerType switch
        {
            AuthProviderType.Mock => new MockProviderSetup(),
            AuthProviderType.EntraId => new EntraIdProviderSetup(providers.EntraId),
            AuthProviderType.Google => new GoogleProviderSetup(providers.Google),
            _ => throw new InvalidOperationException($"未対応の認証プロバイダー: {providerType}")
        };
    }
}
