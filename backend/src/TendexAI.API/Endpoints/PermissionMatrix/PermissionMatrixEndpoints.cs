using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Interfaces;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.API.Endpoints.PermissionMatrix;

/// <summary>
/// API endpoints for managing the flexible permission matrix.
/// Only accessible by Admin and Owner roles.
/// </summary>
public static class PermissionMatrixEndpoints
{
    public static void MapPermissionMatrixEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/permission-matrix")
            .WithTags("Permission Matrix")
            .RequireAuthorization();

        // Get the full permission matrix for the current tenant
        group.MapGet("/", GetMatrixAsync)
            .WithName("GetPermissionMatrix")
            .WithDescription("Gets the full permission matrix for the current tenant");

        // Get permission matrix grouped by scope
        group.MapGet("/by-scope/{scope}", GetMatrixByScopeAsync)
            .WithName("GetPermissionMatrixByScope")
            .WithDescription("Gets the permission matrix filtered by scope");

        // Get permission matrix for a specific role
        group.MapGet("/by-role/{roleId:guid}", GetMatrixByRoleAsync)
            .WithName("GetPermissionMatrixByRole")
            .WithDescription("Gets the permission matrix for a specific role");

        // Update a specific rule
        group.MapPut("/rules/{ruleId:guid}", UpdateRuleAsync)
            .WithName("UpdatePermissionRule")
            .WithDescription("Updates a specific permission matrix rule");

        // Bulk update rules for a role
        group.MapPut("/roles/{roleId:guid}/bulk", BulkUpdateRulesAsync)
            .WithName("BulkUpdatePermissionRules")
            .WithDescription("Bulk updates permission rules for a role");

        // Reset matrix to defaults
        group.MapPost("/reset", ResetMatrixAsync)
            .WithName("ResetPermissionMatrix")
            .WithDescription("Resets the permission matrix to system defaults");

        // Get current user's effective permissions
        group.MapGet("/my-permissions", GetMyPermissionsAsync)
            .WithName("GetMyPermissions")
            .WithDescription("Gets the current user's effective permissions");

        // Get current user's competition permissions
        group.MapGet("/my-permissions/competition/{competitionId:guid}", GetMyCompetitionPermissionsAsync)
            .WithName("GetMyCompetitionPermissions")
            .WithDescription("Gets the current user's effective permissions for a specific competition");

        // Check if current user has a specific permission
        group.MapGet("/check", CheckPermissionAsync)
            .WithName("CheckPermission")
            .WithDescription("Checks if the current user has a specific permission");

        // Get resource types metadata
        group.MapGet("/resource-types", GetResourceTypesAsync)
            .WithName("GetResourceTypes")
            .WithDescription("Gets all resource types with their metadata");

        // Get permission actions metadata
        group.MapGet("/actions", GetPermissionActionsAsync)
            .WithName("GetPermissionActions")
            .WithDescription("Gets all permission actions with their metadata");

