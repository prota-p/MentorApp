namespace MentorApp.Infrastructure;

/// <summary>
/// 認証関連のパス設定オプション
/// </summary>
/// <remarks>
/// Web層から渡される認証関連のルーティングパス。
/// Infrastructure層は技術的な認証メカニズムを提供し、
/// Web層がアプリケーション固有のパスを決定する責任分離を実現する。
/// </remarks>
public sealed record AuthenticationPathOptions
{
    public required string LoginPath { get; init; }
    public required string AccessDeniedPath { get; init; }
    public required string PostLoginRedirectPath { get; init; }
    public required string SignInPath { get; init; }
    public required string SignOutPath { get; init; }
    public required string PostLogoutRedirectPath { get; init; }
    public required string OidcCallbackPath { get; init; }
}
