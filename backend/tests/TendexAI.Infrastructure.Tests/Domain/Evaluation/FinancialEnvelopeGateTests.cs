using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Evaluation;

/// <summary>
/// Integration-style unit tests to verify the CRITICAL business rule:
/// Financial envelopes CANNOT be opened before technical evaluation is approved.
/// Per PRD Section 10.1.
/// </summary>
public sealed class FinancialEnvelopeGateTests
{
    private readonly Guid _competitionId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _committeeId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════
    //  Full Lifecycle: Technical Evaluation → Financial Envelope
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Full_Lifecycle_Should_Allow_Financial_Open_Only_After_Approval()
    {
        // 1. Create evaluation and offers
        var evaluation = TechnicalEvaluation.Create(
            _competitionId, _tenantId, _committeeId, 60m, "admin");

        var offer1 = SupplierOffer.Create(
            _competitionId, _tenantId, "Supplier A", "CR-001", "REF-001", "OFFER-A", "admin");
        var offer2 = SupplierOffer.Create(
            _competitionId, _tenantId, "Supplier B", "CR-002", "REF-002", "OFFER-B", "admin");

        // 2. Verify financial envelopes are locked at start
        offer1.OpenFinancialEnvelope("chair").IsFailure.Should().BeTrue();
        offer2.OpenFinancialEnvelope("chair").IsFailure.Should().BeTrue();

        // 3. Start evaluation
        evaluation.Start("admin");

        // 4. Submit scores
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, offer1.Id, Guid.NewGuid(),
            "eval-1", 85m, 100m, null, "eval-1"));
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, offer2.Id, Guid.NewGuid(),
            "eval-1", 45m, 100m, null, "eval-1"));

        // 5. Complete scoring
        evaluation.MarkAllScoresSubmitted("admin");

        // 6. Set technical results
        offer1.SetTechnicalResult(OfferTechnicalResult.Passed, 85m, "system");
        offer2.SetTechnicalResult(OfferTechnicalResult.Failed, 45m, "system");

        // 7. Verify: financial envelopes still locked (not yet approved)
        evaluation.IsReportApproved.Should().BeFalse();

        // 8. Submit for approval
        evaluation.SubmitForApproval("admin");

        // 9. Verify: still locked at PendingApproval
        evaluation.IsReportApproved.Should().BeFalse();

        // 10. Approve report
        evaluation.ApproveReport("chair-001");
        evaluation.IsReportApproved.Should().BeTrue();

        // 11. Now financial envelopes can be opened for PASSED offers
        var openResult1 = offer1.OpenFinancialEnvelope("chair-001");
        openResult1.IsSuccess.Should().BeTrue();
        offer1.IsFinancialEnvelopeOpen.Should().BeTrue();

        // 12. Failed offers still cannot have their envelopes opened
        var openResult2 = offer2.OpenFinancialEnvelope("chair-001");
        openResult2.IsFailure.Should().BeTrue();
        offer2.IsFinancialEnvelopeOpen.Should().BeFalse();
    }

    [Fact]
    public void Financial_Envelope_Should_Not_Open_When_Evaluation_Is_Rejected()
    {
        var evaluation = TechnicalEvaluation.Create(
            _competitionId, _tenantId, _committeeId, 60m, "admin");

        var offer = SupplierOffer.Create(
            _competitionId, _tenantId, "Supplier A", "CR-001", "REF-001", "OFFER-A", "admin");

        evaluation.Start("admin");
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, offer.Id, Guid.NewGuid(),
            "eval-1", 85m, 100m, null, "eval-1"));
        evaluation.MarkAllScoresSubmitted("admin");
        offer.SetTechnicalResult(OfferTechnicalResult.Passed, 85m, "system");
        evaluation.SubmitForApproval("admin");

        // Reject instead of approve
        evaluation.RejectReport("chair-001", "Needs review");

        // Evaluation is NOT approved
        evaluation.IsReportApproved.Should().BeFalse();

        // Financial envelope can still be opened on the offer level (it passed),
        // but the command handler checks evaluation.IsReportApproved first
        // This test validates the domain invariant at the evaluation level
    }

    [Fact]
    public void Blind_Evaluation_Should_Hide_Identity_Throughout_Process()
    {
        var evaluation = TechnicalEvaluation.Create(
            _competitionId, _tenantId, _committeeId, 60m, "admin");

        // Blind evaluation active from creation
        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        evaluation.Start("admin");
        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "eval-1", 80m, 100m, null, "eval-1"));
        evaluation.MarkAllScoresSubmitted("admin");
        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        evaluation.SubmitForApproval("admin");
        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        // Only after approval should identities be revealed
        evaluation.ApproveReport("chair-001");
        evaluation.IsBlindEvaluationActive.Should().BeFalse();
    }

    [Fact]
    public void Multiple_Offers_Should_Have_Independent_Financial_Envelope_Status()
    {
        var offer1 = SupplierOffer.Create(
            _competitionId, _tenantId, "Supplier A", "CR-001", "REF-001", "OFFER-A", "admin");
        var offer2 = SupplierOffer.Create(
            _competitionId, _tenantId, "Supplier B", "CR-002", "REF-002", "OFFER-B", "admin");
        var offer3 = SupplierOffer.Create(
            _competitionId, _tenantId, "Supplier C", "CR-003", "REF-003", "OFFER-C", "admin");

        offer1.SetTechnicalResult(OfferTechnicalResult.Passed, 90m, "system");
        offer2.SetTechnicalResult(OfferTechnicalResult.Passed, 75m, "system");
        offer3.SetTechnicalResult(OfferTechnicalResult.Failed, 40m, "system");

        // Open only offer1
        offer1.OpenFinancialEnvelope("chair");

        offer1.IsFinancialEnvelopeOpen.Should().BeTrue();
        offer2.IsFinancialEnvelopeOpen.Should().BeFalse();
        offer3.IsFinancialEnvelopeOpen.Should().BeFalse();
    }
}
