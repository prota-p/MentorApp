namespace MentorApp.Web.Components.Shared;

/// <summary>
/// 日時フォーマット用の拡張メソッド
/// </summary>
/// <remarks>
/// DateTimeOffsetをローカルタイムゾーン（サーバー設定）で表示する。
/// Blazor Serverではサーバーのタイムゾーンに依存する点に注意。
/// </remarks>
public static class DateTimeExtensions
{
    /// <summary>
    /// 日付のみのフォーマット（yyyy/MM/dd）
    /// </summary>
    public static string FormatDate(this DateTimeOffset dateTime)
        => dateTime.ToLocalTime().ToString("yyyy/MM/dd");

    /// <summary>
    /// 日時のフォーマット（yyyy/MM/dd HH:mm）
    /// </summary>
    public static string FormatDateTime(this DateTimeOffset dateTime)
        => dateTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

    /// <summary>
    /// 詳細日時のフォーマット（yyyy年MM月dd日 HH:mm:ss）
    /// </summary>
    public static string FormatDateTimeFull(this DateTimeOffset dateTime)
        => dateTime.ToLocalTime().ToString("yyyy年MM月dd日 HH:mm:ss");
}
