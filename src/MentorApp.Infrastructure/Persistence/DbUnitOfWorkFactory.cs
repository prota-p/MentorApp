using MentorApp.Domain.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// IUnitOfWorkFactory の実装
/// </summary>
/// <remarks>
/// IDbContextFactory を使用して DbContext を生成し、DbUnitOfWork でラップする。
/// Blazor Server 環境で適切に DbContext のライフサイクルを管理する。
/// </remarks>
internal sealed class DbUnitOfWorkFactory(IDbContextFactory<AppDbContext> dbContextFactory) : IUnitOfWorkFactory
{
    /// <inheritdoc />
    public async Task<IUnitOfWork> CreateAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return new DbUnitOfWork(dbContext);
    }
}
