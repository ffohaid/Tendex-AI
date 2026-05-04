using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Workflow.Services;

/// <summary>
/// Resolves the approval actor roles from the authenticated user context and tenant data.
/// Provides a single source of truth for mapping identity roles and committee assignments
/// to the workflow authorization model.
/// </summary>
public static class ApprovalActorResolver
{
    /// <summary>
    /// Resolves all system roles represented by the supplied role identifiers.
    /// Supports both display names and normalized role names.
    /// </summary>
    public static IReadOnlyCollection<SystemRole> ResolveSystemRoles(IEnumerable<string>? roleIdentifiers)
    {
        if (roleIdentifiers is null)
            return Array.Empty<SystemRole>();

        var resolvedRoles = new HashSet<SystemRole>();

        foreach (var roleIdentifier in roleIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(roleIdentifier))
                continue;

            var systemRole = MapRoleNameToSystemRole(roleIdentifier);
            if (systemRole.HasValue)
                resolvedRoles.Add(systemRole.Value);
        }

        return resolvedRoles.ToList().AsReadOnly();
    }

    /// <summary>
    /// Resolves all workflow committee roles the user holds for the specified competition.
    /// </summary>
    public static async Task<IReadOnlyCollection<CommitteeRole>> ResolveCommitteeRolesForCompetitionAsync(
        ITenantDbContext dbContext,
        Guid competitionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var committeeMembers = dbContext.GetDbSet<CommitteeMember>();
        var committees = dbContext.GetDbSet<Committee>();
        var committeeCompetitions = dbContext.GetDbSet<CommitteeCompetition>();

        var roles = await committeeMembers
            .AsNoTracking()
            .Where(cm => cm.UserId == userId && cm.IsActive)
            .Join(
                committees.AsNoTracking(),
                cm => cm.CommitteeId,
                c => c.Id,
                (cm, c) => new { CommitteeId = c.Id, c.Type, cm.Role })
            .Join(
                committeeCompetitions.AsNoTracking().Where(cc => cc.CompetitionId == competitionId),
                x => x.CommitteeId,
                cc => cc.CommitteeId,
                (x, _) => MapCommitteeAssignmentToWorkflowRole(x.Type, x.Role))
            .Where(role => role.HasValue)
            .Select(role => role!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return roles.AsReadOnly();
    }

    /// <summary>
    /// Maps an identity role name or normalized role identifier to the corresponding workflow system role.
    /// </summary>
    public static SystemRole? MapRoleNameToSystemRole(string roleName)
    {
        var normalized = roleName.Trim().ToLowerInvariant();

        return normalized switch
        {
            "tenant primary admin"
                or "tenantprimaryadmin"
                or "primary admin"
                or "authority owner"
                or "authorityowner"
                or "tenant owner"
                or "tenantowner"
                or "tenant admin"
                or "tenantadmin"
                or "owner" => SystemRole.TenantPrimaryAdmin,
            "procurement manager" or "procurementmanager" => SystemRole.ProcurementManager,
            "financial controller" or "financialcontroller" => SystemRole.FinancialController,
            "sector representative" or "sectorrepresentative" => SystemRole.SectorRepresentative,
            "committee chair" or "committeechair" or "chair" => SystemRole.CommitteeChair,
            "committee member" or "committeemember" => SystemRole.CommitteeMember,
            "member" => SystemRole.Member,
            "viewer" or "readonly" => SystemRole.Viewer,
            _ => TryParseSystemRole(normalized)
        };
    }

    /// <summary>
    /// Maps a committee assignment into the workflow committee role used by approval steps.
    /// </summary>
    public static CommitteeRole? MapCommitteeAssignmentToWorkflowRole(
        CommitteeType committeeType,
        CommitteeMemberRole memberRole)
    {
        if (memberRole == CommitteeMemberRole.Secretary)
            return CommitteeRole.CommitteeSecretary;

        return committeeType switch
        {
            CommitteeType.BookletPreparation when memberRole == CommitteeMemberRole.Chair => CommitteeRole.PreparationCommitteeChair,
            CommitteeType.BookletPreparation when memberRole == CommitteeMemberRole.Member => CommitteeRole.PreparationCommitteeMember,
            CommitteeType.TechnicalEvaluation when memberRole == CommitteeMemberRole.Chair => CommitteeRole.TechnicalExamCommitteeChair,
            CommitteeType.TechnicalEvaluation when memberRole == CommitteeMemberRole.Member => CommitteeRole.TechnicalExamCommitteeMember,
            CommitteeType.FinancialEvaluation when memberRole == CommitteeMemberRole.Chair => CommitteeRole.FinancialExamCommitteeChair,
            CommitteeType.FinancialEvaluation when memberRole == CommitteeMemberRole.Member => CommitteeRole.FinancialExamCommitteeMember,
            CommitteeType.InquiryReview when memberRole == CommitteeMemberRole.Chair => CommitteeRole.InquiryReviewCommitteeChair,
            CommitteeType.InquiryReview when memberRole == CommitteeMemberRole.Member => CommitteeRole.InquiryReviewCommitteeMember,
            _ => null
        };
    }

    private static SystemRole? TryParseSystemRole(string normalized)
    {
        if (Enum.TryParse<SystemRole>(normalized, ignoreCase: true, out var role))
            return role;

        var noSpaces = normalized.Replace(" ", string.Empty);
        if (Enum.TryParse<SystemRole>(noSpaces, ignoreCase: true, out var parsedWithoutSpaces))
            return parsedWithoutSpaces;

        return null;
    }
}
