import type { NavigationItem } from '@/types/navigation'

/**
 * Main sidebar navigation items.
 *
 * Reorganized for clarity and to match the reference RFP.AI design:
 * - Clean, flat structure where possible
 * - Grouped logically by function
 * - Operator section separated with role-based visibility
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
    children: [
      {
        key: 'rfp-list',
        labelKey: 'nav.rfp.list',
        icon: 'pi pi-list',
        route: 'rfp-list',
      },
      {
        key: 'rfp-create',
        labelKey: 'nav.rfp.create',
        icon: 'pi pi-plus-circle',
        route: 'rfp-method-selection',
      },
      {
        key: 'rfp-templates',
        labelKey: 'nav.rfpTemplates',
        icon: 'pi pi-copy',
        route: 'RfpTemplates',
      },
      {
        key: 'booklet-templates',
        labelKey: 'nav.bookletTemplates',
        icon: 'pi pi-file-word',
        route: 'BookletTemplates',
      },
    ],
  },
  {
    key: 'committees',
    labelKey: 'nav.committees.title',
    icon: 'pi pi-users',
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
  {
    key: 'evaluation',
    labelKey: 'nav.evaluation.title',
    icon: 'pi pi-chart-bar',
    children: [
      {
        key: 'evaluation-technical',
        labelKey: 'nav.evaluation.technical',
        icon: 'pi pi-cog',
        route: 'EvaluationTechnical',
      },
      {
        key: 'evaluation-financial',
        labelKey: 'nav.evaluation.financial',
        icon: 'pi pi-wallet',
        route: 'EvaluationFinancial',
      },
    ],
  },
  {
    key: 'knowledge-base',
    labelKey: 'nav.knowledgeBase',
    icon: 'pi pi-book',
    route: 'KnowledgeBase',
  },
  {
    key: 'ai-assistant',
    labelKey: 'nav.aiAssistant',
    icon: 'pi pi-sparkles',
    route: 'AiAssistant',
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
        key: 'settings-workflows',
        labelKey: 'nav.settings.workflows',
        icon: 'pi pi-sitemap',
        route: 'WorkflowList',
      },
    ],
  },
  /* ── Advanced features (accessible from sidebar) ── */
  {
    key: 'task-center',
    labelKey: 'nav.taskCenter',
    icon: 'pi pi-check-square',
    route: 'TaskCenter',
  },
  {
    key: 'inquiries',
    labelKey: 'nav.inquiries',
    icon: 'pi pi-comments',
    route: 'Inquiries',
  },
  {
    key: 'reports',
    labelKey: 'nav.reports',
    icon: 'pi pi-chart-line',
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
