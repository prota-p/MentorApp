using MentorApp.Domain.Models.Users;

namespace MentorApp.Tests.Domain.Users;

public class EmailTests
{
    [Fact]
    public void CreateEmail_WithWhitespace_Then_TrimsValue()
    {
        // Arrange
        const string input = "  taro.yamada@example.com  ";
        const string expected = "taro.yamada@example.com";

        // Act
        var email = new Email(input);

        // Assert
        Assert.Equal(expected, email.Value);
        Assert.Equal(expected, email.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public void CreateEmail_WithInvalidValue_Then_ThrowsArgumentException(string? input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(input));
    }
}
