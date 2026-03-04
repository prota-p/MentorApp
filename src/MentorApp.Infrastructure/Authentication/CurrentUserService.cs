using System.Security.Claims;
using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Users;
using Microsoft.AspNetCore.Components.Authorization;

namespace MentorApp.Infrastructure.Authentication;

/// <summary>
/// 現在のユーザー情報を取得するサービス。
/// </summary>
/// <remarks>
/// 【役割】
/// ClaimsTransformation が追加したアプリ固有クレームを読み取り、
/// CurrentUser オブジェクトとして返す。
/// 
/// 【なぜ AuthenticationStateProvider を使うのか】
/// Blazor Server では SignalR 接続上で動作するため HttpContext が常に利用可能とは限らない。
/// AuthenticationStateProvider を使用することで、どの状況でも認証状態にアクセスできる。
/// </remarks>
internal class CurrentUserService(
    AuthenticationStateProvider authenticationStateProvider) : ICurrentUserService
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

        // ClaimsTransformation が追加したアプリ固有クレームを取得
        var userIdClaim = user.FindFirst(ClaimsTransformation.AppUserIdClaimType)?.Value;
        var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        var role = Enum.TryParse<Role>(roleClaim, out var r) ? r : Role.Mentee;

        var displayName = user.FindFirst(ClaimsTransformation.AppDisplayNameClaimType)?.Value
            ?? ClaimsTransformation.NoDisplayNameFallback;
        var externalId = user.FindFirst(ClaimsTransformation.AppExternalIdClaimType)?.Value
            ?? string.Empty;

        return new CurrentUser(
            UserId: userId,
            ExternalId: externalId,
            DisplayName: displayName,
            Role: role
        );
    }
}
