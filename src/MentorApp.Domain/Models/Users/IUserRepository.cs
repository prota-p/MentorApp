namespace MentorApp.Domain.Models.Users;

/// <summary>
/// ユーザー集約のリポジトリインターフェイス（Command側）
/// </summary>
/// <remarks>
/// <para>
/// Domain 層で定義し、Infrastructure 層が実装する（依存性逆転の原則）。
/// </para>
/// <para>
/// CQRSパターンにおけるCommand側の責務を担当。
/// 状態変更の前処理（取得→更新）に使用し、一覧取得などのQuery操作は
/// Application層のIUserQueryServiceが担当する。
/// </para>
/// </remarks>
public interface IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<User?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    public Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>指定したロールのユーザーが1人以上存在するかを返す。初期管理者のシードで、管理者不在の判定に使用。</summary>
    public Task<bool> HasAnyByRoleAsync(Role role, CancellationToken cancellationToken = default);
}
