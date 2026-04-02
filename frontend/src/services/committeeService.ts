/**
 * Committee API Service.
 *
 * Provides methods to interact with the backend Committee APIs (TASK-902).
 * Maps to CommitteeEndpoints.cs — /api/v1/committees.
 *
 * All data is fetched dynamically — no mock/hardcoded arrays allowed.
 */
import { httpGet, httpPost, httpPut, httpDelete } from '@/services/http'
import type {
  CommitteePagedResult,
  CommitteeDetail,
  CommitteeListParams,
  CreateCommitteeRequest,
  UpdateCommitteeRequest,
  ChangeCommitteeStatusRequest,
  AddCommitteeMemberRequest,
  ConflictOfInterestResult,
  CommitteeStatistics,
  CommitteeAiAnalysisResponse,
} from '@/types/committee'
import { type CommitteeMemberRole } from '@/types/committee'

const BASE_URL = '/v1/committees'

/* ------------------------------------------------------------------ */
/*  Committee CRUD                                                     */
/* ------------------------------------------------------------------ */

/**
 * Fetch paginated list of committees with optional filters.
 */
export async function fetchCommittees(
  params: CommitteeListParams = {},
): Promise<CommitteePagedResult> {
  const query = new URLSearchParams()

  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  if (params.type !== undefined) query.set('type', String(params.type))
  if (params.status !== undefined) query.set('status', String(params.status))
  if (params.isPermanent !== undefined) query.set('isPermanent', String(params.isPermanent))
  if (params.competitionId) query.set('competitionId', params.competitionId)
  if (params.search) query.set('search', params.search)

  const queryString = query.toString()
  const url = queryString ? `${BASE_URL}?${queryString}` : BASE_URL

  return httpGet<CommitteePagedResult>(url)
}

/**
 * Fetch a single committee by ID with all members.
 */
export async function fetchCommitteeById(
  committeeId: string,
): Promise<CommitteeDetail> {
  return httpGet<CommitteeDetail>(`${BASE_URL}/${committeeId}`)
}

/**
 * Create a new committee (permanent or temporary).
 * Returns the new committee ID.
 */
export async function createCommittee(
  data: CreateCommitteeRequest,
): Promise<string> {
  return httpPost<string>(BASE_URL, data)
}

/**
 * Update committee basic information.
 */
export async function updateCommittee(
  committeeId: string,
  data: UpdateCommitteeRequest,
): Promise<void> {
  return httpPut<void>(`${BASE_URL}/${committeeId}`, data)
}

/**
 * Change committee lifecycle status (Suspend, Reactivate, Dissolve).
 */
export async function changeCommitteeStatus(
  committeeId: string,
  data: ChangeCommitteeStatusRequest,
): Promise<void> {
  return httpPut<void>(`${BASE_URL}/${committeeId}/status`, data)
}

/* ------------------------------------------------------------------ */
/*  Member Management                                                  */
/* ------------------------------------------------------------------ */

/**
 * Add a member to a committee with conflict of interest validation.
 */
export async function addCommitteeMember(
  committeeId: string,
  data: AddCommitteeMemberRequest,
): Promise<void> {
  return httpPost<void>(`${BASE_URL}/${committeeId}/members`, data)
}

/**
 * Remove (deactivate) a member from a committee.
 */
export async function removeCommitteeMember(
  committeeId: string,
  userId: string,
  reason: string,
): Promise<void> {
  return httpDelete<void>(
    `${BASE_URL}/${committeeId}/members/${userId}?reason=${encodeURIComponent(reason)}`,
  )
}

/* ------------------------------------------------------------------ */
/*  Conflict of Interest Validation                                    */
/* ------------------------------------------------------------------ */

/**
 * Check if adding a user to a committee would violate conflict of interest rules.
 */
export async function validateConflictOfInterest(
  committeeId: string,
  userId: string,
  role: CommitteeMemberRole,
): Promise<ConflictOfInterestResult> {
  return httpGet<ConflictOfInterestResult>(
    `${BASE_URL}/${committeeId}/conflict-check/${userId}?role=${role}`,
  )
}

/* ------------------------------------------------------------------ */
/*  Competition-Scoped Queries                                         */
/* ------------------------------------------------------------------ */

/**
 * Get all committees linked to a specific competition.
 */
export async function fetchCompetitionCommittees(
  competitionId: string,
): Promise<CommitteeDetail[]> {
  return httpGet<CommitteeDetail[]>(
    `/v1/competitions/${competitionId}/committees`,
  )
}

/* ------------------------------------------------------------------ */
/*  Statistics & AI Analysis                                           */
/* ------------------------------------------------------------------ */

/**
 * Fetch committee statistics for the current tenant.
 */
export async function fetchCommitteeStatistics(
  isPermanent?: boolean,
): Promise<CommitteeStatistics> {
  const query = new URLSearchParams()
  if (isPermanent !== undefined) query.set('isPermanent', String(isPermanent))
  const queryString = query.toString()
  const url = queryString
    ? `${BASE_URL}/statistics?${queryString}`
    : `${BASE_URL}/statistics`
  return httpGet<CommitteeStatistics>(url)
}

/**
 * Fetch AI-powered analysis and recommendations for a committee.
 */
export async function fetchCommitteeAiAnalysis(
  committeeId: string,
): Promise<CommitteeAiAnalysisResponse> {
  return httpGet<CommitteeAiAnalysisResponse>(
    `${BASE_URL}/${committeeId}/ai-analysis`,
  )
}
