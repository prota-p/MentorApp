using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;
using Microsoft.Extensions.Logging;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// データベースのシード処理を担当するクラス
/// </summary>
/// <remarks>
/// 初期管理者と開発用サンプルデータの投入を担当する。
/// UoW / Repository を使用し、DbContext を直接操作しない。
/// </remarks>
internal static class DataSeeder
{
    // ----------------------------------------
    // 定数・静的データ
    // ----------------------------------------

    private static readonly TimeSpan DefaultTimeIncrement = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TopicTimeIncrement = TimeSpan.FromMinutes(5);

    private static readonly DevUserDefinition[] DevelopmentUsers =
    [
        new(DevUserIds.Admin1, "管理者(ダミー)", "admin1@example.com", Role.Admin),
        new(DevUserIds.Mentor1, "メンター太郎(ダミー)", "mentor1@example.com", Role.Mentor),
        new(DevUserIds.Mentor2, "メンター次郎(ダミー)", "mentor2@example.com", Role.Mentor),
        new(DevUserIds.Mentee1, "メンティー花子(ダミー)", "mentee1@example.com", Role.Mentee),
        new(DevUserIds.Mentee2, "メンティー桜(ダミー)", "mentee2@example.com", Role.Mentee),
        new(DevUserIds.Mentee3, "メンティー梅(ダミー)", "mentee3@example.com", Role.Mentee)
    ];

    private static readonly DevMentorshipDefinition[] DevelopmentMentorships =
    [
        new(DevUserIds.Mentor1, DevUserIds.Mentee1),
        new(DevUserIds.Mentor1, DevUserIds.Mentee2),
        new(DevUserIds.Mentor2, DevUserIds.Mentee2),
        new(DevUserIds.Mentor2, DevUserIds.Mentee3)
    ];

    private static readonly DevTopicDefinition[] DevelopmentTopics =
    [
        // メンター太郎 - メンティー花子
        new(DevUserIds.Mentor1, DevUserIds.Mentee1, "初めての相談",
        [
            new(DevUserIds.Mentee1, "はじめまして！よろしくお願いします。"),
            new(DevUserIds.Mentor1, "こちらこそよろしくお願いします。何でも聞いてくださいね。"),
            new(DevUserIds.Mentee1, "ありがとうございます！早速ですが、プロジェクトの進め方について相談させてください。")
        ]),
        new(DevUserIds.Mentor1, DevUserIds.Mentee1, "キャリアパスについて",
        [
            new(DevUserIds.Mentee1, "将来のキャリアパスについて悩んでいます。"),
            new(DevUserIds.Mentor1, "具体的にどのような方向性を考えていますか？"),
            new(DevUserIds.Mentee1, "技術スペシャリストかマネジメントか迷っています。")
        ]),
        // メンター太郎 - メンティー桜
        new(DevUserIds.Mentor1, DevUserIds.Mentee2, "技術的な質問",
        [
            new(DevUserIds.Mentee2, "C#の非同期処理について教えてください。"),
            new(DevUserIds.Mentor1, "async/awaitの基本から説明しますね。")
        ]),
        // メンター次郎 - メンティー桜
        new(DevUserIds.Mentor2, DevUserIds.Mentee2, "コードレビューのお願い",
        [
            new(DevUserIds.Mentee2, "作成したコードのレビューをお願いできますか？"),
            new(DevUserIds.Mentor2, "もちろんです。どの部分を見てほしいですか？"),
            new(DevUserIds.Mentee2, "リポジトリパターンの実装です。"),
            new(DevUserIds.Mentor2, "確認しました。いくつか改善点がありますね。")
        ]),
        // メンター次郎 - メンティー梅
        new(DevUserIds.Mentor2, DevUserIds.Mentee3, "学習計画の相談",
        [
            new(DevUserIds.Mentee3, "効果的な学習方法について教えてください。"),
            new(DevUserIds.Mentor2, "まず基礎をしっかり固めることが重要です。")
        ]),
        new(DevUserIds.Mentor2, DevUserIds.Mentee3, "アーキテクチャ設計",
        [
            new(DevUserIds.Mentee3, "クリーンアーキテクチャについて詳しく知りたいです。"),
            new(DevUserIds.Mentor2, "良い質問ですね。依存関係の逆転から説明しましょう。"),
            new(DevUserIds.Mentee3, "なるほど、レイヤー間の依存関係が重要なんですね。")
        ])
    ];

