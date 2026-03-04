using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Users;

namespace MentorApp.Domain.Models.Mentorships;

/// <summary>
/// メンタリング関係（集約根）
/// </summary>
/// <remarks>
/// Mentor と Mentee のペアを表す。
/// Topic はこの Mentorship に紐づくが、別集約として管理される。
/// </remarks>
public class Mentorship
{
    public Guid Id { get; private set; }

    public Guid MentorUserId { get; private set; }

    public Guid MenteeUserId { get; private set; }

    public MentorshipStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public User? MentorUser { get; private set; }

    public User? MenteeUser { get; private set; }

    // EF Core 用
    private Mentorship() { }

    public Mentorship(Guid mentorUserId, Guid menteeUserId, DateTimeOffset startedAt)
    {
        Validate(mentorUserId, menteeUserId).ThrowIfInvalid();

        Id = Guid.NewGuid();
        MentorUserId = mentorUserId;
        MenteeUserId = menteeUserId;
        Status = MentorshipStatus.Active;
        StartedAt = startedAt;
    }

    public static IEnumerable<ValidationError> Validate(Guid mentorUserId, Guid menteeUserId)
    {
        return ValidateMentorUserId(mentorUserId).ToValidationErrors(nameof(MentorUserId))
            .Concat(ValidateMenteeUserId(menteeUserId).ToValidationErrors(nameof(MenteeUserId)))
            .Concat(ValidateDifferentUsers(mentorUserId, menteeUserId).ToValidationErrors(nameof(MenteeUserId)));
    }

    public static IEnumerable<string> ValidateMentorUserId(Guid mentorUserId)
    {
        if (mentorUserId == Guid.Empty)
            yield return "メンターは必須です。";
    }

    public static IEnumerable<string> ValidateMenteeUserId(Guid menteeUserId)
    {
        if (menteeUserId == Guid.Empty)
            yield return "メンティーは必須です。";
    }

    public static IEnumerable<string> ValidateDifferentUsers(Guid mentorUserId, Guid menteeUserId)
    {
        if (mentorUserId != Guid.Empty && menteeUserId != Guid.Empty && mentorUserId == menteeUserId)
            yield return "メンターとメンティーは異なるユーザーである必要があります。";
    }

    public bool IsParticipant(Guid userId) =>
        MentorUserId == userId || MenteeUserId == userId;

    public void Complete(DateTimeOffset endedAt)
    {
        if (Status != MentorshipStatus.Active)
            throw new InvalidOperationException("Active 状態の Mentorship のみ完了にできます。");

        Status = MentorshipStatus.Completed;
        EndedAt = endedAt;
    }

    public void Cancel(DateTimeOffset endedAt)
    {
        if (Status != MentorshipStatus.Active)
            throw new InvalidOperationException("Active 状態の Mentorship のみキャンセルできます。");

        Status = MentorshipStatus.Cancelled;
        EndedAt = endedAt;
    }
}
