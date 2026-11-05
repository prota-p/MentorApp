namespace MentorApp.Infrastructure;

/// <summary>
/// Web層（ホスト）がコードで決める認証の設定
/// </summary>
/// <remarks>
/// アプリの作りで決まる値。配置先ごとに変わる値は設定ファイルから読む（AuthenticationConfigOptions）。
/// </remarks>
public sealed record AuthenticationHostOptions
{
    /// <summary>認証関連のルーティングパス</summary>
    public required AuthenticationPathOptions Paths { get; init; }

    /// <summary>Mock 認証を許可するか（どの環境で許可するかは Web 層が決める）</summary>
    public required bool AllowMockProvider { get; init; }
}
