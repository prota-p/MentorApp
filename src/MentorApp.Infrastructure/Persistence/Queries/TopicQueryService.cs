using MentorApp.Application.Contracts.Authentication;
using MentorApp.Application.Contracts.Queries;
using MentorApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence.Queries;

internal sealed class TopicQueryService(
    IDbContextFactory<AppDbContext> dbContextFactory) : ITopicQueryService
{
    public async Task<IReadOnlyList<TopicListItemDto>> GetAccessibleAsync(CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Topics
            .AsNoTracking()
            .Where(t => currentUser.Role == Role.Admin ||
                        t.Mentorship!.MentorUserId == currentUser.UserId ||
                        t.Mentorship.MenteeUserId == currentUser.UserId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TopicListItemDto(
                t.Id,
                t.MentorshipId,
                t.Title,
                t.Status,
                t.CreatedAt,
                t.Mentorship!.MentorUser!.DisplayName,
                t.Mentorship.MenteeUser!.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopicListItemDto>> GetByMentorshipIdAsync(Guid mentorshipId, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Topics
            .AsNoTracking()
            .Where(t => t.MentorshipId == mentorshipId &&
                (currentUser.Role == Role.Admin ||
                 t.Mentorship!.MentorUserId == currentUser.UserId ||
                 t.Mentorship.MenteeUserId == currentUser.UserId))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TopicListItemDto(
                t.Id,
                t.MentorshipId,
                t.Title,
                t.Status,
                t.CreatedAt,
                t.Mentorship!.MentorUser!.DisplayName,
                t.Mentorship.MenteeUser!.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<TopicDetailDto?> GetByIdAsync(Guid topicId, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Topics
            .AsNoTracking()
            .Where(t => t.Id == topicId &&
                (currentUser.Role == Role.Admin ||
                 t.Mentorship!.MentorUserId == currentUser.UserId ||
                 t.Mentorship.MenteeUserId == currentUser.UserId))
            .Select(t => new TopicDetailDto(
                t.Id,
                t.MentorshipId,
                t.Title,
                t.Status,
                t.CreatedAt,
                t.Mentorship!.MentorUserId,
                t.Mentorship.MentorUser!.DisplayName,
                t.Mentorship.MenteeUserId,
                t.Mentorship.MenteeUser!.DisplayName,
                t.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new MessageDto(
                        m.Id,
                        m.Content,
                        m.SentAt,
                        m.SenderUserId,
                        m.SenderUser!.DisplayName))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
