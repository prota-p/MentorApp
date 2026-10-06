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
    /// 対象ユーザーが既に存在する場合はロールを変更しない（意図して降格した管理者を、再起動で戻さないため）。
    /// ただし管理者が1人もいない場合は、画面から回復できないため Admin に昇格する。
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

        var existingUser = await uow.Users.FindByExternalIdAsync(externalId);
        if (existingUser != null)
        {
            if (existingUser.Role == Role.Admin)
            {
                logger.LogInformation("初期管理者は既にAdminとして存在します: {DisplayName}", existingUser.DisplayName);
                return;
            }

            if (await uow.Users.HasAnyByRoleAsync(Role.Admin))
            {
                logger.LogInformation("初期管理者のユーザーはAdminではありませんが、ほかにAdminがいるためロールは変更しません: {DisplayName} (Role: {Role})",
                    existingUser.DisplayName, existingUser.Role);
                return;
            }

            var previousRole = existingUser.Role;
            existingUser.ChangeRole(Role.Admin);
            await uow.SaveChangesAsync();

            logger.LogWarning("管理者が1人もいないため、初期管理者のユーザーをAdminに昇格しました: {DisplayName} (ExternalId: {ExternalId}, PreviousRole: {PreviousRole})",
                existingUser.DisplayName, externalId, previousRole);
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
