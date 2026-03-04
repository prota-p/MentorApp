using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Topics;

namespace MentorApp.Application.Contracts.Queries;

public record TopicListItemDto(
    Guid Id,
    Guid MentorshipId,
    string Title,
    TopicStatus Status,
    DateTimeOffset CreatedAt,
    string MentorDisplayName,
    string MenteeDisplayName);

public record TopicDetailDto(
    Guid Id,
    Guid MentorshipId,
    string Title,
    TopicStatus Status,
    DateTimeOffset CreatedAt,
    Guid MentorUserId,
    string MentorDisplayName,
    Guid MenteeUserId,
    string MenteeDisplayName,
    IReadOnlyList<MessageDto> Messages);

public record MessageDto(
    Guid Id,
    string Content,
    DateTimeOffset SentAt,
    Guid SenderUserId,
    string SenderDisplayName);

/// <summary>
/// トピック情報の読み取り専用クエリサービス
/// </summary>
/// <remarks>
/// 認可ロジックをUI層に分散させると呼び出し漏れが起きやすいため、
/// 全メソッドが CurrentUser を受け取り、QueryService 内部のWHERE句でフィルタリングを完結させる設計としている。
/// </remarks>
public interface ITopicQueryService
{
    /// <summary>現在のユーザーがアクセス可能なトピック一覧を返す。Admin は全件、それ以外は自分が参加するもののみ。</summary>
    public Task<IReadOnlyList<TopicListItemDto>> GetAccessibleAsync(CurrentUser currentUser, CancellationToken cancellationToken = default);

    /// <summary>指定メンタリングのトピック一覧を返す。Admin は全件、それ以外は自分が参加するメンタリングのみアクセス可能（非参加時は空リスト）。</summary>
    public Task<IReadOnlyList<TopicListItemDto>> GetByMentorshipIdAsync(Guid mentorshipId, CurrentUser currentUser, CancellationToken cancellationToken = default);

    /// <summary>指定IDのトピックを返す。Admin は全件取得可、それ以外は自分が参加するメンタリングのトピックのみアクセス可能（非参加時は null）。</summary>
    public Task<TopicDetailDto?> GetByIdAsync(Guid topicId, CurrentUser currentUser, CancellationToken cancellationToken = default);
}
