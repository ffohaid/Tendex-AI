import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

/**
 * Application route definitions.
 *
 * Routes are organized by layout:
 * - MainLayout: authenticated pages with sidebar/header/footer
 * - AuthLayout: authentication pages (login, MFA, password reset)
 *
 * Lazy-loaded components for optimal bundle splitting.
 */
const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Dashboard', requiresAuth: true },
      },
      /* RFP Specification Booklet routes (TASK-504) */
      {
        path: 'rfp',
        name: 'rfp-list',
        component: () => import('@/views/rfp/RfpListView.vue'),
        meta: { title: 'Tendex AI - RFP List', requiresAuth: true },
      },
      {
        path: 'rfp/new',
        name: 'rfp-method-selection',
        component: () => import('@/views/rfp/RfpMethodSelectionView.vue'),
        meta: { title: 'Tendex AI - New RFP', requiresAuth: true },
      },
      {
        path: 'rfp/create',
        name: 'rfp-create',
        component: () => import('@/views/rfp/RfpCreateView.vue'),
        meta: { title: 'Tendex AI - Create RFP', requiresAuth: true },
      },
      {
        path: 'rfp/:id/edit',
        name: 'rfp-edit',
        component: () => import('@/views/rfp/RfpCreateView.vue'),
        meta: { title: 'Tendex AI - Edit RFP', requiresAuth: true },
        props: true,
      },
      /* Committee Management routes (TASK-902) */
      {
        path: 'committees/permanent',
        name: 'CommitteesPermanent',
        component: () => import('@/views/committees/CommitteesPermanentView.vue'),
        meta: { title: 'Tendex AI - Permanent Committees', requiresAuth: true },
      },
      {
        path: 'committees/temporary',
        name: 'CommitteesTemporary',
        component: () => import('@/views/committees/CommitteesTemporaryView.vue'),
        meta: { title: 'Tendex AI - Temporary Committees', requiresAuth: true },
      },
      /* Evaluation routes (TASK-505) */
      {
        path: 'evaluation/technical',
        name: 'EvaluationTechnical',
        component: () => import('@/views/evaluation/technical/TechnicalEvaluationList.vue'),
        meta: { title: 'Tendex AI - Technical Evaluation', requiresAuth: true },
      },
      {
        path: 'evaluation/technical/:id',
        name: 'TechnicalEvaluationDetail',
        component: () => import('@/views/evaluation/technical/TechnicalEvaluationDetail.vue'),
        meta: { title: 'Tendex AI - Technical Evaluation Detail', requiresAuth: true },
      },
      {
        path: 'evaluation/technical/:id/comparison',
        name: 'TechnicalComparison',
        component: () => import('@/views/evaluation/comparison/TechnicalComparison.vue'),
        meta: { title: 'Tendex AI - Technical Comparison', requiresAuth: true },
      },
      {
        path: 'evaluation/financial',
        name: 'EvaluationFinancial',
        component: () => import('@/views/evaluation/financial/FinancialEvaluationList.vue'),
        meta: { title: 'Tendex AI - Financial Evaluation', requiresAuth: true },
      },
      {
        path: 'evaluation/financial/:id',
        name: 'FinancialEvaluationDetail',
        component: () => import('@/views/evaluation/financial/FinancialEvaluationDetail.vue'),
        meta: { title: 'Tendex AI - Financial Evaluation Detail', requiresAuth: true },
      },
      {
        path: 'evaluation/financial/:id/comparison',
        name: 'FinancialComparison',
        component: () => import('@/views/evaluation/comparison/FinancialComparison.vue'),
        meta: { title: 'Tendex AI - Financial Comparison', requiresAuth: true },
      },
      /* Approvals / Task Center routes (TASK-902) */
      {
        path: 'approvals',
        name: 'Approvals',
        component: () => import('@/views/approvals/ApprovalsView.vue'),
        meta: { title: 'Tendex AI - Approvals', requiresAuth: true },
      },
      /* Inquiries routes (TASK-904) */
      {
        path: 'inquiries',
        name: 'Inquiries',
        component: () => import('@/views/inquiries/InquiriesView.vue'),
        meta: { title: 'Tendex AI - Inquiries', requiresAuth: true },
      },
      /* Reports routes (TASK-904) */
      {
        path: 'reports',
        name: 'Reports',
        component: () => import('@/views/reports/ReportsView.vue'),
        meta: { title: 'Tendex AI - Reports', requiresAuth: true },
      },
      /* AI Assistant routes (TASK-904) */
      {
        path: 'ai-assistant',
        name: 'AiAssistant',
        component: () => import('@/views/ai/AiAssistantView.vue'),
        meta: { title: 'Tendex AI - AI Assistant', requiresAuth: true },
      },
      /* Operator / Super Admin routes (TASK-601) */
      {
        path: 'operator',
        name: 'OperatorDashboard',
        component: () => import('@/views/operator/OperatorDashboardView.vue'),
        meta: { title: 'Tendex AI - Operator Dashboard', requiresAuth: true },
      },
      {
        path: 'operator/tenants',
        name: 'TenantList',
        component: () => import('@/views/tenants/TenantListView.vue'),
        meta: { title: 'Tendex AI - Tenants', requiresAuth: true },
      },
      {
        path: 'operator/tenants/create',
        name: 'TenantCreate',
        component: () => import('@/views/tenants/TenantCreateView.vue'),
        meta: { title: 'Tendex AI - Create Tenant', requiresAuth: true },
      },
      {
        path: 'operator/tenants/:id',
        name: 'TenantDetail',
        component: () => import('@/views/tenants/TenantDetailView.vue'),
        meta: { title: 'Tendex AI - Tenant Detail', requiresAuth: true },
        props: true,
      },
      {
        path: 'operator/purchase-orders',
        name: 'PurchaseOrderList',
        component: () => import('@/views/purchase-orders/PurchaseOrderListView.vue'),
        meta: { title: 'Tendex AI - Purchase Orders', requiresAuth: true },
      },
      {
        path: 'operator/purchase-orders/create',
        name: 'PurchaseOrderCreate',
        component: () => import('@/views/purchase-orders/PurchaseOrderCreateView.vue'),
        meta: { title: 'Tendex AI - Create Purchase Order', requiresAuth: true },
      },
      {
        path: 'operator/purchase-orders/:id',
        name: 'PurchaseOrderDetail',
        component: () => import('@/views/purchase-orders/PurchaseOrderDetailView.vue'),
        meta: { title: 'Tendex AI - Purchase Order Detail', requiresAuth: true },
        props: true,
      },
      /* Settings routes (TASK-903) */
      {
        path: 'settings/organization',
        name: 'SettingsOrganization',
        component: () => import('@/views/settings/OrganizationSettingsView.vue'),
        meta: { title: 'Tendex AI - Organization Settings', requiresAuth: true },
      },
      {
        path: 'settings/users',
        name: 'SettingsUsers',
        component: () => import('@/views/settings/UsersManagementView.vue'),
        meta: { title: 'Tendex AI - User Management', requiresAuth: true },
      },
      {
        path: 'settings/roles',
        name: 'SettingsRoles',
        component: () => import('@/views/settings/RolesManagementView.vue'),
        meta: { title: 'Tendex AI - Roles & Permissions', requiresAuth: true },
      },
      /* Operator Panel - Tenant Feature Flags & Branding (TASK-604) */
      {
        path: 'operator/tenants/:id/feature-flags',
        name: 'TenantFeatureFlags',
        component: () => import('@/views/tenants/TenantFeatureFlagsView.vue'),
        meta: {
          title: 'Tendex AI - Tenant Feature Flags',
          requiresAuth: true,
          requiredRoles: ['SuperAdmin', 'Operator'],
        },
        props: true,
      },
      {
        path: 'operator/tenants/:id/branding',
        name: 'TenantBranding',
        component: () => import('@/views/tenants/TenantBrandingView.vue'),
        meta: {
          title: 'Tendex AI - Tenant Branding',
          requiresAuth: true,
          requiredRoles: ['SuperAdmin', 'Operator'],
        },
        props: true,
      },
      /* Operator Panel - User Impersonation (TASK-603) */
      {
        path: 'operator/impersonation',
        name: 'OperatorImpersonation',
        component: () => import('@/views/operator/impersonation/ImpersonationView.vue'),
        meta: {
          title: 'Tendex AI - User Impersonation',
          requiresAuth: true,
          requiredRoles: ['SuperAdmin', 'SupportAdmin'],
        },
      },
      /* ── TASK-1001: New Routes ── */
      /* Workflow Designer */
      {
        path: 'workflow',
        name: 'WorkflowList',
        component: () => import('@/views/workflow/WorkflowListView.vue'),
        meta: { title: 'Tendex AI - Workflow Templates', requiresAuth: true },
      },
      {
        path: 'workflow/designer',
        name: 'WorkflowDesigner',
        component: () => import('@/views/workflow/WorkflowDesignerView.vue'),
        meta: { title: 'Tendex AI - Workflow Designer', requiresAuth: true },
      },
      {
        path: 'workflow/designer/:id',
        name: 'WorkflowDesignerEdit',
        component: () => import('@/views/workflow/WorkflowDesignerView.vue'),
        meta: { title: 'Tendex AI - Edit Workflow', requiresAuth: true },
        props: true,
      },
      /* 4D Permissions Matrix */
      {
        path: 'permissions/matrix',
        name: 'PermissionsMatrix',
        component: () => import('@/views/permissions/PermissionsMatrixView.vue'),
        meta: { title: 'Tendex AI - Permissions Matrix', requiresAuth: true },
      },
      /* Unified Task Center */
      {
        path: 'task-center',
        name: 'TaskCenter',
        component: () => import('@/views/task-center/TaskCenterView.vue'),
        meta: { title: 'Tendex AI - Task Center', requiresAuth: true },
      },
      /* Knowledge Base / RAG */
      {
        path: 'knowledge-base',
        name: 'KnowledgeBase',
        component: () => import('@/views/knowledge-base/KnowledgeBaseView.vue'),
        meta: { title: 'Tendex AI - Knowledge Base', requiresAuth: true },
      },
      /* Competition Templates */
      {
        path: 'rfp/templates',
        name: 'RfpTemplates',
        component: () => import('@/views/rfp/RfpTemplatesView.vue'),
        meta: { title: 'Tendex AI - Competition Templates', requiresAuth: true },
      },
      /* Notifications */
      {
        path: 'notifications',
        name: 'Notifications',
        component: () => import('@/views/notifications/NotificationsView.vue'),
        meta: { title: 'Tendex AI - Notifications', requiresAuth: true },
      },
      /* Report Generator / Export */
      {
        path: 'reports/export',
        name: 'ReportGenerator',
        component: () => import('@/views/reports/ReportGeneratorView.vue'),
        meta: { title: 'Tendex AI - Export Reports', requiresAuth: true },
      },
      /* Operator Portal - New Pages (TASK-1001) */
      {
        path: 'operator/ai-settings',
        name: 'OperatorAiSettings',
        component: () => import('@/views/operator/AiSettingsView.vue'),
        meta: {
          title: 'Tendex AI - AI Settings',
          requiresAuth: true,
          requiredRoles: ['SuperAdmin', 'Operator'],
        },
      },
      {
        path: 'operator/system-health',
        name: 'OperatorSystemHealth',
        component: () => import('@/views/operator/SystemHealthView.vue'),
        meta: {
          title: 'Tendex AI - System Health',
          requiresAuth: true,
          requiredRoles: ['SuperAdmin', 'Operator'],
        },
      },
      {
        path: 'operator/consumption',
        name: 'OperatorConsumption',
        component: () => import('@/views/operator/ConsumptionView.vue'),
        meta: {
          title: 'Tendex AI - Consumption',
          requiresAuth: true,
          requiredRoles: ['SuperAdmin', 'Operator'],
        },
      },
      {
        path: 'operator/subscriptions',
        name: 'OperatorSubscriptions',
        component: () => import('@/views/operator/SubscriptionsView.vue'),
        meta: {
          title: 'Tendex AI - Subscriptions',
          requiresAuth: true,
          requiredRoles: ['SuperAdmin', 'Operator'],
        },
      },
    ],
  },
  {
    path: '/auth',
    component: () => import('@/layouts/AuthLayout.vue'),
    children: [
      {
        path: 'login',
        name: 'Login',
        component: () => import('@/views/auth/LoginView.vue'),
        meta: { title: 'Tendex AI - Login', guest: true },
      },
      {
        path: 'mfa-verify',
        name: 'MfaVerify',
        component: () => import('@/views/auth/MfaVerifyView.vue'),
        meta: { title: 'Tendex AI - Verify Code', guest: true },
      },
      {
        path: 'forgot-password',
        name: 'ForgotPassword',
        component: () => import('@/views/auth/ForgotPasswordView.vue'),
        meta: { title: 'Tendex AI - Forgot Password', guest: true },
      },
      {
        path: 'reset-password',
        name: 'ResetPassword',
        component: () => import('@/views/auth/ResetPasswordView.vue'),
        meta: { title: 'Tendex AI - Reset Password', guest: true },
      },
    ],
  },
  /* Catch-all redirect */
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

/**
 * Global navigation guard.
 *
 * - Updates document title.
 * - Redirects unauthenticated users to login.
 * - Redirects authenticated users away from guest-only pages.
 */
router.beforeEach((to, _from, next) => {
  /* Update document title */
  const title = to.meta.title as string | undefined
  if (title) {
    document.title = title
  }

  /* Check authentication state from localStorage */
  const hasToken = !!localStorage.getItem('access_token')
  const requiresAuth = to.matched.some(
    (record) => record.meta.requiresAuth,
  )
  const isGuestOnly = to.meta.guest === true

  if (requiresAuth && !hasToken) {
    /* Redirect to login with return URL */
    next({
      name: 'Login',
      query: { redirect: to.fullPath },
    })
  } else if (isGuestOnly && hasToken) {
    /* Already authenticated, redirect to dashboard */
    next({ name: 'Dashboard' })
  } else {
    next()
  }
})

export default router
