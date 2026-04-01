/**
 * Inquiry API service for Tendex AI Platform.
 * Matches backend InquiryEndpoints at /api/v1/inquiries.
 * NO mock data — all data comes from the API.
 */
import { httpGet, httpPost, httpPut } from './http'
import type {
  Inquiry,
  InquiryPagedResult,
  InquiryStats,
  InquiryFilters,
  AiAnswerResponse,
  CreateInquiryRequest,
  UpdateInquiryRequest,
  AssignInquiryRequest,
  SubmitAnswerRequest,
  BulkImportRequest,
  GenerateAiAnswerRequest,
} from '@/types/inquiry'

const BASE_URL = '/v1/inquiries'

/* ──────────────────────────────────────────────
 * Queries
 * ──────────────────────────────────────────── */

/** Fetches paginated list of inquiries with optional filters. */
export async function fetchInquiries(
  page = 1,
  pageSize = 10,
  filters?: InquiryFilters,
): Promise<InquiryPagedResult> {
  const params = new URLSearchParams()
  params.append('page', String(page))
  params.append('pageSize', String(pageSize))
  if (filters?.status) params.append('status', filters.status)
  if (filters?.category) params.append('category', filters.category)
  if (filters?.priority) params.append('priority', filters.priority)
  if (filters?.competitionId) params.append('competitionId', filters.competitionId)
  if (filters?.search) params.append('search', filters.search)

  return httpGet<InquiryPagedResult>(`${BASE_URL}?${params.toString()}`)
}

/** Fetches inquiry statistics summary. */
export async function fetchInquiryStats(): Promise<InquiryStats> {
  return httpGet<InquiryStats>(`${BASE_URL}/statistics`)
}

/** Fetches a single inquiry by ID. */
export async function fetchInquiryById(id: string): Promise<Inquiry> {
  return httpGet<Inquiry>(`${BASE_URL}/${id}`)
}

/* ──────────────────────────────────────────────
 * Commands
 * ──────────────────────────────────────────── */

/** Creates a new inquiry. */
export async function createInquiry(data: CreateInquiryRequest): Promise<Inquiry> {
  return httpPost<Inquiry>(BASE_URL, data)
}

/** Updates an existing inquiry. */
export async function updateInquiry(id: string, data: UpdateInquiryRequest): Promise<void> {
  await httpPut(`${BASE_URL}/${id}`, data)
}

/** Assigns an inquiry to a user or committee. */
export async function assignInquiry(id: string, data: AssignInquiryRequest): Promise<void> {
  await httpPost(`${BASE_URL}/${id}/assign`, data)
}

/** Submits an answer for approval. */
export async function submitAnswer(id: string, data: SubmitAnswerRequest): Promise<void> {
  await httpPost(`${BASE_URL}/${id}/submit-answer`, data)
}

/** Approves an inquiry answer. */
export async function approveInquiry(id: string): Promise<void> {
  await httpPost(`${BASE_URL}/${id}/approve`, {})
}

/** Rejects an inquiry answer with reason. */
export async function rejectInquiry(id: string, reason: string): Promise<void> {
  await httpPost(`${BASE_URL}/${id}/reject`, { reason })
}

/** Closes an inquiry. */
export async function closeInquiry(id: string, reason?: string): Promise<void> {
  await httpPost(`${BASE_URL}/${id}/close`, { reason })
}

/** Generates an AI-powered answer for an inquiry. */
export async function generateAiAnswer(
  id: string,
  data: GenerateAiAnswerRequest,
): Promise<AiAnswerResponse> {
  return httpPost<AiAnswerResponse>(`${BASE_URL}/${id}/generate-ai-answer`, data)
}

/** Bulk imports inquiries from Etimad. */
export async function bulkImportInquiries(data: BulkImportRequest): Promise<{ importedCount: number }> {
  return httpPost<{ importedCount: number }>(`${BASE_URL}/bulk-import`, data)
}
