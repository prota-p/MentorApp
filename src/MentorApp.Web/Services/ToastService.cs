namespace MentorApp.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public class ToastMessage
{
    public Guid Id { get; }
    public string Message { get; }
    public ToastType Type { get; }
    internal Timer? Timer { get; set; }

    public ToastMessage(Guid id, string message, ToastType type)
    {
        Id = id;
        Message = message;
        Type = type;
    }
}

/// <summary>
/// トースト通知の状態を管理するサービス。
/// Scoped で登録し、ユーザーセッションごとに状態を保持する。
/// </summary>
public class ToastService : IDisposable
{
    private readonly List<ToastMessage> _messages = [];
    private readonly int _autoHideDelayMs = 4000;

    public event Action? OnChange;

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public void ShowSuccess(string message)
        => Show(message, ToastType.Success);

    public void ShowError(string message)
        => Show(message, ToastType.Error);

    public void ShowWarning(string message)
        => Show(message, ToastType.Warning);

    public void ShowInfo(string message)
        => Show(message, ToastType.Info);

    public void Remove(Guid id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message is not null)
        {
            message.Timer?.Dispose();
            _messages.Remove(message);
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
        foreach (var message in _messages)
        {
            message.Timer?.Dispose();
        }
        _messages.Clear();
        OnChange?.Invoke();
    }

    private void Show(string message, ToastType type)
    {
        var toastMessage = new ToastMessage(Guid.NewGuid(), message, type);
        _messages.Add(toastMessage);

        // タイマーのコールバックは別スレッドで実行される
        // コールバック内でTimerを保持しないことで、循環参照を回避
        toastMessage.Timer = new Timer(
            callback: _ => Remove(toastMessage.Id),
            state: null,
            dueTime: _autoHideDelayMs,
            period: Timeout.Infinite
        );

        OnChange?.Invoke();
    }

    public void Dispose()
    {
        foreach (var message in _messages)
        {
            message.Timer?.Dispose();
        }
        _messages.Clear();
    }
}
