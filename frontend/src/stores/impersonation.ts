/**
 * Impersonation Pinia Store.
 *
 * Manages the state for user impersonation feature in the Super Admin panel.
 * Handles consent requests, session management, and user search.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as impersonationApi from '@/services/impersonationApi'
import type {
  ImpersonationConsentDto,
  ImpersonationSessionDto,
  ImpersonationStartResponse,
  UserSearchResultDto,
  RequestConsentPayload,
  RejectConsentPayload,
} from '@/types/impersonation'

export const useImpersonationStore = defineStore('impersonation', () => {
  /* ---- State ---- */
  const consents = ref<ImpersonationConsentDto[]>([])
  const consentsTotalCount = ref(0)
  const sessions = ref<ImpersonationSessionDto[]>([])
  const sessionsTotalCount = ref(0)
  const userSearchResults = ref<UserSearchResultDto[]>([])
  const userSearchTotalCount = ref(0)
  const activeImpersonation = ref<ImpersonationStartResponse | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  /* ---- Getters ---- */
  const isImpersonating = computed(() => activeImpersonation.value !== null)
  const pendingConsents = computed(() =>
    consents.value.filter((c) => c.status === 'Pending'),
  )
  const activeSessions = computed(() =>
    sessions.value.filter((s) => s.status === 'Active'),
  )

  /* ---- Actions ---- */

  /**
   * Search users across all tenants.
   */
  async function searchUsers(params: {
    searchTerm?: string
    tenantId?: string
    page?: number
    pageSize?: number
  }): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      const result = await impersonationApi.searchUsers(params)
      userSearchResults.value = result.items
      userSearchTotalCount.value = result.totalCount
    } catch (err) {
      error.value = extractError(err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Request consent to impersonate a user.
   */
  async function requestConsent(
    payload: RequestConsentPayload,
  ): Promise<ImpersonationConsentDto | null> {
    isLoading.value = true
    error.value = null
    try {
      const consent = await impersonationApi.requestConsent(payload)
      consents.value.unshift(consent)
      consentsTotalCount.value++
      return consent
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load consent requests with optional filters.
   */
  async function loadConsents(params: {
    status?: string
    page?: number
    pageSize?: number
  }): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      const result = await impersonationApi.getConsents(params)
      consents.value = result.items
      consentsTotalCount.value = result.totalCount
    } catch (err) {
      error.value = extractError(err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Approve a pending consent request.
   */
  async function approveConsent(
    consentId: string,
  ): Promise<ImpersonationConsentDto | null> {
    isLoading.value = true
    error.value = null
    try {
      const updated = await impersonationApi.approveConsent(consentId)
      const index = consents.value.findIndex((c) => c.id === consentId)
      if (index !== -1) consents.value[index] = updated
      return updated
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Reject a pending consent request.
   */
  async function rejectConsent(
    consentId: string,
    payload: RejectConsentPayload,
  ): Promise<ImpersonationConsentDto | null> {
    isLoading.value = true
    error.value = null
    try {
      const updated = await impersonationApi.rejectConsent(consentId, payload)
      const index = consents.value.findIndex((c) => c.id === consentId)
      if (index !== -1) consents.value[index] = updated
      return updated
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Start an impersonation session.
   */
  async function startImpersonation(
    consentId: string,
  ): Promise<ImpersonationStartResponse | null> {
    isLoading.value = true
    error.value = null
    try {
      const response = await impersonationApi.startImpersonation({
        consentId,
      })
      activeImpersonation.value = response

      // Store impersonation state in sessionStorage (not localStorage)
      // so it doesn't persist across browser sessions
      sessionStorage.setItem(
        'impersonation_state',
        JSON.stringify({
          sessionId: response.impersonationSessionId,
          originalAccessToken: localStorage.getItem('access_token'),
          originalUser: localStorage.getItem('user'),
          originalTenantId: localStorage.getItem('tenant_id'),
        }),
      )

      // Switch to impersonated user's token
      localStorage.setItem('access_token', response.accessToken)
      localStorage.setItem(
        'tenant_id',
        response.targetUser.tenantId,
      )
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: response.targetUser.id,
          email: response.targetUser.email,
          firstName: response.targetUser.firstName,
          lastName: response.targetUser.lastName,
          tenantId: response.targetUser.tenantId,
          mfaEnabled: false,
          roles: response.targetUser.roles,
          permissions: response.targetUser.permissions,
        }),
      )

      return response
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * End an active impersonation session and restore original admin tokens.
   */
  async function endImpersonation(): Promise<boolean> {
    isLoading.value = true
    error.value = null
    try {
      const sessionId = activeImpersonation.value?.impersonationSessionId
      if (!sessionId) {
        // Try to get from sessionStorage
        const stored = sessionStorage.getItem('impersonation_state')
        if (stored) {
          const state = JSON.parse(stored)
          await impersonationApi.endImpersonation(state.sessionId)
          restoreOriginalSession(state)
          return true
        }
        error.value = 'No active impersonation session found.'
        return false
      }

      await impersonationApi.endImpersonation(sessionId)

      // Restore original admin session
      const stored = sessionStorage.getItem('impersonation_state')
      if (stored) {
        const state = JSON.parse(stored)
        restoreOriginalSession(state)
      }

      return true
    } catch (err) {
      error.value = extractError(err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load impersonation sessions with optional filters.
   */
  async function loadSessions(params: {
    status?: string
    page?: number
    pageSize?: number
  }): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      const result = await impersonationApi.getSessions(params)
      sessions.value = result.items
      sessionsTotalCount.value = result.totalCount
    } catch (err) {
      error.value = extractError(err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Check if there's an active impersonation from sessionStorage.
   */
  function checkActiveImpersonation(): boolean {
    const stored = sessionStorage.getItem('impersonation_state')
    return !!stored
  }

  /* ---- Helpers ---- */

  function restoreOriginalSession(state: {
    originalAccessToken: string
    originalUser: string
    originalTenantId: string
  }): void {
    if (state.originalAccessToken)
      localStorage.setItem('access_token', state.originalAccessToken)
    if (state.originalUser)
      localStorage.setItem('user', state.originalUser)
    if (state.originalTenantId)
      localStorage.setItem('tenant_id', state.originalTenantId)
    sessionStorage.removeItem('impersonation_state')
    activeImpersonation.value = null
  }

  function extractError(err: unknown): string {
    if (err instanceof Error) return err.message
    if (typeof err === 'object' && err !== null) {
      const e = err as { response?: { data?: { detail?: string } } }
      return e.response?.data?.detail ?? 'An unexpected error occurred.'
    }
    return 'An unexpected error occurred.'
  }

  return {
    /* State */
    consents,
    consentsTotalCount,
    sessions,
    sessionsTotalCount,
    userSearchResults,
    userSearchTotalCount,
    activeImpersonation,
    isLoading,
    error,
    /* Getters */
    isImpersonating,
    pendingConsents,
    activeSessions,
    /* Actions */
    searchUsers,
    requestConsent,
    loadConsents,
    approveConsent,
    rejectConsent,
    startImpersonation,
    endImpersonation,
    loadSessions,
    checkActiveImpersonation,
  }
})
