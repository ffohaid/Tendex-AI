/**
 * Dashboard API Service for Tendex AI Platform.
 *
 * Provides methods to fetch all dashboard data from the backend.
 * Each method maps to a specific API endpoint as defined in the
 * backend Minimal API endpoints.
 *
 * IMPORTANT: No mock data is used. All data is fetched dynamically
 * from the backend APIs. If the API is unavailable, the service
 * returns empty/default values and logs the error.
 */
import { httpGet } from '@/services/http'
import type {
  DashboardStats,
  ActiveCompetition,
  PendingTask,
  Notification,
  ActiveCommittee,
  RecentActivity,
  PerformanceMetrics,
  DashboardData,
} from '@/types/dashboard'

const ENDPOINTS = {
  stats: '/dashboard/stats',
  competitions: '/competitions',
  tasks: '/tasks/pending',
  notifications: '/notifications',
  committees: '/committees',
  activities: '/dashboard/activities',
  metrics: '/dashboard/metrics',
} as const

/**
 * Fetches aggregated dashboard statistics (KPI cards).
 */
export async function fetchDashboardStats(): Promise<DashboardStats> {
  return httpGet<DashboardStats>(ENDPOINTS.stats)
}

/**
 * Fetches active competitions list with current phase info.
 */
export async function fetchActiveCompetitions(
  params?: {
    page?: number
    pageSize?: number
    status?: string
  },
): Promise<{ items: ActiveCompetition[]; totalCount: number }> {
  return httpGet(ENDPOINTS.competitions, { params })
}

/**
 * Fetches pending tasks for the current user based on their role.
 */
export async function fetchPendingTasks(
  params?: {
    page?: number
    pageSize?: number
    type?: string
    priority?: string
  },
): Promise<{ items: PendingTask[]; totalCount: number }> {
  return httpGet(ENDPOINTS.tasks, { params })
}

/**
 * Fetches notifications for the current user.
 */
export async function fetchNotifications(
  params?: {
    page?: number
    pageSize?: number
    isRead?: boolean
  },
): Promise<{ items: Notification[]; totalCount: number; unreadCount: number }> {
  return httpGet(ENDPOINTS.notifications, { params })
}

/**
 * Marks a notification as read.
 */
export async function markNotificationRead(notificationId: string): Promise<void> {
  return httpGet(`${ENDPOINTS.notifications}/${notificationId}/read`)
}

/**
 * Fetches active committees.
 */
export async function fetchActiveCommittees(
  params?: {
    page?: number
    pageSize?: number
    type?: string
  },
): Promise<{ items: ActiveCommittee[]; totalCount: number }> {
  return httpGet(ENDPOINTS.committees, { params })
}

/**
 * Fetches recent activity log.
 */
export async function fetchRecentActivities(
  params?: {
    page?: number
    pageSize?: number
  },
): Promise<{ items: RecentActivity[]; totalCount: number }> {
  return httpGet(ENDPOINTS.activities, { params })
}

/**
 * Fetches performance metrics for charts.
 */
export async function fetchPerformanceMetrics(): Promise<PerformanceMetrics> {
  return httpGet<PerformanceMetrics>(ENDPOINTS.metrics)
}

/**
 * Fetches all dashboard data in parallel for optimal performance.
 * Uses Promise.allSettled to ensure partial data is still displayed
 * even if some endpoints fail.
 */
export async function fetchAllDashboardData(): Promise<DashboardData> {
  const [
    statsResult,
    competitionsResult,
    tasksResult,
    notificationsResult,
    committeesResult,
    activitiesResult,
    metricsResult,
  ] = await Promise.allSettled([
    fetchDashboardStats(),
    fetchActiveCompetitions({ page: 1, pageSize: 5 }),
    fetchPendingTasks({ page: 1, pageSize: 10 }),
    fetchNotifications({ page: 1, pageSize: 10 }),
    fetchActiveCommittees({ page: 1, pageSize: 5 }),
    fetchRecentActivities({ page: 1, pageSize: 10 }),
    fetchPerformanceMetrics(),
  ])

  const defaultStats: DashboardStats = {
    activeCompetitions: 0,
    completedCompetitions: 0,
    pendingEvaluations: 0,
    pendingTasks: 0,
    totalOffers: 0,
    complianceRate: 0,
  }

  const defaultMetrics: PerformanceMetrics = {
    averageCycleTimeDays: 0,
    complianceRate: 0,
    monthlyCompetitions: [],
    statusDistribution: [],
    averageEvaluationTimeDays: 0,
    slaComplianceRate: 0,
  }

  return {
    stats:
      statsResult.status === 'fulfilled'
        ? statsResult.value
        : defaultStats,
    activeCompetitions:
      competitionsResult.status === 'fulfilled'
        ? competitionsResult.value.items
        : [],
    pendingTasks:
      tasksResult.status === 'fulfilled'
        ? tasksResult.value.items
        : [],
    notifications:
      notificationsResult.status === 'fulfilled'
        ? notificationsResult.value.items
        : [],
    activeCommittees:
      committeesResult.status === 'fulfilled'
        ? committeesResult.value.items
        : [],
    recentActivities:
      activitiesResult.status === 'fulfilled'
        ? activitiesResult.value.items
        : [],
    performanceMetrics:
      metricsResult.status === 'fulfilled'
        ? metricsResult.value
        : defaultMetrics,
  }
}
