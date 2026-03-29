/**
 * Inquiry type definitions for Tendex AI Platform.
 * Covers inquiries on specification booklets (RFPs).
 */

/* ──────────────────────────────────────────────
 * Inquiry Status
 * ──────────────────────────────────────────── */
export type InquiryStatus = 'pending' | 'answered' | 'closed' | 'escalated'

export type InquiryPriority = 'low' | 'medium' | 'high' | 'critical'

/* ──────────────────────────────────────────────
 * Inquiry Item
 * ──────────────────────────────────────────── */
export interface Inquiry {
  id: string
  referenceNumber: string
  competitionId: string
  competitionTitleAr: string
  competitionTitleEn: string
  competitionReferenceNumber: string
  subjectAr: string
  subjectEn: string
  bodyAr: string
  bodyEn: string
  status: InquiryStatus
  priority: InquiryPriority
  submittedByNameAr: string
  submittedByNameEn: string
  submittedAt: string
  answeredAt: string | null
  answerAr: string | null
  answerEn: string | null
  answeredByNameAr: string | null
  answeredByNameEn: string | null
  slaDeadline: string | null
  isOverdue: boolean
}

/* ──────────────────────────────────────────────
 * Inquiry Filters
 * ──────────────────────────────────────────── */
export interface InquiryFilters {
  status: InquiryStatus | ''
  priority: InquiryPriority | ''
  competitionId: string | ''
  search: string
}

/* ──────────────────────────────────────────────
 * Inquiry Stats
 * ──────────────────────────────────────────── */
export interface InquiryStats {
  total: number
  pending: number
  answered: number
  overdue: number
  averageResponseTimeHours: number
}

/* ──────────────────────────────────────────────
 * Paged Result
 * ──────────────────────────────────────────── */
export interface InquiryPagedResult {
  items: Inquiry[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
