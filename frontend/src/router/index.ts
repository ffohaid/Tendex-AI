import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

/**
 * Application route definitions.
 *
 * Routes are organized by layout:
 * - MainLayout: authenticated pages with sidebar/header/footer
 * - AuthLayout: authentication pages (login, MFA, password reset)
 *
 * Lazy-loaded components for optimal bundle splitting.
 *
 * Permission-based access control:
 * - `meta.requiresAuth`: requires authentication
 * - `meta.requiredPermission`: requires specific permission code(s)
 * - `meta.requiredRoles`: requires specific system role(s)
 * - OperatorPrimaryAdmin: sees ONLY operator panel routes
 * - TenantPrimaryAdmin: bypasses all permission checks within their tenant
 * - All other roles: controlled by permission matrix
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
        meta: { title: 'Tendex AI - RFP List', requiresAuth: true, requiredPermission: 'rfp.view' },
      },
      {
        path: 'rfp/new',
        name: 'rfp-method-selection',
        component: () => import('@/views/rfp/RfpMethodSelectionView.vue'),
        meta: { title: 'Tendex AI - New RFP', requiresAuth: true, requiredPermission: 'rfp.create' },
      },
      {
        path: 'rfp/create',
        name: 'rfp-create',
        component: () => import('@/views/rfp/RfpCreateView.vue'),
        meta: { title: 'Tendex AI - Create RFP', requiresAuth: true, requiredPermission: 'rfp.create' },
      },
      {
        path: 'rfp/:id/edit',
        name: 'rfp-edit',
        component: () => import('@/views/rfp/RfpCreateView.vue'),
        meta: { title: 'Tendex AI - Edit RFP', requiresAuth: true, requiredPermission: 'rfp.update' },
        props: true,
      },
      {
        path: 'rfp/:id/export',
        name: 'rfp-export',
        component: () => import('@/views/rfp/RfpExportView.vue'),
        meta: { title: 'Tendex AI - Export RFP', requiresAuth: true, requiredPermission: 'rfp.export' },
        props: true,
      },
      /* Committee Management routes (TASK-902) */
      {
        path: 'committees/permanent',
        name: 'CommitteesPermanent',
        component: () => import('@/views/committees/CommitteesPermanentView.vue'),
        meta: { title: 'Tendex AI - Permanent Committees', requiresAuth: true, requiredPermission: 'committees.view' },
      },
      {
        path: 'committees/temporary',
        name: 'CommitteesTemporary',
        component: () => import('@/views/committees/CommitteesTemporaryView.vue'),
        meta: { title: 'Tendex AI - Temporary Committees', requiresAuth: true, requiredPermission: 'committees.view' },
      },
      /* Supplier Offers Management */
      {
        path: 'evaluation/offers',
        name: 'SupplierOffers',
        component: () => import('@/views/evaluation/offers/SupplierOffersView.vue'),
        meta: { title: 'Tendex AI - Supplier Offers', requiresAuth: true, requiredPermission: 'offers.view' },
      },
      {
        path: 'evaluation/offers/:competitionId',
        name: 'SupplierOffersDetail',
        component: () => import('@/views/evaluation/offers/SupplierOffersDetailView.vue'),
        meta: { title: 'Tendex AI - Supplier Offers Detail', requiresAuth: true, requiredPermission: 'offers.view' },
        props: true,
      },
      /* Evaluation routes (TASK-505) */
      {
        path: 'evaluation/technical',
        name: 'EvaluationTechnical',
        component: () => import('@/views/evaluation/technical/TechnicalEvaluationList.vue'),
        meta: { title: 'Tendex AI - Technical Evaluation', requiresAuth: true, requiredPermission: 'evaluation.view' },
      },
      {
        path: 'evaluation/technical/:id',
        name: 'TechnicalEvaluationDetail',
        component: () => import('@/views/evaluation/technical/TechnicalEvaluationDetail.vue'),
        meta: { title: 'Tendex AI - Technical Evaluation Detail', requiresAuth: true, requiredPermission: 'evaluation.technical' },
      },
      {
        path: 'evaluation/technical/:id/comparison',
        name: 'TechnicalComparison',
        component: () => import('@/views/evaluation/comparison/TechnicalComparison.vue'),
        meta: { title: 'Tendex AI - Technical Comparison', requiresAuth: true, requiredPermission: 'evaluation.technical' },
      },
      {
        path: 'evaluation/financial',
        name: 'EvaluationFinancial',
        component: () => import('@/views/evaluation/financial/FinancialEvaluationList.vue'),
        meta: { title: 'Tendex AI - Financial Evaluation', requiresAuth: true, requiredPermission: 'evaluation.view' },
      },
      {
        path: 'evaluation/financial/:id',
        name: 'FinancialEvaluationDetail',
        component: () => import('@/views/evaluation/financial/FinancialEvaluationDetail.vue'),
        meta: { title: 'Tendex AI - Financial Evaluation Detail', requiresAuth: true, requiredPermission: 'evaluation.financial' },
      },
      {
        path: 'evaluation/financial/:id/comparison',
        name: 'FinancialComparison',
        component: () => import('@/views/evaluation/comparison/FinancialComparison.vue'),
        meta: { title: 'Tendex AI - Financial Comparison', requiresAuth: true, requiredPermission: 'evaluation.financial' },
      },
      /* Comprehensive Evaluation & Award Recommendation */
      {
        path: 'evaluation/comprehensive',
        name: 'ComprehensiveEvaluation',
        component: () => import('@/views/evaluation/comprehensive/ComprehensiveEvaluationList.vue'),
        meta: { title: 'Tendex AI - Comprehensive Evaluation', requiresAuth: true, requiredPermission: 'evaluation.view' },
      },
      {
        path: 'evaluation/comprehensive/:id',
        name: 'ComprehensiveEvaluationDetail',
        component: () => import('@/views/evaluation/comprehensive/ComprehensiveEvaluationDetail.vue'),
        meta: { title: 'Tendex AI - Comprehensive Evaluation Detail', requiresAuth: true, requiredPermission: 'evaluation.view' },
      },
      /* Approvals / Task Center routes (TASK-902) */
      {
        path: 'approvals',
        name: 'Approvals',
        component: () => import('@/views/approvals/ApprovalsView.vue'),
        meta: { title: 'Tendex AI - Approvals', requiresAuth: true, requiredPermission: 'approvals.view' },
      },
      /* Inquiries routes (TASK-904) */
      {
        path: 'inquiries',
        name: 'Inquiries',
        component: () => import('@/views/inquiries/InquiriesView.vue'),
        meta: { title: 'Tendex AI - Inquiries', requiresAuth: true, requiredPermission: 'inquiries.view' },
      },
      /* Reports routes (TASK-904) */
      {
        path: 'reports',
        name: 'Reports',
        component: () => import('@/views/reports/ReportsView.vue'),
        meta: { title: 'Tendex AI - Reports', requiresAuth: true, requiredPermission: 'reports.view' },
      },
      /* AI Assistant routes (TASK-904) */
      {
        path: 'ai-assistant',
        name: 'AiAssistant',
        component: () => import('@/views/ai/AiAssistantView.vue'),
        meta: { title: 'Tendex AI - AI Assistant', requiresAuth: true, requiredPermission: 'ai.view' },
      },
      /* Operator / Super Admin routes (TASK-601) */
      {
        path: 'operator',
        name: 'OperatorDashboard',
        component: () => import('@/views/operator/OperatorDashboardView.vue'),
        meta: { title: 'Tendex AI - Operator Dashboard', requiresAuth: true, requiredRoles: ['OperatorPrimaryAdmin'] },
      },
      {
        path: 'operator/tenants',
        name: 'TenantList',
        component: () => import('@/views/tenants/TenantListView.vue'),
        meta: { title: 'Tendex AI - Tenants', requiresAuth: true, requiredRoles: ['OperatorPrimaryAdmin'] },
      },
      {
        path: 'operator/tenants/create',
        name: 'TenantCreate',
        component: () => import('@/views/tenants/TenantCreateView.vue'),
        meta: { title: 'Tendex AI - Create Tenant', requiresAuth: true, requiredRoles: ['OperatorPrimaryAdmin'] },
      },
      {
        path: 'operator/tenants/:id',
        name: 'TenantDetail',
        component: () => import('@/views/tenants/TenantDetailView.vue'),
        meta: { title: 'Tendex AI - Tenant Detail', requiresAuth: true, requiredRoles: ['OperatorPrimaryAdmin'] },
        props: true,
      },
      {
        path: 'operator/purchase-orders',
        name: 'PurchaseOrderList',
        component: () => import('@/views/purchase-orders/PurchaseOrderListView.vue'),
        meta: { title: 'Tendex AI - Purchase Orders', requiresAuth: true, requiredPermission: 'purchaseorders.view' },
      },
      {
        path: 'operator/purchase-orders/create',
        name: 'PurchaseOrderCreate',
        component: () => import('@/views/purchase-orders/PurchaseOrderCreateView.vue'),
        meta: { title: 'Tendex AI - Create Purchase Order', requiresAuth: true, requiredPermission: 'purchaseorders.create' },
      },
      {
        path: 'operator/purchase-orders/:id',
        name: 'PurchaseOrderDetail',
        component: () => import('@/views/purchase-orders/PurchaseOrderDetailView.vue'),
        meta: { title: 'Tendex AI - Purchase Order Detail', requiresAuth: true, requiredPermission: 'purchaseorders.view' },
        props: true,
      },
      /* Settings routes (TASK-903) */
      {
        path: 'settings/organization',
        name: 'SettingsOrganization',
        component: () => import('@/views/settings/OrganizationSettingsView.vue'),
        meta: { title: 'Tendex AI - Organization Settings', requiresAuth: true, requiredPermission: 'organization.view' },
      },
      {
        path: 'settings/users',
        name: 'SettingsUsers',
        component: () => import('@/views/settings/UsersManagementView.vue'),
        meta: { title: 'Tendex AI - User Management', requiresAuth: true, requiredPermission: 'users.view' },
      },
      {
        path: 'settings/roles',
        name: 'SettingsRoles',
        component: () => import('@/views/settings/RolesManagementView.vue'),
        meta: { title: 'Tendex AI - Roles & Permissions', requiresAuth: true, requiredPermission: 'roles.view' },
      },
      /* Operator Panel - Tenant Feature Flags & Branding (TASK-604) */
      {
        path: 'operator/tenants/:id/feature-flags',
        name: 'TenantFeatureFlags',
        component: () => import('@/views/tenants/TenantFeatureFlagsView.vue'),
        meta: {
          title: 'Tendex AI - Tenant Feature Flags',
          requiresAuth: true,
          requiredRoles: ['OperatorPrimaryAdmin'],
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
          requiredRoles: ['OperatorPrimaryAdmin'],
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
          requiredRoles: ['OperatorPrimaryAdmin'],
        },
      },
      /* ── TASK-1001: New Routes ── */
      /* Workflow Designer */
      {
        path: 'workflow',
        name: 'WorkflowList',
        component: () => import('@/views/workflow/WorkflowListView.vue'),
        meta: { title: 'Tendex AI - Workflow Templates', requiresAuth: true, requiredPermission: 'workflow.view' },
      },
      {
        path: 'workflow/designer',
        name: 'WorkflowDesigner',
        component: () => import('@/views/workflow/WorkflowDesignerView.vue'),
        meta: { title: 'Tendex AI - Workflow Designer', requiresAuth: true, requiredPermission: 'workflow.create' },
      },
      {
        path: 'workflow/designer/:id',
        name: 'WorkflowDesignerEdit',
        component: () => import('@/views/workflow/WorkflowDesignerView.vue'),
        meta: { title: 'Tendex AI - Edit Workflow', requiresAuth: true, requiredPermission: 'workflow.edit' },
        props: true,
      },
      /* Flexible Permissions Matrix */
      {
        path: 'permissions/matrix',
        name: 'PermissionsMatrix',
        component: () => import('@/views/permissions/PermissionsMatrixView.vue'),
        meta: { title: 'Tendex AI - Permissions Matrix', requiresAuth: true, requiredPermission: 'matrix.view' },
      },
      /* Unified Task Center */
      {
        path: 'task-center',
        name: 'TaskCenter',
        component: () => import('@/views/task-center/TaskCenterView.vue'),
        meta: { title: 'Tendex AI - Task Center', requiresAuth: true, requiredPermission: 'tasks.view' },
      },
      /* Knowledge Base / RAG */
      {
        path: 'knowledge-base',
        name: 'KnowledgeBase',
        component: () => import('@/views/knowledge-base/KnowledgeBaseView.vue'),
        meta: { title: 'Tendex AI - Knowledge Base', requiresAuth: true, requiredPermission: 'knowledgebase.view' },
      },
      /* Unified Template Library (replaces separate RfpTemplates & BookletTemplates) */
      {
        path: 'rfp/template-library',
        name: 'TemplateLibrary',
        component: () => import('@/views/rfp/TemplateLibraryView.vue'),
        meta: { title: 'Tendex AI - Template Library', requiresAuth: true, requiredPermission: 'templates.view' },
      },
      /* Legacy routes - redirect to unified library */
      {
        path: 'rfp/templates',
        name: 'RfpTemplates',
        redirect: { name: 'TemplateLibrary' },
      },
      {
        path: 'rfp/booklet-templates',
        name: 'BookletTemplates',
        redirect: { name: 'TemplateLibrary' },
      },
      {
        path: 'rfp/booklet-editor/:id',
        name: 'BookletEditor',
        component: () => import('@/views/rfp/BookletEditorView.vue'),
        meta: { title: 'Tendex AI - Booklet Editor', requiresAuth: true, requiredPermission: 'templates.create' },
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
        meta: { title: 'Tendex AI - Export Reports', requiresAuth: true, requiredPermission: 'reports.export' },
      },
      /* Operator Portal - New Pages (TASK-1001) */
      {
        path: 'operator/ai-settings',
        name: 'OperatorAiSettings',
        component: () => import('@/views/operator/AiSettingsView.vue'),
        meta: {
          title: 'Tendex AI - AI Settings',
          requiresAuth: true,
          requiredRoles: ['OperatorPrimaryAdmin'],
        },
      },
      {
        path: 'operator/system-health',
        name: 'OperatorSystemHealth',
        component: () => import('@/views/operator/SystemHealthView.vue'),
        meta: {
          title: 'Tendex AI - System Health',
          requiresAuth: true,
          requiredRoles: ['OperatorPrimaryAdmin'],
        },
      },
      {
        path: 'operator/consumption',
        name: 'OperatorConsumption',
        component: () => import('@/views/operator/ConsumptionView.vue'),
        meta: {
          title: 'Tendex AI - Consumption',
          requiresAuth: true,
          requiredRoles: ['OperatorPrimaryAdmin'],
        },
      },
      {
        path: 'operator/subscriptions',
        name: 'OperatorSubscriptions',
        component: () => import('@/views/operator/SubscriptionsView.vue'),
        meta: {
          title: 'Tendex AI - Subscriptions',
          requiresAuth: true,
          requiredRoles: ['OperatorPrimaryAdmin'],
        },
      },
      /* Operator Portal - Support Tickets */
      {
        path: 'operator/support',
        name: 'OperatorSupportTickets',
        component: () => import('@/views/operator/SupportTicketsView.vue'),
        meta: {
          title: 'Tendex AI - Support Tickets',
          requiresAuth: true,
          requiredRoles: ['OperatorPrimaryAdmin'],
        },
      },
      {
        path: 'operator/audit-log',
        name: 'OperatorAuditLog',
        component: () => import('@/views/operator/AuditLogView.vue'),
        meta: {
          title: 'Tendex AI - Audit Log',
          requiresAuth: true,
          requiredRoles: ['OperatorPrimaryAdmin'],
        },
      },
      /* User Profile (accessible by all authenticated users) */
      {
        path: 'profile',
        name: 'UserProfile',
        component: () => import('@/views/profile/UserProfileView.vue'),
        meta: { title: 'Tendex AI - Profile', requiresAuth: true },
      },
      /* Per-Tenant AI Settings (TASK: tenant-specific AI models) */
      {
        path: 'settings/ai',
        name: 'TenantAiSettings',
        component: () => import('@/views/settings/TenantAiSettingsView.vue'),
        meta: {
          title: 'Tendex AI - AI Settings',
          requiresAuth: true,
          requiredPermission: 'ai.settings',
        },
      },
      /* Active Directory Integration Settings */
      {
        path: 'settings/active-directory',
        name: 'ActiveDirectorySettings',
        component: () => import('@/views/settings/ActiveDirectorySettingsView.vue'),
        meta: {
          title: 'Tendex AI - Active Directory',
          requiresAuth: true,
          requiredPermission: 'organization.manage',
        },
      },
      /* Tenant Support Tickets */
      {
        path: 'support',
        name: 'SupportTickets',
        component: () => import('@/views/support/SupportTicketsView.vue'),
        meta: {
          title: 'Tendex AI - Support',
          requiresAuth: true,
          requiredPermission: 'support.view',
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
        path: 'accept-invitation',
        name: 'AcceptInvitation',
        component: () => import('@/views/auth/AcceptInvitationView.vue'),
        meta: { title: 'Tendex AI - Accept Invitation', guest: true },
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
  /* Access Denied page */
  {
    path: '/access-denied',
    name: 'AccessDenied',
    component: () => import('@/views/errors/AccessDeniedView.vue'),
    meta: { title: 'Tendex AI - Access Denied' },
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
 * - Enforces permission-based access control using requiredPermission meta.
 * - Enforces role-based access control using requiredRoles meta.
 * - OperatorPrimaryAdmin: can only access operator panel routes.
 * - TenantPrimaryAdmin: bypasses all permission checks within their tenant.
 * - All other roles: controlled by permission matrix.
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
    return
  }

  if (isGuestOnly && hasToken) {
    /* Allow accept-invitation page even for authenticated users */
    if (to.name !== 'AcceptInvitation') {
      next({ name: 'Dashboard' })
      return
    }
  }

  /* ── Permission & Role Enforcement ── */
  if (hasToken) {
    // Parse user data from localStorage
    let userRoles: string[] = []
    let userPermissions: string[] = []
    try {
      const raw = localStorage.getItem('user')
      if (raw) {
        const userData = JSON.parse(raw)
        userRoles = userData.roles ?? []
        userPermissions = userData.permissions ?? []
      }
    } catch {
      // Ignore parse errors
    }

    // Role name aliases to support both old and new naming conventions
    const OPERATOR_ALIASES = ['OperatorPrimaryAdmin', 'Operator Super Admin', 'OperatorSuperAdmin', 'SuperAdmin', 'Super Admin']
    const TENANT_ADMIN_ALIASES = ['TenantPrimaryAdmin', 'Tenant Primary Admin', 'Tenant Owner']

    const isOperatorAdmin = userRoles.some(r => OPERATOR_ALIASES.includes(r))
    const isTenantAdmin = userRoles.some(r => TENANT_ADMIN_ALIASES.includes(r))

    const isDualRole = isOperatorAdmin && isTenantAdmin

    // ── Dual-role user (both Operator + Tenant Admin): can access BOTH panels ──
    if (isDualRole) {
      // Dual-role users bypass all permission checks
      // They can access both operator and tenant routes
    }
    // ── OperatorPrimaryAdmin ONLY: can ONLY access operator routes ──
    else if (isOperatorAdmin && !isTenantAdmin) {
      // Operator admin visiting root Dashboard should be redirected to Operator Dashboard
      // Allow profile page for all authenticated users
      if (to.name === 'UserProfile' || to.name === 'TenantAiSettings' || to.name === 'ActiveDirectorySettings') {
        // Profile and settings pages are accessible by all authenticated users
        next()
        return
      } else if (to.name === 'Dashboard' || to.path === '/') {
        next({ name: 'OperatorDashboard' })
        return
      }
      // Only allow routes under /operator path or routes with operator required roles
      const isOperatorRoute = to.path.startsWith('/operator')
      const requiredRoles = to.meta.requiredRoles as string[] | undefined
      const isAllowed = isOperatorRoute || requiredRoles?.some(r => OPERATOR_ALIASES.includes(r))
      if (!isAllowed) {
        next({ name: 'OperatorDashboard' })
        return
      }
    }
    // ── TenantPrimaryAdmin ONLY: bypasses permission checks, but NOT operator routes ──
    else if (isTenantAdmin && !isOperatorAdmin) {
      const requiredRoles = to.meta.requiredRoles as string[] | undefined
      if (requiredRoles?.some(r => OPERATOR_ALIASES.includes(r))) {
        next({ name: 'AccessDenied' })
        return
      }
      // TenantPrimaryAdmin bypasses all other permission/role checks
    }
    // ── Regular users: enforce requiredRoles and requiredPermission ──
    else {
      // Check requiredRoles
      const requiredRoles = to.meta.requiredRoles as string[] | undefined
      if (requiredRoles && requiredRoles.length > 0) {
        const hasRequiredRole = requiredRoles.some(r => userRoles.includes(r))
        if (!hasRequiredRole) {
          next({ name: 'AccessDenied' })
          return
        }
      }

      // Check requiredPermission
      const requiredPermission = to.meta.requiredPermission as string | string[] | undefined
      if (requiredPermission) {
        const perms = Array.isArray(requiredPermission) ? requiredPermission : [requiredPermission]
        const hasPermission = perms.some(p => userPermissions.includes(p))
        if (!hasPermission) {
          next({ name: 'AccessDenied' })
          return
        }
      }
    }
  }

  next()
})

export default router
