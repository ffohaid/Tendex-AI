/**
 * Task Center API service for Tendex AI Platform.
 * Matches backend TaskEndpoints at /api/v1/tasks.
 * NO mock data — all data comes from the API.
 */
import { httpGet } from './http'

/* ──────────────────────────────────────────────
 * Type Definitions (matching Backend DTOs)
 * ──────────────────────────────────────────── */

export type TaskSourceType = 'competition' | 'inquiry'

export type TaskType =
  | 'approve_request'
  | 'evaluate_offer'
  | 'answer_inquiry'
  | 'approve_inquiry_response'
  | 'committee_action'

export type TaskPriority = 'low' | 'medium' | 'high' | 'critical'

export type SlaStatus = 'on_track' | 'approaching' | 'exceeded'

/** Matches backend PendingTaskDto */
export interface TaskItem {
  id: string
  sourceType: TaskSourceType
  type: TaskType | string
  titleAr: string
  titleEn: string
  descriptionAr: string
  descriptionEn: string
  competitionTitleAr: string
  competitionTitleEn: string
  competitionReferenceNumber: string
  assignedAt: string
  slaDeadline: string
  slaStatus: SlaStatus
  remainingTimeSeconds: number
  priority: TaskPriority
  actionRequired: string
  actionUrl: string
  assignedByAr?: string | null
  assignedByEn?: string | null
  aiRecommendationAr?: string | null
  aiRecommendationEn?: string | null
  aiConfidence?: number | null
}

/** Matches backend TaskCenterStatsDto */
export interface TaskCenterStats {
  totalPending: number
  approvalTasks: number
  evaluationTasks: number
  inquiryTasks: number
  reviewTasks: number
  criticalTasks: number
  overdueTasks: number
  completedToday: number
  averageSlaCompliancePercent: number
}

/** Matches backend PendingTasksPagedResultDto */
export interface TaskCenterPagedResult {
  items: TaskItem[]
  totalCount: number
  statistics: TaskCenterStats | null
}

/** Filter parameters for the task center query */
export interface TaskCenterFilters {
  type?: string
  priority?: string
  slaStatus?: string
  source?: string
  search?: string
  sortBy?: string
}

const BASE_URL = '/v1/tasks'

/* ──────────────────────────────────────────────
 * Queries
 * ──────────────────────────────────────────── */

/** Fetches paginated task center data with optional filters. */
export async function fetchTasks(
  page = 1,
  pageSize = 20,
  filters?: TaskCenterFilters,
): Promise<TaskCenterPagedResult> {
  const params = new URLSearchParams()
  params.append('page', String(page))
  params.append('pageSize', String(pageSize))

  if (filters?.type && filters.type !== 'all') params.append('type', filters.type)
  if (filters?.priority) params.append('priority', filters.priority)
  if (filters?.slaStatus) params.append('slaStatus', filters.slaStatus)
  if (filters?.source) params.append('source', filters.source)
  if (filters?.search) params.append('search', filters.search)
  if (filters?.sortBy) params.append('sortBy', filters.sortBy)

  return httpGet<TaskCenterPagedResult>(`${BASE_URL}/pending?${params.toString()}`)
}

/** Fetches task center statistics. */
export async function fetchTaskStatistics(): Promise<TaskCenterStats> {
  return httpGet<TaskCenterStats>(`${BASE_URL}/statistics`)
}
