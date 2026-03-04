using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Mentorships;

namespace MentorApp.Application.Contracts.Queries;

public record MentorshipDto(
    Guid Id,
    MentorshipStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    Guid MentorUserId,
    string MentorDisplayName,
    Guid MenteeUserId,
    string MenteeDisplayName);

/// <summary>
/// メンタリング情報の読み取り専用クエリサービス
/// </summary>
/// <remarks>
/// 認可ロジックをUI層に分散させると呼び出し漏れが起きやすいため、
/// 全メソッドが CurrentUser を受け取り、QueryService 内部のWHERE句でフィルタリングを完結させる設計としている。
/// </remarks>
public interface IMentorshipQueryService
{
    /// <summary>
    /// 現在のユーザーがアクセス可能なメンタリング一覧を返す。
    /// Admin は全件、それ以外は自分が Mentor または Mentee として参加するもののみ。
    /// filterByUserId を指定すると、さらに対象ユーザーのメンタリングに絞り込む（Admin が特定ユーザーの詳細を見る場合に使用）。
    /// </summary>
    public Task<IReadOnlyList<MentorshipDto>> GetAccessibleAsync(CurrentUser currentUser, Guid? filterByUserId = null, CancellationToken cancellationToken = default);

    /// <summary>指定IDのメンタリングを返す。Admin は全件取得可、それ以外は自分が参加するもののみアクセス可能（非参加時は null）。</summary>
    public Task<MentorshipDto?> GetByIdAsync(Guid mentorshipId, CurrentUser currentUser, CancellationToken cancellationToken = default);
}
