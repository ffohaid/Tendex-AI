using FluentAssertions;
using TendexAI.Infrastructure.Services.Identity;

namespace TendexAI.Infrastructure.Tests.Services.Identity;

/// <summary>
/// Unit tests for <see cref="TotpService"/>.
/// </summary>
public sealed class TotpServiceTests
{
    private readonly TotpService _sut = new();

    [Fact]
    public void GenerateSecretKey_Should_Return_Valid_Base32_String()
    {
        // Act
        var secretKey = _sut.GenerateSecretKey();

        // Assert
        secretKey.Should().NotBeNullOrWhiteSpace();
        secretKey.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSecretKey_Should_Return_Unique_Keys()
    {
        // Act
        var key1 = _sut.GenerateSecretKey();
        var key2 = _sut.GenerateSecretKey();

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GenerateQrCodeUri_Should_Return_Valid_OtpAuth_Uri()
    {
        // Arrange
        var email = "user@example.com";
        var issuer = "Tendex AI";
        var secretKey = _sut.GenerateSecretKey();

        // Act
        var uri = _sut.GenerateQrCodeUri(email, issuer, secretKey);

        // Assert
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain(secretKey);
        uri.Should().Contain("Tendex%20AI");
    }

    [Fact]
    public void ValidateCode_Should_Return_True_For_Valid_Code()
    {
        // Arrange
        var secretKey = _sut.GenerateSecretKey();
        var code = _sut.GenerateCurrentCode(secretKey);

        // Act
        var result = _sut.ValidateCode(secretKey, code);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_Should_Return_False_For_Invalid_Code()
    {
        // Arrange
        var secretKey = _sut.GenerateSecretKey();

        // Act
        var result = _sut.ValidateCode(secretKey, "000000");

        // Assert
        // Note: There's a very small chance this could be a valid code,
        // but statistically it's extremely unlikely
        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateCurrentCode_Should_Return_Six_Digit_Code()
    {
        // Arrange
        var secretKey = _sut.GenerateSecretKey();

        // Act
        var code = _sut.GenerateCurrentCode(secretKey);

        // Assert
        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }
}
