using FluentAssertions;
using TendexAI.Domain.Enums;
using TendexAI.Domain.StateMachine;

namespace TendexAI.Infrastructure.Tests.Domain.Rfp;

/// <summary>
/// Unit tests for the CompetitionStateMachine.
/// Validates all valid transitions, blocked transitions, and helper methods.
/// </summary>
public sealed class CompetitionStateMachineTests
{
    // ═══════════════════════════════════════════════════════════════
    //  Valid Forward Transitions (Happy Path)
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CompetitionStatus.Draft, CompetitionStatus.UnderPreparation)]
    [InlineData(CompetitionStatus.UnderPreparation, CompetitionStatus.PendingApproval)]
    [InlineData(CompetitionStatus.UnderPreparation, CompetitionStatus.Draft)]
    [InlineData(CompetitionStatus.PendingApproval, CompetitionStatus.Approved)]
    [InlineData(CompetitionStatus.PendingApproval, CompetitionStatus.Rejected)]
    [InlineData(CompetitionStatus.Approved, CompetitionStatus.Published)]
    [InlineData(CompetitionStatus.Published, CompetitionStatus.InquiryPeriod)]
    [InlineData(CompetitionStatus.Published, CompetitionStatus.ReceivingOffers)]
    [InlineData(CompetitionStatus.InquiryPeriod, CompetitionStatus.ReceivingOffers)]
    [InlineData(CompetitionStatus.ReceivingOffers, CompetitionStatus.OffersClosed)]
    [InlineData(CompetitionStatus.OffersClosed, CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.TechnicalAnalysis, CompetitionStatus.TechnicalAnalysisCompleted)]
    [InlineData(CompetitionStatus.TechnicalAnalysisCompleted, CompetitionStatus.FinancialAnalysis)]
    [InlineData(CompetitionStatus.FinancialAnalysis, CompetitionStatus.FinancialAnalysisCompleted)]
    [InlineData(CompetitionStatus.FinancialAnalysisCompleted, CompetitionStatus.AwardNotification)]
    [InlineData(CompetitionStatus.AwardNotification, CompetitionStatus.AwardApproved)]
    [InlineData(CompetitionStatus.AwardApproved, CompetitionStatus.ContractApproval)]
    [InlineData(CompetitionStatus.ContractApproval, CompetitionStatus.ContractApproved)]
    [InlineData(CompetitionStatus.ContractApproved, CompetitionStatus.ContractSigned)]
    public void CanTransition_ValidForwardTransition_ReturnsTrue(
        CompetitionStatus from, CompetitionStatus to)
    {
        CompetitionStateMachine.CanTransition(from, to).Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Cancellation from any non-terminal state
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CompetitionStatus.Draft)]
    [InlineData(CompetitionStatus.UnderPreparation)]
    [InlineData(CompetitionStatus.PendingApproval)]
    [InlineData(CompetitionStatus.Approved)]
    [InlineData(CompetitionStatus.Published)]
    [InlineData(CompetitionStatus.InquiryPeriod)]
    [InlineData(CompetitionStatus.ReceivingOffers)]
    [InlineData(CompetitionStatus.OffersClosed)]
    [InlineData(CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.TechnicalAnalysisCompleted)]
    [InlineData(CompetitionStatus.FinancialAnalysis)]
    [InlineData(CompetitionStatus.FinancialAnalysisCompleted)]
    [InlineData(CompetitionStatus.AwardNotification)]
    [InlineData(CompetitionStatus.AwardApproved)]
    [InlineData(CompetitionStatus.ContractApproval)]
    [InlineData(CompetitionStatus.ContractApproved)]
    public void CanTransition_ToCancelled_FromNonTerminalState_ReturnsTrue(
        CompetitionStatus from)
    {
        CompetitionStateMachine.CanTransition(from, CompetitionStatus.Cancelled).Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Suspension from post-approval states
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CompetitionStatus.Published)]
    [InlineData(CompetitionStatus.InquiryPeriod)]
    [InlineData(CompetitionStatus.ReceivingOffers)]
    [InlineData(CompetitionStatus.OffersClosed)]
    [InlineData(CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.FinancialAnalysis)]
    [InlineData(CompetitionStatus.AwardNotification)]
    [InlineData(CompetitionStatus.ContractApproval)]
    public void CanTransition_ToSuspended_FromPostApprovalState_ReturnsTrue(
        CompetitionStatus from)
    {
        CompetitionStateMachine.CanTransition(from, CompetitionStatus.Suspended).Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Invalid Transitions — Stage Skipping Prevention
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CompetitionStatus.Draft, CompetitionStatus.PendingApproval)]
    [InlineData(CompetitionStatus.Draft, CompetitionStatus.Approved)]
    [InlineData(CompetitionStatus.Draft, CompetitionStatus.Published)]
    [InlineData(CompetitionStatus.Draft, CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.Draft, CompetitionStatus.ContractSigned)]
    [InlineData(CompetitionStatus.UnderPreparation, CompetitionStatus.Published)]
    [InlineData(CompetitionStatus.UnderPreparation, CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.PendingApproval, CompetitionStatus.Published)]
    [InlineData(CompetitionStatus.Approved, CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.Published, CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.ReceivingOffers, CompetitionStatus.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.TechnicalAnalysis, CompetitionStatus.FinancialAnalysis)]
    [InlineData(CompetitionStatus.FinancialAnalysis, CompetitionStatus.AwardNotification)]
    [InlineData(CompetitionStatus.AwardNotification, CompetitionStatus.ContractSigned)]
    public void CanTransition_SkippingStages_ReturnsFalse(
        CompetitionStatus from, CompetitionStatus to)
    {
        CompetitionStateMachine.CanTransition(from, to).Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Terminal States — No Transitions Allowed
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CanTransition_FromContractSigned_NoTransitionsAllowed()
    {
        var allowed = CompetitionStateMachine.GetAllowedTransitions(CompetitionStatus.ContractSigned);
        allowed.Should().BeEmpty();
    }

    [Fact]
    public void CanTransition_FromCancelled_NoTransitionsAllowed()
    {
        var allowed = CompetitionStateMachine.GetAllowedTransitions(CompetitionStatus.Cancelled);
        allowed.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Rejected → UnderPreparation (rework)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CanTransition_FromRejected_ToUnderPreparation_ReturnsTrue()
    {
        CompetitionStateMachine.CanTransition(
            CompetitionStatus.Rejected,
            CompetitionStatus.UnderPreparation).Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Same-Status Transition
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidateTransition_SameStatus_ReturnsFailure()
    {
        var result = CompetitionStateMachine.ValidateTransition(
            CompetitionStatus.Draft, CompetitionStatus.Draft);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already in");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Phase Mapping
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CompetitionStatus.Draft, CompetitionPhase.BookletPreparation)]
    [InlineData(CompetitionStatus.UnderPreparation, CompetitionPhase.BookletPreparation)]
    [InlineData(CompetitionStatus.PendingApproval, CompetitionPhase.BookletApproval)]
    [InlineData(CompetitionStatus.Approved, CompetitionPhase.BookletApproval)]
    [InlineData(CompetitionStatus.Published, CompetitionPhase.BookletPublishing)]
    [InlineData(CompetitionStatus.InquiryPeriod, CompetitionPhase.BookletPublishing)]
    [InlineData(CompetitionStatus.ReceivingOffers, CompetitionPhase.OfferReception)]
    [InlineData(CompetitionStatus.OffersClosed, CompetitionPhase.OfferReception)]
    [InlineData(CompetitionStatus.TechnicalAnalysis, CompetitionPhase.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.TechnicalAnalysisCompleted, CompetitionPhase.TechnicalAnalysis)]
    [InlineData(CompetitionStatus.FinancialAnalysis, CompetitionPhase.FinancialAnalysis)]
    [InlineData(CompetitionStatus.FinancialAnalysisCompleted, CompetitionPhase.FinancialAnalysis)]
    [InlineData(CompetitionStatus.AwardNotification, CompetitionPhase.AwardNotification)]
    [InlineData(CompetitionStatus.AwardApproved, CompetitionPhase.AwardNotification)]
    [InlineData(CompetitionStatus.ContractApproval, CompetitionPhase.ContractApproval)]
    [InlineData(CompetitionStatus.ContractApproved, CompetitionPhase.ContractApproval)]
    [InlineData(CompetitionStatus.ContractSigned, CompetitionPhase.ContractSigning)]
    public void GetPhase_ReturnsCorrectPhase(CompetitionStatus status, CompetitionPhase expectedPhase)
    {
        CompetitionStateMachine.GetPhase(status).Should().Be(expectedPhase);
    }

    [Theory]
    [InlineData(CompetitionStatus.Draft, 1)]
    [InlineData(CompetitionStatus.PendingApproval, 2)]
    [InlineData(CompetitionStatus.Published, 3)]
    [InlineData(CompetitionStatus.ReceivingOffers, 4)]
    [InlineData(CompetitionStatus.TechnicalAnalysis, 5)]
    [InlineData(CompetitionStatus.FinancialAnalysis, 6)]
    [InlineData(CompetitionStatus.AwardNotification, 7)]
    [InlineData(CompetitionStatus.ContractApproval, 8)]
    [InlineData(CompetitionStatus.ContractSigned, 9)]
    public void GetPhaseNumber_ReturnsCorrectNumber(CompetitionStatus status, int expectedNumber)
    {
        CompetitionStateMachine.GetPhaseNumber(status).Should().Be(expectedNumber);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Terminal / Exception State Checks
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CompetitionStatus.ContractSigned, true)]
    [InlineData(CompetitionStatus.Cancelled, true)]
    [InlineData(CompetitionStatus.Draft, false)]
    [InlineData(CompetitionStatus.Published, false)]
    public void IsTerminal_ReturnsCorrectValue(CompetitionStatus status, bool expected)
    {
        CompetitionStateMachine.IsTerminal(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(CompetitionStatus.Rejected, true)]
    [InlineData(CompetitionStatus.Cancelled, true)]
    [InlineData(CompetitionStatus.Suspended, true)]
    [InlineData(CompetitionStatus.Draft, false)]
    [InlineData(CompetitionStatus.Approved, false)]
    public void IsExceptionState_ReturnsCorrectValue(CompetitionStatus status, bool expected)
    {
        CompetitionStateMachine.IsExceptionState(status).Should().Be(expected);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Phase Name Localization
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetPhaseNameAr_ReturnsArabicNames()
    {
        CompetitionStateMachine.GetPhaseNameAr(CompetitionPhase.BookletPreparation)
            .Should().Be("إعداد الكراسة");
        CompetitionStateMachine.GetPhaseNameAr(CompetitionPhase.ContractSigning)
            .Should().Be("توقيع العقد");
    }

    [Fact]
    public void GetPhaseNameEn_ReturnsEnglishNames()
    {
        CompetitionStateMachine.GetPhaseNameEn(CompetitionPhase.BookletPreparation)
            .Should().Be("Booklet Preparation");
        CompetitionStateMachine.GetPhaseNameEn(CompetitionPhase.ContractSigning)
            .Should().Be("Contract Signing");
    }

    // ═══════════════════════════════════════════════════════════════
    //  ValidateTransition — Error Messages
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidateTransition_InvalidTransition_ReturnsDescriptiveError()
    {
        var result = CompetitionStateMachine.ValidateTransition(
            CompetitionStatus.Draft, CompetitionStatus.Published);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid status transition");
        result.Error.Should().Contain("Draft");
        result.Error.Should().Contain("Published");
        result.Error.Should().Contain("Allowed transitions");
    }

    [Fact]
    public void ValidateTransition_ValidTransition_ReturnsSuccess()
    {
        var result = CompetitionStateMachine.ValidateTransition(
            CompetitionStatus.Draft, CompetitionStatus.UnderPreparation);

        result.IsSuccess.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Full Lifecycle Path Test
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FullLifecyclePath_AllTransitionsValid()
    {
        var path = new[]
        {
            CompetitionStatus.Draft,
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            CompetitionStatus.Published,
            CompetitionStatus.InquiryPeriod,
            CompetitionStatus.ReceivingOffers,
            CompetitionStatus.OffersClosed,
            CompetitionStatus.TechnicalAnalysis,
            CompetitionStatus.TechnicalAnalysisCompleted,
            CompetitionStatus.FinancialAnalysis,
            CompetitionStatus.FinancialAnalysisCompleted,
            CompetitionStatus.AwardNotification,
            CompetitionStatus.AwardApproved,
            CompetitionStatus.ContractApproval,
            CompetitionStatus.ContractApproved,
            CompetitionStatus.ContractSigned
        };

        for (int i = 0; i < path.Length - 1; i++)
        {
            var result = CompetitionStateMachine.ValidateTransition(path[i], path[i + 1]);
            result.IsSuccess.Should().BeTrue(
                $"Transition from {path[i]} to {path[i + 1]} should be valid (step {i + 1})");
        }
    }
}
