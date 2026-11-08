using System.Security.Claims;
using MentorApp.Application.Contracts.Authentication;
using MentorApp.Infrastructure.Authentication.Providers.Shared;
using Microsoft.AspNetCore.Authentication;

namespace MentorApp.Infrastructure.Authentication;

/// <summary>
/// 外部認証プロバイダ（Microsoft Entra ID 等）のクレームをアプリ固有のクレームに変換する。
/// </summary>
/// <remarks>
/// 【クレームの変換】
/// principal には外部プロバイダのクレームが既にある。
/// そこから3項目（外部ID・表示名・メール）を使い、DB のユーザー情報を5項目のアプリ固有クレームとして追加する。
/// 初回はユーザーを DB に新規登録し、以降は DB から取得する。
/// 
/// 【実行タイミングとパフォーマンス】
/// このクラスは ASP.NET Core の認証ミドルウェアから、認証済みの HTTP リクエストごとに呼び出される。
/// Blazor の Enhanced Navigation も内部的には fetch() による HTTP リクエストのため、
/// ページ遷移のたびに DB アクセスが発生する。
/// 
/// DB アクセスは ExternalId によるインデックス付き単一行 SELECT であり、
/// 一般的なユースケースでは十分高速。ただし、DB アクセスを削減したい場合は
/// MemoryCache 等のキャッシュ層を導入する方法もある（その場合はユーザー情報
/// 更新時のキャッシュ無効化も併せて実装が必要）。
/// 
/// 現在の実装では、ユーザー情報の変更（表示名・ロール等）は次回のページ遷移時に
/// 即座に反映される。
/// </remarks>
internal class ClaimsTransformation(
    IUserAuthenticationService userAuthService,
    ClaimMappingIdentityResolver identityResolver) : IClaimsTransformation
{
    // 出力クレームタイプ（5項目）。"app_" プレフィックスで外部と区別
    internal const string AppUserIdClaimType = "app_user_id";
    internal const string AppExternalIdClaimType = "app_external_id";
    internal const string AppDisplayNameClaimType = "app_display_name";
    internal const string AppEmailClaimType = "app_email";
    // Role は ClaimTypes.Role を使用

    // DisplayName が取得できなかった場合のフォールバック値（CurrentUserService でも使用）
    internal const string NoDisplayNameFallback = "no_display_name";

    /// <summary>
    /// 外部クレームをアプリ固有クレームに変換する。認証済みユーザーのアクセス時に自動呼出。
    /// </summary>
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // 処理済みなら即終了（同一リクエスト内で複数回呼ばれる対策）
        if (principal.HasClaim(c => c.Type == AppUserIdClaimType))
            return principal;

        // 前提: 未認証なら何もしない
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        // 入力: 外部プロバイダから3項目（ExternalId, DisplayName, Email）を取得
        var externalIdentity = identityResolver.ResolveIdentity(principal);
        if (externalIdentity is null)
            return principal;

        // DBから取得（初回ログイン時は新規作成、DBへも登録）
        var user = await userAuthService.GetOrCreateUserAsync(
            externalIdentity.ExternalId,
            externalIdentity.DisplayName ?? NoDisplayNameFallback,
            externalIdentity.Email);

        // 出力: principal に5項目のクレームを追加（identity は principal.Identity）
        identity.AddClaims(
        [
            new Claim(AppUserIdClaimType, user.Id.ToString()),
            new Claim(AppExternalIdClaimType, user.ExternalId),
            new Claim(AppDisplayNameClaimType, user.DisplayName),
            new Claim(AppEmailClaimType, user.Email.Value),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        ]);
        return principal;  // クレーム追加済みの principal を返す
    }
}
