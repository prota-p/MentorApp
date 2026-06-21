using MentorApp.Domain.Models.Users;

namespace MentorApp.Domain.Services;

/// <summary>
/// Mentorship の作成可否を検証するドメインサービス
/// </summary>
/// <remarks>
/// Mentorship 作成時の複数のビジネス不変条件（Role 検証、同一性チェック、重複チェック）を
/// 統合的に検証するため、ドメインサービスとして実装。
/// 単なる「複数集約参照」ではなく、「複数のビジネスルールを組み合わせた評価」を行う点が、
/// Repository メソッドではなく Domain Service とする理由。
/// 永続化問い合わせは Application 層で行い、このサービスは取得済みの判定材料を使って最終判断を行う。
/// </remarks>
public class MentorshipDuplicationCheckService
{
    public void ValidateMentorshipCreation(
        User mentor,
        User mentee,
        bool hasActiveMentorshipForPair)
    {
        if (mentor.Role != Role.Mentor)
        {
            throw new InvalidOperationException(
                $"ユーザー {mentor.Id} は Mentor ロールではありません（現在のロール: {mentor.Role}）");
        }

        if (mentee.Role != Role.Mentee)
        {
            throw new InvalidOperationException(
                $"ユーザー {mentee.Id} は Mentee ロールではありません（現在のロール: {mentee.Role}）");
        }

        if (mentor.Id == mentee.Id)
        {
            throw new InvalidOperationException(
                "Mentor と Mentee は異なるユーザーである必要があります");
        }

        if (hasActiveMentorshipForPair)
        {
            throw new InvalidOperationException(
                "この Mentor と Mentee の組み合わせで、既にアクティブな Mentorship が存在します");
        }
    }
}
