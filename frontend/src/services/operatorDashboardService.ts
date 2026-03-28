/**
 * Operator Dashboard API Service.
 *
 * Provides typed HTTP methods for the Super Admin operator dashboard.
 * All data is fetched dynamically from the backend — NO mock data.
 *
 * Endpoints:
 *   GET /v1/operator/dashboard/summary        → OperatorDashboardSummaryDto
 *   GET /v1/operator/dashboard/tenant-usage    → PagedResult<TenantUsageStatisticsDto>
 *   GET /v1/operator/dashboard/system-health   → SystemHealthStatusDto
 *   GET /v1/operator/dashboard/resource-trends → ResourceConsumptionTrendsDto
 */
import { httpGet } from '@/services/http'
import type {
  OperatorDashboardSummaryDto,
  TenantUsageStatisticsDto,
  SystemHealthStatusDto,
  ResourceConsumptionTrendsDto,
  PagedResult,
  TenantUsageParams,
} from '@/types/operatorDashboard'

const BASE_URL = '/v1/operator/dashboard'

/* ------------------------------------------------------------------ */
/*  Dashboard Summary                                                  */
/* ------------------------------------------------------------------ */

/**
 * Fetches the aggregated operator dashboard summary with KPIs.
 */
export async function fetchDashboardSummary(): Promise<OperatorDashboardSummaryDto> {
  return httpGet<OperatorDashboardSummaryDto>(`${BASE_URL}/summary`)
}

/* ------------------------------------------------------------------ */
/*  Tenant Usage Statistics                                            */
/* ------------------------------------------------------------------ */

/**
 * Fetches per-tenant usage statistics with pagination and optional search.
 */
export async function fetchTenantUsageStatistics(
  params: TenantUsageParams = {},
): Promise<PagedResult<TenantUsageStatisticsDto>> {
  const queryParams = new URLSearchParams()
  if (params.page) queryParams.set('page', String(params.page))
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize))
  if (params.search) queryParams.set('search', params.search)

  const queryString = queryParams.toString()
  const url = queryString ? `${BASE_URL}/tenant-usage?${queryString}` : `${BASE_URL}/tenant-usage`
  return httpGet<PagedResult<TenantUsageStatisticsDto>>(url)
}

/* ------------------------------------------------------------------ */
/*  System Health                                                      */
/* ------------------------------------------------------------------ */

/**
 * Fetches the current system health status of all infrastructure services.
 */
export async function fetchSystemHealthStatus(): Promise<SystemHealthStatusDto> {
  return httpGet<SystemHealthStatusDto>(`${BASE_URL}/system-health`)
}

/* ------------------------------------------------------------------ */
/*  Resource Consumption Trends                                        */
/* ------------------------------------------------------------------ */

/**
 * Fetches resource consumption trends and analytics data.
 */
export async function fetchResourceConsumptionTrends(): Promise<ResourceConsumptionTrendsDto> {
  return httpGet<ResourceConsumptionTrendsDto>(`${BASE_URL}/resource-trends`)
}
