using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;
using MentorApp.Infrastructure.Persistence.Repositories;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// IUnitOfWork の実装（AppDbContext をラップ）
/// </summary>
/// <remarks>
/// <para>
/// UnitOfWork内で取得したエンティティは同じDbContextで追跡される。
/// スコープ終了時にDbContextが破棄され、追跡も解除される。
/// </para>
/// <para>
/// 各リポジトリは遅延初期化され、同一のDbContextを共有する。
/// </para>
/// </remarks>
internal sealed class DbUnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private UserRepository? _users;
    private MentorshipRepository? _mentorships;
    private TopicRepository? _topics;

    public IUserRepository Users => _users ??= new UserRepository(dbContext);

    public IMentorshipRepository Mentorships => _mentorships ??= new MentorshipRepository(dbContext);

    public ITopicRepository Topics => _topics ??= new TopicRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => dbContext.DisposeAsync();
}
