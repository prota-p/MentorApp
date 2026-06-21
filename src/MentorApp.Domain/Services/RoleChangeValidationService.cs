using MentorApp.Domain.Models.Users;

namespace MentorApp.Domain.Services;

/// <summary>
/// ロール変更の可否を検証するドメインサービス
/// </summary>
/// <remarks>
/// User 集約と Mentorship 集約にまたがる不変条件
/// 「Active な Mentorship に参加中のユーザーはロールを変更できない」を検証する。
/// 永続化問い合わせは Application 層で行い、このサービスは取得済みの判定材料を使って最終判断を行う。
/// </remarks>
public class RoleChangeValidationService
{
    public void Validate(
        User user,
        Role newRole,
        bool participatesInActiveMentorship)
    {
        if (user.Role == newRole)
            return;

        if (participatesInActiveMentorship)
        {
            throw new InvalidOperationException(
                "Active なメンタリング関係が存在するため、ロールを変更できません。先にメンタリングを完了またはキャンセルしてください。");
        }
    }
}
