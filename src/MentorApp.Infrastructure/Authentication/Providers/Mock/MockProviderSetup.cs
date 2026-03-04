using System.Security.Claims;
using MentorApp.Infrastructure.Authentication.Providers.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MentorApp.Infrastructure.Authentication.Providers.Mock;

/// <summary>
/// 開発・テスト用のモック認証プロバイダー
/// </summary>
/// <remarks>
/// 【概要】
/// 外部認証プロバイダー（Entra ID 等）を使わず、Cookie 認証のみでログインを実現する。
/// クレーム変換（ClaimsTransformation）の仕組みを理解するための最初の題材として有用。
/// 
/// 【認証フロー】
/// 1. クライアントが /authentication/login?externalId=xxx にアクセス
/// 2. クエリパラメータから外部IDを取得し、Entra ID 形式のクレームを生成
/// 3. Cookie に署名して保存 → 認証完了
/// 4. 以降のリクエストで ClaimsTransformation がアプリ固有クレームを追加
/// 
/// 【Entra ID との違い】
/// - Entra ID: OIDC でトークン取得 → クレーム自動設定
/// - Mock: クエリパラメータ → 手動でクレーム生成（同じ形式を模倣）
/// </remarks>
internal class MockProviderSetup : IProviderSetup
{
    // Entra ID 形式を模倣したクレームタイプ
    private const string OidClaimType = "oid";      // Object ID（外部ID）
    private const string NameClaimType = "name";    // 表示名
    private const string EmailClaimType = "email";  // メールアドレス

    // クエリパラメータ名
    private const string ExternalIdParam = "externalId";
    private const string DisplayNameParam = "displayName";
    private const string ReturnUrlParam = "ReturnUrl";

    // デフォルト値
    private const string DefaultDisplayName = "テストユーザー";
    private const string DefaultEmailDomain = "@example.com";

    /// <summary>
    /// クレームマッピング定義
    /// </summary>
    /// <remarks>
    /// ClaimsTransformation が外部クレームからユーザー情報を抽出する際に使用。
    /// Entra ID と同じクレームタイプを使用することで、同じ変換ロジックが適用される。
    /// </remarks>
    public IdentityClaimMappings ClaimMappings { get; } = new()
    {
        ExternalIdClaims = [OidClaimType],
        DisplayNameClaims = [NameClaimType, ClaimTypes.Name],
        EmailClaims = [EmailClaimType, ClaimTypes.Email]
    };

    /// <summary>
    /// 認証スキームを設定する
    /// </summary>
    /// <remarks>
    /// Mock プロバイダーは Cookie 認証のみで完結するため、
    /// 追加のスキーム（OIDC 等）は登録しない。
    /// Cookie 認証は AuthenticationSetup で共通設定済み。
    /// </remarks>
    public void ConfigureAuthentication(AuthenticationBuilder builder, AuthenticationPathOptions pathOptions)
    {
        // 追加のスキーム登録は不要
    }

    /// <summary>
    /// サインインエンドポイントを登録する
    /// </summary>
    /// <remarks>
    /// GET /authentication/login?externalId=xxx&amp;displayName=yyy&amp;ReturnUrl=/path
    /// - externalId: 必須。ユーザーを識別する外部ID
    /// - displayName: 任意。省略時は「テストユーザー」
    /// - ReturnUrl: 任意。ログイン後のリダイレクト先
    /// </remarks>
    public void MapSignInEndpoint(WebApplication app, AuthenticationPathOptions pathOptions)
    {
        app.MapGet(pathOptions.SignInPath, async (HttpContext ctx) =>
        {
            // 入力: クエリパラメータからユーザー情報を取得
            var externalId = ctx.Request.Query[ExternalIdParam].FirstOrDefault();
            var displayName = ctx.Request.Query[DisplayNameParam].FirstOrDefault();
            var returnUrl = ctx.Request.Query[ReturnUrlParam].FirstOrDefault();

            // バリデーション: externalId は必須
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return Results.BadRequest($"{ExternalIdParam} パラメータは必須です。");
            }

            // クレーム生成: Entra ID の oid/name/email クレームを模倣
            // ※本来の OIDC 認証では外部プロバイダーからトークン経由で自動取得されるが、
            //   Mock ではテスト用にクエリパラメータから手動で生成している
            var claims = new List<Claim>
            {
                new(OidClaimType, externalId),
                new(NameClaimType, displayName ?? DefaultDisplayName),
                new(EmailClaimType, $"{externalId}{DefaultEmailDomain}"),
            };

            // 認証: ClaimsIdentity を作成し、Cookie に保存
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // リダイレクト: 指定があればそこへ、なければデフォルトパスへ
            // LocalRedirect でローカルURL以外を拒否（外部URLへのオープンリダイレクト防止）
            return Results.LocalRedirect(
                !string.IsNullOrWhiteSpace(returnUrl)
                    ? returnUrl
                    : pathOptions.PostLoginRedirectPath);
        });
    }
}
