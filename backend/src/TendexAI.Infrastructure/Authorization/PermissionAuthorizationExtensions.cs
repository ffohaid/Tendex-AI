using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace TendexAI.Infrastructure.Authorization;

/// <summary>
/// Extension methods for registering permission-based authorization policies.
/// All policies follow the naming convention "Permission:{code}" where {code}
/// is the permission claim value (e.g., "users.view", "rfp.create").
/// </summary>
public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers all permission-based authorization policies and the
    /// PermissionClaimRequirementHandler in the DI container.
    /// </summary>
    public static IServiceCollection AddPermissionPolicies(
        this IServiceCollection services)
    {
        // Register the claim-based handler
        services.AddScoped<IAuthorizationHandler, PermissionClaimRequirementHandler>();

        services.AddAuthorizationBuilder()
            // ─── Users ───
            .AddPermissionPolicy(PermissionPolicies.UsersView, "users.view")
            .AddPermissionPolicy(PermissionPolicies.UsersCreate, "users.create")
            .AddPermissionPolicy(PermissionPolicies.UsersEdit, "users.edit")
            .AddPermissionPolicy(PermissionPolicies.UsersDelete, "users.delete")
            .AddPermissionPolicy(PermissionPolicies.UsersManageRoles, "users.manage_roles")
            .AddPermissionPolicy(PermissionPolicies.UsersImpersonate, "users.impersonate")
            .AddPermissionPolicy(PermissionPolicies.UsersResetPassword, "users.reset_password")

            // ─── Roles ───
            .AddPermissionPolicy(PermissionPolicies.RolesView, "roles.view")
            .AddPermissionPolicy(PermissionPolicies.RolesCreate, "roles.create")
            .AddPermissionPolicy(PermissionPolicies.RolesEdit, "roles.edit")
            .AddPermissionPolicy(PermissionPolicies.RolesDelete, "roles.delete")

            // ─── Competitions ───
            .AddPermissionPolicy(PermissionPolicies.CompetitionsView, "competitions.view")
            .AddPermissionPolicy(PermissionPolicies.CompetitionsCreate, "competitions.create")
            .AddPermissionPolicy(PermissionPolicies.CompetitionsEdit, "competitions.edit")
            .AddPermissionPolicy(PermissionPolicies.CompetitionsDelete, "competitions.delete")
            .AddPermissionPolicy(PermissionPolicies.CompetitionsManage, "competitions.manage")
            .AddPermissionPolicy(PermissionPolicies.CompetitionsPublish, "competitions.publish")

            // ─── RFP / Booklet ───
            .AddPermissionPolicy(PermissionPolicies.RfpView, "rfp.view")
            .AddPermissionPolicy(PermissionPolicies.RfpCreate, "rfp.create")
            .AddPermissionPolicy(PermissionPolicies.RfpEdit, "rfp.edit")
            .AddPermissionPolicy(PermissionPolicies.RfpDelete, "rfp.delete")
            .AddPermissionPolicy(PermissionPolicies.RfpApprove, "rfp.approve")
            .AddPermissionPolicy(PermissionPolicies.RfpPublish, "rfp.publish")
            .AddPermissionPolicy(PermissionPolicies.RfpExport, "rfp.export")

            // ─── Committees ───
            .AddPermissionPolicy(PermissionPolicies.CommitteesView, "committees.view")
            .AddPermissionPolicy(PermissionPolicies.CommitteesCreate, "committees.create")
            .AddPermissionPolicy(PermissionPolicies.CommitteesEdit, "committees.edit")
            .AddPermissionPolicy(PermissionPolicies.CommitteesDelete, "committees.delete")
            .AddPermissionPolicy(PermissionPolicies.CommitteesManageMembers, "committees.manage_members")

            // ─── Evaluation ───
            .AddPermissionPolicy(PermissionPolicies.EvaluationView, "evaluation.view")
            .AddPermissionPolicy(PermissionPolicies.EvaluationTechnicalScore, "evaluation.technical_score")
            .AddPermissionPolicy(PermissionPolicies.EvaluationFinancialScore, "evaluation.financial_score")
            .AddPermissionPolicy(PermissionPolicies.EvaluationCreate, "evaluation.create")
            .AddPermissionPolicy(PermissionPolicies.EvaluationApprove, "evaluation.approve")

            // ─── Offers ───
            .AddPermissionPolicy(PermissionPolicies.OffersView, "offers.view")
            .AddPermissionPolicy(PermissionPolicies.OffersReview, "offers.review")
            .AddPermissionPolicy(PermissionPolicies.OffersOpen, "offers.open")

            // ─── Inquiries ───
            .AddPermissionPolicy(PermissionPolicies.InquiriesView, "inquiries.view")
            .AddPermissionPolicy(PermissionPolicies.InquiriesCreate, "inquiries.create")
            .AddPermissionPolicy(PermissionPolicies.InquiriesRespond, "inquiries.respond")
            .AddPermissionPolicy(PermissionPolicies.InquiriesManage, "inquiries.manage")

            // ─── Dashboard ───
            .AddPermissionPolicy(PermissionPolicies.DashboardView, "dashboard.view")
            .AddPermissionPolicy(PermissionPolicies.DashboardExport, "dashboard.export")

            // ─── Reports ───
            .AddPermissionPolicy(PermissionPolicies.ReportsView, "reports.view")
            .AddPermissionPolicy(PermissionPolicies.ReportsExport, "reports.export")

            // ─── AI ───
            .AddPermissionPolicy(PermissionPolicies.AiUseAssistant, "ai.use_assistant")
            .AddPermissionPolicy(PermissionPolicies.AiConfigure, "ai.configure")
            .AddPermissionPolicy(PermissionPolicies.AiSettingsView, "ai.settings_view")
            .AddPermissionPolicy(PermissionPolicies.AiSettingsManage, "ai.settings_manage")

            // ─── Knowledge Base ───
            .AddPermissionPolicy(PermissionPolicies.KnowledgeView, "knowledge.view")
            .AddPermissionPolicy(PermissionPolicies.KnowledgeUpload, "knowledge.upload")
            .AddPermissionPolicy(PermissionPolicies.KnowledgeManage, "knowledge.manage")

            // ─── Organization / Settings ───
            .AddPermissionPolicy(PermissionPolicies.OrganizationView, "organization.view")
            .AddPermissionPolicy(PermissionPolicies.OrganizationEdit, "organization.edit")
            .AddPermissionPolicy(PermissionPolicies.OrganizationManage, "organization.manage")
            .AddPermissionPolicy(PermissionPolicies.SettingsView, "settings.view")
            .AddPermissionPolicy(PermissionPolicies.SettingsEdit, "settings.edit")
            .AddPermissionPolicy(PermissionPolicies.SettingsBranding, "settings.branding")
            .AddPermissionPolicy(PermissionPolicies.SettingsSmtp, "settings.smtp")

            // ─── Permission Matrix ───
            .AddPermissionPolicy(PermissionPolicies.MatrixView, "matrix.view")
            .AddPermissionPolicy(PermissionPolicies.MatrixEdit, "matrix.edit")

            // ─── Workflow ───
            .AddPermissionPolicy(PermissionPolicies.WorkflowView, "workflow.view")
            .AddPermissionPolicy(PermissionPolicies.WorkflowCreate, "workflow.create")
            .AddPermissionPolicy(PermissionPolicies.WorkflowEdit, "workflow.edit")
            .AddPermissionPolicy(PermissionPolicies.WorkflowDelete, "workflow.delete")
            .AddPermissionPolicy(PermissionPolicies.WorkflowManage, "workflow.manage")
            .AddPermissionPolicy(PermissionPolicies.WorkflowApprove, "workflow.approve")

            // ─── Approvals ───
            .AddPermissionPolicy(PermissionPolicies.ApprovalsView, "approvals.view")
            .AddPermissionPolicy(PermissionPolicies.ApprovalsCreate, "approvals.create")
            .AddPermissionPolicy(PermissionPolicies.ApprovalsApprove, "approvals.approve")
            .AddPermissionPolicy(PermissionPolicies.ApprovalsReject, "approvals.reject")

            // ─── Audit Trail ───
            .AddPermissionPolicy(PermissionPolicies.AuditView, "audit.view")
            .AddPermissionPolicy(PermissionPolicies.AuditExport, "audit.export")

            // ─── Tasks ───
            .AddPermissionPolicy(PermissionPolicies.TasksView, "tasks.view")

            // ─── Files ───
            .AddPermissionPolicy(PermissionPolicies.FilesUpload, "files.upload")
            .AddPermissionPolicy(PermissionPolicies.FilesView, "files.view")
            .AddPermissionPolicy(PermissionPolicies.FilesDelete, "files.delete")

            // ─── Award ───
            .AddPermissionPolicy(PermissionPolicies.AwardView, "award.view")
            .AddPermissionPolicy(PermissionPolicies.AwardManage, "award.manage")
            .AddPermissionPolicy(PermissionPolicies.AwardApprove, "award.approve")

            // ─── Active Directory ───
            .AddPermissionPolicy(PermissionPolicies.ActiveDirectoryManage, "active_directory.manage")

            // ─── Notifications ───
            .AddPermissionPolicy(PermissionPolicies.NotificationsView, "notifications.view")

            // ─── Support Tickets ───
            .AddPermissionPolicy(PermissionPolicies.SupportView, "support.view")
            .AddPermissionPolicy(PermissionPolicies.SupportCreate, "support.create")
            .AddPermissionPolicy(PermissionPolicies.SupportManage, "support.manage")

            // ─── Templates ───
            .AddPermissionPolicy(PermissionPolicies.TemplatesView, "templates.view")
            .AddPermissionPolicy(PermissionPolicies.TemplatesCreate, "templates.create")
            .AddPermissionPolicy(PermissionPolicies.TemplatesEdit, "templates.edit")
            .AddPermissionPolicy(PermissionPolicies.TemplatesDelete, "templates.delete")

            // ─── Tenants (Operator) ───
            .AddPermissionPolicy(PermissionPolicies.TenantsView, "tenants.view")
            .AddPermissionPolicy(PermissionPolicies.TenantsCreate, "tenants.create")
            .AddPermissionPolicy(PermissionPolicies.TenantsEdit, "tenants.edit")

            // ─── Feature Flags (Operator) ───
            .AddPermissionPolicy(PermissionPolicies.FeatureFlagsManage, "feature_flags.manage")


            // ─── Evaluation Minutes ───
            .AddPermissionPolicy(PermissionPolicies.MinutesView, "minutes.view")
            .AddPermissionPolicy(PermissionPolicies.MinutesCreate, "minutes.create")
            .AddPermissionPolicy(PermissionPolicies.MinutesSign, "minutes.sign");

        return services;
    }

    /// <summary>
    /// Helper method to add a permission policy with the given name and required permission codes.
    /// </summary>
    private static AuthorizationBuilder AddPermissionPolicy(
        this AuthorizationBuilder builder,
        string policyName,
        params string[] permissionCodes)
    {
        builder.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionClaimRequirement(permissionCodes));
        });

        return builder;
    }
}
