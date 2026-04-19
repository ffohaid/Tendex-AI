namespace TendexAI.Infrastructure.Authorization;

/// <summary>
/// Defines all permission-based authorization policy names used across the API.
/// Each policy maps to one or more permission claim codes stored in the JWT token.
/// Policy naming convention: "Permission:{Module}.{Action}"
/// </summary>
public static class PermissionPolicies
{
    // ═══════════════════════════════════════════════════════════════
    //  Users Module
    // ═══════════════════════════════════════════════════════════════
    public const string UsersView = "Permission:users.view";
    public const string UsersCreate = "Permission:users.create";
    public const string UsersEdit = "Permission:users.edit";
    public const string UsersDelete = "Permission:users.delete";
    public const string UsersManageRoles = "Permission:users.manage_roles";
    public const string UsersResetPassword = "Permission:users.reset_password";
    public const string UsersImpersonate = "Permission:users.impersonate";

    // ═══════════════════════════════════════════════════════════════
    //  Roles Module
    // ═══════════════════════════════════════════════════════════════
    public const string RolesView = "Permission:roles.view";
    public const string RolesCreate = "Permission:roles.create";
    public const string RolesEdit = "Permission:roles.edit";
    public const string RolesDelete = "Permission:roles.delete";

    // ═══════════════════════════════════════════════════════════════
    //  Competitions Module
    // ═══════════════════════════════════════════════════════════════
    public const string CompetitionsView = "Permission:competitions.view";
    public const string CompetitionsCreate = "Permission:competitions.create";
    public const string CompetitionsEdit = "Permission:competitions.edit";
    public const string CompetitionsDelete = "Permission:competitions.delete";
    public const string CompetitionsManage = "Permission:competitions.manage";
    public const string CompetitionsPublish = "Permission:competitions.publish";

    // ═══════════════════════════════════════════════════════════════
    //  RFP / Booklet Module
    // ═══════════════════════════════════════════════════════════════
    public const string RfpView = "Permission:rfp.view";
    public const string RfpCreate = "Permission:rfp.create";
    public const string RfpEdit = "Permission:rfp.edit";
    public const string RfpDelete = "Permission:rfp.delete";
    public const string RfpApprove = "Permission:rfp.approve";
    public const string RfpPublish = "Permission:rfp.publish";
    public const string RfpExport = "Permission:rfp.export";

    // ═══════════════════════════════════════════════════════════════
    //  Committees Module
    // ═══════════════════════════════════════════════════════════════
    public const string CommitteesView = "Permission:committees.view";
    public const string CommitteesCreate = "Permission:committees.create";
    public const string CommitteesEdit = "Permission:committees.edit";
    public const string CommitteesDelete = "Permission:committees.delete";
    public const string CommitteesManageMembers = "Permission:committees.manage_members";

    // ═══════════════════════════════════════════════════════════════
    //  Evaluation Module
    // ═══════════════════════════════════════════════════════════════
    public const string EvaluationView = "Permission:evaluation.view";
    public const string EvaluationTechnicalScore = "Permission:evaluation.technical_score";
    public const string EvaluationFinancialScore = "Permission:evaluation.financial_score";
    public const string EvaluationCreate = "Permission:evaluation.create";
    public const string EvaluationApprove = "Permission:evaluation.approve";

    // ═══════════════════════════════════════════════════════════════
    //  Offers Module
    // ═══════════════════════════════════════════════════════════════
    public const string OffersView = "Permission:offers.view";
    public const string OffersReview = "Permission:offers.review";
    public const string OffersOpen = "Permission:offers.open";

    // ═══════════════════════════════════════════════════════════════
    //  Inquiries Module
    // ═══════════════════════════════════════════════════════════════
    public const string InquiriesView = "Permission:inquiries.view";
    public const string InquiriesCreate = "Permission:inquiries.create";
    public const string InquiriesRespond = "Permission:inquiries.respond";
    public const string InquiriesManage = "Permission:inquiries.manage";

    // ═══════════════════════════════════════════════════════════════
    //  Dashboard Module
    // ═══════════════════════════════════════════════════════════════
    public const string DashboardView = "Permission:dashboard.view";
    public const string DashboardExport = "Permission:dashboard.export";

    // ═══════════════════════════════════════════════════════════════
    //  Reports Module
    // ═══════════════════════════════════════════════════════════════
    public const string ReportsView = "Permission:reports.view";
    public const string ReportsExport = "Permission:reports.export";

    // ═══════════════════════════════════════════════════════════════
    //  AI Module
    // ═══════════════════════════════════════════════════════════════
    public const string AiUseAssistant = "Permission:ai.use_assistant";
    public const string AiAssistantUse = "Permission:ai.use_assistant";  // Alias
    public const string AiConfigure = "Permission:ai.configure";
    public const string AiSettingsView = "Permission:ai.settings_view";
    public const string AiSettingsManage = "Permission:ai.settings_manage";

    // ═══════════════════════════════════════════════════════════════
    //  Knowledge Base Module
    // ═══════════════════════════════════════════════════════════════
    public const string KnowledgeView = "Permission:knowledge.view";
    public const string KnowledgeUpload = "Permission:knowledge.upload";
    public const string KnowledgeManage = "Permission:knowledge.manage";
    public const string KnowledgeBaseView = "Permission:knowledge.view";  // Alias
    public const string KnowledgeBaseManage = "Permission:knowledge.manage";  // Alias

