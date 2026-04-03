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
    public static Result ValidateAssignment(
        Guid userId,
        CommitteeType targetCommitteeType,
        CommitteeMemberRole targetRole,
        IReadOnlyList<(CommitteeType Type, CommitteeMemberRole Role)> existingMemberships)
    {
        var isTargetChair = targetRole == CommitteeMemberRole.Chair;

        // Rule 1: Cannot be Chair of both Technical and Financial committees
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
    /// Validates that a committee's selected phases are consistent with its type.
    /// Technical committees must include the TechnicalAnalysis phase.
    /// Financial committees must include the FinancialAnalysis phase.
    /// </summary>
    public static Result ValidatePhaseScope(
        CommitteeType committeeType,
        List<CompetitionPhase>? phases)
    {
        // If no phases specified (comprehensive scope), all phases are included — always valid
        if (phases is null || phases.Count == 0)
            return Result.Success();

        if (committeeType == CommitteeType.TechnicalEvaluation)
        {
            if (!phases.Contains(CompetitionPhase.TechnicalAnalysis))
                return Result.Failure("Technical evaluation committee must include the Technical Analysis phase.");
        }

        if (committeeType == CommitteeType.FinancialEvaluation)
        {
            if (!phases.Contains(CompetitionPhase.FinancialAnalysis))
                return Result.Failure("Financial evaluation committee must include the Financial Analysis phase.");
        }

        return Result.Success();
    }
}
