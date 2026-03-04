using MentorApp.Application.Contracts.Authentication;
using MentorApp.Infrastructure.Authentication.Providers;
using MentorApp.Infrastructure.Authentication.Providers.Shared;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MentorApp.Infrastructure.Authentication;

/// <summary>
/// 認証層の内部サービス登録・エンドポイント構成
/// </summary>
/// <remarks>
/// - AddAuthentication: フェーズ1（サービス登録）で使用
/// - MapAuthenticationEndpoints: フェーズ2（ミドルウェア構成）で使用
/// 
/// IAuthenticationProviderSetup について:
/// フェーズ1でプロバイダ種別に応じた認証スキーム設定が必要なため、
/// フェーズ1で直接インスタンスを生成し、DIコンテナにも登録している。
/// フェーズ2ではDIから取得することで、同一インスタンスの再利用を保証する。
/// </remarks>
internal static class AuthenticationExtensions
{
    /// <summary>
    /// 認証サービスを DI コンテナに登録する（フェーズ1: サービス登録）
    /// </summary>
    internal static IServiceCollection AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        AuthenticationPathOptions pathOptions)
    {
        // オプション設定（AuthenticationOptions, PathOptions）
        services.AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection(AuthenticationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(Options.Create(pathOptions));

        var authOptions = configuration
            .GetSection(AuthenticationOptions.SectionName)
            .Get<AuthenticationOptions>()
            ?? throw new InvalidOperationException($"{AuthenticationOptions.SectionName} の設定が見つかりません。");

        // Cookie認証（全プロバイダ共通）
        var builder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
        services.ConfigureCookieDefaults(pathOptions);

        // プロバイダ固有の設定（認証スキーム追加）
        // ※フェーズ2で再利用するためDIにも登録
        var providerSetup = ProviderFactory.Create(
            ParseProviderType(authOptions.Provider),
            authOptions.Providers);
        services.AddSingleton(providerSetup);
        providerSetup.ConfigureAuthentication(builder, pathOptions);

        // IdentityResolver（全プロバイダ共通: ClaimMappings を使って登録）
        services.AddScoped(_ => new ClaimMappingIdentityResolver(providerSetup.ClaimMappings));

        // 共通サービス（クレーム変換、現在ユーザー取得）
        services.AddScoped<IClaimsTransformation, ClaimsTransformation>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    private static void ConfigureCookieDefaults(
        this IServiceCollection services,
        AuthenticationPathOptions pathOptions)
    {
        services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.LoginPath = pathOptions.LoginPath;
                options.AccessDeniedPath = pathOptions.AccessDeniedPath;
            });
    }

    /// <summary>
    /// 認証エンドポイントをルーティングに追加する（フェーズ2: ミドルウェア構成）
    /// </summary>
    internal static WebApplication MapAuthenticationEndpoints(
        this WebApplication app,
        AuthenticationPathOptions pathOptions)
    {
        // フェーズ1で登録済みのインスタンスを取得
        var providerSetup = app.Services.GetRequiredService<IProviderSetup>();

        // サインイン（プロバイダ固有）
        providerSetup.MapSignInEndpoint(app, pathOptions);

        // サインアウト（共通）
        app.MapSignOutEndpoint(pathOptions);

        return app;
    }

    private static AuthProviderType ParseProviderType(string provider)
    {
        return Enum.TryParse<AuthProviderType>(provider, out var providerType)
            ? providerType
            : throw new InvalidOperationException(
                $"サポートされていない認証プロバイダーです: {provider}。" +
                $"使用可能なプロバイダー: {string.Join(", ", Enum.GetNames<AuthProviderType>())}");
    }

    private static WebApplication MapSignOutEndpoint(
        this WebApplication app,
        AuthenticationPathOptions pathOptions)
    {
        app.MapPost(pathOptions.SignOutPath, async (HttpContext ctx, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(ctx);
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect(pathOptions.PostLogoutRedirectPath);
        });

        return app;
    }
}
