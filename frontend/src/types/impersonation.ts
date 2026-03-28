/**
 * Impersonation feature TypeScript types and interfaces.
 * Aligned with backend DTOs from TendexAI.Application.Features.Impersonation.
 */

/* ------------------------------------------------------------------ */
/*  Consent Types                                                      */
/* ------------------------------------------------------------------ */

export type ConsentStatus = 'Pending' | 'Approved' | 'Rejected' | 'Expired'

export interface ImpersonationConsentDto {
  id: string
  requestedByUserId: string
  requestedByUserName: string
  targetUserId: string
  targetUserName: string
  targetEmail: string
  targetTenantId: string
  reason: string
  ticketReference: string | null
  requestedAtUtc: string
  approvedByUserId: string | null
  approvedByUserName: string | null
  resolvedAtUtc: string | null
  status: ConsentStatus
  rejectionReason: string | null
  expiresAtUtc: string | null
}

export interface RequestConsentPayload {
  targetUserId: string
  reason: string
  ticketReference?: string
}

export interface RejectConsentPayload {
  rejectionReason: string
}

/* ------------------------------------------------------------------ */
/*  Session Types                                                      */
/* ------------------------------------------------------------------ */

export type ImpersonationStatus = 'Active' | 'Ended' | 'Expired'

export interface ImpersonationSessionDto {
  id: string
  adminUserId: string
  adminUserName: string
  adminEmail: string
  targetUserId: string
  targetUserName: string
  targetEmail: string
  targetTenantId: string
  targetTenantName: string
  reason: string
  ticketReference: string | null
  consentReference: string | null
  ipAddress: string
  startedAtUtc: string
  endedAtUtc: string | null
  status: ImpersonationStatus
}

export interface StartImpersonationPayload {
  consentId: string
}

export interface ImpersonationStartResponse {
  accessToken: string
  sessionId: string
  impersonationSessionId: string
  targetUser: UserImpersonationInfo
}

export interface UserImpersonationInfo {
  id: string
  email: string
  firstName: string
  lastName: string
  tenantId: string
  roles: string[]
  permissions: string[]
}

/* ------------------------------------------------------------------ */
/*  User Search Types                                                  */
/* ------------------------------------------------------------------ */

export interface UserSearchResultDto {
  id: string
  email: string
  firstName: string
  lastName: string
  tenantId: string
  tenantName: string | null
  isActive: boolean
  lastLoginAt: string | null
}

/* ------------------------------------------------------------------ */
/*  Paginated Response                                                 */
/* ------------------------------------------------------------------ */

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}
