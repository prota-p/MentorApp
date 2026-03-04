namespace MentorApp.Domain.Models.Mentorships;

/// <summary>
/// メンタリング集約のリポジトリインターフェイス（Command側）
/// </summary>
/// <remarks>
/// <para>
/// Domain 層で定義し、Infrastructure 層が実装する（依存性逆転の原則）。
/// </para>
/// <para>
/// CQRSパターンにおけるCommand側の責務を担当。
/// 状態変更の前処理（取得→更新）に使用し、一覧取得などのQuery操作は
/// Application層のIMentorshipQueryServiceが担当する。
/// </para>
/// <para>
/// 別集約（User）のIncludeは行わない。表示用途での結合は
/// QueryService側で行う。
/// </para>
/// </remarks>
public interface IMentorshipRepository
{
    public Task<Mentorship?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<bool> HasActiveMentorshipAsync(
        Guid mentorUserId,
        Guid menteeUserId,
        CancellationToken cancellationToken = default);

    public Task<bool> HasAnyActiveMentorshipByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    public Task<Mentorship?> FindByMentorAndMenteeAsync(
        Guid mentorUserId,
        Guid menteeUserId,
        CancellationToken cancellationToken = default);

    public Task AddAsync(Mentorship mentorship, CancellationToken cancellationToken = default);

    public void Delete(Mentorship mentorship);
}
