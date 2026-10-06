using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Users;
using Microsoft.Extensions.Logging;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// 初期管理者のシード処理を担当するクラス
/// </summary>
/// <remarks>
/// 本番環境でも使用する、運用に必要な初期データの投入を担当する。
/// UoW / Repository を使用し、DbContext を直接操作しない。
/// </remarks>
internal static class InitialAdminSeeder
{
    /// <summary>
    /// 初期管理者をシードする
    /// </summary>
    /// <remarks>
    /// appsettings.jsonの設定値に基づき、指定されたExternalIdを持つ
    /// Admin権限のユーザーを作成する。本番環境でも使用する。
    /// </remarks>
    public static async Task SeedAsync(
        IUnitOfWorkFactory uowFactory,
        TimeProvider timeProvider,
        string externalId,
        string displayName,
        string email,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            logger.LogWarning("初期管理者のExternalIdが設定されていません。スキップします。");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("初期管理者のEmailが設定されていません。スキップします。");
            return;
        }

        await using var uow = await uowFactory.CreateAsync();

        var existingAdmin = await uow.Users.FindByExternalIdAsync(externalId);
        if (existingAdmin != null)
        {
            logger.LogInformation("初期管理者は既に存在します: {DisplayName}", existingAdmin.DisplayName);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var admin = new User(externalId, displayName, now, email, Role.Admin);
        await uow.Users.AddAsync(admin);
        await uow.SaveChangesAsync();

        logger.LogInformation("初期管理者を作成しました: {DisplayName} (ExternalId: {ExternalId}, Email: {Email})",
            displayName, externalId, email);
    }
}
