using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Users;

namespace MentorApp.Domain.Services;

/// <summary>
/// ロール変更の可否を検証するドメインサービス
/// </summary>
/// <remarks>
/// User 集約と Mentorship 集約にまたがる不変条件
/// 「Active な Mentorship に参加中のユーザーはロールを変更できない」を検証する。
/// MentorshipDuplicationCheckService と同様に、リポジトリをパラメータとして受け取る。
/// </remarks>
public class RoleChangeValidationService
{
    public async Task ValidateAsync(
        User user,
        Role newRole,
        IMentorshipRepository mentorshipRepository,
        CancellationToken cancellationToken = default)
    {
        if (user.Role == newRole)
            return;

        var hasActiveMentorship = await mentorshipRepository.HasAnyActiveMentorshipByUserIdAsync(
            user.Id,
            cancellationToken);

        if (hasActiveMentorship)
        {
            throw new InvalidOperationException(
                "Active なメンタリング関係が存在するため、ロールを変更できません。先にメンタリングを完了またはキャンセルしてください。");
        }
    }
}
