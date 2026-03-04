using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;
using Microsoft.Extensions.Logging;

namespace MentorApp.Application.Topics;

public record CreateTopicRequest(Guid MentorshipId, string Title);

public record PostMessageRequest(Guid TopicId, Guid SenderUserId, string Content);

/// <summary>
/// Topic に関するアプリケーションサービス（Command側）
/// </summary>
/// <remarks>
/// <para>
/// アプリケーション層の責務として、トランザクション境界の制御、認可ロジックの実装、
/// 構造化ログの記録を担当。すべてのpublicメソッドで例外をキャッチし、ログ記録後に再スローする。
/// </para>
/// <para>
/// CQRSパターンにおけるCommand側の責務を担当。
/// 状態変更操作（作成、更新、削除）のみを提供し、
/// 一覧取得などのQuery操作はITopicQueryServiceが担当する。
/// </para>
/// </remarks>
public class TopicService(
    IUnitOfWorkFactory unitOfWorkFactory,
    TimeProvider timeProvider,
    ILogger<TopicService> logger)
{
    public async Task<Topic> CreateTopicAsync(
        CreateTopicRequest request,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            // Mentorship の存在確認
            var mentorship = await uow.Mentorships.FindByIdAsync(request.MentorshipId, cancellationToken)
                ?? throw new ArgumentException($"メンタリング関係が見つかりません: {request.MentorshipId}");

            if (!mentorship.IsParticipant(currentUser.UserId))
                throw new UnauthorizedAccessException("このメンタリングにトピックを作成する権限がありません。");

            if (mentorship.Status != MentorshipStatus.Active)
                throw new InvalidOperationException("Active 状態のメンタリングにのみトピックを作成できます。");

            var now = timeProvider.GetUtcNow();
            var topic = new Topic(request.MentorshipId, request.Title, now);
            await uow.Topics.AddAsync(topic, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "トピックを作成しました: TopicId={TopicId}, MentorshipId={MentorshipId}, Title={Title}",
                topic.Id, request.MentorshipId, request.Title);

            return topic;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "トピックの作成に失敗しました: MentorshipId={MentorshipId}, Title={Title}",
                request.MentorshipId, request.Title);
            throw;
        }
    }

    public async Task<Topic> CloseTopicAsync(
        Guid topicId,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var topic = await uow.Topics.FindByIdAsync(topicId, cancellationToken)
                ?? throw new KeyNotFoundException($"トピックが見つかりません: {topicId}");

            var mentorship = await uow.Mentorships.FindByIdAsync(topic.MentorshipId, cancellationToken)
                ?? throw new KeyNotFoundException($"メンタリング関係が見つかりません: {topic.MentorshipId}");

            if (!mentorship.IsParticipant(currentUser.UserId) && currentUser.Role != Role.Admin)
                throw new UnauthorizedAccessException("このトピックをクローズする権限がありません。");

            topic.Close();
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation("トピックをクローズしました: TopicId={TopicId}, Title={Title}",
                topic.Id, topic.Title);

            return topic;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "トピックのクローズに失敗しました: {TopicId}", topicId);
            throw;
        }
    }

    /// <remarks>
    /// request.SenderUserId と currentUser.UserId の一致確認により、
    /// 他ユーザーへのなりすまし投稿を防止する。
    /// </remarks>
    public async Task<Message> PostMessageAsync(
        PostMessageRequest request,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var topic = await uow.Topics.FindByIdAsync(request.TopicId, cancellationToken)
                ?? throw new KeyNotFoundException($"トピックが見つかりません: {request.TopicId}");

            var mentorship = await uow.Mentorships.FindByIdAsync(topic.MentorshipId, cancellationToken)
                ?? throw new KeyNotFoundException($"メンタリング関係が見つかりません: {topic.MentorshipId}");

            if (!mentorship.IsParticipant(currentUser.UserId))
                throw new UnauthorizedAccessException("このトピックにメッセージを投稿する権限がありません。");

            if (request.SenderUserId != currentUser.UserId)
                throw new UnauthorizedAccessException("他のユーザーとしてメッセージを投稿することはできません。");

            var now = timeProvider.GetUtcNow();
            var message = topic.PostMessage(request.SenderUserId, request.Content, now);
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "メッセージを投稿しました: MessageId={MessageId}, TopicId={TopicId}, SenderUserId={SenderUserId}",
                message.Id, request.TopicId, request.SenderUserId);

            return message;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "メッセージの投稿に失敗しました: TopicId={TopicId}, SenderUserId={SenderUserId}",
                request.TopicId, request.SenderUserId);
            throw;
        }
    }
}
