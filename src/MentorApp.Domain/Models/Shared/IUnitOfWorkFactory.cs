namespace MentorApp.Domain.Models.Shared;

/// <remarks>
/// アプリケーションサービスで使用し、ユースケースごとにUnitOfWorkを作成。
/// 各UnitOfWorkは独立したDbContextを持ち、スコープ終了時に破棄される。
/// </remarks>
public interface IUnitOfWorkFactory
{
    public Task<IUnitOfWork> CreateAsync(CancellationToken cancellationToken = default);
}
