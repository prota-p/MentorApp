using System.Security.Claims;

namespace MentorApp.Infrastructure.Authentication.Providers.Shared;

/// <summary>
/// クレームマッピング定義
/// </summary>
/// <remarks>
/// 各認証プロバイダーが使用するクレームタイプは異なるため、
/// このレコードでマッピングを定義し、ClaimMappingIdentityResolver で統一的に処理する。
/// 具体的なマッピング値は各プロバイダーの ProviderSetup クラスで定義する。
/// </remarks>
internal record IdentityClaimMappings
{
    /// <summary>
    /// 外部ID として使用するクレームタイプ（優先度順）
    /// </summary>
    public required string[] ExternalIdClaims { get; init; }

    /// <summary>
    /// 表示名として使用するクレームタイプ（優先度順）
    /// </summary>
    public required string[] DisplayNameClaims { get; init; }

    /// <summary>
    /// メールアドレスとして使用するクレームタイプ（優先度順）
    /// </summary>
    public required string[] EmailClaims { get; init; }
}

/// <summary>
/// 外部認証プロバイダーから抽出したユーザー識別情報
/// </summary>
internal record ExternalIdentity(string ExternalId, string? DisplayName, string Email);

/// <summary>
/// クレームマッピングに基づいて識別情報を抽出するリゾルバー
/// </summary>
/// <remarks>
/// 認証プロバイダーごとにクレームの形式が異なるため、
/// IdentityClaimMappings の設定に基づいて統一的に処理する。
/// </remarks>
internal class ClaimMappingIdentityResolver(IdentityClaimMappings mappings)
{
    public ExternalIdentity? ResolveIdentity(ClaimsPrincipal principal)
    {
        var externalId = FindFirstClaim(principal, mappings.ExternalIdClaims);
        if (string.IsNullOrEmpty(externalId))
            return null;

        var displayName = FindFirstClaim(principal, mappings.DisplayNameClaims);

        var email = FindFirstClaim(principal, mappings.EmailClaims);
        if (string.IsNullOrEmpty(email))
            return null;

        return new ExternalIdentity(externalId, displayName, email);
    }

    private static string? FindFirstClaim(ClaimsPrincipal principal, string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        return null;
    }
}
