/**
 * Authentication-related TypeScript types and interfaces.
 * Aligned with backend DTOs from TendexAI.Application.Features.Auth.Dtos.
 */

/* ------------------------------------------------------------------ */
/*  Request Models                                                     */
/* ------------------------------------------------------------------ */

export interface LoginRequest {
  email: string
  password: string
  tenantId: string
}

export interface RefreshTokenRequest {
  refreshToken: string
  tenantId: string
}

export interface LogoutRequest {
  refreshToken?: string
  sessionId?: string
}

export interface VerifyMfaRequest {
  sessionId: string
  code: string
  tenantId: string
}

export interface ForgotPasswordRequest {
  email: string
  tenantId: string
}

export interface ResetPasswordRequest {
  token: string
  email: string
  newPassword: string
  confirmPassword: string
  tenantId: string
}

/* ------------------------------------------------------------------ */
/*  Response Models                                                    */
/* ------------------------------------------------------------------ */

export interface UserInfo {
  id: string
  email: string
  firstName: string
  lastName: string
  tenantId: string
  mfaEnabled: boolean
  roles: string[]
  permissions: string[]
}

export interface AuthTokenResponse {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  sessionId: string
  user: UserInfo
  mfaRequired: boolean
}

export interface MfaVerifyResponse {
  success: boolean
  tokenResponse: AuthTokenResponse | null
}

/* ------------------------------------------------------------------ */
/*  API Error                                                          */
/* ------------------------------------------------------------------ */

export interface ApiProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}
