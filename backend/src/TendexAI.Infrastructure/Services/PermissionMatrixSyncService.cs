using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Synchronizes PermissionMatrixRules with the legacy RolePermissions table.
/// When the admin updates the matrix grid, this service translates matrix rules
/// (ResourceType + PermissionAction bitfield) into legacy permission codes
/// (e.g., "rfp.view", "committees.create") so that the JWT token reflects
/// the matrix state on next login.
/// </summary>
public sealed class PermissionMatrixSyncService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<PermissionMatrixSyncService> _logger;

    public PermissionMatrixSyncService(
        TenantDbContext context,
        ILogger<PermissionMatrixSyncService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes all non-protected roles' RolePermissions based on their PermissionMatrixRules.
    /// Protected roles (TenantPrimaryAdmin) are skipped — they always get full access.
    /// </summary>
    public async Task SyncAllRolesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .Where(r => r.TenantId == tenantId && !r.IsProtected)
            .ToListAsync(cancellationToken);

        var allPermissions = await _context.Permissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var permissionsByCode = allPermissions.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            await SyncRolePermissionsAsync(role.Id, tenantId, permissionsByCode, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Synced RolePermissions for {Count} roles in tenant {TenantId}", roles.Count, tenantId);
    }

    /// <summary>
    /// Synchronizes a single role's RolePermissions based on its PermissionMatrixRules.
    /// </summary>
    private async Task SyncRolePermissionsAsync(
        Guid roleId,
        Guid tenantId,
        Dictionary<string, Permission> permissionsByCode,
        CancellationToken cancellationToken)
    {
        // Get all matrix rules for this role
        var matrixRules = await _context.PermissionMatrixRules
            .AsNoTracking()
            .Where(r => r.RoleId == roleId && r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        // Convert matrix rules to permission codes
        var grantedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in matrixRules)
        {
            var codes = MapMatrixRuleToPermissionCodes(rule.ResourceType, (PermissionAction)rule.AllowedActions);
            foreach (var code in codes)
            {
                grantedCodes.Add(code);
            }
        }

        // Get permission IDs for the granted codes
        var grantedPermissionIds = grantedCodes
            .Where(code => permissionsByCode.ContainsKey(code))
            .Select(code => permissionsByCode[code].Id)
            .Distinct()
            .ToList();

        // Remove existing RolePermissions for this role
        var existingRolePermissions = await _context.Set<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _context.Set<RolePermission>().RemoveRange(existingRolePermissions);

        // Add new RolePermissions
        var newRolePermissions = grantedPermissionIds
            .Select(pid => new RolePermission(roleId, pid))
            .ToList();

        await _context.Set<RolePermission>().AddRangeAsync(newRolePermissions, cancellationToken);

        _logger.LogDebug(
            "Role {RoleId}: removed {Removed} old permissions, added {Added} new permissions from {RuleCount} matrix rules",
            roleId, existingRolePermissions.Count, newRolePermissions.Count, matrixRules.Count);
    }

    /// <summary>
    /// Maps a ResourceType + PermissionAction combination to legacy permission codes.
    /// This is the critical bridge between the new matrix system and the legacy JWT permission system.
    /// </summary>
    private static List<string> MapMatrixRuleToPermissionCodes(
        ResourceType resourceType, PermissionAction actions)
    {
        var codes = new List<string>();

        switch (resourceType)
        {
            // ═══════════════════════════════════════════════════════════
            //  Global Resources
            // ═══════════════════════════════════════════════════════════
            case ResourceType.Organization:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("settings.view");
                if (actions.HasFlag(PermissionAction.Update)) { codes.Add("settings.edit"); codes.Add("settings.branding"); }
                break;

            case ResourceType.Users:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("users.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("users.create");
                if (actions.HasFlag(PermissionAction.Update)) { codes.Add("users.edit"); codes.Add("users.manage_roles"); }
                if (actions.HasFlag(PermissionAction.Delete)) codes.Add("users.delete");
                break;

            case ResourceType.Roles:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("roles.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("roles.create");
                if (actions.HasFlag(PermissionAction.Update)) codes.Add("roles.edit");
                if (actions.HasFlag(PermissionAction.Delete)) codes.Add("roles.delete");
                break;

            case ResourceType.Dashboard:
                if (actions.HasFlag(PermissionAction.Read)) { codes.Add("dashboard.view"); codes.Add("dashboard.export"); }
                break;

            case ResourceType.Reports:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("reports.view");
                if (actions.HasFlag(PermissionAction.Create)) { codes.Add("reports.create"); codes.Add("reports.export"); }
                break;

            case ResourceType.AiAssistant:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("ai.view");
                if (actions.HasFlag(PermissionAction.Create))
                {
                    codes.Add("ai.use_assistant");
                    codes.Add("ai.chat");
                    codes.Add("ai.analyze");
                    codes.Add("ai.generate_content");
                }
                break;

            case ResourceType.AiConfiguration:
                if (actions.HasFlag(PermissionAction.Read) || actions.HasFlag(PermissionAction.Update))
                    codes.Add("ai.configure");
                break;

            case ResourceType.KnowledgeBase:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("knowledge.view");
                if (actions.HasFlag(PermissionAction.Create) || actions.HasFlag(PermissionAction.Upload))
                    codes.Add("knowledge.upload");
                if (actions.HasFlag(PermissionAction.Update) || actions.HasFlag(PermissionAction.Delete))
                    codes.Add("knowledge.manage");
                break;

            case ResourceType.WorkflowDefinitions:
                if (actions.HasFlag(PermissionAction.Read)) { codes.Add("workflow.view"); codes.Add("workflows.view"); }
                if (actions.HasFlag(PermissionAction.Create))
                {
                    codes.Add("workflow.create");
                    codes.Add("workflow.manage");
                }
                if (actions.HasFlag(PermissionAction.Update))
                {
                    codes.Add("workflow.edit");
                    codes.Add("workflow.manage");
                }
                if (actions.HasFlag(PermissionAction.Delete))
                {
                    codes.Add("workflow.delete");
                    codes.Add("workflow.manage");
                }
                if (actions.HasFlag(PermissionAction.Approve)) codes.Add("workflow.approve");
                break;

            case ResourceType.AuditLogs:
                if (actions.HasFlag(PermissionAction.Read)) { codes.Add("audit.view"); codes.Add("audit.export"); }
                break;

            case ResourceType.SystemSettings:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("settings.view");
                if (actions.HasFlag(PermissionAction.Update))
                {
                    codes.Add("settings.edit");
                    codes.Add("settings.branding");
                    codes.Add("settings.smtp");
                }
                break;

            case ResourceType.Invitations:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("users.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("users.create");
                break;

            case ResourceType.Notifications:
                // Notifications don't have specific permission codes in the legacy system
                break;

            // ═══════════════════════════════════════════════════════════
            //  Competition Resources
            // ═══════════════════════════════════════════════════════════
            case ResourceType.Competition:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("competitions.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("competitions.create");
                if (actions.HasFlag(PermissionAction.Update)) { codes.Add("competitions.edit"); codes.Add("competitions.manage_phases"); }
                if (actions.HasFlag(PermissionAction.Delete)) codes.Add("competitions.delete");
                break;

            case ResourceType.Booklet:
            case ResourceType.RfpSections:
            case ResourceType.BoqItems:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("rfp.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("rfp.create");
                if (actions.HasFlag(PermissionAction.Update)) codes.Add("rfp.edit");
                if (actions.HasFlag(PermissionAction.Delete)) codes.Add("rfp.delete");
                if (actions.HasFlag(PermissionAction.Approve)) codes.Add("rfp.approve");
                if (actions.HasFlag(PermissionAction.Submit)) codes.Add("rfp.publish");
                break;

            case ResourceType.EvaluationCriteria:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("evaluation.view");
                if (actions.HasFlag(PermissionAction.Create) || actions.HasFlag(PermissionAction.Update))
                    codes.Add("evaluation.technical");
                break;

            case ResourceType.Offers:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("offers.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("offers.create");
                if (actions.HasFlag(PermissionAction.Update)) codes.Add("offers.edit");
                if (actions.HasFlag(PermissionAction.Delete)) codes.Add("offers.delete");
                if (actions.HasFlag(PermissionAction.Approve)) { codes.Add("offers.review"); codes.Add("offers.open"); }
                break;

            case ResourceType.TechnicalEvaluation:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("evaluation.view");
                if (actions.HasFlag(PermissionAction.Create) || actions.HasFlag(PermissionAction.Update))
                    codes.Add("evaluation.technical");
                if (actions.HasFlag(PermissionAction.Score)) codes.Add("evaluation.technical_score");
                if (actions.HasFlag(PermissionAction.Approve)) codes.Add("evaluation.approve");
                if (actions.HasFlag(PermissionAction.Submit)) codes.Add("evaluation.export");
                break;

            case ResourceType.FinancialEvaluation:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("evaluation.view");
                if (actions.HasFlag(PermissionAction.Create) || actions.HasFlag(PermissionAction.Update))
                    codes.Add("evaluation.financial");
                if (actions.HasFlag(PermissionAction.Score)) codes.Add("evaluation.financial_score");
                if (actions.HasFlag(PermissionAction.Approve)) codes.Add("evaluation.approve");
                if (actions.HasFlag(PermissionAction.Submit)) codes.Add("evaluation.export");
                break;

            case ResourceType.Inquiries:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("inquiries.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("inquiries.create");
                if (actions.HasFlag(PermissionAction.Update)) { codes.Add("inquiries.respond"); codes.Add("inquiries.manage"); }
                break;

            case ResourceType.Attachments:
                // Attachments inherit from their parent resource permissions
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("rfp.view");
                if (actions.HasFlag(PermissionAction.Upload)) codes.Add("rfp.edit");
                break;

            case ResourceType.AwardRecommendation:
            case ResourceType.Contracts:
            case ResourceType.Guarantees:
            case ResourceType.Grievances:
                // These map to competition-level permissions
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("competitions.view");
                if (actions.HasFlag(PermissionAction.Create) || actions.HasFlag(PermissionAction.Update))
                    codes.Add("competitions.edit");
                if (actions.HasFlag(PermissionAction.Approve)) codes.Add("rfp.approve");
                break;

            case ResourceType.EvaluationMinutes:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("evaluation.view");
                if (actions.HasFlag(PermissionAction.Create) || actions.HasFlag(PermissionAction.Update))
                    codes.Add("evaluation.export");
                break;

            // ═══════════════════════════════════════════════════════════
            //  Committee Resources
            // ═══════════════════════════════════════════════════════════
            case ResourceType.Committee:
            case ResourceType.CommitteeMembers:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("committees.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("committees.create");
                if (actions.HasFlag(PermissionAction.Update)) codes.Add("committees.edit");
                if (actions.HasFlag(PermissionAction.Delete)) codes.Add("committees.delete");
                if (resourceType == ResourceType.CommitteeMembers &&
                    (actions.HasFlag(PermissionAction.Create) || actions.HasFlag(PermissionAction.Update)))
                    codes.Add("committees.manage_members");
                break;

            case ResourceType.CommitteeMeetings:
            case ResourceType.CommitteeTasks:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("committees.view");
                if (actions.HasFlag(PermissionAction.Update)) codes.Add("committees.edit");
                break;

            // ═══════════════════════════════════════════════════════════
            //  Task Center Resources
            // ═══════════════════════════════════════════════════════════
            case ResourceType.Tasks:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("tasks.view");
                if (actions.HasFlag(PermissionAction.Create)) codes.Add("tasks.create");
                if (actions.HasFlag(PermissionAction.Update)) codes.Add("tasks.edit");
                if (actions.HasFlag(PermissionAction.Approve)) codes.Add("tasks.approve");
                break;

            case ResourceType.ApprovalTasks:
                if (actions.HasFlag(PermissionAction.Read)) codes.Add("tasks.view");
                if (actions.HasFlag(PermissionAction.Approve) || actions.HasFlag(PermissionAction.Reject))
                    codes.Add("tasks.approve");
                break;

            default:
                break;
        }

        return codes;
    }
}
