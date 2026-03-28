using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a supplier's submitted offer for a competition.
/// Contains both technical and financial components, stored separately.
/// The financial envelope remains encrypted/locked until technical evaluation is approved.
/// Per PRD Section 8.5.
/// </summary>
public sealed class SupplierOffer : BaseEntity<Guid>
{
    private SupplierOffer() { } // EF Core constructor

    public static SupplierOffer Create(
        Guid competitionId,
        Guid tenantId,
        string supplierName,
        string supplierIdentifier,
        string offerReferenceNumber,
        DateTime submissionDate,
        string createdBy)
    {
        return new SupplierOffer
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            SupplierName = supplierName,
            SupplierIdentifier = supplierIdentifier,
            OfferReferenceNumber = offerReferenceNumber,
            SubmissionDate = submissionDate,
            BlindCode = GenerateBlindCode(),
            TechnicalResult = OfferTechnicalResult.Pending,
            IsFinancialEnvelopeOpen = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid CompetitionId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Actual supplier name — hidden during blind evaluation.</summary>
    public string SupplierName { get; private set; } = default!;

    /// <summary>Supplier registration/commercial number.</summary>
    public string SupplierIdentifier { get; private set; } = default!;

    /// <summary>Offer reference number assigned by the system.</summary>
    public string OfferReferenceNumber { get; private set; } = default!;

    /// <summary>Date the offer was submitted.</summary>
    public DateTime SubmissionDate { get; private set; }

    /// <summary>
    /// Random anonymous code used during blind evaluation (e.g., "OFFER-A7X2").
    /// Hides supplier identity from the evaluation committee per PRD Section 9.1.
    /// </summary>
    public string BlindCode { get; private set; } = default!;

    /// <summary>Technical evaluation result (Pending, Passed, Failed).</summary>
    public OfferTechnicalResult TechnicalResult { get; private set; }

    /// <summary>Total weighted technical score (0-100).</summary>
    public decimal? TechnicalTotalScore { get; private set; }

    /// <summary>
    /// Whether the financial envelope has been opened.
    /// STRICT RULE: Must remain false until technical evaluation is approved.
    /// Per PRD Section 8.5 and 10.1.
    /// </summary>
    public bool IsFinancialEnvelopeOpen { get; private set; }

    /// <summary>Timestamp when the financial envelope was opened.</summary>
    public DateTime? FinancialEnvelopeOpenedAt { get; private set; }

    /// <summary>User who opened the financial envelope.</summary>
    public string? FinancialEnvelopeOpenedBy { get; private set; }

    // ═══════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Sets the technical evaluation result and total score.
    /// </summary>
    public Result SetTechnicalResult(
        OfferTechnicalResult result,
        decimal totalScore,
        string modifiedBy)
    {
        if (totalScore < 0 || totalScore > 100)
            return Result.Failure("Technical total score must be between 0 and 100.");

        TechnicalResult = result;
        TechnicalTotalScore = totalScore;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }

    /// <summary>
    /// Opens the financial envelope. Only allowed if the offer passed technical evaluation.
    /// Enforces PRD Section 10.1 strict rule.
    /// </summary>
    public Result OpenFinancialEnvelope(string openedBy)
    {
        if (TechnicalResult != OfferTechnicalResult.Passed)
            return Result.Failure("Cannot open financial envelope: offer has not passed technical evaluation.");

        if (IsFinancialEnvelopeOpen)
            return Result.Failure("Financial envelope is already open.");

        IsFinancialEnvelopeOpen = true;
        FinancialEnvelopeOpenedAt = DateTime.UtcNow;
        FinancialEnvelopeOpenedBy = openedBy;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = openedBy;
        return Result.Success();
    }

    // ═══════════════════════════════════════════════
    //  Private Helpers
    // ═══════════════════════════════════════════════

    private static string GenerateBlindCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        var code = new char[4];
        for (int i = 0; i < code.Length; i++)
            code[i] = chars[random.Next(chars.Length)];
        return $"OFFER-{new string(code)}";
    }
}
