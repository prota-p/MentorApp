using MentorApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// User リポジトリの実装（Command側）
/// </summary>
/// <remarks>
/// 状態変更の前処理（取得→更新）に使用。
/// 一覧取得などのQuery操作はUserQueryServiceが担当する。
/// </remarks>
internal sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Users.FindAsync([id], cancellationToken);

    public async Task<User?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        => await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await dbContext.Users.AddAsync(user, cancellationToken);
}
