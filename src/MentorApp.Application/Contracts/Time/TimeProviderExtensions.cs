namespace MentorApp.Application.Contracts.Time;

/// <summary>
/// TimeProviderの拡張メソッド
/// </summary>
/// <remarks>
/// .NET 8以降では標準のTimeProviderを使用し、テスト時はFakeTimeProviderで時刻を制御する。
/// Domain層は「今が何時か」を知らず、時刻は引数として受け取る設計とする。
/// </remarks>
public static class TimeProviderExtensions
{
    public static DateTimeOffset UtcNow(this TimeProvider timeProvider)
        => timeProvider.GetUtcNow();
}
