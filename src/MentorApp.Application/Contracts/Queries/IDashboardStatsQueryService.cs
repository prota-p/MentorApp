using MentorApp.Application.Contracts.Authentication;

namespace MentorApp.Application.Contracts.Queries;

public record AdminStatsDto(
    int TotalUsers,
    int TotalMentorships,
    int ActiveMentorships,
    int CompletedMentorships,
    int TotalTopics,
    int OpenTopics);

public record UserStatsDto(
    int TotalMentorships,
    int ActiveMentorships,
    int TotalTopics,
    int OpenTopics);

/// <summary>
/// ダッシュボード統計データの読み取り専用クエリサービス
/// </summary>
/// <remarks>
/// 認可ロジックをUI層に分散させると呼び出し漏れが起きやすいため、
/// 全メソッドが CurrentUser を受け取り、QueryService 内部でアクセス可否を判断する設計としている。
/// </remarks>
public interface IDashboardStatsQueryService
{
    /// <summary>システム全体の統計を返す。Admin 専用（非 Admin の場合は空の統計を返す）。</summary>
    public Task<AdminStatsDto> GetAdminStatsAsync(CurrentUser currentUser, CancellationToken cancellationToken = default);

    /// <summary>currentUser 自身のメンタリング・トピック統計を返す。</summary>
    public Task<UserStatsDto> GetUserStatsAsync(CurrentUser currentUser, CancellationToken cancellationToken = default);
}
