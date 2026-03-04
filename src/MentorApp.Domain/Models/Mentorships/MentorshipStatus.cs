namespace MentorApp.Domain.Models.Mentorships;

/// <summary>
/// メンタリング関係のステータス
/// </summary>
public enum MentorshipStatus
{
    /// <summary>進行中</summary>
    Active = 0,

    /// <summary>完了</summary>
    Completed = 1,

    /// <summary>キャンセル</summary>
    Cancelled = 2
}
