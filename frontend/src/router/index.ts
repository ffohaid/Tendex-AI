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
      {
        path: 'committees/permanent',
        name: 'CommitteesPermanent',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Permanent Committees', requiresAuth: true },
      },
      {
        path: 'committees/temporary',
        name: 'CommitteesTemporary',
        component: () => import('@/views/DashboardView.vue'),
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
      {
        path: 'approvals',
        name: 'Approvals',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Approvals', requiresAuth: true },
      },
      {
        path: 'inquiries',
        name: 'Inquiries',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Inquiries', requiresAuth: true },
      },
      {
        path: 'reports',
        name: 'Reports',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Reports', requiresAuth: true },
      },
      {
        path: 'ai-assistant',
        name: 'AiAssistant',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - AI Assistant', requiresAuth: true },
      },
      {
        path: 'settings/organization',
        name: 'SettingsOrganization',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Organization Settings', requiresAuth: true },
      },
      {
        path: 'settings/users',
        name: 'SettingsUsers',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - User Management', requiresAuth: true },
      },
      {
        path: 'settings/roles',
        name: 'SettingsRoles',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Roles & Permissions', requiresAuth: true },
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
