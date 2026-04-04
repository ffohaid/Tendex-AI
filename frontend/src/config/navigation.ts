import type { NavigationItem } from '@/types/navigation'

/**
 * Main sidebar navigation items.
 *
 * Governance Role Hierarchy (immutable, cannot be deleted or modified):
 * ─────────────────────────────────────────────────────────────────────
 * 1. OperatorPrimaryAdmin  → Operator panel only (create/manage tenants)
 * 2. TenantPrimaryAdmin    → Full tenant admin (users, roles, permissions,
 *                            committees, workflows, knowledge base, reports,
 *                            organization settings, dashboard)
 *
 * All other roles are flexible and controlled by the permission matrix
 * configured per-tenant by the TenantPrimaryAdmin.
 *
 * Rules:
 * - `requiredRoles`: array of role NameEn values; item hidden if user has none.
 * - `permission`: permission code; item hidden if user lacks this permission.
 * - TenantPrimaryAdmin bypasses all permission checks within their tenant.
 * - OperatorPrimaryAdmin sees ONLY the operator panel, nothing else.
 *
 * IMPORTANT: Route names MUST exactly match the `name` property
 * defined in `@/router/index.ts`. Any mismatch will break navigation.
 *
 * Menu Order Rationale (optimized for government procurement workflow):
 * ────────────────────────────────────────────────────────────────────
 * 1. Information Dashboard – overview & KPIs (first thing users see)
 * 2. Task Center – daily tasks & pending approvals (most used daily)
 * 3. RFP / Specifications Books – core workflow entry point
 * 4. Committees – committee management tied to competitions
 * 5. Evaluation – technical & financial evaluation of offers
 * 6. Inquiries – supplier questions management
 * 7. AI Assistant – AI-powered assistance across modules
 * 8. Knowledge Base – reference documents & regulations
 * 9. Reports – analytics & export (periodic use)
 * 10. Support – help & tickets (occasional use)
 * 11. Settings – admin configuration (least frequent)
 */
