using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Shared;

namespace MentorApp.Domain.Models.Topics;

/// <summary>
/// 相談トピック（集約根）
/// </summary>
/// <remarks>
/// Mentorship に紐づく相談トピック。Message を子エンティティとして持つ。
/// Message の追加・参照は Topic を経由して行う（集約の境界）。
/// </remarks>
public class Topic
{
    public const int TitleMaxLength = 200;

    public Guid Id { get; private set; }

    public Guid MentorshipId { get; private set; }

    public string Title { get; private set; } = null!;

    public TopicStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Mentorship? Mentorship { get; private set; }

    private readonly List<Message> _messages = [];

    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    // EF Core 用
    private Topic() { }

    public Topic(Guid mentorshipId, string? title, DateTimeOffset createdAt)
    {
        Validate(mentorshipId, title).ThrowIfInvalid();

        Id = Guid.NewGuid();
        MentorshipId = mentorshipId;
        Title = title!;
        Status = TopicStatus.Open;
        CreatedAt = createdAt;
    }

    public static IEnumerable<ValidationError> Validate(Guid mentorshipId, string? title)
    {
        return ValidateMentorshipId(mentorshipId).ToValidationErrors(nameof(MentorshipId))
            .Concat(ValidateTitle(title).ToValidationErrors(nameof(Title)));
    }

    public static IEnumerable<string> ValidateMentorshipId(Guid mentorshipId)
    {
        if (mentorshipId == Guid.Empty)
            yield return "メンタリングを選択してください。";
    }

    public static IEnumerable<string> ValidateTitle(string? title)
        => ValidationHelper.ValidateRequiredMaxLength(title, TitleMaxLength, "タイトル");

    public Message PostMessage(Guid senderUserId, string? content, DateTimeOffset sentAt)
    {
        if (Status == TopicStatus.Closed)
            throw new InvalidOperationException("クローズされたトピックにはメッセージを投稿できません。");

        var message = new Message(Id, senderUserId, content, sentAt);
        _messages.Add(message);
        return message;
    }

    public void Close()
    {
        if (Status == TopicStatus.Closed)
            throw new InvalidOperationException("既にクローズされています。");

        Status = TopicStatus.Closed;
    }
}
