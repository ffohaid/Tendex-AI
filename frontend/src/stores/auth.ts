/**
 * Authentication Pinia store.
 *
 * Manages user authentication state including:
 * - Login / Logout flow
 * - MFA verification flow
 * - Token persistence in localStorage
 * - User info
 */
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as authService from '@/services/authService'
import type {
  LoginRequest,
  UserInfo,
  AuthTokenResponse,
  ApiProblemDetails,
} from '@/types/auth'
import { isAxiosError } from 'axios'

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

const STORAGE_KEYS = {
  ACCESS_TOKEN: 'access_token',
  REFRESH_TOKEN: 'refresh_token',
  TENANT_ID: 'tenant_id',
  BASE_TENANT_ID: 'base_tenant_id',
  USER: 'user',
  SESSION_ID: 'session_id',
} as const

/* ------------------------------------------------------------------ */
/*  Store                                                              */
/* ------------------------------------------------------------------ */

export const useAuthStore = defineStore('auth', () => {
  /* ---- State ---- */
  const accessToken = ref<string | null>(
    localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN),
  )
  const refreshTokenValue = ref<string | null>(
    localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN),
  )
  const user = ref<UserInfo | null>(
    (() => {
      try {
        const raw = localStorage.getItem(STORAGE_KEYS.USER)
        return raw ? (JSON.parse(raw) as UserInfo) : null
      } catch {
        return null
      }
    })(),
  )
  const tenantId = ref<string>(
    localStorage.getItem(STORAGE_KEYS.TENANT_ID) || '',
  )
  const mfaSessionId = ref<string | null>(null)
  const mfaRequired = ref(false)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  /* ---- Getters ---- */
  const isAuthenticated = computed(
    () => !!accessToken.value && !!user.value,
  )
  const fullName = computed(() =>
    user.value
      ? `${user.value.firstName} ${user.value.lastName}`.trim()
      : '',
  )
  const userRoles = computed(() => user.value?.roles ?? [])
  const userPermissions = computed(() => user.value?.permissions ?? [])

  /* ---- Helpers ---- */
  function persistTokens(response: AuthTokenResponse): void {
    accessToken.value = response.accessToken
    refreshTokenValue.value = response.refreshToken
    user.value = response.user
    tenantId.value = response.user.tenantId

    localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, response.accessToken)
    localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, response.refreshToken)
    localStorage.setItem(STORAGE_KEYS.TENANT_ID, response.user.tenantId)
    localStorage.setItem(STORAGE_KEYS.BASE_TENANT_ID, response.user.tenantId)
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(response.user))
  }

  function clearAuthState(): void {
    accessToken.value = null
    refreshTokenValue.value = null
    user.value = null
    mfaSessionId.value = null
    mfaRequired.value = false

    Object.values(STORAGE_KEYS).forEach((key) =>
      localStorage.removeItem(key),
    )

    // Clear tenant branding from sessionStorage on logout
    sessionStorage.removeItem('tenant_branding')
  }

  function extractErrorMessage(err: unknown): string {
    if (isAxiosError(err)) {
      // Prioritize translated i18n keys over raw backend messages
      if (err.response?.status === 401) return 'auth.errors.invalidCredentials'
      if (err.response?.status === 429) return 'auth.errors.tooManyAttempts'
      if (err.response?.status === 403) return 'auth.errors.accountDeactivated'
      if (!err.response) return 'auth.errors.networkError'
      // Fallback to backend detail only if no i18n key matches
      const problem = err.response?.data as ApiProblemDetails | undefined
      if (problem?.detail) return problem.detail
    }
    return 'auth.errors.unexpectedError'
  }

  /* ---- Actions ---- */

  /**
   * Login with email and password.
   * Returns true if login succeeded (or MFA is required).
   */
  async function loginAction(
    payload: LoginRequest,
  ): Promise<boolean> {
    isLoading.value = true
    error.value = null

    try {
      const response = await authService.login(payload)

      if (response.mfaRequired) {
        mfaRequired.value = true
        mfaSessionId.value = response.sessionId
        tenantId.value = payload.tenantId
        localStorage.setItem(STORAGE_KEYS.TENANT_ID, payload.tenantId)
        localStorage.setItem(STORAGE_KEYS.BASE_TENANT_ID, payload.tenantId)
        localStorage.setItem(STORAGE_KEYS.SESSION_ID, response.sessionId)
        return true
      }

      persistTokens(response)
      mfaRequired.value = false
      return true
    } catch (err) {
      error.value = extractErrorMessage(err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Verify MFA code after login.
   * Returns true if verification succeeded.
   */
  async function verifyMfaAction(code: string): Promise<boolean> {
    if (!mfaSessionId.value || !tenantId.value) {
      error.value = 'auth.errors.sessionExpired'
      return false
    }

    isLoading.value = true
    error.value = null

    try {
      const response = await authService.verifyMfa({
        sessionId: mfaSessionId.value,
        code,
        tenantId: tenantId.value,
      })

      if (response.success && response.tokenResponse) {
        persistTokens(response.tokenResponse)
        mfaRequired.value = false
        mfaSessionId.value = null
        localStorage.removeItem(STORAGE_KEYS.SESSION_ID)
        return true
      }

      error.value = 'auth.errors.invalidMfaCode'
      return false
    } catch (err) {
      error.value = extractErrorMessage(err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Logout and clear all auth state.
   */
  async function logoutAction(): Promise<void> {
    try {
      if (accessToken.value) {
        await authService.logout({
          refreshToken: refreshTokenValue.value ?? undefined,
          sessionId: mfaSessionId.value ?? undefined,
        })
      }
    } catch {
      /* Silently ignore logout API errors */
    } finally {
      clearAuthState()
    }
  }

  /**
   * Set the tenant ID (used on login form).
   */
  function setTenantId(id: string): void {
    tenantId.value = id
    localStorage.setItem(STORAGE_KEYS.TENANT_ID, id)
  }

  /**
   * Check if user has a specific role.
   */
  function hasRole(role: string): boolean {
    return userRoles.value.includes(role)
  }

  /**
   * Check if user has a specific permission.
   */
  function hasPermission(permission: string): boolean {
    return userPermissions.value.includes(permission)
  }

  return {
    /* State */
    accessToken,
    refreshTokenValue,
    user,
    tenantId,
    mfaSessionId,
    mfaRequired,
    isLoading,
    error,

    /* Getters */
    isAuthenticated,
    fullName,
    userRoles,
    userPermissions,

    /* Actions */
    loginAction,
    verifyMfaAction,
    logoutAction,
    setTenantId,
    hasRole,
    hasPermission,
    clearAuthState,
  }
})
