using FluentAssertions;
using TendexAI.Application.Features.Tenants.Commands.CreateTenant;
using TendexAI.Application.Features.Tenants.Validators;

namespace TendexAI.Infrastructure.Tests.Application.Tenants;

/// <summary>
/// Unit tests for CreateTenantCommandValidator.
/// </summary>
public sealed class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    private static CreateTenantCommand CreateValidCommand() => new(
        NameAr: "وزارة المالية",
        NameEn: "Ministry of Finance",
        Identifier: "MOF",
        Subdomain: "mof",
        ContactPersonName: null,
        ContactPersonEmail: null,
        ContactPersonPhone: null,
        Notes: null,
        LogoUrl: null,
        PrimaryColor: null,
        SecondaryColor: null);

    [Fact]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        // Act
        var result = await _validator.ValidateAsync(CreateValidCommand());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyNameAr_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { NameAr = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NameAr");
    }

    [Fact]
    public async Task Validate_EmptyIdentifier_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Identifier = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_InvalidIdentifierFormat_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Identifier = "123-invalid" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Identifier");
    }

    [Fact]
    public async Task Validate_InvalidSubdomainFormat_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Subdomain = "MOF-UPPER" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subdomain");
    }

    [Fact]
    public async Task Validate_InvalidEmail_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { ContactPersonEmail = "not-an-email" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactPersonEmail");
    }

    [Fact]
    public async Task Validate_ValidEmail_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { ContactPersonEmail = "ahmed@mof.gov.sa" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidColorFormat_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { PrimaryColor = "red" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrimaryColor");
    }

    [Fact]
    public async Task Validate_ValidColorFormat_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { PrimaryColor = "#1A5276" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NotesTooLong_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('x', 2001) };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }
}
