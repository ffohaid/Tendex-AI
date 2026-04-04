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
import { httpGet, httpPost } from '@/services/http'
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
  stats: '/v1/dashboard/stats',
  competitions: '/v1/competitions',
  tasks: '/v1/tasks/pending',
  notifications: '/v1/notifications',
  committees: '/v1/committees',
  activities: '/v1/dashboard/activities',
  metrics: '/v1/dashboard/metrics',
} as const

/**
 * Fetches aggregated dashboard statistics (KPI cards).
 */
export async function fetchDashboardStats(): Promise<DashboardStats> {
  return httpGet<DashboardStats>(ENDPOINTS.stats)
}

/**
 * Maps backend integer competition status to frontend string.
 * Backend: Draft=0, UnderPreparation=1, PendingApproval=2, Approved=3,
 * Published=4, InquiriesOpen=5, InquiriesClosed=6,
 * ReceivingOffers=7, OffersClosed=8, TechnicalAnalysis=9, TechnicalAnalysisCompleted=10,
 * FinancialAnalysis=11, FinancialAnalysisCompleted=12, AwardNotification=13, AwardApproved=14,
 * ContractApproval=15, ContractApproved=16, ContractSigned=17, Rejected=90, Cancelled=91, Suspended=92
 */
function mapCompetitionStatus(status: number | string): string {
  if (typeof status === 'string') return status
  const statusMap: Record<number, string> = {
    0: 'draft', 1: 'draft', 2: 'pending_approval', 3: 'approved',
    4: 'published', 5: 'published', 6: 'receiving_offers', 7: 'receiving_offers',
    8: 'technical_evaluation', 9: 'technical_evaluation',
    10: 'financial_evaluation', 11: 'financial_evaluation',
    12: 'awarding', 13: 'awarding', 14: 'awarding', 15: 'awarding', 16: 'completed',
    90: 'cancelled', 91: 'cancelled', 92: 'cancelled',
  }
  return statusMap[status] ?? 'draft'
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
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const response = await httpGet<{ items: any[]; totalCount: number }>(ENDPOINTS.competitions, { params })
  return {
    ...response,
    items: (response.items || []).map(item => ({
      id: item.id,
      referenceNumber: item.referenceNumber ?? '',
      titleAr: item.projectNameAr ?? item.titleAr ?? '',
      titleEn: item.projectNameEn ?? item.titleEn ?? '',
      status: mapCompetitionStatus(item.status) as ActiveCompetition['status'],
      currentPhase: item.currentPhase ?? { id: 0, nameAr: '', nameEn: '' },
      estimatedBudget: item.estimatedBudget ?? 0,
      offersCount: item.offersCount ?? 0,
      createdAt: item.createdAt ?? '',
      deadline: item.submissionDeadline ?? item.deadline ?? '',
      progress: item.progress ?? 0,
      isDelayed: item.isDelayed ?? false,
    })),
  }
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
  return httpPost(`${ENDPOINTS.notifications}/${notificationId}/read`)
}

/**
 * Maps backend integer committee type to frontend string.
 * Backend: TechnicalEvaluation=1, FinancialEvaluation=2, BookletPreparation=3, InquiryReview=4, OtherPermanent=5
 */
function mapCommitteeType(type: number | string): 'permanent' | 'temporary' {
  if (typeof type === 'string') return type as 'permanent' | 'temporary'
  // Types 1,2,5 are permanent; 3,4 are temporary
  return [3, 4].includes(type) ? 'temporary' : 'permanent'
}

/**
 * Maps backend integer committee status to frontend string.
 * Backend: Active=1, Suspended=2, Dissolved=3, Expired=4
 */
function mapCommitteeStatus(status: number | string): 'active' | 'expired' | 'suspended' {
  const statusMap: Record<number, 'active' | 'expired' | 'suspended'> = {
    1: 'active',
    2: 'suspended',
    3: 'expired',
    4: 'expired',
  }
  if (typeof status === 'string') return status as 'active' | 'expired' | 'suspended'
  return statusMap[status] ?? 'active'
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
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const response = await httpGet<{ items: any[]; totalCount: number }>(ENDPOINTS.committees, { params })
  return {
    ...response,
    items: response.items.map(item => ({
      ...item,
      type: mapCommitteeType(item.type),
      status: mapCommitteeStatus(item.status),
      membersCount: item.membersCount ?? item.activeMemberCount ?? 0,
      nameAr: item.nameAr ?? item.name ?? '',
      nameEn: item.nameEn ?? item.name ?? '',
      expiryDate: item.expiryDate ?? item.endDate ?? null,
    })),
  }
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
