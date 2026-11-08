using MentorApp.Application.Contracts.Authentication;
using MentorApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Authentication;

/// <summary>
/// 現在のユーザー情報を取得するサービス。
/// </summary>
/// <remarks>
/// 【役割】
/// 認証状態からはユーザーIDだけを読み、ロール・表示名は DB から読む
/// （回線の認証状態は接続時のまま更新されないため）。
///
/// 【なぜ AuthenticationStateProvider を使うのか】
/// Blazor Server では SignalR 接続上で動作するため HttpContext が常に利用可能とは限らない。
/// AuthenticationStateProvider を使用することで、HTTP リクエストの処理中（静的 SSR）でも回線の中でも、同じ方法で認証状態を読める。
/// </remarks>
internal class CurrentUserService(
    AuthenticationStateProvider authenticationStateProvider,
    IDbContextFactory<AppDbContext> dbContextFactory) : ICurrentUserService
{
    public async Task<CurrentUser?> GetCurrentUserAsync()
    {
        // 認証状態を取得（Blazor Server 対応）
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        // 未認証ならnullを返す
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // ClaimsTransformation が追加したユーザーIDを取得
        var userIdClaim = user.FindFirst(ClaimsTransformation.AppUserIdClaimType)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        // ユーザーが削除されていれば null
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new CurrentUser(u.Id, u.ExternalId, u.DisplayName, u.Role))
            .FirstOrDefaultAsync();
    }
}
