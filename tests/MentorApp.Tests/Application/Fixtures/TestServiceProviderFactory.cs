using MentorApp.Application;
using MentorApp.Infrastructure;
using MentorApp.Tests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MentorApp.Tests.Application.Fixtures;

/// <summary>
/// Application層統合テスト用のServiceProvider生成
/// </summary>
/// <remarks>
/// 本番と同じDI拡張メソッドを再利用し、TimeProviderのみ差し替える方針。
/// テストごとに独立したGUID付きDBを作成・削除することで並列実行を実現。
/// </remarks>
public sealed class TestServiceProviderFactory : IAsyncDisposable
{
    private ServiceProvider? _serviceProvider;
    private string? _databaseName;

    public async Task<ServiceProvider> CreateAsync(FakeTimeProvider timeProvider)
    {
        _databaseName = TestDatabaseConfiguration.CreateUniqueDatabaseName();
        var configuration = BuildTestConfiguration(_databaseName);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistenceOnly(configuration);
        services.AddApplication();
        services.AddSingleton<TimeProvider>(timeProvider);

        _serviceProvider = services.BuildServiceProvider();

        await InfrastructureInitialization.EnsureDatabaseCreatedAsync(_serviceProvider);

        return _serviceProvider;
    }

    private static IConfiguration BuildTestConfiguration(string databaseName)
    {
        var connectionString = TestDatabaseConfiguration.CreateConnectionString(databaseName);

        var configValues = new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "SqlServer",
            ["Persistence:EnableSensitiveDataLogging"] = "true",
            ["Persistence:EnableDetailedErrors"] = "true",
            ["Persistence:Providers:SqlServer:ConnectionString"] = connectionString,
            ["Persistence:Providers:SqlServer:CommandTimeout"] = "30",
            ["Persistence:Providers:SqlServer:EnableRetryOnFailure"] = "false",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            await InfrastructureInitialization.EnsureDatabaseDeletedAsync(_serviceProvider);
            await _serviceProvider.DisposeAsync();
            _serviceProvider = null;
        }
    }
}