        // Seed default rules for the current tenant (admin only)
        group.MapPost("/seed", SeedDefaultRulesAsync)
            .WithName("SeedDefaultPermissionRules")
            .WithDescription("Seeds default permission matrix rules for the current tenant");
    }

    private static async Task<IResult> GetMatrixAsync(
        [FromServices] IPermissionMatrixRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        var rules = await repository.GetAllByTenantAsync(tenantId, cancellationToken);

        var response = rules.Select(r => MapToResponse(r)).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetMatrixByScopeAsync(
        [FromRoute] ResourceScope scope,
        [FromServices] IPermissionMatrixRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        var rules = await repository.GetAllByTenantAsync(tenantId, cancellationToken);
        var filtered = rules.Where(r => r.Scope == scope).ToList();

        var response = filtered.Select(r => MapToResponse(r)).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetMatrixByRoleAsync(
        [FromRoute] Guid roleId,
        [FromServices] IPermissionMatrixRepository repository,
        CancellationToken cancellationToken)
    {
        var rules = await repository.GetAllRulesForRoleAsync(roleId, cancellationToken);

        var response = rules.Select(r => MapToResponse(r)).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateRuleAsync(
        [FromRoute] Guid ruleId,
        [FromBody] UpdateRuleRequest request,
        [FromServices] IPermissionMatrixRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(ruleId, cancellationToken);
        if (rule == null)
            return Results.NotFound(new { message = "Rule not found" });

        var userId = GetCurrentUserId(httpContext);
        rule.UpdateAllowedActions(request.AllowedActions, userId);

        await repository.SaveChangesAsync(cancellationToken);

        return Results.Ok(MapToResponse(rule));
    }

    private static async Task<IResult> BulkUpdateRulesAsync(
        [FromRoute] Guid roleId,
        [FromBody] BulkUpdateRulesRequest request,
        [FromServices] IPermissionMatrixRepository repository,
        [FromServices] IRoleRepository roleRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        // Prevent modifying permission matrix for protected roles
        var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is not null && role.IsProtected)
            return Results.Problem(
                "Protected governance roles have full access and their permissions cannot be modified.",
                statusCode: 403);

        var userId = GetCurrentUserId(httpContext);

        foreach (var update in request.Updates)
        {
            // Find existing rule by dimensions
            var existingRule = await repository.GetRuleAsync(
                roleId, update.Scope, update.ResourceType,
                update.CommitteeRole, update.CompetitionPhase, cancellationToken);

            if (existingRule != null)
            {
                existingRule.UpdateAllowedActions(update.AllowedActions, userId);
            }
            else
            {
                // Create new rule
                var newRule = PermissionMatrixRule.Create(
                    tenantId, roleId, update.Scope, update.ResourceType,
                    update.AllowedActions, update.CommitteeRole, update.CompetitionPhase,
                    isCustomized: true, createdBy: userId);
                await repository.AddAsync(newRule, cancellationToken);
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "Rules updated successfully", count = request.Updates.Count });
    }

    private static async Task<IResult> ResetMatrixAsync(
        [FromServices] IPermissionMatrixRepository repository,
        [FromServices] IRoleRepository roleRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        // Remove all existing rules
        await repository.RemoveAllByTenantAsync(tenantId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Get system roles and build the role map
        var roles = await roleRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var roleMap = MapRolesToSystemRoles(roles);

        // Seed default rules
        var defaultRules = Domain.StateMachine.DefaultPermissionMatrixRuleSeeder
            .GenerateDefaultRules(tenantId, roleMap);

        await repository.AddRangeAsync(defaultRules, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Permission matrix reset to defaults", rulesCount = defaultRules.Count });
    }

    private static async Task<IResult> GetMyPermissionsAsync(
        [FromServices] IPermissionEvaluator evaluator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(httpContext);
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var permissions = await evaluator.GetUserPermissionSummaryAsync(userId, cancellationToken);
        return Results.Ok(permissions);
    }

    private static async Task<IResult> GetMyCompetitionPermissionsAsync(
        [FromRoute] Guid competitionId,
        [FromServices] IPermissionEvaluator evaluator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(httpContext);
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var permissions = await evaluator.GetUserCompetitionPermissionSummaryAsync(
            userId, competitionId, cancellationToken);
        return Results.Ok(permissions);
    }

    private static async Task<IResult> CheckPermissionAsync(
        [FromQuery] ResourceScope scope,
        [FromQuery] ResourceType resourceType,
        [FromQuery] PermissionAction action,
        [FromQuery] Guid? competitionId,
        [FromQuery] Guid? committeeId,
        [FromServices] IPermissionEvaluator evaluator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(httpContext);
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var hasPermission = await evaluator.HasPermissionAsync(
            userId, scope, resourceType, action, competitionId, committeeId, cancellationToken);

        return Results.Ok(new { hasPermission, scope, resourceType, action });
    }

    private static IResult GetResourceTypesAsync()
    {
        var resourceTypes = Enum.GetValues<ResourceType>()
            .Select(rt => new
            {
                id = (int)rt,
                code = rt.ToString(),
                nameAr = GetResourceTypeNameAr(rt),
                nameEn = GetResourceTypeNameEn(rt),
                scope = GetDefaultScope(rt).ToString()
            })
            .ToList();

        return Results.Ok(resourceTypes);
    }

    private static IResult GetPermissionActionsAsync()
    {
        var actions = new[]
        {
            new { key = "read", flag = (int)PermissionAction.Read, nameAr = "قراءة", nameEn = "Read" },
            new { key = "create", flag = (int)PermissionAction.Create, nameAr = "إنشاء", nameEn = "Create" },
            new { key = "update", flag = (int)PermissionAction.Update, nameAr = "تعديل", nameEn = "Update" },
            new { key = "delete", flag = (int)PermissionAction.Delete, nameAr = "حذف", nameEn = "Delete" },
            new { key = "approve", flag = (int)PermissionAction.Approve, nameAr = "اعتماد", nameEn = "Approve" },
            new { key = "reject", flag = (int)PermissionAction.Reject, nameAr = "رفض", nameEn = "Reject" },
            new { key = "submit", flag = (int)PermissionAction.Submit, nameAr = "تقديم", nameEn = "Submit" },
            new { key = "upload", flag = (int)PermissionAction.Upload, nameAr = "رفع ملفات", nameEn = "Upload" },
            new { key = "score", flag = (int)PermissionAction.Score, nameAr = "تقييم", nameEn = "Score" },
            new { key = "sign", flag = (int)PermissionAction.Sign, nameAr = "توقيع", nameEn = "Sign" }
        };

        return Results.Ok(actions);
    }

    private static async Task<IResult> SeedDefaultRulesAsync(
        [FromServices] IPermissionMatrixRepository repository,
        [FromServices] IRoleRepository roleRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        // Check if rules already exist
        var existing = await repository.GetAllByTenantAsync(tenantId, cancellationToken);
        if (existing.Count > 0)
            return Results.Conflict(new { message = "Permission matrix rules already exist. Use /reset to regenerate." });

        // Get system roles
        var roles = await roleRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var roleMap = MapRolesToSystemRoles(roles);

        var defaultRules = Domain.StateMachine.DefaultPermissionMatrixRuleSeeder
            .GenerateDefaultRules(tenantId, roleMap);

        await repository.AddRangeAsync(defaultRules, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Default rules seeded successfully", rulesCount = defaultRules.Count });
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var tenantClaim = httpContext.User.FindFirstValue("tenant_id");
        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : Guid.Empty;
    }

    private static string GetCurrentUserId(HttpContext httpContext)
    {
        return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? "";
    }

    /// <summary>
    /// Maps Role entities to SystemRole enum values using NormalizedName.
    /// The NormalizedName in the database corresponds to the system role type.
    /// </summary>
    private static Dictionary<SystemRole, Guid> MapRolesToSystemRoles(IReadOnlyList<Role> roles)
    {
        var roleMap = new Dictionary<SystemRole, Guid>();
        var normalizedNameToSystemRole = new Dictionary<string, SystemRole>(StringComparer.OrdinalIgnoreCase)
        {
            ["OPERATOR_SUPER_ADMIN"] = SystemRole.OperatorSuperAdmin,
            ["TENANT_PRIMARY_ADMIN"] = SystemRole.TenantPrimaryAdmin,
            ["TENANT_OWNER"] = SystemRole.TenantPrimaryAdmin,
            ["TENANT_ADMIN"] = SystemRole.TenantPrimaryAdmin,
            ["SUPER_ADMIN"] = SystemRole.TenantPrimaryAdmin,
            ["PROCUREMENT_MANAGER"] = SystemRole.ProcurementManager,
            ["FINANCIAL_CONTROLLER"] = SystemRole.FinancialController,
            ["SECTOR_REPRESENTATIVE"] = SystemRole.SectorRepresentative,
            ["COMMITTEE_CHAIR"] = SystemRole.CommitteeChair,
            ["COMMITTEE_MEMBER"] = SystemRole.CommitteeMember,
            ["MEMBER"] = SystemRole.Member,
            ["VIEWER"] = SystemRole.Viewer
        };

        foreach (var role in roles)
        {
            if (role.IsSystemRole && normalizedNameToSystemRole.TryGetValue(role.NormalizedName, out var systemRole))
            {
                // Use the first match (don't overwrite if already mapped)
                roleMap.TryAdd(systemRole, role.Id);
            }
        }

        return roleMap;
    }

    private static PermissionMatrixRuleResponse MapToResponse(PermissionMatrixRule rule)
    {
        return new PermissionMatrixRuleResponse
        {
            Id = rule.Id,
            RoleId = rule.RoleId,
            RoleName = rule.Role?.NameAr ?? "",
            RoleNameEn = rule.Role?.NameEn ?? "",
            Scope = rule.Scope,
            ScopeNameAr = GetScopeNameAr(rule.Scope),
            ScopeNameEn = rule.Scope.ToString(),
            ResourceType = rule.ResourceType,
            ResourceTypeNameAr = GetResourceTypeNameAr(rule.ResourceType),
            ResourceTypeNameEn = GetResourceTypeNameEn(rule.ResourceType),
            CommitteeRole = rule.CommitteeRole,
            CompetitionPhase = rule.CompetitionPhase,
            AllowedActions = rule.AllowedActions,
            IsCustomized = rule.IsCustomized,
            IsActive = rule.IsActive
        };
    }

    private static string GetScopeNameAr(ResourceScope scope) => scope switch
    {
        ResourceScope.Global => "عام",
        ResourceScope.Competition => "المنافسات",
        ResourceScope.Committee => "اللجان",
        _ => scope.ToString()
    };

    private static ResourceScope GetDefaultScope(ResourceType type) => type switch
    {
        >= ResourceType.Organization and <= ResourceType.Notifications => ResourceScope.Global,
        >= ResourceType.Competition and <= ResourceType.Attachments => ResourceScope.Competition,
        >= ResourceType.Committee and <= ResourceType.CommitteeTasks => ResourceScope.Committee,
        ResourceType.Tasks or ResourceType.ApprovalTasks => ResourceScope.Global,
        _ => ResourceScope.Global
    };

    private static string GetResourceTypeNameAr(ResourceType type) => type switch
    {
        ResourceType.Organization => "بيانات الجهة",
        ResourceType.Users => "المستخدمين",
        ResourceType.Roles => "الأدوار والصلاحيات",
        ResourceType.PermissionMatrix => "مصفوفة الصلاحيات",
        ResourceType.Dashboard => "لوحة التحكم",
        ResourceType.Reports => "التقارير",
        ResourceType.AiAssistant => "المساعد الذكي",
        ResourceType.AiConfiguration => "إعدادات الذكاء الاصطناعي",
        ResourceType.KnowledgeBase => "قاعدة المعرفة",
        ResourceType.WorkflowDefinitions => "مسارات الاعتماد",
        ResourceType.AuditLogs => "سجلات التدقيق",
        ResourceType.SystemSettings => "إعدادات النظام",
        ResourceType.Invitations => "الدعوات",
        ResourceType.Notifications => "الإشعارات",
        ResourceType.Competition => "المنافسة",
        ResourceType.Booklet => "كراسة الشروط",
        ResourceType.RfpSections => "أقسام طلب العرض",
        ResourceType.BoqItems => "جدول الكميات",
        ResourceType.EvaluationCriteria => "معايير التقييم",
        ResourceType.Offers => "العروض",
        ResourceType.TechnicalEvaluation => "التقييم الفني",
        ResourceType.FinancialEvaluation => "التقييم المالي",
        ResourceType.AwardRecommendation => "توصية الترسية",
        ResourceType.Contracts => "العقود",
        ResourceType.Inquiries => "الاستفسارات",
        ResourceType.Guarantees => "الضمانات",
        ResourceType.Grievances => "التظلمات",
        ResourceType.EvaluationMinutes => "محاضر التقييم",
        ResourceType.Attachments => "المرفقات",
        ResourceType.Committee => "اللجنة",
        ResourceType.CommitteeMembers => "أعضاء اللجنة",
        ResourceType.CommitteeMeetings => "اجتماعات اللجنة",
        ResourceType.CommitteeTasks => "مهام اللجنة",
        ResourceType.Tasks => "المهام",
        ResourceType.ApprovalTasks => "مهام الاعتماد",
        _ => type.ToString()
    };

    private static string GetResourceTypeNameEn(ResourceType type) => type switch
    {
        ResourceType.Organization => "Organization",
        ResourceType.Users => "Users",
        ResourceType.Roles => "Roles & Permissions",
        ResourceType.PermissionMatrix => "Permission Matrix",
        ResourceType.Dashboard => "Dashboard",
        ResourceType.Reports => "Reports",
        ResourceType.AiAssistant => "AI Assistant",
        ResourceType.AiConfiguration => "AI Configuration",
        ResourceType.KnowledgeBase => "Knowledge Base",
        ResourceType.WorkflowDefinitions => "Workflow Definitions",
        ResourceType.AuditLogs => "Audit Logs",
        ResourceType.SystemSettings => "System Settings",
        ResourceType.Invitations => "Invitations",
        ResourceType.Notifications => "Notifications",
        ResourceType.Competition => "Competition",
        ResourceType.Booklet => "Booklet",
        ResourceType.RfpSections => "RFP Sections",
        ResourceType.BoqItems => "Bill of Quantities",
        ResourceType.EvaluationCriteria => "Evaluation Criteria",
        ResourceType.Offers => "Offers",
        ResourceType.TechnicalEvaluation => "Technical Evaluation",
        ResourceType.FinancialEvaluation => "Financial Evaluation",
        ResourceType.AwardRecommendation => "Award Recommendation",
        ResourceType.Contracts => "Contracts",
        ResourceType.Inquiries => "Inquiries",
        ResourceType.Guarantees => "Guarantees",
        ResourceType.Grievances => "Grievances",
        ResourceType.EvaluationMinutes => "Evaluation Minutes",
        ResourceType.Attachments => "Attachments",
        ResourceType.Committee => "Committee",
        ResourceType.CommitteeMembers => "Committee Members",
        ResourceType.CommitteeMeetings => "Committee Meetings",
        ResourceType.CommitteeTasks => "Committee Tasks",
        ResourceType.Tasks => "Tasks",
        ResourceType.ApprovalTasks => "Approval Tasks",
        _ => type.ToString()
    };
}

// ═══════════════════════════════════════════════════════════════
//  Request/Response DTOs
// ═══════════════════════════════════════════════════════════════

public sealed class PermissionMatrixRuleResponse
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public string RoleNameEn { get; set; } = "";
    public ResourceScope Scope { get; set; }
    public string ScopeNameAr { get; set; } = "";
    public string ScopeNameEn { get; set; } = "";
    public ResourceType ResourceType { get; set; }
    public string ResourceTypeNameAr { get; set; } = "";
    public string ResourceTypeNameEn { get; set; } = "";
    public CommitteeRole? CommitteeRole { get; set; }
    public CompetitionPhase? CompetitionPhase { get; set; }
    public PermissionAction AllowedActions { get; set; }
    public bool IsCustomized { get; set; }
    public bool IsActive { get; set; }
}

public sealed record UpdateRuleRequest(PermissionAction AllowedActions);

public sealed record BulkUpdateRulesRequest(List<BulkRuleUpdate> Updates);

public sealed record BulkRuleUpdate(
    ResourceScope Scope,
    ResourceType ResourceType,
    CommitteeRole? CommitteeRole,
    CompetitionPhase? CompetitionPhase,
    PermissionAction AllowedActions);
