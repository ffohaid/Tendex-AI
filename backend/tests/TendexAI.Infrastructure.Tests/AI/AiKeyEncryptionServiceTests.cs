using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TendexAI.Infrastructure.Security;

namespace TendexAI.Infrastructure.Tests.AI;

/// <summary>
/// Unit tests for AiKeyEncryptionService (AES-256-CBC).
/// Verifies encryption/decryption round-trip, uniqueness of ciphertexts,
/// and proper error handling.
/// </summary>
public sealed class AiKeyEncryptionServiceTests
{
    // A valid 256-bit key encoded in Base64 (32 bytes)
    private const string ValidKeyBase64 = "dGVzdC1lbmNyeXB0aW9uLWtleS0yNTYtYml0cyE="; // 32 bytes

    private static AiKeyEncryptionService CreateService(string? keyBase64 = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AiEncryptionKey"] = keyBase64 ?? ValidKeyBase64
            })
            .Build();

        return new AiKeyEncryptionService(config);
    }

    [Fact]
    public void Encrypt_And_Decrypt_Should_Return_Original_PlainText()
    {
        // Arrange
        var service = CreateService();
        var originalKey = "sk-test-1234567890abcdef";

        // Act
        var encrypted = service.Encrypt(originalKey);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalKey);
    }

    [Fact]
    public void Encrypt_Should_Produce_Different_CipherText_Each_Time()
    {
        // Arrange (due to random IV)
        var service = CreateService();
        var originalKey = "sk-test-1234567890abcdef";

        // Act
        var encrypted1 = service.Encrypt(originalKey);
        var encrypted2 = service.Encrypt(originalKey);

        // Assert
        encrypted1.Should().NotBe(encrypted2,
            "each encryption should use a unique IV, producing different ciphertexts");
    }

    [Fact]
    public void Encrypt_Should_Return_Base64_String()
    {
        // Arrange
        var service = CreateService();

        // Act
        var encrypted = service.Encrypt("test-api-key");

        // Assert
        encrypted.Should().NotBeNullOrWhiteSpace();
        var action = () => Convert.FromBase64String(encrypted);
        action.Should().NotThrow("the encrypted output should be valid Base64");
    }

    [Fact]
    public void Decrypt_With_Wrong_Key_Should_Throw()
    {
        // Arrange
        var service1 = CreateService(ValidKeyBase64);
        var encrypted = service1.Encrypt("my-secret-key");

        // Use a different key for decryption
        var differentKey = Convert.ToBase64String(new byte[32]); // all zeros
        var service2 = CreateService(differentKey);

        // Act & Assert
        var action = () => service2.Decrypt(encrypted);
        action.Should().Throw<Exception>(
            "decrypting with a different key should fail");
    }

    [Fact]
    public void Encrypt_Should_Throw_For_Null_Or_Empty_Input()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var action1 = () => service.Encrypt(null!);
        action1.Should().Throw<ArgumentException>();

        var action2 = () => service.Encrypt("");
        action2.Should().Throw<ArgumentException>();

        var action3 = () => service.Encrypt("   ");
        action3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_Should_Throw_For_Null_Or_Empty_Input()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var action1 = () => service.Decrypt(null!);
        action1.Should().Throw<ArgumentException>();

        var action2 = () => service.Decrypt("");
        action2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Key_Not_Configured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var action = () => new AiKeyEncryptionService(config);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Key_Is_Wrong_Size()
    {
        // Arrange - 16-byte key (128-bit) instead of 32-byte (256-bit)
        var shortKey = Convert.ToBase64String(new byte[16]);

        // Act & Assert
        var action = () => CreateService(shortKey);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*256-bit*");
    }

    [Fact]
    public void Encrypt_Decrypt_Should_Handle_Unicode_Content()
    {
        // Arrange
        var service = CreateService();
        var unicodeKey = "مفتاح-API-عربي-123";

        // Act
        var encrypted = service.Encrypt(unicodeKey);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(unicodeKey);
    }

    [Fact]
    public void Encrypt_Decrypt_Should_Handle_Long_Keys()
    {
        // Arrange
        var service = CreateService();
        var longKey = new string('A', 2048);

        // Act
        var encrypted = service.Encrypt(longKey);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(longKey);
    }
}