    // ----------------------------------------
    // パブリックメソッド
    // ----------------------------------------

    /// <summary>
    /// 初期管理者をシードする
    /// </summary>
    /// <remarks>
    /// appsettings.jsonの設定値に基づき、指定されたExternalIdを持つ
    /// Admin権限のユーザーを作成する。本番環境でも使用する。
    /// </remarks>
    public static async Task SeedInitialAdminAsync(
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

    /// <summary>
    /// 開発用データをシードする
    /// </summary>
    /// <remarks>
    /// 開発環境（Development）でのみ使用する。
    /// 6名のモックユーザー（admin×1, mentor×2, mentee×3）と関連データを作成する。
    /// </remarks>
    public static async Task SeedDevelopmentDataAsync(
        IUnitOfWorkFactory uowFactory,
        DateTimeOffset baseTime,
        ILogger logger)
    {
        var timeSeq = new TimeSequence(baseTime);

        var users = await SeedUsersAsync(uowFactory, timeSeq, logger);
        var mentorships = await SeedMentorshipsAsync(uowFactory, users, timeSeq, logger);
        await SeedTopicsAndMessagesAsync(uowFactory, mentorships, users, timeSeq, logger);
    }

    // ----------------------------------------
    // プライベートメソッド
    // ----------------------------------------

    private static async Task<UserLookup> SeedUsersAsync(
        IUnitOfWorkFactory uowFactory,
        TimeSequence timeSeq,
        ILogger logger)
    {
        await using var uow = await uowFactory.CreateAsync();
        var addedCount = 0;
        var userDict = new Dictionary<string, User>();

        foreach (var def in DevelopmentUsers)
        {
            var existingUser = await uow.Users.FindByExternalIdAsync(def.ExternalId);
            if (existingUser != null)
            {
                userDict[def.ExternalId] = existingUser;
                logger.LogDebug("開発用ユーザーは既に存在します: {ExternalId}", def.ExternalId);
                continue;
            }

            var user = new User(def.ExternalId, def.DisplayName, timeSeq.Next(), def.Email, def.Role);
            await uow.Users.AddAsync(user);
            userDict[def.ExternalId] = user;
            addedCount++;

            logger.LogDebug("開発用ユーザーを追加しました: {DisplayName} (ExternalId: {ExternalId})",
                def.DisplayName, def.ExternalId);
        }

        if (addedCount > 0)
        {
            await uow.SaveChangesAsync();
            logger.LogInformation("開発用ユーザーをシードしました: {Count}名のユーザーを追加", addedCount);
        }
        else
        {
            logger.LogInformation("開発用ユーザーは全て存在しています。追加なし。");
        }

        return new UserLookup(userDict);
    }

    private static async Task<MentorshipLookup> SeedMentorshipsAsync(
        IUnitOfWorkFactory uowFactory,
        UserLookup users,
        TimeSequence timeSeq,
        ILogger logger)
    {
        await using var uow = await uowFactory.CreateAsync();
        var mentorshipDict = new Dictionary<(string, string), Mentorship>();
        var addedCount = 0;

        foreach (var def in DevelopmentMentorships)
        {
            var mentorId = users.GetId(def.MentorExternalId);
            var menteeId = users.GetId(def.MenteeExternalId);

            var existingMentorship = await uow.Mentorships.FindByMentorAndMenteeAsync(mentorId, menteeId);
            if (existingMentorship != null)
            {
                mentorshipDict[(def.MentorExternalId, def.MenteeExternalId)] = existingMentorship;
                logger.LogDebug("開発用 Mentorship は既に存在します: {MentorId} - {MenteeId}",
                    def.MentorExternalId, def.MenteeExternalId);
                continue;
            }

            var mentorship = new Mentorship(mentorId, menteeId, timeSeq.Next());
            await uow.Mentorships.AddAsync(mentorship);
            mentorshipDict[(def.MentorExternalId, def.MenteeExternalId)] = mentorship;
            addedCount++;

            logger.LogDebug("開発用 Mentorship を追加しました: {MentorId} - {MenteeId}",
                def.MentorExternalId, def.MenteeExternalId);
        }

        if (addedCount > 0)
        {
            await uow.SaveChangesAsync();
            logger.LogInformation("開発用 Mentorship をシードしました: {Count}件の関係を追加", addedCount);
        }
        else
        {
            logger.LogInformation("開発用 Mentorship は全て存在しています。追加なし。");
        }

        return new MentorshipLookup(mentorshipDict);
    }

    private static async Task SeedTopicsAndMessagesAsync(
        IUnitOfWorkFactory uowFactory,
        MentorshipLookup mentorships,
        UserLookup users,
        TimeSequence timeSeq,
        ILogger logger)
    {
        await using var uow = await uowFactory.CreateAsync();
        var addedCount = 0;

        foreach (var def in DevelopmentTopics)
        {
            var mentorship = mentorships.Get(def.MentorExternalId, def.MenteeExternalId);

            var existingTopic = await uow.Topics.FindByMentorshipAndTitleAsync(mentorship.Id, def.Title);
            if (existingTopic != null)
            {
                logger.LogDebug("開発用 Topic は既に存在します: {Title}", def.Title);
                continue;
            }

            var topic = new Topic(mentorship.Id, def.Title, timeSeq.Next(TopicTimeIncrement));

            foreach (var msgDef in def.Messages)
            {
                topic.PostMessage(users.GetId(msgDef.SenderExternalId), msgDef.Content, timeSeq.Next());
            }

            await uow.Topics.AddAsync(topic);
            addedCount++;

            logger.LogDebug("開発用 Topic を追加しました: {Title} ({MessageCount}件のメッセージ)",
                def.Title, topic.Messages.Count);
        }

        if (addedCount > 0)
        {
            await uow.SaveChangesAsync();
            logger.LogInformation("開発用 Topic と Message をシードしました: {Count}件のトピックを追加", addedCount);
        }
        else
        {
            logger.LogInformation("開発用 Topic は全て存在しています。追加なし。");
        }
    }

    // ----------------------------------------
    // ネストした型（定義・ヘルパー）
    // ----------------------------------------

    private static class DevUserIds
    {
        public const string Admin1 = "dummy_extid_admin1";
        public const string Mentor1 = "dummy_extid_mentor1";
        public const string Mentor2 = "dummy_extid_mentor2";
        public const string Mentee1 = "dummy_extid_mentee1";
        public const string Mentee2 = "dummy_extid_mentee2";
        public const string Mentee3 = "dummy_extid_mentee3";
    }

    private record DevUserDefinition(string ExternalId, string DisplayName, string Email, Role Role);
    private record DevMentorshipDefinition(string MentorExternalId, string MenteeExternalId);
    private record DevTopicDefinition(string MentorExternalId, string MenteeExternalId, string Title, DevMessageDefinition[] Messages);
    private record DevMessageDefinition(string SenderExternalId, string Content);

    private sealed class TimeSequence(DateTimeOffset baseTime)
    {
        private DateTimeOffset _current = baseTime;

        public DateTimeOffset Next(TimeSpan? increment = null)
        {
            var result = _current;
            _current = _current.Add(increment ?? DefaultTimeIncrement);
            return result;
        }
    }

    private sealed class UserLookup(Dictionary<string, User> users)
    {
        public Guid GetId(string externalId) => users[externalId].Id;
    }

    private sealed class MentorshipLookup(Dictionary<(string, string), Mentorship> mentorships)
    {
        public Mentorship Get(string mentorExternalId, string menteeExternalId)
            => mentorships[(mentorExternalId, menteeExternalId)];
    }
}
