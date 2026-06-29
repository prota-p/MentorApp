using MentorApp.Domain.Models.Users;
using MentorApp.Domain.Services;

namespace MentorApp.Tests.Domain.Services;

public class MentorshipDuplicationCheckServiceTests
{
    [Fact]
    public void ValidateMentorshipCreation_WithoutActiveMentorship_Then_DoesNotThrow()
    {
        // Arrange
        var service = new MentorshipDuplicationCheckService();
        var mentor = CreateUser("mentor-001", "テストメンター", "mentor@example.com", Role.Mentor);
        var mentee = CreateUser("mentee-001", "テストメンティー", "mentee@example.com", Role.Mentee);

        // Act
        var exception = Record.Exception(() =>
            service.ValidateMentorshipCreation(mentor, mentee, hasActiveMentorshipForPair: false));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateMentorshipCreation_WithActiveMentorship_Then_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new MentorshipDuplicationCheckService();
        var mentor = CreateUser("mentor-001", "テストメンター", "mentor@example.com", Role.Mentor);
        var mentee = CreateUser("mentee-001", "テストメンティー", "mentee@example.com", Role.Mentee);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.ValidateMentorshipCreation(mentor, mentee, hasActiveMentorshipForPair: true));
    }

    private static User CreateUser(string externalId, string displayName, string email, Role role)
        => new(
            externalId: externalId,
            displayName: displayName,
            createdAt: new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            email: email,
            role: role);
}
