using FluentAssertions;
using FluentValidation.TestHelper;
using TendexAI.Application.Features.UserManagement.Commands.AcceptInvitation;
using TendexAI.Application.Features.UserManagement.Commands.AssignRole;
using TendexAI.Application.Features.UserManagement.Commands.SendInvitation;
using TendexAI.Application.Features.UserManagement.Commands.UpdateUser;

namespace TendexAI.Infrastructure.Tests.Application.UserManagement;

public sealed class SendInvitationCommandValidatorTests
{
    private readonly SendInvitationCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenEmailIsEmpty()
    {
        var command = new SendInvitationCommand("", "محمد", "أحمد", null, null, null,
            Guid.NewGuid(), Guid.NewGuid(), "Admin", "Tenant", "https://app.tendex.ai");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_WhenEmailIsInvalid()
    {
        var command = new SendInvitationCommand("not-an-email", "محمد", "أحمد", null, null, null,
            Guid.NewGuid(), Guid.NewGuid(), "Admin", "Tenant", "https://app.tendex.ai");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_WhenArabicFirstNameIsEmpty()
    {
        var command = new SendInvitationCommand("test@example.com", "", "أحمد", null, null, null,
            Guid.NewGuid(), Guid.NewGuid(), "Admin", "Tenant", "https://app.tendex.ai");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FirstNameAr);
    }

    [Fact]
    public void Should_HaveError_WhenBaseUrlIsInvalid()
    {
        var command = new SendInvitationCommand("test@example.com", "محمد", "أحمد", null, null, null,
            Guid.NewGuid(), Guid.NewGuid(), "Admin", "Tenant", "not-a-url");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    [Fact]
    public void Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new SendInvitationCommand("test@example.com", "محمد", "أحمد", "Mohammed", "Ahmed",
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Admin", "Tenant", "https://app.tendex.ai");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class AcceptInvitationCommandValidatorTests
{
    private readonly AcceptInvitationCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenTokenIsEmpty()
    {
        var command = new AcceptInvitationCommand("", "StrongP@ss1", "StrongP@ss1", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Should_HaveError_WhenPasswordIsTooShort()
    {
        var command = new AcceptInvitationCommand("token", "Sh@1", "Sh@1", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_WhenPasswordsDoNotMatch()
    {
        var command = new AcceptInvitationCommand("token", "StrongP@ss1", "DifferentP@ss1", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void Should_HaveError_WhenPasswordMissingUppercase()
    {
        var command = new AcceptInvitationCommand("token", "weakp@ss1", "weakp@ss1", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_WhenPasswordMissingSpecialChar()
    {
        var command = new AcceptInvitationCommand("token", "StrongPass1", "StrongPass1", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_WhenPhoneNumberIsInvalid()
    {
        var command = new AcceptInvitationCommand("token", "StrongP@ss1", "StrongP@ss1", "123");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new AcceptInvitationCommand("valid-token", "StrongP@ss1", "StrongP@ss1", "+966501234567");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenFirstNameIsEmpty()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), "", "Last", null, null, null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), "First", "Last", "+966501234567", null, null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class AssignRoleCommandValidatorTests
{
    private readonly AssignRoleCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenUserIdIsEmpty()
    {
        var command = new AssignRoleCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "admin");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_HaveError_WhenRoleIdIsEmpty()
    {
        var command = new AssignRoleCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "admin");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public void Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "admin");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