    // ═══════════════════════════════════════════════════════════════
    //  Organization / Settings Module
    // ═══════════════════════════════════════════════════════════════
    public const string OrganizationView = "Permission:organization.view";
    public const string OrganizationEdit = "Permission:organization.edit";
    public const string OrganizationManage = "Permission:organization.manage";
    public const string SettingsView = "Permission:settings.view";
    public const string SettingsEdit = "Permission:settings.edit";
    public const string SettingsBranding = "Permission:settings.branding";
    public const string SettingsSmtp = "Permission:settings.smtp";

    // ═══════════════════════════════════════════════════════════════
    //  Permission Matrix Module
    // ═══════════════════════════════════════════════════════════════
    public const string MatrixView = "Permission:matrix.view";
    public const string MatrixEdit = "Permission:matrix.edit";

    // ═══════════════════════════════════════════════════════════════
    //  Workflow Module
    // ═════════════════════════════════════════════════════════════════
    public const string WorkflowView = "Permission:workflow.view";
    public const string WorkflowCreate = "Permission:workflow.create";
    public const string WorkflowEdit = "Permission:workflow.edit";
    public const string WorkflowDelete = "Permission:workflow.delete";
    public const string WorkflowManage = "Permission:workflow.manage";
    public const string WorkflowApprove = "Permission:workflow.approve";

    // ═══════════════════════════════════════════════════════════════
    //  Approvals Module
    // ═════════════════════════════════════════════════════════════════
    public const string ApprovalsView = "Permission:approvals.view";
    public const string ApprovalsCreate = "Permission:approvals.create";
    public const string ApprovalsApprove = "Permission:approvals.approve";
    public const string ApprovalsReject = "Permission:approvals.reject";

    // ═══════════════════════════════════════════════════════════════
    //  Audit Trail Module
    // ═══════════════════════════════════════════════════════════════
    public const string AuditView = "Permission:audit.view";
    public const string AuditExport = "Permission:audit.export";

    // ═══════════════════════════════════════════════════════════════
    //  Tasks Module
    // ═══════════════════════════════════════════════════════════════
    public const string TasksView = "Permission:tasks.view";

    // ═══════════════════════════════════════════════════════════════
    //  Files Module
    // ═══════════════════════════════════════════════════════════════
    public const string FilesUpload = "Permission:files.upload";
    public const string FilesView = "Permission:files.view";
    public const string FilesDelete = "Permission:files.delete";

    // ═══════════════════════════════════════════════════════════════
    //  Award Module
    // ═══════════════════════════════════════════════════════════════
    public const string AwardView = "Permission:award.view";
    public const string AwardManage = "Permission:award.manage";
    public const string AwardApprove = "Permission:award.approve";

    // ═══════════════════════════════════════════════════════════════
    //  Active Directory Module
    // ═══════════════════════════════════════════════════════════════
    public const string ActiveDirectoryManage = "Permission:active_directory.manage";

    // ═══════════════════════════════════════════════════════════
    //  Notifications Module
    // ═══════════════════════════════════════════════════════════
    public const string NotificationsView = "Permission:notifications.view";

    // ═══════════════════════════════════════════════════════════
    //  Support Tickets Module
    // ═══════════════════════════════════════════════════════════
    public const string SupportView = "Permission:support.view";
    public const string SupportCreate = "Permission:support.create";
    public const string SupportManage = "Permission:support.manage";

    // ═══════════════════════════════════════════════════════════
    //  Templates Module
    // ═══════════════════════════════════════════════════════════
    public const string TemplatesView = "Permission:templates.view";
    public const string TemplatesCreate = "Permission:templates.create";
    public const string TemplatesEdit = "Permission:templates.edit";
    public const string TemplatesDelete = "Permission:templates.delete";

    // ═══════════════════════════════════════════════════════════
    //  Tenants Module (Operator-level)
    // ═══════════════════════════════════════════════════════════
    public const string TenantsView = "Permission:tenants.view";
    public const string TenantsCreate = "Permission:tenants.create";
    public const string TenantsEdit = "Permission:tenants.edit";

    // ═══════════════════════════════════════════════════════════
    //  Profile Module (self-service)
    // ═══════════════════════════════════════════════════════════
    // Profile endpoints are self-service - any authenticated user can access their own profile
    // No specific permission needed beyond authentication

    // ═══════════════════════════════════════════════════════════
    //  Feature Flags Module (Operator-level)
    // ═══════════════════════════════════════════════════════════
    public const string FeatureFlagsManage = "Permission:feature_flags.manage";

    // ═══════════════════════════════════════════════════════════
    //  Impersonation Module
    // ═══════════════════════════════════════════════════════════
    public const string Impersonate = "Permission:users.impersonate";

    // ═══════════════════════════════════════════════════════════
    //  Evaluation Minutes Module
    // ═══════════════════════════════════════════════════════════
    public const string MinutesView = "Permission:minutes.view";
    public const string MinutesCreate = "Permission:minutes.create";
    public const string MinutesSign = "Permission:minutes.sign";
}