export const sidebarNavigation: NavigationItem[] = [
  /* ── 1. Information Dashboard (visible to all authenticated tenant users) ── */
  {
    key: 'dashboard',
    labelKey: 'nav.dashboard',
    icon: 'pi pi-home',
    route: 'Dashboard',
  },

  /* ── 2. Task Center (most used daily – pending approvals & tasks) ── */
  {
    key: 'task-center',
    labelKey: 'nav.taskCenter',
    icon: 'pi pi-check-square',
    route: 'TaskCenter',
    permission: 'tasks.view',
  },

  /* ── 3. RFP / Specifications Books ── */
  {
    key: 'rfp',
    labelKey: 'nav.rfp.title',
    icon: 'pi pi-file-edit',
    permission: 'rfp.view',
    children: [
      {
        key: 'rfp-list',
        labelKey: 'nav.rfp.list',
        icon: 'pi pi-list',
        route: 'rfp-list',
        permission: 'rfp.view',
      },
      {
        key: 'rfp-create',
        labelKey: 'nav.rfp.create',
        icon: 'pi pi-plus-circle',
        route: 'rfp-method-selection',
        permission: 'rfp.create',
      },
      {
        key: 'rfp-templates',
        labelKey: 'nav.rfpTemplates',
        icon: 'pi pi-copy',
        route: 'RfpTemplates',
        permission: 'templates.view',
      },
      {
        key: 'booklet-templates',
        labelKey: 'nav.bookletTemplates',
        icon: 'pi pi-file-word',
        route: 'BookletTemplates',
        permission: 'templates.view',
      },
    ],
  },

  /* ── 4. Committees (permission-based, not role-locked) ── */
  {
    key: 'committees',
    labelKey: 'nav.committees.title',
    icon: 'pi pi-users',
    permission: 'committees.view',
    children: [
      {
        key: 'committees-permanent',
        labelKey: 'nav.committees.permanent',
        icon: 'pi pi-shield',
        route: 'CommitteesPermanent',
      },
      {
        key: 'committees-temporary',
        labelKey: 'nav.committees.temporary',
        icon: 'pi pi-clock',
        route: 'CommitteesTemporary',
      },
    ],
  },

  /* ── 5. Evaluation ── */
  {
    key: 'evaluation',
    labelKey: 'nav.evaluation.title',
    icon: 'pi pi-chart-bar',
    permission: 'evaluation.view',
    children: [
      {
        key: 'evaluation-offers',
        labelKey: 'nav.evaluation.offers',
        icon: 'pi pi-file-edit',
        route: 'SupplierOffers',
        permission: 'offers.view',
      },
      {
        key: 'evaluation-technical',
        labelKey: 'nav.evaluation.technical',
        icon: 'pi pi-cog',
        route: 'EvaluationTechnical',
        permission: 'evaluation.technical',
      },
      {
        key: 'evaluation-financial',
        labelKey: 'nav.evaluation.financial',
        icon: 'pi pi-wallet',
        route: 'EvaluationFinancial',
        permission: 'evaluation.financial',
      },
      {
        key: 'evaluation-comprehensive',
        labelKey: 'nav.evaluation.comprehensive',
        icon: 'pi pi-chart-pie',
        route: 'ComprehensiveEvaluation',
        permission: 'evaluation.view',
      },
    ],
  },

  /* ── 6. Inquiries (supplier questions – tied to active competitions) ── */
  {
    key: 'inquiries',
    labelKey: 'nav.inquiries',
    icon: 'pi pi-comments',
    route: 'Inquiries',
    permission: 'inquiries.view',
  },

  /* ── 7. AI Assistant ── */
  {
    key: 'ai-assistant',
    labelKey: 'nav.aiAssistant',
    icon: 'pi pi-sparkles',
    route: 'AiAssistant',
    permission: 'ai.view',
  },

  /* ── 8. Knowledge Base (permission-based) ── */
  {
    key: 'knowledge-base',
    labelKey: 'nav.knowledgeBase',
    icon: 'pi pi-book',
    route: 'KnowledgeBase',
    permission: 'knowledge.view',
  },

  /* ── 9. Reports (permission-based) ── */
  {
    key: 'reports',
    labelKey: 'nav.reports',
    icon: 'pi pi-chart-line',
    permission: 'reports.view',
    children: [
      {
        key: 'reports-analytics',
        labelKey: 'nav.reports',
        icon: 'pi pi-chart-bar',
        route: 'Reports',
      },
      {
        key: 'reports-export',
        labelKey: 'nav.reportGenerator',
        icon: 'pi pi-download',
        route: 'ReportGenerator',
      },
    ],
  },

  /* ── 10. Support Tickets ── */
  {
    key: 'support',
    labelKey: 'nav.support',
    icon: 'pi pi-ticket',
    route: 'SupportTickets',
    permission: 'support.view',
  },

  /* ── 11. Settings (admin – least frequent, always last) ── */
  {
    key: 'settings',
    labelKey: 'nav.settings.title',
    icon: 'pi pi-cog',
    requiredRoles: ['TenantPrimaryAdmin'],
    children: [
      {
        key: 'settings-organization',
        labelKey: 'nav.settings.organization',
        icon: 'pi pi-building',
        route: 'SettingsOrganization',
      },
      {
        key: 'settings-users',
        labelKey: 'nav.settings.users',
        icon: 'pi pi-user',
        route: 'SettingsUsers',
      },
      {
        key: 'settings-roles',
        labelKey: 'nav.settings.roles',
        icon: 'pi pi-key',
        route: 'SettingsRoles',
      },
      {
        key: 'settings-permissions-matrix',
        labelKey: 'nav.settings.permissionsMatrix',
        icon: 'pi pi-th-large',
        route: 'PermissionsMatrix',
      },
      {
        key: 'settings-workflows',
        labelKey: 'nav.settings.workflows',
        icon: 'pi pi-sitemap',
        route: 'WorkflowList',
      },
      {
        key: 'settings-ai',
        labelKey: 'nav.aiSettings',
        icon: 'pi pi-sparkles',
        route: 'TenantAiSettings',
      },
      {
        key: 'settings-active-directory',
        labelKey: 'activeDirectory.title',
        icon: 'pi pi-microsoft',
        route: 'ActiveDirectorySettings',
      },
    ],
  },

  /* ── Operator Portal (OperatorPrimaryAdmin ONLY) ── */
  {
    key: 'operator',
    labelKey: 'nav.operator.title',
    icon: 'pi pi-shield',
    requiredRoles: ['OperatorPrimaryAdmin'],
    children: [
      {
        key: 'operator-dashboard',
        labelKey: 'nav.operator.dashboard',
        icon: 'pi pi-chart-pie',
        route: 'OperatorDashboard',
      },
      {
        key: 'operator-tenants',
        labelKey: 'nav.operator.tenants',
        icon: 'pi pi-building',
        route: 'TenantList',
      },
      {
        key: 'operator-purchase-orders',
        labelKey: 'nav.operator.purchaseOrders',
        icon: 'pi pi-file',
        route: 'PurchaseOrderList',
      },
      {
        key: 'operator-ai-settings',
        labelKey: 'operator.aiSettings',
        icon: 'pi pi-sparkles',
        route: 'OperatorAiSettings',
      },
      {
        key: 'operator-system-health',
        labelKey: 'operator.systemHealth',
        icon: 'pi pi-server',
        route: 'OperatorSystemHealth',
      },
      {
        key: 'operator-consumption',
        labelKey: 'operator.consumption',
        icon: 'pi pi-chart-bar',
        route: 'OperatorConsumption',
      },
      {
        key: 'operator-subscriptions',
        labelKey: 'operator.subscriptions',
        icon: 'pi pi-credit-card',
        route: 'OperatorSubscriptions',
      },
      {
        key: 'operator-support',
        labelKey: 'nav.operator.support',
        icon: 'pi pi-ticket',
        route: 'OperatorSupportTickets',
      },
      {
        key: 'operator-audit-log',
        labelKey: 'nav.operator.auditLog',
        icon: 'pi pi-history',
        route: 'OperatorAuditLog',
      },
      {
        key: 'operator-impersonation',
        labelKey: 'nav.operator.impersonation',
        icon: 'pi pi-user-edit',
        route: 'OperatorImpersonation',
      },
    ],
  },
]
