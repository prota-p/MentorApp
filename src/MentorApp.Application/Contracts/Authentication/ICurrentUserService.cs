using MentorApp.Domain.Models.Users;

namespace MentorApp.Application.Contracts.Authentication;

public record CurrentUser(
    Guid UserId,
    string ExternalId,
    string DisplayName,
    Role Role);

/// <summary>
/// 現在の認証ユーザー情報を取得するサービス
/// </summary>
/// <remarks>
/// 認証状態からユーザーIDを読み、ロール・表示名は呼ばれた時点の DB の値を返す。
/// 返した値はその時点のコピーのため、権限が関わる操作では、呼ぶ直前に取り直す。
/// 
/// Web層はクレーム取得などのInfrastructure層の実装詳細を知らず、
/// このインターフェース経由で認証ユーザー情報を取得する。
/// </remarks>
public interface ICurrentUserService
{
    public Task<CurrentUser?> GetCurrentUserAsync();
}
