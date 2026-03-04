namespace MentorApp.Domain.Models.Topics;

/// <summary>
/// トピック集約のリポジトリインターフェイス（Command側）
/// </summary>
/// <remarks>
/// <para>
/// Domain 層で定義し、Infrastructure 層が実装する（依存性逆転の原則）。
/// </para>
/// <para>
/// CQRSパターンにおけるCommand側の責務を担当。
/// 状態変更の前処理（取得→更新）に使用し、一覧取得などのQuery操作は
/// Application層のITopicQueryServiceが担当する。
/// </para>
/// <para>
/// 同一集約内のMessages はIncludeするが、別集約（Mentorship→User）の
/// Includeは行わない。表示用途での結合はQueryService側で行う。
/// </para>
/// </remarks>
public interface ITopicRepository
{
    public Task<Topic?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Topic?> FindByMentorshipAndTitleAsync(
        Guid mentorshipId,
        string title,
        CancellationToken cancellationToken = default);

    public Task AddAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>指定した Mentorship に Topic が1件以上存在するかを返す。削除可否チェックに使用。</summary>
    public Task<bool> HasAnyByMentorshipIdAsync(Guid mentorshipId, CancellationToken cancellationToken = default);
}
