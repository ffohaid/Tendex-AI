using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Committees;

/// <summary>
/// Encapsulates conflict of interest business rules for committee assignments.
/// Maps to PRD Section 4.2 (Critical Note) and Section 23 (Business Rules #3, #7).
/// 
/// Key rules:
/// 1. Technical and Financial evaluation committees must be completely separate.
/// 2. A person who prepared the booklet cannot approve evaluation results.
/// 3. Each committee must have its own independent chair and formation.
/// </summary>
public static class ConflictOfInterestRules
{
    /// <summary>
    /// Validates that adding a user to a committee does not violate conflict of interest rules.
    /// </summary>
    /// <param name="userId">The user being added.</param>
    /// <param name="targetCommitteeType">The type of committee the user is being added to.</param>
    /// <param name="targetRole">The role the user will have in the target committee.</param>
    /// <param name="existingMemberships">All existing active committee memberships for this user in the same competition.</param>
    /// <returns>A Result indicating success or the specific conflict violation.</returns>
    public static Result ValidateAssignment(
        Guid userId,
        CommitteeType targetCommitteeType,
        CommitteeMemberRole targetRole,
        IReadOnlyList<(CommitteeType Type, CommitteeMemberRole Role)> existingMemberships)
    {
        // Rule 1: Cannot be Chair of both Technical and Financial committees
        // PRD Section 4.2: "Each committee must have its own chair and independent formation"
        var isTargetChair = targetRole == CommitteeMemberRole.Chair;

        if (isTargetChair && targetCommitteeType == CommitteeType.TechnicalEvaluation)
        {
            if (existingMemberships.Any(m =>
                m.Type == CommitteeType.FinancialEvaluation &&
                m.Role == CommitteeMemberRole.Chair))
            {
                return Result.Failure(
                    "Conflict of interest: A user cannot be the chair of both the Technical and Financial evaluation committees.");
            }
        }

        if (isTargetChair && targetCommitteeType == CommitteeType.FinancialEvaluation)
        {
            if (existingMemberships.Any(m =>
                m.Type == CommitteeType.TechnicalEvaluation &&
                m.Role == CommitteeMemberRole.Chair))
            {
                return Result.Failure(
                    "Conflict of interest: A user cannot be the chair of both the Technical and Financial evaluation committees.");
            }
        }

        // Rule 2: Booklet preparer cannot be Chair of evaluation committees
        // PRD Section 23, Rule #7: "Person who prepared booklet CANNOT approve evaluation results"
        if (targetCommitteeType is CommitteeType.TechnicalEvaluation or CommitteeType.FinancialEvaluation &&
            targetRole == CommitteeMemberRole.Chair)
        {
            if (existingMemberships.Any(m => m.Type == CommitteeType.BookletPreparation))
            {
                return Result.Failure(
                    "Conflict of interest: A booklet preparation committee member cannot chair an evaluation committee.");
            }
        }

        // Rule 3: Evaluation committee chair cannot be in booklet preparation committee
        if (targetCommitteeType == CommitteeType.BookletPreparation)
        {
            if (existingMemberships.Any(m =>
                m.Type is CommitteeType.TechnicalEvaluation or CommitteeType.FinancialEvaluation &&
                m.Role == CommitteeMemberRole.Chair))
            {
                return Result.Failure(
                    "Conflict of interest: An evaluation committee chair cannot be a member of the booklet preparation committee.");
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates that a committee assignment respects the phase-based activation rules.
    /// Technical committees should only be active during Technical Analysis phase.
    /// Financial committees should only be active during Financial Analysis phase.
    /// </summary>
    public static Result ValidatePhaseScope(
        CommitteeType committeeType,
        CompetitionPhase? activeFromPhase,
        CompetitionPhase? activeToPhase)
    {
        if (committeeType == CommitteeType.TechnicalEvaluation)
        {
            if (activeFromPhase.HasValue && activeFromPhase.Value > CompetitionPhase.TechnicalAnalysis)
                return Result.Failure("Technical evaluation committee must be active during the Technical Analysis phase.");

            if (activeToPhase.HasValue && activeToPhase.Value < CompetitionPhase.TechnicalAnalysis)
                return Result.Failure("Technical evaluation committee must be active during the Technical Analysis phase.");
        }

        if (committeeType == CommitteeType.FinancialEvaluation)
        {
            if (activeFromPhase.HasValue && activeFromPhase.Value > CompetitionPhase.FinancialAnalysis)
                return Result.Failure("Financial evaluation committee must be active during the Financial Analysis phase.");

            if (activeToPhase.HasValue && activeToPhase.Value < CompetitionPhase.FinancialAnalysis)
                return Result.Failure("Financial evaluation committee must be active during the Financial Analysis phase.");
        }

        return Result.Success();
    }
}
