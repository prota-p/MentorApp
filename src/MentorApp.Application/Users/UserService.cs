using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Users;
using MentorApp.Domain.Services;
using Microsoft.Extensions.Logging;

namespace MentorApp.Application.Users;

public record ChangeRoleRequest(Guid UserId, Role NewRole);

public record UpdateDisplayNameRequest(Guid UserId, string DisplayName);

public record UpdateUserRequest(Guid UserId, string DisplayName, Role Role);

/// <summary>
/// User エンティティに関するアプリケーションサービス（Command側）
/// </summary>
/// <remarks>
/// <para>
/// アプリケーション層の責務として、トランザクション境界の制御と構造化ログの記録を担当。
/// すべてのpublicメソッドで例外をキャッチし、ログ記録後に再スローする。
/// </para>
/// <para>
/// CQRSパターンにおけるCommand側の責務を担当。
/// 状態変更操作（作成、更新、削除）のみを提供し、
/// 一覧取得などのQuery操作はIUserQueryServiceが担当する。
/// </para>
/// </remarks>
public class UserService(
    IUnitOfWorkFactory unitOfWorkFactory,
    RoleChangeValidationService roleChangeValidation,
    ILogger<UserService> logger)
{
    public async Task<User> ChangeRoleAsync(ChangeRoleRequest request, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            if (currentUser.Role != Role.Admin)
                throw new UnauthorizedAccessException("ロールの変更は管理者のみ可能です。");

            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var user = await uow.Users.FindByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("ロール変更対象のユーザーが見つかりませんでした: {UserId}", request.UserId);
                throw new KeyNotFoundException($"ユーザー {request.UserId} が見つかりません");
            }

            var oldRole = user.Role;
            var participatesInActiveMentorship = await uow.Mentorships.HasAnyActiveMentorshipByUserIdAsync(
                user.Id,
                cancellationToken);

            roleChangeValidation.Validate(user, request.NewRole, participatesInActiveMentorship);
            user.ChangeRole(request.NewRole);

            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "ユーザーのロールを変更しました: UserId={UserId}, OldRole={OldRole}, NewRole={NewRole}",
                user.Id, oldRole, request.NewRole);

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ユーザーのロール変更に失敗しました: {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<User> UpdateDisplayNameAsync(UpdateDisplayNameRequest request, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            if (currentUser.Role != Role.Admin && request.UserId != currentUser.UserId)
                throw new UnauthorizedAccessException("他のユーザーの表示名を更新する権限がありません。");

            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var user = await uow.Users.FindByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("表示名更新対象のユーザーが見つかりませんでした: {UserId}", request.UserId);
                throw new KeyNotFoundException($"ユーザー {request.UserId} が見つかりません");
            }

            var oldDisplayName = user.DisplayName;
            user.UpdateDisplayName(request.DisplayName);

            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "ユーザーの表示名を更新しました: UserId={UserId}, OldDisplayName={OldDisplayName}, NewDisplayName={NewDisplayName}",
                user.Id, oldDisplayName, request.DisplayName);

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "表示名の更新に失敗しました: {UserId}", request.UserId);
            throw;
        }
    }

    /// <summary>
    /// ユーザー情報（表示名・ロール）を更新する。
    /// </summary>
    /// <returns>更新されたユーザー。変更がなかった場合は null。</returns>
    public async Task<User?> UpdateUserAsync(UpdateUserRequest request, CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            if (currentUser.Role != Role.Admin && request.UserId != currentUser.UserId)
                throw new UnauthorizedAccessException("他のユーザーの情報を更新する権限がありません。");

            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var user = await uow.Users.FindByIdAsync(request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException($"ユーザー {request.UserId} が見つかりません");

            var hasDisplayNameChanged = request.DisplayName != user.DisplayName;
            var hasRoleChanged = request.Role != user.Role;

            if (hasRoleChanged && currentUser.Role != Role.Admin)
                throw new UnauthorizedAccessException("ロールの変更は管理者のみ可能です。");

            if (!hasDisplayNameChanged && !hasRoleChanged)
            {
                logger.LogInformation("ユーザーに変更はありませんでした: {UserId}", request.UserId);
                return null;
            }

            var oldDisplayName = user.DisplayName;
            var oldRole = user.Role;

            if (hasDisplayNameChanged)
            {
                user.UpdateDisplayName(request.DisplayName);
            }

            if (hasRoleChanged)
            {
                var participatesInActiveMentorship = await uow.Mentorships.HasAnyActiveMentorshipByUserIdAsync(
                    user.Id,
                    cancellationToken);

                roleChangeValidation.Validate(user, request.Role, participatesInActiveMentorship);
                user.ChangeRole(request.Role);
            }

            await uow.SaveChangesAsync(cancellationToken);

            if (hasDisplayNameChanged)
            {
                logger.LogInformation(
                    "ユーザーの表示名を更新しました: UserId={UserId}, OldDisplayName={OldDisplayName}, NewDisplayName={NewDisplayName}",
                    user.Id, oldDisplayName, request.DisplayName);
            }

            if (hasRoleChanged)
            {
                logger.LogInformation(
                    "ユーザーのロールを変更しました: UserId={UserId}, OldRole={OldRole}, NewRole={NewRole}",
                    user.Id, oldRole, request.Role);
            }

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ユーザーの更新に失敗しました: {UserId}", request.UserId);
            throw;
        }
    }
}
