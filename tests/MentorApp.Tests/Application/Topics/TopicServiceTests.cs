using MentorApp.Application.Contracts.Authentication;
using MentorApp.Application.Contracts.Queries;
using MentorApp.Application.Topics;
using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Shared;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;
using MentorApp.Tests.Application.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MentorApp.Tests.Application.Topics;

/// <summary>
/// TopicServiceのApplication層統合テスト
/// </summary>
/// <remarks>
/// SQL Server LocalDBを使用し、テストごとに個別DBを作成・削除する。
/// </remarks>
public class TopicServiceTests : IAsyncLifetime
{
    private TestServiceProviderFactory _factory = null!;
    private ServiceProvider _serviceProvider = null!;
    private FakeTimeProvider _timeProvider = null!;

    public async ValueTask InitializeAsync()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero));
        _factory = new TestServiceProviderFactory();
        _serviceProvider = await _factory.CreateAsync(_timeProvider);
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task CreateTopic_Then_CanRetrieveTopic()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (mentor, mentee, mentorship) = await ArrangeMentorshipAsync(ct);
        const string testTopicTitle = "テストトピック";
        var topicService = _serviceProvider.GetRequiredService<TopicService>();
        var topicQuery = _serviceProvider.GetRequiredService<ITopicQueryService>();
        var request = new CreateTopicRequest(mentorship.Id, testTopicTitle);
        var mentorCurrentUser = new CurrentUser(mentor.Id, mentor.ExternalId, mentor.DisplayName, Role.Mentor);

        // Act
        var createdTopic = await topicService.CreateTopicAsync(request, mentorCurrentUser, ct);

        // Assert
        var retrievedTopic = await topicQuery.GetByIdAsync(createdTopic.Id, mentorCurrentUser, ct);

        Assert.NotNull(retrievedTopic);
        Assert.Equal(createdTopic.Id, retrievedTopic.Id);
        Assert.Equal(testTopicTitle, retrievedTopic.Title);
        Assert.Equal(mentorship.Id, retrievedTopic.MentorshipId);
        Assert.Equal(_timeProvider.GetUtcNow(), retrievedTopic.CreatedAt);
    }

    [Fact]
    public async Task PostMessage_Then_CanRetrieveMessageFromTopic()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (mentor, mentee, mentorship) = await ArrangeMentorshipAsync(ct);
        var topicService = _serviceProvider.GetRequiredService<TopicService>();
        var topicQuery = _serviceProvider.GetRequiredService<ITopicQueryService>();

        var mentorCurrentUser = new CurrentUser(mentor.Id, mentor.ExternalId, mentor.DisplayName, Role.Mentor);
        var topic = await topicService.CreateTopicAsync(
            new CreateTopicRequest(mentorship.Id, "相談トピック"), mentorCurrentUser, ct);

        // 投稿時刻がトピック作成時刻と異なることをAssertで検証するため
        _timeProvider.Advance(TimeSpan.FromHours(1));
        const string messageContent = "こんにちは、メンターです";

        // Act
        var postedMessage = await topicService.PostMessageAsync(
            new PostMessageRequest(topic.Id, mentor.Id, messageContent), mentorCurrentUser, ct);

        // Assert
        var retrievedTopic = await topicQuery.GetByIdAsync(topic.Id, mentorCurrentUser, ct);

        Assert.NotNull(retrievedTopic);
        var message = Assert.Single(retrievedTopic.Messages);
        Assert.Equal(postedMessage.Id, message.Id);
        Assert.Equal(mentor.Id, message.SenderUserId);
        Assert.Equal(messageContent, message.Content);
        Assert.Equal(_timeProvider.GetUtcNow(), message.SentAt);
    }

    [Fact]
    public async Task CloseTopic_Then_CannotPostMessage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (mentor, mentee, mentorship) = await ArrangeMentorshipAsync(ct);
        var topicService = _serviceProvider.GetRequiredService<TopicService>();
        var topicQuery = _serviceProvider.GetRequiredService<ITopicQueryService>();

        var mentorCurrentUser = new CurrentUser(mentor.Id, mentor.ExternalId, mentor.DisplayName, Role.Mentor);
        var topic = await topicService.CreateTopicAsync(
            new CreateTopicRequest(mentorship.Id, "クローズテスト"), mentorCurrentUser, ct);
        await topicService.CloseTopicAsync(topic.Id, mentorCurrentUser, ct);

        // Act
        var act = async () => await topicService.PostMessageAsync(
            new PostMessageRequest(topic.Id, mentor.Id, "クローズ後のメッセージ"), mentorCurrentUser, ct);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Contains("クローズされたトピックにはメッセージを投稿できません", ex.Message);

        var retrievedTopic = await topicQuery.GetByIdAsync(topic.Id, mentorCurrentUser, ct);
        Assert.Equal(TopicStatus.Closed, retrievedTopic!.Status);
        Assert.Empty(retrievedTopic.Messages);
    }

    private async Task<(User Mentor, User Mentee, Mentorship Mentorship)> ArrangeMentorshipAsync(
        CancellationToken cancellationToken = default)
    {
        var unitOfWorkFactory = _serviceProvider.GetRequiredService<IUnitOfWorkFactory>();
        await using var uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        var mentor = new User(
            externalId: $"mentor-{Guid.NewGuid()}",
            displayName: "テストメンター",
            createdAt: _timeProvider.GetUtcNow(),
            email: "mentor@example.com",
            role: Role.Mentor);
        await uow.Users.AddAsync(mentor, cancellationToken);

        var mentee = new User(
            externalId: $"mentee-{Guid.NewGuid()}",
            displayName: "テストメンティー",
            createdAt: _timeProvider.GetUtcNow(),
            email: "mentee@example.com",
            role: Role.Mentee);
        await uow.Users.AddAsync(mentee, cancellationToken);

        var mentorship = new Mentorship(mentor.Id, mentee.Id, _timeProvider.GetUtcNow());
        await uow.Mentorships.AddAsync(mentorship, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);

        return (mentor, mentee, mentorship);
    }
}
