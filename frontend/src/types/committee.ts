/**
 * Committee & Approval type definitions.
 *
 * Maps to backend DTOs defined in:
 * - TendexAI.Application.Features.Committees.Dtos
 * - TendexAI.Application.Features.Dashboard.Dtos
 * - TendexAI.Domain.Enums
 *
 * TASK-902: Committees & Approvals frontend pages.
 *
 * NOTE: Uses `as const` objects instead of `enum` to comply with
 * TypeScript `erasableSyntaxOnly` compiler option.
 */

/* ------------------------------------------------------------------ */
/*  Enums (mirror backend Domain.Enums)                                */
/* ------------------------------------------------------------------ */

export const CommitteeType = {
  TechnicalEvaluation: 1,
  FinancialEvaluation: 2,
  BookletPreparation: 3,
  InquiryReview: 4,
  OtherPermanent: 5,
} as const
export type CommitteeType = (typeof CommitteeType)[keyof typeof CommitteeType]

export const CommitteeStatus = {
  Active: 1,
  Suspended: 2,
  Dissolved: 3,
  Expired: 4,
} as const
export type CommitteeStatus = (typeof CommitteeStatus)[keyof typeof CommitteeStatus]

export const CommitteeMemberRole = {
  Chair: 1,
  Member: 2,
  Secretary: 3,
} as const
export type CommitteeMemberRole = (typeof CommitteeMemberRole)[keyof typeof CommitteeMemberRole]

export const CommitteeScopeType = {
  Comprehensive: 1,
  SpecificPhasesAllCompetitions: 2,
  SpecificPhasesSpecificCompetitions: 3,
} as const
export type CommitteeScopeType = (typeof CommitteeScopeType)[keyof typeof CommitteeScopeType]

export const CompetitionPhase = {
  BookletPreparation: 1,
  BookletApproval: 2,
  BookletPublishing: 3,
  OfferReception: 4,
  TechnicalAnalysis: 5,
  FinancialAnalysis: 6,
  AwardNotification: 7,
  ContractApproval: 8,
  ContractSigning: 9,
} as const
export type CompetitionPhase = (typeof CompetitionPhase)[keyof typeof CompetitionPhase]

/* ------------------------------------------------------------------ */
/*  Committee DTOs                                                     */
/* ------------------------------------------------------------------ */

/** Competition linked to a committee. */
export interface CommitteeCompetition {
  id: string
  competitionId: string
  competitionNameAr: string | null
  competitionNameEn: string | null
  assignedAt: string
}

/** Summary DTO for committee list view. */
export interface CommitteeListItem {
  id: string
  nameAr: string
  nameEn: string
  type: CommitteeType
  isPermanent: boolean
  scopeType: CommitteeScopeType
  phases: CompetitionPhase[]
  status: CommitteeStatus
  activeMemberCount: number
  startDate: string
  endDate: string
  competitions: CommitteeCompetition[]
  createdAt: string
  daysRemaining: number
  workloadScore: number
}

/** Detail DTO for a single committee with members. */
export interface CommitteeDetail {
  id: string
  nameAr: string
  nameEn: string
  type: CommitteeType
  isPermanent: boolean
  scopeType: CommitteeScopeType
  phases: CompetitionPhase[]
  description: string | null
  status: CommitteeStatus
  startDate: string
  endDate: string
  competitions: CommitteeCompetition[]
  statusChangeReason: string | null
  statusChangedBy: string | null
  statusChangedAt: string | null
  members: CommitteeMember[]
  createdAt: string
  createdBy: string | null
  daysRemaining: number
  workloadScore: number
  aiInsight: CommitteeAiInsight | null
}

/** DTO for a committee member. */
export interface CommitteeMember {
  id: string
  userId: string
  userFullName: string
  role: CommitteeMemberRole
  isActive: boolean
  assignedAt: string
  assignedBy: string
  removedAt: string | null
  removedBy: string | null
  removalReason: string | null
}

