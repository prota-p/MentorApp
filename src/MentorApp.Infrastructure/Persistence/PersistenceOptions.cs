using System.ComponentModel.DataAnnotations;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// データベース接続のオプション設定
/// </summary>
/// <remarks>
/// プロバイダーの切り替えは設定ファイルで行う。
/// 開発・本番環境では SQL Server、テスト環境では InMemory または SQLite を使用する。
/// </remarks>
internal class PersistenceOptions
{
    public const string SectionName = "Persistence";

    [Required(ErrorMessage = "データベースプロバイダーは必須です")]
    public string Provider { get; set; } = null!;

    /// <remarks>
    /// 開発環境専用。本番環境ではセキュリティリスクがあるため無効にすること。
    /// </remarks>
    public bool EnableSensitiveDataLogging { get; set; }

    /// <remarks>
    /// 開発環境専用。本番環境では内部情報の漏洩リスクがあるため無効にすること。
    /// </remarks>
    public bool EnableDetailedErrors { get; set; }

    [Required(ErrorMessage = "プロバイダー設定は必須です")]
    public DatabaseProviderOptions Providers { get; set; } = null!;
}

internal static class DatabaseProviders
{
    public const string SqlServer = "SqlServer";
    public const string Sqlite = "Sqlite";
    public const string InMemory = "InMemory";
}

internal class DatabaseProviderOptions
{
    public SqlServerOptions? SqlServer { get; set; }
    public SqliteOptions? Sqlite { get; set; }
    public InMemoryOptions? InMemory { get; set; }
}
internal class SqlServerOptions
{
    [Required(ErrorMessage = "SQL Server接続文字列は必須です")]
    public string ConnectionString { get; set; } = null!;

    public int CommandTimeout { get; set; } = 30;

    public bool EnableRetryOnFailure { get; set; } = true;

    public int MaxRetryCount { get; set; } = 3;
}

internal class SqliteOptions
{
    /// <remarks>
    /// 外部キー制約を有効化する場合は "Foreign Keys=True" を含めること。
    /// </remarks>
    [Required(ErrorMessage = "SQLite接続文字列は必須です")]
    public string ConnectionString { get; set; } = null!;

    public int CommandTimeout { get; set; } = 30;
}

internal class InMemoryOptions
{
    /// <remarks>
    /// テストごとに異なる名前を使用することで、テスト間のデータ分離が可能。
    /// </remarks>
    [Required(ErrorMessage = "InMemoryデータベース名は必須です")]
    public string DatabaseName { get; set; } = null!;
}
