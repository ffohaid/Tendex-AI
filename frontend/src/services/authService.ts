/**
 * Authentication API service.
 *
 * Wraps all auth-related HTTP calls to the backend.
 * Endpoints aligned with AuthEndpoints.cs (/api/v1/auth/*).
 */
import httpClient from './http'
import type {
  LoginRequest,
  AuthTokenResponse,
  RefreshTokenRequest,
  LogoutRequest,
  VerifyMfaRequest,
  MfaVerifyResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '@/types/auth'

const AUTH_BASE = '/v1/auth'

/**
 * Authenticate user with email and password.
 * If MFA is required, response will have mfaRequired=true and a sessionId.
 *
 * NOTE: Explicitly passes X-Tenant-Id header from the request payload
 * because the global interceptor reads from localStorage which may not
 * be populated yet during the first login after clearing auth state.
 */
export async function login(payload: LoginRequest): Promise<AuthTokenResponse> {
  const { data } = await httpClient.post<AuthTokenResponse>(
    `${AUTH_BASE}/login`,
    payload,
    {
      headers: {
        'X-Tenant-Id': payload.tenantId,
      },
    },
  )
  return data
}

/**
 * Verify MFA code during login flow.
 */
export async function verifyMfa(
  payload: VerifyMfaRequest,
): Promise<MfaVerifyResponse> {
  const { data } = await httpClient.post<MfaVerifyResponse>(
    `${AUTH_BASE}/mfa/verify`,
    payload,
    {
      headers: {
        'X-Tenant-Id': payload.tenantId,
      },
    },
  )
  return data
}

/**
 * Refresh an expired access token.
 */
export async function refreshToken(
  payload: RefreshTokenRequest,
): Promise<AuthTokenResponse> {
  const { data } = await httpClient.post<AuthTokenResponse>(
    `${AUTH_BASE}/refresh-token`,
    payload,
    {
      headers: {
        'X-Tenant-Id': payload.tenantId,
      },
    },
  )
  return data
}

/**
 * Log out and revoke tokens.
 */
export async function logout(payload: LogoutRequest): Promise<void> {
  await httpClient.post(`${AUTH_BASE}/logout`, payload)
}

/**
 * Request a password reset link.
 * Note: Backend endpoint may not be implemented yet.
 * This is a placeholder that will work once the API is available.
 */
export async function forgotPassword(
  payload: ForgotPasswordRequest,
): Promise<void> {
  await httpClient.post(`${AUTH_BASE}/forgot-password`, payload)
}

/**
 * Reset password using a token received via email.
 * Note: Backend endpoint may not be implemented yet.
 */
export async function resetPassword(
  payload: ResetPasswordRequest,
): Promise<void> {
  await httpClient.post(`${AUTH_BASE}/reset-password`, payload)
}
