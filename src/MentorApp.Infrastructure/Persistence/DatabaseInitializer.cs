using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// データベースの作成・削除を担当するクラス
/// </summary>
/// <remarks>
/// EnsureCreated / EnsureDeleted などのデータベース操作のみを担当する。
/// シード処理は <see cref="DataSeeder"/> が担当する。
/// </remarks>
internal static class DatabaseInitializer
{
    /// <summary>
    /// データベースを作成する（EnsureCreated）
    /// </summary>
    public static async Task EnsureCreatedAsync(IServiceProvider serviceProvider)
    {
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer));

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();

        logger.LogInformation("データベースを作成しました");
    }

    /// <summary>
    /// データベースを削除する（EnsureDeleted）
    /// </summary>
    public static async Task EnsureDeletedAsync(IServiceProvider serviceProvider)
    {
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer));

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();

        logger.LogInformation("データベースを削除しました");
    }
}
