using MentorApp.Application.Contracts.Authentication;
using MentorApp.Application.Contracts.Queries;
using MentorApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MentorApp.Infrastructure.Persistence.Queries;

internal sealed class UserQueryService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<UserQueryService> logger) : IUserQueryService
{
    public async Task<IReadOnlyList<UserDto>> GetAccessibleAsync(CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Admin は全件、それ以外は自分自身のみ
            var query = dbContext.Users.AsNoTracking();
            if (currentUser.Role != Role.Admin)
                query = query.Where(u => u.Id == currentUser.UserId);

            return await query
                .OrderBy(u => u.DisplayName)
                .Select(u => new UserDto(
                    u.Id,
                    u.DisplayName,
                    u.Email.Value,
                    u.Role,
                    u.ExternalId,
                    u.CreatedAt))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ユーザー一覧の取得に失敗しました: CurrentUserId={CurrentUserId}",
                currentUser.UserId);
            throw;
        }
    }

    public async Task<IReadOnlyList<UserDto>> GetByRoleAsync(Role role, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Admin 専用。非 Admin からの呼び出しは空リストを返す
            if (currentUser.Role != Role.Admin)
                return [];

            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Role == role)
                .OrderBy(u => u.DisplayName)
                .Select(u => new UserDto(
                    u.Id,
                    u.DisplayName,
                    u.Email.Value,
                    u.Role,
                    u.ExternalId,
                    u.CreatedAt))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ロール別ユーザー一覧の取得に失敗しました: Role={Role}, CurrentUserId={CurrentUserId}",
                role, currentUser.UserId);
            throw;
        }
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Admin は任意ユーザー、それ以外は自分自身のみ（他者を指定した場合は null）
            if (currentUser.Role != Role.Admin && userId != currentUser.UserId)
                return null;

            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserDto(
                    u.Id,
                    u.DisplayName,
                    u.Email.Value,
                    u.Role,
                    u.ExternalId,
                    u.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ユーザーの取得に失敗しました: UserId={UserId}, CurrentUserId={CurrentUserId}",
                userId, currentUser.UserId);
            throw;
        }
    }
}
