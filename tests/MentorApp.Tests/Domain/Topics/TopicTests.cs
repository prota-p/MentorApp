using MentorApp.Domain.Models.Topics;

namespace MentorApp.Tests.Domain.Topics;

public class TopicTests
{
    [Fact]
    public void CreateTopic_Then_InitialStateIsOpen()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        const string title = "相談したいこと";

        // Act
        var topic = new Topic(mentorshipId, title, createdAt);

        // Assert
        Assert.NotEqual(Guid.Empty, topic.Id);
        Assert.Equal(mentorshipId, topic.MentorshipId);
        Assert.Equal(title, topic.Title);
        Assert.Equal(TopicStatus.Open, topic.Status);
        Assert.Equal(createdAt, topic.CreatedAt);
        Assert.Empty(topic.Messages);
    }

    [Fact]
    public void CloseTopic_Then_CannotPostMessage()
    {
        // Arrange
        var topic = new Topic(Guid.NewGuid(), "クローズ確認", new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
        var senderUserId = Guid.NewGuid();
        const string messageContent = "クローズ後の投稿";
        topic.Close();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            topic.PostMessage(senderUserId, messageContent, new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.Zero)));

        // Assert
        Assert.Equal(TopicStatus.Closed, topic.Status);
        Assert.Empty(topic.Messages);
    }
}
