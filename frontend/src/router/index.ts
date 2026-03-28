import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

/**
 * Application route definitions.
 *
 * Routes are organized by layout:
 * - MainLayout: authenticated pages with sidebar/header/footer
 * - AuthLayout: authentication pages (login, etc.)
 *
 * Lazy-loaded components for optimal bundle splitting.
 */
const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Dashboard' },
      },
      /* Placeholder routes for future Sprint 5 tasks */
      {
        path: 'rfp',
        name: 'RfpList',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - RFP List' },
      },
      {
        path: 'rfp/create',
        name: 'RfpCreate',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Create RFP' },
      },
      {
        path: 'committees/permanent',
        name: 'CommitteesPermanent',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Permanent Committees' },
      },
      {
        path: 'committees/temporary',
        name: 'CommitteesTemporary',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Temporary Committees' },
      },
      {
        path: 'evaluation/technical',
        name: 'EvaluationTechnical',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Technical Evaluation' },
      },
      {
        path: 'evaluation/financial',
        name: 'EvaluationFinancial',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Financial Evaluation' },
      },
      {
        path: 'approvals',
        name: 'Approvals',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Approvals' },
      },
      {
        path: 'inquiries',
        name: 'Inquiries',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Inquiries' },
      },
      {
        path: 'reports',
        name: 'Reports',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Reports' },
      },
      {
        path: 'ai-assistant',
        name: 'AiAssistant',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - AI Assistant' },
      },
      {
        path: 'settings/organization',
        name: 'SettingsOrganization',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Organization Settings' },
      },
      {
        path: 'settings/users',
        name: 'SettingsUsers',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - User Management' },
      },
      {
        path: 'settings/roles',
        name: 'SettingsRoles',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tendex AI - Roles & Permissions' },
      },
    ],
  },
  {
    path: '/auth',
    component: () => import('@/layouts/AuthLayout.vue'),
    children: [
      /* Auth routes will be added in TASK-502 */
    ],
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

/**
 * Global navigation guard to update the document title.
 */
router.beforeEach((to, _from, next) => {
  const title = to.meta.title as string | undefined
  if (title) {
    document.title = title
  }
  next()
})

export default router
