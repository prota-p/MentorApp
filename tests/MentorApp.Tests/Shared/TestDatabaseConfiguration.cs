namespace MentorApp.Tests.Shared;

/// <summary>
/// テスト用データベースの名前と接続文字列を生成するユーティリティ
/// </summary>
public static class TestDatabaseConfiguration
{
    public static string CreateUniqueDatabaseName()
        => $"MentorApp_Test_{Guid.NewGuid():N}";

    public static string CreateConnectionString(string databaseName)
        => $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=true;MultipleActiveResultSets=true";
}
