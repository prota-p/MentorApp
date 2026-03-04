using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using AuthOptions = Microsoft.AspNetCore.Authentication.AuthenticationOptions;

namespace MentorApp.Infrastructure.Authentication.Providers.Shared;

/// <summary>
/// OpenID Connect を使用する認証プロバイダーの共通基底クラス
/// </summary>
/// <remarks>
/// 【概要】
/// Entra ID と Google は両方とも OIDC (OpenID Connect) を使用するため、
/// 共通処理をこの基底クラスに集約する。
/// 派生クラスは ClaimMappings のみを定義すればよい。
/// 
/// 【認証フロー（Authorization Code Flow）】
/// 1. クライアントが /authentication/login にアクセス
/// 2. Challenge() により外部プロバイダーのログイン画面へリダイレクト
/// 3. ユーザーが認証 → 認可コードと共にコールバックURLへ戻る
/// 4. ASP.NET Core がコードをトークンに交換し、クレームを取得
/// 5. Cookie に保存 → 認証完了
/// 6. 以降のリクエストで ClaimsTransformation がアプリ固有クレームを追加
/// 
/// 【Mock との違い】
/// - Mock: 手動でクレーム生成（開発・テスト用）
/// - OIDC: 外部プロバイダーからトークン取得 → クレーム自動設定
/// </remarks>
/// <param name="authority">OIDC Authority URL</param>
/// <param name="clientId">OAuth クライアントID</param>
/// <param name="clientSecret">OAuth クライアントシークレット</param>
internal abstract class OidcProviderSetupBase(
    string authority,
    string clientId,
    string clientSecret) : IProviderSetup
{
    /// <summary>
    /// このプロバイダー用のクレームマッピング
    /// </summary>
    /// <remarks>
    /// 各プロバイダーが返すクレームの形式が異なるため、
    /// 派生クラスでプロバイダー固有のマッピングを定義する。
    /// </remarks>
    public abstract IdentityClaimMappings ClaimMappings { get; }

    /// <summary>
    /// OIDC 認証スキームを設定する
    /// </summary>
    public void ConfigureAuthentication(AuthenticationBuilder builder, AuthenticationPathOptions pathOptions)
    {
        // 【スキーム構成】
        // - DefaultScheme: Cookie（認証状態の保持）
        // - DefaultChallengeScheme: OIDC（未認証時のリダイレクト先）
        builder.Services.Configure<AuthOptions>(authOptions =>
        {
            authOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            authOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        });

        // 【OIDC 設定】
        builder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidcOptions =>
        {
            // プロバイダー固有の設定（派生クラスから渡される）
            // - Authority: トークン発行者のURL（Entra ID や Google）
            // - ClientId/Secret: OAuth アプリの認証情報
            oidcOptions.Authority = authority;
            oidcOptions.ClientId = clientId;
            oidcOptions.ClientSecret = clientSecret;

            // Authorization Code Flow を使用（セキュリティ推奨）
            oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
            // 認可コードを受け取るコールバックパス
            oidcOptions.CallbackPath = pathOptions.OidcCallbackPath;

            // 要求するスコープ（OIDC標準: https://openid.net/specs/openid-connect-core-1_0.html#ScopeClaims）
            oidcOptions.Scope.Add("openid");   // 必須: OIDC 認証
            oidcOptions.Scope.Add("profile");  // 名前等の基本情報
            oidcOptions.Scope.Add("email");    // メールアドレス
        });
    }

    /// <summary>
    /// サインインエンドポイントを登録する（GET /authentication/login）
    /// </summary>
    public void MapSignInEndpoint(WebApplication app, AuthenticationPathOptions pathOptions)
    {
        // Challenge() を返すことで、ASP.NET Core が自動的に
        // OIDC プロバイダーのログイン画面へリダイレクトする
        // （Mock のように手動でクレームを作る必要がない）
        app.MapGet(pathOptions.SignInPath, (HttpContext ctx) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = pathOptions.PostLoginRedirectPath }));
    }
}
