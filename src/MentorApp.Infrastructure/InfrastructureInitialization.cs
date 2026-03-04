using MentorApp.Domain.Models.Shared;
using MentorApp.Infrastructure.Authentication;
using MentorApp.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentorApp.Infrastructure;

/// <summary>
/// Infrastructure層の初期化処理を公開するファサード
/// </summary>
/// <remarks>
/// DB初期化などの実行・副作用処理を提供する。
/// DI登録（構成）とは明確に分離される。
/// </remarks>
public static class InfrastructureInitialization
{
    /// <summary>
    /// データベースの作成と初期管理者のシードを実行する
    /// </summary>
    /// <remarks>
    /// Production/Development 環境の両方で実行される。
    /// - データベースの作成（マイグレーション未使用時）
    /// - 初期管理者のシード（設定されている場合）
    /// Web層（Program.cs）から明示的に呼び出す。
    /// </remarks>
    public static async Task InitializeRequiredDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        // データベース作成
        await DatabaseInitializer.EnsureCreatedAsync(scopedServices);

        // 初期管理者のシード（UoW / Repository 経由）
        var authOptions = scopedServices.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        if (authOptions.InitialAdmin.IsConfigured)
        {
            var uowFactory = scopedServices.GetRequiredService<IUnitOfWorkFactory>();
            var timeProvider = scopedServices.GetRequiredService<TimeProvider>();
            var loggerFactory = scopedServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(InfrastructureInitialization));

            await DataSeeder.SeedInitialAdminAsync(
                uowFactory,
                timeProvider,
                authOptions.InitialAdmin.ExternalId!,
                authOptions.InitialAdmin.DisplayName,
                authOptions.InitialAdmin.Email!,
                logger);
        }
    }

    /// <summary>
    /// 開発用のサンプルデータをシードする
    /// </summary>
    /// <remarks>
    /// Development環境専用。
    /// デモ用のUser、Mentorship、Topic、Messageを作成する。
    /// Web層（Program.cs）から明示的に呼び出す。
    /// </remarks>
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var uowFactory = scopedServices.GetRequiredService<IUnitOfWorkFactory>();
        var loggerFactory = scopedServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(InfrastructureInitialization));

        var baseTime = new DateTimeOffset(2025, 12, 1, 12, 0, 0, TimeSpan.Zero);
        await DataSeeder.SeedDevelopmentDataAsync(uowFactory, baseTime, logger);
    }

    /// <summary>
    /// データベースを作成する（テスト用）
    /// </summary>
    /// <remarks>
    /// Testing環境専用。
    /// テストコードから明示的にデータベースを作成する。
    /// </remarks>
    public static Task EnsureDatabaseCreatedAsync(IServiceProvider serviceProvider)
        => DatabaseInitializer.EnsureCreatedAsync(serviceProvider);

    /// <summary>
    /// データベースを削除する（テスト用）
    /// </summary>
    /// <remarks>
    /// Testing環境専用。
    /// テストクリーンアップ時にデータベースを削除する。
    /// </remarks>
    public static Task EnsureDatabaseDeletedAsync(IServiceProvider serviceProvider)
        => DatabaseInitializer.EnsureDeletedAsync(serviceProvider);
}
