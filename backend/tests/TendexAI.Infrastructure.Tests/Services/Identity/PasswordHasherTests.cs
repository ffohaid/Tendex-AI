using FluentAssertions;
using TendexAI.Infrastructure.Services.Identity;

namespace TendexAI.Infrastructure.Tests.Services.Identity;

/// <summary>
/// Unit tests for <see cref="PasswordHasher"/>.
/// </summary>
public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_Should_Return_NonEmpty_Hash()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";

        // Act
        var hash = _sut.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_Should_Return_Different_Hashes_For_Same_Password()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";

        // Act
        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2, "BCrypt should use random salt each time");
    }

    [Fact]
    public void VerifyPassword_Should_Return_True_For_Correct_Password()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";
        var hash = _sut.HashPassword(password);

        // Act
        var result = _sut.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_Should_Return_False_For_Incorrect_Password()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";
        var wrongPassword = "WrongP@ssw0rd!";
        var hash = _sut.HashPassword(password);

        // Act
        var result = _sut.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Should_Return_False_For_Empty_Password()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";
        var hash = _sut.HashPassword(password);

        // Act
        var result = _sut.VerifyPassword(string.Empty, hash);

        // Assert
        result.Should().BeFalse();
    }
}
