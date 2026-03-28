using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a signatory on an evaluation minutes document.
/// Per PRD Section 11.2 — electronic signatures.
/// </summary>
public sealed class MinutesSignatory : BaseEntity<Guid>
{
    private MinutesSignatory() { }

    public static MinutesSignatory Create(
        Guid evaluationMinutesId, string userId, string fullName,
        string role, string createdBy)
    {
        return new MinutesSignatory
        {
            Id = Guid.NewGuid(),
            EvaluationMinutesId = evaluationMinutesId,
            UserId = userId,
            FullName = fullName,
            Role = role,
            HasSigned = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid EvaluationMinutesId { get; private set; }
    public string UserId { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public bool HasSigned { get; private set; }
    public DateTime? SignedAt { get; private set; }

    public EvaluationMinutes EvaluationMinutes { get; private set; } = default!;

    public Result Sign(string signedBy)
    {
        if (HasSigned)
            return Result.Failure("Already signed.");
        if (signedBy != UserId)
            return Result.Failure("Only the designated signatory can sign.");
        HasSigned = true;
        SignedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = signedBy;
        return Result.Success();
    }
}
