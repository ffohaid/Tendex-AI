namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the system-level roles for the platform governance model.
/// 
/// GOVERNANCE HIERARCHY:
///   Tier 1 — OperatorSuperAdmin: Platform-wide operator (immutable, cross-tenant)
///   Tier 2 — TenantPrimaryAdmin: Highest authority within a single tenant (immutable)
///   Tier 3 — All other roles: Flexible, per-tenant, managed via Permission Matrix
/// 
/// Tier 1 and Tier 2 roles are PROTECTED and cannot be edited, deleted, or reassigned
/// except by a user of equal or higher tier.
/// </summary>
public enum SystemRole
{
    /// <summary>
    /// OperatorSuperAdmin: The first responsible person for the operating company.
    /// Platform-wide scope. Can create/manage tenants and assign TenantPrimaryAdmin.
    /// Only role that can access the Operator Panel.
    /// IMMUTABLE — cannot be modified or deleted.
    /// </summary>
    OperatorSuperAdmin = 0,

    /// <summary>
    /// TenantPrimaryAdmin: The first responsible person within a government entity.
    /// Tenant-scoped. Manages users, roles, permissions, committees, workflows,
    /// knowledge base, reports, and settings for their tenant ONLY.
    /// IMMUTABLE — cannot be modified or deleted.
    /// </summary>
    TenantPrimaryAdmin = 1,

    /// <summary>
    /// ProcurementManager: Manages procurement processes within the tenant.
    /// Flexible role — permissions controlled via Permission Matrix.
    /// </summary>
    ProcurementManager = 2,

    /// <summary>
    /// FinancialController: Financial oversight and approval.
    /// Flexible role — permissions vary per entity and competition phase.
    /// </summary>
    FinancialController = 3,

    /// <summary>
    /// SectorRepresentative: Represents a specific sector within the government entity.
    /// Flexible role — manages tasks and users from their sector.
    /// </summary>
    SectorRepresentative = 4,

    /// <summary>
    /// CommitteeChair: Leads committee activities.
    /// Flexible role — permissions controlled via Permission Matrix + Committee Role.
    /// </summary>
    CommitteeChair = 5,

    /// <summary>
    /// CommitteeMember: Participates in committee work.
    /// Flexible role — permissions controlled via Permission Matrix + Committee Role.
    /// </summary>
    CommitteeMember = 6,

    /// <summary>
    /// Member: General operational member.
    /// Flexible role — creates/edits competitions, uploads offers, uses AI assistant.
    /// </summary>
    Member = 7,

    /// <summary>
    /// Viewer: Read-only access to assigned competitions and reports.
    /// Flexible role — cannot modify or take any action.
    /// </summary>
    Viewer = 8
}

/// <summary>
/// Extension methods for SystemRole governance checks.
/// </summary>
public static class SystemRoleExtensions
{
    /// <summary>
    /// Returns true if this role is a protected/immutable governance role (Tier 1 or Tier 2).
    /// Protected roles cannot be edited, deleted, or have their permissions modified.
    /// </summary>
    public static bool IsProtected(this SystemRole role)
        => role is SystemRole.OperatorSuperAdmin or SystemRole.TenantPrimaryAdmin;

    /// <summary>
    /// Returns true if this role is an operator-level role (Tier 1).
    /// Operator roles have platform-wide (cross-tenant) scope.
    /// </summary>
    public static bool IsOperatorLevel(this SystemRole role)
        => role is SystemRole.OperatorSuperAdmin;

    /// <summary>
    /// Returns true if this role is a tenant admin role (Tier 2).
    /// Tenant admin roles have full authority within a single tenant.
    /// </summary>
    public static bool IsTenantAdminLevel(this SystemRole role)
        => role is SystemRole.TenantPrimaryAdmin;

    /// <summary>
    /// Returns true if this role is a flexible role (Tier 3).
    /// Flexible roles are per-tenant and managed via the Permission Matrix.
    /// </summary>
    public static bool IsFlexible(this SystemRole role)
        => !role.IsProtected();

    /// <summary>
    /// Gets the Arabic display name for the role.
    /// </summary>
    public static string GetArabicName(this SystemRole role) => role switch
    {
        SystemRole.OperatorSuperAdmin => "المسؤول الأول",
        SystemRole.TenantPrimaryAdmin => "المسؤول الأول بالجهة",
        SystemRole.ProcurementManager => "مدير المشتريات",
        SystemRole.FinancialController => "المراقب المالي",
        SystemRole.SectorRepresentative => "ممثل القطاع",
        SystemRole.CommitteeChair => "رئيس اللجنة",
        SystemRole.CommitteeMember => "عضو اللجنة",
        SystemRole.Member => "عضو",
        SystemRole.Viewer => "مستعرض",
        _ => role.ToString()
    };

    /// <summary>
    /// Gets the English display name for the role.
    /// </summary>
    public static string GetEnglishName(this SystemRole role) => role switch
    {
        SystemRole.OperatorSuperAdmin => "Operator Super Admin",
        SystemRole.TenantPrimaryAdmin => "Tenant Primary Admin",
        SystemRole.ProcurementManager => "Procurement Manager",
        SystemRole.FinancialController => "Financial Controller",
        SystemRole.SectorRepresentative => "Sector Representative",
        SystemRole.CommitteeChair => "Committee Chair",
        SystemRole.CommitteeMember => "Committee Member",
        SystemRole.Member => "Member",
        SystemRole.Viewer => "Viewer",
        _ => role.ToString()
    };

    /// <summary>
    /// Gets the role key used for programmatic identification.
    /// </summary>
    public static string GetRoleKey(this SystemRole role) => role switch
    {
        SystemRole.OperatorSuperAdmin => "operator_super_admin",
        SystemRole.TenantPrimaryAdmin => "tenant_primary_admin",
        SystemRole.ProcurementManager => "procurement_manager",
        SystemRole.FinancialController => "financial_controller",
        SystemRole.SectorRepresentative => "sector_representative",
        SystemRole.CommitteeChair => "committee_chair",
        SystemRole.CommitteeMember => "committee_member",
        SystemRole.Member => "member",
        SystemRole.Viewer => "viewer",
        _ => role.ToString().ToLowerInvariant()
    };
}