/** Paginated result for committees. */
export interface CommitteePagedResult {
  items: CommitteeListItem[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

/** Conflict of interest check result. */
export interface ConflictOfInterestResult {
  hasConflict: boolean
  conflictDetails: string | null
}

/** Eligible user that can be added to a committee. */
export interface EligibleUser {
  id: string
  fullName: string
  email: string
  roles: UserRoleSummary[]
}

/** Summary of a user's platform role. */
export interface UserRoleSummary {
  nameAr: string
  nameEn: string
  normalizedName: string
}

/* ------------------------------------------------------------------ */
/*  Statistics & AI DTOs                                               */
/* ------------------------------------------------------------------ */

/** Committee statistics DTO. */
export interface CommitteeStatistics {
  totalCommittees: number
  activeCommittees: number
  suspendedCommittees: number
  dissolvedCommittees: number
  expiredCommittees: number
  totalMembers: number
  totalActiveMembers: number
  averageMembers: number
  committeesExpiringSoon: number
  committeesWithNoChair: number
  typeBreakdown: CommitteeTypeBreakdown[]
}

/** Breakdown by committee type. */
export interface CommitteeTypeBreakdown {
  type: CommitteeType
  count: number
  activeCount: number
}

/** AI-generated insight for a committee. */
export interface CommitteeAiInsight {
  summary: string
  recommendations: string[]
  risks: string[]
  healthScore: number
  healthLabel: string
}

/** AI recommendation for committee composition. */
export interface CommitteeAiRecommendation {
  recommendationType: string
  title: string
  description: string
  impact: string
  confidence: number
}

/** Response for AI committee analysis endpoint. */
export interface CommitteeAiAnalysisResponse {
  insight: CommitteeAiInsight
  recommendations: CommitteeAiRecommendation[]
}

/* ------------------------------------------------------------------ */
/*  Request DTOs                                                       */
/* ------------------------------------------------------------------ */

/** Request body for creating a new committee. */
export interface CreateCommitteeRequest {
  nameAr: string
  nameEn: string
  type: CommitteeType
  isPermanent: boolean
  scopeType: CommitteeScopeType
  description?: string
  startDate: string
  endDate: string
  competitionIds?: string[]
  phases?: CompetitionPhase[]
}

/** Request body for updating committee information. */
export interface UpdateCommitteeRequest {
  nameAr: string
  nameEn: string
  description?: string
  scopeType: CommitteeScopeType
  phases?: CompetitionPhase[]
  competitionIds?: string[]
}

/** Request body for changing committee status. */
export interface ChangeCommitteeStatusRequest {
  newStatus: CommitteeStatus
  reason?: string
}

/** Request body for adding a registered platform user to a committee. */
export interface AddCommitteeMemberRequest {
  userId: string
  role: CommitteeMemberRole
}

/* ------------------------------------------------------------------ */
/*  Query Parameters                                                   */
/* ------------------------------------------------------------------ */

/** Query parameters for listing committees. */
export interface CommitteeListParams {
  pageNumber?: number
  pageSize?: number
  type?: CommitteeType
  status?: CommitteeStatus
  isPermanent?: boolean
  competitionId?: string
  search?: string
}

/* ------------------------------------------------------------------ */
/*  Pending Task (Approvals)                                           */
/* ------------------------------------------------------------------ */

/** Pending task from the unified task center. */
export interface PendingTask {
  id: string
  type: string
  titleAr: string
  titleEn: string
  competitionTitleAr: string
  competitionTitleEn: string
  competitionReferenceNumber: string
  assignedAt: string
  slaDeadline: string
  slaStatus: string
  remainingTimeSeconds: number
  priority: string
  actionRequired: string
  actionUrl: string
}

/** Paginated result for pending tasks. */
export interface PendingTasksPagedResult {
  items: PendingTask[]
  totalCount: number
}

/** Query parameters for pending tasks. */
export interface PendingTasksParams {
  page?: number
  pageSize?: number
  type?: string
  priority?: string
}
