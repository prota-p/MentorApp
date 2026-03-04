using MentorApp.Application.Contracts.Authentication;
using MentorApp.Application.Contracts.Queries;
using MentorApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence.Queries;

internal sealed class MentorshipQueryService(
    IDbContextFactory<AppDbContext> dbContextFactory) : IMentorshipQueryService
{
    public async Task<IReadOnlyList<MentorshipDto>> GetAccessibleAsync(
        CurrentUser currentUser,
        Guid? filterByUserId = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Mentorships.AsNoTracking();

        if (currentUser.Role == Role.Admin)
        {
            // Admin は全件アクセス可能。filterByUserId が指定された場合はそのユーザーで絞り込む
            if (filterByUserId.HasValue)
                query = query.Where(m => m.MentorUserId == filterByUserId || m.MenteeUserId == filterByUserId);
        }
        else
        {
            // Admin 以外は自分が参加するもののみ（filterByUserId は無視）
            query = query.Where(m => m.MentorUserId == currentUser.UserId || m.MenteeUserId == currentUser.UserId);
        }

        return await query
            .OrderByDescending(m => m.StartedAt)
            .Select(m => new MentorshipDto(
                m.Id,
                m.Status,
                m.StartedAt,
                m.EndedAt,
                m.MentorUserId,
                m.MentorUser!.DisplayName,
                m.MenteeUserId,
                m.MenteeUser!.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<MentorshipDto?> GetByIdAsync(Guid mentorshipId, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Mentorships
            .AsNoTracking()
            .Where(m => m.Id == mentorshipId &&
                (currentUser.Role == Role.Admin ||
                 m.MentorUserId == currentUser.UserId ||
                 m.MenteeUserId == currentUser.UserId))
            .Select(m => new MentorshipDto(
                m.Id,
                m.Status,
                m.StartedAt,
                m.EndedAt,
                m.MentorUserId,
                m.MentorUser!.DisplayName,
                m.MenteeUserId,
                m.MenteeUser!.DisplayName))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
