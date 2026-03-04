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
/// IClaimsTransformationでDBのユーザー情報がクレームに追加されるため、
/// このサービスはクレームを読み取るだけでユーザー情報を取得できる。
/// 
/// Web層はクレーム取得などのInfrastructure層の実装詳細を知らず、
/// このインターフェース経由で認証ユーザー情報を取得する。
/// </remarks>
public interface ICurrentUserService
{
    public Task<CurrentUser?> GetCurrentUserAsync();
}
