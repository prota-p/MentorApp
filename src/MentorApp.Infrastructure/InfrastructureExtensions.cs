using MentorApp.Infrastructure.Authentication;
using MentorApp.Infrastructure.Persistence;
using MentorApp.Infrastructure.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MentorApp.Infrastructure;

/// <summary>
/// Infrastructure層の公開API（Web層 Program.cs から呼び出される）
/// </summary>
/// <remarks>
/// - AddInfrastructure: フェーズ1（サービス登録）で使用
/// - MapInfrastructureEndpoints: フェーズ2（ミドルウェア構成）で使用
/// 内部実装は internal として隠蔽し、このクラス経由でのみアクセス可能。
/// </remarks>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Infrastructure層のサービスをDIコンテナに登録する（フェーズ1: サービス登録）
    /// </summary>
    /// <remarks>
    /// 永続化、認証、時刻プロバイダーを一括登録する。
    /// </remarks>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        AuthenticationPathOptions authPathOptions)
    {
        services.AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection(AuthenticationOptions.SectionName))
            .ValidateOnStart();

        services.AddPersistence(configuration);
        services.AddAuthentication(configuration, authPathOptions);
        services.AddTimeProvider();

        return services;
    }

    /// <summary>
    /// 永続化層のサービスのみをDIコンテナに登録する（テスト用）
    /// </summary>
    /// <remarks>
    /// Application層の統合テストで使用。認証なしでテスト可能。
    /// IClock は登録されないため、テスト側で差し替えが必要。
    /// </remarks>
    public static IServiceCollection AddPersistenceOnly(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        return services;
    }

    /// <summary>
    /// Infrastructure層のエンドポイントをマッピングする（フェーズ2: ミドルウェア構成）
    /// </summary>
    /// <remarks>
    /// 認証エンドポイント等をルーティングに追加する。
    /// </remarks>
    public static WebApplication MapInfrastructureEndpoints(
        this WebApplication app,
        AuthenticationPathOptions authPathOptions)
    {
        app.MapAuthenticationEndpoints(authPathOptions);
        return app;
    }
}
