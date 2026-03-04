using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Users;

namespace MentorApp.Domain.Models.Topics;

/// <summary>
/// メッセージ（Topic の子エンティティ）
/// </summary>
/// <remarks>
/// Topic 集約内の子エンティティ。単独では操作せず、必ず Topic を経由する。
/// 追記のみで編集・削除は不可。
/// </remarks>
public class Message
{
    public const int ContentMaxLength = 100;

    public Guid Id { get; private set; }

    public Guid TopicId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Content { get; private set; } = null!;

    public DateTimeOffset SentAt { get; private set; }

    public User? SenderUser { get; private set; }

    // EF Core 用
    private Message() { }

    /// <remarks>Topic 集約内部からのみ呼び出される（internal）</remarks>
    internal Message(Guid topicId, Guid senderUserId, string? content, DateTimeOffset sentAt)
    {
        Validate(topicId, senderUserId, content).ThrowIfInvalid();

        Id = Guid.NewGuid();
        TopicId = topicId;
        SenderUserId = senderUserId;
        Content = content!;
        SentAt = sentAt;
    }

    public static IEnumerable<ValidationError> Validate(Guid topicId, Guid senderUserId, string? content)
    {
        return ValidateTopicId(topicId).ToValidationErrors(nameof(TopicId))
            .Concat(ValidateSenderUserId(senderUserId).ToValidationErrors(nameof(SenderUserId)))
            .Concat(ValidateContent(content).ToValidationErrors(nameof(Content)));
    }

    public static IEnumerable<string> ValidateTopicId(Guid topicId)
    {
        if (topicId == Guid.Empty)
            yield return "TopicId は必須です。";
    }

    public static IEnumerable<string> ValidateSenderUserId(Guid senderUserId)
    {
        if (senderUserId == Guid.Empty)
            yield return "送信者を指定してください。";
    }

    public static IEnumerable<string> ValidateContent(string? content)
        => ValidationHelper.ValidateRequiredMaxLength(content, ContentMaxLength, "メッセージ");
}
