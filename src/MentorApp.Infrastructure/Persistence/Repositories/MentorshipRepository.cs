using MentorApp.Domain.Models.Mentorships;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Mentorship リポジトリの実装（Command側）
/// </summary>
/// <remarks>
/// <para>
/// 状態変更の前処理（取得→更新）に使用。
/// 一覧取得などのQuery操作はMentorshipQueryServiceが担当する。
/// </para>
/// <para>
/// 別集約（User）のIncludeは行わない。
/// 表示用途での結合はQueryService側で行う。
/// </para>
/// </remarks>
internal sealed class MentorshipRepository(AppDbContext dbContext) : IMentorshipRepository
{
    public async Task<Mentorship?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Mentorships.FindAsync([id], cancellationToken);

    public async Task<bool> HasActiveMentorshipAsync(
        Guid mentorUserId,
        Guid menteeUserId,
        CancellationToken cancellationToken = default)
        => await dbContext.Mentorships
            .AnyAsync(
                m => m.MentorUserId == mentorUserId
                    && m.MenteeUserId == menteeUserId
                    && m.Status == MentorshipStatus.Active,
                cancellationToken);

    public async Task<bool> HasAnyActiveMentorshipByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await dbContext.Mentorships
            .AnyAsync(
                m => (m.MentorUserId == userId || m.MenteeUserId == userId)
                    && m.Status == MentorshipStatus.Active,
                cancellationToken);

    public async Task<Mentorship?> FindByMentorAndMenteeAsync(
        Guid mentorUserId,
        Guid menteeUserId,
        CancellationToken cancellationToken = default)
        => await dbContext.Mentorships
            .FirstOrDefaultAsync(
                m => m.MentorUserId == mentorUserId && m.MenteeUserId == menteeUserId,
                cancellationToken);

    public async Task AddAsync(Mentorship mentorship, CancellationToken cancellationToken = default)
        => await dbContext.Mentorships.AddAsync(mentorship, cancellationToken);

    public void Delete(Mentorship mentorship)
        => dbContext.Mentorships.Remove(mentorship);
}
