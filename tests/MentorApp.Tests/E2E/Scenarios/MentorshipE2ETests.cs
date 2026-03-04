using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace MentorApp.Tests.E2E.Scenarios;

/// <summary>
/// メンタリング機能のE2Eテスト
/// </summary>
/// <remarks>
/// Playwrightを使用してブラウザ経由でアプリケーションをテスト。
/// テストクラス全体で1つのWebApplicationFactory（Kestrelサーバー）とDBを共有し、
/// 各テストは独自のブラウザコンテキストを持つためCookie等は分離される。
/// 各テストでユニークなユーザーIDを使用してデータ衝突を回避。
/// </remarks>
public class MentorshipE2ETests
    : IClassFixture<Fixtures.PlaywrightFixture>,
      IClassFixture<Fixtures.MentorAppWebApplicationFactory>,
      IAsyncLifetime
{
    private readonly Fixtures.PlaywrightFixture _playwrightFixture;
    private readonly Fixtures.MentorAppWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    private string BaseUrl => _factory.ServerAddress;

    public MentorshipE2ETests(
        Fixtures.PlaywrightFixture playwrightFixture,
        Fixtures.MentorAppWebApplicationFactory factory,
        ITestOutputHelper output)
    {
        _playwrightFixture = playwrightFixture;
        _factory = factory;
        _output = output;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _playwrightFixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_page is not null) await _page.CloseAsync();
            if (_context is not null) await _context.DisposeAsync();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Browser cleanup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// テスト用のモックサインインを実行し、/home へのリダイレクトを待機する。
    /// </summary>
    private async Task SignInAsync(string displayName)
    {
        var externalId = $"e2e-user-{Guid.NewGuid():N}";
        await _page.GotoAsync($"{BaseUrl}/auth/signin?externalId={externalId}&displayName={displayName}");
        await _page.WaitForURLAsync("**/home");
    }

    [Fact]
    public async Task MockSignIn_RedirectsToHomePage()
    {
        // Act
        await SignInAsync("E2Eテストユーザー");

        // Assert
        await Assertions.Expect(_page).ToHaveURLAsync(new Regex("/home"));
    }

    [Fact]
    public async Task SignedInUser_DisplaysUserNameInHeader()
    {
        // Arrange
        var displayName = "テスト太郎";

        // Act
        await SignInAsync(displayName);

        // Assert
        await Assertions.Expect(_page.GetByText(displayName)).ToBeVisibleAsync();
        // ウェルカムメッセージはInteractiveモード移行後に表示されるため、データ取得完了の検証になる
        await Assertions.Expect(_page.GetByText($"こんにちは、{displayName} さん")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotAccessHomePage()
    {
        // Act
        await _page.GotoAsync($"{BaseUrl}/home");

        // Assert
        await Assertions.Expect(_page).ToHaveURLAsync(new Regex(@"\?ReturnUrl="));
    }

    [Fact]
    public async Task Profile_CanUpdateDisplayName()
    {
        // Arrange
        var originalName = "変更前ユーザー";
        await SignInAsync(originalName);
        await _page.GotoAsync($"{BaseUrl}/profile");
        await Assertions.Expect(_page).ToHaveURLAsync(new Regex("/profile"));
        var displayNameInput = _page.GetByLabel("表示名");
        await Assertions.Expect(displayNameInput).ToHaveValueAsync(originalName);

        // Act
        var newName = "変更後ユーザー";
        await displayNameInput.ClearAsync();
        await displayNameInput.FillAsync(newName);
        await _page.GetByRole(AriaRole.Button, new() { Name = "更新" }).ClickAsync();

        // Assert
        await Assertions.Expect(_page.GetByText("プロフィールを更新しました。")).ToBeVisibleAsync();
        // DBに保存されたことの間接的検証として、入力欄の値も確認
        await Assertions.Expect(displayNameInput).ToHaveValueAsync(newName);
    }
}
