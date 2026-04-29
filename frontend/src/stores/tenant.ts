/**
 * Tenant Management Pinia Store.
 *
 * Manages state for the Super Admin tenant management module including:
 * - Tenant list with pagination, search, and status filtering
 * - Tenant CRUD operations
 * - Tenant status transitions
 * - Tenant selector for Super Admin context switching
 */
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as tenantService from '@/services/tenantService'
import type {
  TenantDto,
  TenantListItemDto,
  TenantStatusOption,
  CreateTenantRequest,
  UpdateTenantRequest,
  ChangeTenantStatusRequest,
  UpdateTenantBrandingRequest,
  OperatorResetTenantAdminPasswordRequest,
  SetupTenantAdminRequest,
  TenantListParams,
  TenantSelectorOption,
  TenantStatus,
} from '@/types/tenant'

export const useTenantStore = defineStore('tenant', () => {
  /* ---------------------------------------------------------------- */
  /*  State                                                            */
  /* ---------------------------------------------------------------- */

  /** List state */
  const tenants = ref<TenantListItemDto[]>([])
  const totalCount = ref(0)
  const totalPages = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(10)
  const searchQuery = ref('')
  const statusFilter = ref<TenantStatus | null>(null)

  /** Detail state */
  const currentTenant = ref<TenantDto | null>(null)

  /** Status options */
  const statusOptions = ref<TenantStatusOption[]>([])

  /** Selector state */
  const selectorOptions = ref<TenantSelectorOption[]>([])
  const selectedTenantId = ref<string>(
    localStorage.getItem('super_admin_tenant_id') || '',
  )

  /** UI state */
  const isLoading = ref(false)
  const isSubmitting = ref(false)
  const error = ref<string | null>(null)
  const successMessage = ref<string | null>(null)

  /* ---------------------------------------------------------------- */
  /*  Getters                                                          */
  /* ---------------------------------------------------------------- */

  const activeTenants = computed(() =>
    tenants.value.filter((t) => t.status === 1),
  )

  const suspendedTenants = computed(() =>
    tenants.value.filter((t) => t.status === 2),
  )

  const selectedTenant = computed(() =>
    selectorOptions.value.find((t) => t.id === selectedTenantId.value),
  )

  const hasSelectedTenant = computed(() => !!selectedTenantId.value)

  /* ---------------------------------------------------------------- */
  /*  Actions                                                          */
  /* ---------------------------------------------------------------- */

  /** Load paginated tenant list from API. */
  async function loadTenants(): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      const params: TenantListParams = {
        page: currentPage.value,
        pageSize: pageSize.value,
      }
      if (searchQuery.value) params.search = searchQuery.value
      if (statusFilter.value !== null) params.status = statusFilter.value

      const result = await tenantService.fetchTenantsList(params)
      tenants.value = result.items
      totalCount.value = result.totalCount
      totalPages.value = result.totalPages
    } catch (err) {
      /* Graceful degradation */ console.warn('[Tenants] API unavailable:', err)
      tenants.value = []
    } finally {
      isLoading.value = false
    }
  }

  /** Load tenant detail by ID. */
  async function loadTenantDetail(id: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      currentTenant.value = await tenantService.fetchTenantById(id)
    } catch (err) {
      error.value = extractError(err)
      currentTenant.value = null
    } finally {
      isLoading.value = false
    }
  }

  /** Create a new tenant. */
  async function createTenant(
    request: CreateTenantRequest,
  ): Promise<TenantDto | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const tenant = await tenantService.createTenant(request)
      successMessage.value = 'tenants.messages.createSuccess'
      await loadTenants()
      return tenant
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Update an existing tenant. */
  async function updateTenant(
    id: string,
    request: UpdateTenantRequest,
  ): Promise<TenantDto | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const tenant = await tenantService.updateTenant(id, request)
      successMessage.value = 'tenants.messages.updateSuccess'
      if (currentTenant.value?.id === id) {
        currentTenant.value = tenant
      }
      await loadTenants()
      return tenant
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Update tenant branding. */
  async function updateBranding(
    id: string,
    request: UpdateTenantBrandingRequest,
  ): Promise<TenantDto | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const tenant = await tenantService.updateTenantBranding(id, request)
      if (currentTenant.value?.id === id) {
        currentTenant.value = tenant
      }
      successMessage.value = 'tenants.messages.brandingSuccess'
      return tenant
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Change tenant lifecycle status. */
  async function changeStatus(
    id: string,
    request: ChangeTenantStatusRequest,
  ): Promise<TenantDto | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const tenant = await tenantService.changeTenantStatus(id, request)
      if (currentTenant.value?.id === id) {
        currentTenant.value = tenant
      }
      successMessage.value = 'tenants.messages.statusChanged'
      await loadTenants()
      return tenant
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Trigger database provisioning. */
  async function provision(id: string): Promise<TenantDto | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const tenant = await tenantService.provisionTenant(id)
      if (currentTenant.value?.id === id) {
        currentTenant.value = tenant
      }
      successMessage.value = 'tenants.messages.provisionSuccess'
      await loadTenants()
      return tenant
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Operator: Setup tenant admin credentials. */
  async function setupTenantAdmin(
    id: string,
    request: SetupTenantAdminRequest,
  ): Promise<boolean> {
    isSubmitting.value = true
    error.value = null

    try {
      await tenantService.setupTenantAdmin(id, request)
      successMessage.value = 'tenants.messages.setupAdminSuccess'
      return true
    } catch (err) {
      error.value = extractError(err)
      return false
    } finally {
      isSubmitting.value = false
    }
  }

  /** Operator: Reset tenant admin password. */
  async function resetTenantAdminPassword(
    id: string,
    request: OperatorResetTenantAdminPasswordRequest,
  ): Promise<boolean> {
    isSubmitting.value = true
    error.value = null

    try {
      await tenantService.operatorResetTenantAdminPassword(id, request)
      successMessage.value = 'tenants.messages.resetAdminPasswordSuccess'
      return true
    } catch (err) {
      error.value = extractError(err)
      return false
    } finally {
      isSubmitting.value = false
    }
  }

  /** Load available status options. */
  async function loadStatusOptions(): Promise<void> {
    try {
      statusOptions.value = await tenantService.fetchTenantStatuses()
    } catch {
      /* Silently ignore — not critical */
    }
  }

  /** Load tenant selector options for Super Admin. */
  async function loadSelectorOptions(): Promise<void> {
    try {
      selectorOptions.value =
        await tenantService.fetchTenantSelectorOptions()
    } catch {
      /* Silently ignore */
    }
  }

  /** Set the selected tenant for Super Admin context. */
  function selectTenant(tenantId: string): void {
    selectedTenantId.value = tenantId
    localStorage.setItem('super_admin_tenant_id', tenantId)
    localStorage.setItem('tenant_id', tenantId)
  }

  /** Clear tenant selection. */
  function clearTenantSelection(): void {
    selectedTenantId.value = ''
    localStorage.removeItem('super_admin_tenant_id')
  }

  /** Reset pagination and reload. */
  function resetAndReload(): void {
    currentPage.value = 1
    loadTenants()
  }

  /** Clear messages. */
  function clearMessages(): void {
    error.value = null
    successMessage.value = null
  }

  /* ---------------------------------------------------------------- */
  /*  Helpers                                                          */
  /* ---------------------------------------------------------------- */

  function extractError(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      const axiosErr = err as { response?: { data?: { error?: string; detail?: string } } }
      return (
        axiosErr.response?.data?.error ||
        axiosErr.response?.data?.detail ||
        'tenants.errors.unexpected'
      )
    }
    return 'tenants.errors.unexpected'
  }

  return {
    /* State */
    tenants,
    totalCount,
    totalPages,
    currentPage,
    pageSize,
    searchQuery,
    statusFilter,
    currentTenant,
    statusOptions,
    selectorOptions,
    selectedTenantId,
    isLoading,
    isSubmitting,
    error,
    successMessage,

    /* Getters */
    activeTenants,
    suspendedTenants,
    selectedTenant,
    hasSelectedTenant,

    /* Actions */
    loadTenants,
    loadTenantDetail,
    createTenant,
    updateTenant,
    updateBranding,
    changeStatus,
    provision,
    setupTenantAdmin,
    resetTenantAdminPassword,
    loadStatusOptions,
    loadSelectorOptions,
    selectTenant,
    clearTenantSelection,
    resetAndReload,
    clearMessages,
  }
})
