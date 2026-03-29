/**
 * Report API service for Tendex AI Platform.
 * Fetches report data dynamically from the backend API.
 * NO mock data — all data comes from the API.
 */
import { httpGet } from './http'
import type { ReportData, ReportFilters } from '@/types/reports'

const BASE_URL = '/v1/reports'

/**
 * Fetches the full report data including summary, trends, and distributions.
 */
export async function fetchReportData(filters?: ReportFilters): Promise<ReportData> {
  const params = new URLSearchParams()
  if (filters?.dateFrom) params.append('dateFrom', filters.dateFrom)
  if (filters?.dateTo) params.append('dateTo', filters.dateTo)
  if (filters?.status) params.append('status', filters.status)
  if (filters?.department) params.append('department', filters.department)

  const queryString = params.toString()
  const url = queryString ? `${BASE_URL}?${queryString}` : BASE_URL

  return httpGet<ReportData>(url)
}
