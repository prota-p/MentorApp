using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Users;
using Microsoft.Extensions.Logging;

namespace MentorApp.Application.Users;

/// <summary>
/// 認証時にユーザーを取得または自動作成するサービスの実装
/// </summary>
/// <remarks>
/// Infrastructure層の認証パイプラインから呼び出される。
/// 初回ログイン時にユーザーを自動作成することで、
/// 認証が成功した時点で必ずUserエンティティが存在することを保証する。
/// Application層で処理することで、ユーザー作成のログ出力やトランザクション管理を統一できる。
/// </remarks>
internal sealed class UserAuthenticationService(
    IUnitOfWorkFactory unitOfWorkFactory,
    TimeProvider timeProvider,
    ILogger<UserAuthenticationService> logger) : IUserAuthenticationService
{
    public async Task<User> GetOrCreateUserAsync(
        string externalId,
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var user = await uow.Users.FindByExternalIdAsync(externalId, cancellationToken);
            if (user is not null)
            {
                logger.LogInformation(
                    "認証時にユーザーが見つかりました: ExternalId={ExternalId}, UserId={UserId}",
                    externalId, user.Id);
                return user;
            }

            var now = timeProvider.GetUtcNow();
            var newUser = new User(
                externalId: externalId,
                displayName: displayName,
                createdAt: now,
                email: email,
                role: Role.Mentee
            );

            await uow.Users.AddAsync(newUser, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "認証時に新規ユーザーを作成しました: UserId={UserId}, ExternalId={ExternalId}, DisplayName={DisplayName}, Email={Email}, Role={Role}",
                newUser.Id, externalId, displayName, email, Role.Mentee);

            return newUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "認証時のユーザー取得・作成に失敗しました: ExternalId={ExternalId}", externalId);
            throw;
        }
    }
}
