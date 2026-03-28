using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a ranked supplier offer in the award recommendation.
/// Combines technical and financial scores for final ranking.
/// Per PRD Section 11.1 — final comprehensive minutes.
/// </summary>
public sealed class AwardRanking : BaseEntity<Guid>
{
    private AwardRanking() { }

    public static AwardRanking Create(
        Guid awardRecommendationId, Guid supplierOfferId, string supplierName,
        int rank, decimal technicalScore, decimal financialScore,
        decimal combinedScore, decimal totalOfferAmount, string createdBy)
    {
        return new AwardRanking
        {
            Id = Guid.NewGuid(),
            AwardRecommendationId = awardRecommendationId,
            SupplierOfferId = supplierOfferId,
            SupplierName = supplierName,
            Rank = rank,
            TechnicalScore = technicalScore,
            FinancialScore = financialScore,
            CombinedScore = combinedScore,
            TotalOfferAmount = totalOfferAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid AwardRecommendationId { get; private set; }
    public Guid SupplierOfferId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public int Rank { get; private set; }
    public decimal TechnicalScore { get; private set; }
    public decimal FinancialScore { get; private set; }
    public decimal CombinedScore { get; private set; }
    public decimal TotalOfferAmount { get; private set; }

    public AwardRecommendation AwardRecommendation { get; private set; } = default!;
    public SupplierOffer SupplierOffer { get; private set; } = default!;
}
