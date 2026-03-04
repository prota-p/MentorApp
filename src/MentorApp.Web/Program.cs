using MentorApp.Application;
using MentorApp.Infrastructure;
using MentorApp.Web.Components;
using MentorApp.Web.Constants;
using MentorApp.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog設定（構造化ログ）
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ========================================
// フェーズ1: サービス登録（DI Container）
// ========================================

// Razorコンポーネント（Blazor Server）+ インタラクティブモード有効化
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 認可サービスを登録（ポリシーベース・ロールベースの認可を有効化）
builder.Services.AddAuthorizationBuilder();
// Blazorコンポーネント全体に認証状態をカスケード（AuthorizeViewなどで使用）
builder.Services.AddCascadingAuthenticationState();

// Web層のサービス登録
builder.Services.AddScoped<ToastService>();
builder.Services.AddSingleton<TopicUpdateNotificationService>();

// Application層のサービス登録（UseCase、Validator等）
builder.Services.AddApplication();

// 認証パス設定（Web層で一元管理）
var authPathOptions = new AuthenticationPathOptions
{
    LoginPath = PageRoutes.Login,
    AccessDeniedPath = PageRoutes.AccessDenied,
    PostLoginRedirectPath = PageRoutes.Home,
    SignInPath = AuthRoutes.SignIn,
    SignOutPath = AuthRoutes.SignOut,
    PostLogoutRedirectPath = AuthRoutes.PostLogoutRedirect,
    OidcCallbackPath = AuthRoutes.OidcCallback
};

// Infrastructure層のサービス登録（DB、認証、外部サービス等）
builder.Services.AddInfrastructure(builder.Configuration, authPathOptions);

var app = builder.Build();

// ========================================
// フェーズ2: ミドルウェアパイプライン構成
// ========================================

app.UseSerilogRequestLogging();

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler(PageRoutes.Error, createScopeForErrors: true);
    app.UseHsts();
}

// サーバーレベルでの404エラーをNotFoundページに内部転送
// （リダイレクトではなくパイプラインを再実行するため、URLは変わらず404ステータスも維持）
app.UseStatusCodePagesWithReExecute(PageRoutes.NotFound);

app.UseHttpsRedirection();
app.UseAntiforgery();

// 認証・認可ミドルウェア（この順序が重要）
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Infrastructure層のエンドポイント（認証関連など）
app.MapInfrastructureEndpoints(authPathOptions);

app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var url in app.Urls)
    {
        Log.Information("アプリケーションが {Url} で起動しました", url);
    }
});

// ========================================
// フェーズ3: 起動時処理
// ========================================
try
{
    Log.Information("================================================================================");
    Log.Information($"MentorApp 起動プロセス開始({app.Environment.EnvironmentName})");
    Log.Information("================================================================================");

    // 環境に応じたDB初期化
    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
    {
        Log.Information("データベースを初期化しています");
        await InfrastructureInitialization.InitializeRequiredDataAsync(app.Services);

        if (app.Environment.IsDevelopment())
        {
            Log.Information("開発環境: サンプルデータを生成しています");
            await InfrastructureInitialization.SeedDevelopmentDataAsync(app.Services);
        }
    }
    else if (app.Environment.IsEnvironment("Testing"))
    {
        // Testing環境：DB初期化はスキップ
        // テストコード（WebApplicationFactory）が明示的にDB作成・削除を管理する
        Log.Information("Testing環境: DB初期化はテストフィクスチャーで管理されます");
    }
    else
    {
        // 想定外の環境名
        Log.Warning("未定義の環境 '{EnvironmentName}' が指定されています。DB初期化をスキップします。", app.Environment.EnvironmentName);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "アプリケーションが予期せず終了しました");
}
finally
{
    Log.CloseAndFlush();
}
