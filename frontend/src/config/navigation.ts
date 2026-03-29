import type { NavigationItem } from '@/types/navigation'

/**
 * Main sidebar navigation items.
 *
 * Each item uses i18n keys for labels, PrimeIcons for icons,
 * and Vue Router route names for navigation targets.
 *
 * The structure supports nested children for sub-menus.
 * Permission keys are included for future RBAC integration.
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
        route: 'rfp-create',
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
    key: 'approvals',
    labelKey: 'nav.approvals',
    icon: 'pi pi-check-circle',
    route: 'Approvals',
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
    route: 'Reports',
  },
  {
    key: 'ai-assistant',
    labelKey: 'nav.aiAssistant',
    icon: 'pi pi-sparkles',
    route: 'AiAssistant',
  },
  {
    key: 'operator',
    labelKey: 'nav.operator.title',
    icon: 'pi pi-shield',
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
    ],
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
    ],
  },
  {
    key: 'operator-advanced',
    labelKey: 'nav.operator.title',
    icon: 'pi pi-shield',
    requiredRoles: ['SuperAdmin', 'SupportAdmin'],
    children: [
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
