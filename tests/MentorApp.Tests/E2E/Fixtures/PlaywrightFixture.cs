using Microsoft.Playwright;

namespace MentorApp.Tests.E2E.Fixtures;

/// <summary>
/// テストクラス間でPlaywrightブラウザを共有するフィクスチャ
/// </summary>
/// <remarks>
/// IClassFixture&lt;PlaywrightFixture&gt;として使用する。
/// ブラウザインスタンスの生成コストが高いため、テストクラス間で共有する方針。
/// </remarks>
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright install failed with exit code {exitCode}");
        }

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();
    }
}
