/**
 * Approval / Task Center API Service.
 *
 * Provides methods to interact with the backend Task APIs (TASK-902).
 * Maps to TaskEndpoints.cs — /api/v1/tasks.
 *
 * All data is fetched dynamically — no mock/hardcoded arrays allowed.
 */
import { httpGet } from '@/services/http'
import type {
  PendingTasksPagedResult,
  PendingTasksParams,
} from '@/types/committee'

const BASE_URL = '/v1/tasks'

/* ------------------------------------------------------------------ */
/*  Pending Tasks                                                      */
/* ------------------------------------------------------------------ */

/**
 * Fetch paginated pending tasks for the current user.
 */
export async function fetchPendingTasks(
  params: PendingTasksParams = {},
): Promise<PendingTasksPagedResult> {
  const query = new URLSearchParams()

  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  if (params.type) query.set('type', params.type)
  if (params.priority) query.set('priority', params.priority)

  const queryString = query.toString()
  const url = queryString ? `${BASE_URL}/pending?${queryString}` : `${BASE_URL}/pending`

  return httpGet<PendingTasksPagedResult>(url)
}
