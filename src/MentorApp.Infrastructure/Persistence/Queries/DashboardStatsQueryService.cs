using MentorApp.Application.Contracts.Authentication;
using MentorApp.Application.Contracts.Queries;
using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MentorApp.Infrastructure.Persistence.Queries;

internal sealed class DashboardStatsQueryService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<DashboardStatsQueryService> logger) : IDashboardStatsQueryService
{
    public async Task<AdminStatsDto?> GetAdminStatsAsync(CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            // Admin 専用。ゼロの統計を返すと「まだ1件もない」と区別できないため、非 Admin には null を返す
            if (currentUser.Role != Role.Admin)
                return null;
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
        catch (Exception ex)
        {
            logger.LogError(ex, "管理者向け統計の取得に失敗しました: CurrentUserId={CurrentUserId}",
                currentUser.UserId);
            throw;
        }
    }

    public async Task<UserStatsDto> GetUserStatsAsync(CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "ユーザー統計の取得に失敗しました: CurrentUserId={CurrentUserId}",
                currentUser.UserId);
            throw;
        }
    }
}
