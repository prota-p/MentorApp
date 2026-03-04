namespace MentorApp.Web.Components.Shared;

/// <summary>スピナーの表示モード</summary>
public enum SpinnerMode
{
    /// <summary>SSR→Interactive 切り替え時（全画面）</summary>
    Overlay,
    /// <summary>ページ全体</summary>
    Page,
    /// <summary>カード・セクション内（デフォルト）</summary>
    Section,
    /// <summary>小領域・フォーム内</summary>
    Compact,
    /// <summary>ボタン内インライン</summary>
    Inline
}
