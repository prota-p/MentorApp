using MentorApp.Domain.Models.Users;

namespace MentorApp.Tests.Domain.Users;

public class UserTests
{
    [Fact]
    public void CreateUser_Then_PropertiesAreInitialized()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        const string externalId = "mentor-001";
        const string displayName = "山田太郎";
        const string email = "taro.yamada@example.com";

        // Act
        var user = new User(externalId, displayName, createdAt, email, Role.Mentor);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(externalId, user.ExternalId);
        Assert.Equal(displayName, user.DisplayName);
        Assert.Equal(email, user.Email.Value);
        Assert.Equal(Role.Mentor, user.Role);
        Assert.Equal(createdAt, user.CreatedAt);
    }

    [Fact]
    public void UpdateDisplayName_WithBlankName_Then_ThrowsArgumentException()
    {
        // Arrange
        const string externalId = "mentee-001";
        const string currentDisplayName = "佐藤花子";
        const string email = "hanako.sato@example.com";

        var user = new User(
            externalId: externalId,
            displayName: currentDisplayName,
            createdAt: new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
            email: email,
            role: Role.Mentee);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => user.UpdateDisplayName("   "));

        // Assert
        Assert.Equal(currentDisplayName, user.DisplayName);
    }
}
