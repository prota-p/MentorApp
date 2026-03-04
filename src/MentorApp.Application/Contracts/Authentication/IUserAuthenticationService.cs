using MentorApp.Domain.Models.Users;

namespace MentorApp.Application.Contracts.Authentication;

/// <summary>
/// 認証時にユーザーを取得または自動作成するサービス
/// </summary>
/// <remarks>
/// ユーザーの初回ログイン時、ExternalIdを元にMenteeロールでユーザーを自動作成する。
/// 認証パイプラインから呼び出される専用サービス。
/// ロール変更や表示名更新など、通常のユーザー管理操作にはUserServiceを使用する。
/// </remarks>
public interface IUserAuthenticationService
{
    /// <summary>
    /// ExternalIdでユーザーを取得、存在しなければRole=Menteeで作成
    /// </summary>
    public Task<User> GetOrCreateUserAsync(
        string externalId,
        string displayName,
        string email,
        CancellationToken cancellationToken = default);
}
