namespace MentorApp.Domain.Models.Topics;

/// <summary>
/// トピックのステータス
/// </summary>
public enum TopicStatus
{
    /// <summary>オープン（投稿可能）</summary>
    Open = 0,

    /// <summary>クローズ（投稿不可）</summary>
    Closed = 1
}
