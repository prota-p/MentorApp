using MentorApp.Application.Contracts.Authentication;
using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Users;
using MentorApp.Domain.Services;
using Microsoft.Extensions.Logging;

namespace MentorApp.Application.Mentorships;

public record CreateMentorshipRequest(Guid MentorUserId, Guid MenteeUserId);

/// <summary>
/// Mentorship に関するアプリケーションサービス（Command側）
/// </summary>
/// <remarks>
/// <para>
/// アプリケーション層の責務として、トランザクション境界の制御、ドメインサービスの呼び出し、
/// 構造化ログの記録を担当。すべてのpublicメソッドで例外をキャッチし、ログ記録後に再スローする。
/// </para>
/// <para>
/// CQRSパターンにおけるCommand側の責務を担当。
/// 状態変更操作（作成、更新、削除）のみを提供し、
/// 一覧取得などのQuery操作はIMentorshipQueryServiceが担当する。
/// </para>
/// </remarks>
public class MentorshipService(
    IUnitOfWorkFactory unitOfWorkFactory,
    MentorshipDuplicationCheckService duplicationCheckService,
    TimeProvider timeProvider,
    ILogger<MentorshipService> logger)
{
    public async Task<Mentorship> CreateMentorshipAsync(
        CreateMentorshipRequest request,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (currentUser.Role != Role.Admin)
                throw new UnauthorizedAccessException("メンタリングを作成できるのは管理者のみです。");

            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            // Mentor と Mentee の存在確認
            var mentor = await uow.Users.FindByIdAsync(request.MentorUserId, cancellationToken)
                ?? throw new ArgumentException($"メンターユーザーが見つかりません: {request.MentorUserId}");

            var mentee = await uow.Users.FindByIdAsync(request.MenteeUserId, cancellationToken)
                ?? throw new ArgumentException($"メンティーユーザーが見つかりません: {request.MenteeUserId}");

            // ドメインサービスで複数のビジネスルール（Role検証、同一性チェック、重複チェック）を統合的に検証
            await duplicationCheckService.ValidateMentorshipCreationAsync(
                mentor,
                mentee,
                uow.Mentorships,
                cancellationToken);

            var now = timeProvider.GetUtcNow();
            var mentorship = new Mentorship(request.MentorUserId, request.MenteeUserId, now);
            await uow.Mentorships.AddAsync(mentorship, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "メンタリング関係を作成しました: MentorshipId={MentorshipId}, MentorUserId={MentorUserId}, MenteeUserId={MenteeUserId}",
                mentorship.Id, request.MentorUserId, request.MenteeUserId);

            return mentorship;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "メンタリング関係の作成に失敗しました: MentorUserId={MentorUserId}, MenteeUserId={MenteeUserId}",
                request.MentorUserId, request.MenteeUserId);
            throw;
        }
    }

    public async Task<Mentorship> CompleteMentorshipAsync(
        Guid mentorshipId,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var mentorship = await uow.Mentorships.FindByIdAsync(mentorshipId, cancellationToken)
                ?? throw new KeyNotFoundException($"メンタリング関係が見つかりません: {mentorshipId}");

            var isMentor = currentUser.Role == Role.Mentor && mentorship.MentorUserId == currentUser.UserId;
            if (currentUser.Role != Role.Admin && !isMentor)
                throw new UnauthorizedAccessException("このメンタリングを完了する権限がありません。");

            var now = timeProvider.GetUtcNow();
            mentorship.Complete(now);
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "メンタリング関係を完了しました: MentorshipId={MentorshipId}, MentorUserId={MentorUserId}, MenteeUserId={MenteeUserId}",
                mentorship.Id, mentorship.MentorUserId, mentorship.MenteeUserId);

            return mentorship;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "メンタリング関係の完了処理に失敗しました: {MentorshipId}", mentorshipId);
            throw;
        }
    }

    public async Task<Mentorship> CancelMentorshipAsync(
        Guid mentorshipId,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var mentorship = await uow.Mentorships.FindByIdAsync(mentorshipId, cancellationToken)
                ?? throw new KeyNotFoundException($"メンタリング関係が見つかりません: {mentorshipId}");

            if (currentUser.Role != Role.Admin)
                throw new UnauthorizedAccessException("このメンタリングを中止する権限がありません。");

            var now = timeProvider.GetUtcNow();
            mentorship.Cancel(now);
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "メンタリング関係をキャンセルしました: MentorshipId={MentorshipId}, MentorUserId={MentorUserId}, MenteeUserId={MenteeUserId}",
                mentorship.Id, mentorship.MentorUserId, mentorship.MenteeUserId);

            return mentorship;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "メンタリング関係のキャンセルに失敗しました: {MentorshipId}", mentorshipId);
            throw;
        }
    }

    /// <remarks>
    /// トピックが存在する Mentorship は削除不可。履歴保持のため、完了または中止を使用する。
    /// </remarks>
    public async Task DeleteMentorshipAsync(
        Guid mentorshipId,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

            var mentorship = await uow.Mentorships.FindByIdAsync(mentorshipId, cancellationToken)
                ?? throw new KeyNotFoundException($"メンタリング関係が見つかりません: {mentorshipId}");

            if (currentUser.Role != Role.Admin)
                throw new UnauthorizedAccessException("メンタリングを削除できるのは管理者のみです。");

            if (await uow.Topics.HasAnyByMentorshipIdAsync(mentorshipId, cancellationToken))
                throw new InvalidOperationException("トピックが存在するメンタリングは削除できません。終了する場合は完了または中止を使用してください。");

            uow.Mentorships.Delete(mentorship);
            await uow.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "メンタリング関係を削除しました: MentorshipId={MentorshipId}, MentorUserId={MentorUserId}, MenteeUserId={MenteeUserId}",
                mentorshipId, mentorship.MentorUserId, mentorship.MenteeUserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "メンタリング関係の削除に失敗しました: {MentorshipId}", mentorshipId);
            throw;
        }
    }
}
