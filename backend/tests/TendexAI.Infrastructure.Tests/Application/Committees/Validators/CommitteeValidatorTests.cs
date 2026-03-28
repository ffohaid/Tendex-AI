using FluentValidation.TestHelper;
using TendexAI.Application.Features.Committees.Commands.CreateCommittee;
using TendexAI.Application.Features.Committees.Commands.AddCommitteeMember;
using TendexAI.Application.Features.Committees.Commands.RemoveCommitteeMember;
using TendexAI.Application.Features.Committees.Commands.ChangeCommitteeStatus;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Committees.Validators;

/// <summary>
/// Unit tests for committee command validators.
/// </summary>
public sealed class CommitteeValidatorTests
{
    // ═════════════════════════════════════════════════════════════
    //  CreateCommitteeCommandValidator Tests
    // ═════════════════════════════════════════════════════════════

    private readonly CreateCommitteeCommandValidator _createValidator = new();

    [Fact]
    public void CreateCommittee_ShouldPass_WithValidData()
    {
        var command = new CreateCommitteeCommand(
            NameAr: "لجنة الفحص الفني",
            NameEn: "Technical Evaluation Committee",
            Type: CommitteeType.TechnicalEvaluation,
            IsPermanent: false,
            Description: "Test",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(6),
            CompetitionId: Guid.NewGuid(),
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCommittee_ShouldFail_WhenNameArIsEmpty()
    {
        var command = new CreateCommitteeCommand(
            NameAr: "",
            NameEn: "Technical Evaluation Committee",
            Type: CommitteeType.TechnicalEvaluation,
            IsPermanent: false,
            Description: null,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(6),
            CompetitionId: Guid.NewGuid(),
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NameAr);
    }

    [Fact]
    public void CreateCommittee_ShouldFail_WhenNameEnIsEmpty()
    {
        var command = new CreateCommitteeCommand(
            NameAr: "لجنة الفحص الفني",
            NameEn: "",
            Type: CommitteeType.TechnicalEvaluation,
            IsPermanent: false,
            Description: null,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(6),
            CompetitionId: Guid.NewGuid(),
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NameEn);
    }

    [Fact]
    public void CreateCommittee_ShouldFail_WhenEndDateBeforeStartDate()
    {
        var command = new CreateCommitteeCommand(
            NameAr: "لجنة الفحص الفني",
            NameEn: "Technical Evaluation Committee",
            Type: CommitteeType.TechnicalEvaluation,
            IsPermanent: false,
            Description: null,
            StartDate: DateTime.UtcNow.AddMonths(6),
            EndDate: DateTime.UtcNow,
            CompetitionId: Guid.NewGuid(),
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void CreateCommittee_ShouldFail_WhenTemporaryWithoutCompetitionId()
    {
        var command = new CreateCommitteeCommand(
            NameAr: "لجنة الفحص الفني",
            NameEn: "Technical Evaluation Committee",
            Type: CommitteeType.TechnicalEvaluation,
            IsPermanent: false,
            Description: null,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(6),
            CompetitionId: null,
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CompetitionId);
    }

    // ═════════════════════════════════════════════════════════════
    //  AddCommitteeMemberCommandValidator Tests
    // ═════════════════════════════════════════════════════════════

    private readonly AddCommitteeMemberCommandValidator _addMemberValidator = new();

    [Fact]
    public void AddMember_ShouldPass_WithValidData()
    {
        var command = new AddCommitteeMemberCommand(
            CommitteeId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserFullName: "Ahmed Ali",
            Role: CommitteeMemberRole.Member,
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _addMemberValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddMember_ShouldFail_WhenCommitteeIdIsEmpty()
    {
        var command = new AddCommitteeMemberCommand(
            CommitteeId: Guid.Empty,
            UserId: Guid.NewGuid(),
            UserFullName: "Ahmed Ali",
            Role: CommitteeMemberRole.Member,
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _addMemberValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CommitteeId);
    }

    [Fact]
    public void AddMember_ShouldFail_WhenUserFullNameIsEmpty()
    {
        var command = new AddCommitteeMemberCommand(
            CommitteeId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserFullName: "",
            Role: CommitteeMemberRole.Member,
            ActiveFromPhase: null,
            ActiveToPhase: null);

        var result = _addMemberValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserFullName);
    }

    // ═════════════════════════════════════════════════════════════
    //  RemoveCommitteeMemberCommandValidator Tests
    // ═════════════════════════════════════════════════════════════

    private readonly RemoveCommitteeMemberCommandValidator _removeMemberValidator = new();

    [Fact]
    public void RemoveMember_ShouldFail_WhenReasonIsEmpty()
    {
        var command = new RemoveCommitteeMemberCommand(
            CommitteeId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Reason: "");

        var result = _removeMemberValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    // ═════════════════════════════════════════════════════════════
    //  ChangeCommitteeStatusCommandValidator Tests
    // ═════════════════════════════════════════════════════════════

    private readonly ChangeCommitteeStatusCommandValidator _statusValidator = new();

    [Fact]
    public void ChangeStatus_ShouldFail_WhenSuspendingWithoutReason()
    {
        var command = new ChangeCommitteeStatusCommand(
            CommitteeId: Guid.NewGuid(),
            NewStatus: CommitteeStatus.Suspended,
            Reason: null);

        var result = _statusValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ChangeStatus_ShouldFail_WhenDissolvingWithoutReason()
    {
        var command = new ChangeCommitteeStatusCommand(
            CommitteeId: Guid.NewGuid(),
            NewStatus: CommitteeStatus.Dissolved,
            Reason: null);

        var result = _statusValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ChangeStatus_ShouldPass_WhenReactivatingWithoutReason()
    {
        var command = new ChangeCommitteeStatusCommand(
            CommitteeId: Guid.NewGuid(),
            NewStatus: CommitteeStatus.Active,
            Reason: null);

        var result = _statusValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}
