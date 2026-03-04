using MentorApp.Infrastructure;
using MentorApp.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace MentorApp.Tests.E2E.Fixtures;

/// <summary>
/// E2Eテスト用のWebApplicationFactory（Kestrelサーバー起動）
/// </summary>
/// <remarks>
/// IClassFixtureとして使用し、テストクラス全体でサーバーとDBを共有する。
/// Testing環境に設定することでProgram.csの自動DB初期化をスキップし、
/// クラス単位で固有のGUID付きDBを作成・削除する。
/// xUnitはIAsyncLifetime.DisposeAsync（DB削除）→ IAsyncDisposable.DisposeAsync（サーバー停止）
/// の順に呼び出すため、DB削除時にDIコンテナが有効であることが保証される。
/// </remarks>
public class MentorAppWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = TestDatabaseConfiguration.CreateUniqueDatabaseName();

    public string ServerAddress => ClientOptions.BaseAddress.ToString().TrimEnd('/');

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Testing環境ではStatic Web Assetsが自動有効化されないため明示的に呼び出す
        // これがないとblazor.web.jsやScoped CSSが404になる
        builder.UseStaticWebAssets();

        builder.UseSetting("Persistence:Providers:SqlServer:ConnectionString",
            TestDatabaseConfiguration.CreateConnectionString(_databaseName));
        builder.UseSetting("Authentication:Provider", "Mock");

        // テスト時のアプリケーションログを抑制し、テスト出力（[OUTPUT]）を見やすくする
        // Program.csでSerilog（ReadFrom.Configuration）を使用しているため、
        // ConfigureLogging.SetMinimumLevelではなく構成値で上書きする必要がある
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
    }

    public async ValueTask InitializeAsync()
    {
        this.UseKestrel();
        this.StartServer();

        await InfrastructureInitialization.EnsureDatabaseCreatedAsync(Services);
    }

    /// <summary>
    /// クリーンアップ（DB削除→サーバー停止）
    /// </summary>
    /// <remarks>
    /// xUnit v3ではIAsyncLifetimeがIAsyncDisposableを継承するため、
    /// DisposeAsyncをoverrideしてDB削除後にbase.DisposeAsync()でサーバーを停止する。
    /// </remarks>
    public override async ValueTask DisposeAsync()
    {
        try
        {
            await InfrastructureInitialization.EnsureDatabaseDeletedAsync(Services);
        }
        catch (Exception ex)
        {
            TestContext.Current.SendDiagnosticMessage("Failed to delete test database {0}: {1}", _databaseName, ex.Message);
        }

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
