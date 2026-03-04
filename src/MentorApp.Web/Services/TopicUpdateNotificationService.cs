namespace MentorApp.Web.Services;

/// <summary>
/// トピック更新の通知を管理するサービス。
/// Singleton で登録し、サーバー内の全ユーザーに更新を通知する。
/// </summary>
public class TopicUpdateNotificationService : IDisposable
{
    public event Action<Guid>? OnTopicUpdated;

    public void NotifyTopicUpdated(Guid topicId)
    {
        OnTopicUpdated?.Invoke(topicId);
    }

    public void Dispose()
    {
        OnTopicUpdated = null;
    }
}
