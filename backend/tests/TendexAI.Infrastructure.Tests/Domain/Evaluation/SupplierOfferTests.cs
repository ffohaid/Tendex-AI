using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Evaluation;

/// <summary>
/// Unit tests for the SupplierOffer entity.
/// Validates blind code assignment, technical results, and financial envelope gating.
/// </summary>
public sealed class SupplierOfferTests
{
    private readonly Guid _competitionId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    private SupplierOffer CreateOffer()
    {
        return SupplierOffer.Create(
            _competitionId,
            _tenantId,
            "Supplier A",
            "CR-123456",
            "REF-001",
            DateTime.UtcNow,
            "system");
    }

    // ═══════════════════════════════════════════════════════════
    //  Creation Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Create_Should_Initialize_With_Pending_Result()
    {
        var offer = CreateOffer();

        offer.CompetitionId.Should().Be(_competitionId);
        offer.TenantId.Should().Be(_tenantId);
        offer.SupplierName.Should().Be("Supplier A");
        offer.BlindCode.Should().StartWith("OFFER-");
        offer.BlindCode.Should().HaveLength(10); // "OFFER-" (6) + 4 random chars
        offer.TechnicalResult.Should().Be(OfferTechnicalResult.Pending);
        offer.IsFinancialEnvelopeOpen.Should().BeFalse();
        offer.TechnicalTotalScore.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    //  Technical Result Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SetTechnicalResult_Passed_Should_Update_Score_And_Result()
    {
        var offer = CreateOffer();

        var result = offer.SetTechnicalResult(OfferTechnicalResult.Passed, 85.5m, "system");

        result.IsSuccess.Should().BeTrue();
        offer.TechnicalResult.Should().Be(OfferTechnicalResult.Passed);
        offer.TechnicalTotalScore.Should().Be(85.5m);
    }

    [Fact]
    public void SetTechnicalResult_Failed_Should_Update_Score_And_Result()
    {
        var offer = CreateOffer();

        var result = offer.SetTechnicalResult(OfferTechnicalResult.Failed, 45m, "system");

        result.IsSuccess.Should().BeTrue();
        offer.TechnicalResult.Should().Be(OfferTechnicalResult.Failed);
        offer.TechnicalTotalScore.Should().Be(45m);
    }

    // ═══════════════════════════════════════════════════════════
    //  Financial Envelope Gate Tests (CRITICAL)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void OpenFinancialEnvelope_Should_Succeed_For_Passed_Offer()
    {
        var offer = CreateOffer();
        offer.SetTechnicalResult(OfferTechnicalResult.Passed, 85m, "system");

        var result = offer.OpenFinancialEnvelope("chair-001");

        result.IsSuccess.Should().BeTrue();
        offer.IsFinancialEnvelopeOpen.Should().BeTrue();
        offer.FinancialEnvelopeOpenedAt.Should().NotBeNull();
        offer.FinancialEnvelopeOpenedBy.Should().Be("chair-001");
    }

    [Fact]
    public void OpenFinancialEnvelope_Should_Fail_For_Pending_Offer()
    {
        var offer = CreateOffer();

        var result = offer.OpenFinancialEnvelope("chair-001");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("passed");
        offer.IsFinancialEnvelopeOpen.Should().BeFalse();
    }

    [Fact]
    public void OpenFinancialEnvelope_Should_Fail_For_Failed_Offer()
    {
        var offer = CreateOffer();
        offer.SetTechnicalResult(OfferTechnicalResult.Failed, 30m, "system");

        var result = offer.OpenFinancialEnvelope("chair-001");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("passed");
        offer.IsFinancialEnvelopeOpen.Should().BeFalse();
    }

    [Fact]
    public void OpenFinancialEnvelope_Should_Fail_If_Already_Open()
    {
        var offer = CreateOffer();
        offer.SetTechnicalResult(OfferTechnicalResult.Passed, 85m, "system");
        offer.OpenFinancialEnvelope("chair-001");

        var result = offer.OpenFinancialEnvelope("chair-001");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already open");
    }

    // ═══════════════════════════════════════════════════════════
    //  Blind Code Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BlindCode_Should_Be_Unique_Identifier()
    {
        var offer1 = CreateOffer();
        var offer2 = CreateOffer();

        offer1.BlindCode.Should().NotBe(offer2.BlindCode);
    }

    [Fact]
    public void BlindCode_Should_Not_Reveal_Supplier_Identity()
    {
        var offer = CreateOffer();

        offer.BlindCode.Should().NotContain("Supplier");
        offer.BlindCode.Should().NotContain("CR-123456");
    }
}
