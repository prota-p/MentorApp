using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Users;

namespace MentorApp.Application.Contracts.Queries;

public record UserDto(
    Guid Id,
    string DisplayName,
    string Email,
    Role Role,
    string ExternalId,
    DateTimeOffset CreatedAt);

/// <summary>
/// ユーザー情報の読み取り専用クエリサービス
/// </summary>
/// <remarks>
/// 認可ロジックをUI層に分散させると呼び出し漏れが起きやすいため、
/// 全メソッドが CurrentUser を受け取り、QueryService 内部のWHERE句でフィルタリングを完結させる設計としている。
/// </remarks>
public interface IUserQueryService
{
    /// <summary>現在のユーザーがアクセス可能なユーザー一覧を返す。Admin は全件、それ以外は自分自身のみ。</summary>
    public Task<IReadOnlyList<UserDto>> GetAccessibleAsync(CurrentUser currentUser, CancellationToken cancellationToken = default);

    /// <summary>指定ロールのユーザー一覧を返す。Admin 専用（非 Admin の場合は空リストを返す）。</summary>
    public Task<IReadOnlyList<UserDto>> GetByRoleAsync(Role role, CurrentUser currentUser, CancellationToken cancellationToken = default);

    /// <summary>指定IDのユーザーを返す。Admin は任意ユーザー、それ以外は自分自身のみアクセス可能（他者指定時は null）。</summary>
    public Task<UserDto?> GetByIdAsync(Guid userId, CurrentUser currentUser, CancellationToken cancellationToken = default);
}
