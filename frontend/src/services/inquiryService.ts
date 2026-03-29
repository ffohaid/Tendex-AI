/**
 * Inquiry API service for Tendex AI Platform.
 * Fetches inquiry data dynamically from the backend API.
 * NO mock data — all data comes from the API.
 */
import { httpGet, httpPost, httpPut } from './http'
import type {
  Inquiry,
  InquiryPagedResult,
  InquiryStats,
  InquiryFilters,
} from '@/types/inquiry'

const BASE_URL = '/v1/inquiries'

/**
 * Fetches paginated list of inquiries with optional filters.
 */
export async function fetchInquiries(
  page = 1,
  pageSize = 10,
  filters?: InquiryFilters,
): Promise<InquiryPagedResult> {
  const params = new URLSearchParams()
  params.append('page', String(page))
  params.append('pageSize', String(pageSize))
  if (filters?.status) params.append('status', filters.status)
  if (filters?.priority) params.append('priority', filters.priority)
  if (filters?.competitionId) params.append('competitionId', filters.competitionId)
  if (filters?.search) params.append('search', filters.search)

  return httpGet<InquiryPagedResult>(`${BASE_URL}?${params.toString()}`)
}

/**
 * Fetches inquiry statistics summary.
 */
export async function fetchInquiryStats(): Promise<InquiryStats> {
  return httpGet<InquiryStats>(`${BASE_URL}/stats`)
}

/**
 * Fetches a single inquiry by ID.
 */
export async function fetchInquiryById(id: string): Promise<Inquiry> {
  return httpGet<Inquiry>(`${BASE_URL}/${id}`)
}

/**
 * Submits an answer for an inquiry.
 */
export async function answerInquiry(
  id: string,
  answerAr: string,
  answerEn: string,
): Promise<void> {
  await httpPut(`${BASE_URL}/${id}/answer`, { answerAr, answerEn })
}

/**
 * Creates a new inquiry.
 */
export async function createInquiry(data: {
  competitionId: string
  subjectAr: string
  subjectEn: string
  bodyAr: string
  bodyEn: string
  priority: string
}): Promise<Inquiry> {
  return httpPost<Inquiry>(BASE_URL, data)
}
