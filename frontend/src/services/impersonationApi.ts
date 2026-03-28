/**
 * Impersonation API service.
 *
 * Wraps all impersonation-related HTTP calls to the backend.
 * Endpoints aligned with ImpersonationEndpoints.cs (/api/v1/impersonation/*).
 * All endpoints require SuperAdmin or SupportAdmin role.
 */
import { httpGet, httpPost } from './http'
import type {
  ImpersonationConsentDto,
  ImpersonationSessionDto,
  ImpersonationStartResponse,
  UserSearchResultDto,
  PaginatedResponse,
  RequestConsentPayload,
  RejectConsentPayload,
  StartImpersonationPayload,
} from '@/types/impersonation'

const BASE = '/v1/impersonation'

/* ------------------------------------------------------------------ */
/*  Consent Management                                                 */
/* ------------------------------------------------------------------ */

/**
 * Request consent to impersonate a target user.
 */
export async function requestConsent(
  payload: RequestConsentPayload,
): Promise<ImpersonationConsentDto> {
  return httpPost<ImpersonationConsentDto>(`${BASE}/consents`, payload)
}

/**
 * Get impersonation consent requests with optional filters.
 */
export async function getConsents(params: {
  status?: string
  requestedByUserId?: string
  targetUserId?: string
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<ImpersonationConsentDto>> {
  return httpGet<PaginatedResponse<ImpersonationConsentDto>>(
    `${BASE}/consents`,
    { params },
  )
}

/**
 * Approve a pending impersonation consent request.
 */
export async function approveConsent(
  consentId: string,
): Promise<ImpersonationConsentDto> {
  return httpPost<ImpersonationConsentDto>(
    `${BASE}/consents/${consentId}/approve`,
  )
}

/**
 * Reject a pending impersonation consent request.
 */
export async function rejectConsent(
  consentId: string,
  payload: RejectConsentPayload,
): Promise<ImpersonationConsentDto> {
  return httpPost<ImpersonationConsentDto>(
    `${BASE}/consents/${consentId}/reject`,
    payload,
  )
}

/* ------------------------------------------------------------------ */
/*  Session Management                                                 */
/* ------------------------------------------------------------------ */

/**
 * Start an impersonation session using an approved consent.
 */
export async function startImpersonation(
  payload: StartImpersonationPayload,
): Promise<ImpersonationStartResponse> {
  return httpPost<ImpersonationStartResponse>(
    `${BASE}/sessions/start`,
    payload,
  )
}

/**
 * End an active impersonation session.
 */
export async function endImpersonation(sessionId: string): Promise<void> {
  await httpPost<void>(`${BASE}/sessions/${sessionId}/end`)
}

/**
 * Get impersonation sessions with optional filters.
 */
export async function getSessions(params: {
  adminUserId?: string
  targetUserId?: string
  targetTenantId?: string
  status?: string
  fromUtc?: string
  toUtc?: string
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<ImpersonationSessionDto>> {
  return httpGet<PaginatedResponse<ImpersonationSessionDto>>(
    `${BASE}/sessions`,
    { params },
  )
}

/* ------------------------------------------------------------------ */
/*  User Search                                                        */
/* ------------------------------------------------------------------ */

/**
 * Search users across all tenants for impersonation.
 */
export async function searchUsers(params: {
  searchTerm?: string
  tenantId?: string
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<UserSearchResultDto>> {
  return httpGet<PaginatedResponse<UserSearchResultDto>>(
    `${BASE}/users/search`,
    { params },
  )
}
