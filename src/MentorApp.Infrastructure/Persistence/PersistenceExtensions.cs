using MentorApp.Application.Contracts.Queries;
using MentorApp.Domain.Models.Shared;
using MentorApp.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// 永続化層の内部サービス登録
/// </summary>
/// <remarks>
/// - AddPersistence: フェーズ1（サービス登録）で使用
/// </remarks>
internal static class PersistenceExtensions
{
    private const int DefaultMaxRetryDelaySeconds = 30;

    /// <summary>
    /// DbContext と UnitOfWork を DI コンテナに登録する（フェーズ1: サービス登録）
    /// </summary>
    internal static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // DbContextFactory を使用する理由:
        // Blazor Server では回線(Circuit)単位でスコープが維持され、
        // 通常の DbContext だと長時間キャッシュが残り整合性の問題が生じる。
        // Factory パターンにより、必要な時に短命な DbContext を生成できる。
        //
        // DB プロバイダー切り替えについて:
        // この DI 登録箇所で Provider 設定を変更するだけで切り替え可能。
        // アプリケーション層は DbContext を通じてアクセスするため影響を受けない。
        services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

            ConfigureProvider(options, databaseOptions);
            ConfigureDiagnostics(options, databaseOptions);
        });

        services.AddScoped<IUnitOfWorkFactory, DbUnitOfWorkFactory>();

        // Query側のサービス登録（CQRSのRead側）
        // Command側（UnitOfWork経由）とは異なり、DbContextを直接注入して読み取り専用クエリを実行
        services.AddScoped<IDashboardStatsQueryService, DashboardStatsQueryService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IMentorshipQueryService, MentorshipQueryService>();
        services.AddScoped<ITopicQueryService, TopicQueryService>();

        return services;
    }

    private static void ConfigureProvider(DbContextOptionsBuilder options, PersistenceOptions databaseOptions)
    {
        switch (databaseOptions.Provider)
        {
            case DatabaseProviders.Sqlite:
                ConfigureSqlite(options, databaseOptions);
                break;

            case DatabaseProviders.SqlServer:
                ConfigureSqlServer(options, databaseOptions);
                break;

            case DatabaseProviders.InMemory:
                ConfigureInMemory(options, databaseOptions);
                break;

            default:
                throw new InvalidOperationException(
                    $"サポートされていないデータベースプロバイダーです: {databaseOptions.Provider}。" +
                    $"使用可能なプロバイダー: {DatabaseProviders.SqlServer}, {DatabaseProviders.Sqlite}, {DatabaseProviders.InMemory}");
        }
    }

    private static void ConfigureSqlite(DbContextOptionsBuilder options, PersistenceOptions databaseOptions)
    {
        var sqliteOptions = GetProviderOptions(
            databaseOptions.Providers.Sqlite,
            DatabaseProviders.Sqlite);

        options.UseSqlite(sqliteOptions.ConnectionString, sqlite =>
        {
            sqlite.CommandTimeout(sqliteOptions.CommandTimeout);
        });
    }

    private static void ConfigureSqlServer(DbContextOptionsBuilder options, PersistenceOptions databaseOptions)
    {
        var sqlServerOptions = GetProviderOptions(
            databaseOptions.Providers.SqlServer,
            DatabaseProviders.SqlServer);

        options.UseSqlServer(sqlServerOptions.ConnectionString, sqlServer =>
        {
            sqlServer.CommandTimeout(sqlServerOptions.CommandTimeout);
            if (sqlServerOptions.EnableRetryOnFailure)
            {
                sqlServer.EnableRetryOnFailure(
                    maxRetryCount: sqlServerOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(DefaultMaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
            }
        });
    }

    private static void ConfigureInMemory(DbContextOptionsBuilder options, PersistenceOptions databaseOptions)
    {
        var inMemoryOptions = GetProviderOptions(
            databaseOptions.Providers.InMemory,
            DatabaseProviders.InMemory);

        options.UseInMemoryDatabase(inMemoryOptions.DatabaseName);
    }

    private static void ConfigureDiagnostics(DbContextOptionsBuilder options, PersistenceOptions databaseOptions)
    {
        if (databaseOptions.EnableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        if (databaseOptions.EnableDetailedErrors)
        {
            options.EnableDetailedErrors();
        }
    }

    private static T GetProviderOptions<T>(T? options, string providerName) where T : class
    {
        return options ?? throw new InvalidOperationException(
            $"{providerName}プロバイダーが選択されていますが、Providers.{providerName}設定が見つかりません。");
    }
}
