/**
 * Inquiry type definitions for Tendex AI Platform.
 * Matches backend DTOs from TendexAI.Application.Features.Inquiries.Dtos.
 */

/* ──────────────────────────────────────────────
 * Enums (match backend InquiryStatus, InquiryCategory, InquiryPriority)
 * ──────────────────────────────────────────── */
export type InquiryStatus = 'New' | 'InProgress' | 'PendingApproval' | 'Approved' | 'Rejected' | 'Closed'
export type InquiryCategory = 'General' | 'Technical' | 'Financial' | 'Administrative' | 'Legal'
export type InquiryPriority = 'Low' | 'Medium' | 'High' | 'Critical'

/* ──────────────────────────────────────────────
 * Inquiry Response (AI or manual draft)
 * ──────────────────────────────────────────── */
export interface InquiryResponse {
  id: string
  answerText: string
  isAiGenerated: boolean
  aiConfidenceScore: number | null
  aiModelUsed: string | null
  aiSources: string | null
  isSelected: boolean
  createdAt: string
  createdBy: string | null
}

/* ──────────────────────────────────────────────
 * Inquiry Item (matches InquiryDto)
 * ──────────────────────────────────────────── */
export interface Inquiry {
  id: string
  competitionId: string
  competitionName: string | null
  referenceNumber: string
  questionText: string
  category: InquiryCategory
  priority: InquiryPriority
  status: InquiryStatus
  supplierName: string | null
  etimadReferenceNumber: string | null
  approvedAnswer: string | null
  assignedToUserId: string | null
  assignedToUserName: string | null
  assignedToCommitteeId: string | null
  slaDeadline: string | null
  isOverdue: boolean
  answeredAt: string | null
  answeredBy: string | null
  approvedAt: string | null
  approvedBy: string | null
  rejectionReason: string | null
  isAiAssisted: boolean
  internalNotes: string | null
  isExportedToEtimad: boolean
  exportedToEtimadAt: string | null
  createdAt: string
  createdBy: string | null
  responses: InquiryResponse[]
}

/* ──────────────────────────────────────────────
 * Inquiry Filters
 * ──────────────────────────────────────────── */
export interface InquiryFilters {
  status: InquiryStatus | ''
  category: InquiryCategory | ''
  priority: InquiryPriority | ''
  competitionId: string
  search: string
}

/* ──────────────────────────────────────────────
 * Inquiry Stats (matches InquiryStatisticsDto)
 * ──────────────────────────────────────────── */
export interface InquiryStats {
  total: number
  new: number
  inProgress: number
  pendingApproval: number
  approved: number
  rejected: number
  overdue: number
  averageResponseTimeHours: number
}

/* ──────────────────────────────────────────────
 * Paged Result (matches InquiryPagedResultDto)
 * ──────────────────────────────────────────── */
export interface InquiryPagedResult {
  items: Inquiry[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

/* ──────────────────────────────────────────────
 * AI Answer Response (matches AiAnswerResponseDto)
 * ──────────────────────────────────────────── */
export interface AiAnswerResponse {
  answerText: string
  confidenceScore: number
  modelUsed: string
  sources: string | null
  responseId: string
}

/* ──────────────────────────────────────────────
 * Request DTOs
 * ──────────────────────────────────────────── */
export interface CreateInquiryRequest {
  competitionId: string
  questionText: string
  category: string
  priority: string
  supplierName?: string
  etimadReferenceNumber?: string
  internalNotes?: string
}

export interface UpdateInquiryRequest {
  questionText: string
  category: string
  priority: string
  supplierName?: string
  internalNotes?: string
}

export interface AssignInquiryRequest {
  userId?: string
  userName?: string
  committeeId?: string
}

export interface SubmitAnswerRequest {
  answerText: string
  isAiAssisted: boolean
}

export interface BulkImportItem {
  questionText: string
  supplierName?: string
  etimadReferenceNumber?: string
  category: string
  priority: string
}

export interface BulkImportRequest {
  competitionId: string
  inquiries: BulkImportItem[]
}

export interface GenerateAiAnswerRequest {
  additionalContext?: string
  useRag: boolean
}
