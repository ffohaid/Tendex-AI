using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Infrastructure.Tests.EvaluationMinutes;

/// <summary>
/// Unit tests for MinutesSignatory entity - electronic signing.
/// </summary>
public class MinutesSignatoryTests
{
    [Fact]
    public void Create_ShouldInitializeAsUnsigned()
    {
        // Act
        var signatory = MinutesSignatory.Create(
            Guid.NewGuid(), "user-1", "Ahmed Ali",
            "Committee Chair", "test-user");

        // Assert
        signatory.HasSigned.Should().BeFalse();
        signatory.SignedAt.Should().BeNull();
        signatory.FullName.Should().Be("Ahmed Ali");
        signatory.Role.Should().Be("Committee Chair");
    }

    [Fact]
    public void Sign_ShouldMarkAsSigned()
    {
        // Arrange
        var signatory = MinutesSignatory.Create(
            Guid.NewGuid(), "user-1", "Ahmed Ali",
            "Committee Chair", "test-user");

        // Act
        var result = signatory.Sign("user-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        signatory.HasSigned.Should().BeTrue();
        signatory.SignedAt.Should().NotBeNull();
    }

    [Fact]
    public void Sign_ByDifferentUser_ShouldFail()
    {
        // Arrange
        var signatory = MinutesSignatory.Create(
            Guid.NewGuid(), "user-1", "Ahmed Ali",
            "Committee Chair", "test-user");

        // Act
        var result = signatory.Sign("user-2"); // Different user

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Sign_WhenAlreadySigned_ShouldFail()
    {
        // Arrange
        var signatory = MinutesSignatory.Create(
            Guid.NewGuid(), "user-1", "Ahmed Ali",
            "Committee Chair", "test-user");
        signatory.Sign("user-1");

        // Act
        var result = signatory.Sign("user-1");

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
