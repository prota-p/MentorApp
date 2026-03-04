using MentorApp.Domain.Models.Topics;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Topic リポジトリの実装（Command側）
/// </summary>
/// <remarks>
/// <para>
/// 状態変更の前処理（取得→更新）に使用。
/// 一覧取得などのQuery操作はTopicQueryServiceが担当する。
/// </para>
/// <para>
/// 同一集約内のMessagesはIncludeするが、別集約（Mentorship→User）の
/// Includeは行わない。表示用途での結合はQueryService側で行う。
/// </para>
/// </remarks>
internal sealed class TopicRepository(AppDbContext dbContext) : ITopicRepository
{
    public async Task<Topic?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // 同一集約内のMessagesのみInclude（更新時に必要）
        // 別集約（Mentorship→User）はIncludeしない
        return await dbContext.Topics
            .Include(t => t.Messages.OrderBy(m => m.SentAt))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Topic?> FindByMentorshipAndTitleAsync(
        Guid mentorshipId,
        string title,
        CancellationToken cancellationToken = default)
        => await dbContext.Topics
            .FirstOrDefaultAsync(
                t => t.MentorshipId == mentorshipId && t.Title == title,
                cancellationToken);

    public async Task AddAsync(Topic topic, CancellationToken cancellationToken = default)
        => await dbContext.Topics.AddAsync(topic, cancellationToken);

    public async Task<bool> HasAnyByMentorshipIdAsync(Guid mentorshipId, CancellationToken cancellationToken = default)
        => await dbContext.Topics.AnyAsync(t => t.MentorshipId == mentorshipId, cancellationToken);
}
