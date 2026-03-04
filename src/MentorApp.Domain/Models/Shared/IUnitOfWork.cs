using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;

namespace MentorApp.Domain.Models.Shared;

/// <summary>
/// データベース操作のトランザクション境界を管理
/// </summary>
/// <remarks>
/// 同一UnitOfWork内の全リポジトリは同じDbContextを共有し、変更を一括コミットする。
/// インターフェースはドメイン層に配置し、Infrastructure層が実装することで依存性逆転の原則（DIP）を実現。
/// </remarks>
public interface IUnitOfWork : IAsyncDisposable
{
    public IUserRepository Users { get; }

    public IMentorshipRepository Mentorships { get; }

    public ITopicRepository Topics { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
