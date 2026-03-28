/**
 * Dashboard Pinia Store for Tendex AI Platform.
 *
 * Manages all dashboard state including:
 * - KPI statistics
 * - Active competitions
 * - Pending tasks
 * - Notifications
 * - Active committees
 * - Recent activities
 * - Performance metrics
 *
 * All data is fetched dynamically from APIs.
 * NO mock/hardcoded data is used.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  DashboardStats,
  ActiveCompetition,
  PendingTask,
  Notification,
  ActiveCommittee,
  RecentActivity,
  PerformanceMetrics,
  UserRole,
  DashboardConfig,
} from '@/types/dashboard'
import {
  fetchAllDashboardData,
  fetchPendingTasks,
  fetchNotifications,
  markNotificationRead as apiMarkNotificationRead,
} from '@/services/dashboard'

/**
 * Returns dashboard visibility config based on user role.
 */
function getRoleConfig(role: UserRole): DashboardConfig {
  const baseConfig: DashboardConfig = {
    role,
    showCompetitions: true,
    showTasks: true,
    showCommittees: true,
    showPerformanceMetrics: true,
    showRecentActivity: true,
    showNotifications: true,
  }

  switch (role) {
    case 'procurement_manager':
      return { ...baseConfig }
    case 'system_supervisor':
      return { ...baseConfig }
    case 'department_manager':
      return {
        ...baseConfig,
        showCommittees: false,
      }
    case 'committee_member':
      return {
        ...baseConfig,
        showPerformanceMetrics: false,
        showRecentActivity: false,
      }
    case 'committee_chair':
      return {
        ...baseConfig,
        showRecentActivity: false,
      }
    case 'financial_controller':
      return {
        ...baseConfig,
        showCommittees: false,
      }
    case 'sector_representative':
      return {
        ...baseConfig,
        showPerformanceMetrics: false,
        showCommittees: false,
      }
    default:
      return baseConfig
  }
}

export const useDashboardStore = defineStore('dashboard', () => {
  /* ── State ────────────────────────────────── */
  const isLoading = ref(false)
  const isRefreshing = ref(false)
  const error = ref<string | null>(null)
  const lastUpdated = ref<Date | null>(null)

  const stats = ref<DashboardStats>({
    activeCompetitions: 0,
    completedCompetitions: 0,
    pendingEvaluations: 0,
    pendingTasks: 0,
    totalOffers: 0,
    complianceRate: 0,
  })

  const activeCompetitions = ref<ActiveCompetition[]>([])
  const pendingTasks = ref<PendingTask[]>([])
  const notifications = ref<Notification[]>([])
  const activeCommittees = ref<ActiveCommittee[]>([])
  const recentActivities = ref<RecentActivity[]>([])
  const performanceMetrics = ref<PerformanceMetrics>({
    averageCycleTimeDays: 0,
    complianceRate: 0,
    monthlyCompetitions: [],
    statusDistribution: [],
    averageEvaluationTimeDays: 0,
    slaComplianceRate: 0,
  })

  const userRole = ref<UserRole>('procurement_manager')

  /* ── Computed ─────────────────────────────── */
  const dashboardConfig = computed(() => getRoleConfig(userRole.value))

  const unreadNotificationsCount = computed(
    () => notifications.value.filter((n) => !n.isRead).length,
  )

  const criticalTasks = computed(
    () => pendingTasks.value.filter((t) => t.priority === 'critical'),
  )

  const exceededSlaTasks = computed(
    () => pendingTasks.value.filter((t) => t.slaStatus === 'exceeded'),
  )

  const delayedCompetitions = computed(
    () => activeCompetitions.value.filter((c) => c.isDelayed),
  )

  const hasData = computed(
    () =>
      stats.value.activeCompetitions > 0 ||
      pendingTasks.value.length > 0 ||
      notifications.value.length > 0,
  )

  /* ── Actions ──────────────────────────────── */

  /**
   * Loads all dashboard data from APIs in parallel.
   */
  async function loadDashboard(): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      const data = await fetchAllDashboardData()

      stats.value = data.stats
      activeCompetitions.value = data.activeCompetitions
      pendingTasks.value = data.pendingTasks
      notifications.value = data.notifications
      activeCommittees.value = data.activeCommittees
      recentActivities.value = data.recentActivities
      performanceMetrics.value = data.performanceMetrics
      lastUpdated.value = new Date()
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load dashboard data'
      console.error('[DashboardStore] Error loading dashboard:', err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Refreshes dashboard data without showing full loading state.
   */
  async function refreshDashboard(): Promise<void> {
    isRefreshing.value = true
    try {
      await loadDashboard()
    } finally {
      isRefreshing.value = false
    }
  }

  /**
   * Loads more pending tasks (pagination).
   */
  async function loadMoreTasks(page: number): Promise<void> {
    try {
      const result = await fetchPendingTasks({ page, pageSize: 10 })
      pendingTasks.value = [...pendingTasks.value, ...result.items]
    } catch (err) {
      console.error('[DashboardStore] Error loading more tasks:', err)
    }
  }

  /**
   * Loads more notifications (pagination).
   */
  async function loadMoreNotifications(page: number): Promise<void> {
    try {
      const result = await fetchNotifications({ page, pageSize: 10 })
      notifications.value = [...notifications.value, ...result.items]
    } catch (err) {
      console.error('[DashboardStore] Error loading more notifications:', err)
    }
  }

  /**
   * Marks a notification as read.
   */
  async function markNotificationRead(notificationId: string): Promise<void> {
    try {
      await apiMarkNotificationRead(notificationId)
      const notification = notifications.value.find((n) => n.id === notificationId)
      if (notification) {
        notification.isRead = true
      }
    } catch (err) {
      console.error('[DashboardStore] Error marking notification as read:', err)
    }
  }

  /**
   * Sets the current user role for role-based rendering.
   */
  function setUserRole(role: UserRole): void {
    userRole.value = role
  }

  return {
    /* State */
    isLoading,
    isRefreshing,
    error,
    lastUpdated,
    stats,
    activeCompetitions,
    pendingTasks,
    notifications,
    activeCommittees,
    recentActivities,
    performanceMetrics,
    userRole,

    /* Computed */
    dashboardConfig,
    unreadNotificationsCount,
    criticalTasks,
    exceededSlaTasks,
    delayedCompetitions,
    hasData,

    /* Actions */
    loadDashboard,
    refreshDashboard,
    loadMoreTasks,
    loadMoreNotifications,
    markNotificationRead,
    setUserRole,
  }
})
