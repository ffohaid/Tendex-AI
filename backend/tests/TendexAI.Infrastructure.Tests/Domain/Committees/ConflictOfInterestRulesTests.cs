using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Committees;

/// <summary>
/// Unit tests for ConflictOfInterestRules.
/// Validates PRD Section 4.2 (Critical Note) and Section 23 (Business Rules #3, #7).
/// </summary>
public sealed class ConflictOfInterestRulesTests
{
    private readonly Guid _userId = Guid.NewGuid();

    // ═════════════════════════════════════════════════════════════
    //  Rule 1: Cannot be Chair of both Technical and Financial
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void ValidateAssignment_ShouldFail_WhenUserIsChairOfBothTechnicalAndFinancial()
    {
        // Arrange: User is already Chair of Technical committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.TechnicalEvaluation, CommitteeMemberRole.Chair)
        }.AsReadOnly();

        // Act: Try to make them Chair of Financial committee
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.FinancialEvaluation,
            CommitteeMemberRole.Chair,
            existingMemberships);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Conflict of interest", result.Error!);
        Assert.Contains("chair of both", result.Error!);
    }

    [Fact]
    public void ValidateAssignment_ShouldFail_WhenFinancialChairTriesToBecomeTechnicalChair()
    {
        // Arrange: User is already Chair of Financial committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.FinancialEvaluation, CommitteeMemberRole.Chair)
        }.AsReadOnly();

        // Act: Try to make them Chair of Technical committee
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.TechnicalEvaluation,
            CommitteeMemberRole.Chair,
            existingMemberships);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Conflict of interest", result.Error!);
    }

    [Fact]
    public void ValidateAssignment_ShouldSucceed_WhenUserIsMemberOfBothCommittees()
    {
        // Arrange: User is a regular Member (not Chair) of Technical committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.TechnicalEvaluation, CommitteeMemberRole.Member)
        }.AsReadOnly();

        // Act: Try to make them a regular Member of Financial committee
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.FinancialEvaluation,
            CommitteeMemberRole.Member,
            existingMemberships);

        // Assert: Regular members can be in both (only Chair restriction)
        Assert.True(result.IsSuccess);
    }

    // ═════════════════════════════════════════════════════════════
    //  Rule 2: Booklet preparer cannot chair evaluation committees
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void ValidateAssignment_ShouldFail_WhenBookletPreparerTriesToChairTechnicalCommittee()
    {
        // Arrange: User is in Booklet Preparation committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.BookletPreparation, CommitteeMemberRole.Member)
        }.AsReadOnly();

        // Act: Try to make them Chair of Technical evaluation committee
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.TechnicalEvaluation,
            CommitteeMemberRole.Chair,
            existingMemberships);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("booklet preparation", result.Error!);
    }

    [Fact]
    public void ValidateAssignment_ShouldFail_WhenBookletPreparerTriesToChairFinancialCommittee()
    {
        // Arrange: User is in Booklet Preparation committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.BookletPreparation, CommitteeMemberRole.Chair)
        }.AsReadOnly();

        // Act: Try to make them Chair of Financial evaluation committee
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.FinancialEvaluation,
            CommitteeMemberRole.Chair,
            existingMemberships);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("booklet preparation", result.Error!);
    }

    // ═════════════════════════════════════════════════════════════
    //  Rule 3: Evaluation chair cannot be in booklet preparation
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void ValidateAssignment_ShouldFail_WhenEvaluationChairTriesToJoinBookletPreparation()
    {
        // Arrange: User is Chair of Technical evaluation committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.TechnicalEvaluation, CommitteeMemberRole.Chair)
        }.AsReadOnly();

        // Act: Try to add them to Booklet Preparation committee
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.BookletPreparation,
            CommitteeMemberRole.Member,
            existingMemberships);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("evaluation committee chair", result.Error!);
    }

    [Fact]
    public void ValidateAssignment_ShouldFail_WhenFinancialChairTriesToJoinBookletPreparation()
    {
        // Arrange: User is Chair of Financial evaluation committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.FinancialEvaluation, CommitteeMemberRole.Chair)
        }.AsReadOnly();

        // Act: Try to add them to Booklet Preparation committee
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.BookletPreparation,
            CommitteeMemberRole.Member,
            existingMemberships);

        // Assert
        Assert.True(result.IsFailure);
    }

    // ═════════════════════════════════════════════════════════════
    //  Positive Tests (No Conflict)
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void ValidateAssignment_ShouldSucceed_WhenNoExistingMemberships()
    {
        // Arrange
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>().AsReadOnly();

        // Act
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.TechnicalEvaluation,
            CommitteeMemberRole.Chair,
            existingMemberships);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAssignment_ShouldSucceed_WhenInquiryReviewMemberJoinsEvaluation()
    {
        // Arrange: User is in Inquiry Review committee
        var existingMemberships = new List<(CommitteeType, CommitteeMemberRole)>
        {
            (CommitteeType.InquiryReview, CommitteeMemberRole.Chair)
        }.AsReadOnly();

        // Act: Try to make them Chair of Technical evaluation
        var result = ConflictOfInterestRules.ValidateAssignment(
            _userId,
            CommitteeType.TechnicalEvaluation,
            CommitteeMemberRole.Chair,
            existingMemberships);

        // Assert: Inquiry review doesn't conflict with evaluation
        Assert.True(result.IsSuccess);
    }

    // ═════════════════════════════════════════════════════════════
    //  Phase Scope Validation Tests
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void ValidatePhaseScope_ShouldSucceed_ForTechnicalCommitteeInTechnicalPhase()
    {
        // Act
        var result = ConflictOfInterestRules.ValidatePhaseScope(
            CommitteeType.TechnicalEvaluation,
            CompetitionPhase.TechnicalAnalysis,
            CompetitionPhase.TechnicalAnalysis);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidatePhaseScope_ShouldFail_ForTechnicalCommitteeAfterTechnicalPhase()
    {
        // Act: Technical committee active only in Financial phase
        var result = ConflictOfInterestRules.ValidatePhaseScope(
            CommitteeType.TechnicalEvaluation,
            CompetitionPhase.FinancialAnalysis,
            CompetitionPhase.FinancialAnalysis);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Technical Analysis phase", result.Error!);
    }

    [Fact]
    public void ValidatePhaseScope_ShouldFail_ForFinancialCommitteeBeforeFinancialPhase()
    {
        // Act: Financial committee active only in Technical phase
        var result = ConflictOfInterestRules.ValidatePhaseScope(
            CommitteeType.FinancialEvaluation,
            CompetitionPhase.BookletPreparation,
            CompetitionPhase.TechnicalAnalysis);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Financial Analysis phase", result.Error!);
    }

    [Fact]
    public void ValidatePhaseScope_ShouldSucceed_ForBookletPreparationInAnyPhase()
    {
        // Act: Booklet preparation has no phase restrictions
        var result = ConflictOfInterestRules.ValidatePhaseScope(
            CommitteeType.BookletPreparation,
            CompetitionPhase.BookletPreparation,
            CompetitionPhase.BookletApproval);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidatePhaseScope_ShouldSucceed_WhenNullPhases()
    {
        // Act: Null phases mean active for all phases
        var result = ConflictOfInterestRules.ValidatePhaseScope(
            CommitteeType.TechnicalEvaluation,
            null,
            null);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
