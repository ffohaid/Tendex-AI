import type { NavigationItem } from '@/types/navigation'

/**
 * Main sidebar navigation items.
 *
 * Each item can have:
 * - `permission`: a permission code string; the item is hidden if the user lacks this permission.
 * - `requiredRoles`: an array of role names; the item is hidden if the user has none of these roles.
 * - Owner and Admin users bypass all permission/role checks.
 *
 * IMPORTANT: Route names MUST exactly match the `name` property
 * defined in `@/router/index.ts`. Any mismatch will break navigation.
 */
export const sidebarNavigation: NavigationItem[] = [
  {
    key: 'dashboard',
    labelKey: 'nav.dashboard',
    icon: 'pi pi-home',
    route: 'Dashboard',
  },
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
        permission: 'committees.view',
      },
      {
        key: 'committees-temporary',
        labelKey: 'nav.committees.temporary',
        icon: 'pi pi-clock',
        route: 'CommitteesTemporary',
        permission: 'committees.view',
      },
    ],
  },
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
  {
    key: 'knowledge-base',
    labelKey: 'nav.knowledgeBase',
    icon: 'pi pi-book',
    route: 'KnowledgeBase',
    permission: 'knowledgebase.view',
  },
  {
    key: 'ai-assistant',
    labelKey: 'nav.aiAssistant',
    icon: 'pi pi-sparkles',
    route: 'AiAssistant',
    permission: 'ai.view',
  },
  {
    key: 'settings',
    labelKey: 'nav.settings.title',
    icon: 'pi pi-cog',
    children: [
      {
        key: 'settings-organization',
        labelKey: 'nav.settings.organization',
        icon: 'pi pi-building',
        route: 'SettingsOrganization',
        permission: 'organization.view',
      },
      {
        key: 'settings-users',
        labelKey: 'nav.settings.users',
        icon: 'pi pi-user',
        route: 'SettingsUsers',
        permission: 'users.view',
      },
      {
        key: 'settings-roles',
        labelKey: 'nav.settings.roles',
        icon: 'pi pi-key',
        route: 'SettingsRoles',
        permission: 'roles.view',
      },
      {
        key: 'settings-permissions-matrix',
        labelKey: 'nav.settings.permissionsMatrix',
        icon: 'pi pi-th-large',
        route: 'PermissionsMatrix',
        permission: 'matrix.view',
      },
      {
        key: 'settings-workflows',
        labelKey: 'nav.settings.workflows',
        icon: 'pi pi-sitemap',
        route: 'WorkflowList',
        permission: 'workflow.view',
      },
    ],
  },
  /* ── Advanced features (accessible from sidebar) ── */
  {
    key: 'task-center',
    labelKey: 'nav.taskCenter',
    icon: 'pi pi-check-square',
    route: 'TaskCenter',
    permission: 'tasks.view',
  },
  {
    key: 'inquiries',
    labelKey: 'nav.inquiries',
    icon: 'pi pi-comments',
    route: 'Inquiries',
    permission: 'inquiries.view',
  },
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
        permission: 'reports.view',
      },
      {
        key: 'reports-export',
        labelKey: 'nav.reportGenerator',
        icon: 'pi pi-download',
        route: 'ReportGenerator',
        permission: 'reports.export',
      },
    ],
  },
  /* ── Operator Portal (SuperAdmin / Operator only) ── */
  {
    key: 'operator',
    labelKey: 'nav.operator.title',
    icon: 'pi pi-shield',
    requiredRoles: ['SuperAdmin', 'Operator'],
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
        key: 'operator-impersonation',
        labelKey: 'nav.operator.impersonation',
        icon: 'pi pi-user-edit',
        route: 'OperatorImpersonation',
        requiredRoles: ['SuperAdmin', 'SupportAdmin'],
      },
    ],
  },
]
