using MentorApp.Application.Contracts.Authentication;
using MentorApp.Application.Contracts.Queries;
using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence.Queries;

internal sealed class DashboardStatsQueryService(
    IDbContextFactory<AppDbContext> dbContextFactory) : IDashboardStatsQueryService
{
    public async Task<AdminStatsDto> GetAdminStatsAsync(CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        // Admin 専用。非 Admin からの呼び出しは空の統計を返す
        if (currentUser.Role != Role.Admin)
            return new AdminStatsDto(0, 0, 0, 0, 0, 0);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // ユーザー数（単純なCOUNT）
        var totalUsers = await dbContext.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // メンタリング統計（ステータス別にGROUP BY）
        var mentorshipStats = await dbContext.Mentorships
            .AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalMentorships = mentorshipStats.Sum(s => s.Count);
        var activeMentorships = mentorshipStats
            .FirstOrDefault(s => s.Status == MentorshipStatus.Active)?.Count ?? 0;
        var completedMentorships = mentorshipStats
            .FirstOrDefault(s => s.Status == MentorshipStatus.Completed)?.Count ?? 0;

        // トピック統計（ステータス別にGROUP BY）
        var topicStats = await dbContext.Topics
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalTopics = topicStats.Sum(s => s.Count);
        var openTopics = topicStats
            .FirstOrDefault(s => s.Status == TopicStatus.Open)?.Count ?? 0;

        return new AdminStatsDto(
            totalUsers,
            totalMentorships,
            activeMentorships,
            completedMentorships,
            totalTopics,
            openTopics);
    }

    public async Task<UserStatsDto> GetUserStatsAsync(CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var userId = currentUser.UserId;

        // ユーザーのメンタリング統計（Mentor/Menteeどちらでも該当するもの）
        var mentorshipStats = await dbContext.Mentorships
            .AsNoTracking()
            .Where(m => m.MentorUserId == userId || m.MenteeUserId == userId)
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalMentorships = mentorshipStats.Sum(s => s.Count);
        var activeMentorships = mentorshipStats
            .FirstOrDefault(s => s.Status == MentorshipStatus.Active)?.Count ?? 0;

        // ユーザーのトピック統計（所属するメンタリングのトピック）
        var topicStats = await dbContext.Topics
            .AsNoTracking()
            .Where(t => t.Mentorship!.MentorUserId == userId || t.Mentorship!.MenteeUserId == userId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalTopics = topicStats.Sum(s => s.Count);
        var openTopics = topicStats
            .FirstOrDefault(s => s.Status == TopicStatus.Open)?.Count ?? 0;

        return new UserStatsDto(
            totalMentorships,
            activeMentorships,
            totalTopics,
            openTopics);
    }
}
